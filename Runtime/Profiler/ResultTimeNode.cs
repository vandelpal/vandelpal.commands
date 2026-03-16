using Vandelpal.Commands.Api;
using UnityEngine;

namespace Vandelpal.Commands.Profiler {
    public class ResultTimeNode {
        public string label;
        
        public int start;
        public int end;
        public string content;
        public string group;

        public ResultTimeNode(ITimeNode node) {
            label = node.Label;
            start = Mathf.RoundToInt(node.StartTime * 1000);
            end = Mathf.RoundToInt((node.EndTime ?? int.MaxValue) * 1000);
            content = node.Label + $"[{end - start}ms]";
        }
    }
}