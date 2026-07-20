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

        private static MvpOrderedRouteRoom EmptyRoom(int index) => new MvpOrderedRouteRoom {
            FloorIndex = 0, RoomIndex = index, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
            IncludeRoomPlacement = true, HasActiveContent = false, Capacity = new MvpRoomSlotCapacity()
        };
    }
}
#endif
