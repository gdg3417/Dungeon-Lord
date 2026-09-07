#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class StructuralConstructionGameRootTests
    {
        [Test]
        public void PreviewWithoutCanonicalAuthorityFailsClosedAndCommitPreservesRuntime()
        {
            var go = new GameObject("StructuralConstructionGameRootNoAuthority");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);

                StructuralEditPreview preview = root.PreviewStructuralConstruction(Request());
                DetachedCanonicalWriteResult commit = root.CommitStructuralConstruction();

                Assert.That(preview.IsValid, Is.False);
                CollectionAssert.AreEqual(new[] { StructuralEditService.InvalidContextReason },
                    preview.ReasonCodes);
                Assert.That(commit.IsSuccess, Is.False);
                Assert.That(commit.Reason, Is.EqualTo(StructuralEditService.InvalidContextReason));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.InvalidContextReason));
                Assert.That(root.Save, Is.SameAs(current));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CommitRetainedInvalidPreviewPreservesSpecificReasonAndRuntime()
        {
            var go = new GameObject("StructuralConstructionGameRootInvalidPreview");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);
                SetProperty(root, "StructuralConstructionPreview",
                    StructuralEditService.InvalidPreview(StructuralEditService.OutOfBoundsReason, Request()));

                DetachedCanonicalWriteResult result = root.CommitStructuralConstruction();

                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Reason, Is.EqualTo(StructuralEditService.OutOfBoundsReason));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.OutOfBoundsReason));
                Assert.That(root.Save, Is.SameAs(current));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CanonicalPublicationReplacesRuntimeAndClearsPreviewPresentation()
        {
            var go = new GameObject("StructuralConstructionGameRootPublication");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var oldSave = new SaveData();
                var published = new SaveData();
                SetProperty(root, "Save", oldSave);
                SetProperty(root, "StructuralConstructionPreview",
                    StructuralEditService.InvalidPreview(StructuralEditService.OutOfBoundsReason, Request()));
                SetProperty(root, "StructuralConstructionReasonKey",
                    StructuralEditService.OutOfBoundsReason);

                typeof(GameRoot).GetMethod("PublishCanonicalRuntime",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root,
                    new object[] { published });

                Assert.That(root.Save, Is.SameAs(published));
                Assert.That(root.StructuralConstructionPreview, Is.Null);
                Assert.That(root.StructuralConstructionReasonKey, Is.Empty);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RenovationRequiresPreviewAndCanonicalPublicationClearsRetainedPreview()
        {
            var go = new GameObject("StructuralRenovationGameRootPublication");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var oldSave = new SaveData();
                var published = new SaveData();
                SetProperty(root, "Save", oldSave);
                DetachedCanonicalWriteResult missingPreview = root.CommitStructuralRenovation();
                Assert.That(missingPreview.IsSuccess, Is.False);
                Assert.That(missingPreview.Reason, Is.EqualTo(StructuralEditService.InvalidContextReason));

                StructuralEditPreview retained = StructuralRenovationService.InvalidMovement(
                    StructuralEditService.OutOfBoundsReason, new StructuralMovementRequest
                    { RoomInstanceId = "test.room", Anchor = new TileCoordinate(1, 1) });
                SetProperty(root, "StructuralRenovationPreview", retained);
                SetProperty(root, "StructuralDeletionPreview", StructuralDeletionService.Invalid(
                    StructuralEditService.OutOfBoundsReason,
                    new StructuralDeletionRequest { TargetRoomInstanceId = "test.room" }));
                typeof(GameRoot).GetMethod("PublishCanonicalRuntime",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(root, new object[] { published });

                Assert.That(root.Save, Is.SameAs(published));
                Assert.That(root.StructuralRenovationPreview, Is.Null);
                Assert.That(root.StructuralDeletionPreview, Is.Null);
                Assert.That(root.StructuralConstructionPreview, Is.Null);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SaveDeleteQuiescenceBlocksStructuralPreviewAndCommit()
        {
            var go = new GameObject("StructuralConstructionGameRootQuiescence");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
                var current = new SaveData();
                SetProperty(root, "Save", current);
                typeof(GameRoot).GetField("_explicitSaveDeleteQuiesced",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, true);

                StructuralEditPreview preview = root.PreviewStructuralConstruction(Request());
                DetachedCanonicalWriteResult commit = root.CommitStructuralConstruction();

                Assert.That(preview.IsValid, Is.False);
                Assert.That(commit.IsSuccess, Is.False);
                Assert.That(root.Save, Is.SameAs(current));
                Assert.That(root.StructuralConstructionReasonKey,
                    Is.EqualTo(StructuralEditService.InvalidContextReason));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CanonicalRouteSelectsRenovationTargetsByRequiredRouteOrder()
        {
            var rootObject = new GameObject("RenovationRouteOrderRoot");
            var overlayObject = new GameObject("RenovationRouteOrderOverlay");
            try
            {
                GameRoot root = rootObject.AddComponent<GameRoot>();
                BootstrapOverlay overlay = overlayObject.AddComponent<BootstrapOverlay>();
                SetProperty(root, "Save", SaveWithTwoPlayerRoomsInReverseStorageOrder());

                overlay.Bind(root);
                overlay.AdjustStructuralAnchor(2, 3);
                TileCoordinate constructionAnchor = overlay.SelectedStructuralAnchor;
                overlay.RefreshStructuralConstructionAuthority();

                Assert.That(overlay.StructuralRenovationControlsAvailable, Is.True);
                Assert.That(overlay.SelectedRenovationRoomInstanceId, Is.EqualTo("test.room.first"));
                Assert.That(overlay.SelectedRenovationAnchor, Is.EqualTo(new TileCoordinate(1, 1)));
                Assert.That(overlay.SelectedStructuralAnchor, Is.EqualTo(constructionAnchor));
                Assert.That(overlay.CycleRenovationTarget(), Is.True);
                Assert.That(overlay.SelectedRenovationRoomInstanceId, Is.EqualTo("test.room.second"));
                Assert.That(overlay.SelectedRenovationAnchor, Is.EqualTo(new TileCoordinate(5, 1)));
                Assert.That(overlay.SelectedStructuralAnchor, Is.EqualTo(constructionAnchor));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void MovementAnchorAndTargetChangesInvalidateRetainedRenovationPreview()
        {
            var rootObject = new GameObject("RenovationInvalidationRoot");
            var overlayObject = new GameObject("RenovationInvalidationOverlay");
            try
            {
                GameRoot root = rootObject.AddComponent<GameRoot>();
                BootstrapOverlay overlay = overlayObject.AddComponent<BootstrapOverlay>();
                SetProperty(root, "Save", SaveWithTwoPlayerRoomsInReverseStorageOrder());
                overlay.Bind(root);
                SetProperty(root, "StructuralRenovationPreview", RetainedPreview());

                TileCoordinate constructionAnchor = overlay.SelectedStructuralAnchor;
                overlay.AdjustRenovationAnchor(1, 0);
                Assert.That(root.StructuralRenovationPreview, Is.Null);
                Assert.That(overlay.SelectedStructuralAnchor, Is.EqualTo(constructionAnchor));

                SetProperty(root, "StructuralRenovationPreview", RetainedPreview());
                overlay.CycleRenovationTarget();
                Assert.That(root.StructuralRenovationPreview, Is.Null);
                Assert.That(overlay.SelectedStructuralAnchor, Is.EqualTo(constructionAnchor));
            }
            finally
            {
                Object.DestroyImmediate(overlayObject);
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CanonicalRoomTargetCyclesThroughLargeChamberAndTargetsItsStableIdentity()
        {
            var go = new GameObject("CanonicalLargeChamberRoomTargetRoot");
            try
            {
                Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture source =
                    Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
                Assert.That(SchemaSevenToEightUpgrade.TryPrepare(
                    source.Result.Attempt.Candidate.GetBytes(), source.Limits, out byte[] current), Is.True);
                DetachedCompleteSaveValidationResult parsed =
                    DetachedCompleteSaveContract.ParseValidateAndRoundTrip(current,
                        new DetachedCurrentTargetValidationContext(source.Compatibility,
                            source.Production, source.LegacyBytes, source.Limits));
                Assert.That(parsed.IsValid, Is.True, parsed.Reason);
                RunSimulationConfig configuration =
                    LegacyGameplayConfigurationContract.Parse(source.LegacyBytes);
                DetachedCanonicalMutationResult firstRoom = DetachedCanonicalSpatialMutation.Prepare(
                    parsed.State, DetachedCanonicalMutationRequest.Place(
                        MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId),
                    source.Production, source.Compatibility, configuration, source.Limits);
                Assert.That(firstRoom.IsSuccess, Is.True, firstRoom.Reason);
                StructuralEditPreview secondRoom = StructuralEditService.Preview(firstRoom.State,
                    new StructuralConstructionRequest
                    {
                        RoomDefinitionId = "spatial.room.large_chamber",
                        Anchor = new TileCoordinate(4, 1),
                        Orientation = CardinalOrientation.Zero,
                        TerminalConnectionPointId = "north"
                    }, source.Production, source.Compatibility, configuration, source.Limits);
                Assert.That(secondRoom.IsValid, Is.True, string.Join(",", secondRoom.ReasonCodes));

                DetachedCanonicalSpatialSaveState state = secondRoom.DetachedCandidate;
                RoomSpatialInstance largeChamber = state.Floors[0].Layout.Rooms.Single(room =>
                    room.RoomDefinitionId == "spatial.room.large_chamber");
                var save = new SaveData
                {
                    canonicalSpatialAuthority = state.Authority,
                    spatialFloors = state.Floors,
                    validatedCanonicalSpatialState = state,
                    mvpSelectedRoomSlotIndex = 0
                };
                var content = new ContentService();
                SetProperty(content, "ProductionSpatialContent", source.Production);
                GameRoot root = go.AddComponent<GameRoot>();
                SetProperty(root, "Save", save);
                SetProperty(root, "Content", content);
                typeof(GameRoot).GetField("_runSimulationService",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root,
                    new RunSimulationService(configuration));

                root.CycleSelectedMvpRoomSlotTarget();
                Assert.That(save.mvpSelectedRoomSlotIndex, Is.EqualTo(1));
                root.CycleSelectedMvpRoomSlotTarget();
                Assert.That(save.mvpSelectedRoomSlotIndex, Is.Zero);
                root.CycleSelectedMvpRoomSlotTarget();

                CanonicalMvpRouteProjectionResult route =
                    CanonicalMvpRouteProjection.InspectWithProductionContent(save, source.Production);
                Assert.That(route.AuthorityState,
                    Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
                string targetRoomId = GameRoot.ResolveCanonicalMutationTargetRoomId(save,
                    configuration, source.Production, route.Rooms);
                Assert.That(targetRoomId, Is.EqualTo(largeChamber.RoomInstanceId));
                DetachedCanonicalMutationResult placement = DetachedCanonicalSpatialMutation.Prepare(
                    state, DetachedCanonicalMutationRequest.Place(
                        MvpDungeonPlacementIds.MonsterCategoryId,
                        "placement.option.monster.skeleton", targetRoomId), source.Production,
                    source.Compatibility, configuration, source.Limits);
                Assert.That(placement.IsSuccess, Is.True, placement.Reason);
                Assert.That(placement.State.Floors[0].RoomContents.Assignments.Single(assignment =>
                    assignment.OptionId == "placement.option.monster.skeleton").RoomInstanceId,
                    Is.EqualTo(largeChamber.RoomInstanceId));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RenovationCommitRequiresPreviewAndInvalidationPreservesPhase3APreview()
        {
            var go = new GameObject("RenovationPreviewRequirementRoot");
            try
            {
                GameRoot root = go.AddComponent<GameRoot>();
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
            finally { Object.DestroyImmediate(go); }
        }

        private static StructuralEditPreview RetainedPreview() =>
            StructuralRenovationService.InvalidMovement(StructuralEditService.OutOfBoundsReason,
                new StructuralMovementRequest { RoomInstanceId = "test.room.first",
                    Anchor = new TileCoordinate(1, 1) });

        private static SaveData SaveWithTwoPlayerRoomsInReverseStorageOrder()
        {
            const string floorId = "test.floor";
            var first = new RoomSpatialInstance { RoomInstanceId = "test.room.first",
                RoomDefinitionId = "spatial.room.basic", FloorId = floorId,
                Anchor = new TileCoordinate(1, 1) };
            var second = new RoomSpatialInstance { RoomInstanceId = "test.room.second",
                RoomDefinitionId = "spatial.room.basic", FloorId = floorId,
                Anchor = new TileCoordinate(5, 1) };
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
            state.LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(state.Floors);
            return new SaveData
            {
                canonicalSpatialAuthority = state.Authority,
                spatialFloors = state.Floors,
                validatedCanonicalSpatialState = state
            };
        }

        private static StructuralConstructionRequest Request() =>
            new StructuralConstructionRequest
            {
                RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "north"
            };

        private static void SetProperty(object target, string name, object value) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
