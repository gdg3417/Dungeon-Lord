using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class TelemetryServiceTests
    {
        [Test]
        public void Track_BuffersEvents()
        {
            var telemetry = new TelemetryService(new SimpleLogger(includeTimestamps: false));
            telemetry.Track("tick_processed", "{\"tick\":1}");
            telemetry.Track("verification_pending", "{\"pending\":true}");

            var items = telemetry.GetBufferedEvents();

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("tick_processed", items[0].EventName);
            Assert.AreEqual("verification_pending", items[1].EventName);
        }
    }
}
