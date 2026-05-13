using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class SimulationClockTests
    {
        private sealed class FakeTimeSource : ITimeSource
        {
            public long Now;
            public long UtcNowUnixSeconds() => Now;
        }

        [Test]
        public void TryConsumeTick_ConsumesAtConfiguredInterval()
        {
            var time = new FakeTimeSource();
            var clock = new SimulationClock(10, 300, time);

            Assert.IsFalse(clock.TryConsumeTick(4f));
            Assert.IsFalse(clock.TryConsumeTick(5f));
            Assert.IsTrue(clock.TryConsumeTick(1f));
        }

        [Test]
        public void ResumeAndDetectSkew_UsesConfiguredThreshold()
        {
            var time = new FakeTimeSource { Now = 1000 };
            var clock = new SimulationClock(10, 300, time);

            clock.MarkPaused();
            time.Now = 1405;

            ClockResumeResult result = clock.ResumeAndDetectSkew();

            Assert.AreEqual(1405, result.ResumedAtUtc);
            Assert.AreEqual(405, result.PauseDeltaSeconds);
            Assert.IsTrue(result.SkewDetected);
        }
    }
}
