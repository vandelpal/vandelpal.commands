using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class WrapperCommandsTests : TestsInstaller {
        [Test]
        public void NotWaitWrapperCommand_WhenExecute_CompletesImmediately() {
            InstallDependencies();
            var innerCommand = Substitute.For<IProgressCommand>();
            var command = new NotWaitWrapperCommand(innerCommand, _logger, _bugTracker);

            command.Execute();

            innerCommand.Received(1).Execute();
            Assert.AreEqual(CommandState.Completed, command.State);
            Assert.IsTrue(command.IsSucceed);
        }

        [Test]
        public void PredicateWrapperCommand_WhenPredicateFalse_CompletedWithError() {
            InstallDependencies();
            var command = new PredicateWrapperCommand(() => false, _logger, _bugTracker, "PredicateFalse");

            command.Execute();

            Assert.AreEqual(CommandState.Completed, command.State);
            Assert.IsTrue(command.HasError);
            Assert.IsFalse(command.IsSucceed);
        }

        [Test]
        public void PredicateWrapperCommand_WhenPredicateTrue_CompletedWithoutError() {
            InstallDependencies();
            var command = new PredicateWrapperCommand(() => true, _logger, _bugTracker, "PredicateTrue");

            command.Execute();

            Assert.AreEqual(CommandState.Completed, command.State);
            Assert.IsFalse(command.HasError);
            Assert.IsTrue(command.IsSucceed);
        }
    }
}
