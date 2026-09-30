#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSixA4Tests
    {
        internal static RunSimulationConfig Config() => JsonUtility.FromJson<RunSimulationConfig>(
            File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"));
        internal static DetachedCanonicalWriteAuthority Writer(Fixture f) => new DetachedCanonicalWriteAuthority(
            f.Production, f.Compatibility, f.Configuration, f.Context, f.Profile, f.RemovalPolicy, f.Economy,
            acquisition: f.Acquisition, branchingResearch: f.BranchingResearch,
            floorConstructionProfiles: PhaseSixA3FloorActivationEligibilityTests.Profiles(f),
            floorConstructionResearch: PhaseSixA3FloorActivationEligibilityTests.Research(f), runLoot: Loot());
        internal static Fixture Eligible() => PhaseSixA3FloorActivationEligibilityTests.Eligible();
        internal static string FloorTwo(Fixture f) => f.State.Floors.Single(x => x.FloorIndex == 1).FloorInstanceId;

        [Test]
        public void LockedProductionConfigurationIsValid()
        {
            var phaseSix = Config().PhaseSix;
            Assert.That(PhaseSixRunConfigValidation.IsValid(phaseSix), Is.True);
            Assert.That(phaseSix.TotalAppealWeight, Is.EqualTo(5.75));
            var shallow = phaseSix.Objectives.Single(x => x.Mode == "shallow");
            var target = phaseSix.Objectives.Single(x => x.Mode == "target_depth");
            var deepest = phaseSix.Objectives.Single(x => x.Mode == "deepest_reasonable");
            Assert.That(shallow.Weight, Is.EqualTo(.2)); Assert.That(shallow.PullStrength, Is.Zero);
            Assert.That(target.Weight, Is.EqualTo(.5)); Assert.That(target.PullStrength, Is.EqualTo(.75));
            Assert.That(target.TargetFloorIndex, Is.EqualTo(1));
            Assert.That(deepest.Weight, Is.EqualTo(.3)); Assert.That(deepest.PullStrength, Is.EqualTo(.5));
            Assert.That(LootRollResolver.Resolve(Loot(), Config().LootTableId, 0).success, Is.True);
            Assert.That(Snapshot(Eligible()), Is.Not.Null);
        }

        [Test]
        public void LifecyclePreviewIsPureAndActivationReopensWithoutManaOrInvestmentChanges()
        {
            var f = Eligible(); var writer = Writer(f);
            byte[] before = f.Session.GetCurrentBytes(); string live = JsonUtility.ToJson(f.Runtime);
            var preview = writer.PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(preview.IsCommittable, Is.True, preview.Reason);
            Assert.That(preview.RunSnapshot.Floors.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live));
            double mana = f.Runtime.structureRuntime.ManaReserve;
            var original = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, f.Context);
            var result = writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(JsonUtility.ToJson(new InvestmentEnvelope { Values = result.Validation.Investment }),
                Is.EqualTo(JsonUtility.ToJson(new InvestmentEnvelope { Values = original.Investment })));
            Assert.That(DetachedCanonicalSaveSession.Open(f.FileSystem.ReadAllBytes(f.ActivePath), f.Context, f.Profile).IsSuccess, Is.True);
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out int count), Is.True);
            Assert.That(count, Is.EqualTo(2));
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Deactivate, FloorTwo(f)));
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out count), Is.True);
            Assert.That(count, Is.EqualTo(1)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
        }
        [Serializable] private sealed class InvestmentEnvelope { public StructuralInvestmentRecord[] Values; }

        [TestCase(false)]
        [TestCase(true)]
        public void ConstructedFloorActivationPublishesThroughGameRootAndShowsLocalFeedback(bool nativeAuthority)
        {
            var f = Eligible();
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId);
            AddContent(f, 1, MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId);
            if (nativeAuthority)
            {
                // Native saves have no migration metadata. Unity's detached JSON clone
                // materializes those null strings as empty strings, unlike migrated fixtures.
                f.State.Authority.CreationKind = CanonicalSpatialCreationKind.NativeCanonical;
                f.State.Authority.MigrationTransactionId = null;
                f.State.Authority.MigrationDescriptorFingerprint = null;
                f = f.Rebase(f.State);
            }
            f.Reopen();
            var before = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            Assert.That(before.IsValid, Is.True, before.Reason);
            string originalSpatial = JsonUtility.ToJson(before.State);
            string originalInvestment = JsonUtility.ToJson(new InvestmentEnvelope { Values = before.Investment });
            double originalMana = f.Runtime.structureRuntime.ManaReserve;
            var go = new GameObject("PhaseSixA4GameRootActivation"); go.SetActive(false);
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                ConfigureLifecycle(root, f);
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                root.CycleSelectedCanonicalFloor();
                Assert.That(root.SelectedCanonicalFloor.FloorIndex, Is.EqualTo(1));
                Assert.That(root.SelectedCanonicalFloor.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
                Assert.That(root.Save.completedResearch.ProjectIds, Does.Contain("ac_100"));
                Assert.That(root.SaveService.PreviewFloorLifecycle(root.Save, FloorLifecycleAction.Activate,
                    FloorTwo(f)).IsCommittable, Is.True);
                var result = overlay.CommitFloorLifecycle(FloorLifecycleAction.Activate);
                Assert.That(result.IsSuccess, Is.True, result.Reason);
                Assert.That(root.Save.validatedCanonicalSpatialState.Floors.Single(x => x.FloorIndex == 1)
                    .ActivationState, Is.EqualTo(FloorActivationState.Active));
                Assert.That(CanonicalActiveFloorResolver.TryResolve(root.Save, f.Profile.Canonical.Spatial,
                    out int activeCount), Is.True);
                Assert.That(activeCount, Is.EqualTo(2));
                Assert.That(overlay.LifecycleFeedback,
                    Is.EqualTo(root.Content.GetString("ui.floor.lifecycle_success", "")));
                foreach (var action in new[] { FloorLifecycleAction.Deactivate, FloorLifecycleAction.Activate })
                {
                    var next = overlay.CommitFloorLifecycle(action);
                    Assert.That(next.IsSuccess, Is.True, next.Reason);
                    byte[] durable = f.FileSystem.ReadAllBytes(f.ActivePath);
                    CollectionAssert.AreEqual(durable, root.SaveService.CanonicalSession.GetCurrentBytes());
                    Assert.That(DetachedCanonicalSaveSession.Open(durable, f.Context, f.Profile).IsSuccess, Is.True);
                    var reopened = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(durable, f.Context);
                    Assert.That(reopened.IsValid, Is.True, reopened.Reason);
                    Assert.That(reopened.State.Floors.Single(x => x.FloorIndex == 0).ActivationState,
                        Is.EqualTo(FloorActivationState.Active));
                    Assert.That(CanonicalActiveFloorResolver.TryResolve(root.Save, f.Profile.Canonical.Spatial,
                        out activeCount), Is.True);
                    Assert.That(activeCount, Is.EqualTo(action == FloorLifecycleAction.Activate ? 2 : 1));
                    Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(originalMana));
                    Assert.That(JsonUtility.ToJson(new InvestmentEnvelope { Values = reopened.Investment }),
                        Is.EqualTo(originalInvestment));
                    // Compare every canonical identity/content/geometry/custody field after
                    // removing only the intended lifecycle difference on the detached readback.
                    reopened.State.Floors.Single(x => x.FloorIndex == 1).ActivationState = FloorActivationState.Inactive;
                    Assert.That(JsonUtility.ToJson(reopened.State), Is.EqualTo(originalSpatial));
                }
                overlay.SynchronizeStructuralConstructionPublication();
                overlay.RefreshStructuralConstructionAuthority();
                Assert.That(overlay.LifecycleFeedback, Is.Not.Empty);
                var repeated = overlay.CommitFloorLifecycle(FloorLifecycleAction.ActivateAllEligible);
                Assert.That(repeated.IsSuccess, Is.True, repeated.Reason);
                Assert.That(repeated.IsNoOp, Is.True);
                Assert.That(overlay.LifecycleFeedback,
                    Is.EqualTo(root.Content.GetString("ui.floor.lifecycle_success", "")));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void IncompleteConstructedFloorShowsExactReadableBlockerThroughGameRoot()
        {
            var f = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            var go = new GameObject("PhaseSixA4GameRootBlockedActivation"); go.SetActive(false);
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                ConfigureLifecycle(root, f);
                Assert.That(root.PreviewFloorConstruction().IsCommittable, Is.True);
                Assert.That(root.CommitFloorConstruction().IsSuccess, Is.True);
                root.CycleSelectedCanonicalFloor();
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                Assert.That(root.SelectedCanonicalFloor.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
                byte[] before = f.FileSystem.ReadAllBytes(f.ActivePath);
                var result = overlay.CommitFloorLifecycle(FloorLifecycleAction.Activate);
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Reason, Is.EqualTo(FloorActivationEligibilityReasons.ActivationLayoutInvalid));
                Assert.That(root.SelectedCanonicalFloor.ActivationState, Is.EqualTo(FloorActivationState.Inactive));
                Assert.That(overlay.LifecycleFeedback,
                    Is.EqualTo(root.Content.GetString(result.Reason, "")));
                Assert.That(overlay.LifecycleFeedback, Is.Not.Empty.And.Not.EqualTo(result.Reason));
                overlay.SynchronizeStructuralConstructionPublication();
                overlay.RefreshStructuralConstructionAuthority();
                Assert.That(overlay.LifecycleFeedback, Is.Not.Empty);
                var all = overlay.CommitFloorLifecycle(FloorLifecycleAction.ActivateAllEligible);
                Assert.That(all.IsSuccess, Is.False);
                Assert.That(all.Reason, Is.EqualTo(result.Reason));
                Assert.That(overlay.LifecycleFeedback,
                    Is.EqualTo(root.Content.GetString(all.Reason, "")));
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void EveryFloorLifecycleReasonHasReadableEnglishLocalization()
        {
            var f = Eligible();
            var go = new GameObject("PhaseSixA4LifecycleLocalization"); go.SetActive(false);
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                string[] reasons = {
                    FloorActivationEligibilityReasons.InvalidContext,
                    FloorActivationEligibilityReasons.CanonicalStateInvalid,
                    FloorActivationEligibilityReasons.TargetConfigurationInvalid,
                    FloorActivationEligibilityReasons.TargetIdentityInvalid,
                    FloorActivationEligibilityReasons.TargetNotConstructed,
                    FloorActivationEligibilityReasons.TargetAlreadyActive,
                    FloorActivationEligibilityReasons.ResearchConfigurationInvalid,
                    FloorActivationEligibilityReasons.ResearchRequired,
                    FloorActivationEligibilityReasons.ShallowerFloorInactive,
                    FloorActivationEligibilityReasons.ActivationLayoutInvalid,
                    FloorActivationEligibilityReasons.RequiredRouteMissingRoom,
                    FloorActivationEligibilityReasons.ProductionSemanticsInvalid,
                    "floor.lifecycle.invalid_state", "floor.lifecycle.first_floor_required",
                    "floor.lifecycle.stale_session",
                    DetachedCanonicalWriteAuthority.AtomicSaveFailedReason,
                    DetachedCanonicalWriteAuthority.RecoveryRequiredReason,
                    DetachedCanonicalSpatialMutation.ValidationFailedReason,
                    CanonicalMvpRouteProjection.ContradictoryAuthorityReason,
                    DetachedWholeSaveCandidateSerializer.CandidateInvalidReason,
                    DetachedWholeSaveCandidateSerializer.WorkloadExceededReason,
                    DetachedWholeSaveCandidateSerializer.UnknownMemberUnpreservableReason,
                    RawSavePayloadClassifier.UnreadableReason
                };
                Assert.That(FloorActivationEligibilityReasons.ActivationLayoutInvalid,
                    Is.EqualTo("floor.activation.layout_invalid"));
                foreach (string reason in reasons)
                {
                    string localized = root.Content.GetString(reason, reason);
                    Assert.That(localized, Is.Not.Empty.And.Not.EqualTo(reason), reason);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void ConfigureLifecycle(GameRoot root, Fixture f)
        {
            root.SaveService.ConfigureRunLoot(Loot());
            root.SaveService.ConfigureFloorConstruction(PhaseSixA3FloorActivationEligibilityTests.Profiles(f),
                PhaseSixA3FloorActivationEligibilityTests.Research(f));
        }

        [Test]
        public void CommitReresolvesPermissionAndPublishesNothingWhenBlocked()
        {
            var f = Eligible(); var writer = Writer(f); byte[] before = f.Session.GetCurrentBytes();
            Assert.That(writer.PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)).IsCommittable, Is.True);
            f.Runtime.completedResearch.ProjectIds = Array.Empty<string>();
            var result = writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Reason, Is.EqualTo(FloorActivationEligibilityReasons.ResearchRequired));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.State.Floors.Single(x => x.FloorIndex == 1).ActivationState, Is.EqualTo(FloorActivationState.Inactive));
        }

        [Test]
        public void FloorOneCannotDeactivate()
        {
            var f = Eligible();
            Assert.That(Writer(f).PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Deactivate,
                f.State.Floors[0].FloorInstanceId).Reason, Is.EqualTo("floor.lifecycle.first_floor_required"));
        }

        [Test]
        public void ActivateAllAlreadyActiveIsValidatedNoOpAndReactivatesAfterDeactivation()
        {
            var f = Eligible(); var writer = Writer(f);
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null));
            byte[] activeBytes = f.FileSystem.ReadAllBytes(f.ActivePath);
            string live = JsonUtility.ToJson(f.Runtime);
            int operations = f.FileSystem.Operations.Count();
            var preview = writer.PreviewFloorLifecycle(f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(preview.IsCommittable, Is.True, preview.Reason);
            Assert.That(preview.IsNoOp, Is.True);
            Assert.That(preview.RunSnapshot.Floors.Count, Is.EqualTo(2));
            var repeated = writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(repeated.IsSuccess, Is.True, repeated.Reason);
            Assert.That(repeated.IsNoOp, Is.True);
            Assert.That(repeated.RuntimeProjection, Is.SameAs(f.Runtime));
            Assert.That(repeated.Session, Is.SameAs(f.Session));
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations + 1), "Only the stale-session read is allowed.");
            CollectionAssert.AreEqual(activeBytes, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live), "Mana, investment, layout, content, and IDs stay unchanged.");
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out int count), Is.True);
            Assert.That(count, Is.EqualTo(2));
            var direct = writer.PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(direct.IsCommittable, Is.False);
            Assert.That(direct.Reason, Is.EqualTo(FloorActivationEligibilityReasons.TargetAlreadyActive));
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Deactivate, FloorTwo(f)));
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out count), Is.True);
            Assert.That(count, Is.EqualTo(1));
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null));
            Assert.That(CanonicalActiveFloorResolver.TryResolve(f.Runtime, f.Profile.Canonical.Spatial, out count), Is.True);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void ActivateAllNoOpThroughSaveServiceDoesNotRepublishOrReplaceSession()
        {
            var f = Eligible();
            var service = PhaseFiveBBranchIntegrationTests.CanonicalSaveService(f);
            service.ConfigureRunLoot(Loot()); service.ConfigureStructuralEconomy(f.Economy);
            service.ConfigureFloorConstruction(PhaseSixA3FloorActivationEligibilityTests.Profiles(f),
                PhaseSixA3FloorActivationEligibilityTests.Research(f));
            int publications = 0; service.CanonicalRuntimePublished += _ => publications++;
            var first = service.CommitFloorLifecycle(f.Runtime, FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(publications, Is.EqualTo(1));
            var activeSession = service.CanonicalSession;
            byte[] bytes = f.FileSystem.ReadAllBytes(f.ActivePath);
            int replaces = f.FileSystem.Operations.Count(o =>
                o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace);
            var repeated = service.CommitFloorLifecycle(first.RuntimeProjection, FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(repeated.IsSuccess, Is.True, repeated.Reason);
            Assert.That(repeated.IsNoOp, Is.True);
            Assert.That(service.CanonicalSession, Is.SameAs(activeSession));
            Assert.That(publications, Is.EqualTo(1));
            Assert.That(f.FileSystem.Operations.Count(o =>
                o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace), Is.EqualTo(replaces));
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void SnapshotCopiesEveryFloorAndConfiguration()
        {
            var f = Eligible();
            var snapshot = Writer(f).PreviewFloorLifecycle(f.Session, f.Runtime, FloorLifecycleAction.ActivateAllEligible, null).RunSnapshot;
            Assert.That(snapshot, Is.Not.Null);
            string before = JsonUtility.ToJson(snapshot.Floors[1].MaterializePlan());
            f.State.Floors[1].Layout.Rooms[0].RoomInstanceId = "changed";
            snapshot.Floors[1].MaterializePlan().RequiredRooms[0].Room.RoomInstanceId = "also.changed";
            snapshot.Configuration.PhaseSix.RewardWeight = 999;
            Assert.That(JsonUtility.ToJson(snapshot.Floors[1].MaterializePlan()), Is.EqualTo(before));
            Assert.That(snapshot.Configuration.PhaseSix.RewardWeight, Is.EqualTo(1));
            Assert.That(snapshot.Floors[0].MaterializePlan().RequiredRooms.All(r => r.Room.FloorIndex == 0), Is.True);
            Assert.That(snapshot.Floors[1].MaterializePlan().RequiredRooms.All(r => r.Room.FloorIndex == 1), Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void FirstCompletionExcludesPostTerminalRoom(bool beforeTerminal)
        {
            var f = Eligible(); var floor = f.State.Floors[1];
            var entrance = floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Entrance);
            var completion = floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Completion);
            var room = floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Room);
            floor.Layout.Edges = beforeTerminal ? new[] { Edge(entrance.NodeId, room.NodeId), Edge(room.NodeId, completion.NodeId), Edge(completion.NodeId, room.NodeId) }
                : new[] { Edge(entrance.NodeId, completion.NodeId), Edge(completion.NodeId, room.NodeId), Edge(room.NodeId, completion.NodeId) };
            var result = CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production);
            Assert.That(result.AuthorityState, Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
            Assert.That(result.Rooms.Length, Is.EqualTo(beforeTerminal ? 1 : 0));
        }
        private static FloorRouteEdge Edge(string source, string target) => new FloorRouteEdge { SourceNodeId = source, DestinationNodeId = target, Classification = RouteClassification.Required };

        [Test]
        public void ObjectiveWeightsAreNormalizedAndConfigOwned()
        {
            var c = Config().PhaseSix;
            var first = TransientDepthObjective.Select(c, "run.test").Mode;
            foreach (var w in c.Objectives) w.Weight *= 10;
            Assert.That(TransientDepthObjective.Select(c, "run.test").Mode, Is.EqualTo(first));
            foreach (var mode in c.Objectives.Select(x => x.Mode).ToArray())
            {
                foreach (var w in c.Objectives) w.Weight = w.Mode == mode ? 9 : 0;
                Assert.That(TransientDepthObjective.Select(c, "run.test").Mode, Is.EqualTo(mode));
            }
        }

        [Test]
        public void NoNextFloorExitsWithoutRollAndMarginalEqualityExits()
        {
            var c = Config(); var party = RunPartyGenerator.Create(c.PhaseFiveB, "run.test");
            var result = FloorTransitionDecision.Resolve(c.PhaseSix, party, "floor.one", null, 0, 0, FloorTransitionPerception.Unknown(c.PhaseSix));
            Assert.That(result.Descend, Is.False); Assert.That(result.Roll, Is.Null);
            Assert.That(FloorTransitionDecision.MarginalDescend(0.5, 0.5), Is.False);
        }

        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1)]
        [TestCase(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush, 1)]
        public void LifecyclePersistenceFaultPublishesNothing(Gd66DetachedSpatialMigrationTransactionTests.OperationType operation, int occurrence)
        {
            var f = Eligible(); byte[] before = f.Session.GetCurrentBytes(); var live = f.Runtime;
            f.FileSystem.EnableFailure(operation, occurrence);
            var result = Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(f.Runtime, Is.SameAs(live)); f.FileSystem.DisableFailure();
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void ReadbackFailureRollsBackAndStaleSessionDoesNotWrite()
        {
            var f = Eligible(); byte[] before = f.Session.GetCurrentBytes();
            f.FileSystem.EnableTargetedFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read,
                paths => paths.Length == 1 && paths[0] == f.ActivePath, 4, false);
            var result = Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.IsSuccess, Is.False); f.FileSystem.DisableFailure();
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.Seed(f.ActivePath, new byte[] { 7 });
            result = Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.Reason, Is.EqualTo("floor.lifecycle.stale_session"));
            CollectionAssert.AreEqual(new byte[] { 7 }, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void UnrunnableDamageProfileBlocksActivationWithoutPublication()
        {
            var f = Eligible(); AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            byte[] before = f.Session.GetCurrentBytes();
            f.Configuration.PhaseFiveB.DamageProfiles = f.Configuration.PhaseFiveB.DamageProfiles.Where(p => p.OptionId != MvpDungeonPlacementIds.SkeletonOptionId).ToArray();
            var result = Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f));
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        internal static void AddContent(Fixture f, int floorIndex, string category, string option)
        {
            var room = f.State.Floors.Single(x => x.FloorIndex == floorIndex).Layout.Rooms[0];
            f.Accept(Writer(f).Execute(f.ActivePath, f.FileSystem, f.Session, f.Runtime, DetachedCanonicalMutationRequest.Place(category, option, room.RoomInstanceId, f.State.Floors.Single(x => x.FloorIndex == floorIndex).FloorInstanceId)));
        }
        internal static LootConfig Loot() => JsonUtility.FromJson<LootConfig>(File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json"));
        internal static void ForceDescent(RunSimulationConfig c, bool descend)
        {
            c.BaseSuccessChance = 1; c.SuccessThreshold = 0; c.HeatPenaltyPerPoint = 0; c.ManaReserveBonusPerPoint = 0;
            c.PhaseSix.ExitThreshold = descend ? -1 : .9; c.PhaseSix.DescendThreshold = descend ? -.9 : 1;
            foreach (var p in c.PhaseSix.ProfileMinimums) p.MinimumSurvivability = 0;
        }
        internal static ActiveFloorRunSnapshot Snapshot(Fixture f, RunSimulationConfig config = null) =>
            ActiveFloorRunSnapshot.Create(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context),
                f.Production, config ?? f.Configuration, f.Profile.Canonical, Loot(), executionInputs: f.Runtime);

        [TestCase(false)] [TestCase(true)]
        public void RealTwoFloorRunUsesOnePartyAndPublishesOneSettlement(bool descend)
        {
            var f = Eligible(); AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            ForceDescent(f.Configuration, descend);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            int sequence = Math.Max(1, f.Runtime.runHistory.NextRunSequence);
            int history = f.Runtime.runHistory.RecentOutcomes?.Length ?? 0;
            int replaces = f.FileSystem.Operations.Count(o => o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace);
            var result = Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                new RunSimulationService(f.Configuration, Loot()), RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result);
            var run = f.Runtime.runHistory.LatestOutcome;
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(sequence + 1));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Length, Is.EqualTo(history + 1));
            Assert.That(f.FileSystem.Operations.Count(o => o.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace), Is.EqualTo(replaces + 1));
            Assert.That(run.RunId, Is.EqualTo(run.Party.RunId));
            Assert.That(run.FloorTransitions[0].Descend, Is.EqualTo(descend));
            Assert.That(run.RoomResolutions.Length, Is.EqualTo(descend ? 2 : 1));
            Assert.That(run.Success, Is.EqualTo(descend));
            Assert.That(run.RunHeatApplicationSummary.RuleResolved, Is.True);
            Assert.That(run.LootHeatCoolingSummary, Is.Not.Null);
            Assert.That(run.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(run.RoomResolutions.Sum(r => r.GeneratedLootValue)));
            if (descend)
            {
                var next = run.EncounterEvents.First(e => e.FloorIndex == 1);
                Assert.That(next.HealthBefore, Is.EqualTo(run.FloorTransitions[0].MemberHealth[next.MemberOrdinal]));
                Assert.That(next.MemberOrdinal, Is.EqualTo(run.FloorTransitions[0].FormationOrdinals[0]));
                Assert.That(run.RoomResolutions[1].CarriedLootValueAfterRoom, Is.GreaterThanOrEqualTo(run.RoomResolutions[0].CarriedLootValueAfterRoom));
            }
            else
            {
                Assert.That(run.ReasonKey, Is.EqualTo("run.reason.floor_exit"));
                Assert.That(run.FinalRouteOutcomeKey, Is.EqualTo("run.route.floor_exit"));
                Assert.That(run.FeedbackTagKeys, Does.Not.Contain("run.feedback.failure"));
            }
        }

        [Test]
        public void HiddenContentCannotChangeUnknownTransitionAndSnapshotSurvivesSourceMutation()
        {
            var f = Eligible(); AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            ForceDescent(f.Configuration, true);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            var snapshot = Snapshot(f); var engine = new RunSimulationService(f.Configuration, Loot());
            var first = engine.SimulateSnapshot(1, snapshot);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            var second = engine.SimulateSnapshot(1, Snapshot(f));
            Assert.That(second.FloorTransitions[0].Appeal, Is.EqualTo(first.FloorTransitions[0].Appeal));
            Assert.That(second.FloorTransitions[0].Roll, Is.EqualTo(first.FloorTransitions[0].Roll));
            Assert.That(second.FloorTransitions[0].Descend, Is.EqualTo(first.FloorTransitions[0].Descend));
            Assert.That(second.EncounterEvents.Length, Is.GreaterThan(first.EncounterEvents.Length));
            f.Configuration.BaseSuccessChance = 0; f.Runtime.structureRuntime.Heat = 900; f.Runtime.totalTicks += 500;
            var repeat = engine.SimulateSnapshot(1, snapshot);
            Assert.That(JsonUtility.ToJson(repeat), Is.EqualTo(JsonUtility.ToJson(first)));
            CollectionAssert.AreEqual(first.Party.Members.Select(m => m.CurrentHealth), repeat.Party.Members.Select(m => m.CurrentHealth));
        }

        [TestCase(0)] [TestCase(1)]
        public void WipeTerminatesWithoutDecisionAtThatFloor(int wipeFloor)
        {
            var f = Eligible();
            foreach (int floor in new[] { 0, 1 })
            {
                AddContent(f, floor, MvpDungeonPlacementIds.MonsterCategoryId, floor == wipeFloor ? MvpDungeonPlacementIds.SkeletonOptionId : MvpDungeonPlacementIds.GoblinOptionId);
                AddContent(f, floor, MvpDungeonPlacementIds.MonsterCategoryId, floor == wipeFloor ? MvpDungeonPlacementIds.SkeletonOptionId : MvpDungeonPlacementIds.GoblinOptionId);
                if (floor == wipeFloor) AddContent(f, floor, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            }
            ForceDescent(f.Configuration, true); f.Configuration.PhaseFiveB.MinPartySize = f.Configuration.PhaseFiveB.MaxPartySize = 3;
            foreach (var p in f.Configuration.PhaseFiveB.DamageProfiles)
                if (p.OptionId == MvpDungeonPlacementIds.SkeletonOptionId || p.OptionId == MvpDungeonPlacementIds.SpikeTrapOptionId)
                    p.MinimumDamage = p.MaximumDamage = 100;
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            var result = new RunSimulationService(f.Configuration, Loot()).SimulateSnapshot(1, Snapshot(f));
            Assert.That(result.Party.IsWiped, Is.True); Assert.That(result.FloorTransitions.Length, Is.EqualTo(wipeFloor));
            Assert.That(result.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteWipedKey));
            Assert.That(result.LootExtractionSummary.TotalExtractedWorldValue, Is.Zero);
        }

        [Test]
        public void ObjectivePullStrengthAndTargetDepthAreConfigOwned()
        {
            var c = Config().PhaseSix;
            var shallow = new TransientDepthObjective(c.Objectives.Single(x => x.Mode == "shallow"), c.ObjectiveRuleSourceId);
            var targetDefinition = c.Objectives.Single(x => x.Mode == "target_depth");
            var deepest = new TransientDepthObjective(c.Objectives.Single(x => x.Mode == "deepest_reasonable"), c.ObjectiveRuleSourceId);
            foreach (var definition in c.Objectives) definition.Weight = definition.Mode == "target_depth" ? 1 : 0;
            var target = TransientDepthObjective.Select(c, "run.config-owned-objective");

            Assert.That(shallow.Pull(0, true, 1), Is.Zero);
            Assert.That(target.Pull(0, true, 1), Is.EqualTo(.75));
            Assert.That(target.Pull(1, false, 1), Is.Zero);
            Assert.That(target.Pull(0, false, 1), Is.Zero);
            Assert.That(deepest.Pull(0, true, 1), Is.EqualTo(.5));
            Assert.That(deepest.Pull(1, false, 1), Is.Zero);

            targetDefinition.PullStrength = .25;
            targetDefinition.TargetFloorIndex = 2;
            Assert.That(PhaseSixRunConfigValidation.IsValid(c), Is.True);
            target = TransientDepthObjective.Select(c, "run.config-owned-objective");
            Assert.That(target.Pull(0, true, 1), Is.Zero, "The configured target is outside the Active snapshot.");
            Assert.That(target.Pull(0, true, 2), Is.EqualTo(.25));
            Assert.That(target.Pull(1, true, 2), Is.EqualTo(.25));
            Assert.That(target.Pull(2, false, 2), Is.Zero);
            Assert.That(target.Pull(1, false, 2), Is.Zero);
        }

        [Test]
        public void ObjectiveApplicabilityUsesImmutableSnapshotDepth()
        {
            var f = Eligible();
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.ActivateAllEligible, null));
            var snapshot = Snapshot(f);
            int deepest = snapshot.Floors.Last().FloorIndex;
            var c = Config().PhaseSix;
            var target = c.Objectives.Single(x => x.Mode == "target_depth");
            target.TargetFloorIndex = 2;
            foreach (var definition in c.Objectives) definition.Weight = definition.Mode == "target_depth" ? 1 : 0;
            var selected = TransientDepthObjective.Select(c, "run.snapshot-depth");
            f.State.Floors[1].ActivationState = FloorActivationState.Inactive;
            Assert.That(deepest, Is.EqualTo(1));
            Assert.That(selected.Pull(snapshot.Floors[0].FloorIndex, true, deepest), Is.Zero);
        }

        [TestCase(0)] [TestCase(1)]
        public void RetreatBeforeCompletionHasNoDecisionAtThatFloor(int retreatFloor)
        {
            var f = Eligible(); AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            ForceDescent(f.Configuration, true); f.Configuration.SuccessThreshold = .5;
            f.Configuration.MvpCompositionOutcomeTuning.SuccessChancePerPathCapacity = 0;
            f.Configuration.MvpCompositionOutcomeTuning.SuccessChancePenaltyPerDanger = .1;
            foreach (var e in f.Configuration.MvpPlacementEffects)
                e.Danger = e.OptionId == (retreatFloor == 0 ? MvpDungeonPlacementIds.GoblinOptionId : MvpDungeonPlacementIds.SkeletonOptionId) ? 10 : 0;
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            var run = new RunSimulationService(f.Configuration, Loot()).SimulateSnapshot(1, Snapshot(f));
            Assert.That(run.Party.IsWiped, Is.False); Assert.That(run.Success, Is.False);
            Assert.That(run.FloorTransitions.Length, Is.EqualTo(retreatFloor));
            Assert.That(run.RoomResolutions.Last().FloorIndex, Is.EqualTo(retreatFloor));
            Assert.That(run.RoomResolutions.Last().StopReasonKey, Is.EqualTo(RunSimulationService.RouteRetreatedKey));
        }

        [Test]
        public void CasualtiesStayDeadAndOriginalMemberTraitsAndFormationPersist()
        {
            var f = Eligible(); AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            ForceDescent(f.Configuration, true);
            var goblin = f.Configuration.PhaseFiveB.DamageProfiles.Single(p => p.OptionId == MvpDungeonPlacementIds.GoblinOptionId);
            goblin.MinimumDamage = goblin.MaximumDamage = 100;
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            var expected = RunPartyGenerator.Create(f.Configuration.PhaseFiveB, "run-1");
            var run = new RunSimulationService(f.Configuration, Loot()).SimulateSnapshot(1, Snapshot(f));
            var transfer = run.FloorTransitions[0];
            Assert.That(transfer.Descend, Is.True);
            Assert.That(transfer.MemberHealth.Count(h => h == 0), Is.EqualTo(1));
            var dead = Array.FindIndex(transfer.MemberHealth, h => h == 0);
            Assert.That(run.Party.Members[dead].CurrentHealth, Is.Zero);
            foreach (var member in run.Party.Members)
            {
                var original = expected.Members[member.MemberOrdinal];
                Assert.That(member.ClassId, Is.EqualTo(original.ClassId)); Assert.That(member.Level, Is.EqualTo(original.Level));
                Assert.That(member.MaxHealth, Is.EqualTo(original.MaxHealth)); Assert.That(member.TrapExpertise, Is.EqualTo(original.TrapExpertise));
                Assert.That(member.BehaviorProfileId, Is.EqualTo(original.BehaviorProfileId));
                Assert.That(member.CurrentHealth, Is.LessThanOrEqualTo(transfer.MemberHealth[member.MemberOrdinal]));
            }
            Assert.That(run.Party.Members.Count, Is.EqualTo(expected.Members.Count));
            Assert.That(run.Party.IntelligenceId, Is.EqualTo(expected.IntelligenceId));
            Assert.That(run.Party.RewardAppetite, Is.EqualTo(expected.RewardAppetite));
            var encounter = run.EncounterEvents.First(e => e.FloorIndex == 1);
            Assert.That(encounter.MemberOrdinal, Is.EqualTo(transfer.FormationOrdinals[0]));
            Assert.That(encounter.HealthBefore, Is.EqualTo(transfer.MemberHealth[encounter.MemberOrdinal]));
        }

        [Test]
        public void BranchPlansAreFloorLocalAndFloorTwoDangerCannotChangeFloorOneDecision()
        {
            var f = Eligible(); f.Runtime.completedResearch.ProjectIds = new[] { "ac_100", "ac_300" };
            var first = f.State.Floors.Single(x => x.FloorIndex == 0);
            var room = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                FloorInstanceId = first.FloorInstanceId, RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(room.IsValid, Is.True, string.Join(",", room.ReasonCodes)); f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(room)));
            foreach (int index in new[] { 0, 1 })
            {
                var floor = f.State.Floors.Single(x => x.FloorIndex == index);
                var origin = CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production).Rooms[0];
                var node = floor.Layout.Nodes.Single(n => n.RoomInstanceId == origin.RoomInstanceId);
                var branch = OptionalBranchStructuralEditService.PreviewConstruction(f.State, new OptionalBranchConstructionRequest {
                    FloorInstanceId = floor.FloorInstanceId, OriginNodeId = node.NodeId,
                    OriginConnectionPointId = index == 0 ? "east" : "north", CorridorLength = 2 },
                    f.Runtime.completedResearch, f.BranchingResearch, f.Production, f.Configuration, f.Profile.Canonical);
                Assert.That(branch.IsSpatiallyValid, Is.True, string.Join(",", branch.ReasonCodes));
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.ConstructBranch(branch)));
            }
            ForceDescent(f.Configuration, true);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.Activate, FloorTwo(f)));
            var before = Snapshot(f); var floorOne = before.Floors[0].MaterializePlan().Forks.Single();
            Assert.That(floorOne.RequiredSuffix.Length, Is.EqualTo(1));
            Assert.That(floorOne.RequiredSuffix.All(r => r.Room.FloorIndex == 0), Is.True);
            Assert.That(before.Floors[1].MaterializePlan().Forks.Single().RequiredSuffix, Is.Empty);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            var after = Snapshot(f); var newFloorOne = after.Floors[0].MaterializePlan().Forks.Single();
            Assert.That(newFloorOne.RemainingRequiredDanger, Is.EqualTo(floorOne.RemainingRequiredDanger));
            Assert.That(newFloorOne.Fingerprint, Is.EqualTo(floorOne.Fingerprint));
            var engine = new RunSimulationService(f.Configuration, Loot());
            var a = engine.SimulateSnapshot(1, before).BranchOutcomes.First().Decision;
            var b = engine.SimulateSnapshot(1, after).BranchOutcomes.First().Decision;
            Assert.That(b.Q, Is.EqualTo(a.Q)); Assert.That(b.Enter, Is.EqualTo(a.Enter)); Assert.That(b.DecisionRoll, Is.EqualTo(a.DecisionRoll));
        }

        [Test]
        public void OneFloorSnapshotMatchesExistingPhaseFiveBExecution()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture();
            var plan = PhaseFiveBRouteProjection.Resolve(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context), f.Runtime, f.Production, f.Configuration);
            var engine = new RunSimulationService(f.Configuration, Loot());
            var expected = engine.SimulateRoute(JsonUtility.FromJson<Gameplay.Structures.StructureRuntimeState>(JsonUtility.ToJson(f.Runtime.structureRuntime)),
                f.Runtime.totalTicks, 1, RunPostureResolver.BalancedId, plan.RequiredRooms.Select(r => r.Room).ToArray(),
                new RunSimulationService.BranchTraversal { Forks = plan.Forks, Workload = new BranchRunWorkload(f.Configuration.PhaseFiveB.BranchDecision) });
            var actual = engine.SimulateSnapshot(1, Snapshot(f));
            Assert.That(JsonUtility.ToJson(actual), Is.EqualTo(JsonUtility.ToJson(expected)));
            CollectionAssert.AreEqual(actual.Party.Members.Select(m => m.CurrentHealth), expected.Party.Members.Select(m => m.CurrentHealth));
        }

        [Test]
        public void SyntheticDeeperCascadeAndManualActivationPreserveIdentityAndContents()
        {
            var state = new DetachedCanonicalSpatialSaveState { Floors = Enumerable.Range(0, 5).Select(i => new SavedSpatialFloor {
                FloorIndex = i, FloorInstanceId = "test.floor." + i, ActivationState = FloorActivationState.Active,
                RoomContents = new FloorRoomContentState() }).ToArray() };
            var contents = state.Floors.Select(f => f.RoomContents).ToArray();
            DetachedCanonicalWriteAuthority.ApplyDetachedActivation(state, "test.floor.1", false);
            CollectionAssert.AreEqual(new[] { true, false, false, false, false }, state.Floors.Select(f => f.ActivationState == FloorActivationState.Active));
            DetachedCanonicalWriteAuthority.ApplyDetachedActivation(state, "test.floor.1", true);
            CollectionAssert.AreEqual(new[] { true, true, false, false, false }, state.Floors.Select(f => f.ActivationState == FloorActivationState.Active));
            for (int i = 0; i < 5; i++) { Assert.That(state.Floors[i].FloorInstanceId, Is.EqualTo("test.floor." + i)); Assert.That(state.Floors[i].RoomContents, Is.SameAs(contents[i])); }
        }

        [Test]
        public void ActivateAllStopsAtPermissionBlockerAndInactiveSnapshotContainsOnlyFloorOne()
        {
            var f = Eligible(); Assert.That(Snapshot(f).Floors.Count, Is.EqualTo(1));
            f.Runtime.completedResearch.ProjectIds = Array.Empty<string>(); byte[] before = f.Session.GetCurrentBytes();
            var result = Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(result.Reason, Is.EqualTo(FloorActivationEligibilityReasons.ResearchRequired));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void ReopenFailureStillHasRollbackEvidenceAndRestoresExactBytes()
        {
            var f = Eligible(); var original = f.Session.GetCurrentBytes(); var candidate = original.Concat(new byte[] { 32 }).ToArray();
            int reopenCalls = 0;
            string reason = ExactCompleteSaveAtomicPersistence.Persist(f.ActivePath, f.FileSystem, original, candidate, 100, bytes => {
                reopenCalls++; Assert.That(f.FileSystem.Paths.Any(p => p.EndsWith(".rollback", StringComparison.Ordinal)), Is.True);
                return false; // Injected reopen failure after exact durable candidate readback.
            });
            Assert.That(reopenCalls, Is.EqualTo(1)); Assert.That(reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(original, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [TestCase("run-1", "canonical.floor.00", "canonical.floor.01", 4269552003d)]
        [TestCase("run-42", "floor.é", "floor.二", 2018946062d)]
        public void IndependentSha256VectorsMatch(string run, string current, string next, double word)
        {
            // Expected unsigned words independently calculated with Python hashlib/struct.
            Assert.That(PhaseSixIdentity.Roll("run.floor_transition_decision.phase6.v1", run, current, next), Is.EqualTo(word / 4294967296d));
            Assert.That(PhaseSixIdentity.Roll("run.depth_objective.phase6.v1", "run-1"), Is.EqualTo(2903264033d / 4294967296d));
        }

        [Test]
        public void TransitionObjectiveAndBranchHashesHaveNoCallOrderState()
        {
            var c = Config(); string rule = c.PhaseSix.TransitionRuleSourceId;
            double expected = PhaseSixIdentity.Roll(rule, "run-1", "floor.0", "floor.1");
            var objective = TransientDepthObjective.Select(c.PhaseSix, "run-1").Mode;
            double branch = BranchDecisionResolver.Roll(c.PhaseFiveB.BranchDecision.RuleSourceId, "run-1", "floor.0", "branch.1");
            for (int i = 0; i < 20; i++)
            {
                BranchDecisionResolver.Roll(c.PhaseFiveB.BranchDecision.RuleSourceId, "run-" + i, "floor.0", "branch.1");
                TransientDepthObjective.Select(c.PhaseSix, "run-" + i);
                PhaseSixIdentity.Roll(rule, "run-" + i, "floor.0", "floor.1");
            }
            Assert.That(PhaseSixIdentity.Roll(rule, "run-1", "floor.0", "floor.1"), Is.EqualTo(expected));
            Assert.That(TransientDepthObjective.Select(c.PhaseSix, "run-1").Mode, Is.EqualTo(objective));
            Assert.That(BranchDecisionResolver.Roll(c.PhaseFiveB.BranchDecision.RuleSourceId, "run-1", "floor.0", "branch.1"), Is.EqualTo(branch));
            Assert.That(branch, Is.Not.EqualTo(expected));
        }

        [Test]
        public void SurvivabilityRefusalPrecedesRewardAndEqualityProceedsToAppeal()
        {
            var c = Config(); var party = RunPartyGenerator.Create(c.PhaseFiveB, "run-1");
            var perception = new FloorTransitionPerception(true, 12, true, 9, 1);
            foreach (var p in c.PhaseSix.ProfileMinimums) p.MinimumSurvivability = .99;
            var refused = FloorTransitionDecision.Resolve(c.PhaseSix, party, "floor.0", "floor.1", 0, 1, perception);
            Assert.That(refused.Reason, Is.EqualTo("run.floor.exit_survivability")); Assert.That(refused.Roll, Is.Null);
            foreach (var p in c.PhaseSix.ProfileMinimums) p.MinimumSurvivability = refused.ExpectedSurvivability;
            var equal = FloorTransitionDecision.Resolve(c.PhaseSix, party, "floor.0", "floor.1", 0, 1, perception);
            Assert.That(equal.ExpectedSurvivability, Is.EqualTo(equal.PartyMinimum));
            Assert.That(equal.Reason, Is.Not.EqualTo("run.floor.exit_survivability"));
        }

        [TestCase(false)] [TestCase(true)]
        public void AppealThresholdEqualityIsDeterministic(bool descend)
        {
            var c = Config(); var party = RunPartyGenerator.Create(c.PhaseFiveB, "run-1");
            var perception = FloorTransitionPerception.Unknown(c.PhaseSix);
            var first = FloorTransitionDecision.Resolve(c.PhaseSix, party, "floor.0", "floor.1", 0, 1, perception);
            c.PhaseSix.ExitThreshold = descend ? -1 : first.Appeal;
            c.PhaseSix.DescendThreshold = descend ? first.Appeal : 1;
            var equal = FloorTransitionDecision.Resolve(c.PhaseSix, party, "floor.0", "floor.1", 0, 1, perception);
            Assert.That(equal.Descend, Is.EqualTo(descend)); Assert.That(equal.Roll, Is.Null);
        }

        [TestCase("duplicate_objective")] [TestCase("negative_weight")] [TestCase("zero_objectives")]
        [TestCase("nan")] [TestCase("infinite")] [TestCase("zero_appeal")] [TestCase("minimum_one")]
        [TestCase("duplicate_minimum")] [TestCase("unknown_mode")] [TestCase("bad_thresholds")]
        [TestCase("zero_reference")] [TestCase("workload")] [TestCase("too_many_floors")]
        [TestCase("too_many_transitions")] [TestCase("objective_source")] [TestCase("transition_source")]
        [TestCase("missing_target")] [TestCase("invalid_target")] [TestCase("nan_pull")]
        [TestCase("negative_pull")] [TestCase("large_pull")] [TestCase("shallow_pull")]
        public void InvalidTransitionConfigurationFailsClosed(string defect)
        {
            var c = Config().PhaseSix;
            switch (defect)
            {
                case "duplicate_objective": c.Objectives[1].Mode = c.Objectives[0].Mode; break;
                case "negative_weight": c.Objectives[0].Weight = -1; break;
                case "zero_objectives": foreach (var x in c.Objectives) x.Weight = 0; break;
                case "nan": c.RewardWeight = double.NaN; break;
                case "infinite": c.DangerReference = double.PositiveInfinity; break;
                case "zero_appeal": c.RewardWeight = c.SurvivabilityMarginWeight = c.ObjectiveWeight = c.DangerAppealWeight = c.UncertaintyAppealWeight = c.CarriedLootPreservationWeight = 0; break;
                case "minimum_one": c.ProfileMinimums[0].MinimumSurvivability = 1; break;
                case "duplicate_minimum": c.ProfileMinimums[1].ProfileId = c.ProfileMinimums[0].ProfileId; break;
                case "unknown_mode": c.Objectives[0].Mode = "unknown"; break;
                case "bad_thresholds": c.ExitThreshold = c.DescendThreshold; break;
                case "zero_reference": c.CarriedLootReference = 0; break;
                case "workload": c.MaximumTransitions = 0; break;
                case "too_many_floors": c.MaximumActiveFloors = 6; break;
                case "too_many_transitions": c.MaximumTransitions = 5; break;
                case "objective_source": c.ObjectiveRuleSourceId = "run.depth_objective.unsupported"; break;
                case "transition_source": c.TransitionRuleSourceId = "run.floor_transition_decision.unsupported"; break;
                case "missing_target": c.Objectives.Single(x => x.Mode == "target_depth").TargetFloorIndex = -1; break;
                case "invalid_target": c.Objectives.Single(x => x.Mode == "target_depth").TargetFloorIndex = c.MaximumActiveFloors; break;
                case "nan_pull": c.Objectives[0].PullStrength = double.NaN; break;
                case "negative_pull": c.Objectives[1].PullStrength = -.01; break;
                case "large_pull": c.Objectives[2].PullStrength = 1.01; break;
                case "shallow_pull": c.Objectives.Single(x => x.Mode == "shallow").PullStrength = .01; break;
            }
            Assert.That(PhaseSixRunConfigValidation.IsValid(c), Is.False);
            Assert.Throws<ArgumentException>(() => TransientDepthObjective.Select(c, "run-1"));
        }

        [Test]
        public void RunnableProjectionIgnoresInsertionOrderAndRejectsAmbiguityAndCrossFloorTargets()
        {
            var f = Eligible(); var floor = f.State.Floors[1];
            var expected = CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production);
            floor.Layout.Nodes = floor.Layout.Nodes.Reverse().ToArray(); floor.Layout.Edges = floor.Layout.Edges.Reverse().ToArray();
            var actual = CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production);
            CollectionAssert.AreEqual(expected.Rooms.Select(r => r.RoomInstanceId), actual.Rooms.Select(r => r.RoomInstanceId));
            var edge = floor.Layout.Edges.First(e => e.SourceNodeId == floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Entrance).NodeId);
            floor.Layout.Edges = floor.Layout.Edges.Concat(new[] { Edge(edge.SourceNodeId, edge.DestinationNodeId) }).ToArray();
            Assert.That(CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production).AuthorityState, Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
            floor.Layout.Edges = floor.Layout.Edges.Take(floor.Layout.Edges.Length - 1).ToArray();
            edge.DestinationNodeId = f.State.Floors[0].Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Entrance).NodeId;
            Assert.That(CanonicalRunnableFloorProjection.Resolve(floor, f.Configuration, f.Production).AuthorityState, Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ContradictoryCanonical));
        }

        [Test]
        public void TwoActiveFloorsSupportOfflinePublicationBeforeRunServiceStarts()
        {
            var f = Eligible();
            var service = PhaseFiveBBranchIntegrationTests.CanonicalSaveService(f);
            service.ConfigureRunLoot(Loot());
            service.ConfigureStructuralEconomy(f.Economy);
            service.ConfigureFloorConstruction(PhaseSixA3FloorActivationEligibilityTests.Profiles(f), PhaseSixA3FloorActivationEligibilityTests.Research(f));
            SaveData published = null; service.CanonicalRuntimePublished += value => published = value;
            var activation = service.CommitFloorLifecycle(f.Runtime, FloorLifecycleAction.ActivateAllEligible, null);
            Assert.That(activation.IsSuccess, Is.True, activation.Reason); f.Accept(activation);
            Assert.That(published, Is.SameAs(activation.RuntimeProjection));
            var config = PhaseFourTestSupport.PassiveMana(f.Profile.Canonical);
            var online = new CanonicalPassiveManaService(config, f.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, 1);
            var offline = new CanonicalOfflinePassiveManaService(config, online);
            f.Runtime.lastSavedUtcUnix = 1000; f.Runtime.structureRuntime.ManaReserve = 0;
            var calculated = offline.Resolve(f.Runtime, f.Configuration, 4600);
            Assert.That(calculated.CanonicalRate.ActiveFloorCount, Is.EqualTo(2));
            Assert.That(calculated.ApplicableOnlineManaPerHour, Is.EqualTo(online.ResolveRate(f.Runtime, f.Configuration).ManaPerHour));
            var committed = service.CommitOfflinePassiveMana(f.Runtime, calculated);
            Assert.That(committed.Persisted, Is.True, committed.Reason.ToString());
            Assert.That(published.structureRuntime.ManaReserve, Is.EqualTo(calculated.WalletAfter));
            var deactivation = service.CommitFloorLifecycle(published, FloorLifecycleAction.Deactivate, FloorTwo(f));
            Assert.That(deactivation.IsSuccess, Is.True, deactivation.Reason);
            Assert.That(online.ResolveRate(published, f.Configuration).ActiveFloorCount, Is.EqualTo(1));
            Assert.That(published.structureRuntime.ManaReserve, Is.EqualTo(calculated.WalletAfter));
        }

        [Test]
        public void SameSessionOfflinePublicationRetainsAllTransientRunEvidenceButReopenDoesNotFabricateIt()
        {
            var f = Eligible();
            AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            ForceDescent(f.Configuration, true);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, FloorTwo(f)));
            f.Accept(Writer(f).CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                new RunSimulationService(f.Configuration, Loot()), RunPostureResolver.BalancedId,
                Math.Max(1, f.Runtime.lastSavedUtcUnix)));
            var before = f.Runtime.runHistory.LatestOutcome;
            Assert.That(before.Party, Is.Not.Null); Assert.That(before.EncounterEvents, Is.Not.Empty);
            Assert.That(before.BranchOutcomes, Is.Not.Null); Assert.That(before.FloorTransitions, Is.Not.Empty);
            Assert.That(before.DepthObjective, Is.Not.Null);
            string serialized = JsonUtility.ToJson(f.Runtime);
            Assert.That(serialized, Does.Not.Contain("\"Party\""));
            Assert.That(serialized, Does.Not.Contain("\"EncounterEvents\""));
            Assert.That(serialized, Does.Not.Contain("\"BranchOutcomes\""));
            Assert.That(serialized, Does.Not.Contain("\"FloorTransitions\""));
            Assert.That(serialized, Does.Not.Contain("\"DepthObjective\""));

            var service = PhaseFiveBBranchIntegrationTests.CanonicalSaveService(f);
            service.ConfigureRunLoot(Loot()); service.ConfigureStructuralEconomy(f.Economy);
            service.ConfigureFloorConstruction(PhaseSixA3FloorActivationEligibilityTests.Profiles(f),
                PhaseSixA3FloorActivationEligibilityTests.Research(f));
            var go = new GameObject("PhaseSixA4TransientEvidenceRetention");
            try
            {
                var root = go.AddComponent<GameRoot>();
                typeof(GameRoot).GetProperty("Save").SetValue(root, f.Runtime);
                root.AttachSaveServiceForTests(service);
                var passive = PhaseFourTestSupport.PassiveMana(f.Profile.Canonical);
                var online = new CanonicalPassiveManaService(passive, f.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, 1);
                var calculated = new CanonicalOfflinePassiveManaService(passive, online).Resolve(f.Runtime, f.Configuration,
                    Math.Max(1, f.Runtime.lastSavedUtcUnix) + 3600);
                var committed = service.CommitOfflinePassiveMana(f.Runtime, calculated);
                Assert.That(committed.Persisted, Is.True, committed.Reason.ToString());
                var after = root.Save.runHistory.LatestOutcome;
                Assert.That(after.Party, Is.SameAs(before.Party));
                Assert.That(after.EncounterEvents, Is.SameAs(before.EncounterEvents));
                Assert.That(after.BranchOutcomes, Is.SameAs(before.BranchOutcomes));
                Assert.That(after.FloorTransitions, Is.SameAs(before.FloorTransitions));
                Assert.That(after.DepthObjective, Is.SameAs(before.DepthObjective));

                var reopened = DetachedCanonicalSaveSession.Open(f.FileSystem.ReadAllBytes(f.ActivePath), f.Context, f.Profile);
                Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
                var validation = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                    f.FileSystem.ReadAllBytes(f.ActivePath), f.Context);
                Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(validation, f.Production,
                    out SaveData durableSave, out string reason), Is.True, reason);
                var durable = durableSave.runHistory.LatestOutcome ?? durableSave.runHistory.RecentOutcomes.Last();
                Assert.That(durable.Party, Is.Null); Assert.That(durable.EncounterEvents, Is.Null);
                Assert.That(durable.BranchOutcomes, Is.Null); Assert.That(durable.FloorTransitions, Is.Null);
                Assert.That(durable.DepthObjective, Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void EmptyRequiredRoomsStillReachFinalCompletionWithoutInventingEncounters()
        {
            var f = Eligible();
            foreach (int floorIndex in new[] { 0, 1 })
            {
                var floor = f.State.Floors.Single(x => x.FloorIndex == floorIndex);
                var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                    FloorInstanceId = floor.FloorInstanceId, RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(floorIndex == 0 ? 0 : 1, floorIndex == 0 ? 6 : 8),
                    Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            }
            ForceDescent(f.Configuration, true);
            f.Accept(Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime, FloorLifecycleAction.ActivateAllEligible, null));
            var result = new RunSimulationService(f.Configuration, Loot()).SimulateSnapshot(1, Snapshot(f));
            Assert.That(result.Success, Is.True); Assert.That(result.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteClearedKey));
            Assert.That(result.RoomResolutions.Length, Is.EqualTo(4)); Assert.That(result.EncounterEvents, Is.Empty);
            Assert.That(result.Party.Members.All(m => m.CurrentHealth == m.MaxHealth), Is.True);
        }
    }
}
#endif
