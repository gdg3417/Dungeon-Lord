using System;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    public sealed class StructuralRenovationBootstrapPlayModeTests
    {
        [Test]
        public void CanonicalRouteEnablesRenovationControlsAndSelectsTargetByRouteOrder()
        {
            GameObject rootObject = new GameObject("RenovationPlayModeRoot");
            GameObject overlayObject = new GameObject("RenovationPlayModeOverlay");
            try
            {
                GameRoot root = rootObject.AddComponent<GameRoot>();
                BootstrapOverlay overlay = overlayObject.AddComponent<BootstrapOverlay>();
                SetProperty(root, "Save", SaveWithTwoPlayerRoomsInReverseStorageOrder());

                overlay.Bind(root);

                Assert.That(overlay.StructuralRenovationControlsAvailable, Is.True);
                Assert.That(overlay.SelectedRenovationRoomInstanceId, Is.EqualTo("test.room.first"));
                Assert.That(overlay.CycleRenovationTarget(), Is.True);
                Assert.That(overlay.SelectedRenovationRoomInstanceId, Is.EqualTo("test.room.second"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overlayObject);
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void MovementAnchorAndTargetChangesInvalidateRetainedRenovationPreview()
        {
            GameObject rootObject = new GameObject("RenovationInvalidationRoot");
            GameObject overlayObject = new GameObject("RenovationInvalidationOverlay");
            try
            {
                GameRoot root = rootObject.AddComponent<GameRoot>();
                BootstrapOverlay overlay = overlayObject.AddComponent<BootstrapOverlay>();
                SetProperty(root, "Save", SaveWithTwoPlayerRoomsInReverseStorageOrder());
                overlay.Bind(root);
                SetProperty(root, "StructuralRenovationPreview", RetainedPreview());

                overlay.AdjustStructuralAnchor(1, 0);
                Assert.That(root.StructuralRenovationPreview, Is.Null);

                SetProperty(root, "StructuralRenovationPreview", RetainedPreview());
                overlay.CycleRenovationTarget();
                Assert.That(root.StructuralRenovationPreview, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(overlayObject);
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CommitWithoutPreviewFailsAndPhase3AInvalidationRemainsIndependent()
        {
            GameObject rootObject = new GameObject("RenovationPreviewRequirementRoot");
            try
            {
                GameRoot root = rootObject.AddComponent<GameRoot>();
                SetProperty(root, "Save", SaveWithTwoPlayerRoomsInReverseStorageOrder());
                DetachedCanonicalWriteResult result = root.CommitStructuralRenovation();
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Reason, Is.EqualTo(StructuralEditService.InvalidContextReason));

                SetProperty(root, "StructuralConstructionPreview",
                    StructuralEditService.InvalidPreview(StructuralEditService.OutOfBoundsReason,
                        new StructuralConstructionRequest()));
                root.InvalidateStructuralRenovationPreview();
                Assert.That(root.StructuralConstructionPreview, Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(rootObject); }
        }

        private static StructuralEditPreview RetainedPreview() =>
            StructuralRenovationService.InvalidMovement(StructuralEditService.OutOfBoundsReason,
                new StructuralMovementRequest { RoomInstanceId = "test.room.first",
                    Anchor = new TileCoordinate(1, 1) });

        private static SaveData SaveWithTwoPlayerRoomsInReverseStorageOrder()
        {
            const string floorId = "test.floor";
            var first = new RoomSpatialInstance { RoomInstanceId = "test.room.first",
                RoomDefinitionId = "spatial.room.basic", FloorId = floorId };
            var second = new RoomSpatialInstance { RoomInstanceId = "test.room.second",
                RoomDefinitionId = "spatial.room.basic", FloorId = floorId };
            var entrance = new FloorRouteNode { NodeId = "test.node.entrance", FloorId = floorId,
                Kind = FloorRouteNodeKind.Entrance, RoomInstanceId = string.Empty };
            var firstNode = new FloorRouteNode { NodeId = "test.node.first", FloorId = floorId,
                Kind = FloorRouteNodeKind.Room, RoomInstanceId = first.RoomInstanceId };
            var secondNode = new FloorRouteNode { NodeId = "test.node.second", FloorId = floorId,
                Kind = FloorRouteNodeKind.Room, RoomInstanceId = second.RoomInstanceId };
            var completion = new FloorRouteNode { NodeId = "test.node.completion", FloorId = floorId,
                Kind = FloorRouteNodeKind.Completion, RoomInstanceId = string.Empty };
            FloorRouteEdge Edge(string id, FloorRouteNode source, FloorRouteNode destination) =>
                new FloorRouteEdge { EdgeId = id, FloorId = floorId, SourceNodeId = source.NodeId,
                    DestinationNodeId = destination.NodeId, Classification = RouteClassification.Required,
                    ConnectionKind = FloorRouteConnectionKind.DirectDoorway,
                    CorridorDefinitionId = string.Empty, OptionalBranchId = string.Empty };
            var state = new DetachedCanonicalSpatialSaveState
            {
                Authority = new CanonicalSpatialAuthorityMarker(),
                Floors = new[] { new SavedSpatialFloor { FloorInstanceId = floorId,
                    Layout = new FloorSpatialLayout { FloorId = floorId,
                        Rooms = new[] { second, first },
                        Nodes = new[] { secondNode, completion, entrance, firstNode },
                        Edges = new[] { Edge("test.edge.second", secondNode, completion),
                            Edge("test.edge.entrance", entrance, firstNode),
                            Edge("test.edge.first", firstNode, secondNode) } },
                    RoomContents = new FloorRoomContentState { RoomSemantics = new[]
                    { new CanonicalRoomSemantics { RoomInstanceId = first.RoomInstanceId,
                        LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced },
                      new CanonicalRoomSemantics { RoomInstanceId = second.RoomInstanceId,
                        LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced } } } } }
            };
            return new SaveData { validatedCanonicalSpatialState = state };
        }

        private static void SetProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(target, value);
    }
}
