using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0
{
    public partial class SaveService
    {
        public LivingDungeonForegroundConfig ForegroundConfiguration { get; private set; }
        internal Func<bool> BeforeForegroundAction;
        internal Func<SaveData> ForegroundCurrent;
        internal bool ForegroundWriting { get; private set; }
        private bool foregroundRecoveryRequired;
        private bool PrepareForegroundAction(ref SaveData current)
        {
            if (foregroundRecoveryRequired) return false;
            if (ForegroundWriting || BeforeForegroundAction == null) return true;
            if (!BeforeForegroundAction()) return false;
            current = ForegroundCurrent?.Invoke() ?? current;
            return current != null;
        }
        private DetachedCanonicalWriteResult ForegroundBlocked() => new DetachedCanonicalWriteResult(false,
            foregroundRecoveryRequired ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : "living.foreground.boundary_pending",
            false, false, null, null, null, null);

        internal DetachedCanonicalWriteResult AdvanceForeground(SaveData current, long through, int tickSeconds, Action<SaveData, long> passiveTick,
            long? suspendedSaveUtc = null)
        {
            if (foregroundRecoveryRequired || ForegroundWriting || _canonicalSession == null || ForegroundConfiguration == null)
                return ForegroundBlocked();
            ForegroundWriting = true;
            try {
                // Closing/reinspecting already accepted time while suspended must
                // retain the existing offline passive-mana boundary. Only newly
                // observed eligible time advances it; ordinary Save owns pause UTC.
                long savedBoundary = through > (current.livingDungeon?.Scheduler?.AcceptedThrough ?? -1)
                    && current.livingDungeon?.IsClosed != false ? CaptureMonotonicSaveBoundary(current.lastSavedUtcUnix) : current.lastSavedUtcUnix;
                // Draining an older accepted horizon may defer admission of the
                // final pre-pause sample. Preserve the pause UTC boundary even
                // when that sample is accepted by a later suspended pump.
                if (suspendedSaveUtc.HasValue)
                    savedBoundary = Math.Max(current.lastSavedUtcUnix, Math.Min(savedBoundary, suspendedSaveUtc.Value));
                var result = CreateWriteAuthority().AdvanceForeground(SavePath, _canonicalFileSystem, _canonicalSession, current,
                    ForegroundConfiguration, through, tickSeconds, passiveTick, savedBoundary);
                if (result.Reason == DetachedCanonicalWriteAuthority.RecoveryRequiredReason) foregroundRecoveryRequired = true;
                if (result.IsSuccess) {
                    _canonicalSession = result.Session; _undo = null;
                    CanonicalRuntimePublished?.Invoke(result.RuntimeProjection);
                }
                return result;
            } finally { ForegroundWriting = false; }
        }

        internal LivingDungeonForegroundInspection InspectForeground(SaveData current, int tickSeconds)
        {
            using var measurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Inspect);
            if (current?.livingDungeon == null || ForegroundConfiguration == null) return null;
            var owner = current.livingDungeon;
            var view = new LivingDungeonForegroundInspection { StateKey = "ui.living.awaiting", NextBoundary = 0 };
            if (owner.IsDormant) return view;
            if (owner.Scheduler.ProfileId != LivingDungeonForegroundConfig.ProfileId)
            { view.StateKey = "ui.living.unsupported_profile"; view.NextBoundary = long.MaxValue; return view; }
            long tickUnits = checked((long)tickSeconds * ForegroundConfiguration.UnitsPerSecond);
            view.NextBoundary = checked((Math.Max(0, owner.Scheduler.ClosedThrough) / tickUnits + 1) * tickUnits);
            if (owner.Scheduler.NextOpportunityTime.HasValue) view.NextBoundary = Math.Min(view.NextBoundary, owner.Scheduler.NextOpportunityTime.Value);
            if (!owner.IsClosed) view.NextBoundary = 0;
            if (!owner.Scheduler.NextOpportunityTime.HasValue) view.StateKey = "ui.living.not_eligible";
            var activeFloors = current.validatedCanonicalSpatialState.Floors.Where(f => f.ActivationState == FloorActivationState.Active).ToArray();
            if (activeFloors.Length > 1 || activeFloors.Length == 1 && activeFloors[0].FloorIndex != 0)
                view.StateKey = "ui.living.unsupported_layout";
            var profile = _validationContext.LivingDungeonProfiles.Single(p => p.Id == LivingDungeonForegroundConfig.ProfileId);
            foreach (var p in owner.Parties) {
                var e = LivingDungeonContracts.Restore(p, profile, retainPresentation: true);
                try {
                    if (e.IsTerminal) { view.StateKey = "ui.living.blocked"; continue; }
                    var inspection = e.Inspect();
                    view.RunId = inspection.RunId; view.Ordinal = e.NextOrdinal; view.Step = inspection.LastStep;
                    view.Members = inspection.Members; view.FloorId = e.Snapshot.Floors[0].FloorInstanceId;
                    view.AdmittedAt = p.AdmittedAt; view.MovementUnits = ForegroundConfiguration.Timing.UnitsPerTileEdge;
                    view.CarriedLootValue = inspection.CarriedLootValue;
                    view.Steps = Array.AsReadOnly(e.PresentationSteps.ToArray());
                    view.PositionStep = view.Steps.Where(s => s.PositionSpace != RunPositionSpace.None).Select(s => (RunStep?)s).LastOrDefault();
                    view.RecentInteraction = view.Steps.Where(s => s.Kind == RunStepKind.MonsterEngagement || s.Kind == RunStepKind.TrapTrigger ||
                        s.Kind == RunStepKind.LootInteraction || s.Kind == RunStepKind.UnreachableInteraction).Select(s => (RunStep?)s).LastOrDefault();
                    if (view.RecentInteraction.HasValue) {
                        var interaction = view.RecentInteraction.Value;
                        view.InteractionCombat = Array.AsReadOnly(e.Combat.Events.Where(c => c.RunOrdinal == interaction.Ordinal).ToArray());
                        view.InteractionOption = interaction.Encounter?.OptionId ??
                            e.Combat.Kernel.InspectMonsters().FirstOrDefault(m => m.AssignmentId == interaction.AssignmentId)?.OptionId;
                    }
                    view.StateKey = "ui.living.active";
                    view.NextBoundary = Math.Min(view.NextBoundary, checked(p.AdmittedAt + e.NextDueTime().Value));
                    view.AnnouncedAction = e.AnnouncedAction;
                    view.GeometryCompatible = FloorKnowledgeApplicability.TryCompute(current.validatedCanonicalSpatialState,
                        current.corridorContent, _limits.Canonical, view.FloorId, out string fingerprint) &&
                        fingerprint == e.Snapshot.Floors[0].KnowledgeFingerprint;
                } finally { e.Abandon(); }
            }
            return view;
        }
    }

    // Read-only presentation projection; no execution handle or gameplay mutator.
    public sealed class LivingDungeonForegroundInspection
    {
        public string StateKey { get; internal set; }
        public string RunId { get; internal set; }
        public string FloorId { get; internal set; }
        public long Ordinal { get; internal set; }
        public bool GeometryCompatible { get; internal set; }
        public RunStep? Step { get; internal set; }
        public RunStep? PositionStep { get; internal set; }
        public RunStep? RecentInteraction { get; internal set; }
        public RunStepKind? AnnouncedAction { get; internal set; }
        public string InteractionOption { get; internal set; }
        public int CarriedLootValue { get; internal set; }
        public long AdmittedAt { get; internal set; }
        public long MovementUnits { get; internal set; }
        public System.Collections.Generic.IReadOnlyList<RunStep> Steps { get; internal set; } = Array.Empty<RunStep>();
        public System.Collections.Generic.IReadOnlyList<CombatC0Event> InteractionCombat { get; internal set; } = Array.Empty<CombatC0Event>();
        public System.Collections.Generic.IReadOnlyList<RunMemberState> Members { get; internal set; }
        internal long NextBoundary;
    }
}
