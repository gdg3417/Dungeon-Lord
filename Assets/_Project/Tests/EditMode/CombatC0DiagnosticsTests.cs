#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class CombatC0DiagnosticsTests
    {
        private static string Localize(string key, string fallback)
        {
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            return table.entries.SingleOrDefault(e => e.key == key)?.text ?? fallback;
        }
        private static RunOutcomeRecord Outcome(params CombatC0Event[] events)
        {
            var c = CombatC0IntegrationTests.Config().PhaseFiveB;
            var party = new RunParty("test.c0", c.PartyRuleSourceId, 0,
                new[] { new RunPartyMember(0, c.Classes.Single(p => p.ClassId == "adventurer.class.warrior"), c.Behaviors[0], 1) },
                new[] { c.Behaviors[0] }, c.IntelligenceBands[0]);
            return new RunOutcomeRecord { Party = party, CombatExecutable = CombatC0Config.Executable, CombatEvents = events };
        }
        private static CombatC0Event Event(CombatC0EventKind kind = CombatC0EventKind.Attack,
            string target = "test.c0/monster/floor.b/same-suffix", TileCoordinate? source = null, TileCoordinate? destination = null,
            CombatC0Reason reason = CombatC0Reason.Legal) => new CombatC0Event("test.c0", "floor.b/room/encounter/0",
                7, 2, 1, "floor.b", "room", kind, reason, "test.c0/member/0", target,
                source: source, destination: destination, before: 3, after: 0, executable: CombatC0Config.Executable);
        private static string Text(RunOutcomeRecord outcome) => RunPartyDiagnosticsPresenter.Build(outcome, Localize);

        [Test] public void NumericNullableCoordinatesAndActionFormattingAreInvariant()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                var text = Text(Outcome(Event(source: new TileCoordinate(-2, 3))));
                Assert.That(text, Does.Contain("(-2,3) → —").And.Not.Contain("TileCoordinate"));
                Assert.That(text, Does.Contain("E1 R1 #7 A1 → M1"));
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }
        [Test] public void CompactAttackReportsActualHealthDamageAndDefeatWithoutMutatingEvidence()
        {
            var e = Event(source: new TileCoordinate(1, 2), destination: new TileCoordinate(2, 2));
            var outcome = Outcome(e); var text = Text(outcome);
            Assert.That(text, Does.Contain("E1 R1 #7 A1 → M1: Attack: HP 3 → 0, damage 3; defeated"));
            Assert.That(text, Does.Contain("(1,2) → (2,2)"));
            Assert.That(CombatC0Diagnostics.Events(outcome)[0], Is.SameAs(e));
            Assert.That(e.TargetId, Is.EqualTo("test.c0/monster/floor.b/same-suffix"));
            Assert.That(text, Does.Not.Contain("ui.run.").And.Not.Contain("combat.c0.reason."));
        }
        [TestCase(CombatC0EventKind.Eligibility, CombatC0Reason.LineOfSightBlocked)]
        [TestCase(CombatC0EventKind.Skipped, CombatC0Reason.DefeatedBeforeAction)]
        [TestCase(CombatC0EventKind.EncounterComplete, CombatC0Reason.DefendersDefeated)]
        [TestCase(CombatC0EventKind.Bypass, CombatC0Reason.OutOfRange)]
        public void LocalizedNonAttacksDoNotInventDamage(CombatC0EventKind kind, CombatC0Reason reason)
        {
            var text = Text(Outcome(Event(kind, reason: reason)));
            Assert.That(text, Does.Not.Contain("damage").And.Not.Contain("combat.c0.reason.").And.Not.Contain("ui.run.combat."));
            Assert.That(text, Does.Contain(kind == CombatC0EventKind.EncounterComplete ? "Complete" : kind.ToString()));
        }
        [Test] public void AliasesDisambiguateSharedSuffixesIndependentOfCollectionInsertion()
        {
            var a = Event(target: "test.c0/monster/floor.a/same-suffix"); var b = Event();
            var first = Text(Outcome(a, b)); var second = Text(Outcome(b, a));
            CollectionAssert.AreEqual(first.Split('\n').Where(l => l.Contains(" = ")), second.Split('\n').Where(l => l.Contains(" = ")));
            Assert.That(first, Does.Contain("M1 = test.c0/monster/floor.a/same-suffix").And.Contain("M2 = test.c0/monster/floor.b/same-suffix"));
            Assert.That(first.Split('\n').Where(l => l.StartsWith("E1 R")).All(l => l.Length < 90), Is.True);
            Assert.That(Text(Outcome(a, b)), Is.EqualTo(first));
        }
        [Test] public void HistoricalB1MemberTextRemainsExact()
        {
            var outcome = Outcome(); outcome.CombatExecutable = null;
            var member = outcome.Party.Members[0];
            Assert.That(Text(outcome), Is.EqualTo("Member 1: Warrior, HP " + member.CurrentHealth + "/" + member.MaxHealth));
        }
        [Test] public void BypassWithoutEncounterHasLocationAndDoesNotInventAnEncounterOrPosition()
        {
            var e = new CombatC0Event("test.c0", null, 0, -1, 0, "floor.b", "room", CombatC0EventKind.Bypass,
                CombatC0Reason.OutOfRange, "test.c0/monster/g");
            var text = Text(Outcome(e));
            Assert.That(text, Does.Contain("floor.b").And.Contain("room").And.Contain("Bypass"));
            Assert.That(text, Does.Not.Contain("E1 =").And.Not.Contain("damage").And.Not.Contain("TileCoordinate"));
        }
        [Test] public void ReplayRequiresTransientDetailsForBothC0AndHistoricalRuns()
        {
            var outcome = Outcome();
            Assert.That(DungeonRunReplay.TryCreate(outcome, null, null, null, null, null, out _, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(DungeonRunReplay.Missing));
            outcome.CombatExecutable = null;
            Assert.That(DungeonRunReplay.TryCreate(outcome, null, null, null, null, null, out _, out var historical), Is.False);
            Assert.That(historical, Is.EqualTo(DungeonRunReplay.Missing));
            Assert.That(Localize(reason, reason), Does.Contain("saved history"));
            Assert.That(Localize(historical, historical), Does.Contain("saved history").And.Not.Contain("new manual run"));
        }
    }
}
#endif
