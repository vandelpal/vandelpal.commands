using System;

namespace Vandelpal.Commands.Api {
    /// <summary>Command that reports progress (0–100)</summary>
    public interface IProgressCommand : ICommand {
        int CurrentPercent { get; }
        IProgressCommand AddProgressHandler(Action<IProgressCommand, int> progressHandler);
        IProgressCommand RemoveProgressHandler(Action<IProgressCommand, int> progressHandler);
    }

    public interface IProgressCommand<out T> : IProgressCommand {
        T Result { get; }
    }
}