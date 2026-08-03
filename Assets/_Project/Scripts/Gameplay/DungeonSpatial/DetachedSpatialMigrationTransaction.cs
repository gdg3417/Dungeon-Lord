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
            string transactionId, string fingerprint,
            out DetachedUnfinishedAttemptValidationContext context)
        {
            context = null;
            if (ValidatePins(descriptor) != null || !Compatibility.TryRecoverMigration(
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
                selectedContract.Value, profile, geometry, ProductionContent, legacyConfigurationBytes,
                validationInputs, Limits);
            return true;
        }

        internal bool IsJournalBoundCandidateValid(byte[] bytes, SpatialMigrationJournal journal)
        {
            if (bytes == null || journal == null || !HashEquals(bytes, journal.ExpectedCandidateSha256))
                return false;
            string fingerprint;
            try { fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                journal.Descriptor, Limits.Serialized); }
            catch (ArgumentException) { return false; }
            if (fingerprint != journal.DescriptorFingerprintSha256) return false;
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                journal.Descriptor.OriginalPayloadSha256, fingerprint);
            if (identity != journal.TransactionIdentitySha256 ||
                SpatialMigrationTransactionIdentity.CreateTransactionId(identity) != journal.TransactionId)
                return false;
            return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, Limits, ProductionContent,
                journal.TransactionId, journal.DescriptorFingerprintSha256).IsValid;
        }

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
                SpatialContractResult<SpatialMigrationJournal> existing = FindLiveJournal(
                    directory, Path.GetFileNameWithoutExtension(activePath), out int liveCount);
                if (liveCount > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                SpatialMigrationJournal journal;
                if (liveCount == 1)
                {
                    if (!existing.IsValid || !string.Equals(existing.Value.TransactionId, transactionId, StringComparison.Ordinal))
                    {
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
                        IsCanonicalSchemaSeven(candidate.GetBytes(), descriptor, transactionId, fingerprint))
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
            catch (IOException) { return Failure(RecoveryFailedReason, null, SpatialTrustedPayload.None); }
            catch (UnauthorizedAccessException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
            catch (ArgumentException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
            catch (NotSupportedException) { return Failure(PathInvalidReason, null, SpatialTrustedPayload.None); }
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
                SpatialContractResult<SpatialMigrationJournal> found = FindLiveJournal(
                    directory, Path.GetFileNameWithoutExtension(activePath), out int count);
                if (count > 1) return Failure(MultipleAttemptsReason, null, SpatialTrustedPayload.None);
                if (count == 0)
                {
                    byte[] active = fileSystem.ReadAllBytes(activePath);
                    // Terminal journals and receipts are audit evidence only.  Current-target C is
                    // self-authoritative after complete validation and may have changed normally.
                    if (IsCanonicalSchemaSeven(active))
                        return new DetachedSpatialMigrationOutcome(true, AlreadyCommittedReason, null,
                            SpatialTrustedPayload.Candidate);
                    DetachedLegacyValidationResult legacy = recoveryContext.ValidateLegacy(active);
                    bool activeLegacy = legacy.IsValid;
                    if (TryQuarantineMalformedJournal(directory,
                        Path.GetFileNameWithoutExtension(activePath), out bool malformed))
                    {
                        if (activeLegacy) return Failure(
                            "gd66.transaction.journal_malformed_with_verified_original", null,
                            SpatialTrustedPayload.Original);
                    }
                    else if (malformed) return Failure(PathInvalidReason, null, SpatialTrustedPayload.None);
                    if (activeLegacy)
                        return new DetachedSpatialMigrationOutcome(true, NoJournalLegacyDiagnostic, null,
                            SpatialTrustedPayload.Original, new[] { NoJournalLegacyDiagnostic });
                    if (!string.IsNullOrEmpty(legacy.Reason))
                        return Failure(legacy.Reason, null, SpatialTrustedPayload.None);
                    SpatialMigrationJournal terminal = FindTerminalJournal(directory,
                        Path.GetFileNameWithoutExtension(activePath), out int terminalCount);
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
                return Restore(journalPath, backupPath, activePath, directory, journal, backup, pinFailure);
            if (recoveryContext.IsJournalBoundCandidateValid(active, journal))
                return Failure(RollbackSourceMissingReason, journal.Stage, SpatialTrustedPayload.Candidate);
            return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
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
            if (activeOriginal && journal.Stage != SpatialMigrationJournalStage.DescriptorPinned &&
                HasRestorationIntent(backupPath, journal))
            {
                SpatialMigrationJournal restored = CopyStage(journal,
                    SpatialMigrationJournalStage.OriginalRestored);
                if (!RewriteJournal(journalPath, restored))
                    return Failure(OriginalRestoredStageWriteFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original);
                return new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                    restored.Stage, SpatialTrustedPayload.Original);
            }
            if (journal.Stage == SpatialMigrationJournalStage.DescriptorPinned)
                return activeOriginal
                    ? Failure(BackupIncompleteReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (!backupValid && !activeOriginal)
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            if (journal.Stage == SpatialMigrationJournalStage.BackupVerified)
                return activeOriginal ? Failure(CandidateAbsentReason, journal.Stage, SpatialTrustedPayload.Original)
                    : Restore(journalPath, backupPath, activePath, directory, journal, backup);

            bool activeCandidate = HashIs(active, journal.ExpectedCandidateSha256) && IsCanonicalSchemaSeven(active,
                journal.Descriptor, journal.TransactionId, journal.DescriptorFingerprintSha256);
            byte[] staged = fileSystem.Exists(stagingPath) ? fileSystem.ReadAllBytes(stagingPath) : null;
            bool stagedCandidate = HashIs(staged, journal.ExpectedCandidateSha256) &&
                IsCanonicalSchemaSeven(staged, journal.Descriptor, journal.TransactionId,
                    journal.DescriptorFingerprintSha256);
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
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup,
                        ReplacementFailedReason);
                journal = replaced; activeCandidate = true;
            }
            if (journal.Stage == SpatialMigrationJournalStage.Replaced)
            {
                if (!activeCandidate) return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                try { fileSystem.FlushDirectory(directory); }
                catch { return Restore(journalPath, backupPath, activePath, directory, journal, backup,
                    DurabilityFailedReason); }
                active = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(active, journal.ExpectedCandidateSha256) || !IsCanonicalSchemaSeven(active,
                    journal.Descriptor, journal.TransactionId, journal.DescriptorFingerprintSha256))
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                CleanupInterruptedRestoration(directory, backupPath);
                SpatialMigrationJournal durable = CopyStage(journal, SpatialMigrationJournalStage.DurableVerified);
                if (!RewriteJournal(journalPath, durable))
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup,
                        DurabilityFailedReason);
                journal = durable;
            }
            if (journal.Stage == SpatialMigrationJournalStage.DurableVerified)
            {
                byte[] durableActive = fileSystem.ReadAllBytes(activePath);
                if (!HashIs(durableActive, journal.ExpectedCandidateSha256) ||
                    !IsCanonicalSchemaSeven(durableActive, journal.Descriptor, journal.TransactionId,
                        journal.DescriptorFingerprintSha256))
                    return Restore(journalPath, backupPath, activePath, directory, journal, backup);
                string receiptDiagnostic = TryWriteReceipt(directory, journal);
                SpatialMigrationJournal finalized = CopyStage(journal, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, finalized))
                    return Failure(FinalizationFailedReason, journal.Stage, SpatialTrustedPayload.Candidate);
                journal = finalized;
                return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                    SpatialTrustedPayload.Candidate, receiptDiagnostic == null ? null : new[]
                    { receiptDiagnostic });
            }
            return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage, SpatialTrustedPayload.Candidate);
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
            string restoredFailureReason = null)
        {
            if (!HashIs(verifiedBackup, journal.OriginalPayloadSha256))
                return Failure(NoTrustedPayloadReason, journal.Stage, SpatialTrustedPayload.None);
            bool restoredActive = false;
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
                string intentRelative = Path.GetFileName(backupPath) + ".restore.intent";
                if (!Resolve(directory, intentRelative, out string intentPath) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, intentPath))
                    return Failure(PathInvalidReason, journal.Stage, SpatialTrustedPayload.None);
                var intent = new DetachedRestorationIntent(journal.TransactionId,
                    journal.DescriptorFingerprintSha256, journal.OriginalPayloadSha256,
                    SpatialContractSha256.Compute(verifiedBackup), journal.RelativeJournalFilename,
                    (int)journal.Stage);
                byte[] intentBytes = DetachedRestorationIntentContract.Serialize(intent, limits);
                if (fileSystem.Exists(intentPath))
                {
                    byte[] existingIntent = fileSystem.ReadAllBytes(intentPath);
                    if (!Same(existingIntent, intentBytes) ||
                        DetachedRestorationIntentContract.Parse(existingIntent, limits) == null)
                    {
                        if (!QuarantineEvidence(directory, intentPath, existingIntent))
                            return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                    }
                }
                if (!fileSystem.Exists(intentPath)) fileSystem.WriteAllBytesDurable(intentPath, intentBytes);
                fileSystem.FlushDirectory(directory);
                byte[] persistedIntent = fileSystem.ReadAllBytes(intentPath);
                if (!Same(persistedIntent, intentBytes) ||
                    DetachedRestorationIntentContract.Parse(persistedIntent, limits) == null)
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                fileSystem.ReplaceSameDirectoryAtomic(restoreStaging, activePath);
                fileSystem.FlushDirectory(directory);
                if (!HashIs(fileSystem.ReadAllBytes(activePath), journal.OriginalPayloadSha256))
                    return Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
                restoredActive = true;
                SpatialMigrationJournal restored = CopyStage(journal, SpatialMigrationJournalStage.OriginalRestored);
                if (!RewriteJournal(journalPath, restored))
                    return Failure(OriginalRestoredStageWriteFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original);
                try { QuarantineEvidence(directory, intentPath, intentBytes); }
                catch { }
                return restoredFailureReason == null
                    ? new DetachedSpatialMigrationOutcome(true, RecoveredOriginalReason,
                        restored.Stage, SpatialTrustedPayload.Original)
                    : Failure(restoredFailureReason, restored.Stage, SpatialTrustedPayload.Original);
            }
            catch
            {
                if (!restoredActive && journal.Stage == SpatialMigrationJournalStage.Replaced)
                {
                    try
                    {
                        byte[] active = fileSystem.ReadAllBytes(activePath);
                        if (HashIs(active, journal.ExpectedCandidateSha256) && IsCanonicalSchemaSeven(active,
                            journal.Descriptor, journal.TransactionId, journal.DescriptorFingerprintSha256))
                            return new DetachedSpatialMigrationOutcome(false,
                                ReplacedPendingDurabilityDiagnostic, journal.Stage,
                                SpatialTrustedPayload.Candidate,
                                new[] { ReplacedPendingDurabilityDiagnostic });
                    }
                    catch { }
                }
                return restoredActive
                    ? Failure(OriginalRestoredStageWriteFailedReason, journal.Stage,
                        SpatialTrustedPayload.Original)
                    : Failure(RecoveryFailedReason, journal.Stage, SpatialTrustedPayload.None);
            }
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
                if (!RewriteJournal(journalPath, next))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), ReplacementFailedReason);
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
                if (!RewriteJournal(journalPath, next))
                    return Restore(journalPath, backupPath, activePath, directory, journal,
                        fileSystem.ReadAllBytes(backupPath), DurabilityFailedReason);
                journal = next; persisted = journal.Stage;
            }
            if (persisted == SpatialMigrationJournalStage.DurableVerified)
            {
                if (!Same(fileSystem.ReadAllBytes(activePath), candidateBytes))
                    return Failure(ActivePayloadUnknownReason, persisted, SpatialTrustedPayload.None);
                string receiptDiagnostic = TryWriteReceipt(directory, journal);
                SpatialMigrationJournal next = CreateJournal(descriptor, fingerprint, identity, transactionId, names,
                    descriptor.OriginalPayloadSha256, candidate.Sha256, SpatialMigrationJournalStage.Finalized);
                if (!RewriteJournal(journalPath, next)) return Failure(FinalizationFailedReason, persisted, SpatialTrustedPayload.Candidate);
                journal = next;
                return new DetachedSpatialMigrationOutcome(true, SuccessReason, journal.Stage,
                    SpatialTrustedPayload.Candidate, receiptDiagnostic == null ? null : new[]
                    { receiptDiagnostic });
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

        private SpatialMigrationJournal FindTerminalJournal(string directory, string stem, out int count)
        {
            count = 0; SpatialMigrationJournal found = null;
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory,
                stem + ".gd66-*.journal.json", limits.MaximumCollectionRecords);
            for (int index = 0; index < paths.Count; index++)
            {
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, paths[index])) continue;
                SpatialContractResult<SpatialMigrationJournal> parsed = SpatialMigrationJournalContracts.Parse(
                    fileSystem.ReadAllBytes(paths[index]), limits);
                if (!parsed.IsValid || (parsed.Value.Stage != SpatialMigrationJournalStage.Finalized &&
                    parsed.Value.Stage != SpatialMigrationJournalStage.OriginalRestored) ||
                    Path.GetFileName(paths[index]) != parsed.Value.RelativeJournalFilename) continue;
                count++; found = parsed.Value;
            }
            return found;
        }

        private bool TryQuarantineMalformedJournal(string directory, string stem, out bool malformed)
        {
            malformed = false;
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory,
                stem + ".gd66-*.journal.json", limits.MaximumCollectionRecords);
            string malformedPath = null; byte[] malformedBytes = null;
            for (int index = 0; index < paths.Count; index++)
            {
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, paths[index]))
                { malformed = true; return false; }
                byte[] bytes = fileSystem.ReadAllBytes(paths[index]);
                if (!SpatialMigrationJournalContracts.Parse(bytes, limits).IsValid)
                {
                    if (malformedPath != null) { malformed = true; return false; }
                    malformedPath = paths[index]; malformedBytes = bytes;
                }
            }
            if (malformedPath == null) return false;
            malformed = true;
            try { return QuarantineEvidence(directory, malformedPath, malformedBytes); }
            catch { return false; }
        }

        private bool QuarantineEvidence(string directory, string path, byte[] bytes)
        {
            string evidenceHash = SpatialContractSha256.Compute(bytes);
            string pathHash = SpatialContractSha256.Compute(System.Text.Encoding.UTF8.GetBytes(
                Path.GetFileName(path))).Substring(0, 16);
            string quarantine = Path.Combine(directory, "gd66-quarantine-" + evidenceHash + "-" +
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
            string expectedTransactionId = null, string expectedDescriptorFingerprint = null)
        {
            if (productionContent == null) return false;
            var completeLimits = new CanonicalSpatialSerializationLimits(limits,
                new CanonicalSpatialSaveWorkloadLimits(limits.MaximumCollectionRecords,
                    limits.MaximumCollectionRecords));
            if (descriptor != null)
            {
                return recoveryContext.TryCreateUnfinishedValidationContext(descriptor,
                    expectedTransactionId, expectedDescriptorFingerprint,
                    out DetachedUnfinishedAttemptValidationContext unfinished) &&
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, unfinished).IsValid;
            }
            return DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes,
                new DetachedCurrentTargetValidationContext(recoveryContext.Compatibility,
                    productionContent, completeLimits)).IsValid;
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
                if (fileSystem.Exists(temporary))
                {
                    byte[] existing = fileSystem.ReadAllBytes(temporary);
                    if (!Same(existing, bytes.Value) && !QuarantineEvidence(
                        Path.GetDirectoryName(path), temporary, existing)) return false;
                }
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
        private static DetachedSpatialMigrationOutcome WithDiagnostic(
            DetachedSpatialMigrationOutcome outcome, string diagnostic)
        {
            string[] diagnostics = outcome.Diagnostics.Concat(new[] { diagnostic }).Distinct().ToArray();
            return new DetachedSpatialMigrationOutcome(outcome.IsSuccess, outcome.Reason, outcome.Stage,
                outcome.TrustedPayload, diagnostics);
        }
    }
}
