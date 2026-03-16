using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public class CommandsFactory : ICommandsFactory {
        private readonly ICommandLogger _logger;
        private readonly ICommandBugTracker _bugTracker;

        public CommandsFactory(ICommandLogger logger, ICommandBugTracker bugTracker) {
            _logger = logger;
            _bugTracker = bugTracker;
        }

        public IQueueCommand GetQueueCommand(string name, CommandFailBehaviour behaviour = CommandFailBehaviour.Continue) =>
            new QueueCommand(behaviour, name, _logger, _bugTracker);

        public IQueueCommand GetManualQueueCommand(string name, CommandFailBehaviour behaviour = CommandFailBehaviour.Continue) {
            var queue = new QueueCommand(behaviour, name, _logger, _bugTracker);
            queue.SetExecuteMode(QueueExecuteMode.Manual);
            return queue;
        }

        public IQueueCommand GetQueueCommand(params ICommand[] commands) {
            var queue = GetQueueCommand(null, CommandFailBehaviour.Continue);
            if (commands != null && commands.Length > 0) {
                foreach (var c in commands) {
                    queue.Add(c);
                }
            }
            return queue;
        }

        public IProgressQueueCommand GetProgressQueueCommand(string name, CommandFailBehaviour behaviour = CommandFailBehaviour.Continue) =>
            new ProgressQueueCommand(behaviour, name, _logger, _bugTracker);

        public IProgressQueueCommand GetProgressQueueCommand(string name, params ICommand[] commands) {
            var queue = GetProgressQueueCommand(name, CommandFailBehaviour.Continue);
            if (commands != null && commands.Length > 0) {
                foreach (var c in commands) {
                    queue.Add(c);
                }
            }
            return queue;
        }
    }
}