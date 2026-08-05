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
            internal RetainedEvidence(bool journal = false, bool backup = false, bool candidate = false,
                bool restore = false, bool intent = false, bool quarantine = false, bool receipt = false)
            { Journal = journal; Backup = backup; Candidate = candidate; Restore = restore; Intent = intent; Quarantine = quarantine; Receipt = receipt; }
            internal bool Journal { get; }
            internal bool Backup { get; }
            internal bool Candidate { get; }
            internal bool Restore { get; }
            internal bool Intent { get; }
            internal bool Quarantine { get; }
            internal bool Receipt { get; }
        }

        private sealed class MatrixCase
        {
            internal MatrixCase(string name, EntryPoint entryPoint, MatrixCheckpoint checkpoint,
                MatrixOperation operation, int occurrence, PathRole role, bool afterMutation,
                string firstReason, SpatialMigrationJournalStage? firstStage, MatrixAuthority firstAuthority,
                ActiveExpectation firstActive, RetainedEvidence firstEvidence, int firstTransitions,
                EntryPoint retryEntryPoint, bool retrySuccess, SpatialMigrationJournalStage? finalStage,
                MatrixAuthority finalAuthority, ActiveExpectation finalActive, int finalTransitions)
            { Name = name; EntryPoint = entryPoint; Checkpoint = checkpoint; Operation = operation;
              Occurrence = occurrence; Role = role; AfterMutation = afterMutation; FirstReason = firstReason;
              FirstStage = firstStage; FirstAuthority = firstAuthority; FirstActive = firstActive;
              FirstEvidence = firstEvidence; FirstTransitions = firstTransitions; RetryEntryPoint = retryEntryPoint;
              RetrySuccess = retrySuccess; FinalStage = finalStage; FinalAuthority = finalAuthority;
              FinalActive = finalActive; FinalTransitions = finalTransitions; }
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
            internal bool RetrySuccess { get; }
            internal SpatialMigrationJournalStage? FinalStage { get; }
            internal MatrixAuthority FinalAuthority { get; }
            internal ActiveExpectation FinalActive { get; }
            internal int FinalTransitions { get; }
            public override string ToString() => Name;
        }

        private static IEnumerable<MatrixCase> Cases
        {
            get
            {
                yield return C("fresh-exists-discovery", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.Exists, PathRole.Journal, DetachedSpatialMigrationTransaction.RecoveryFailedReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("fresh-journal-write", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.DurableWrite, PathRole.Journal, DetachedSpatialMigrationTransaction.BackupFailedReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("fresh-enumeration", EntryPoint.FreshExecute, MatrixCheckpoint.NoJournal, MatrixOperation.Enumeration, PathRole.Directory, DetachedSpatialMigrationTransaction.RecoveryFailedReason, null, MatrixAuthority.Original, ActiveExpectation.Original, E(), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("descriptor-backup-write", EntryPoint.Recover, MatrixCheckpoint.DescriptorPinned, MatrixOperation.DurableWrite, PathRole.Backup, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("descriptor-backup-reread", EntryPoint.Recover, MatrixCheckpoint.DescriptorPinned, MatrixOperation.Read, PathRole.Backup, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("backup-candidate-write", EntryPoint.Recover, MatrixCheckpoint.BackupVerified, MatrixOperation.DurableWrite, PathRole.CandidateStaging, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("backup-candidate-flush", EntryPoint.Recover, MatrixCheckpoint.BackupVerified, MatrixOperation.DirectoryFlush, PathRole.Directory, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("backup-candidate-reread", EntryPoint.Recover, MatrixCheckpoint.BackupVerified, MatrixOperation.Read, PathRole.CandidateStaging, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("candidate-active-replace-before", EntryPoint.Recover, MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, PathRole.Active, DetachedSpatialMigrationTransaction.ReplacementFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("candidate-active-replace-after", EntryPoint.Recover, MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, PathRole.Active, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1, true);
                yield return C("replaced-directory-flush", EntryPoint.Recover, MatrixCheckpoint.Replaced, MatrixOperation.DirectoryFlush, PathRole.Directory, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, intent:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("replaced-active-reread", EntryPoint.Recover, MatrixCheckpoint.Replaced, MatrixOperation.Read, PathRole.Active, DetachedSpatialMigrationTransaction.RecoveryFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("replaced-durable-advance", EntryPoint.Recover, MatrixCheckpoint.Replaced, MatrixOperation.AtomicReplace, PathRole.Journal, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("durable-final-receipt-write", EntryPoint.Recover, MatrixCheckpoint.DurableVerified, MatrixOperation.DurableWrite, PathRole.Receipt, DetachedSpatialMigrationTransaction.FinalizationFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("durable-final-journal-advance", EntryPoint.Recover, MatrixCheckpoint.DurableVerified, MatrixOperation.AtomicReplace, PathRole.Journal, DetachedSpatialMigrationTransaction.FinalizationFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, receipt:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("restoration-intent-containment", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.Containment, PathRole.Directory, DetachedSpatialMigrationTransaction.PathInvalidReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restoration-intent-write", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.DurableWrite, PathRole.RestorationIntent, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restoration-intent-reread", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.Read, PathRole.RestorationIntent, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, intent:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-staging-move", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicMove, PathRole.Quarantine, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-active-replace-before", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicReplace, PathRole.Active, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, backup:true, restore:true, intent:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("restore-active-replace-after", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicReplace, PathRole.Active, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, intent:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1, true);
                yield return C("restore-originalrestored-advance", EntryPoint.Recover, MatrixCheckpoint.RestorationAttempt, MatrixOperation.AtomicReplace, PathRole.Journal, DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, intent:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.OriginalRestored, MatrixAuthority.Original, ActiveExpectation.Original, 1);
                yield return C("finalization-cleanup-delete", EntryPoint.Recover, MatrixCheckpoint.FinalizationAttempt, MatrixOperation.Delete, PathRole.Backup, DetachedSpatialMigrationTransaction.SuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, E(journal:true, receipt:true), 1, EntryPoint.Recover, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
                yield return C("resumed-execute-existing-candidate", EntryPoint.ResumedExecute, MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, PathRole.Active, DetachedSpatialMigrationTransaction.ReplacementFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Original, ActiveExpectation.Original, E(journal:true, backup:true, candidate:true), 0, EntryPoint.ResumedExecute, true, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, ActiveExpectation.Candidate, 1);
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void PacketC_BoundedFailureMatrix_ExecutesDeclaredFaultAndRestartScenario(MatrixCase row)
        {
            Scenario scenario = Scenario.Create(TestContext.CurrentContext.Test.Name, row);
            scenario.Inject(row);

            DetachedSpatialMigrationOutcome first = Invoke(row.EntryPoint, scenario);

            AssertOutcome(first, row.FirstReason, row.FirstStage, row.FirstAuthority);
            scenario.FileSystem.DisableFailure();
            AssertActiveBytes(scenario, row.FirstActive);
            AssertEvidence(scenario, row.FirstEvidence);
            Assert.That(AuthorityTransitions(scenario), Is.EqualTo(row.FirstTransitions), row.Name);
            Assert.That(scenario.LiveJournalCount(), Is.LessThanOrEqualTo(1), row.Name);
            Assert.That(scenario.FailedOperationMatches(row.Operation, row.Role), Is.True, row.Name);
            AssertChronologicalDuplicateFree(first);

            int candidateWritesBeforeRetry = scenario.CandidateStagingWriteCount();
            DetachedSpatialMigrationOutcome retry = Invoke(row.RetryEntryPoint, scenario);

            Assert.That(retry.IsSuccess, Is.EqualTo(row.RetrySuccess), row.Name);
            AssertOutcome(retry, row.RetrySuccess ? DetachedSpatialMigrationTransaction.SuccessReason : retry.Reason,
                row.FinalStage, row.FinalAuthority, checkReason: row.RetrySuccess);
            AssertActiveBytes(scenario, row.FinalActive);
            Assert.That(AuthorityTransitions(scenario), Is.EqualTo(row.FinalTransitions), row.Name);
            Assert.That(scenario.LiveJournalCount(), Is.LessThanOrEqualTo(1), row.Name);
            Assert.That(scenario.CandidateStagingWriteCount(), Is.EqualTo(candidateWritesBeforeRetry), row.Name);
            AssertChronologicalDuplicateFree(retry);
        }

        [Test]
        public void PacketC_BoundedFailureMatrix_CoversExpectedDimensions()
        {
            MatrixCase[] rows = Cases.ToArray();
            Assert.That(rows.Length, Is.InRange(18, 30));
            foreach (MatrixOperation operation in (MatrixOperation[])Enum.GetValues(typeof(MatrixOperation)))
                Assert.That(rows.Any(row => row.Operation == operation), Is.True, operation.ToString());
            foreach (MatrixCheckpoint checkpoint in (MatrixCheckpoint[])Enum.GetValues(typeof(MatrixCheckpoint)))
                Assert.That(rows.Any(row => row.Checkpoint == checkpoint), Is.True, checkpoint.ToString());
            Assert.That(rows.Any(row => row.AfterMutation), Is.True);
        }

        private static MatrixCase C(string name, EntryPoint entry, MatrixCheckpoint checkpoint,
            MatrixOperation operation, PathRole role, string reason, SpatialMigrationJournalStage? stage,
            MatrixAuthority authority, ActiveExpectation active, RetainedEvidence evidence, int transitions,
            EntryPoint retry, bool retrySuccess, SpatialMigrationJournalStage? finalStage,
            MatrixAuthority finalAuthority, ActiveExpectation finalActive, int finalTransitions,
            bool after = false) => new MatrixCase(name, entry, checkpoint, operation, -1, role, after, reason,
                stage, authority, active, evidence, transitions, retry, retrySuccess, finalStage, finalAuthority,
                finalActive, finalTransitions);
        private static RetainedEvidence E(bool journal = false, bool backup = false, bool candidate = false,
            bool restore = false, bool intent = false, bool quarantine = false, bool receipt = false) =>
            new RetainedEvidence(journal, backup, candidate, restore, intent, quarantine, receipt);

        private static DetachedSpatialMigrationOutcome Invoke(EntryPoint entryPoint, Scenario scenario)
        {
            var transaction = new DetachedSpatialMigrationTransaction(scenario.FileSystem,
                Gd66DetachedSpatialMigrationTransactionTests.Recovery(scenario.Fixture, scenario.RecoveryLegacyBytes));
            return entryPoint == EntryPoint.FreshExecute || entryPoint == EntryPoint.ResumedExecute
                ? transaction.Execute(scenario.ActivePath, scenario.Fixture.Result.Attempt)
                : transaction.Recover(scenario.ActivePath);
        }

        private static void AssertOutcome(DetachedSpatialMigrationOutcome outcome, string reason,
            SpatialMigrationJournalStage? stage, MatrixAuthority authority, bool checkReason = true)
        {
            if (checkReason) Assert.That(outcome.Reason, Is.EqualTo(reason));
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
            Assert.That(scenario.ExistsEnding(".journal.json"), Is.EqualTo(expected.Journal));
            Assert.That(scenario.ExistsEnding(".backup"), Is.EqualTo(expected.Backup));
            Assert.That(scenario.ExistsEnding(".candidate.tmp"), Is.EqualTo(expected.Candidate));
            Assert.That(scenario.ExistsEnding(".restore"), Is.EqualTo(expected.Restore));
            Assert.That(scenario.ExistsEnding(".restore.intent"), Is.EqualTo(expected.Intent));
            Assert.That(scenario.ExistsContaining("gd66-quarantine"), Is.EqualTo(expected.Quarantine));
            Assert.That(scenario.ExistsEnding(".receipt.json"), Is.EqualTo(expected.Receipt));
        }

        private static void AssertChronologicalDuplicateFree(DetachedSpatialMigrationOutcome outcome)
        {
            string[] diagnostics = outcome.Diagnostics == null ? Array.Empty<string>() : outcome.Diagnostics.ToArray();
            Assert.That(diagnostics.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(diagnostics.Length));
        }

        private static int AuthorityTransitions(Scenario scenario) => scenario.FileSystem.Operations.Count(operation =>
            operation.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace &&
            Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(operation.Paths[1], scenario.ActivePath));

        private sealed class Scenario
        {
            internal Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture Fixture { get; private set; }
            internal Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem FileSystem { get; private set; }
            internal string ActivePath { get; private set; }
            internal SpatialMigrationSidecarNames Names { get; private set; }
            internal byte[] CandidateBytes { get; private set; }
            internal byte[] UntrustedBytes { get; private set; }
            internal byte[] RecoveryLegacyBytes { get; private set; }

            internal static Scenario Create(string identity, MatrixCase row)
            {
                var scenario = new Scenario();
                scenario.Fixture = Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
                scenario.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
                scenario.ActivePath = Gd66DetachedSpatialMigrationTransactionTests.ActivePath("matrix-" + identity);
                scenario.CandidateBytes = scenario.Fixture.Result.Attempt.Candidate.GetBytes();
                scenario.UntrustedBytes = new byte[] { 0x7b, 0x7d, 0x20, 0x30 };
                scenario.Materialize(row.Checkpoint);
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
                Gd66DetachedSpatialMigrationTransactionTests.OperationType type = ToOperationType(row.Operation);
                if (row.Name.Contains("reread"))
                    FileSystem.SubstituteNextRead(ActivePath, UntrustedBytes);
                if (row.AfterMutation) FileSystem.EnableFailureAfterMutation(type, row.Occurrence);
                else FileSystem.EnableFailure(type, row.Occurrence);
            }

            internal bool FailedOperationMatches(MatrixOperation operation, PathRole role)
            {
                Gd66DetachedSpatialMigrationTransactionTests.OperationType type = ToOperationType(operation);
                return FileSystem.Operations.Any(value => value.Type == type && MatchesRole(value, role));
            }

            internal bool ExistsEnding(string suffix) => FileSystem.Paths.Any(path => path.EndsWith(suffix,
                StringComparison.Ordinal));
            internal bool ExistsContaining(string text) => FileSystem.Paths.Any(path => path.Contains(text));
            internal int LiveJournalCount() => FileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal));
            internal int CandidateStagingWriteCount() => FileSystem.Operations.Count(operation =>
                operation.Type == Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write &&
                operation.Paths.Any(path => path.EndsWith(".candidate.tmp", StringComparison.Ordinal)));

            private bool MatchesRole(Gd66DetachedSpatialMigrationTransactionTests.FileOperation operation,
                PathRole role)
            {
                return operation.Paths.Any(path =>
                    role == PathRole.Active ? Gd66DetachedSpatialMigrationTransactionTests.PathComparer().Equals(path, ActivePath) :
                    role == PathRole.Journal ? path.EndsWith(".journal.json", StringComparison.Ordinal) :
                    role == PathRole.JournalNext ? path.EndsWith(".journal.json.next", StringComparison.Ordinal) :
                    role == PathRole.Backup ? path.EndsWith(".backup", StringComparison.Ordinal) :
                    role == PathRole.CandidateStaging ? path.EndsWith(".candidate.tmp", StringComparison.Ordinal) :
                    role == PathRole.RestoreStaging ? path.EndsWith(".restore", StringComparison.Ordinal) :
                    role == PathRole.RestorationIntent ? path.EndsWith(".restore.intent", StringComparison.Ordinal) :
                    role == PathRole.Receipt ? path.EndsWith(".receipt.json", StringComparison.Ordinal) :
                    role == PathRole.Quarantine ? path.Contains("gd66-quarantine") || path.EndsWith(".restore", StringComparison.Ordinal) :
                    role == PathRole.Directory ? Directory.Exists(path) || path == Path.GetDirectoryName(ActivePath) || !Path.HasExtension(path) : false);
            }
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
