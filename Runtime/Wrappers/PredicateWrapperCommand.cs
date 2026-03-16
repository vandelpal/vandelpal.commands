using System;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public class PredicateWrapperCommand : AbstractFakeProgressCommand {
        private readonly Func<bool> _predicate;
        private readonly string _logName;

        public PredicateWrapperCommand(Func<bool> predicate, ICommandLogger logger, ICommandBugTracker bugTracker, string logName = null) : base(logger, bugTracker) {
            _predicate = predicate;
            _logName = logName ?? predicate?.Method?.Name ?? "Predicate";
        }

        protected override void ExecInternal() {
            var ok = _predicate.Invoke();
            if (!ok) {
                Error = BaseCommandsErrors.UnknownError;
            }
            NotifyComplete();
        }

        protected override bool NeedMeasureTime() => true;
        protected override string GetLogName() => _logName;
    }
}