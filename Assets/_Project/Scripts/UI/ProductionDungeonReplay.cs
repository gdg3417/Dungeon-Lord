using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Text;

namespace DungeonBuilder.M0
{
    public sealed partial class ProductionDungeonController
    {
        private DungeonRunReplay replay;
        private RunOutcomeRecord replayOutcome, replayAvailableOutcome;
        private string replayReason = DungeonRunReplay.Missing;
        private string replayAvailabilityFingerprint;
        private string replayAvailabilityCorridorFingerprint;
        private Label replayDamage;
        private int damageTextIndex = -1;
        private readonly List<Label> replayActorLabels = new List<Label>();
        private bool showReplayPositions;
        private int actorTextIndex = -1;
        public DungeonRunReplay Replay => replay;
        private void InitializeReplay()
        {
            Button("replayRun", () => StartReplay());
            Button("pauseReplay", ToggleReplayPause);
            Button("closeReplay", CloseReplay);
            Button("nextReplay", NextReplay);
            Button("replayPositions", () => { showReplayPositions = !showReplayPositions; PresentReplayControls(); Layout(); });
            replayDamage = document.rootVisualElement.Q<Label>("replayDamage");
            RefreshReplayAvailability();
        }
        private void RefreshReplayAvailability()
        {
            var available = root.Save.runHistory.LatestOutcome;
            var fingerprint = draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState);
            RunTransientEvidence.TryCorridorFingerprint(available, root.Save.corridorContent,
                root.SaveSpatialMigrationLimits.Canonical, out var corridorFingerprint);
            // An older replay can remain open when a newer manual run is published.
            // Validate its own traversed branches, not the newer run's potentially different scope.
            if (replay != null)
            {
                RunTransientEvidence.TryCorridorFingerprint(replayOutcome, root.Save.corridorContent,
                    root.SaveSpatialMigrationLimits.Canonical, out var playingCorridorFingerprint);
                if (replayOutcome.PresentationLayoutFingerprint != fingerprint ||
                    replayOutcome.PresentationCorridorFingerprint != playingCorridorFingerprint) CloseReplay();
            }
            if (available?.RunId == replayAvailableOutcome?.RunId &&
                ReferenceEquals(available?.PresentationSteps, replayAvailableOutcome?.PresentationSteps) &&
                fingerprint == replayAvailabilityFingerprint && corridorFingerprint == replayAvailabilityCorridorFingerprint)
                { replayAvailableOutcome = available; return; }
            replayAvailableOutcome = available; replayAvailabilityFingerprint = fingerprint;
            replayAvailabilityCorridorFingerprint = corridorFingerprint;
            // Validate/build once per publication or explicit start, never per frame.
            DungeonRunReplay.TryCreate(replayAvailableOutcome, root.Save.validatedCanonicalSpatialState,
                root.ProductionSpatialContent.Catalog, fingerprint, corridorFingerprint,
                presentationPolicy, out _, out replayReason);
        }
        public bool StartReplay()
        {
            if (!initialized || !HasCanonicalRuntime() || IsEditing || recoveryBytes != null || recoveryReason != null) return false;
            RunTransientEvidence.TryCorridorFingerprint(root.Save.runHistory.LatestOutcome, root.Save.corridorContent,
                root.SaveSpatialMigrationLimits.Canonical, out var corridorFingerprint);
            if (!DungeonRunReplay.TryCreate(root.Save.runHistory.LatestOutcome, root.Save.validatedCanonicalSpatialState,
                root.ProductionSpatialContent.Catalog, draftContext.Fingerprint(root.Save.validatedCanonicalSpatialState), corridorFingerprint,
                presentationPolicy, out var created, out replayReason)) { PresentReplayControls(); return false; }
            replay?.Close(); replay = created; replayOutcome = root.Save.runHistory.LatestOutcome;
            damageTextIndex = -1;
            actorTextIndex = -1;
            CloseSheet(); PresentReplayControls(); world.PresentReplay(replay, IsEditing); Layout(); return true;
        }
        public void ToggleReplayPause()
        { if (!HasCanonicalRuntime() || IsEditing) return; replay?.TogglePause(); PresentReplayControls(); }
        public void NextReplay()
        { if (!HasCanonicalRuntime() || IsEditing) return; replay?.Next(); PresentReplayControls(); world.PresentReplay(replay,false); }
        public void CloseReplay()
        { replay?.Close(); replay = null; replayOutcome = null; world?.HideReplay(); HideReplayFeedback(); if (initialized) { PresentReplayControls(); Layout(); } }
        private void UpdateReplay()
        {
            if (replay == null) { HideReplayFeedback(); return; }
            if (IsEditing || legacy) replay.Suspend();
            bool damageBefore = replay.DamageApplied;
            if (!IsEditing && !legacy && (replay.Advance(Time.unscaledDeltaTime) || damageBefore != replay.DamageApplied)) PresentReplayControls();
            world.PresentReplay(replay, IsEditing || legacy);
            PresentReplayFeedback();
        }
        private void HideReplayFeedback()
        { if (replayDamage != null) replayDamage.style.display = DisplayStyle.None;
          foreach (var label in replayActorLabels) label.style.display = DisplayStyle.None; }
        private void PresentReplayFeedback()
        {
            PresentCombatActorLabels();
            if (replay == null || replay.Combat != null || IsEditing || legacy || !replay.DamageVisible || replay.Current.FloorId != selectedFloor)
            { if (replayDamage != null) replayDamage.style.display = DisplayStyle.None; return; }
            var encounter = replay.Current.Step.Encounter;
            if (damageTextIndex != replay.Index)
            {
                damageTextIndex = replay.Index;
                replayDamage.text = Format(encounter.HealthAfter == 0 ? "ui.dungeon.replay.hit_casualty" :
                    encounter.Damage == 0 ? (encounter.UnmitigatedDamage > 0 ? "ui.dungeon.replay.hit_mitigated" : "ui.dungeon.replay.hit_zero") : "ui.dungeon.replay.hit",
                    encounter.MemberOrdinal + 1, encounter.Damage, encounter.HealthAfter);
                replayDamage.style.color = encounter.HealthAfter == 0 ? presentationPolicy.ReplayCasualtyColor :
                    encounter.Damage == 0 ? presentationPolicy.ReplayMitigatedColor : presentationPolicy.ReplayDamageColor;
            }
            var layer = replayDamage.parent;
            var screen = worldCamera.WorldToScreenPoint(replay.Current.Position);
            var panel = new Vector2(screen.x, Screen.height - screen.y) / document.panelSettings.scale;
            var local = layer.WorldToLocal(panel);
            bool visible = worldCamera.pixelRect.Contains(new Vector2(screen.x, screen.y));
            replayDamage.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            float width = Mathf.Min(presentationPolicy.ReplayFeedbackWidth, layer.contentRect.width);
            replayDamage.style.width = width;
            replayDamage.style.fontSize = Mathf.Max(presentationPolicy.ReplayFeedbackTextSize, presentationPolicy.Text(textSize));
            replayDamage.style.left = Mathf.Clamp(local.x - width * .5f, 0, Mathf.Max(0, layer.contentRect.width - width));
            float height = replayDamage.resolvedStyle.height;
            replayDamage.style.top = Mathf.Max(0, local.y - presentationPolicy.ReplayFeedbackOffset - (float.IsFinite(height) ? height : 0));
        }
        private void PresentReplayControls()
        {
            PresentReplayFeedback();
            var ui = document.rootVisualElement;
            ui.Q("replayDetails").style.maxHeight = presentationPolicy.ReplayDescriptionHeight;
            ui.Q("replayLegend").style.display = !IsEditing && replay != null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q("replayPanel").style.display = !IsEditing && replay != null ? DisplayStyle.Flex : DisplayStyle.None;
            var start = ui.Q<Button>("replayRun");
            start.style.display = !IsEditing && replay == null ? DisplayStyle.Flex : DisplayStyle.None;
            start.SetEnabled(replayReason == null && recoveryBytes == null && recoveryReason == null);
            start.tooltip = Text(replayReason ?? "ui.dungeon.replay.start");
            ui.Q<Label>("replayAvailability").text = Text(replayReason ?? "ui.dungeon.replay.ready");
            ui.Q("replayAvailability").style.display = !IsEditing && replay == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (replay == null) return;
            var frame = replay.Current;
            var pause = ui.Q<Button>("pauseReplay");
            pause.text = Text(replay.Paused ? "ui.dungeon.replay.resume" : "ui.dungeon.replay.pause");
            pause.SetEnabled(!replay.Complete);
            ui.Q<Button>("nextReplay").SetEnabled(!replay.Complete);
            ui.Q("nextReplay").style.display = replay.Combat != null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q("replayPositions").style.display = replay.Combat != null ? DisplayStyle.Flex : DisplayStyle.None;
            string description = replay.Complete ? Format("ui.dungeon.replay.completed", Text(replayOutcome.ReasonKey),
                replayOutcome.SurvivalSummary.SurvivorCount, replayOutcome.SurvivalSummary.DeathCount, replayOutcome.Score) :
                Text(EventKey(frame.Step.Kind));
            if (replay.Combat == null && !replay.Complete && frame.Step.Kind == RunStepKind.MonsterEngagement && !replay.DamageApplied)
                description = Text("ui.dungeon.replay.approach");
            if (!replay.Complete && frame.Option != null) description += " " + ContentName(frame.Option);
            if (frame.Step.Kind == RunStepKind.BranchDecision)
            {
                var branch = System.Linq.Enumerable.FirstOrDefault(replayOutcome.BranchOutcomes ?? System.Array.Empty<BranchOutcomeEvidence>(),
                    b => b.Fork.FloorIndex == frame.Step.FloorIndex && b.Fork.OptionalBranchId == frame.Step.AssignmentId);
                if (branch?.Decision != null) description += " " + Text(branch.Decision.Reason);
            }
            if (!replay.Complete && replay.DamageApplied && frame.Combat == null)
                description += " " + Format("ui.dungeon.replay.damage", frame.Step.Encounter.MemberOrdinal + 1,
                    frame.Step.Encounter.Damage, frame.Step.Encounter.HealthAfter, frame.Survivors);
            if (frame.Combat != null) description = CombatDescription(frame);
            ui.Q<Label>("replayEvent").text = Format("ui.dungeon.replay.event", replayOutcome.RunId,
                Text(replayOutcome.RunPostureId + ".name"),
                replay.Index + 1, replay.FrameCount, description);
            ui.Q<Label>("replayLegend").text = Text(replay.Combat != null ? "ui.dungeon.replay.combat_legend" : "ui.dungeon.replay.legend");
            ui.Q<Label>("replayEvent").tooltip = ui.Q<Label>("replayLegend").text;
            PresentCombatRoster();
            ui.Q<Label>("replayFloor").text = frame.FloorId != selectedFloor && frame.HasPosition ?
                Format("ui.dungeon.replay.other_floor", replay.CurrentFloorIndex + 1) : Format("ui.dungeon.replay.recorded", replay.DisplaySurvivors);
        }
        private static string EventKey(RunStepKind kind) => "ui.dungeon.replay." + kind.ToString().ToLowerInvariant();
        private string ActorName(int index)
        {
            if (index < 0) return Text("ui.run.combat.none");
            var actor = replay.Combat.Actors[index];
            return Format(actor.IsMonster ? "ui.dungeon.replay.defender" : "ui.dungeon.replay.member", actor.Ordinal + 1,
                actor.IsMonster ? ContentName(actor.ContentId) : AdventurerPartyCompositionResolver.ResolveClassLabel(actor.ContentId, root.Content.GetString));
        }
        private string CombatDescription(DungeonReplayFrame frame)
        {
            var e = frame.Combat;
            string detail = e.Kind == CombatC0EventKind.Attack ? Format("ui.dungeon.replay.combat_attack", ActorName(frame.ActorIndex),
                ActorName(frame.TargetIndex), e.Damage, e.HealthBefore, e.HealthAfter,
                e.Defeated ? Text("ui.run.combat.defeated") : string.Empty) :
                Format("ui.dungeon.replay.combat_reason", Text("ui.run.combat.kind." + e.Kind), ActorName(frame.ActorIndex), Text("combat.c0.reason." + e.Reason));
            if (e.EncounterId == null) return detail;
            if (e.Round == 0) return Format("ui.dungeon.replay.encounter_start",replay.Combat.EncounterNumber(e.EncounterId),detail);
            return Format("ui.dungeon.replay.combat_order", replay.Combat.EncounterNumber(e.EncounterId),e.Round, e.ActionOrdinal >= 0 ?
                Format("ui.dungeon.replay.action_order", e.ActionOrdinal + 1) : Text("ui.run.combat.none"), detail);
        }
        private void PresentCombatRoster()
        {
            var ui = document.rootVisualElement;
            ui.Q("replayActors").style.display = replay?.Combat != null ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Q("replayEligibility").style.display = replay?.Combat != null && showReplayPositions ? DisplayStyle.Flex : DisplayStyle.None;
            if (replay?.Combat == null) return;
            var text = new StringBuilder();
            for (int i=0;i<replay.Combat.Actors.Count;i++)
            {
                var a = replay.Combat.Actors[i];
                if (a.IsMonster && !replay.Complete && (a.FloorId != replay.Current.FloorId || a.RoomId != replay.Current.Step.RoomId)) continue;
                var state = a.At(replay.Index);
                text.AppendLine(Format("ui.dungeon.replay.actor_hp", ActorName(i), state.Health, a.MaxHealth,
                    state.Health == 0 ? Text("ui.run.combat.defeated") : string.Empty));
            }
            ui.Q<Label>("replayActors").text = text.ToString();
            text.Clear();
            foreach (var e in replay.Combat.Denials(replay.Current.Combat?.EncounterId))
                text.AppendLine(Format("ui.dungeon.replay.eligibility", ActorName(replay.Combat.ActorIndex(e.ActorId)),
                    ActorName(replay.Combat.ActorIndex(e.TargetId)), Text("combat.c0.reason." + e.Reason),
                    e.SourcePosition?.X, e.SourcePosition?.Y, e.TargetPosition?.X, e.TargetPosition?.Y));
            ui.Q<Label>("replayEligibility").text = text.Length > 0 ? text.ToString() : Text("ui.dungeon.replay.no_denials");
        }
        private void PresentCombatActorLabels()
        {
            if (replay?.Combat == null || IsEditing || legacy || replay.Current.FloorId != selectedFloor)
            { foreach (var label in replayActorLabels) label.style.display = DisplayStyle.None; return; }
            bool changed = actorTextIndex != replay.Index; actorTextIndex = replay.Index;
            for (int i=0;i<replay.Combat.Actors.Count;i++)
            {
                if (replayActorLabels.Count == i)
                { var label=new Label { pickingMode=PickingMode.Ignore }; label.AddToClassList("replay-actor");
                  document.rootVisualElement.Q("mapArea").Add(label); replayActorLabels.Add(label); changed=true; }
                var view=replayActorLabels[i]; var actor=replay.Combat.Actors[i]; var state=actor.At(replay.Index);
                bool visible=actor.IsMonster && actor.FloorId==selectedFloor && actor.RoomId==replay.Current.Step.RoomId;
                var screen=worldCamera.WorldToScreenPoint(state.Position);
                var local=view.parent.WorldToLocal(new Vector2(screen.x,Screen.height-screen.y)/document.panelSettings.scale);
                visible &= worldCamera.pixelRect.Contains(new Vector2(screen.x,screen.y));
                float width=Mathf.Min(presentationPolicy.ReplayActorLabelWidth,view.parent.contentRect.width);
                view.style.width=width;
                view.style.left=Mathf.Clamp(local.x-width*.5f,0,Mathf.Max(0,view.parent.contentRect.width-width));
                view.style.top=Mathf.Max(0,local.y-presentationPolicy.ReplayActorLabelOffset);
                view.style.fontSize=Mathf.Max(presentationPolicy.ReplayActorTextSize,presentationPolicy.Text(textSize));
                view.style.display=visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (changed) view.text=Format("ui.run.combat.defender_alias",actor.Ordinal+1);
                view.style.color=state.Health==0 ? presentationPolicy.ReplayCasualtyColor : presentationPolicy.ReplayPartyColor;
            }
            for (int i=replay.Combat.Actors.Count;i<replayActorLabels.Count;i++) replayActorLabels[i].style.display=DisplayStyle.None;
        }
    }
}
