using System;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class AbstractProgressCommandTests : TestsInstaller {
        [Test]
        public void CurrentPercent_ByDefault_ReturnsZero() {
            var command = new TestProgressCommand();
            Assert.AreEqual(0, command.CurrentPercent);
        }

        [Test]
        public void OnProgress_WhenCalled_SetCurrentPercent() {
            var command = new TestProgressCommand();
            command.EmulateProgress(10);
            Assert.AreEqual(10, command.CurrentPercent);
        }

        [Test]
        public void OnProgress_WhenCalled_InvokeProgressHandler() {
            var command = new TestProgressCommand();
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            command.AddProgressHandler(mockHandler);
            command.EmulateProgress(10);
            mockHandler.Received(1).Invoke(command, 10);
        }

        [Test]
        public void WhenComplete_InvokeProgressHandlerWith_100Percent() {
            InstallDependencies();
            var command = new TestProgressCommand();
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            command.AddProgressHandler(mockHandler);
            command.Execute();
            command.FinishSuccess();
            mockHandler.Received(1).Invoke(command, 100);
        }

        [Test]
        public void WhenCompleteWithError_NoInvokeProgressHandler() {
            InstallDependencies();
            var command = new TestProgressCommand();
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            command.AddProgressHandler(mockHandler);
            command.Execute();
            command.FinishFail();
            mockHandler.DidNotReceive().Invoke(command, Arg.Any<int>());
        }
    }

    public class TestProgressCommand : AbstractProgressCommand {
        protected bool ThrowException;
        public TestProgressCommand() : base(Substitute.For<ICommandLogger>(), Substitute.For<ICommandBugTracker>()) {}

        protected override void ExecInternal() {
            if (ThrowException) {
                throw new Exception("Test");
            }
        }

        public void SetThrowException(bool value) => ThrowException = value;
        public void FinishSuccess() => NotifyComplete();
        public void FinishFail() => NotifyComplete(BaseCommandsErrors.UnknownError);
        public void EmulateProgress(int percent) => OnProgress(percent);
    }

    public class TestProgressAsyncCommand : TestProgressCommand {
        protected override void ExecInternal() => TryExecInternalAsync();
        protected override async UniTask ExecInternalAsync() {
            if (ThrowException) {
                throw new InvalidOperationException("");
            }
            await base.ExecInternalAsync();
        }
    }
}