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
        internal bool RequiresCanonicalRecovery => foregroundRecoveryRequired;
        private bool PrepareForegroundAction(ref SaveData current)
        {
            if (foregroundRecoveryRequired) return false;
            if (recoveryTime?.RecoveryClockActive == true && !ForegroundWriting) return false;
            if (current?.timeProvenance?.Episode != null && BeforeForegroundAction == null && !ForegroundWriting) return false;
            if (ForegroundWriting || BeforeForegroundAction == null) return true;
            if (!BeforeForegroundAction()) return false;
            current = ForegroundCurrent?.Invoke() ?? current;
            return current != null;
        }
        private DetachedCanonicalWriteResult ForegroundBlocked() => new DetachedCanonicalWriteResult(false,
            foregroundRecoveryRequired ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : "living.foreground.boundary_pending",
            false, false, null, null, null, null);

#if UNITY_EDITOR
        // Historical deterministic service tests supply explicit clock cuts. Live
        // coordination must use TimeService.CaptureForegroundObservation instead.
        internal DetachedCanonicalWriteResult AdvanceForeground(SaveData current, long through, int tickSeconds,
            Action<SaveData, long> passiveTick, long? suspendedSaveUtc = null) => AdvanceForeground(current,
                ForegroundObservation.ForDeterministicServiceTest(_canonicalSession.GetCurrentBytes(), through,
                    current.livingDungeon?.Scheduler?.AcceptedThrough ?? -1, current.livingDungeon?.Scheduler?.ClosedThrough ?? -1,
                    new UnixTimePoint(CaptureMonotonicSaveBoundary(current.lastSavedUtcUnix), 0),
                    suspendedSaveUtc.HasValue ? ForegroundObservationContext.Suspended : ForegroundObservationContext.EligibleForeground),
                tickSeconds, passiveTick, suspendedSaveUtc);
#endif

        internal DetachedCanonicalWriteResult AdvanceForeground(SaveData current, ForegroundObservation observation, int tickSeconds, Action<SaveData, long> passiveTick,
            long? suspendedSaveUtc = null)
        {
            if (foregroundRecoveryRequired || ForegroundWriting || _canonicalSession == null || ForegroundConfiguration == null)
                return ForegroundBlocked();
            if (observation == null || !observation.Authorizes(_canonicalSession, current, observation.CapturedThrough)) return ForegroundBlocked();
            long through = observation.CapturedThrough;
            ForegroundWriting = true;
            try {
                // Closing/reinspecting already accepted time while suspended must
                // retain the existing offline passive-mana boundary. Only newly
                // observed eligible time advances it; ordinary Save owns pause UTC.
                long savedBoundary = through > (current.livingDungeon?.Scheduler?.AcceptedThrough ?? -1)
                    && current.livingDungeon?.IsClosed != false ? Math.Max(current.lastSavedUtcUnix, observation.WallEndpoint.Seconds) : current.lastSavedUtcUnix;
                // Draining an older accepted horizon may defer admission of the
                // final pre-pause sample. Preserve the pause UTC boundary even
                // when that sample is accepted by a later suspended pump.
                if (suspendedSaveUtc.HasValue)
                    savedBoundary = Math.Max(current.lastSavedUtcUnix, Math.Min(savedBoundary, suspendedSaveUtc.Value));
                CanonicalTimeTransition time = null;
                if (current.timeProvenance?.Mode == TimeProvenance.PreciseMode) {
                    if (current.timeProvenance.Episode != null || recoveryTime?.RecoveryClockActive == true) return ForegroundBlocked();
                    savedBoundary = current.lastSavedUtcUnix;
                    if (current.livingDungeon?.IsClosed != false && observation.Context == ForegroundObservationContext.EligibleForeground) {
                        time = CanonicalTimeTransition.Foreground(_canonicalSession.GetCurrentBytes(), current.timeProvenance, observation, current.livingDungeon);
                        savedBoundary = time.Successor.AccountedBoundary.Seconds;
                    }
                }
                var result = CreateWriteAuthority().AdvanceForeground(SavePath, _canonicalFileSystem, _canonicalSession, current,
                    ForegroundConfiguration, through, tickSeconds, passiveTick, savedBoundary, time);
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
            using var diagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.PostPublicationInspection,
                ForegroundQualificationMetrics.Origin.Inspection);
            using var measurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Inspect);
            if (current?.livingDungeon == null || ForegroundConfiguration == null) return null;
            var owner = current.livingDungeon;
            var view = new LivingDungeonForegroundInspection { StateKey = "ui.living.awaiting", NextBoundary = 0 };
            if (owner.IsDormant) return view;
            if (!LivingDungeonForegroundConfig.IsProduction(owner.Scheduler.ProfileId))
            { view.StateKey = "ui.living.unsupported_profile"; view.NextBoundary = long.MaxValue; return view; }
            long tickUnits = checked((long)tickSeconds * ForegroundConfiguration.UnitsPerSecond);
            view.NextBoundary = checked((Math.Max(0, owner.Scheduler.ClosedThrough) / tickUnits + 1) * tickUnits);
            if (owner.Scheduler.NextOpportunityTime.HasValue) view.NextBoundary = Math.Min(view.NextBoundary, owner.Scheduler.NextOpportunityTime.Value);
            if (!owner.IsClosed) view.NextBoundary = 0;
            if (!owner.Scheduler.NextOpportunityTime.HasValue) view.StateKey = "ui.living.not_eligible";
            var activeFloors = current.validatedCanonicalSpatialState.Floors.Where(f => f.ActivationState == FloorActivationState.Active).ToArray();
            if (activeFloors.Length > (ForegroundConfiguration.TwoFloor ? 2 : 1) || activeFloors.Length == 1 && activeFloors[0].FloorIndex != 0)
                view.StateKey = "ui.living.unsupported_layout";
            var profile = _validationContext.LivingDungeonProfiles.Single(p => p.Id == owner.Scheduler.ProfileId);
            var parties = new System.Collections.Generic.List<LivingDungeonForegroundInspection>();
            string vacantStateKey = view.StateKey;
            long nextBoundary = view.NextBoundary;
            foreach (var p in owner.Parties) {
                var e = LivingDungeonContracts.Restore(p, profile, retainPresentation: true);
                try {
                    if (e.IsTerminal) { view.StateKey = "ui.living.blocked"; continue; }
                    view = new LivingDungeonForegroundInspection();
                    var inspection = e.Inspect();
                    view.RunId = inspection.RunId; view.Ordinal = e.NextOrdinal; view.Step = inspection.LastStep;
                    view.Members = inspection.Members; view.FloorId = e.TransferContract ? e.OccupiedFloor : e.Snapshot.Floors[0].FloorInstanceId;
                    view.AdmittedAt = p.AdmittedAt; view.MovementUnits = profile.QualificationTiming.UnitsPerTileEdge;
                    view.CarriedLootValue = inspection.CarriedLootValue;
                    view.Steps = Array.AsReadOnly(e.PresentationSteps.ToArray());
                    int floorIndex = e.Snapshot.Floors.Single(f => f.FloorInstanceId == view.FloorId).FloorIndex;
                    view.PositionStep = view.Steps.Where(s => s.FloorIndex == floorIndex && s.PositionSpace != RunPositionSpace.None).Select(s => (RunStep?)s).LastOrDefault();
                    view.RecentInteraction = view.Steps.Where(s => s.Kind == RunStepKind.MonsterEngagement || s.Kind == RunStepKind.TrapTrigger ||
                        s.Kind == RunStepKind.LootInteraction || s.Kind == RunStepKind.UnreachableInteraction).Select(s => (RunStep?)s).LastOrDefault();
                    if (view.RecentInteraction.HasValue) {
                        var interaction = view.RecentInteraction.Value;
                        view.InteractionCombat = Array.AsReadOnly(e.Combat.Events.Where(c => c.RunOrdinal == interaction.Ordinal).ToArray());
                        view.InteractionOption = interaction.Encounter?.OptionId ??
                            e.Combat.Kernel.InspectMonsters().FirstOrDefault(m => m.AssignmentId == interaction.AssignmentId)?.OptionId;
                    }
                    view.Waiting = e.TransferWaiting;
                    view.Descended = view.Steps.Any(s => s.Kind == RunStepKind.FloorTransfer && s.FloorIndex == floorIndex);
                    view.Decision = view.Steps.Where(s => s.Kind == RunStepKind.FloorDecision && s.FloorIndex == floorIndex)
                        .Select(s => s.ContinueChosen).LastOrDefault();
                    view.StateKey = view.Waiting ? "ui.living.waiting" : "ui.living.active";
                    var due = e.NextDueTime();
                    if (due.HasValue) nextBoundary = Math.Min(nextBoundary, checked(p.AdmittedAt + due.Value));
                    view.AnnouncedAction = e.AnnouncedAction;
                    view.GeometryCompatible = FloorKnowledgeApplicability.TryCompute(current.validatedCanonicalSpatialState,
                        current.corridorContent, _limits.Canonical, view.FloorId, out string fingerprint) &&
                        fingerprint == e.Snapshot.Floors.Single(f => f.FloorInstanceId == view.FloorId).KnowledgeFingerprint;
                    parties.Add(view);
                } finally { e.Abandon(); }
            }
            view.NextBoundary = nextBoundary; view.Parties = parties.AsReadOnly();
            view.SetVacantReason(parties.Count == 0 ? view.StateKey : vacantStateKey); return view;
        }
    }

    // Read-only presentation projection; no execution handle or gameplay mutator.
    public sealed class LivingDungeonForegroundInspection
    {
        public bool Waiting { get; internal set; }
        public bool Descended { get; internal set; }
        public bool? Decision { get; internal set; }
        private LivingDungeonForegroundInspection vacant;
        private string vacantStateKey;
        internal void SetVacantReason(string key)
        {
            vacantStateKey = key;
            if (Parties.Count == 0) StateKey = key;
            if (vacant != null) vacant.StateKey = key;
        }
        public System.Collections.Generic.IReadOnlyList<LivingDungeonForegroundInspection> Parties { get; internal set; } = Array.Empty<LivingDungeonForegroundInspection>();
        public LivingDungeonForegroundInspection ForFloor(string floor)
        {
            if (Parties.Count == 0) return this;
            // Called by presentation every frame; selection needs no LINQ closure.
            for (int i = 0; i < Parties.Count; i++) if (Parties[i].FloorId == floor) return Parties[i];
            return vacant ??= new LivingDungeonForegroundInspection { StateKey = vacantStateKey ?? StateKey };
        }
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
