using System;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorRouteNodeKind { Entrance = 1, Room = 2, Exit = 3, Descent = 4, Completion = 5 }
    public enum RouteClassification { Required = 1, Optional = 2 }

    [Serializable]
    public sealed class FloorRouteNode
    {
        public string NodeId;
        public string FloorId;
        public FloorRouteNodeKind Kind;
        public string RoomInstanceId;
    }

    [Serializable]
    public sealed class CorridorEdge
    {
        public string EdgeId;
        public string CorridorDefinitionId;
        public string FloorId;
        public string SourceNodeId;
        public string DestinationNodeId;
        public ResolvedTileFootprint Footprint = new ResolvedTileFootprint();
        public RouteClassification Classification;
        public string OptionalBranchId;
    }
}
