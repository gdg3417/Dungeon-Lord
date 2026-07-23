using System;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CorridorSpatialCategory { Straight = 1 }
    public enum FixedSpatialStructureKind { Entrance = 1, CompletionTerminal = 2 }

    [Serializable]
    public sealed class SpatialContentExportMetadata
    {
        public string SchemaId;
        public int SchemaVersion;
        public string ContentVersion;
    }

    [Serializable]
    public sealed class SpatialSocketTypeDefinition
    {
        public string SocketTypeId;
        public string[] CompatibleSocketTypeIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SpatialConnectionPointDefinition
    {
        public string ConnectionPointId;
        public TileCoordinate Offset;
        public CardinalOrientation Facing;
        public string SocketTypeId;
    }

    [Serializable]
    public sealed class FixedSpatialStructureDefinition
    {
        public string StructureDefinitionId;
        public string LocalizationKey;
        public FixedSpatialStructureKind Kind;
        public RectangularFootprintDefinition GrossFootprint;
        public TileCoordinate[] ReservedTileOffsets = Array.Empty<TileCoordinate>();
        public CardinalOrientation[] AllowedOrientations = Array.Empty<CardinalOrientation>();
        public SpatialConnectionPointDefinition[] ConnectionPoints = Array.Empty<SpatialConnectionPointDefinition>();
        public int MaximumConnectionCount;
    }

    [Serializable]
    public sealed class SpatialContentCatalog
    {
        // Definition kinds intentionally own separate ordinal ID namespaces. Connection-point IDs are
        // unique within their owning structure; socket references use the catalog-wide socket namespace.
        public SpatialContentExportMetadata Metadata;
        public FloorSpatialConfiguration[] Floors = Array.Empty<FloorSpatialConfiguration>();
        public RoomSpatialDefinition[] Rooms = Array.Empty<RoomSpatialDefinition>();
        public CorridorSpatialDefinition[] Corridors = Array.Empty<CorridorSpatialDefinition>();
        public FixedSpatialStructureDefinition[] FixedStructures = Array.Empty<FixedSpatialStructureDefinition>();
        public SpatialSocketTypeDefinition[] SocketTypes = Array.Empty<SpatialSocketTypeDefinition>();
    }

    public readonly struct SpatialContentValidationWorkloadLimits
    {
        public SpatialContentValidationWorkloadLimits(int maximumTopLevelRecords, int maximumNestedRecords,
            int maximumMaterializedTiles, int maximumIssues)
        {
            MaximumTopLevelRecords = maximumTopLevelRecords;
            MaximumNestedRecords = maximumNestedRecords;
            MaximumMaterializedTiles = maximumMaterializedTiles;
            MaximumIssues = maximumIssues;
        }
        public int MaximumTopLevelRecords { get; }
        public int MaximumNestedRecords { get; }
        public int MaximumMaterializedTiles { get; }
        public int MaximumIssues { get; }
        public bool IsValid => MaximumTopLevelRecords > 0 && MaximumNestedRecords > 0 &&
            MaximumMaterializedTiles > 0 && MaximumIssues > 0;
    }
}
