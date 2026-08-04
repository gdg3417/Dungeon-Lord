#if UNITY_EDITOR
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedMigrationSemanticTests
    {
        public static IEnumerable<TestCaseData> PopulatedCases
        {
            get
            {
                yield return Case("AssignmentR1", Assign(Room(0)), 1);
                yield return Case("AssignmentR2", Assign(Room(0) + "," + Room(1)), 2);
                yield return Case("FloorR1", Floor(Node(0, "placement.category.room", "placement.option.room.basic")), 1);
                yield return Case("FloorR2", Floor(Node(0, "placement.category.room", "placement.option.room.basic")) +
                    "," + Placement(RoomPlacement(1)), 1);
                yield return Case("PlacementExplicitR1", Placement(RoomPlacement(1)), 1);
                yield return Case("PlacementImplicitR1", Placement(Entry("placement.category.monster",
                    "placement.option.monster.skeleton", 1)), 1, "placement.option.monster.skeleton");
                yield return Case("MonsterOnly", Placement(RoomPlacement(1) + "," + Entry(
                    "placement.category.monster", "placement.option.monster.skeleton", 2)), 1,
                    "placement.option.monster.skeleton");
                yield return Case("TrapOnly", Placement(RoomPlacement(1) + "," + Entry(
                    "placement.category.trap", "placement.option.trap.spike", 2)), 1,
                    "placement.option.trap.spike");
                yield return Case("LootOnly", Placement(RoomPlacement(1) + "," + Entry(
                    "placement.category.loot_node", "placement.option.loot_node.basic", 2)), 1,
                    "placement.option.loot_node.basic");
                yield return Case("MonsterTrapLoot", Placement(RoomPlacement(1) + "," + Entry(
                    "placement.category.monster", "placement.option.monster.skeleton", 2) + "," + Entry(
                    "placement.category.trap", "placement.option.trap.spike", 3) + "," + Entry(
                    "placement.category.loot_node", "placement.option.loot_node.basic", 4)), 1,
                    "placement.option.monster.skeleton", "placement.option.trap.spike",
                    "placement.option.loot_node.basic");
                yield return Case("LowerExactAgreement", Assign(Room(0, "placement.option.monster.skeleton")) +
                    "," + Placement(RoomPlacement(1) + "," + Entry("placement.category.monster",
                    "placement.option.monster.skeleton", 2)), 1, "placement.option.monster.skeleton");
                yield return Case("LowerIneffectiveConflict", Assign(Room(0, "placement.option.monster.skeleton")) +
                    "," + Placement(RoomPlacement(1) + "," + Entry("placement.category.monster",
                    "placement.option.monster.goblin", 2)), 1, "placement.option.monster.skeleton");
                yield return Case("LowerEffectiveContribution", Floor(Node(0, "placement.category.room",
                    "placement.option.room.basic")) + "," + Placement(Entry("placement.category.monster",
                    "placement.option.monster.skeleton", 1)), 1, "placement.option.monster.skeleton");
            }
        }

        [TestCaseSource(nameof(PopulatedCases))]
        public void PopulatedLegacyFixture_PreparesExecutesRecoversDeterministically(string identity,
            string members, int rooms, string[] options)
        {
            byte[] candidate = Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                identity, members, rooms, options);
            Assert.That(candidate, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LowerEffectiveMismatch_ReturnsExactReasonAndNoAttempt()
        {
            string members = Floor(Node(0, "placement.category.room", "placement.option.room.basic") + "," +
                Node(1, "placement.category.monster", "placement.option.monster.skeleton")) + "," +
                Placement(Entry("placement.category.monster", "placement.option.monster.goblin", 1));
            DetachedSpatialMigrationPreparationResult result =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareSemanticResult(members);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Attempt, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
        }

        private static TestCaseData Case(string id, string members, int rooms, params string[] options) =>
            new TestCaseData(id, members, rooms, options).SetName("Semantic_" + id);
        private static string Assign(string rooms) => "\"mvpRoomSlotAssignments\":{\"Rooms\":[" + rooms +
            "],\"NextRevision\":3}";
        private static string Room(int index, string monster = null) => "{\"FloorIndex\":0,\"RoomIndex\":" + index +
            ",\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":" +
            (monster == null ? "[]" : "[\"" + monster + "\"]") +
            ",\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}";
        private static string Floor(string nodes) => "\"mvpDungeonFloorLayout\":{\"Nodes\":[" + nodes +
            "],\"NextRevision\":4}";
        private static string Node(int index, string category, string option) => "{\"FloorIndex\":0,\"NodeIndex\":" +
            index + ",\"SlotId\":\"slot." + index + "\",\"CategoryId\":\"" + category +
            "\",\"OptionId\":\"" + option + "\",\"Revision\":1}";
        private static string Placement(string entries) => "\"mvpDungeonPlacements\":{\"Entries\":[" + entries +
            "],\"NextRevision\":5}";
        private static string RoomPlacement(int revision) => Entry("placement.category.room",
            "placement.option.room.basic", revision);
        private static string Entry(string category, string option, int revision) => "{\"CategoryId\":\"" +
            category + "\",\"OptionId\":\"" + option + "\",\"Revision\":" + revision + "}";
    }
}
#endif
