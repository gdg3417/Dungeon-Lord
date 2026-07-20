using DungeonBuilder.M0.Gameplay.RunSimulation;
using System.Collections.Generic;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class MvpRouteResultPresenterTests
    {
        [Test]
        public void BuildCompactText_LocalizesOutcomeAndDepthWithoutRawIds()
        {
            var strings = new Dictionary<string, string> {
                [MvpRouteResultPresenter.RouteFormatKey] = "Route result: {0}",
                [MvpRouteResultPresenter.DepthFormatKey] = "Depth reached: {0}.",
                [MvpRouteResultPresenter.RoomNumberFormatKey] = "Room {0}",
                [RunSimulationService.RouteStoppedRoomTwoKey] = "The party stopped in the second room."
            };
            var summary = new MvpPlayerLoopSummary { ConfiguredRoomCount = 2, ReachedRoomCount = 2, HighestRoomReached = 1,
                FinalRouteOutcomeKey = RunSimulationService.RouteStoppedRoomTwoKey };
            string text = MvpRouteResultPresenter.BuildCompactText(summary, (key, fallback) => strings.TryGetValue(key, out string value) ? value : fallback);
            Assert.That(text, Is.EqualTo("Route result: The party stopped in the second room.\nDepth reached: Room 2."));
            Assert.That(text, Does.Not.Contain("run.route"));
        }

        [Test]
        public void BuildCompactText_ConfiguredTwoRoomRouteStoppedInRoomOneStillRenders()
        {
            var strings = new Dictionary<string, string> {
                [MvpRouteResultPresenter.RouteFormatKey] = "Route result: {0}", [MvpRouteResultPresenter.DepthFormatKey] = "Depth reached: {0}.",
                [MvpRouteResultPresenter.RoomNumberFormatKey] = "Room {0}", [RunSimulationService.RouteWipedKey] = "Party wiped." };
            var summary = new MvpPlayerLoopSummary { ConfiguredRoomCount = 2, ReachedRoomCount = 1, HighestRoomReached = 0, FinalRouteOutcomeKey = RunSimulationService.RouteWipedKey };
            string text = MvpRouteResultPresenter.BuildCompactText(summary, (key, fallback) => strings.TryGetValue(key, out string value) ? value : fallback);
            Assert.That(text, Does.Contain("Route result: Party wiped."));
            Assert.That(text, Does.Contain("Depth reached: Room 1."));
        }
    }
}
