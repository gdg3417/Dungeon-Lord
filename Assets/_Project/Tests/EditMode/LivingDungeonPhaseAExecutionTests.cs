#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.Structures;
using DungeonBuilder.M0.Economy;
using NUnit.Framework;
using UnityEngine;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseAExecutionTests
    {
        // Explicit test-only logical units. No production duration is approved.
        internal static RunLogicalTiming Timing() => new RunLogicalTiming(3, 5, 7, 11, 13);
        private static RunExecution Start(RunSimulationConfig c = null, long? limit = null, RunLogicalTiming timing = null) =>
            new RunSimulationService(c ?? B.Configuration("clear"), PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId,
                new[] { B.Room(0, true, B.Content()), B.Room(1, true, B.Content()) }, timing ?? Timing(), limit);
        public static object CreateExecutionForPlayMode() => Start();
        public static object CreateNonspatialExecutionForPlayMode() => NonspatialLoot();
        public static string CompleteForPlayMode(object execution) => B.Structured(((RunExecution)execution).DrainToCompletion());
        public static void AdvanceForPlayMode(object execution)
        { var state=(RunExecution)execution; state.Advance(1,state.NextOrdinal); }
        private static string Steps(IEnumerable<RunStep> steps) => string.Join("\n", steps.Select(s =>
            s.EventId + ":" + s.Kind + ":" + s.LogicalTime + ":" + s.FloorIndex + ":" + s.RoomId + ":" +
            s.AssignmentId + ":" + s.Position.X + "," + s.Position.Y + ":" + s.PositionSpace + ":" + s.Distance + ":" +
            s.ContinueChosen + ":" + s.CarriedLootValueAfter + ":" + B.Structured(s.Encounter)));

        private static RunExecution NonspatialLoot(bool wipe = false) =>
            new RunSimulationService(B.Configuration(wipe ? "wipe" : "clear"), PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId,
                new[] { B.Room(0, false, wipe ? Enumerable.Range(0, 3).Select(i => B.Assignment("killer" + i,
                    MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 0, 1, i))
                    .Concat(B.Content().Where(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId)).ToArray()
                    : B.Content()) });

        [Test]
        public void NonspatialLootBoundaryCountsReachOnceAndDefersRewardToRoomComplete()
        {
            var state = NonspatialLoot();
            Assert.That(state.AdvanceToNextEvent(0).Kind, Is.EqualTo(RunStepKind.RoomEntry));
            for (int i = 0; i < 4; i++)
                Assert.That(state.AdvanceToNextEvent(state.NextOrdinal).Encounter, Is.Not.Null);
            var before = state.Inspect();
            Assert.That(before.ReachedLootInteractions, Is.Zero);
            Assert.That(before.CarriedLootValue, Is.Zero);
            var first = state.AdvanceToNextEvent(state.NextOrdinal);
            Assert.That(first.Kind, Is.EqualTo(RunStepKind.LootInteraction));
            Assert.That(first.AssignmentId, Is.EqualTo("l1"));
            Assert.That(first.Encounter, Is.Null);
            Assert.That(state.Inspect().ReachedLootInteractions, Is.EqualTo(1));
            CollectionAssert.AreEqual(before.Members.Select(m => m.Health), state.Inspect().Members.Select(m => m.Health));
            Assert.That(state.Inspect().CarriedLootValue, Is.Zero);
            var second = state.AdvanceToNextEvent(state.NextOrdinal);
            Assert.That(second.Kind, Is.EqualTo(RunStepKind.LootInteraction));
            Assert.That(second.AssignmentId, Is.EqualTo("l2"));
            Assert.That(state.Inspect().ReachedLootInteractions, Is.EqualTo(2));
            Assert.That(state.Inspect().CarriedLootValue, Is.Zero);
            Assert.That(state.AdvanceToNextEvent(state.NextOrdinal).Kind, Is.EqualTo(RunStepKind.RoomComplete));
            Assert.That(state.Inspect().CarriedLootValue, Is.GreaterThan(0));
            var outcome = state.DrainToCompletion();
            Assert.That(outcome.LootSummary.TotalGeneratedWorldValue, Is.GreaterThan(0));
            Assert.That(state.Inspect().ReachedLootInteractions, Is.EqualTo(2));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(7)]
        public void NonspatialLootIdentitiesAndOrdinalsAreBatchIndependent(int budget)
        {
            var one = NonspatialLoot(); var many = NonspatialLoot();
            var a = new List<RunStep>(); var b = new List<RunStep>();
            while (!one.IsTerminal) a.AddRange(one.Advance(1, one.NextOrdinal));
            while (!many.IsTerminal) b.AddRange(many.Advance(budget, many.NextOrdinal));
            Assert.That(Steps(b), Is.EqualTo(Steps(a)));
            foreach (string id in new[] { "l1", "l2" })
            {
                var interaction = a.Single(s => s.Kind == RunStepKind.LootInteraction && s.AssignmentId == id);
                Assert.That(interaction.Ordinal, Is.LessThan(a.Single(s => s.Kind == RunStepKind.RoomComplete).Ordinal));
                Assert.That(interaction.CarriedLootValueAfter, Is.Zero);
            }
            CollectionAssert.AreEqual(Enumerable.Range(0, a.Count).Select(i => (long)i), a.Select(s => s.Ordinal));
            Assert.That(B.Structured(many.TakeOutcome()), Is.EqualTo(B.Structured(one.TakeOutcome())));
        }

        [Test]
        public void NonspatialWipeDoesNotEmitUnreachedLootBoundaries()
        {
            var state = NonspatialLoot(true); var steps = new List<RunStep>();
            while (!state.IsTerminal) steps.Add(state.AdvanceToNextEvent(state.NextOrdinal));
            Assert.That(state.TakeOutcome().Party.IsWiped, Is.True);
            Assert.That(steps.Any(s => s.Kind == RunStepKind.LootInteraction), Is.False);
            Assert.That(state.Inspect().ReachedLootInteractions, Is.Zero);
            Assert.That(state.Inspect().CarriedLootValue, Is.Zero);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(7)]
        public void DifferentBatchesHaveIdenticalEventSequenceAndStructuredOutcome(int budget)
        {
            var a = Start(); var b = Start(); var one = new List<RunStep>(); var many = new List<RunStep>();
            while (!a.IsTerminal) one.AddRange(a.Advance(1, a.NextOrdinal));
            while (!b.IsTerminal) many.AddRange(b.Advance(budget, b.NextOrdinal));
            Assert.That(Steps(many), Is.EqualTo(Steps(one)));
            Assert.That(B.Structured(b.TakeOutcome()), Is.EqualTo(B.Structured(a.TakeOutcome())));
            CollectionAssert.AreEqual(Enumerable.Range(0, one.Count).Select(i => (long)i), one.Select(s => s.Ordinal));
            Assert.That(one.Select(s => s.EventId).Distinct().Count(), Is.EqualTo(one.Count));
        }
        [Test]
        public void PartyHpChangesOnlyWhenEncounterExecutesAndLootWaitsForInteraction()
        {
            var state = Start(); var initial = state.Inspect(); Assert.That(initial.ResolvedEncounters, Is.Zero);
            Assert.That(initial.CarriedLootValue, Is.Zero); Assert.That(initial.ReachedLootInteractions, Is.Zero);
            var entry = state.AdvanceToNextEvent(0); Assert.That(entry.Kind, Is.EqualTo(RunStepKind.RoomEntry));
            CollectionAssert.AreEqual(initial.Members.Select(m => m.Health), state.Inspect().Members.Select(m => m.Health));
            while (state.Inspect().ResolvedEncounters == 0)
            {
                var before = state.Inspect(); var step = state.AdvanceToNextEvent(state.NextOrdinal); var after = state.Inspect();
                if (step.Encounter == null) CollectionAssert.AreEqual(before.Members.Select(m => m.Health), after.Members.Select(m => m.Health));
                else {
                    Assert.That(step.Encounter.HealthBefore, Is.EqualTo(before.Members[step.Encounter.MemberOrdinal].Health));
                    Assert.That(step.Encounter.HealthAfter, Is.EqualTo(after.Members[step.Encounter.MemberOrdinal].Health));
                    Assert.That(after.Members.Sum(m => m.Health), Is.LessThan(before.Members.Sum(m => m.Health)));
                }
            }
            Assert.That(state.Inspect().CarriedLootValue, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => state.TakeOutcome());
            var result = state.DrainToCompletion();
            Assert.That(result.LootSummary.TotalGeneratedWorldValue, Is.GreaterThan(0));
            Assert.That(result.EncounterEvents.Select(e => e.FloorIndex + "/" + e.RoomIndex + "/" + e.AssignmentId).Distinct().Count(),
                Is.EqualTo(result.EncounterEvents.Length));
            Assert.That(state.Advance(1, state.NextOrdinal), Is.Empty);
            Assert.Throws<InvalidOperationException>(() => state.TakeOutcome());
        }
        [Test]
        public void WipeLeavesFutureTrapsLootAndSecondRoomUnresolved()
        {
            var monsters = Enumerable.Range(0,3).Select(i => B.Assignment("killer"+i,
                MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId,0,1,i)).ToArray();
            var state = new RunSimulationService(B.Configuration("wipe"),PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { ManaReserve=100 },42,1,RunPostureResolver.BalancedId,new[] {
                    B.Room(0,true,monsters.Concat(B.Content().Where(a=>a.CategoryId!=MvpDungeonPlacementIds.MonsterCategoryId)).ToArray()),
                    B.Room(1,true,B.Content()) },Timing());
            var steps = new List<RunStep>();
            while (!state.IsTerminal) steps.Add(state.AdvanceToNextEvent(state.NextOrdinal));
            var outcome = state.TakeOutcome();
            Assert.That(outcome.Party.IsWiped, Is.True); Assert.That(outcome.ReachedRoomCount, Is.EqualTo(1));
            Assert.That(steps.Any(s => s.Kind == RunStepKind.LootInteraction), Is.False);
            Assert.That(steps.Any(s => s.AssignmentId == "t2"), Is.False);
            Assert.That(outcome.LootSummary.TotalGeneratedWorldValue, Is.Zero);
        }
        [Test]
        public void LiveConfigGeometryAndRuntimeEditsCannotAlterContinuation()
        {
            var c = B.Configuration("clear"); var room = B.Room(0, true, B.Content());
            var runtime = new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 };
            var service = new RunSimulationService(c, PhaseSixA4Tests.Loot());
            var state = service.StartRoute(runtime, 42, 1, RunPostureResolver.BalancedId, new[] { room }, Timing());
            var expected = service.SimulateRoute(new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 }, 42, 1,
                RunPostureResolver.BalancedId, new[] { room });
            state.AdvanceToNextEvent(0);
            foreach (var p in c.PhaseFiveB.DamageProfiles) p.MinimumDamage = p.MaximumDamage = int.MaxValue;
            room.Spatial.TraversableTiles = Array.Empty<TileCoordinate>(); room.Assignments = Array.Empty<RunRoomAssignment>();
            runtime.Heat = 30; runtime.ManaReserve = 0;
            Assert.That(B.Structured(state.DrainToCompletion()), Is.EqualTo(B.Structured(expected)));
            Assert.That(runtime.Heat, Is.EqualTo(30)); Assert.That(runtime.ManaReserve, Is.Zero);
        }
        [Test]
        public void InvalidBudgetStaleCursorWorkloadAndOverflowFailWithoutPublication()
        {
            var state = Start(limit: 1); var before = B.Structured(state.Inspect());
            Assert.Throws<ArgumentException>(() => state.Advance(0, 0));
            Assert.Throws<InvalidOperationException>(() => state.Advance(1, 2));
            Assert.That(B.Structured(state.Inspect()), Is.EqualTo(before));
            state.Advance(1, 0);
            Assert.Throws<InvalidOperationException>(() => state.Advance(1, 0));
            Assert.Throws<InvalidOperationException>(() => state.Advance(1, 1));
            Assert.That(state.Inspect().FailureReason, Is.EqualTo("run.execution.failed"));
            Assert.Throws<InvalidOperationException>(() => state.DrainToCompletion());
            var overflow = Start(timing: new RunLogicalTiming(long.MaxValue, long.MaxValue, 0, 0, 0));
            Assert.Throws<OverflowException>(() => overflow.DrainToCompletion());
            Assert.Throws<ArgumentException>(() => new RunLogicalTiming(0, 1, 1, 1, 1));
        }
        [Test]
        public void ZeroDurationInteractionsStillHaveFiniteOrderedProgress()
        {
            var state = Start(timing: new RunLogicalTiming(1, 0, 0, 0, 0));
            var steps = new List<RunStep>(); while (!state.IsTerminal) steps.Add(state.AdvanceToNextEvent(state.NextOrdinal));
            Assert.That(steps.Count, Is.LessThan(100));
            Assert.That(steps.Zip(steps.Skip(1), (a,b) => b.LogicalTime >= a.LogicalTime).All(x => x), Is.True);
            Assert.That(state.TakeOutcome(), Is.Not.Null);
        }
        [Test]
        public void LogicalTimeEqualsActualTraversedEdgesAndConfiguredInteractions()
        {
            var state = Start(); var steps = new List<RunStep>(); while (!state.IsTerminal) steps.Add(state.AdvanceToNextEvent(state.NextOrdinal));
            long expected = steps.Sum(s => 3L * s.Distance + (s.Kind == RunStepKind.MonsterEngagement ? 5 :
                s.Kind == RunStepKind.TrapTrigger ? 7 : s.Kind == RunStepKind.LootInteraction ? 11 : 0));
            Assert.That(state.Inspect().LogicalTime, Is.EqualTo(expected));
            foreach (var group in steps.Where(s => s.Kind == RunStepKind.Movement).GroupBy(s => s.RoomId)) {
                var path = group.ToArray();
                for (int i = 1; i < path.Length; i++) Assert.That(Math.Abs(path[i].Position.X-path[i-1].Position.X) +
                    Math.Abs(path[i].Position.Y-path[i-1].Position.Y), Is.EqualTo(1));
            }
        }
        [Test]
        public void BranchDecisionAndRewardsWaitForTheirBoundaryAndReturnCannotDuplicate()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture(); var state = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot())
                .StartSnapshot(1, PhaseSixA4Tests.Snapshot(f), Timing());
            while (true) { var step = state.AdvanceToNextEvent(state.NextOrdinal); if (step.Kind == RunStepKind.BranchDecision) {
                    Assert.That(step.ContinueChosen,Is.True); break; }
                Assert.That(state.Inspect().ResolvedEncounters, Is.Zero); Assert.That(state.Inspect().CarriedLootValue, Is.Zero); }
            Assert.That(state.Inspect().CarriedLootValue, Is.Zero);
            var remaining = new List<RunStep>(); while (!state.IsTerminal) remaining.Add(state.AdvanceToNextEvent(state.NextOrdinal));
            var result = state.TakeOutcome(); Assert.That(result.BranchOutcomes.Single().Returned, Is.True);
            Assert.That(remaining.Count(s => s.Kind == RunStepKind.TrapTrigger), Is.EqualTo(1));
            Assert.That(remaining.Count(s => s.Kind == RunStepKind.LootInteraction), Is.EqualTo(1));
            Assert.That(remaining.Count(s => s.Kind == RunStepKind.BranchReturn), Is.EqualTo(1));
            Assert.That(result.LootSummary.TotalGeneratedWorldValue, Is.EqualTo(state.Inspect().CarriedLootValue));
        }
        [Test]
        public void FloorTransferKeepsExactPartyHpAndNoFutureFloorEncounter()
        {
            var f = PhaseSixA4Tests.Eligible(); PhaseSixA4Tests.ForceDescent(f.Configuration, true);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 1, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,
                FloorLifecycleAction.Activate,PhaseSixA4Tests.FloorTwo(f)));
            var state = new RunSimulationService(f.Configuration, PhaseSixA4Tests.Loot()).StartSnapshot(1, PhaseSixA4Tests.Snapshot(f), Timing());
            while (true) {
                var before = state.Inspect(); var step = state.AdvanceToNextEvent(state.NextOrdinal);
                if (step.Kind == RunStepKind.FloorTransfer) {
                    CollectionAssert.AreEqual(before.Members.Select(m => m.Health), state.Inspect().Members.Select(m => m.Health));
                    Assert.That(state.Inspect().ResolvedEncounters, Is.EqualTo(1)); break;
                }
            }
            Assert.That(state.DrainToCompletion().EncounterEvents, Has.Length.EqualTo(2));
        }
        [Test]
        public void SmallWorkBudgetDoesNotExecuteLargeFutureRoute()
        {
            var route = Enumerable.Range(0, 100).Select(i => B.Room(i, true, B.Content())).ToArray();
            long allocated = GC.GetAllocatedBytesForCurrentThread(); var clock=System.Diagnostics.Stopwatch.StartNew();
            var config=B.Configuration("clear"); foreach(var member in config.PhaseFiveB.Classes) member.LevelOneMaxHealth=1000000;
            var state = new RunSimulationService(config, PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId, route, Timing());
            long captureAllocated=GC.GetAllocatedBytesForCurrentThread()-allocated; double captureMs=clock.Elapsed.TotalMilliseconds;
            allocated=GC.GetAllocatedBytesForCurrentThread(); clock.Restart();
            state.Advance(1,0); long advanceAllocated=GC.GetAllocatedBytesForCurrentThread()-allocated; clock.Stop();
            Assert.That(state.Inspect().ResolvedEncounters, Is.Zero);
            Assert.That(state.Inspect().ReachedLootInteractions, Is.Zero); Assert.That(state.IsTerminal, Is.False);
            TestContext.WriteLine("100-room captured route; one-event advancement executes zero encounters/rewards. Capture bytes="+
                captureAllocated+", ms="+captureMs+"; first advancement bytes="+advanceAllocated+", ms="+clock.Elapsed.TotalMilliseconds);
            allocated=GC.GetAllocatedBytesForCurrentThread(); clock.Restart(); var outcome=state.DrainToCompletion(); clock.Stop();
            Assert.That(outcome.ReachedRoomCount,Is.EqualTo(100));
            TestContext.WriteLine("Drain of remaining 100-room workload: events="+state.NextOrdinal+", bytes="+
                (GC.GetAllocatedBytesForCurrentThread()-allocated)+", ms="+clock.Elapsed.TotalMilliseconds);
        }
        [Test]
        public void InvalidInputsAndOpaqueCursorCorruptionCannotProduceOutcome()
        {
            var service=new RunSimulationService(B.Configuration("clear"),PhaseSixA4Tests.Loot());
            foreach(double heat in new[] { double.NaN,double.PositiveInfinity,-1d })
                Assert.Throws<ArgumentException>(()=>service.StartRoute(new StructureRuntimeState { Heat=heat },42,1,
                    RunPostureResolver.BalancedId,new[] { B.Room(0,true,B.Content()) }));
            var state=Start();
            typeof(RunExecution).GetField("cursor",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(state,null);
            Assert.Throws<InvalidOperationException>(()=>state.Advance(1,0));
            Assert.That(state.IsTerminal,Is.False); Assert.Throws<InvalidOperationException>(()=>state.TakeOutcome());
        }
        [Test]
        public void UnreachablePlacementsAndOffPathTrapStayUnresolvedAcrossPauses()
        {
            var room=B.Room(0,true,B.Assignment("offpath",MvpDungeonPlacementIds.TrapCategoryId,MvpDungeonPlacementIds.SpikeTrapOptionId,1,3),
                B.Assignment("isolatedLoot",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,3,3),
                B.Assignment("isolatedMonster",MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId,3,3));
            room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(new TileCoordinate(2,3))&&!t.Equals(new TileCoordinate(3,2))).ToArray();
            var state=new RunSimulationService(B.Configuration("clear"),PhaseSixA4Tests.Loot()).StartRoute(new StructureRuntimeState { ManaReserve=100 },
                42,1,RunPostureResolver.BalancedId,new[] { room },Timing());
            while(!state.IsTerminal) {
                state.Advance(1,state.NextOrdinal); var view=state.Inspect();
                Assert.That(view.ResolvedEncounters,Is.Zero); Assert.That(view.ReachedLootInteractions,Is.Zero); Assert.That(view.CarriedLootValue,Is.Zero);
            }
            var result=state.TakeOutcome(); Assert.That(result.EncounterEvents,Is.Empty);
            Assert.That(result.ReachedRoutePlacementEffects.ContributingOptionIds,Has.Length.EqualTo(1));
        }
        [Test]
        public void CanonicalDungeonEditDuringPausedRunAffectsOnlyLaterSnapshot()
        {
            var f=PhaseFiveBBranchIntegrationTests.Fixture(); var snapshot=PhaseSixA4Tests.Snapshot(f);
            var service=new RunSimulationService(f.Configuration,PhaseSixA4Tests.Loot());
            var state=service.StartSnapshot(1,snapshot,Timing()); state.Advance(1,0);
            var expected=service.SimulateSnapshot(1,snapshot);
            PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId);
            Assert.That(B.Structured(state.DrainToCompletion()),Is.EqualTo(B.Structured(expected)));
            var later=service.SimulateSnapshot(1,PhaseSixA4Tests.Snapshot(f));
            Assert.That(later.EncounterEvents.Length,Is.GreaterThan(expected.EncounterEvents.Length));
        }
    }
}
#endif
