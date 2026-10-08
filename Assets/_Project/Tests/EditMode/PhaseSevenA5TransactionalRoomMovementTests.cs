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
    public class PhaseSevenA5TransactionalRoomMovementTests
    {
        private static SpatialContentValidationWorkloadLimits GuidanceLimits => ProductionSpatialContentWorkloadLimitParser.Parse(
            UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits;
        [TestCase(0)]
        [TestCase(1)]
        public void MoveGuidanceEditGridUsesExactlySelectedAuthoredFloorBounds(int floorIndex)
        {
            var f = Room(); var floor = f.State.Floors[0];
            var definition = f.Production.Catalog.Floors.Single(value => value.FloorIndex == floorIndex);
            floor.FloorDefinitionId = definition.FloorDefinitionId; floor.FloorIndex = floorIndex;
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            var obj = new GameObject("GridBoundsTest"); var view = obj.AddComponent<DungeonFloorWorldView>();
            try
            {
                view.Initialize(policy); view.Render(floor, f.Production, f.Occupancy, f.Profile.Canonical.Spatial.MaximumMaterializedTiles, GuidanceLimits.MaximumMaterializedTiles, true);
                Assert.That(view.GridTileCount, Is.EqualTo(definition.Bounds.TileCount));
                Assert.That(view.Bounds, Is.EqualTo(new Rect(definition.Bounds.Minimum.X, definition.Bounds.Minimum.Y, definition.Bounds.Width, definition.Bounds.Height)));
                var grid = obj.transform.Find("EditorGrid").GetComponent<UnityEngine.Tilemaps.Tilemap>();
                for (int x = definition.Bounds.Minimum.X; x < definition.Bounds.Minimum.X + definition.Bounds.Width; x++)
                    for (int y = definition.Bounds.Minimum.Y; y < definition.Bounds.Minimum.Y + definition.Bounds.Height; y++)
                        Assert.That(grid.HasTile(new Vector3Int(x, y, 0)), Is.True);
                Assert.That(grid.HasTile(new Vector3Int(definition.Bounds.Minimum.X - 1, definition.Bounds.Minimum.Y, 0)), Is.False);
                view.Render(floor, f.Production, f.Occupancy, f.Profile.Canonical.Spatial.MaximumMaterializedTiles, GuidanceLimits.MaximumMaterializedTiles, false);
                Assert.That(view.GridVisible, Is.False); Assert.That(view.GridTileCount, Is.Zero);
                Assert.That(view.Bounds, Is.Not.EqualTo(view.LegalBounds));
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(policy); }
        }

        [Test]
        public void MoveGuidanceCustomGridBoundsAndWorkloadAreConfigurationOwned()
        {
            var f = Room(); var catalog = f.Production.Catalog;
            catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(-2, -3), 7, 9);
            var production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            var obj = new GameObject("CustomGridBoundsTest"); var view = obj.AddComponent<DungeonFloorWorldView>();
            try
            {
                view.Initialize(policy); view.Render(f.State.Floors[0], production, f.Occupancy, f.Profile.Canonical.Spatial.MaximumMaterializedTiles, 63, true);
                Assert.That(view.GridTileCount, Is.EqualTo(catalog.Floors[0].Bounds.TileCount)); Assert.That(view.Bounds, Is.EqualTo(new Rect(-2, -3, 7, 9)));
                Assert.Throws<InvalidOperationException>(() => view.Render(f.State.Floors[0], production, f.Occupancy, f.Profile.Canonical.Spatial.MaximumMaterializedTiles, 62, true));
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(policy); }
        }

        [Test]
        public void MoveGuidanceIsExactlyAuthoritativeOrderedAlternativesWithoutMutation()
        {
            var f = Room(); var ctx = Context(f); var floor = f.State.Floors[0]; var room = floor.Layout.Rooms[0];
            var fingerprint = ctx.Fingerprint(f.State); var disk = f.Session.GetCurrentBytes(); var wallet = f.Runtime.structureRuntime.ManaReserve;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var guidance = StructuralRenovationService.GetMovementGuidance(f.State, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits);
            timer.Stop(); TestContext.WriteLine("Configured guidance: " + guidance.ExaminedAnchorCount + " previews, " + timer.ElapsedMilliseconds + " ms");
            var bounds = f.Production.Catalog.Floors[0].Bounds;
            Assert.That(guidance.Reason, Is.Null); Assert.That(guidance.ExaminedAnchorCount, Is.EqualTo(bounds.TileCount - 1));
            CollectionAssert.AreEqual(guidance.ValidAnchors.OrderBy(value => value).ToArray(), guidance.ValidAnchors);
            CollectionAssert.AreEqual(guidance.ValidAnchors, StructuralRenovationService.GetMovementGuidance(f.State, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits).ValidAnchors);
            Assert.That(guidance.ValidAnchors, Does.Contain(new TileCoordinate(0, 3)));
            foreach (var invalid in new[] { room.Anchor, new TileCoordinate(0, 0), new TileCoordinate(-1, 3), new TileCoordinate(1, 3) })
                Assert.That(guidance.ValidAnchors.Contains(invalid), Is.False);
            for (int x = bounds.Minimum.X; x < bounds.Minimum.X + bounds.Width; x++)
                for (int y = bounds.Minimum.Y; y < bounds.Minimum.Y + bounds.Height; y++)
                {
                    var anchor = new TileCoordinate(x, y);
                    var preview = StructuralRenovationService.PreviewMovement(f.State, new StructuralMovementRequest { FloorInstanceId = floor.FloorInstanceId, RoomInstanceId = room.RoomInstanceId, Anchor = anchor }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                    Assert.That(guidance.ValidAnchors.Contains(anchor), Is.EqualTo(!anchor.Equals(room.Anchor) && preview.IsValid && ctx.Validate(preview.DetachedCandidate)), anchor.ToString());
                }
            Assert.That(ctx.Fingerprint(f.State), Is.EqualTo(fingerprint)); CollectionAssert.AreEqual(disk, f.Session.GetCurrentBytes());
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet));
        }

        [Test]
        public void MoveGuidanceUsesLastValidDraftProjectionAfterChangesAndInvalidIntent()
        {
            var f = Room(); var ctx = Context(f); var floor = f.State.Floors[0]; var room = floor.Layout.Rooms[0]; var store = new Store();
            var d = TransactionalDungeonDraft.Create(f.State, ctx, store); Move(d, f, 0, 3); Flush(d);
            var valid = StructuralRenovationService.GetMovementGuidance(d.ReadModel, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits);
            Assert.That(valid.ValidAnchors, Does.Contain(room.Anchor)); Assert.That(valid.ValidAnchors.Contains(new TileCoordinate(0, 3)), Is.False);
            Move(d, f, -1, -1); Flush(d); var evidence = store.Read(); var commands = d.CommandCount;
            var invalid = StructuralRenovationService.GetMovementGuidance(d.ReadModel, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits);
            CollectionAssert.AreEqual(valid.ValidAnchors, invalid.ValidAnchors); CollectionAssert.AreEqual(evidence, store.Read()); Assert.That(d.CommandCount, Is.EqualTo(commands));
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, ctx, store, out var reason); Assert.That(recovered, Is.Not.Null, reason);
            CollectionAssert.AreEqual(valid.ValidAnchors, StructuralRenovationService.GetMovementGuidance(recovered.ReadModel, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits).ValidAnchors);
            Assert.That(recovered.MoveRoom(floor.FloorInstanceId, room.RoomInstanceId, invalid.ValidAnchors[0]), Is.True); Flush(recovered);
            Assert.That(recovered.IsStructurallyValid, Is.True);
        }

        [Test]
        public void MoveGuidanceNoAlternativeHasEmptyResultAndBoundedWork()
        {
            var f = Room(); var catalog = f.Production.Catalog;
            catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(0, 0), 4, 8); catalog.Floors[0].FinalFloorSpaceCapacity = 32;
            var production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var ctx = new DungeonDraftContext(production, f.Configuration, f.Occupancy, f.Profile, f.Compatibility);
            Assert.That(ctx.Validate(f.State), Is.True);
            var floor = f.State.Floors[0]; var room = floor.Layout.Rooms[0];
            var result = StructuralRenovationService.GetMovementGuidance(f.State, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits);
            Assert.That(result.Reason, Is.Null); Assert.That(result.ValidAnchors, Is.Empty); Assert.That(result.ExaminedAnchorCount, Is.EqualTo(31));
        }

        [Test]
        public void MoveGuidanceRefusesOverBudgetEnvelopeBeforeCandidateEnumeration()
        {
            var f = Room(); var catalog = f.Production.Catalog; var max = GuidanceLimits.MaximumMaterializedTiles;
            catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(0, 0), max, 2);
            var production = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var ctx = new DungeonDraftContext(production, f.Configuration, f.Occupancy, f.Profile, f.Compatibility);
            var floor = f.State.Floors[0]; var room = floor.Layout.Rooms[0];
            var result = StructuralRenovationService.GetMovementGuidance(f.State, floor.FloorInstanceId, room.RoomInstanceId, ctx, GuidanceLimits);
            Assert.That(result.ValidAnchors, Is.Empty); Assert.That(result.ExaminedAnchorCount, Is.Zero); Assert.That(result.Reason, Is.Not.Null);
        }

        internal static Fixture Room(string definition = "spatial.room.basic")
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content();
            if (definition != "spatial.room.basic")
            {
                if (definition == "spatial.room.large_chamber")
                {
                    var construction = StructuralEditService.Preview(f.State, new StructuralConstructionRequest { RoomDefinitionId = definition,
                        Anchor = new TileCoordinate(4, 1), Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                        f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                    Assert.That(construction.IsValid, Is.True, string.Join(",", construction.ReasonCodes)); return f.Rebase(construction.DetachedCandidate);
                }
                var p = StructuralRenovationService.PreviewReplacement(f.State, new StructuralReplacementRequest {
                    FloorInstanceId = f.State.Floors[0].FloorInstanceId,
                    RoomInstanceId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, RoomDefinitionId = definition },
                    f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
                Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes)); f = f.Rebase(p.DetachedCandidate);
            }
            return f;
        }
        internal static DungeonDraftContext Context(Fixture f) => PhaseSevenA4TransactionalEditorTests.Context(f);
        internal static bool Move(TransactionalDungeonDraft d, Fixture f, int x, int y, int room = 0) =>
            d.MoveRoom(f.State.Floors[0].FloorInstanceId, f.State.Floors[0].Layout.Rooms[room].RoomInstanceId, new TileCoordinate(x, y));
        private static StructuralInvestmentRecord[] Ledger(Fixture f) =>
            DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).Investment;
        private static StructuralEconomyPreview Price(TransactionalDungeonDraft d, Fixture f) => StructuralEconomyService.PreviewFinalMovement(
            f.State, d.ReadModel, Ledger(f), d.IsStructurallyValid ? d.NormalizedMovementTargets() : null,
            f.Runtime.structureRuntime.ManaReserve, f.Economy);
        private static void Flush(TransactionalDungeonDraft d) { while (d.Durability == DraftDurability.Pending) Assert.That(d.FlushNext(), Is.True); }

        [TestCase(false)][TestCase(true)]
        public void SuffixConsequencesAndIndependentTargetsUseUniqueDeterministicFinalBasis(bool reverse)
        {
            var f = Room();
            var p = StructuralEditService.Preview(f.State, new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(4, 2), Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(p.IsValid, Is.True, string.Join(",", p.ReasonCodes)); f = f.Rebase(p.DetachedCandidate);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            if (reverse) { Move(d, f, 5, 2, 1); Flush(d); }
            Move(d, f, 0, 3); Flush(d);
            Assert.That(d.IsStructurallyValid, Is.True, d.StructuralReason);
            if (!reverse)
            {
                Assert.That(Price(d, f).Cost, Is.EqualTo(10));
                Assert.That(Price(d, f).Investment.Count(r => r.RenovationMana > 0), Is.EqualTo(1));
                Move(d, f, 5, 3, 1); Flush(d);
            }
            Assert.That(d.IsStructurallyValid, Is.True, d.StructuralReason);
            CollectionAssert.AreEqual(f.State.Floors[0].Layout.Rooms.Select(r => r.RoomInstanceId).OrderBy(id => id, StringComparer.Ordinal), d.NormalizedMovementTargets());
            Assert.That(Price(d, f).Cost, Is.EqualTo(20));
            Assert.That(Price(d, f).Investment.Count(r => r.RenovationMana > 0), Is.EqualTo(2));
            var expected = StructuralRenovationService.PreviewMovement(f.State, new StructuralMovementRequest {
                RoomInstanceId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, Anchor = new TileCoordinate(0, 3) },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical).DetachedCandidate;
            expected = StructuralRenovationService.PreviewMovement(expected, new StructuralMovementRequest {
                RoomInstanceId = f.State.Floors[0].Layout.Rooms[1].RoomInstanceId, Anchor = new TileCoordinate(5, 3) },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical).DetachedCandidate;
            Assert.That(Context(f).Fingerprint(d.ReadModel), Is.EqualTo(Context(f).Fingerprint(expected)));
            double wallet = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d)); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet - 20));
            Assert.That(Ledger(f).Count(r => r.RenovationMana > 0), Is.EqualTo(2));
        }
        [Test]
        public void ActualA4V2RecordsRecoverAndUpgradeOnlyThroughExplicitNewRecord()
        {
            var f = Room(); string id = "0123456789abcdef0123456789abcdef"; string predecessor = string.Empty;
            var assignment = f.State.Floors[0].RoomContents.Assignments[0]; string baseline = Context(f).Fingerprint(f.State);
            for (int sequence = 0; sequence <= 1; sequence++)
            {
                var candidate = new DungeonDraftV1Candidate { Version = 1, DraftId = id, Baseline = baseline, Sequence = sequence,
                    PredecessorCommit = predecessor, Commands = sequence == 0 ? Array.Empty<DungeonDraftV2Command>() : new[] {
                        new DungeonDraftV2Command { Sequence = 1, FloorInstanceId = f.State.Floors[0].FloorInstanceId,
                            RoomInstanceId = assignment.RoomInstanceId, AssignmentId = assignment.AssignmentId, Target = new TileCoordinate(1, 1) } } };
                byte[] payload = DungeonDraftFormat.Bytes(candidate);
                var commit = new DungeonDraftV1Commit { Version = 1, DraftId = id, Baseline = baseline, Sequence = sequence,
                    PredecessorCommit = predecessor, CandidateHash = SpatialContractSha256.Compute(payload) };
                byte[] receipt = DungeonDraftFormat.Bytes(commit); predecessor = SpatialContractSha256.Compute(receipt);
                string stem = f.ActivePath + ".editor-draft.draft-" + id + "." + sequence.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
                f.FileSystem.Seed(stem + ".candidate", payload); f.FileSystem.Seed(stem + ".commit", receipt);
            }
            var store = new FileDungeonDraftStore(f.ActivePath + ".editor-draft", f.FileSystem, f.Profile);
            byte[] oldBytes = store.Read(); Assert.That(Encoding.UTF8.GetString(oldBytes), Does.StartWith("{\"FormatVersion\":2,"));
            var d = TransactionalDungeonDraft.Recover(oldBytes, f.State, Context(f), store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(d.AcknowledgedSequence, Is.EqualTo(1));
            Assert.That(d.ReadModel.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(1, 1)));
            string legacyCandidatePath = f.ActivePath + ".editor-draft.draft-" + id + ".000001.candidate";
            byte[] legacyCandidate = f.FileSystem.ReadAllBytes(legacyCandidatePath);
            Move(d, f, 20, 20); Flush(d); Assert.That(d.StructuralReason, Is.EqualTo(StructuralEditService.OutOfBoundsReason));
            Assert.That(Encoding.UTF8.GetString(store.Read()), Does.StartWith("{\"FormatVersion\":3,"));
            CollectionAssert.AreEqual(legacyCandidate, f.FileSystem.ReadAllBytes(legacyCandidatePath));
            d = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out reason);
            Assert.That(reason, Is.Null); Assert.That(d.CommandCount, Is.EqualTo(2)); Assert.That(d.InvalidMovements.Length, Is.EqualTo(1));
        }
        [TestCase("candidate-before", 0)][TestCase("candidate-partial", 0)][TestCase("candidate-after", 0)][TestCase("candidate-barrier", 0)]
        [TestCase("commit-before", 0)][TestCase("commit-after", 1)][TestCase("commit-barrier", 1)][TestCase("commit-readback", 1)]
        public void InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement(string boundary, int recoveredCount)
        {
            var f = Room(); var store = new FileDungeonDraftStore(f.ActivePath + ".editor-draft", f.FileSystem, f.Profile);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), store); var before = f.Session.GetCurrentBytes();
            Move(d, f, 20, 20);
            switch (boundary)
            {
                case "candidate-partial": f.FileSystem.EnablePartialWriteFailure(8); break;
                case "candidate-before": case "candidate-after": f.FileSystem.EnableTargetedFailure(Operation.Write, paths => paths[0].EndsWith(".000001.candidate"), 1, boundary.EndsWith("after")); break;
                case "candidate-barrier": f.FileSystem.EnableFailure(Operation.Flush, 1); break;
                case "commit-before": case "commit-after": f.FileSystem.EnableTargetedFailure(Operation.Write, paths => paths[0].EndsWith(".000001.commit"), 1, boundary.EndsWith("after")); break;
                case "commit-barrier": f.FileSystem.EnableFailure(Operation.Flush, 2); break;
                case "commit-readback": f.FileSystem.EnableTargetedFailure(Operation.Read, paths => paths[0].EndsWith(".000001.commit"), 1, false); break;
            }
            Assert.That(d.FlushNext(), Is.False); Assert.That(d.Durability, Is.EqualTo(DraftDurability.Unknown));
            Assert.That(d.AcknowledgedSequence, Is.Zero); Assert.That(d.CanSave, Is.False); Assert.That(Move(d, f, 0, 3), Is.False);
            f.FileSystem.DisableFailure();
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(recovered.AcknowledgedSequence, Is.EqualTo(recoveredCount));
            Assert.That(recovered.InvalidMovements.Length, Is.EqualTo(recoveredCount));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.Zero);
        }
        [Test]
        public void ExpandedPrefixBudgetRefusesBeforeMutationAndKeepsAcknowledgedEvidenceRecoverable()
        {
            var f = Room(); string probePath = f.ActivePath + ".budget-probe";
            var probe = new FileDungeonDraftStore(probePath, f.FileSystem, f.Profile);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), probe);
            Move(draft, f, 0, 3); Flush(draft);
            int bytes = f.FileSystem.Paths.Where(p => p.StartsWith(probePath, StringComparison.Ordinal))
                .Sum(p => f.FileSystem.ReadAllBytes(p).Length) - 1;
            var old = f.Profile.Raw;
            var raw = new RawSavePayloadClassificationLimits(bytes, old.MaximumNestingDepth, old.MaximumObjectMembers,
                old.MaximumArrayElements, old.MaximumStringBytes, old.MaximumScanWork);
            var whole = new DetachedWholeSaveLimits(f.Profile.Whole.MaximumCandidateBytes,
                Math.Min(bytes, f.Profile.Whole.MaximumCopiedValueBytes), f.Profile.Whole.MaximumUnknownMembers,
                Math.Min(bytes, f.Profile.Whole.MaximumUnknownMemberBytes));
            Assert.That(raw.IsValid && whole.IsValid, Is.True);
            var profile = new SaveSpatialMigrationLimitsProfile(raw, f.Profile.Canonical, whole);
            var context = new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, profile, f.Compatibility);
            string path = f.ActivePath + ".bounded-draft";
            var store = new FileDungeonDraftStore(path, f.FileSystem, profile);
            draft = TransactionalDungeonDraft.Create(f.State, context, store);
            byte[] acknowledged = store.Read(); var paths = f.FileSystem.Paths.ToArray();
            Move(draft, f, 0, 3); Assert.That(draft.FlushNext(), Is.False);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Failed)); Assert.That(store.OutcomeUnknown, Is.False);
            CollectionAssert.AreEqual(paths, f.FileSystem.Paths.ToArray()); CollectionAssert.AreEqual(acknowledged, store.Read());
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(recovered.AcknowledgedSequence, Is.Zero);
            Assert.That(recovered.Discard(), Is.True);
        }

        [TestCase("context")][TestCase("downgrade")][TestCase("duplicate")]
        public void ExpandedRecordBindingsRejectContradictionWithoutCanonicalPublication(string corruption)
        {
            var f = Room(); string path = f.ActivePath + ".editor-draft";
            var store = new FileDungeonDraftStore(path, f.FileSystem, f.Profile);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(d, f, 0, 3); Flush(d); Move(d, f, 0, 4); Flush(d);
            string commitPath = f.FileSystem.Paths.Single(p => p.StartsWith(path, StringComparison.Ordinal) && p.EndsWith(".000002.commit"));
            string candidatePath = commitPath.Substring(0, commitPath.Length - "commit".Length) + "candidate";
            var commit = DungeonDraftFormat.Exact<DungeonDraftCommitRecord>(f.FileSystem.ReadAllBytes(commitPath));
            var candidate = DungeonDraftFormat.Exact<DungeonDraftV2Candidate>(f.FileSystem.ReadAllBytes(candidatePath));
            Assert.That(commit.Version, Is.EqualTo(2));
            if (corruption == "context") candidate.RuleIdentity = commit.RuleIdentity = new string('0', 64);
            if (corruption == "downgrade")
            { candidate.JournalFormatVersion = commit.JournalFormatVersion = 2; candidate.RuleIdentity = commit.RuleIdentity = null; }
            if (corruption != "duplicate")
            {
                byte[] payload = DungeonDraftFormat.Bytes(candidate);
                f.FileSystem.Seed(candidatePath, payload); commit.CandidateHash = SpatialContractSha256.Compute(payload);
            }
            string receipt = JsonUtility.ToJson(commit);
            if (corruption == "duplicate") receipt = receipt.Replace("\"Version\":2", "\"Version\":2,\"Version\":2");
            f.FileSystem.Seed(commitPath, Encoding.UTF8.GetBytes(receipt));
            var before = f.Session.GetCurrentBytes(); double mana = f.Runtime.structureRuntime.ManaReserve;
            Assert.That(Assert.Throws<IOException>(() => store.Read()).Message, Is.EqualTo(TransactionalDungeonDraft.RecoveryFailedReason));
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Reason, Is.EqualTo(TransactionalDungeonDraft.RecoveryFailedReason));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.Zero);
        }

        [Test]
        public void FailedInvalidPersistenceAllowsCorrectionButOnlyAcknowledgedPrefixCanSave()
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(d, f, 20, 20); store.FailWrite = true; Assert.That(d.FlushNext(), Is.False);
            Assert.That(d.Durability, Is.EqualTo(DraftDurability.Failed)); Move(d, f, 0, 3); Assert.That(d.CanSave, Is.False);
            Assert.That(d.AcknowledgedSequence, Is.Zero); store.FailWrite = false; Assert.That(d.FlushNext(), Is.True);
            Assert.That(d.CanSave, Is.False); Assert.That(d.FlushNext(), Is.True); Assert.That(d.CanSave, Is.True);
        }
        [Test]
        public void DraftAndDiscardPreserveKnowledgeLifecycleWalletAndActiveRunWhileCommitAffectsOnlyLaterRun()
        {
            var f = Room(); var old = PhaseSixA4Tests.Snapshot(f); var before = f.Session.GetCurrentBytes();
            var spatial = JsonUtility.ToJson(old.Floors[0].InspectSpatialInputs());
            var corridor = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(before, f.Context).CorridorContent;
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State, corridor, f.Profile.Canonical,
                f.State.Floors[0].FloorInstanceId, out var fingerprint), Is.True);
            var record = new FloorKnowledgeRecord { FloorInstanceId = f.State.Floors[0].FloorInstanceId, ApplicabilityFingerprint = fingerprint };
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            Move(d, f, 0, 3); Flush(d); Move(d, f, 20, 20); Flush(d);
            Assert.That(FloorKnowledgeApplicability.IsApplicable(record, f.State, corridor, f.Profile.Canonical), Is.True);
            Assert.That(JsonUtility.ToJson(old.Floors[0].InspectSpatialInputs()), Is.EqualTo(spatial));
            Assert.That(d.Discard(), Is.True); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Move(d, f, 0, 3); Flush(d);
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d));
            Assert.That(FloorKnowledgeApplicability.IsApplicable(record, f.State, corridor, f.Profile.Canonical), Is.False);
            Assert.That(JsonUtility.ToJson(old.Floors[0].InspectSpatialInputs()), Is.EqualTo(spatial));
            Assert.That(JsonUtility.ToJson(PhaseSixA4Tests.Snapshot(f).Floors[0].InspectSpatialInputs()), Is.Not.EqualTo(spatial));
        }

        [Test]
        public void DiscriminatedPayloadRoundTripsWithoutHiddenFieldMeanings()
        {
            var c = new DungeonDraftCommand { Sequence = 1, Kind = DungeonDraftCommandKind.RoomMovement,
                RoomMovements = new[] { new StructuralMovementRequest { FloorInstanceId = "test.floor", RoomInstanceId = "test.room", Anchor = new TileCoordinate(3, 4) } } };
            var copy = c.Copy(); Assert.That(copy.HasExactPayload, Is.True, JsonUtility.ToJson(copy));
            Assert.That(copy.RoomMovement.Anchor, Is.EqualTo(c.RoomMovement.Anchor));
        }
        [TestCase("spatial.room.basic", 300, 0.25, 75)]
        [TestCase("spatial.room.rectangle", 320, 0.25, 80)]
        [TestCase("spatial.room.large_chamber", 400, 0.25, 100)]
        public void ValidDraftUsesInjectedRoomPriceAndMovementFactorWithoutCanonicalMutation(string definition, double price, double factor, double expected)
        {
            var f = Room(definition); var config = JsonUtility.FromJson<StructuralEconomyConfiguration>(File.ReadAllText("Assets/_Project/Resources/structural_economy.json"));
            config.Rooms.Single(r => r.DefinitionId == definition).Mana = price; config.MovementFactor = factor;
            Assert.That(StructuralEconomySnapshot.TryCreate(config, f.Production.Catalog, out f.Economy), Is.True);
            var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store); byte[] before = f.Session.GetCurrentBytes();
            var target = f.State.Floors[0].Layout.Rooms.Single(r => r.RoomDefinitionId == definition);
            var anchor = definition == "spatial.room.large_chamber" ? new TileCoordinate(target.Anchor.X + 1, target.Anchor.Y) : new TileCoordinate(target.Anchor.X, target.Anchor.Y + 1);
            Assert.That(d.MoveRoom(f.State.Floors[0].FloorInstanceId, target.RoomInstanceId, anchor), Is.True); Assert.That(d.IsStructurallyValid, Is.True, d.StructuralReason);
            Assert.That(d.CanSave, Is.False); Flush(d); Assert.That(d.CanSave, Is.True);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(Price(recovered, f).Cost, Is.EqualTo(expected));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.Zero);
            double wallet = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, recovered)); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(wallet - expected));
            Assert.That(Ledger(f).Single(r => r.StructureId == target.RoomInstanceId).RenovationMana, Is.EqualTo(expected));
        }
        [TestCase(0)][TestCase(9)][TestCase(10)]
        public void CurrentWalletExactBalanceAndInsufficientCommitAreAtomic(double mana)
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            f.Runtime.structureRuntime.ManaReserve = mana;
            Assert.That(Move(d, f, 0, 3), Is.True); Flush(d); Assert.That(d.CanSave, Is.True);
            byte[] before = f.Session.GetCurrentBytes(); var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.EqualTo(mana == 10), result.Reason);
            if (result.IsSuccess) { f.Accept(result); f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.Zero);
                Assert.That(Ledger(f).Single(r => r.StructureId == f.State.Floors[0].Layout.Rooms[0].RoomInstanceId).RenovationMana, Is.EqualTo(10)); }
            else { CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
                Assert.That(d.CanSave, Is.True); Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.Zero); }
        }
        [TestCase(false)][TestCase(true)]
        public void ExperimentationPricesOnlyFinalTargetAndOriginRestoresZeroInvestment(bool origin)
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(d, f, 0, 3); Flush(d); Move(d, f, 20, 20); Flush(d);
            Assert.That(d.IsStructurallyValid, Is.False); Assert.That(d.CanSave, Is.False);
            Move(d, f, 0, 4); Flush(d); if (origin) { Move(d, f, 0, 2); Flush(d); }
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(recovered.CommandCount, Is.EqualTo(origin ? 4 : 3));
            var p = Price(recovered, f); Assert.That(p.Cost, Is.EqualTo(origin ? 0 : 10));
            Assert.That(p.Investment.Sum(r => r.RenovationMana), Is.EqualTo(p.Cost));
            Assert.That(recovered.HasChanges, Is.EqualTo(!origin));
        }
        [TestCase(20, 20, StructuralEditService.OutOfBoundsReason)]
        [TestCase(0, 0, StructuralEditService.FixedOverlapReason)]
        public void InvalidIntentAcknowledgesRecoversAndCorrectsWithExactStableReason(int x, int y, string expected)
        {
            var f = Room(); var store = new FileDungeonDraftStore(f.ActivePath + ".editor-draft", f.FileSystem, f.Profile);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), store); byte[] before = f.Session.GetCurrentBytes();
            Assert.That(Move(d, f, x, y), Is.True); Assert.That(d.HasChanges, Is.True); Assert.That(d.CanSave, Is.False);
            Assert.That(d.StructuralReason, Is.EqualTo(expected)); Flush(d);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                d = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason);
                Assert.That(reason, Is.Null); Assert.That(d.InvalidMovements.Single().RequestedAnchor, Is.EqualTo(new TileCoordinate(x, y)));
                Assert.That(d.StructuralReason, Is.EqualTo(expected)); Assert.That(d.CanSave, Is.False);
                Assert.That(Context(f).Fingerprint(d.ReadModel), Is.EqualTo(Context(f).Fingerprint(f.State)));
            }
            Assert.That(Move(d, f, 0, 3), Is.True); Assert.That(d.CanSave, Is.False); Flush(d); Assert.That(d.CanSave, Is.True);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void MixedContentAndStructuralHistoryPreservesExactIdentityAndRoomLocalArrangement()
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            var old = f.State.Floors[0].RoomContents.Assignments[0];
            Assert.That(d.Move(f.State.Floors[0].FloorInstanceId, old.RoomInstanceId, old.AssignmentId, new TileCoordinate(1, 1)), Is.True);
            Flush(d); Move(d, f, 0, 3); Flush(d); Move(d, f, 20, 20); Flush(d); Move(d, f, 0, 4); Flush(d);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(Context(f).Fingerprint(recovered.ReadModel), Is.EqualTo(Context(f).Fingerprint(d.ReadModel)));
            var a = recovered.ReadModel.Floors[0].RoomContents.Assignments.Single();
            Assert.That(a.AssignmentId, Is.EqualTo(old.AssignmentId)); Assert.That(a.RoomInstanceId, Is.EqualTo(old.RoomInstanceId));
            Assert.That(a.CategoryId, Is.EqualTo(old.CategoryId)); Assert.That(a.OptionId, Is.EqualTo(old.OptionId));
            Assert.That(a.Sequence, Is.EqualTo(old.Sequence)); Assert.That(a.RoomLocalPosition, Is.EqualTo(new TileCoordinate(1, 1)));
            Assert.That(Price(recovered, f).Cost, Is.EqualTo(10));
        }
        [Test]
        public void MaterialContextDriftFailsClosedAndCanonicalStalenessRemainsDistinct()
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(d, f, 20, 20); Flush(d);
            var catalog = f.Production.Catalog; catalog.Corridors[0].MaximumLength++;
            var changed = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var context = new DungeonDraftContext(changed, f.Configuration, f.Occupancy, f.Profile, f.Compatibility);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.IncompatibleReason));
            var next = StructuralRenovationService.PreviewMovement(f.State, new StructuralMovementRequest {
                RoomInstanceId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, Anchor = new TileCoordinate(0, 3) },
                f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), next.DetachedCandidate, Context(f), store, out reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.StaleReason));
        }
        [TestCase(Operation.Write)][TestCase(Operation.Flush)][TestCase(Operation.Replace)][TestCase(Operation.Read)]
        public void CompleteSaveFailurePublishesNoGeometryWalletOrInvestment(Operation operation)
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(d, f, 0, 3); Flush(d); var before = f.Session.GetCurrentBytes(); double mana = f.Runtime.structureRuntime.ManaReserve;
            if (operation == Operation.Read) f.FileSystem.EnableTargetedFailure(operation, paths => paths[0] == f.ActivePath, 2, false);
            else f.FileSystem.EnableFailure(operation, 1);
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.IsSuccess, Is.False); f.FileSystem.DisableFailure();
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.Zero); Assert.That(d.CanSave, Is.True); Assert.That(store.Read(), Is.Not.Null);
        }

        [Test]
        public void CurrentEconomicConfigurationAndCurrentWalletAreRecalculatedAfterDrafting()
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Move(d, f, 0, 3); Flush(d);
            Assert.That(Price(d, f).Cost, Is.EqualTo(10));
            var config = JsonUtility.FromJson<StructuralEconomyConfiguration>(File.ReadAllText("Assets/_Project/Resources/structural_economy.json"));
            config.Rooms.Single(r => r.DefinitionId == "spatial.room.basic").Mana = 333; config.MovementFactor = 0.5;
            Assert.That(StructuralEconomySnapshot.TryCreate(config, f.Production.Catalog, out f.Economy), Is.True);
            f.Runtime.structureRuntime.ManaReserve = 167;
            Assert.That(Price(d, f).Cost, Is.EqualTo(167));
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.Zero); Assert.That(Ledger(f).Sum(r => r.RenovationMana), Is.EqualTo(167));
        }

        [Test]
        public void MovingRoomPreservesRepresentativeMonsterTrapAndLootExactlyThroughCompleteSave()
        {
            var f = Room(); var floor = f.State.Floors[0]; string room = floor.Layout.Rooms[0].RoomInstanceId;
            f = f.Rebase(f.Prepare(DetachedCanonicalMutationRequest.Place("placement.category.trap", "placement.option.trap.spike",
                room, floor.FloorInstanceId, new TileCoordinate(1, 0))).State);
            f = f.Rebase(f.Prepare(DetachedCanonicalMutationRequest.Place("placement.category.loot_node", "placement.option.loot_node.basic",
                room, floor.FloorInstanceId, new TileCoordinate(2, 0))).State);
            var before = JsonUtility.ToJson(f.State.Floors[0].RoomContents);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Move(d, f, 0, 3); Flush(d);
            Assert.That(JsonUtility.ToJson(d.ReadModel.Floors[0].RoomContents), Is.EqualTo(before));
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d)); f.Reopen();
            Assert.That(JsonUtility.ToJson(f.State.Floors[0].RoomContents), Is.EqualTo(before));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents, Is.Empty);
        }

        [Test]
        public void InvalidOneFloorBlocksWholeDungeonAndDiscardPreservesRecognizedState()
        {
            var f = PhaseSixA4Tests.Eligible(); var before = f.Session.GetCurrentBytes(); string runtime = JsonUtility.ToJson(f.Runtime);
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            foreach (var floor in f.State.Floors.Where(v => v.Layout.Rooms.Length != 0))
            {
                var room = floor.Layout.Rooms[0]; Assert.That(d.MoveRoom(floor.FloorInstanceId, room.RoomInstanceId,
                    new TileCoordinate(room.Anchor.X, room.Anchor.Y + 1)), Is.True); Flush(d);
            }
            var target = f.State.Floors[1]; Assert.That(d.MoveRoom(target.FloorInstanceId, target.Layout.Rooms[0].RoomInstanceId, new TileCoordinate(20, 20)), Is.True); Flush(d);
            Assert.That(d.InvalidMovements.Any(v => v.FloorInstanceId == target.FloorInstanceId), Is.True);
            Assert.That(d.FloorChanged(target.FloorInstanceId), Is.True); Assert.That(d.CanSave, Is.False);
            Assert.That(PhaseSixA4Tests.Writer(f).CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d).IsSuccess, Is.False);
            Assert.That(d.Discard(), Is.True); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(runtime));
        }

        [TestCase("bounds")][TestCase("sockets")][TestCase("orientation")][TestCase("capacity")]
        [TestCase("occupancy")][TestCase("options")][TestCase("limits")]
        public void EveryMaterialValidationInputIsBoundToRecoveryAndCommit(string changedInput)
        {
            var f = Room(); var store = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), store); Move(d, f, 0, 3); Flush(d);
            var production = f.Production; var occupancy = f.Occupancy; var profile = f.Profile;
            var config = JsonUtility.FromJson<RunSimulationConfig>(JsonUtility.ToJson(f.Configuration)); var catalog = production.Catalog;
            switch (changedInput)
            {
                case "bounds": catalog.Floors[0].Bounds = new RectangularFloorBounds(new TileCoordinate(0, 0), 1, 1); break;
                case "sockets": catalog.SocketTypes[0].CompatibleSocketTypeIds = Array.Empty<string>(); break;
                case "orientation": catalog.Rooms[0].AllowedOrientations = Array.Empty<CardinalOrientation>(); break;
                case "capacity": catalog.Floors[0].FinalFloorSpaceCapacity--; break;
                case "occupancy":
                    var value = JsonUtility.FromJson<RoomContentSpatialOccupancyConfiguration>(JsonUtility.ToJson(occupancy.Value));
                    value.Records[0].OccupiedTileOffsets = new[] { new TileCoordinate(0, 0), new TileCoordinate(1, 0) };
                    occupancy = new RoomContentSpatialOccupancySnapshot(value, occupancy.MaximumValidationMaterializedTiles); break;
                case "options": config.MvpPlacementEffects = config.MvpPlacementEffects.Skip(1).ToArray(); break;
                case "limits": profile = new SaveSpatialMigrationLimitsProfile(f.Profile.Raw,
                    new CanonicalSpatialSerializationLimits(f.Profile.Canonical.Serialized, new CanonicalSpatialSaveWorkloadLimits(
                        f.Profile.Canonical.Spatial.MaximumRecords, f.Profile.Canonical.Spatial.MaximumMaterializedTiles - 1)), f.Profile.Whole); break;
            }
            production = new ProductionSpatialContentSnapshot(production.Manifest, catalog, production.Languages);
            var current = new DungeonDraftContext(production, config, occupancy, profile, f.Compatibility);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, current, store, out var reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.IncompatibleReason));
            var context = new DetachedCurrentTargetValidationContext(f.Compatibility, production,
                LegacyGameplayConfigurationContract.SerializeCanonical(config), profile.Canonical, occupancy);
            var authority = new DetachedCanonicalWriteAuthority(production, f.Compatibility, config, context, profile, f.RemovalPolicy, f.Economy);
            var before = f.Session.GetCurrentBytes(); var result = authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d);
            Assert.That(result.Reason, Is.EqualTo(TransactionalDungeonDraft.IncompatibleReason)); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        [Test]
        public void BootstrapMovementIsDevelopmentOnlyWhileReplacementAndConstructionRemain()
        {
            var f = Room(); var rootObject = new GameObject("A5BootstrapParity"); var overlayObject = new GameObject("A5BootstrapOverlay");
            try
            {
                var root = rootObject.AddComponent<GameRoot>(); var overlay = overlayObject.AddComponent<BootstrapOverlay>();
                typeof(GameRoot).GetProperty("Save").SetValue(root, f.Runtime); overlay.Bind(root);
                Assert.That(overlay.StructuralRenovationControlsAvailable, Is.True); Assert.That(overlay.StructuralMovementControlsAvailable, Is.False);
                Assert.That(overlay.PreviewStructuralMovement().IsValid, Is.False);
                typeof(GameRoot).GetProperty("DevPanelEnabled").SetValue(root, true);
                Assert.That(overlay.StructuralMovementControlsAvailable, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(overlayObject); UnityEngine.Object.DestroyImmediate(rootObject); }
        }
    }
}
#endif
