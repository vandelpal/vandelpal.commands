using System;
using System.Collections.Generic;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public class BoxCommand : AbstractProgressCommand {
        private readonly List<ICommand> _queue = new List<ICommand>();
        private readonly List<ICommand> _activeQueue = new List<ICommand>();
        private readonly Dictionary<ICommand, int> _progressMap = new Dictionary<ICommand, int>();
        private readonly Dictionary<ICommand, IProgressSettings> _settingsMap = new Dictionary<ICommand, IProgressSettings>();
        internal int QueueCount => _queue.Count;
        public ICommand CompletedCommand { get; private set; }
        public event Action<BoxCommand> CommandCompleteEvent;
        private readonly string _name;
        private readonly CommandFailBehaviour _failBehaviour;

        public BoxCommand(CommandFailBehaviour failBehavior, string name, ICommandLogger logger, ICommandBugTracker bugTracker, params ICommand[] commands)
            : base(logger, bugTracker) {
            _failBehaviour = failBehavior;
            _name = name;

            if (commands is { Length: > 0 }) {
                foreach (var c in commands) {
                    Add(c);
                }
            }
        }

        public void Add(ICommand c, IProgressSettings settings = null) {
            settings ??= new ProgressSettings();
            _queue.Add(c);
            _settingsMap.TryAdd(c, settings);
        }

        public void AddList(IEnumerable<ICommand> list) {
            foreach (var c in list) {
                Add(c);
            }
        }

        protected override void ExecInternal() {
            InitProgressMap();
            if (QueueCount == 0) {
                NotifyComplete();
                return;
            }
            foreach (var c in _queue) {
                AddHandlers(c);
            }
            CheckPercents();
            NotifyProgress();
            foreach (var c in _queue.ToArray()) {
                _activeQueue.Add(c);
                c.Execute();
            }
        }

        private void InitProgressMap() {
            foreach (var c in _queue) {
                _progressMap[c] = 0;
            }
        }

        private void AddHandlers(ICommand c) {
            c.RemoveCompleteHandler(OnCommandComplete);
            c.AddCompleteHandler(OnCommandComplete);

            if (c is IProgressCommand pc) {
                pc.RemoveProgressHandler(OnCommandProgress);
                pc.AddProgressHandler(OnCommandProgress);
            }
        }

        private void CheckPercents() => ProgressSettings.DistributeAutoPercents(_settingsMap.Values, MAX_PERCENT);

        public IProgressCommand AddCommandCompleteHandler(Action<BoxCommand> completeHandler) {
            CommandCompleteEvent += completeHandler;
            return this;
        }
        
        private void OnCommandComplete(ICommand c) {
            if (c.IsSucceed) {
                _progressMap[c] = MAX_PERCENT;
                NotifyProgress();
            }

            CompletedCommand = c;
            CommandCompleteEvent?.Invoke(this);
            _activeQueue.Remove(CompletedCommand);

            if (!CompletedCommand.IsSucceed && _failBehaviour == CommandFailBehaviour.Terminate) {
                if (Error == BaseCommandsErrors.NoError) {
                    Error = new CommandError(BaseCommandsErrors.CommandInQueueFailedError, $" fail cmd = {CompletedCommand}");
                }
                Terminate();
                return;
            }

            _queue.Remove(CompletedCommand);

            if (QueueCount == 0) {
                NotifyComplete();
            }
        }

        private void OnCommandProgress(IProgressCommand c, int percent) {
            _progressMap[c] = percent;
            NotifyProgress();
        }

        private void NotifyProgress() {
            var total = 0;
            foreach (var kv in _progressMap) {
                var pct = _settingsMap[kv.Key].Percents;
                if (pct == 0) {
                    continue;
                }
                total += (int)Math.Ceiling(kv.Value * (double)pct / MAX_PERCENT);
            }
            OnProgress(Math.Min(MAX_PERCENT, total));
        }

        protected override void PostExecuteActions() {
            foreach (var cmd in _activeQueue.ToArray()) {
                cmd.Terminate();
            }
            base.PostExecuteActions();
        }

        protected override string GetLogName() => string.IsNullOrEmpty(_name) ? base.GetLogName() : base.GetLogName() + " " + _name;

        public override void Reset() {
            base.Reset();
            foreach (var c in _queue) {
                c.Reset();
            }
        }
    }
}