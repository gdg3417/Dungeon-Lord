using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public enum RunStepKind
    {
        RoomEntry, Movement, MonsterEngagement, TrapTrigger, LootInteraction, UnreachableInteraction,
        RoomEgress, RoomComplete, BranchDecision, BranchReturn, FloorDecision, FloorTransfer, Terminal
    }
    public enum RunPositionSpace { None, RoomLocal, Floor }

    // No production values are chosen here. Null timing means ordinal-only manual execution.
    public sealed class RunLogicalTiming
    {
        public long UnitsPerTileEdge { get; }
        public long MonsterInteractionUnits { get; }
        public long TrapInteractionUnits { get; }
        public long LootInteractionUnits { get; }
        public long FloorTransferUnits { get; }
        public RunLogicalTiming(long unitsPerTileEdge, long monsterInteractionUnits, long trapInteractionUnits,
            long lootInteractionUnits, long floorTransferUnits)
        {
            if (unitsPerTileEdge <= 0 || monsterInteractionUnits < 0 || trapInteractionUnits < 0 ||
                lootInteractionUnits < 0 || floorTransferUnits < 0) throw new ArgumentException("run.execution.invalid_timing");
            UnitsPerTileEdge = unitsPerTileEdge; MonsterInteractionUnits = monsterInteractionUnits;
            TrapInteractionUnits = trapInteractionUnits; LootInteractionUnits = lootInteractionUnits;
            FloorTransferUnits = floorTransferUnits;
        }
        internal long Duration(RunStepKind kind, int distance) => checked(distance * UnitsPerTileEdge +
            (kind == RunStepKind.MonsterEngagement ? MonsterInteractionUnits : kind == RunStepKind.TrapTrigger ? TrapInteractionUnits :
             kind == RunStepKind.LootInteraction ? LootInteractionUnits : kind == RunStepKind.FloorTransfer ? FloorTransferUnits : 0));
    }

    public readonly struct RunStep
    {
        public RunStepKind Kind { get; }
        public string RunId { get; }
        public long Ordinal { get; }
        public string EventId => RunId + "/" + Ordinal.ToString(CultureInfo.InvariantCulture);
        public long LogicalTime { get; }
        public int FloorIndex { get; }
        public string RoomId { get; }
        public string AssignmentId { get; }
        public TileCoordinate Position { get; }
        public int Distance { get; }
        public RunPositionSpace PositionSpace { get; }
        public bool? ContinueChosen { get; }
        public int CarriedLootValueAfter { get; }
        // Resolver evidence is already read-only to callers; includes exact HP/death causality.
        public RunEncounterEvent Encounter { get; }
        internal RunStep(RunStepKind kind, int floor, string room, string assignment = null,
            TileCoordinate position = default, int distance = 0, RunEncounterEvent encounter = null,
            string run = null, long ordinal = 0, long time = 0, RunPositionSpace space = RunPositionSpace.None,
            bool? continueChosen = null, int carriedLootValue = 0)
        { Kind = kind; FloorIndex = floor; RoomId = room; AssignmentId = assignment;
          Position = position; Distance = distance; Encounter = encounter; RunId = run; Ordinal = ordinal; LogicalTime = time;
          PositionSpace = space; ContinueChosen = continueChosen; CarriedLootValueAfter = carriedLootValue; }
        internal RunStep Stamp(string run, long ordinal, long time, int carriedLootValue) =>
            new RunStep(Kind, FloorIndex, RoomId, AssignmentId, Position, Distance, Encounter, run, ordinal, time,
                PositionSpace, ContinueChosen, carriedLootValue);
    }

    public readonly struct RunMemberState
    {
        public int Ordinal { get; }
        public string ClassId { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        internal RunMemberState(RunPartyMember member)
        { Ordinal = member.MemberOrdinal; ClassId = member.ClassId; Health = member.CurrentHealth; MaxHealth = member.MaxHealth; }
    }

    public sealed class RunExecutionInspection
    {
        public string RunId { get; internal set; }
        public long NextOrdinal { get; internal set; }
        public long LogicalTime { get; internal set; }
        public bool HasTiming { get; internal set; }
        public bool Terminal { get; internal set; }
        public string FailureReason { get; internal set; }
        public RunStep? LastStep { get; internal set; }
        public IReadOnlyList<RunMemberState> Members { get; internal set; }
        public int CarriedLootValue { get; internal set; }
        public int ResolvedEncounters { get; internal set; }
        public int ReachedLootInteractions { get; internal set; }
    }

    // Opaque in-memory continuation; the existing simulation iterators own all gameplay locals.
    // Single caller only. No public setters, cursor reconstruction, save writes or retained event log.
    public sealed class RunExecution
    {
        private IEnumerator<RunStep> cursor;
        private readonly RunLogicalTiming timing;
        private readonly long maximumEvents;
        private bool advancing, taken;
        private long ordinal, logicalTime;
        private RunStep? last;
        private string failure;
        internal RunParty Party { get; }
        internal RunOutcomeRecord Outcome;
        internal RunSimulationService.BranchTraversal Traversal;
        internal ActiveFloorRunSnapshot Snapshot;
        internal int CarriedLootValue;
        private int encounters, lootInteractions;
        public bool IsTerminal { get; private set; }
        public long NextOrdinal => ordinal;
        internal RunExecution(RunParty party, RunLogicalTiming timing, long maximumEvents)
        {
            Party = party; this.timing = timing;
            if (maximumEvents <= 0) throw new ArgumentException("run.execution.invalid_limit");
            this.maximumEvents = maximumEvents;
        }
        internal void Initialize(IEnumerable<RunStep> steps) => cursor = steps.GetEnumerator();
        public RunExecutionInspection Inspect() => new RunExecutionInspection {
            RunId = Party.RunId, NextOrdinal = ordinal, LogicalTime = logicalTime, HasTiming = timing != null,
            Terminal = IsTerminal, FailureReason = failure, LastStep = last,
            Members = Array.AsReadOnly(Party.Members.Select(m => new RunMemberState(m)).ToArray()),
            CarriedLootValue = CarriedLootValue, ResolvedEncounters = encounters, ReachedLootInteractions = lootInteractions };

        public RunStep[] Advance(int eventBudget, long expectedNextOrdinal) =>
            AdvanceCore(eventBudget, expectedNextOrdinal, true);

        private RunStep[] AdvanceCore(int eventBudget, long expectedNextOrdinal, bool observe)
        {
            if (eventBudget <= 0 || eventBudget > maximumEvents) throw new ArgumentException("run.execution.invalid_budget");
            if (expectedNextOrdinal != ordinal || advancing) throw new InvalidOperationException("run.execution.stale_cursor");
            if (failure != null) throw new InvalidOperationException(failure);
            var batch = observe ? new List<RunStep>() : null;
            int processed = 0; advancing = true;
            try
            {
                while (processed < eventBudget && !IsTerminal)
                {
                    if (ordinal >= maximumEvents) throw new InvalidOperationException("run.execution.workload_exceeded");
                    if (cursor == null || !cursor.MoveNext()) throw new InvalidOperationException("run.execution.invalid_cursor");
                    var step = cursor.Current;
                    if (step.Distance < 0) throw new InvalidOperationException("run.execution.invalid_cursor");
                    logicalTime = checked(logicalTime + (timing?.Duration(step.Kind, step.Distance) ?? 0));
                    last = step.Stamp(Party.RunId, ordinal++, logicalTime, CarriedLootValue);
                    if (step.Encounter != null) encounters++;
                    if (step.Kind == RunStepKind.LootInteraction) lootInteractions++;
                    batch?.Add(last.Value); processed++;
                    if (step.Kind == RunStepKind.Terminal)
                    {
                        if (Outcome == null) throw new InvalidOperationException("run.execution.invalid_cursor");
                        IsTerminal = true; cursor.Dispose();
                    }
                }
                return batch?.ToArray() ?? Array.Empty<RunStep>();
            }
            catch { failure = "run.execution.failed"; cursor?.Dispose(); throw; }
            finally { advancing = false; }
        }
        public RunStep AdvanceToNextEvent(long expectedNextOrdinal)
        {
            var steps = Advance(1, expectedNextOrdinal);
            if (steps.Length == 0) throw new InvalidOperationException("run.execution.terminal");
            return steps[0];
        }
        public RunOutcomeRecord DrainToCompletion()
        { while (!IsTerminal) AdvanceCore(1, ordinal, false); return TakeOutcome(); }
        public RunOutcomeRecord TakeOutcome()
        {
            if (!IsTerminal || taken || failure != null) throw new InvalidOperationException("run.execution.outcome_unavailable");
            taken = true; return Outcome;
        }
    }
}
