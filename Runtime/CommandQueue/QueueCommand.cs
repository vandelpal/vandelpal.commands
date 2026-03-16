using System;
using System.Collections.Generic;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public class QueueCommand : AbstractProgressCommand, IQueueCommand {
        protected readonly Queue<ICommand> Queue = new Queue<ICommand>();
        private readonly string _name;
        internal readonly CommandFailBehaviour FailBehaviour;
        
        public int QueueCount => Queue.Count;
        private int? _countInQueueOnStart;
        public ICommand CompletedCommand { get; protected set; }
        public ICommand RunningCommand { get; protected set; }
        internal bool SomeCmdInQueueNoCompleted { get; private set; }
        public event Action<IQueueCommand> CommandCompleteEvent;
       
        private QueueExecuteMode _mode = QueueExecuteMode.Auto;

        internal QueueCommand(CommandFailBehaviour behaviour, string name, ICommandLogger logger, ICommandBugTracker bugTracker)
            : base(logger, bugTracker) {
            FailBehaviour = behaviour;
            _name = name;
        }

        public void SetExecuteMode(QueueExecuteMode mode) {
            if (State != CommandState.NotStarted) {
                return;
            }
            _mode = mode;
        }

        public bool IsContains(ICommand cmd) => Queue.Contains(cmd);
        public virtual void Add(ICommand c) => Queue.Enqueue(c);
        public virtual void Add(IEnumerable<ICommand> list) {
            foreach (var c in list) {
                Queue.Enqueue(c);
            }
        }
        public virtual void Add(params ICommand[] list) {
            foreach (var c in list) {
                Queue.Enqueue(c);
            }
        }
        public IQueueCommand AddCommandCompleteHandler(Action<IQueueCommand> completeHandler) {
            CommandCompleteEvent += completeHandler;
            return this;
        }
        
        public virtual void ContinueExecute() => Run();
        public virtual void RetryCompletedCommand() {
            if (CompletedCommand == null) {
                return;
            }
            NotifyProgress();
            RunningCommand = CompletedCommand;
            RunningCommand.AddCompleteHandler(OnCommandComplete);
            RunningCommand.Retry();
        }

        protected override void ExecInternal() {
            _countInQueueOnStart = Queue.Count;
            SomeCmdInQueueNoCompleted = false;
            Run();
        }

        public override void Reset() {
            if (RunningCommand != null) {
                RunningCommand.RemoveCompleteHandler(OnCommandComplete);
                RunningCommand.Terminate();
            }

            RunningCommand = null;
            CompletedCommand = null;
            SomeCmdInQueueNoCompleted = false;
            _countInQueueOnStart = null;
            Queue.Clear();

            base.Reset();
        }

        protected override void PostExecuteActions() {
            CommandCompleteEvent = null;
            RunningCommand?.Terminate();
            RunningCommand = null;
            base.PostExecuteActions();
        }

        protected virtual void Run() {
            if (State != CommandState.Executing) {
                return;
            }
            NotifyProgress();
            if (RunningCommand != null) {
                return;
            }
            if (Queue.Count == 0) {
                NotifyComplete();
                return;
            }
            RunningCommand = Queue.Dequeue();
            RunningCommand.AddCompleteHandler(OnCommandComplete);
            RunningCommand.Execute();
        }

        protected virtual void OnCommandComplete(ICommand cmd) {
            if (RunningCommand == null) {
                return;
            }
            CompletedCommand = RunningCommand;
            RunningCommand = null;
            CommandCompleteEvent?.Invoke(this);
            if (_mode == QueueExecuteMode.Manual) {
                return;
            }
            if (CompletedCommand != null && !CompletedCommand.IsSucceed) {
                SomeCmdInQueueNoCompleted = true;
                if (FailBehaviour == CommandFailBehaviour.Terminate) {
                    if (Error == BaseCommandsErrors.NoError) {
                        Error = new CommandError(BaseCommandsErrors.CommandInQueueFailedError, $" fail cmd = {CompletedCommand}");
                    }
                    Terminate();
                    return;
                }
            }
            ContinueExecute();
        }

        protected virtual void NotifyProgress() {
            var countInQueueOnStart = _countInQueueOnStart ?? 0;
            OnProgress(this, countInQueueOnStart == 0 ? MAX_PERCENT : MAX_PERCENT * (countInQueueOnStart - GetCurrentProgressCmdCount()) / countInQueueOnStart);
        }

        private int GetCurrentProgressCmdCount() => RunningCommand != null ? QueueCount + 1 : QueueCount;
        protected override string GetLogName() => string.IsNullOrEmpty(_name) ? base.GetLogName() : base.GetLogName() + " " + _name;
    }
}