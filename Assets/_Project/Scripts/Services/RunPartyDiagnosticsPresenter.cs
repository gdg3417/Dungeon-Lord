using System;
using System.Linq;

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
            return string.Join("\n", members.Concat(events));
        }
    }
}
