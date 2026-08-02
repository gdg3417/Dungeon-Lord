#if UNITY_EDITOR
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedCompleteSaveContractTests
    {
        [Test]
        public void ContractJson_ParsesPreservedBooleanValues()
        {
            var limits = new SpatialSerializedInputLimits(1024, 32, 8, 128, 8);
            var issues = new SpatialIssueCollector(8);

            Assert.That(ContractJson.TryParse(Encoding.UTF8.GetBytes("{\"preserved\":true}"),
                limits, issues, out ContractJsonNode node), Is.True);
            Assert.That(node.Fields[0].Value.Kind, Is.EqualTo(ContractJsonKind.Boolean));
        }

        [Test]
        public void CandidateInvalidReason_UsesTransactionRegistry()
        {
            Assert.That(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason,
                Is.EqualTo("gd66.transaction.candidate_invalid"));
        }
    }
}
#endif
