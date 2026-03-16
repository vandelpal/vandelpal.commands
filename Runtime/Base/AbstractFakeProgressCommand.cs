using System;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public abstract class AbstractFakeProgressCommand : AbstractCommand, IProgressCommand {
        protected AbstractFakeProgressCommand(ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {}
        public int CurrentPercent { get; } = 0;
        public IProgressCommand AddProgressHandler(Action<IProgressCommand, int> progressHandler) => this;
        public IProgressCommand RemoveProgressHandler(Action<IProgressCommand, int> progressHandler) => this;
    }
}