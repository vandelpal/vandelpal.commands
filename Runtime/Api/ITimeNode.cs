using System;
using System.Collections.Generic;

namespace Vandelpal.Commands.Api {
    public interface ITimeNode : IDisposable {
        string Label { get; }
        float StartTime { get; }
        float? EndTime { get; }
        IEnumerable<ITimeNode> Children { get; }
        void CompleteChild(string withLabel, float? start = null);
    }
}