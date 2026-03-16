using System;
using UnityEngine;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands.Sample {
    /// <summary>
    /// Basic example: QueueCommand, BoxCommand, ProgressQueueCommand, CommandsFactory.
    /// For combined queues (Box of queues), progress/fake progress, async (ExecuteAsync), and manual mode
    /// see <see cref="AdvancedUsageExample"/>.
    /// </summary>
    public static class BasicUsageExample {

        public static void SimpleExample1(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var cmd = new TestCommand(new object(), logger, bugTracker);
            cmd.AddCompleteHandler(c=> Debug.Log("Command completed"));
            cmd.Execute();
            
            var cmd2 = new TestCommand(new object(), logger, bugTracker)
                .AddSucceedHandler(c=> Debug.Log("Succeed"));
            cmd2.Execute();
        }
        
        public static async UniTask SimpleExample2(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var cmd = new TestCommand(new object(), logger, bugTracker);
            await cmd.ExecuteAsync();
            if (cmd.IsSucceed) {
                Debug.Log("Command succeed");
            }
        }
        
        public static void QueuesExample(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var factory = new CommandsFactory(logger, bugTracker);

            var queue = factory.GetQueueCommand("ExampleQueue", CommandFailBehaviour.Continue);
            queue.AddCompleteHandler(cmd => Debug.Log("Queue finished: " + cmd.IsSucceed));
            queue.Add(new TestCommand(new object(), logger, bugTracker));
            queue.Add(new ActionWrapperCommand(() => Debug.Log("Step 1"), logger, bugTracker));
            queue.Add(new WaitSecondsCommand(0.5f, logger, bugTracker));
            queue.Add(new ActionWrapperCommand(() => Debug.Log("Step 2"), logger, bugTracker));
            queue.Execute();

            var box = new BoxCommand(CommandFailBehaviour.Terminate, "Box", logger, bugTracker);
            box.AddCompleteHandler(cmd => Debug.Log("Box finished: " + cmd.IsSucceed));
            box.Add(new ActionWrapperCommand(() => Debug.Log("A"), logger, bugTracker));
            box.Add(new ActionWrapperCommand(() => Debug.Log("B"), logger, bugTracker));
            box.Execute();

            var progressQueue = factory.GetProgressQueueCommand("Progress", CommandFailBehaviour.Terminate);
            progressQueue.AddProgressHandler((cmd, pct) => Debug.Log("Progress: " + pct + "%"));
            progressQueue.AddCompleteHandler(cmd => {
                var progressCmd = cmd as ProgressQueueCommand;
                Debug.Log("Progress queue done: " + progressCmd!.CurrentPercent + "%");
            });
            progressQueue.AddProgress(new WaitSecondsCommand(0.2f, logger, bugTracker), new ProgressSettings(50));
            
            progressQueue.Add(new WaitSecondsCommand(0.25f, logger, bugTracker));
            progressQueue.Add(new ActionWrapperCommand(async () => {
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));
                Debug.Log("[ActionAndWait] async action");
            }, logger, bugTracker));

            progressQueue.Execute();
        }
    }

    public class TestCommand : AbstractCommand {
        private readonly object _context;
        public TestCommand(object context, ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {
            _context = context;
        }
        protected override void ExecInternal() {
            // Some business logic with context
            NotifyComplete();
        }
    }

    public class UnityCommandLogger : ICommandLogger {
        public void LogInfo(string format, params object[] args) => Debug.LogFormat(format, args);
        public void LogWarning(string format, params object[] args) => Debug.LogWarningFormat(format, args);
        public void LogError(string format, params object[] args) => Debug.LogErrorFormat(format, args);
        public void LogError(Exception ex, object payload, string format, params object[] args) =>
            Debug.LogError(string.Format(format, args) + " " + (ex?.Message ?? ""));
    }

    public class UnityCommandBugTracker : ICommandBugTracker {
        public ICommandBugData CreateBugData(Type commandType, string message = null) => new UnityBugData(message);
        public void ReportBug(ICommandBugData bugData) {
            Debug.LogError("[BugTracker] " + (bugData?.ToString() ?? "null bug data"));
        }
    }

    internal class UnityBugData : ICommandBugData {
        private string _msg;
        public UnityBugData(string message) { _msg = message ?? ""; }
        public ICommandBugData SetMessage(string message) { _msg = message; return this; }
        public ICommandBugData SetException(Exception ex) { _msg += " " + (ex?.Message ?? ""); return this; }
        public override string ToString() => _msg;
    }
}
