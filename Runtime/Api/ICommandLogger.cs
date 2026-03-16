namespace Vandelpal.Commands.Api {
    public interface ICommandLogger {
        void LogInfo(string format, params object[] args);
        void LogWarning(string format, params object[] args);
        void LogError(string format, params object[] args);
        void LogError(System.Exception exception, object payload, string format, params object[] args);
    }
}