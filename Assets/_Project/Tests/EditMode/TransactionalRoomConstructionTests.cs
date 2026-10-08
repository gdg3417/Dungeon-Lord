#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Store = DungeonBuilder.M0.Tests.EditMode.PhaseSevenA4TransactionalEditorTests.Store;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class TransactionalRoomConstructionTests
    {
        internal static Fixture Room() => PhaseSevenA4TransactionalEditorTests.Content();
        internal static DungeonDraftContext Context(Fixture f) => PhaseSevenA4TransactionalEditorTests.Context(f);
        internal static StructuralConstructionRequest Request(Fixture f, int x = 4, int y = 2, string room = "spatial.room.basic", string connection = "north", CardinalOrientation orientation = CardinalOrientation.Zero) =>
            new StructuralConstructionRequest { FloorInstanceId = f.State.Floors[0].FloorInstanceId, RoomDefinitionId = room,
                Anchor = new TileCoordinate(x, y), Orientation = orientation, TerminalConnectionPointId = connection };
        internal static void Flush(TransactionalDungeonDraft draft) { while (draft.Durability == DraftDurability.Pending) Assert.That(draft.FlushNext(), Is.True); }
        internal static StructuralEconomyPreview Price(Fixture f, TransactionalDungeonDraft draft) => StructuralEconomyService.PreviewFinalDraft(
            f.State, draft.ReadModel, DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment,
            draft.NormalizedMovementTargets(), f.Runtime.structureRuntime.ManaReserve, f.Economy);
        internal static string Build(Fixture f, TransactionalDungeonDraft draft, StructuralConstructionRequest request = null)
        {
            string id = Guid.NewGuid().ToString("N"); Assert.That(draft.ConstructRoom(id, request ?? Request(f)), Is.True); Flush(draft); return id;
        }

        [TestCase("spatial.room.basic", CardinalOrientation.Zero)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Zero)]
        [TestCase("spatial.room.rectangle", CardinalOrientation.Ninety)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Zero)]
        [TestCase("spatial.room.large_chamber", CardinalOrientation.Ninety)]
        public void ConstructionGuidanceMatchesEveryAuthoritativeAnchorForAuthoredConfiguration(string room, CardinalOrientation orientation)
        {
            var f = Room(); var context = Context(f); var before = context.Fingerprint(f.State);
            var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var request = Request(f, room: room, orientation: orientation);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var guidance = StructuralEditService.GetConstructionGuidance(f.State, request, context, limits);
            watch.Stop(); TestContext.WriteLine(room + "/" + orientation + ": " + guidance.ExaminedAnchorCount + " previews, " + watch.ElapsedMilliseconds + " ms, " + guidance.ValidAnchors.Length + " anchors");
            Assert.That(guidance.Reason, Is.Null); Assert.That(guidance.ExaminedAnchorCount, Is.EqualTo(f.Production.Catalog.Floors[0].Bounds.TileCount));
            CollectionAssert.AreEqual(guidance.ValidAnchors.OrderBy(a => a), guidance.ValidAnchors);
            var bounds = f.Production.Catalog.Floors[0].Bounds;
            for (int x = bounds.Minimum.X; x < bounds.Minimum.X + bounds.Width; x++)
                for (int y = bounds.Minimum.Y; y < bounds.Minimum.Y + bounds.Height; y++)
                {
                    request.Anchor = new TileCoordinate(x, y);
                    var preview = StructuralEditService.Preview(f.State, request, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                    Assert.That(guidance.ValidAnchors.Contains(request.Anchor), Is.EqualTo(preview.IsValid && context.Validate(preview.DetachedCandidate)), request.Anchor.ToString());
                }
            Assert.That(context.Fingerprint(f.State), Is.EqualTo(before));
        }

        [TestCase("floor")][TestCase("room")][TestCase("orientation")][TestCase("connection")][TestCase("bounds")][TestCase("overlap")][TestCase("terminal")]
        public void InvalidConstructionNeverConsumesNativeIdentityOrChangesAcknowledgedProjection(string kind)
        {
            var f = Room(); var store = new Store(); var context = Context(f); var d = TransactionalDungeonDraft.Create(f.State, context, store);
            var request = Request(f);
            switch (kind)
            {
                case "floor": request.FloorInstanceId = "missing.floor"; break;
                case "room": request.RoomDefinitionId = "missing.room"; break;
                case "orientation": request.Orientation = CardinalOrientation.OneEighty; break;
                case "connection": request.TerminalConnectionPointId = "missing.connection"; break;
                case "bounds": request.Anchor = new TileCoordinate(-1, 2); break;
                case "overlap": request.Anchor = f.State.Floors[0].Layout.Rooms[0].Anchor; break;
                case "terminal": request.TerminalConnectionPointId = "west"; break;
            }
            var preview = StructuralEditService.Preview(f.State, request, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.False);
            bool accepted = d.ConstructRoom(Guid.NewGuid().ToString("N"), request);
            Assert.That(accepted, Is.EqualTo(kind != "floor" && kind != "room"));
            Flush(d); Assert.That(context.Fingerprint(d.ReadModel), Is.EqualTo(context.Fingerprint(f.State)));
            Assert.That(d.CanSave, Is.False);
            if (accepted)
            {
                Assert.That(d.InvalidConstructions.Single().Reason, Is.EqualTo(preview.ReasonCodes.First()));
                var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
                Assert.That(recovered, Is.Not.Null, reason); Assert.That(recovered.InvalidConstructions.Length, Is.EqualTo(1));
            }
        }

        [TestCase(false)][TestCase(true)]
        public void InvalidIntentRecoveryCanCorrectOrExplicitlyCancelWithoutHiddenBlocker(bool cancel)
        {
            var f = Room(); var context = Context(f); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, context, store);
            string id = Build(f, d, Request(f, -1, 2));
            d = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out _);
            Assert.That(cancel ? d.CancelConstruction(id) : d.ConstructRoom(id, Request(f)), Is.True); Flush(d);
            Assert.That(d.InvalidConstructions, Is.Empty); Assert.That(d.IsStructurallyValid, Is.True);
            Assert.That(d.HasChanges, Is.EqualTo(!cancel));
            Assert.That(d.ConstructRoom(id, Request(f)), Is.False, "No duplicate completed intent identities");
            Assert.That(d.ReadModel.LifecycleAndOwnership.Floors[0].NextNativeRoomOrdinal,
                Is.EqualTo(f.State.LifecycleAndOwnership.Floors[0].NextNativeRoomOrdinal + (cancel ? 0 : 1)));
        }

        [TestCase(false)][TestCase(true)]
        public void NewRoomMovedBeforeSaveHasSameFinalCostAsDirectConstruction(bool experiment)
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            Build(f, d, Request(f, experiment ? 4 : 5));
            if (experiment)
            {
                var room = d.ReadModel.Floors[0].Layout.Rooms.Single(r => !f.State.Floors[0].Layout.Rooms.Any(old => old.RoomInstanceId == r.RoomInstanceId));
                Assert.That(d.MoveRoom(room.FloorId, room.RoomInstanceId, new TileCoordinate(5, 2)), Is.True); Flush(d);
            }
            Assert.That(d.IsStructurallyValid, Is.True, d.StructuralReason);
            var price = Price(f, d); Assert.That(price.Cost, Is.EqualTo(105)); Assert.That(price.Investment.Sum(r => r.RenovationMana), Is.Zero);
            f.Runtime.structureRuntime.ManaReserve = price.Cost;
            var before = f.Session.GetCurrentBytes(); var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.Zero); Assert.That(f.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d).IsSuccess, Is.False);
        }

        [Test]
        public void MixedLegacyContentMovementConstructionChainsKeepExactPredecessorBytes()
        {
            var f = Room(); var context = Context(f); string path = f.ActivePath + ".editor-draft";
            var store = new FileDungeonDraftStore(path, f.FileSystem, f.Profile); var d = TransactionalDungeonDraft.Create(f.State, context, store);
            Assert.That(PhaseSevenA4TransactionalEditorTests.Move(d, f, 1, 1), Is.True); Flush(d);
            var v2 = f.FileSystem.Paths.Where(p => p.StartsWith(path)).ToDictionary(p => p, p => f.FileSystem.ReadAllBytes(p));
            Assert.That(DungeonDraftFormat.Parse(store.Read()).FormatVersion, Is.EqualTo(2));
            Assert.That(d.MoveRoom(f.State.Floors[0].FloorInstanceId, f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, new TileCoordinate(0, 3)), Is.True); Flush(d);
            var v3bytes = store.Read(); Assert.That(DungeonDraftFormat.Parse(v3bytes).FormatVersion, Is.EqualTo(3));
            d = TransactionalDungeonDraft.Recover(v3bytes, f.State, context, store, out var reason); Assert.That(d, Is.Not.Null, reason);
            var v3 = f.FileSystem.Paths.Where(p => p.StartsWith(path)).ToDictionary(p => p, p => f.FileSystem.ReadAllBytes(p));
            Build(f, d, Request(f, 4, 3));
            Assert.That(DungeonDraftFormat.Parse(store.Read()).FormatVersion, Is.EqualTo(4));
            foreach (var pair in v2.Concat(v3)) CollectionAssert.AreEqual(pair.Value, f.FileSystem.ReadAllBytes(pair.Key));
            d = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, new FileDungeonDraftStore(path, f.FileSystem, f.Profile), out reason);
            Assert.That(d, Is.Not.Null, reason); Assert.That(d.CommandCount, Is.EqualTo(3)); Assert.That(d.IsStructurallyValid, Is.True, d.StructuralReason);
            var price = Price(f, d); Assert.That(price.Cost, Is.EqualTo(110)); Assert.That(price.Investment.Sum(r => r.RenovationMana), Is.EqualTo(10));
            Assert.That(d.ReadModel.Floors[0].RoomContents.Assignments.Single().RoomLocalPosition, Is.EqualTo(new TileCoordinate(1, 1)));
        }

        [TestCase(Operation.Write, 2)][TestCase(Operation.Replace, 1)][TestCase(Operation.Read, 1)][TestCase(Operation.Flush, 1)]
        public void AtomicConstructionSaveFailurePublishesNothingAndPreservesDraft(Operation operation, int occurrence)
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store); Build(f, d);
            var before = f.Session.GetCurrentBytes(); double balance = f.Runtime.structureRuntime.ManaReserve;
            f.FileSystem.EnableFailure(operation, occurrence);
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null); Assert.That(result.Session, Is.Null);
            f.FileSystem.DisableFailure(); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(balance)); Assert.That(d.CanSave, Is.True); Assert.That(store.Read(), Is.Not.Null);
        }

        [Test]
        public void ConstructionSaveRechecksConfiguredPricesAndInsufficientManaPreservesEvidence()
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Build(f, d);
            var config = JsonUtility.FromJson<StructuralEconomyConfiguration>(File.ReadAllText("Assets/_Project/Resources/structural_economy.json"));
            config.Rooms.Single(r => r.DefinitionId == "spatial.room.basic").Mana = 333;
            Assert.That(StructuralEconomySnapshot.TryCreate(config, f.Production.Catalog, out f.Economy), Is.True);
            f.Runtime.structureRuntime.ManaReserve = 332; var before = f.Session.GetCurrentBytes();
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.Reason, Is.EqualTo(StructuralEconomyService.InsufficientReason)); Assert.That(d.CanSave, Is.True);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Runtime.structureRuntime.ManaReserve = 333; result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.True, result.Reason); Assert.That(result.RuntimeProjection.structureRuntime.ManaReserve, Is.Zero);
        }

        [Test]
        public void ConsecutiveConstructionReplaysStableNativeIdentitiesAndCanonicalOrdering()
        {
            var f = Room(); var context = Context(f); var store = new Store();
            var d = TransactionalDungeonDraft.Create(f.State, context, store);
            Build(f, d);
            var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var guidance = StructuralEditService.GetConstructionGuidance(d.ReadModel, Request(f), context, limits);
            Assert.That(guidance.ValidAnchors, Is.Not.Empty);
            var second = Request(f); second.Anchor = guidance.ValidAnchors.First(); Build(f, d, second);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
            Assert.That(recovered, Is.Not.Null, reason);
            Assert.That(context.Fingerprint(recovered.ReadModel), Is.EqualTo(context.Fingerprint(d.ReadModel)));
            Assert.That(d.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(3));
            Assert.That(d.ReadModel.Floors[0].Layout.Rooms.Select(r => r.RoomInstanceId).Distinct().Count(), Is.EqualTo(3));
            var price = Price(f, d);
            Assert.That(price.IsAffordable, Is.True, price.Reason);
            Assert.That(price.Investment.Sum(r => r.ConstructionMana), Is.EqualTo(price.Cost));
            Assert.That(price.Investment.Sum(r => r.RenovationMana), Is.Zero);
        }

        [Test]
        [TestCase(false)][TestCase(true)]
        public void FinalConstructionMatchesExistingSingleConstructionFormulaAndPaidInvestment(bool paidBaseline)
        {
            var f = Room();
            if (paidBaseline)
            {
                var existing = StructuralEditService.Preview(f.State, Request(f, 5), f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                Assert.That(existing.IsValid, Is.True);
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(existing)));
            }
            var context = Context(f); var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var request = Request(f); request.Anchor = StructuralEditService.GetConstructionGuidance(f.State, request, context, limits).ValidAnchors.First();
            var spatial = StructuralEditService.Preview(f.State, request, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            var ledger = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment;
            var canonicalPrice = StructuralEconomyService.Preview(spatial, f.State, ledger, f.Runtime.structureRuntime.ManaReserve, f.Economy);
            var d = TransactionalDungeonDraft.Create(f.State, context, new Store()); Build(f, d, request); var final = Price(f, d);
            Assert.That(final.Cost, Is.EqualTo(canonicalPrice.Cost)); Assert.That(final.Refund, Is.EqualTo(canonicalPrice.Refund));
            Assert.That(JsonUtility.ToJson(new InvestmentComparison { Records = final.Investment }),
                Is.EqualTo(JsonUtility.ToJson(new InvestmentComparison { Records = canonicalPrice.Investment })));
            foreach (var room in f.State.Floors[0].Layout.Rooms)
                Assert.That(final.Investment.Single(r => r.StructureId == room.RoomInstanceId).ConstructionMana,
                    Is.EqualTo(ledger.Single(r => r.StructureId == room.RoomInstanceId).ConstructionMana));
        }
        [Serializable] private sealed class InvestmentComparison { public StructuralInvestmentRecord[] Records; }

        [Test]
        public void ConstructionOnExistingInactiveFloorUsesAuthoredBoundsWithoutLifecycleMutation()
        {
            var f = Room(); f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            Assert.That(FloorConstructionProfileSnapshot.TryParse(File.ReadAllBytes("Assets/_Project/Resources/floor_construction_profiles.json"),
                f.Production, f.Profile.Canonical, out var profiles), Is.True);
            const string research = "Assets/_Project/Data/Production/Research/Dungeon_Builder_Research_Export_Bundle/architecture/";
            Assert.That(FloorConstructionResearchAuthority.TryParse(File.ReadAllText(research + "research_nodes.json"), File.ReadAllText(research + "tables.json"),
                f.Profile.Canonical, out var permissions), Is.True);
            var floorAuthority = new DetachedCanonicalWriteAuthority(f.Production, f.Compatibility, f.Configuration, f.Context, f.Profile,
                f.RemovalPolicy, f.Economy, acquisition: f.Acquisition, branchingResearch: f.BranchingResearch,
                floorConstructionProfiles: profiles, floorConstructionResearch: permissions);
            var shell = FloorConstructionService.Preview(f.State, f.Runtime, f.Runtime.completedResearch, profiles, permissions,
                f.Production, f.Configuration, f.Profile.Canonical);
            f.Accept(floorAuthority.ConstructFloor(f.ActivePath, f.FileSystem, f.Session, f.Runtime, shell));
            var floor = f.State.Floors[1]; var context = Context(f); var request = Request(f, connection: "east"); request.FloorInstanceId = floor.FloorInstanceId;
            var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var watch = System.Diagnostics.Stopwatch.StartNew(); var guidance = StructuralEditService.GetConstructionGuidance(f.State, request, context, limits); watch.Stop();
            TestContext.WriteLine("Inactive Floor 2: " + guidance.ExaminedAnchorCount + " previews, " + watch.ElapsedMilliseconds + " ms");
            Assert.That(guidance.ValidAnchors, Is.Not.Empty); Assert.That(guidance.ExaminedAnchorCount, Is.EqualTo(f.Production.Catalog.Floors[1].Bounds.TileCount));
            request.Anchor = guidance.ValidAnchors.First(); var d = TransactionalDungeonDraft.Create(f.State, context, new Store()); Build(f, d, request);
            Assert.That(d.ReadModel.Floors[1].ActivationState, Is.EqualTo(FloorActivationState.Inactive));
            Assert.That(d.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(f.State.Floors[0].Layout.Rooms.Length));
            Assert.That(Price(f, d).IsAffordable, Is.True);
            Assert.That(f.State.Floors[1].Layout.Rooms, Is.Empty);
        }

        [Test]
        public void ConstructionGuidanceUnsupportedFloorUsesCanonicalReasonBeforeSearching()
        {
            var f = Room(); var request = Request(f); request.FloorInstanceId = "missing.floor";
            var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var guidance = StructuralEditService.GetConstructionGuidance(f.State, request, Context(f), limits);
            var preview = StructuralEditService.Preview(f.State, request, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(guidance.ValidAnchors, Is.Empty); Assert.That(guidance.ExaminedAnchorCount, Is.Zero);
            Assert.That(guidance.Reason, Is.EqualTo(preview.ReasonCodes.Single()));
        }

        [Test]
        public void ConstructionGuidanceCapacityAndWorkloadFailClosedWithoutMutation()
        {
            var f = Room(); var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
            var catalog = f.Production.Catalog;
            catalog.Floors[0].FinalFloorSpaceCapacity = 32;
            f.Production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var context = Context(f); Assert.That(context.Validate(f.State), Is.True);
            var guidance = StructuralEditService.GetConstructionGuidance(f.State, Request(f), context, limits);
            Assert.That(guidance.ValidAnchors, Is.Empty); Assert.That(guidance.ExaminedAnchorCount, Is.EqualTo(144));
            catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(0, 0), limits.MaximumMaterializedTiles, 2);
            f.Production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            context = Context(f); guidance = StructuralEditService.GetConstructionGuidance(f.State, Request(f), context, limits);
            Assert.That(guidance.ValidAnchors, Is.Empty); Assert.That(guidance.ExaminedAnchorCount, Is.Zero); Assert.That(guidance.Reason, Is.Not.Null);
        }

        [TestCase("duplicate")][TestCase("empty")][TestCase("cancel_payload")][TestCase("extra_kind")][TestCase("missing_point")][TestCase("bad_id")]
        public void MalformedConstructionPayloadRecoveryFailsClosed(string shape)
        {
            var f = Room(); var store = new Store(); var context = Context(f);
            var d = TransactionalDungeonDraft.Create(f.State, context, store); Build(f, d);
            var journal = DungeonDraftFormat.Parse(store.Read()); var command = journal.Commands.Single();
            switch (shape)
            {
                case "duplicate": command.RoomConstructions = new[] { command.RoomConstructions[0], command.RoomConstructions[0] }; break;
                case "empty": command.RoomConstructions[0].Placements = Array.Empty<StructuralConstructionRequest>(); break;
                case "cancel_payload": command.RoomConstructions[0].Cancel = true; break;
                case "extra_kind": command.RoomMovements = new[] { new StructuralMovementRequest() }; break;
                case "missing_point": command.RoomConstructions[0].Placements[0].TerminalConnectionPointId = null; break;
                case "bad_id": command.RoomConstructions[0].IntentId = "invalid"; break;
            }
            Assert.That(TransactionalDungeonDraft.Recover(DungeonDraftFormat.Encode(journal), f.State, context, store, out var reason), Is.Null);
            Assert.That(reason, Is.Not.Null); Assert.That(f.State.Floors[0].Layout.Rooms.Length, Is.EqualTo(1));
        }

        [Test]
        public void FailedConstructionAcknowledgementRetriesOrderedPrefixWithoutLoss()
        {
            var f = Room(); var store = new Store { FailWrite = true }; var context = Context(f);
            var d = TransactionalDungeonDraft.Create(f.State, context, store);
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"), Request(f)), Is.True);
            Assert.That(d.FlushNext(), Is.False); Assert.That(d.Durability, Is.EqualTo(DraftDurability.Failed));
            Assert.That(d.CanSave, Is.False); Assert.That(d.AcknowledgedSequence, Is.Zero);
            store.FailWrite = false; Flush(d); // Failed writes require an explicit retry.
            Assert.That(d.FlushNext(), Is.True); Assert.That(d.CanSave, Is.True);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
            Assert.That(recovered, Is.Not.Null, reason); Assert.That(recovered.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(2));
        }

        [Test]
        public void ConstructionRecoveryRejectsChangedCanonicalBaselineAndMaterialRules()
        {
            var f = Room(); var store = new Store(); var context = Context(f);
            var d = TransactionalDungeonDraft.Create(f.State, context, store); Build(f, d);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), d.ReadModel, context, store, out _), Is.Null);
            var catalog = f.Production.Catalog; catalog.Floors[0].FinalFloorSpaceCapacity--;
            f.Production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason), Is.Null);
            Assert.That(reason, Is.Not.Null);
        }

        [TestCase("candidate-before", 0)][TestCase("candidate-partial", 0)][TestCase("candidate-after", 0)][TestCase("candidate-barrier", 0)]
        [TestCase("commit-before", 0)][TestCase("commit-after", 1)][TestCase("commit-barrier", 1)][TestCase("commit-readback", 1)]
        public void ConstructionUnknownDurabilityResolvesOnlyIndependentCommitEvidence(string fault, int prefix)
        {
            var f = Room(); var context = Context(f); string path = f.ActivePath + ".editor-draft";
            var store = new FileDungeonDraftStore(path, f.FileSystem, f.Profile);
            var d = TransactionalDungeonDraft.Create(f.State, context, store); var before = f.Session.GetCurrentBytes();
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"), Request(f)), Is.True);
            switch (fault)
            {
                case "candidate-before": case "candidate-after":
                    f.FileSystem.EnableTargetedFailure(Operation.Write, p => p[0].EndsWith(".000001.candidate"), 1, fault.EndsWith("after")); break;
                case "candidate-partial": f.FileSystem.EnablePartialWriteFailure(8); break;
                case "candidate-barrier": f.FileSystem.EnableFailure(Operation.Flush, 1); break;
                case "commit-before": case "commit-after":
                    f.FileSystem.EnableTargetedFailure(Operation.Write, p => p[0].EndsWith(".000001.commit"), 1, fault.EndsWith("after")); break;
                case "commit-barrier": f.FileSystem.EnableFailure(Operation.Flush, 2); break;
                case "commit-readback": f.FileSystem.EnableTargetedFailure(Operation.Read, p => p[0].EndsWith(".000001.commit"), 1, false); break;
            }
            Assert.That(d.FlushNext(), Is.False); Assert.That(d.Durability, Is.EqualTo(DraftDurability.Unknown));
            f.FileSystem.DisableFailure(); Assert.That(d.FlushNext(), Is.False); Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"), Request(f)), Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            var recoveredStore = new FileDungeonDraftStore(path, f.FileSystem, f.Profile);
            var recovered = TransactionalDungeonDraft.Recover(recoveredStore.Read(), f.State, context, recoveredStore, out var reason);
            Assert.That(recovered, Is.Not.Null, reason); Assert.That(recovered.AcknowledgedSequence, Is.EqualTo(prefix));
            Assert.That(recovered.ReadModel.Floors[0].Layout.Rooms.Length, Is.EqualTo(1 + prefix));
        }

        [Test]
        public void ConstructionRecordByteBudgetRefusesBeforeFilesystemMutation()
        {
            var f = Room(); string probePath = f.ActivePath + ".construction-probe";
            var probe = new FileDungeonDraftStore(probePath, f.FileSystem, f.Profile);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), probe); Build(f, d);
            int budget = f.FileSystem.Paths.Where(p => p.StartsWith(probePath, StringComparison.Ordinal)).Sum(p => f.FileSystem.ReadAllBytes(p).Length) - 1;
            var old = f.Profile.Raw;
            var raw = new RawSavePayloadClassificationLimits(budget, old.MaximumNestingDepth, old.MaximumObjectMembers,
                old.MaximumArrayElements, old.MaximumStringBytes, old.MaximumScanWork);
            var whole = new DetachedWholeSaveLimits(f.Profile.Whole.MaximumCandidateBytes,
                Math.Min(budget, f.Profile.Whole.MaximumCopiedValueBytes), f.Profile.Whole.MaximumUnknownMembers,
                Math.Min(budget, f.Profile.Whole.MaximumUnknownMemberBytes));
            var profile = new SaveSpatialMigrationLimitsProfile(raw, f.Profile.Canonical, whole);
            var context = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, profile, f.Compatibility);
            var store = new FileDungeonDraftStore(f.ActivePath + ".bounded-construction", f.FileSystem, profile);
            d = TransactionalDungeonDraft.Create(f.State, context, store); var acknowledged = store.Read(); var paths = f.FileSystem.Paths.ToArray();
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"), Request(f)), Is.True);
            Assert.That(d.FlushNext(), Is.False); Assert.That(d.Durability, Is.EqualTo(DraftDurability.Failed));
            CollectionAssert.AreEqual(paths, f.FileSystem.Paths.ToArray()); CollectionAssert.AreEqual(acknowledged, store.Read());
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
            Assert.That(recovered, Is.Not.Null, reason); Assert.That(recovered.AcknowledgedSequence, Is.Zero);
        }

        [TestCase("context")][TestCase("downgrade")][TestCase("predecessor")][TestCase("duplicate")]
        public void ConstructionRecordBindingsAndPredecessorCorruptionFailClosed(string corruption)
        {
            var f = Room(); string path = f.ActivePath + ".editor-draft"; var context = Context(f);
            var store = new FileDungeonDraftStore(path, f.FileSystem, f.Profile); var d = TransactionalDungeonDraft.Create(f.State, context, store); Build(f, d);
            if (corruption == "context")
            {
                var limits = ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
                var request = Request(f); request.Anchor = StructuralEditService.GetConstructionGuidance(d.ReadModel, request, context, limits).ValidAnchors.First();
                Build(f, d, request);
            }
            string suffix = corruption == "context" ? ".000002.commit" : ".000001.commit";
            string commitPath = f.FileSystem.Paths.Single(p => p.StartsWith(path) && p.EndsWith(suffix));
            string candidatePath = commitPath.Substring(0, commitPath.Length - "commit".Length) + "candidate";
            var commit = DungeonDraftFormat.Exact<DungeonDraftCommitRecord>(f.FileSystem.ReadAllBytes(commitPath));
            var candidate = DungeonDraftFormat.Exact<DungeonDraftCandidate>(f.FileSystem.ReadAllBytes(candidatePath));
            Assert.That(commit.Version, Is.EqualTo(3)); Assert.That(candidate.JournalFormatVersion, Is.EqualTo(4));
            if (corruption == "context") candidate.RuleIdentity = commit.RuleIdentity = new string('0', 64);
            if (corruption == "downgrade") candidate.JournalFormatVersion = commit.JournalFormatVersion = 3;
            if (corruption == "predecessor") candidate.PredecessorCommit = commit.PredecessorCommit = new string('0', 64);
            if (corruption != "duplicate")
            {
                var payload = DungeonDraftFormat.Bytes(candidate); f.FileSystem.Seed(candidatePath, payload);
                commit.CandidateHash = SpatialContractSha256.Compute(payload);
            }
            string receipt = JsonUtility.ToJson(commit);
            if (corruption == "duplicate") receipt = receipt.Replace("\"Version\":3", "\"Version\":3,\"Version\":3");
            f.FileSystem.Seed(commitPath, Encoding.UTF8.GetBytes(receipt));
            Assert.That(Assert.Throws<IOException>(() => store.Read()).Message, Is.EqualTo(TransactionalDungeonDraft.RecoveryFailedReason));
            var before = f.Session.GetCurrentBytes();
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void BootstrapConstructionVisibilityIsDiagnosticOnlyWithOtherCapabilitiesRetained()
        {
            var f = Room(); var go = new GameObject("ConstructionRetirementSmoke"); go.SetActive(false);
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                Assert.That(overlay.StructuralConstructionControlsAvailable, Is.True);
                Assert.That(overlay.StructuralConstructionControlsVisible, Is.False);
                Assert.That(overlay.LegacyRoomPlacementControlsVisible, Is.False);
                Assert.That(overlay.StructuralRenovationControlsAvailable, Is.True);
                typeof(GameRoot).GetProperty("DevPanelEnabled").SetValue(root, true);
                Assert.That(overlay.StructuralConstructionControlsVisible, Is.True);
                Assert.That(overlay.LegacyRoomPlacementControlsVisible, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BootstrapUnmigratedActiveFloorStarterSetupRemainsAvailable()
        {
            var f = Fixture.Create(null); var go = new GameObject("StarterSetupBoundary"); go.SetActive(false);
            try
            {
                var root = StructuralConstructionGameRootTests.PurchaseRoot(go, f);
                var overlay = go.AddComponent<BootstrapOverlay>(); overlay.Bind(root);
                Assert.That(root.SelectedCanonicalRooms, Is.Empty);
                Assert.That(overlay.LegacyRoomPlacementControlsVisible, Is.True);
                Assert.That(overlay.StructuralConstructionControlsVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ConstructionDraftPreservesRunSnapshotKnowledgeAndLifecycleUntilAtomicPublication()
        {
            var f = Room(); var oldRun = PhaseSixA4Tests.Snapshot(f); var bytes = f.Session.GetCurrentBytes();
            var inputs = JsonUtility.ToJson(oldRun.Floors[0].InspectSpatialInputs());
            var canonical = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, f.Context);
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State, canonical.CorridorContent, f.Profile.Canonical,
                f.State.Floors[0].FloorInstanceId, out var fingerprint), Is.True);
            var knowledge = new FloorKnowledgeRecord { FloorInstanceId = f.State.Floors[0].FloorInstanceId, ApplicabilityFingerprint = fingerprint };
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Build(f, d);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(knowledge, f.State, canonical.CorridorContent, f.Profile.Canonical), Is.True);
            Assert.That(d.ReadModel.Floors[0].ActivationState, Is.EqualTo(f.State.Floors[0].ActivationState));
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d));
            Assert.That(FloorKnowledgeApplicability.IsApplicable(knowledge, f.State, canonical.CorridorContent, f.Profile.Canonical), Is.False);
            Assert.That(JsonUtility.ToJson(oldRun.Floors[0].InspectSpatialInputs()), Is.EqualTo(inputs));
            Assert.That(JsonUtility.ToJson(PhaseSixA4Tests.Snapshot(f).Floors[0].InspectSpatialInputs()), Is.Not.EqualTo(inputs));
        }

        [Test]
        public void ConstructionDiscardLeavesWholeCanonicalStateAndWalletUntouched()
        {
            var f = Room(); var before = f.Session.GetCurrentBytes(); var runtime = JsonUtility.ToJson(f.Runtime);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Build(f, d);
            Assert.That(d.Discard(), Is.True); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }
    }
}
#endif
