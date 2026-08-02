using System;
using System.IO;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialTrustedPayload { None, Original, Backup, Candidate }

    public sealed class DetachedSpatialMigrationOutcome
    {
        internal DetachedSpatialMigrationOutcome(bool success, string reason,
            SpatialMigrationJournalStage? stage, SpatialTrustedPayload trusted)
        { IsSuccess = success; Reason = reason; Stage = stage; TrustedPayload = trusted; }
        public bool IsSuccess { get; }
        public string Reason { get; }
        public SpatialMigrationJournalStage? Stage { get; }
        public SpatialTrustedPayload TrustedPayload { get; }
    }

    public interface ISpatialMigrationFileSystem
    {
        bool Exists(string path);
        byte[] ReadAllBytes(string path);
        void WriteAllBytesDurable(string path, byte[] bytes);
        void ReplaceSameDirectoryAtomic(string stagingPath, string activePath);
        void FlushDirectory(string directoryPath);
    }

    public sealed class RuntimeSpatialMigrationFileSystem : ISpatialMigrationFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public void WriteAllBytesDurable(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
        }
        public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath)
        {
            if (!string.Equals(Path.GetDirectoryName(stagingPath), Path.GetDirectoryName(activePath),
                StringComparison.Ordinal)) throw new IOException();
            File.Replace(stagingPath, activePath, null);
        }
        public void FlushDirectory(string directoryPath)
        {
            // Unity's supported managed profile has no portable directory fsync. Atomic replacement
            // is still explicit; platforms that can guarantee directory durability inject it here.
        }
    }

    public sealed class DetachedSpatialMigrationTransaction
    {
        public const string SuccessReason = "gd66.success.migrated";
        public const string BackupFailedReason = "gd66.transaction.backup_failed";
        public const string CandidateFailedReason = "gd66.transaction.candidate_write_failed";
        public const string ReplacementFailedReason = "gd66.transaction.atomic_replace_failed";
        public const string DurabilityFailedReason = "gd66.transaction.durability_failed";
        public const string JournalFailedReason = "gd66.transaction.journal_write_failed";
        public const string PathInvalidReason = "gd66.transaction.path_invalid";

        private readonly ISpatialMigrationFileSystem fileSystem;
        private readonly SpatialSerializedInputLimits limits;

        public DetachedSpatialMigrationTransaction(ISpatialMigrationFileSystem fileSystem,
            SpatialSerializedInputLimits limits)
        { this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
          if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); this.limits = limits; }

        public DetachedSpatialMigrationOutcome Execute(string activePath, byte[] exactOriginalBytes,
            DetachedWholeSaveCandidate candidate, SpatialMigrationInputDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(activePath) || exactOriginalBytes == null || candidate == null || descriptor == null)
                return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
            try
            {
                string directory = Path.GetFullPath(Path.GetDirectoryName(activePath));
                string normalizedActive = Path.GetFullPath(activePath);
                if (!string.Equals(normalizedActive, activePath, StringComparison.Ordinal) ||
                    !string.Equals(SpatialContractSha256.Compute(exactOriginalBytes), descriptor.OriginalPayloadSha256,
                        StringComparison.Ordinal) || !Same(fileSystem.ReadAllBytes(activePath), exactOriginalBytes))
                    return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);

                string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(descriptor, limits);
                string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(descriptor.OriginalPayloadSha256, fingerprint);
                string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
                SpatialContractResult<SpatialMigrationSidecarNames> names =
                    SpatialMigrationSidecarPaths.Derive(Path.GetFileName(activePath), transactionId);
                if (!names.IsValid) return Failure(PathInvalidReason, null, SpatialTrustedPayload.Original);
                if (!Resolve(directory, names.Value.Journal, out string journalPath) ||
                    !Resolve(directory, names.Value.OriginalBackup, out string backupPath) ||
                    !Resolve(directory, names.Value.CandidateStaging, out string stagingPath))
                    return Failure(PathInvalidReason, null, SpatialTrustedPayload.Original);

                SpatialMigrationJournal journal = CreateJournal(descriptor, fingerprint, identity, transactionId,
                    names.Value, null, null, SpatialMigrationJournalStage.DescriptorPinned);
                if (!WriteJournal(journalPath, journal)) return Failure(JournalFailedReason, null, SpatialTrustedPayload.Original);

                try { fileSystem.WriteAllBytesDurable(backupPath, exactOriginalBytes); }
                catch { return Failure(BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, SpatialTrustedPayload.Original); }
                if (!Same(fileSystem.ReadAllBytes(backupPath), exactOriginalBytes))
                    return Failure(BackupFailedReason, SpatialMigrationJournalStage.DescriptorPinned, SpatialTrustedPayload.Original);
                journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                    descriptor.OriginalPayloadSha256, null, SpatialMigrationJournalStage.BackupVerified);
                if (!RewriteJournal(journalPath, journal)) return Failure(JournalFailedReason, journal.Stage, SpatialTrustedPayload.Backup);

                byte[] candidateBytes = candidate.GetBytes();
                try { fileSystem.WriteAllBytesDurable(stagingPath, candidateBytes); }
                catch { return Failure(CandidateFailedReason, journal.Stage, SpatialTrustedPayload.Backup); }
                if (!Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes) ||
                    candidate.Sha256 != SpatialContractSha256.Compute(candidateBytes))
                    return Failure(CandidateFailedReason, journal.Stage, SpatialTrustedPayload.Backup);
                journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.CandidateVerified);
                if (!RewriteJournal(journalPath, journal)) return Failure(JournalFailedReason, journal.Stage, SpatialTrustedPayload.Backup);

                try { fileSystem.ReplaceSameDirectoryAtomic(stagingPath, activePath); }
                catch { return Failure(ReplacementFailedReason, journal.Stage, SpatialTrustedPayload.Backup); }
                journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, journal)) return Failure(JournalFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                try { fileSystem.FlushDirectory(directory); }
                catch { return Failure(DurabilityFailedReason, journal.Stage, SpatialTrustedPayload.Candidate); }
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(DurabilityFailedReason, journal.Stage, SpatialTrustedPayload.Backup);
                journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, journal)) return Failure(JournalFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, journal)) return Failure(JournalFailedReason,
                    SpatialMigrationJournalStage.DurableVerified, SpatialTrustedPayload.Candidate);
                return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage, SpatialTrustedPayload.Candidate);
            }
            catch { return Failure(DurabilityFailedReason, null, SpatialTrustedPayload.None); }
        }

        private bool Resolve(string directory, string relative, out string path) =>
            SpatialMigrationSidecarPaths.TryResolveContained(directory, relative,
                SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters, out path);
        private bool WriteJournal(string path, SpatialMigrationJournal journal)
        {
            SpatialContractResult<byte[]> bytes = SpatialMigrationJournalContracts.Serialize(journal, limits);
            if (!bytes.IsValid) return false;
            try { fileSystem.WriteAllBytesDurable(path, bytes.Value); }
            catch { return false; }
            return VerifyJournal(path, bytes.Value);
        }
        private bool RewriteJournal(string path, SpatialMigrationJournal journal)
        {
            SpatialContractResult<byte[]> bytes = SpatialMigrationJournalContracts.Serialize(journal, limits);
            if (!bytes.IsValid) return false;
            string temporary = path + ".next";
            try { fileSystem.WriteAllBytesDurable(temporary, bytes.Value); fileSystem.ReplaceSameDirectoryAtomic(temporary, path); }
            catch { return false; }
            return VerifyJournal(path, bytes.Value);
        }
        private bool VerifyJournal(string path, byte[] expected)
        {
            byte[] actual = fileSystem.ReadAllBytes(path);
            return Same(actual, expected) && SpatialMigrationJournalContracts.Parse(actual, limits).IsValid;
        }
        private static SpatialMigrationJournal CreateJournal(SpatialMigrationInputDescriptor descriptor,
            string fingerprint, string identity, string transactionId, SpatialMigrationSidecarNames names,
            string backupHash, string candidateHash, SpatialMigrationJournalStage stage) =>
            new SpatialMigrationJournal(SpatialMigrationContractIdentity.JournalSchemaVersion, descriptor,
                fingerprint, identity, transactionId, names.Journal, names.OriginalBackup,
                names.CandidateStaging, stage == SpatialMigrationJournalStage.DurableVerified ||
                stage == SpatialMigrationJournalStage.Finalized ? names.FinalizedReceipt : null,
                descriptor.OriginalPayloadSha256, backupHash, candidateHash, stage);
        private static bool Same(byte[] left, byte[] right)
        { if (left == null || right == null || left.Length != right.Length) return false;
          for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false; return true; }
        private static DetachedSpatialMigrationOutcome Failure(string reason,
            SpatialMigrationJournalStage? stage, SpatialTrustedPayload trusted) =>
            new DetachedSpatialMigrationOutcome(false, reason, stage, trusted);
    }
}
