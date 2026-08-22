#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;

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
                yield return Case("CapacityOverflow", Overflow);
                yield return Case("BrokenRequiredRoute", state => state.Floors[0].Layout.Edges =
                    state.Floors[0].Layout.Edges.Take(state.Floors[0].Layout.Edges.Length - 1).ToArray());
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





        public static IEnumerable<TestCaseData> FixedStructureBoundsMutations
        {
            get
            {
                yield return new TestCaseData(FixedSpatialStructureKind.Entrance).SetName("FixedBounds_Entrance");
                yield return new TestCaseData(FixedSpatialStructureKind.CompletionTerminal).SetName("FixedBounds_Completion");
            }
        }



        public static IEnumerable<TestCaseData> ExtraFixedStructureMutations
        {
            get
            {
                yield return new TestCaseData(FixedSpatialStructureKind.Entrance,
                    "spatial.fixed.test_extra_entrance", "compat.floor.00.fixed.test-extra-entrance")
                    .SetName("ExtraFixed_Entrance");
                yield return new TestCaseData(FixedSpatialStructureKind.CompletionTerminal,
                    "spatial.fixed.test_extra_completion", "compat.floor.00.fixed.test-extra-completion")
                    .SetName("ExtraFixed_Completion");
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
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.CurrentContext).IsValid, Is.True);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.UnfinishedContext).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), Rebound(fixture, changedCandidate.Candidate.Sha256)).IsValid,
                Is.False);
        }

        [TestCaseSource(nameof(ExactContextMutations))]
        public void ExactPinnedIdentity_CurrentTargetAccepts_UnfinishedRejects(Mutation mutation)
        {
            var fixture = Baseline("context-" + mutation.Name, true);
            Assert.That(Validate(fixture, fixture.State, false), Is.True);
            Assert.That(Validate(fixture, fixture.State, true), Is.True);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            mutation.Apply(changed);
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.CurrentContext).IsValid, Is.True);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.UnfinishedContext).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), Rebound(fixture, changedCandidate.Candidate.Sha256)).IsValid,
                Is.False);
        }

        [TestCaseSource(nameof(FixedStructureBoundsMutations))]
        public void FixedStructureFootprint_OutOfBounds_IsFixedStructureIssue(FixedSpatialStructureKind kind)
        {
            var fixture = Baseline("fixed-bounds-" + kind, false);
            SpatialContentCatalog catalog = fixture.Production.Catalog;
            SavedSpatialFloor floor = fixture.State.Floors[0];
            FloorSpatialConfiguration floorDefinition = ResolveFloor(catalog, floor);
            Assert.That(floor.FixedStructures.Count(value => value.Kind == FixedSpatialStructureKind.Entrance), Is.EqualTo(1));
            Assert.That(floor.FixedStructures.Count(value => value.Kind == FixedSpatialStructureKind.CompletionTerminal), Is.EqualTo(1));
            AssertSemanticIssues(fixture.State, fixture.Production, fixture.LegacyBytes, fixture.Limits);
            SavedFixedSpatialStructure original = floor.FixedStructures.Single(value => value.Kind == kind);
            FixedSpatialStructureDefinition definition = ResolveFixed(catalog, original.FixedStructureDefinitionId);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            SavedFixedSpatialStructure target = changed.Floors[0].FixedStructures.Single(value => value.Kind == kind);
            target.Anchor = OutsideAnchor(floorDefinition.Bounds, definition.GrossFootprint, target.Orientation);
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(CanonicalSpatialSaveContracts.Validate(changed, fixture.Limits.Spatial, true).IsValid, Is.True);
            Assert.That(FloorLayoutValid(changed.Floors[0], catalog, fixture.Limits), Is.False);
            DetachedCanonicalProductionSemanticIssue[] issues = SemanticIssues(changed, fixture.Production,
                fixture.LegacyBytes, fixture.Limits);
            Assert.That(issues, Is.EquivalentTo(new[] { DetachedCanonicalProductionSemanticIssue.FixedStructure }));
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.CurrentContext).IsValid, Is.False);
        }

        [Test]
        public void ConfiguredOption_RegistryValidGoblinMissingFromConfig_IsAssignmentOptionOnly()
        {
            var fixture = Baseline("configured-option", false);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            RoomContentAssignment assignment = changed.Floors[0].RoomContents.Assignments[0];
            assignment.OptionId = MvpDungeonPlacementIds.GoblinOptionId;
            assignment.CategoryId = MvpDungeonPlacementIds.MonsterCategoryId;
            Assert.That(MvpDungeonPlacementIds.TryGetCategoryForOption(assignment.OptionId, out string category), Is.True);
            Assert.That(category, Is.EqualTo(MvpDungeonPlacementIds.MonsterCategoryId));
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(CanonicalSpatialSaveContracts.Validate(changed, fixture.Limits.Spatial, true).IsValid, Is.True);
            Assert.That(SemanticIssues(changed, fixture.Production, fixture.LegacyBytes, fixture.Limits), Is.Empty);
            byte[] reducedConfig = WithoutOption(fixture.LegacyBytes, MvpDungeonPlacementIds.GoblinOptionId);
            DetachedCanonicalProductionSemanticIssue[] issues = SemanticIssues(changed, fixture.Production,
                reducedConfig, fixture.Limits);
            Assert.That(issues, Has.Member(DetachedCanonicalProductionSemanticIssue.AssignmentOption));
            Assert.That(issues, Has.No.Member(DetachedCanonicalProductionSemanticIssue.AssignmentCategory));
            var reducedContext = new DetachedCurrentTargetValidationContext(fixture.Compatibility,
                fixture.Production, reducedConfig, fixture.Limits);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), reducedContext).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), fixture.CurrentContext).IsValid, Is.True);
        }

        [Test]
        public void UnapprovedCorridor_KnownDefinitionAbsentFromAllowedList_IsCorridorDefinitionOnly()
        {
            var fixture = Baseline("corridor-isolation", false);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            ProductionSpatialContentSnapshot withCorridor = CloneProductionWithTestCorridor(fixture.Production, false,
                out SpatialContentCatalog catalog);
            FloorRouteEdge edge = changed.Floors[0].Layout.Edges[0];
            edge.ConnectionKind = FloorRouteConnectionKind.PhysicalCorridor;
            edge.CorridorDefinitionId = TestCorridorId;
            edge.Footprint = new ResolvedTileFootprint(new[] { FreeTile(changed.Floors[0], catalog, fixture.Limits) });
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(CanonicalSpatialSaveContracts.Validate(changed, fixture.Limits.Spatial, true).IsValid, Is.True);
            Assert.That(FloorLayoutValid(changed.Floors[0], catalog, fixture.Limits), Is.True);
            DetachedCanonicalProductionSemanticIssue[] issues = SemanticIssues(changed, withCorridor,
                fixture.LegacyBytes, fixture.Limits);
            Assert.That(issues, Has.Member(DetachedCanonicalProductionSemanticIssue.CorridorDefinition));
            Assert.That(issues, Has.No.Member(DetachedCanonicalProductionSemanticIssue.FloorLayout));
            var rejectedContext = new DetachedCurrentTargetValidationContext(fixture.Compatibility,
                withCorridor, fixture.LegacyBytes, fixture.Limits);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), rejectedContext).IsValid, Is.False);
            ProductionSpatialContentSnapshot allowed = CloneProductionWithTestCorridor(fixture.Production, true,
                out _);
            Assert.That(SemanticIssues(changed, allowed, fixture.LegacyBytes, fixture.Limits), Is.Empty);
        }





        [TestCaseSource(nameof(ExtraFixedStructureMutations))]
        public void ExtraFixedStructure_IndividuallyValid_IsFixedStructureCardinalityIssue(
            FixedSpatialStructureKind kind, string definitionId, string instanceId)
        {
            var fixture = Baseline("extra-fixed-" + kind, false);
            ProductionSpatialContentSnapshot testProduction = CloneProductionWithExtraFixed(
                fixture.Production, kind, definitionId, out SpatialContentCatalog catalog);
            AssertSemanticIssues(fixture.State, fixture.Production, fixture.LegacyBytes, fixture.Limits);
            AssertSemanticIssues(fixture.State, testProduction, fixture.LegacyBytes, fixture.Limits);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            SavedSpatialFloor floor = changed.Floors[0];
            FixedSpatialStructureDefinition definition = ResolveFixed(catalog, definitionId);
            CardinalOrientation orientation = (definition.AllowedOrientations ??
                Array.Empty<CardinalOrientation>())[0];
            var extra = new SavedFixedSpatialStructure
            {
                FixedStructureInstanceId = instanceId,
                FixedStructureDefinitionId = definitionId,
                Kind = kind,
                FloorInstanceId = floor.FloorInstanceId,
                Anchor = FreeFixedAnchor(floor, catalog, definition, orientation, fixture.Limits),
                Orientation = orientation
            };
            floor.FixedStructures = floor.FixedStructures.Concat(new[] { extra })
                .OrderBy(value => value.FixedStructureInstanceId, StringComparer.Ordinal).ToArray();
            Assert.That(AllFixedStructuresIndividuallyValid(floor, catalog, fixture.Limits), Is.True);
            DetachedWholeSaveResult changedCandidate = Build(fixture, changed);
            Assert.That(changedCandidate.IsSuccess, Is.True, changedCandidate.Reason);
            Assert.That(CanonicalSpatialSaveContracts.Validate(changed, fixture.Limits.Spatial, true).IsValid, Is.True);
            Assert.That(FloorLayoutValid(floor, catalog, fixture.Limits), Is.True);
            DetachedCanonicalProductionSemanticIssue[] issues = SemanticIssues(changed, testProduction,
                fixture.LegacyBytes, fixture.Limits);
            Assert.That(issues, Is.EquivalentTo(new[] { DetachedCanonicalProductionSemanticIssue.FixedStructure }));
            var context = new DetachedCurrentTargetValidationContext(fixture.Compatibility,
                testProduction, fixture.LegacyBytes, fixture.Limits);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                changedCandidate.Candidate.GetBytes(), context).IsValid, Is.False);
        }

        [Test]
        public void ExtraFixedStructure_ActualOverlapRetainsFloorLayoutIssue()
        {
            var fixture = Baseline("extra-fixed-overlap", false);
            ProductionSpatialContentSnapshot production = CloneProductionWithExtraFixed(fixture.Production,
                FixedSpatialStructureKind.Entrance, "spatial.fixed.test_extra_overlap",
                out SpatialContentCatalog catalog);
            DetachedCanonicalSpatialSaveState changed = Clone(fixture.State, fixture.Limits);
            SavedSpatialFloor floor = changed.Floors[0];
            SavedFixedSpatialStructure exemplar = floor.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.Entrance);
            floor.FixedStructures = floor.FixedStructures.Concat(new[] { new SavedFixedSpatialStructure
            {
                FixedStructureInstanceId = "compat.floor.00.fixed.test-extra-overlap",
                FixedStructureDefinitionId = "spatial.fixed.test_extra_overlap",
                Kind = FixedSpatialStructureKind.Entrance,
                FloorInstanceId = floor.FloorInstanceId,
                Anchor = exemplar.Anchor,
                Orientation = exemplar.Orientation
            } }).ToArray();
            Assert.That(FloorLayoutValid(floor, catalog, fixture.Limits), Is.False);
            Assert.That(SemanticIssues(changed, production, fixture.LegacyBytes, fixture.Limits),
                Is.EquivalentTo(new[] { DetachedCanonicalProductionSemanticIssue.FloorLayout,
                    DetachedCanonicalProductionSemanticIssue.FixedStructure }));
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



        private const string TestCorridorId = "spatial.corridor.test_unapproved";

        private static DetachedUnfinishedAttemptValidationContext Rebound(
            Gd66DetachedSpatialMigrationTransactionTests.SemanticFixtureExecution fixture, string candidateSha256) =>
            new DetachedUnfinishedAttemptValidationContext(fixture.Attempt.Descriptor,
                fixture.Attempt.TransactionId, fixture.Attempt.DescriptorFingerprint, candidateSha256,
                fixture.UnfinishedContext.SelectedContract, fixture.UnfinishedContext.Profile,
                fixture.UnfinishedContext.Geometry, fixture.UnfinishedContext.Production,
                fixture.LegacyBytes, fixture.ValidationInputs, fixture.Limits);

        private static DetachedCanonicalProductionSemanticIssue[] SemanticIssues(
            DetachedCanonicalSpatialSaveState state, ProductionSpatialContentSnapshot production,
            byte[] configurationBytes, CanonicalSpatialSerializationLimits limits) =>
            DetachedCanonicalProductionSemanticValidation.Validate(state, production,
                LegacyGameplayConfigurationContract.Parse(configurationBytes), limits.Spatial).Issues;

        private static void AssertSemanticIssues(DetachedCanonicalSpatialSaveState state,
            ProductionSpatialContentSnapshot production, byte[] configurationBytes,
            CanonicalSpatialSerializationLimits limits) =>
            Assert.That(SemanticIssues(state, production, configurationBytes, limits), Is.Empty);

        private static FloorSpatialConfiguration ResolveFloor(SpatialContentCatalog catalog, SavedSpatialFloor floor) =>
            (catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>()).Single(value => value != null &&
                value.FloorDefinitionId == floor.FloorDefinitionId && value.FloorIndex == floor.FloorIndex);

        private static FixedSpatialStructureDefinition ResolveFixed(SpatialContentCatalog catalog, string id) =>
            (catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>()).Single(value => value != null &&
                value.StructureDefinitionId == id);

        private static TileCoordinate OutsideAnchor(RectangularFloorBounds bounds,
            RectangularFootprintDefinition footprint, CardinalOrientation orientation)
        {
            int width = orientation == CardinalOrientation.Ninety || orientation == CardinalOrientation.TwoSeventy ?
                footprint.Height : footprint.Width;
            return new TileCoordinate(bounds.Minimum.X + bounds.Width - width + 1, bounds.Minimum.Y);
        }

        private static bool FloorLayoutValid(SavedSpatialFloor floor, SpatialContentCatalog catalog,
            CanonicalSpatialSerializationLimits limits)
        {
            FloorSpatialConfiguration floorDefinition = ResolveFloor(catalog, floor);
            return FloorLayoutValidator.Validate(floor.Layout, floorDefinition,
                catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>(),
                catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>(),
                new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                floor.FixedStructures, catalog.FixedStructures).IsValid;
        }

        private static byte[] WithoutOption(byte[] sourceConfigBytes, string optionId)
        {
            RunSimulationConfig config = LegacyGameplayConfigurationContract.Parse(sourceConfigBytes);
            config.MvpPlacementEffects = (config.MvpPlacementEffects ?? Array.Empty<MvpPlacementEffectConfig>())
                .Where(value => value == null || value.OptionId != optionId).ToArray();
            return LegacyGameplayConfigurationContract.SerializeCanonical(config);
        }

        private static ProductionSpatialContentSnapshot CloneProductionWithTestCorridor(
            ProductionSpatialContentSnapshot production, bool allowOnFloor, out SpatialContentCatalog catalog)
        {
            catalog = CloneCatalog(production.Catalog);
            CorridorSpatialDefinition source = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>())
                .First(value => value != null);
            CorridorSpatialDefinition copy = new CorridorSpatialDefinition
            {
                CorridorDefinitionId = TestCorridorId,
                LocalizationKey = source.LocalizationKey,
                Category = source.Category,
                MinimumLength = source.MinimumLength,
                MaximumLength = source.MaximumLength,
                Width = source.Width,
                MonsterCapacity = source.MonsterCapacity,
                TrapCapacity = source.TrapCapacity,
                LootCapacity = source.LootCapacity,
                AllowedOrientations = (CardinalOrientation[])(source.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Clone(),
                CompatibleSocketTypeIds = (string[])(source.CompatibleSocketTypeIds ?? Array.Empty<string>()).Clone()
            };
            catalog.Corridors = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>()).Concat(new[] { copy })
                .OrderBy(value => value.CorridorDefinitionId, StringComparer.Ordinal).ToArray();
            if (allowOnFloor)
            {
                FloorSpatialConfiguration floor = catalog.Floors[0];
                floor.AllowedCorridorDefinitionIds = (floor.AllowedCorridorDefinitionIds ?? Array.Empty<string>())
                    .Concat(new[] { TestCorridorId }).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }
            return new ProductionSpatialContentSnapshot(production.Manifest, catalog, production.Languages);
        }

        private static SpatialContentCatalog CloneCatalog(SpatialContentCatalog catalog) =>
            JsonUtility.FromJson<SpatialContentCatalog>(Encoding.UTF8.GetString(
                ProductionSpatialGeneratedSetParser.SerializeCanonical(catalog)));



        private static ProductionSpatialContentSnapshot CloneProductionWithExtraFixed(
            ProductionSpatialContentSnapshot production, FixedSpatialStructureKind kind, string definitionId,
            out SpatialContentCatalog catalog)
        {
            catalog = CloneCatalog(production.Catalog);
            FixedSpatialStructureDefinition source = (catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .First(value => value != null && value.Kind == kind);
            var copy = new FixedSpatialStructureDefinition
            {
                StructureDefinitionId = definitionId,
                LocalizationKey = source.LocalizationKey,
                Kind = source.Kind,
                GrossFootprint = source.GrossFootprint == null ? null :
                    new RectangularFootprintDefinition(source.GrossFootprint.Width, source.GrossFootprint.Height),
                ReservedTileOffsets = (TileCoordinate[])(source.ReservedTileOffsets ?? Array.Empty<TileCoordinate>()).Clone(),
                AllowedOrientations = (CardinalOrientation[])(source.AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Clone(),
                ConnectionPoints = (source.ConnectionPoints ?? Array.Empty<SpatialConnectionPointDefinition>())
                    .Select(value => value == null ? null : new SpatialConnectionPointDefinition
                    {
                        ConnectionPointId = value.ConnectionPointId,
                        Offset = value.Offset,
                        Facing = value.Facing,
                        SocketTypeId = value.SocketTypeId
                    }).ToArray(),
                MaximumConnectionCount = source.MaximumConnectionCount
            };
            catalog.FixedStructures = (catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .Concat(new[] { copy }).OrderBy(value => value.StructureDefinitionId, StringComparer.Ordinal).ToArray();
            return new ProductionSpatialContentSnapshot(production.Manifest, catalog, production.Languages);
        }

        private static bool AllFixedStructuresIndividuallyValid(SavedSpatialFloor floor,
            SpatialContentCatalog catalog, CanonicalSpatialSerializationLimits limits)
        {
            FloorSpatialConfiguration floorDefinition = ResolveFloor(catalog, floor);
            if (floorDefinition.Bounds == null || !floorDefinition.Bounds.IsValid) return false;
            foreach (SavedFixedSpatialStructure value in floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>())
            {
                FixedSpatialStructureDefinition[] matches = (catalog.FixedStructures ??
                    Array.Empty<FixedSpatialStructureDefinition>()).Where(definition => definition != null &&
                    definition.StructureDefinitionId == value?.FixedStructureDefinitionId).ToArray();
                if (value == null || matches.Length != 1 || matches[0].Kind != value.Kind ||
                    value.FloorInstanceId != floor.FloorInstanceId ||
                    !(matches[0].AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(value.Orientation) ||
                    !TileFootprintResolver.TryResolveRectangle(matches[0].GrossFootprint, value.Anchor,
                        value.Orientation, new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                        out ResolvedTileFootprint footprint) || footprint?.OccupiedTiles == null ||
                    footprint.OccupiedTiles.Any(tile => !floorDefinition.Bounds.Contains(tile))) return false;
            }
            return true;
        }

        private static TileCoordinate FreeTile(SavedSpatialFloor floor, SpatialContentCatalog catalog,
            CanonicalSpatialSerializationLimits limits)
        {
            FloorSpatialConfiguration floorDefinition = ResolveFloor(catalog, floor);
            HashSet<TileCoordinate> occupied = OccupiedTiles(floor, catalog, limits);
            RectangularFloorBounds bounds = floorDefinition.Bounds;
            for (int x = 0; x < bounds.Width; x++)
                for (int y = 0; y < bounds.Height; y++)
                {
                    var candidate = new TileCoordinate(bounds.Minimum.X + x, bounds.Minimum.Y + y);
                    if (!occupied.Contains(candidate)) return candidate;
                }
            Assert.Fail("No free corridor tile found in test-owned production bounds.");
            return default(TileCoordinate);
        }

        private static TileCoordinate FreeFixedAnchor(SavedSpatialFloor floor,
            SpatialContentCatalog catalog, FixedSpatialStructureDefinition definition,
            CardinalOrientation orientation, CanonicalSpatialSerializationLimits limits)
        {
            FloorSpatialConfiguration floorDefinition = ResolveFloor(catalog, floor);
            HashSet<TileCoordinate> occupied = OccupiedTiles(floor, catalog, limits);
            var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
            RectangularFloorBounds bounds = floorDefinition.Bounds;
            for (int x = 0; x < bounds.Width; x++)
                for (int y = 0; y < bounds.Height; y++)
                {
                    var candidate = new TileCoordinate(bounds.Minimum.X + x, bounds.Minimum.Y + y);
                    if (!TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint, candidate,
                            orientation, workload, out ResolvedTileFootprint footprint) ||
                        footprint.OccupiedTiles.Any(tile => !bounds.Contains(tile) || occupied.Contains(tile)) ||
                        occupied.Count + footprint.OccupiedTiles.Length >
                            floorDefinition.FinalFloorSpaceCapacity) continue;
                    return candidate;
                }
            Assert.Fail("No non-overlapping fixed-structure anchor found in test-owned production bounds.");
            return default(TileCoordinate);
        }

        private static HashSet<TileCoordinate> OccupiedTiles(SavedSpatialFloor floor,
            SpatialContentCatalog catalog, CanonicalSpatialSerializationLimits limits)
        {
            var occupied = new HashSet<TileCoordinate>();
            var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
            foreach (RoomSpatialInstance room in floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>())
            {
                RoomSpatialDefinition definition = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                    .Single(value => value != null && value.RoomDefinitionId == room.RoomDefinitionId);
                Assert.That(TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint,
                    room.Anchor, room.Orientation,
                    workload,
                    out ResolvedTileFootprint footprint), Is.True);
                foreach (TileCoordinate tile in footprint.OccupiedTiles) occupied.Add(tile);
            }
            foreach (SavedFixedSpatialStructure structure in floor.FixedStructures ??
                Array.Empty<SavedFixedSpatialStructure>())
            {
                FixedSpatialStructureDefinition definition = ResolveFixed(catalog,
                    structure.FixedStructureDefinitionId);
                Assert.That(TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint,
                    structure.Anchor, structure.Orientation, workload,
                    out ResolvedTileFootprint footprint), Is.True);
                foreach (TileCoordinate tile in footprint.OccupiedTiles) occupied.Add(tile);
            }
            foreach (FloorRouteEdge edge in floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>())
                if (edge?.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor)
                    foreach (TileCoordinate tile in edge.Footprint?.OccupiedTiles ??
                        Array.Empty<TileCoordinate>()) occupied.Add(tile);
            return occupied;
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
