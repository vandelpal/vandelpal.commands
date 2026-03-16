using System;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    public class WaitSecondsCommand : AbstractFakeProgressCommand {
        private readonly float _seconds;

        public WaitSecondsCommand(float seconds, ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {
            _seconds = seconds;
        }

        protected override void ExecInternal() => TryExecInternalAsync();

        protected override async UniTask ExecInternalAsync() {
            await UniTask.Delay(TimeSpan.FromSeconds(_seconds), true);
            NotifyComplete();
        }

        protected override bool NeedMeasureTime() => true;
        protected override string GetLogName() => base.GetLogName() + " " + nameof(_seconds) + " = " + _seconds;
    }
}