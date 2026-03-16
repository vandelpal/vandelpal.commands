using System;
using System.Collections.Generic;
using Vandelpal.Commands.Api;
using NSubstitute;
using NUnit.Framework;

namespace Vandelpal.Commands.Tests {
    internal class AbstractCommandTests : TestsInstaller {
        [Test]
        public void State_ByDefault_IsNotStarted() {
            var command = GetTestCommand();
            Assert.AreEqual(CommandState.NotStarted, command.State);
        }

        [Test]
        public void State_WhenRunning_IsExecuting() {
            InstallDependencies();
            var command = GetTestCommand();
            command.Execute();
            Assert.AreEqual(CommandState.Executing, command.State);
            command.CompleteManually();
        }

        [Test]
        public void State_WhenCommandStop_StateIsStopped() {
            InstallDependencies();
            var command = GetTestCommand();
            command.Execute();
            command.Terminate();
            Assert.AreEqual(CommandState.Terminated, command.State);
        }

        [Test]
        public void Execute_WhenAlreadyExecuting_SkipCall() {
            InstallDependencies();
            var command = GetTestCommand();
            var handler = Substitute.For<Action>();
            command.HandleExecInternal += handler;
            command.Execute();
            command.Execute();
            handler.Received(1).Invoke();
            command.CompleteManually();
        }

        [Test]
        public void Execute_ThrowExceptionInExecInternal_NotifyToBugTracker() {
            InstallDependencies();
            var command = GetTestCommand();
            var exception = new Exception(nameof(Execute_ThrowExceptionInExecInternal_NotifyToBugTracker));
            command.HandleExecInternal += () => throw exception;
            command.Execute();
            _bugTracker.Received(1).CreateBugData(typeof(TestCommand), Arg.Any<string>());
            _bugData.Received(1).SetMessage(Arg.Any<string>());
            _bugData.Received(1).SetException(Arg.Any<Exception>());
            _logger.Received().LogError(Arg.Any<Exception>(), Arg.Any<object>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>());
            command.CompleteManually();
        }

        [Test]
        public void Execute_ThrowExceptionInExecInternal_IsFailedState() {
            InstallDependencies();
            var command = GetTestCommand();
            command.HandleExecInternal += () => throw new Exception("");
            command.Execute();
            Assert.AreEqual(CommandState.Failed, command.State);
        }

        [Test]
        public void Execute_WhenCalled_CallLog() {
            InstallDependencies();
            var command = GetTestCommand();
            command.Execute();
            _logger.Received(1).LogInfo(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>());
            command.CompleteManually();
        }

        [Test]
        public void AddCompleteHandler_WhenAddedHandler_CallHandler() {
            InstallDependencies();
            var command = GetTestCommand();
            var mockHandler = Substitute.For<Action<ICommand>>();
            command.AddCompleteHandler(mockHandler);
            command.Execute();
            command.Terminate();
            mockHandler.Received(1).Invoke(command);
        }

        [Test]
        public void RemoveCompleteHandler_WhenRemoveHandler_NotCallHandler() {
            InstallDependencies();
            var command = GetTestCommand();
            var mockHandler = Substitute.For<Action<ICommand>>();
            command.AddCompleteHandler(mockHandler);
            command.RemoveCompleteHandler(mockHandler);
            command.Execute();
            command.Terminate();
            mockHandler.DidNotReceive();
        }

        [Test]
        public void Reset_WhenCalled_CanExecuteAgain() {
            InstallDependencies();
            var command = GetTestCommand();
            var handler = Substitute.For<Action>();
            command.HandleExecInternal += handler;
            command.Execute();
            command.CompleteManually();
            command.CallRetry();
            handler.Received(2).Invoke();
            command.CompleteManually();
        }

        [Test]
        public void Retry_WhenCalledBeforeComplete_CallExecuteTwice() {
            InstallDependencies();
            var command = GetTestCommand();
            var handler = Substitute.For<Action>();
            command.HandleExecInternal += handler;
            command.Execute();
            command.CallRetry();
            handler.Received(2).Invoke();
            command.CompleteManually();
        }

        [Test]
        public void MarkSuccess_WhenCalled_StateIsCompleted() {
            InstallDependencies();
            var command = GetTestCommand();
            command.Execute();
            command.CompleteManually();
            Assert.AreEqual(CommandState.Completed, command.State);
        }

        [Test]
        public void Terminate_ThrowExceptionInHandler_LogException() {
            InstallDependencies();
            var command = GetTestCommand();
            var ex = new Exception();
            command.AddCompleteHandler(obj => throw ex);
            command.Execute();
            command.Terminate();
            _logger.Received().LogError(Arg.Any<Exception>(), Arg.Any<object>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>());
        }

        [Test]
        public void Execute_WhenCalled_AddCommandToCache() {
            InstallDependencies();
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var cmd3 = GetTestCommand();
            cmd1.Execute();
            cmd2.Execute();
            Assert.AreEqual(2, AbstractCommand.CacheSize);
            cmd1.CompleteManually();
            cmd2.CompleteManually();
        }

        [Test]
        public void Command_WhenFinished_RemoveCommandFromCache() {
            InstallDependencies();
            var cmd1 = GetTestCommand();
            var cmd2 = GetTestCommand();
            var cmd3 = GetTestCommand();
            cmd1.Execute();
            cmd2.Execute();
            cmd3.Execute();
            cmd1.CompleteManually();
            cmd2.Terminate();
            Assert.AreEqual(1, AbstractCommand.CacheSize);
            cmd3.CompleteManually();
        }

        [Test]
        public void ExecuteAsync_WhenAlreadyExecutedWithExecute_ThrowsInvalidOperationException() {
            InstallDependencies();
            var command = GetTestCommand();
            command.Execute();
            command.CompleteManually();
            Assert.Throws<InvalidOperationException>(() => command.ExecuteAsync());
        }

        [Test]
        public void Execute_WhenAlreadyCompleted_SkipCall() {
            InstallDependencies();
            var command = GetTestCommand();
            var handler = Substitute.For<Action>();
            command.HandleExecInternal += handler;
            command.Execute();
            command.CompleteManually();
            command.Execute();
            handler.Received(1).Invoke();
        }

        [Test]
        public void SetTimeout_WhenCalledSeveralTimes_DoNotThrow() {
            InstallDependencies();
            var command = GetTestCommand();
            Assert.DoesNotThrow(() => {
                command.SetTimeout(100);
                command.SetTimeout(50);
                command.SetTimeout(10);
            });
        }

        [Test]
        public void Execute_WhenUsingCustomTimeProvider_UsesProvidedTimeForDuration() {
            InstallDependencies();
            CommandTime.SetProvider(new TestTimeProvider(10f, 11.234f));
            var command = GetTestCommand();

            command.Execute();
            command.CompleteManually();

            Assert.AreEqual(1.234f, command.ExecuteTime, 0.0001f);
            Assert.AreEqual(1234, command.ExecuteTimeInMs);
        }

        private sealed class TestTimeProvider : ITimeProvider {
            private readonly Queue<float> _times;
            private float _last;

            public TestTimeProvider(params float[] times) {
                _times = new Queue<float>(times ?? Array.Empty<float>());
                _last = _times.Count > 0 ? _times.Peek() : 0f;
            }

            public float RealtimeSinceStartup {
                get {
                    if (_times.Count > 0) {
                        _last = _times.Dequeue();
                    }
                    return _last;
                }
            }
        }
    }
}