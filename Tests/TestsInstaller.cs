using System;
using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    public abstract class TestsInstaller {
        protected ICommandLogger _logger;
        protected ICommandBugTracker _bugTracker;
        protected ICommandBugData _bugData;

        protected void InstallDependencies() {
            _logger = Substitute.For<ICommandLogger>();
            _bugTracker = Substitute.For<ICommandBugTracker>();
            _bugData = Substitute.For<ICommandBugData>();
            _bugData.SetMessage(Arg.Any<string>()).Returns(_bugData);
            _bugData.SetException(Arg.Any<Exception>()).Returns(_bugData);
            _bugTracker.CreateBugData(Arg.Any<Type>(), Arg.Any<string>()).Returns(_bugData);
        }

        [TearDown]
        public virtual void Teardown() {
            CommandTime.SetProvider(null);
            AbstractCommand.ClearExecutingCache();
        }

        protected TestCommand GetTestCommand() => new TestCommand(_logger, _bugTracker);
    }

    public class TestCommand : AbstractCommand {
        public event Action HandleExecInternal;

        protected override void ExecInternal() => HandleExecInternal?.Invoke();

        public void CompleteManually() => NotifyComplete();
        public void FailManually() => NotifyComplete(new CommandError("testError"));
        public void CallRetry() => Retry();

        public TestCommand(ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {}
    }
}