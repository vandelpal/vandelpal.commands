using System.Collections.Generic;
using System.Linq;
using Vandelpal.Commands.Api;

namespace Vandelpal.Commands {
    /// <summary>
    /// Weight (0-100) for a command in a progress queue. Use <see cref="CALC_AUTO"/> to auto-distribute remaining percent.
    /// </summary>
    public class ProgressSettings : IProgressSettings {
        public const int CALC_AUTO = -1;
        public static readonly ProgressSettings AUTO = new ProgressSettings();
        public static readonly ProgressSettings ZERO = new ProgressSettings(0);

        public int Percents { get; set; }

        public ProgressSettings(int percents = CALC_AUTO) => Percents = percents;

        public static void DistributeAutoPercents(IEnumerable<IProgressSettings> settings, int maxPercent = 100) {
            var list = settings.ToList();
            var busy = list.Where(s => s.Percents != CALC_AUTO).Sum(s => s.Percents);
            var freeList = list.Where(s => s.Percents == CALC_AUTO).ToList();
            var free = maxPercent - busy;
            if (free <= 0 || freeList.Count == 0) {
                return;
            }
            var once = free / freeList.Count;
            foreach (var s in freeList) {
                s.Percents = once;
            }
            var rest = free - once * freeList.Count;
            if (rest > 0) {
                freeList[^1].Percents += rest;
            }
        }
    }
}