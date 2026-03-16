using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    internal class OneShotTimer {
        private readonly int _timeLeftMilliseconds;
        private CancellationTokenSource _cts;
        private bool _started;

        public int TimeLeftMilliseconds => _timeLeftMilliseconds;
        public event Action OnTimePassed;

        public OneShotTimer(int milliseconds) {
            _timeLeftMilliseconds = milliseconds;
            _cts = new CancellationTokenSource();
        }

        public void Start() {
            if (_started) {
                return;
            }
            _started = true;
            
            if (_timeLeftMilliseconds <= 0) {
                return;
            }

            var cts = _cts;
            if (cts == null) {
                return;
            }
            FireAfterAsync(cts.Token).Forget(MainThreadDispatcher.ReportUnhandledException);
        }

        private async UniTask FireAfterAsync(CancellationToken token) {
            await UniTask.Delay(_timeLeftMilliseconds, cancellationToken : token).SuppressCancellationThrow();
            if (!token.IsCancellationRequested) {
                OnTimePassed?.Invoke();
            }
        }

        public void StopAndDispose() {
            var cts = _cts;
            if (cts == null) {
                return;
            }
            _cts = null;

            try {
                cts.Cancel();
            } finally {
                cts.Dispose();
                OnTimePassed = null;
            }
        }
    }
}