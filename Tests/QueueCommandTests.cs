using System;
using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class QueueCommandTests : TestsInstaller {
        [Test]
        public void ctor_ByDefault_QueueIsEmpty() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            Assert.AreEqual(0, queue.QueueCount);
        }

        [Test]
        public void Execute_EmptyQueue_CompletesImmediately() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<ICommand>>();
            queue.AddCompleteHandler(mockHandler);
            queue.Execute();
            Assert.AreEqual(CommandState.Completed, queue.State);
            mockHandler.Received(1).Invoke(queue);
        }

        [Test]
        public void ctor_ByDefault_FailBehaviourIsContinue() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            Assert.AreEqual(CommandFailBehaviour.Continue, queue.FailBehaviour);
        }

        [Test]
        public void Add_CommandsAsParams_AddToQueue() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            queue.Add(GetTestCommand());
            Assert.AreEqual(1, queue.QueueCount);
        }

        [Test]
        public void Execute_QueueWithTwoCommands_RunOnlyFirstCommand() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd1 = GetTestCommand();
            var mockHandler1 = Substitute.For<Action>();
            cmd1.HandleExecInternal += mockHandler1;
            var cmd2 = GetTestCommand();
            var mockHandler2 = Substitute.For<Action>();
            cmd2.HandleExecInternal += mockHandler2;
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.Execute();
            mockHandler1.Received(1).Invoke();
            mockHandler2.DidNotReceive().Invoke();
            cmd1.CompleteManually();
            cmd2.CompleteManually();
        }

        [Test]
        public void Execute_QueueWithTwoCommands_RunSecondAfterFirstComplete() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var mockHandler = Substitute.For<Action>();
            cmd2.HandleExecInternal += mockHandler;
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.Execute();
            cmd1.CompleteManually();
            mockHandler.Received(1).Invoke();
            cmd2.CompleteManually();
        }

        [Test]
        public void Execute_QueueWithTwoCommandsManual_NotRunSecondAfterFirstComplete() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var mockHandler = Substitute.For<Action>();
            cmd2.HandleExecInternal += mockHandler;
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.SetExecuteMode(QueueExecuteMode.Manual);
            queue.Execute();
            cmd1.CompleteManually();
            Assert.AreEqual(CommandState.NotStarted, cmd2.State);
            queue.ContinueExecute();
            Assert.AreEqual(CommandState.Executing, cmd2.State);
            cmd2.CompleteManually();
        }

        [Test]
        public void AddCommandCompleteHandler_WhenConcreteCommandComplete_ReceiveEventWithCmd() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<IQueueCommand>>();
            queue.AddCommandCompleteHandler(mockHandler);
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            queue.Add(cmd1);
            queue.Add(cmd2);
            queue.Execute();
            cmd1.CompleteManually();
            mockHandler.Received(1).Invoke(Arg.Is<QueueCommand>(x => x.CompletedCommand == cmd1));
            cmd2.CompleteManually();
        }

        [Test]
        public void CommandFailBehaviourContinue_WhenTerminateFirstCmd_ExecuteSecond() {
            InstallDependencies();
            var queue = new QueueCommand(CommandFailBehaviour.Continue, "test_queue", _logger, _bugTracker);
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var mockHandler = Substitute.For<Action>();
            cmd2.HandleExecInternal += mockHandler;
            var mockQueueCompleteHandler = Substitute.For<Action<ICommand>>();
            queue.AddCompleteHandler(mockQueueCompleteHandler);
            queue.Add(cmd1, cmd2);
            queue.Execute();
            Assert.False(queue.SomeCmdInQueueNoCompleted);
            cmd1.Terminate();
            mockHandler.Received(1).Invoke();
            cmd2.CompleteManually();
        }
    }
}