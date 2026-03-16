using System;
using System.Collections.Generic;
using System.Text;
using Vandelpal.Commands.Api;
using Vandelpal.Commands.Profiler;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    public abstract class AbstractCommand : ICommand {
        private static readonly HashSet<ICommand> _executingCache = new HashSet<ICommand>();
        public static int CacheSize => _executingCache.Count;
        internal static void ClearExecutingCache() => _executingCache.Clear();

        protected ICommandLogger Logger { get; }
        protected ICommandBugTracker BugTracker { get; }

        public CommandError Error { get; protected internal set; } = BaseCommandsErrors.NoError;
        public string ErrorMessage => Error.Message;

        private UniTaskCompletionSource<ICommand> _promise;
        private OneShotTimer _timeoutTimer;
        private event Action<ICommand> CompleteEvent;
        private event Action<ICommand> SucceedEvent;

        public CommandState State { get; private set; } = CommandState.NotStarted;
        public bool IsExecuting => State == CommandState.Executing;
        public bool IsSucceed => State == CommandState.Completed && Error == BaseCommandsErrors.NoError;
        public bool HasResult => State != CommandState.NotStarted && State != CommandState.Executing;
        public bool HasError => Error != BaseCommandsErrors.NoError;

        protected float StartTime { get; private set; }
        protected static float CurrentTime => CommandTime.RealtimeSinceStartup;
        public float ExecuteTime { get; private set; }
        public int ExecuteTimeInMs => (int) Math.Round(ExecuteTime * 1000f, MidpointRounding.AwayFromZero);
        protected ITimeNode TimeInfo { get; private set; }

        protected AbstractCommand(ICommandLogger logger, ICommandBugTracker bugTracker) {
            Logger = logger;
            BugTracker = bugTracker;
        }

        public ICommand SetTimeout(int milliseconds) {
            StopTimeoutTimer();
            _timeoutTimer = new OneShotTimer(milliseconds);
            _timeoutTimer.OnTimePassed += OnTimeout;
            return this;
        }

        public void Execute() {
            if (State != CommandState.NotStarted) {
                return;
            }
            CommonExecutePart();
        }

        public UniTask<ICommand> ExecuteAsync() {
            if (State != CommandState.NotStarted && _promise == null) {
                throw new InvalidOperationException($"Command already executed (no async): {GetLogName()}, state={State}");
            }
            if (_promise != null) {
                return _promise.Task;
            }
            _promise = new UniTaskCompletionSource<ICommand>();
            CommonExecutePart();
            return _promise.Task;
        }

        private void CommonExecutePart() {
            MainThreadDispatcher.SetLogger(Logger);
            _timeoutTimer?.Start();
            StartTime = CurrentTime;
            
            if (Profiler.TimeInfo.InMeasure && NeedMeasureTime()) {
                TimeInfo = TimeNodeFactory.Create(GetLogName());
            }

            Logger?.LogInfo("{0}: Start at {1:f2}", GetLogName(), StartTime);

            SetState(CommandState.Executing);
            try {
                ExecInternal();
            } catch (Exception e) {
                HandleExecuteException(e);
            }
        }

        protected void HandleExecuteException(Exception e) {
            NotifyExceptionToBugTracker(e, $"Command execute exception: {e.Message}");
            Error = new CommandError(BaseCommandsErrors.InternalCmdExceptionError, e);
            SetStateOnlyIfExecuting(CommandState.Failed);
        }

        public virtual void Terminate() => SetStateOnlyIfExecuting(CommandState.Terminated);

        protected void TryExecInternalAsync() {
            ExecInternalAsync().Forget(HandleExecuteException);
        }

        protected abstract void ExecInternal();
        protected virtual UniTask ExecInternalAsync() => UniTask.CompletedTask;
        protected virtual void PostExecuteActions() {}
        protected virtual bool NeedMeasureTime() => false;

        public ICommand AddCompleteHandler(Action<ICommand> completeHandler) {
            CompleteEvent += completeHandler;
            return this;
        }

        public void RemoveCompleteHandler(Action<ICommand> completeHandler) => CompleteEvent -= completeHandler;
        public ICommand AddSucceedHandler(Action<ICommand> succeedHandler) {
            SucceedEvent += succeedHandler;
            return this;
        }
        public void RemoveSucceedHandler(Action<ICommand> succeedHandler) => SucceedEvent -= succeedHandler;

        protected void NotifyComplete(CommandError error) {
            Error = error;
            NotifyComplete();
        }
        protected void NotifyComplete() => SetStateOnlyIfExecuting(CommandState.Completed);

        public void Retry() {
            if (_promise != null) {
                LogWarning("Retry is not supported for async commands");
                return;
            }
            if (State == CommandState.NotStarted) {
                LogWarning("Retry called before start");
                return;
            }
            Reset();
            Execute();
        }

        public virtual void Reset() {
            StopTimeoutTimer();
            SetState(CommandState.NotStarted);
            Error = BaseCommandsErrors.NoError;
        }

        private void SetStateOnlyIfExecuting(CommandState state) {
            if (State != CommandState.Executing) {
                LogWarning($"Set state to '{state}' when not Executing");
                return;
            }
            SetState(state);
        }

        private void SetState(CommandState state) {
            if (State == state) {
                return;
            }
            State = state;
            switch (State) {
                case CommandState.Executing:
                    _executingCache.Add(this);
                    break;
                case CommandState.Completed:
                case CommandState.Failed:
                case CommandState.Terminated:
                case CommandState.Timeout:
                    try {
                        _executingCache.Remove(this);
                        StopTimeoutTimer();
                        PostExecuteActions();
                        TimeInfo?.Dispose();
                        TimeInfo = null;
                        ExecuteTime = CurrentTime - StartTime;
                        ProcessEvaluatedDuration();
                    } catch (Exception e) {
                        NotifyExceptionToBugTracker(e, "Exception on processing command result");
                        Error = new CommandError(BaseCommandsErrors.InternalCmdExceptionError, e);
                    } finally {
                        InvokeCompletionHandlers();
                    }
                    break;
            }
        }

        private void InvokeCompletionHandlers() {
            try {
                CompleteEvent?.Invoke(this);
            } catch (Exception e) {
                NotifyExceptionToBugTracker(e, $"completeEvent: {e.Message}");
                Error = BaseCommandsErrors.CompleteEventCmdExceptionError;
            }
            try {
                if (IsSucceed) {
                    SucceedEvent?.Invoke(this);
                }
            } catch (Exception e) {
                NotifyExceptionToBugTracker(e, $"succeedEvent: {e.Message}");
                Error = BaseCommandsErrors.CompleteEventCmdExceptionError;
            }
            CompleteEvent = null;
            SucceedEvent = null;
            _promise?.TrySetResult(this);
        }

        private void OnTimeout() => SetStateOnlyIfExecuting(CommandState.Timeout);
        private void StopTimeoutTimer() {
            if (_timeoutTimer == null) {
                return;
            }
            _timeoutTimer.OnTimePassed -= OnTimeout;
            _timeoutTimer.StopAndDispose();
            _timeoutTimer = null;
        }

        protected virtual void ProcessEvaluatedDuration() {
            var sb = new StringBuilder(GetLogName()).Append(": Finished. ");
            if (!IsSucceed) {
                sb.Append("State=").Append(State);
                if (Error != BaseCommandsErrors.NoError) {
                    sb.Append(", Error=").Append(Error);
                }
                if (!string.IsNullOrEmpty(ErrorMessage)) {
                    sb.Append(", ErrorMessage=").Append(ErrorMessage);
                }
                sb.Append(", ");
            }
            sb.Append("executeTime=").Append(ExecuteTimeInMs).Append(" ms");
            Logger?.LogInfo(sb.ToString());
        }

        public override string ToString() {
            var sb = new StringBuilder(GetType().Name);
            sb.Append(' ').Append(nameof(State)).Append('=').Append(State);
            if (Error != BaseCommandsErrors.NoError) {
                sb.Append(' ').Append(nameof(Error)).Append('=').Append(Error);
            }
            if (_timeoutTimer != null) {
                sb.Append(" Timeout=").Append(_timeoutTimer.TimeLeftMilliseconds);
            }
            return sb.ToString();
        }

        protected ICommandBugData CreateBugData(Exception exception, string message = "") {
            message += "\ninfo=" + GetInfoToBugTracker();
            var bugData = BugTracker.CreateBugData(GetType(), message);
            bugData?.SetMessage(message);
            bugData?.SetException(exception);
            return bugData;
        }

        protected virtual void NotifyExceptionToBugTracker(Exception exception, string message = "") {
            try {
                var bugData = CreateBugData(exception, message);
                if (bugData != null) {
                    BugTracker.ReportBug(bugData);
                }
                LogException(exception, bugData?.ToString());
            } catch (Exception ex) {
                LogException(ex, "Exception creating bug data");
            }
        }

        protected void NotifyErrorToBugTracker(string error) {
            try {
                var message = error + "\ninfo=" + GetInfoToBugTracker();
                var bugData = BugTracker.CreateBugData(GetType(), message);
                bugData?.SetMessage(message);
                if (bugData != null) {
                    BugTracker.ReportBug(bugData);
                }
            } catch (Exception ex) {
                LogException(ex, "Exception creating bug data");
            }
        }

        protected virtual string GetInfoToBugTracker() => GetLogName();
        protected void LogException(Exception exception, string logMessage = null) {
            Logger?.LogError(exception, null, "{0}: {1}", GetLogName(), logMessage ?? exception?.Message);
        }
        protected void LogError(string logMessage) => Logger?.LogError("{0}: {1}", GetLogName(), logMessage);
        protected void LogWarning(string logMessage) => Logger?.LogWarning("{0}: {1}", GetLogName(), logMessage);
        protected void LogInfo(string logMessage) => Logger?.LogInfo("{0}: {1}", GetLogName(), logMessage);

        protected virtual string GetLogName() => GetType().Name;
    }
}