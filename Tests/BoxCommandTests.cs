using System;
using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class BoxCommandTests : TestsInstaller {
        [Test]
        public void ctor_ByDefault_QueueIsEmpty() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            Assert.AreEqual(0, box.QueueCount);
        }

        [Test]
        public void ctor_CommandsAsParams_AddToQueue() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker, GetTestCommand(), GetTestCommand());
            Assert.AreEqual(2, box.QueueCount);
        }

        [Test]
        public void Add_CommandsAsParams_AddToQueue() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            box.Add(GetTestCommand());
            Assert.AreEqual(1, box.QueueCount);
        }

        [Test]
        public void ExecInternal_QueueIsEmpty_CallCompleteHandler() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<ICommand>>();
            box.AddCompleteHandler(mockHandler);
            box.Execute();
            mockHandler.Received(1).Invoke(box);
        }

        [Test]
        public void ExecInternal_QueueWithCommands_RunAllCommands() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var cmd1 = GetTestCommand();
            var mockHandler1 = Substitute.For<Action>();
            cmd1.HandleExecInternal += mockHandler1;
            var cmd2 = GetTestCommand();
            var mockHandler2 = Substitute.For<Action>();
            cmd2.HandleExecInternal += mockHandler2;
            box.Add(cmd1);
            box.Add(cmd2);
            box.Execute();
            mockHandler1.Received(1).Invoke();
            mockHandler2.Received(1).Invoke();
            cmd1.CompleteManually();
            cmd2.CompleteManually();
        }

        [Test]
        public void AddCommandCompleteHandler_WhenConcreteCommandComplete_ReceiveEventWithCmd() {
            InstallDependencies();
            var box = new BoxCommand(CommandFailBehaviour.Continue, null, _logger, _bugTracker);
            var mockHandler = Substitute.For<Action<BoxCommand>>();
            box.AddCommandCompleteHandler(mockHandler);
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            box.Add(cmd1);
            box.Add(cmd2);
            box.Execute();
            cmd1.CompleteManually();
            mockHandler.Received(1).Invoke(box);
            cmd2.CompleteManually();
        }
    }
}