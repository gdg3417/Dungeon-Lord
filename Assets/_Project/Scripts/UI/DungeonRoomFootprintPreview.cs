using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    /// <summary>Bounded retained UI mesh over resolved authored cells, never a gameplay footprint owner.</summary>
    public sealed class DungeonRoomFootprintPreview : VisualElement
    {
        private readonly TileCoordinate[] cells;
        private readonly HashSet<TileCoordinate> occupied;
        private readonly Sprite stone;
        private readonly int commonSpan;
        public IReadOnlyList<TileCoordinate> Cells => cells;
        public Rect RenderedFootprintRect { get; private set; }
        public DungeonRoomFootprintPreview(TileCoordinate[] resolvedCells, int commonSpan, Sprite stone)
        {
            cells = resolvedCells.ToArray(); occupied = new HashSet<TileCoordinate>(cells);
            this.commonSpan = Math.Max(1,commonSpan); this.stone = stone;
            pickingMode = PickingMode.Ignore; generateVisualContent += Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            if (cells.Length == 0 || stone == null) return;
            float unit = Mathf.Max(0,Mathf.Min(contentRect.width,contentRect.height)-8)/commonSpan;
            int minX=cells.Min(c=>c.X), minY=cells.Min(c=>c.Y), maxY=cells.Max(c=>c.Y);
            float width=(cells.Max(c=>c.X)-minX+1)*unit, height=(maxY-minY+1)*unit;
            var start=contentRect.center-new Vector2(width,height)*.5f;
            RenderedFootprintRect=new Rect(start.x,start.y,width,height);
            Rect uv=stone.textureRect; uv.x/=stone.texture.width; uv.width/=stone.texture.width;
            uv.y/=stone.texture.height; uv.height/=stone.texture.height;
            foreach(var cell in cells)
            {
                float x=start.x+(cell.X-minX)*unit, y=start.y+(maxY-cell.Y)*unit;
                var mesh=context.Allocate(4,6,stone.texture);
                mesh.SetNextVertex(new Vertex { position=new Vector3(x,y,Vertex.nearZ),tint=Color.white,uv=new Vector2(uv.xMin,uv.yMax) });
                mesh.SetNextVertex(new Vertex { position=new Vector3(x+unit,y,Vertex.nearZ),tint=Color.white,uv=new Vector2(uv.xMax,uv.yMax) });
                mesh.SetNextVertex(new Vertex { position=new Vector3(x+unit,y+unit,Vertex.nearZ),tint=Color.white,uv=new Vector2(uv.xMax,uv.yMin) });
                mesh.SetNextVertex(new Vertex { position=new Vector3(x,y+unit,Vertex.nearZ),tint=Color.white,uv=new Vector2(uv.xMin,uv.yMin) });
                mesh.SetNextIndex(0); mesh.SetNextIndex(1); mesh.SetNextIndex(2);
                mesh.SetNextIndex(2); mesh.SetNextIndex(3); mesh.SetNextIndex(0);
                var painter=context.painter2D; painter.strokeColor=new Color(.78f,.73f,.57f); painter.lineWidth=2;
                void Edge(int dx,int dy,Vector2 a,Vector2 b)
                { if(occupied.Contains(new TileCoordinate(cell.X+dx,cell.Y+dy))) return;
                  painter.BeginPath(); painter.MoveTo(a); painter.LineTo(b); painter.Stroke(); }
                Edge(0,1,new Vector2(x,y),new Vector2(x+unit,y));
                Edge(1,0,new Vector2(x+unit,y),new Vector2(x+unit,y+unit));
                Edge(0,-1,new Vector2(x,y+unit),new Vector2(x+unit,y+unit));
                Edge(-1,0,new Vector2(x,y),new Vector2(x,y+unit));
            }
        }
    }
}
