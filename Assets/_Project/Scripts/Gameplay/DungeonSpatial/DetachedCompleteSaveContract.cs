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
            CanonicalSpatialSerializationLimits limits)
        { Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
          Production = production ?? throw new ArgumentNullException(nameof(production));
          this.legacyConfiguration = legacyConfiguration == null ? null : (byte[])legacyConfiguration.Clone();
          if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); Limits = limits; }
        internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
        internal ProductionSpatialContentSnapshot Production { get; }
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
            string transactionId, string descriptorFingerprint,
            CanonicalLayoutContractSelection selectedContract,
            SpatialMigrationCompatibilityProfile profile, CompatibilityLayoutGeometryRecord geometry,
            ProductionSpatialContentSnapshot production, byte[] legacyConfiguration,
            IReadOnlyDictionary<string, byte[]> validationInputs,
            CanonicalSpatialSerializationLimits limits)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            TransactionId = transactionId; DescriptorFingerprint = descriptorFingerprint;
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
            int? layoutContractVersion = null, DetachedCanonicalSpatialSaveState state = null)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); Reason = reason;
          LayoutContractVersion = layoutContractVersion; State = state; }
        public bool IsValid => Bytes != null;
        public byte[] GetBytes() => Bytes == null ? null : (byte[])Bytes.Clone();
        public string Reason { get; }
        private byte[] Bytes { get; }
        internal int? LayoutContractVersion { get; }
        internal DetachedCanonicalSpatialSaveState State { get; }
    }

    public static class DetachedCompleteSaveContract
    {
        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            DetachedCurrentTargetValidationContext context)
        {
            if (context == null) return Failure();
            DetachedCompleteSaveValidationResult result = ParseValidateAndRoundTrip(bytes,
                context.Limits);
            if (!result.IsValid || !result.LayoutContractVersion.HasValue) return Failure();
            if (!DetachedCanonicalProductionSemanticValidation.Validate(result.State, context.Production,
                context.Configuration, context.Limits.Spatial).IsValid) return Failure();
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> selected =
                context.Compatibility.SelectContract(DetachedWholeSaveCandidateSerializer.TargetSchemaVersion);
            return selected.Success && selected.Value.CanonicalLayoutContractVersion ==
                result.LayoutContractVersion.Value ? result : Failure();
        }

        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            DetachedUnfinishedAttemptValidationContext context)
        {
            if (context == null || !context.PinsAreValid()) return Failure();
            DetachedCompleteSaveValidationResult result = ParseValidateAndRoundTrip(bytes, context.Limits,
                null, context.TransactionId, context.DescriptorFingerprint);
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
            if (bytes == null || !limits.IsValid) return Failure();
            try
            {
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root) ||
                    root.Kind != ContractJsonKind.Object || root.Fields.Count < 3 ||
                    !Field(root, 0, "schema", ContractJsonKind.String) || root.Fields[0].Value.Text != "save_root" ||
                    !Field(root, 1, "schemaVersion", ContractJsonKind.Number) || root.Fields[1].Value.Text != "7" ||
                    !Field(root, 2, "primary", ContractJsonKind.Object)) return Failure();
                if (HasCaseAmbiguousSibling(root) || CaseAmbiguous(root,
                    new[] { "schema", "schemaVersion", "primary" })) return Failure();
                ContractJsonNode primary = root.Fields[2].Value;
                if (primary.Fields.Count < 2 ||
                    primary.Fields[primary.Fields.Count - 2].Key != "canonicalSpatialAuthority" ||
                    primary.Fields[primary.Fields.Count - 1].Key != "spatialFloors" ||
                    CaseAmbiguous(primary, new[] { "canonicalSpatialAuthority", "spatialFloors" })) return Failure();
                if (!PrimaryOrderIsCanonical(primary)) return Failure();

                var spatialWriter = new ContractJsonWriter(limits.Serialized);
                spatialWriter.Node(); spatialWriter.Token("{"); spatialWriter.String("Authority"); spatialWriter.Token(":");
                WriteNode(spatialWriter, primary.Fields[primary.Fields.Count - 2].Value);
                spatialWriter.Token(","); spatialWriter.String("Floors"); spatialWriter.Token(":");
                WriteNode(spatialWriter, primary.Fields[primary.Fields.Count - 1].Value); spatialWriter.Token("}");
                SpatialContractResult<DetachedCanonicalSpatialSaveState> parsedSpatial =
                    CanonicalSpatialSaveSerializer.Parse(spatialWriter.Finish(), limits);
                if (!parsedSpatial.IsValid || !CanonicalSpatialSaveValidator.Validate(parsedSpatial.Value,
                        limits.Spatial, true).IsValid ||
                    (expectedTransactionId != null && parsedSpatial.Value.Authority.MigrationTransactionId != expectedTransactionId) ||
                    (expectedDescriptorFingerprint != null &&
                        parsedSpatial.Value.Authority.MigrationDescriptorFingerprint != expectedDescriptorFingerprint)) return Failure();

                var completeWriter = new ContractJsonWriter(limits.Serialized);
                WriteNode(completeWriter, root); byte[] again = completeWriter.Finish();
                if (!Same(bytes, again)) return Failure();
                return new DetachedCompleteSaveValidationResult(bytes, null,
                    parsedSpatial.Value.Authority.CanonicalLayoutContractVersion, parsedSpatial.Value);
            }
            catch { return Failure(); }
        }

        private static bool ContextSemanticsValid(DetachedCanonicalSpatialSaveState state,
            DetachedUnfinishedAttemptValidationContext context)
        {
            if (state?.Floors == null || state.Floors.Length == 0) return state?.Floors?.Length == 0;
            if (state.Floors.Length != 1) return false;
            SavedSpatialFloor floor = state.Floors[0];
            if (floor == null || floor.Layout == null || floor.FloorDefinitionId != context.Geometry.FloorDefinitionId ||
                floor.FloorIndex != context.Geometry.FloorIndex) return false;
            RoomSpatialInstance[] rooms = floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>();
            CompatibilityLayoutVariant variant = (context.Geometry.Layouts ?? Array.Empty<CompatibilityLayoutVariant>())
                .SingleOrDefault(value => value != null && value.Placements.Count(placement => placement != null &&
                    (placement.Role == CompatibilityRouteRole.BasicRoom0 ||
                     placement.Role == CompatibilityRouteRole.BasicRoom1)) == rooms.Length);
            if (variant == null || (floor.Layout.Edges?.Length ?? 0) != variant.Connections.Length) return false;
            foreach (CompatibilityLayoutPlacement placement in variant.Placements ??
                Array.Empty<CompatibilityLayoutPlacement>())
            {
                if (placement.Role == CompatibilityRouteRole.BasicRoom0 ||
                    placement.Role == CompatibilityRouteRole.BasicRoom1)
                {
                    int index = placement.Role == CompatibilityRouteRole.BasicRoom0 ? 0 : 1;
                    RoomSpatialInstance room = rooms.SingleOrDefault(value => value != null &&
                        value.RoomInstanceId == "compat.floor.00.legacy-room." + index.ToString("D2"));
                    if (room == null || room.RoomDefinitionId != context.Geometry.BasicRoomDefinitionId ||
                        !room.Anchor.Equals(placement.Anchor) || room.Orientation != placement.Orientation) return false;
                }
                else
                {
                    FixedSpatialStructureKind kind = placement.Role == CompatibilityRouteRole.Entrance ?
                        FixedSpatialStructureKind.Entrance : FixedSpatialStructureKind.CompletionTerminal;
                    string definition = placement.Role == CompatibilityRouteRole.Entrance ?
                        context.Geometry.EntranceStructureDefinitionId : context.Geometry.CompletionStructureDefinitionId;
                    SavedFixedSpatialStructure fixedValue = (floor.FixedStructures ??
                        Array.Empty<SavedFixedSpatialStructure>()).SingleOrDefault(value => value != null && value.Kind == kind);
                    if (fixedValue == null || fixedValue.FixedStructureDefinitionId != definition ||
                        !fixedValue.Anchor.Equals(placement.Anchor) || fixedValue.Orientation != placement.Orientation) return false;
                }
            }
            return true;
        }

        private static bool Field(ContractJsonNode node, int index, string name, ContractJsonKind kind) =>
            node.Fields[index].Key == name && node.Fields[index].Value.Kind == kind;
        private static bool PrimaryOrderIsCanonical(ContractJsonNode primary)
        {
            IReadOnlyList<string> recognized = RawSavePayloadClassifier.RecognizedSaveDataMemberNames;
            int previous = -1; bool unknownSeen = false;
            for (int index = 0; index < primary.Fields.Count - 2; index++)
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
        private static bool Same(byte[] left, byte[] right) => left != null && right != null && left.SequenceEqual(right);
        private static DetachedCompleteSaveValidationResult Failure() =>
            new DetachedCompleteSaveValidationResult(null, "gd66.transaction.candidate_invalid");
    }
}
