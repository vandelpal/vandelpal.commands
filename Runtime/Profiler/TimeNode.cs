using System;
using System.Collections.Generic;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands.Profiler {
    internal class TimeNode : ITimeNode {
        private const float IGNORE_TOLERANCE = 0.02f;
        public string Label { get; private set; }
        public float StartTime { get; private set; }
        public float? EndTime { get; private set; }
        private List<TimeNode> _children;
        public IEnumerable<ITimeNode> Children => _children;
        private float? _lastChildTime;

        private TimeNode() {}

        private TimeNode(string label) {
            Label = label;
        }

        internal static ITimeNode Create(string label) {
            return new TimeNode(label) { StartTime = CommandTime.RealtimeSinceStartup };
        }

        internal static ITimeNode CreateSinceStartApp(string label) {
            return new TimeNode(label) { StartTime = 0 };
        }

        public void CompleteChild(string withLabel, float? start = null) {
            var endTime = CommandTime.RealtimeSinceStartup;
            var startTime = start ?? _lastChildTime ?? StartTime;
            if (endTime - startTime <= IGNORE_TOLERANCE) {
                return;
            }
            var child = new TimeNode {
                Label = withLabel,
                StartTime = startTime,
                EndTime = endTime
            };

            _children ??= new List<TimeNode>();
            _children.Add(child);

            if (!start.HasValue) {
                _lastChildTime = endTime;
            }
        }

        private void Complete() {
            if (EndTime.HasValue) {
                throw new InvalidOperationException($"Node {Label} already completed.");
            }
            EndTime = CommandTime.RealtimeSinceStartup;
            TimeInfo.AddNode(this);
        }

        public void Dispose() => Complete();
    }
}