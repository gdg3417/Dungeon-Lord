using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialTrustedPayload { None, Original, Backup, Candidate }

    public sealed class DetachedSpatialMigrationOutcome
    {
        internal DetachedSpatialMigrationOutcome(bool success, string reason,
            SpatialMigrationJournalStage? stage, SpatialTrustedPayload trusted,
            IEnumerable<string> diagnostics = null)
        { IsSuccess = success; Reason = reason; Stage = stage; TrustedPayload = trusted;
          Diagnostics = (diagnostics ?? Array.Empty<string>()).ToArray(); }
        public bool IsSuccess { get; }
        public string Reason { get; }
        public SpatialMigrationJournalStage? Stage { get; }
        public SpatialTrustedPayload TrustedPayload { get; }
        public string[] Diagnostics { get; }
    }

    public sealed class DetachedSpatialMigrationRecoveryContext
    {
        private readonly Dictionary<string, byte[]> validationInputs;
        private readonly byte[] legacyConfigurationBytes;
        private readonly RawSavePayloadClassificationLimits rawLimits;
        private readonly RawSaveEnvelopeVersionContract rawVersions;
        private readonly RawLegacyBlankFloorContract blankFloor;
        private readonly DetachedWholeSaveLimits wholeSaveLimits;

        public DetachedSpatialMigrationRecoveryContext(SpatialLayoutCompatibilitySnapshot compatibility,
            ProductionSpatialContentSnapshot productionContent,
            IReadOnlyDictionary<string, byte[]> validationInputs, byte[] legacyConfigurationBytes,
            CanonicalSpatialSerializationLimits limits,
            RawSavePayloadClassificationLimits rawLimits = default(RawSavePayloadClassificationLimits),
            RawSaveEnvelopeVersionContract rawVersions = default(RawSaveEnvelopeVersionContract),
            RawLegacyBlankFloorContract blankFloor = null,
            DetachedWholeSaveLimits wholeSaveLimits = default(DetachedWholeSaveLimits))
        {
            Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            ProductionContent = productionContent ?? throw new ArgumentNullException(nameof(productionContent));
            if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits));
            Limits = limits;
            this.validationInputs = validationInputs == null ? null : validationInputs.ToDictionary(
                pair => pair.Key, pair => pair.Value == null ? null : (byte[])pair.Value.Clone(),
                StringComparer.Ordinal);
            this.legacyConfigurationBytes = legacyConfigurationBytes == null ? null :
                (byte[])legacyConfigurationBytes.Clone();
            this.rawLimits = rawLimits; this.rawVersions = rawVersions; this.blankFloor = blankFloor;
            this.wholeSaveLimits = wholeSaveLimits;
        }

        public SpatialLayoutCompatibilitySnapshot Compatibility { get; }
        public ProductionSpatialContentSnapshot ProductionContent { get; }
        public CanonicalSpatialSerializationLimits Limits { get; }

        internal DetachedLegacyValidationResult ValidateLegacy(byte[] bytes)
        {
            if (bytes == null || !rawLimits.IsValid || !rawVersions.IsValid || blankFloor == null ||
                !wholeSaveLimits.IsValid) return new DetachedLegacyValidationResult(false,
                    "gd66.transaction.pinned_input_missing");
            RawSavePayloadClassification classification = RawSavePayloadClassifier.Classify(
                bytes, rawLimits, rawVersions, blankFloor);
            if (!classification.IsSuccess) return new DetachedLegacyValidationResult(false,
                classification.FailureReason);
            int schema = classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                ? 1 : classification.SchemaVersion.GetValueOrDefault();
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> contract =
                Compatibility.SelectContract(DetachedWholeSaveCandidateSerializer.TargetSchemaVersion);
            if (!contract.Success) return new DetachedLegacyValidationResult(false, contract.Code);
            CompatibilitySelectionResult<SpatialMigrationCompatibilityProfile> profile =
                Compatibility.SelectMigration(schema, DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                    contract.Value.CanonicalLayoutContractVersion);
            if (!profile.Success) return new DetachedLegacyValidationResult(false, profile.Code);
            try
            {
                var descriptor = new SpatialMigrationInputDescriptor(SpatialContractSha256.Compute(bytes), schema,
                    classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                        ? SpatialRawEnvelopeClassification.UnwrappedSaveData
                        : SpatialRawEnvelopeClassification.WrappedSaveRoot,
                    DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                    SpatialMigrationContractIdentity.AuthorityMarkerContractVersion,
                    SpatialMigrationContractIdentity.MigrationContractVersion, profile.Value.ProfileId,
                    profile.Value.ProfileVersion, profile.Value.CanonicalHash, profile.Value.GeometryId,
                    profile.Value.GeometryVersion, profile.Value.GeometryCanonicalHash,
                    SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                        ProductionContent.Manifest)),
                    SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                        ProductionContent.Catalog)), Array.Empty<SpatialValidationInputHash>(),
                    SpatialContractSha256.Compute(legacyConfigurationBytes),
                    SpatialMigrationContractIdentity.CanonicalSerializerId,
                    SpatialMigrationContractIdentity.CanonicalSerializerVersion);
                var inputs = new DetachedSpatialMigrationPreparationInputs(bytes, classification, descriptor,
                    Compatibility, ProductionContent, LegacyGameplayConfigurationContract.Parse(
                        legacyConfigurationBytes), validationInputs, Limits, wholeSaveLimits);
                DetachedSpatialMigrationPreparationResult prepared = DetachedSpatialMigrationPreparer.Prepare(inputs);
                return new DetachedLegacyValidationResult(prepared.IsSuccess, prepared.Reason);
            }
            catch (ArgumentException) { return new DetachedLegacyValidationResult(false,
                "gd66.transaction.pinned_input_hash_mismatch"); }
            catch (FormatException) { return new DetachedLegacyValidationResult(false,
                "gd66.transaction.pinned_input_hash_mismatch"); }
            catch (InvalidOperationException) { return new DetachedLegacyValidationResult(false,
                "gd66.profile.invalid"); }
        }

        internal string ValidatePins(SpatialMigrationInputDescriptor descriptor)
        {
            if (descriptor == null) return "gd66.transaction.pinned_input_missing";
            if (descriptor.CanonicalSerializerId != SpatialMigrationContractIdentity.CanonicalSerializerId ||
                descriptor.CanonicalSerializerVersion != SpatialMigrationContractIdentity.CanonicalSerializerVersion ||
                descriptor.AuthorityMarkerContractVersion != SpatialMigrationContractIdentity.AuthorityMarkerContractVersion ||
                descriptor.MigrationContractVersion != SpatialMigrationContractIdentity.MigrationContractVersion)
                return "gd66.transaction.pinned_input_hash_mismatch";
            SpatialLayoutCompatibilityProfilesData compatibilityData = Compatibility.Value;
            SpatialMigrationCompatibilityProfile[] profileIdentities =
                (compatibilityData.MigrationProfiles ?? Array.Empty<SpatialMigrationCompatibilityProfile>())
                .Where(value => value != null && value.ProfileId == descriptor.MigrationProfileId &&
                    value.ProfileVersion == descriptor.MigrationProfileVersion).ToArray();
            if (profileIdentities.Length == 0) return "gd66.transaction.pinned_profile_missing";
            CompatibilityLayoutGeometryRecord[] geometryIdentities =
                (compatibilityData.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>())
                .Where(value => value != null && value.GeometryId == descriptor.SharedGeometryId &&
                    value.GeometryVersion == descriptor.SharedGeometryVersion).ToArray();
            if (geometryIdentities.Length == 0) return "gd66.transaction.pinned_spatial_input_missing";
            if (!Compatibility.TryRecoverMigration(descriptor.MigrationProfileId,
                descriptor.MigrationProfileVersion, descriptor.MigrationProfileCanonicalHash,
                descriptor.SharedGeometryId, descriptor.SharedGeometryVersion,
                descriptor.SharedGeometryCanonicalHash, out SpatialMigrationCompatibilityProfile profile))
                return profileIdentities.All(value => value.CanonicalHash != descriptor.MigrationProfileCanonicalHash)
                    ? "gd66.transaction.pinned_profile_hash_mismatch" :
                    "gd66.transaction.pinned_spatial_input_hash_mismatch";
            if (profile.Lifecycle != CompatibilityProfileLifecycle.Active &&
                profile.Lifecycle != CompatibilityProfileLifecycle.Retired)
                return "gd66.profile.invalid";
            byte[] manifest = ProductionSpatialGeneratedSetParser.SerializeCanonical(ProductionContent.Manifest);
            byte[] catalog = ProductionSpatialGeneratedSetParser.SerializeCanonical(ProductionContent.Catalog);
            if (!HashEquals(manifest, descriptor.ProductionManifestSha256) ||
                !HashEquals(catalog, descriptor.ProductionCatalogSha256))
                return "gd66.transaction.pinned_spatial_input_hash_mismatch";
            if (!HashEquals(legacyConfigurationBytes, descriptor.LegacyGameplayConfigurationSha256))
                return legacyConfigurationBytes == null ? "gd66.transaction.pinned_input_missing" :
                    "gd66.transaction.pinned_input_hash_mismatch";
            SpatialValidationInputHash[] pins = descriptor.ValidationInputHashes;
            string registryReason = DetachedRequiredValidationInputSpecification.Current.Validate(
                validationInputs, pins);
            if (registryReason != null) return registryReason;
            if (validationInputs == null && pins.Length != 0) return "gd66.transaction.pinned_input_missing";
            if (validationInputs != null && validationInputs.Count != pins.Length)
                return "gd66.transaction.pinned_input_hash_mismatch";
            foreach (SpatialValidationInputHash pin in pins)
            {
                if (!validationInputs.TryGetValue(pin.InputId, out byte[] bytes) || bytes == null)
                    return "gd66.transaction.pinned_input_missing";
                if (!HashEquals(bytes, pin.Sha256)) return "gd66.transaction.pinned_input_hash_mismatch";
            }
            return null;
        }

        internal bool TryCreateUnfinishedValidationContext(SpatialMigrationInputDescriptor descriptor,
            string transactionId, string fingerprint, string expectedCandidateSha256,
            out DetachedUnfinishedAttemptValidationContext context)
        {
            context = null;
            if (descriptor == null || !SpatialContractSha256.IsCanonical(expectedCandidateSha256) ||
                ValidatePins(descriptor) != null || !Compatibility.TryRecoverMigration(
                descriptor.MigrationProfileId, descriptor.MigrationProfileVersion,
                descriptor.MigrationProfileCanonicalHash, descriptor.SharedGeometryId,
                descriptor.SharedGeometryVersion, descriptor.SharedGeometryCanonicalHash,
                out SpatialMigrationCompatibilityProfile profile)) return false;
            CompatibilityLayoutGeometryRecord geometry = (Compatibility.Value.GeometryRecords ??
                Array.Empty<CompatibilityLayoutGeometryRecord>()).FirstOrDefault(value => value != null &&
                value.GeometryId == descriptor.SharedGeometryId &&
                value.GeometryVersion == descriptor.SharedGeometryVersion &&
                value.CanonicalHash == descriptor.SharedGeometryCanonicalHash);
            if (geometry == null) return false;
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> selectedContract =
                Compatibility.SelectContract(descriptor.SelectedTargetSchemaVersion);
            if (!selectedContract.Success) return false;
            context = new DetachedUnfinishedAttemptValidationContext(descriptor, transactionId, fingerprint,
                expectedCandidateSha256, selectedContract.Value, profile, geometry, ProductionContent, legacyConfigurationBytes,
                validationInputs, Limits);
            return true;
        }

        internal bool IsJournalBoundCandidateValid(byte[] bytes, SpatialMigrationJournal journal)
        {
            try
            {
                if (bytes == null || journal == null || journal.Descriptor == null ||
                    !HashEquals(bytes, journal.ExpectedCandidateSha256)) return false;
                string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                    journal.Descriptor, Limits.Serialized);
                if (fingerprint != journal.DescriptorFingerprintSha256) return false;
                string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                    journal.Descriptor.OriginalPayloadSha256, fingerprint);
                if (identity != journal.TransactionIdentitySha256 ||
                    SpatialMigrationTransactionIdentity.CreateTransactionId(identity) != journal.TransactionId)
                    return false;
                return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, Limits, null,
                    journal.TransactionId, journal.DescriptorFingerprintSha256).IsValid;
            }
            catch (ArgumentException) { return false; }
            catch (FormatException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        internal byte[] LegacyConfigurationBytes => legacyConfigurationBytes == null ? null :
            (byte[])legacyConfigurationBytes.Clone();

        private static bool HashEquals(byte[] bytes, string expected) => bytes != null &&
            string.Equals(SpatialContractSha256.Compute(bytes), expected, StringComparison.Ordinal);
    }

    internal sealed class DetachedLegacyValidationResult
    {
        internal DetachedLegacyValidationResult(bool valid, string reason)
        { IsValid = valid; Reason = reason; }
        internal bool IsValid { get; }
        internal string Reason { get; }
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
        void MoveSameDirectoryAtomic(string sourcePath, string destinationPath);
        void DeleteFile(string path);
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
            if (!string.Equals(Path.GetFullPath(Path.GetDirectoryName(stagingPath)),
                Path.GetFullPath(Path.GetDirectoryName(activePath)), PlatformPathComparison)) throw new IOException();
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
            if (maximumResults < 0) throw new ArgumentOutOfRangeException(nameof(maximumResults));
            var paths = new List<string>(Math.Min(maximumResults, 256));
            foreach (string path in Directory.EnumerateFiles(directoryPath, searchPattern,
                SearchOption.TopDirectoryOnly))
            {
                if (paths.Count == maximumResults) throw new IOException();
                paths.Add(path);
            }
            paths.Sort(StringComparer.Ordinal);
            return paths;
        }
        public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
        {
            string directory = Path.GetFullPath(directoryPath);
            string candidate = Path.GetFullPath(path);
            string prefix = directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? directory : directory + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, PlatformPathComparison)) return false;
            for (string current = candidate; !string.Equals(current, directory, PlatformPathComparison);
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
        public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath)
        {
            if (!string.Equals(Path.GetFullPath(Path.GetDirectoryName(sourcePath)),
                Path.GetFullPath(Path.GetDirectoryName(destinationPath)), PlatformPathComparison))
                throw new IOException();
            File.Move(sourcePath, destinationPath);
        }
        public void DeleteFile(string path) => File.Delete(path);
        private static StringComparison PlatformPathComparison => Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
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
        public const string OriginalRestoredStageWriteFailedReason =
            "gd66.transaction.original_restored_stage_write_failed";
        public const string PathInvalidReason = "gd66.transaction.path_invalid";
        public const string RollbackSourceMissingReason = "gd66.transaction.rollback_source_missing";
        public const string ContradictoryAuthorityReason = "gd66.authority.contradictory_state";
        public const string DependencyChangedReason = "gd66.transaction.dependency_changed";
        public const string ReceiptInvalidReason = "gd66.transaction.finalization_receipt_invalid";
        public const string ReceiptWriteDiagnostic = "gd66.diagnostic.finalization_receipt_write_failed";
        public const string NoJournalLegacyDiagnostic = "gd66.diagnostic.no_journal_legacy_valid";
        public const string ReplacedPendingDurabilityDiagnostic =
            "gd66.diagnostic.replaced_candidate_pending_durability";
        public const string FinalizedBackupQuarantinedDiagnostic =
            "gd66.diagnostic.finalized_backup_quarantined";
        public const string OrphanStagingQuarantinedDiagnostic =
            "gd66.diagnostic.orphan_staging_quarantined";
        public const string OrphanReceiptQuarantinedDiagnostic =
            "gd66.diagnostic.orphan_receipt_quarantined";
        public const string JournalMalformedWithVerifiedOriginalReason =
            "gd66.transaction.journal_malformed_with_verified_original";
        public const string StaleJournalOriginalMismatchReason =
            "gd66.transaction.stale_journal_original_mismatch";
        public const string StagedCandidateVerifiedDiagnostic =
            "gd66.diagnostic.staged_candidate_verified";
        public const string DurableCandidatePendingFinalizationDiagnostic =
            "gd66.diagnostic.durable_candidate_pending_finalization";

        private readonly ISpatialMigrationFileSystem fileSystem;
        private readonly SpatialSerializedInputLimits limits;
        private readonly ProductionSpatialContentSnapshot productionContent;
        private readonly DetachedSpatialMigrationRecoveryContext recoveryContext;

        public DetachedSpatialMigrationTransaction(ISpatialMigrationFileSystem fileSystem,
            DetachedSpatialMigrationRecoveryContext recoveryContext)
        { this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
          this.recoveryContext = recoveryContext ?? throw new ArgumentNullException(nameof(recoveryContext));
          limits = recoveryContext.Limits.Serialized; productionContent = recoveryContext.ProductionContent; }

        public DetachedSpatialMigrationOutcome Execute(string activePath,
            DetachedPreparedSpatialMigrationAttempt attempt)
        {
            if (attempt == null) return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
            DetachedSpatialMigrationOutcome outcome = ExecutePrepared(activePath, attempt.GetOriginalBytes(),
                attempt.Candidate, attempt.Descriptor);
            return outcome.IsSuccess && attempt.IsEmptyMigration && outcome.Reason == SuccessReason
                ? new DetachedSpatialMigrationOutcome(true, EmptySuccessReason, outcome.Stage,
                    outcome.TrustedPayload, outcome.Diagnostics) : outcome;
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
                if (!names.IsValid) return Failure(PathInvalidReason, null,
                    TrustedActive(activePath, exactOriginalBytes, candidate.GetBytes()));
                if (!Resolve(directory, names.Value.Journal, out string journalPath) ||
                    !Resolve(directory, names.Value.OriginalBackup, out string backupPath) ||
                    !Resolve(directory, names.Value.CandidateStaging, out string stagingPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, journalPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, backupPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, stagingPath))
                    return Failure(PathInvalidReason, null,
                        TrustedActive(activePath, exactOriginalBytes, candidate.GetBytes()));

                bool dependencyChanged = false;
            DiscoverAttempt:
                EvidenceSnapshot executeEvidence = DiscoverEvidence(directory,
                    Path.GetFileNameWithoutExtension(activePath));
                SpatialContractResult<SpatialMigrationJournal> existing = executeEvidence.SingleLive;
                int liveCount = executeEvidence.LiveCount;
                if (liveCount > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                SpatialMigrationJournal journal;
                if (liveCount == 1)
                {
                    if (!existing.IsValid || !string.Equals(existing.Value.TransactionId, transactionId, StringComparison.Ordinal))
                    {
                        if (existing.IsValid && !string.Equals(existing.Value.OriginalPayloadSha256,
                            descriptor.OriginalPayloadSha256, StringComparison.Ordinal))
                        {
                            byte[] activeBytes = fileSystem.ReadAllBytes(activePath);
                            DetachedLegacyValidationResult repaired = Same(activeBytes, exactOriginalBytes)
                                ? recoveryContext.ValidateLegacy(activeBytes) : null;
                            if (repaired != null && repaired.IsValid)
                            {
                                QuarantineBoundNonterminalEvidence(directory, existing.Value);
                                return Failure(StaleJournalOriginalMismatchReason, existing.Value.Stage,
                                    SpatialTrustedPayload.Original);
                            }
                            return Failure(ActivePayloadUnknownReason, existing.Value.Stage, SpatialTrustedPayload.None);
                        }
                        if (existing.IsValid && existing.Value.OriginalPayloadSha256 ==
                            descriptor.OriginalPayloadSha256)
                        {
                            if (existing.Value.Stage == SpatialMigrationJournalStage.DescriptorPinned &&
                                Same(fileSystem.ReadAllBytes(activePath), exactOriginalBytes) &&
                                QuarantineLiveAttemptJournal(directory, existing.Value))
                            { dependencyChanged = true; goto DiscoverAttempt; }
                            DetachedSpatialMigrationOutcome oldOutcome;
                            if ((int)existing.Value.Stage >= (int)SpatialMigrationJournalStage.CandidateVerified &&
                                (int)existing.Value.Stage <= (int)SpatialMigrationJournalStage.DurableVerified &&
                                ResolveEvidencePaths(directory, existing.Value, out string oldJournalPath,
                                    out string oldBackupPath, out string ignoredStaging) &&
                                fileSystem.Exists(oldBackupPath))
                            {
                                byte[] oldBackup = fileSystem.ReadAllBytes(oldBackupPath);
                                oldOutcome = HashIs(oldBackup, existing.Value.OriginalPayloadSha256)
                                    ? Restore(oldJournalPath, oldBackupPath, activePath, directory,
                                        existing.Value, oldBackup)
                                    : RecoverLive(activePath, directory, existing.Value);
                            }
                            else oldOutcome = RecoverLive(activePath, directory, existing.Value);
                            if (oldOutcome.TrustedPayload == SpatialTrustedPayload.Original &&
                                Same(fileSystem.ReadAllBytes(activePath), exactOriginalBytes))
                            {
                                if (oldOutcome.Stage != SpatialMigrationJournalStage.OriginalRestored &&
                                    !QuarantineLiveAttemptJournal(directory, existing.Value))
                                    return WithDiagnostic(oldOutcome, DependencyChangedReason);
                                dependencyChanged = true; goto DiscoverAttempt;
                            }
                            return WithDiagnostic(oldOutcome, DependencyChangedReason);
                        }
                        return Failure(FingerprintMismatchReason, existing.IsValid ? existing.Value.Stage : (SpatialMigrationJournalStage?)null,
                            TrustedActive(activePath, exactOriginalBytes, candidate.GetBytes()));
                    }
                    journal = existing.Value;
                }
                else
                {
                    if (Same(fileSystem.ReadAllBytes(activePath), candidate.GetBytes()) &&
                        IsCanonicalSchemaSeven(candidate.GetBytes(), descriptor, transactionId, fingerprint, candidate.Sha256))
                    {
                        var committed = new DetachedSpatialMigrationOutcome(true, AlreadyCommittedReason,
                            null, SpatialTrustedPayload.Candidate);
                        return dependencyChanged ? WithDiagnostic(committed, DependencyChangedReason) : committed;
                    }
                    if (!Same(fileSystem.ReadAllBytes(activePath), exactOriginalBytes))
                        return Failure(ActivePayloadUnknownReason, null, SpatialTrustedPayload.None);
                    journal = CreateJournal(descriptor, fingerprint, identity, transactionId, names.Value,
                        null, null, SpatialMigrationJournalStage.DescriptorPinned);
                    if (!WriteJournal(journalPath, journal))
                        return Failure(BackupFailedReason, null, SpatialTrustedPayload.Original);
                }
                DetachedSpatialMigrationOutcome resumed = Resume(activePath, directory, journalPath, backupPath, stagingPath, exactOriginalBytes,
                    candidate, descriptor, fingerprint, identity, transactionId, names.Value, journal);
                return dependencyChanged ? WithDiagnostic(resumed, DependencyChangedReason) : resumed;
            }
            catch (IOException) { return Failure(RecoveryFailedReason, null,
                TrustedActiveSafe(activePath, exactOriginalBytes, candidate.GetBytes())); }
            catch (UnauthorizedAccessException) { return Failure(PathInvalidReason, null,
                TrustedActiveSafe(activePath, exactOriginalBytes, candidate.GetBytes())); }
            catch (ArgumentException) { return Failure(PathInvalidReason, null,
                TrustedActiveSafe(activePath, exactOriginalBytes, candidate.GetBytes())); }
            catch (NotSupportedException) { return Failure(PathInvalidReason, null,
                TrustedActiveSafe(activePath, exactOriginalBytes, candidate.GetBytes())); }
        }

        private bool QuarantineLiveAttemptJournal(string directory, SpatialMigrationJournal journal)
        {
            if (!Resolve(directory, journal.RelativeJournalFilename, out string journalPath) ||
                !fileSystem.Exists(journalPath)) return false;
            byte[] bytes = fileSystem.ReadAllBytes(journalPath);
            try { return QuarantineEvidence(directory, journalPath, bytes); }
            catch { return false; }
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
                EvidenceSnapshot evidence = DiscoverEvidence(directory,
                    Path.GetFileNameWithoutExtension(activePath));
                SpatialContractResult<SpatialMigrationJournal> found = evidence.SingleLive;
                int count = evidence.LiveCount;
                if (count > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                if (count == 0)
                {
                    byte[] active = fileSystem.ReadAllBytes(activePath);
                    // Terminal journals and receipts are audit evidence only.  Current-target C is
                    // self-authoritative after complete validation and may have changed normally.
                    bool activeCandidate = IsCanonicalSchemaSeven(active);
                    bool canonicalLooking = activeCandidate || HasStructuralCanonicalTargetEvidence(active);
                    DetachedLegacyValidationResult legacy = canonicalLooking ? null :
                        recoveryContext.ValidateLegacy(active);
                    bool activeLegacy = legacy != null && legacy.IsValid;
                    if (activeCandidate)
                    {
                        IReadOnlyList<string> orphanDiagnostics = CleanupOrphanEvidence(directory, evidence);
                        return new DetachedSpatialMigrationOutcome(true, AlreadyCommittedReason, null,
                            SpatialTrustedPayload.Candidate, orphanDiagnostics.Count == 0 ? null : orphanDiagnostics);
                    }
                    bool malformed = evidence.Malformed.Count != 0;
                    if (malformed && QuarantineMalformedEvidence(directory, evidence.Malformed))
                    {
                        if (activeLegacy) return Failure(JournalMalformedWithVerifiedOriginalReason, null,
                            SpatialTrustedPayload.Original);
                    }
                    else if (malformed) return Failure(PathInvalidReason, null, activeLegacy ?
                        SpatialTrustedPayload.Original : SpatialTrustedPayload.None);
                    if (canonicalLooking) return Failure(ContradictoryAuthorityReason, null, SpatialTrustedPayload.None);
                    if (activeLegacy)
                        return new DetachedSpatialMigrationOutcome(true, NoJournalLegacyDiagnostic, null,
                            SpatialTrustedPayload.Original, new[] { NoJournalLegacyDiagnostic });
                    if (legacy != null && !string.IsNullOrEmpty(legacy.Reason))
                        return Failure(legacy.Reason, null, SpatialTrustedPayload.None);
                    SpatialMigrationJournal terminal = evidence.Terminal.Count == 1
                        ? evidence.Terminal[0].Journal : null;
                    int terminalCount = evidence.Terminal.Count;
                    if (terminal != null)
                    {
                        if (terminal.Stage == SpatialMigrationJournalStage.OriginalRestored &&
                            HashIs(active, terminal.OriginalPayloadSha256))
                            return new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                                terminal.Stage, SpatialTrustedPayload.Original);
                    }
                    return Failure(NoTrustedPayloadReason, terminalCount == 1 ? terminal?.Stage : null,
                        SpatialTrustedPayload.None);
                }
                string pinFailure = recoveryContext.ValidatePins(found.Value.Descriptor);
                if (pinFailure != null)
                    return ResolvePinFailure(activePath, directory, found.Value, pinFailure);
                return RecoverLive(activePath, directory, found.Value);
            }
            catch (InvalidOperationException) { return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None); }
            catch (IOException) { return RecoverExceptionOutcome(activePath, PathInvalidReason); }
            catch (UnauthorizedAccessException) { return RecoverExceptionOutcome(activePath, PathInvalidReason); }
        }

        private DetachedSpatialMigrationOutcome RecoverExceptionOutcome(string activePath, string reason)
        {
            try
            {
                byte[] active = fileSystem.ReadAllBytes(activePath);
                if (IsCanonicalSchemaSeven(active))
                    // Failure to discover evidence cannot establish the no-live-journal precondition
                    // for AlreadyCommitted.  C remains independently trusted, but gameplay stays
                    // blocked until discovery succeeds and any live attempt can be ruled out.
                    return Failure(reason, null, SpatialTrustedPayload.Candidate);
                DetachedLegacyValidationResult legacy = recoveryContext.ValidateLegacy(active);
                if (legacy.IsValid) return Failure(reason, null, SpatialTrustedPayload.Original);
                return Failure(string.IsNullOrEmpty(legacy.Reason) ? reason : legacy.Reason,
                    null, SpatialTrustedPayload.None);
            }
            catch { return Failure(reason, null, SpatialTrustedPayload.None); }
        }

        private DetachedSpatialMigrationOutcome ResolvePinFailure(string activePath, string directory,
            SpatialMigrationJournal journal, string pinFailure)
        {
            if (!ResolveEvidencePaths(directory, journal, out string journalPath, out string backupPath,
                out string ignoredStaging)) return Failure(PathInvalidReason, journal.Stage, SpatialTrustedPayload.None);
            byte[] active = fileSystem.ReadAllBytes(activePath);
            if (HashIs(active, journal.OriginalPayloadSha256))
                return Failure(pinFailure, journal.Stage, SpatialTrustedPayload.Original);
            byte[] backup = fileSystem.Exists(backupPath) ? fileSystem.ReadAllBytes(backupPath) : null;
            if (HashIs(backup, journal.OriginalPayloadSha256))
            {
                var diagnostics = new List<string>();
                if (journal.Stage == SpatialMigrationJournalStage.CandidateVerified)
                    AddDiagnostic(diagnostics, StagedCandidateVerifiedDiagnostic);
                if (journal.Stage == SpatialMigrationJournalStage.Replaced ||
                    journal.Stage == SpatialMigrationJournalStage.DurableVerified)
                    AddDiagnostic(diagnostics, ReplacedPendingDurabilityDiagnostic);
                if (journal.Stage == SpatialMigrationJournalStage.DurableVerified)
                    AddDiagnostic(diagnostics, DurableCandidatePendingFinalizationDiagnostic);
                return Restore(journalPath, backupPath, activePath, directory, journal, backup,
                    pinFailure, diagnostics);
            }
            if (recoveryContext.IsJournalBoundCandidateValid(active, journal))
                return Failure(RollbackSourceMissingReason, journal.Stage, SpatialTrustedPayload.Candidate);
            return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
        }

        private DetachedSpatialMigrationOutcome RecoverLive(string activePath, string directory,
            SpatialMigrationJournal journal)
        {
            var diagnostics = new List<string>();
            if (!ResolveEvidencePaths(directory, journal, out string journalPath, out string backupPath,
                out string stagingPath)) return Failure(PathInvalidReason, journal.Stage, SpatialTrustedPayload.None);
            byte[] active = fileSystem.ReadAllBytes(activePath);
            bool activeOriginal = HashIs(active, journal.OriginalPayloadSha256);
            byte[] backup = fileSystem.Exists(backupPath) ? fileSystem.ReadAllBytes(backupPath) : null;
            bool backupValid = HashIs(backup, journal.OriginalPayloadSha256);
            if (activeOriginal && journal.Stage != SpatialMigrationJournalStage.DescriptorPinned &&
                HasRestorationIntent(backupPath, journal))
            {
                SpatialMigrationJournal restored = CopyStage(journal,
                    SpatialMigrationJournalStage.OriginalRestored);
                if (!RewriteJournal(journalPath, restored))
                    return FailureWithDiagnostics(OriginalRestoredStageWriteFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original, diagnostics);
                return new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                    restored.Stage, SpatialTrustedPayload.Original);
            }
            if (journal.Stage == SpatialMigrationJournalStage.DescriptorPinned)
                return activeOriginal
                    ? Failure(BackupIncompleteReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (!backupValid && !activeOriginal && journal.Stage != SpatialMigrationJournalStage.Replaced &&
                journal.Stage != SpatialMigrationJournalStage.DurableVerified)
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (journal.Stage == SpatialMigrationJournalStage.BackupVerified)
                return activeOriginal ? Failure(CandidateAbsentReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Restore(journalPath, backupPath, activePath, directory, journal, backup);

            bool activeCandidate = HashIs(active, journal.ExpectedCandidateSha256) && IsCanonicalSchemaSeven(active,
                journal.Descriptor, journal.TransactionId, journal.DescriptorFingerprintSha256,
                journal.ExpectedCandidateSha256);
            byte[] staged = fileSystem.Exists(stagingPath) ? fileSystem.ReadAllBytes(stagingPath) : null;
            bool stagedCandidate = HashIs(staged, journal.ExpectedCandidateSha256) &&
                IsCanonicalSchemaSeven(staged, journal.Descriptor, journal.TransactionId,
                    journal.DescriptorFingerprintSha256, journal.ExpectedCandidateSha256);
            if (journal.Stage == SpatialMigrationJournalStage.CandidateVerified)
            {
                if (stagedCandidate) AddDiagnostic(diagnostics, StagedCandidateVerifiedDiagnostic);
                if (!backupValid)
                    return activeOriginal ? FailureWithDiagnostics(BackupFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original, diagnostics)
                        : FailureWithDiagnostics(NoTrustedPayloadReason, journal.Stage,
                            SpatialTrustedPayload.None, diagnostics);
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
                            return FailureWithDiagnostics(ReplacementFailedReason, journal.Stage,
                                SpatialTrustedPayload.Original, diagnostics);
                        return WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup),
                            diagnostics);
                    }
                }
                SpatialMigrationJournal replaced = CopyStage(journal, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, replaced))
                    return WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup,
                        ReplacementFailedReason), diagnostics);
                journal = replaced; activeCandidate = true;
            }
            if (journal.Stage == SpatialMigrationJournalStage.Replaced)
            {
                if (!activeCandidate) return backupValid
                    ? Restore(journalPath, backupPath, activePath, directory, journal, backup)
                    : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
                AddDiagnostic(diagnostics, ReplacedPendingDurabilityDiagnostic);
                try { fileSystem.FlushDirectory(directory); }
                catch
                {
                    if (backupValid) return WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup,
                        DurabilityFailedReason), diagnostics);
                    return FailureWithDiagnostics(ReplacedPendingDurabilityDiagnostic, journal.Stage,
                        SpatialTrustedPayload.Candidate, diagnostics);
                }
                active = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(active, journal.ExpectedCandidateSha256) || !IsCanonicalSchemaSeven(active,
                    journal.Descriptor, journal.TransactionId, journal.DescriptorFingerprintSha256,
                    journal.ExpectedCandidateSha256))
                    return backupValid ? WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup),
                        diagnostics) : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
                if (backupValid) CleanupInterruptedRestoration(directory, backupPath);
                SpatialMigrationJournal durable = CopyStage(journal, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, durable))
                {
                    if (backupValid) return WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup,
                        DurabilityFailedReason), diagnostics);
                    return FailureWithDiagnostics(ReplacedPendingDurabilityDiagnostic, journal.Stage,
                        SpatialTrustedPayload.Candidate, diagnostics);
                }
                journal = durable;
            }
            if (journal.Stage == SpatialMigrationJournalStage.DurableVerified)
            {
                AddDiagnostic(diagnostics, DurableCandidatePendingFinalizationDiagnostic);
                byte[] durableActive = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(durableActive, journal.ExpectedCandidateSha256) ||
                    !IsCanonicalSchemaSeven(durableActive, journal.Descriptor, journal.TransactionId,
                        journal.DescriptorFingerprintSha256, journal.ExpectedCandidateSha256))
                    return backupValid ? WithDiagnostics(Restore(journalPath, backupPath, activePath, directory, journal, backup),
                        diagnostics) : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
                string receiptDiagnostic = TryWriteReceipt(directory, journal);
                if (receiptDiagnostic != null) AddDiagnostic(diagnostics, receiptDiagnostic);
                SpatialMigrationJournal finalized = CopyStage(journal, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, finalized))
                    return FailureWithDiagnostics(FinalizationFailedReason, journal.Stage,
                        SpatialTrustedPayload.Candidate, diagnostics);
                journal = finalized;
                return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                    SpatialTrustedPayload.Candidate, diagnostics);
            }
            return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                SpatialTrustedPayload.Candidate, diagnostics);
        }

        private string TryWriteReceipt(string directory, SpatialMigrationJournal journal)
        {
            string relative = ReceiptName(journal.RelativeJournalFilename, journal.TransactionId);
            if (!Resolve(directory, relative, out string path) ||
                !fileSystem.IsPathContainedWithoutRedirection(directory, path)) return ReceiptWriteDiagnostic;
            var receipt = new DetachedFinalizationReceipt(journal.TransactionId,
                journal.DescriptorFingerprintSha256, journal.ExpectedCandidateSha256);
            byte[] expected = DetachedFinalizationReceiptContract.Serialize(receipt, limits);
            if (expected == null) return ReceiptWriteDiagnostic;
            try
            {
                if (fileSystem.Exists(path))
                {
                    byte[] existing = fileSystem.ReadAllBytes(path);
                    DetachedFinalizationReceipt parsed = DetachedFinalizationReceiptContract.Parse(existing, limits);
                    if (parsed != null && Same(existing, expected)) return null;
                    if (!QuarantineEvidence(directory, path, existing)) return ReceiptInvalidReason;
                    fileSystem.WriteAllBytesDurable(path, expected);
                    fileSystem.FlushDirectory(directory);
                    byte[] replaced = fileSystem.ReadAllBytes(path);
                    return DetachedFinalizationReceiptContract.Parse(replaced, limits) != null &&
                        Same(replaced, expected) ? ReceiptInvalidReason : ReceiptWriteDiagnostic;
                }
                fileSystem.WriteAllBytesDurable(path, expected);
                fileSystem.FlushDirectory(directory);
                byte[] actual = fileSystem.ReadAllBytes(path);
                return DetachedFinalizationReceiptContract.Parse(actual, limits) != null &&
                    Same(actual, expected) ? null : ReceiptWriteDiagnostic;
            }
            catch { return ReceiptWriteDiagnostic; }
        }

        private DetachedSpatialMigrationOutcome Restore(string journalPath, string backupPath, string activePath,
            string directory, SpatialMigrationJournal journal, byte[] verifiedBackup,
            string restoredFailureReason = null, IEnumerable<string> diagnostics = null)
        {
            if (!HashIs(verifiedBackup, journal.OriginalPayloadSha256))
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            bool restoredActive = false;
            try
            {
                if (!Same(fileSystem.ReadAllBytes(backupPath), verifiedBackup))
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                string restoreRelative = Path.GetFileName(backupPath) + ".restore";
                if (!Resolve(directory, restoreRelative, out string restoreStaging) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, restoreStaging))
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? PathInvalidReason, diagnostics);
                if (!fileSystem.Exists(restoreStaging))
                {
                    fileSystem.WriteAllBytesDurable(restoreStaging, verifiedBackup);
                    fileSystem.FlushDirectory(directory);
                }
                if (!Same(fileSystem.ReadAllBytes(restoreStaging), verifiedBackup))
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                string intentRelative = Path.GetFileName(backupPath) + ".restore.intent";
                if (!Resolve(directory, intentRelative, out string intentPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, intentPath))
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? PathInvalidReason, diagnostics);
                var intent = new DetachedRestorationIntent(journal.TransactionId,
                    journal.DescriptorFingerprintSha256, journal.OriginalPayloadSha256,
                    SpatialContractSha256.Compute(verifiedBackup), journal.RelativeJournalFilename,
                    (int)journal.Stage);
                byte[] intentBytes = DetachedRestorationIntentContract.Serialize(intent, limits);
                if (intentBytes == null) return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                if (fileSystem.Exists(intentPath))
                {
                    byte[] existingIntent = fileSystem.ReadAllBytes(intentPath);
                    if (!Same(existingIntent, intentBytes) ||
                        DetachedRestorationIntentContract.Parse(existingIntent, limits) == null)
                    {
                        if (!QuarantineEvidence(directory, intentPath, existingIntent))
                            return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                    }
                }
                if (!fileSystem.Exists(intentPath)) fileSystem.WriteAllBytesDurable(intentPath, intentBytes);
                fileSystem.FlushDirectory(directory);
                byte[] persistedIntent = fileSystem.ReadAllBytes(intentPath);
                if (!Same(persistedIntent, intentBytes) ||
                    DetachedRestorationIntentContract.Parse(persistedIntent, limits) == null)
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                fileSystem.ReplaceSameDirectoryAtomic(restoreStaging, activePath);
                fileSystem.FlushDirectory(directory);
                if (!HashIs(fileSystem.ReadAllBytes(activePath), journal.OriginalPayloadSha256))
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                restoredActive = true;
                SpatialMigrationJournal restored = CopyStage(journal, SpatialMigrationJournalStage.OriginalRestored);
                if (!RewriteJournal(journalPath, restored))
                    return FailureWithDiagnostics(OriginalRestoredStageWriteFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original, diagnostics);
                try { QuarantineEvidence(directory, intentPath, intentBytes); }
                catch { }
                return restoredFailureReason == null
                    ? new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                        restored.Stage, SpatialTrustedPayload.Original)
                    : FailureWithDiagnostics(restoredFailureReason, restored.Stage,
                        SpatialTrustedPayload.Original, diagnostics);
            }
            catch
            {
                if (!restoredActive)
                    return ClassifyRestoreFailure(activePath, journal, restoredFailureReason ?? RecoveryFailedReason, diagnostics);
                return FailureWithDiagnostics(OriginalRestoredStageWriteFailedReason, journal.Stage,
                    SpatialTrustedPayload.Original, diagnostics);
            }
        }


        private DetachedSpatialMigrationOutcome ClassifyRestoreFailure(string activePath,
            SpatialMigrationJournal journal, string reason, IEnumerable<string> diagnostics)
        {
            string failure = string.IsNullOrEmpty(reason) ? RecoveryFailedReason : reason;
            try
            {
                byte[] active = fileSystem.ReadAllBytes(activePath);
                if (HashIs(active, journal.OriginalPayloadSha256))
                    return FailureWithDiagnostics(failure, journal.Stage, SpatialTrustedPayload.Original, diagnostics);
                if ((journal.Stage == SpatialMigrationJournalStage.CandidateVerified ||
                    journal.Stage == SpatialMigrationJournalStage.Replaced) &&
                    IsTrustedJournalBoundCandidateForRecovery(active, journal))
                    return FailureWithDiagnostics(failure, journal.Stage, SpatialTrustedPayload.Candidate, diagnostics);
            }
            catch { }
            return FailureWithDiagnostics(failure, journal.Stage, SpatialTrustedPayload.None, diagnostics);
        }


        private bool IsTrustedJournalBoundCandidateForRecovery(byte[] active, SpatialMigrationJournal journal)
        {
            if (active == null || journal == null || journal.Descriptor == null ||
                !HashIs(active, journal.ExpectedCandidateSha256)) return false;
            try
            {
                string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                    journal.Descriptor, limits);
                if (fingerprint != journal.DescriptorFingerprintSha256) return false;
                string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                    journal.Descriptor.OriginalPayloadSha256, fingerprint);
                if (identity != journal.TransactionIdentitySha256 ||
                    SpatialMigrationTransactionIdentity.CreateTransactionId(identity) != journal.TransactionId)
                    return false;
                return recoveryContext.ValidatePins(journal.Descriptor) == null
                    ? IsCanonicalSchemaSeven(active, journal.Descriptor, journal.TransactionId,
                        journal.DescriptorFingerprintSha256, journal.ExpectedCandidateSha256)
                    : recoveryContext.IsJournalBoundCandidateValid(active, journal);
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        private bool HasRestorationIntent(string backupPath, SpatialMigrationJournal journal)
        {
            string intentPath = backupPath + ".restore.intent";
            if (!fileSystem.Exists(intentPath)) return false;
            DetachedRestorationIntent intent = DetachedRestorationIntentContract.Parse(
                fileSystem.ReadAllBytes(intentPath), limits);
            return intent != null && intent.TransactionId == journal.TransactionId &&
                intent.DescriptorFingerprint == journal.DescriptorFingerprintSha256 &&
                intent.OriginalSha256 == journal.OriginalPayloadSha256 &&
                intent.BackupSha256 == journal.OriginalPayloadSha256 &&
                intent.JournalFilename == journal.RelativeJournalFilename &&
                intent.JournalStage == (int)journal.Stage;
        }

        private void CleanupInterruptedRestoration(string directory, string backupPath)
        {
            string[] paths = { backupPath + ".restore", backupPath + ".restore.intent" };
            for (int index = 0; index < paths.Length; index++)
            {
                try
                {
                    if (!fileSystem.Exists(paths[index])) continue;
                    byte[] bytes = fileSystem.ReadAllBytes(paths[index]);
                    QuarantineEvidence(directory, paths[index], bytes);
                }
                catch { /* Audit cleanup never revokes already verified C. */ }
            }
        }

        private DetachedSpatialMigrationOutcome Resume(string activePath, string directory, string journalPath,
            string backupPath, string stagingPath, byte[] original, DetachedWholeSaveCandidate candidate,
            SpatialMigrationInputDescriptor descriptor, string fingerprint, string identity, string transactionId,
            SpatialMigrationSidecarNames names, SpatialMigrationJournal journal)
        {
            byte[] candidateBytes = candidate.GetBytes();
            var diagnostics = new List<string>();
            SpatialMigrationJournalStage persisted = journal.Stage;
            if (persisted == SpatialMigrationJournalStage.Finalized)
                return Same(fileSystem.ReadAllBytes(activePath), candidateBytes)
                    ? new DetachedSpatialMigrationOutcome(true, SuccessReason, persisted, SpatialTrustedPayload.Candidate)
                    : Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
            if ((int)persisted >= (int)SpatialMigrationJournalStage.CandidateVerified &&
                (int)persisted <= (int)SpatialMigrationJournalStage.DurableVerified)
            {
                return string.Equals(journal.ExpectedCandidateSha256, candidate.Sha256, StringComparison.Ordinal)
                    ? RecoverLive(activePath, directory, journal)
                    : Failure(FingerprintMismatchReason, persisted, TrustedActive(activePath, original, candidateBytes));
            }

            if (persisted == SpatialMigrationJournalStage.DescriptorPinned)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), original))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                try
                {
                    if (!fileSystem.Exists(backupPath))
                    {
                        fileSystem.WriteAllBytesDurable(backupPath, original);
                        fileSystem.FlushDirectory(directory);
                    }
                }
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
                try
                {
                    if (!fileSystem.Exists(stagingPath))
                    {
                        fileSystem.WriteAllBytesDurable(stagingPath, candidateBytes);
                        fileSystem.FlushDirectory(directory);
                    }
                }
                catch { return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Original); }
                if (!Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes) ||
                    candidate.Sha256 != SpatialContractSha256.Compute(candidateBytes))
                    return Failure(CandidateFailedReason, persisted, SpatialTrustedPayload.Original);
                AddDiagnostic(diagnostics, StagedCandidateVerifiedDiagnostic);
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
                if (fileSystem.Exists(stagingPath) && Same(fileSystem.ReadAllBytes(stagingPath), candidateBytes))
                    AddDiagnostic(diagnostics, StagedCandidateVerifiedDiagnostic);
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
                            fileSystem.ReadAllBytes(backupPath), ReplacementFailedReason, diagnostics);
                    }
                }
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Replaced);
                if (!RewriteJournal(journalPath, next))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), ReplacementFailedReason, diagnostics);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.Replaced)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                {
                    if (Same(fileSystem.ReadAllBytes(activePath), original))
                        return Failure(DurabilityFailedReason, persisted, SpatialTrustedPayload.Original);
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason, diagnostics);
                }
                AddDiagnostic(diagnostics, ReplacedPendingDurabilityDiagnostic);
                try { fileSystem.FlushDirectory(directory); }
                catch
                {
                    byte[] backupSnapshot = fileSystem.ReadAllBytes(backupPath);
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        backupSnapshot, DurabilityFailedReason, diagnostics);
                }
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason, diagnostics);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, next))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason, diagnostics);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.DurableVerified)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                AddDiagnostic(diagnostics, DurableCandidatePendingFinalizationDiagnostic);
                string receiptDiagnostic = TryWriteReceipt(directory, journal);
                if (receiptDiagnostic != null) AddDiagnostic(diagnostics, receiptDiagnostic);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, next))
                    return FailureWithDiagnostics(FinalizationFailedReason, persisted,
                        SpatialTrustedPayload.Candidate, diagnostics);
                journal = next;
                return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                    SpatialTrustedPayload.Candidate, diagnostics);
            }
            return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                SpatialTrustedPayload.Candidate, diagnostics);
        }

        private enum EvidenceKind
        { LiveJournal, FinalizedJournal, OriginalRestoredJournal, MalformedJournal,
          RedirectedEvidence, FilenameInvalidJournal, BindingInvalidJournal, OriginalBackup,
          CandidateStaging, JournalNext, RestoreStaging, RestorationIntent,
          FinalizationReceipt, ExistingQuarantine, Unknown }

        private sealed class EvidenceRecord
        {
            internal EvidenceRecord(string path, bool contained, byte[] bytes, EvidenceKind kind,
                SpatialMigrationJournal journal)
            { Path = path; Contained = contained; Bytes = bytes == null ? null : (byte[])bytes.Clone();
              Sha256 = bytes == null ? null : SpatialContractSha256.Compute(bytes); Kind = kind;
              Journal = journal; FilenameKind = ClassifyFilename(Path.GetFileName(path)); }
            internal string Path { get; }
            internal bool Contained { get; }
            internal byte[] Bytes { get; }
            internal string Sha256 { get; }
            internal EvidenceKind Kind { get; }
            internal EvidenceKind FilenameKind { get; }
            internal SpatialMigrationJournal Journal { get; }
        }

        private sealed class EvidenceSnapshot
        {
            internal EvidenceSnapshot(List<EvidenceRecord> records)
            { Records = records.AsReadOnly(); Live = records.Where(value => value.Kind ==
                EvidenceKind.LiveJournal).ToList().AsReadOnly(); Terminal = records.Where(value =>
                value.Kind == EvidenceKind.FinalizedJournal || value.Kind ==
                EvidenceKind.OriginalRestoredJournal).ToList().AsReadOnly(); Malformed = records.Where(
                value => value.Kind == EvidenceKind.MalformedJournal || value.Kind ==
                EvidenceKind.RedirectedEvidence || value.Kind == EvidenceKind.FilenameInvalidJournal ||
                value.Kind == EvidenceKind.BindingInvalidJournal).ToList().AsReadOnly(); }
            internal IReadOnlyList<EvidenceRecord> Records { get; }
            internal IReadOnlyList<EvidenceRecord> Live { get; }
            internal IReadOnlyList<EvidenceRecord> Terminal { get; }
            internal IReadOnlyList<EvidenceRecord> Malformed { get; }
            internal int LiveCount => Live.Count;
            internal SpatialContractResult<SpatialMigrationJournal> SingleLive => Live.Count == 1
                ? new SpatialContractResult<SpatialMigrationJournal>(Live[0].Journal,
                    Array.Empty<SpatialContractIssue>())
                : default(SpatialContractResult<SpatialMigrationJournal>);
        }

        private EvidenceSnapshot DiscoverEvidence(string directory, string stem)
        {
            int maximum = limits.MaximumCollectionRecords;
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory, stem + ".gd66-*", maximum + 1);
            if (paths.Count > maximum) throw new IOException("GD66 evidence limit exceeded.");
            var records = new List<EvidenceRecord>(paths.Count);
            foreach (string enumerated in paths.OrderBy(value => value, StringComparer.Ordinal))
            {
                string path = Path.GetFullPath(enumerated);
                bool contained = fileSystem.IsPathContainedWithoutRedirection(directory, path);
                if (!contained) { records.Add(new EvidenceRecord(path, false, null,
                    EvidenceKind.RedirectedEvidence, null)); continue; }
                byte[] bytes = fileSystem.ReadAllBytes(path);
                string name = Path.GetFileName(path);
                EvidenceKind sidecar = ClassifyFilename(name);
                if (!name.EndsWith(".journal.json", StringComparison.Ordinal))
                { records.Add(new EvidenceRecord(path, true, bytes, sidecar, null)); continue; }
                SpatialContractResult<SpatialMigrationJournal> parsed =
                    SpatialMigrationJournalContracts.Parse(bytes, limits);
                if (!parsed.IsValid) { records.Add(new EvidenceRecord(path, true, bytes,
                    EvidenceKind.MalformedJournal, null)); continue; }
                SpatialMigrationJournal journal = parsed.Value;
                if (!string.Equals(name, journal.RelativeJournalFilename, StringComparison.Ordinal))
                { records.Add(new EvidenceRecord(path, true, bytes,
                    EvidenceKind.FilenameInvalidJournal, journal)); continue; }
                if (!ResolveEvidencePaths(directory, journal, out string resolved, out string ignoredBackup,
                    out string ignoredStaging) || !string.Equals(path, resolved, StringComparison.Ordinal))
                { records.Add(new EvidenceRecord(path, true, bytes,
                    EvidenceKind.BindingInvalidJournal, journal)); continue; }
                EvidenceKind kind = journal.Stage == SpatialMigrationJournalStage.Finalized
                    ? EvidenceKind.FinalizedJournal : journal.Stage ==
                    SpatialMigrationJournalStage.OriginalRestored ? EvidenceKind.OriginalRestoredJournal
                    : EvidenceKind.LiveJournal;
                records.Add(new EvidenceRecord(path, true, bytes, kind, journal));
            }
            return new EvidenceSnapshot(records);
        }

        private static EvidenceKind ClassifyFilename(string name)
        {
            if (name.EndsWith(".journal.json", StringComparison.Ordinal)) return EvidenceKind.LiveJournal;
            if (name.EndsWith(".journal.json.next", StringComparison.Ordinal)) return EvidenceKind.JournalNext;
            if (name.EndsWith(".original.bak.restore.intent", StringComparison.Ordinal)) return EvidenceKind.RestorationIntent;
            if (name.EndsWith(".original.bak.restore", StringComparison.Ordinal)) return EvidenceKind.RestoreStaging;
            if (name.EndsWith(".original.bak", StringComparison.Ordinal)) return EvidenceKind.OriginalBackup;
            if (name.EndsWith(".candidate.tmp", StringComparison.Ordinal)) return EvidenceKind.CandidateStaging;
            if (name.EndsWith(".finalized", StringComparison.Ordinal)) return EvidenceKind.FinalizationReceipt;
            if (name.EndsWith(".evidence", StringComparison.Ordinal)) return EvidenceKind.ExistingQuarantine;
            return EvidenceKind.Unknown;
        }

        private bool QuarantineMalformedEvidence(string directory, IReadOnlyList<EvidenceRecord> malformed)
        {
            try
            {
                foreach (EvidenceRecord record in malformed)
                    if (!record.Contained || record.Bytes == null ||
                        !QuarantineEvidence(directory, record.Path, record.Bytes)) return false;
                return true;
            }
            catch { return false; }
        }

        private bool QuarantineEvidence(string directory, string path, byte[] bytes)
        {
            string evidenceHash = SpatialContractSha256.Compute(bytes);
            string evidenceName = Path.GetFileName(path);
            string pathHash = SpatialContractSha256.Compute(System.Text.Encoding.UTF8.GetBytes(
                evidenceName)).Substring(0, 16);
            int marker = evidenceName.IndexOf(".gd66-", StringComparison.Ordinal);
            string stem = marker > 0 ? evidenceName.Substring(0, marker) : "gd66";
            string quarantine = Path.Combine(directory, stem + ".gd66-quarantine-" + evidenceHash + "-" +
                pathHash + ".evidence");
            if (quarantine.Length > SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters ||
                !fileSystem.IsPathContainedWithoutRedirection(directory, quarantine)) return false;
            if (fileSystem.Exists(quarantine))
            {
                if (!Same(fileSystem.ReadAllBytes(quarantine), bytes)) return false;
                fileSystem.DeleteFile(path);
            }
            else fileSystem.MoveSameDirectoryAtomic(path, quarantine);
            fileSystem.FlushDirectory(directory);
            return !fileSystem.Exists(path) && fileSystem.Exists(quarantine) &&
                Same(fileSystem.ReadAllBytes(quarantine), bytes);
        }

        private bool ResolveEvidencePaths(string directory, SpatialMigrationJournal journal,
            out string journalPath, out string backupPath, out string stagingPath)
        {
            journalPath = null; backupPath = null; stagingPath = null;
            if (journal == null || !Resolve(directory, journal.RelativeJournalFilename, out journalPath) ||
                !Resolve(directory, journal.RelativeOriginalBackupFilename, out backupPath) ||
                !Resolve(directory, journal.RelativeCandidateStagingFilename, out stagingPath)) return false;
            string nextRelative = journal.RelativeJournalFilename + ".next";
            string restoreRelative = journal.RelativeOriginalBackupFilename + ".restore";
            string restoredRelative = journal.RelativeOriginalBackupFilename + ".restore.intent";
            if (!SpatialMigrationSidecarPaths.IsValidRelativeFilename(nextRelative,
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) ||
                !SpatialMigrationSidecarPaths.IsValidRelativeFilename(restoreRelative,
                    SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) ||
                !SpatialMigrationSidecarPaths.IsValidRelativeFilename(restoredRelative,
                    SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) ||
                !Resolve(directory, nextRelative, out string nextPath) ||
                !Resolve(directory, restoreRelative, out string restorePath) ||
                !Resolve(directory, restoredRelative, out string restoredPath)) return false;
            return fileSystem.IsPathContainedWithoutRedirection(directory, journalPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, backupPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, stagingPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, nextPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, restorePath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, restoredPath);
        }


        private bool HasStructuralCanonicalTargetEvidence(byte[] bytes)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits, issues, out ContractJsonNode root) ||
                root.Kind != ContractJsonKind.Object) return false;
            bool hasEnvelopeMember = false;
            ContractJsonNode primary = null;
            foreach (KeyValuePair<string, ContractJsonNode> field in root.Fields)
            {
                if (field.Key == "schema" || field.Key == "schemaVersion" || field.Key == "primary")
                    hasEnvelopeMember = true;
                if (field.Key == "schemaVersion" && field.Value.Kind == ContractJsonKind.Number &&
                    ContractJson.Int(field.Value, out int version) && version == 7) return true;
                if (field.Key == "primary" && field.Value.Kind == ContractJsonKind.Object) primary = field.Value;
            }
            ContractJsonNode payload = hasEnvelopeMember ? primary : root;
            if (payload == null) return false;
            foreach (KeyValuePair<string, ContractJsonNode> field in payload.Fields)
                if (field.Key == "canonicalSpatialAuthority" || field.Key == "spatialFloors") return true;
            return false;
        }


        private sealed class StaleJournalCleanupResult
        {
            internal bool JournalQuarantined;
            internal bool SidecarCleanupComplete = true;
        }

        private StaleJournalCleanupResult QuarantineBoundNonterminalEvidence(string directory, SpatialMigrationJournal journal)
        {
            var result = new StaleJournalCleanupResult();
            if (!ResolveEvidencePaths(directory, journal, out string journalPath, out string backupPath,
                out string stagingPath)) return result;
            try
            {
                if (!fileSystem.Exists(journalPath)) return result;
                result.JournalQuarantined = QuarantineEvidence(directory, journalPath,
                    fileSystem.ReadAllBytes(journalPath));
                if (!result.JournalQuarantined || fileSystem.Exists(journalPath)) return result;
            }
            catch { return result; }

            string receiptRelative = ReceiptName(journal.RelativeJournalFilename, journal.TransactionId);
            var sidecars = new List<string>
            {
                backupPath, stagingPath, journalPath + ".next", backupPath + ".restore",
                backupPath + ".restore.intent"
            };
            if (SpatialMigrationSidecarPaths.IsValidRelativeFilename(receiptRelative,
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) &&
                Resolve(directory, receiptRelative, out string receiptPath) &&
                fileSystem.IsPathContainedWithoutRedirection(directory, receiptPath))
                sidecars.Add(receiptPath);
            else result.SidecarCleanupComplete = false;

            foreach (string path in sidecars.OrderBy(value => value, StringComparer.Ordinal))
            {
                try
                {
                    if (!fileSystem.Exists(path)) continue;
                    result.SidecarCleanupComplete = QuarantineEvidence(directory, path,
                        fileSystem.ReadAllBytes(path)) && result.SidecarCleanupComplete;
                }
                catch { result.SidecarCleanupComplete = false; }
            }
            return result;
        }

        private IReadOnlyList<string> CleanupOrphanEvidence(string directory, EvidenceSnapshot evidence)
        {
            var diagnostics = new List<string>();
            foreach (EvidenceRecord record in evidence.Records.OrderBy(value => value.Path, StringComparer.Ordinal))
            {
                if (record.Kind != EvidenceKind.MalformedJournal && record.Kind != EvidenceKind.FilenameInvalidJournal &&
                    record.Kind != EvidenceKind.BindingInvalidJournal && record.Kind != EvidenceKind.OriginalBackup &&
                    record.Kind != EvidenceKind.CandidateStaging && record.Kind != EvidenceKind.FinalizationReceipt &&
                    record.Kind != EvidenceKind.JournalNext && record.Kind != EvidenceKind.RestoreStaging &&
                    record.Kind != EvidenceKind.RestorationIntent)
                    continue;
                try
                {
                    if (record.Bytes == null || !QuarantineEvidence(directory, record.Path, record.Bytes)) continue;
                    if (record.Kind == EvidenceKind.OriginalBackup) AddDiagnostic(diagnostics, FinalizedBackupQuarantinedDiagnostic);
                    else if (record.Kind == EvidenceKind.CandidateStaging || record.Kind == EvidenceKind.JournalNext)
                        AddDiagnostic(diagnostics, OrphanStagingQuarantinedDiagnostic);
                    else if (record.Kind == EvidenceKind.FinalizationReceipt || record.Kind == EvidenceKind.RestoreStaging ||
                        record.Kind == EvidenceKind.RestorationIntent)
                        AddDiagnostic(diagnostics, OrphanReceiptQuarantinedDiagnostic);
                }
                catch { /* Orphan cleanup cannot revoke already verified canonical active bytes. */ }
            }
            return diagnostics;
        }

        private static void AddDiagnostic(List<string> diagnostics, string diagnostic)
        {
            if (!diagnostics.Contains(diagnostic)) diagnostics.Add(diagnostic);
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

        private bool IsCanonicalSchemaSeven(byte[] bytes, SpatialMigrationInputDescriptor descriptor = null,
            string expectedTransactionId = null, string expectedDescriptorFingerprint = null,
            string expectedCandidateSha256 = null)
        {
            if (productionContent == null) return false;
            var completeLimits = new CanonicalSpatialSerializationLimits(limits,
                new CanonicalSpatialSaveWorkloadLimits(limits.MaximumCollectionRecords,
                    limits.MaximumCollectionRecords));
            if (descriptor != null)
            {
                return recoveryContext.TryCreateUnfinishedValidationContext(descriptor,
                    expectedTransactionId, expectedDescriptorFingerprint, expectedCandidateSha256,
                    out DetachedUnfinishedAttemptValidationContext unfinished) &&
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, unfinished).IsValid;
            }
            return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes,
                new DetachedCurrentTargetValidationContext(recoveryContext.Compatibility,
                    productionContent, recoveryContext.LegacyConfigurationBytes, completeLimits)).IsValid;
        }

        private SpatialTrustedPayload TrustedActive(string activePath, byte[] original, byte[] candidate)
        {
            byte[] active = fileSystem.ReadAllBytes(activePath);
            if (Same(active, original)) return SpatialTrustedPayload.Original;
            if (Same(active, candidate)) return SpatialTrustedPayload.Candidate;
            return SpatialTrustedPayload.None;
        }
        private SpatialTrustedPayload TrustedActiveSafe(string activePath, byte[] original, byte[] candidate)
        { try { return TrustedActive(activePath, original, candidate); }
          catch { return SpatialTrustedPayload.None; } }
        private bool Resolve(string directory, string relative, out string path) =>
            SpatialMigrationSidecarPaths.TryResolveContained(directory, relative,
                SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters, out path);
        private bool WriteJournal(string path, SpatialMigrationJournal journal)
        {
            SpatialContractResult<byte[]> bytes = SpatialMigrationJournalContracts.Serialize(journal, limits);
            if (!bytes.IsValid) return false;
            try
            {
                fileSystem.WriteAllBytesDurable(path, bytes.Value);
                fileSystem.FlushDirectory(Path.GetDirectoryName(path));
            }
            catch { return false; }
            return VerifyJournal(path, bytes.Value, journal.Stage);
        }
        private bool RewriteJournal(string path, SpatialMigrationJournal journal)
        {
            SpatialContractResult<byte[]> bytes = SpatialMigrationJournalContracts.Serialize(journal, limits);
            if (!bytes.IsValid) return false;
            string directory = Path.GetDirectoryName(path);
            string temporary = path + ".next";
            try
            {
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, temporary)) return false;
                if (fileSystem.Exists(temporary))
                {
                    byte[] existing = fileSystem.ReadAllBytes(temporary);
                    if (!Same(existing, bytes.Value) && !QuarantineEvidence(directory, temporary, existing))
                        return false;
                }
                if (!fileSystem.Exists(temporary))
                {
                    fileSystem.WriteAllBytesDurable(temporary, bytes.Value);
                    fileSystem.FlushDirectory(directory);
                }
                if (!VerifyJournal(temporary, bytes.Value, journal.Stage)) return false;
                fileSystem.ReplaceSameDirectoryAtomic(temporary, path);
                fileSystem.FlushDirectory(directory);
            }
            catch { return false; }
            return VerifyJournal(path, bytes.Value, journal.Stage);
        }
        private bool VerifyJournal(string path, byte[] expected, SpatialMigrationJournalStage expectedStage)
        {
            byte[] actual = fileSystem.ReadAllBytes(path);
            SpatialContractResult<SpatialMigrationJournal> parsed = SpatialMigrationJournalContracts.Parse(actual, limits);
            return Same(actual, expected) && parsed.IsValid && parsed.Value.Stage == expectedStage;
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
        private static DetachedSpatialMigrationOutcome FailureWithDiagnostics(string reason,
            SpatialMigrationJournalStage? stage, SpatialTrustedPayload trusted, IEnumerable<string> diagnostics) =>
            new DetachedSpatialMigrationOutcome(false, reason, stage, trusted, diagnostics);
        private static DetachedSpatialMigrationOutcome WithDiagnostics(
            DetachedSpatialMigrationOutcome outcome, IEnumerable<string> diagnostics)
        {
            var merged = new List<string>();
            if (diagnostics != null)
                foreach (string diagnostic in diagnostics)
                    if (!merged.Contains(diagnostic)) merged.Add(diagnostic);
            foreach (string diagnostic in outcome.Diagnostics)
                if (!merged.Contains(diagnostic)) merged.Add(diagnostic);
            return new DetachedSpatialMigrationOutcome(outcome.IsSuccess, outcome.Reason, outcome.Stage,
                outcome.TrustedPayload, merged);
        }
        private static DetachedSpatialMigrationOutcome WithDiagnostic(
            DetachedSpatialMigrationOutcome outcome, string diagnostic)
        {
            return WithDiagnostics(outcome, new[] { diagnostic });
        }
    }
}
