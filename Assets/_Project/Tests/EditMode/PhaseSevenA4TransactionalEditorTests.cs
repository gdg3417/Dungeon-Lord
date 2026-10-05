#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSevenA4TransactionalEditorTests
    {
        internal sealed class Store : IDungeonDraftStore
        {
            public bool OutcomeUnknown => false;
            internal byte[] Bytes;
            internal bool FailWrite, FailDelete;
            public byte[] Read() => Bytes == null ? null : (byte[])Bytes.Clone();
            public bool Write(byte[] expected, byte[] next)
            { if (FailWrite || !(expected == null ? Bytes == null : Bytes != null && expected.SequenceEqual(Bytes))) return false;
              Bytes = (byte[])next.Clone(); return true; }
            public bool Delete(byte[] expected) { if (FailDelete) return false; Bytes = null; return true; }
        }
        internal static Fixture Content(string category = CanonicalSpatialSaveContracts.MonsterCategoryId,
            string option = "placement.option.monster.skeleton")
        {
            var f = Fixture.Create(null);
            var room = f.Prepare(DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic", null, null, null));
            Assert.That(room.IsSuccess, Is.True, room.Reason);
            f = f.Rebase(room.State);
            var placed = f.Prepare(DetachedCanonicalMutationRequest.Place(category, option,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, f.State.Floors[0].FloorInstanceId, new TileCoordinate(0, 0)));
            Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            return f.Rebase(placed.State);
        }
        internal static DungeonDraftContext Context(Fixture f) => new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile);
        internal static bool Move(TransactionalDungeonDraft draft, Fixture f, int x, int y) => draft.Move(
            f.State.Floors[0].FloorInstanceId, f.State.Floors[0].RoomContents.Assignments[0].RoomInstanceId,
            f.State.Floors[0].RoomContents.Assignments[0].AssignmentId, new TileCoordinate(x, y));

        [TestCase("placement.category.monster", "placement.option.monster.skeleton")]
        [TestCase("placement.category.trap", "placement.option.trap.spike")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.basic")]
        public void RepositionPreservesIdentityWalletCanonicalBytesAndReopensExactPosition(string category, string option)
        {
            var f = Content(category, option); var store = new Store();
            byte[] before = f.Session.GetCurrentBytes(); double mana = f.Runtime.structureRuntime.ManaReserve;
            var original = f.State.Floors[0].RoomContents.Assignments[0];
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Assert.That(Move(draft, f, 1, 1), Is.True);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Pending)); Assert.That(draft.CanSave, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(draft.ReadModel.Floors[0].RoomContents.Assignments[0].RoomInstanceId, Is.EqualTo(original.RoomInstanceId));
            Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.CanSave, Is.True);
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft);
            f.Accept(result); f.Reopen();
            var final = f.State.Floors[0].RoomContents.Assignments[0];
            Assert.That(final.RoomLocalPosition, Is.EqualTo(new TileCoordinate(1, 1)));
            Assert.That(final.AssignmentId, Is.EqualTo(original.AssignmentId)); Assert.That(final.CategoryId, Is.EqualTo(original.CategoryId));
            Assert.That(final.OptionId, Is.EqualTo(original.OptionId)); Assert.That(final.Sequence, Is.EqualTo(original.Sequence));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(13)); Assert.That(store.Bytes, Is.Null);
        }

        [Test]
        public void OrderedAcknowledgedPrefixRecoversWithoutPendingSuffixOrAutomaticCommit()
        {
            var f = Content(); var store = new Store(); var context = Context(f);
            var draft = TransactionalDungeonDraft.Create(f.State, context, store);
            Assert.That(Move(draft, f, 1, 1), Is.True); Assert.That(draft.FlushNext(), Is.True);
            Assert.That(Move(draft, f, 2, 1), Is.True); Assert.That(Move(draft, f, 3, 1), Is.True);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason);
            Assert.That(reason, Is.Null); Assert.That(recovered.AcknowledgedSequence, Is.EqualTo(1));
            Assert.That(recovered.ReadModel.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(1, 1)));
            Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.AcknowledgedSequence, Is.EqualTo(2));
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Pending)); Assert.That(draft.CanSave, Is.False);
            Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.AcknowledgedSequence, Is.EqualTo(3));
            var final = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out _);
            Assert.That(final.ReadModel.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(3, 1)));
            Assert.That(f.State.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(0, 0)));
        }
        [Test]
        public void FailedPredecessorBlocksSaveAndLaterCommandsUntilRetry()
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); store.FailWrite = true;
            Assert.That(draft.FlushNext(), Is.False); Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Failed));
            Assert.That(Move(draft, f, 2, 1), Is.True); Assert.That(draft.CanSave, Is.False);
            Assert.That(draft.AcknowledgedSequence, Is.Zero);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out _).CommandCount, Is.Zero);
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess, Is.False);
            store.FailWrite = false; Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.AcknowledgedSequence, Is.EqualTo(1));
            Assert.That(draft.CanSave, Is.False); Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.CanSave, Is.True);
        }
        [Test]
        public void OriginalReproduction_CandidateBarrierFailureNeverRecoversCandidateOrNeedsRollback()
        {
            var f = Content();
            byte[] canonicalBefore = f.FileSystem.ReadAllBytes(f.ActivePath);
            double manaBefore = f.Runtime.structureRuntime.ManaReserve;
            string draftPath = f.ActivePath + ".editor-draft";
            var store = new FileDungeonDraftStore(draftPath, f.FileSystem, f.Profile);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(Move(draft, f, 1, 1), Is.True);
            // Preserve the original compound fault setup. The corrected append-only protocol
            // never invokes replacement or rollback; this failure occurs before a commit exists.
            f.FileSystem.EnableFailureSequence(Operation.Flush, 1, Operation.Replace, 2);
            Assert.That(draft.FlushNext(), Is.False);
            Assert.That(draft.AcknowledgedSequence, Is.Zero);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Unknown));
            Assert.That(draft.CanSave, Is.False);
            f.FileSystem.DisableFailure();
            CollectionAssert.AreEqual(canonicalBefore, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(manaBefore));
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess,
                Is.False);
            CollectionAssert.AreEqual(canonicalBefore, f.FileSystem.ReadAllBytes(f.ActivePath));
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out _);
            Assert.That(recovered == null || recovered.AcknowledgedSequence == 0, Is.True,
                "INV-12: candidate bytes without commit evidence must not become recoverable commands.");
        }

        // Investigation evidence, not acceptance of the draft protocol. These probes show
        // that a receipt/marker operation's successful return is not reconstructible from
        // file names and bytes alone under the established filesystem failure contract.
        [TestCase(Operation.Write)]
        [TestCase(Operation.Replace)]
        [TestCase(Operation.Move)]
        [TestCase(Operation.Delete)]
        [TestCase(Operation.Flush)]
        public void DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Operation operation)
        {
            var success = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var failure = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            string directory = Path.GetFullPath(Path.Combine("Temp", "phase7a4-evidence-probe"));
            string receipt = Path.Combine(directory, "acknowledged.receipt");
            string staged = Path.Combine(directory, "staged.receipt");
            string pending = Path.Combine(directory, "pending.intent");
            byte[] oldReceipt = Encoding.UTF8.GetBytes("test-only acknowledged prefix 0");
            byte[] nextReceipt = Encoding.UTF8.GetBytes("test-only acknowledged prefix 1");
            foreach (var files in new[] { success, failure })
            {
                files.Seed(Path.Combine(directory, "predecessor.payload"), oldReceipt);
                files.Seed(Path.Combine(directory, "candidate.payload"), nextReceipt);
                files.Seed(pending, Encoding.UTF8.GetBytes("test-only attempt 1"));
                if (operation == Operation.Replace) files.Seed(receipt, oldReceipt);
                if (operation == Operation.Replace || operation == Operation.Move) files.Seed(staged, nextReceipt);
                if (operation == Operation.Delete || operation == Operation.Flush) files.Seed(receipt, nextReceipt);
            }
            Action<ISpatialMigrationFileSystem> finalTransition = files =>
            {
                switch (operation)
                {
                    case Operation.Write: files.WriteAllBytesDurable(receipt, nextReceipt); break;
                    case Operation.Replace: files.ReplaceSameDirectoryAtomic(staged, receipt); break;
                    case Operation.Move: files.MoveSameDirectoryAtomic(staged, receipt); break;
                    case Operation.Delete: files.DeleteFile(pending); break;
                    case Operation.Flush: files.FlushDirectory(directory); break;
                    default: throw new ArgumentOutOfRangeException(nameof(operation));
                }
            };
            Assert.DoesNotThrow(() => finalTransition(success));
            if (operation == Operation.Flush) failure.EnableFailure(operation, 1);
            else failure.EnableFailureAfterMutation(operation, 1);
            Assert.Throws<IOException>(() => finalTransition(failure));
            if (operation != Operation.Flush)
                Assert.That(failure.FailedOperation.FailedAfterMutation, Is.True);
            failure.DisableFailure();
            CollectionAssert.AreEqual(success.Paths.ToArray(), failure.Paths.ToArray());
            foreach (string path in success.Paths)
                CollectionAssert.AreEqual(success.ReadAllBytes(path), failure.ReadAllBytes(path), path);
            // Another read supplies no historical evidence that the original operation returned successfully.
            foreach (string path in success.Paths)
                CollectionAssert.AreEqual(success.ReadAllBytes(path), failure.ReadAllBytes(path), path);
        }
        [TestCase(-1, 0)] [TestCase(4, 0)] [TestCase(0, 4)] [TestCase(int.MaxValue, int.MinValue)]
        public void InvalidTargetDoesNotMutateDraftOrCanonical(int x, int y)
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            byte[] before = store.Read(); var state = Context(f).Fingerprint(draft.ReadModel);
            Assert.That(Move(draft, f, x, y), Is.False); Assert.That(draft.CommandCount, Is.Zero);
            Assert.That(Context(f).Fingerprint(draft.ReadModel), Is.EqualTo(state)); CollectionAssert.AreEqual(before, store.Read());
        }
        [Test]
        public void WrongRoomAndAssignmentIdsNeverReassignOrSubstitute()
        {
            var f = Content(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            Assert.That(draft.Move(f.State.Floors[0].FloorInstanceId, "wrong.room", "wrong.assignment", new TileCoordinate(1, 1)), Is.False);
            Assert.That(draft.CommandCount, Is.Zero);
        }
        [Test]
        public void OccupiedReservedAndConfiguredMultiTilePositionsFailClosed()
        {
            var f = Content(); var state = Context(f).Copy(f.State); var original = state.Floors[0].RoomContents.Assignments[0];
            state.Floors[0].RoomContents.Assignments = new[] { original, new RoomContentAssignment {
                AssignmentId = "test.assignment.second", RoomInstanceId = original.RoomInstanceId,
                CategoryId = original.CategoryId, OptionId = original.OptionId, Sequence = original.Sequence + 1,
                RoomLocalPosition = new TileCoordinate(2, 1) } };
            state.Floors[0].RoomContents.NextSequence = original.Sequence + 2;
            var draft = TransactionalDungeonDraft.Create(state, Context(f), new Store()); Assert.That(draft, Is.Not.Null);
            Assert.That(Move(draft, f, 2, 1), Is.False);
            var room = f.Production.Catalog.Rooms.Single(r => r.RoomDefinitionId == state.Floors[0].Layout.Rooms[0].RoomDefinitionId);
            // Production snapshot inspection returns copies; inject modified copies as synthetic test configuration.
            var catalog = f.Production.Catalog; catalog.Rooms.Single(r => r.RoomDefinitionId == room.RoomDefinitionId).ReservedTileOffsets = new[] { new TileCoordinate(1, 1) };
            var reservedProduction = new ProductionSpatialContentSnapshot(f.Production.Manifest, catalog, f.Production.Languages);
            var reservedContext = new DungeonDraftContext(reservedProduction, f.Configuration, f.Occupancy, f.Profile);
            var reservedDraft = TransactionalDungeonDraft.Create(f.State, reservedContext, new Store());
            Assert.That(reservedDraft, Is.Not.Null); Assert.That(Move(reservedDraft, f, 1, 1), Is.False);
            // Configure occupancy through the validated snapshot's internal test boundary.
            var rule = f.Occupancy.Value.Records.Single(r => r.OptionId == original.OptionId);
            rule.OccupiedTileOffsets = new[] { new TileCoordinate(0, 0), new TileCoordinate(1, 0) };
            draft = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Assert.That(draft, Is.Not.Null);
            Assert.That(Move(draft, f, 3, 1), Is.False); Assert.That(Move(draft, f, 2, 1), Is.True);
        }
        [Test]
        public void BaselineDeterminismRecoveryRejectsStaleCanonicalWithoutRebase()
        {
            var f = Content(); var context = Context(f); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, context, store);
            var copy = context.Copy(f.State); Array.Reverse(copy.Floors[0].Layout.Nodes); Array.Reverse(copy.Floors[0].Layout.Edges);
            Assert.That(context.Fingerprint(copy), Is.EqualTo(context.Fingerprint(f.State)));
            Move(draft, f, 1, 1); draft.FlushNext(); copy.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = new TileCoordinate(2, 1);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), copy, context, store, out string reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.StaleReason));
            f = f.Rebase(copy); Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).Reason,
                Is.EqualTo(TransactionalDungeonDraft.StaleReason));
        }
        [Test]
        public void DiscardFailureNeverReportsDurableDiscardAndNeverChangesCanonical()
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); draft.FlushNext(); byte[] before = f.Session.GetCurrentBytes(); store.FailDelete = true;
            Assert.That(draft.Discard(), Is.False); Assert.That(draft.IsClosed, Is.False); Assert.That(store.Read(), Is.Not.Null);
            store.FailDelete = false; Assert.That(draft.Discard(), Is.True); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void CommitCleanupFailureLeavesStaleDraftThatCannotReapply()
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); draft.FlushNext(); store.FailDelete = true;
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft));
            Assert.That(draft.IsClosed, Is.True); Assert.That(draft.Reason, Is.EqualTo(TransactionalDungeonDraft.DeleteFailedReason));
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out var reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.StaleReason));
        }
        [TestCase(Operation.Write)] [TestCase(Operation.Replace)] [TestCase(Operation.Flush)]
        public void CanonicalPersistenceFailureAppliesNothingAndRetainsDurableDraft(Operation operation)
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); draft.FlushNext(); byte[] before = f.Session.GetCurrentBytes(), journal = store.Read();
            f.FileSystem.EnableTargetedFailure(operation, paths => true, 1, false);
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft);
            Assert.That(result.IsSuccess, Is.False); Assert.That(draft.CanSave, Is.True);
            CollectionAssert.AreEqual(journal, store.Read()); CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void FinalValidationAndStaleSessionFailuresApplyNothing()
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); draft.FlushNext(); var before = f.Session.GetCurrentBytes();
            f.Occupancy.Value.Records.Single(r => r.OptionId == f.State.Floors[0].RoomContents.Assignments[0].OptionId)
                .OccupiedTileOffsets = new[] { new TileCoordinate(100, 0) };
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(store.Read(), Is.Not.Null);
        }
        [TestCase("version")] [TestCase("duplicate")] [TestCase("malformed")] [TestCase("oversized")] [TestCase("sequence")] [TestCase("target")]
        public void MalformedAndUnsupportedDraftsFailClosed(string kind)
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1, 1); draft.FlushNext(); string json = Encoding.UTF8.GetString(store.Read());
            byte[] bytes = kind == "oversized" ? new byte[f.Profile.Raw.MaximumInputBytes + 1] : Encoding.UTF8.GetBytes(
                kind == "version" ? json.Replace("\"FormatVersion\":2", "\"FormatVersion\":3") :
                kind == "duplicate" ? json.Replace("\"FormatVersion\":2", "\"FormatVersion\":2,\"FormatVersion\":2") :
                kind == "sequence" ? json.Replace("\"Sequence\":1", "\"Sequence\":2") :
                kind == "target" ? json.Replace("\"X\":1", "\"X\":99") : "{");
            Assert.That(TransactionalDungeonDraft.Recover(bytes, f.State, Context(f), store, out _), Is.Null);
        }
        [Test]
        public void MaximumCommandEnvelopeIsBoundedAndRecoverable()
        {
            var f = Content(); var store = new Store(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            for (int i = 0; i < f.Profile.Raw.MaximumArrayElements; i++)
            { Assert.That(Move(draft, f, i % 2 + 1, 1), Is.True); Assert.That(draft.FlushNext(), Is.True); }
            Assert.That(Move(draft, f, 3, 1), Is.False); Assert.That(draft.Reason, Is.EqualTo(TransactionalDungeonDraft.LimitReason));
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out _).CommandCount,
                Is.EqualTo(f.Profile.Raw.MaximumArrayElements));
        }
        [Test]
        public void NetUnchangedDraftHasNoChangedFloorOrEconomicCommit()
        {
            var f = Content(); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            Move(draft, f, 1, 1); draft.FlushNext(); Move(draft, f, 0, 0); draft.FlushNext();
            Assert.That(draft.HasChanges, Is.False); Assert.That(draft.CanSave, Is.False);
            Assert.That(draft.FloorChanged(f.State.Floors[0].FloorInstanceId), Is.False);
        }
        [Test]
        public void ActiveSnapshotIsolatedWhileLaterRunUsesCommittedPosition()
        {
            var f = Content(); var old = PhaseSixA4Tests.Snapshot(f); var engine = PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            string oldPlan = JsonUtility.ToJson(old.Floors[0].MaterializePlan());
            var oldRun = engine.SimulateSnapshot(1, old);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); Move(draft, f, 1, 1); draft.FlushNext();
            Assert.That(JsonUtility.ToJson(old.Floors[0].MaterializePlan()), Is.EqualTo(oldPlan));
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft));
            Assert.That(JsonUtility.ToJson(old.Floors[0].MaterializePlan()), Is.EqualTo(oldPlan));
            var later = PhaseSixA4Tests.Snapshot(f);
            Assert.That(later.Floors[0].MaterializePlan().RequiredRooms.Single().Room.Assignments.Single().RoomLocalPosition,
                Is.EqualTo(new TileCoordinate(1, 1)));
            Assert.That(JsonUtility.ToJson(later.Floors[0].MaterializePlan()), Is.Not.EqualTo(oldPlan));
            string[] Events(RunOutcomeRecord run) => run.SpatialEvents.Select(e => JsonUtility.ToJson(e)).ToArray();
            CollectionAssert.AreEqual(Events(oldRun), Events(engine.SimulateSnapshot(1, old)));
            Assert.That(engine.SimulateSnapshot(1, later).SpatialEvents, Is.Not.Empty);
        }
        [TestCase(DungeonTargetPlatform.Android, 320, 96)] [TestCase(DungeonTargetPlatform.IOS, 326, 88)]
        public void HitTargetsConvertPhysicalUnitsAndActualPanelScale(DungeonTargetPlatform platform, float dpi, float expected)
        {
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            Assert.That(policy.MinimumPixels(platform, dpi), Is.EqualTo(expected));
            Assert.That(policy.MinimumPanelUnits(platform, dpi, 2), Is.EqualTo(expected / 2)); UnityEngine.Object.DestroyImmediate(policy);
        }
        [Test]
        public void SafeAreaMappingZoomPanAndBlockedPinchArePureAndClamped()
        {
            Assert.That(DungeonPresentationPolicy.SafePanelRect(new Rect(20, 40, 360, 700), 800, 2), Is.EqualTo(new Rect(10, 30, 180, 350)));
            var policy = ScriptableObject.CreateInstance<DungeonPresentationPolicy>(); var viewport = new DungeonViewport(policy);
            viewport.Configure(new Rect(0, 0, 12, 12), new Rect(0, 0, 400, 800), true);
            Assert.That(viewport.ScreenToWorld(new Vector2(200, 400)), Is.EqualTo(new Vector2(6, 6)));
            viewport.Zoom(1000, new Vector2(200, 400)); Assert.That(viewport.Size, Is.GreaterThanOrEqualTo(viewport.FitSize / policy.MaximumZoomFactor));
            viewport.Pan(new Vector2(100000, -100000)); Assert.That(viewport.Center.x, Is.GreaterThanOrEqualTo(0));
            var gesture = new DungeonViewportGesture(); var center = viewport.Center; var size = viewport.Size;
            gesture.Begin(new Vector2(100, 100), true); gesture.Update(viewport, new Vector2(300, 300), new Vector2(350, 350), false, 8);
            Assert.That(gesture.End(out _), Is.False); Assert.That(viewport.Center, Is.EqualTo(center)); Assert.That(viewport.Size, Is.EqualTo(size));
            gesture.Begin(new Vector2(100, 100), false); gesture.Update(viewport, new Vector2(100, 100), new Vector2(200, 100), false, 8);
            gesture.Update(viewport, new Vector2(50, 100), new Vector2(250, 100), false, 8); Assert.That(gesture.End(out _), Is.False);
            viewport.Reset(); Assert.That(viewport.Center, Is.EqualTo(new Vector2(6, 6))); UnityEngine.Object.DestroyImmediate(policy);
        }
        [Test]
        public void LocalizedHudLongValuesAndProductionAssetsResolveWithoutMutatingState()
        {
            var f = Content(); var before = f.Session.GetCurrentBytes();
            var hud = ProductionDungeonPresenter.Hud(f.Runtime, f.Configuration, 120,
                key => key.StartsWith("ui.dungeon.hud") ? "A deliberately much longer localized label that wraps and expands across several lines: {0}" : "Localized heat",
                System.Globalization.CultureInfo.GetCultureInfo("ja-JP"));
            Assert.That(hud.TotalMana.Length, Is.GreaterThan(60)); Assert.That(hud.Heat, Does.Contain("Localized heat"));
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Project/UI/ProductionDungeon/Dungeon.uxml"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.StyleSheet>("Assets/_Project/UI/ProductionDungeon/Dungeon.uss"), Is.Not.Null);
        }
    }
}
#endif
