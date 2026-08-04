#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedCompleteSaveSemanticValidationTests
    {
        public sealed class Mutation
        { internal string Name; internal Action<DetachedCanonicalSpatialSaveState> Apply; }

        public static IEnumerable<TestCaseData> SharedMutations
        {
            get
            {
                yield return Case("UnknownFloor", state => state.Floors[0].FloorDefinitionId = "floor.unknown");
                yield return Case("UnknownRoom", state => state.Floors[0].Layout.Rooms[0].RoomDefinitionId = "room.unknown");
                yield return Case("WrongFixedKind", state => state.Floors[0].FixedStructures[0].Kind =
                    state.Floors[0].FixedStructures[0].Kind == FixedSpatialStructureKind.Entrance ?
                    FixedSpatialStructureKind.CompletionTerminal : FixedSpatialStructureKind.Entrance);
                yield return Case("WrongAssignmentCategory", state => state.Floors[0].RoomContents.Assignments[0].CategoryId =
                    "placement.category.trap");
                yield return Case("UnconfiguredOption", state => state.Floors[0].RoomContents.Assignments[0].OptionId =
                    "placement.option.monster.unknown");
                yield return Case("CapacityOverflow", Overflow);
                yield return Case("BrokenRequiredRoute", state => state.Floors[0].Layout.Edges =
                    state.Floors[0].Layout.Edges.Take(state.Floors[0].Layout.Edges.Length - 1).ToArray());
                yield return Case("UnapprovedCorridor", state => { FloorRouteEdge edge = state.Floors[0].Layout.Edges[0];
                    edge.ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor;
                    edge.CorridorDefinitionId = "corridor.unknown"; });
            }
        }

        [TestCaseSource(nameof(SharedMutations))]
        public void SharedProductionMutation_IsRejectedByBothModes(Mutation mutation)
        {
            var fixture = Baseline("mutation-" + mutation.Name, false);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            mutation.Apply(changed);
            Assert.That(Validate(fixture, changed, false), Is.False);
            Assert.That(Validate(fixture, changed, true), Is.False);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
        }

        [Test]
        public void HistoricalMarkerIdentity_CurrentTargetAccepts_UnfinishedRejects()
        {
            var fixture = Baseline("marker", false);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            changed.Authority.MigrationTransactionId = "gd66-" + new string('a', 64);
            changed.Authority.MigrationDescriptorFingerprint = new string('b', 64);
            Assert.That(Validate(fixture, changed, false), Is.True);
            Assert.That(Validate(fixture, changed, true), Is.False);
        }

        [Test]
        public void SwappedR2RoomGeometry_CurrentTargetAccepts_UnfinishedRejects()
        {
            var fixture = Baseline("geometry", true);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            RoomSpatialInstance first = changed.Floors[0].Layout.Rooms[0];
            RoomSpatialInstance second = changed.Floors[0].Layout.Rooms[1];
            TileCoordinate anchor = first.Anchor; CardinalOrientation orientation = first.Orientation;
            first.Anchor = second.Anchor; first.Orientation = second.Orientation;
            second.Anchor = anchor; second.Orientation = orientation;
            Assert.That(Validate(fixture, changed, false), Is.True);
            Assert.That(Validate(fixture, changed, true), Is.False);
        }

        private static Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution Baseline(
            string identity, bool r2)
        {
            string rooms = Room(0, "placement.option.monster.skeleton") + (r2 ? "," + Room(1) : "");
            return Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(identity, 5,
                "\"mvpRoomSlotAssignments\":{\"Rooms\":[" + rooms + "],\"NextRevision\":3}");
        }

        private static bool Validate(Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture,
            DetachedCanonicalSpatialSaveState state, bool unfinished)
        {
            DetachedWholeSaveResult built = DetachedWholeSaveCandidateSerializer.BuildPrepared(fixture.Classification,
                state, fixture.Limits, fixture.WholeLimits);
            if (!built.IsSuccess) return false;
            return unfinished ? DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                built.Candidate.GetBytes(), fixture.UnfinishedContext).IsValid :
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                    built.Candidate.GetBytes(), fixture.CurrentContext).IsValid;
        }

        private static DetachedCanonicalSpatialSaveState Clone(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits)
        {
            byte[] bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits).Value;
            return CanonicalSpatialSaveSerializer.Parse(bytes, limits).Value;
        }

        private static void Overflow(DetachedCanonicalSpatialSaveState state)
        {
            string room = state.Floors[0].Layout.Rooms[0].RoomInstanceId;
            state.Floors[0].RoomContents.Assignments = Enumerable.Range(0, 100).Select(index =>
                new RoomContentAssignment { AssignmentId = room + ".content.monster." + index.ToString("D4"),
                    RoomInstanceId = room, CategoryId = "placement.category.monster",
                    OptionId = "placement.option.monster.skeleton", Sequence = index }).ToArray();
            state.Floors[0].RoomContents.NextSequence = 100;
        }
        private static TestCaseData Case(string name, Action<DetachedCanonicalSpatialSaveState> apply) =>
            new TestCaseData(new Mutation { Name = name, Apply = apply }).SetName("ProductionSemantic_" + name);
        private static string Room(int index, string monster = null) => "{\"FloorIndex\":0,\"RoomIndex\":" + index +
            ",\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":" +
            (monster == null ? "[]" : "[\"" + monster + "\"]") +
            ",\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}";
    }
}
#endif
