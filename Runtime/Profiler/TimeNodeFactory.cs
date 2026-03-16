using Vandelpal.Commands.Api;

namespace Vandelpal.Commands.Profiler {
    public static class TimeNodeFactory {
        public static ITimeNode Create(string label) =>
            TimeInfo.InMeasure ? TimeNode.Create(label) : TimeNodeStub.Stub;

        public static ITimeNode CreateSinceStartApp(string label) =>
            TimeInfo.InMeasure ? TimeNode.CreateSinceStartApp(label) : TimeNodeStub.Stub;
    }
}