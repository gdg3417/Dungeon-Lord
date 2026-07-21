#if UNITY_EDITOR
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class OrderedRouteRunSimulationTests
    {
        [Test]
        public void TwoEmptyRooms_TraverseWithoutLootDeathsOrHeatMutation()
        {
            var config = new RunSimulationConfig { BaseSuccessChance = 0.6d, SuccessThreshold = 0.5d, BaseScoreOnSuccess = 10,
                MinPartySize = 3, MaxPartySize = 3, MaxAllowedPartySize = 3, SuccessSurvivorRatio = 1d, FailureSurvivorRatio = 0d };
            var service = new RunSimulationService(config);
            var runtime = new StructureRuntimeState { Heat = 17d, ManaReserve = 4d };
            var route = new[] { EmptyRoom(0), EmptyRoom(1) };

            RunOutcomeRecord outcome = service.SimulateRoute(runtime, 123L, 7, RunPostureResolver.BalancedId, route);

            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.Score, Is.Zero);
            Assert.That(outcome.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteNoEncounterKey));
            Assert.That(outcome.ReasonKey, Is.EqualTo(RunSimulationService.NoEncounterReasonKey));
            Assert.That(outcome.ConfiguredRoomCount, Is.EqualTo(2));
            Assert.That(outcome.ReachedRoomCount, Is.EqualTo(2));
            Assert.That(outcome.ClearedRoomCount, Is.EqualTo(2));
            Assert.That(outcome.SurvivalSummary.DeathCount, Is.Zero);
            Assert.That(outcome.LootSummary.TotalGeneratedWorldValue, Is.Zero);
            Assert.That(outcome.RunHeatApplicationSummary.AppliedDelta, Is.Zero);
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(17d));
            Assert.That(outcome.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(17d));
            Assert.That(runtime.Heat, Is.EqualTo(17d));
            Assert.That(outcome.RoomResolutions[0].PartyEntering, Is.EqualTo(outcome.RoomResolutions[1].PartyEntering));
        }

        [Test]
        public void TwoEmptyRooms_AreDeterministicForIdenticalInputs()
        {
            var config = new RunSimulationConfig { BaseSuccessChance = 0.6d, SuccessThreshold = 0.5d, BaseScoreOnSuccess = 10,
                MinPartySize = 2, MaxPartySize = 4, MaxAllowedPartySize = 4, SuccessSurvivorRatio = 1d, FailureSurvivorRatio = 0d };
            var service = new RunSimulationService(config);
            RunOutcomeRecord first = service.SimulateRoute(new StructureRuntimeState { Heat = 8d }, 44L, 2, RunPostureResolver.BalancedId, new[] { EmptyRoom(0), EmptyRoom(1) });
            RunOutcomeRecord second = service.SimulateRoute(new StructureRuntimeState { Heat = 8d }, 44L, 2, RunPostureResolver.BalancedId, new[] { EmptyRoom(0), EmptyRoom(1) });
            Assert.That(second.SurvivalSummary.PartySize, Is.EqualTo(first.SurvivalSummary.PartySize));
            Assert.That(second.FinalRouteOutcomeKey, Is.EqualTo(first.FinalRouteOutcomeKey));
            Assert.That(second.RoomResolutions[1].DeterministicSeed, Is.EqualTo(first.RoomResolutions[1].DeterministicSeed));
        }

        [Test]
        public void ClearedRoomOnePressureWipe_OverridesClearAndPreventsRoomTwo()
        {
            var config = new RunSimulationConfig { BaseSuccessChance = 1d, SuccessThreshold = 0.5d, BaseScoreOnSuccess = 100,
                MinPartySize = 3, MaxPartySize = 3, MaxAllowedPartySize = 3, SuccessSurvivorRatio = 1d, FailureSurvivorRatio = 0d,
                CasualtyPressureRuleSourceId = "test.casualty", CasualtyPressurePerDanger = 1d, CasualtyPressureMinimum = 0d, CasualtyPressureMaximum = 1d,
                BalancedCasualtyPressureMultiplier = 1d, PartyWipeCasualtyPressureThreshold = 0.5d,
                MvpPlacementEffects = new[] { new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 1 } } };
            var service = new RunSimulationService(config);
            MvpOrderedRouteRoom dangerous = EmptyRoom(0); dangerous.HasActiveContent = true; dangerous.AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId };
            RunOutcomeRecord outcome = service.SimulateRoute(new StructureRuntimeState(), 9L, 1, RunPostureResolver.BalancedId, new[] { dangerous, EmptyRoom(1) });
            Assert.That(outcome.RoomResolutions, Has.Length.EqualTo(1));
            Assert.That(outcome.RoomResolutions[0].Cleared, Is.True);
            Assert.That(outcome.SurvivalSummary.SurvivorCount, Is.Zero);
            Assert.That(outcome.SurvivalSummary.SuccessAtResolution, Is.False);
            Assert.That(outcome.Success, Is.False);
            Assert.That(outcome.Score, Is.Zero);
            Assert.That(outcome.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteWipedKey));
            Assert.That(outcome.ReasonKey, Is.EqualTo(RunSimulationService.PartyWipedReasonKey));
            Assert.That(outcome.LootExtractionSummary.TotalExtractedWorldValue, Is.Zero);
            Assert.That(outcome.ClearedRoomCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoClearedRooms_CarryExactSurvivorsAndPreserveOriginalTick()
        {
            var config = new RunSimulationConfig { BaseSuccessChance = 1d, SuccessThreshold = 0.5d, BaseScoreOnSuccess = 10,
                MinPartySize = 3, MaxPartySize = 3, MaxAllowedPartySize = 3, SuccessSurvivorRatio = 1d, FailureSurvivorRatio = 0d };
            var service = new RunSimulationService(config);
            MvpOrderedRouteRoom first = EmptyRoom(0); first.HasActiveContent = true; first.AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId };
            MvpOrderedRouteRoom second = EmptyRoom(1); second.HasActiveContent = true; second.AssignedMonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId };
            RunOutcomeRecord outcome = service.SimulateRoute(new StructureRuntimeState(), 456L, 3, RunPostureResolver.BalancedId, new[] { first, second });
            Assert.That(outcome.TickStarted, Is.EqualTo(456L));
            Assert.That(outcome.RoomResolutions, Has.Length.EqualTo(2));
            Assert.That(outcome.RoomResolutions[1].PartyEntering, Is.EqualTo(outcome.RoomResolutions[0].SurvivorsLeaving));
            Assert.That(outcome.SurvivalSummary.PartySize, Is.EqualTo(outcome.RoomResolutions[0].PartyEntering));
            Assert.That(outcome.Success, Is.True);
            Assert.That(outcome.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteClearedKey));
        }

        [Test]
        public void PartialRoomOneCasualties_CarryExactSurvivorsIntoRoomTwo()
        {
            RunSimulationConfig config = BaseConfig(1d, 1d);
            config.CasualtyPressureRuleSourceId = "test.casualty"; config.CasualtyPressurePerDanger = 0.34d; config.CasualtyPressureMaximum = 1d;
            config.BalancedCasualtyPressureMultiplier = 1d; config.PartyWipeCasualtyPressureThreshold = 0.9d;
            config.MvpPlacementEffects = new[] { new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 1 } };
            var service = new RunSimulationService(config); MvpOrderedRouteRoom first = ActiveRoom(0, MvpDungeonPlacementIds.SkeletonOptionId); MvpOrderedRouteRoom second = ActiveRoom(1, MvpDungeonPlacementIds.GoblinOptionId);
            RunOutcomeRecord outcome = service.SimulateRoute(new StructureRuntimeState(), 1L, 1, RunPostureResolver.BalancedId, new[] { first, second });
            Assert.That(outcome.RoomResolutions[1].PartyEntering, Is.EqualTo(outcome.RoomResolutions[0].SurvivorsLeaving));
            Assert.That(outcome.RoomResolutions[1].PartyEntering, Is.LessThan(outcome.SurvivalSummary.PartySize));
            Assert.That(outcome.SurvivalSummary.DeathCount, Is.EqualTo(outcome.RoomResolutions[0].Deaths + outcome.RoomResolutions[1].Deaths));
        }

        [Test]
        public void NonWipeRoomOneFailure_StopsBeforeRoomTwo()
        {
            RunSimulationConfig config = BaseConfig(0d, 0.67d); config.SuccessThreshold = 1d;
            var service = new RunSimulationService(config);
            RunOutcomeRecord outcome = service.SimulateRoute(new StructureRuntimeState(), 2L, 1, RunPostureResolver.BalancedId, new[] { ActiveRoom(0, MvpDungeonPlacementIds.SkeletonOptionId), ActiveRoom(1, MvpDungeonPlacementIds.GoblinOptionId) });
            Assert.That(outcome.Success, Is.False); Assert.That(outcome.SurvivalSummary.SurvivorCount, Is.GreaterThan(0));
            Assert.That(outcome.RoomResolutions, Has.Length.EqualTo(1)); Assert.That(outcome.ReachedRoomCount, Is.EqualTo(1));
            Assert.That(outcome.HighestRoomReached, Is.Zero); Assert.That(outcome.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteStoppedRoomOneKey));
        }

        [Test]
        public void ClearedRoomOneLoot_IsCarriedAndExtractedOnceWhenRoomTwoFails()
        {
            RunSimulationConfig config = BaseConfig(1d, 1d); config.LootTableId = "loot.test"; config.LootExtractionRoundingPolicyId = "loot_extraction.round_floor";
            config.MvpCompositionOutcomeTuning = new MvpCompositionOutcomeTuningConfig { SuccessChancePenaltyPerDanger = 0.2d, GeneratedLootMultiplierPerLootBonus = 0d, ExtractedLootMultiplierPerLootBonus = 0d };
            config.MvpPlacementEffects = new[] { new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.GoblinOptionId, Danger = 10 } };
            LootConfig loot = TestLootConfig(); var service = new RunSimulationService(config, loot);
            RunOutcomeRecord outcome = service.SimulateRoute(new StructureRuntimeState(), 3L, 1, RunPostureResolver.BalancedId, new[] { ActiveRoom(0, MvpDungeonPlacementIds.SkeletonOptionId), ActiveRoom(1, MvpDungeonPlacementIds.GoblinOptionId) });
            Assert.That(outcome.RoomResolutions, Has.Length.EqualTo(2)); Assert.That(outcome.RoomResolutions[0].GeneratedLootValue, Is.EqualTo(5));
            Assert.That(outcome.RoomResolutions[1].GeneratedLootValue, Is.Zero); Assert.That(outcome.LootSummary.GeneratedItemIds, Is.EqualTo(new[] { "loot.test.item" }));
            Assert.That(outcome.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(5)); Assert.That(outcome.LootExtractionSummary.GeneratedItemCount, Is.EqualTo(1));
            Assert.That(outcome.LootExtractionSummary.TotalExtractedWorldValue, Is.EqualTo(5)); Assert.That(outcome.LootBreakdown[0].TotalWorldValue, Is.EqualTo(5));
        }

        [Test]
        public void OneRoomRoute_MatchesSimulateOnceAggregateFields()
        {
            RunSimulationConfig config = BaseConfig(0.8d, 0.5d); config.LootTableId = "loot.test"; config.LootExtractionRoundingPolicyId = "loot_extraction.round_floor";
            LootConfig loot = TestLootConfig(); var service = new RunSimulationService(config, loot); var effects = new MvpPlacementEffectsSummary { RuleResolved = true };
            RunOutcomeRecord once = service.SimulateOnce(new StructureRuntimeState { Heat = 2d, ManaReserve = 1d }, 4L, 2, RunPostureResolver.BalancedId, effects);
            RunOutcomeRecord route = service.SimulateRoute(new StructureRuntimeState { Heat = 2d, ManaReserve = 1d }, 4L, 2, RunPostureResolver.BalancedId, new[] { ActiveRoom(0, MvpDungeonPlacementIds.SkeletonOptionId) });
            Assert.That(route.Success, Is.EqualTo(once.Success)); Assert.That(route.Score, Is.EqualTo(once.Score)); Assert.That(route.ReasonKey, Is.EqualTo(once.ReasonKey));
            Assert.That(route.SurvivalSummary.PartySize, Is.EqualTo(once.SurvivalSummary.PartySize)); Assert.That(route.SurvivalSummary.SurvivorCount, Is.EqualTo(once.SurvivalSummary.SurvivorCount)); Assert.That(route.SurvivalSummary.DeathCount, Is.EqualTo(once.SurvivalSummary.DeathCount));
            Assert.That(route.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(once.LootSummary.TotalGeneratedWorldValue)); Assert.That(route.LootExtractionSummary.TotalExtractedWorldValue, Is.EqualTo(once.LootExtractionSummary.TotalExtractedWorldValue)); Assert.That(route.LootExtractionSummary.TotalExtractedTradeableWorldValue, Is.EqualTo(once.LootExtractionSummary.TotalExtractedTradeableWorldValue));
            Assert.That(route.HeatAtStart, Is.EqualTo(once.HeatAtStart)); Assert.That(route.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(once.RunHeatApplicationSummary.HeatAfter)); Assert.That(route.FinalChance, Is.EqualTo(once.FinalChance)); Assert.That(route.RunPostureId, Is.EqualTo(once.RunPostureId));
        }

        private static RunSimulationConfig BaseConfig(double chance, double failureRatio) => new RunSimulationConfig { BaseSuccessChance = chance, SuccessThreshold = 0.5d, BaseScoreOnSuccess = 10, MinPartySize = 3, MaxPartySize = 3, MaxAllowedPartySize = 3, SuccessSurvivorRatio = 1d, FailureSurvivorRatio = failureRatio };
        private static MvpOrderedRouteRoom ActiveRoom(int index, string monster) { MvpOrderedRouteRoom room = EmptyRoom(index); room.HasActiveContent = true; room.AssignedMonsterOptionIds = new[] { monster }; return room; }
        private static LootConfig TestLootConfig() => new LootConfig { items = new[] { new LootItemRecord { id = "loot.test.item", worldValue = 5, reserveCost = 1, isTradeable = true, nameKey = "loot.test.name" } }, tables = new[] { new LootTableRecord { id = "loot.test", minRollCount = 1, maxRollCount = 1, pool = new[] { new LootTablePoolEntry { itemId = "loot.test.item", weight = 1d } } } } };

        private static MvpOrderedRouteRoom EmptyRoom(int index) => new MvpOrderedRouteRoom {
            FloorIndex = 0, RoomIndex = index, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
            IncludeRoomPlacement = true, HasActiveContent = false, Capacity = new MvpRoomSlotCapacity()
        };
    }
}
#endif
