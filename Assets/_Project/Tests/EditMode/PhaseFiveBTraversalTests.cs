#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseFiveBTraversalTests
    {
        private static RunSimulationConfig Config()
        {
            var c = PhaseFiveBTestConfig.Production(); c.BaseSuccessChance = 1d; c.SuccessThreshold = 0d;
            c.HeatPenaltyPerPoint = c.ManaReserveBonusPerPoint = 0d;
            c.PhaseFiveB.MinPartySize = c.PhaseFiveB.MaxPartySize = 3;
            c.PhaseFiveB.BranchDecision.SkipThreshold = -1d; c.PhaseFiveB.BranchDecision.EnterThreshold = -.9d;
            foreach (var p in c.PhaseFiveB.BranchDecision.ProfileMinimums) p.MinimumSurvivability = 0d;
            return c;
        }
        private static RunRoomAssignment Assignment(string id, string option) => new RunRoomAssignment {
            AssignmentId = id, OptionId = option, CategoryId = option.Contains(".monster.") ? MvpDungeonPlacementIds.MonsterCategoryId :
                option.Contains(".trap.") ? MvpDungeonPlacementIds.TrapCategoryId : MvpDungeonPlacementIds.LootNodeCategoryId };
        private static MvpOrderedRouteRoom Room(int index, params RunRoomAssignment[] assignments) => new MvpOrderedRouteRoom {
            FloorIndex = 0, RoomIndex = index, RoomInstanceId = "room-" + index, Assignments = assignments,
            RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, HasActiveContent = assignments.Length > 0 };
        private static PhaseFiveBFork Fork(int room, int floor = 0, int assignments = 2) => new PhaseFiveBFork {
            FloorIndex = floor, RoomIndex = room, FloorInstanceId = "floor-" + floor,
            OptionalBranchId = "branch-" + room, EdgeId = "edge-" + room, Fingerprint = new string('a', 64),
            Assignments = Enumerable.Range(0, assignments).Select(n => new CorridorContentAssignment {
                AssignmentId = "corridor-" + room + "-" + n, Sequence = n, Tile = new TileCoordinate(n, 0),
                OptionId = n == assignments - 1 ? MvpDungeonPlacementIds.HiddenCacheOptionId : MvpDungeonPlacementIds.SpikeTrapOptionId,
                CategoryId = n == assignments - 1 ? MvpDungeonPlacementIds.LootNodeCategoryId : MvpDungeonPlacementIds.TrapCategoryId }).ToArray() };
        private static RunSimulationService.BranchTraversal Traversal(RunSimulationConfig c, params PhaseFiveBFork[] forks) =>
            new RunSimulationService.BranchTraversal { Forks = forks, Workload = new BranchRunWorkload(c.PhaseFiveB.BranchDecision) };
        private static RunOutcomeRecord Run(RunSimulationConfig c, RunSimulationService.BranchTraversal t, params MvpOrderedRouteRoom[] rooms) =>
            PhaseFiveBBranchIntegrationTests.Service(c).SimulateRoute(new StructureRuntimeState(), 17L, 1,
                RunPostureResolver.BalancedId, rooms, t);

        [Test]
        public void OriginDamagePrecedesDecisionAndBranchDamageCarriesIntoSuffix()
        {
            var c = Config(); var t = Traversal(c, Fork(0));
            var run = Run(c, t, Room(0, Assignment("origin", MvpDungeonPlacementIds.GoblinOptionId)),
                Room(1, Assignment("suffix", MvpDungeonPlacementIds.SnareTrapOptionId)));
            Assert.That(run.EncounterEvents.Select(e => e.AssignmentId), Is.EqualTo(new[] { "origin", "corridor-0-0", "suffix" }));
            Assert.That(t.Evidence.Single().Decision.AveragePartyHealth, Is.LessThan(1d));
            Assert.That(run.EncounterEvents[1].HealthBefore, Is.EqualTo(run.EncounterEvents[0].HealthAfter));
            Assert.That(run.EncounterEvents[2].HealthBefore, Is.EqualTo(run.EncounterEvents[1].HealthAfter));
            Assert.That(t.Evidence.Single().Returned, Is.True); Assert.That(run.ReachedRoomCount, Is.EqualTo(2));
            Assert.That(run.RoomResolutions.Last().CarriedLootValueAfterRoom, Is.EqualTo(run.LootSummary.TotalGeneratedWorldValue));
        }

        [TestCase(false)][TestCase(true)]
        public void StopOrWipeAtOriginMakesNoDecision(bool wipe)
        {
            var c = Config();
            if (wipe) { foreach (var p in c.PhaseFiveB.Classes) p.LevelOneMaxHealth = 1; }
            else { c.BaseSuccessChance = 0d; c.SuccessThreshold = 1d; }
            var t = Traversal(c, Fork(0));
            var run = Run(c, t, Room(0, Enumerable.Range(0, 3).Select(n => Assignment("monster-" + n, MvpDungeonPlacementIds.GoblinOptionId)).ToArray()),
                Room(1, Assignment("later", MvpDungeonPlacementIds.GoblinOptionId)));
            Assert.That(t.Evidence.Single().Decision, Is.Null); Assert.That(t.Evidence.Single().Traversed, Is.False);
            Assert.That(t.Evidence.Single().PrecedenceReason, Is.EqualTo("branch.decision.retreat_precedence"));
            Assert.That(run.ReachedRoomCount, Is.EqualTo(1)); Assert.That(run.EncounterEvents.Any(e => e.AssignmentId.StartsWith("corridor")), Is.False);
        }

        [Test]
        public void SkipContinuesRequiredRouteAndDoesNotTriggerOrRewardCorridor()
        {
            var c = Config(); c.PhaseFiveB.BranchDecision.SkipThreshold = .9d; c.PhaseFiveB.BranchDecision.EnterThreshold = 1d;
            var t = Traversal(c, Fork(0));
            var rooms = new[] { Room(0, Assignment("origin", MvpDungeonPlacementIds.GoblinOptionId)), Room(1, Assignment("later", MvpDungeonPlacementIds.SnareTrapOptionId)) };
            var branched = Run(c, t, rooms); var baseline = Run(c, null, rooms);
            Assert.That(t.Evidence.Single().Reason, Is.EqualTo("branch.outcome.skipped"));
            Assert.That(JsonUtility.ToJson(branched), Is.EqualTo(JsonUtility.ToJson(baseline)));
            Assert.That(t.Value, Is.Zero); Assert.That(t.CasualtyHeat, Is.Zero);
        }

        [Test]
        public void CorridorWipeStopsUnreachedLootAndCreatesNoPreciseKnowledge()
        {
            var c = Config(); foreach (var p in c.PhaseFiveB.Classes) { p.LevelOneMaxHealth = 1; p.TrapExpertise = 0d; }
            var t = Traversal(c, Fork(0));
            var run = Run(c, t, Room(0, Assignment("first", MvpDungeonPlacementIds.GoblinOptionId), Assignment("second", MvpDungeonPlacementIds.GoblinOptionId)),
                Room(1, Assignment("unreached", MvpDungeonPlacementIds.GoblinOptionId)));
            Assert.That(run.Party.IsWiped, Is.True); Assert.That(run.ReachedRoomCount, Is.EqualTo(1));
            Assert.That(t.Evidence.Single().Reason, Is.EqualTo("branch.outcome.wiped"));
            Assert.That(t.Evidence.Single().ReachedAssignments, Has.Length.EqualTo(1)); Assert.That(t.Value, Is.Zero);
            var knowledge = BranchKnowledgeLearning.Propose(new SharedBranchKnowledgeAuthority(), t.Evidence, false,
                run.RunId, c.PhaseFiveB.BranchDecision, t.Workload);
            Assert.That(knowledge.Records, Is.Empty);
        }

        [Test]
        public void ExpertDeathImmediatelyRetargetsNextTrapAndReturnAddsNoEvents()
        {
            var c = Config(); c.PhaseFiveB.TrapMitigationCoefficient = 0d;
            foreach (var p in c.PhaseFiveB.Classes) { p.LevelOneMaxHealth = 2; p.TrapExpertise = 0d; }
            var preview = RunPartyGenerator.Create(c.PhaseFiveB, "run-1");
            string expertClass = preview.Formation[0].ClassId;
            c.PhaseFiveB.Classes.Single(p => p.ClassId == expertClass).TrapExpertise = 1d;
            c.PhaseFiveB.BranchDecision.MaximumAssignmentsPerBranch = 3;
            var fork = Fork(0, 0, 3); var t = Traversal(c, fork);
            var run = Run(c, t, Room(0), Room(1));
            Assert.That(t.Evidence.Single().Encounters, Has.Length.EqualTo(2));
            var hits = t.Evidence.Single().Encounters;
            Assert.That(hits[0].HealthAfter, Is.Zero); Assert.That(hits[1].MemberOrdinal, Is.Not.EqualTo(hits[0].MemberOrdinal));
            Assert.That(hits[1].TrapExpertise, Is.EqualTo(preview.Members.Where(m => m.MemberOrdinal != hits[0].MemberOrdinal)
                .Select(m => m.ClassId == expertClass ? 1d : 0d).DefaultIfEmpty().Max()));
            Assert.That(t.Evidence.Single().Returned, Is.True); Assert.That(t.Evidence, Has.Count.EqualTo(1));
            Assert.That(run.EncounterEvents, Has.Length.EqualTo(2)); Assert.That(run.SurvivalSummary.DeathCount, Is.EqualTo(2));
            Assert.That(run.RunHeatDeltaSummary.DeathHeatDelta, Is.GreaterThanOrEqualTo(t.CasualtyHeat));
        }

        [TestCase("floor")][TestCase("run")][TestCase("assignments")][TestCase("knowledge")]
        public void PureCompleteRunWorkloadExactAndOneOverNeverPublishesPartialCandidate(string kind)
        {
            foreach (bool exceed in new[] { false, true })
            {
                var c = Config(); var d = c.PhaseFiveB.BranchDecision;
                int limit = kind == "floor" ? d.MaximumDecisionsPerFloor : kind == "run" ? d.MaximumDecisionsPerRun :
                    kind == "knowledge" ? d.MaximumKnowledgeUpdatesPerRun : d.MaximumAssignmentsPerBranch;
                int count = limit + (exceed ? 1 : 0);
                if (kind == "knowledge") d.MaximumDecisionsPerRun = count;
                int forks = kind == "assignments" ? 1 : count;
                var rooms = Enumerable.Range(0, forks).Select(n => Room(n)).ToArray();
                if (kind != "floor") for (int n = 0; n < rooms.Length; n++) rooms[n].FloorIndex = n;
                var projection = rooms.Select(r => Fork(r.RoomIndex, r.FloorIndex, kind == "assignments" ? count : 0)).ToArray();
                var t = Traversal(c, projection);
                var durable = PhaseFiveBBranchIntegrationTests.Fixture(false);
                var session = durable.Session;
                byte[] bytes = durable.FileSystem.ReadAllBytes(durable.ActivePath);
                var live = durable.Runtime; string before = JsonUtility.ToJson(live);
                string knowledgeBefore = JsonUtility.ToJson(live.sharedBranchKnowledge);
                var candidate = JsonUtility.FromJson<SaveData>(before);
                bool published = false;
                try
                {
                    var result = PhaseFiveBBranchIntegrationTests.Service(c).SimulateRoute(candidate.structureRuntime, 1L, 1,
                        RunPostureResolver.BalancedId, rooms, t);
                    candidate.sharedBranchKnowledge = BranchKnowledgeLearning.Propose(new SharedBranchKnowledgeAuthority(), t.Evidence,
                        !result.Party.IsWiped, result.RunId, d, t.Workload);
                    candidate.runHistory.AppendOutcome(result, c.MaxRunHistoryEntries);
                    published = true; // test-only commit sentinel; never entered after any calculation failure
                }
                catch (InvalidOperationException e) { Assert.That(e.Message, Is.EqualTo(BranchRunWorkload.WorkloadExceeded)); }
                Assert.That(published, Is.EqualTo(!exceed), kind);
                Assert.That(JsonUtility.ToJson(live), Is.EqualTo(before));
                Assert.That(JsonUtility.ToJson(live.sharedBranchKnowledge), Is.EqualTo(knowledgeBefore));
                Assert.That(durable.Runtime, Is.SameAs(live));
                Assert.That(durable.Session, Is.SameAs(session));
                CollectionAssert.AreEqual(bytes, durable.FileSystem.ReadAllBytes(durable.ActivePath));
            }
        }

        [TestCase(false)][TestCase(true)]
        public void BranchMonstersFailClosedEvenWhenDecisionWouldSkip(bool skip)
        {
            var c = Config(); if (skip) { c.PhaseFiveB.BranchDecision.SkipThreshold = .9d; c.PhaseFiveB.BranchDecision.EnterThreshold = 1d; }
            var fork = Fork(0); fork.Assignments[0].CategoryId = MvpDungeonPlacementIds.MonsterCategoryId;
            fork.Assignments[0].OptionId = MvpDungeonPlacementIds.GoblinOptionId;
            var t = Traversal(c, fork);
            Assert.That(Assert.Throws<ArgumentException>(() => Run(c, t, Room(0))).Message, Is.EqualTo(PhaseFiveBRouteProjection.InvalidRoute));
            Assert.That(t.Evidence, Is.Empty);
        }

        [Test]
        public void BranchLootAndDamageRepeatWithoutGlobalRngOrTickCoupling()
        {
            var c = Config(); var a = Traversal(c, Fork(0)); var b = Traversal(c, Fork(0));
            var rooms = new[] { Room(0, Assignment("origin", MvpDungeonPlacementIds.GoblinOptionId)) };
            var first = Run(c, a, rooms); var previous = UnityEngine.Random.state;
            try { UnityEngine.Random.InitState(91); Run(c, b, rooms); }
            finally { UnityEngine.Random.state = previous; }
            Assert.That(a.Value, Is.EqualTo(b.Value)); Assert.That(a.Items, Is.EqualTo(b.Items));
            Assert.That(a.Evidence.Single().Decision.DecisionRoll, Is.EqualTo(b.Evidence.Single().Decision.DecisionRoll));
            var differentTick = Traversal(c, Fork(0));
            PhaseFiveBBranchIntegrationTests.Service(c).SimulateRoute(new StructureRuntimeState(), 999L, 1,
                RunPostureResolver.BalancedId, rooms, differentTick);
            Assert.That(differentTick.Value, Is.EqualTo(a.Value));
            Assert.That(differentTick.Evidence.Single().Decision.DecisionRoll, Is.EqualTo(a.Evidence.Single().Decision.DecisionRoll));
            Assert.That(first.EncounterEvents.Last().Damage, Is.EqualTo(b.Evidence.Single().Encounters.Single().Damage));
        }
    }
}
#endif
