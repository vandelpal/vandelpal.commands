using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands.Sample {
    /// <summary>
    /// Advanced examples: multiple queues combined in one Box, progress weights and fake progress,
    /// working with command instance via async (ExecuteAsync, result, WhenAll),
    /// and wrappers/manual queue usage.
    /// </summary>
    public static class AdvancedUsageExample {
        /// <summary>
        /// 1) Several queues are combined into one "main" progress via BoxCommand (like RootEntry).
        ///    Each sub-queue has a weight (percent of total). All queues run in parallel.
        /// </summary>
        public static void RunCombinedQueuesExample(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var factory = new CommandsFactory(logger, bugTracker);

            // Sub-queue "Device" — 20% of total
            var deviceQueue = factory.GetProgressQueueCommand("DeviceQueue");
            deviceQueue.AddProgress(new WaitSecondsCommand(0.3f, logger, bugTracker), new ProgressSettings(50));
            deviceQueue.AddProgress(new WaitSecondsCommand(0.2f, logger, bugTracker), new ProgressSettings(50));

            // Sub-queue "Network" — 50% of total; one step has no visible progress (ZERO)
            var networkQueue = factory.GetProgressQueueCommand("NetworkQueue");
            networkQueue.AddProgress(new ActionWrapperCommand(() => { }, logger, bugTracker), ProgressSettings.ZERO); // invisible step
            networkQueue.AddProgress(new WaitSecondsCommand(0.4f, logger, bugTracker), new ProgressSettings(100));

            // Sub-queue "Data" — 30% of total with AUTO progress destribute
            var dataQueue = factory.GetProgressQueueCommand("DataQueue");
            dataQueue.AddProgress(new WaitSecondsCommand(0.2f, logger, bugTracker));
            dataQueue.AddProgress(new WaitSecondsCommand(0.2f, logger, bugTracker));

            // Main progress: Box runs all three queues in parallel and aggregates progress by weights
            var mainProgress = new BoxCommand(CommandFailBehaviour.Continue, "MainLoading", logger, bugTracker);
            mainProgress.Add(deviceQueue, new ProgressSettings(20));   // 20%
            mainProgress.Add(networkQueue, new ProgressSettings(50));  // 50%
            mainProgress.Add(dataQueue, new ProgressSettings(30));     // 30%

            mainProgress.AddProgressHandler((cmd, percent) =>
                Debug.Log($"[Combined] Total progress: {percent}%"));

            mainProgress.AddCompleteHandler(cmd =>
                Debug.Log($"[Combined] All queues finished. Success={mainProgress.IsSucceed}, Final={mainProgress.CurrentPercent}%"));

            mainProgress.Execute();
        }

        /// <summary>
        /// 2) Progress and fake progress: ProgressSettings(percent), FakeProgressSettings(percent),
        ///    ProgressSettings.ZERO for steps that don't move the bar.
        /// </summary>
        public static void RunProgressAndFakeExample(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var factory = new CommandsFactory(logger, bugTracker);
            var queue = factory.GetProgressQueueCommand("ProgressDemo");

            queue.AddProgressHandler((cmd, percent) => Debug.Log($"[Progress] {percent}%"));

            // Real progress — command reports progress (e.g. WaitSecondsCommand doesn't, but we give it 25% weight)
            queue.AddProgress(new WaitSecondsCommand(0.2f, logger, bugTracker), new ProgressSettings(25));

            // Fake progress — long operation that doesn't report progress; bar moves by timer (30% weight, 200ms step)
            queue.AddProgress(
                new ActionWrapperCommand(async () => await UniTask.Delay(TimeSpan.FromSeconds(1)), logger, bugTracker),
                new FakeProgressSettings(30, fakeTime: 200, fakeStep: 2));

            // Invisible step — doesn't affect progress bar
            queue.AddProgress(new ActionWrapperCommand(() => Debug.Log("Invisible step"), logger, bugTracker), ProgressSettings.ZERO);

            // Remaining 45% — auto-distributed if you use CALC_AUTO, or explicit:
            queue.AddProgress(new WaitSecondsCommand(0.15f, logger, bugTracker), new ProgressSettings(45));

            queue.AddCompleteHandler(_ => Debug.Log($"[Progress] Done: {queue.CurrentPercent}%"));
            queue.Execute();
        }

        /// <summary>
        /// 3) Working with command instance via async: ExecuteAsync(), result (State, IsSucceed, Error, ExecuteTime), WhenAll.
        /// </summary>
        public static async UniTaskVoid RunAsyncExample(ICommandLogger logger, ICommandBugTracker bugTracker) {
            // Single command: await and use result
            var singleCmd = new WaitSecondsCommand(0.5f, logger, bugTracker);
            var result = await singleCmd.ExecuteAsync();

            Debug.Log($"[Async] Single command: State={result.State}, IsSucceed={result.IsSucceed}, Time={result.ExecuteTimeInMs} ms");
            if (result.HasError) {
                Debug.LogWarning($"[Async] Error: {result.Error}");
            }

            // Parallel async: run several commands and wait for all
            var cmd1 = new WaitSecondsCommand(0.2f, logger, bugTracker);
            var cmd2 = new WaitSecondsCommand(0.3f, logger, bugTracker);
            var cmd3 = new ActionWrapperCommand(() => Debug.Log("Parallel step"), logger, bugTracker);
            await UniTask.WhenAll(cmd1.ExecuteAsync(), cmd2.ExecuteAsync(), cmd3.ExecuteAsync());
        }

        /// <summary>
        /// 4) Wrappers + manual queue:
        ///    - PredicateWrapperCommand for validation checks;
        ///    - NotWaitWrapperCommand to start a long command without blocking queue flow;
        ///    - GetManualQueueCommand + ContinueExecute for explicit step-by-step control.
        /// </summary>
        public static void RunWrappersAndManualQueueExample(ICommandLogger logger, ICommandBugTracker bugTracker) {
            var factory = new CommandsFactory(logger, bugTracker);
            var manualQueue = factory.GetManualQueueCommand("ManualDemo");
            
            manualQueue.Add(new PredicateWrapperCommand(predicate: () => true, logger, bugTracker));
            var longBackgroundStep = new WaitSecondsCommand(1.5f, logger, bugTracker).SetTimeout(2500);
            manualQueue.Add(new NotWaitWrapperCommand((IProgressCommand)longBackgroundStep, logger, bugTracker));
            manualQueue.Add(new ActionWrapperCommand(() => Debug.Log("[Manual] Final foreground step"), logger, bugTracker));

            manualQueue.AddCommandCompleteHandler(OnCommandInQueueComplete);
            manualQueue.AddCompleteHandler(_ => Debug.Log("[Manual] Queue completed"));
            manualQueue.Execute();
            return;

            void OnCommandInQueueComplete(IQueueCommand obj) {
                var completedCmd = obj.CompletedCommand;
                if (obj.HasError) {
                    // Show error window and than retry
                    manualQueue.RetryCompletedCommand();
                } else {
                    // Manual mode requires explicit continuation after every completed step.
                    manualQueue.ContinueExecute();
                }
            }
        }
    }
}
