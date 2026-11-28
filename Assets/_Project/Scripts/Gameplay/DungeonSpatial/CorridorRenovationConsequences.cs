using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class CorridorRenovationConsequences
    {
        internal const string UnresolvedReason = "ui.dungeon.route.corridor_content_unresolved";

        internal static CorridorContentAssignment[] Displaced(DetachedCompleteSaveValidationResult owned,
            DetachedCanonicalSpatialSaveState final, ProductionSpatialContentSnapshot production)
        {
            return owned.CorridorContent.Assignments.Where(assignment =>
            {
                var floor = final.Floors.Single(value => value.FloorInstanceId == assignment.FloorInstanceId);
                var edge = floor.Layout.Edges.SingleOrDefault(value => value.EdgeId == assignment.EdgeId &&
                    value.OptionalBranchId == assignment.OptionalBranchId &&
                    value.Classification == RouteClassification.Optional);
                if (edge == null || !edge.CorridorTiles.Contains(assignment.Tile)) return true;
                if (assignment.CategoryId != CanonicalSpatialSaveContracts.LootNodeCategoryId) return false;
                return !OptionalBranchGeometry.TryResolveSourceTile(floor, edge, production.Catalog, out var start) ||
                    !assignment.Tile.Equals(PhaseFiveSaveContracts.TerminalTile(edge, start));
            }).ToArray();
        }

        internal static string Prepare(DetachedCompleteSaveValidationResult owned, DetachedCanonicalSpatialSaveState final,
            StructuralRouteRenovation[] dispositions, ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            StructuralContentRemovalPolicySnapshot removalPolicy, CanonicalSpatialSaveWorkloadLimits limits,
            out CorridorContentAuthority corridor, out SharedBranchKnowledgeAuthority knowledge)
        {
            corridor = PhaseFiveSaveContracts.Canonicalize(owned.CorridorContent);
            knowledge = PhaseFiveSaveContracts.Canonicalize(owned.BranchKnowledge);
            var affected = Displaced(owned, final, production);
            var displacedIds = affected.Select(value => value.AssignmentId).ToArray();
            if (displacedIds.Any(id => !dispositions.Any(intent => intent.TargetId == id))) return UnresolvedReason;

            foreach (var disposition in dispositions.Where(intent => displacedIds.Contains(intent.TargetId)))
            {
                string result = disposition.Kind == StructuralRouteRenovationKind.ReturnCorridorContent
                    ? CorridorContentMutationService.Unassign(final, corridor, disposition.TargetId, removalPolicy)
                    : CorridorContentMutationService.Reassign(final, corridor, disposition.TargetId,
                        disposition.FloorInstanceId, disposition.DestinationNodeId, disposition.Anchor, production);
                if (result != null) return result;
            }
            corridor = PhaseFiveSaveContracts.Canonicalize(corridor);
            knowledge.Records = knowledge.Records.Where(record => final.Floors.Any(floor =>
                floor.FloorInstanceId == record.FloorInstanceId && floor.Layout.Edges.Any(edge =>
                    edge.EdgeId == record.EdgeId && edge.OptionalBranchId == record.OptionalBranchId))).ToArray();
            return PhaseFiveSaveContracts.Validate(corridor, knowledge, final, production, configuration, limits)
                ? null : UnresolvedReason;
        }

        internal static string BranchEntitlement(DetachedCanonicalSpatialSaveState baseline,
            DetachedCanonicalSpatialSaveState final, ProductionSpatialContentSnapshot production,
            CompletedResearchState completed, BasicBranchingResearchSnapshot research)
        {
            var catalog = production.Catalog;
            foreach (var floor in final.Floors)
            {
                var existing = baseline.Floors.Single(value => value.FloorInstanceId == floor.FloorInstanceId);
                var oldIds = existing.Layout.Edges.Where(edge => edge.Classification == RouteClassification.Optional)
                    .Select(edge => edge.OptionalBranchId).ToArray();
                var branches = floor.Layout.Edges.Where(edge => edge.Classification == RouteClassification.Optional).ToArray();
                if (branches.All(edge => oldIds.Contains(edge.OptionalBranchId))) continue;
                var definition = catalog.Floors.Single(value => value.FloorDefinitionId == floor.FloorDefinitionId);
                var allowance = BasicBranchingResearchAuthority.Resolve(completed, research, definition);
                if (!allowance.IsResolved || branches.Length > allowance.EffectiveAllowance)
                    return allowance.Reason ?? OptionalBranchStructuralEditService.AllowanceExceededReason;
            }
            return null;
        }
    }
}
