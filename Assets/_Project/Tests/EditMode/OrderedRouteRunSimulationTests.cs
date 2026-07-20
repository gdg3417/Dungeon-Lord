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

        private static MvpOrderedRouteRoom EmptyRoom(int index) => new MvpOrderedRouteRoom {
            FloorIndex = 0, RoomIndex = index, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId,
            IncludeRoomPlacement = true, HasActiveContent = false, Capacity = new MvpRoomSlotCapacity()
        };
    }
}
#endif
