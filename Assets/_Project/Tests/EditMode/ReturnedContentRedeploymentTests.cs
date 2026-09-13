#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class ReturnedContentRedeploymentTests
    {
        // Production placement/construction/deletion creates custody; no production inventory fixture path.
        internal static Fixture Owned()
        {
            var f = Fixture.Create(null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.BasicRoomOptionId)));
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 7),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            string tail = f.State.Floors[0].Layout.Rooms.Single(r => r.RoomInstanceId.Contains(".room.player.")).RoomInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId, tail)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.TrapCategoryId,
                MvpDungeonPlacementIds.SnareTrapOptionId, tail)));
            var deletion = StructuralDeletionService.Preview(f.State,
                new StructuralDeletionRequest { TargetRoomInstanceId = tail }, f.RemovalPolicy,
                f.Production, f.Configuration, f.Profile.Canonical);
            Assert.That(deletion.IsValid, Is.True, string.Join(",", deletion.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Delete(deletion)));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(2));
            return f;
        }

        private static string Target(Fixture f) => f.State.Floors[0].Layout.Rooms.Single().RoomInstanceId;
        private static ReturnedStructuralContent Item(Fixture f) => f.State.LifecycleAndOwnership.ReturnedContents[0];
        private static DetachedCanonicalMutationRequest Request(Fixture f) =>
            DetachedCanonicalMutationRequest.Redeploy(Item(f).AssignmentId, Target(f));
        private static byte[] Bytes(Fixture f) => Encoding.UTF8.GetBytes(JsonUtility.ToJson(f.State));
        private static void Canonicalize(Fixture f)
        {
            Assert.That(CanonicalSpatialSaveContracts.TryCanonicalize(f.State, f.Profile.Canonical.Spatial,
                out var canonical), Is.True);
            f.State = canonical;
            Assert.That(CanonicalSpatialSaveContracts.Validate(f.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
        }

        [Test]
        public void PureRedeploymentPreservesOwnershipAndSequenceAndOrdersBothCollections()
        {
            var f = Owned(); var owned = Item(f); byte[] before = Bytes(f);
            var result = f.Prepare(Request(f));
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            var live = result.State.Floors[0].RoomContents.Assignments.Single();
            Assert.That(live.AssignmentId, Is.EqualTo(owned.AssignmentId));
            Assert.That(live.CategoryId, Is.EqualTo(owned.CategoryId));
            Assert.That(live.OptionId, Is.EqualTo(owned.OptionId));
            Assert.That(live.Sequence, Is.EqualTo(owned.Sequence));
            Assert.That(live.RoomInstanceId, Is.EqualTo(Target(f)));
            Assert.That(result.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId,
                Is.EqualTo(f.State.LifecycleAndOwnership.ReturnedContents[1].AssignmentId));
            Assert.That(CanonicalSpatialSaveContracts.Validate(result.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
            CollectionAssert.AreEqual(before, Bytes(f));
            // Equivalent requests produce exact equivalent canonical bytes.
            CollectionAssert.AreEqual(CanonicalSpatialSaveSerializer.Serialize(result.State, f.Profile.Canonical).Value,
                CanonicalSpatialSaveSerializer.Serialize(f.Prepare(Request(f)).State, f.Profile.Canonical).Value);
        }

        [TestCase(false)] [TestCase(true)]
        public void CollisionUsesNextSequenceAndHighSequenceAdvancesItWithoutRenumbering(bool high)
        {
            var f = Owned();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.GoblinOptionId, Target(f))));
            var existing = f.State.Floors[0].RoomContents.Assignments.Single();
            Item(f).Sequence = high ? 100 : existing.Sequence;
            Canonicalize(f);
            var owned = f.State.LifecycleAndOwnership.ReturnedContents.Single(i => i.OptionId == MvpDungeonPlacementIds.SkeletonOptionId);
            long next = f.State.Floors[0].RoomContents.NextSequence;
            var result = f.Prepare(DetachedCanonicalMutationRequest.Redeploy(owned.AssignmentId, Target(f)));
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            var live = result.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == owned.AssignmentId);
            Assert.That(live.Sequence, Is.EqualTo(high ? 100 : next));
            Assert.That(result.State.Floors[0].RoomContents.NextSequence, Is.EqualTo(high ? 101 : next + 1));
            Assert.That(JsonUtility.ToJson(result.State.Floors[0].RoomContents.Assignments.Single(a => a.AssignmentId == existing.AssignmentId)),
                Is.EqualTo(JsonUtility.ToJson(existing)));
            Assert.That(CanonicalSpatialSaveContracts.Validate(result.State, f.Profile.Canonical.Spatial, true).IsValid, Is.True);
        }

        [TestCase(null)] [TestCase("")] [TestCase("missing.owned.item")]
        public void MissingCustodyFailsStablyWithoutChanges(string id)
        {
            var f = Owned(); byte[] before = Bytes(f);
            var result = f.Prepare(DetachedCanonicalMutationRequest.Redeploy(id, Target(f)));
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ReturnedItemMissingReason));
            CollectionAssert.AreEqual(before, Bytes(f));
        }

        [TestCase(null)] [TestCase("missing.room")]
        public void MissingTargetFailsStablyRetainingCustody(string target)
        {
            var f = Owned(); byte[] before = Bytes(f);
            var result = f.Prepare(DetachedCanonicalMutationRequest.Redeploy(Item(f).AssignmentId, target));
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.TargetRoomMissingReason));
            CollectionAssert.AreEqual(before, Bytes(f));
        }

        [Test]
        public void FullCategoryRetainsCustody()
        {
            var f = Owned(); var floor = f.State.Floors[0];
            Assert.That(CanonicalRoomCapacityResolver.TryResolve(f.Production, floor.Layout.Rooms[0].RoomDefinitionId,
                out var capacity, out _), Is.True);
            floor.RoomContents.Assignments = Enumerable.Range(0, capacity.MonsterCapacity).Select(i =>
                new RoomContentAssignment { AssignmentId = "test.live.monster." + i, RoomInstanceId = Target(f),
                    CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.GoblinOptionId,
                    Sequence = i }).ToArray(); // Inline test-only canonical capacity fixture.
            floor.RoomContents.NextSequence = capacity.MonsterCapacity;
            Canonicalize(f); byte[] before = Bytes(f);
            var result = f.Prepare(Request(f));
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.CapacityReason));
            CollectionAssert.AreEqual(before, Bytes(f));
        }

        [Test]
        public void AcquisitionDoesNotConsumeCustodyAndSameOptionRedeploymentIsNoOp()
        {
            var f = Owned(); var request = Request(f); var owned = Item(f);
            Assert.That(request.Kind, Is.Not.EqualTo(DetachedCanonicalMutationRequest.Place(owned.CategoryId, owned.OptionId).Kind));
            Assert.That(request.CategoryId, Is.Null); Assert.That(request.OptionId, Is.Null);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(owned.CategoryId, owned.OptionId, Target(f))));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Length, Is.EqualTo(2));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId, Is.Not.EqualTo(owned.AssignmentId));
            byte[] before = f.Session.GetCurrentBytes(); var runtime = f.Runtime;
            var result = f.Execute(request);
            Assert.That(result.IsNoOp, Is.True); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(f.Runtime, Is.SameAs(runtime));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [TestCase("category")] [TestCase("option")] [TestCase("duplicate")]
        [TestCase("assigned")] [TestCase("destination")] [TestCase("overflow")]
        public void CorruptionIsRejectedWithoutRepair(string kind)
        {
            var f = Owned(); var request = Request(f); var owned = Item(f);
            if (kind == "category") owned.CategoryId = MvpDungeonPlacementIds.TrapCategoryId;
            if (kind == "option") owned.OptionId = "test.invalid.option";
            if (kind == "duplicate") f.State.LifecycleAndOwnership.ReturnedContents =
                f.State.LifecycleAndOwnership.ReturnedContents.Concat(new[] { owned }).ToArray();
            if (kind == "assigned") f.State.Floors[0].RoomContents.Assignments = new[] {
                new RoomContentAssignment { AssignmentId = owned.AssignmentId, RoomInstanceId = Target(f),
                    CategoryId = owned.CategoryId, OptionId = owned.OptionId, Sequence = owned.Sequence } };
            if (kind == "destination") f.State.Floors[0].RoomContents.NextSequence = -1;
            if (kind == "overflow") { owned.Sequence = long.MaxValue; Canonicalize(f); }
            byte[] before = Bytes(f);
            var result = f.Prepare(request);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.IsNoOp, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            CollectionAssert.AreEqual(before, Bytes(f));
        }

        [Test]
        public void UnavailableConfiguredContentFailsEvenThoughCustodyIsValid()
        {
            var f = Owned(); var request = Request(f);
            f.Configuration.MvpPlacementEffects = f.Configuration.MvpPlacementEffects.Where(e => e.OptionId != Item(f).OptionId).ToArray();
            byte[] before = Bytes(f); var result = f.Prepare(request);
            Assert.That(result.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.InvalidOptionReason));
            CollectionAssert.AreEqual(before, Bytes(f));
        }

        [Test]
        public void ExistingReturnedLootIsNotInvalidatedByUnresolvedCurrentRemovalPolicy()
        {
            var f = Owned(); var owned = Item(f);
            owned.CategoryId = MvpDungeonPlacementIds.LootNodeCategoryId;
            owned.OptionId = MvpDungeonPlacementIds.HiddenCacheOptionId;
            Canonicalize(f);
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(f.RemovalPolicy,
                owned.CategoryId, owned.OptionId, out _, out _), Is.False);
            var result = f.Prepare(Request(f));
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            Assert.That(result.State.Floors[0].RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(owned.AssignmentId));
        }

        [Test]
        public void DurableWriteReopensSchemaNineWithoutManaOrInvestmentEffectsOrReturnedCopy()
        {
            var f = Owned(); var owned = Item(f); double mana = f.Runtime.structureRuntime.ManaReserve;
            var oldSession = f.Session; var oldState = f.State; var oldRuntime = f.Runtime; var request = Request(f);
            var investment = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context)
                .Investment.Select(JsonUtility.ToJson).ToArray();
            // Current policy is a future removal authority only; redeployment does not require it.
            f.RemovalPolicy = null;
            var result = f.Execute(Request(f));
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            CollectionAssert.AreEqual(result.GetPersistedBytes(), f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Accept(result); f.Reopen();
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(9));
            Assert.That(Encoding.UTF8.GetString(f.Session.GetCurrentBytes()), Does.Contain("\"schemaVersion\":9"));
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(owned.AssignmentId));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Any(i => i.AssignmentId == owned.AssignmentId), Is.False);
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            CollectionAssert.AreEqual(investment, result.Validation.Investment.Select(JsonUtility.ToJson).ToArray());
            var retry = f.Execute(DetachedCanonicalMutationRequest.Redeploy(owned.AssignmentId, Target(f)));
            Assert.That(retry.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ReturnedItemMissingReason));
            byte[] latest = f.Session.GetCurrentBytes();
            var stale = f.Authority.Execute(f.ActivePath, f.FileSystem, oldSession, oldState, oldRuntime, request);
            Assert.That(stale.IsSuccess, Is.False); Assert.That(stale.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(latest, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void AtomicFailureAndStaleStateKeepExactDiskRuntimeAndCustody()
        {
            var f = Owned(); var request = Request(f); byte[] before = f.Session.GetCurrentBytes();
            byte[] spatial = Bytes(f); var runtime = f.Runtime; var session = f.Session;
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace, 1);
            var failure = f.Execute(request);
            Assert.That(failure.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(failure.RuntimeProjection, Is.Null); Assert.That(failure.Session, Is.Null);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(spatial, Bytes(f));
            Assert.That(f.Runtime, Is.SameAs(runtime)); Assert.That(f.Session, Is.SameAs(session));
            f.FileSystem.DisableFailure();
            f.State.Floors[0].RoomContents.NextSequence++;
            var stale = f.Execute(request);
            Assert.That(stale.Reason, Is.EqualTo(DetachedCanonicalSpatialMutation.ValidationFailedReason));
            var invalid = f.Authority.Execute(f.ActivePath, f.FileSystem, null, runtime, request);
            Assert.That(invalid.IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [TestCase(0)] [TestCase(1)]
        public void BootstrapCyclesCanonicalCustodyAndRedeploysSelectedItemWithLocalizedFeedback(int selectedRoom)
        {
            var f = Owned();
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest
            { RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 7),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "east" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            f.Runtime.mvpSelectedRoomSlotIndex = selectedRoom;
            var route = CanonicalMvpRouteProjection.InspectWithProductionContent(f.Runtime, f.Production);
            string target = route.Rooms[selectedRoom].RoomInstanceId;
            var go = new GameObject("ReturnedContentPlayerTest");
            try
            {
                var root = go.AddComponent<GameRoot>(); var overlay = go.AddComponent<BootstrapOverlay>();
                var content = new ContentService(); const string dir = "Assets/_Project/Data/Bootstrap/";
                var assets = new[] { "content_bootstrap", "build_config", "schema_versions", "content_manifest",
                    "dev_commands", "string_table_en", "heat_runtime" }.Select(n => AssetDatabase.LoadAssetAtPath<TextAsset>(dir + n + ".json")).ToArray();
                content.LoadAll(assets[0], assets[1], assets[2], assets[3], assets[4], assets[5], assets[6], new SimpleLogger(false), out _);
                typeof(ContentService).GetProperty("ProductionSpatialContent").SetValue(content, f.Production);
                typeof(GameRoot).GetProperty("Content").SetValue(root, content);
                typeof(GameRoot).GetProperty("Save").SetValue(root, f.Runtime);
                var service = new SaveService(new SimpleLogger(false), null, Path.GetDirectoryName(f.ActivePath));
                service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                    Encoding.UTF8.GetBytes(JsonUtility.ToJson(f.Configuration)));
                typeof(SaveService).GetProperty("SavePath").SetValue(service, f.ActivePath);
                foreach (var pair in new[] { Tuple.Create("_canonicalSession", (object)f.Session), Tuple.Create("_canonicalFileSystem", (object)f.FileSystem),
                    Tuple.Create("_validationContext", (object)f.Context) })
                    typeof(SaveService).GetField(pair.Item1, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, pair.Item2);
                root.AttachSaveServiceForTests(service);
                typeof(BootstrapOverlay).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(overlay, root);
                var returned = f.State.LifecycleAndOwnership.ReturnedContents;
                Assert.That(root.SelectedReturnedAssignmentId, Is.EqualTo(returned[0].AssignmentId));
                Assert.That(overlay.ReturnedContentSelectionText, Does.Contain(content.GetString(MvpDungeonPlacementPresenter.SkeletonOptionKey, "")));
                overlay.CycleReturnedContent(); Assert.That(root.SelectedReturnedAssignmentId, Is.EqualTo(returned[1].AssignmentId));
                overlay.CycleReturnedContent(); Assert.That(root.SelectedReturnedAssignmentId, Is.EqualTo(returned[0].AssignmentId));
                Assert.That(overlay.RedeploySelectedReturnedContent(), Is.True);
                Assert.That(root.BannerMessage, Is.EqualTo(content.GetString("ui.returned_content.success", "")));
                Assert.That(root.Save.spatialFloors[0].RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(returned[0].AssignmentId));
                Assert.That(root.Save.spatialFloors[0].RoomContents.Assignments.Single().RoomInstanceId, Is.EqualTo(target));
                Assert.That(overlay.RedeploySelectedReturnedContent(), Is.True); // Next owned trap, never another skeleton.
                Assert.That(overlay.RedeploySelectedReturnedContent(), Is.False);
                Assert.That(root.Save.spatialFloors[0].RoomContents.Assignments.Length, Is.EqualTo(2));
                Assert.That(root.BannerMessage, Is.EqualTo(content.GetString(DetachedCanonicalSpatialMutation.ReturnedItemMissingReason, "")));
                Assert.That(root.BannerMessage, Does.Not.Contain("content.redeployment."));
                foreach (string key in new[] { "ui.returned_content.empty", "ui.returned_content.selection", "ui.returned_content.next",
                    "ui.returned_content.redeploy", "ui.returned_content.success", "ui.returned_content.same_option",
                    DetachedCanonicalSpatialMutation.ReturnedItemMissingReason, DetachedCanonicalSpatialMutation.TargetRoomMissingReason })
                    Assert.That(content.GetString(key, ""), Is.Not.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
