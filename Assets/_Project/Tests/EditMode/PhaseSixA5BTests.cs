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
        public void ApplicableKnowledgeChangesProductionTransitionDecisionWithIdenticalIdentity()
        {
            var f = PhaseSixA4Tests.Eligible();
            var config = PhaseSixA4Tests.Config();
            var party = RunPartyGenerator.Create(config.PhaseFiveB, "run-10");
            var objective = TransientDepthObjective.Select(config.PhaseSix, party.RunId);
            double pull = objective.Pull(0, true, 1);
            var record = Record(f, 1);
            record.PerceivedRewardScore = config.PhaseSix.RewardReference;
            record.PerceivedDangerScore = 0d;
            var unknown = FloorTransitionPerception.Unknown(config.PhaseSix);
            var known = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, party.IntelligenceInterpretationFactor, config.PhaseSix);
            string currentFloor = f.State.Floors.Single(x => x.FloorIndex == 0).FloorInstanceId;
            string nextFloor = record.FloorInstanceId;
            double carriedLoot = config.PhaseSix.CarriedLootReference;
            var before = FloorTransitionDecision.Resolve(config.PhaseSix, party, currentFloor, nextFloor,
                carriedLoot, pull, unknown);
            var after = FloorTransitionDecision.Resolve(config.PhaseSix, party, currentFloor, nextFloor,
                carriedLoot, pull, known);
            Assert.That(objective.Mode, Is.EqualTo("target_depth"));
            Assert.That(pull, Is.EqualTo(.75d));
            Assert.That(before.Uncertainty, Is.EqualTo(1d));
            Assert.That(after.Uncertainty, Is.LessThan(before.Uncertainty));
            Assert.That(after.ExpectedSurvivability, Is.GreaterThan(before.ExpectedSurvivability));
            Assert.That(after.Appeal, Is.GreaterThan(before.Appeal));
            Assert.That(after.Reason, Is.Not.EqualTo(before.Reason));
            Assert.That(after.Descend, Is.True);
            Assert.That(before.Reason, Is.EqualTo("run.floor.exit_marginal"));
            Assert.That(before.Descend, Is.False);
            Assert.That(after.Reason, Is.EqualTo("run.floor.descend_appeal"));
            var repeated = FloorTransitionDecision.Resolve(config.PhaseSix, party, currentFloor, nextFloor,
                carriedLoot, pull, known);
            Assert.That(repeated.Reason, Is.EqualTo(after.Reason));
            Assert.That(repeated.Descend, Is.EqualTo(after.Descend));
            Assert.That(repeated.Appeal, Is.EqualTo(after.Appeal));
            Assert.That(repeated.Roll, Is.EqualTo(after.Roll));
        }

        [Test]
        public void DisabledUnavailableTargetDepthIsValidAndNeverSelected()
        {
            var config = PhaseSixA4Tests.Config().PhaseSix;
            var target = config.Objectives.Single(x => x.Mode == "target_depth");
            Assert.That(config.MaximumActiveFloors, Is.EqualTo(5));
            Assert.That(target.TargetFloorIndex, Is.EqualTo(1));
            Assert.That(target.Weight, Is.EqualTo(.5d));
            Assert.That(PhaseSixRunConfigValidation.IsValid(config), Is.True);
            config.MaximumActiveFloors = 2;
            Assert.That(PhaseSixRunConfigValidation.IsValid(config), Is.True);
            config.MaximumActiveFloors = 1;
            Assert.That(PhaseSixRunConfigValidation.IsValid(config), Is.False);
            target.Weight = 0d;
            Assert.That(PhaseSixRunConfigValidation.IsValid(config), Is.True);
            var first = TransientDepthObjective.Select(config, "run-1").Mode;
            Assert.That(first, Is.Not.EqualTo("target_depth"));
            bool selectedShallow = false, selectedDeepest = false;
            for (int index = 1; index <= 64; index++)
            {
                string runId = "run-" + index;
                string mode = TransientDepthObjective.Select(config, runId).Mode;
                Assert.That(mode, Is.Not.EqualTo("target_depth"));
                Assert.That(TransientDepthObjective.Select(config, runId).Mode, Is.EqualTo(mode));
                selectedShallow |= mode == "shallow";
                selectedDeepest |= mode == "deepest_reasonable";
            }
            Assert.That(selectedShallow && selectedDeepest, Is.True);
        }

        [Test]
        public void DurableKnowledgeRecordCountUsesFixedSupportedCeiling()
        {
            var f = PhaseSixA4Tests.Eligible();
            int ceiling = PhaseSixRunConfigValidation.MaximumSupportedActiveFloors;
            var template = Record(f, 0);
            var records = Enumerable.Range(0, ceiling + 1).Select(index => {
                var copy = PhaseSixFloorKnowledge.Copy(template);
                copy.FloorInstanceId = "floor.test." + index;
                return copy;
            }).ToArray();
            var spatial = new DetachedCanonicalSpatialSaveState { Floors = records.Select(record =>
                new SavedSpatialFloor { FloorInstanceId = record.FloorInstanceId }).ToArray() };
            Assert.That(PhaseSixFloorKnowledge.Validate(new SharedFloorKnowledgeAuthority {
                Records = records.Take(ceiling).ToArray() }, spatial, ceiling), Is.True);
            Assert.That(PhaseSixFloorKnowledge.Validate(new SharedFloorKnowledgeAuthority {
                Records = records }, spatial, ceiling + 1), Is.True);
            Assert.That(PhaseSixFloorKnowledge.Validate(new SharedFloorKnowledgeAuthority {
                Records = records }, spatial, ceiling), Is.False);
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
        public void UnknownComponentsUseConfiguredUncertaintyWhileKnownComponentsRemainConfidenceDerived()
        {
            var f = PhaseSixA4Tests.Eligible();
            var production = PhaseSixA4Tests.Config().PhaseSix;
            var record = Record(f, 1);
            double factor = f.Configuration.PhaseFiveB.IntelligenceBands[0].InterpretationFactor;
            double effective = Math.Min(1d, record.Confidence * factor);
            Assert.That(production.UnknownInformationUncertainty, Is.EqualTo(1d));
            Assert.That(FloorTransitionPerception.Unknown(production).Uncertainty, Is.EqualTo(1d));

            var alternate = PhaseSixA4Tests.Config().PhaseSix;
            alternate.UnknownInformationUncertainty = .4d;
            Assert.That(PhaseSixRunConfigValidation.IsValid(alternate), Is.True);
            Assert.That(FloorTransitionPerception.Unknown(alternate).Uncertainty, Is.EqualTo(.4d));

            record.DangerKnown = false; record.PerceivedDangerScore = 0d;
            var rewardOnly = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, alternate);
            Assert.That(rewardOnly.Uncertainty,
                Is.EqualTo((1d - effective + .4d) / 2d).Within(1e-12));

            record.RewardKnown = false; record.PerceivedRewardScore = 0d;
            record.DangerKnown = true; record.PerceivedDangerScore = 3d;
            var dangerOnly = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, alternate);
            Assert.That(dangerOnly.Uncertainty,
                Is.EqualTo((.4d + 1d - effective) / 2d).Within(1e-12));

            record.RewardKnown = true; record.PerceivedRewardScore = 2d;
            var bothAlternate = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, alternate);
            var bothProduction = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, production);
            Assert.That(bothAlternate.Uncertainty, Is.EqualTo(1d - effective).Within(1e-12));
            Assert.That(bothAlternate.Uncertainty, Is.EqualTo(bothProduction.Uncertainty));

            Assert.That(FloorTransitionPerceptionResolver.Resolve(record, "stale", factor,
                alternate).Uncertainty, Is.EqualTo(.4d));
            Assert.That(FloorTransitionPerceptionResolver.Resolve(null, record.ApplicabilityFingerprint,
                factor, alternate).Uncertainty, Is.EqualTo(.4d));
            var repeated = FloorTransitionPerceptionResolver.Resolve(record,
                record.ApplicabilityFingerprint, factor, alternate);
            Assert.That(repeated.Uncertainty, Is.EqualTo(bothAlternate.Uncertainty));
            Assert.That(repeated.RewardScore, Is.EqualTo(bothAlternate.RewardScore));
            Assert.That(repeated.DangerScore, Is.EqualTo(bothAlternate.DangerScore));
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
