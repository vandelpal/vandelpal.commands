using System;
using Vandelpal.Commands.Api;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands {
    public class ActionWrapperCommand : AbstractFakeProgressCommand {
        private readonly Func<UniTask> _func;
        private readonly Action _action;
        private readonly string _methodName;
        private readonly bool _useAction;

        public ActionWrapperCommand(Action action, ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {
            _action = action;
            _methodName = _action.Method?.Name ?? "Action";
            _useAction = true;
        }

        public ActionWrapperCommand(Func<UniTask> func, ICommandLogger logger, ICommandBugTracker bugTracker) : base(logger, bugTracker) {
            _func = func;
            _methodName = TryGetGenericName(_func.Method?.Name ?? "Func");
        }

        private static string TryGetGenericName(string methodName) {
            var begin = methodName.IndexOf('<');
            var end = methodName.LastIndexOf('>');
            if (begin == -1 || end == -1) {
                return methodName;
            }
            return methodName.Substring(begin + 1, end - begin - 1);
        }

        protected override void ExecInternal() {
            if (_useAction) {
                _action.Invoke();
                NotifyComplete();
            } else {
                TryExecInternalAsync();
            }
        }

        protected override async UniTask ExecInternalAsync() {
            await _func.Invoke();
            NotifyComplete();
        }

        protected override bool NeedMeasureTime() => true;
        protected override string GetLogName() => _methodName;
    }
}