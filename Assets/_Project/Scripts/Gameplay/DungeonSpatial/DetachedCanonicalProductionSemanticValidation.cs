using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal enum DetachedCanonicalProductionSemanticIssue
    {
        InvalidContext, FloorConfiguration, FloorLayout, RoomDefinition, CorridorDefinition,
        FixedStructure, AssignmentOption, AssignmentCategory, RoomCapacity
    }

    internal sealed class DetachedCanonicalProductionSemanticValidationResult
    {
        internal DetachedCanonicalProductionSemanticValidationResult(IEnumerable<DetachedCanonicalProductionSemanticIssue> issues)
        { Issues = issues.Distinct().OrderBy(value => (int)value).ToArray(); }
        internal DetachedCanonicalProductionSemanticIssue[] Issues { get; }
        internal bool IsValid => Issues.Length == 0;
    }

    internal static class DetachedCanonicalProductionSemanticValidation
    {
        internal static DetachedCanonicalProductionSemanticValidationResult Validate(
            DetachedCanonicalSpatialSaveState state, ProductionSpatialContentSnapshot production,
            RunSimulationConfig configuration, CanonicalSpatialSaveWorkloadLimits limits)
        {
            var issues = new List<DetachedCanonicalProductionSemanticIssue>();
            if (state == null || production == null || configuration == null || !limits.IsValid)
                return Result(DetachedCanonicalProductionSemanticIssue.InvalidContext);
            SpatialContentCatalog catalog = production.Catalog;
            var configured = new HashSet<string>((configuration.MvpPlacementEffects ??
                Array.Empty<MvpPlacementEffectConfig>()).Where(value => value != null)
                .Select(value => value.OptionId), StringComparer.Ordinal);
            foreach (SavedSpatialFloor floor in state.Floors ?? Array.Empty<SavedSpatialFloor>())
            {
                if (floor == null) { issues.Add(DetachedCanonicalProductionSemanticIssue.FloorConfiguration); continue; }
                FloorSpatialConfiguration[] floorMatches = (catalog.Floors ?? Array.Empty<FloorSpatialConfiguration>())
                    .Where(value => value != null && value.FloorDefinitionId == floor.FloorDefinitionId &&
                        value.FloorIndex == floor.FloorIndex).ToArray();
                if (floorMatches.Length != 1) { issues.Add(DetachedCanonicalProductionSemanticIssue.FloorConfiguration); continue; }
                FloorSpatialConfiguration floorDefinition = floorMatches[0];
                var allowedRooms = new HashSet<string>(floorDefinition.AllowedRoomDefinitionIds ?? Array.Empty<string>(), StringComparer.Ordinal);
                var allowedCorridors = new HashSet<string>(floorDefinition.AllowedCorridorDefinitionIds ?? Array.Empty<string>(), StringComparer.Ordinal);
                var rooms = (catalog.Rooms ?? Array.Empty<RoomSpatialDefinition>()).Where(value => value != null).ToArray();
                var corridors = (catalog.Corridors ?? Array.Empty<CorridorSpatialDefinition>()).Where(value => value != null).ToArray();
                if (!FloorLayoutValidator.Validate(floor.Layout, floorDefinition, rooms, corridors,
                    new SpatialValidationWorkloadLimits(limits.MaximumMaterializedTiles)).IsValid)
                    issues.Add(DetachedCanonicalProductionSemanticIssue.FloorLayout);
                var roomByInstance = new Dictionary<string, RoomSpatialDefinition>(StringComparer.Ordinal);
                foreach (RoomSpatialInstance room in floor.Layout?.Rooms ?? Array.Empty<RoomSpatialInstance>())
                {
                    RoomSpatialDefinition[] matches = rooms.Where(value => value.RoomDefinitionId == room?.RoomDefinitionId).ToArray();
                    if (room == null || matches.Length != 1 || !allowedRooms.Contains(room.RoomDefinitionId) ||
                        !(matches[0].AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(room.Orientation))
                    { issues.Add(DetachedCanonicalProductionSemanticIssue.RoomDefinition); continue; }
                    roomByInstance[room.RoomInstanceId] = matches[0];
                }
                foreach (FloorRouteEdge edge in floor.Layout?.Edges ?? Array.Empty<FloorRouteEdge>())
                    if (edge != null && edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
                        !allowedCorridors.Contains(edge.CorridorDefinitionId))
                        issues.Add(DetachedCanonicalProductionSemanticIssue.CorridorDefinition);
                ValidateFixed(floor, floorDefinition, catalog, limits, issues);
                ValidateAssignments(floor, roomByInstance, configured, issues);
            }
            return new DetachedCanonicalProductionSemanticValidationResult(issues);
        }

        private static void ValidateFixed(SavedSpatialFloor floor, FloorSpatialConfiguration floorDefinition,
            SpatialContentCatalog catalog, CanonicalSpatialSaveWorkloadLimits limits,
            ICollection<DetachedCanonicalProductionSemanticIssue> issues)
        {
            SavedFixedSpatialStructure[] values = floor.FixedStructures ?? Array.Empty<SavedFixedSpatialStructure>();
            bool boundsValid = floorDefinition.Bounds != null && floorDefinition.Bounds.IsValid;
            foreach (SavedFixedSpatialStructure value in values)
            {
                FixedSpatialStructureDefinition[] matches = (catalog.FixedStructures ??
                    Array.Empty<FixedSpatialStructureDefinition>()).Where(item => item != null &&
                    item.StructureDefinitionId == value?.FixedStructureDefinitionId).ToArray();
                if (value == null || matches.Length != 1 || matches[0].Kind != value.Kind ||
                    value.FloorInstanceId != floor.FloorInstanceId ||
                    !(matches[0].AllowedOrientations ?? Array.Empty<CardinalOrientation>()).Contains(value.Orientation))
                { issues.Add(DetachedCanonicalProductionSemanticIssue.FixedStructure); continue; }
                if (!boundsValid || !TileFootprintResolver.TryResolveRectangle(matches[0].GrossFootprint,
                        value.Anchor, value.Orientation,
                        new SpatialValidationWorkloadLimits(limits.MaximumMaterializedTiles),
                        out ResolvedTileFootprint footprint) || footprint?.OccupiedTiles == null ||
                    footprint.OccupiedTiles.Any(tile => !floorDefinition.Bounds.Contains(tile)))
                    issues.Add(DetachedCanonicalProductionSemanticIssue.FixedStructure);
            }
            SavedFixedSpatialStructure[] entrances = values.Where(value => value != null &&
                value.Kind == FixedSpatialStructureKind.Entrance).ToArray();
            SavedFixedSpatialStructure[] completions = values.Where(value => value != null &&
                value.Kind == FixedSpatialStructureKind.CompletionTerminal).ToArray();
            if (values.Length != 2 || entrances.Length != 1 || completions.Length != 1 ||
                entrances[0].FixedStructureDefinitionId != floorDefinition.EntranceStructureDefinitionId ||
                completions[0].FixedStructureDefinitionId != floorDefinition.CompletionStructureDefinitionId)
                issues.Add(DetachedCanonicalProductionSemanticIssue.FixedStructure);
        }

        private static void ValidateAssignments(SavedSpatialFloor floor,
            IReadOnlyDictionary<string, RoomSpatialDefinition> rooms, HashSet<string> configured,
            ICollection<DetachedCanonicalProductionSemanticIssue> issues)
        {
            RoomContentAssignment[] assignments = floor.RoomContents?.Assignments ?? Array.Empty<RoomContentAssignment>();
            foreach (RoomContentAssignment value in assignments)
            {
                if (value == null || !MvpDungeonPlacementIds.TryGetCategoryForOption(value.OptionId, out string category))
                { issues.Add(DetachedCanonicalProductionSemanticIssue.AssignmentOption); continue; }
                if (category != value.CategoryId) issues.Add(DetachedCanonicalProductionSemanticIssue.AssignmentCategory);
                if (!configured.Contains(value.OptionId)) issues.Add(DetachedCanonicalProductionSemanticIssue.AssignmentOption);
            }
            foreach (KeyValuePair<string, RoomSpatialDefinition> room in rooms)
            {
                if (assignments.Count(value => value?.RoomInstanceId == room.Key && value.CategoryId ==
                        MvpDungeonPlacementIds.MonsterCategoryId) > room.Value.MonsterCapacity ||
                    assignments.Count(value => value?.RoomInstanceId == room.Key && value.CategoryId ==
                        MvpDungeonPlacementIds.TrapCategoryId) > room.Value.TrapCapacity ||
                    assignments.Count(value => value?.RoomInstanceId == room.Key && value.CategoryId ==
                        MvpDungeonPlacementIds.LootNodeCategoryId) > room.Value.LootCapacity)
                    issues.Add(DetachedCanonicalProductionSemanticIssue.RoomCapacity);
            }
        }

        private static DetachedCanonicalProductionSemanticValidationResult Result(
            DetachedCanonicalProductionSemanticIssue issue) =>
            new DetachedCanonicalProductionSemanticValidationResult(new[] { issue });
    }
}
