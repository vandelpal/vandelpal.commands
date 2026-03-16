using System;
using System.Collections.Generic;

namespace Vandelpal.Commands.Api {
    /// <summary>Sequential queue of commands with optional progress and fail behaviour.</summary>
    public interface IQueueCommand : IProgressCommand {
        ICommand CompletedCommand { get; }
        ICommand RunningCommand { get; }
        int QueueCount { get; }
        bool IsContains(ICommand cmd);
        void Add(ICommand c);
        void Add(IEnumerable<ICommand> list);
        void Add(params ICommand[] list);
        IQueueCommand AddCommandCompleteHandler(Action<IQueueCommand> completeHandler);
        void RetryCompletedCommand();
        void ContinueExecute();
        void SetExecuteMode(QueueExecuteMode mode);
    }
}