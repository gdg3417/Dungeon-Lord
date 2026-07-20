#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class OrderedRouteSaveServiceTests
    {
        [Test]
        public void SaveService_TwoRoomOutcomeRoundTripsOrderedRouteEvidence()
        {
            string directory = Path.Combine(Path.GetTempPath(), "gd60-route-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "route.json", useAtomicWrites = false }, directory);
                SaveData save = service.LoadOrCreate("gd60-test", out _);
                save.runHistory = new RunHistoryState();
                save.runHistory.AppendOutcome(new RunOutcomeRecord {
                    RunId = "run-1", TickStarted = 12, ConfiguredRoomCount = 2, ReachedRoomCount = 2, ClearedRoomCount = 1, HighestRoomReached = 1,
                    FinalRouteOutcomeKey = Gameplay.RunSimulation.RunSimulationService.RouteStoppedRoomTwoKey,
                    ConfiguredRoutePlacementEffects = Effects(5), ReachedRoutePlacementEffects = Effects(5), ClearedRewardPlacementEffects = Effects(2),
                    SurvivalSummary = new RunSurvivalSummary { PartySize = 3, SurvivorCount = 1, DeathCount = 2, RuleResolved = true },
                    LootSummary = new RunLootSummary { ResolverSuccess = true, TotalGeneratedWorldValue = 9 },
                    LootExtractionSummary = new RunLootExtractionSummary { RuleResolved = true, TotalExtractedWorldValue = 4 },
                    RunHeatApplicationSummary = new RunHeatApplicationSummary { RuleResolved = true, HeatBefore = 7, HeatAfter = 9 },
                    RoomResolutions = new[] { Room(0, 3, 2, 1), Room(1, 2, 1, 1) }
                }, 5);
                service.Save(save, SaveReason.ManualDev);
                SaveData loaded = service.LoadOrCreate("gd60-test", out _);
                RunOutcomeRecord outcome = loaded.runHistory.LatestOutcome;
                Assert.That(outcome.ConfiguredRoomCount, Is.EqualTo(2));
                Assert.That(outcome.RoomResolutions[0].RoomIndex, Is.EqualTo(0));
                Assert.That(outcome.RoomResolutions[1].RoomIndex, Is.EqualTo(1));
                Assert.That(outcome.SurvivalSummary.DeathCount, Is.EqualTo(outcome.RoomResolutions[0].Deaths + outcome.RoomResolutions[1].Deaths));
                Assert.That(outcome.ConfiguredRoutePlacementEffects.Danger, Is.EqualTo(5));
                Assert.That(outcome.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(9));
                Assert.That(outcome.LootExtractionSummary.TotalExtractedWorldValue, Is.EqualTo(4));
                Assert.That(outcome.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(9));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void SaveService_LegacyOutcomeWithoutRouteFieldsLoadsSafely()
        {
            string directory = Path.Combine(Path.GetTempPath(), "gd60-legacy-" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new SaveService(new SimpleLogger(false), new SaveConfig { fileName = "legacy.json", useAtomicWrites = false }, directory);
                SaveData save = service.LoadOrCreate("legacy", out _); save.runHistory = new RunHistoryState();
                save.runHistory.AppendOutcome(new RunOutcomeRecord { RunId = "legacy", CompositionOutcomeSummary = new RunCompositionOutcomeSummary { PlacementEffects = Effects(1) } }, 5);
                service.Save(save, SaveReason.ManualDev);
                RunOutcomeRecord loaded = service.LoadOrCreate("legacy", out _).runHistory.LatestOutcome;
                Assert.That(loaded.ConfiguredRoomCount, Is.Zero);
                Assert.That(loaded.RoomResolutions, Is.Empty);
                Assert.That(loaded.CompositionOutcomeSummary.PlacementEffects.Danger, Is.EqualTo(1));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static MvpPlacementEffectsSummary Effects(int danger) => new MvpPlacementEffectsSummary { RuleResolved = true, Danger = danger };
        private static RunRoomResolutionSummary Room(int index, int entering, int survivors, int deaths) => new RunRoomResolutionSummary { FloorIndex = 0, RoomIndex = index, Reached = true, PartyEntering = entering, SurvivorsLeaving = survivors, Deaths = deaths };
    }
}
#endif
