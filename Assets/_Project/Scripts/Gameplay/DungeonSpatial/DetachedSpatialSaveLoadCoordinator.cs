using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum DetachedSpatialSaveLoadDisposition
    {
        None = 0,
        CurrentTarget = 1,
        AlreadyCommitted = 2,
        Migrated = 3,
        RecoveredThenMigrated = 4
    }

    public sealed class DetachedSpatialSaveLoadResult
    {
        private readonly byte[] validatedBytes;

        internal DetachedSpatialSaveLoadResult(bool success, string reason,
            SpatialTrustedPayload trustedPayload, DetachedSpatialSaveLoadDisposition disposition,
            byte[] bytes, DetachedCompleteSaveValidationResult validation,
            DetachedCanonicalSaveSession session, SaveData runtimeProjection,
            DetachedSpatialMigrationOutcome recovery, DetachedSpatialMigrationOutcome transaction,
            IEnumerable<string> diagnostics)
        {
            IsSuccess = success; Reason = reason; TrustedPayload = trustedPayload;
            Disposition = disposition; validatedBytes = bytes == null ? null : (byte[])bytes.Clone();
            Validation = validation; Session = session; RuntimeProjection = runtimeProjection;
            Recovery = recovery; Transaction = transaction;
            Diagnostics = diagnostics == null ? Array.Empty<string>() :
                new List<string>(diagnostics).ToArray();
        }

        public bool IsSuccess { get; }
        public string Reason { get; }
        public SpatialTrustedPayload TrustedPayload { get; }
        public DetachedSpatialSaveLoadDisposition Disposition { get; }
        public DetachedCompleteSaveValidationResult Validation { get; }
        public DetachedCanonicalSaveSession Session { get; }
        public SaveData RuntimeProjection { get; }
        public DetachedSpatialMigrationOutcome Recovery { get; }
        public DetachedSpatialMigrationOutcome Transaction { get; }
        public string[] Diagnostics { get; }
        public byte[] GetValidatedBytes() => validatedBytes == null ? null : (byte[])validatedBytes.Clone();
    }

    /// <summary>
    /// Inactive raw-before-legacy orchestration boundary. It may settle and execute the existing
    /// durable GD66 transaction, but it never publishes gameplay state or participates in live save I/O.
    /// </summary>
    public sealed class DetachedSpatialSaveLoadCoordinator
    {
        private readonly SaveSpatialMigrationLimitsProfile limits;
        private readonly SpatialLayoutCompatibilitySnapshot compatibility;
        private readonly ProductionSpatialContentSnapshot production;
        private readonly byte[] legacyConfiguration;
        private readonly Dictionary<string, byte[]> validationInputs;
        private readonly RawSaveEnvelopeVersionContract rawVersions;
        private readonly RawLegacyBlankFloorContract blankFloor;

        public DetachedSpatialSaveLoadCoordinator(SaveSpatialMigrationLimitsProfile limits,
            SpatialLayoutCompatibilitySnapshot compatibility,
            ProductionSpatialContentSnapshot production, byte[] legacyConfiguration,
            IReadOnlyDictionary<string, byte[]> validationInputs,
            RawSaveEnvelopeVersionContract rawVersions, RawLegacyBlankFloorContract blankFloor)
        {
            this.limits = limits; this.compatibility = compatibility; this.production = production;
            this.legacyConfiguration = legacyConfiguration == null ? null : (byte[])legacyConfiguration.Clone();
            this.validationInputs = validationInputs == null ? null : new Dictionary<string, byte[]>(StringComparer.Ordinal);
            if (validationInputs != null)
                foreach (KeyValuePair<string, byte[]> pair in validationInputs)
                    this.validationInputs.Add(pair.Key, pair.Value == null ? null : (byte[])pair.Value.Clone());
            this.rawVersions = rawVersions; this.blankFloor = blankFloor;
        }

        public DetachedSpatialSaveLoadResult Load(string activePath)
        {
            string dependencyReason = ValidateDependencies();
            return dependencyReason == null
                ? LoadAfterDependencies(activePath, SpatialMigrationFileSystemSelector.Evaluate(activePath))
                : Failure(dependencyReason);
        }

        public DetachedSpatialSaveLoadResult Load(string activePath,
            SpatialMigrationActivationPreflight preflight)
        {
            string dependencyReason = ValidateDependencies();
            return dependencyReason == null ? LoadAfterDependencies(activePath, preflight) : Failure(dependencyReason);
        }

        internal DetachedSpatialSaveLoadResult Load(string activePath,
            Func<string, SpatialMigrationActivationPreflight> preflightEvaluator)
        {
            string dependencyReason = ValidateDependencies();
            if (dependencyReason != null) return Failure(dependencyReason);
            if (preflightEvaluator == null) return Failure(SpatialMigrationCapabilityReason.NativeProbeFailed);
            return LoadAfterDependencies(activePath, preflightEvaluator(activePath));
        }

        private DetachedSpatialSaveLoadResult LoadAfterDependencies(string activePath,
            SpatialMigrationActivationPreflight preflight)
        {
            if (preflight == null || !preflight.IsSupported || preflight.FileSystem == null)
                return Failure(preflight?.Reason ?? SpatialMigrationCapabilityReason.NativeProbeFailed);

            DetachedSpatialMigrationRecoveryContext recoveryContext;
            DetachedCurrentTargetValidationContext currentContext;
            RunSimulationConfig legacy;
            try
            {
                legacy = LegacyGameplayConfigurationContract.Parse(legacyConfiguration);
                if (legacy == null) return Failure("gd66.transaction.pinned_input_missing");
                recoveryContext = new DetachedSpatialMigrationRecoveryContext(compatibility, production,
                    validationInputs, legacyConfiguration, limits.Canonical, limits.Raw, rawVersions,
                    blankFloor, limits.Whole);
                currentContext = new DetachedCurrentTargetValidationContext(compatibility, production,
                    legacyConfiguration, limits.Canonical);
            }
            catch { return Failure("gd66.profile.invalid"); }

            var migration = new DetachedSpatialMigrationTransaction(preflight.FileSystem, recoveryContext);
            DetachedSpatialMigrationOutcome recovered = migration.Recover(activePath);
            if (!recovered.IsSuccess)
                return Failure(recovered.Reason, recovered.TrustedPayload, recovered, null,
                    recovered.Diagnostics);

            byte[] trusted;
            try { trusted = preflight.FileSystem.ReadAllBytes(activePath); }
            catch { return Failure(DetachedSpatialMigrationTransaction.NoTrustedPayloadReason,
                recovered.TrustedPayload, recovered); }

            if (recovered.TrustedPayload == SpatialTrustedPayload.Candidate)
            {
                DetachedSpatialSaveLoadDisposition disposition = recovered.Reason ==
                    DetachedSpatialMigrationTransaction.AlreadyCommittedReason
                    ? DetachedSpatialSaveLoadDisposition.AlreadyCommitted
                    : DetachedSpatialSaveLoadDisposition.CurrentTarget;
                return PublishValidated(trusted, currentContext, disposition, recovered, null);
            }
            if (recovered.TrustedPayload != SpatialTrustedPayload.Original &&
                recovered.TrustedPayload != SpatialTrustedPayload.Backup)
                return Failure(DetachedSpatialMigrationTransaction.NoTrustedPayloadReason,
                    recovered.TrustedPayload, recovered);

            RawSavePayloadClassification classification = RawSavePayloadClassifier.Classify(
                trusted, limits.Raw, rawVersions, blankFloor);
            if (!classification.IsSuccess)
                return Failure(classification.FailureReason, recovered.TrustedPayload, recovered);
            int schema = classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                ? 1 : classification.SchemaVersion.GetValueOrDefault();
            if (schema == DetachedWholeSaveCandidateSerializer.TargetSchemaVersion)
                return PublishValidated(trusted, currentContext,
                    DetachedSpatialSaveLoadDisposition.CurrentTarget, recovered, null);

            DescriptorSeedResult descriptorResult = CreateDescriptorSeed(trusted, classification, schema);
            if (!descriptorResult.IsSuccess)
                return Failure(descriptorResult.Reason, recovered.TrustedPayload, recovered);
            SpatialMigrationInputDescriptor descriptor = descriptorResult.Descriptor;
            var inputs = new DetachedSpatialMigrationPreparationInputs(trusted, classification, descriptor,
                compatibility, production, legacy, validationInputs, limits.Canonical, limits.Whole);
            DetachedSpatialMigrationPreparationResult prepared = DetachedSpatialMigrationPreparer.Prepare(inputs);
            if (!prepared.IsSuccess)
                return Failure(prepared.Reason, recovered.TrustedPayload, recovered, null,
                    prepared.Diagnostics);

            DetachedSpatialMigrationOutcome executed = migration.Execute(activePath, prepared.Attempt);
            if (!executed.IsSuccess)
                return Failure(executed.Reason, executed.TrustedPayload, recovered, executed,
                    executed.Diagnostics);
            byte[] committed;
            try { committed = preflight.FileSystem.ReadAllBytes(activePath); }
            catch { return Failure(DetachedSpatialMigrationTransaction.NoTrustedPayloadReason,
                executed.TrustedPayload, recovered, executed); }
            DetachedSpatialSaveLoadDisposition migratedDisposition = recovered.Reason ==
                DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic
                ? DetachedSpatialSaveLoadDisposition.Migrated
                : DetachedSpatialSaveLoadDisposition.RecoveredThenMigrated;
            return PublishValidated(committed, currentContext, migratedDisposition, recovered, executed);
        }

        private DetachedSpatialSaveLoadResult PublishValidated(byte[] bytes,
            DetachedCurrentTargetValidationContext currentContext,
            DetachedSpatialSaveLoadDisposition disposition,
            DetachedSpatialMigrationOutcome recovery, DetachedSpatialMigrationOutcome transaction)
        {
            DetachedCompleteSaveValidationResult validation =
                DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, currentContext);
            if (!validation.IsValid || !validation.CurrentTargetValidated)
                return Failure(DetachedWholeSaveCandidateSerializer.CandidateInvalidReason,
                    transaction?.TrustedPayload ?? recovery?.TrustedPayload ?? SpatialTrustedPayload.None,
                    recovery, transaction);
            DetachedCanonicalSaveSessionResult opened =
                DetachedCanonicalSaveSession.Open(validation.GetBytes(), currentContext, limits);
            if (!opened.IsSuccess || !DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.
                CanonicalMvpRouteProjection.TryPublishValidated(validation, out SaveData runtime, out string reason))
                return Failure(reason ?? opened.Reason ??
                    DungeonBuilder.M0.Gameplay.MvpDungeonPlacements.CanonicalMvpRouteProjection.
                        ContradictoryAuthorityReason,
                    transaction?.TrustedPayload ?? recovery?.TrustedPayload ?? SpatialTrustedPayload.None,
                    recovery, transaction);
            return new DetachedSpatialSaveLoadResult(true, transaction?.Reason ?? recovery?.Reason,
                transaction?.TrustedPayload ?? recovery?.TrustedPayload ?? SpatialTrustedPayload.Candidate,
                disposition, validation.GetBytes(), validation, opened.Session, runtime, recovery, transaction,
                transaction?.Diagnostics ?? recovery?.Diagnostics);
        }

        private DescriptorSeedResult CreateDescriptorSeed(byte[] original,
            RawSavePayloadClassification classification, int schema)
        {
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> contract =
                compatibility.SelectContract(DetachedWholeSaveCandidateSerializer.TargetSchemaVersion);
            if (!contract.Success) return DescriptorSeedResult.Failure(contract.Code);
            CompatibilitySelectionResult<SpatialMigrationCompatibilityProfile> profile =
                compatibility.SelectMigration(schema, DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                    contract.Value.CanonicalLayoutContractVersion);
            if (!profile.Success) return DescriptorSeedResult.Failure(profile.Code);
            try
            {
                SpatialMigrationCompatibilityProfile selected = profile.Value;
                return DescriptorSeedResult.Success(new SpatialMigrationInputDescriptor(
                    SpatialContractSha256.Compute(original), schema,
                    classification.Envelope == RawSaveEnvelopeKind.UnwrappedSaveData
                        ? SpatialRawEnvelopeClassification.UnwrappedSaveData
                        : SpatialRawEnvelopeClassification.WrappedSaveRoot,
                    DetachedWholeSaveCandidateSerializer.TargetSchemaVersion,
                    SpatialMigrationContractIdentity.AuthorityMarkerContractVersion,
                    SpatialMigrationContractIdentity.MigrationContractVersion, selected.ProfileId,
                    selected.ProfileVersion, selected.CanonicalHash, selected.GeometryId,
                    selected.GeometryVersion, selected.GeometryCanonicalHash,
                    SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                        production.Manifest)),
                    SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                        production.Catalog)), ValidationHashes(), SpatialContractSha256.Compute(legacyConfiguration),
                    SpatialMigrationContractIdentity.CanonicalSerializerId,
                    SpatialMigrationContractIdentity.CanonicalSerializerVersion));
            }
            catch { return DescriptorSeedResult.Failure("gd66.transaction.pinned_input_hash_mismatch"); }
        }

        private SpatialValidationInputHash[] ValidationHashes()
        {
            if (validationInputs == null) return Array.Empty<SpatialValidationInputHash>();
            var names = new List<string>(validationInputs.Keys); names.Sort(StringComparer.Ordinal);
            var values = new SpatialValidationInputHash[names.Count];
            for (int index = 0; index < names.Count; index++) values[index] =
                new SpatialValidationInputHash(names[index], SpatialContractSha256.Compute(validationInputs[names[index]]));
            return values;
        }

        private string ValidateDependencies()
        {
            if (limits == null || !limits.Raw.IsValid || !limits.Canonical.IsValid ||
                !limits.Whole.IsValid || !rawVersions.IsValid || blankFloor == null ||
                !blankFloor.IsValid) return "gd66.profile.invalid";
            if (compatibility == null || production == null || legacyConfiguration == null ||
                validationInputs == null) return "gd66.transaction.pinned_input_missing";
            try
            {
                return LegacyGameplayConfigurationContract.Parse(legacyConfiguration) == null
                    ? "gd66.transaction.pinned_input_missing" : null;
            }
            catch { return "gd66.profile.invalid"; }
        }

        private sealed class DescriptorSeedResult
        {
            private DescriptorSeedResult(SpatialMigrationInputDescriptor descriptor, string reason)
            { Descriptor = descriptor; Reason = reason; }
            internal SpatialMigrationInputDescriptor Descriptor { get; }
            internal string Reason { get; }
            internal bool IsSuccess => Descriptor != null;
            internal static DescriptorSeedResult Success(SpatialMigrationInputDescriptor descriptor) =>
                new DescriptorSeedResult(descriptor, null);
            internal static DescriptorSeedResult Failure(string reason) =>
                new DescriptorSeedResult(null, reason);
        }

        private static DetachedSpatialSaveLoadResult Failure(string reason,
            SpatialTrustedPayload trusted = SpatialTrustedPayload.None,
            DetachedSpatialMigrationOutcome recovery = null,
            DetachedSpatialMigrationOutcome transaction = null,
            IEnumerable<string> diagnostics = null) =>
            new DetachedSpatialSaveLoadResult(false, reason, trusted,
                DetachedSpatialSaveLoadDisposition.None, null, null, null, null,
                recovery, transaction, diagnostics);
    }
}
