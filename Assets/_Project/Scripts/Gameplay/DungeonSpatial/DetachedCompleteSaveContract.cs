using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCurrentTargetValidationContext
    {
        private readonly byte[] legacyConfiguration;
        public DetachedCurrentTargetValidationContext(SpatialLayoutCompatibilitySnapshot compatibility,
            ProductionSpatialContentSnapshot production,
            byte[] legacyConfiguration,
            CanonicalSpatialSerializationLimits limits,
            RoomContentSpatialOccupancySnapshot roomContentOccupancy = null,
            bool allowEditorTestOccupancyFallback = true,
            IReadOnlyList<LivingDungeonProfile> livingDungeonProfiles = null,
            MonsterRecoveryRules monsterRecovery = null)
        { Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
          Production = production ?? throw new ArgumentNullException(nameof(production));
          RoomContentOccupancy = roomContentOccupancy;
#if UNITY_EDITOR
          if (RoomContentOccupancy == null && allowEditorTestOccupancyFallback)
          {
              RunSimulationConfig testConfiguration;
              try { testConfiguration = LegacyGameplayConfigurationContract.Parse(legacyConfiguration); }
              catch { testConfiguration = null; }
              RoomContentOccupancy =
                  RoomContentSpatialOccupancyAuthority.CreateEditorTestSnapshot(testConfiguration);
          }
#endif
          this.legacyConfiguration = legacyConfiguration == null ? null : (byte[])legacyConfiguration.Clone();
          if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); Limits = limits;
          LivingDungeonProfiles = livingDungeonProfiles?.ToArray();
          MonsterRecovery = monsterRecovery ?? MonsterRecoveryRules.EditorFallback(); }
        internal MonsterRecoveryRules MonsterRecovery { get; }
        internal RecoveryContextIdentity RecoveryContext { get; private set; }
        internal RecoveryRuntimeInputs RecoveryInputs { get; private set; }
        internal void BindRecoveryInputs(RecoveryRuntimeInputs inputs, IHistoricalRecoveryEvidence evidence = null)
        { BindRecoveryContext(inputs?.Identity, evidence); RecoveryInputs = inputs; }
        internal IHistoricalRecoveryEvidence HistoricalRecoveryEvidence { get; private set; }
        internal void BindRecoveryContext(RecoveryContextIdentity inputs, IHistoricalRecoveryEvidence evidence = null)
        {
            if (RecoveryContext != null || inputs == null ||
                !BootstrapConfigValidationService.TryParseRunSimulationConfig(new System.Text.UTF8Encoding(false, true).GetString(inputs.GetInput(0)), out var full) ||
                !LegacyGameplayConfigurationContract.SerializeCanonical(full).AsSpan().SequenceEqual(legacyConfiguration) ||
                !MonsterRecovery.SameRules(MonsterRecoveryRules.CaptureBound(full, inputs.GetInput(3))) ||
                !ProductionSpatialGeneratedSetParser.SerializeCanonical(Production.Manifest).AsSpan().SequenceEqual(inputs.GetInput(4)) ||
                !ProductionSpatialGeneratedSetParser.SerializeCanonical(Production.Catalog).AsSpan().SequenceEqual(inputs.GetInput(5)))
                throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            RecoveryContext = inputs; HistoricalRecoveryEvidence = evidence;
        }
        internal IReadOnlyList<LivingDungeonProfile> LivingDungeonProfiles { get; }
        internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
        internal ProductionSpatialContentSnapshot Production { get; }
        internal RoomContentSpatialOccupancySnapshot RoomContentOccupancy { get; }
        public CanonicalSpatialSerializationLimits Limits { get; }
        internal RunSimulationConfig Configuration
        { get { try { return LegacyGameplayConfigurationContract.Parse(legacyConfiguration); }
                catch { return null; } } }
    }

    public sealed class DetachedUnfinishedAttemptValidationContext
    {
        private readonly byte[] legacyConfiguration;
        private readonly Dictionary<string, byte[]> validationInputs;
        public DetachedUnfinishedAttemptValidationContext(SpatialMigrationInputDescriptor descriptor,
            string transactionId, string descriptorFingerprint, string expectedCandidateSha256,
            CanonicalLayoutContractSelection selectedContract,
            SpatialMigrationCompatibilityProfile profile, CompatibilityLayoutGeometryRecord geometry,
            ProductionSpatialContentSnapshot production, byte[] legacyConfiguration,
            IReadOnlyDictionary<string, byte[]> validationInputs,
            CanonicalSpatialSerializationLimits limits)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            TransactionId = transactionId; DescriptorFingerprint = descriptorFingerprint;
            ExpectedCandidateSha256 = expectedCandidateSha256;
            SelectedContract = selectedContract ?? throw new ArgumentNullException(nameof(selectedContract));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Production = production ?? throw new ArgumentNullException(nameof(production));
            this.legacyConfiguration = legacyConfiguration == null ? null : (byte[])legacyConfiguration.Clone();
            this.validationInputs = validationInputs == null ? null : validationInputs.ToDictionary(
                pair => pair.Key, pair => pair.Value == null ? null : (byte[])pair.Value.Clone(),
                StringComparer.Ordinal);
            if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); Limits = limits;
        }
        public SpatialMigrationInputDescriptor Descriptor { get; }
        public string TransactionId { get; }
        public string DescriptorFingerprint { get; }
        public string ExpectedCandidateSha256 { get; }
        internal SpatialMigrationCompatibilityProfile Profile { get; }
        internal CanonicalLayoutContractSelection SelectedContract { get; }
        internal CompatibilityLayoutGeometryRecord Geometry { get; }
        internal ProductionSpatialContentSnapshot Production { get; }
        public CanonicalSpatialSerializationLimits Limits { get; }
        internal RunSimulationConfig Configuration
        { get { try { return LegacyGameplayConfigurationContract.Parse(legacyConfiguration); }
                catch { return null; } } }

        internal bool PinsAreValid()
        {
            if (!SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(TransactionId) ||
                !SpatialContractSha256.IsCanonical(DescriptorFingerprint) ||
                !SpatialContractSha256.IsCanonical(ExpectedCandidateSha256) ||
                SpatialMigrationDescriptorContracts.ComputeInputFingerprint(Descriptor,
                    Limits.Serialized) != DescriptorFingerprint ||
                SpatialMigrationTransactionIdentity.CreateTransactionId(
                    SpatialMigrationTransactionIdentity.ComputeIdentity(
                        Descriptor.OriginalPayloadSha256, DescriptorFingerprint)) != TransactionId ||
                Descriptor.SelectedTargetSchemaVersion != DetachedWholeSaveCandidateSerializer.TargetSchemaVersion ||
                Descriptor.CanonicalSerializerId != SpatialMigrationContractIdentity.CanonicalSerializerId ||
                Descriptor.CanonicalSerializerVersion != SpatialMigrationContractIdentity.CanonicalSerializerVersion ||
                Descriptor.AuthorityMarkerContractVersion != SpatialMigrationContractIdentity.AuthorityMarkerContractVersion ||
                Descriptor.MigrationContractVersion != SpatialMigrationContractIdentity.MigrationContractVersion ||
                Profile.ProfileId != Descriptor.MigrationProfileId ||
                Profile.ProfileVersion != Descriptor.MigrationProfileVersion ||
                (Profile.Lifecycle != CompatibilityProfileLifecycle.Active &&
                    Profile.Lifecycle != CompatibilityProfileLifecycle.Retired) ||
                Profile.CanonicalHash != Descriptor.MigrationProfileCanonicalHash ||
                SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(Profile) !=
                    Descriptor.MigrationProfileCanonicalHash ||
                Descriptor.RawSourceSchemaVersion < Profile.MinimumSourceSchemaVersion ||
                Descriptor.RawSourceSchemaVersion > Profile.MaximumSourceSchemaVersion ||
                Profile.TargetSchemaVersion != Descriptor.SelectedTargetSchemaVersion ||
                SelectedContract.Lifecycle != CompatibilityProfileLifecycle.Active ||
                SelectedContract.TargetSchemaVersion != Descriptor.SelectedTargetSchemaVersion ||
                Profile.TargetCanonicalLayoutContractVersion !=
                    SelectedContract.CanonicalLayoutContractVersion ||
                Geometry.GeometryId != Descriptor.SharedGeometryId ||
                Geometry.GeometryVersion != Descriptor.SharedGeometryVersion ||
                Geometry.CanonicalHash != Descriptor.SharedGeometryCanonicalHash ||
                SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(Geometry) !=
                    Descriptor.SharedGeometryCanonicalHash ||
                SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                    Production.Manifest)) != Descriptor.ProductionManifestSha256 ||
                SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(
                    Production.Catalog)) != Descriptor.ProductionCatalogSha256 ||
                legacyConfiguration == null || SpatialContractSha256.Compute(legacyConfiguration) !=
                    Descriptor.LegacyGameplayConfigurationSha256) return false;
            return DetachedRequiredValidationInputSpecification.Current.Validate(validationInputs,
                Descriptor.ValidationInputHashes) == null;
        }
    }

    public sealed class DetachedCompleteSaveValidationResult
    {
        internal DetachedCompleteSaveValidationResult(byte[] bytes, string reason,
            int? layoutContractVersion = null, DetachedCanonicalSpatialSaveState state = null,
            bool currentTargetValidated = false, bool researchPendingExplicitNull = false,
            bool researchProgressExplicitNull = false, bool lastOfflineSummaryExplicitNull = false,
            CorridorContentAuthority corridorContent = null,
            SharedBranchKnowledgeAuthority branchKnowledge = null,
            SharedFloorKnowledgeAuthority floorKnowledge = null)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); Reason = reason;
          LayoutContractVersion = layoutContractVersion; State = state;
          CurrentTargetValidated = currentTargetValidated;
          ResearchPendingExplicitNull = researchPendingExplicitNull;
          ResearchProgressExplicitNull = researchProgressExplicitNull;
          LastOfflineSummaryExplicitNull = lastOfflineSummaryExplicitNull;
          CorridorContent = corridorContent; BranchKnowledge = branchKnowledge;
          FloorKnowledge = floorKnowledge; }
        public bool IsValid => Bytes != null;
        public byte[] GetBytes() => Bytes == null ? null : (byte[])Bytes.Clone();
        public string Reason { get; }
        private byte[] Bytes { get; }
        internal int? LayoutContractVersion { get; }
        internal DetachedCanonicalSpatialSaveState State { get; }
        internal bool CurrentTargetValidated { get; }
        internal bool ResearchPendingExplicitNull { get; }
        internal bool ResearchProgressExplicitNull { get; }
        internal bool LastOfflineSummaryExplicitNull { get; }
        internal CorridorContentAuthority CorridorContent { get; }
        internal SharedBranchKnowledgeAuthority BranchKnowledge { get; }
        internal SharedFloorKnowledgeAuthority FloorKnowledge { get; }
        internal StructuralInvestmentRecord[] Investment { get; set; }
        internal RoomContentSpatialOccupancySnapshot RoomContentOccupancy { get; set; }
        internal LivingDungeonState LivingDungeon { get; set; }
        internal MonsterContinuityState MonsterContinuity { get; set; }
        internal TimeProvenance TimeProvenance { get; set; }
    }

    public static class DetachedCompleteSaveContract
    {
        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            DetachedCurrentTargetValidationContext context) =>
            ParseValidateContextAndRoundTrip(bytes, context, CanonicalSaveSchemaVersions.CurrentWritableTarget, true);

        // Historical context validation proves production semantics, but grants no writable session authority.
        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaFifteenAndRoundTrip(
            byte[] bytes, DetachedCurrentTargetValidationContext context) =>
            ParseValidateContextAndRoundTrip(bytes, context, CanonicalSaveSchemaVersions.FrozenSchemaFifteen, false);

        // Frozen source validation is never a writable target/session capability.
        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaSixteenAndRoundTrip(
            byte[] bytes, DetachedCurrentTargetValidationContext context) =>
            ParseValidateContextAndRoundTrip(bytes, context, CanonicalSaveSchemaVersions.FrozenSchemaSixteen, false);

        private static DetachedCompleteSaveValidationResult ParseValidateContextAndRoundTrip(byte[] bytes,
            DetachedCurrentTargetValidationContext context, int schemaVersion, bool currentTarget)
        {
            using var diagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.CompleteValidation);
            if (context == null) return Failure();
            var limits = context.Limits.ForSchema(schemaVersion);
            DetachedCompleteSaveValidationResult result = ParseValidateAndRoundTripCore(bytes,
                limits, schemaVersion, true,
                null, null, context.LivingDungeonProfiles);
            if (!result.IsValid || !result.LayoutContractVersion.HasValue) return Failure();
            if (schemaVersion == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction &&
                (context.RecoveryContext?.Matches(result.TimeProvenance) != true ||
                 result.TimeProvenance.Episode?.Phase == RecoveryEpisode.ResidualPending && !result.LivingDungeon.IsClosed)) return Failure();
            if (schemaVersion == CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction && context.RecoveryInputs != null &&
                !ResearchStateValid(result, context.RecoveryInputs)) return Failure();
            if (!MonsterContinuity.Valid(result.MonsterContinuity, result.State, context.MonsterRecovery, limits.Spatial.MaximumRecords)) return Failure();
            if (!DetachedCanonicalProductionSemanticValidation.Validate(result.State, context.Production,
                context.Configuration, limits.Spatial, context.RoomContentOccupancy, true).IsValid)
                return Failure();
            if (!PhaseFiveSaveContracts.Validate(result.CorridorContent, result.BranchKnowledge,
                    result.State, context.Production, context.Configuration, limits.Spatial))
                return Failure();
            if (!PhaseSixFloorKnowledge.Validate(result.FloorKnowledge, result.State,
                    DungeonBuilder.M0.Gameplay.RunSimulation.PhaseSixRunConfigValidation.MaximumSupportedActiveFloors)) return Failure();
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> selected =
                context.Compatibility.SelectContract(schemaVersion);
            return selected.Success && selected.Value.CanonicalLayoutContractVersion ==
                result.LayoutContractVersion.Value
                ? new DetachedCompleteSaveValidationResult(result.GetBytes(), null,
                    result.LayoutContractVersion, result.State,
                    currentTarget && schemaVersion == CanonicalSaveSchemaVersions.CurrentWritableTarget,
                    result.ResearchPendingExplicitNull, result.ResearchProgressExplicitNull,
                    result.LastOfflineSummaryExplicitNull, result.CorridorContent,
                    result.BranchKnowledge, result.FloorKnowledge) { Investment = result.Investment,
                        RoomContentOccupancy = context.RoomContentOccupancy, LivingDungeon = result.LivingDungeon, MonsterContinuity = result.MonsterContinuity,
                        TimeProvenance = result.TimeProvenance }
                : Failure();
        }

        internal static bool ResearchStateValid(DetachedCompleteSaveValidationResult result, RecoveryRuntimeInputs inputs)
        {
            try {
                var save = UnityEngine.JsonUtility.FromJson<SaveRoot>(System.Text.Encoding.UTF8.GetString(result.GetBytes())).primary;
                if (result.ResearchPendingExplicitNull) save.researchPending = null;
                if (result.ResearchProgressExplicitNull) save.researchProgress = null;
                return inputs.ResearchValid(save);
            } catch { return false; }
        }

        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            DetachedUnfinishedAttemptValidationContext context)
        {
            if (context == null || !context.PinsAreValid() || bytes == null ||
                !string.Equals(SpatialContractSha256.Compute(bytes), context.ExpectedCandidateSha256,
                    StringComparison.Ordinal)) return Failure();
            DetachedCompleteSaveValidationResult result = ParseValidateFrozenSchemaSevenAndRoundTrip(
                bytes, context.Limits, context.TransactionId, context.DescriptorFingerprint);
            if (result.IsValid && !DetachedCanonicalProductionSemanticValidation.Validate(result.State,
                context.Production, context.Configuration, context.Limits.Spatial).IsValid) return Failure();
            return result.IsValid && result.LayoutContractVersion ==
                context.SelectedContract.CanonicalLayoutContractVersion &&
                ContextSemanticsValid(result.State, context) ? result : Failure();
        }

        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            CanonicalSpatialSerializationLimits limits, ProductionSpatialContentSnapshot production = null,
            string expectedTransactionId = null, string expectedDescriptorFingerprint = null)
        {
            return ParseValidateAndRoundTripCore(bytes, limits,
                CanonicalSaveSchemaVersions.CurrentWritableTarget, true,
                expectedTransactionId, expectedDescriptorFingerprint);
        }

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaSevenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits,
            string expectedTransactionId = null, string expectedDescriptorFingerprint = null)
        {
            return ParseValidateAndRoundTripCore(bytes, limits,
                CanonicalSaveSchemaVersions.FrozenLegacyCanonicalMigrationTarget, false,
                expectedTransactionId, expectedDescriptorFingerprint);
        }

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaEightAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 8, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaNineAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 9, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaTenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 10, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaElevenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 11, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaTwelveAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 12, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaThirteenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits) =>
            ParseValidateAndRoundTripCore(bytes, limits, 13, true, null, null);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaFourteenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits, IReadOnlyList<LivingDungeonProfile> profiles = null) =>
            ParseValidateAndRoundTripCore(bytes, limits, 14, true, null, null, profiles);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaFifteenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits, IReadOnlyList<LivingDungeonProfile> profiles = null) =>
            ParseValidateAndRoundTripCore(bytes, limits, CanonicalSaveSchemaVersions.FrozenSchemaFifteen,
                true, null, null, profiles);

        internal static DetachedCompleteSaveValidationResult ParseValidateFrozenSchemaSixteenAndRoundTrip(
            byte[] bytes, CanonicalSpatialSerializationLimits limits, IReadOnlyList<LivingDungeonProfile> profiles = null) =>
            ParseValidateAndRoundTripCore(bytes, limits, CanonicalSaveSchemaVersions.FrozenSchemaSixteen,
                true, null, null, profiles);

        private static DetachedCompleteSaveValidationResult ParseValidateAndRoundTripCore(byte[] bytes,
            CanonicalSpatialSerializationLimits limits, int schemaVersion, bool requireLifecycle,
            string expectedTransactionId, string expectedDescriptorFingerprint,
            IReadOnlyList<LivingDungeonProfile> livingDungeonProfiles = null)
        {
            limits = limits.ForSchema(schemaVersion);
            if (bytes == null || !limits.IsValid) return Failure();
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root) ||
                    root.Kind != ContractJsonKind.Object || root.Fields.Count < 3 ||
                    !Field(root, 0, "schema", ContractJsonKind.String) || root.Fields[0].Value.Text != "save_root" ||
                    !Field(root, 1, "schemaVersion", ContractJsonKind.Number) ||
                    root.Fields[1].Value.Text != schemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) ||
                    !Field(root, 2, "primary", ContractJsonKind.Object)) return Failure();
                if (HasCaseAmbiguousSibling(root) || CaseAmbiguous(root,
                    new[] { "schema", "schemaVersion", "primary" })) return Failure();
                ContractJsonNode primary = root.Fields[2].Value;
                bool requireInvestment = schemaVersion >= 9;
                bool requirePhaseFive = schemaVersion >= 10;
                bool requireFloorKnowledge = schemaVersion >= CanonicalSaveSchemaVersions.FloorKnowledgeIntroduction;
                bool requireLivingDungeon = schemaVersion >= CanonicalSaveSchemaVersions.LivingDungeonIntroduction;
                bool requireMonsters = schemaVersion >= CanonicalSaveSchemaVersions.MonsterContinuityIntroduction;
                bool requireTime = schemaVersion >= CanonicalSaveSchemaVersions.TimeProvenanceIntroduction;
                int canonicalMembers = requireTime ? 10 : requireMonsters ? 9 : requireLivingDungeon ? 8 : requireFloorKnowledge ? 7 : requirePhaseFive ? 6 : requireInvestment ? 4 : requireLifecycle ? 3 : 2;
                int canonicalStart = primary.Fields.Count - canonicalMembers;
                if (primary.Fields.Count < canonicalMembers ||
                    primary.Fields[canonicalStart].Key != "canonicalSpatialAuthority" ||
                    primary.Fields[canonicalStart + 1].Key != "spatialFloors" ||
                    (requireInvestment && primary.Fields[canonicalStart + 3].Key != "structuralInvestment") ||
                    (requirePhaseFive && (primary.Fields[canonicalStart + 4].Key != PhaseFiveSaveContracts.CorridorOwnerName ||
                        primary.Fields[canonicalStart + 5].Key != PhaseFiveSaveContracts.KnowledgeOwnerName)) ||
                    (requireFloorKnowledge && primary.Fields[canonicalStart + 6].Key != PhaseSixFloorKnowledge.OwnerName) ||
                    (requireLivingDungeon && primary.Fields[canonicalStart + 7].Key != LivingDungeonContracts.OwnerName) ||
                    (requireMonsters && primary.Fields[canonicalStart + 8].Key != MonsterContinuity.Owner) ||
                    (requireTime && primary.Fields[canonicalStart + 9].Key != TimeProvenance.Owner) ||
                    (requireLifecycle && primary.Fields[canonicalStart + 2].Key !=
                        "structuralLifecycleAndOwnership") || CaseAmbiguous(primary,
                        requireFloorKnowledge ? new[] { "canonicalSpatialAuthority", "spatialFloors", "structuralLifecycleAndOwnership", "structuralInvestment", PhaseFiveSaveContracts.CorridorOwnerName, PhaseFiveSaveContracts.KnowledgeOwnerName, PhaseSixFloorKnowledge.OwnerName } :
                        requirePhaseFive ? new[] { "canonicalSpatialAuthority", "spatialFloors", "structuralLifecycleAndOwnership", "structuralInvestment", PhaseFiveSaveContracts.CorridorOwnerName, PhaseFiveSaveContracts.KnowledgeOwnerName } :
                        requireInvestment ? new[] { "canonicalSpatialAuthority", "spatialFloors", "structuralLifecycleAndOwnership", "structuralInvestment" } :
                        requireLifecycle ? new[] { "canonicalSpatialAuthority", "spatialFloors",
                            "structuralLifecycleAndOwnership" } :
                            new[] { "canonicalSpatialAuthority", "spatialFloors" })) return Failure();
                if (!PrimaryOrderIsCanonical(primary, canonicalMembers)) return Failure();
                if (requireLivingDungeon && primary.Fields.Take(canonicalStart).Any(f =>
                    string.Equals(f.Key, LivingDungeonContracts.OwnerName, StringComparison.OrdinalIgnoreCase))) return Failure();
                if (requireMonsters && primary.Fields.Take(canonicalStart).Any(f =>
                    string.Equals(f.Key, MonsterContinuity.Owner, StringComparison.OrdinalIgnoreCase))) return Failure();
                TimeProvenance timeProvenance = null;
                if (requireTime && (root.Fields.Any(f => string.Equals(f.Key, TimeProvenance.Owner, StringComparison.OrdinalIgnoreCase)) ||
                    primary.Fields.Take(canonicalStart + 9).Any(f => string.Equals(f.Key, TimeProvenance.Owner, StringComparison.OrdinalIgnoreCase)) ||
                    !TimeProvenance.TryRead(primary, schemaVersion, out timeProvenance))) return Failure();

                var spatialWriter = new ContractJsonWriter(limits.Serialized);
                spatialWriter.Node(); spatialWriter.Token("{"); spatialWriter.String("Authority"); spatialWriter.Token(":");
                WriteNode(spatialWriter, primary.Fields[canonicalStart].Value);
                spatialWriter.Token(","); spatialWriter.String("Floors"); spatialWriter.Token(":");
                ContractJsonNode floorsNode = primary.Fields[canonicalStart + 1].Value;
                WriteNode(spatialWriter, floorsNode);
                spatialWriter.Token(","); spatialWriter.String("LifecycleAndOwnership"); spatialWriter.Token(":");
                if (requireLifecycle) WriteNode(spatialWriter, primary.Fields[canonicalStart + 2].Value);
                else spatialWriter.Token("null");
                spatialWriter.Token("}");
                SpatialContractResult<DetachedCanonicalSpatialSaveState> parsedSpatial =
                    !requireLifecycle ? CanonicalSpatialSaveSerializer.ParseFrozenSchemaSeven(
                        spatialWriter.Finish(), limits) :
                    schemaVersion != CanonicalSaveSchemaVersions.FrozenSchemaFifteen &&
                    schemaVersion == CanonicalSaveSchemaVersions.CurrentWritableTarget
                        ? CanonicalSpatialSaveSerializer.Parse(spatialWriter.Finish(), limits) :
                    CanonicalSpatialSaveSerializer.ParseFrozenWithLifecycle(
                        spatialWriter.Finish(), limits, schemaVersion);
                CanonicalSpatialSaveValidationResult structuralValidation = !parsedSpatial.IsValid ? null :
                    !requireLifecycle ? CanonicalSpatialSaveContracts.ValidateFrozenSchemaSeven(
                        parsedSpatial.Value, limits.Spatial, true) :
                    schemaVersion != CanonicalSaveSchemaVersions.FrozenSchemaFifteen &&
                    schemaVersion == CanonicalSaveSchemaVersions.CurrentWritableTarget
                        ? CanonicalSpatialSaveContracts.Validate(parsedSpatial.Value,
                        limits.Spatial, true) : CanonicalSpatialSaveContracts.ValidateFrozenWithLifecycle(
                        parsedSpatial.Value, limits.Spatial, schemaVersion, true);
                if (!parsedSpatial.IsValid || structuralValidation == null || !structuralValidation.IsValid ||
                    (expectedTransactionId != null && parsedSpatial.Value.Authority.MigrationTransactionId != expectedTransactionId) ||
                    (expectedDescriptorFingerprint != null &&
                        parsedSpatial.Value.Authority.MigrationDescriptorFingerprint != expectedDescriptorFingerprint)) return Failure();

                StructuralInvestmentRecord[] investment = null;
                if (requireInvestment && !StructuralInvestment.TryRead(primary.Fields[canonicalStart + 3].Value,
                    parsedSpatial.Value, limits.Spatial.MaximumRecords, out investment)) return Failure();
                if (requireInvestment)
                {
                    var originalLedger = new ContractJsonWriter(limits.Serialized);
                    WriteNode(originalLedger, primary.Fields[canonicalStart + 3].Value);
                    var canonicalLedger = new ContractJsonWriter(limits.Serialized);
                    StructuralInvestment.Write(canonicalLedger, investment);
                    if (!Same(originalLedger.Finish(), canonicalLedger.Finish())) return Failure();
                }
                CorridorContentAuthority corridorContent = null;
                SharedBranchKnowledgeAuthority branchKnowledge = null;
                if (requirePhaseFive &&
                    (!PhaseFiveSaveContracts.TryReadCorridor(primary.Fields[canonicalStart + 4].Value,
                         out corridorContent) ||
                     !PhaseFiveSaveContracts.TryReadKnowledge(primary.Fields[canonicalStart + 5].Value,
                         out branchKnowledge))) return Failure();
                SharedFloorKnowledgeAuthority floorKnowledge = null;
                if (requireFloorKnowledge && !PhaseSixFloorKnowledge.TryRead(
                    primary.Fields[canonicalStart + 6].Value, out floorKnowledge)) return Failure();
                if (requireFloorKnowledge && !PhaseSixFloorKnowledge.Validate(floorKnowledge,
                    parsedSpatial.Value,
                    DungeonBuilder.M0.Gameplay.RunSimulation.PhaseSixRunConfigValidation.MaximumSupportedActiveFloors))
                    return Failure();
                LivingDungeonState livingDungeon = null;
                if (requireLivingDungeon && !LivingDungeonContracts.TryRead(primary.Fields[canonicalStart + 7].Value,
                    livingDungeonProfiles, out livingDungeon)) return Failure();
                MonsterContinuityState monsters = null;
                if (requireMonsters && !MonsterContinuity.TryRead(primary.Fields[canonicalStart + 8].Value,
                    limits.Spatial.MaximumRecords, out monsters)) return Failure();
                if (requireMonsters && !MonsterContinuity.IdentitiesValid(monsters, parsedSpatial.Value)) return Failure();
                if (requireLivingDungeon && !livingDungeon.IsDormant)
                {
                    var historyNode = primary.Fields.SingleOrDefault(f => f.Key == "runHistory").Value;
                    if (!LivingDungeonScheduling.HistorySequence(historyNode, out int historySequence)) return Failure();
                    var historyWriter = new ContractJsonWriter(limits.Serialized); WriteNode(historyWriter, historyNode);
                    var history = UnityEngine.JsonUtility.FromJson<RunHistoryState>(System.Text.Encoding.UTF8.GetString(historyWriter.Finish()));
                    if (history.NextRunSequence != historySequence || !LivingDungeonScheduling.AllocatorValid(history, livingDungeon, false)) return Failure();
                    var profile = LivingDungeonContracts.Profile(livingDungeon, livingDungeonProfiles);
                    if (!profile.BaseBudget.IsValid || (long)profile.MaximumOwnerBytes + profile.MaximumBaseSaveBytes > limits.Serialized.MaximumInputBytes)
                        return Failure();
                    var ownerWriter = new ContractJsonWriter(limits.Serialized); LivingDungeonContracts.Write(ownerWriter, livingDungeon);
                    int ownerBytes = ownerWriter.Finish().Length + ContractJson.Bytes(",\"" + LivingDungeonContracts.OwnerName + "\":").Length;
                    int monsterBytes = 0;
                    if (requireMonsters)
                    {
                        var monsterWriter = new ContractJsonWriter(limits.Serialized); MonsterContinuity.Write(monsterWriter, monsters);
                        monsterBytes = monsterWriter.Finish().Length + ContractJson.Bytes(",\"" + MonsterContinuity.Owner + "\":").Length;
                    }
                    int timeBytes = requireTime ? TimeProvenance.LegacyMemberByteCount : 0;
                    if (schemaVersion >= CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction) {
                        var timeWriter = new ContractJsonWriter(limits.Serialized); timeProvenance.Write(timeWriter);
                        timeBytes = timeWriter.Finish().Length + ContractJson.Bytes(",\"" + TimeProvenance.Owner + "\":").Length;
                    }
                    if (ownerBytes > profile.MaximumOwnerBytes || bytes.Length - ownerBytes - monsterBytes - timeBytes > profile.MaximumBaseSaveBytes) return Failure();
                    var baseWriter = new ContractJsonWriter(profile.BaseBudget); baseWriter.Node(); baseWriter.Token("{");
                    for (int i = 0; i < root.Fields.Count; i++)
                    {
                        if (i != 0) baseWriter.Token(","); var field = root.Fields[i]; baseWriter.String(field.Key); baseWriter.Token(":");
                        if (field.Key == "schemaVersion") baseWriter.Token("13");
                        else if (field.Key == "primary")
                        {
                            baseWriter.Node(); baseWriter.Token("{");
                            for (int j = 0; j < canonicalStart + 7; j++)
                            {
                                if (j != 0) baseWriter.Token(","); baseWriter.String(primary.Fields[j].Key); baseWriter.Token(":");
                                WriteNode(baseWriter, primary.Fields[j].Value);
                            }
                            baseWriter.Token("}");
                        }
                        else WriteNode(baseWriter, field.Value);
                    }
                    baseWriter.Token("}");
                    if (!ParseValidateFrozenSchemaThirteenAndRoundTrip(baseWriter.Finish(),
                        new CanonicalSpatialSerializationLimits(profile.BaseBudget, limits.Spatial)).IsValid) return Failure();
                }
                var completeWriter = new ContractJsonWriter(limits.Serialized);
                WriteNode(completeWriter, root); byte[] again = completeWriter.Finish();
                if (!Same(bytes, again)) return Failure();
                return new DetachedCompleteSaveValidationResult(bytes, null,
                    parsedSpatial.Value.Authority.CanonicalLayoutContractVersion, parsedSpatial.Value,
                    false, ExplicitNull(primary, "researchPending"),
                    ExplicitNull(primary, "researchProgress"),
                    ExplicitNull(primary, "lastOfflineSummary"), corridorContent,
                        branchKnowledge, floorKnowledge) { Investment = investment, LivingDungeon = livingDungeon, MonsterContinuity = monsters,
                            TimeProvenance = timeProvenance };
            }
            catch { return Failure(); }
        }

        private static bool ContextSemanticsValid(DetachedCanonicalSpatialSaveState state,
            DetachedUnfinishedAttemptValidationContext context)
        {
            if (state?.Floors == null || state.Floors.Length == 0) return state?.Floors?.Length == 0;
            if (state.Floors.Length != 1) return false;
            const string floorId = "compat.floor.00";
            SavedSpatialFloor floor = state.Floors[0];
            if (floor == null || floor.Layout == null || floor.FloorInstanceId != floorId ||
                floor.Layout.FloorId != floorId || floor.FloorDefinitionId != context.Geometry.FloorDefinitionId ||
                floor.FloorIndex != context.Geometry.FloorIndex) return false;
            RoomSpatialInstance[] rooms = floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            CompatibilityLayoutVariant[] variants = (context.Geometry.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
                .Where(value => value != null && (value.Placements ?? Array.Empty<CompatibilityLayoutPlacement>())
                    .Count(placement => placement != null &&
                    (placement.Role == CompatibilityRouteRole.BasicRoom0 ||
                     placement.Role == CompatibilityRouteRole.BasicRoom1)) == rooms.Length).ToArray();
            if (variants.Length != 1) return false;
            CompatibilityLayoutVariant variant = variants[0];
            CompatibilityLayoutPlacement[] placements = variant.Placements ?? Array.Empty<CompatibilityLayoutPlacement>();
            SavedFixedSpatialStructure[] fixedValues = floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>();
            FloorRouteNode[] nodes = floor.Layout.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteEdge[] edges = floor.Layout.Edges ?? Array.Empty<FloorRouteEdge>();
            if (rooms.Length != placements.Count(IsRoom) || fixedValues.Length != 2 ||
                nodes.Length != placements.Length || edges.Length != (variant.Connections?.Length ?? 0)) return false;
            foreach (CompatibilityLayoutPlacement placement in placements)
            {
                string role = Role(placement.Role);
                string nodeId = floorId + ".node." + role;
                bool roomRole = IsRoom(placement);
                if (roomRole)
                {
                    int index = placement.Role == CompatibilityRouteRole.BasicRoom0 ? 0 : 1;
                    string roomId = floorId + ".legacy-room." + index.ToString("D2");
                    if (!ExactlyOne(rooms, value => value != null && value.RoomInstanceId == roomId,
                        out RoomSpatialInstance room) || room.FloorId != floorId ||
                        room.RoomDefinitionId != context.Geometry.BasicRoomDefinitionId ||
                        !room.Anchor.Equals(placement.Anchor) || room.Orientation != placement.Orientation) return false;
                    if (!ExactlyOne(nodes, value => value != null && value.NodeId == nodeId,
                        out FloorRouteNode node) || node.FloorId != floorId || node.Kind != FloorRouteNodeKind.Room ||
                        node.RoomInstanceId != roomId) return false;
                }
                else
                {
                    FixedSpatialStructureKind kind = placement.Role == CompatibilityRouteRole.Entrance ?
                        FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
                    string definition = placement.Role == CompatibilityRouteRole.Entrance ?
                        context.Geometry.EntranceStructureDefinitionId : context.Geometry.CompletionStructureDefinitionId;
                    string fixedId = floorId + ".fixed." + role;
                    if (!ExactlyOne(fixedValues, value => value != null && value.FixedStructureInstanceId == fixedId,
                        out SavedFixedSpatialStructure fixedValue) || fixedValue.Kind != kind ||
                        fixedValue.FloorInstanceId != floorId || fixedValue.FixedStructureDefinitionId != definition ||
                        !fixedValue.Anchor.Equals(placement.Anchor) || fixedValue.Orientation != placement.Orientation) return false;
                    FloorRouteNodeKind nodeKind = placement.Role == CompatibilityRouteRole.Entrance ?
                        FloorRouteNodeKind.Entrance : FloorRouteNodeKind.Completion;
                    if (!ExactlyOne(nodes, value => value != null && value.NodeId == nodeId,
                        out FloorRouteNode node) || node.FloorId != floorId || node.Kind != nodeKind ||
                        !string.IsNullOrEmpty(node.RoomInstanceId)) return false;
                }
            }
            foreach (CompatibilityLayoutConnection connection in variant.Connections ??
                Array.Empty<CompatibilityLayoutConnection>())
            {
                string source = Role(connection.SourceRole), destination = Role(connection.DestinationRole);
                string edgeId = floorId + ".edge.direct." + source + "." + destination;
                if (!ExactlyOne(edges, value => value != null && value.EdgeId == edgeId,
                    out FloorRouteEdge edge) || edge.FloorId != floorId ||
                    edge.SourceNodeId != floorId + ".node." + source ||
                    edge.DestinationNodeId != floorId + ".node." + destination ||
                    edge.Classification != RouteClassification.Required ||
                    edge.ConnectionKind != FloorRouteConnectionKind.DirectDoorway ||
                    !string.IsNullOrEmpty(edge.CorridorDefinitionId) || edge.Footprint != null ||
                    !string.IsNullOrEmpty(edge.OptionalBranchId)) return false;
            }
            return true;
        }

        private static bool IsRoom(CompatibilityLayoutPlacement value) => value != null &&
            (value.Role == CompatibilityRouteRole.BasicRoom0 || value.Role == CompatibilityRouteRole.BasicRoom1);
        private static string Role(CompatibilityRouteRole role) => role == CompatibilityRouteRole.Entrance ? "entrance" :
            role == CompatibilityRouteRole.Completion ? "completion" : role == CompatibilityRouteRole.BasicRoom0 ?
            "legacy-room.00" : "legacy-room.01";
        private static bool ExactlyOne<T>(IEnumerable<T> values, Func<T, bool> predicate, out T result)
        {
            result = default(T); int count = 0;
            foreach (T value in values ?? Enumerable.Empty<T>())
                if (predicate(value)) { result = value; if (++count > 1) return false; }
            return count == 1;
        }

        private static bool Field(ContractJsonNode node, int index, string name, ContractJsonKind kind) =>
            node.Fields[index].Key == name && node.Fields[index].Value.Kind == kind;
        private static bool ExplicitNull(ContractJsonNode primary, string name)
        {
            foreach (KeyValuePair<string, ContractJsonNode> field in primary.Fields)
                if (field.Key == name) return field.Value.Kind == ContractJsonKind.Null;
            return false;
        }
        private static bool PrimaryOrderIsCanonical(ContractJsonNode primary, int canonicalMemberCount = 3)
        {
            IReadOnlyList<string> recognized = RawSavePayloadClassifier.RecognizedSaveDataMemberNames;
            int previous = -1; bool unknownSeen = false;
            for (int index = 0; index < primary.Fields.Count - canonicalMemberCount; index++)
            {
                string name = primary.Fields[index].Key;
                int recognizedIndex = -1;
                for (int candidate = 0; candidate < recognized.Count; candidate++)
                    if (recognized[candidate] == name) { recognizedIndex = candidate; break; }
                if (recognizedIndex >= 0)
                {
                    if (unknownSeen || recognizedIndex <= previous) return false;
                    previous = recognizedIndex;
                }
                else
                {
                    foreach (string known in recognized)
                        if (string.Equals(known, name, StringComparison.OrdinalIgnoreCase)) return false;
                    unknownSeen = true;
                }
            }
            return true;
        }
        private static bool CaseAmbiguous(ContractJsonNode node, IEnumerable<string> reserved)
        {
            foreach (KeyValuePair<string, ContractJsonNode> field in node.Fields)
                foreach (string name in reserved)
                    if (field.Key != name && string.Equals(field.Key, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static bool HasCaseAmbiguousSibling(ContractJsonNode node)
        {
            if (node.Kind == ContractJsonKind.Object)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, ContractJsonNode> field in node.Fields)
                {
                    if (!names.Add(field.Key) || HasCaseAmbiguousSibling(field.Value)) return true;
                }
            }
            else if (node.Kind == ContractJsonKind.Array)
                foreach (ContractJsonNode item in node.Items)
                    if (HasCaseAmbiguousSibling(item)) return true;
            return false;
        }
        private static void WriteNode(ContractJsonWriter writer, ContractJsonNode node)
        {
            writer.Node();
            if (node.Kind == ContractJsonKind.Null) { writer.Token("null"); return; }
            if (node.Kind == ContractJsonKind.String) { writer.String(node.Text); return; }
            if (node.Kind == ContractJsonKind.Number || node.Kind == ContractJsonKind.Boolean)
            { writer.Token(node.Text); return; }
            if (node.Kind == ContractJsonKind.Array)
            {
                writer.Token("["); for (int index = 0; index < node.Items.Count; index++)
                { if (index != 0) writer.Token(","); writer.Record(); WriteNode(writer, node.Items[index]); }
                writer.Token("]"); return;
            }
            writer.Token("{"); for (int index = 0; index < node.Fields.Count; index++)
            { if (index != 0) writer.Token(","); writer.String(node.Fields[index].Key); writer.Token(":"); WriteNode(writer, node.Fields[index].Value); }
            writer.Token("}");
        }

        internal static void WriteCanonicalNode(ContractJsonWriter writer, ContractJsonNode node) =>
            WriteNode(writer, node);
        private static bool Same(byte[] left, byte[] right) => left != null && right != null && left.SequenceEqual(right);
        private static DetachedCompleteSaveValidationResult Failure() =>
            new DetachedCompleteSaveValidationResult(null, "gd66.transaction.candidate_invalid");
    }
}
