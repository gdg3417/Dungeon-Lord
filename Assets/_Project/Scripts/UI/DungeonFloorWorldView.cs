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
        private Tilemap rooms, fixedStructures, corridors, grid, invalidRooms, selectedRoom, moveAnchors, constructionPreview, invalidConstruction, perimeter, surrounding;
        private readonly List<SpriteRenderer> fixedPool = new List<SpriteRenderer>();
        private readonly Dictionary<(Sprite, Color, float), Tile> cachedTiles = new Dictionary<(Sprite, Color, float), Tile>();
        public int ReconstructionCount { get; private set; }
        public int PooledEntityCount => pool.Count + fixedPool.Count;
        private readonly SpriteRenderer[] floorBoundary = new SpriteRenderer[4];
        private Texture2D anchorTexture;
        private Sprite anchorSprite;
        public Rect LegalBounds { get; private set; }
        public int GridTileCount { get; private set; }
        public TileCoordinate[] MoveGuidanceAnchors { get; private set; } = Array.Empty<TileCoordinate>();
        private DungeonDraftInvalidMovement[] invalidIntents = Array.Empty<DungeonDraftInvalidMovement>();
        private int maximumTiles, maximumGridTiles;
        public int InvalidFootprintTileCount { get; private set; }
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly List<RoomContentAssignment> identities = new List<RoomContentAssignment>();
        private SavedSpatialFloor floor;
        private ProductionSpatialContentSnapshot production;
        private DungeonPresentationPolicy policy;
        private RoomContentSpatialOccupancySnapshot occupancy;
        private Sprite sprite;
        private readonly List<Tile> ownedTiles = new List<Tile>();
        private Tile roomSelectionTile, invalidIntentTile, moveAnchorTile;
        private SpriteRenderer highlight;
        public Rect Bounds { get; private set; }
        public int ActiveEntityCount => identities.Count;
        public bool GridVisible => grid != null && grid.gameObject.activeSelf;
        public bool PreviewVisible => highlight != null && highlight.gameObject.activeSelf;
        public int ConstructionPreviewTileCount { get; private set; }
        public string FloorInstanceId => floor?.FloorInstanceId;

        public void Initialize(DungeonPresentationPolicy policy)
        {
            this.policy = policy;
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
                Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
            gameObject.AddComponent<Grid>();
            rooms = Layer("Rooms", 0); fixedStructures = Layer("Fixed", 0);
            corridors = Layer("Corridors", 0); grid = Layer("EditorGrid", -1);
            perimeter = Layer("StonePerimeter", 1); surrounding = Layer("Surrounding", -3);
            invalidRooms = Layer("InvalidRoomIntents", 1); selectedRoom = Layer("RoomSelection", 1);
            moveAnchors = Layer("MoveAnchors", 3);
            constructionPreview = Layer("ConstructionPreview", 2); invalidConstruction = Layer("InvalidConstructionIntents", 1);
            for (int i = 0; i < floorBoundary.Length; i++) floorBoundary[i] = Entity("FloorBoundary" + i, -1);
            // A hollow diamond carries target meaning independently of marker color.
            anchorTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float distance = Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f);
                pixels[y * 32 + x] = distance >= 11 && distance <= 15 ? Color.white : Color.clear;
            }
            anchorTexture.SetPixels(pixels); anchorTexture.Apply();
            anchorSprite = Sprite.Create(anchorTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            highlight = Entity("Selection", 3);
            if (policy.Visuals != null) highlight.sprite = policy.Visuals.Selection;
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
        private Tile Tile(Color color, float size, Sprite art = null)
        {
            var key = (art != null ? art : sprite, color, size);
            if (cachedTiles.TryGetValue(key, out var cached)) return cached;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = key.Item1; tile.color = color;
            tile.transform = Matrix4x4.Scale(new Vector3(size, size, 1)); ownedTiles.Add(tile); cachedTiles.Add(key,tile); return tile;
        }
        public void Render(SavedSpatialFloor selected, ProductionSpatialContentSnapshot production,
            RoomContentSpatialOccupancySnapshot occupancy, int maximumTiles, int maximumGridTiles, bool edit)
        {
            ReconstructionCount++;
            floor = selected; this.production = production; this.occupancy = occupancy;
            this.maximumTiles = maximumTiles; invalidIntents = Array.Empty<DungeonDraftInvalidMovement>(); invalidRooms.ClearAllTiles(); InvalidFootprintTileCount = 0;
            this.maximumGridTiles = maximumGridTiles;
            rooms.ClearAllTiles(); fixedStructures.ClearAllTiles(); corridors.ClearAllTiles(); grid.ClearAllTiles();
            ClearMoveGuidance(); GridTileCount = 0;
            perimeter.ClearAllTiles(); surrounding.ClearAllTiles(); identities.Clear();
            ClearPreview();
            foreach (var view in pool) view.gameObject.SetActive(false);
            foreach (var view in fixedPool) view.gameObject.SetActive(false);
            if (selected == null) { Bounds = LegalBounds = new Rect(0, 0, 1, 1); SetEdit(false); return; }
            var art = policy.Visuals;
            roomSelectionTile = Tile(policy.ValidColor, policy.TileSize,art?.Selection); invalidIntentTile = Tile(policy.InvalidColor, policy.TileSize,art?.Invalid);
            moveAnchorTile = Tile(policy.ValidColor, policy.MoveAnchorSize,art != null ? art.Anchor : anchorSprite);
            var limits = new SpatialValidationWorkloadLimits(maximumTiles);
            var visible = new HashSet<TileCoordinate>();
            foreach (RoomSpatialInstance room in selected.Layout.Rooms)
            {
                var definition = production.Catalog.Rooms.Single(d => d.RoomDefinitionId == room.RoomDefinitionId);
                if (definition.TryResolveGrossTiles(room.Anchor, room.Orientation, limits, out var footprint))
                    foreach (var cell in footprint.OccupiedTiles)
                    {
                        var surface = art != null ? art.RoomStone[(int)((Math.Abs((long)cell.X)+Math.Abs((long)cell.Y))%art.RoomStone.Length)] : null;
                        Draw(rooms,new[] {cell},Tile(art != null ? Color.white : policy.RoomColor,policy.TileSize,surface),visible);
                    }
            }
            Tile fixedTile = Tile(art != null ? Color.white : policy.FixedColor, policy.TileSize,art?.RoomStone[0]);
            int fixedIndex=0;
            foreach (SavedFixedSpatialStructure value in selected.FixedStructures)
            {
                var definition = production.Catalog.FixedStructures.Single(d => d.StructureDefinitionId == value.FixedStructureDefinitionId);
                if (TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint, value.Anchor,
                    value.Orientation, limits, out var footprint))
                {
                    Draw(fixedStructures, footprint.OccupiedTiles, fixedTile, visible);
                    if (art != null)
                    {
                        if (fixedIndex==fixedPool.Count) fixedPool.Add(Entity("FixedIdentity",2));
                        var view=fixedPool[fixedIndex++]; view.gameObject.SetActive(true);
                        view.sprite=value.Kind==FixedSpatialStructureKind.Entrance ? art.Entrance : art.Terminal;
                        var cells=footprint.OccupiedTiles;
                        view.transform.localPosition=new Vector3((cells.Min(t=>t.X)+cells.Max(t=>t.X)+1)*.5f,(cells.Min(t=>t.Y)+cells.Max(t=>t.Y)+1)*.5f);
                        float size=Mathf.Min(cells.Max(t=>t.X)-cells.Min(t=>t.X)+1,cells.Max(t=>t.Y)-cells.Min(t=>t.Y)+1);
                        view.transform.localScale=new Vector3(size,size,1); view.color=Color.white;
                    }
                }
            }
            Tile corridorTile = Tile(art != null ? Color.white : policy.CorridorColor, policy.TileSize,art?.Corridor);
            foreach (FloorRouteEdge edge in selected.Layout.Edges)
                if (edge.Footprint?.OccupiedTiles != null) Draw(corridors, edge.Footprint.OccupiedTiles, corridorTile, visible);
            // Canonical tile centers are integer cells plus one half presentation unit.
            Bounds = visible.Count == 0 ? new Rect(0, 0, 1, 1) : new Rect(visible.Min(t => t.X), visible.Min(t => t.Y),
                visible.Max(t => t.X) - visible.Min(t => t.X) + 1, visible.Max(t => t.Y) - visible.Min(t => t.Y) + 1);
            var configured = production.Catalog.Floors.Single(d => d.FloorDefinitionId == selected.FloorDefinitionId && d.FloorIndex == selected.FloorIndex).Bounds;
            if (configured == null || !configured.IsValid || maximumGridTiles <= 0 || configured.TileCount > maximumGridTiles)
                throw new InvalidOperationException(StructuralEditService.WorkloadReason);
            LegalBounds = new Rect(configured.Minimum.X, configured.Minimum.Y, configured.Width, configured.Height);
            if (art != null)
            {
                var rock=Tile(Color.white,1,art.Surrounding);
                for(int x=configured.Minimum.X;x<(long)configured.Minimum.X+configured.Width;x++)
                    for(int y=configured.Minimum.Y;y<(long)configured.Minimum.Y+configured.Height;y++)
                        surrounding.SetTile(new Vector3Int(x,y,0),rock);
                // Low stone lips follow only exposed edges of existing constructed cells.
                // Adjacent rooms, fixed structures and authored corridors retain open visual joins.
                foreach(var cell in visible)
                {
                    int mask=0;
                    if(!visible.Contains(new TileCoordinate(cell.X,cell.Y+1))) mask|=1;
                    if(!visible.Contains(new TileCoordinate(cell.X+1,cell.Y))) mask|=2;
                    if(!visible.Contains(new TileCoordinate(cell.X,cell.Y-1))) mask|=4;
                    if(!visible.Contains(new TileCoordinate(cell.X-1,cell.Y))) mask|=8;
                    if(mask!=0) perimeter.SetTile(new Vector3Int(cell.X,cell.Y,0),Tile(Color.white,1,art.Boundaries[mask]));
                }
            }
            if (edit)
            {
                Tile gridTile = Tile(art != null ? Color.white : policy.EditorGridColor, policy.GridTileSize,art?.Grid);
                for (long x = configured.Minimum.X; x < (long)configured.Minimum.X + configured.Width; x++)
                    for (long y = configured.Minimum.Y; y < (long)configured.Minimum.Y + configured.Height; y++)
                    { grid.SetTile(new Vector3Int((int)x, (int)y, 0), gridTile); GridTileCount++; }
                Bounds = LegalBounds;
                DrawBoundary();
            }
            foreach (RoomContentAssignment assignment in CanonicalSpatialSaveContracts.CanonicalOrderAssignments(selected.RoomContents.Assignments))
            {
                if (!Position(assignment, assignment.RoomLocalPosition, out var tile)) continue;
                int index = identities.Count; if (index == pool.Count) pool.Add(Entity("Content", 2));
                var view = pool[index]; identities.Add(assignment); view.gameObject.SetActive(true);
                view.transform.localPosition = new Vector3(tile.X + 0.5f, tile.Y + 0.5f);
                bool trap = assignment.CategoryId == CanonicalSpatialSaveContracts.TrapCategoryId;
                bool loot = assignment.CategoryId == CanonicalSpatialSaveContracts.LootNodeCategoryId;
                view.sprite=art != null ? art.Resolve(assignment.CategoryId,assignment.OptionId) : sprite;
                view.transform.localScale = new Vector3(policy.EntitySize, policy.EntitySize * (art == null && loot ? 0.55f : 1), 1);
                view.transform.localRotation = Quaternion.Euler(0, 0, art == null && trap ? 45 : 0);
                view.color = art != null ? Color.white : trap ? policy.TrapColor : loot ? policy.LootColor : policy.MonsterColor;
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
        public string SelectRoom(TileCoordinate cell, bool movableOnly = true)
        {
            if (floor == null) return null;
            foreach (var value in invalidIntents)
                if (Footprint(value.RoomDefinitionId, value.RequestedAnchor, value.Orientation).Contains(cell)) return value.RoomInstanceId;
            foreach (var room in floor.Layout.Rooms.OrderBy(r => r.RoomInstanceId, StringComparer.Ordinal))
            {
                var semantics = floor.RoomContents.RoomSemantics.SingleOrDefault(s => s.RoomInstanceId == room.RoomInstanceId);
                if (movableOnly && (semantics == null || semantics.LegacyRoomOriginKind == LegacyRoomOriginKind.ImplicitCompatibilityContainer)) continue;
                var node = floor.Layout.Nodes.Single(n => n.RoomInstanceId == room.RoomInstanceId);
                if (movableOnly && (!floor.Layout.Edges.Any(e => e.Classification == RouteClassification.Required && e.DestinationNodeId == node.NodeId) ||
                    !floor.Layout.Edges.Any(e => e.Classification == RouteClassification.Required && e.SourceNodeId == node.NodeId))) continue;
                if (Footprint(room.RoomDefinitionId, room.Anchor, room.Orientation).Contains(cell)) return room.RoomInstanceId;
            }
            return null;
        }
        public string SelectCorridor(TileCoordinate cell) => floor?.Layout.Edges.Where(e=>e.Footprint?.OccupiedTiles?.Contains(cell)==true)
            .OrderBy(e=>e.EdgeId,StringComparer.Ordinal).Select(e=>e.EdgeId).FirstOrDefault();
        public void SelectRoomFootprint(string roomId)
        {
            selectedRoom.ClearAllTiles(); if (roomId == null || floor == null) return;
            // The attempted footprint already identifies an invalid selected room. Do not
            // paint its last valid projection over the invalid overlap being corrected.
            if (invalidIntents.Any(value => value.RoomInstanceId == roomId)) return;
            var room = floor.Layout.Rooms.Single(r => r.RoomInstanceId == roomId);
            SelectFootprint(Footprint(room.RoomDefinitionId,room.Anchor,room.Orientation));
        }
        public void SelectCorridorFootprint(string edgeId)
        {
            selectedRoom.ClearAllTiles();
            var edge=floor?.Layout.Edges.SingleOrDefault(e=>e.EdgeId==edgeId);
            if(edge?.Footprint?.OccupiedTiles!=null) SelectFootprint(edge.Footprint.OccupiedTiles);
        }
        private void SelectFootprint(IEnumerable<TileCoordinate> footprint)
        {
            var tile = roomSelectionTile;
            var cells=new HashSet<TileCoordinate>(footprint);
            foreach (var cell in cells)
            {
                int mask=0;
                if(!cells.Contains(new TileCoordinate(cell.X,cell.Y+1))) mask|=1;
                if(!cells.Contains(new TileCoordinate(cell.X+1,cell.Y))) mask|=2;
                if(!cells.Contains(new TileCoordinate(cell.X,cell.Y-1))) mask|=4;
                if(!cells.Contains(new TileCoordinate(cell.X-1,cell.Y))) mask|=8;
                if(policy.Visuals?.SelectedEdges?.Length==16)
                {
                    if(mask==0) continue;
                    tile=Tile(policy.SelectionColor,policy.TileSize,policy.Visuals.SelectedEdges[mask]);
                }
                selectedRoom.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile);
            }
        }
        public void SelectContent(RoomContentAssignment assignment)
        {
            if(assignment==null || !Position(assignment,assignment.RoomLocalPosition,out var cell)) return;
            Preview(cell,true); highlight.color=policy.SelectionColor;
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
            if(policy.Visuals!=null) highlight.sprite=valid ? policy.Visuals.Selection : policy.Visuals.Invalid;
            highlight.gameObject.SetActive(true); highlight.color = valid ? policy.ValidColor : policy.InvalidColor;
            highlight.transform.localPosition = new Vector3(cell.X + 0.5f, cell.Y + 0.5f);
            highlight.transform.localScale = new Vector3(policy.PreviewSize, policy.PreviewSize, 1);
        }
        public void ClearPreview() { if (highlight != null) highlight.gameObject.SetActive(false); selectedRoom?.ClearAllTiles(); constructionPreview?.ClearAllTiles(); ConstructionPreviewTileCount = 0; }

        public void PresentConstructionPreview(StructuralEditPreview preview)
        {
            constructionPreview.ClearAllTiles(); ConstructionPreviewTileCount = 0;
            if (preview == null) return;
            var cells = new HashSet<TileCoordinate>(preview.OccupiedTiles);
            if (preview.IsValid)
            {
                cells.UnionWith(preview.IncomingConnectionTiles);
                foreach (var terminal in preview.DetachedCandidate.Floors.Single(f => f.FloorInstanceId == floor.FloorInstanceId)
                    .FixedStructures.Where(s => s.Kind == FixedSpatialStructureKind.CompletionTerminal))
                {
                    var definition = production.Catalog.FixedStructures.Single(d => d.StructureDefinitionId == terminal.FixedStructureDefinitionId);
                    if (TileFootprintResolver.TryResolveRectangle(definition.GrossFootprint, terminal.Anchor, terminal.Orientation,
                        new SpatialValidationWorkloadLimits(maximumTiles), out var footprint)) cells.UnionWith(footprint.OccupiedTiles);
                }
            }
            var tile = preview.IsValid ? roomSelectionTile : invalidIntentTile;
            foreach (var cell in cells) constructionPreview.SetTile(new Vector3Int(cell.X, cell.Y, 0), tile);
            ConstructionPreviewTileCount = cells.Count;
        }

        public void PresentConstructionIntents(DungeonDraftInvalidConstruction[] values)
        {
            invalidConstruction.ClearAllTiles();
            if (floor == null) return;
            var cells = new HashSet<TileCoordinate>();
            foreach (var value in (values ?? Array.Empty<DungeonDraftInvalidConstruction>())
                .Where(v => v.Request.FloorInstanceId == floor.FloorInstanceId)
                .OrderBy(v => v.IntentId, StringComparer.Ordinal))
                foreach (var cell in Footprint(value.Request.RoomDefinitionId, value.Request.Anchor, value.Request.Orientation))
                    if (cells.Add(cell))
                    {
                        invalidConstruction.SetTile(new Vector3Int(cell.X, cell.Y, 0), invalidIntentTile);
                        float minX = Mathf.Min(Bounds.xMin, cell.X), minY = Mathf.Min(Bounds.yMin, cell.Y);
                        Bounds = Rect.MinMaxRect(minX, minY, Mathf.Max(Bounds.xMax, cell.X + 1f), Mathf.Max(Bounds.yMax, cell.Y + 1f));
                    }
        }
        private void DrawBoundary()
        {
            for (int i = 0; i < floorBoundary.Length; i++)
            {
                bool horizontal = i < 2;
                floorBoundary[i].color = policy.FloorBoundaryColor;
                floorBoundary[i].transform.localPosition = horizontal
                    ? new Vector3(LegalBounds.center.x, i == 0 ? LegalBounds.yMin : LegalBounds.yMax)
                    : new Vector3(i == 2 ? LegalBounds.xMin : LegalBounds.xMax, LegalBounds.center.y);
                floorBoundary[i].transform.localScale = horizontal
                    ? new Vector3(LegalBounds.width, policy.FloorBoundaryWidth, 1)
                    : new Vector3(policy.FloorBoundaryWidth, LegalBounds.height, 1);
            }
        }
        public void PresentMoveGuidance(TileCoordinate[] anchors)
        {
            ClearMoveGuidance();
            if (anchors == null || anchors.Length > maximumGridTiles) return;
            MoveGuidanceAnchors = anchors.ToArray();
            foreach (var anchor in MoveGuidanceAnchors) moveAnchors.SetTile(new Vector3Int(anchor.X, anchor.Y, 0), moveAnchorTile);
        }
        public void ClearMoveGuidance() { moveAnchors?.ClearAllTiles(); MoveGuidanceAnchors = Array.Empty<TileCoordinate>(); }
        public void SetEdit(bool edit)
        { grid.gameObject.SetActive(edit); foreach (var edge in floorBoundary) edge.gameObject.SetActive(edit); if (!edit) ClearMoveGuidance(); }
        public bool TryRoomBounds(string roomId, out Rect bounds)
        {
            bounds=default;
            var room=floor?.Layout.Rooms.SingleOrDefault(r=>r.RoomInstanceId==roomId);
            if(room==null) return false;
            var cells=Footprint(room.RoomDefinitionId,room.Anchor,room.Orientation);
            if(cells.Length==0) return false;
            bounds=Rect.MinMaxRect(cells.Min(t=>t.X),cells.Min(t=>t.Y),cells.Max(t=>t.X)+1,cells.Max(t=>t.Y)+1); return true;
        }
        private void OnDestroy()
        { foreach (Tile tile in ownedTiles) Release(tile); Release(sprite); Release(anchorSprite); Release(anchorTexture); }
        private static void Release(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
