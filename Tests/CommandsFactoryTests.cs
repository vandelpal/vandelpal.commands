using Vandelpal.Commands.Api;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class CommandsFactoryTests : TestsInstaller {
        private CommandsFactory _factory;

        [SetUp]
        public void CreateFactory() {
            InstallDependencies();
            _factory = new CommandsFactory(_logger, _bugTracker);
        }

        [Test]
        public void GetQueueCommand_ReturnsQueue_ThatExecutesCommands() {
            var queue = _factory.GetQueueCommand("Test");
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.Execute();

            Assert.AreEqual(cmd1, queue.RunningCommand);
            cmd1.CompleteManually();
            Assert.AreEqual(cmd2, queue.RunningCommand);
            cmd2.CompleteManually();
            Assert.AreEqual(CommandState.Completed, queue.State);
        }

        [Test]
        public void GetManualQueueCommand_ReturnsQueue_ThatRequiresContinueExecute() {
            var queue = _factory.GetManualQueueCommand("Manual");
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var secondStarted = false;
            cmd2.HandleExecInternal += () => secondStarted = true;
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.Execute();

            cmd1.CompleteManually();
            Assert.IsFalse(secondStarted, "Manual queue must not run next until ContinueExecute");
            queue.ContinueExecute();
            Assert.IsTrue(secondStarted);
            cmd2.CompleteManually();
        }

        [Test]
        public void GetQueueCommand_WithParams_AddsCommandsToQueue() {
            var c1 = GetTestCommand();
            var c2 = GetTestCommand();
            var queue = _factory.GetQueueCommand(c1, c2);
            Assert.AreEqual(2, queue.QueueCount);
        }

        [Test]
        public void GetProgressQueueCommand_ReturnsProgressQueue_ThatAcceptsAddProgress() {
            var queue = _factory.GetProgressQueueCommand("Progress");
            var cmd = new TestProgressCommand();
            queue.AddProgress(cmd, new ProgressSettings(50));
            queue.Execute();
            cmd.FinishSuccess();
            Assert.AreEqual(CommandState.Completed, queue.State);
        }

        [Test]
        public void GetProgressQueueCommand_WithParams_AddsCommands() {
            var c1 = new TestProgressCommand();
            var c2 = new TestProgressCommand();
            var queue = _factory.GetProgressQueueCommand("Progress", c1, c2);
            Assert.AreEqual(2, queue.QueueCount);
        }
    }
}