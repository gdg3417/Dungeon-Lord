using System;
using System.Globalization;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class CorridorContentMutationService
    {
        internal const string InvalidTargetReason = "corridor.content.invalid_target";
        internal const string CategoryReason = "corridor.content.category_not_allowed";
        internal const string TileReason = "corridor.content.invalid_tile";
        internal const string OccupiedReason = "corridor.content.tile_occupied";
        internal const string CapacityReason = "corridor.content.capacity_exceeded";
        internal const string LootTerminalReason = "corridor.content.loot_requires_terminal_tile";

        internal static string Acquire(DetachedCanonicalSpatialSaveState spatial,
            CorridorContentAuthority corridor, string categoryId, string optionId,
            string floorInstanceId, string optionalBranchId, TileCoordinate tile,
            ProductionSpatialContentSnapshot production)
        {
            if (!TryTarget(spatial, corridor, categoryId, optionId, floorInstanceId,
                    optionalBranchId, tile, production, out SavedSpatialFloor floor,
                    out FloorRouteEdge edge, out string reason)) return reason;
            long sequence = floor.RoomContents.NextSequence;
            if (sequence < 0 || sequence == long.MaxValue) return InvalidTargetReason;
            string shortCategory = categoryId == CanonicalSpatialSaveContracts.TrapCategoryId
                ? "trap" : "loot";
            string assignmentId = edge.EdgeId + ".content." + shortCategory + "." +
                sequence.ToString("D4", CultureInfo.InvariantCulture);
            if (AllIds(spatial, corridor).Contains(assignmentId)) return InvalidTargetReason;
            floor.RoomContents.NextSequence = sequence + 1L;
            corridor.Assignments = corridor.Assignments.Concat(new[] { new CorridorContentAssignment
            {
                AssignmentId = assignmentId, CategoryId = categoryId, OptionId = optionId,
                Sequence = sequence, FloorInstanceId = floor.FloorInstanceId,
                OptionalBranchId = edge.OptionalBranchId, EdgeId = edge.EdgeId, Tile = tile
            }}).ToArray();
            return null;
        }

        internal static string Redeploy(DetachedCanonicalSpatialSaveState spatial,
            CorridorContentAuthority corridor, string assignmentId, string floorInstanceId,
            string optionalBranchId, TileCoordinate tile, ProductionSpatialContentSnapshot production)
        {
            ReturnedStructuralContent[] matches = (spatial.LifecycleAndOwnership?.ReturnedContents ??
                Array.Empty<ReturnedStructuralContent>()).Where(value => value != null &&
                value.AssignmentId == assignmentId).ToArray();
            if (matches.Length != 1) return DetachedCanonicalSpatialMutation.ReturnedItemMissingReason;
            ReturnedStructuralContent owned = matches[0];
            if (!TryTarget(spatial, corridor, owned.CategoryId, owned.OptionId, floorInstanceId,
                    optionalBranchId, tile, production, out SavedSpatialFloor floor,
                    out FloorRouteEdge edge, out string reason)) return reason;
            if (owned.Sequence == long.MaxValue) return InvalidTargetReason;
            corridor.Assignments = corridor.Assignments.Concat(new[] { new CorridorContentAssignment
            {
                AssignmentId = owned.AssignmentId, CategoryId = owned.CategoryId,
                OptionId = owned.OptionId, Sequence = owned.Sequence,
                FloorInstanceId = floor.FloorInstanceId, OptionalBranchId = edge.OptionalBranchId,
                EdgeId = edge.EdgeId, Tile = tile
            }}).ToArray();
            floor.RoomContents.NextSequence = Math.Max(floor.RoomContents.NextSequence,
                owned.Sequence + 1L);
            spatial.LifecycleAndOwnership.ReturnedContents = spatial.LifecycleAndOwnership.ReturnedContents
                .Where(value => value.AssignmentId != assignmentId).ToArray();
            return null;
        }

        internal static string Unassign(DetachedCanonicalSpatialSaveState spatial,
            CorridorContentAuthority corridor, string assignmentId,
            StructuralContentRemovalPolicySnapshot removalPolicy)
        {
            CorridorContentAssignment[] matches = (corridor.Assignments ??
                Array.Empty<CorridorContentAssignment>()).Where(value => value != null &&
                value.AssignmentId == assignmentId).ToArray();
            if (matches.Length == 0) return DetachedCanonicalSpatialMutation.ActiveAssignmentMissingReason;
            if (matches.Length != 1 || spatial.LifecycleAndOwnership.ReturnedContents.Any(value =>
                    value.AssignmentId == assignmentId))
                return DetachedCanonicalSpatialMutation.ValidationFailedReason;
            CorridorContentAssignment owned = matches[0];
            if (!StructuralContentRemovalPolicyAuthority.TryResolve(removalPolicy, owned.CategoryId,
                    owned.OptionId, out StructuralContentRemovalPolicy policy, out string reason))
                return reason;
            if (policy != StructuralContentRemovalPolicy.ReturnToPlayerCustody)
                return DetachedCanonicalSpatialMutation.ReturnNotPermittedReason;
            corridor.Assignments = corridor.Assignments.Where(value => value.AssignmentId != assignmentId)
                .ToArray();
            spatial.LifecycleAndOwnership.ReturnedContents = spatial.LifecycleAndOwnership.ReturnedContents
                .Concat(new[] { new ReturnedStructuralContent
                {
                    AssignmentId = owned.AssignmentId, CategoryId = owned.CategoryId,
                    OptionId = owned.OptionId, Sequence = owned.Sequence,
                    RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody
                }}).ToArray();
            return null;
        }

        private static bool TryTarget(DetachedCanonicalSpatialSaveState spatial,
            CorridorContentAuthority corridor, string categoryId, string optionId,
            string floorInstanceId, string optionalBranchId, TileCoordinate tile,
            ProductionSpatialContentSnapshot production, out SavedSpatialFloor floor,
            out FloorRouteEdge edge, out string reason)
        {
            floor = null; edge = null; reason = InvalidTargetReason;
            if (!MvpDungeonPlacements.MvpDungeonPlacementIds.TryGetCategoryForOption(optionId,
                    out string actualCategory) || actualCategory != categoryId ||
                (categoryId != CanonicalSpatialSaveContracts.TrapCategoryId &&
                 categoryId != CanonicalSpatialSaveContracts.LootNodeCategoryId))
            { reason = CategoryReason; return false; }
            floor = (spatial.Floors ?? Array.Empty<SavedSpatialFloor>()).SingleOrDefault(value =>
                value != null && value.FloorInstanceId == floorInstanceId);
            edge = (floor?.Layout?.Edges ?? Array.Empty<FloorRouteEdge>()).SingleOrDefault(value =>
                value != null && value.OptionalBranchId == optionalBranchId &&
                value.Classification == RouteClassification.Optional &&
                value.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor);
            TileCoordinate[] tiles = edge?.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>();
            if (edge == null) return false;
            FloorRouteEdge targetEdge = edge;
            if (!tiles.Contains(tile)) { reason = TileReason; return false; }
            if ((corridor.Assignments ?? Array.Empty<CorridorContentAssignment>()).Any(value => value != null &&
                    value.FloorInstanceId == floorInstanceId && value.EdgeId == targetEdge.EdgeId &&
                    value.Tile.Equals(tile)))
            { reason = OccupiedReason; return false; }
            CorridorSpatialDefinition definition = (production.Catalog.Corridors ??
                Array.Empty<CorridorSpatialDefinition>()).SingleOrDefault(value => value != null &&
                value.CorridorDefinitionId == targetEdge.CorridorDefinitionId);
            if (definition == null) return false;
            int capacity = categoryId == CanonicalSpatialSaveContracts.TrapCategoryId
                ? definition.TrapCapacity : definition.LootCapacity;
            if (corridor.Assignments.Count(value => value != null && value.EdgeId == targetEdge.EdgeId &&
                    value.CategoryId == categoryId) >= capacity)
            { reason = CapacityReason; return false; }
            if (!OptionalBranchGeometry.TryResolveSourceTile(floor, targetEdge, production.Catalog,
                    out TileCoordinate sourceTile)) return false;
            TileCoordinate terminal = tiles.OrderBy(value => Math.Abs(value.X - sourceTile.X) +
                Math.Abs(value.Y - sourceTile.Y)).Last();
            if (categoryId == CanonicalSpatialSaveContracts.LootNodeCategoryId && !tile.Equals(terminal))
            { reason = LootTerminalReason; return false; }
            reason = null; return true;
        }

        private static System.Collections.Generic.HashSet<string> AllIds(
            DetachedCanonicalSpatialSaveState spatial, CorridorContentAuthority corridor) =>
            new System.Collections.Generic.HashSet<string>((spatial.Floors ??
                Array.Empty<SavedSpatialFloor>()).Where(value => value != null).SelectMany(value =>
                value.RoomContents?.Assignments ?? Array.Empty<RoomContentAssignment>())
                .Where(value => value != null).Select(value => value.AssignmentId)
                .Concat(spatial.LifecycleAndOwnership?.ReturnedContents?.Where(value => value != null)
                    .Select(value => value.AssignmentId) ?? Enumerable.Empty<string>())
                .Concat((corridor.Assignments ?? Array.Empty<CorridorContentAssignment>())
                    .Where(value => value != null).Select(value => value.AssignmentId)), StringComparer.Ordinal);
    }
}
