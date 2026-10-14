using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private DungeonRunReplay replay;
        private RunOutcomeRecord replayOutcome, replayAvailableOutcome;
        private string replayReason = DungeonRunReplay.Missing;
        private string replayAvailabilityFingerprint;
        public DungeonRunReplay Replay => replay;
        private void InitializeReplay()
        {
            Button("replayRun", () => StartReplay());
            Button("pauseReplay", ToggleReplayPause);
            Button("closeReplay", CloseReplay);
            RefreshReplayAvailability();
        }
        private void RefreshReplayAvailability()
        {
            var available = root.Save.runHistory.LatestOutcome;
            var fingerprint = draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState);
            if (available?.RunId == replayAvailableOutcome?.RunId &&
                ReferenceEquals(available?.PresentationSteps, replayAvailableOutcome?.PresentationSteps) &&
                fingerprint == replayAvailabilityFingerprint) { replayAvailableOutcome = available; return; }
            replayAvailableOutcome = available; replayAvailabilityFingerprint = fingerprint;
            // Validate/build once per publication or explicit start, never per frame.
            DungeonRunReplay.TryCreate(replayAvailableOutcome, root.Save.validatedCanonicalSpatialState,
                root.ProductionSpatialContent.Catalog, fingerprint,
                presentationPolicy, out _, out replayReason);
            if (replay != null && replayOutcome.PresentationLayoutFingerprint !=
                fingerprint) CloseReplay();
        }
        public bool StartReplay()
        {
            if (!initialized || !HasCanonicalRuntime() || IsEditing || recoveryBytes != null || recoveryReason != null) return false;
            if (!DungeonRunReplay.TryCreate(root.Save.runHistory.LatestOutcome, root.Save.validatedCanonicalSpatialState,
                root.ProductionSpatialContent.Catalog, draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState),
                presentationPolicy, out var created, out replayReason)) { PresentReplayControls(); return false; }
            replay?.Close(); replay = created; replayOutcome = root.Save.runHistory.LatestOutcome;
            CloseSheet(); PresentReplayControls(); world.PresentReplay(replay, IsEditing); Layout(); return true;
        }
        public void ToggleReplayPause()
        { if (!HasCanonicalRuntime() || IsEditing) return; replay?.TogglePause(); PresentReplayControls(); }
        public void CloseReplay()
        { replay?.Close(); replay = null; replayOutcome = null; world?.HideReplay(); if (initialized) { PresentReplayControls(); Layout(); } }
        private void UpdateReplay()
        {
            if (replay == null) return;
            if (IsEditing || legacy) replay.Suspend();
            if (!IsEditing && !legacy && replay.Advance(Time.unscaledDeltaTime)) PresentReplayControls();
            world.PresentReplay(replay, IsEditing || legacy);
        }
        private void PresentReplayControls()
        {
            var ui = document.rootVisualElement;
            ui.Q("replayPanel").style.display = !IsEditing && replay != null ? DisplayStyle.Flex : DisplayStyle.None;
            var start = ui.Q<Button>("replayRun");
            start.style.display = !IsEditing && replay == null ? DisplayStyle.Flex : DisplayStyle.None;
            start.SetEnabled(replayReason == null && recoveryBytes == null && recoveryReason == null);
            start.tooltip = Text(replayReason ?? "ui.dungeon.replay.start");
            ui.Q<Label>("replayAvailability").text = Text(replayReason ?? "ui.dungeon.replay.ready");
            ui.Q("replayAvailability").style.display = !IsEditing && replay == null &&
                !string.IsNullOrEmpty(replayAvailableOutcome?.RunId) ? DisplayStyle.Flex : DisplayStyle.None;
            if (replay == null) return;
            var frame = replay.Current;
            var pause = ui.Q<Button>("pauseReplay");
            pause.text = Text(replay.Paused ? "ui.dungeon.replay.resume" : "ui.dungeon.replay.pause");
            pause.SetEnabled(!replay.Complete);
            string description = replay.Complete ? Format("ui.dungeon.replay.completed", Text(replayOutcome.ReasonKey),
                replayOutcome.SurvivalSummary.SurvivorCount, replayOutcome.SurvivalSummary.DeathCount, replayOutcome.Score) :
                Text(EventKey(frame.Step.Kind));
            if (!replay.Complete && frame.Step.Encounter != null)
                description += " " + Format("ui.dungeon.replay.damage", frame.Step.Encounter.MemberOrdinal + 1,
                    frame.Step.Encounter.Damage, frame.Step.Encounter.HealthAfter, frame.Survivors);
            ui.Q<Label>("replayEvent").text = Format("ui.dungeon.replay.event", replayOutcome.RunId,
                Text(replayOutcome.RunPostureId + ".name"),
                frame.Step.Ordinal + 1, replay.FrameCount, description);
            ui.Q<Label>("replayFloor").text = frame.FloorId != selectedFloor && frame.HasPosition ?
                Format("ui.dungeon.replay.other_floor", replay.CurrentFloorIndex + 1) : Format("ui.dungeon.replay.recorded", frame.Survivors);
        }
        private static string EventKey(RunStepKind kind) => "ui.dungeon.replay." + kind.ToString().ToLowerInvariant();
    }
}
