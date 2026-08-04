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
        { internal string Name; internal bool UseR2; internal Action<DetachedCanonicalSpatialSaveState> Apply; }

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

        public static IEnumerable<TestCaseData> ExactContextMutations
        {
            get
            {
                yield return ContextCase("FloorIdentity", RenameFloor);
                yield return ContextCase("FixedIdentity", state => state.Floors[0].FixedStructures[0]
                    .FixedStructureInstanceId = "compat.floor.00.fixed.alternate");
                yield return ContextCase("NodeIdentity", RenameRoomNode);
                yield return ContextCase("EdgeIdentity", state => state.Floors[0].Layout.Edges[0].EdgeId =
                    "compat.floor.00.edge.direct.alternate");
                yield return ContextCase("AlternateRoute", AlternateRoute);
            }
        }



        public static IEnumerable<TestCaseData> ExactCandidateMutations
        {
            get
            {
                yield return CandidateCase("PopulatedR1ToEmpty", false,
                    state => state.Floors = Array.Empty<SavedSpatialFloor>());
                yield return CandidateCase("R2ToValidR1", true, ReplaceWithR1Floor);
                yield return CandidateCase("RemoveAssignment", false, state =>
                {
                    state.Floors[0].RoomContents.Assignments = Array.Empty<RoomContentAssignment>();
                    state.Floors[0].RoomContents.NextSequence = 0;
                });
                yield return CandidateCase("ChangeRoomSemantics", false, state =>
                    state.Floors[0].RoomContents.RoomSemantics[0].LegacyRoomOriginKind =
                        LegacyRoomOriginKind.CanonicalPlayerPlaced);
            }
        }

        public static IEnumerable<TestCaseData> ExpectedHashFailures
        {
            get
            {
                yield return new TestCaseData(new object[] { null }).SetName("ExpectedCandidateHash_Null");
                yield return new TestCaseData("not-a-sha").SetName("ExpectedCandidateHash_Malformed");
                yield return new TestCaseData(new string('0', 64)).SetName("ExpectedCandidateHash_Incorrect");
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

        [TestCaseSource(nameof(ExactContextMutations))]
        public void ExactPinnedIdentity_CurrentTargetAccepts_UnfinishedRejects(Mutation mutation)
        {
            var fixture = Baseline("context-" + mutation.Name, true);
            Assert.That(Validate(fixture, fixture.State, false), Is.True);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            mutation.Apply(changed);
            Assert.That(Validate(fixture, changed, false), Is.True);
            Assert.That(Validate(fixture, changed, true), Is.False);
        }



        [TestCaseSource(nameof(ExactCandidateMutations))]
        public void ExactPreparedCandidate_CurrentTargetAccepts_UnfinishedRejects(Mutation mutation)
        {
            var fixture = Baseline("exact-candidate-" + mutation.Name, mutation.UseR2);
            Assert.That(Validate(fixture, fixture.State, false), Is.True);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            mutation.Apply(changed);
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(changedCandidate.Candidate.Sha256, Is.Not.EqualTo(fixture.Attempt.CandidateSha256));
            Assert.That(Validate(fixture, changed, false), Is.True);
            Assert.That(Validate(fixture, changed, true), Is.False);
            Assert.That(Validate(fixture, fixture.State, false), Is.True);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
        }

        [TestCaseSource(nameof(ExpectedHashFailures))]
        public void ExpectedCandidateHash_InvalidOrWrong_FailsClosed(string expectedHash)
        {
            var fixture = Baseline("expected-hash", false);
            var context = new DetachedUnfinishedAttemptValidationContext(fixture.Attempt.Descriptor,
                fixture.Attempt.TransactionId, fixture.Attempt.DescriptorFingerprint, expectedHash,
                fixture.UnfinishedContext.SelectedContract, fixture.UnfinishedContext.Profile,
                fixture.UnfinishedContext.Geometry, fixture.UnfinishedContext.Production,
                fixture.LegacyBytes, fixture.ValidationInputs, fixture.Limits);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Attempt.Candidate.GetBytes(), context).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Attempt.Candidate.GetBytes(), fixture.CurrentContext).IsValid, Is.True);
        }

        private static Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution Baseline(
            string identity, bool r2)
        {
            string rooms = Room(0, "placement.option.monster.skeleton") + (r2 ? "," + Room(1) : "");
            return Gd66DetachedSpatialMigrationTransactionTests.RunPopulatedSemanticFixture(identity, 5,
                "\"mvpRoomSlotAssignments\":{\"Rooms\":[" + rooms + "],\"NextRevision\":3}");
        }


        private static DetachedWholeSaveResult Build(
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture,
            DetachedCanonicalSpatialSaveState state) =>
            DetachedWholeSaveCandidateSerializer.BuildPrepared(fixture.Classification, state,
                fixture.Limits, fixture.WholeLimits);

        private static void ReplaceWithR1Floor(DetachedCanonicalSpatialSaveState state)
        {
            var r1 = Baseline("r2-to-r1-source", false);
            state.Floors = Clone(r1.State, r1.Limits).Floors;
        }

        private static bool Validate(Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture,
            DetachedCanonicalSpatialSaveState state, bool unfinished)
        {
            DetachedWholeSaveResult built = Build(fixture, state);
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
        private static void RenameFloor(DetachedCanonicalSpatialSaveState state)
        {
            SavedSpatialFloor floor = state.Floors[0]; const string renamed = "compat.floor.renamed";
            floor.FloorInstanceId = renamed; floor.Layout.FloorId = renamed;
            foreach (RoomSpatialInstance value in floor.Layout.Rooms) value.FloorId = renamed;
            foreach (SavedFixedSpatialStructure value in floor.FixedStructures) value.FloorInstanceId = renamed;
            foreach (FloorRouteNode value in floor.Layout.Nodes) value.FloorId = renamed;
            foreach (FloorRouteEdge value in floor.Layout.Edges) value.FloorId = renamed;
        }
        private static void RenameRoomNode(DetachedCanonicalSpatialSaveState state)
        {
            FloorRouteNode node = state.Floors[0].Layout.Nodes.First(value => value.Kind == FloorRouteNodeKind.Room);
            string previous = node.NodeId; node.NodeId = previous + ".renamed";
            foreach (FloorRouteEdge edge in state.Floors[0].Layout.Edges)
            { if (edge.SourceNodeId == previous) edge.SourceNodeId = node.NodeId;
              if (edge.DestinationNodeId == previous) edge.DestinationNodeId = node.NodeId; }
        }
        private static void AlternateRoute(DetachedCanonicalSpatialSaveState state)
        {
            FloorRouteEdge[] edges = state.Floors[0].Layout.Edges;
            string entrance = state.Floors[0].Layout.Nodes.First(value => value.Kind == FloorRouteNodeKind.Entrance).NodeId;
            string completion = state.Floors[0].Layout.Nodes.First(value => value.Kind == FloorRouteNodeKind.Completion).NodeId;
            string[] rooms = state.Floors[0].Layout.Nodes.Where(value => value.Kind == FloorRouteNodeKind.Room)
                .Select(value => value.NodeId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            edges[0].SourceNodeId = entrance; edges[0].DestinationNodeId = rooms[1];
            edges[1].SourceNodeId = rooms[1]; edges[1].DestinationNodeId = rooms[0];
            edges[2].SourceNodeId = rooms[0]; edges[2].DestinationNodeId = completion;
        }
        private static TestCaseData Case(string name, Action<DetachedCanonicalSpatialSaveState> apply) =>
            new TestCaseData(new Mutation { Name = name, Apply = apply }).SetName("ProductionSemantic_" + name);
        private static TestCaseData ContextCase(string name, Action<DetachedCanonicalSpatialSaveState> apply) =>
            new TestCaseData(new Mutation { Name = name, Apply = apply }).SetName("PinnedContext_" + name);
        private static TestCaseData CandidateCase(string name, bool r2, Action<DetachedCanonicalSpatialSaveState> apply) =>
            new TestCaseData(new Mutation { Name = name, UseR2 = r2, Apply = apply }).SetName("ExactCandidate_" + name);
        private static string Room(int index, string monster = null) => "{\"FloorIndex\":0,\"RoomIndex\":" + index +
            ",\"RoomOptionId\":\"placement.option.room.basic\",\"MonsterOptionIds\":" +
            (monster == null ? "[]" : "[\"" + monster + "\"]") +
            ",\"TrapOptionIds\":[],\"LootNodeOptionIds\":[]}";
    }
}
#endif
