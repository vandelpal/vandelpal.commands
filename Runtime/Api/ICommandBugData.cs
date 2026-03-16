using System;

namespace Vandelpal.Commands.Api {
    public interface ICommandBugData {
        ICommandBugData SetMessage(string message);
        ICommandBugData SetException(Exception exception);
    }
}