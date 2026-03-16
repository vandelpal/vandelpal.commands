using System;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    internal static class MainThreadDispatcher {
        private static Action<Exception> _unhandledExceptionHandler = DefaultUnhandledExceptionHandler;
        private static ICommandLogger _logger;

        public static void SetLogger(ICommandLogger logger) => _logger = logger;

        public static void SetUnhandledExceptionHandler(Action<Exception> handler) =>
            _unhandledExceptionHandler = handler ?? DefaultUnhandledExceptionHandler;

        private static void DefaultUnhandledExceptionHandler(Exception exception) {
            _logger?.LogError(exception, null, "MainThreadDispatcher unhandled exception: {0}", exception?.Message);
        }

        public static void Post(Action action) {
            if (action == null) {
                return;
            }
            RunOnMainThreadAsync(action).Forget(ReportUnhandledException);
        }

        public static void CallAtEndOfFrame(Action action) {
            if (action == null) {
                return;
            }
            RunAtEndOfFrameAsync(action).Forget(ReportUnhandledException);
        }
        
        internal static void ReportUnhandledException(Exception exception) {
            try {
                _unhandledExceptionHandler?.Invoke(exception);
            } catch (Exception e) {
                _logger?.LogError(e, null, "MainThreadDispatcher exception in unhandled handler: {0}", e?.Message);
            }
        }

        private static async UniTask RunOnMainThreadAsync(Action action) {
            await UniTask.SwitchToMainThread();
            action();
        }

        private static async UniTask RunAtEndOfFrameAsync(Action action) {
            await UniTask.SwitchToMainThread();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            action();
        }
    }
}