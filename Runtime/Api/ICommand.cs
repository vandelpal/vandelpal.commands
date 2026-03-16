using System;
using Cysharp.Threading.Tasks;

namespace Vandelpal.Commands.Api {
    /// <summary>
    /// Single runnable operation.
    /// Use <see cref="Execute"/> or <see cref="ExecuteAsync"/>; subscribe via AddCompleteHandler / AddSucceedHandler.
    /// </summary>
    public interface ICommand {
        CommandState State { get; }
        bool IsExecuting { get; }
        bool IsSucceed { get; }
        bool HasResult { get; }
        bool HasError { get; }
        CommandError Error { get; }
        float ExecuteTime { get; }
        int ExecuteTimeInMs { get; }

        ICommand SetTimeout(int milliseconds);

        ICommand AddCompleteHandler(Action<ICommand> completeHandler);
        void RemoveCompleteHandler(Action<ICommand> completeHandler);
        ICommand AddSucceedHandler(Action<ICommand> succeedHandler);
        void RemoveSucceedHandler(Action<ICommand> succeedHandler);

        void Execute();
        UniTask<ICommand> ExecuteAsync();
        void Terminate();
        void Reset();
        void Retry();
    }
}