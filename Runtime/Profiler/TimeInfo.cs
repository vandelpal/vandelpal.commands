using System.Collections.Generic;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands.Profiler {
    public static class TimeInfo {
        private static readonly List<ITimeNode> _nodes = new List<ITimeNode>();

        public static bool InMeasure { get; private set; }

        internal static void AddNode(ITimeNode node) {
            _nodes.Add(node);
        }
        
        public static List<ResultTimeNode> GetResults() {
            var results = new List<ResultTimeNode>();
            foreach (var node in _nodes) {
                var resNode = new ResultTimeNode(node);
                results.Add(resNode);
                if (node.Children != null) {
                    resNode.group = resNode.content;
                    foreach (var nodeChild in node.Children) {
                        var resChild = new ResultTimeNode(nodeChild) {
                            group = resNode.content
                        };
                        results.Add(resChild);
                    }
                }
            }
            return results;
        }

        public static void Reset() {
            _nodes.Clear();
            InMeasure = true;
        }

        public static void Complete() {
            InMeasure = false;
        }
    }
}