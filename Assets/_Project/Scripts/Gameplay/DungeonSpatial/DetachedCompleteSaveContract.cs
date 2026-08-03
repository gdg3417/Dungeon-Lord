using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class DetachedCurrentTargetValidationContext
    {
        public DetachedCurrentTargetValidationContext(SpatialLayoutCompatibilitySnapshot compatibility,
            ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits)
        { Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
          Production = production ?? throw new ArgumentNullException(nameof(production));
          if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits)); Limits = limits; }
        internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
        internal ProductionSpatialContentSnapshot Production { get; }
        public CanonicalSpatialSerializationLimits Limits { get; }
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
            int? layoutContractVersion = null)
        { Bytes = bytes == null ? null : (byte[])bytes.Clone(); Reason = reason;
          LayoutContractVersion = layoutContractVersion; }
        public bool IsValid => Bytes != null;
        public byte[] GetBytes() => Bytes == null ? null : (byte[])Bytes.Clone();
        public string Reason { get; }
        private byte[] Bytes { get; }
        internal int? LayoutContractVersion { get; }
    }

    public static class DetachedCompleteSaveContract
    {
        public static DetachedCompleteSaveValidationResult ParseValidateAndRoundTrip(byte[] bytes,
            DetachedCurrentTargetValidationContext context)
        {
            if (context == null) return Failure();
            DetachedCompleteSaveValidationResult result = ParseValidateAndRoundTrip(bytes,
                context.Limits, context.Production);
            if (!result.IsValid || !result.LayoutContractVersion.HasValue) return Failure();
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
                context.Production, context.TransactionId, context.DescriptorFingerprint);
            return result.IsValid && result.LayoutContractVersion ==
                context.SelectedContract.CanonicalLayoutContractVersion ? result : Failure();
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
                if (!parsedSpatial.IsValid || !DefinitionsValid(parsedSpatial.Value, production) ||
                    (expectedTransactionId != null && parsedSpatial.Value.Authority.MigrationTransactionId != expectedTransactionId) ||
                    (expectedDescriptorFingerprint != null &&
                        parsedSpatial.Value.Authority.MigrationDescriptorFingerprint != expectedDescriptorFingerprint)) return Failure();

                var completeWriter = new ContractJsonWriter(limits.Serialized);
                WriteNode(completeWriter, root); byte[] again = completeWriter.Finish();
                if (!Same(bytes, again)) return Failure();
                return new DetachedCompleteSaveValidationResult(bytes, null,
                    parsedSpatial.Value.Authority.CanonicalLayoutContractVersion);
            }
            catch { return Failure(); }
        }

        private static bool DefinitionsValid(DetachedCanonicalSpatialSaveState state,
            ProductionSpatialContentSnapshot production)
        {
            if (production == null) return true;
            SpatialContentCatalog catalog = production.Catalog;
            var floors = new HashSet<string>((catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                .Where(value => value != null).Select(value => value.FloorDefinitionId), StringComparer.Ordinal);
            var rooms = new HashSet<string>((catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Where(value => value != null).Select(value => value.RoomDefinitionId), StringComparer.Ordinal);
            var fixedDefinitions = new HashSet<string>((catalog.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>())
                .Where(value => value != null).Select(value => value.StructureDefinitionId), StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in state.Floors ?? Array.Empty<SavedSpatialFloor>())
            {
                if (!floors.Contains(floor.FloorDefinitionId)) return false;
                if ((floor.Layout.Rooms ?? Array.Empty<RoomSpatialInstance>()).Any(value => !rooms.Contains(value.RoomDefinitionId))) return false;
                if ((floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>()).Any(value =>
                    !fixedDefinitions.Contains(value.FixedStructureDefinitionId))) return false;
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
