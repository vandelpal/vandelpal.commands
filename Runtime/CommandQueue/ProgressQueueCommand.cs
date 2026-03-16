using System;
using System.Collections.Generic;
using System.Threading;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    public class ProgressQueueCommand : QueueCommand, IProgressQueueCommand {
        protected readonly Dictionary<IProgressCommand, IProgressSettings> SettingsMap = new Dictionary<IProgressCommand, IProgressSettings>();
        private readonly Queue<IProgressSettings> _settingsQueue = new Queue<IProgressSettings>();
        private readonly ProgressRuntimeState _progressState = new ProgressRuntimeState();
        public override int CurrentPercent => _progressState.CompletedPercent + _progressState.RunningCurrentPercent;
        private CancellationTokenSource _fakeTimerCts;

        internal ProgressQueueCommand(CommandFailBehaviour behaviour, string name, ICommandLogger logger, ICommandBugTracker bugTracker)
            : base(behaviour, name, logger, bugTracker) {}

        public void AddProgress(IProgressCommand cmd, IProgressSettings settings = null) {
            settings ??= new ProgressSettings();
            if (SettingsMap.TryAdd(cmd, settings)) {
                base.Add(cmd);
                _settingsQueue.Enqueue(settings);
            }
        }

        public override void Add(ICommand cmd) {
            if (cmd is IProgressCommand pc) {
                AddProgress(pc);
            } else {
                throw new InvalidOperationException("ProgressQueue accepts only IProgressCommand.");
            }
        }
        public override void Add(IEnumerable<ICommand> list) {
            foreach (var c in list) {
                Add(c);
            }
        }
        public override void Add(params ICommand[] list) {
            foreach (var c in list) {
                Add(c);
            }
        }

        protected override void ExecInternal() {
            _progressState.ResetRunning();
            if (Queue.Count == 0) {
                _progressState.CompletedPercent = MAX_PERCENT;
                NotifyComplete();
                return;
            }
            _progressState.CompletedPercent = 0;
            CheckPercents();
            NotifyProgress();
            base.ExecInternal();
        }

        protected void CheckPercents() => ProgressSettings.DistributeAutoPercents(SettingsMap.Values, MAX_PERCENT);

        protected override void NotifyProgress() => OnProgress(this, CurrentPercent);

        public override void RetryCompletedCommand() {
            if (CompletedCommand == null) {
                return;
            }
            SettingsMap.TryGetValue((IProgressCommand)CompletedCommand, out var settings);
            settings ??= ProgressSettings.ZERO;
            var percent = Math.Max(0, settings.Percents);
            _progressState.CompletedPercent -= percent;
            TrySetFakeTimer(settings);
            base.RetryCompletedCommand();
        }

        protected override void Run() {
            while (true) {
                if (State != CommandState.Executing) {
                    return;
                }
                if (RunningCommand != null) {
                    return;
                }
                if (Queue.Count == 0) {
                    NotifyComplete();
                    return;
                }
                var cmd = (IProgressCommand)Queue.Dequeue();
                RunningCommand = cmd;
                if (!_settingsQueue.TryDequeue(out var settings) || settings == null) {
                    SettingsMap.TryGetValue(cmd, out settings);
                    settings ??= ProgressSettings.ZERO;
                }
                _progressState.RunningTotalPercent = Math.Max(0, settings.Percents);
                if (cmd.HasResult) {
                    _progressState.CompletedPercent += _progressState.RunningTotalPercent;
                    continue;
                }
                
                cmd.AddCompleteHandler(OnCommandComplete);
                if (!TrySetFakeTimer(settings)) {
                    cmd.AddProgressHandler(OnCommandProgress);
                }
                cmd.Execute();
                break;
            }
        }

        private bool TrySetFakeTimer(IProgressSettings settings) {
            if (settings is FakeProgressSettings fake) {
                _progressState.RunningFakeStep = fake.FakeStep;
                _progressState.RunningFakeTime = fake.FakeTime;
                StartFakeTimer();
                return true;
            }
            _progressState.ResetFakeTimerSettings();
            return false;
        }

        private void OnCommandProgress(ICommand cmd, int percent) {
            percent = Math.Min(percent, MAX_PERCENT);
            _progressState.RunningCurrentPercent = (_progressState.RunningTotalPercent * percent) / MAX_PERCENT;
            NotifyProgress();
        }

        protected virtual void OnFakeTimer() {
            if (_fakeTimerCts == null) {
                return;
            }
            if (_progressState.RunningCurrentPercent < _progressState.RunningTotalPercent) {
                _progressState.RunningCurrentPercent = Math.Min(
                    _progressState.RunningTotalPercent,
                    _progressState.RunningCurrentPercent + _progressState.RunningFakeStep);
                NotifyProgress();
            } else {
                StopFakeTimer();
            }
        }

        protected override void OnCommandComplete(ICommand cmd) {
            StopFakeTimer();
            _progressState.CompletedPercent += _progressState.RunningTotalPercent;
            _progressState.RunningCurrentPercent = 0;
            NotifyProgress();
            base.OnCommandComplete(cmd);
        }

        private void StopFakeTimer() {
            if (_fakeTimerCts == null) {
                return;
            }
            _fakeTimerCts.Cancel();
            _fakeTimerCts.Dispose();
            _fakeTimerCts = null;
        }

        protected override void PostExecuteActions() {
            CleanUp();
            base.PostExecuteActions();
        }

        public override void Reset() {
            CleanUp();
            base.Reset();
        }

        public void CleanUp() {
            StopFakeTimer();
            SettingsMap.Clear();
            _settingsQueue.Clear();
            Queue.Clear();
        }

        private void StartFakeTimer() {
            StopFakeTimer();
            _fakeTimerCts = new CancellationTokenSource();
            RunFakeTimerAsync(_fakeTimerCts.Token).Forget(MainThreadDispatcher.ReportUnhandledException);
        }

        private async UniTask RunFakeTimerAsync(CancellationToken token) {
            await UniTask.SwitchToMainThread();
            while (!token.IsCancellationRequested) {
                await UniTask.Delay(_progressState.RunningFakeTime, cancellationToken : token).SuppressCancellationThrow();
                if (token.IsCancellationRequested || State != CommandState.Executing || RunningCommand == null) {
                    break;
                }
                OnFakeTimer();
            }
        }

        private sealed class ProgressRuntimeState {
            public int RunningTotalPercent;
            public int RunningCurrentPercent;
            public int RunningFakeStep = FakeProgressSettings.FAKE_STEP_DEFAULT;
            public int RunningFakeTime = FakeProgressSettings.FAKE_TIME_MS;
            public int CompletedPercent;

            public void ResetRunning() {
                RunningTotalPercent = 0;
                RunningCurrentPercent = 0;
                ResetFakeTimerSettings();
            }

            public void Reset() {
                ResetRunning();
                CompletedPercent = 0;
            }

            public void ResetFakeTimerSettings() {
                RunningFakeStep = FakeProgressSettings.FAKE_STEP_DEFAULT;
                RunningFakeTime = FakeProgressSettings.FAKE_TIME_MS;
            }
        }
    }
}