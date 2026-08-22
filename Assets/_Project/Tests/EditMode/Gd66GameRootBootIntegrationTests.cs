#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66GameRootBootIntegrationTests
    {
        private GameObject rootObject;

        [TearDown]
        public void TearDown()
        {
            if (rootObject != null) Object.DestroyImmediate(rootObject);
        }

        [Test]
        public void SuccessfulCanonicalBootCompletionInstallsAuthorityAndEntersHome()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = Service(fixture, fileSystem, "root-success.json");
            SaveData canonical = service.LoadOrCreate("gd66-live", out string banner);
            Assert.That(canonical, Is.Not.Null, banner);
            DetachedCanonicalSaveSession beforeSession = service.CanonicalSession;
            GameRoot root = Root(service);

            bool completed = root.CompleteSuccessfulBootForTests(canonical, true);

            Assert.That(completed, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.Save.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(root.TimeService, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
            Assert.That(root.Save, Is.Not.SameAs(canonical));
            Assert.That(service.CanonicalSession, Is.Not.SameAs(beforeSession));
            Assert.That(service.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(service.SavePath)));
            Assert.That(root.GameplayServicesInitializedForTests, Is.True);
            Assert.That(root.StateLine, Does.Contain("Home"));
        }

        [Test]
        public void StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = Service(fixture, fileSystem, "root-structural-construction.json");
            SaveData empty = service.LoadOrCreate("gd66-live", out string banner);
            Assert.That(empty, Is.Not.Null, banner);
            DetachedCanonicalWriteResult starter = service.ExecuteCanonicalMutation(empty,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId,
                    MvpDungeonPlacementIds.BasicRoomOptionId));
            Assert.That(starter.IsSuccess, Is.True, starter.Reason);
            GameRoot root = Root(service);
            LoadPlayerFacingContent(root);
            var overlay = rootObject.AddComponent<BootstrapOverlay>();
            root.overlay = overlay;
            overlay.Bind(root);
            Assert.That(overlay.StructuralConstructionControlsAvailable, Is.False);
            Assert.That(overlay.SelectedStructuralRoomDefinitionId, Is.Null);
            Assert.That(root.CompleteSuccessfulBootForTests(starter.RuntimeProjection, true), Is.True);
            SaveData before = root.Save;
            Assert.That(overlay.StructuralConstructionControlsAvailable, Is.True);
            Assert.That(overlay.SelectedStructuralRoomDefinitionId, Is.EqualTo("spatial.room.basic"));
            CollectionAssert.AreEqual(new[] { CardinalOrientation.Zero },
                overlay.SelectableStructuralOrientations);
            Assert.That(overlay.SelectedStructuralOrientation, Is.EqualTo(CardinalOrientation.Zero));
            Assert.That(overlay.SelectableStructuralConnectionPointIds, Does.Contain(
                overlay.SelectedStructuralTerminalConnectionPointId));
            CollectionAssert.AreEqual(fixture.Production.Catalog.Rooms.Single(value =>
                value.RoomDefinitionId == overlay.SelectedStructuralRoomDefinitionId).ConnectionPoints
                .Select(value => value.ConnectionPointId).OrderBy(value => value, System.StringComparer.Ordinal),
                overlay.SelectableStructuralConnectionPointIds);
            Assert.That(overlay.SelectedStructuralTerminalConnectionPointId,
                Is.EqualTo(overlay.SelectableStructuralConnectionPointIds[0]));
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Is.EqualTo("Basic Room"));
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Does.Not.Contain("spatial.room."));
            StructuralEditPreview immediate = overlay.PreviewStructuralConstruction();
            Assert.That(immediate.ReasonCodes, Does.Not.Contain(StructuralEditService.InvalidContextReason));
            string[] productionAllowed = fixture.Production.Catalog.Floors.Single(value =>
                value.FloorDefinitionId == root.Save.validatedCanonicalSpatialState.Floors[0].FloorDefinitionId &&
                value.FloorIndex == root.Save.validatedCanonicalSpatialState.Floors[0].FloorIndex)
                .AllowedRoomDefinitionIds.OrderBy(value => value, System.StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(productionAllowed, overlay.SelectableStructuralRoomDefinitionIds);
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(overlay.SelectedStructuralRoomDefinitionId, Is.EqualTo("spatial.room.large_chamber"));
            CollectionAssert.AreEqual(new[] { CardinalOrientation.Zero, CardinalOrientation.Ninety },
                overlay.SelectableStructuralOrientations);
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Is.EqualTo("Large Chamber"));
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Does.Not.Contain("spatial.room."));
            CollectionAssert.AreEqual(fixture.Production.Catalog.Rooms.Single(value =>
                value.RoomDefinitionId == overlay.SelectedStructuralRoomDefinitionId).ConnectionPoints
                .Select(value => value.ConnectionPointId).OrderBy(value => value, System.StringComparer.Ordinal),
                overlay.SelectableStructuralConnectionPointIds);
            Assert.That(overlay.SelectableStructuralConnectionPointIds, Does.Contain(
                overlay.SelectedStructuralTerminalConnectionPointId));
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(overlay.SelectedStructuralRoomDefinitionId, Is.EqualTo("spatial.room.rectangle"));
            CollectionAssert.AreEqual(new[] { CardinalOrientation.Zero, CardinalOrientation.Ninety },
                overlay.SelectableStructuralOrientations);
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Is.EqualTo("Rectangle Room"));
            Assert.That(overlay.SelectedStructuralRoomDisplayName, Does.Not.Contain("spatial.room."));
            CollectionAssert.AreEqual(fixture.Production.Catalog.Rooms.Single(value =>
                value.RoomDefinitionId == overlay.SelectedStructuralRoomDefinitionId).ConnectionPoints
                .Select(value => value.ConnectionPointId).OrderBy(value => value, System.StringComparer.Ordinal),
                overlay.SelectableStructuralConnectionPointIds);
            Assert.That(overlay.SelectedStructuralTerminalConnectionPointId,
                Is.EqualTo(overlay.SelectableStructuralConnectionPointIds[0]));
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(overlay.SelectedStructuralRoomDefinitionId, Is.EqualTo("spatial.room.basic"));
            for (int step = 0; step < 6; step++) overlay.AdjustStructuralAnchor(0, 1);
            while (overlay.SelectedStructuralTerminalConnectionPointId != "north")
                Assert.That(overlay.CycleStructuralConnectionPoint(), Is.True);
            Assert.That(overlay.PreviewStructuralConstruction().IsValid, Is.True);
            Assert.That(overlay.StructuralFeedback, Is.Not.Empty);
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (0, 6)"));
            Assert.That(overlay.StructuralConnectionPointDisplay, Does.Contain("North"));
            overlay.AdjustStructuralAnchor(1, 0);
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (1, 6)"));
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            overlay.AdjustStructuralAnchor(-1, 0);
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (0, 6)"));
            Assert.That(root.PreviewStructuralConstruction(new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" }).IsValid, Is.True);
            overlay.AdjustStructuralAnchor(-1, 0);
            Assert.That(overlay.SelectedStructuralAnchor.X, Is.EqualTo(-1));
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (-1, 6)"));
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            overlay.AdjustStructuralAnchor(1, 0);
            Assert.That(root.PreviewStructuralConstruction(new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" }).IsValid, Is.True);
            overlay.AdjustStructuralAnchor(0, 1);
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (0, 7)"));
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            overlay.AdjustStructuralAnchor(0, -1);
            Assert.That(root.PreviewStructuralConstruction(new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic", Anchor = new TileCoordinate(0, 6),
                Orientation = CardinalOrientation.Zero, TerminalConnectionPointId = "north" }).IsValid, Is.True);
            overlay.AdjustStructuralAnchor(0, -1);
            Assert.That(overlay.StructuralAnchorDisplay, Is.EqualTo("Anchor: (0, 5)"));
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            overlay.AdjustStructuralAnchor(0, 1);
            Assert.That(overlay.CycleStructuralConnectionPoint(), Is.True);
            Assert.That(overlay.StructuralConnectionPointDisplay, Does.Contain("South"));
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(overlay.StructuralConnectionPointDisplay, Does.Contain("East"));
            Assert.That(overlay.CycleStructuralOrientation(), Is.True);
            Assert.That(overlay.StructuralConnectionPointDisplay, Does.Contain("South"));
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            while (overlay.SelectedStructuralTerminalConnectionPointId != "north")
                Assert.That(overlay.CycleStructuralConnectionPoint(), Is.True);
            Assert.That(overlay.PreviewStructuralConstruction().IsValid, Is.True);
            DetachedCanonicalWriteResult intervening = service.ExecuteCanonicalMutation(root.Save,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId,
                    MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(intervening.IsSuccess, Is.True, intervening.Reason);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            Assert.That(overlay.StructuralFeedback, Is.Empty);
            overlay.AdjustStructuralAnchor(1, 0);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            overlay.AdjustStructuralAnchor(-1, 0);
            var validRequest = new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(0, 6), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "north" };
            Assert.That(root.PreviewStructuralConstruction(validRequest).IsValid, Is.True);
            Assert.That(overlay.CycleStructuralRoom(), Is.True);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            Assert.That(overlay.CommitStructuralConstruction(), Is.Null);
            Assert.That(root.PreviewStructuralConstruction(validRequest).IsValid, Is.True);
            Assert.That(overlay.CycleStructuralOrientation(), Is.True);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            Assert.That(overlay.CommitStructuralConstruction(), Is.Null);
            Assert.That(root.PreviewStructuralConstruction(validRequest).IsValid, Is.True);
            overlay.AdjustStructuralAnchor(1, 0);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            Assert.That(overlay.CommitStructuralConstruction(), Is.Null);
            overlay.AdjustStructuralAnchor(-1, 0);
            Assert.That(root.PreviewStructuralConstruction(validRequest).IsValid, Is.True);
            Assert.That(overlay.CycleStructuralConnectionPoint(), Is.True);
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            Assert.That(overlay.CommitStructuralConstruction(), Is.Null);
            TileCoordinate terminalBefore = before.validatedCanonicalSpatialState.Floors[0]
                .FixedStructures.Single(value => value.Kind == FixedSpatialStructureKind.CompletionTerminal).Anchor;
            StructuralEditPreview invalid = root.PreviewStructuralConstruction(
                new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(-100, -100), Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "north" });
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.ReasonCodes, Does.Contain(StructuralEditService.OutOfBoundsReason));
            Assert.That(overlay.BuildStructuralPreviewPresentation(invalid), Does.Contain(
                root.Content.GetString(StructuralEditService.OutOfBoundsReason, string.Empty)));

            StructuralEditPreview corridorPreview = root.PreviewStructuralConstruction(
                new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(5, 2), Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "east" });
            Assert.That(corridorPreview.IsValid, Is.True, string.Join(",", corridorPreview.ReasonCodes));
            Assert.That(corridorPreview.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.PhysicalCorridor));
            CollectionAssert.AreEqual(new[] { new TileCoordinate(4, 3) },
                corridorPreview.IncomingConnectionTiles);
            Assert.That(corridorPreview.ResultingUsedFloorSpace, Is.EqualTo(43));
            Assert.That(corridorPreview.ResultingRemainingFloorSpace, Is.EqualTo(17));
            Assert.That(overlay.BuildStructuralPreviewPresentation(corridorPreview),
                Does.Contain(root.Content.GetString("ui.structural.connection.corridor", string.Empty)));

            StructuralEditPreview preview = root.PreviewStructuralConstruction(
                new StructuralConstructionRequest { RoomDefinitionId = "spatial.room.basic",
                    Anchor = new TileCoordinate(0, 6), Orientation = CardinalOrientation.Zero,
                    TerminalConnectionPointId = "north" });
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            Assert.That(preview.ConnectionKind, Is.EqualTo(FloorRouteConnectionKind.DirectDoorway));
            Assert.That(preview.ResultingUsedFloorSpace, Is.EqualTo(42));
            Assert.That(preview.ResultingRemainingFloorSpace, Is.EqualTo(18));
            string presentation = overlay.BuildStructuralPreviewPresentation(preview);
            Assert.That(presentation, Does.Contain(root.Content.GetString(
                "ui.structural.connection.direct", string.Empty)));
            Assert.That(presentation, Does.Contain(root.Content.GetString(
                "ui.structural.consequence.terminal_moved", string.Empty)));
            DetachedCanonicalWriteResult committed = root.CommitStructuralConstruction();

            Assert.That(committed.IsSuccess, Is.True, committed.Reason);
            Assert.That(root.Save, Is.Not.SameAs(before));
            Assert.That(root.StructuralConstructionPreview, Is.Null);
            SavedSpatialFloor floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            Assert.That(floor.Layout.Rooms, Has.Length.EqualTo(2));
            Assert.That(floor.FixedStructures.Single(value =>
                value.Kind == FixedSpatialStructureKind.CompletionTerminal).Anchor, Is.Not.EqualTo(terminalBefore));
            Assert.That(service.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(service.SavePath)));
        }

        [Test]
        public void FailedBootCompletionLeavesGameplayAndHomeUninitialized()
        {
            GameRoot root = Root(null);

            bool completed = root.CompleteSuccessfulBootForTests(null, true);

            Assert.That(completed, Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.StateLine, Does.Not.Contain("Home"));
        }

        [Test]
        public void ExplicitDevDeleteQuiescesCanonicalRuntimeAndFreshRootCreatesNativeSave()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            const string filename = "root-delete-success.json";
            SaveService service = Service(fixture, fileSystem, filename, errors);
            SaveData canonical = service.LoadOrCreate("gd66-live", out string loadBanner);
            Assert.That(canonical, Is.Not.Null, loadBanner);
            GameRoot root = Root(service);
            Assert.That(root.CompleteSuccessfulBootForTests(canonical, true), Is.True);
            SaveData deletedRuntime = root.Save;
            long ticksBeforeDelete = deletedRuntime.totalTicks;

            bool deleted = root.TryDeleteSaveFromDevPanel(out string deleteBanner);

            Assert.That(deleted, Is.True, deleteBanner);
            Assert.That(deleteBanner, Is.EqualTo("Save deleted."));
            Assert.That(root.BannerMessage, Does.Contain("Save deleted."));
            Assert.That(service.CanonicalSession, Is.Null);
            Assert.That(fileSystem.Exists(service.SavePath), Is.False);
            Assert.That(SaveService.HasOwnedRecoveryEvidence(service.SavePath, fileSystem, 64), Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.ExplicitSaveDeleteQuiescedForTests, Is.True);
            var overlay = rootObject.AddComponent<BootstrapOverlay>();
            overlay.Bind(root);
            Assert.That(overlay.NormalGameplayActionsAvailable, Is.False);
            Assert.That(overlay.NarrowHallRepairOnlyVisible, Is.False);
            Assert.That(overlay.BuildCurrentPlayerFacingSmokeText(), Does.Contain("Save deleted."));
            Assert.That(overlay.BuildCurrentPlayerFacingSmokeText(), Does.Not.Contain("gd66."));
            int errorsAfterDelete = errors.Count;
            root.ApplyPauseState(true);
            root.ApplyPauseState(false);
            root.ApplyApplicationQuit();
            Assert.That(errors, Has.Count.EqualTo(errorsAfterDelete));
            Assert.That(fileSystem.Exists(service.SavePath), Is.False);
            Assert.That(deletedRuntime.totalTicks, Is.EqualTo(ticksBeforeDelete));

            Object.DestroyImmediate(rootObject);
            rootObject = null;
            SaveService restartedService = Service(fixture, fileSystem, filename, errors);
            SaveData fresh = restartedService.LoadOrCreate("gd66-live", out string restartBanner);
            Assert.That(fresh, Is.Not.Null, restartBanner);
            Assert.That(fresh.validatedCanonicalSpatialState, Is.Not.Null);
            GameRoot restartedRoot = Root(restartedService);
            Assert.That(restartedRoot.CompleteSuccessfulBootForTests(fresh, true), Is.True);
            Assert.That(restartedRoot.Save, Is.Not.Null);
            Assert.That(restartedRoot.TimeService.AttachedSaveForTests,
                Is.SameAs(restartedRoot.Save));
            Assert.That(restartedRoot.GameplayServicesInitializedForTests, Is.True);
            Assert.That(restartedRoot.StateLine, Does.Contain("Home"));
            var restartedOverlay = rootObject.AddComponent<BootstrapOverlay>();
            restartedOverlay.Bind(restartedRoot);
            Assert.That(restartedOverlay.NormalGameplayActionsAvailable, Is.True);
        }

        [Test]
        public void FailedExplicitDevDeleteStillQuiescesAndBlocksLifecycleWrites()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService service = Service(fixture, fileSystem, "root-delete-failed.json", errors);
            SaveData canonical = service.LoadOrCreate("gd66-live", out string loadBanner);
            Assert.That(canonical, Is.Not.Null, loadBanner);
            GameRoot root = Root(service);
            Assert.That(root.CompleteSuccessfulBootForTests(canonical, true), Is.True);
            fileSystem.EnableFailure(
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Delete, 1);

            bool deleted = root.TryDeleteSaveFromDevPanel(out string deleteBanner);

            Assert.That(deleted, Is.False);
            Assert.That(deleteBanner, Is.EqualTo("Failed to delete save."));
            Assert.That(root.BannerMessage, Does.Contain("Failed to delete save."));
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.ExplicitSaveDeleteQuiescedForTests, Is.True);
            int errorsAfterDelete = errors.Count;
            Assert.That(errorsAfterDelete, Is.GreaterThan(0));
            root.ApplyPauseState(true);
            root.ApplyPauseState(false);
            root.ApplyApplicationQuit();
            Assert.That(errors, Has.Count.EqualTo(errorsAfterDelete));
        }

        [Test]
        public void NarrowHallBlockedRootRepairsThroughRealBootCompletionOnce()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService service = Service(fixture, fileSystem, "root-repair.json", errors);
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            CollectionAssert.AreEqual(new[] { NarrowHallLoadError }, errors);
            GameRoot root = Root(service);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(service.NarrowHallRepairAvailable, Is.True);
            DetachedCanonicalSaveSession blockedSession = service.CanonicalSession;
            const string blockedBanner = "Replace Narrow Hall to continue safely.";
            root.SetBanner(blockedBanner);
            Assert.That(root.BannerMessage, Is.EqualTo(blockedBanner));

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.True);
            Assert.That(root.Save, Is.Not.Null);
            Assert.That(root.Save.validatedCanonicalSpatialState, Is.Not.Null);
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
            Assert.That(service.NarrowHallRepairAvailable, Is.False);
            Assert.That(service.CanonicalSession, Is.Not.Null);
            Assert.That(service.CanonicalSession, Is.Not.SameAs(blockedSession));
            Assert.That(service.CanonicalSession.GetCurrentBytes(),
                Is.EqualTo(fileSystem.ReadAllBytes(service.SavePath)));
            Assert.That(root.GameplayServicesInitializedForTests, Is.True);
            Assert.That(root.StateLine, Does.Contain("Home"));
            Assert.That(root.BannerMessage, Is.Not.EqualTo(blockedBanner));
            Assert.That(root.BannerMessage, Does.Not.Contain("Narrow Hall"));
            TimeService initializedTimeService = root.TimeService;
            SaveData initializedSave = root.Save;
            Assert.That(root.TryRepairMigrationBlockedNarrowHall(), Is.False);
            Assert.That(root.TimeService, Is.SameAs(initializedTimeService));
            Assert.That(root.Save, Is.SameAs(initializedSave));
            Assert.That(root.TimeService.AttachedSaveForTests, Is.SameAs(root.Save));
        }

        [Test]
        public void FailedNarrowHallRepairKeepsRootBlockedWithoutPartialServices()
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":6," +
                "\"primary\":{\"mvpDungeonPlacements\":{\"Entries\":[{" +
                "\"CategoryId\":\"placement.category.room\",\"OptionId\":\"placement.option.room.narrow_hall\"," +
                "\"Revision\":1}],\"NextRevision\":2}}}");
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture =
                Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6, false, original);
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            var errors = new List<string>();
            SaveService service = Service(fixture, fileSystem, "root-repair-failed.json", errors);
            fileSystem.Seed(service.SavePath, original);
            Assert.That(service.LoadOrCreate("gd66-live", out _), Is.Null);
            CollectionAssert.AreEqual(new[] { NarrowHallLoadError }, errors);
            GameRoot root = Root(service);
            const string blockedBanner = "Replace Narrow Hall to continue safely.";
            root.SetBanner(blockedBanner);
            fileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 1);

            bool repaired = root.TryRepairMigrationBlockedNarrowHall();

            Assert.That(repaired, Is.False);
            Assert.That(root.Save, Is.Null);
            Assert.That(root.TimeService, Is.Null);
            Assert.That(root.GameplayServicesInitializedForTests, Is.False);
            Assert.That(root.StateLine, Does.Not.Contain("Home"));
            Assert.That(root.BannerMessage, Is.Not.Empty);
            Assert.That(root.BannerMessage, Is.Not.EqualTo(blockedBanner));
        }

        [Test]
        public void CanonicalActionFeedbackNamesEachPlacementWithoutWritingLegacyEvidence()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            SaveService service = Service(fixture, fileSystem, "root-placement-feedback.json");
            SaveData canonical = service.LoadOrCreate("gd66-live", out string loadBanner);
            Assert.That(canonical, Is.Not.Null, loadBanner);
            GameRoot root = Root(service);
            LoadPlayerFacingContent(root);
            Assert.That(root.CompleteSuccessfulBootForTests(canonical, true), Is.True);
            string frozenLegacyEvidence = CaptureLegacySpatialEvidence(root.Save);
            var overlay = rootObject.AddComponent<BootstrapOverlay>();
            overlay.Bind(root);

            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId,
                "Room", "Basic Room", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId,
                "Monster", "Skeleton", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.ChillingSigilOptionId,
                "Trap", "Chilling Sigil", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.GlitteringHoardOptionId,
                "Loot node", "Glittering Hoard", frozenLegacyEvidence);

            CanonicalMvpRouteProjectionResult route = CanonicalMvpRouteProjection.InspectWithProductionContent(
                root.Save, root.ProductionSpatialContent);
            Assert.That(route.AuthorityState, Is.EqualTo(CanonicalMvpRuntimeAuthorityState.ValidatedCanonical));
            Assert.That(route.Rooms, Has.Length.EqualTo(1));
            Assert.That(route.Rooms[0].RoomOptionId, Is.EqualTo(MvpDungeonPlacementIds.BasicRoomOptionId));
            CollectionAssert.Contains(route.Rooms[0].AssignedMonsterOptionIds,
                MvpDungeonPlacementIds.SkeletonOptionId);
            CollectionAssert.Contains(route.Rooms[0].AssignedTrapOptionIds,
                MvpDungeonPlacementIds.ChillingSigilOptionId);
            CollectionAssert.Contains(route.Rooms[0].AssignedLootNodeOptionIds,
                MvpDungeonPlacementIds.GlitteringHoardOptionId);
        }

        [Test]
        public void CanonicalAdditiveFeedbackDoesNotClaimReplacementAndFailureDoesNotInventSuccess()
        {
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture = Fixture();
            var fileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            const string filename = "root-additive-feedback.json";
            SaveService service = Service(fixture, fileSystem, filename);
            SaveData canonical = service.LoadOrCreate("gd66-live", out string loadBanner);
            Assert.That(canonical, Is.Not.Null, loadBanner);
            GameRoot root = Root(service);
            LoadPlayerFacingContent(root);
            Assert.That(root.CompleteSuccessfulBootForTests(canonical, true), Is.True);
            string frozenLegacyEvidence = CaptureLegacySpatialEvidence(root.Save);
            var overlay = rootObject.AddComponent<BootstrapOverlay>();
            overlay.Bind(root);

            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId,
                "Room", "Basic Room", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId,
                "Monster", "Skeleton", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId,
                "Monster", "Goblin", frozenLegacyEvidence);
            Assert.That(overlay.MvpStructurePlacementFeedback, Does.Contain("Empty slot"));
            Assert.That(overlay.MvpStructurePlacementFeedback, Does.Not.Contain("Skeleton"));

            SaveService reopenedService = Service(fixture, fileSystem, filename);
            SaveData reopened = reopenedService.LoadOrCreate("gd66-live", out string reopenBanner);
            Assert.That(reopened, Is.Not.Null, reopenBanner);
            CanonicalMvpRouteProjectionResult reopenedRoute =
                CanonicalMvpRouteProjection.InspectWithProductionContent(reopened, fixture.Production);
            CollectionAssert.AreEqual(new[]
            {
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.GoblinOptionId
            }, reopenedRoute.Rooms[0].AssignedMonsterOptionIds);
            RoomContentAssignment[] reopenedMonsters = reopened.validatedCanonicalSpatialState.Floors[0]
                .RoomContents.Assignments.Where(value =>
                    value.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId).ToArray();
            CollectionAssert.AreEqual(new long[] { 0, 1 },
                reopenedMonsters.Select(value => value.Sequence).ToArray());
            Assert.That(reopenedMonsters.Select(value => value.AssignmentId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(CaptureLegacySpatialEvidence(reopened), Is.EqualTo(frozenLegacyEvidence));

            Assert.That(overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.MonsterCategoryId), Is.True);
            Assert.That(overlay.SelectMvpPlacementOption(MvpDungeonPlacementIds.SkeletonOptionId), Is.True);
            overlay.PlaceSelectedMvpStructure();

            Assert.That(overlay.MvpStructurePlacementFeedback, Is.Empty);
            Assert.That(root.BannerMessage,
                Is.EqualTo(root.Content.GetString("ui.banner.place_failed", "ui.banner.place_failed")));
            Assert.That(root.BannerMessage, Does.Not.Contain("gd66."));
            Assert.That(root.BannerMessage, Does.Not.Contain("Unknown category"));
            Assert.That(CaptureLegacySpatialEvidence(root.Save), Is.EqualTo(frozenLegacyEvidence));
            CanonicalMvpRouteProjectionResult route = CanonicalMvpRouteProjection.InspectWithProductionContent(
                root.Save, root.ProductionSpatialContent);
            CollectionAssert.AreEquivalent(new[]
            {
                MvpDungeonPlacementIds.SkeletonOptionId,
                MvpDungeonPlacementIds.GoblinOptionId
            }, route.Rooms[0].AssignedMonsterOptionIds);

            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                "Trap", "Spike Trap", frozenLegacyEvidence);
            PlaceAndAssertFeedback(overlay, root,
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SnareTrapOptionId,
                "Trap", "Snare Trap", frozenLegacyEvidence);
            byte[] beforeCapacityFailure = service.CanonicalSession.GetCurrentBytes();
            Assert.That(overlay.SelectMvpPlacementCategory(MvpDungeonPlacementIds.TrapCategoryId), Is.True);
            Assert.That(overlay.SelectMvpPlacementOption(MvpDungeonPlacementIds.ChillingSigilOptionId), Is.True);
            overlay.PlaceSelectedMvpStructure();
            Assert.That(overlay.MvpStructurePlacementFeedback, Is.Empty);
            Assert.That(root.BannerMessage, Is.EqualTo(root.Content.GetString(
                "ui.banner.place_room_capacity_full", "ui.banner.place_room_capacity_full")));
            Assert.That(root.BannerMessage, Does.Not.Contain("gd66."));
            Assert.That(root.BannerMessage, Does.Not.Contain("save.migration."));
            Assert.That(root.BannerMessage, Does.Not.Contain("placement."));
            Assert.That(root.BannerMessage, Does.Not.Contain("ui."));
            Assert.That(root.BannerMessage,
                Does.Not.Contain("saved room contains more content").IgnoreCase);
            Assert.That(service.CanonicalSession.GetCurrentBytes(), Is.EqualTo(beforeCapacityFailure));
            route = CanonicalMvpRouteProjection.InspectWithProductionContent(
                root.Save, root.ProductionSpatialContent);
            Assert.That(route.Rooms[0].Capacity.TrapCapacity, Is.EqualTo(2));
            Assert.That(route.Rooms[0].AssignedTrapOptionIds, Has.Length.EqualTo(2));
            CollectionAssert.AreEqual(new[]
            {
                MvpDungeonPlacementIds.SpikeTrapOptionId,
                MvpDungeonPlacementIds.SnareTrapOptionId
            }, route.Rooms[0].AssignedTrapOptionIds);
            Assert.That(CaptureLegacySpatialEvidence(root.Save), Is.EqualTo(frozenLegacyEvidence));
        }

        private static void PlaceAndAssertFeedback(BootstrapOverlay overlay, GameRoot root,
            string categoryId, string optionId, string categoryLabel, string optionLabel,
            string frozenLegacyEvidence)
        {
            Assert.That(overlay.SelectMvpPlacementCategory(categoryId), Is.True);
            Assert.That(overlay.SelectMvpPlacementOption(optionId), Is.True);
            overlay.PlaceSelectedMvpStructure();

            string feedback = overlay.MvpStructurePlacementFeedback;
            Assert.That(feedback, Does.Contain(categoryLabel));
            Assert.That(feedback, Does.Contain(optionLabel));
            Assert.That(feedback, Does.Not.Contain("Unknown category"));
            Assert.That(feedback, Does.Not.Contain("Unknown placement"));
            Assert.That(feedback, Does.Not.Contain("Role unavailable"));
            Assert.That(feedback, Does.Not.Contain("placement."));
            Assert.That(feedback, Does.Not.Contain("ui."));
            Assert.That(CaptureLegacySpatialEvidence(root.Save), Is.EqualTo(frozenLegacyEvidence));
        }

        private static string CaptureLegacySpatialEvidence(SaveData save)
        {
            return string.Join("|", new[]
            {
                save.mvpDungeonPlacements == null ? "null" : JsonUtility.ToJson(save.mvpDungeonPlacements),
                save.mvpDungeonFloorLayout == null ? "null" : JsonUtility.ToJson(save.mvpDungeonFloorLayout),
                save.mvpRoomSlotAssignments == null ? "null" : JsonUtility.ToJson(save.mvpRoomSlotAssignments)
            });
        }

        private static void LoadPlayerFacingContent(GameRoot root)
        {
            const string bootstrapRoot = "Assets/_Project/Data/Bootstrap/";
            TextAsset contentBootstrap = RequiredAsset(bootstrapRoot + "content_bootstrap.json");
            TextAsset buildConfig = RequiredAsset(bootstrapRoot + "build_config.json");
            TextAsset schemaVersions = RequiredAsset(bootstrapRoot + "schema_versions.json");
            TextAsset contentManifest = RequiredAsset(bootstrapRoot + "content_manifest.json");
            TextAsset devCommands = RequiredAsset(bootstrapRoot + "dev_commands.json");
            TextAsset strings = RequiredAsset(bootstrapRoot + "string_table_en.json");
            TextAsset heatRuntime = RequiredAsset(bootstrapRoot + "heat_runtime.json");
            root.Content.LoadAll(contentBootstrap, buildConfig, schemaVersions, contentManifest,
                devCommands, strings, heatRuntime, root.Logger, out string warningBanner);
            Assert.That(warningBanner, Is.Empty);
            const string spatialRoot = "Assets/_Project/Data/Production/DungeonSpatial/";
            ProductionSpatialContentLoadResult loaded = root.Content.LoadProductionSpatialContent(
                RequiredAsset(spatialRoot + "content_manifest.json"),
                RequiredAsset(spatialRoot + "dungeon_spatial_content.json"),
                new[] { RequiredAsset(spatialRoot + "string_table_en.json") },
                RequiredAsset(spatialRoot + "validation_limits.json"));
            Assert.That(loaded.Success, Is.True);
        }

        private static TextAsset RequiredAsset(string path)
        {
            TextAsset asset = Asset(path);
            Assert.That(asset, Is.Not.Null, "Required test fixture asset is missing: " + path);
            return asset;
        }

        private GameRoot Root(SaveService service)
        {
            rootObject = new GameObject("GD66 GameRoot integration");
            GameRoot root = rootObject.AddComponent<GameRoot>();
            root.structureSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/structure_simulation_config.json");
            root.runSimulationConfigJson = Asset("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
            root.lootConfigJson = Asset("Assets/_Project/Data/Bootstrap/loot_config.json");
            root.AttachSaveServiceForTests(service);
            return root;
        }

        private static SaveService Service(
            Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture fixture,
            ISpatialMigrationFileSystem fileSystem, string filename, ICollection<string> errors = null)
        {
            var logger = errors == null ? new SimpleLogger(false) :
                new SimpleLogger(false, (level, formatted) =>
                {
                    if (level == "ERROR") errors.Add(formatted);
                });
            var service = new SaveService(logger,
                new SaveConfig { fileName = filename, useAtomicWrites = true },
                Path.GetFullPath(Path.Combine(Path.GetTempPath(), "gd66-root-tests")));
            service.ConfigureCanonical(new SaveSpatialMigrationLimitsProfile(
                    Gd66DetachedSpatialMigrationTransactionTests.RawLimitsForCoordinator,
                    fixture.Limits, fixture.WholeLimits), fixture.Production, fixture.Compatibility,
                LegacyGameplayConfigurationContract.Parse(fixture.LegacyBytes), fixture.LegacyBytes);
            service.SetPreflightEvaluatorForTests(path => new SpatialMigrationActivationPreflight(true,
                SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor,
                fileSystem, Path.GetFullPath(path)));
            return service;
        }

        private const string NarrowHallLoadError =
            "[ERROR] GD66 load failed: gd66.content.migration_blocked_narrow_hall";

        private static Gd66DetachedSpatialMigrationTransactionTests.PreparedFixture Fixture() =>
            Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);

        private static TextAsset Asset(string path) => AssetDatabase.LoadAssetAtPath<TextAsset>(path);

    }
}
#endif
