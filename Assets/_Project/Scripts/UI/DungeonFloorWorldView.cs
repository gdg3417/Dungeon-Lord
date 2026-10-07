using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonBuilder.M0
{
    /// <summary>Presentation only. Tilemaps and sprite views never supply placement or simulation state.</summary>
    public sealed class DungeonFloorWorldView : MonoBehaviour
    {
        private Tilemap rooms, fixedStructures, corridors, grid, invalidRooms, selectedRoom;
        private DungeonDraftInvalidMovement[] invalidIntents = Array.Empty<DungeonDraftInvalidMovement>();
        private int maximumTiles;
        public int InvalidFootprintTileCount { get; private set; }
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly List<RoomContentAssignment> identities = new List<RoomContentAssignment>();
        private SavedSpatialFloor floor;
        private ProductionSpatialContentSnapshot production;
        private DungeonPresentationPolicy policy;
        private RoomContentSpatialOccupancySnapshot occupancy;
        private Sprite sprite;
        private readonly List<Tile> ownedTiles = new List<Tile>();
        private Tile roomSelectionTile, invalidIntentTile;
        private SpriteRenderer highlight;
        public Rect Bounds { get; private set; }
        public int ActiveEntityCount => identities.Count;
        public bool GridVisible => grid != null && grid.gameObject.activeSelf;
        public bool PreviewVisible => highlight != null && highlight.gameObject.activeSelf;
        public string FloorInstanceId => floor?.FloorInstanceId;

        public void Initialize(DungeonPresentationPolicy policy)
        {
            this.policy = policy;
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
                Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
            gameObject.AddComponent<Grid>();
            rooms = Layer("Rooms", 0); fixedStructures = Layer("Fixed", 0);
            corridors = Layer("Corridors", 0); grid = Layer("EditorGrid", -1);
            invalidRooms = Layer("InvalidRoomIntents", 1); selectedRoom = Layer("RoomSelection", 1);
            highlight = Entity("Selection", 3);
            ClearPreview();
        }
        private Tilemap Layer(string name, int order)
        {
            var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            child.GetComponent<TilemapRenderer>().sortingOrder = order;
            return child.GetComponent<Tilemap>();
        }
        private SpriteRenderer Entity(string name, int order)
        {
            var child = new GameObject(name, typeof(SpriteRenderer)); child.transform.SetParent(transform, false);
            child.layer = gameObject.layer;
            var renderer = child.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            return renderer;
        }
        private Tile Tile(Color color, float size)
        {
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite; tile.color = color;
            tile.transform = Matrix4x4.Scale(new Vector3(size, size, 1)); ownedTiles.Add(tile); return tile;
        }
        public void Render(SavedSpatialFloor selected, ProductionSpatialContentSnapshot production,
            RoomContentSpatialOccupancySnapshot occupancy, int maximumTiles, bool edit)
        {
            floor = selected; this.production = production; this.occupancy = occupancy;
            this.maximumTiles = maximumTiles; invalidIntents = Array.Empty<DungeonDraftInvalidMovement>(); invalidRooms.ClearAllTiles(); InvalidFootprintTileCount = 0;
            rooms.ClearAllTiles(); fixedStructures.ClearAllTiles(); corridors.ClearAllTiles(); grid.ClearAllTiles();
            foreach (Tile tile in ownedTiles) Destroy(tile); ownedTiles.Clear(); identities.Clear();
            ClearPreview();
            foreach (var view in pool) view.gameObject.SetActive(false);
            if (selected == null) { Bounds = new Rect(0, 0, 1, 1); return; }
            roomSelectionTile = Tile(policy.ValidColor, policy.TileSize); invalidIntentTile = Tile(policy.InvalidColor, policy.TileSize);
            var limits = new SpatialValidationWorkloadLimits(maximumTiles);
            var visible = new HashSet<TileCoordinate>();
            Tile roomTile = Tile(policy.RoomColor, policy.TileSize);
            foreach (RoomSpatialInstance room in selected.Layout.Rooms)
            {
                var definition = production.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
                if (definition.TryResolveGrossTiles(room.Anchor, room.Orientation, limits, out var footprint))
                    Draw(rooms, footprint.OccupiedTiles, roomTile, visible);
            }
            Tile fixedTile = Tile(policy.FixedColor, policy.TileSize);
            foreach (SavedFixedSpatialStructure value in selected.FixedStructures)
            {
                var definition = production.Catalog.FixedStructures.Single(d => d.StructureDefinitionId == value.FixedStructureDefinitionId);
                if (TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint, value.Anchor,
                    value.Orientation, limits, out var footprint)) Draw(fixedStructures, footprint.OccupiedTiles, fixedTile, visible);
            }
            Tile corridorTile = Tile(policy.CorridorColor, policy.TileSize);
            foreach (FloorRouteEdge edge in selected.Layout.Edges)
                if (edge.Footprint?.OccupiedTiles != null) Draw(corridors, edge.Footprint.OccupiedTiles, corridorTile, visible);
            // Canonical tile centers are integer cells plus one half presentation unit.
            Tile gridTile = Tile(policy.GridColor, 1);
            foreach (TileCoordinate tile in visible) grid.SetTile(new Vector3Int(tile.X, tile.Y, 0), gridTile);
            Bounds = visible.Count == 0 ? new Rect(0, 0, 1, 1) : new Rect(visible.Min(t => t.X), visible.Min(t => t.Y),
                visible.Max(t => t.X) - visible.Min(t => t.X) + 1, visible.Max(t => t.Y) - visible.Min(t => t.Y) + 1);
            foreach (RoomContentAssignment assignment in CanonicalSpatialSaveContracts.CanonicalOrderAssignments(selected.RoomContents.Assignments))
            {
                if (!Position(assignment, assignment.RoomLocalPosition, out var tile)) continue;
                int index = identities.Count; if (index == pool.Count) pool.Add(Entity("Content", 2));
                var view = pool[index]; identities.Add(assignment); view.gameObject.SetActive(true);
                view.transform.localPosition = new Vector3(tile.X + 0.5f, tile.Y + 0.5f);
                bool trap = assignment.CategoryId == CanonicalSpatialSaveContracts.TrapCategoryId;
                bool loot = assignment.CategoryId == CanonicalSpatialSaveContracts.LootNodeCategoryId;
                view.transform.localScale = new Vector3(policy.EntitySize, policy.EntitySize * (loot ? 0.55f : 1), 1);
                view.transform.localRotation = Quaternion.Euler(0, 0, trap ? 45 : 0);
                view.color = trap ? policy.TrapColor : loot ? policy.LootColor : policy.MonsterColor;
            }
            SetEdit(edit);
        }
        private static void Draw(Tilemap map, IEnumerable<TileCoordinate> tiles, Tile tile, HashSet<TileCoordinate> visible)
        { foreach (var cell in tiles) { map.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile); visible.Add(cell); } }
        private bool Position(RoomContentAssignment assignment, TileCoordinate local, out TileCoordinate tile)
        {
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == assignment.RoomInstanceId);
            var definition = production.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
            return RoomLocalCoordinateTransform.TryToFloor(local, definition.GrossFootprint, room.Anchor, room.Orientation, out tile);
        }
        public RoomContentAssignment Select(TileCoordinate cell)
        {
            foreach (var assignment in identities)
                if (RoomContentSpatialOccupancyAuthority.TryResolve(occupancy, assignment.CategoryId, assignment.OptionId, out var rule))
                    foreach (var offset in rule.OccupiedTileOffsets)
                        if (Position(assignment, new TileCoordinate(assignment.RoomLocalPosition.X + offset.X,
                            assignment.RoomLocalPosition.Y + offset.Y), out var tile) && tile.Equals(cell)) return assignment;
            return null;
        }
        public void PresentRoomIntents(DungeonDraftInvalidMovement[] values)
        {
            invalidIntents = (values ?? Array.Empty<DungeonDraftInvalidMovement>()).Where(v => v.FloorInstanceId == floor?.FloorInstanceId)
                .OrderBy(v => v.RoomInstanceId, StringComparer.Ordinal).ToArray();
            invalidRooms.ClearAllTiles();
            InvalidFootprintTileCount = 0;
            if (floor == null || invalidIntents.Length == 0) return;
            var tile = invalidIntentTile;
            var cells = new HashSet<TileCoordinate>();
            foreach (var value in invalidIntents)
                foreach (var cell in Footprint(value.RoomDefinitionId, value.RequestedAnchor, value.Orientation))
                    if (cells.Add(cell))
                    {
                        invalidRooms.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile);
                        float minX = Mathf.Min(Bounds.xMin, cell.X), minY = Mathf.Min(Bounds.yMin, cell.Y);
                        Bounds = Rect.MinMaxRect(minX, minY, Mathf.Max(Bounds.xMax, cell.X + 1f), Mathf.Max(Bounds.yMax, cell.Y + 1f));
                    }
            InvalidFootprintTileCount = cells.Count;
        }
        private TileCoordinate[] Footprint(string definitionId, TileCoordinate anchor, CardinalOrientation orientation)
        {
            var definition = production.Catalog.Rooms.Single(d => d.RoomDefinitionId == definitionId);
            return definition.TryResolveGrossTiles(anchor, orientation, new SpatialValidationWorkloadLimits(maximumTiles), out var footprint)
                ? footprint.OccupiedTiles : Array.Empty<TileCoordinate>();
        }
        // Interaction precedence: exact content occupancy, then invalid room attempt, then
        // valid room footprint. Room ties resolve by ordinal stable identity, never objects.
        public string SelectRoom(TileCoordinate cell)
        {
            if (floor == null) return null;
            foreach (var value in invalidIntents)
                if (Footprint(value.RoomDefinitionId, value.RequestedAnchor, value.Orientation).Contains(cell)) return value.RoomInstanceId;
            foreach (var room in floor.Layout.Rooms.OrderBy(r => r.RoomInstanceId, StringComparer.Ordinal))
            {
                var semantics = floor.RoomContents.RoomSemantics.SingleOrDefault(s => s.RoomInstanceId == room.RoomInstanceId);
                if (semantics == null || semantics.LegacyRoomOriginKind == LegacyRoomOriginKind.ImplicitCompatibilityContainer) continue;
                var node = floor.Layout.Nodes.Single(n => n.RoomInstanceId == room.RoomInstanceId);
                if (!floor.Layout.Edges.Any(e => e.Classification == RouteClassification.Required && e.DestinationNodeId == node.NodeId) ||
                    !floor.Layout.Edges.Any(e => e.Classification == RouteClassification.Required && e.SourceNodeId == node.NodeId)) continue;
                if (Footprint(room.RoomDefinitionId, room.Anchor, room.Orientation).Contains(cell)) return room.RoomInstanceId;
            }
            return null;
        }
        public void SelectRoomFootprint(string roomId)
        {
            selectedRoom.ClearAllTiles(); if (roomId == null || floor == null) return;
            // The attempted footprint already identifies an invalid selected room. Do not
            // paint its last valid projection over the invalid overlap being corrected.
            if (invalidIntents.Any(value => value.RoomInstanceId == roomId)) return;
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == roomId);
            var tile = roomSelectionTile;
            foreach (var cell in Footprint(room.RoomDefinitionId, room.Anchor, room.Orientation))
                selectedRoom.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile);
        }
        public bool TryRoomLocal(RoomContentAssignment assignment, TileCoordinate cell, out TileCoordinate local)
        {
            local = default;
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == assignment.RoomInstanceId);
            var definition = production.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
            var shape = definition.GrossFootprint;
            bool rotated = room.Orientation == CardinalOrientation.Ninety || room.Orientation == CardinalOrientation.TwoSeventy;
            return RoomLocalCoordinateTransform.TryFromOriented(new TileCoordinate(cell.X - room.Anchor.X, cell.Y - room.Anchor.Y),
                rotated ? new RectangularFootprintDefinition(shape.Height, shape.Width) : shape, room.Orientation, out local);
        }
        public void Preview(TileCoordinate cell, bool valid)
        {
            highlight.gameObject.SetActive(true); highlight.color = valid ? policy.ValidColor : policy.InvalidColor;
            highlight.transform.localPosition = new Vector3(cell.X + 0.5f, cell.Y + 0.5f);
            highlight.transform.localScale = new Vector3(policy.PreviewSize, policy.PreviewSize, 1);
        }
        public void ClearPreview() { if (highlight != null) highlight.gameObject.SetActive(false); selectedRoom?.ClearAllTiles(); }
        public void SetEdit(bool edit) { grid.gameObject.SetActive(edit); }
        private void OnDestroy()
        { foreach (Tile tile in ownedTiles) Destroy(tile); if (sprite != null) Destroy(sprite); }
    }
}
