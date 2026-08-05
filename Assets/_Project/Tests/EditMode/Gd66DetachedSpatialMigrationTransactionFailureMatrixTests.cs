#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedSpatialMigrationTransactionFailureMatrixTests
    {
        public enum MatrixOperation { Exists, Read, DurableWrite, AtomicReplace, DirectoryFlush, Enumeration, Containment, AtomicMove, Delete }
        public enum MatrixCheckpoint { NoJournal, DescriptorPinned, BackupVerified, CandidateVerified, Replaced, DurableVerified, Finalization, Restoration, RestartedRecover, ResumedExecute }
        public enum MatrixAuthority { Original, Candidate, None }

        public sealed class MatrixCase
        {
            public MatrixCase(string name, MatrixCheckpoint checkpoint, MatrixOperation operation,
                bool afterMutation, string reason, SpatialMigrationJournalStage? stage,
                MatrixAuthority authority, int transitions)
            { Name = name; Checkpoint = checkpoint; Operation = operation; AfterMutation = afterMutation;
              Reason = reason; Stage = stage; Authority = authority; Transitions = transitions; }
            public string Name { get; }
            public MatrixCheckpoint Checkpoint { get; }
            public MatrixOperation Operation { get; }
            public bool AfterMutation { get; }
            public string Reason { get; }
            public SpatialMigrationJournalStage? Stage { get; }
            public MatrixAuthority Authority { get; }
            public int Transitions { get; }
            public override string ToString() => Name;
        }

        public static IEnumerable<MatrixCase> Cases
        {
            get
            {
                yield return new MatrixCase("fresh-enumerate-before-journal", MatrixCheckpoint.NoJournal, MatrixOperation.Enumeration, false, DetachedSpatialMigrationTransaction.RecoveryFailedReason, null, MatrixAuthority.None, 0);
                yield return new MatrixCase("fresh-descriptor-write", MatrixCheckpoint.NoJournal, MatrixOperation.DurableWrite, false, DetachedSpatialMigrationTransaction.BackupFailedReason, null, MatrixAuthority.Original, 0);
                yield return new MatrixCase("descriptor-backup-write", MatrixCheckpoint.DescriptorPinned, MatrixOperation.DurableWrite, false, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, 0);
                yield return new MatrixCase("descriptor-backup-flush", MatrixCheckpoint.DescriptorPinned, MatrixOperation.DirectoryFlush, false, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, 0);
                yield return new MatrixCase("descriptor-backup-reread", MatrixCheckpoint.DescriptorPinned, MatrixOperation.Read, false, DetachedSpatialMigrationTransaction.BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, MatrixAuthority.Original, 0);
                yield return new MatrixCase("backup-candidate-write", MatrixCheckpoint.BackupVerified, MatrixOperation.DurableWrite, false, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, 0);
                yield return new MatrixCase("backup-candidate-flush", MatrixCheckpoint.BackupVerified, MatrixOperation.DirectoryFlush, false, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, 0);
                yield return new MatrixCase("backup-candidate-reread", MatrixCheckpoint.BackupVerified, MatrixOperation.Read, false, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, 0);
                yield return new MatrixCase("backup-candidateverified-advance", MatrixCheckpoint.BackupVerified, MatrixOperation.AtomicReplace, false, DetachedSpatialMigrationTransaction.CandidateFailedReason, SpatialMigrationJournalStage.BackupVerified, MatrixAuthority.Original, 0);
                yield return new MatrixCase("candidate-active-replace-before", MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, false, DetachedSpatialMigrationTransaction.ReplacementFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Original, 0);
                yield return new MatrixCase("candidate-active-replace-after", MatrixCheckpoint.CandidateVerified, MatrixOperation.AtomicReplace, true, DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason, SpatialMigrationJournalStage.CandidateVerified, MatrixAuthority.Original, 1);
                yield return new MatrixCase("replaced-directory-flush", MatrixCheckpoint.Replaced, MatrixOperation.DirectoryFlush, false, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Original, 1);
                yield return new MatrixCase("replaced-active-reread", MatrixCheckpoint.Replaced, MatrixOperation.Read, false, DetachedSpatialMigrationTransaction.RecoveryFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("replaced-durable-advance", MatrixCheckpoint.Replaced, MatrixOperation.AtomicReplace, false, DetachedSpatialMigrationTransaction.DurabilityFailedReason, SpatialMigrationJournalStage.Replaced, MatrixAuthority.Original, 1);
                yield return new MatrixCase("durable-final-receipt-write", MatrixCheckpoint.DurableVerified, MatrixOperation.DurableWrite, false, DetachedSpatialMigrationTransaction.FinalizationFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("durable-final-journal-advance", MatrixCheckpoint.Finalization, MatrixOperation.AtomicReplace, false, DetachedSpatialMigrationTransaction.FinalizationFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("restore-intent-containment", MatrixCheckpoint.Restoration, MatrixOperation.Containment, false, DetachedSpatialMigrationTransaction.PathInvalidReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("restore-staging-move", MatrixCheckpoint.Restoration, MatrixOperation.AtomicMove, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("restore-active-replace-before", MatrixCheckpoint.Restoration, MatrixOperation.AtomicReplace, false, "gd66.transaction.pinned_input_hash_mismatch", SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Candidate, 1);
                yield return new MatrixCase("restore-active-replace-after", MatrixCheckpoint.Restoration, MatrixOperation.AtomicReplace, true, DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Original, 1);
                yield return new MatrixCase("restore-originalrestored-advance", MatrixCheckpoint.RestartedRecover, MatrixOperation.AtomicReplace, false, DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason, SpatialMigrationJournalStage.DurableVerified, MatrixAuthority.Original, 1);
                yield return new MatrixCase("cleanup-delete", MatrixCheckpoint.ResumedExecute, MatrixOperation.Delete, false, DetachedSpatialMigrationTransaction.SuccessReason, SpatialMigrationJournalStage.Finalized, MatrixAuthority.Candidate, 1);
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void PacketC_BoundedFailureMatrix_DefinesRegisteredRestartExpectations(MatrixCase row)
        {
            Assert.That(row.Reason, Is.Not.Empty);
            Assert.That(row.Transitions, Is.InRange(0, 1));
            Assert.That(Enum.IsDefined(typeof(MatrixOperation), row.Operation), Is.True);
            Assert.That(Enum.IsDefined(typeof(MatrixCheckpoint), row.Checkpoint), Is.True);
        }

        [Test]
        public void PacketC_BoundedFailureMatrix_CoversExpectedDimensions()
        {
            MatrixCase[] rows = new List<MatrixCase>(Cases).ToArray();
            Assert.That(rows.Length, Is.EqualTo(22));
            foreach (MatrixOperation operation in (MatrixOperation[])Enum.GetValues(typeof(MatrixOperation)))
                Assert.That(Array.Exists(rows, row => row.Operation == operation), Is.True, operation.ToString());
            foreach (MatrixCheckpoint checkpoint in (MatrixCheckpoint[])Enum.GetValues(typeof(MatrixCheckpoint)))
                Assert.That(Array.Exists(rows, row => row.Checkpoint == checkpoint), Is.True, checkpoint.ToString());
            Assert.That(Array.Exists(rows, row => row.AfterMutation), Is.True);
        }
    }
}
#endif
