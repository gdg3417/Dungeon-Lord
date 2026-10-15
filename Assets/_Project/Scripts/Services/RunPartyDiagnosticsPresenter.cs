using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0
{
    public static class RunPartyDiagnosticsPresenter
    {
        public static string Build(RunOutcomeRecord outcome, Func<string, string, string> localize)
        {
            if (outcome?.Party == null || localize == null) return string.Empty;
            string memberFormat = localize("ui.run.health.member_format", "ui.run.health.member_format");
            string eventFormat = localize("ui.run.health.event_format", "ui.run.health.event_format");
            var members = outcome.Party.Members.Select(m => string.Format(memberFormat, m.MemberOrdinal + 1,
                AdventurerPartyCompositionResolver.ResolveClassLabel(m.ClassId, localize), m.CurrentHealth, m.MaxHealth));
            // Stable content/assignment IDs are deliberate development evidence, never normal party labels.
            var events = (outcome.EncounterEvents ?? Array.Empty<Gameplay.RunSimulation.RunEncounterEvent>())
                .Select(e => string.Format(eventFormat, e.RoomIndex + 1, e.AssignmentId, e.OptionId,
                    e.MemberOrdinal + 1, e.HealthBefore, e.HealthAfter, e.Damage, e.Severity, e.TrapExpertise));
            var branches = (outcome.BranchOutcomes ?? Array.Empty<Gameplay.RunSimulation.BranchOutcomeEvidence>()).Select(b =>
            {
                if (b.Decision == null)
                {
                    string format = localize("ui.run.branch.no_decision_format", "ui.run.branch.no_decision_format");
                    return string.Format(format, b.Fork.OptionalBranchId,
                        localize(b.PrecedenceReason, b.PrecedenceReason), localize(b.Reason, b.Reason),
                        b.ReachedAssignments.Length, localize(b.KnowledgeOutcome, b.KnowledgeOutcome));
                }
                string branchFormat = localize("ui.run.branch.format", "ui.run.branch.format");
                return string.Format(branchFormat, b.Fork.OptionalBranchId,
                    localize(b.Decision.Reason, b.Decision.Reason), b.Decision.Condition,
                    b.Decision.ExpectedSurvivability, localize(b.Reason, b.Reason),
                    b.ReachedAssignments.Length, localize(b.KnowledgeOutcome, b.KnowledgeOutcome));
            });
            var transitions = (outcome.FloorTransitions ?? Array.Empty<Gameplay.RunSimulation.FloorTransitionEvidence>())
                .Select(t => string.Format(localize("ui.run.floor.transition_format", "ui.run.floor.transition_format"),
                    t.CurrentFloorInstanceId, t.NextFloorInstanceId, localize(t.Reason, t.Reason)));
            var combat = BuildCombat(outcome, localize);
            return string.Join("\n", members.Concat(events).Concat(branches).Concat(transitions).Concat(combat));
        }

        private static IEnumerable<string> BuildCombat(RunOutcomeRecord outcome, Func<string, string, string> localize)
        {
            var events = CombatC0Diagnostics.Events(outcome);
            if (events.Count == 0) yield break;
            string Text(string key) => localize(key, key);
            string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Text(key), args);
            // Never shorten by suffix: assignments on different floors may share one. Ordinal
            // maps are run-wide, collision-free and independent of source collection insertion.
            var members = outcome.Party.Members.ToDictionary(m => CombatC0Kernel.MemberId(outcome.Party, m.MemberOrdinal),
                m => Format("ui.run.combat.member_alias", m.MemberOrdinal + 1), StringComparer.Ordinal);
            var ids = events.SelectMany(e => new[] { e.ActorId, e.TargetId }).Where(id => id != null)
                .Distinct(StringComparer.Ordinal).Where(id => !members.ContainsKey(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < ids.Length; i++) members.Add(ids[i], Format("ui.run.combat.defender_alias", i + 1));
            var encounters = events.Select(e => e.EncounterId).Where(id => id != null).Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal).Select((id, i) => new { id, label = Format("ui.run.combat.encounter_alias", i + 1) })
                .ToDictionary(e => e.id, e => e.label, StringComparer.Ordinal);
            string Actor(string id) => id == null ? Text("ui.run.combat.none") : members[id];
            foreach (var entry in members.OrderBy(p => p.Key, StringComparer.Ordinal))
                yield return Format("ui.run.combat.identity_format", entry.Value, entry.Key);
            foreach (var entry in encounters.OrderBy(p => p.Key, StringComparer.Ordinal))
                yield return Format("ui.run.combat.identity_format", entry.Value, entry.Key);
            foreach (var e in events)
            {
                string kind = Text("ui.run.combat.kind." + e.Kind);
                string reason = Text("combat.c0.reason." + e.Reason);
                string action = e.Kind == CombatC0EventKind.Attack
                    ? Format("ui.run.combat.attack_format", e.HealthBefore, e.HealthAfter, e.Damage,
                        e.Defeated ? Text("ui.run.combat.defeated") : string.Empty)
                    : Format("ui.run.combat.reason_format", kind, reason);
                string encounter = e.EncounterId == null ? Text("ui.run.combat.none") : encounters[e.EncounterId];
                if (e.Kind == CombatC0EventKind.EncounterStart || e.Kind == CombatC0EventKind.Bypass)
                    yield return Format("ui.run.combat.location_format", encounter, e.FloorId, e.RoomId);
                yield return Format("ui.run.combat.event_format", encounter, e.Round,
                    e.Ordinal, Actor(e.ActorId), Actor(e.TargetId), action);
                if (e.SourcePosition.HasValue || e.TargetPosition.HasValue)
                    yield return Format("ui.run.combat.positions_format", Position(e.SourcePosition), Position(e.TargetPosition));
            }

            string Position(TileCoordinate? position) => position.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "({0},{1})", position.Value.X, position.Value.Y)
                : Text("ui.run.combat.none");
        }
    }
}
