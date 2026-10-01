#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using UnityEngine;
using DungeonBuilder.M0;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseSixA5BTests
    {
        private static FloorKnowledgeRecord Record(Fixture f, int floorIndex)
        {
            string id = f.State.Floors.Single(x => x.FloorIndex == floorIndex).FloorInstanceId;
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State, f.Runtime.corridorContent,
                f.Profile.Canonical, id, out string fingerprint), Is.True);
            return new FloorKnowledgeRecord { FloorInstanceId = id, ApplicabilityFingerprint = fingerprint,
                RewardKnown = true, PerceivedRewardScore = 2d, DangerKnown = true,
                PerceivedDangerScore = 3d, ConfidenceKnown = true, Confidence = .75d,
                HasLastConfirmedRun = true, LastConfirmedRunId = "run-test-1" };
        }

        [Test]
        public void KnowledgeInterpretationChangesUncertaintyButNeverStoredScores()
        {
            var f = PhaseSixA4Tests.Eligible(); var record = Record(f, 1);
            double[] factors = f.Configuration.PhaseFiveB.IntelligenceBands
                .OrderBy(b => b.InterpretationFactor).Select(b => b.InterpretationFactor).ToArray();
            Assert.That(factors.Length, Is.GreaterThanOrEqualTo(3));
            double previous = double.PositiveInfinity;
            foreach (double factor in factors)
            {
                var perception = FloorTransitionPerceptionResolver.Resolve(record,
                    record.ApplicabilityFingerprint, factor, f.Configuration.PhaseSix);
                Assert.That(perception.RewardScore, Is.EqualTo(2d));
                Assert.That(perception.DangerScore, Is.EqualTo(3d));
                Assert.That(perception.Uncertainty, Is.EqualTo(1d - Math.Min(1d, .75d * factor)).Within(1e-12));
                Assert.That(perception.Uncertainty, Is.LessThan(previous));
                previous = perception.Uncertainty;
            }
            record.Confidence = f.Configuration.PhaseSix.ConfidenceClamp;
            Assert.That(FloorTransitionPerceptionResolver.Resolve(record, record.ApplicabilityFingerprint,
                factors[1], f.Configuration.PhaseSix).Uncertainty, Is.LessThan(1d - .75d * factors[1]));
        }

        [Test]
        public void PartialMissingAndStaleKnowledgeNeverInspectHiddenFloor()
        {
            var f = PhaseSixA4Tests.Eligible(); var record = Record(f, 1);
            double factor = f.Configuration.PhaseFiveB.IntelligenceBands[0].InterpretationFactor;
            var rewardOnly = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, f.Configuration.PhaseSix);
            record.DangerKnown = false; record.PerceivedDangerScore = 0d;
            rewardOnly = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, f.Configuration.PhaseSix);
            Assert.That(rewardOnly.RewardKnown && !rewardOnly.DangerKnown, Is.True);
            Assert.That(rewardOnly.Uncertainty, Is.EqualTo((2d - Math.Min(1d, .75d * factor)) / 2d));
            record.RewardKnown = false; record.PerceivedRewardScore = 0d;
            record.DangerKnown = true; record.PerceivedDangerScore = 3d;
            var dangerOnly = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, f.Configuration.PhaseSix);
            Assert.That(!dangerOnly.RewardKnown && dangerOnly.DangerKnown, Is.True);
            Assert.That(dangerOnly.Uncertainty, Is.EqualTo(rewardOnly.Uncertainty));
            Assert.That(FloorTransitionPerceptionResolver.Resolve(record, "stale", factor,
                f.Configuration.PhaseSix).Uncertainty, Is.EqualTo(1d));
            Assert.That(FloorTransitionPerceptionResolver.Resolve(null, record.ApplicabilityFingerprint,
                factor, f.Configuration.PhaseSix).Uncertainty, Is.EqualTo(1d));
        }

        [Test]
        public void DurableKnowledgeSurvivesOneActiveFloorLimitAndReopen()
        {
            var f = PhaseSixA4Tests.Eligible();
            var records = new[] { Record(f, 0), Record(f, 1) };
            f.Configuration.PhaseSix.MaximumActiveFloors = 1;
            f.Configuration.PhaseSix.Objectives.Single(x => x.Mode == "target_depth").Weight = 0;
            Assert.That(PhaseSixRunConfigValidation.IsValid(f.Configuration.PhaseSix), Is.True);
            Assert.That(f.State.Floors.Single(x => x.FloorIndex == 1).ActivationState,
                Is.Not.EqualTo(FloorActivationState.Active));
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            var recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var update = f.Session.PrepareLiveReplacement(recognized, f.State, owned.Investment,
                owned.CorridorContent, owned.BranchKnowledge,
                new SharedFloorKnowledgeAuthority { Records = records });
            Assert.That(update.IsSuccess, Is.True, update.Reason);
            byte[] bytes = update.Update.GetBytes();
            var oneActiveContext = new DetachedCurrentTargetValidationContext(f.Compatibility,
                f.Production, LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration),
                f.Profile.Canonical);
            var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, oneActiveContext);
            Assert.That(validated.CurrentTargetValidated, Is.True);
            Assert.That(validated.FloorKnowledge.Records.Length, Is.EqualTo(2));
            Assert.That(DetachedCanonicalSaveSession.Open(bytes, oneActiveContext, f.Profile).IsSuccess, Is.True);
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain(records[1].FloorInstanceId));
            Assert.That(PhaseSixFloorKnowledge.Validate(validated.FloorKnowledge, f.State,
                PhaseSixRunConfigValidation.MaximumSupportedActiveFloors), Is.True);
            var snapshot = PhaseSixA4Tests.Snapshot(f);
            Assert.That(snapshot.Floors.Count, Is.EqualTo(1));
            Assert.That(new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot())
                .SimulateSnapshot(1, snapshot).RoomResolutions.All(r => r.FloorIndex == 0), Is.True);
            Assert.That(PhaseSixFloorKnowledge.Validate(new SharedFloorKnowledgeAuthority {
                Records = Enumerable.Repeat(records[0],
                    PhaseSixRunConfigValidation.MaximumSupportedActiveFloors + 1).ToArray() },
                f.State, PhaseSixRunConfigValidation.MaximumSupportedActiveFloors), Is.False);
        }

        [Test]
        public void LaterRunUsesApplicablePreRunKnowledgeAndCurrentRunCannotLearnRetroactively()
        {
            var f = PhaseSixA4Tests.Eligible();
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId,
                MvpDungeonPlacementIds.HiddenCacheOptionId);
            PhaseSixA4Tests.ForceDescent(f.Configuration, true);
            f.Accept(PhaseSixA4Tests.Writer(f)
                .CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            ActiveFloorRunSnapshot unknown = PhaseSixA4Tests.Snapshot(f);
            var simulation = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot());
            var before = simulation.SimulateSnapshot(1, unknown).FloorTransitions[0];
            Assert.That(before.Uncertainty, Is.EqualTo(1d));
            Assert.That(before.RewardKnown || before.DangerKnown, Is.False);

            FloorKnowledgeRecord record = Record(f, 1);
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            var replacement = f.Session.PrepareLiveReplacement(
                DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State,
                owned.Investment, owned.CorridorContent, owned.BranchKnowledge,
                new SharedFloorKnowledgeAuthority { Records = new[] { record } });
            Assert.That(replacement.IsSuccess, Is.True, replacement.Reason);
            var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                replacement.Update.GetBytes(), f.Context);
            var known = ActiveFloorRunSnapshot.Create(validated, f.Production, f.Configuration,
                f.Profile.Canonical, PhaseSixA4Tests.Loot(), executionInputs: f.Runtime);
            var after = simulation.SimulateSnapshot(1, known).FloorTransitions[0];
            Assert.That(after.RewardKnown && after.DangerKnown, Is.True);
            Assert.That(after.Uncertainty, Is.LessThan(before.Uncertainty));
            Assert.That(after.RunId, Is.EqualTo(before.RunId));
            Assert.That(unknown.FloorKnowledge.Records, Is.Empty);
            record.Confidence = 1d;
            var repeated = simulation.SimulateSnapshot(1, known).FloorTransitions[0];
            Assert.That(repeated.Uncertainty, Is.EqualTo(after.Uncertainty));
            Assert.That(repeated.Reason, Is.EqualTo(after.Reason));
            Assert.That(repeated.Roll, Is.EqualTo(after.Roll));
        }

        [Test]
        public void LocalizedResultExplainsTransitionAndReopenKeepsOnlyCoarseReach()
        {
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/string_table_en.json"));
            var strings = table.entries.ToDictionary(item => item.key, item => item.text,
                StringComparer.Ordinal);
            Func<string, string, string> localize = (key, fallback) =>
                strings.TryGetValue(key, out string value) ? value : fallback;
            var summary = new MvpPlayerLoopSummary {
                ConfiguredRoomCount = 2, HighestRoomReached = 0,
                FinalRouteOutcomeKey = "run.route.floor_exit",
                RoomResolutions = new[] { new RunRoomResolutionSummary { FloorIndex = 0, Reached = true } },
                FloorTransitions = new[] { new FloorTransitionEvidence {
                    Reason = "run.floor.exit_survivability", NextFloorInstanceId = "floor-2", RewardKnown = true,
                    DangerKnown = false, Uncertainty = .7d } }
            };
            string current = MvpRouteResultPresenter.BuildCompactText(summary, localize);
            Assert.That(current, Does.Contain("Reached Floor 1"));
            Assert.That(current, Does.Contain("survival"));
            Assert.That(current, Does.Contain("danger unknown"));
            Assert.That(current, Does.Not.Contain("run.floor"));
            Assert.That(current, Does.Not.Contain("ui.mvp_loop"));
            summary.FloorTransitions = null;
            string historical = MvpRouteResultPresenter.BuildCompactText(summary, localize);
            Assert.That(historical, Does.Contain("Reached Floor 1"));
            Assert.That(historical, Does.Not.Contain("survival"));
            summary.FloorTransitions = new[] { new FloorTransitionEvidence {
                Reason = "run.floor.exit_final", NextFloorInstanceId = null } };
            string terminal = MvpRouteResultPresenter.BuildCompactText(summary, localize);
            Assert.That(terminal, Does.Contain("Completed the final Active floor"));
            Assert.That(terminal, Does.Not.Contain("Next-floor information"));
        }
    }
}
#endif
