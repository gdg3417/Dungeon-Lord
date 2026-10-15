#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class CombatC0KernelTests
    {
        // All numbers, positions and attack permissions below are test-only inputs.
        // In particular the permissions DO NOT qualify range, LOS or engagement geometry.
        private static readonly string[] Classes = { "warrior", "rogue", "ranger", "cleric", "mage" };
        private static string Class(string shortId) => "adventurer.class." + shortId;
        private static CombatC0QualificationConfig Config(int memberDamage = 2, int monsterDamage = 1,
            int monsterHealth = 5, int memberInitiative = 10, int monsterInitiative = 5,
            int rounds = 20, int participants = 64, int encounters = 30, int events = 32768, long work = 1000000,
            CombatC0AttackProfile[] classProfiles = null, CombatC0AttackProfile[] monsterProfiles = null) =>
            new CombatC0QualificationConfig(CombatC0QualificationConfig.Rules, CombatC0QualificationConfig.Targeting,
                CombatC0QualificationConfig.InitiativeOrder, rounds, participants, encounters, events, work,
                classProfiles ?? Classes.Select(c => new CombatC0AttackProfile(Class(c), "test.basic.member", memberDamage,
                    memberInitiative, 0, Array.Empty<string>())).ToArray(),
                monsterProfiles ?? new[] { MvpDungeonPlacementIds.GoblinOptionId, MvpDungeonPlacementIds.SkeletonOptionId }
                    .Select(c => new CombatC0AttackProfile(c, "test.basic.monster", monsterDamage,
                        monsterInitiative, monsterHealth, Classes.Select(Class).ToArray())).ToArray());
        private static RunParty Party(params string[] classes)
        {
            if (classes.Length == 0) classes = new[] { "warrior" };
            var config = PhaseFiveBTestConfig.Create();
            var members = classes.Select((c, i) => new RunPartyMember(i, config.Classes.Single(p => p.ClassId == Class(c)),
                config.Behaviors[0], 1)).ToArray();
            return new RunParty("test.c0.run", "test.party", 0, members,
                classes.Select(_ => config.Behaviors[0]).ToArray(), config.IntelligenceBands[0]);
        }
        private static CombatC0MonsterPlacement Placement(string id = "a", long sequence = 0, string floor = "f", string room = "r",
            string option = MvpDungeonPlacementIds.GoblinOptionId) => new CombatC0MonsterPlacement(id, option, floor, room, sequence);
        private static CombatC0AttackLink[] Links(RunParty party, string[] assignments,
            CombatC0Reason permission = CombatC0Reason.Legal)
        {
            var links = new List<CombatC0AttackLink>();
            foreach (var m in party.Members)
                for (int i = 0; i < assignments.Length; i++)
                {
                    var member = CombatC0Kernel.MemberId(party, m.MemberOrdinal);
                    var monster = CombatC0Kernel.MonsterId(party, assignments[i]);
                    links.Add(new CombatC0AttackLink(member, monster, new TileCoordinate(0, 0), new TileCoordinate(i + 1, 0), permission));
                    links.Add(new CombatC0AttackLink(monster, member, new TileCoordinate(i + 1, 0), new TileCoordinate(0, 0), permission));
                }
            return links.ToArray();
        }
        private static CombatC0Event[] Attacks(CombatC0EncounterResult result) => result.Events.Where(e => e.Kind == CombatC0EventKind.Attack).ToArray();
        private static string Evidence(CombatC0EncounterResult result) => string.Join("\n", result.Events.Select(e =>
            string.Join("|", e.RulesId, e.RunId, e.EncounterId, e.Ordinal, e.ActionOrdinal, e.Round, e.FloorId,
                e.RoomId, e.Kind, e.Reason, e.ActorId, e.TargetId, e.AttackId, e.TargetingId,
                e.SourcePosition?.X, e.SourcePosition?.Y, e.TargetPosition?.X, e.TargetPosition?.Y,
                e.PositionSpace, e.HealthBefore, e.HealthAfter, e.Damage, e.Defeated)));

        [Test]
        public void OneVersusOneIsReciprocalAndExactHpDrivesDefeat()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            Assert.That(result.Completion, Is.EqualTo(CombatC0Reason.DefendersDefeated));
            Assert.That(Attacks(result).Select(e => e.Damage), Is.EqualTo(new[] { 2, 1, 2, 1, 1 }));
            Assert.That(k.MonsterHealth("a"), Is.Zero);
            Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(8));
            Assert.That(Attacks(result).Last().Defeated, Is.True);
            Assert.That(result.Events.Last().Kind, Is.EqualTo(CombatC0EventKind.EncounterComplete));
            Assert.That(result.Events.Select(e => e.Ordinal), Is.EqualTo(Enumerable.Range(0, result.Events.Count)));
        }
        [Test]
        public void SeveralMonstersShareRoundsAndRetargetAfterDeath()
        {
            var p = Party("warrior", "rogue");
            var k = new CombatC0Kernel(p, Config(memberDamage: 3), new[] { Placement("a"), Placement("b", 1) });
            var result = k.Resolve("f", "r", new[] { "b", "a" }, Links(p, new[] { "a", "b" }));
            var first = Attacks(result).Where(e => e.Round == 1).ToArray();
            Assert.That(first.Select(e => e.ActorId), Is.EqualTo(new[] { CombatC0Kernel.MemberId(p, 0),
                CombatC0Kernel.MemberId(p, 1), CombatC0Kernel.MonsterId(p, "b") }));
            Assert.That(result.Events.Any(e => e.Kind == CombatC0EventKind.Skipped && e.ActorId == CombatC0Kernel.MonsterId(p, "a") &&
                e.Reason == CombatC0Reason.DefeatedBeforeAction), Is.True);
            Assert.That(Attacks(result).Any(e => e.TargetId == CombatC0Kernel.MonsterId(p, "b")), Is.True);
            Assert.That(result.Events.Select(e => e.EncounterId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(k.MonsterHealth("a") + k.MonsterHealth("b"), Is.Zero);
        }
        [Test]
        public void MonsterClassPriorityPrecedesMemberOrdinalAndRetargetsCasualties()
        {
            var p = Party("mage", "warrior", "rogue");
            var k = new CombatC0Kernel(p, Config(memberDamage: 0, monsterDamage: 10, monsterInitiative: 50), new[] { Placement() });
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            Assert.That(Attacks(result).Where(e => e.ActorId == CombatC0Kernel.MonsterId(p, "a")).Select(e => e.TargetId),
                Is.EqualTo(new[] { 1, 2, 0 }.Select(i => CombatC0Kernel.MemberId(p, i))));
            Assert.That(result.Completion, Is.EqualTo(CombatC0Reason.PartyWiped));
            Assert.That(p.DeriveSurvival(false).DeathCount, Is.EqualTo(3));
            Assert.That(p.IsWiped, Is.True);
        }
        [Test]
        public void OneActionPerLivingSlotAndNoAttackAfterDefeat()
        {
            var p = Party("warrior", "rogue");
            var k = new CombatC0Kernel(p, Config(memberDamage: 1, monsterHealth: 50, monsterDamage: 10,
                monsterInitiative: 30), new[] { Placement() });
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            Assert.That(result.Events.Any(e => e.Kind == CombatC0EventKind.Skipped && e.ActorId == CombatC0Kernel.MemberId(p, 0) &&
                e.Reason == CombatC0Reason.DefeatedBeforeAction), Is.True);
            Assert.That(Attacks(result).Any(e => e.ActorId == CombatC0Kernel.MemberId(p, 0)), Is.False);
            Assert.That(Attacks(result).GroupBy(e => (e.Round, e.ActorId)).All(g => g.Count() == 1), Is.True);
            Assert.That(result.Events.Where(e => e.ActionOrdinal >= 0).Select(e => e.ActionOrdinal),
                Is.EqualTo(Enumerable.Range(0, result.Events.Count(e => e.ActionOrdinal >= 0))));
        }
        [Test]
        public void EqualInitiativeUsesNumericOrdinalsSequenceAndStableId()
        {
            var p = Party("warrior", "rogue");
            var k = new CombatC0Kernel(p, Config(memberDamage: 0, monsterDamage: 0, memberInitiative: 5),
                new[] { Placement("z", 2), Placement("b", 1), Placement("a", 1) });
            var result = k.Resolve("f", "r", new[] { "z", "b", "a" }, Links(p, new[] { "a", "b", "z" }));
            Assert.That(Attacks(result).Select(e => e.ActorId), Is.EqualTo(new[] { CombatC0Kernel.MemberId(p, 0),
                CombatC0Kernel.MemberId(p, 1), CombatC0Kernel.MonsterId(p, "a"), CombatC0Kernel.MonsterId(p, "b"), CombatC0Kernel.MonsterId(p, "z") }));
        }
        [TestCase((int)CombatC0Reason.MeleePositionUnavailable)]
        [TestCase((int)CombatC0Reason.OutOfRange)]
        [TestCase((int)CombatC0Reason.LineOfSightBlocked)]
        [TestCase((int)CombatC0Reason.Unreachable)]
        public void InjectedIneligibilityBypassesMonstersWithoutDamageAndRetainsReason(int reason)
        {
            var permission = (CombatC0Reason)reason;
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }, permission));
            Assert.That(result.Completion, Is.EqualTo(CombatC0Reason.NoParticipants));
            Assert.That(Attacks(result), Is.Empty);
            Assert.That(result.Events.Count(e => e.Kind == CombatC0EventKind.Eligibility && e.Reason == permission), Is.EqualTo(2));
            Assert.That(k.MonsterHealth("a"), Is.EqualTo(5)); Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(10));
        }
        [Test]
        public void DirectionalLegalityAndNoLegalTargetRemainDistinct()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            var links = Links(p, new[] { "a" }); var l = links[1];
            links[1] = new CombatC0AttackLink(l.ActorId, l.TargetId, l.Source, l.Target, CombatC0Reason.LineOfSightBlocked);
            var result = k.Resolve("f", "r", new[] { "a" }, links);
            Assert.That(Attacks(result).All(e => e.ActorId == CombatC0Kernel.MemberId(p, 0)), Is.True);
            Assert.That(result.Events.Any(e => e.Kind == CombatC0EventKind.Skipped && e.Reason == CombatC0Reason.NoLivingLegalTarget), Is.True);
            Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(10));
        }
        [Test]
        public void EmptyRoomAndInitiallyWipedPartyTerminate()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), Array.Empty<CombatC0MonsterPlacement>());
            Assert.That(k.Resolve("f", "r", Array.Empty<string>(), Array.Empty<CombatC0AttackLink>()).Completion,
                Is.EqualTo(CombatC0Reason.NoParticipants));
            p.Members[0].ApplyDamage(10);
            Assert.That(k.Resolve("f", "r2", Array.Empty<string>(), Array.Empty<CombatC0AttackLink>()).Completion,
                Is.EqualTo(CombatC0Reason.PartyWiped));
        }
        [Test]
        public void ZeroDamageAndMaximumRoundsAreFiniteTerminalReasons()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(0, 0), new[] { Placement() });
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            Assert.That(result.Completion, Is.EqualTo(CombatC0Reason.ZeroDamageRound));
            Assert.That(Attacks(result).All(e => e.Round == 1 && e.Damage == 0), Is.True);
            var q = Party(); var limited = new CombatC0Kernel(q, Config(1, 0, 50, rounds: 2), new[] { Placement() });
            Assert.That(limited.Resolve("f", "r", new[] { "a" }, Links(q, new[] { "a" })).Completion,
                Is.EqualTo(CombatC0Reason.RoundLimit));
            Assert.That(limited.MonsterHealth("a"), Is.EqualTo(48));
        }
        [Test]
        public void RepeatedIntersectionIsIdempotentButContradictoryInputFails()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            var links = Links(p, new[] { "a" }); var result = k.Resolve("f", "r", new[] { "a" }, links);
            int events = k.EventCount, health = p.Members[0].CurrentHealth;
            Assert.That(k.Resolve("f", "r", new[] { "a" }, links.Reverse().ToArray()), Is.SameAs(result));
            Assert.That(k.EventCount, Is.EqualTo(events)); Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(health));
            Assert.Throws<ArgumentException>(() => k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }, CombatC0Reason.OutOfRange)));
        }
        [Test]
        public void ReorderingSourceCollectionsCannotChangeEvidence()
        {
            string baseline = null;
            for (int repeat = 0; repeat < 12; repeat++)
            {
                var p = Party("mage", "warrior", "rogue"); var c = Config();
                var monsters = new[] { Placement("a", 2), Placement("b", 10), Placement("c", 2) };
                var links = Links(p, new[] { "a", "b", "c" });
                if (repeat % 2 == 0) Array.Reverse(monsters);
                links = links.Skip(repeat).Concat(links.Take(repeat)).ToArray();
                var k = new CombatC0Kernel(p, Config(classProfiles: c.Classes.Reverse().ToArray(), monsterProfiles: c.Monsters.Reverse().ToArray()), monsters);
                string result = Evidence(k.Resolve("f", "r", monsters.Select(m => m.AssignmentId).ToArray(), links));
                if (baseline == null) baseline = result; else Assert.That(result, Is.EqualTo(baseline));
            }
        }
        [TestCase("events")][TestCase("work")][TestCase("encounters")]
        public void BudgetFailureIsAtomicAndLeavesNoPartialHpOrEvidence(string kind)
        {
            var p = Party();
            var k = new CombatC0Kernel(p, Config(events: kind == "events" ? 6 : 32768,
                work: kind == "work" ? 6 : 1000000, encounters: 1), new[] { Placement() });
            if (kind == "encounters") k.Resolve("f", "empty", Array.Empty<string>(), Array.Empty<CombatC0AttackLink>());
            int count = k.EventCount; long work = k.WorkCount;
            Assert.Throws<InvalidOperationException>(() => k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" })));
            Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(10)); Assert.That(k.MonsterHealth("a"), Is.EqualTo(5));
            Assert.That(k.EventCount, Is.EqualTo(count)); Assert.That(k.WorkCount, Is.EqualTo(work));
        }
        [Test]
        public void ExactEvidenceAndWorkCeilingsSucceed()
        {
            var p = Party(); var first = new CombatC0Kernel(p, Config(), new[] { Placement() });
            first.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            var q = Party(); var exact = new CombatC0Kernel(q, Config(events: first.EventCount, work: first.WorkCount), new[] { Placement() });
            Assert.That(exact.Resolve("f", "r", new[] { "a" }, Links(q, new[] { "a" })).Completion,
                Is.EqualTo(CombatC0Reason.DefendersDefeated));
        }
        [Test]
        public void InputsAreOwnedAndNestedPolicyMutationCannotChangeRun()
        {
            var priorities = Classes.Select(Class).ToArray();
            var monsters = new[] { MvpDungeonPlacementIds.GoblinOptionId, MvpDungeonPlacementIds.SkeletonOptionId }
                .Select(c => new CombatC0AttackProfile(c, "test.attack", 10, 50, 5, priorities)).ToArray();
            var classes = Config().Classes.ToArray(); var config = Config(classProfiles: classes, monsterProfiles: monsters);
            var p = Party("mage", "warrior"); var placements = new[] { Placement() }; var k = new CombatC0Kernel(p, config, placements);
            Array.Reverse(priorities); classes[0] = null; monsters[0] = null; placements[0] = null;
            var result = k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            Assert.That(Attacks(result).First().TargetId, Is.EqualTo(CombatC0Kernel.MemberId(p, 1)));
            Assert.That(config.Monsters[0].TargetClassOrder[0], Is.EqualTo(Class("warrior")));
        }
        [TestCase("null")][TestCase("profile")][TestCase("duplicate")][TestCase("missing")]
        [TestCase("health")][TestCase("damage")][TestCase("initiative")][TestCase("priority")]
        [TestCase("attack")][TestCase("limits")][TestCase("identity")]
        public void InvalidConfigurationIsRejectedBeforeAnyGameplay(string kind)
        {
            var c = Config(); var classes = c.Classes.ToArray(); var monsters = c.Monsters.ToArray();
            switch (kind)
            {
                case "null": classes = null; break;
                case "profile": classes[0] = null; break;
                case "duplicate": classes[0] = classes[1]; break;
                case "missing": monsters = monsters.Take(1).ToArray(); break;
                case "health": monsters[0] = new CombatC0AttackProfile(monsters[0].Id, "test", 1, 1, 0, Classes.Select(Class).ToArray()); break;
                case "damage": monsters[0] = new CombatC0AttackProfile(monsters[0].Id, "test", -1, 1, 1, Classes.Select(Class).ToArray()); break;
                case "initiative": monsters[0] = new CombatC0AttackProfile(monsters[0].Id, "test", 1, -1, 1, Classes.Select(Class).ToArray()); break;
                case "priority": monsters[0] = new CombatC0AttackProfile(monsters[0].Id, "test", 1, 1, 1, new[] { Class("warrior") }); break;
                case "attack": monsters[0] = new CombatC0AttackProfile(monsters[0].Id, null, 1, 1, 1, Classes.Select(Class).ToArray()); break;
            }
            Assert.Throws<ArgumentException>(() => new CombatC0QualificationConfig(kind == "identity" ? "run.executable.living-dungeon-b1.v1" : c.RulesId,
                CombatC0QualificationConfig.Targeting, CombatC0QualificationConfig.InitiativeOrder, kind == "limits" ? 0 : 20,
                64, 30, 1000, 10000, classes, monsters));
        }
        [TestCase("duplicate")][TestCase("unknown")][TestCase("floor")][TestCase("missing_link")]
        [TestCase("duplicate_link")][TestCase("position")][TestCase("same_side")][TestCase("reason")]
        public void MalformedEncounterCannotMutateAnything(string kind)
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            var links = Links(p, new[] { "a" }); var assignments = new[] { "a" }; string floor = "f";
            switch (kind)
            {
                case "duplicate": assignments = new[] { "a", "a" }; break;
                case "unknown": assignments[0] = "unknown"; break;
                case "floor": floor = "other"; break;
                case "missing_link": links = links.Take(1).ToArray(); break;
                case "duplicate_link": links[1] = links[0]; break;
                case "position": links[1] = new CombatC0AttackLink(links[1].ActorId, links[1].TargetId, new TileCoordinate(9, 9), links[1].Target, CombatC0Reason.Legal); break;
                case "same_side": links[0] = new CombatC0AttackLink(links[0].ActorId, links[0].ActorId, links[0].Source, links[0].Source, CombatC0Reason.Legal); break;
                case "reason": links[0] = new CombatC0AttackLink(links[0].ActorId, links[0].TargetId, links[0].Source, links[0].Target, (CombatC0Reason)999); break;
            }
            Assert.Throws<ArgumentException>(() => k.Resolve(floor, "r", assignments, links));
            Assert.That(k.EventCount, Is.Zero); Assert.That(k.MonsterHealth("a"), Is.EqualTo(5));
            Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(10));
        }
        [Test]
        public void DistinctCanonicalIdentitiesAndParticipantBoundsAreRequired()
        {
            Assert.Throws<ArgumentException>(() => new CombatC0Kernel(Party(), Config(), new[] { Placement(), Placement() }));
            Assert.Throws<ArgumentException>(() => new CombatC0Kernel(Party(), Config(participants: 1), new[] { Placement() }));
            Assert.Throws<ArgumentException>(() => new CombatC0Kernel(Party(), Config(), new[] { Placement(option: "trap") }));
        }
        [Test]
        public void SharedPartyHpCarriesAcrossInjectedFloorsAndExistingTrapAuthority()
        {
            var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement(), Placement("b", 1, "f2", "r2") });
            k.Resolve("f", "r", new[] { "a" }, Links(p, new[] { "a" }));
            var trap = new RunRoomAssignment { AssignmentId = "trap", CategoryId = MvpDungeonPlacementIds.TrapCategoryId,
                OptionId = MvpDungeonPlacementIds.SpikeTrapOptionId };
            var e = RunEncounterResolver.Resolve(p, PhaseFiveBTestConfig.Create(), trap, 0, 0, 0);
            Assert.That(e.HealthBefore, Is.EqualTo(8));
            k.Resolve("f2", "r2", new[] { "b" }, Links(p, new[] { "b" }));
            Assert.That(p.Members[0].CurrentHealth, Is.EqualTo(e.HealthAfter - 2));
            Assert.That(k.MonsterHealth("a"), Is.Zero); Assert.That(k.MonsterHealth("b"), Is.Zero);
            // This qualifies HP authority reuse, not real floor transfer or loot integration.
        }
        [Test]
        public void ProductionAndDurableB1InputsHaveNoC0StateAndCheckpointCannotBeMislabelled()
        {
            var c = PhaseFiveBTestConfig.Production(); var p = Party(); var k = new CombatC0Kernel(p, Config(), new[] { Placement() });
            Assert.That(k.RulesId, Is.Not.EqualTo(RunCheckpointCodec.Rules));
            Assert.Throws<InvalidOperationException>(() => k.ExportCheckpoint(new RunCheckpointLimits(100000, 1000)));
            var e = new RunSimulationService(c).StartRoute(new StructureRuntimeState(), 42, 1,
                RunPostureResolver.BalancedId, Array.Empty<MvpOrderedRouteRoom>());
            var checkpoint = e.ExportCheckpoint(new RunCheckpointLimits(100000, 1000));
            var restored = new RunSimulationService(c).RestoreCheckpoint(checkpoint.GetBytes(), checkpoint.Identity, e.StartIdentity, 0,
                new RunCheckpointLimits(100000, 1000));
            Assert.That(restored.IsValid, Is.True, restored.FailureReason);
            CollectionAssert.AreEqual(checkpoint.GetBytes(), restored.Execution.ExportCheckpoint(new RunCheckpointLimits(100000, 1000)).GetBytes());
            Assert.That(JsonUtility.ToJson(c), Does.Not.Contain("CombatC0"));
            Assert.That(File.ReadAllText("Assets/_Project/Scripts/Gameplay/RunSimulation/RunStartCapture.cs"), Does.Not.Contain("CombatC0"));
        }
        [Test]
        public void SyntheticMaximumConfiguredWorkloadHasCanonicalBoundedEvidence()
        {
            var p = Party("warrior", "rogue", "mage", "cleric", "ranger");
            var placements = Enumerable.Range(0, 59).Select(i => Placement("m" + i.ToString("D3"), i)).ToArray();
            var k = new CombatC0Kernel(p, Config(1, 0, 100000, rounds: 32), placements);
            var ids = placements.Select(m => m.AssignmentId).ToArray(); var clock = Stopwatch.StartNew();
            var result = k.Resolve("f", "r", ids, Links(p, ids)); clock.Stop();
            Assert.That(result.Completion, Is.EqualTo(CombatC0Reason.RoundLimit));
            Assert.That(Attacks(result).Length, Is.EqualTo(64 * 32));
            Assert.That(k.EventCount, Is.EqualTo(2 + 2 * 5 * 59 + 64 * 32));
            Assert.That(k.WorkCount, Is.LessThanOrEqualTo(1000000));
            TestContext.WriteLine("C0 injected envelope: participants=64; rounds=32; events=" + k.EventCount +
                "; charged work=" + k.WorkCount + "; elapsed_ms=" + clock.Elapsed.TotalMilliseconds);
        }
    }
}
#endif
