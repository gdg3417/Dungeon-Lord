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
        IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern, int maximumResults);
        bool IsPathContainedWithoutRedirection(string directoryPath, string path);
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
        public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern, int maximumResults)
        {
            string[] paths = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
            if (paths.Length > maximumResults) throw new IOException();
            return paths;
        }
        public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
        {
            string directory = Path.GetFullPath(directoryPath);
            string candidate = Path.GetFullPath(path);
            string prefix = directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? directory : directory + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, StringComparison.Ordinal)) return false;
            for (string current = candidate; !string.Equals(current, directory, StringComparison.Ordinal);
                current = Path.GetDirectoryName(current))
            {
                if (string.IsNullOrEmpty(current)) return false;
                if (File.Exists(current) || Directory.Exists(current))
                {
                    FileAttributes attributes = File.GetAttributes(current);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) return false;
                }
            }
            return true;
        }
    }

    public sealed class DetachedSpatialMigrationTransaction
    {
        public const string SuccessReason = "gd66.success.migrated";
        public const string EmptySuccessReason = "gd66.success.empty_migrated";
        public const string BackupFailedReason = "gd66.transaction.backup_failed";
        public const string CandidateFailedReason = "gd66.transaction.candidate_invalid";
        public const string ReplacementFailedReason = "gd66.transaction.commit_failed";
        public const string DurabilityFailedReason = "gd66.transaction.durability_failed";
        public const string FinalizationFailedReason = "gd66.transaction.finalized_stage_write_failed";
        public const string MultipleAttemptsReason = "gd66.transaction.multiple_live_attempts";
        public const string FingerprintMismatchReason = "gd66.transaction.input_fingerprint_mismatch";
        public const string ActivePayloadUnknownReason = "gd66.transaction.active_payload_unknown";
        public const string NoTrustedPayloadReason = "gd66.transaction.no_trusted_active_payload";
        public const string RecoveryFailedReason = "gd66.transaction.recovery_failed";
        public const string CandidateAbsentReason = "gd66.transaction.candidate_absent";
        public const string BackupIncompleteReason = "gd66.transaction.backup_incomplete";
        public const string AlreadyCommittedReason = "gd66.success.already_committed";
        public const string RecoveredOriginalReason = "gd66.success.recovered_original";
        public const string ReplacedPendingDurabilityReason = "gd66.diagnostic.replaced_candidate_pending_durability";
        public const string PathInvalidReason = "gd66.transaction.path_invalid";

        private readonly ISpatialMigrationFileSystem fileSystem;
        private readonly SpatialSerializedInputLimits limits;

        public DetachedSpatialMigrationTransaction(ISpatialMigrationFileSystem fileSystem,
            SpatialSerializedInputLimits limits)
        { this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
          if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); this.limits = limits; }

        public DetachedSpatialMigrationOutcome Execute(string activePath,
            DetachedPreparedSpatialMigrationAttempt attempt)
        {
            if (attempt == null) return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
            DetachedSpatialMigrationOutcome outcome = ExecutePrepared(activePath, attempt.GetOriginalBytes(),
                attempt.Candidate, attempt.Descriptor);
            return outcome.IsSuccess && attempt.IsEmptyMigration && outcome.Reason == SuccessReason
                ? new DetachedSpatialMigrationOutcome(true, EmptySuccessReason, outcome.Stage,
                    outcome.TrustedPayload) : outcome;
        }

        private DetachedSpatialMigrationOutcome ExecutePrepared(string activePath, byte[] exactOriginalBytes,
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
                    !Resolve(directory, names.Value.CandidateStaging, out string stagingPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, journalPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, backupPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, stagingPath))
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
                    if (Same(fileSystem.ReadAllBytes(activePath), candidate.GetBytes()) &&
                        IsCanonicalSchemaSeven(candidate.GetBytes()))
                        return new DetachedSpatialMigrationOutcome(true, AlreadyCommittedReason, null,
                            SpatialTrustedPayload.Candidate);
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
            catch (IOException) { return Failure(DurabilityFailedReason, null, SpatialTrustedPayload.None); }
            catch (UnauthorizedAccessException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
            catch (ArgumentException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
            catch (NotSupportedException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
        }

        // Restart recovery deliberately consumes persisted evidence only. It never reconstructs C.
        public DetachedSpatialMigrationOutcome Recover(string activePath)
        {
            if (string.IsNullOrEmpty(activePath)) return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
            try
            {
                string normalizedActive = Path.GetFullPath(activePath);
                string directory = Path.GetFullPath(Path.GetDirectoryName(activePath));
                if (!string.Equals(normalizedActive, activePath, StringComparison.Ordinal) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, activePath))
                    return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
                SpatialContractResult<SpatialMigrationJournal> found = FindLiveJournal(
                    directory, Path.GetFileNameWithoutExtension(activePath), out int count);
                if (count > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                if (count == 0)
                    return IsCanonicalSchemaSeven(fileSystem.ReadAllBytes(activePath))
                        ? new DetachedSpatialMigrationOutcome(true, AlreadyCommittedReason, null, SpatialTrustedPayload.Candidate)
                        : Failure(NoTrustedPayloadReason, null, SpatialTrustedPayload.None);
                return RecoverLive(activePath, directory, found.Value);
            }
            catch (InvalidOperationException) { return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None); }
            catch (IOException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
            catch (UnauthorizedAccessException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
        }

        private DetachedSpatialMigrationOutcome RecoverLive(string activePath, string directory,
            SpatialMigrationJournal journal)
        {
            if (!ResolveEvidencePaths(directory, journal, out string journalPath, out string backupPath,
                out string stagingPath)) return Failure(PathInvalidReason, journal.Stage, SpatialTrustedPayload.None);
            byte[] active = fileSystem.ReadAllBytes(activePath);
            bool activeOriginal = HashIs(active, journal.OriginalPayloadSha256);
            byte[] backup = fileSystem.Exists(backupPath) ? fileSystem.ReadAllBytes(backupPath) : null;
            bool backupValid = HashIs(backup, journal.OriginalPayloadSha256);
            if (journal.Stage == SpatialMigrationJournalStage.DescriptorPinned)
                return activeOriginal
                    ? Failure(BackupIncompleteReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (!backupValid && !activeOriginal)
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (journal.Stage == SpatialMigrationJournalStage.BackupVerified)
                return activeOriginal ? Failure(CandidateAbsentReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Restore(journalPath, backupPath, activePath, directory, journal, backup);

            bool activeCandidate = HashIs(active, journal.ExpectedCandidateSha256) && IsCanonicalSchemaSeven(active);
            byte[] staged = fileSystem.Exists(stagingPath) ? fileSystem.ReadAllBytes(stagingPath) : null;
            bool stagedCandidate = HashIs(staged, journal.ExpectedCandidateSha256) &&
                IsCanonicalSchemaSeven(staged);
            if (journal.Stage == SpatialMigrationJournalStage.CandidateVerified)
            {
                if (!activeCandidate && !stagedCandidate)
                    return activeOriginal ? Failure(CandidateAbsentReason, journal.Stage, SpatialTrustedPayload.Original)
                        : Restore(journalPath, backupPath, activePath, directory, journal, backup);
                if (!activeCandidate)
                {
                    try { fileSystem.ReplaceSameDirectoryAtomic(stagingPath, activePath); }
                    catch
                    {
                        byte[] afterFailure = fileSystem.ReadAllBytes(activePath);
                        if (HashIs(afterFailure, journal.OriginalPayloadSha256))
                            return Failure(ReplacementFailedReason, journal.Stage, SpatialTrustedPayload.Original);
                        return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                    }
                }
                SpatialMigrationJournal replaced = CopyStage(journal, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, replaced))
                    return Failure(ReplacementFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                journal = replaced; activeCandidate = true;
            }
            if (journal.Stage == SpatialMigrationJournalStage.Replaced)
            {
                if (!activeCandidate) return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                try { fileSystem.FlushDirectory(directory); }
                catch { return Restore(journalPath, backupPath, activePath, directory, journal, backup,
                    DurabilityFailedReason); }
                active = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(active, journal.ExpectedCandidateSha256) || !IsCanonicalSchemaSeven(active))
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                SpatialMigrationJournal durable = CopyStage(journal, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, durable))
                    return Failure(DurabilityFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                journal = durable;
            }
            if (journal.Stage == SpatialMigrationJournalStage.DurableVerified)
            {
                byte[] durableActive = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(durableActive, journal.ExpectedCandidateSha256) ||
                    !IsCanonicalSchemaSeven(durableActive))
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                SpatialMigrationJournal finalized = CopyStage(journal, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, finalized))
                    return Failure(FinalizationFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                journal = finalized;
            }
            return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage, SpatialTrustedPayload.Candidate);
        }

        private DetachedSpatialMigrationOutcome Restore(string journalPath, string backupPath, string activePath,
            string directory, SpatialMigrationJournal journal, byte[] verifiedBackup,
            string restoredFailureReason = null)
        {
            if (!HashIs(verifiedBackup, journal.OriginalPayloadSha256))
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            try
            {
                if (!Same(fileSystem.ReadAllBytes(backupPath), verifiedBackup))
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                string restoreRelative = Path.GetFileName(backupPath) + ".restore";
                if (!Resolve(directory, restoreRelative, out string restoreStaging) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, restoreStaging))
                    return Failure(PathInvalidReason, journal.Stage, SpatialTrustedPayload.None);
                if (!fileSystem.Exists(restoreStaging))
                    fileSystem.WriteAllBytesDurable(restoreStaging, verifiedBackup);
                if (!Same(fileSystem.ReadAllBytes(restoreStaging), verifiedBackup))
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                fileSystem.ReplaceSameDirectoryAtomic(restoreStaging, activePath);
                fileSystem.FlushDirectory(directory);
                if (!HashIs(fileSystem.ReadAllBytes(activePath), journal.OriginalPayloadSha256))
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                SpatialMigrationJournal restored = CopyStage(journal, SpatialMigrationJournalStage.OriginalRestored);
                if (!RewriteJournal(journalPath, restored))
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                return restoredFailureReason == null
                    ? new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                        restored.Stage, SpatialTrustedPayload.Original)
                    : Failure(restoredFailureReason, restored.Stage, SpatialTrustedPayload.Original);
            }
            catch { return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None); }
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
                if (!RewriteJournal(journalPath, next)) return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.Original);
                journal = next; persisted = journal.Stage;
            }
            if (!Same(fileSystem.ReadAllBytes(backupPath), original))
                return Failure(BackupFailedReason, persisted, SpatialTrustedPayload.None);

            if (persisted == SpatialMigrationJournalStage.BackupVerified)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), original))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                try { if (!fileSystem.Exists(stagingPath)) fileSystem.WriteAllBytesDurable(stagingPath, candidateBytes); }
                catch { return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Original); }
                if (!Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes) ||
                    candidate.Sha256 != SpatialContractSha256.Compute(candidateBytes))
                    return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Original);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.CandidateVerified);
                if (!RewriteJournal(journalPath, next)) return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Original);
                journal = next; persisted = journal.Stage;
            }
            if (!string.Equals(journal.ExpectedCandidateSha256, candidate.Sha256, StringComparison.Ordinal))
                return Failure(FingerprintMismatchReason, persisted,
                    Same(fileSystem.ReadAllBytes(activePath), original) ? SpatialTrustedPayload.Original : SpatialTrustedPayload.None);

            if (persisted == SpatialMigrationJournalStage.CandidateVerified)
            {
                bool alreadyReplaced = Same(fileSystem.ReadAllBytes(activePath), candidateBytes);
                if (!alreadyReplaced)
                {
                    if (!Same(fileSystem.ReadAllBytes(activePath), original) || !Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes))
                        return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                    try { fileSystem.ReplaceSameDirectoryAtomic(stagingPath, activePath); }
                    catch
                    {
                        if (Same(fileSystem.ReadAllBytes(activePath), original))
                            return Failure(ReplacementFailedReason, persisted, SpatialTrustedPayload.Original);
                        return Restore(journalPath, backupPath, activePath, directory, journal,
                            fileSystem.ReadAllBytes(backupPath), ReplacementFailedReason);
                    }
                }
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, next)) return Failure(ReplacedPendingDurabilityReason,
                    persisted, SpatialTrustedPayload.Candidate);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.Replaced)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                {
                    if (Same(fileSystem.ReadAllBytes(activePath), original))
                        return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Original);
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason);
                }
                try { fileSystem.FlushDirectory(directory); }
                catch
                {
                    byte[] backupSnapshot = fileSystem.ReadAllBytes(backupPath);
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        backupSnapshot, DurabilityFailedReason);
                }
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason);
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
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory, stem + ".gd66-*.journal.json",
                limits.MaximumCollectionRecords);
            for (int i = 0; i < paths.Count; i++)
            {
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, paths[i])) continue;
                SpatialContractResult<SpatialMigrationJournal> parsed = SpatialMigrationJournalContracts.Parse(
                    fileSystem.ReadAllBytes(paths[i]), limits);
                if (!parsed.IsValid || parsed.Value.Stage == SpatialMigrationJournalStage.Finalized ||
                    parsed.Value.Stage == SpatialMigrationJournalStage.OriginalRestored ||
                    !string.Equals(Path.GetFileName(paths[i]), parsed.Value.RelativeJournalFilename,
                        StringComparison.Ordinal)) continue;
                if (!ResolveEvidencePaths(directory, parsed.Value, out string resolvedJournal,
                    out string ignoredBackup, out string ignoredStaging) ||
                    !string.Equals(resolvedJournal, paths[i], StringComparison.Ordinal)) continue;
                count++; found = parsed;
            }
            return found;
        }

        private bool ResolveEvidencePaths(string directory, SpatialMigrationJournal journal,
            out string journalPath, out string backupPath, out string stagingPath)
        {
            journalPath = null; backupPath = null; stagingPath = null;
            if (journal == null || !Resolve(directory, journal.RelativeJournalFilename, out journalPath) ||
                !Resolve(directory, journal.RelativeOriginalBackupFilename, out backupPath) ||
                !Resolve(directory, journal.RelativeCandidateStagingFilename, out stagingPath)) return false;
            string nextRelative = journal.RelativeJournalFilename + ".next";
            if (!SpatialMigrationSidecarPaths.IsValidRelativeFilename(nextRelative,
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) ||
                !Resolve(directory, nextRelative, out string nextPath)) return false;
            return fileSystem.IsPathContainedWithoutRedirection(directory, journalPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, backupPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, stagingPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, nextPath);
        }

        private static SpatialMigrationJournal CopyStage(SpatialMigrationJournal journal,
            SpatialMigrationJournalStage stage) =>
            new SpatialMigrationJournal(journal.JournalSchemaVersion, journal.Descriptor,
                journal.DescriptorFingerprintSha256, journal.TransactionIdentitySha256, journal.TransactionId,
                journal.RelativeJournalFilename, journal.RelativeOriginalBackupFilename,
                journal.RelativeCandidateStagingFilename,
                stage == SpatialMigrationJournalStage.DurableVerified || stage == SpatialMigrationJournalStage.Finalized
                    ? ReceiptName(journal.RelativeJournalFilename, journal.TransactionId) : null,
                journal.OriginalPayloadSha256, journal.OriginalPayloadSha256,
                journal.ExpectedCandidateSha256, stage);

        private static string ReceiptName(string journalName, string transactionId)
        {
            string suffix = "." + transactionId + ".journal.json";
            return journalName.Substring(0, journalName.Length - suffix.Length) + "." + transactionId + ".finalized";
        }

        private static bool HashIs(byte[] bytes, string expected) => bytes != null &&
            SpatialContractSha256.IsCanonical(expected) &&
            string.Equals(SpatialContractSha256.Compute(bytes), expected, StringComparison.Ordinal);

        private bool IsCanonicalSchemaSeven(byte[] bytes)
        {
            var completeLimits = new CanonicalSpatialSerializationLimits(limits,
                new CanonicalSpatialSaveWorkloadLimits(limits.MaximumCollectionRecords,
                    limits.MaximumCollectionRecords));
            return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, completeLimits).IsValid;
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
