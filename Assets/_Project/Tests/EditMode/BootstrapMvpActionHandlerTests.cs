using System;
using System.Collections.Generic;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public sealed class BootstrapMvpActionHandlerTests
    {
        private string _banner;
        private string _lastPosture;
        private int _summaryCallCount;
        private MvpPlayerLoopSummary _beforeSummary;
        private MvpPlayerLoopSummary _afterSummary;

        [SetUp]
        public void SetUp()
        {
            _banner = string.Empty;
            _lastPosture = string.Empty;
            _summaryCallCount = 0;
            _beforeSummary = BuildSummary(null, hasOutcome: false);
            _afterSummary = BuildSummary(null, hasOutcome: true);
        }

        [Test]
        public void PlaceOrModifySelectedMvpPlacement_WhenSuccessful_ReturnsLocalizedPlacementFeedbackAndBanner()
        {
            var prior = new MvpDungeonPlacementEntry { CategoryId = MvpDungeonPlacementIds.RoomCategoryId, OptionId = MvpDungeonPlacementIds.BasicRoomOptionId };
            var next = new MvpDungeonPlacementEntry { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId };
            BootstrapMvpActionHandler handler = CreateHandler(place: (category, option) =>
                new BootstrapMvpActionHandler.PlacementAttempt(true, prior, next, "ui.banner.place_success"));

            BootstrapMvpActionHandler.PlacementResult result = handler.PlaceOrModifySelectedMvpPlacement(
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PlacementFeedback, Is.EqualTo("Placement changed from Room / Basic Room to Monster / Skeleton. Skeleton guards the route."));
            Assert.That(_banner, Is.EqualTo(result.PlacementFeedback));
        }

        [Test]
        public void PlaceOrModifySelectedMvpPlacement_WhenRoomTargetedFeedbackExists_DoesNotDuplicateFeedbackAsBanner()
        {
            var next = new MvpDungeonPlacementEntry { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId };
            BootstrapMvpActionHandler handler = CreateHandler(place: (category, option) =>
                new BootstrapMvpActionHandler.PlacementAttempt(true, null, next, "ui.banner.place_success", "Changed Room 1 Monster: Goblin -> Skeleton.", string.Empty));

            BootstrapMvpActionHandler.PlacementResult result = handler.PlaceOrModifySelectedMvpPlacement(
                MvpDungeonPlacementIds.MonsterCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PlacementFeedback, Is.EqualTo("Changed Room 1 Monster: Goblin -> Skeleton."));
            Assert.That(result.BannerMessage, Is.Empty);
            Assert.That(_banner, Is.Empty);
        }

        [Test]
        public void PlaceOrModifySelectedMvpPlacement_WhenSelectionInvalid_RejectsWithoutRawIdLeakage()
        {
            BootstrapMvpActionHandler handler = CreateHandler(place: (category, option) =>
                throw new InvalidOperationException("Invalid placement should be rejected before orchestration."));

            BootstrapMvpActionHandler.PlacementResult result = handler.PlaceOrModifySelectedMvpPlacement(
                MvpDungeonPlacementIds.RoomCategoryId,
                MvpDungeonPlacementIds.SkeletonOptionId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.PlacementFeedback, Is.Empty);
            Assert.That(result.BannerMessage, Is.EqualTo("Placement failed."));
            Assert.That(result.BannerMessage, Does.Not.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(_banner, Is.EqualTo("Placement failed."));
        }

        [Test]
        public void RunOrObserveDungeon_WhenIntentResolved_UsesResolvedIntentInsteadOfSelectedDebugPosture()
        {
            _beforeSummary = BuildSummary(new AdventurerRunIntentSummary
            {
                RuleResolved = true,
                IntentId = RunPostureResolver.GreedyId,
                PostureId = RunPostureResolver.GreedyId,
                RuleSourceId = "intent.rule"
            }, hasOutcome: false);
            BootstrapMvpActionHandler handler = CreateHandler();

            BootstrapMvpActionHandler.RunResult result = handler.RunOrObserveDungeon(RunPostureResolver.CautiousId);

            Assert.That(result.DidRun, Is.True);
            Assert.That(result.PostureUsedId, Is.EqualTo(RunPostureResolver.GreedyId));
            Assert.That(result.DebugPostureId, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(result.IntentFallbackUsed, Is.False);
            Assert.That(_lastPosture, Is.EqualTo(RunPostureResolver.GreedyId));
            Assert.That(result.RunFeedback, Does.Contain("Latest visit intent: Greedy. Challenge posture used: Greedy. Debug selected posture: Cautious."));
            Assert.That(result.RunFeedback.Split(new[] { "Challenge posture used:" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
        }

        [Test]
        public void RunOrObserveDungeon_WhenIntentResolutionFails_FallsBackToSelectedDebugPosture()
        {
            _beforeSummary = BuildSummary(new AdventurerRunIntentSummary
            {
                RuleResolved = false,
                PostureId = RunPostureResolver.GreedyId
            }, hasOutcome: false);
            BootstrapMvpActionHandler handler = CreateHandler();

            BootstrapMvpActionHandler.RunResult result = handler.RunOrObserveDungeon(RunPostureResolver.CautiousId);

            Assert.That(result.DidRun, Is.True);
            Assert.That(result.PostureUsedId, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(result.DebugPostureId, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(result.IntentFallbackUsed, Is.True);
            Assert.That(_lastPosture, Is.EqualTo(RunPostureResolver.CautiousId));
            Assert.That(result.RunFeedback, Does.Contain("Latest visit intent unavailable. Challenge posture used debug fallback: Cautious. Debug selected posture: Cautious."));
            Assert.That(result.RunFeedback.Split(new[] { "Challenge posture used" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
        }

        [Test]
        public void RunOrObserveDungeon_UpdatesRunFeedbackAndLatestIntentEvidenceFields()
        {
            AdventurerRunIntentSummary intent = new AdventurerRunIntentSummary
            {
                RuleResolved = true,
                IntentId = RunPostureResolver.BalancedId,
                PostureId = RunPostureResolver.BalancedId,
                RuleSourceId = "intent.rule"
            };
            _beforeSummary = BuildSummary(intent, hasOutcome: false);
            BootstrapMvpActionHandler handler = CreateHandler();

            BootstrapMvpActionHandler.RunResult result = handler.RunOrObserveDungeon(RunPostureResolver.GreedyId);

            Assert.That(result.RunFeedback, Does.Contain("Run feedback: success"));
            Assert.That(result.IntentSummary, Is.SameAs(intent));
            Assert.That(result.PostureUsedId, Is.EqualTo(RunPostureResolver.BalancedId));
            Assert.That(result.DebugPostureId, Is.EqualTo(RunPostureResolver.GreedyId));
            Assert.That(result.IntentFallbackUsed, Is.False);
            Assert.That(_banner, Is.EqualTo("Run simulated."));
        }

        [Test]
        public void RunOrObserveDungeon_DoesNotOverwritePlacementFeedbackOwnedByCaller()
        {
            string placementFeedback = "Existing placement feedback";
            BootstrapMvpActionHandler handler = CreateHandler();

            BootstrapMvpActionHandler.RunResult result = handler.RunOrObserveDungeon(RunPostureResolver.BalancedId);

            Assert.That(result.RunFeedback, Is.Not.Empty);
            Assert.That(placementFeedback, Is.EqualTo("Existing placement feedback"));
        }

        [Test]
        public void ActionHandling_DoesNotMutateDiagnosticsOrViewOnlyScrollState()
        {
            int diagnosticsScroll = 7;
            int playerScroll = 3;
            BootstrapMvpActionHandler handler = CreateHandler();

            handler.PlaceOrModifySelectedMvpPlacement(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId);
            handler.RunOrObserveDungeon(RunPostureResolver.BalancedId);

            Assert.That(diagnosticsScroll, Is.EqualTo(7));
            Assert.That(playerScroll, Is.EqualTo(3));
        }

        private BootstrapMvpActionHandler CreateHandler(Func<string, string, BootstrapMvpActionHandler.PlacementAttempt> place = null)
        {
            return new BootstrapMvpActionHandler(new BootstrapMvpActionHandler.Context(
                Localize,
                place ?? ((category, option) => new BootstrapMvpActionHandler.PlacementAttempt(
                    true,
                    null,
                    new MvpDungeonPlacementEntry { CategoryId = category, OptionId = option },
                    "ui.banner.place_success")),
                () => _summaryCallCount++ == 0 ? _beforeSummary : _afterSummary,
                postureId => { _lastPosture = postureId; return true; },
                message => _banner = message));
        }

        private static MvpPlayerLoopSummary BuildSummary(AdventurerRunIntentSummary intent, bool hasOutcome)
        {
            return new MvpPlayerLoopSummary
            {
                RuleResolved = true,
                HasRunOutcome = hasOutcome,
                RunSucceeded = hasOutcome,
                ManaReserve = 12d,
                LootGeneratedWorldValue = 5,
                LootExtractedWorldValue = 4,
                LootExtractedTradeableWorldValue = 3,
                HeatBefore = 8d,
                HeatAfter = 6d,
                AdventurerRunIntent = intent ?? new AdventurerRunIntentSummary { RuleResolved = false }
            };
        }

        private static string Localize(string key, string fallback)
        {
            var strings = new Dictionary<string, string>
            {
                ["ui.banner.place_failed"] = "Placement failed.",
                ["ui.banner.place_success"] = "Placement succeeded.",
                ["ui.banner.run_simulated"] = "Run simulated.",
                ["ui.banner.run_sim_failed"] = "Run failed.",
                [MvpStructurePlacementFeedbackPresenter.EmptySlotKey] = "Empty slot",
                [MvpStructurePlacementFeedbackPresenter.EmptyPlacementValueKey] = "Empty",
                [MvpStructurePlacementFeedbackPresenter.RoomTargetedPlacementChangedFormatKey] = "Changed Room {0} {1}: {2} -> {3}.",
                [MvpStructurePlacementFeedbackPresenter.PlacementChangedFormatKey] = "Placement changed from {0} to {1} / {2}. {3}",
                [MvpDungeonPlacementPresenter.EntryFormatKey] = "{0} / {1}",
                [MvpDungeonPlacementPresenter.RoomCategoryKey] = "Room",
                [MvpDungeonPlacementPresenter.MonsterCategoryKey] = "Monster",
                [MvpDungeonPlacementPresenter.BasicRoomOptionKey] = "Basic Room",
                [MvpDungeonPlacementPresenter.SkeletonOptionKey] = "Skeleton",
                [MvpDungeonPlacementPresenter.BasicRoomPreviewKey] = "Basic room anchors the route.",
                [MvpDungeonPlacementPresenter.SkeletonPreviewKey] = "Skeleton guards the route.",
                [AdventurerRunIntentPresenter.RunPostureUsedFormatKey] = "Latest visit intent: {0}. Challenge posture used: {1}. Debug selected posture: {2}.",
                [AdventurerRunIntentPresenter.FallbackRunPostureUsedFormatKey] = "Latest visit intent unavailable. Challenge posture used debug fallback: {0}. Debug selected posture: {1}.",
                [MvpRunResultFeedbackPresenter.FormatKey] = "Run feedback: {0}; mana {1}; loot {2}/{3}/{4}; heat {5}->{6}.",
                [MvpRunResultFeedbackPresenter.SuccessHeatReducedKey] = "success",
                [MvpRunResultFeedbackPresenter.OutcomeCueControlledLootKey] = "controlled loot",
                [MvpRunResultFeedbackPresenter.OutcomeCueFormatKey] = "{0} Cue: {1}",
                [MvpRunResultFeedbackPresenter.PostureFormatKey] = "{0}: {1}",
                ["run.posture.cautious.name"] = "Cautious",
                ["run.posture.balanced.name"] = "Balanced",
                ["run.posture.greedy.name"] = "Greedy"
            };

            return strings.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
