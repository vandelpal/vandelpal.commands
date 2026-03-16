using System.Collections.Generic;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands.Profiler {
    internal class TimeNodeStub : ITimeNode {
        public static readonly ITimeNode Stub = new TimeNodeStub();
        public string Label => "";
        public float StartTime => 0;
        public float? EndTime => null;
        public IEnumerable<ITimeNode> Children => System.Linq.Enumerable.Empty<ITimeNode>();
        public void CompleteChild(string withLabel, float? start = null) {}
        public void Dispose() {}
    }
}