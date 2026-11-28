using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class CorridorRenovationConsequences
    {
        internal const string UnresolvedReason = "ui.dungeon.route.corridor_content_unresolved";
        internal const string InvalidDispositionReason = "ui.dungeon.route.corridor_disposition_invalid";

        internal sealed class Projection
        {
            internal CorridorContentAuthority Corridor;
            internal SharedBranchKnowledgeAuthority Knowledge;
            internal ReturnedStructuralContent[] Returned;
            internal readonly Dictionary<string, string> Unresolved = new(StringComparer.Ordinal);
            internal string Failure;
            internal string Reason => Failure ?? (Unresolved.Count == 0 ? null : UnresolvedReason);
        }

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

        internal static Projection Project(DetachedCompleteSaveValidationResult owned, DetachedCanonicalSpatialSaveState final,
            StructuralRouteRenovation[] dispositions, ProductionSpatialContentSnapshot production, RunSimulationConfig configuration,
            StructuralContentRemovalPolicySnapshot removalPolicy, CanonicalSpatialSaveWorkloadLimits limits)
        {
            var result = new Projection
            {
                Corridor = PhaseFiveSaveContracts.Canonicalize(owned.CorridorContent),
                Knowledge = PhaseFiveSaveContracts.Canonicalize(owned.BranchKnowledge)
            };
            var assignments = result.Corridor.Assignments.ToDictionary(item => item.AssignmentId, StringComparer.Ordinal);
            var decisions = new Dictionary<string, StructuralRouteRenovation>(StringComparer.Ordinal);
            foreach (var intent in dispositions)
            {
                if (intent?.HasExactPayload != true || !intent.IsCorridorDisposition ||
                    intent.Kind == StructuralRouteRenovationKind.CancelCorridorDisposition ||
                    !assignments.TryGetValue(intent.TargetId, out var item) || item.FloorInstanceId != intent.FloorInstanceId ||
                    !decisions.TryAdd(intent.TargetId, intent))
                {
                    result.Failure = InvalidDispositionReason;
                    return result;
                }
            }

            var spatial = new DetachedCanonicalSpatialSaveState
            {
                Floors = final.Floors,
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    ReturnedContents = final.LifecycleAndOwnership.ReturnedContents
                }
            };
            var displaced = new HashSet<string>(Displaced(owned, final, production)
                .Select(item => item.AssignmentId), StringComparer.Ordinal);
            foreach (var item in assignments.Values.Where(item => displaced.Contains(item.AssignmentId) &&
                !decisions.ContainsKey(item.AssignmentId)))
                result.Unresolved.Add(item.AssignmentId, UnresolvedReason);

            // Returns and moves release their original slots before any destination is tested.
            // Unresolved assets remain owned by the canonical predecessor until publication.
            foreach (var intent in decisions.Values.Where(intent => intent.Kind == StructuralRouteRenovationKind.ReturnCorridorContent))
            {
                string failure = CorridorContentMutationService.Unassign(spatial, result.Corridor, intent.TargetId, removalPolicy);
                if (failure != null) result.Unresolved.Add(intent.TargetId, failure);
            }
            result.Corridor.Assignments = result.Corridor.Assignments.Where(item =>
                !displaced.Contains(item.AssignmentId) &&
                (!decisions.TryGetValue(item.AssignmentId, out var decision) ||
                    decision.Kind != StructuralRouteRenovationKind.ReassignCorridorContent)).ToArray();
            foreach (var intent in decisions.Values.Where(intent => intent.Kind == StructuralRouteRenovationKind.ReassignCorridorContent)
                .OrderBy(intent => intent.TargetId, StringComparer.Ordinal))
            {
                string failure = CorridorContentMutationService.PlaceExisting(spatial, result.Corridor, assignments[intent.TargetId],
                    intent.FloorInstanceId, intent.DestinationNodeId, intent.Anchor, production);
                if (failure != null) result.Unresolved.Add(intent.TargetId, failure);
            }

            result.Returned = spatial.LifecycleAndOwnership.ReturnedContents;
            result.Corridor = PhaseFiveSaveContracts.Canonicalize(result.Corridor);
            result.Knowledge.Records = result.Knowledge.Records.Where(record => final.Floors.Any(floor =>
                floor.FloorInstanceId == record.FloorInstanceId && floor.Layout.Edges.Any(edge =>
                    edge.EdgeId == record.EdgeId && edge.OptionalBranchId == record.OptionalBranchId))).ToArray();
            if (!PhaseFiveSaveContracts.Validate(result.Corridor, result.Knowledge, spatial, production, configuration, limits))
                result.Failure = InvalidDispositionReason;
            return result;
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
