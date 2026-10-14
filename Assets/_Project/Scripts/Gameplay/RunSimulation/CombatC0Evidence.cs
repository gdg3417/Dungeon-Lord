using System;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    internal enum CombatC0EventKind { EncounterStart, Eligibility, Attack, Skipped, EncounterComplete }
    internal enum CombatC0Reason
    {
        Legal, MeleePositionUnavailable, OutOfRange, LineOfSightBlocked, Unreachable,
        DefeatedBeforeAction, NoLivingLegalTarget, DefendersDefeated, PartyWiped, NoParticipants,
        ZeroDamageRound, RoundLimit
    }

    // Immutable values only. No serializer, player text, playback timing or save owner.
    internal sealed class CombatC0Event
    {
        internal string RulesId => CombatC0QualificationConfig.Rules;
        internal string RunId { get; }
        internal string EncounterId { get; }
        internal int Ordinal { get; }
        internal int ActionOrdinal { get; }
        internal int Round { get; }
        internal string FloorId { get; }
        internal string RoomId { get; }
        internal CombatC0EventKind Kind { get; }
        internal CombatC0Reason Reason { get; }
        internal string ActorId { get; }
        internal string TargetId { get; }
        internal string AttackId { get; }
        internal string TargetingId => CombatC0QualificationConfig.Targeting;
        internal TileCoordinate? SourcePosition { get; }
        internal TileCoordinate? TargetPosition { get; }
        internal RunPositionSpace PositionSpace => SourcePosition.HasValue ? RunPositionSpace.RoomLocal : RunPositionSpace.None;
        internal int HealthBefore { get; }
        internal int HealthAfter { get; }
        internal int Damage { get; }
        internal bool Defeated => Kind == CombatC0EventKind.Attack && HealthBefore > 0 && HealthAfter == 0;
        internal CombatC0Event(string run, string encounter, int ordinal, int action, int round,
            string floor, string room, CombatC0EventKind kind, CombatC0Reason reason, string actor = null,
            string target = null, string attack = null, TileCoordinate? source = null, TileCoordinate? destination = null,
            int before = 0, int after = 0)
        {
            RunId = run; EncounterId = encounter; Ordinal = ordinal; ActionOrdinal = action; Round = round;
            FloorId = floor; RoomId = room; Kind = kind; Reason = reason; ActorId = actor; TargetId = target;
            AttackId = attack; SourcePosition = source; TargetPosition = destination;
            HealthBefore = before; HealthAfter = after; Damage = before - after;
        }
    }

    internal sealed class CombatC0EncounterResult
    {
        internal CombatC0Reason Completion { get; }
        internal IReadOnlyList<CombatC0Event> Events { get; }
        internal CombatC0EncounterResult(CombatC0Reason completion, CombatC0Event[] events)
        { Completion = completion; Events = Array.AsReadOnly((CombatC0Event[])events.Clone()); }
    }

    internal sealed class CombatC0MonsterPlacement
    {
        internal string AssignmentId { get; }
        internal string OptionId { get; }
        internal string FloorId { get; }
        internal string RoomId { get; }
        internal long Sequence { get; }
        internal CombatC0MonsterPlacement(string assignment, string option, string floor, string room, long sequence)
        { AssignmentId = assignment; OptionId = option; FloorId = floor; RoomId = room; Sequence = sequence; }
    }

    // Qualification supplies the directed legality matrix. The kernel does NOT infer geometry
    // from path reachability or assert that unreviewed coordinates/LOS are production rules.
    internal sealed class CombatC0AttackLink
    {
        internal string ActorId { get; }
        internal string TargetId { get; }
        internal TileCoordinate Source { get; }
        internal TileCoordinate Target { get; }
        internal CombatC0Reason Reason { get; }
        internal CombatC0AttackLink(string actor, string target, TileCoordinate source,
            TileCoordinate destination, CombatC0Reason reason)
        { ActorId = actor; TargetId = target; Source = source; Target = destination; Reason = reason; }
    }
}
