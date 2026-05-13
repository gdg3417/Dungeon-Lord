using DungeonBuilder.M0;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class KpiServiceTests
    {
        [Test]
        public void Snapshot_ComputesAveragesAndGateCounts()
        {
            IKpiService kpi = new KpiService();
            kpi.RecordManaTick(10);
            kpi.RecordManaTick(20);
            kpi.RecordGateEvaluation(false);
            kpi.RecordGateEvaluation(true);

            KpiSnapshot snap = kpi.Snapshot();

            Assert.AreEqual(15, snap.AverageManaPerTick);
            Assert.AreEqual(1, snap.BlockedGateCount);
            Assert.AreEqual(2, snap.TotalGateEvaluations);
        }
    }
}
