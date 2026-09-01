using System.IO;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralDeletionServiceTests
    {
        [Test]
        public void ProductionRemovalPolicy_ReturnsReusableContent_AndLeavesLootUnresolved()
        {
            byte[] bytes = File.ReadAllBytes(StructuralContentRemovalPolicyAuthority.ProductionPath);
            Assert.That(StructuralContentRemovalPolicyAuthority.TryParse(bytes, out var policy), Is.True);
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(policy,
                "placement.category.monster", "placement.option.monster.goblin", out var monster, out _), Is.True);
            Assert.That(monster, Is.EqualTo(StructuralContentRemovalPolicy.ReturnToPlayerCustody));
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(policy,
                "placement.category.trap", "placement.option.trap.snare", out var trap, out _), Is.True);
            Assert.That(trap, Is.EqualTo(StructuralContentRemovalPolicy.ReturnToPlayerCustody));
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(policy,
                "placement.category.loot_node", "placement.option.loot_node.basic", out _, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(StructuralContentRemovalPolicyAuthority.MissingOrUnresolvedReason));
        }

        [Test]
        public void Preview_MissingPolicy_FailsClosedWithoutCandidate()
        {
            var request = new StructuralDeletionRequest { TargetRoomInstanceId = "test.room.tail" };
            StructuralEditPreview preview = StructuralDeletionService.Preview(null, request, null,
                null, null, default);
            Assert.That(preview.IsValid, Is.False);
            Assert.That(preview.DetachedCandidate, Is.Null);
            Assert.That(preview.Operation, Is.EqualTo(StructuralEditOperation.Deletion));
        }
    }
}
