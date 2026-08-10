#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedMigrationSemanticTests
    {
        public enum Winner { Placements, Floor, Assignments }
        public sealed class Expected
        {
            internal string Id; internal int Schema; internal Winner Winner; internal string Members;
            internal int Rooms; internal string[] Diagnostics; internal string[] Categories;
            internal string[] Options; internal LegacyRoomOriginKind Semantics;
        }

        public static IEnumerable<TestCaseData> PopulatedCases
        {
            get
            {
                yield return Data("PlacementExplicitR1", 3, Winner.Placements, Placement(RoomPlacement(1)), 1);
                yield return Data("PlacementImplicitR1", 3, Winner.Placements, Placement(Entry(Monster, Skeleton, 1)), 1,
                    new[] { MissingRoom, ImplicitRoom }, new[] { Monster }, new[] { Skeleton },
                    LegacyRoomOriginKind.ImplicitCompatibilityContainer);
                yield return Data("MonsterOnly", 3, Winner.Placements, Placement(RoomPlacement(1) + "," + Entry(Monster, Skeleton, 2)), 1,
                    null, new[] { Monster }, new[] { Skeleton });
                yield return Data("TrapOnly", 3, Winner.Placements, Placement(RoomPlacement(1) + "," + Entry(Trap, Spike, 2)), 1,
                    null, new[] { Trap }, new[] { Spike });
                yield return Data("LootOnly", 3, Winner.Placements, Placement(RoomPlacement(1) + "," + Entry(Loot, BasicLoot, 2)), 1,
                    null, new[] { Loot }, new[] { BasicLoot });
                yield return Data("MonsterTrapLoot", 3, Winner.Placements, Placement(RoomPlacement(1) + "," + Entry(Monster, Skeleton, 2) +
                    "," + Entry(Trap, Spike, 3) + "," + Entry(Loot, BasicLoot, 4)), 1, null,
                    new[] { Monster, Trap, Loot }, new[] { Skeleton, Spike, BasicLoot });
                yield return Data("FloorR1", 4, Winner.Floor, Floor(Node(0, RoomCategory, BasicRoom)), 1);
                yield return Data("FloorLowerBasicRoomAgreement", 4, Winner.Floor,
                    Floor(Node(0, RoomCategory, BasicRoom)) + "," + Placement(RoomPlacement(1)), 1,
                    new[] { Agreement });
                yield return Data("FloorPlacementAgreement", 4, Winner.Floor, Floor(Node(0, RoomCategory, BasicRoom) + "," +
                    Node(1, Monster, Skeleton)) + "," + Placement(RoomPlacement(1) + "," +
                    Entry(Monster, Skeleton, 2)), 1, new[] { Agreement }, new[] { Monster }, new[] { Skeleton });
                yield return Data("LowerEffectiveContribution", 4, Winner.Floor, Floor(Node(0, RoomCategory, BasicRoom)) + "," +
                    Placement(Entry(Monster, Skeleton, 1)), 1, new[] { Contribution }, new[] { Monster }, new[] { Skeleton });
                yield return Data("FloorBlankLowerContentImplicit", 4, Winner.Floor, Floor(Node(1, "", "")) + "," +
                    Placement(Entry(Monster, Skeleton, 1)), 1,
                    new[] { Contribution, MissingRoom, ImplicitRoom }, new[] { Monster }, new[] { Skeleton },
                    LegacyRoomOriginKind.ImplicitCompatibilityContainer);
                yield return Data("LowerIneffectiveConflict", 5, Winner.Assignments, Assign(Room(0, Skeleton)) + "," +
                    Placement(RoomPlacement(1) + "," + Entry(Monster, Goblin, 2)), 1,
                    new[] { Ineffective }, new[] { Monster }, new[] { Skeleton });
                yield return Data("AssignmentCombinedLowerConflict", 5, Winner.Assignments, Assign(Room(0, Skeleton)) +
                    "," + Floor(Node(0, RoomCategory, BasicRoom) + "," + Node(1, Monster, Goblin)) +
                    "," + Placement(RoomPlacement(1) + "," + Entry(Monster, Goblin, 2)), 1,
                    new[] { Ineffective }, new[] { Monster }, new[] { Skeleton });
                yield return Data("AssignmentR1", 5, Winner.Assignments, Assign(Room(0)), 1);
                yield return Data("AssignmentR2", 5, Winner.Assignments, Assign(Room(0) + "," + Room(1)), 2);
                yield return Data("LowerExactAgreement", 5, Winner.Assignments, Assign(Room(0, Skeleton)) + "," +
                    Placement(RoomPlacement(1) + "," + Entry(Monster, Skeleton, 2)), 1,
                    new[] { Agreement }, new[] { Monster }, new[] { Skeleton });
                yield return Data("Schema6AssignmentR1", 6, Winner.Assignments, Assign(Room(0)), 1);
                yield return Data("Schema6AssignmentR2", 6, Winner.Assignments, Assign(Room(0) + "," + Room(1)), 2);
            }
        }

        [TestCaseSource(nameof(PopulatedCases))]
        public void PopulatedLegacyFixture_InspectsCanonicalSpatialState(Expected expected)
        {
            var run = Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                expected.Id, expected.Schema, expected.Members);
            Assert.That(run.Classification.SchemaVersion, Is.EqualTo(expected.Schema));
            Assert.That(run.Classification.RoomSlotAssignmentsPresence == RawLegacyRoutePresence.Present,
                Is.EqualTo(ContainsMember(expected.Members, "mvpRoomSlotAssignments")));
            Assert.That(run.Classification.FloorLayoutPresence == RawLegacyRoutePresence.Present,
                Is.EqualTo(ContainsMember(expected.Members, "mvpDungeonFloorLayout")));
            Assert.That(run.Classification.DungeonPlacementsPresence == RawLegacyRoutePresence.Present,
                Is.EqualTo(ContainsMember(expected.Members, "mvpDungeonPlacements")));
            AssertWinner(run.Classification, expected.Winner);
            Assert.That(run.Attempt.Descriptor.RawSourceSchemaVersion, Is.EqualTo(expected.Schema));
            Assert.That(run.Attempt.Diagnostics, Is.EqualTo(expected.Diagnostics));
            Assert.That(run.Attempt.Diagnostics.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(run.Attempt.Diagnostics.Length));
            Assert.That(run.State.Floors, Has.Length.EqualTo(1));
            SavedSpatialFloor floor = run.State.Floors[0];
            RoomSpatialInstance[] rooms = floor.Layout.Rooms;
            Assert.That(rooms, Has.Length.EqualTo(expected.Rooms));
            Assert.That(rooms.Select(value => value.RoomInstanceId), Is.EqualTo(Enumerable.Range(0,
                expected.Rooms).Select(value => "compat.floor.00.legacy-room." + value.ToString("D2"))));
            Assert.That(rooms.Select(value => value.RoomInstanceId).Distinct().Count(), Is.EqualTo(expected.Rooms));
            Assert.That(rooms.All(value => value.RoomDefinitionId == run.BasicRoomDefinitionId), Is.True);
            RoomContentAssignment[] assignments = floor.RoomContents.Assignments;
            Assert.That(assignments.Select(value => value.CategoryId), Is.EqualTo(expected.Categories));
            Assert.That(assignments.Select(value => value.OptionId), Is.EqualTo(expected.Options));
            Assert.That(assignments.Select(value => value.Sequence), Is.EqualTo(Enumerable.Range(0,
                assignments.Length).Select(value => (long)value)));
            for (int index = 0; index < assignments.Length; index++)
            {
                string shortCategory = assignments[index].CategoryId == Monster ? "monster" :
                    assignments[index].CategoryId == Trap ? "trap" : "loot";
                Assert.That(assignments[index].RoomInstanceId, Is.EqualTo("compat.floor.00.legacy-room.00"));
                Assert.That(assignments[index].AssignmentId, Is.EqualTo(assignments[index].RoomInstanceId +
                    ".content." + shortCategory + "." + index.ToString("D4")));
            }
            Assert.That(floor.RoomContents.NextSequence, Is.EqualTo(assignments.Length));
            Assert.That(floor.RoomContents.RoomSemantics, Has.Length.EqualTo(expected.Rooms));
            Assert.That(floor.RoomContents.RoomSemantics.All(value =>
                value.LegacyRoomOriginKind == expected.Semantics), Is.True);
            Assert.That(run.Execute.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(run.Execute.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(run.FirstRecovery.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            Assert.That(run.SecondRecovery.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
        }

        [TestCase(Winner.Placements)]
        [TestCase(Winner.Floor)]
        [TestCase(Winner.Assignments)]
        public void PrettyAndCompactLegacyRouteEvidenceProduceEquivalentCanonicalProjection(Winner winner)
        {
            int schema = winner == Winner.Placements ? 3 : winner == Winner.Floor ? 4 : 6;
            string compact = winner == Winner.Placements ? Placement(RoomPlacement(1)) :
                winner == Winner.Floor ? Floor(Node(0, RoomCategory, BasicRoom)) : Assign(Room(0));
            string pretty = compact.Replace(":{", ":\n  {").Replace(":[", ": [\n    ")
                .Replace("},{", "},\n    {").Replace("],", "\n  ],\n  ")
                .Replace(",\"", ",\n  \"");

            var compactRun = Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                "compact-" + winner, schema, compact);
            var prettyRun = Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(
                "pretty-" + winner, schema, pretty);

            Assert.That(JsonUtility.ToJson(prettyRun.State.Floors[0]),
                Is.EqualTo(JsonUtility.ToJson(compactRun.State.Floors[0])));
        }

        [Test]
        public void LowerEffectiveMismatch_ReturnsExactReasonAndNoAttempt()
        {
            string members = Floor(Node(0, RoomCategory, BasicRoom) + "," + Node(1, Monster, Skeleton)) +
                "," + Placement(Entry(Monster, Goblin, 1));
            DetachedSpatialMigrationPreparationResult result =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareSemanticResult(4, members);
            Assert.That(result.Attempt, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
        }

        [TestCase("LowerNarrowHall", "placement.option.room.narrow_hall",
            DetachedSpatialMigrationPreparer.OutcomeMismatchReason)]
        [TestCase("LowerCategoryMismatch", "placement.option.monster.skeleton",
            DetachedSpatialMigrationPreparer.OutcomeMismatchReason)]
        [TestCase("LowerBlankRoom", "", DetachedSpatialMigrationPreparer.OutcomeMismatchReason)]
        public void LowerEffectiveRoomConflict_ReturnsExactReasonAndNoAttempt(string name,
            string lowerRoomOption, string expectedReason)
        {
            string members = Floor(Node(0, RoomCategory, BasicRoom)) + "," +
                Placement(Entry(RoomCategory, lowerRoomOption, 1));
            DetachedSpatialMigrationPreparationResult result =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareSemanticResult(4, members);
            Assert.That(result.Attempt, Is.Null, name);
            Assert.That(result.Reason, Is.EqualTo(expectedReason), name);
        }

        [Test]
        public void LowerExplicitRoomCannotAgreeWithImplicitWinner()
        {
            string members = Floor(Node(1, Monster, Skeleton)) + "," + Placement(RoomPlacement(1));
            DetachedSpatialMigrationPreparationResult result =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareSemanticResult(4, members);
            Assert.That(result.Attempt, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
        }

        [Test]
        public void LowerTiedRoomRevision_ReturnsDuplicatePlacementRevisionAndNoAttempt()
        {
            string members = Floor(Node(0, RoomCategory, BasicRoom)) + "," +
                Placement(RoomPlacement(2) + "," + RoomPlacement(2));
            DetachedSpatialMigrationPreparationResult result =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareSemanticResult(4, members);
            Assert.That(result.Attempt, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(
                DetachedSpatialMigrationPreparer.DuplicatePlacementRevisionReason));
        }

        private static void AssertWinner(RawSavePayloadClassification value, Winner winner)
        {
            switch (winner)
            {
                case Winner.Assignments:
                    Assert.That(value.RoomSlotAssignmentsPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
                    break;
                case Winner.Floor:
                    Assert.That(value.RoomSlotAssignmentsPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
                    Assert.That(value.FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
                    break;
                case Winner.Placements:
                    Assert.That(value.RoomSlotAssignmentsPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
                    Assert.That(value.FloorLayoutPresence, Is.EqualTo(RawLegacyRoutePresence.Absent));
                    Assert.That(value.DungeonPlacementsPresence, Is.EqualTo(RawLegacyRoutePresence.Present));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(winner));
            }
        }
        private static bool ContainsMember(string members, string memberName) =>
            members.IndexOf("\"" + memberName + "\"", StringComparison.Ordinal) >= 0;
        private static TestCaseData Data(string id, int schema, Winner winner, string members, int rooms,
            string[] diagnostics = null, string[] categories = null, string[] options = null,
            LegacyRoomOriginKind semantics = LegacyRoomOriginKind.MigratedExplicitLegacyRoom) =>
            new TestCaseData(new Expected { Id = id, Schema = schema, Winner = winner, Members = members,
                Rooms = rooms, Diagnostics = (diagnostics ?? Array.Empty<string>()).ToArray(), Categories = categories ??
                Array.Empty<string>(), Options = options ?? Array.Empty<string>(), Semantics = semantics }).SetName("Semantic_" + id);

        private const string RoomCategory = "placement.category.room", Monster = "placement.category.monster",
            Trap = "placement.category.trap", Loot = "placement.category.loot_node", BasicRoom = "placement.option.room.basic",
            NarrowHall = "placement.option.room.narrow_hall", Skeleton = "placement.option.monster.skeleton",
            Goblin = "placement.option.monster.goblin", Spike = "placement.option.trap.spike",
            BasicLoot = "placement.option.loot_node.basic", Agreement = "gd66.diagnostic.lower_model_agreement",
            Contribution = "gd66.diagnostic.lower_effective_content_contributed",
            Ineffective = "gd66.diagnostic.lower_ineffective_conflict",
            MissingRoom = "gd66.diagnostic.missing_explicit_room_supported_content",
            ImplicitRoom = "gd66.diagnostic.implicit_basic_container_created";
        private static string Assign(string rooms) => "\"mvpRoomSlotAssignments\":{\"Rooms\":[" + rooms + "],\"NextRevision\":3}";
        private static string Room(int index, string monster = null) => "{\"FloorIndex\":0,\"RoomIndex\":" + index +
            ",\"RoomOptionId\":\"" + BasicRoom + "\",\"MonsterOptionIds\":" + (monster == null ? "[]" : "[\"" + monster + "\"]") +
            ",\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}";
        private static string Floor(string nodes) => "\"mvpDungeonFloorLayout\":{\"Nodes\":[" + nodes + "],\"NextRevision\":4}";
        private static string Node(int index, string category, string option) => "{\"FloorIndex\":0,\"NodeIndex\":" + index +
            ",\"SlotId\":\"slot." + index + "\",\"CategoryId\":\"" + category + "\",\"OptionId\":\"" + option + "\",\"Revision\":1}";
        private static string Placement(string entries) => "\"mvpDungeonPlacements\":{\"Entries\":[" + entries + "],\"NextRevision\":5}";
        private static string RoomPlacement(int revision) => Entry(RoomCategory, BasicRoom, revision);
        private static string Entry(string category, string option, int revision) => "{\"CategoryId\":\"" + category +
            "\",\"OptionId\":\"" + option + "\",\"Revision\":" + revision + "}";
    }
}
#endif
