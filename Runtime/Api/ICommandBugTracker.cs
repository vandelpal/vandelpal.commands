using System;

namespace Vandelpal.Commands.Api {
    public interface ICommandBugTracker {
        ICommandBugData CreateBugData(Type commandType, string message = null);
        void ReportBug(ICommandBugData bugData);
    }
}