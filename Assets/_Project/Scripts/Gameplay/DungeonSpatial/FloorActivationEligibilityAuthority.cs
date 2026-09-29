using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class FloorActivationEligibilityReasons
    {
        public const string InvalidContext = "floor.activation.invalid_context";
        public const string CanonicalStateInvalid = "floor.activation.canonical_state_invalid";
        public const string TargetConfigurationInvalid = "floor.activation.target_configuration_invalid";
        public const string TargetIdentityInvalid = "floor.activation.target_identity_invalid";
        public const string TargetNotConstructed = "floor.activation.target_not_constructed";
        public const string TargetAlreadyActive = "floor.activation.target_already_active";
        public const string ResearchConfigurationInvalid = "floor.activation.research_configuration_invalid";
        public const string ResearchRequired = "floor.activation.research_required";
        public const string ShallowerFloorInactive = "floor.activation.shallower_floor_inactive";
        public const string ActivationLayoutInvalid = "floor.activation.layout_invalid";
        public const string RequiredRouteMissingRoom = "floor.activation.required_route_missing_room";
        public const string ProductionSemanticsInvalid = "floor.activation.production_semantics_invalid";
    }

    public sealed class FloorActivationEligibilityResult
    {
        internal FloorActivationEligibilityResult(bool eligible, IEnumerable<string> reasons,
            SavedSpatialFloor target)
        {
            IsEligible = eligible;
            ReasonCodes = (reasons ?? Enumerable.Empty<string>()).ToArray();
            PrimaryBlocker = ReasonCodes.FirstOrDefault();
            if (target == null) return;
            HasTargetFloor = true;
            TargetFloorInstanceId = target.FloorInstanceId;
            TargetFloorDefinitionId = target.FloorDefinitionId;
            TargetFloorIndex = target.FloorIndex;
        }

        public bool IsEligible { get; }
        public string[] ReasonCodes { get; }
        public string PrimaryBlocker { get; }
        public bool HasTargetFloor { get; }
        public string TargetFloorInstanceId { get; }
        public string TargetFloorDefinitionId { get; }
        public int TargetFloorIndex { get; }
    }

    /// <summary>
    /// Resolves whether the production Floor 2 may become Active. This authority never writes,
    /// canonicalizes, repairs, or temporarily changes caller-owned state.
    /// </summary>
    public static class FloorActivationEligibilityAuthority
    {
        public const string ProductionFloorTwoDefinitionId = "spatial.floor.02";

        public static FloorActivationEligibilityResult Resolve(
            DetachedCanonicalSpatialSaveState state, CompletedResearchState completedResearch,
            FloorConstructionProfileSnapshot constructionProfiles,
            FloorConstructionResearchSnapshot constructionResearch,
            ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            CanonicalSpatialSerializationLimits limits)
        {
            if (state?.Floors == null || constructionProfiles == null || production?.Catalog == null ||
                configuration == null || !limits.IsValid)
                return Blocked(FloorActivationEligibilityReasons.InvalidContext);

            FloorSpatialConfiguration[] productionTargets = (production.Catalog.Floors ??
                Array.Empty<FloorSpatialConfiguration>()).Where(value => value != null &&
                string.Equals(value.FloorDefinitionId, ProductionFloorTwoDefinitionId,
                    StringComparison.Ordinal)).ToArray();
            if (productionTargets.Length != 1 ||
                !constructionProfiles.TryResolve(productionTargets[0].FloorIndex,
                    out FloorConstructionProfile profile) ||
                !string.Equals(profile.FloorDefinitionId, ProductionFloorTwoDefinitionId,
                    StringComparison.Ordinal))
                return Blocked(FloorActivationEligibilityReasons.TargetConfigurationInvalid);

            CanonicalSpatialSaveValidationResult canonical = CanonicalSpatialSaveContracts.Validate(
                state, limits.Spatial, requireCanonicalOrdering: true);
            if (!canonical.IsValid)
                return Blocked(FloorActivationEligibilityReasons.CanonicalStateInvalid,
                    ResolveStableTarget(state, productionTargets[0], profile));

            FloorSpatialConfiguration targetConfiguration = productionTargets[0];
            string expectedInstanceId = FloorConstructionService.FloorIdentity(targetConfiguration.FloorIndex);
            SavedSpatialFloor[] identityCandidates = state.Floors.Where(value => value != null &&
                (string.Equals(value.FloorInstanceId, expectedInstanceId, StringComparison.Ordinal) ||
                 value.FloorIndex == targetConfiguration.FloorIndex ||
                 string.Equals(value.FloorDefinitionId, targetConfiguration.FloorDefinitionId,
                     StringComparison.Ordinal))).ToArray();
            if (identityCandidates.Length == 0)
                return Blocked(FloorActivationEligibilityReasons.TargetNotConstructed);
            if (identityCandidates.Length != 1 ||
                !MatchesTarget(identityCandidates[0], targetConfiguration, expectedInstanceId))
                return Blocked(FloorActivationEligibilityReasons.TargetIdentityInvalid);

            SavedSpatialFloor target = identityCandidates[0];
            var reasons = new List<string>();
            if (target.ActivationState == FloorActivationState.Active)
                reasons.Add(FloorActivationEligibilityReasons.TargetAlreadyActive);

            string researchReason = FloorConstructionResearchAuthority.Resolve(
                completedResearch, constructionResearch, profile);
            if (researchReason == FloorConstructionResearchAuthority.ConfigurationInvalidReason)
                reasons.Add(FloorActivationEligibilityReasons.ResearchConfigurationInvalid);
            else if (researchReason != null)
                reasons.Add(FloorActivationEligibilityReasons.ResearchRequired);

            if (!ShallowerFloorsAreActive(state.Floors, target.FloorIndex))
                reasons.Add(FloorActivationEligibilityReasons.ShallowerFloorInactive);

            FloorLayoutValidationResult layout = FloorLayoutValidator.Validate(
                target.Layout, targetConfiguration,
                production.Catalog.Rooms, production.Catalog.Corridors,
                new SpatialValidationWorkloadLimits(limits.Spatial.MaximumMaterializedTiles),
                target.FixedStructures, production.Catalog.FixedStructures,
                FloorLayoutValidationMode.ActivationValid);
            if (!layout.IsValid)
                reasons.Add(FloorActivationEligibilityReasons.ActivationLayoutInvalid);
            else if (!HasRequiredRouteRoom(target.Layout))
                reasons.Add(FloorActivationEligibilityReasons.RequiredRouteMissingRoom);

            if (!DetachedCanonicalProductionSemanticValidation.Validate(
                    state, production, configuration, limits.Spatial).IsValid)
                reasons.Add(FloorActivationEligibilityReasons.ProductionSemanticsInvalid);

            return reasons.Count == 0
                ? new FloorActivationEligibilityResult(true, Array.Empty<string>(), target)
                : new FloorActivationEligibilityResult(false, reasons, target);
        }

        private static FloorActivationEligibilityResult Blocked(string reason,
            SavedSpatialFloor target = null) =>
            new FloorActivationEligibilityResult(false, new[] { reason }, target);

        private static SavedSpatialFloor ResolveStableTarget(DetachedCanonicalSpatialSaveState state,
            FloorSpatialConfiguration target, FloorConstructionProfile profile)
        {
            if (state?.Floors == null || target == null || profile == null) return null;
            string expected = FloorConstructionService.FloorIdentity(target.FloorIndex);
            SavedSpatialFloor[] matches = state.Floors.Where(value =>
                MatchesTarget(value, target, expected)).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static bool MatchesTarget(SavedSpatialFloor floor,
            FloorSpatialConfiguration target, string expectedInstanceId) =>
            floor != null && target != null &&
            string.Equals(floor.FloorInstanceId, expectedInstanceId, StringComparison.Ordinal) &&
            string.Equals(floor.FloorDefinitionId, target.FloorDefinitionId,
                StringComparison.Ordinal) && floor.FloorIndex == target.FloorIndex;

        private static bool ShallowerFloorsAreActive(IEnumerable<SavedSpatialFloor> floors,
            int targetIndex)
        {
            SavedSpatialFloor[] values = (floors ?? Enumerable.Empty<SavedSpatialFloor>()).ToArray();
            for (int index = 0; index < targetIndex; index++)
            {
                SavedSpatialFloor[] matches = values.Where(value => value != null &&
                    value.FloorIndex == index).ToArray();
                if (matches.Length != 1 ||
                    matches[0].ActivationState != FloorActivationState.Active) return false;
            }
            return true;
        }

        private static bool HasRequiredRouteRoom(FloorSpatialLayout layout)
        {
            FloorRouteNode[] nodes = layout?.Nodes ?? Array.Empty<FloorRouteNode>();
            FloorRouteNode[] entrances = nodes.Where(value => value != null &&
                value.Kind == FloorRouteNodeKind.Entrance).ToArray();
            FloorRouteNode[] completions = nodes.Where(value => value != null &&
                value.Kind == FloorRouteNodeKind.Completion).ToArray();
            if (entrances.Length != 1 || completions.Length != 1) return false;

            var nodeById = nodes.Where(value => value != null).ToDictionary(
                value => value.NodeId, StringComparer.Ordinal);
            FloorRouteEdge[] edges = (layout.Edges ?? Array.Empty<FloorRouteEdge>())
                .Where(value => value != null && value.Classification == RouteClassification.Required)
                .OrderBy(value => value.SourceNodeId, StringComparer.Ordinal)
                .ThenBy(value => value.DestinationNodeId, StringComparer.Ordinal)
                .ThenBy(value => value.EdgeId, StringComparer.Ordinal).ToArray();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<RouteStep>();
            queue.Enqueue(new RouteStep(entrances[0].NodeId, false));
            while (queue.Count > 0)
            {
                RouteStep current = queue.Dequeue();
                if (string.Equals(current.NodeId, completions[0].NodeId,
                        StringComparison.Ordinal)) return current.HasRoom;
                string key = current.NodeId + (current.HasRoom ? "\u0001" : "\u0000");
                if (!visited.Add(key)) continue;
                foreach (FloorRouteEdge edge in edges.Where(value =>
                    string.Equals(value.SourceNodeId, current.NodeId, StringComparison.Ordinal)))
                {
                    if (!nodeById.TryGetValue(edge.DestinationNodeId, out FloorRouteNode destination))
                        continue;
                    queue.Enqueue(new RouteStep(destination.NodeId,
                        current.HasRoom || destination.Kind == FloorRouteNodeKind.Room));
                }
            }
            return false;
        }

        private readonly struct RouteStep
        {
            internal RouteStep(string nodeId, bool hasRoom)
            { NodeId = nodeId; HasRoom = hasRoom; }
            internal string NodeId { get; }
            internal bool HasRoom { get; }
        }
    }
}
