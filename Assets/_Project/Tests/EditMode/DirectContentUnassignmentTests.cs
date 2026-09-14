#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class DirectContentUnassignmentTests
    {
        private static Fixture Active(string option = MvpDungeonPlacementIds.GoblinOptionId,
            SaveSpatialMigrationLimitsProfile profile = null)
        {
            var f = Fixture.Create(null, workloadProfile: profile);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            Assert.That(MvpDungeonPlacementIds.TryGetCategoryForOption(option, out string category), Is.True);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(category, option, Room(f))));
            return f;
        }

        private static string Room(Fixture f) => f.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
        private static RoomContentAssignment Item(Fixture f) => f.State.Floors[0].RoomContents.Assignments[0];
        private static DetachedCanonicalMutationResult Prepare(Fixture f, string id) =>
            DetachedCanonicalSpatialMutation.Prepare(f.State, DetachedCanonicalMutationRequest.Unassign(id),
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical, f.RemovalPolicy);
        private static string Json(object value) => JsonUtility.ToJson(value);
        private static string Investment(Fixture f) => string.Join("\n",
            DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context)
                .Investment.Select(Json));

        private static void AssertRejected(Fixture f, string id, string reason)
        {
            byte[] bytes = f.Session.GetCurrentBytes(); string state = Json(f.State), runtime = Json(f.Runtime);
            var session = f.Session;
            var result = f.Execute(DetachedCanonicalMutationRequest.Unassign(id));
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.IsNoOp, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.RuntimeProjection, Is.Null); Assert.That(result.Session, Is.Null);
            Assert.That(f.Session, Is.SameAs(session)); Assert.That(Json(f.State), Is.EqualTo(state));
            Assert.That(Json(f.Runtime), Is.EqualTo(runtime));
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [TestCase(MvpDungeonPlacementIds.GoblinOptionId)]
        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId)]
        [TestCase(MvpDungeonPlacementIds.SpikeTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.SnareTrapOptionId)]
        [TestCase(MvpDungeonPlacementIds.ChillingSigilOptionId)]
        [TestCase(MvpDungeonPlacementIds.BasicLootNodeOptionId)]
        [TestCase(MvpDungeonPlacementIds.HiddenCacheOptionId)]
        [TestCase(MvpDungeonPlacementIds.GlitteringHoardOptionId)]
        public void EveryAuthoredReusableOptionMovesExactIdentityWithoutOtherStateChanges(string option)
        {
            var f = Active(option); var item = Item(f);
            f.Runtime.structureRuntime.ManaReserve = 0; // No purchase balance is required for an ownership move.
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            string stateBefore = Json(f.State), runtimeBefore = Json(f.Runtime), ledger = Investment(f);
            long next = f.State.Floors[0].RoomContents.NextSequence;
            var request = DetachedCanonicalMutationRequest.Unassign(item.AssignmentId);
            Assert.That(request.CategoryId, Is.Null); Assert.That(request.OptionId, Is.Null);
            Assert.That(request.RoomInstanceId, Is.Null);
            var pure = Prepare(f, item.AssignmentId);
            Assert.That(pure.IsSuccess, Is.True, pure.Reason); Assert.That(pure.ApplyExplicitRoomEffect, Is.False);
            Assert.That(Json(f.State), Is.EqualTo(stateBefore));
            CollectionAssert.AreEqual(CanonicalSpatialSaveSerializer.Serialize(pure.State, f.Profile.Canonical).Value,
                CanonicalSpatialSaveSerializer.Serialize(Prepare(f, item.AssignmentId).State, f.Profile.Canonical).Value);
            f.Accept(f.Execute(request)); f.Reopen();
            var returned = f.State.LifecycleAndOwnership.ReturnedContents.Single();
            Assert.That(returned.AssignmentId, Is.EqualTo(item.AssignmentId));
            Assert.That(returned.CategoryId, Is.EqualTo(item.CategoryId));
            Assert.That(returned.OptionId, Is.EqualTo(item.OptionId));
            Assert.That(returned.Sequence, Is.EqualTo(item.Sequence));
            Assert.That(returned.RemovalDisposition, Is.EqualTo(StructuralContentRemovalDisposition.ReturnToPlayerCustody));
            Assert.That(f.State.Floors[0].RoomContents.Assignments, Is.Empty);
            Assert.That(f.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(next));
            Assert.That(Investment(f), Is.EqualTo(ledger));
            // Compare the entire spatial state after reversing only the two expected collection edits.
            var restored = CanonicalSpatialSaveSerializer.Parse(
                CanonicalSpatialSaveSerializer.Serialize(f.State, f.Profile.Canonical).Value, f.Profile.Canonical).Value;
            restored.Floors[0].RoomContents.Assignments = new[] { item };
            restored.LifecycleAndOwnership.ReturnedContents = Array.Empty<ReturnedStructuralContent>();
            Assert.That(Json(restored), Is.EqualTo(stateBefore));
            // Recognized runtime fields include mana, run history and resolved loot; projections are compared separately.
            var beforeRuntime = JsonUtility.FromJson<SaveData>(runtimeBefore);
            var beforeSnapshot = DetachedRecognizedSaveStateSnapshot.Capture(beforeRuntime, f.Profile);
            var afterSnapshot = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            Assert.That(beforeSnapshot.IsSuccess && afterSnapshot.IsSuccess, Is.True);
            foreach (var field in typeof(SaveData).GetFields().Where(field =>
                DetachedCanonicalSaveSession.IsLiveRecognizedMember(field.Name)))
            {
                Assert.That(beforeSnapshot.Snapshot.TryGet(field.Name, out var before), Is.True);
                Assert.That(afterSnapshot.Snapshot.TryGet(field.Name, out var after), Is.True);
                CollectionAssert.AreEqual(before, after, field.Name);
            }
            Assert.That(Json(f.Runtime.structureRuntime), Is.EqualTo(Json(beforeRuntime.structureRuntime)));
            Assert.That(Json(f.Runtime.runHistory), Is.EqualTo(Json(beforeRuntime.runHistory)));
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(9));
            Assert.That(Encoding.UTF8.GetString(f.Session.GetCurrentBytes()), Does.Contain("\"schemaVersion\":9"));
            AssertRejected(f, item.AssignmentId, DetachedCanonicalSpatialMutation.ActiveAssignmentMissingReason);
        }

        [Test]
        public void DuplicateGoblinSelectionMovesOnlyTargetAndKeepsCanonicalOrder()
        {
            var f = Active();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId, Room(f))));
            var other = Item(f); var target = f.State.Floors[0].RoomContents.Assignments[1];
            long next = f.State.Floors[0].RoomContents.NextSequence;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(target.AssignmentId)));
            Assert.That(Json(Item(f)), Is.EqualTo(Json(other)));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId, Is.EqualTo(target.AssignmentId));
            Assert.That(f.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(next));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(other.AssignmentId)));
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
            CollectionAssert.AreEqual(new[] { other.AssignmentId, target.AssignmentId },
                f.State.LifecycleAndOwnership.ReturnedContents.Select(value => value.AssignmentId));
        }

        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase("invalid/id")] [TestCase("missing.assignment")]
        public void InvalidOrMissingTargetFailsWithoutChanges(string id) =>
            AssertRejected(Active(), id, DetachedCanonicalSpatialMutation.ActiveAssignmentMissingReason);

        [TestCase("missing")] [TestCase("missing-record")] [TestCase("unresolved")] [TestCase("destructive")]
        public void PolicyMustExplicitlyPermitReturn(string policy)
        {
            var f = Active();
            if (policy == "missing") f.RemovalPolicy = null;
            else if (policy == "missing-record") Assert.That(StructuralContentRemovalPolicyAuthority.TryParse(
                Encoding.UTF8.GetBytes(JsonUtility.ToJson(new StructuralContentRemovalPolicyConfiguration {
                    Schema = "structural_content_removal_policy", SchemaVersion = 1 }, true) + "\n"),
                out f.RemovalPolicy), Is.True);
            else Assert.That(StructuralContentRemovalPolicyAuthority.TryParse(Encoding.UTF8.GetBytes(
                File.ReadAllText(StructuralContentRemovalPolicyAuthority.ProductionPath)
                    .Replace("\"Policy\": 1", policy == "unresolved" ? "\"Policy\": 0" : "\"Policy\": 2")),
                out f.RemovalPolicy), Is.True);
            AssertRejected(f, Item(f).AssignmentId, policy == "destructive"
                ? DetachedCanonicalSpatialMutation.ReturnNotPermittedReason
                : StructuralContentRemovalPolicyAuthority.MissingOrUnresolvedReason);
        }

        [TestCase("duplicate")] [TestCase("assigned-and-returned")]
        [TestCase("sequence")] [TestCase("option")] [TestCase("geometry")]
        public void CorruptSourceCannotBeRepairedByUnassignment(string corruption)
        {
            var f = Active(); var item = Item(f);
            if (corruption == "duplicate") f.State.Floors[0].RoomContents.Assignments = new[] { item, item };
            if (corruption == "assigned-and-returned") f.State.LifecycleAndOwnership.ReturnedContents = new[] {
                new ReturnedStructuralContent { AssignmentId = item.AssignmentId, CategoryId = item.CategoryId,
                    OptionId = item.OptionId, Sequence = item.Sequence,
                    RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody } };
            if (corruption == "sequence") f.State.Floors[0].RoomContents.NextSequence = -1;
            if (corruption == "option") item.OptionId = "test.invalid.option";
            if (corruption == "geometry") f.State.Floors[0].Layout.Rooms[0].Anchor = new TileCoordinate(-999, -999);
            string before = Json(f.State);
            var result = Prepare(f, item.AssignmentId);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            Assert.That(Json(f.State), Is.EqualTo(before));
            AssertRejected(f, item.AssignmentId, DetachedCanonicalSpatialMutation.ValidationFailedReason);
        }

        [Test]
        public void PersistenceFailureLeavesActiveOwnershipDiskSessionAndRuntimeUntouched()
        {
            var f = Active(); string id = Item(f).AssignmentId;
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            AssertRejected(f, id, DetachedCanonicalWriteAuthority.AtomicSaveFailedReason);
            f.FileSystem.DisableFailure();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(id))); f.Reopen();
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId, Is.EqualTo(id));
        }

        [Test]
        public void StaleSessionCannotReplayEvenIdenticalCandidateOrReturnRedeployedIdentity()
        {
            var f = Active(); string id = Item(f).AssignmentId;
            string source = Room(f);
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 7),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            string destination = f.State.Floors[0].Layout.Rooms.Single(r => r.RoomInstanceId != source).RoomInstanceId;
            var oldSession = f.Session; var oldRuntime = f.Runtime; var oldState = f.State;
            var request = DetachedCanonicalMutationRequest.Unassign(id);
            f.Accept(f.Execute(request));
            for (int step = 0; step < 2; step++)
            {
                byte[] bytes = f.Session.GetCurrentBytes();
                var stale = f.Authority.Execute(f.ActivePath, f.FileSystem, oldSession, oldState, oldRuntime, request);
                Assert.That(stale.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
                Assert.That(stale.RuntimeProjection, Is.Null); Assert.That(stale.Session, Is.Null);
                CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
                if (step == 0) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(id, destination)));
            }
        }

        [Test]
        public void ProductionCustodyBoundaryAcceptsLastRecordAndRejectsOneOverAtomically()
        {
            var loaded = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath));
            Assert.That(loaded.IsSuccess, Is.True, loaded.Reason);
            var f = Active(profile: loaded.Profile); var item = Item(f);
            // Test-only owned records isolate the actual production collection boundary.
            f.State.LifecycleAndOwnership.ReturnedContents = Enumerable.Range(0, loaded.Profile.Raw.MaximumArrayElements - 1)
                .Select(i => new ReturnedStructuralContent { AssignmentId = "test.custody." + i,
                    CategoryId = item.CategoryId, OptionId = item.OptionId, Sequence = i,
                    RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody }).ToArray();
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(f.State, f.Profile.Canonical.Spatial,
                out var canonical), Is.True);
            f = f.Rebase(canonical);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(item.AssignmentId))); f.Reopen();
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(loaded.Profile.Raw.MaximumArrayElements));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(item.CategoryId, item.OptionId, Room(f))));
            string id = Item(f).AssignmentId;
            AssertRejected(f, id, RawSavePayloadClassifier.WorkloadExceededReason);
            f.Reopen();
            Assert.That(Item(f).AssignmentId, Is.EqualTo(id));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Any(value => value.AssignmentId == id), Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void UnassignAndRedeployToAnotherRoomPreservesIdentityAndCostsNoMana(bool fullDestination)
        {
            var f = Active(); var item = Item(f);
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 7),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            var destination = f.State.Floors[0].Layout.Rooms.Single(r => r.RoomInstanceId != item.RoomInstanceId);
            if (fullDestination)
            {
                Assert.That(CanonicalRoomCapacityResolver.TryResolve(f.Production, destination.RoomDefinitionId,
                    out var capacity, out _), Is.True);
                for (int i = 0; i < capacity.MonsterCapacity; i++)
                    f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(item.CategoryId, item.OptionId, destination.RoomInstanceId)));
            }
            double mana = f.Runtime.structureRuntime.ManaReserve; string ledger = Investment(f);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(item.AssignmentId))); f.Reopen();
            byte[] before = f.Session.GetCurrentBytes();
            var result = f.Execute(DetachedCanonicalMutationRequest.Redeploy(item.AssignmentId, destination.RoomInstanceId));
            if (fullDestination)
            {
                Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.CapacityReason));
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId, Is.EqualTo(item.AssignmentId));
            }
            else
            {
                f.Accept(result); f.Reopen();
                var active = Item(f);
                Assert.That(active.AssignmentId, Is.EqualTo(item.AssignmentId));
                Assert.That(active.CategoryId, Is.EqualTo(item.CategoryId));
                Assert.That(active.OptionId, Is.EqualTo(item.OptionId));
                Assert.That(active.RoomInstanceId, Is.EqualTo(destination.RoomInstanceId));
                Assert.That(f.State.LifecycleAndOwnership.ReturnedContents, Is.Empty);
            }
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(Investment(f), Is.EqualTo(ledger));
        }

        [TestCase(false, false)] [TestCase(true, true)]
        public void BootstrapCyclesIndividualDuplicatesAndUnassignsOutsidePurchaseGate(bool online, bool pending)
        {
            var f = Active();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId, Room(f))));
            var first = Item(f); var second = f.State.Floors[0].RoomContents.Assignments[1];
            var go = new GameObject("DirectUnassignmentTest");
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                root.SetOnline(online); root.SetVerificationPending(pending);
                double mana = root.Save.structureRuntime.ManaReserve;
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(first.AssignmentId));
                string label = overlay.ActiveContentSelectionText;
                Assert.That(label, Does.Contain("1 of 2"));
                Assert.That(label, Does.Contain(root.Content.GetString(MvpDungeonPlacementPresenter.GoblinOptionKey, "")));
                overlay.CycleActiveContent(); Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(second.AssignmentId));
                Assert.That(overlay.ActiveContentSelectionText, Does.Contain("2 of 2"));
                overlay.CycleActiveContent(); Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(first.AssignmentId));
                overlay.CycleActiveContent();
                root.Save.structureRuntime.PlacementLocked = true;
                Assert.That(overlay.UnassignSelectedActiveContent(), Is.False);
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(second.AssignmentId));
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString("ui.banner.place_blocked_heat_crisis", "")));
                root.Save.structureRuntime.PlacementLocked = false;
                Assert.That(overlay.UnassignSelectedActiveContent(), Is.True);
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString("ui.active_content.success", "")));
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(first.AssignmentId));
                Assert.That(root.SelectedReturnedAssignmentId, Is.EqualTo(second.AssignmentId));
                Assert.That(overlay.RedeploySelectedReturnedContent(), Is.True);
                Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana));
                Assert.That(overlay.UnassignSelectedActiveContent(), Is.True);
                Assert.That(overlay.UnassignSelectedActiveContent(), Is.True);
                Assert.That(overlay.UnassignSelectedActiveContent(), Is.False);
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(
                    DetachedCanonicalSpatialMutation.ActiveAssignmentMissingReason, "")));
                foreach (string text in new[] { label, overlay.ActiveContentSelectionText, root.BannerMessage })
                    foreach (string raw in new[] { first.AssignmentId, second.AssignmentId, first.CategoryId, first.OptionId, "content.unassignment." })
                        Assert.That(text, Does.Not.Contain(raw));
                foreach (string key in new[] { "ui.active_content.empty", "ui.active_content.selection", "ui.active_content.next",
                    "ui.active_content.unassign", "ui.active_content.success", DetachedCanonicalSpatialMutation.ActiveAssignmentMissingReason,
                    DetachedCanonicalSpatialMutation.ReturnNotPermittedReason })
                    Assert.That(root.Content.GetString(key, ""), Is.Not.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BootstrapRoomChangeResolvesSelectionFromThatRoomAndRetainsItOnPolicyFailure()
        {
            var f = Active(); var first = Item(f);
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 7),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            string target = f.State.Floors[0].Layout.Rooms.Single(r => r.RoomInstanceId != first.RoomInstanceId).RoomInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId, target)));
            var second = f.State.Floors[0].RoomContents.Assignments.Single(a => a.RoomInstanceId == target);
            var go = new GameObject("DirectUnassignmentRoomSelectionTest");
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(first.AssignmentId));
                root.Save.mvpSelectedRoomSlotIndex = 1;
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(second.AssignmentId));
                root.CycleActiveContent(); Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(second.AssignmentId));
                byte[] before = root.SaveService.CanonicalSession.GetCurrentBytes();
                root.SaveService.ConfigureStructuralRemovalPolicy(null);
                Assert.That(root.TryUnassignSelectedActiveContent(), Is.False);
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(second.AssignmentId));
                Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(
                    StructuralContentRemovalPolicyAuthority.MissingOrUnresolvedReason, "")));
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                root.SaveService.ConfigureStructuralRemovalPolicy(f.RemovalPolicy);
                Assert.That(root.TryUnassignSelectedActiveContent(), Is.True);
                Assert.That(root.SelectedActiveAssignmentId, Is.Null);
                root.Save.mvpSelectedRoomSlotIndex = 0;
                Assert.That(root.SelectedActiveAssignmentId, Is.EqualTo(first.AssignmentId));
                Assert.That(root.Save.spatialFloors[0].RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(first.AssignmentId));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
