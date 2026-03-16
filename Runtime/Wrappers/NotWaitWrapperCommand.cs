using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public class NotWaitWrapperCommand : AbstractFakeProgressCommand {
        private readonly IProgressCommand _cmd;

        public NotWaitWrapperCommand(IProgressCommand cmd, ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {
            _cmd = cmd;
        }

        protected override void ExecInternal() {
            _cmd.Execute();
            NotifyComplete();
        }

        protected override bool NeedMeasureTime() => false;
    }
}