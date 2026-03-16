namespace Vandelpal.Commands.Api {
    public interface ITimeProvider {
        float RealtimeSinceStartup { get; }
    }
}
