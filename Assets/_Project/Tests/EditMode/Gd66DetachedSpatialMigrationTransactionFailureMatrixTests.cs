#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedSpatialMigrationTransactionFailureMatrixTests
    {
        private enum EntryPoint { FreshExecute, ResumedExecute, Recover }
        private enum MatrixOperation { Exists, Read, DurableWrite, AtomicReplace, DirectoryFlush, Enumeration, Containment, AtomicMove, Delete }
        private enum MatrixCheckpoint { NoJournal, DescriptorPinned, BackupVerified, CandidateVerified, Replaced, DurableVerified, FinalizationAttempt, RestorationAttempt }
        private enum MatrixAuthority { Original, Candidate, None }
        private enum ActiveExpectation { Original, Candidate, Untrusted }
        private enum PathRole { Active, Journal, JournalNext, Backup, CandidateStaging, RestoreStaging, RestorationIntent, Receipt, Quarantine, Directory }

        private sealed class RetainedEvidence
        {
            internal RetainedEvidence(bool journal = false, bool journalNext = false, bool backup = false,
                bool candidate = false, bool restore = false, bool intent = false, bool quarantine = false,
                bool receipt = false)
            { Journal = journal; JournalNext = journalNext; Backup = backup; Candidate = candidate;
              Restore = restore; Intent = intent; Quarantine = quarantine; Receipt = receipt; }
            internal bool Journal { get; }
            internal bool JournalNext { get; }
            internal bool Backup { get; }
            internal bool Candidate { get; }
            internal bool Restore { get; }
            internal bool Intent { get; }
            internal bool Quarantine { get; }
            internal bool Receipt { get; }
        }

        private sealed class ReadSubstitution
        {
            internal ReadSubstitution(PathRole role, byte[] bytes)
            { Role = role; Bytes = bytes; }
            internal PathRole Role { get; }
            internal byte[] Bytes { get; }
        }

        private sealed class MatrixCase
        {
            internal MatrixCase(string name, EntryPoint entryPoint, MatrixCheckpoint checkpoint,
                MatrixOperation operation, int occurrence, PathRole role, bool afterMutation,
                string firstReason, SpatialMigrationJournalStage? firstStage, MatrixAuthority firstAuthority,
                ActiveExpectation firstActive, RetainedEvidence firstEvidence, int firstTransitions,
                EntryPoint retryEntryPoint, bool firstSuccess, bool injectFailure, int firstCandidateWriteAttempts, int firstCandidateWriteCompletions,
                int finalCandidateWriteAttempts, int finalCandidateWriteCompletions, bool finalSuccess, string finalReason,
                SpatialMigrationJournalStage? finalStage, MatrixAuthority finalAuthority,
                ActiveExpectation finalActive, int finalTransitions, ReadSubstitution substitution = null)
            { Name = name; EntryPoint = entryPoint; Checkpoint = checkpoint; Operation = operation;
              Occurrence = occurrence; Role = role; AfterMutation = afterMutation; FirstReason = firstReason;
              FirstStage = firstStage; FirstAuthority = firstAuthority; FirstActive = firstActive;
              FirstEvidence = firstEvidence; FirstTransitions = firstTransitions; RetryEntryPoint = retryEntryPoint;
              FirstSuccess = firstSuccess; InjectFailure = injectFailure;
              FirstCandidateWriteAttempts = firstCandidateWriteAttempts;
              FirstCandidateWriteCompletions = firstCandidateWriteCompletions;
              FinalCandidateWriteAttempts = finalCandidateWriteAttempts;
              FinalCandidateWriteCompletions = finalCandidateWriteCompletions; FinalSuccess = finalSuccess; FinalReason = finalReason; FinalStage = finalStage;
              FinalAuthority = finalAuthority; FinalActive = finalActive; FinalTransitions = finalTransitions;
              Substitution = substitution; }
            internal string Name { get; }
            internal EntryPoint EntryPoint { get; }
            internal MatrixCheckpoint Checkpoint { get; }
            internal MatrixOperation Operation { get; }
            internal int Occurrence { get; }
            internal PathRole Role { get; }
            internal bool AfterMutation { get; }
            internal string FirstReason { get; }
            internal SpatialMigrationJournalStage? FirstStage { get; }
            internal MatrixAuthority FirstAuthority { get; }
            internal ActiveExpectation FirstActive { get; }
            internal RetainedEvidence FirstEvidence { get; }
            internal int FirstTransitions { get; }
            internal EntryPoint RetryEntryPoint { get; }
            internal bool FirstSuccess { get; }
            internal bool InjectFailure { get; }
            internal int FirstCandidateWriteAttempts { get; }
            internal int FirstCandidateWriteCompletions { get; }
            internal int FinalCandidateWriteAttempts { get; }
            internal int FinalCandidateWriteCompletions { get; }
            internal bool FinalSuccess { get; }
            internal string FinalReason { get; }
            internal SpatialMigrationJournalStage? FinalStage { get; }
            internal MatrixAuthority FinalAuthority { get; }
            internal ActiveExpectation FinalActive { get; }
            internal int FinalTransitions { get; }
            internal ReadSubstitution Substitution { get; }
            public override string ToString() => Name;
        }

        private static IEnumerable<MatrixCase> Cases
        {
            get
            {
                yield return C("fresh-containment", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.Containment, PathRole.Journal, DetachedSpatialMigrationTransaction.PathInvalidReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("fresh-journal-write", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.DurableWrite, PathRole.Journal, DetachedSpatialMigrationTransaction.BackupFailedReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("fresh-enumeration", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.Enumeration, PathRole.Directory, DetachedSpatialMigrationTransaction.RecoveryFailedReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("descriptor-backup-write", EntryPoint.ResumedExecute, MatrixCheckpoint.DescriptorPinned, MatrixOperation.DurableWrite, PathRole.Backup, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("descriptor-backup-exists", EntryPoint.ResumedExecute, MatrixCheckpoint.DescriptorPinned, MatrixOperation.Exists, PathRole.Backup, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("descriptor-backup-mismatch", EntryPoint.ResumedExecute, MatrixCheckpoint.DescriptorPinned, MatrixOperation.Read, PathRole.Backup, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, new ReadSubstitution(PathRole.Backup, new byte[] { 9, 9, 9 }), injectFailure:false, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("backup-candidate-write", EntryPoint.ResumedExecute, MatrixCheckpoint.BackupVerified, MatrixOperation.DurableWrite, PathRole.CandidateStaging, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, firstCandidateWriteAttempts:1, firstCandidateWriteCompletions:0, finalCandidateWriteAttempts:2, finalCandidateWriteCompletions:1);
                yield return C("backup-candidate-flush", EntryPoint.ResumedExecute, MatrixCheckpoint.BackupVerified, MatrixOperation.DirectoryFlush, PathRole.Directory, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, firstCandidateWriteAttempts:1, firstCandidateWriteCompletions:1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("backup-candidate-mismatch", EntryPoint.ResumedExecute, MatrixCheckpoint.BackupVerified, MatrixOperation.Read, PathRole.CandidateStaging, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, new ReadSubstitution(PathRole.CandidateStaging, new byte[] { 1, 2, 3 }), injectFailure:false, firstCandidateWriteAttempts:1, firstCandidateWriteCompletions:1, finalCandidateWriteAttempts:1, finalCandidateWriteCompletions:1);
                yield return C("candidate-active-replace-before", EntryPoint.Recover, MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, PathRole.Active, DetachedSpatialMigrationTransaction.ReplacementFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, DetachedSpatialMigrationTransaction.EmptySuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, finalCandidateWriteAttempts:0, finalCandidateWriteCompletions:0);
                yield return C("replaced-directory-flush", EntryPoint.Recover, MatrixCheckpoint.Replaced, MatrixOperation.DirectoryFlush, PathRole.Directory, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, quarantine:true), 1, EntryPoint.Recover, true, DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic, null, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("replaced-active-read-exception", EntryPoint.Recover, MatrixCheckpoint.Replaced, MatrixOperation.Read, PathRole.Active, DetachedSpatialMigrationTransaction.PathInvalidReason, null, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 0, EntryPoint.Recover, true, DetachedSpatialMigrationTransaction.SuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 0);
                yield return C("durable-final-receipt-write", EntryPoint.Recover, MatrixCheckpoint.DurableVerified, MatrixOperation.DurableWrite, PathRole.Receipt, DetachedSpatialMigrationTransaction.SuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 0, EntryPoint.Recover, true, DetachedSpatialMigrationTransaction.AlreadyCommittedReason, null, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 0, firstSuccess:true);
                yield return C("durable-final-journal-advance", EntryPoint.Recover, MatrixCheckpoint.DurableVerified, MatrixOperation.AtomicReplace, PathRole.Journal, DetachedSpatialMigrationTransaction.FinalizationFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, journalNext:true, backup:true, receipt:true), 0, EntryPoint.Recover, true, DetachedSpatialMigrationTransaction.SuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 0);
                yield return C("restoration-intent-write", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.DurableWrite, PathRole.RestorationIntent, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restoration-intent-mismatch", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.Read, PathRole.RestorationIntent, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, intent:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1, new ReadSubstitution(PathRole.RestorationIntent, new byte[] { 4, 5, 6 }), injectFailure:false);
                yield return C("restore-staging-quarantine-move", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicMove, PathRole.Quarantine, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-staging-mismatch", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.Read, PathRole.RestoreStaging, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1, new ReadSubstitution(PathRole.RestoreStaging, new byte[] { 6, 5, 4 }), injectFailure:false);
                yield return C("restore-active-replace-before", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicReplace, PathRole.Active, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, intent:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-exact-quarantine-delete", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.Delete, PathRole.RestoreStaging, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, quarantine:true), 0, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-originalrestored-advance", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicReplace, PathRole.Journal, DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, journalNext:true, backup:true, intent:true), 1, EntryPoint.Recover, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void PacketC_BoundedFailureMatrix_ExecutesDeclaredFaultAndRestartScenario(MatrixCase row)
        {
            Scenario scenario = Scenario.Create(TestContext.CurrentContext.Test.Name, row);
            scenario.Inject(row);

            DetachedSpatialMigrationOutcome first = Invoke(row.EntryPoint, scenario);

            Gd66DetachedSpatialMigrationTransactionTests.FileOperation failedOperation = scenario.FileSystem.FailedOperation;
            int failedOccurrence = scenario.FileSystem.FailedTargetOccurrence;
            AssertOutcome(first, row.FirstSuccess, row.FirstReason, row.FirstStage, row.FirstAuthority);
            AssertActiveBytes(scenario, row.FirstActive);
            AssertEvidence(scenario, row.FirstEvidence);
            Assert.That(AuthorityTransitions(scenario), Is.EqualTo(row.FirstTransitions), row.Name);
            Assert.That(scenario.CandidateStagingWriteAttempts(), Is.EqualTo(row.FirstCandidateWriteAttempts), row.Name);
            Assert.That(scenario.CandidateStagingWriteCompletions(), Is.EqualTo(row.FirstCandidateWriteCompletions), row.Name);
            Assert.That(scenario.LiveJournalCount(), Is.LessThanOrEqualTo(1), row.Name);
            AssertThrownOperation(scenario, row, failedOperation, failedOccurrence);
            scenario.AssertSubstitutionConsumed(row);
            AssertChronologicalDuplicateFree(first);
            scenario.FileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry = Invoke(row.RetryEntryPoint, scenario);

            AssertOutcome(retry, row.FinalSuccess, row.FinalReason, row.FinalStage, row.FinalAuthority);
            AssertActiveBytes(scenario, row.FinalActive);
            Assert.That(AuthorityTransitions(scenario), Is.EqualTo(row.FinalTransitions), row.Name);
            Assert.That(scenario.LiveJournalCount(), Is.LessThanOrEqualTo(1), row.Name);
            Assert.That(scenario.CandidateStagingWriteAttempts(), Is.EqualTo(row.FinalCandidateWriteAttempts), row.Name);
            Assert.That(scenario.CandidateStagingWriteCompletions(), Is.EqualTo(row.FinalCandidateWriteCompletions), row.Name);
            AssertChronologicalDuplicateFree(retry);
        }

        [Test]
        public void PacketC_BoundedFailureMatrix_CoversExpectedDimensions()
        {
            MatrixCase[] rows = Cases.ToArray();
            Assert.That(rows.Length, Is.InRange(18, 30));
            Assert.That(rows.Where(row => row.InjectFailure).All(row => row.Occurrence > 0), Is.True);
            foreach (MatrixOperation operation in (MatrixOperation[])Enum.GetValues(typeof(MatrixOperation)))
                Assert.That(rows.Any(row => row.Operation == operation), Is.True, operation.ToString());
            foreach (MatrixCheckpoint checkpoint in (MatrixCheckpoint[])Enum.GetValues(typeof(MatrixCheckpoint)))
                Assert.That(rows.Any(row => row.Checkpoint == checkpoint), Is.True, checkpoint.ToString());
            Assert.That(rows.All(row => !string.IsNullOrEmpty(row.FinalReason)), Is.True);
        }

        [Test]
        public void PacketC_DiagnosticChronologyGuardRejectsReorderedAndDuplicateEvents()
        {
            Assert.That(AreChronologicalAndDuplicateFree(new[]
            {
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic,
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic
            }), Is.True);
            Assert.That(AreChronologicalAndDuplicateFree(new[]
            {
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic,
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic
            }), Is.False);
            Assert.That(AreChronologicalAndDuplicateFree(new[]
            {
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic,
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic
            }), Is.False);
            Assert.That(AreChronologicalAndDuplicateFree(new[]
            {
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic,
                "gd66.diagnostic.unranked",
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic
            }), Is.True);
        }

        private static MatrixCase C(string name, EntryPoint entry, MatrixCheckpoint checkpoint,
            MatrixOperation operation, PathRole role, string reason, SpatialMigrationJournalStage? stage,
            MatrixAuthority authority, ActiveExpectation active, RetainedEvidence evidence, int transitions,
            EntryPoint retry, bool finalSuccess, string finalReason, SpatialMigrationJournalStage? finalStage,
            MatrixAuthority finalAuthority, ActiveExpectation finalActive, int finalTransitions,
            ReadSubstitution substitution = null, bool after = false, bool firstSuccess = false,
            bool injectFailure = true, int firstCandidateWriteAttempts = 0, int firstCandidateWriteCompletions = 0,
            int finalCandidateWriteAttempts = 0, int finalCandidateWriteCompletions = 0) =>
            new MatrixCase(name, entry, checkpoint, operation, injectFailure ? 1 : 0, role, after, reason, stage,
                authority, active, evidence, transitions, retry, firstSuccess, injectFailure, firstCandidateWriteAttempts, firstCandidateWriteCompletions,
                finalCandidateWriteAttempts, finalCandidateWriteCompletions, finalSuccess, finalReason, finalStage, finalAuthority, finalActive,
                finalTransitions, substitution);
        private static RetainedEvidence E(bool journal = false, bool journalNext = false, bool backup = false,
            bool candidate = false, bool restore = false, bool intent = false, bool quarantine = false,
            bool receipt = false) => new RetainedEvidence(journal, journalNext, backup, candidate, restore,
                intent, quarantine, receipt);

        private static DetachedSpatialMigrationOutcome Invoke(EntryPoint entryPoint, Scenario scenario)
        {
            var transaction = new DetachedSpatialMigrationTransaction(scenario.FileSystem,
                Gd66DetachedSpatialMigrationTransactionTests.Recovery(scenario.Fixture, scenario.RecoveryLegacyBytes));
            return entryPoint == EntryPoint.FreshExecute || entryPoint == EntryPoint.ResumedExecute
                ? transaction.Execute(scenario.ActivePath, scenario.Fixture.Result.Attempt)
                : transaction.Recover(scenario.ActivePath);
        }

        private static void AssertOutcome(DetachedSpatialMigrationOutcome outcome, bool success, string reason,
            SpatialMigrationJournalStage? stage, MatrixAuthority authority)
        {
            Assert.That(outcome.IsSuccess, Is.EqualTo(success), reason);
            Assert.That(outcome.Reason, Is.EqualTo(reason));
            Assert.That(outcome.Stage, Is.EqualTo(stage));
            Assert.That(outcome.TrustedPayload.ToString(), Is.EqualTo(authority.ToString()));
        }

        private static void AssertActiveBytes(Scenario scenario, ActiveExpectation expected)
        {
            byte[] active = scenario.FileSystem.ReadAllBytes(scenario.ActivePath);
            if (expected == ActiveExpectation.Original) Assert.That(active, Is.EqualTo(scenario.Fixture.Original));
            else if (expected == ActiveExpectation.Candidate) Assert.That(active, Is.EqualTo(scenario.CandidateBytes));
            else Assert.That(active, Is.EqualTo(scenario.UntrustedBytes));
        }

        private static void AssertEvidence(Scenario scenario, RetainedEvidence expected)
        {
            Assert.That(scenario.FileSystem.Exists(scenario.JournalPath), Is.EqualTo(expected.Journal));
            Assert.That(scenario.FileSystem.Exists(scenario.JournalNextPath), Is.EqualTo(expected.JournalNext));
            Assert.That(scenario.FileSystem.Exists(scenario.BackupPath), Is.EqualTo(expected.Backup));
            Assert.That(scenario.FileSystem.Exists(scenario.CandidateStagingPath), Is.EqualTo(expected.Candidate));
            Assert.That(scenario.FileSystem.Exists(scenario.RestoreStagingPath), Is.EqualTo(expected.Restore));
            Assert.That(scenario.FileSystem.Exists(scenario.RestorationIntentPath), Is.EqualTo(expected.Intent));
            Assert.That(scenario.FileSystem.Exists(scenario.ReceiptPath), Is.EqualTo(expected.Receipt));
            Assert.That(scenario.FileSystem.Paths.Any(path => path.Contains("gd66-quarantine")),
                Is.EqualTo(expected.Quarantine));
        }

        private static void AssertThrownOperation(Scenario scenario, MatrixCase row,
            Gd66DetachedSpatialMigrationTransactionTests.FileOperation failed, int failedOccurrence)
        {
            if (!row.InjectFailure)
            { Assert.That(failed, Is.Null, row.Name); return; }
            Assert.That(failed, Is.Not.Null, row.Name);
            Assert.That(failed.Type, Is.EqualTo(ToOperationType(row.Operation)), row.Name);
            Assert.That(scenario.RoleMatches(failed.Paths, row.Role), Is.True, row.Name);
            Assert.That(failedOccurrence, Is.EqualTo(row.Occurrence), row.Name);
            Assert.That(failed.FailedAfterMutation, Is.EqualTo(row.AfterMutation), row.Name);
            Assert.That(failed.MutationCompleted, Is.EqualTo(row.AfterMutation), row.Name);
        }

        private static void AssertChronologicalDuplicateFree(DetachedSpatialMigrationOutcome outcome)
        {
            string[] diagnostics = outcome.Diagnostics == null ? Array.Empty<string>() : outcome.Diagnostics.ToArray();
            Assert.That(AreChronologicalAndDuplicateFree(diagnostics), Is.True);
        }

        private static bool AreChronologicalAndDuplicateFree(IEnumerable<string> diagnostics)
        {
            string[] values = diagnostics == null ? Array.Empty<string>() : diagnostics.ToArray();
            if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) return false;
            int previousRank = -1;
            foreach (string diagnostic in values)
            {
                if (!TryDiagnosticRank(diagnostic, out int rank)) continue;
                if (rank <= previousRank) return false;
                previousRank = rank;
            }
            return true;
        }

        private static bool TryDiagnosticRank(string diagnostic, out int rank)
        {
            if (diagnostic == DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic)
            { rank = 0; return true; }
            if (diagnostic == DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic)
            { rank = 1; return true; }
            if (diagnostic == DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic)
            { rank = 2; return true; }
            if (diagnostic == DetachedSpatialMigrationTransaction.ReceiptInvalidReason ||
                diagnostic == DetachedSpatialMigrationTransaction.ReceiptWriteDiagnostic)
            { rank = 3; return true; }
            rank = -1; return false;
        }

        private static int AuthorityTransitions(Scenario scenario) => scenario.FileSystem.Operations.Count(operation =>
            operation.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace &&
            operation.MutationCompleted &&
            Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(operation.Paths[1], scenario.ActivePath));

        private sealed class Scenario
        {
            internal Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture Fixture { get; private set; }
            internal Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem FileSystem { get; private set; }
            internal string ActivePath { get; private set; }
            internal string DirectoryPath { get; private set; }
            internal SpatialMigrationSidecarNames Names { get; private set; }
            internal byte[] CandidateBytes { get; private set; }
            internal byte[] UntrustedBytes { get; private set; }
            internal byte[] RecoveryLegacyBytes { get; private set; }
            internal string JournalPath => Path.Combine(DirectoryPath, Names.Journal);
            internal string JournalNextPath => JournalPath + ".next";
            internal string BackupPath => Path.Combine(DirectoryPath, Names.OriginalBackup);
            internal string CandidateStagingPath => Path.Combine(DirectoryPath, Names.CandidateStaging);
            internal string RestoreStagingPath => BackupPath + ".restore";
            internal string RestorationIntentPath => BackupPath + ".restore.intent";
            internal string ReceiptPath => Path.Combine(DirectoryPath, Names.FinalizedReceipt);

            internal static Scenario Create(string identity, MatrixCase row)
            {
                var scenario = new Scenario();
                scenario.Fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
                scenario.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
                scenario.ActivePath = Gd66DetachedSpatialMigrationTransactionTests.ActivePath("matrix-" + identity);
                scenario.DirectoryPath = Path.GetDirectoryName(scenario.ActivePath);
                scenario.CandidateBytes = scenario.Fixture.Result.Attempt.Candidate.GetBytes();
                scenario.UntrustedBytes = new byte[] { 0x7b, 0x7d, 0x20, 0x30 };
                scenario.Materialize(row.Checkpoint);
                if (row.Operation == MatrixOperation.AtomicMove)
                    scenario.FileSystem.Seed(scenario.RestoreStagingPath, scenario.UntrustedBytes);
                if (row.Name.Contains("exact-quarantine"))
                {
                    scenario.FileSystem.Seed(scenario.RestoreStagingPath, scenario.UntrustedBytes);
                    scenario.FileSystem.Seed(Gd66DetachedSpatialMigrationTransactionTests.QuarantinePath(
                        scenario.DirectoryPath, scenario.RestoreStagingPath, scenario.UntrustedBytes),
                        scenario.UntrustedBytes);
                }
                return scenario;
            }

            internal void Materialize(MatrixCheckpoint checkpoint)
            {
                switch (checkpoint)
                {
                    case MatrixCheckpoint.NoJournal:
                        FileSystem.Seed(ActivePath, Fixture.Original);
                        Names = SpatialMigrationSidecarPaths.Derive(Path.GetFileName(ActivePath),
                            Fixture.Result.Attempt.TransactionId).Value;
                        break;
                    case MatrixCheckpoint.DescriptorPinned:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.DescriptorPinned, false, false, false);
                        break;
                    case MatrixCheckpoint.BackupVerified:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.BackupVerified, true, false, false);
                        break;
                    case MatrixCheckpoint.CandidateVerified:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.CandidateVerified, true, true, false);
                        break;
                    case MatrixCheckpoint.Replaced:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.Replaced, true, false, true);
                        break;
                    case MatrixCheckpoint.DurableVerified:
                    case MatrixCheckpoint.FinalizationAttempt:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.DurableVerified, true, false, true);
                        break;
                    case MatrixCheckpoint.RestorationAttempt:
                        Names = Gd66DetachedSpatialMigrationTransactionTests.MaterializeJournal(FileSystem, Fixture,
                            ActivePath, SpatialMigrationJournalStage.DurableVerified, true, false, true);
                        RecoveryLegacyBytes = new byte[] { 1 };
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(checkpoint));
                }
            }

            internal void Inject(MatrixCase row)
            {
                if (row.Substitution != null)
                    FileSystem.SubstituteNextRead(PathForRole(row.Substitution.Role), row.Substitution.Bytes);
                if (!row.InjectFailure) return;
                Gd66DetachedSpatialMigrationTransactionTests.OperationType type = ToOperationType(row.Operation);
                FileSystem.EnableTargetedFailure(type, paths => RoleMatches(paths, row.Role), row.Occurrence,
                    row.AfterMutation);
            }

            internal void AssertSubstitutionConsumed(MatrixCase row)
            {
                if (row.Substitution == null) return;
                Assert.That(FileSystem.PendingReadSubstitutions(PathForRole(row.Substitution.Role)),
                    Is.EqualTo(0), row.Name);
            }

            internal bool RoleMatches(string[] paths, PathRole role)
            {
                string expected = PathForRole(role);
                return paths.Any(path => Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(path,
                    expected));
            }

            internal string PathForRole(PathRole role)
            {
                switch (role)
                {
                    case PathRole.Active: return ActivePath;
                    case PathRole.Journal: return JournalPath;
                    case PathRole.JournalNext: return JournalNextPath;
                    case PathRole.Backup: return BackupPath;
                    case PathRole.CandidateStaging: return CandidateStagingPath;
                    case PathRole.RestoreStaging: return RestoreStagingPath;
                    case PathRole.RestorationIntent: return RestorationIntentPath;
                    case PathRole.Receipt: return ReceiptPath;
                    case PathRole.Directory: return DirectoryPath;
                    case PathRole.Quarantine: return Gd66DetachedSpatialMigrationTransactionTests.QuarantinePath(DirectoryPath, RestoreStagingPath, UntrustedBytes);
                    default: throw new ArgumentOutOfRangeException(nameof(role));
                }
            }

            internal int LiveJournalCount() => FileSystem.Paths.Count(path =>
                Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(path, JournalPath));
            internal int CandidateStagingWriteAttempts() => CandidateStagingWriteOperations().Count();
            internal int CandidateStagingWriteCompletions() => CandidateStagingWriteOperations().Count(operation =>
                operation.MutationCompleted);
            private IEnumerable<Gd66DetachedSpatialMigrationTransactionTests.FileOperation> CandidateStagingWriteOperations() =>
                FileSystem.Operations.Where(operation =>
                    operation.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write &&
                    operation.Paths.Any(path => Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(path,
                        CandidateStagingPath)));
        }

        private static Gd66DetachedSpatialMigrationTransactionTests.OperationType ToOperationType(MatrixOperation operation)
        {
            switch (operation)
            {
                case MatrixOperation.Exists: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Exists;
                case MatrixOperation.Read: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read;
                case MatrixOperation.DurableWrite: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write;
                case MatrixOperation.AtomicReplace: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace;
                case MatrixOperation.DirectoryFlush: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush;
                case MatrixOperation.Enumeration: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Enumerate;
                case MatrixOperation.Containment: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Containment;
                case MatrixOperation.AtomicMove: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Move;
                case MatrixOperation.Delete: return Gd66DetachedSpatialMigrationTransactionTests.OperationType.Delete;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
    }
}
#endif
