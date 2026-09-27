#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

// Explicit test fixture injection, never a runtime fallback or a duplicated production tuning table.
public static class PhaseFiveBTestConfig
{
    public static PhaseFiveBConfig Create() => Production().PhaseFiveB;
    public static RunSimulationConfig Production() => JsonUtility.FromJson<RunSimulationConfig>(
        File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json"));
}

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseFiveBPartyHealthTests
    {
        private static RunSimulationConfig Config()
        {
            RunSimulationConfig c = PhaseFiveBTestConfig.Production();
            c.BaseSuccessChance = 1d; c.SuccessThreshold = 0d;
            c.HeatPenaltyPerPoint = 0d; c.ManaReserveBonusPerPoint = 0d;
            return c;
        }

        private static MvpOrderedRouteRoom Room(int index, params RunRoomAssignment[] assignments) =>
            new MvpOrderedRouteRoom { FloorIndex = 0, RoomIndex = index, HasActiveContent = assignments.Length > 0,
                Assignments = assignments, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId };

        private static RunRoomAssignment Assignment(string id, string option, int sequence = 0) =>
            new RunRoomAssignment { AssignmentId = id, OptionId = option, Sequence = sequence,
                CategoryId = option.Contains(".monster.") ? MvpDungeonPlacementIds.MonsterCategoryId :
                    option.Contains(".trap.") ? MvpDungeonPlacementIds.TrapCategoryId : MvpDungeonPlacementIds.LootNodeCategoryId };

        private static RunOutcomeRecord Run(RunSimulationConfig c, params MvpOrderedRouteRoom[] rooms) =>
            new RunSimulationService(c, JsonUtility.FromJson<LootConfig>(File.ReadAllText(
                "Assets/_Project/Data/Bootstrap/loot_config.json"))).SimulateRoute(new StructureRuntimeState(),
                    123L, 1, RunPostureResolver.BalancedId, rooms);

        [Test]
        public void ProductionConfigurationLoadsAndMissingConfigFailsClosed()
        {
            var c = PhaseFiveBTestConfig.Production();
            Assert.That(BootstrapConfigValidationService.ValidateRunSimulationConfig(c).IsValid, Is.True);
            c.PhaseFiveB = null;
            Assert.That(BootstrapConfigValidationService.ValidateRunSimulationConfig(c).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => Run(c, Room(0)));
        }

        [TestCase("version")][TestCase("size")][TestCase("health")][TestCase("class")]
        [TestCase("duplicate")][TestCase("behavior_id")][TestCase("dimension")][TestCase("weight")]
        [TestCase("zero_weights")][TestCase("intelligence")][TestCase("capability")]
        [TestCase("formation")][TestCase("damage")][TestCase("target")][TestCase("mapping")]
        [TestCase("coefficient")][TestCase("rule")][TestCase("nan")][TestCase("infinity")]
        public void InvalidConfigurationFailsClosed(string kind)
        {
            var c = Config(); var p = c.PhaseFiveB;
            switch (kind)
            {
                case "version": p.Version++; break;
                case "size": p.MinPartySize = 2; break;
                case "health": p.Classes[0].LevelOneMaxHealth = 0; break;
                case "class": p.Classes[0].ClassId = "unknown"; break;
                case "duplicate": p.Classes[0].ClassId = p.Classes[1].ClassId; break;
                case "behavior_id": p.Behaviors[0].Id = "unknown"; break;
                case "dimension": p.Behaviors[0].RiskTolerance = 2d; break;
                case "weight": p.Behaviors[0].Weight = -1d; break;
                case "zero_weights": foreach (var b in p.Behaviors) b.Weight = 0d; break;
                case "intelligence": foreach (var b in p.IntelligenceBands) b.Weight = 0d; break;
                case "capability": p.Classes[0].TrapExpertise = 2d; break;
                case "formation": p.Classes[0].FormationPriority = p.Classes[1].FormationPriority; break;
                case "damage": p.DamageProfiles[0].MinimumDamage = p.DamageProfiles[0].MaximumDamage + 1; break;
                case "target": p.DamageProfiles[0].TargetingPolicyId = "unknown"; break;
                case "mapping": p.DamageProfiles[0].OptionId = "unknown"; break;
                case "coefficient": p.TrapMitigationCoefficient = -1d; break;
                case "rule": p.BehaviorRuleSourceId = null; break;
                case "nan": p.Behaviors[0].RewardAppetite = double.NaN; break;
                case "infinity": p.IntelligenceBands[0].Weight = double.PositiveInfinity; break;
            }
            Assert.That(BootstrapConfigValidationService.ValidateRunSimulationConfig(c).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => RunPartyGenerator.Create(p, "test.run"));
        }

        [Test]
        public void PartyGenerationIsDeterministicCanonicalAndConfigOwned()
        {
            var c = Config().PhaseFiveB;
            for (int run = 0; run < 200; run++)
            {
                RunParty a = RunPartyGenerator.Create(c, "run-" + run);
                RunParty b = RunPartyGenerator.Create(c, "run-" + run);
                Assert.That(a.Members.Count, Is.InRange(3, 5));
                Assert.That(a.Members.Select(m => m.MemberOrdinal), Is.EqualTo(Enumerable.Range(0, a.Members.Count)));
                Assert.That(a.Members.Select(m => m.ClassId), Is.EqualTo(b.Members.Select(m => m.ClassId)));
                Assert.That(a.Members.Select(m => m.BehaviorProfileId), Is.EqualTo(b.Members.Select(m => m.BehaviorProfileId)));
                Assert.That(a.IntelligenceId, Is.EqualTo(b.IntelligenceId));
                Assert.That(a.Formation.Select(m => m.MemberOrdinal), Is.EqualTo(b.Formation.Select(m => m.MemberOrdinal)));
                foreach (var m in a.Members)
                {
                    var profile = c.Classes.Single(p => p.ClassId == m.ClassId);
                    Assert.That(m.Level, Is.EqualTo(1));
                    Assert.That(m.MaxHealth, Is.EqualTo(profile.LevelOneMaxHealth));
                    Assert.That(m.CurrentHealth, Is.EqualTo(m.MaxHealth));
                    Assert.That(m.TrapExpertise, Is.EqualTo(profile.TrapExpertise));
                }
            }
        }

        [Test]
        public void ConfigOrderAndWeightScalingDoNotChangePartyAndClassDoesNotChooseBehavior()
        {
            var c = Config().PhaseFiveB;
            RunParty a = RunPartyGenerator.Create(c, "run-test");
            Array.Reverse(c.Classes); Array.Reverse(c.Behaviors); Array.Reverse(c.IntelligenceBands);
            foreach (var b in c.Behaviors) b.Weight *= 10d;
            foreach (var b in c.IntelligenceBands) b.Weight *= 10d;
            RunParty bParty = RunPartyGenerator.Create(c, "run-test");
            Assert.That(bParty.Members.Select(m => m.ClassId), Is.EqualTo(a.Members.Select(m => m.ClassId)));
            Assert.That(bParty.Members.Select(m => m.BehaviorProfileId), Is.EqualTo(a.Members.Select(m => m.BehaviorProfileId)));
            Assert.That(bParty.IntelligenceId, Is.EqualTo(a.IntelligenceId));
            c.PartyRuleSourceId += ".different-class-selection";
            RunParty different = RunPartyGenerator.Create(c, "run-test");
            Assert.That(different.Members.Take(Math.Min(a.Members.Count, different.Members.Count)).Select(m => m.BehaviorProfileId),
                Is.EqualTo(a.Members.Take(Math.Min(a.Members.Count, different.Members.Count)).Select(m => m.BehaviorProfileId)));
            Assert.That(Enumerable.Range(0, 100).SelectMany(i => RunPartyGenerator.Create(c, "run-" + i).Members)
                .GroupBy(m => m.ClassId).All(g => g.Select(m => m.BehaviorProfileId).Distinct().Count() > 1), Is.True);
        }

        [TestCase(1, 2, 0d, 1)][TestCase(1, 2, 1d, 2)][TestCase(1, 2, .5d, 2)]
        [TestCase(1, 3, .25d, 2)][TestCase(2, 4, .25d, 3)][TestCase(0, 0, 1d, 0)]
        [TestCase(1, 3, -1d, 1)][TestCase(1, 3, 2d, 3)]
        public void DamageInterpolationHasExplicitBoundariesAndHalfAwayRounding(int min, int max, double severity, int expected)
            => Assert.That(RunEncounterResolver.Interpolate(min, max, severity), Is.EqualTo(expected));

        [Test]
        public void AllProfilesStayWithinConfiguredBounds()
        {
            foreach (var p in Config().PhaseFiveB.DamageProfiles)
                for (int i = 0; i <= 100; i++)
                    Assert.That(RunEncounterResolver.Interpolate(p.MinimumDamage, p.MaximumDamage, i / 100d),
                        Is.InRange(p.MinimumDamage, p.MaximumDamage));
        }

        [Test]
        public void FormationClosesImmediatelyAndPersonalityDoesNotChange()
        {
            var c = Config().PhaseFiveB;
            RunParty p = RunPartyGenerator.Create(c, "run-1");
            var before = p.Formation;
            Assert.That(before.Select(m => m.MemberOrdinal), Is.EqualTo(p.Members.OrderBy(m => m.FormationPriority)
                .ThenBy(m => m.MemberOrdinal).Select(m => m.MemberOrdinal)));
            double personality = p.RiskTolerance;
            var profile = c.DamageProfiles.Single(x => x.OptionId == MvpDungeonPlacementIds.GoblinOptionId);
            profile.MinimumDamage = profile.MaximumDamage = before[0].MaxHealth;
            var first = RunEncounterResolver.Resolve(p, c, Assignment("a", profile.OptionId), 0, 0, 0d);
            var second = RunEncounterResolver.Resolve(p, c, Assignment("b", profile.OptionId), 0, 0, 0d);
            Assert.That(first.MemberOrdinal, Is.EqualTo(before[0].MemberOrdinal));
            Assert.That(first.HealthAfter, Is.Zero);
            Assert.That(second.MemberOrdinal, Is.EqualTo(before[1].MemberOrdinal));
            Assert.That(p.Formation.All(m => m.IsActive), Is.True);
            Assert.That(p.RiskTolerance, Is.EqualTo(personality));
        }

        [Test]
        public void ActiveExpertiseChangesAfterExpertDeathAndOnlyMitigatesTraps()
        {
            var c = Config().PhaseFiveB;
            // Deliberate test-only formation makes the expert first, independently of generation.
            c.Classes.Single(x => x.ClassId.EndsWith("rogue")).FormationPriority = 1;
            c.Classes.Single(x => x.ClassId.EndsWith("warrior")).FormationPriority = 2;
            RunParty p = Enumerable.Range(0, 500).Select(i => RunPartyGenerator.Create(c, "expert-" + i))
                .First(x => x.Members.Count(m => m.ClassId.EndsWith("rogue")) == 1 && x.Members.Any(m => m.ClassId.EndsWith("ranger")));
            var spike = Assignment("trap", MvpDungeonPlacementIds.SpikeTrapOptionId);
            var trap = RunEncounterResolver.Resolve(p, c, spike, 0, 0, 1d);
            Assert.That(trap.Damage, Is.EqualTo(2)); Assert.That(trap.TrapExpertise, Is.EqualTo(1d));
            var goblin = c.DamageProfiles.Single(x => x.OptionId == MvpDungeonPlacementIds.GoblinOptionId);
            goblin.MinimumDamage = goblin.MaximumDamage = p.Formation[0].CurrentHealth;
            var monster = RunEncounterResolver.Resolve(p, c, Assignment("monster", goblin.OptionId), 0, 0, 1d);
            Assert.That(monster.Damage, Is.EqualTo(goblin.MaximumDamage));
            Assert.That(p.ActiveTrapExpertise, Is.EqualTo(.5d));
            Assert.That(RunEncounterResolver.Resolve(p, c, spike, 0, 0, 1d).Damage, Is.EqualTo(3));
        }

        [Test]
        public void SequentialCanonicalAssignmentsUseSharedSeverityAndReplayIdentically()
        {
            var c = Config();
            foreach (var profile in c.PhaseFiveB.Classes) profile.LevelOneMaxHealth = 1;
            var room = Room(0, Assignment("z", MvpDungeonPlacementIds.SpikeTrapOptionId),
                Assignment("b", MvpDungeonPlacementIds.GoblinOptionId, 1),
                Assignment("a", MvpDungeonPlacementIds.SkeletonOptionId, 1),
                Assignment("loot", MvpDungeonPlacementIds.BasicLootNodeOptionId));
            RunOutcomeRecord a = Run(c, room, Room(1));
            Array.Reverse(room.Assignments);
            RunOutcomeRecord b = Run(c, Room(1), room);
            Assert.That(a.EncounterEvents.Select(e => e.AssignmentId), Is.EqualTo(new[] { "a", "b", "z" }));
            Assert.That(a.EncounterEvents.Select(e => e.MemberOrdinal).Distinct().Count(), Is.EqualTo(3));
            Assert.That(a.EncounterEvents.Select(e => e.Severity).Distinct().Count(), Is.EqualTo(1));
            Assert.That(a.EncounterEvents.Select(e => (e.AssignmentId, e.MemberOrdinal, e.Damage, e.HealthBefore, e.HealthAfter)),
                Is.EqualTo(b.EncounterEvents.Select(e => (e.AssignmentId, e.MemberOrdinal, e.Damage, e.HealthBefore, e.HealthAfter))));
            Assert.That(JsonUtility.ToJson(a), Is.EqualTo(JsonUtility.ToJson(b)));
            Assert.That(a.SurvivalSummary.DeathCount, Is.EqualTo(a.Party.Members.Count(m => !m.IsActive)));
        }

        [Test]
        public void WipeStopsAssignmentsLaterLootAndRooms()
        {
            var c = Config(); c.PhaseFiveB.MinPartySize = c.PhaseFiveB.MaxPartySize = 3;
            foreach (var profile in c.PhaseFiveB.Classes) profile.LevelOneMaxHealth = 1;
            var room = Room(0, Assignment("a", MvpDungeonPlacementIds.GoblinOptionId, 0),
                Assignment("b", MvpDungeonPlacementIds.GoblinOptionId, 1),
                Assignment("c", MvpDungeonPlacementIds.GoblinOptionId, 2),
                Assignment("d", MvpDungeonPlacementIds.SpikeTrapOptionId),
                Assignment("e", MvpDungeonPlacementIds.BasicLootNodeOptionId));
            var result = Run(c, room, Room(1, Assignment("later", MvpDungeonPlacementIds.HiddenCacheOptionId)));
            Assert.That(result.EncounterEvents.Length, Is.EqualTo(3));
            Assert.That(result.ReachedRoomCount, Is.EqualTo(1));
            Assert.That(result.Party.IsWiped, Is.True);
            Assert.That(result.SurvivalSummary.SurvivorRatio, Is.Zero);
            Assert.That(result.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteWipedKey));
            Assert.That(result.LootSummary.GeneratedItemIds, Is.Empty);
            Assert.That(result.LootExtractionSummary.TotalExtractedWorldValue, Is.Zero);
            Assert.That(result.RunHeatDeltaSummary.DeathHeatDelta,
                Is.EqualTo(3 * (c.RunHeatNormalDeathDelta + c.CasualtyHeatDeltaPerCasualty)));
        }

        [Test]
        public void OneRoomAndCompatibilityIgnoreRetiredAggregateCasualtyAuthorities()
        {
            var c = Config();
            c.SuccessSurvivorRatio = c.FailureSurvivorRatio = 0d;
            c.MvpCompositionOutcomeTuning.SurvivorRatioPenaltyPerDanger = 100d;
            c.PartyWipeCasualtyPressureThreshold = 0d;
            c.CasualtyPressureMinimum = c.CasualtyPressureMaximum = 1d;
            var result = Run(c, Room(0, Assignment("sigil", MvpDungeonPlacementIds.ChillingSigilOptionId)));
            Assert.That(result.Party.Members.All(m => m.CurrentHealth == m.MaxHealth), Is.True);
            Assert.That(result.SurvivalSummary.DeathCount, Is.Zero);
            Assert.That(result.SurvivalSummary.CasualtyPressure, Is.EqualTo(1d));
            Assert.That(result.EncounterEvents.Single().Damage, Is.Zero);
            c.BaseSuccessChance = 0d; c.SuccessThreshold = 1d;
            var compatibility = new RunSimulationService(c).SimulateOnce(new StructureRuntimeState(), 1L, 2);
            Assert.That(compatibility.Success, Is.False);
            Assert.That(compatibility.SurvivalSummary.DeathCount, Is.Zero);
            Assert.That(compatibility.SurvivalSummary.SurvivorCount, Is.EqualTo(compatibility.Party.ActiveCount));
        }

        [Test]
        public void EmptyOneRoomCompatibilityPreservesSuccessThroughAuthoritativeRoster()
        {
            var c = Config();
            c.SuccessSurvivorRatio = c.FailureSurvivorRatio = 0d;
            RunOutcomeRecord result = Run(c, Room(0));
            Assert.That(result.Success, Is.True);
            Assert.That(result.FinalRouteOutcomeKey, Is.EqualTo(RunSimulationService.RouteClearedKey));
            Assert.That(result.Party, Is.Not.Null);
            Assert.That(result.SurvivalSummary.PartySize, Is.EqualTo(result.Party.Members.Count));
            Assert.That(result.SurvivalSummary.DeathCount, Is.Zero);
            Assert.That(result.Party.Members.All(member => member.CurrentHealth == member.MaxHealth), Is.True);
        }

        [Test]
        public void HpCarriesAcrossRoomsAndPresentationUsesActualRosterWithoutSerialization()
        {
            var c = Config();
            var result = Run(c, Room(0, Assignment("a", MvpDungeonPlacementIds.GoblinOptionId)),
                Room(1, Assignment("b", MvpDungeonPlacementIds.GoblinOptionId)));
            Assert.That(result.EncounterEvents[1].HealthBefore, Is.EqualTo(result.EncounterEvents[0].HealthAfter));
            Assert.That(result.SurvivalSummary.PartySize, Is.EqualTo(result.Party.Members.Count));
            var save = new SaveData(); save.runHistory.AppendOutcome(result, 10);
            var presentation = MvpPlayerLoopSummaryPresenter.Resolve(save, c);
            Assert.That(presentation.AdventurerPartyClassIds, Is.EqualTo(result.Party.Members.Select(m => m.ClassId)));
            Assert.That(presentation.LatestRunDeathCount, Is.EqualTo(result.SurvivalSummary.DeathCount));
            Assert.That(presentation.AdventurerArrivalPressure.RecentDeathCount, Is.EqualTo(result.SurvivalSummary.DeathCount));
            string json = JsonUtility.ToJson(result);
            Assert.That(json, Does.Not.Contain("CurrentHealth").And.Not.Contain("MemberOrdinal").And.Not.Contain("EncounterEvents"));
            var loaded = JsonUtility.FromJson<RunOutcomeRecord>(json);
            Assert.That(loaded.Party, Is.Null);
            Assert.That(loaded.SurvivalSummary.SurvivorCount, Is.EqualTo(result.SurvivalSummary.SurvivorCount));
            Assert.That(AdventurerPartyCompositionResolver.Resolve(loaded.Party).RuleResolved, Is.False);
        }

        [Test]
        public void EveryConfiguredContentProfileIsUsedAndChangingTuningChangesAbsoluteDamage()
        {
            var c = Config().PhaseFiveB;
            c.TrapMitigationCoefficient = 0d;
            foreach (var profile in c.DamageProfiles)
            {
                var p = RunPartyGenerator.Create(c, "damage-profile");
                var e = RunEncounterResolver.Resolve(p, c, Assignment("a", profile.OptionId), 0, 0, 1d);
                Assert.That(e.Damage, Is.EqualTo(profile.MaximumDamage));
                Assert.That(e.RuleSourceId, Is.EqualTo(c.EncounterRuleSourceId));
                profile.MinimumDamage = profile.MaximumDamage = 5; // Test-only content tuning, including sigil.
                p = RunPartyGenerator.Create(c, "damage-profile");
                Assert.That(RunEncounterResolver.Resolve(p, c, Assignment("a", profile.OptionId), 0, 0, 0d).Damage, Is.EqualTo(5));
            }
        }

        [Test]
        public void DeadMembersCannotBeRevivedByLegacyRatiosAndOriginalDimensionsRemainFixed()
        {
            var c = Config();
            foreach (var profile in c.PhaseFiveB.Classes) profile.LevelOneMaxHealth = 1;
            var room = Room(0, Assignment("a", MvpDungeonPlacementIds.GoblinOptionId));
            var first = Run(c, room);
            c.SuccessSurvivorRatio = c.FailureSurvivorRatio = 1d;
            c.MvpCompositionOutcomeTuning.SurvivorRatioBonusPerPathCapacity = 100d;
            c.MvpCompositionOutcomeTuning.SurvivorRatioPenaltyPerDanger = -100d;
            var second = Run(c, room);
            Assert.That(second.SurvivalSummary.DeathCount, Is.EqualTo(1));
            Assert.That(second.Party.Members.Select(m => m.CurrentHealth), Is.EqualTo(first.Party.Members.Select(m => m.CurrentHealth)));
            var original = RunPartyGenerator.Create(c.PhaseFiveB, first.RunId);
            Assert.That(new[] { second.Party.RewardAppetite, second.Party.RiskTolerance,
                second.Party.UncertaintyTolerance, second.Party.RequiredRouteCommitment },
                Is.EqualTo(new[] { original.RewardAppetite, original.RiskTolerance,
                    original.UncertaintyTolerance, original.RequiredRouteCommitment }));
        }

        [Test]
        public void EarlierLootIsLostOnLaterWipeAndUnreachedAssignmentsDoNotContributeEffects()
        {
            var c = Config(); c.PhaseFiveB.MinPartySize = c.PhaseFiveB.MaxPartySize = 3;
            foreach (var profile in c.PhaseFiveB.Classes) profile.LevelOneMaxHealth = 1;
            var first = Room(0, Assignment("early", MvpDungeonPlacementIds.BasicLootNodeOptionId));
            var second = Room(1, Assignment("a", MvpDungeonPlacementIds.GoblinOptionId, 0),
                Assignment("b", MvpDungeonPlacementIds.GoblinOptionId, 1),
                Assignment("c", MvpDungeonPlacementIds.GoblinOptionId, 2),
                Assignment("later", MvpDungeonPlacementIds.GlitteringHoardOptionId));
            var result = Run(c, first, second);
            Assert.That(result.LootSummary.GeneratedItemIds, Is.Not.Empty);
            Assert.That(result.LootExtractionSummary.ExtractedItemIds, Is.Empty);
            Assert.That(result.LootExtractionSummary.LostItemIds, Is.EqualTo(result.LootSummary.GeneratedItemIds));
            Assert.That(result.RoomResolutions[1].LocalPlacementEffects.ContributingOptionIds,
                Does.Not.Contain(MvpDungeonPlacementIds.GlitteringHoardOptionId));
            Assert.That(result.RoomResolutions[1].GeneratedLootValue, Is.Zero);
        }

        [Test]
        public void AssignmentSequenceIs64BitAndPrecedesOrdinalIdTies()
        {
            var c = Config();
            foreach (var profile in c.PhaseFiveB.Classes) profile.LevelOneMaxHealth = 100;
            var a = Assignment("a", MvpDungeonPlacementIds.GoblinOptionId);
            var z = Assignment("z", MvpDungeonPlacementIds.GoblinOptionId);
            var b = Assignment("b", MvpDungeonPlacementIds.GoblinOptionId);
            a.Sequence = b.Sequence = (long)int.MaxValue + 2L; z.Sequence = (long)int.MaxValue + 1L;
            Assert.That(Run(c, Room(0, b, a, z)).EncounterEvents.Select(e => e.AssignmentId), Is.EqualTo(new[] { "z", "a", "b" }));
        }

        [Test]
        public void CanonicalReadbackRetainsSameSessionRosterAndDiagnosticsAreLocalized()
        {
            var c = Config(); var result = Run(c, Room(0, Assignment("a", MvpDungeonPlacementIds.GoblinOptionId)));
            var save = new SaveData(); save.runHistory.AppendOutcome(result, 10);
            var published = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            var go = new GameObject("phase5b-readback");
            try
            {
                var root = go.AddComponent<GameRoot>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(GameRoot).GetField("<Save>k__BackingField", flags).SetValue(root, save);
                typeof(GameRoot).GetMethod("PublishCanonicalRuntime", flags).Invoke(root, new object[] { published });
                Assert.That(root.Save.runHistory.LatestOutcome.Party, Is.SameAs(result.Party));
                Assert.That(root.Save.runHistory.RecentOutcomes.Last().Party, Is.SameAs(result.Party));
                var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
                var strings = table.entries.ToDictionary(e => e.key, e => e.text);
                string diagnostics = RunPartyDiagnosticsPresenter.Build(result, (key, fallback) => strings.TryGetValue(key, out string value) ? value : fallback);
                Assert.That(diagnostics, Does.Contain("HP").And.Contain("damage").And.Not.Contain("ui.run.health."));
                Assert.That(diagnostics, Does.Not.Contain("adventurer.class."));
                Assert.That(JsonUtility.ToJson(root.Save), Does.Not.Contain("CurrentHealth"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
#endif
