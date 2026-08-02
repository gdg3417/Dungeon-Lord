using System;
using System.IO;
using System.Collections.Generic;

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
        IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern);
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
            // Never report DurableVerified when the managed runtime cannot provide directory fsync.
            // Supported platforms must inject an implementation with a real durability primitive.
            throw new PlatformNotSupportedException();
        }
        public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern) =>
            Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
    }

    public sealed class DetachedSpatialMigrationTransaction
    {
        public const string SuccessReason = "gd66.success.migrated";
        public const string BackupFailedReason = "gd66.transaction.backup_failed";
        public const string CandidateFailedReason = "gd66.transaction.candidate_invalid";
        public const string ReplacementFailedReason = "gd66.transaction.commit_failed";
        public const string DurabilityFailedReason = "gd66.transaction.durability_failed";
        public const string FinalizationFailedReason = "gd66.transaction.finalized_stage_write_failed";
        public const string MultipleAttemptsReason = "gd66.transaction.multiple_live_attempts";
        public const string FingerprintMismatchReason = "gd66.transaction.input_fingerprint_mismatch";
        public const string ActivePayloadUnknownReason = "gd66.transaction.active_payload_unknown";
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
                        StringComparison.Ordinal))
                    return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);

                string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(descriptor, limits);
                string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(descriptor.OriginalPayloadSha256, fingerprint);
                string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
                if (!string.Equals(candidate.MigrationTransactionId, transactionId, StringComparison.Ordinal) ||
                    !string.Equals(candidate.MigrationDescriptorFingerprint, fingerprint, StringComparison.Ordinal))
                    return Failure(FingerprintMismatchReason, null, TrustedActive(activePath, exactOriginalBytes, candidate.GetBytes()));

                SpatialContractResult<SpatialMigrationSidecarNames> names =
                    SpatialMigrationSidecarPaths.Derive(Path.GetFileName(activePath), transactionId);
                if (!names.IsValid) return Failure(PathInvalidReason, null, SpatialTrustedPayload.Original);
                if (!Resolve(directory, names.Value.Journal, out string journalPath) ||
                    !Resolve(directory, names.Value.OriginalBackup, out string backupPath) ||
                    !Resolve(directory, names.Value.CandidateStaging, out string stagingPath))
                    return Failure(PathInvalidReason, null, SpatialTrustedPayload.Original);

                SpatialContractResult<SpatialMigrationJournal> existing = FindLiveJournal(
                    directory, Path.GetFileNameWithoutExtension(activePath), out int liveCount);
                if (liveCount > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                SpatialMigrationJournal journal;
                if (liveCount == 1)
                {
                    if (!existing.IsValid || !string.Equals(existing.Value.TransactionId, transactionId, StringComparison.Ordinal))
                        return Failure(FingerprintMismatchReason, existing.IsValid ? existing.Value.Stage : (SpatialMigrationJournalStage?)null,
                            TrustedActive(activePath, exactOriginalBytes, candidate.GetBytes()));
                    journal = existing.Value;
                }
                else
                {
                    if (!Same(fileSystem.ReadAllBytes(activePath), exactOriginalBytes))
                        return Failure(ActivePayloadUnknownReason, null, SpatialTrustedPayload.None);
                    journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                        null, null, SpatialMigrationJournalStage.DescriptorPinned);
                    if (!WriteJournal(journalPath, journal))
                        return Failure(BackupFailedReason, null, SpatialTrustedPayload.Original);
                }
                return Resume(activePath, directory, journalPath, backupPath, stagingPath, exactOriginalBytes,
                    candidate, descriptor, fingerprint, identity, transactionId, names.Value, journal);
            }
            catch { return Failure(DurabilityFailedReason, null, SpatialTrustedPayload.None); }
        }

        private DetachedSpatialMigrationOutcome Resume(string activePath, string directory, string journalPath,
            string backupPath, string stagingPath, byte[] original, DetachedWholeSaveCandidate candidate,
            SpatialMigrationInputDescriptor descriptor, string fingerprint, string identity, string transactionId,
            SpatialMigrationSidecarNames names, SpatialMigrationJournal journal)
        {
            byte[] candidateBytes = candidate.GetBytes();
            SpatialMigrationJournalStage persisted = journal.Stage;
            if (persisted == SpatialMigrationJournalStage.Finalized)
                return Same(fileSystem.ReadAllBytes(activePath), candidateBytes)
                    ? new DetachedSpatialMigrationOutcome(true, SuccessReason, persisted, SpatialTrustedPayload.Candidate)
                    : Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);

            if (persisted == SpatialMigrationJournalStage.DescriptorPinned)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), original))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                try { if (!fileSystem.Exists(backupPath)) fileSystem.WriteAllBytesDurable(backupPath, original); }
                catch { return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.Original); }
                if (!Same(fileSystem.ReadAllBytes(backupPath), original))
                    return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.Original);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, null, SpatialMigrationJournalStage.BackupVerified);
                if (!RewriteJournal(journalPath, next)) return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.Backup);
                journal = next; persisted = journal.Stage;
            }
            if (!Same(fileSystem.ReadAllBytes(backupPath), original))
                return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.None);

            if (persisted == SpatialMigrationJournalStage.BackupVerified)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), original))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                try { if (!fileSystem.Exists(stagingPath)) fileSystem.WriteAllBytesDurable(stagingPath, candidateBytes); }
                catch { return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Backup); }
                if (!Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes) ||
                    candidate.Sha256 != SpatialContractSha256.Compute(candidateBytes))
                    return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Backup);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.CandidateVerified);
                if (!RewriteJournal(journalPath, next)) return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Backup);
                journal = next; persisted = journal.Stage;
            }
            if (!string.Equals(journal.ExpectedCandidateSha256, candidate.Sha256, StringComparison.Ordinal))
                return Failure(FingerprintMismatchReason, persisted, SpatialTrustedPayload.Backup);

            if (persisted == SpatialMigrationJournalStage.CandidateVerified)
            {
                bool alreadyReplaced = Same(fileSystem.ReadAllBytes(activePath), candidateBytes);
                if (!alreadyReplaced)
                {
                    if (!Same(fileSystem.ReadAllBytes(activePath), original) || !Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes))
                        return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                    try { fileSystem.ReplaceSameDirectoryAtomic(stagingPath, activePath); }
                    catch { return Failure(ReplacementFailedReason, persisted, SpatialTrustedPayload.Backup); }
                }
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, next)) return Failure(ReplacementFailedReason, persisted, SpatialTrustedPayload.Candidate);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.Replaced)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Backup);
                try { fileSystem.FlushDirectory(directory); }
                catch { return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Candidate); }
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Backup);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, next)) return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Candidate);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.DurableVerified)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, next)) return Failure(FinalizationFailedReason, persisted, SpatialTrustedPayload.Candidate);
                journal = next;
            }
            return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage, SpatialTrustedPayload.Candidate);
        }

        private SpatialContractResult<SpatialMigrationJournal> FindLiveJournal(string directory, string stem, out int count)
        {
            count = 0; SpatialContractResult<SpatialMigrationJournal> found = default(SpatialContractResult<SpatialMigrationJournal>);
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory, stem + ".gd66-*.journal.json");
            for (int i = 0; i < paths.Count; i++)
            {
                SpatialContractResult<SpatialMigrationJournal> parsed = SpatialMigrationJournalContracts.Parse(
                    fileSystem.ReadAllBytes(paths[i]), limits);
                if (!parsed.IsValid || parsed.Value.Stage == SpatialMigrationJournalStage.OriginalRestored) continue;
                count++; found = parsed;
            }
            return found;
        }

        private SpatialTrustedPayload TrustedActive(string activePath, byte[] original, byte[] candidate)
        {
            byte[] active = fileSystem.ReadAllBytes(activePath);
            if (Same(active, original)) return SpatialTrustedPayload.Original;
            if (Same(active, candidate)) return SpatialTrustedPayload.Candidate;
            return SpatialTrustedPayload.None;
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
            try
            {
                if (!fileSystem.Exists(temporary)) fileSystem.WriteAllBytesDurable(temporary, bytes.Value);
                if (!Same(fileSystem.ReadAllBytes(temporary), bytes.Value)) return false;
                fileSystem.ReplaceSameDirectoryAtomic(temporary, path);
            }
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
