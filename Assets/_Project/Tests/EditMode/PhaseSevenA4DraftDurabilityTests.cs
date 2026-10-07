#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSevenA4DraftDurabilityTests
    {
        private static SaveService DeleteService(Fixture f)
        {
            byte[] legacy = (byte[])typeof(DetachedCurrentTargetValidationContext).GetField("legacyConfiguration",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(f.Context);
            var service = new SaveService(new SimpleLogger(false, (level, message) => { }),
                new SaveConfig { fileName = Path.GetFileName(f.ActivePath), useAtomicWrites = true }, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration, legacy, f.PositionProfiles, f.Occupancy);
            service.ConfigureStructuralEconomy(f.Economy); service.ConfigureContentAcquisitionEconomy(f.Acquisition);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, f.FileSystem, path));
            Assert.That(service.LoadOrCreate("a4-explicit-delete", out string banner), Is.Not.Null, banner);
            return service;
        }
        private static SaveService CommittedDeleteDraft(Fixture f)
        {
            var service = DeleteService(f); var draft = TransactionalDungeonDraft.Create(f.State, Context(f), service.CreateDungeonDraftStore());
            Assert.That(Move(draft, f, 1), Is.True); Assert.That(draft.FlushNext(), Is.True);
            return service;
        }
        [Test]
        public void ExplicitDeleteRemovesDraftLegacyAndCanonicalEvidenceBeforeFreshBoot()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var service = CommittedDeleteDraft(f);
            string ordinary = f.ActivePath + ".canonical-write-0123456789abcdef-fedcba9876543210.rollback";
            string migration = Path.Combine(Path.GetDirectoryName(f.ActivePath), Path.GetFileNameWithoutExtension(f.ActivePath) + ".gd66-invalid-id.journal.json");
            string unrelated = f.ActivePath + ".editor-draft-not-owned.keep";
            foreach (string path in new[] { ordinary, migration, f.ActivePath + ".editor-draft", unrelated }) f.FileSystem.Seed(path, new byte[] { 7 });
            Assert.That(service.DeleteSave(out string banner), Is.True, banner);
            foreach (string path in new[] { ordinary, migration, f.ActivePath, f.ActivePath + ".editor-draft" })
                Assert.That(f.FileSystem.Exists(path), Is.False, path);
            Assert.That(f.FileSystem.Paths.Any(p => p.StartsWith(f.ActivePath + ".editor-draft.draft-", StringComparison.Ordinal)), Is.False);
            Assert.That(f.FileSystem.Exists(unrelated), Is.True);
            Assert.That(SaveService.HasOwnedRecoveryEvidence(f.ActivePath, f.FileSystem, f.Profile.Canonical.Serialized.MaximumCollectionRecords), Is.False);
            var deletes = f.FileSystem.Operations.Where(o => o.Type == Operation.Delete).ToArray();
            int firstCanonical = Array.FindIndex(deletes, o => o.Paths[0] == ordinary || o.Paths[0] == migration || o.Paths[0] == f.ActivePath);
            Assert.That(firstCanonical, Is.GreaterThan(0));
            Assert.That(deletes.Skip(firstCanonical).Any(o => o.Paths[0].StartsWith(f.ActivePath + ".editor-draft", StringComparison.Ordinal)), Is.False);
            Assert.That(service.LoadOrCreate("a4-explicit-delete", out string freshBanner), Is.Not.Null, freshBanner);
            Assert.That(service.CreateDungeonDraftStore().Read(), Is.Null);
        }
        [TestCase(false)] [TestCase(true)]
        public void ExplicitDeleteDraftFailureBeforeOrAfterMutationPreservesCanonical(bool afterMutation)
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var service = CommittedDeleteDraft(f);
            byte[] canonical = f.FileSystem.ReadAllBytes(f.ActivePath);
            string unrelated = f.ActivePath + ".editor-draft-not-owned.keep"; f.FileSystem.Seed(unrelated, new byte[] { 8 });
            string ordinary = f.ActivePath + ".canonical-write-0123456789abcdef-fedcba9876543210.rollback";
            f.FileSystem.Seed(ordinary, new byte[] { 9 });
            f.FileSystem.EnableTargetedFailure(Operation.Delete, p => p[0].StartsWith(f.ActivePath + ".editor-draft.draft-", StringComparison.Ordinal), 1, afterMutation);
            Assert.That(service.DeleteSave(out string banner), Is.False); Assert.That(banner, Is.EqualTo("Failed to delete save."));
            CollectionAssert.AreEqual(canonical, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.FileSystem.Exists(ordinary), Is.True); Assert.That(f.FileSystem.Exists(unrelated), Is.True);
            Assert.That(f.FileSystem.Paths.Any(p => p.StartsWith(f.ActivePath + ".editor-draft.draft-", StringComparison.Ordinal)), Is.True);
            f.FileSystem.DisableFailure(); Assert.That(service.DeleteSave(out banner), Is.True, banner);
        }
        [Test]
        public void ExplicitDeleteDraftContainmentFailureDeletesNothing()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var service = CommittedDeleteDraft(f);
            string[] paths = f.FileSystem.Paths.OrderBy(p => p, StringComparer.Ordinal).ToArray();
            f.FileSystem.EnableTargetedFailure(Operation.Containment, p => p.Length == 2 &&
                p[1].StartsWith(f.ActivePath + ".editor-draft", StringComparison.Ordinal), 1, false);
            Assert.That(service.DeleteSave(out _), Is.False);
            CollectionAssert.AreEqual(paths, f.FileSystem.Paths.OrderBy(p => p, StringComparer.Ordinal));
        }
        [Test]
        public void ExplicitDeleteOversizedDraftEvidenceFailsClosedBeforeCanonicalDeletion()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var service = DeleteService(f);
            int maximum = (f.Profile.Raw.MaximumArrayElements + 1) * 2;
            string prefix = f.ActivePath + ".editor-draft.draft-" + new string('a', 32) + ".";
            for (int i = 0; i <= maximum; i++) f.FileSystem.Seed(prefix + i.ToString("D6") + ".candidate", new byte[] { 1 });
            string[] before = f.FileSystem.Paths.OrderBy(p => p, StringComparer.Ordinal).ToArray();
            Assert.That(service.DeleteSave(out _), Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.Paths.OrderBy(p => p, StringComparer.Ordinal));
        }
        [Test]
        public void ExplicitDeleteDraftFlushFailureCannotClaimTotalDeletion()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var service = CommittedDeleteDraft(f);
            byte[] canonical = f.FileSystem.ReadAllBytes(f.ActivePath);
            f.FileSystem.EnableFailure(Operation.Flush, 1);
            Assert.That(service.DeleteSave(out _), Is.False);
            CollectionAssert.AreEqual(canonical, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.FileSystem.DisableFailure(); Assert.That(service.DeleteSave(out string banner), Is.True, banner);
        }
        private static FileDungeonDraftStore Store(Fixture f) => new FileDungeonDraftStore(f.ActivePath + ".editor-draft", f.FileSystem, f.Profile);
        private static DungeonDraftContext Context(Fixture f) => PhaseSevenA4TransactionalEditorTests.Context(f);
        private static bool Move(TransactionalDungeonDraft draft, Fixture f, int x) => PhaseSevenA4TransactionalEditorTests.Move(draft, f, x, 1);
        private static string Entry(Fixture f, int sequence, string kind) => f.FileSystem.Paths.Single(p =>
            p.EndsWith("." + sequence.ToString("D6") + "." + kind, StringComparison.Ordinal));
        private static TransactionalDungeonDraft Reopen(Fixture f)
        {
            var store = Store(f);
            var recovered = TransactionalDungeonDraft.Recover(store.Read(), f.State, Context(f), store, out string reason);
            Assert.That(reason, Is.Null); Assert.That(recovered, Is.Not.Null);
            return recovered;
        }

        [Test]
        public void DurabilityMatrix_SuccessHasIndependentCommitChainAndCanonicalCommitAfterRecovery()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Acknowledged));
            Assert.That(Move(draft, f, 1), Is.True); Assert.That(draft.CanSave, Is.False);
            Assert.That(draft.FlushNext(), Is.True); Assert.That(draft.AcknowledgedSequence, Is.EqualTo(1));
            Assert.That(Move(draft, f, 2), Is.True); Assert.That(draft.FlushNext(), Is.True);
            var recovered = Reopen(f); Assert.That(recovered.AcknowledgedSequence, Is.EqualTo(2));
            Assert.That(recovered.CanSave, Is.True);
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, recovered));
            f.Reopen(); Assert.That(f.State.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(2, 1)));
            Assert.That(Store(f).Read(), Is.Null); Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(13));
        }

        [TestCase("candidate-before", 0)]
        [TestCase("candidate-partial", 0)]
        [TestCase("candidate-after", 0)]
        [TestCase("candidate-barrier", 0)]
        [TestCase("commit-before", 0)]
        [TestCase("commit-after", 1)]
        [TestCase("commit-barrier", 1)]
        [TestCase("commit-readback", 1)]
        public void DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence(string fault, int recoveredSequence)
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            byte[] canonical = f.FileSystem.ReadAllBytes(f.ActivePath); double mana = f.Runtime.structureRuntime.ManaReserve;
            Assert.That(Move(draft, f, 1), Is.True);
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
            Assert.That(draft.FlushNext(), Is.False); Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Unknown));
            Assert.That(draft.Reason, Is.EqualTo(TransactionalDungeonDraft.UnknownReason));
            Assert.That(draft.AcknowledgedSequence, Is.Zero); Assert.That(draft.CanSave, Is.False);
            f.FileSystem.DisableFailure();
            Assert.That(draft.FlushNext(), Is.False); Assert.That(Move(draft, f, 2), Is.False);
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess, Is.False);
            CollectionAssert.AreEqual(canonical, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            string[] paths = f.FileSystem.Paths.ToArray(); byte[] first = Store(f).Read();
            var recovered = Reopen(f); Assert.That(recovered.AcknowledgedSequence, Is.EqualTo(recoveredSequence));
            Assert.That(recovered.CanSave, Is.EqualTo(recoveredSequence > 0));
            Assert.That(Reopen(f).AcknowledgedSequence, Is.EqualTo(recoveredSequence));
            CollectionAssert.AreEqual(first, Store(f).Read()); CollectionAssert.AreEqual(paths, f.FileSystem.Paths.ToArray());
            if (recoveredSequence == 0)
            {
                Assert.That(Move(recovered, f, 2), Is.True); Assert.That(recovered.FlushNext(), Is.True);
                Assert.That(Reopen(f).ReadModel.Floors[0].RoomContents.Assignments[0].RoomLocalPosition, Is.EqualTo(new TileCoordinate(2, 1)));
            }
        }

        [TestCase("delete-before", 1)]
        [TestCase("delete-after", 0)]
        [TestCase("delete-barrier", -1)]
        public void DurabilityMatrix_FailedDiscardBlocksLiveSaveAndRecoveryUsesRemainingCommitEvidence(string fault, int recoveredSequence)
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Assert.That(Move(draft, f, 1), Is.True); Assert.That(draft.FlushNext(), Is.True);
            byte[] canonical = f.FileSystem.ReadAllBytes(f.ActivePath); double mana = f.Runtime.structureRuntime.ManaReserve;
            if (fault == "delete-barrier") f.FileSystem.EnableFailure(Operation.Flush, 1);
            else f.FileSystem.EnableTargetedFailure(Operation.Delete, p => p[0].EndsWith(".000001.commit"), 1, fault == "delete-after");
            Assert.That(draft.Discard(), Is.False); Assert.That(draft.IsClosed, Is.False);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Unknown));
            Assert.That(draft.Reason, Is.EqualTo(TransactionalDungeonDraft.DeleteFailedReason));
            Assert.That(draft.AcknowledgedSequence, Is.EqualTo(1)); Assert.That(draft.CanSave, Is.False);
            f.FileSystem.DisableFailure();
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess, Is.False);
            CollectionAssert.AreEqual(canonical, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            if (recoveredSequence < 0) Assert.That(Store(f).Read(), Is.Null);
            else Assert.That(Reopen(f).AcknowledgedSequence, Is.EqualTo(recoveredSequence));
        }

        [TestCase("missing-predecessor-commit")]
        [TestCase("missing-predecessor-candidate")]
        [TestCase("wrong-predecessor")]
        [TestCase("wrong-hash")]
        [TestCase("wrong-identity")]
        [TestCase("malformed-commit")]
        [TestCase("duplicate-field")]
        [TestCase("conflicting-session")]
        public void DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed(string corruption)
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Move(draft, f, 1); Assert.That(draft.FlushNext(), Is.True); Move(draft, f, 2); Assert.That(draft.FlushNext(), Is.True);
            string commitPath = Entry(f, 2, "commit");
            var commit = JsonUtility.FromJson<DungeonDraftCommitRecord>(Encoding.UTF8.GetString(f.FileSystem.ReadAllBytes(commitPath)));
            switch (corruption)
            {
                case "missing-predecessor-commit": f.FileSystem.RemoveSeededEvidence(Entry(f, 1, "commit")); break;
                case "missing-predecessor-candidate": f.FileSystem.RemoveSeededEvidence(Entry(f, 1, "candidate")); break;
                case "wrong-predecessor": commit.PredecessorCommit = new string('0', 64); break;
                case "wrong-hash": commit.CandidateHash = new string('f', 64); break;
                case "wrong-identity": commit.DraftId = new string('a', 32); break;
                case "malformed-commit": f.FileSystem.Seed(commitPath, Encoding.UTF8.GetBytes("{")); break;
                case "duplicate-field": f.FileSystem.Seed(commitPath, Encoding.UTF8.GetBytes(JsonUtility.ToJson(commit).Replace("\"Version\":1", "\"Version\":1,\"Version\":1"))); break;
                case "conflicting-session": f.FileSystem.Seed(commitPath.Replace(commit.DraftId, new string('b', 32)), f.FileSystem.ReadAllBytes(commitPath)); break;
            }
            if (corruption.StartsWith("wrong-")) f.FileSystem.Seed(commitPath, Encoding.UTF8.GetBytes(JsonUtility.ToJson(commit)));
            var first = Assert.Throws<IOException>(() => Store(f).Read());
            var second = Assert.Throws<IOException>(() => Store(f).Read());
            Assert.That(first.Message, Is.EqualTo(TransactionalDungeonDraft.RecoveryFailedReason)); Assert.That(second.Message, Is.EqualTo(first.Message));
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, draft).IsSuccess, Is.False);
        }

        [Test]
        public void DurabilityMatrix_StaleBaselineRejectsCommittedEvidenceAndMaximumJournalRecovers()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            for (int i = 0; i < f.Profile.Raw.MaximumArrayElements; i++)
            { Assert.That(Move(draft, f, i % 2 + 1), Is.True); Assert.That(draft.FlushNext(), Is.True); }
            Assert.That(Reopen(f).CommandCount, Is.EqualTo(f.Profile.Raw.MaximumArrayElements));
            var changed = Context(f).Copy(f.State); changed.Floors[0].RoomContents.Assignments[0].RoomLocalPosition = new TileCoordinate(3, 1);
            Assert.That(TransactionalDungeonDraft.Recover(store.Read(), changed, Context(f), store, out var reason), Is.Null);
            Assert.That(reason, Is.EqualTo(TransactionalDungeonDraft.StaleReason));
        }

        [TestCase(false)] [TestCase(true)]
        public void DurabilityMatrix_InitialUnknownOutcomeRecoversOnlyWithCommitOrOffersExplicitDiscard(bool commitSurvives)
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var store = Store(f);
            byte[] canonical = f.FileSystem.ReadAllBytes(f.ActivePath);
            f.FileSystem.EnableTargetedFailure(Operation.Write,
                p => p[0].EndsWith(commitSurvives ? ".000000.commit" : ".000000.candidate"), 1, true);
            var draft = TransactionalDungeonDraft.Create(f.State, Context(f), store);
            Assert.That(draft.Durability, Is.EqualTo(DraftDurability.Unknown)); Assert.That(draft.CanSave, Is.False);
            f.FileSystem.DisableFailure();
            if (commitSurvives) Assert.That(Reopen(f).CommandCount, Is.Zero);
            else Assert.That(Assert.Throws<IOException>(() => Store(f).Read()).Message, Is.EqualTo(TransactionalDungeonDraft.RecoveryFailedReason));
            Assert.That(Store(f).Delete(null), Is.True); Assert.That(Store(f).Read(), Is.Null);
            CollectionAssert.AreEqual(canonical, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
    }
}
#endif
