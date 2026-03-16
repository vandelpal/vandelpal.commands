using System;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public abstract class AbstractProgressCommand : AbstractCommand, IProgressCommand {
        public const int MAX_PERCENT = 100;

        private event Action<IProgressCommand, int> ProgressEvent;

        public virtual int CurrentPercent { get; private set; }

        protected AbstractProgressCommand(ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {}

        public IProgressCommand AddProgressHandler(Action<IProgressCommand, int> progressHandler) {
            if (progressHandler != null) {
                ProgressEvent += progressHandler;
            }
            return this;
        }
        public IProgressCommand RemoveProgressHandler(Action<IProgressCommand, int> progressHandler) {
            if (progressHandler != null) {
                ProgressEvent -= progressHandler;
            }
            return this;
        }

        protected virtual void OnProgress(int percent) {
            CurrentPercent = percent;
            ProgressEvent?.Invoke(this, percent);
        }
        protected void OnProgress(ICommand cmd, int percent) => OnProgress(percent);

        protected override void PostExecuteActions() {
            if (IsSucceed) {
                OnProgress(MAX_PERCENT);
            }
            ProgressEvent = null;
            base.PostExecuteActions();
        }
    }
}