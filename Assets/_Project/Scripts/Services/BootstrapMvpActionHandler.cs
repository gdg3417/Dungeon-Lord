using System;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0
{
    public sealed class BootstrapMvpActionHandler
    {
        public readonly struct Context
        {
            public Context(
                Func<string, string, string> localize,
                Func<string, string, PlacementAttempt> placeOrModifySelectedPlacement,
                Func<MvpPlayerLoopSummary> resolveMvpPlayerLoopSummary,
                Func<string, bool> simulateMvpActiveLoopOnce,
                Action<string> setBanner)
            {
                Localize = localize;
                PlaceOrModifySelectedPlacement = placeOrModifySelectedPlacement;
                ResolveMvpPlayerLoopSummary = resolveMvpPlayerLoopSummary;
                SimulateMvpActiveLoopOnce = simulateMvpActiveLoopOnce;
                SetBanner = setBanner;
            }

            public Func<string, string, string> Localize { get; }
            public Func<string, string, PlacementAttempt> PlaceOrModifySelectedPlacement { get; }
            public Func<MvpPlayerLoopSummary> ResolveMvpPlayerLoopSummary { get; }
            public Func<string, bool> SimulateMvpActiveLoopOnce { get; }
            public Action<string> SetBanner { get; }
        }

        public readonly struct PlacementAttempt
        {
            public PlacementAttempt(bool succeeded, MvpDungeonPlacementEntry priorEntry, MvpDungeonPlacementEntry newEntry, string bannerKey)
                : this(succeeded, priorEntry, newEntry, bannerKey, string.Empty, string.Empty)
            {
            }

            public PlacementAttempt(bool succeeded, MvpDungeonPlacementEntry priorEntry, MvpDungeonPlacementEntry newEntry, string bannerKey, string targetFeedback, string failureFeedback)
            {
                Succeeded = succeeded;
                PriorEntry = priorEntry;
                NewEntry = newEntry;
                BannerKey = bannerKey;
                TargetFeedback = targetFeedback ?? string.Empty;
                FailureFeedback = failureFeedback ?? string.Empty;
            }

            public bool Succeeded { get; }
            public MvpDungeonPlacementEntry PriorEntry { get; }
            public MvpDungeonPlacementEntry NewEntry { get; }
            public string BannerKey { get; }
            public string TargetFeedback { get; }
            public string FailureFeedback { get; }
        }

        public readonly struct PlacementResult
        {
            public PlacementResult(bool succeeded, string placementFeedback, string bannerMessage)
            {
                Succeeded = succeeded;
                PlacementFeedback = placementFeedback ?? string.Empty;
                BannerMessage = bannerMessage ?? string.Empty;
            }

            public bool Succeeded { get; }
            public string PlacementFeedback { get; }
            public string BannerMessage { get; }
        }

        public readonly struct RunResult
        {
            public RunResult(
                bool didRun,
                string runFeedback,
                AdventurerRunIntentSummary intentSummary,
                string postureUsedId,
                string debugPostureId,
                bool intentFallbackUsed,
                string bannerMessage)
            {
                DidRun = didRun;
                RunFeedback = runFeedback ?? string.Empty;
                IntentSummary = intentSummary;
                PostureUsedId = postureUsedId ?? string.Empty;
                DebugPostureId = debugPostureId ?? string.Empty;
                IntentFallbackUsed = intentFallbackUsed;
                BannerMessage = bannerMessage ?? string.Empty;
            }

            public bool DidRun { get; }
            public string RunFeedback { get; }
            public AdventurerRunIntentSummary IntentSummary { get; }
            public string PostureUsedId { get; }
            public string DebugPostureId { get; }
            public bool IntentFallbackUsed { get; }
            public string BannerMessage { get; }
        }

        private readonly Context _context;

        public BootstrapMvpActionHandler(Context context)
        {
            _context = context;
        }

        public PlacementResult PlaceOrModifySelectedMvpPlacement(string categoryId, string optionId)
        {
            if (!IsValidPlacementSelection(categoryId, optionId) || _context.PlaceOrModifySelectedPlacement == null)
            {
                string failedMessage = Localize("ui.banner.place_failed");
                _context.SetBanner?.Invoke(failedMessage);
                return new PlacementResult(false, string.Empty, failedMessage);
            }

            PlacementAttempt attempt = _context.PlaceOrModifySelectedPlacement(categoryId, optionId);
            string message = Localize(attempt.BannerKey);
            if (!attempt.Succeeded)
            {
                string failureFeedback = string.IsNullOrWhiteSpace(attempt.FailureFeedback) ? string.Empty : attempt.FailureFeedback;
                _context.SetBanner?.Invoke(string.IsNullOrWhiteSpace(failureFeedback) ? message : failureFeedback);
                return new PlacementResult(false, failureFeedback, string.IsNullOrWhiteSpace(failureFeedback) ? message : failureFeedback);
            }

            string changedFeedback = MvpStructurePlacementFeedbackPresenter.BuildPlacementFeedbackText(
                attempt.PriorEntry,
                attempt.NewEntry,
                Localize);
            string feedback = string.IsNullOrWhiteSpace(attempt.TargetFeedback)
                ? changedFeedback
                : attempt.TargetFeedback;
            _context.SetBanner?.Invoke(feedback);
            return new PlacementResult(true, feedback, feedback);
        }

        public RunResult RunOrObserveDungeon(string selectedDebugPostureId)
        {
            MvpPlayerLoopSummary beforeRunSummary = _context.ResolveMvpPlayerLoopSummary?.Invoke();
            AdventurerRunIntentSummary intentSummary = beforeRunSummary?.AdventurerRunIntent;
            bool fallbackUsed = !IsResolvedAllowedIntent(intentSummary);
            string runPostureId = fallbackUsed ? selectedDebugPostureId : intentSummary.PostureId;
            bool didRun = _context.SimulateMvpActiveLoopOnce != null && _context.SimulateMvpActiveLoopOnce(runPostureId);
            MvpPlayerLoopSummary afterRunSummary = _context.ResolveMvpPlayerLoopSummary?.Invoke();
            string postureSource = AdventurerRunIntentPresenter.BuildRunPostureUsedLine(
                intentSummary,
                runPostureId,
                selectedDebugPostureId,
                fallbackUsed,
                Localize);
            string resultFeedback = MvpRunResultFeedbackPresenter.BuildFeedbackText(
                beforeRunSummary,
                afterRunSummary,
                didRun,
                Localize);
            string feedback = string.Concat(postureSource, " ", resultFeedback);
            string banner = Localize(didRun ? "ui.banner.run_simulated" : "ui.banner.run_sim_failed");
            _context.SetBanner?.Invoke(banner);
            return new RunResult(didRun, feedback, intentSummary, runPostureId, selectedDebugPostureId, fallbackUsed, banner);
        }

        public static bool IsResolvedAllowedIntent(AdventurerRunIntentSummary intentSummary)
        {
            return intentSummary != null &&
                   intentSummary.RuleResolved &&
                   IsAllowedMvpRunPosture(intentSummary.PostureId);
        }

        public static bool IsAllowedMvpRunPosture(string postureId)
        {
            return postureId == RunPostureResolver.CautiousId ||
                   postureId == RunPostureResolver.BalancedId ||
                   postureId == RunPostureResolver.GreedyId;
        }

        private static bool IsValidPlacementSelection(string categoryId, string optionId)
        {
            return MvpDungeonPlacementIds.IsAllowedCategory(categoryId) &&
                   MvpDungeonPlacementIds.IsAllowedOption(optionId) &&
                   MvpDungeonPlacementIds.TryGetCategoryForOption(optionId, out string optionCategoryId) &&
                   string.Equals(optionCategoryId, categoryId, StringComparison.Ordinal);
        }

        private string Localize(string key, string fallback)
        {
            return _context.Localize == null ? fallback : _context.Localize(key, fallback);
        }

        private string Localize(string key)
        {
            return Localize(key, key);
        }
    }
}
