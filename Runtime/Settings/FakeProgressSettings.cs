namespace Vandelpal.Commands {
    public class FakeProgressSettings : ProgressSettings {
        public const int FAKE_STEP_DEFAULT = 1;
        public const int FAKE_TIME_MS = 400;
        public int FakeStep { get; }
        public int FakeTime { get; }
        public FakeProgressSettings(int percents = CALC_AUTO, int fakeTime = FAKE_TIME_MS, int fakeStep = FAKE_STEP_DEFAULT) : base(percents) {
            FakeStep = fakeStep;
            FakeTime = fakeTime;
        }
    }
}