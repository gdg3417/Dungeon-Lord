#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseFiveBBranchDecisionTests
    {
        private static BranchDecisionInput Input(PhaseFiveBConfig c, bool known = true) => new BranchDecisionInput {
            Party = RunPartyGenerator.Create(c, "run-1"), FloorInstanceId = "floor-1", OptionalBranchId = "branch-1",
            Applicable = known, Knowledge = new BranchKnowledgeRecord { IncentiveKnown = known, DangerKnown = known,
                ConfidenceKnown = known, Confidence = known ? 1d : 0d, PerceivedIncentive = known ? 1d : 0d } };

        [TestCase("run-1", "floor-1", "branch-1", "2f14e61aaba388a5c2355af4ab258bb29c8b494aad3d5c0af7f0da91018a2a9e", 789898778L)]
        [TestCase("run-2", "compat.floor.00", "compat.floor.00.edge.native.00000000.branch", "3454cfedea2e165d2ecccc3aeb0c1034c49cf5992619509870cf5fc3f7a6be2a", 877973485L)]
        public void DecisionV2KnownVectors(string run, string floor, string branch, string expectedDigest, long expectedWord)
        {
            const string rule = "run.branch_decision.rule.phase5b.v2";
            byte[] digest = BranchDecisionResolver.IdentityDigest(rule, run, floor, branch);
            Assert.That(BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant(), Is.EqualTo(expectedDigest));
            Assert.That(BranchDecisionResolver.IdentityWord(rule, run, floor, branch), Is.EqualTo((uint)expectedWord));
            Assert.That(BranchDecisionResolver.Roll(rule, run, floor, branch),
                Is.EqualTo(expectedWord / 4294967296d));
        }

        [Test]
        public void OrderedIdentityIsIndependentOfGlobalRandomCallOrderAndRuntimeHashes()
        {
            const string rule = "run.branch_decision.rule.phase5b.v2";
            double first = BranchDecisionResolver.Roll(rule, "run-1", "floor-1", "branch-1");
            var previous = UnityEngine.Random.state;
            try { UnityEngine.Random.InitState(873); Assert.That(BranchDecisionResolver.Roll(rule, "run-1", "floor-1", "branch-1"), Is.EqualTo(first)); }
            finally { UnityEngine.Random.state = previous; }
            Assert.That(first, Is.EqualTo(789898778d / 4294967296d));
            double unrelated = BranchDecisionResolver.Roll(rule, "run-unrelated", "floor-unrelated", "branch-unrelated");
            Assert.That(BranchDecisionResolver.Roll(rule, "run-1", "floor-1", "branch-1"), Is.EqualTo(first));
            Assert.That(unrelated, Is.Not.EqualTo(first));
            Assert.That(BranchDecisionResolver.Roll(rule, "floor-1", "run-1", "branch-1"), Is.Not.EqualTo(first));
            foreach (var changed in new[] {
                new[] { rule + ".changed", "run-1", "floor-1", "branch-1" },
                new[] { rule, "run-1.changed", "floor-1", "branch-1" },
                new[] { rule, "run-1", "floor-1.changed", "branch-1" },
                new[] { rule, "run-1", "floor-1", "branch-1.changed" } })
                Assert.That(BranchDecisionResolver.Roll(changed[0], changed[1], changed[2], changed[3]), Is.Not.EqualTo(first));
            Assert.That(BranchDecisionResolver.Roll(new string(rule.ToCharArray()), new string("run-1".ToCharArray()),
                new string("floor-1".ToCharArray()), new string("branch-1".ToCharArray())), Is.EqualTo(first));
        }

        [Test]
        public void ProductionValidationAcceptsOnlyDecisionV2Rule()
        {
            var c = PhaseFiveBTestConfig.Create();
            Assert.That(c.Version, Is.EqualTo(1));
            Assert.That(c.BranchDecision.RuleSourceId, Is.EqualTo("run.branch_decision.rule.phase5b.v2"));
            Assert.That(PhaseFiveBConfigValidation.ValidDecision(c.BranchDecision), Is.True);
            c.BranchDecision.RuleSourceId = "run.branch_decision.rule.phase5b.v1";
            Assert.That(PhaseFiveBConfigValidation.ValidDecision(c.BranchDecision), Is.False);
        }

        [Test]
        public void SequentialProductionIdentitiesSpanLowAndHighRolls()
        {
            const string rule = "run.branch_decision.rule.phase5b.v2";
            double[] rolls = Enumerable.Range(1, 16).Select(n => BranchDecisionResolver.Roll(rule, "run-" + n,
                "compat.floor.00", "compat.floor.00.edge.native.00000000.branch")).ToArray();
            Assert.That(rolls.All(r => r >= 0d && r < 1d), Is.True);
            Assert.That(rolls.Any(r => r < .25d), Is.True);
            Assert.That(rolls.Any(r => r > .75d), Is.True);
        }

        [Test]
        public void ProductionLikeUnknownBranchAllowsEarlyRunTwoEntry()
        {
            var c = PhaseFiveBTestConfig.Create();
            var result = BranchDecisionResolver.Resolve(c.BranchDecision, new BranchDecisionInput {
                Party = RunPartyGenerator.Create(c, "run-2"), FloorInstanceId = "compat.floor.00",
                OptionalBranchId = "compat.floor.00.edge.native.00000000.branch", Applicable = true,
                Knowledge = new BranchKnowledgeRecord { TopologyKnown = true }, RemainingRequiredDanger = 0d });
            Assert.That(result.U, Is.EqualTo(1d));
            Assert.That(result.IncentiveKnown || result.DangerKnown, Is.False);
            Assert.That(result.DecisionRoll, Is.EqualTo(877973485d / 4294967296d));
            Assert.That(result.Reason, Is.EqualTo("branch.decision.marginal_enter"));
            Assert.That(result.Enter, Is.True);
        }

        [TestCase("missing")][TestCase("rule")][TestCase("nan")][TestCase("infinity")]
        [TestCase("negative")][TestCase("zero")][TestCase("lower")][TestCase("upper")][TestCase("equal")]
        [TestCase("reverse")][TestCase("profile")][TestCase("duplicate")][TestCase("minimum")]
        [TestCase("reference")][TestCase("floor")][TestCase("run")][TestCase("assignments")][TestCase("knowledge")]
        [TestCase("inclination")][TestCase("condition")][TestCase("threat")]
        public void InvalidDecisionConfigurationFailsClosed(string kind)
        {
            var c = PhaseFiveBTestConfig.Create(); var d = c.BranchDecision;
            switch (kind)
            {
                case "missing": c.BranchDecision = null; break;
                case "rule": d.RuleSourceId = "wrong"; break;
                case "nan": d.TrapInterpretationConfidenceBonus = double.NaN; break;
                case "infinity": d.RewardWeight = double.PositiveInfinity; break;
                case "negative": d.DangerWeight = -1d; break;
                case "zero": d.RewardWeight = d.DangerWeight = d.UncertaintyWeight = d.ReserveWeight = d.IntentWeight = 0d; break;
                case "lower": d.SkipThreshold = -2d; break;
                case "upper": d.EnterThreshold = 2d; break;
                case "equal": d.SkipThreshold = d.EnterThreshold; break;
                case "reverse": d.SkipThreshold = 1d; d.EnterThreshold = -1d; break;
                case "profile": d.ProfileMinimums = d.ProfileMinimums.Take(4).ToArray(); break;
                case "duplicate": d.ProfileMinimums[0].ProfileId = d.ProfileMinimums[1].ProfileId; break;
                case "minimum": d.ProfileMinimums[0].MinimumSurvivability = -1d; break;
                case "reference": d.DangerReferenceValue = 0d; break;
                case "floor": d.MaximumDecisionsPerFloor = 0; break;
                case "run": d.MaximumDecisionsPerRun = 0; break;
                case "assignments": d.MaximumAssignmentsPerBranch = 0; break;
                case "knowledge": d.MaximumKnowledgeUpdatesPerRun = 0; break;
                case "inclination": d.InitialInclination = 1d; break;
                case "condition": d.ConditionHealthWeight = 2d; break;
                case "threat": d.ThreatUncertaintyWeight = -1d; break;
            }
            Assert.That(PhaseFiveBConfigValidation.IsValid(c), Is.False);
            Assert.That(BranchDecisionResolver.Resolve(c.BranchDecision, null).Reason, Is.EqualTo(BranchDecisionResolver.InvalidConfiguration));
        }

        [Test]
        public void EveryRequiredNumericFieldRejectsMissingJson()
        {
            string json = JsonUtility.ToJson(PhaseFiveBTestConfig.Create().BranchDecision);
            foreach (var field in typeof(BranchDecisionConfig).GetFields().Where(f => f.FieldType == typeof(double)))
            {
                string missing = System.Text.RegularExpressions.Regex.Replace(json,
                    "\\\"" + field.Name + "\\\":[^,}]+,?", "").Replace(",}", "}");
                Assert.That(PhaseFiveBConfigValidation.ValidDecision(JsonUtility.FromJson<BranchDecisionConfig>(missing)), Is.False, field.Name);
            }
        }

        [Test]
        public void ExactThresholdsAndOnlyStrictMarginalBandUseRoll()
        {
            var c = PhaseFiveBTestConfig.Create(); var i = Input(c); var d = c.BranchDecision;
            double appeal = BranchDecisionResolver.Resolve(d, i).BranchAppeal;
            d.SkipThreshold = appeal; d.EnterThreshold = 1d;
            var skip = BranchDecisionResolver.Resolve(d, i);
            Assert.That(skip.Reason, Is.EqualTo("branch.decision.appeal_skip")); Assert.That(skip.DecisionRoll, Is.Null);
            d.SkipThreshold = -1d; d.EnterThreshold = appeal;
            var enter = BranchDecisionResolver.Resolve(d, i);
            Assert.That(enter.Reason, Is.EqualTo("branch.decision.appeal_enter")); Assert.That(enter.DecisionRoll, Is.Null);
            d.EnterThreshold = 1d;
            var marginal = BranchDecisionResolver.Resolve(d, i);
            Assert.That(marginal.DecisionRoll.HasValue, Is.True);
            double oldLikelihood = marginal.EntryLikelihood.Value;
            i.Knowledge.PerceivedIncentive = .5d;
            Assert.That(BranchDecisionResolver.Resolve(d, i).EntryLikelihood, Is.LessThan(oldLikelihood));
            Assert.That(BranchDecisionResolver.MarginalEnter(.5d, .5d), Is.False);
        }

        [Test]
        public void SurvivabilityStrictGateAndHealthUseOriginalBehavior()
        {
            var c = PhaseFiveBTestConfig.Create(); var i = Input(c); var d = c.BranchDecision;
            double health = BranchDecisionResolver.Resolve(d, i).ExpectedSurvivability;
            foreach (var profile in d.ProfileMinimums) profile.MinimumSurvivability = health;
            Assert.That(BranchDecisionResolver.Resolve(d, i).Reason, Is.Not.EqualTo("branch.decision.survivability_refusal"));
            double reward = i.Party.RewardAppetite;
            i.Party.Formation[0].ApplyDamage(1);
            var injured = BranchDecisionResolver.Resolve(d, i);
            Assert.That(injured.AveragePartyHealth, Is.LessThan(1d)); Assert.That(injured.ActiveMemberFraction, Is.EqualTo(1d));
            Assert.That(injured.Reason, Is.EqualTo("branch.decision.survivability_refusal"));
            i.Party.Formation[0].ApplyDamage(int.MaxValue);
            Assert.That(i.Party.RewardAppetite, Is.EqualTo(reward));
            Assert.That(BranchDecisionResolver.Resolve(d, i).PartyMinimumSurvivability, Is.EqualTo(health).Within(1e-14));
        }

        [Test]
        public void UnknownInapplicableAndIndependentKnowledgeConfidence()
        {
            var c = PhaseFiveBTestConfig.Create(); var i = Input(c, false); var d = c.BranchDecision;
            var unknown = BranchDecisionResolver.Resolve(d, i);
            Assert.That(unknown.U, Is.EqualTo(1d)); Assert.That(unknown.I, Is.Zero); Assert.That(unknown.DangerKnown, Is.False);
            i.Applicable = true; i.Knowledge.IncentiveKnown = true; i.Knowledge.ConfidenceKnown = true; i.Knowledge.Confidence = .75d;
            var incentive = BranchDecisionResolver.Resolve(d, i);
            Assert.That(incentive.EffectiveIncentiveConfidence, Is.EqualTo(.75d * i.Party.IntelligenceInterpretationFactor));
            Assert.That(incentive.EffectiveDangerConfidence, Is.Zero);
            i.Knowledge.DangerKnown = true;
            var danger = BranchDecisionResolver.Resolve(d, i);
            Assert.That(danger.EffectiveDangerConfidence, Is.EqualTo(Math.Min(1d, .75d * i.Party.IntelligenceInterpretationFactor + .15d * i.Party.ActiveTrapExpertise)));
            i.Applicable = false;
            Assert.That(BranchDecisionResolver.Resolve(d, i).U, Is.EqualTo(1d));
            Assert.That(danger.J, Is.Zero);
        }

        [TestCase("floor")][TestCase("run")][TestCase("assignments")][TestCase("knowledge")]
        public void WorkloadExactBoundPassesAndOneOverFails(string kind)
        {
            var c = PhaseFiveBTestConfig.Create().BranchDecision; var w = new BranchRunWorkload(c);
            int bound = kind == "floor" ? c.MaximumDecisionsPerFloor : kind == "run" ? c.MaximumDecisionsPerRun :
                kind == "assignments" ? c.MaximumAssignmentsPerBranch : c.MaximumKnowledgeUpdatesPerRun;
            Action<int> consume = n => { if (kind == "floor") w.Decision("floor"); else if (kind == "run") w.Decision("floor-" + n);
                else if (kind == "assignments") w.Assignment(); else w.Knowledge(); };
            for (int n = 0; n < bound; n++) consume(n);
            Assert.That(Assert.Throws<InvalidOperationException>(() => consume(bound)).Message, Is.EqualTo(BranchRunWorkload.WorkloadExceeded));
        }
    }
}
#endif
