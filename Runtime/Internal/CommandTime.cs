using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    public static class CommandTime {
        private static ITimeProvider _provider = new UnityTimeProvider();

        public static ITimeProvider Provider => _provider;
        public static float RealtimeSinceStartup => _provider.RealtimeSinceStartup;

        public static void SetProvider(ITimeProvider provider) {
            _provider = provider ?? new UnityTimeProvider();
        }
    }
}
