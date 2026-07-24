using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // These explicit values become stable content-export diagnostics when GD65A merges.
    public enum SpatialContentValidationReason
    {
        CatalogMissing = 1,
        WorkloadLimitsInvalid = 2,
        WorkloadExceeded = 3,
        DiagnosticLimitExceeded = 4,
        MetadataMissing = 5,
        SchemaIdentityMissing = 6,
        SchemaIdentityMalformed = 7,
        SchemaVersionNonpositive = 8,
        ContentVersionMissing = 9,
        DefinitionMissing = 10,
        StableIdMissing = 11,
        DuplicateStableId = 12,
        DuplicateFloorIndex = 13,
        UnknownEnumValue = 14,
        FootprintMissing = 15,
        FootprintDimensionsInvalid = 16,
        FootprintTileCountExceeded = 17,
        ReservedTileDuplicate = 18,
        ReservedTileOutsideFootprint = 19,
        OrientationSetMissing = 20,
        OrientationDuplicate = 21,
        OrientationInvalid = 22,
        CapacityNegative = 23,
        MaximumConnectionsNegative = 24,
        MaximumConnectionsExceedPoints = 25,
        ConnectionPointSetMissing = 26,
        ConnectionPointIdDuplicate = 27,
        ConnectionPointOffsetInvalid = 28,
        ConnectionPointBoundaryInvalid = 29,
        ConnectionPointFacingInvalid = 30,
        ConnectionPointOnReservedTile = 31,
        ConnectionPointPositionDuplicate = 32,
        ForeignKeyMissing = 33,
        ForeignKeyAmbiguous = 34,
        CorridorLengthInvalid = 35,
        CorridorWidthInvalid = 36,
        CorridorMonsterCapacityInvalid = 37,
        FixedStructureKindInvalid = 38,
        LocalizationKeyMissing = 39,
        LocalizationReferenceMissing = 40,
        LocalizationLookupEntryMissing = 41,
        FloorIndexNegative = 42,
        FloorCapacityNegative = 43,
        FloorCapacityExceedsBounds = 44,
        FloorBranchAllowanceNegative = 45,
        FloorBranchAllowanceExceeded = 46
    }

    [Serializable]
    public sealed class SpatialContentValidationIssue
    {
        public SpatialContentValidationReason Reason;
        public string Path;
        public string RelatedId;
    }

    public sealed class SpatialContentValidationResult
    {
        public SpatialContentValidationResult(SpatialContentValidationIssue[] issues)
        {
            Issues = issues;
        }

        public SpatialContentValidationIssue[] Issues { get; }
        public bool IsValid => Issues.Length == 0;
    }

    public static class SpatialContentValidator
    {
        public static SpatialContentValidationResult Validate(
            SpatialContentCatalog suppliedCatalog,
            SpatialContentValidationWorkloadLimits limits,
            ISet<string> suppliedLocalizationKeys = null)
        {
            if (!limits.IsValid)
                return Single(SpatialContentValidationReason.WorkloadLimitsInvalid);
            if (suppliedCatalog == null)
                return Single(SpatialContentValidationReason.CatalogMissing);

            if (!SpatialContentWorkload.TryPreflight(suppliedCatalog, suppliedLocalizationKeys, limits))
                return Single(SpatialContentValidationReason.WorkloadExceeded);

            if (!SpatialContentCanonicalizer.TryCanonicalize(suppliedCatalog, limits, out SpatialContentCatalog catalog))
                return Single(SpatialContentValidationReason.WorkloadExceeded);

            var issues = new IssueCollector(limits.MaximumIssues);
            HashSet<string> localizationKeys = BuildLocalizationIndex(suppliedLocalizationKeys, issues);

            ValidateMetadata(catalog.Metadata, issues);

            DefinitionIndex<FloorSpatialConfiguration> floors = BuildIndex(
                catalog.Floors, value => value?.FloorDefinitionId, "floors", issues);
            DefinitionIndex<RoomSpatialDefinition> rooms = BuildIndex(
                catalog.Rooms, value => value?.RoomDefinitionId, "rooms", issues);
            DefinitionIndex<CorridorSpatialDefinition> corridors = BuildIndex(
                catalog.Corridors, value => value?.CorridorDefinitionId, "corridors", issues);
            DefinitionIndex<FixedSpatialStructureDefinition> fixedStructures = BuildIndex(
                catalog.FixedStructures, value => value?.StructureDefinitionId, "fixedStructures", issues);
            DefinitionIndex<SpatialSocketTypeDefinition> sockets = BuildIndex(
                catalog.SocketTypes, value => value?.SocketTypeId, "socketTypes", issues);

            ValidateFloors(catalog.Floors, rooms, corridors, fixedStructures, limits, issues);
            ValidateRooms(catalog.Rooms, sockets, localizationKeys, suppliedLocalizationKeys != null, limits, issues);
            ValidateCorridors(catalog.Corridors, sockets, localizationKeys, suppliedLocalizationKeys != null, issues);
            ValidateFixedStructures(catalog.FixedStructures, sockets, localizationKeys,
                suppliedLocalizationKeys != null, limits, issues);
            ValidateSockets(catalog.SocketTypes, sockets, issues);

            if (issues.Exceeded)
                return Single(SpatialContentValidationReason.DiagnosticLimitExceeded);

            return new SpatialContentValidationResult(issues.Items
                .OrderBy(issue => (int)issue.Reason)
                .ThenBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.RelatedId, StringComparer.Ordinal)
                .ToArray());
        }

        private static HashSet<string> BuildLocalizationIndex(
            ISet<string> suppliedKeys,
            IssueCollector issues)
        {
            if (suppliedKeys == null)
                return null;

            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in suppliedKeys)
            {
                if (key == null)
                {
                    issues.Add(SpatialContentValidationReason.LocalizationLookupEntryMissing,
                        "localizationKeys");
                    continue;
                }

                result.Add(key);
            }

            return result;
        }

        private static void ValidateMetadata(
            SpatialContentExportMetadata metadata,
            IssueCollector issues)
        {
            if (metadata == null)
            {
                issues.Add(SpatialContentValidationReason.MetadataMissing, "metadata");
                return;
            }

            if (string.IsNullOrWhiteSpace(metadata.SchemaId))
                issues.Add(SpatialContentValidationReason.SchemaIdentityMissing, "metadata.schemaId");
            else if (metadata.SchemaId.Any(char.IsWhiteSpace))
                issues.Add(SpatialContentValidationReason.SchemaIdentityMalformed, "metadata.schemaId");

            if (metadata.SchemaVersion <= 0)
                issues.Add(SpatialContentValidationReason.SchemaVersionNonpositive, "metadata.schemaVersion");
            if (string.IsNullOrWhiteSpace(metadata.ContentVersion))
                issues.Add(SpatialContentValidationReason.ContentVersionMissing, "metadata.contentVersion");
        }

        private static void ValidateFloors(
            FloorSpatialConfiguration[] floors,
            DefinitionIndex<RoomSpatialDefinition> rooms,
            DefinitionIndex<CorridorSpatialDefinition> corridors,
            DefinitionIndex<FixedSpatialStructureDefinition> fixedStructures,
            SpatialContentValidationWorkloadLimits limits,
            IssueCollector issues)
        {
            var floorIndexes = new HashSet<int>();
            ForEach(floors, "floors", (floor, path) =>
            {
                if (floor == null)
                    return;

                if (!floorIndexes.Add(floor.FloorIndex))
                    issues.Add(SpatialContentValidationReason.DuplicateFloorIndex, path);
                if (floor.FloorIndex < 0)
                    issues.Add(SpatialContentValidationReason.FloorIndexNegative, path + ".floorIndex");
                if (floor.FinalFloorSpaceCapacity < 0)
                    issues.Add(SpatialContentValidationReason.FloorCapacityNegative, path + ".finalFloorSpaceCapacity");
                if (floor.OptionalBranchAllowance < 0)
                    issues.Add(SpatialContentValidationReason.FloorBranchAllowanceNegative, path + ".optionalBranchAllowance");
                if (floor.OptionalBranchAllowance > 1)
                    issues.Add(SpatialContentValidationReason.FloorBranchAllowanceExceeded, path + ".optionalBranchAllowance");

                if (floor.Bounds == null)
                {
                    issues.Add(SpatialContentValidationReason.FootprintMissing, path + ".bounds");
                }
                else if (!floor.Bounds.IsValid)
                {
                    issues.Add(SpatialContentValidationReason.FootprintDimensionsInvalid, path + ".bounds");
                }
                else
                {
                    if (floor.Bounds.TileCount > limits.MaximumMaterializedTiles)
                        issues.Add(SpatialContentValidationReason.FootprintTileCountExceeded, path + ".bounds");
                    if (floor.FinalFloorSpaceCapacity > floor.Bounds.TileCount)
                        issues.Add(SpatialContentValidationReason.FloorCapacityExceedsBounds,
                            path + ".finalFloorSpaceCapacity");
                }

                ValidateReferences(floor.AllowedRoomDefinitionIds, rooms, path + ".allowedRooms", issues);
                ValidateReferences(floor.AllowedCorridorDefinitionIds, corridors,
                    path + ".allowedCorridors", issues);
                ValidateFixedReference(floor.EntranceStructureDefinitionId,
                    FixedSpatialStructureKind.Entrance, fixedStructures, path + ".entrance", issues);
                ValidateFixedReference(floor.CompletionStructureDefinitionId,
                    FixedSpatialStructureKind.CompletionTerminal, fixedStructures, path + ".completion", issues);
            });
        }

        private static void ValidateRooms(
            RoomSpatialDefinition[] rooms,
            DefinitionIndex<SpatialSocketTypeDefinition> sockets,
            HashSet<string> localizationKeys,
            bool validateLocalizationReference,
            SpatialContentValidationWorkloadLimits limits,
            IssueCollector issues)
        {
            ForEach(rooms, "rooms", (room, path) =>
            {
                if (room == null)
                    return;

                ValidateLocalization(room.LocalizationKey, localizationKeys,
                    validateLocalizationReference, path, issues);
                ValidateCapacities(room.MonsterCapacity, room.TrapCapacity, room.LootCapacity, path, issues);
                ValidateConnectableShape(room.GrossFootprint, room.ReservedTileOffsets,
                    room.AllowedOrientations, room.ConnectionPoints, room.MaximumConnectionCount,
                    sockets, limits, path, issues);
            });
        }

        private static void ValidateCorridors(
            CorridorSpatialDefinition[] corridors,
            DefinitionIndex<SpatialSocketTypeDefinition> sockets,
            HashSet<string> localizationKeys,
            bool validateLocalizationReference,
            IssueCollector issues)
        {
            ForEach(corridors, "corridors", (corridor, path) =>
            {
                if (corridor == null)
                    return;

                ValidateLocalization(corridor.LocalizationKey, localizationKeys,
                    validateLocalizationReference, path, issues);
                if (!Enum.IsDefined(typeof(CorridorSpatialCategory), corridor.Category))
                    issues.Add(SpatialContentValidationReason.UnknownEnumValue, path + ".category");
                if (corridor.MinimumLength <= 0 || corridor.MaximumLength <= 0 ||
                    corridor.MinimumLength > corridor.MaximumLength)
                    issues.Add(SpatialContentValidationReason.CorridorLengthInvalid, path + ".length");
                if (corridor.Width <= 0)
                    issues.Add(SpatialContentValidationReason.CorridorWidthInvalid, path + ".width");
                if (corridor.Category == CorridorSpatialCategory.Straight && corridor.MonsterCapacity > 0)
                    issues.Add(SpatialContentValidationReason.CorridorMonsterCapacityInvalid,
                        path + ".monsterCapacity");

                ValidateCapacities(corridor.MonsterCapacity, corridor.TrapCapacity,
                    corridor.LootCapacity, path, issues);
                ValidateOrientations(corridor.AllowedOrientations, path, issues);
                ValidateReferences(corridor.CompatibleSocketTypeIds, sockets,
                    path + ".compatibleSockets", issues);
            });
        }

        private static void ValidateFixedStructures(
            FixedSpatialStructureDefinition[] structures,
            DefinitionIndex<SpatialSocketTypeDefinition> sockets,
            HashSet<string> localizationKeys,
            bool validateLocalizationReference,
            SpatialContentValidationWorkloadLimits limits,
            IssueCollector issues)
        {
            ForEach(structures, "fixedStructures", (structure, path) =>
            {
                if (structure == null)
                    return;

                ValidateLocalization(structure.LocalizationKey, localizationKeys,
                    validateLocalizationReference, path, issues);
                if (!Enum.IsDefined(typeof(FixedSpatialStructureKind), structure.Kind))
                    issues.Add(SpatialContentValidationReason.FixedStructureKindInvalid, path + ".kind");

                ValidateConnectableShape(structure.GrossFootprint, structure.ReservedTileOffsets,
                    structure.AllowedOrientations, structure.ConnectionPoints,
                    structure.MaximumConnectionCount, sockets, limits, path, issues);
            });
        }

        private static void ValidateSockets(
            SpatialSocketTypeDefinition[] socketTypes,
            DefinitionIndex<SpatialSocketTypeDefinition> sockets,
            IssueCollector issues)
        {
            ForEach(socketTypes, "socketTypes", (socket, path) =>
            {
                if (socket != null)
                    ValidateReferences(socket.CompatibleSocketTypeIds, sockets,
                        path + ".compatible", issues);
            });
        }

        private static void ValidateConnectableShape(
            RectangularFootprintDefinition footprint,
            TileCoordinate[] reservedOffsets,
            CardinalOrientation[] orientations,
            SpatialConnectionPointDefinition[] connectionPoints,
            int maximumConnections,
            DefinitionIndex<SpatialSocketTypeDefinition> sockets,
            SpatialContentValidationWorkloadLimits limits,
            string path,
            IssueCollector issues)
        {
            ValidateOrientations(orientations, path, issues);
            if (maximumConnections < 0)
                issues.Add(SpatialContentValidationReason.MaximumConnectionsNegative,
                    path + ".maximumConnections");
            if (connectionPoints == null || connectionPoints.Length == 0)
                issues.Add(SpatialContentValidationReason.ConnectionPointSetMissing,
                    path + ".connectionPoints");

            bool validGeometry = footprint != null && footprint.Width > 0 && footprint.Height > 0;
            if (footprint == null)
                issues.Add(SpatialContentValidationReason.FootprintMissing, path + ".footprint");
            else if (!validGeometry)
                issues.Add(SpatialContentValidationReason.FootprintDimensionsInvalid, path + ".footprint");
            else if ((long)footprint.Width * footprint.Height > limits.MaximumMaterializedTiles)
                issues.Add(SpatialContentValidationReason.FootprintTileCountExceeded, path + ".footprint");

            var reserved = new HashSet<TileCoordinate>();
            ForEach(reservedOffsets, path + ".reserved", (offset, offsetPath) =>
            {
                if (!reserved.Add(offset))
                    issues.Add(SpatialContentValidationReason.ReservedTileDuplicate, offsetPath);
                if (validGeometry && !IsInside(offset, footprint))
                    issues.Add(SpatialContentValidationReason.ReservedTileOutsideFootprint, offsetPath);
            });

            var pointIds = new HashSet<string>(StringComparer.Ordinal);
            var positions = new HashSet<TileCoordinate>();
            int distinctValidPoints = 0;
            ForEach(connectionPoints, path + ".connectionPoints", (point, pointPath) =>
            {
                if (point == null)
                {
                    issues.Add(SpatialContentValidationReason.DefinitionMissing, pointPath);
                    return;
                }

                if (string.IsNullOrWhiteSpace(point.ConnectionPointId))
                    issues.Add(SpatialContentValidationReason.StableIdMissing, pointPath);
                else if (!pointIds.Add(point.ConnectionPointId))
                    issues.Add(SpatialContentValidationReason.ConnectionPointIdDuplicate,
                        pointPath, point.ConnectionPointId);

                bool uniquePosition = positions.Add(point.Offset);
                if (!uniquePosition)
                    issues.Add(SpatialContentValidationReason.ConnectionPointPositionDuplicate, pointPath);

                bool pointGeometryValid = false;
                if (validGeometry)
                {
                    if (!IsInside(point.Offset, footprint))
                        issues.Add(SpatialContentValidationReason.ConnectionPointOffsetInvalid, pointPath);
                    else if (!IsBoundary(point.Offset, footprint))
                        issues.Add(SpatialContentValidationReason.ConnectionPointBoundaryInvalid, pointPath);
                    else
                    {
                        pointGeometryValid = true;
                        if (reserved.Contains(point.Offset))
                            issues.Add(SpatialContentValidationReason.ConnectionPointOnReservedTile, pointPath);
                    }
                }

                bool facingValid = Enum.IsDefined(typeof(CardinalOrientation), point.Facing);
                if (!facingValid)
                    issues.Add(SpatialContentValidationReason.OrientationInvalid, pointPath + ".facing");
                else if (pointGeometryValid && !FacingMatchesBoundary(point.Offset, footprint, point.Facing))
                    issues.Add(SpatialContentValidationReason.ConnectionPointFacingInvalid, pointPath);

                ReferenceResolution<SpatialSocketTypeDefinition> socketResolution =
                    ResolveReference(point.SocketTypeId, sockets, pointPath + ".socket", issues);
                if (uniquePosition && pointGeometryValid && facingValid && socketResolution.IsUnique &&
                    !reserved.Contains(point.Offset) && !string.IsNullOrWhiteSpace(point.ConnectionPointId))
                    distinctValidPoints++;
            });

            if (maximumConnections >= 0 && maximumConnections > distinctValidPoints)
                issues.Add(SpatialContentValidationReason.MaximumConnectionsExceedPoints,
                    path + ".maximumConnections");
        }

        private static void ValidateOrientations(
            CardinalOrientation[] orientations,
            string path,
            IssueCollector issues)
        {
            if (orientations == null || orientations.Length == 0)
            {
                issues.Add(SpatialContentValidationReason.OrientationSetMissing, path + ".orientations");
                return;
            }

            var seen = new HashSet<CardinalOrientation>();
            ForEach(orientations, path + ".orientations", (orientation, orientationPath) =>
            {
                if (!Enum.IsDefined(typeof(CardinalOrientation), orientation))
                    issues.Add(SpatialContentValidationReason.OrientationInvalid, orientationPath);
                else if (!seen.Add(orientation))
                    issues.Add(SpatialContentValidationReason.OrientationDuplicate, orientationPath);
            });
        }

        private static void ValidateCapacities(
            int monsterCapacity,
            int trapCapacity,
            int lootCapacity,
            string path,
            IssueCollector issues)
        {
            if (monsterCapacity < 0 || trapCapacity < 0 || lootCapacity < 0)
                issues.Add(SpatialContentValidationReason.CapacityNegative, path + ".capacities");
        }

        private static void ValidateLocalization(
            string key,
            HashSet<string> localizationKeys,
            bool validateReference,
            string path,
            IssueCollector issues)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                issues.Add(SpatialContentValidationReason.LocalizationKeyMissing,
                    path + ".localizationKey");
                return;
            }

            if (validateReference && !localizationKeys.Contains(key))
                issues.Add(SpatialContentValidationReason.LocalizationReferenceMissing,
                    path + ".localizationKey", key);
        }

        private static void ValidateFixedReference(
            string id,
            FixedSpatialStructureKind requiredKind,
            DefinitionIndex<FixedSpatialStructureDefinition> index,
            string path,
            IssueCollector issues)
        {
            ReferenceResolution<FixedSpatialStructureDefinition> resolution =
                ResolveReference(id, index, path, issues);
            if (resolution.IsUnique && resolution.Value.Kind != requiredKind)
                issues.Add(SpatialContentValidationReason.FixedStructureKindInvalid, path, id);
        }

        private static void ValidateReferences<T>(
            string[] ids,
            DefinitionIndex<T> index,
            string path,
            IssueCollector issues)
        {
            ForEach(ids, path, (id, idPath) => ValidateReference(id, index, idPath, issues));
        }

        private static void ValidateReference<T>(
            string id,
            DefinitionIndex<T> index,
            string path,
            IssueCollector issues)
        {
            ResolveReference(id, index, path, issues);
        }

        private static ReferenceResolution<T> ResolveReference<T>(
            string id,
            DefinitionIndex<T> index,
            string path,
            IssueCollector issues)
        {
            if (string.IsNullOrWhiteSpace(id) || !index.Entries.TryGetValue(id, out List<T> matches))
            {
                issues.Add(SpatialContentValidationReason.ForeignKeyMissing, path, id);
                return default;
            }

            if (matches.Count != 1)
            {
                issues.Add(SpatialContentValidationReason.ForeignKeyAmbiguous, path, id);
                return default;
            }

            return new ReferenceResolution<T>(matches[0]);
        }

        private static DefinitionIndex<T> BuildIndex<T>(
            T[] values,
            Func<T, string> getId,
            string path,
            IssueCollector issues)
        {
            var entries = new Dictionary<string, List<T>>(StringComparer.Ordinal);
            ForEach(values, path, (value, valuePath) =>
            {
                if (value == null)
                {
                    issues.Add(SpatialContentValidationReason.DefinitionMissing, valuePath);
                    return;
                }

                string id = getId(value);
                if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add(SpatialContentValidationReason.StableIdMissing, valuePath);
                    return;
                }

                if (!entries.TryGetValue(id, out List<T> matches))
                {
                    matches = new List<T>();
                    entries.Add(id, matches);
                }

                matches.Add(value);
                if (matches.Count > 1)
                    issues.Add(SpatialContentValidationReason.DuplicateStableId, valuePath, id);
            });
            return new DefinitionIndex<T>(entries);
        }

        private static bool IsInside(TileCoordinate value, RectangularFootprintDefinition footprint) =>
            value.X >= 0 && value.Y >= 0 && value.X < footprint.Width && value.Y < footprint.Height;

        private static bool IsBoundary(TileCoordinate value, RectangularFootprintDefinition footprint) =>
            value.X == 0 || value.Y == 0 || value.X == footprint.Width - 1 || value.Y == footprint.Height - 1;

        private static bool FacingMatchesBoundary(
            TileCoordinate value,
            RectangularFootprintDefinition footprint,
            CardinalOrientation facing) =>
            (facing == CardinalOrientation.Zero && value.Y == footprint.Height - 1) ||
            (facing == CardinalOrientation.Ninety && value.X == footprint.Width - 1) ||
            (facing == CardinalOrientation.OneEighty && value.Y == 0) ||
            (facing == CardinalOrientation.TwoSeventy && value.X == 0);

        private static void ForEach<T>(T[] values, string path, Action<T, string> action)
        {
            if (values == null)
                return;
            for (int index = 0; index < values.Length; index++)
                action(values[index], path + "[" + index + "]");
        }

        private static SpatialContentValidationResult Single(SpatialContentValidationReason reason) =>
            new SpatialContentValidationResult(new[]
            {
                new SpatialContentValidationIssue
                {
                    Reason = reason,
                    Path = string.Empty,
                    RelatedId = string.Empty
                }
            });

        private sealed class IssueCollector
        {
            private readonly int maximumIssues;

            public IssueCollector(int maximumIssues)
            {
                this.maximumIssues = maximumIssues;
                Items = new List<SpatialContentValidationIssue>(Math.Min(maximumIssues, 16));
            }

            public List<SpatialContentValidationIssue> Items { get; }
            public bool Exceeded { get; private set; }

            public void Add(SpatialContentValidationReason reason, string path, string relatedId = "")
            {
                if (Items.Count >= maximumIssues)
                {
                    Exceeded = true;
                    return;
                }

                Items.Add(new SpatialContentValidationIssue
                {
                    Reason = reason,
                    Path = path,
                    RelatedId = relatedId ?? string.Empty
                });
            }
        }

        private sealed class DefinitionIndex<T>
        {
            public DefinitionIndex(Dictionary<string, List<T>> entries)
            {
                Entries = entries;
            }

            public Dictionary<string, List<T>> Entries { get; }
        }

        private readonly struct ReferenceResolution<T>
        {
            public ReferenceResolution(T value)
            {
                Value = value;
                IsUnique = true;
            }

            public T Value { get; }
            public bool IsUnique { get; }
        }
    }

    internal static class SpatialContentWorkload
    {
        public static bool TryPreflight(
            SpatialContentCatalog catalog,
            ISet<string> localizationKeys,
            SpatialContentValidationWorkloadLimits limits)
        {
            long topLevel = 0;
            if (!TryAdd(ref topLevel, Length(catalog.Floors), limits.MaximumTopLevelRecords) ||
                !TryAdd(ref topLevel, Length(catalog.Rooms), limits.MaximumTopLevelRecords) ||
                !TryAdd(ref topLevel, Length(catalog.Corridors), limits.MaximumTopLevelRecords) ||
                !TryAdd(ref topLevel, Length(catalog.FixedStructures), limits.MaximumTopLevelRecords) ||
                !TryAdd(ref topLevel, Length(catalog.SocketTypes), limits.MaximumTopLevelRecords))
                return false;

            long nested = 0;
            if (!TryAdd(ref nested, localizationKeys?.Count ?? 0, limits.MaximumNestedRecords))
                return false;

            long characters = 0;
            if (!TryAddString(ref characters, catalog.Metadata?.SchemaId, limits) ||
                !TryAddString(ref characters, catalog.Metadata?.ContentVersion, limits) ||
                !TryCountFloors(catalog.Floors, ref nested, ref characters, limits) ||
                !TryCountRooms(catalog.Rooms, ref nested, ref characters, limits) ||
                !TryCountCorridors(catalog.Corridors, ref nested, ref characters, limits) ||
                !TryCountFixed(catalog.FixedStructures, ref nested, ref characters, limits) ||
                !TryCountSockets(catalog.SocketTypes, ref nested, ref characters, limits))
                return false;

            if (localizationKeys == null)
                return true;

            foreach (string key in localizationKeys)
            {
                if (!TryAddString(ref characters, key, limits))
                    return false;
            }

            return true;
        }

        internal static bool TryAdd(ref long current, long additional, long maximum)
        {
            if (additional < 0 || current < 0 || maximum < 0 || current > maximum - additional)
                return false;

            current += additional;
            return true;
        }

        private static bool TryCountFloors(
            FloorSpatialConfiguration[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (FloorSpatialConfiguration value in values)
            {
                if (!TryAddString(ref characters, value?.FloorDefinitionId, limits) ||
                    !TryAddString(ref characters, value?.EntranceStructureDefinitionId, limits) ||
                    !TryAddString(ref characters, value?.CompletionStructureDefinitionId, limits) ||
                    !TryCountStrings(value?.AllowedRoomDefinitionIds, ref nested, ref characters, limits) ||
                    !TryCountStrings(value?.AllowedCorridorDefinitionIds, ref nested, ref characters, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountRooms(
            RoomSpatialDefinition[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (RoomSpatialDefinition value in values)
            {
                if (!TryAddString(ref characters, value?.RoomDefinitionId, limits) ||
                    !TryAddString(ref characters, value?.LocalizationKey, limits) ||
                    !TryAdd(ref nested, Length(value?.ReservedTileOffsets), limits.MaximumNestedRecords) ||
                    !TryAdd(ref nested, Length(value?.AllowedOrientations), limits.MaximumNestedRecords) ||
                    !TryAdd(ref nested, Length(value?.ConnectionPoints), limits.MaximumNestedRecords) ||
                    !TryCountPoints(value?.ConnectionPoints, ref characters, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountCorridors(
            CorridorSpatialDefinition[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (CorridorSpatialDefinition value in values)
            {
                if (!TryAddString(ref characters, value?.CorridorDefinitionId, limits) ||
                    !TryAddString(ref characters, value?.LocalizationKey, limits) ||
                    !TryAdd(ref nested, Length(value?.AllowedOrientations), limits.MaximumNestedRecords) ||
                    !TryCountStrings(value?.CompatibleSocketTypeIds, ref nested, ref characters, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountFixed(
            FixedSpatialStructureDefinition[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (FixedSpatialStructureDefinition value in values)
            {
                if (!TryAddString(ref characters, value?.StructureDefinitionId, limits) ||
                    !TryAddString(ref characters, value?.LocalizationKey, limits) ||
                    !TryAdd(ref nested, Length(value?.ReservedTileOffsets), limits.MaximumNestedRecords) ||
                    !TryAdd(ref nested, Length(value?.AllowedOrientations), limits.MaximumNestedRecords) ||
                    !TryAdd(ref nested, Length(value?.ConnectionPoints), limits.MaximumNestedRecords) ||
                    !TryCountPoints(value?.ConnectionPoints, ref characters, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountSockets(
            SpatialSocketTypeDefinition[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (SpatialSocketTypeDefinition value in values)
            {
                if (!TryAddString(ref characters, value?.SocketTypeId, limits) ||
                    !TryCountStrings(value?.CompatibleSocketTypeIds, ref nested, ref characters, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountPoints(
            SpatialConnectionPointDefinition[] values,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (values == null)
                return true;

            foreach (SpatialConnectionPointDefinition value in values)
            {
                if (!TryAddString(ref characters, value?.ConnectionPointId, limits) ||
                    !TryAddString(ref characters, value?.SocketTypeId, limits))
                    return false;
            }

            return true;
        }

        private static bool TryCountStrings(
            string[] values,
            ref long nested,
            ref long characters,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (!TryAdd(ref nested, Length(values), limits.MaximumNestedRecords))
                return false;
            if (values == null)
                return true;

            foreach (string value in values)
            {
                if (!TryAddString(ref characters, value, limits))
                    return false;
            }

            return true;
        }

        private static bool TryAddString(
            ref long characters,
            string value,
            SpatialContentValidationWorkloadLimits limits) =>
            TryAdd(ref characters, value?.Length ?? 0L, limits.MaximumStringCharacters);

        private static long Length(Array values) => values?.LongLength ?? 0L;
    }

    public static class SpatialContentCanonicalizer
    {
        public static bool TryCanonicalize(
            SpatialContentCatalog source,
            SpatialContentValidationWorkloadLimits limits,
            out SpatialContentCatalog result)
        {
            result = null;
            if (source == null || !limits.IsValid ||
                !SpatialContentWorkload.TryPreflight(source, null, limits))
                return false;

            result = CopyCatalog(source);
            CanonicalizeNested(result);

            SortRecords(result.Floors, value => value?.FloorDefinitionId);
            SortRecords(result.Rooms, value => value?.RoomDefinitionId);
            SortRecords(result.Corridors, value => value?.CorridorDefinitionId);
            SortRecords(result.FixedStructures, value => value?.StructureDefinitionId);
            SortRecords(result.SocketTypes, value => value?.SocketTypeId);
            return true;
        }

        private static SpatialContentCatalog CopyCatalog(SpatialContentCatalog source) =>
            new SpatialContentCatalog
            {
                Metadata = CopyMetadata(source.Metadata),
                Floors = CopyReferenceArray(source.Floors, CopyFloor),
                Rooms = CopyReferenceArray(source.Rooms, CopyRoom),
                Corridors = CopyReferenceArray(source.Corridors, CopyCorridor),
                FixedStructures = CopyReferenceArray(source.FixedStructures, CopyFixedStructure),
                SocketTypes = CopyReferenceArray(source.SocketTypes, CopySocketType)
            };

        private static SpatialContentExportMetadata CopyMetadata(SpatialContentExportMetadata source) =>
            source == null ? null : new SpatialContentExportMetadata
            {
                SchemaId = source.SchemaId,
                SchemaVersion = source.SchemaVersion,
                ContentVersion = source.ContentVersion
            };

        private static FloorSpatialConfiguration CopyFloor(FloorSpatialConfiguration source) =>
            source == null ? null : new FloorSpatialConfiguration
            {
                FloorDefinitionId = source.FloorDefinitionId,
                FloorIndex = source.FloorIndex,
                Bounds = CopyBounds(source.Bounds),
                FinalFloorSpaceCapacity = source.FinalFloorSpaceCapacity,
                OptionalBranchAllowance = source.OptionalBranchAllowance,
                AllowedRoomDefinitionIds = CopyArray(source.AllowedRoomDefinitionIds),
                AllowedCorridorDefinitionIds = CopyArray(source.AllowedCorridorDefinitionIds),
                EntranceStructureDefinitionId = source.EntranceStructureDefinitionId,
                CompletionStructureDefinitionId = source.CompletionStructureDefinitionId
            };

        private static RoomSpatialDefinition CopyRoom(RoomSpatialDefinition source) =>
            source == null ? null : new RoomSpatialDefinition
            {
                RoomDefinitionId = source.RoomDefinitionId,
                GrossFootprint = CopyFootprint(source.GrossFootprint),
                ReservedTileOffsets = CopyArray(source.ReservedTileOffsets),
                MaximumConnectionCount = source.MaximumConnectionCount,
                MonsterCapacity = source.MonsterCapacity,
                TrapCapacity = source.TrapCapacity,
                LootCapacity = source.LootCapacity,
                LocalizationKey = source.LocalizationKey,
                AllowedOrientations = CopyArray(source.AllowedOrientations),
                ConnectionPoints = CopyReferenceArray(source.ConnectionPoints, CopyConnectionPoint)
            };

        private static CorridorSpatialDefinition CopyCorridor(CorridorSpatialDefinition source) =>
            source == null ? null : new CorridorSpatialDefinition
            {
                CorridorDefinitionId = source.CorridorDefinitionId,
                LocalizationKey = source.LocalizationKey,
                Category = source.Category,
                MinimumLength = source.MinimumLength,
                MaximumLength = source.MaximumLength,
                Width = source.Width,
                MonsterCapacity = source.MonsterCapacity,
                TrapCapacity = source.TrapCapacity,
                LootCapacity = source.LootCapacity,
                AllowedOrientations = CopyArray(source.AllowedOrientations),
                CompatibleSocketTypeIds = CopyArray(source.CompatibleSocketTypeIds)
            };

        private static FixedSpatialStructureDefinition CopyFixedStructure(
            FixedSpatialStructureDefinition source) =>
            source == null ? null : new FixedSpatialStructureDefinition
            {
                StructureDefinitionId = source.StructureDefinitionId,
                LocalizationKey = source.LocalizationKey,
                Kind = source.Kind,
                GrossFootprint = CopyFootprint(source.GrossFootprint),
                ReservedTileOffsets = CopyArray(source.ReservedTileOffsets),
                AllowedOrientations = CopyArray(source.AllowedOrientations),
                ConnectionPoints = CopyReferenceArray(source.ConnectionPoints, CopyConnectionPoint),
                MaximumConnectionCount = source.MaximumConnectionCount
            };

        private static SpatialSocketTypeDefinition CopySocketType(SpatialSocketTypeDefinition source) =>
            source == null ? null : new SpatialSocketTypeDefinition
            {
                SocketTypeId = source.SocketTypeId,
                CompatibleSocketTypeIds = CopyArray(source.CompatibleSocketTypeIds)
            };

        private static SpatialConnectionPointDefinition CopyConnectionPoint(
            SpatialConnectionPointDefinition source) =>
            source == null ? null : new SpatialConnectionPointDefinition
            {
                ConnectionPointId = source.ConnectionPointId,
                Offset = source.Offset,
                Facing = source.Facing,
                SocketTypeId = source.SocketTypeId
            };

        private static RectangularFloorBounds CopyBounds(RectangularFloorBounds source) =>
            source == null ? null : new RectangularFloorBounds
            {
                Minimum = source.Minimum,
                Width = source.Width,
                Height = source.Height
            };

        private static RectangularFootprintDefinition CopyFootprint(
            RectangularFootprintDefinition source) =>
            source == null ? null : new RectangularFootprintDefinition
            {
                Width = source.Width,
                Height = source.Height
            };

        private static T[] CopyArray<T>(T[] source) =>
            source == null ? null : (T[])source.Clone();

        private static TOutput[] CopyReferenceArray<TInput, TOutput>(
            TInput[] source,
            Func<TInput, TOutput> copy)
        {
            if (source == null)
                return null;

            var result = new TOutput[source.Length];
            for (int index = 0; index < source.Length; index++)
                result[index] = copy(source[index]);
            return result;
        }

        private static void CanonicalizeNested(SpatialContentCatalog catalog)
        {
            ForEach(catalog.Floors, floor =>
            {
                SortStrings(floor.AllowedRoomDefinitionIds);
                SortStrings(floor.AllowedCorridorDefinitionIds);
            });
            ForEach(catalog.Rooms, room => CanonicalizeShape(
                room.ReservedTileOffsets, room.AllowedOrientations, room.ConnectionPoints));
            ForEach(catalog.Corridors, corridor =>
            {
                SortOrientations(corridor.AllowedOrientations);
                SortStrings(corridor.CompatibleSocketTypeIds);
            });
            ForEach(catalog.FixedStructures, structure => CanonicalizeShape(
                structure.ReservedTileOffsets, structure.AllowedOrientations, structure.ConnectionPoints));
            ForEach(catalog.SocketTypes, socket => SortStrings(socket.CompatibleSocketTypeIds));
        }

        private static void CanonicalizeShape(
            TileCoordinate[] reserved,
            CardinalOrientation[] orientations,
            SpatialConnectionPointDefinition[] points)
        {
            if (reserved != null)
                Array.Sort(reserved);
            SortOrientations(orientations);
            SortRecords(points, point => point?.ConnectionPointId);
        }

        private static void SortRecords<T>(T[] values, Func<T, string> getId) where T : class
        {
            if (values == null)
                return;

            var keyedRecords = new CanonicalRecord<T>[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                T value = values[index];
                keyedRecords[index] = new CanonicalRecord<T>(
                    value,
                    getId(value),
                    value == null ? string.Empty : CreateCanonicalPayload(value));
            }

            Array.Sort(keyedRecords, (left, right) =>
            {
                int idComparison = StringComparer.Ordinal.Compare(left.Id, right.Id);
                if (idComparison != 0)
                    return idComparison;
                return StringComparer.Ordinal.Compare(left.Payload, right.Payload);
            });

            for (int index = 0; index < values.Length; index++)
                values[index] = keyedRecords[index].Value;
        }

        private static string CreateCanonicalPayload<T>(T value) where T : class
        {
            var topology = new StringBuilder();
            AppendNullTopology(topology, value);
            topology.Append('|').Append(JsonUtility.ToJson(value));
            return topology.ToString();
        }

        private static void AppendNullTopology(StringBuilder output, object value)
        {
            if (value is FloorSpatialConfiguration floor)
            {
                AppendReference(output, floor.Bounds);
                AppendStringArray(output, floor.AllowedRoomDefinitionIds);
                AppendStringArray(output, floor.AllowedCorridorDefinitionIds);
            }
            else if (value is RoomSpatialDefinition room)
            {
                AppendReference(output, room.GrossFootprint);
                AppendArray(output, room.ReservedTileOffsets);
                AppendArray(output, room.AllowedOrientations);
                AppendReferenceArray(output, room.ConnectionPoints);
            }
            else if (value is CorridorSpatialDefinition corridor)
            {
                AppendArray(output, corridor.AllowedOrientations);
                AppendStringArray(output, corridor.CompatibleSocketTypeIds);
            }
            else if (value is FixedSpatialStructureDefinition structure)
            {
                AppendReference(output, structure.GrossFootprint);
                AppendArray(output, structure.ReservedTileOffsets);
                AppendArray(output, structure.AllowedOrientations);
                AppendReferenceArray(output, structure.ConnectionPoints);
            }
            else if (value is SpatialSocketTypeDefinition socket)
            {
                AppendStringArray(output, socket.CompatibleSocketTypeIds);
            }
            else if (value is SpatialConnectionPointDefinition point)
            {
                AppendReference(output, point.ConnectionPointId);
                AppendReference(output, point.SocketTypeId);
            }
        }

        private static void AppendReference(StringBuilder output, object value) =>
            output.Append(value == null ? '0' : '1');

        private static void AppendArray<T>(StringBuilder output, T[] values) =>
            output.Append(values == null ? "N;" : "A" + values.Length + ";");

        private static void AppendReferenceArray<T>(StringBuilder output, T[] values) where T : class
        {
            if (values == null)
            {
                output.Append("N;");
                return;
            }

            output.Append('A').Append(values.Length).Append(':');
            foreach (T value in values)
                output.Append(value == null ? '0' : '1');
            output.Append(';');
        }

        private static void AppendStringArray(StringBuilder output, string[] values)
        {
            if (values == null)
            {
                output.Append("N;");
                return;
            }

            output.Append('A').Append(values.Length).Append(':');
            foreach (string value in values)
                output.Append(value == null ? '0' : '1');
            output.Append(';');
        }

        private static void SortStrings(string[] values)
        {
            if (values != null)
                Array.Sort(values, StringComparer.Ordinal);
        }

        private static void SortOrientations(CardinalOrientation[] values)
        {
            if (values != null)
                Array.Sort(values);
        }

        private static void ForEach<T>(T[] values, Action<T> action) where T : class
        {
            if (values == null)
                return;
            foreach (T value in values)
            {
                if (value != null)
                    action(value);
            }
        }

        private readonly struct CanonicalRecord<T>
        {
            public CanonicalRecord(T value, string id, string payload)
            {
                Value = value;
                Id = id;
                Payload = payload;
            }

            public T Value { get; }
            public string Id { get; }
            public string Payload { get; }
        }
    }
}
