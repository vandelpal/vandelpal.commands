using System;
using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class ProgressQueueCommandTests : TestsInstaller {
        [Test]
        public void Execute_CommandsWithSettings_DistributeProgress() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            queue.AddProgressHandler(mockHandler);
            var cmd1 = new TestProgressCommand();
            var cmd2 = new TestProgressCommand();
            var cmd3 = new TestProgressCommand();
            const int percent1 = 10;
            const int percent2 = 15;
            queue.AddProgress(cmd1, new ProgressSettings(percent1));
            queue.AddProgress(cmd2, new ProgressSettings(percent2));
            queue.AddProgress(cmd3, new ProgressSettings(ProgressSettings.CALC_AUTO));
            queue.Execute();
            mockHandler.Received().Invoke(queue, 0);
            cmd1.EmulateProgress(AbstractProgressCommand.MAX_PERCENT / 2);
            mockHandler.Received().Invoke(queue, Arg.Is<int>(p => p == percent1 / 2));
            cmd1.FinishSuccess();
            mockHandler.Received().Invoke(queue, percent1);
            cmd2.FinishSuccess();
            mockHandler.Received().Invoke(queue, percent1 + percent2);
            cmd3.EmulateProgress(AbstractProgressCommand.MAX_PERCENT / 2);
            var resultPercent3 = (AbstractProgressCommand.MAX_PERCENT - percent1 - percent2) / 2;
            mockHandler.Received().Invoke(queue, Arg.Is<int>(p => p == percent1 + percent2 + resultPercent3));
            cmd3.FinishSuccess();
            mockHandler.Received().Invoke(queue, AbstractProgressCommand.MAX_PERCENT);
        }

        [Test]
        public void Execute_CommandsWithSettingsManualMode_ResetProgress() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            queue.AddProgressHandler(mockHandler);
            var cmd1 = new TestProgressCommand();
            var cmd2 = new TestProgressCommand();
            const int percent1 = 40;
            const int percent2 = 60;
            queue.AddProgress(cmd1, new ProgressSettings(percent1));
            queue.AddProgress(cmd2, new ProgressSettings(percent2));
            queue.SetExecuteMode(QueueExecuteMode.Manual);
            queue.Execute();
            mockHandler.Received().Invoke(queue, 0);
            cmd1.FinishSuccess();
            mockHandler.Received().Invoke(queue, percent1);
            mockHandler.ClearReceivedCalls();
            queue.RetryCompletedCommand();
            mockHandler.Received().Invoke(queue, 0);
            mockHandler.ClearReceivedCalls();
            cmd1.FinishSuccess();
            mockHandler.Received().Invoke(queue, percent1);
            mockHandler.ClearReceivedCalls();
            queue.ContinueExecute();
            cmd2.FinishSuccess();
            mockHandler.Received().Invoke(queue, percent1 + percent2);
        }

        [Test]
        public void Execute_AutoCalcProgress_DistributeProgress() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            queue.AddProgressHandler(mockHandler);
            var cmd1 = new TestProgressCommand();
            var cmd2 = new TestProgressCommand();
            var cmd3 = new TestProgressCommand();
            var cmd4 = new TestProgressCommand();
            ICommand[] arr = { cmd1, cmd2, cmd3, cmd4 };
            queue.Add(arr);
            queue.Execute();
            mockHandler.Received().Invoke(queue, 0);
            var percentForCommand = AbstractProgressCommand.MAX_PERCENT / arr.Length;
            cmd1.EmulateProgress(20);
            var resultPercent = (percentForCommand * 20) / AbstractProgressCommand.MAX_PERCENT;
            mockHandler.Received().Invoke(queue, Arg.Is<int>(p => p == resultPercent));
            cmd1.FinishSuccess();
            cmd2.FinishSuccess();
            cmd3.FinishSuccess();
            mockHandler.Received().Invoke(queue, Arg.Is<int>(p => p == percentForCommand * 3));
            cmd4.FinishSuccess();
            mockHandler.Received().Invoke(queue, AbstractProgressCommand.MAX_PERCENT);
        }

        [Test]
        public void Execute_CommandWithFakeSettings_DontChangeProgressByCommand() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<IProgressCommand, int>>();
            queue.AddProgressHandler(mockHandler);
            var cmd = new TestProgressCommand();
            queue.AddProgress(cmd, new FakeProgressSettings());
            queue.Execute();
            mockHandler.Received().Invoke(queue, 0);
            mockHandler.ClearReceivedCalls();
            cmd.EmulateProgress(AbstractProgressCommand.MAX_PERCENT / 2);
            mockHandler.DidNotReceive().Invoke(queue, Arg.Any<int>());
            queue.Terminate();
        }

        [Test]
        public void Add_NoProgressCommand_ThrowException() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd = GetTestCommand();
            Assert.Catch<Exception>(() => queue.Add(cmd));
            Assert.Catch<Exception>(() => queue.Add(new[] { cmd, cmd }));
        }

        [Test]
        public void EmptyQueue_WhenExecute_CorrectFinish() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            queue.Execute();
            Assert.AreEqual(CommandState.Completed, queue.State);
            Assert.AreEqual(AbstractProgressCommand.MAX_PERCENT, queue.CurrentPercent);
        }

        [Test]
        public void AddProgress_SameCommandTwice_AddsOnlyOnce() {
            InstallDependencies();
            var queue = new ProgressQueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd = new TestProgressCommand();
            queue.AddProgress(cmd, new ProgressSettings(50));
            queue.AddProgress(cmd, new ProgressSettings(50));
            Assert.AreEqual(1, queue.QueueCount);
        }
    }
}