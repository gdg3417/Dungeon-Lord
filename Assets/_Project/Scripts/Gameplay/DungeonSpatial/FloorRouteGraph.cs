using System;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum FloorRouteNodeKind { Entrance = 1, Room = 2, Exit = 3, Descent = 4, Completion = 5 }
    public enum RouteClassification { Required = 1, Optional = 2 }
    public enum FloorRouteConnectionKind { DirectDoorway = 1, PhysicalCorridor = 2 }

    [Serializable]
    public sealed class FloorRouteNode
    {
        public string NodeId;
        public string FloorId;
        public FloorRouteNodeKind Kind;
        public string RoomInstanceId;
    }

    [Serializable]
    public sealed class FloorRouteEdge : ISerializationCallbackReceiver
    {
        private enum FootprintPresence { UnknownOrLegacy = 0, Absent = 1, Present = 2 }

        public string EdgeId;
        public string CorridorDefinitionId;
        public string FloorId;
        public string SourceNodeId;
        public string DestinationNodeId;
        public ResolvedTileFootprint Footprint;
        public RouteClassification Classification;
        public string OptionalBranchId;
        public FloorRouteConnectionKind ConnectionKind;

        [SerializeField] private FootprintPresence footprintPresence;

        public void OnBeforeSerialize()
        {
            footprintPresence = Footprint == null ? FootprintPresence.Absent : FootprintPresence.Present;
        }

        public void OnAfterDeserialize()
        {
            if (footprintPresence == FootprintPresence.Absent) Footprint = null;
        }
    }
}
