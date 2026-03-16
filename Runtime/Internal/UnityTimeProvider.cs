using Vandelpal.Commands.Api;
using UnityEngine;

namespace Vandelpal.Commands {
    internal sealed class UnityTimeProvider : ITimeProvider {
        public float RealtimeSinceStartup => Time.realtimeSinceStartup;
    }
}
