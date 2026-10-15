using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    public enum CombatC0EventKind { EncounterStart, Eligibility, Attack, Skipped, EncounterComplete, Bypass }
    public enum CombatC0Reason
    {
        Legal, MeleePositionUnavailable, OutOfRange, LineOfSightBlocked, Unreachable,
        DefeatedBeforeAction, NoLivingLegalTarget, DefendersDefeated, PartyWiped, NoParticipants,
        ZeroDamageRound, RoundLimit, PartyAnchorConflict
    }

    // Immutable values only. No serializer, player text, playback timing or save owner.
    public sealed class CombatC0Event
    {
        public string RulesId => ExecutableId;
        public string ExecutableId { get; }
        public string RuleSourceId { get; }
        public long RunOrdinal { get; }
        public string RunId { get; }
        public string EncounterId { get; }
        public int Ordinal { get; }
        public int ActionOrdinal { get; }
        public int Round { get; }
        public string FloorId { get; }
        public string RoomId { get; }
        public CombatC0EventKind Kind { get; }
        public CombatC0Reason Reason { get; }
        public string ActorId { get; }
        public string TargetId { get; }
        public string AttackId { get; }
        public string TargetingId { get; }
        public TileCoordinate? SourcePosition { get; }
        public TileCoordinate? TargetPosition { get; }
        public RunPositionSpace PositionSpace => SourcePosition.HasValue ? RunPositionSpace.RoomLocal : RunPositionSpace.None;
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public int Damage { get; }
        public bool Defeated => Kind == CombatC0EventKind.Attack && HealthBefore > 0 && HealthAfter == 0;
        internal CombatC0Event(string run, string encounter, int ordinal, int action, int round,
            string floor, string room, CombatC0EventKind kind, CombatC0Reason reason, string actor = null,
            string target = null, string attack = null, TileCoordinate? source = null, TileCoordinate? destination = null,
            int before = 0, int after = 0, string executable = null, string targeting = null, long runOrdinal = -1)
        {
            RunId = run; EncounterId = encounter; Ordinal = ordinal; ActionOrdinal = action; Round = round;
            FloorId = floor; RoomId = room; Kind = kind; Reason = reason; ActorId = actor; TargetId = target;
            AttackId = attack; SourcePosition = source; TargetPosition = destination;
            HealthBefore = before; HealthAfter = after; Damage = before - after;
            ExecutableId = executable ?? CombatC0QualificationConfig.Rules;
            RuleSourceId = ExecutableId == CombatC0Config.Executable ? CombatC0Config.Rule : ExecutableId;
            TargetingId = targeting ?? CombatC0QualificationConfig.Targeting; RunOrdinal = runOrdinal;
        }
        internal CombatC0Event AtBoundary(long ordinal) => new CombatC0Event(RunId, EncounterId, Ordinal, ActionOrdinal,
            Round, FloorId, RoomId, Kind, Reason, ActorId, TargetId, AttackId, SourcePosition, TargetPosition,
            HealthBefore, HealthAfter, ExecutableId, TargetingId, ordinal);
    }

    internal sealed class CombatC0EncounterResult
    {
        public CombatC0Reason Completion { get; }
        internal IReadOnlyList<CombatC0Event> Events { get; }
        internal CombatC0EncounterResult(CombatC0Reason completion, CombatC0Event[] events)
        { Completion = completion; Events = Array.AsReadOnly((CombatC0Event[])events.Clone()); }
    }

    internal sealed class CombatC0MonsterPlacement
    {
        public string AssignmentId { get; }
        public string OptionId { get; }
        public string FloorId { get; }
        public string RoomId { get; }
        internal long Sequence { get; }
        internal CombatC0MonsterPlacement(string assignment, string option, string floor, string room, long sequence)
        { AssignmentId = assignment; OptionId = option; FloorId = floor; RoomId = room; Sequence = sequence; }
    }

    // The geometry adapter supplies directed legality; the kernel never equates movement
    // reachability with visibility. Qualification may inject explicit synthetic links.
    internal sealed class CombatC0AttackLink
    {
        public string ActorId { get; }
        public string TargetId { get; }
        internal TileCoordinate Source { get; }
        internal TileCoordinate Target { get; }
        public CombatC0Reason Reason { get; }
        internal CombatC0AttackLink(string actor, string target, TileCoordinate source,
            TileCoordinate destination, CombatC0Reason reason)
        { ActorId = actor; TargetId = target; Source = source; Target = destination; Reason = reason; }
    }

    public sealed class CombatC0MonsterState
    {
        public string AssignmentId { get; }
        public string OptionId { get; }
        public string FloorId { get; }
        public string RoomId { get; }
        public int MaxHealth { get; }
        public int Health { get; }
        public bool Defeated => Health == 0;
        internal CombatC0MonsterState(CombatC0MonsterPlacement placement, int maximum, int health)
        { AssignmentId = placement.AssignmentId; OptionId = placement.OptionId; FloorId = placement.FloorId;
          RoomId = placement.RoomId; MaxHealth = maximum; Health = health; }
    }
}
