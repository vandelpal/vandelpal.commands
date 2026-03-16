namespace Vandelpal.Commands.Api {
    public enum CommandState {
        NotStarted,
        Executing,
        Completed,
        Failed,
        Terminated,
        Timeout
    }

    public enum CommandFailBehaviour {
        Terminate,
        Continue
    }

    public enum QueueExecuteMode {
        Auto,
        Manual
    }
}