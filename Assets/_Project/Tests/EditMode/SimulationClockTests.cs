#if UNITY_EDITOR
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

        [Test]
        public void TimeService_PauseStopsConfiguredTicksAndResumeKeepsSingleSubscription()
        {
            var source = new FakeTimeSource { Now = 1000 };
            var save = new SaveData();
            var service = new TimeService(new SimpleLogger(false), 10, 300, source);
            int observed = 0;
            service.AttachSave(save);
            service.OnTick += _ => observed++;

            service.Update(10f);
            service.OnPause();
            service.Update(30f);
            source.Now = 1010;
            service.OnResume();
            service.Update(10f);

            Assert.That(observed, Is.EqualTo(2));
            Assert.That(save.totalTicks, Is.EqualTo(2));
            Assert.That(service.IsPausedForTests, Is.False);
        }

        [Test]
        public void TimeService_LongResumeReturnsStableLocalizationKeyNotEnglishClaim()
        {
            var source = new FakeTimeSource { Now = 1000 };
            var service = new TimeService(new SimpleLogger(false), 10, 300, source);
            service.AttachSave(new SaveData());
            service.OnPause();
            source.Now = 91000;

            string result = service.OnResume();

            Assert.That(result, Is.EqualTo(TimeService.ClockAnomalyMessageKey));
            Assert.That(result, Does.Not.Contain("limited"));
            Assert.That(result, Does.Not.Contain("Time change detected"));
        }
    }
}
#endif
