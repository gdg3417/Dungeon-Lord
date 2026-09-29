using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    internal sealed class FloorConstructionProfileDocument
    {
        public FloorConstructionProfileRecord[] profiles = Array.Empty<FloorConstructionProfileRecord>();
    }

    [Serializable]
    internal sealed class FloorConstructionProfileRecord
    {
        public string profileId;
        public int profileVersion;
        public string floorDefinitionId;
        public string requiredResearchId;
        public double constructionMana;
        public TileCoordinate entranceAnchor;
        public CardinalOrientation entranceOrientation;
        public TileCoordinate completionAnchor;
        public CardinalOrientation completionOrientation;
    }

    public sealed class FloorConstructionProfile
    {
        internal FloorConstructionProfile(FloorConstructionProfileRecord record,
            FloorSpatialConfiguration floor)
        {
            ProfileId = record.profileId;
            ProfileVersion = record.profileVersion;
            FloorDefinitionId = record.floorDefinitionId;
            FloorIndex = floor.FloorIndex;
            RequiredResearchId = record.requiredResearchId;
            ConstructionMana = record.constructionMana;
            EntranceAnchor = record.entranceAnchor;
            EntranceOrientation = record.entranceOrientation;
            CompletionAnchor = record.completionAnchor;
            CompletionOrientation = record.completionOrientation;
        }

        public string ProfileId { get; }
        public int ProfileVersion { get; }
        public string FloorDefinitionId { get; }
        public int FloorIndex { get; }
        public string RequiredResearchId { get; }
        public double ConstructionMana { get; }
        public TileCoordinate EntranceAnchor { get; }
        public CardinalOrientation EntranceOrientation { get; }
        public TileCoordinate CompletionAnchor { get; }
        public CardinalOrientation CompletionOrientation { get; }

        internal bool Matches(FloorConstructionProfile other) => other != null &&
            ProfileId == other.ProfileId && ProfileVersion == other.ProfileVersion &&
            FloorDefinitionId == other.FloorDefinitionId && FloorIndex == other.FloorIndex &&
            RequiredResearchId == other.RequiredResearchId && ConstructionMana == other.ConstructionMana &&
            EntranceAnchor.Equals(other.EntranceAnchor) && EntranceOrientation == other.EntranceOrientation &&
            CompletionAnchor.Equals(other.CompletionAnchor) && CompletionOrientation == other.CompletionOrientation;
    }

    public sealed class FloorConstructionProfileSnapshot
    {
        public const string ProductionResourcePath = "floor_construction_profiles";
        private readonly FloorConstructionProfile[] profiles;

        private FloorConstructionProfileSnapshot(IEnumerable<FloorConstructionProfile> values) =>
            profiles = values.OrderBy(value => value.FloorIndex)
                .ThenBy(value => value.ProfileId, StringComparer.Ordinal).ToArray();

        public static bool TryParse(TextAsset asset, ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits, out FloorConstructionProfileSnapshot snapshot) =>
            TryParse(asset == null ? null : asset.bytes, production, limits, out snapshot);

        public static bool TryParse(byte[] bytes, ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits, out FloorConstructionProfileSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (bytes == null || bytes.Length == 0 || production?.Catalog == null || !limits.IsValid)
                    return false;
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(bytes, limits.Serialized, issues, out ContractJsonNode root, true) ||
                    root.Kind != ContractJsonKind.Object || root.Fields.Count != 1 ||
                    root.Fields[0].Key != "profiles" || root.Fields[0].Value.Kind != ContractJsonKind.Array ||
                    root.Fields[0].Value.Items.Any(value => !ValidShape(value))) return false;
                FloorConstructionProfileDocument document = JsonUtility.FromJson<FloorConstructionProfileDocument>(
                    Encoding.UTF8.GetString(bytes));
                FloorConstructionProfileRecord[] records = document?.profiles;
                if (records == null || records.Length == 0 || records.Length > limits.Spatial.MaximumRecords ||
                    records.Any(value => value == null || string.IsNullOrWhiteSpace(value.profileId) ||
                        value.profileVersion <= 0 || string.IsNullOrWhiteSpace(value.floorDefinitionId) ||
                        value.requiredResearchId != FloorConstructionResearchAuthority.ResearchId ||
                        value.floorDefinitionId != "spatial.floor.02" ||
                        !StructuralEconomySnapshot.Nonnegative(value.constructionMana) ||
                        value.constructionMana <= 0d ||
                        !Enum.IsDefined(typeof(CardinalOrientation), value.entranceOrientation) ||
                        !Enum.IsDefined(typeof(CardinalOrientation), value.completionOrientation)) ||
                    records.Select(value => value.profileId).Distinct(StringComparer.Ordinal).Count() != records.Length ||
                    records.Select(value => value.floorDefinitionId).Distinct(StringComparer.Ordinal).Count() != records.Length)
                    return false;

                var result = new List<FloorConstructionProfile>();
                foreach (FloorConstructionProfileRecord record in records)
                {
                    FloorSpatialConfiguration[] floorMatches = (production.Catalog.Floors ??
                        Array.Empty<FloorSpatialConfiguration>()).Where(value => value != null &&
                        value.FloorDefinitionId == record.floorDefinitionId).ToArray();
                    if (floorMatches.Length != 1) return false;
                    FloorSpatialConfiguration floor = floorMatches[0];
                    FixedSpatialStructureDefinition[] entrances = (production.Catalog.FixedStructures ??
                        Array.Empty<FixedSpatialStructureDefinition>()).Where(value => value != null &&
                        value.StructureDefinitionId == floor.EntranceStructureDefinitionId &&
                        value.Kind == FixedSpatialStructureKind.Entrance).ToArray();
                    FixedSpatialStructureDefinition[] completions = (production.Catalog.FixedStructures ??
                        Array.Empty<FixedSpatialStructureDefinition>()).Where(value => value != null &&
                        value.StructureDefinitionId == floor.CompletionStructureDefinitionId &&
                        value.Kind == FixedSpatialStructureKind.CompletionTerminal).ToArray();
                    if (entrances.Length != 1 || completions.Length != 1 || floor.Bounds == null ||
                        !floor.Bounds.IsValid || !(entrances[0].AllowedOrientations ??
                            Array.Empty<CardinalOrientation>()).Contains(record.entranceOrientation) ||
                        !(completions[0].AllowedOrientations ??
                            Array.Empty<CardinalOrientation>()).Contains(record.completionOrientation))
                        return false;
                    var workload = new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles);
                    if (!TileFootprintResolver.TryResolveRectangle(entrances[0].GrossFootprint,
                            record.entranceAnchor, record.entranceOrientation, workload,
                            out ResolvedTileFootprint entranceTiles) ||
                        !TileFootprintResolver.TryResolveRectangle(completions[0].GrossFootprint,
                            record.completionAnchor, record.completionOrientation, workload,
                            out ResolvedTileFootprint completionTiles) ||
                        entranceTiles.OccupiedTiles.Any(tile => !floor.Bounds.Contains(tile)) ||
                        completionTiles.OccupiedTiles.Any(tile => !floor.Bounds.Contains(tile)) ||
                        new HashSet<TileCoordinate>(entranceTiles.OccupiedTiles)
                            .Overlaps(completionTiles.OccupiedTiles))
                        return false;
                    result.Add(new FloorConstructionProfile(record, floor));
                }
                snapshot = new FloorConstructionProfileSnapshot(result);
                return true;
            }
            catch { return false; }
        }

        public bool TryResolve(int floorIndex, out FloorConstructionProfile profile)
        {
            FloorConstructionProfile[] matches = profiles.Where(value => value.FloorIndex == floorIndex).ToArray();
            profile = matches.Length == 1 ? matches[0] : null;
            return profile != null;
        }

        private static bool ValidShape(ContractJsonNode record)
        {
            string[] names = { "profileId", "profileVersion", "floorDefinitionId", "requiredResearchId",
                "constructionMana", "entranceAnchor", "entranceOrientation", "completionAnchor", "completionOrientation" };
            if (record?.Kind != ContractJsonKind.Object || record.Fields.Count != names.Length ||
                !record.Fields.Select(value => value.Key).OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(names.OrderBy(value => value, StringComparer.Ordinal))) return false;
            foreach (var field in record.Fields)
            {
                if (field.Key.EndsWith("Anchor", StringComparison.Ordinal))
                {
                    if (field.Value.Kind != ContractJsonKind.Object || field.Value.Fields.Count != 2 ||
                        !field.Value.Fields.Select(value => value.Key).OrderBy(value => value, StringComparer.Ordinal)
                            .SequenceEqual(new[] { "X", "Y" }) || field.Value.Fields.Any(value =>
                            value.Value.Kind != ContractJsonKind.Number || !int.TryParse(value.Value.Text,
                                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))) return false;
                }
                else if (field.Key.EndsWith("Id", StringComparison.Ordinal))
                { if (field.Value.Kind != ContractJsonKind.String) return false; }
                else if (field.Value.Kind != ContractJsonKind.Number ||
                    (field.Key != "constructionMana" && !int.TryParse(field.Value.Text,
                        NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))) return false;
            }
            return true;
        }
    }

    public sealed class FloorConstructionResearchSnapshot
    {
        internal FloorConstructionResearchSnapshot(string researchId, int maximumFloorCount)
        { ResearchId = researchId; MaximumFloorCount = maximumFloorCount; }
        public string ResearchId { get; }
        public int MaximumFloorCount { get; }
    }

    public static class FloorConstructionResearchAuthority
    {
        public const string ResearchId = "ac_100";
        public const string ConfigurationInvalidReason = "floor.construction.research_configuration_invalid";
        public const string ResearchRequiredReason = "floor.construction.research_required";
        private const string Category = "architecture";
        private const string UnlockType = "effect";
        private const string UnlockTarget = "max_floors";
        private const string EffectType = "max_floors_set";
        private const string Unit = "int";

        public static bool TryParse(TextAsset nodes, TextAsset effects, CanonicalSpatialSerializationLimits limits,
            out FloorConstructionResearchSnapshot snapshot) => TryParse(nodes?.text, effects?.text, limits, out snapshot);

        public static bool TryParse(string nodesJson, string effectsJson, CanonicalSpatialSerializationLimits limits,
            out FloorConstructionResearchSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                if (string.IsNullOrWhiteSpace(nodesJson) || string.IsNullOrWhiteSpace(effectsJson) || !limits.IsValid) return false;
                var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(Encoding.UTF8.GetBytes(nodesJson), limits.Serialized, issues,
                        out ContractJsonNode nodeDocument, true) || nodeDocument.Kind != ContractJsonKind.Array ||
                    !ContractJson.TryParse(Encoding.UTF8.GetBytes(effectsJson), limits.Serialized, issues,
                        out ContractJsonNode effectDocument, true) || effectDocument.Kind != ContractJsonKind.Object) return false;
                ContractJsonNode[] rawNodes = nodeDocument.Items.Where(value => StringField(value, "research_id") == ResearchId).ToArray();
                ContractJsonNode rawEffects = effectDocument.Fields.SingleOrDefault(value => value.Key == "ArchitectureEffects").Value;
                if (rawNodes.Length != 1 || rawEffects?.Kind != ContractJsonKind.Array ||
                    new[] { "category", "unlock_type", "unlock_target_id", "effect_profile_id" }.Any(name =>
                        StringField(rawNodes[0], name) == null)) return false;
                string effectId = StringField(rawNodes[0], "effect_profile_id");
                ContractJsonNode[] rawMatches = rawEffects.Items.Where(value => StringField(value, "eff_id") == effectId).ToArray();
                if (rawMatches.Length != 1 || StringField(rawMatches[0], "effect_type") == null ||
                    StringField(rawMatches[0], "unit") == null ||
                    rawMatches[0].Fields.SingleOrDefault(value => value.Key == "value").Value?.Kind != ContractJsonKind.Number)
                    return false;
                ProductionResearchNodeCollection nodes = JsonUtility.FromJson<ProductionResearchNodeCollection>(
                    "{\"Nodes\":" + nodesJson + "}");
                ProductionArchitectureEffectCollection effects =
                    JsonUtility.FromJson<ProductionArchitectureEffectCollection>(effectsJson);
                ProductionResearchNode[] nodeMatches = (nodes?.Nodes ?? Array.Empty<ProductionResearchNode>())
                    .Where(value => value != null && value.research_id == ResearchId).ToArray();
                if (nodeMatches.Length != 1) return false;
                ProductionResearchNode node = nodeMatches[0];
                if (node.category != Category || node.unlock_type != UnlockType ||
                    node.unlock_target_id != UnlockTarget || node.effect_profile_id != "eff_ac_floor_2_permit" ||
                    nodes.Nodes.Any(value => value != null && value.research_id != ResearchId &&
                        value.effect_profile_id == node.effect_profile_id))
                    return false;
                ProductionArchitectureEffect[] effectMatches = (effects?.ArchitectureEffects ??
                    Array.Empty<ProductionArchitectureEffect>()).Where(value => value != null &&
                    value.eff_id == node.effect_profile_id).ToArray();
                if (effectMatches.Length != 1) return false;
                ProductionArchitectureEffect effect = effectMatches[0];
                if (effect.effect_type != EffectType || effect.unit != Unit ||
                    double.IsNaN(effect.value) || double.IsInfinity(effect.value) || effect.value <= 0d ||
                    effect.value > int.MaxValue || Math.Floor(effect.value) != effect.value)
                    return false;
                snapshot = new FloorConstructionResearchSnapshot(node.research_id, (int)effect.value);
                return true;
            }
            catch { return false; }
        }

        private static string StringField(ContractJsonNode node, string name)
        {
            if (node?.Kind != ContractJsonKind.Object) return null;
            ContractJsonNode value = node.Fields.SingleOrDefault(field => field.Key == name).Value;
            return value?.Kind == ContractJsonKind.String ? value.Text : null;
        }

        public static string Resolve(CompletedResearchState completed,
            FloorConstructionResearchSnapshot research, FloorConstructionProfile profile)
        {
            if (research == null || profile == null || profile.RequiredResearchId != research.ResearchId ||
                profile.FloorIndex < 0 || profile.FloorIndex + 1 != research.MaximumFloorCount)
                return ConfigurationInvalidReason;
            return CompletedResearchStateResolver.HasCompletedProject(completed, research.ResearchId)
                ? null : ResearchRequiredReason;
        }
    }

    public sealed class FloorConstructionPreview
    {
        internal DetachedCanonicalSpatialSaveState Candidate { get; set; }
        internal string BaselineFingerprint { get; set; }
        public FloorConstructionProfile Profile { get; internal set; }
        public string FloorInstanceId { get; internal set; }
        public string Reason { get; internal set; }
        public bool IsAffordable { get; internal set; }
        public bool IsValid => Candidate != null && Reason == null;
        public bool IsCommittable => IsValid && IsAffordable;
    }

    public static class FloorConstructionService
    {
        internal static string FloorIdentity(int floorIndex) =>
            "canonical.floor." + floorIndex.ToString("D2", CultureInfo.InvariantCulture);
        public const string InvalidContextReason = "floor.construction.invalid_context";
        public const string AlreadyConstructedReason = "floor.construction.already_constructed";
        public const string ProfileInvalidReason = "floor.construction.profile_invalid";
        public const string InsufficientManaReason = "floor.construction.insufficient_mana";
        public const string StalePreviewReason = "floor.construction.stale_preview";
        public const string IdentityInvalidReason = "floor.construction.identity_invalid";

        public static FloorConstructionPreview Preview(DetachedCanonicalSpatialSaveState current,
            SaveData runtime, CompletedResearchState completedResearch,
            FloorConstructionProfileSnapshot profiles, FloorConstructionResearchSnapshot research,
            ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            var result = new FloorConstructionPreview();
            if (current?.Floors == null || runtime?.structureRuntime == null || profiles == null ||
                production == null || configuration == null || !limits.IsValid ||
                !CanonicalSpatialSaveContracts.Validate(current, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(current, production,
                    configuration, limits.Spatial).IsValid ||
                !StructuralEditService.TryFingerprint(current, limits, out string baseline))
                return Fail(result, InvalidContextReason);
            // This packet constructs only the production Floor 2 permission, never deeper floors.
            FloorSpatialConfiguration[] targets = production.Catalog.Floors.Where(value =>
                value != null && value.FloorDefinitionId == "spatial.floor.02").ToArray();
            if (targets.Length != 1 || !profiles.TryResolve(targets[0].FloorIndex, out FloorConstructionProfile profile))
                return Fail(result, ProfileInvalidReason);
            result.Profile = profile;
            if (current.Floors.Any(value => value != null &&
                    (value.FloorIndex == profile.FloorIndex || value.FloorDefinitionId == profile.FloorDefinitionId)))
                return Fail(result, AlreadyConstructedReason);
            if (current.Floors.Count(value => value.FloorIndex == 0 &&
                    value.ActivationState == FloorActivationState.Active) != 1)
                return Fail(result, InvalidContextReason);
            string researchReason = FloorConstructionResearchAuthority.Resolve(completedResearch, research, profile);
            if (researchReason != null) return Fail(result, researchReason);
            if (!TryClone(current, limits, out DetachedCanonicalSpatialSaveState candidate))
                return Fail(result, InvalidContextReason);
            string floorId = FloorIdentity(profile.FloorIndex);
            if (!Persistent(floorId) || AllIdentities(candidate).Contains(floorId))
                return Fail(result, IdentityInvalidReason);
            FloorSpatialConfiguration floorDefinition = production.Catalog.Floors.Single(value =>
                value.FloorDefinitionId == profile.FloorDefinitionId && value.FloorIndex == profile.FloorIndex);
            string entranceId = floorId + ".fixed.entrance";
            string completionId = floorId + ".fixed.completion";
            string entranceNodeId = floorId + ".node.entrance";
            string completionNodeId = floorId + ".node.completion";
            string[] proposedIds = { floorId, entranceId, completionId, entranceNodeId, completionNodeId,
                StructuralInvestment.ShellId(floorId) };
            HashSet<string> identities = AllIdentities(candidate);
            if (proposedIds.Any(value => !Persistent(value) || identities.Contains(value)) ||
                proposedIds.Distinct(StringComparer.Ordinal).Count() != proposedIds.Length)
                return Fail(result, IdentityInvalidReason);
            var floor = new SavedSpatialFloor
            {
                FloorInstanceId = floorId,
                FloorDefinitionId = profile.FloorDefinitionId,
                FloorIndex = profile.FloorIndex,
                ActivationState = FloorActivationState.Inactive,
                Layout = new FloorSpatialLayout
                {
                    FloorId = floorId,
                    Rooms = Array.Empty<RoomSpatialInstance>(),
                    Nodes = new[]
                    {
                        new FloorRouteNode { NodeId = entranceNodeId, FloorId = floorId,
                            Kind = FloorRouteNodeKind.Entrance, RoomInstanceId = null },
                        new FloorRouteNode { NodeId = completionNodeId, FloorId = floorId,
                            Kind = FloorRouteNodeKind.Completion, RoomInstanceId = null }
                    },
                    Edges = Array.Empty<FloorRouteEdge>()
                },
                FixedStructures = new[]
                {
                    new SavedFixedSpatialStructure { FixedStructureInstanceId = entranceId,
                        FixedStructureDefinitionId = floorDefinition.EntranceStructureDefinitionId,
                        FloorInstanceId = floorId, Anchor = profile.EntranceAnchor,
                        Orientation = profile.EntranceOrientation, Kind = FixedSpatialStructureKind.Entrance },
                    new SavedFixedSpatialStructure { FixedStructureInstanceId = completionId,
                        FixedStructureDefinitionId = floorDefinition.CompletionStructureDefinitionId,
                        FloorInstanceId = floorId, Anchor = profile.CompletionAnchor,
                        Orientation = profile.CompletionOrientation,
                        Kind = FixedSpatialStructureKind.CompletionTerminal }
                },
                RoomContents = new FloorRoomContentState()
            };
            candidate.Floors = candidate.Floors.Concat(new[] { floor }).ToArray();
            candidate.LifecycleAndOwnership.Floors = candidate.LifecycleAndOwnership.Floors.Concat(new[]
            {
                new FloorStructuralIdentityLifecycle { FloorInstanceId = floorId,
                    NextNativeRoomOrdinal = 0, NextNativeEdgeOrdinal = 0L }
            }).ToArray();
            if (!CanonicalSpatialSaveContracts.TryCanonicalize(candidate, limits.Spatial, out candidate) ||
                !CanonicalSpatialSaveContracts.Validate(candidate, limits.Spatial, true).IsValid ||
                !DetachedCanonicalProductionSemanticValidation.Validate(candidate, production,
                    configuration, limits.Spatial).IsValid)
                return Fail(result, InvalidContextReason);
            result.Candidate = candidate;
            result.BaselineFingerprint = baseline;
            result.FloorInstanceId = floorId;
            result.IsAffordable = StructuralEconomySnapshot.Nonnegative(runtime.structureRuntime.ManaReserve) &&
                runtime.structureRuntime.ManaReserve >= profile.ConstructionMana;
            if (!result.IsAffordable) result.Reason = InsufficientManaReason;
            return result;
        }

        private static FloorConstructionPreview Fail(FloorConstructionPreview result, string reason)
        { result.Reason = reason; return result; }

        private static bool TryClone(DetachedCanonicalSpatialSaveState state,
            CanonicalSpatialSerializationLimits limits, out DetachedCanonicalSpatialSaveState clone)
        {
            clone = null;
            SpatialContractResult<byte[]> bytes = CanonicalSpatialSaveSerializer.Serialize(state, limits);
            if (!bytes.IsValid) return false;
            SpatialContractResult<DetachedCanonicalSpatialSaveState> parsed =
                CanonicalSpatialSaveSerializer.Parse(bytes.Value, limits);
            clone = parsed.Value;
            return parsed.IsValid;
        }

        private static HashSet<string> AllIdentities(DetachedCanonicalSpatialSaveState state)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in state.Floors ?? Array.Empty<SavedSpatialFloor>())
            {
                if (floor == null) continue;
                result.Add(floor.FloorInstanceId);
                foreach (RoomSpatialInstance room in floor.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>())
                    result.Add(room?.RoomInstanceId);
                foreach (FloorRouteNode node in floor.Layout?.Nodes ?? Array.Empty<FloorRouteNode>()) result.Add(node?.NodeId);
                foreach (FloorRouteEdge edge in floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()) result.Add(edge?.EdgeId);
                foreach (SavedFixedSpatialStructure fixedValue in floor.FixedStructures ??
                    Array.Empty<SavedFixedSpatialStructure>()) result.Add(fixedValue?.FixedStructureInstanceId);
                foreach (RoomContentAssignment assignment in floor.RoomContents?.Assignments ??
                    Array.Empty<RoomContentAssignment>()) result.Add(assignment?.AssignmentId);
            }
            foreach (ReturnedStructuralContent returned in state.LifecycleAndOwnership?.ReturnedContents ??
                Array.Empty<ReturnedStructuralContent>()) result.Add(returned?.AssignmentId);
            return result;
        }

        private static bool Persistent(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                bool alphaNumeric = character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
                if (alphaNumeric) { separator = false; continue; }
                if ((character != '.' && character != '_' && character != '-') || separator) return false;
                separator = true;
            }
            return !separator;
        }
    }

    /// <summary>Stable floor targeting. Omission is compatible only with a single persisted floor.</summary>
    public static class CanonicalEditFloorTarget
    {
        public const string InvalidReason = "structural.edit.floor_target_invalid";
        public static bool TryResolve(DetachedCanonicalSpatialSaveState state, string floorInstanceId,
            out SavedSpatialFloor floor)
        {
            floor = null;
            SavedSpatialFloor[] floors = state?.Floors;
            if (floors == null || floors.Any(value => value == null)) return false;
            SavedSpatialFloor[] matches = string.IsNullOrWhiteSpace(floorInstanceId)
                ? floors : floors.Where(value => value.FloorInstanceId == floorInstanceId).ToArray();
            if (matches.Length != 1) return false;
            floor = matches.Single();
            string resolvedId = floor.FloorInstanceId;
            return !string.IsNullOrWhiteSpace(floor.FloorInstanceId) &&
                floors.Count(value => value.FloorInstanceId == resolvedId) == 1;
        }
        public static FloorLayoutValidationMode Mode(SavedSpatialFloor floor) =>
            floor.ActivationState == FloorActivationState.Inactive
                ? FloorLayoutValidationMode.ConstructionValid : FloorLayoutValidationMode.ActivationValid;
    }
}
