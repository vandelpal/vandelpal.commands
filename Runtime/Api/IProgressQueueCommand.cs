namespace Vandelpal.Commands.Api {
    /// <summary>Queue that accepts only <see cref="IProgressCommand"/> and allows per-command progress weight (and fake progress).</summary>
    public interface IProgressQueueCommand : IQueueCommand {
        void AddProgress(IProgressCommand cmd, IProgressSettings settings = null);
        void CleanUp();
    }
}