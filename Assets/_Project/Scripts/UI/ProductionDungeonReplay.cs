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
        private readonly List<Label> replayActorHealth = new List<Label>();
        private readonly List<Rect> replayActorRects = new List<Rect>(), replayTagRects = new List<Rect>();
        private Rect replayFeedbackRect;
        private VisualElement replayPartyInfo;
        private readonly List<Label> replayPartyRows=new List<Label>();
        private readonly List<Label> replayPartyHealth=new List<Label>();
        private readonly List<Rect> replayRegionRects=new List<Rect>();
        private Rect replayPartyInfoRect;
        private int partyTextIndex=-1,feedbackTarget=-1;
        private int replayRegionRender=-1;
        private Vector3 replayRegionCamera;
        private float replayRegionSize,replayRegionScale;
        private Rect replayRegionViewport;
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
            replayPartyInfo = document.rootVisualElement.Q("replayPartyInfo");
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
            partyTextIndex = -1;
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
          if (replayPartyInfo!=null) replayPartyInfo.style.display=DisplayStyle.None;
          foreach (var label in replayActorLabels) label.parent.style.display = DisplayStyle.None; }
        private void PresentReplayFeedback()
        {
            if (replay?.Combat!=null)
            {
                CaptureReplayCombatantRects();
                replayTagRects.Clear(); replayFeedbackRect=default;
                PresentReplayPartyInfo();
                PresentCombatActorLabels();
                PresentCombatFeedback();
                return;
            }
            foreach (var label in replayActorLabels) label.parent.style.display=DisplayStyle.None;
            if(replayPartyInfo!=null) replayPartyInfo.style.display=DisplayStyle.None;
            replayDamage?.EnableInClassList("replay-combat-damage",false);
            if(replayDamage!=null) replayDamage.style.opacity=1;
            if (replay == null || IsEditing || legacy || !replay.DamageVisible || replay.Current.FloorId != selectedFloor)
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
            ui.Q("replayAvailability").style.display = !IsEditing && replay == null &&
                !string.IsNullOrEmpty(replayAvailableOutcome?.RunId) ? DisplayStyle.Flex : DisplayStyle.None;
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
            {
                var hit=frame.Step.Encounter;
                description += " " + (replay.Combat==null ? Format("ui.dungeon.replay.damage",hit.MemberOrdinal+1,hit.Damage,hit.HealthAfter,frame.Survivors) :
                    Format("ui.dungeon.replay.trap_damage",ActorName(replay.Combat.ActorIndex(replayOutcome.RunId+"/member/"+hit.MemberOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture))),hit.Damage,hit.HealthBefore,hit.HealthAfter,frame.Survivors));
            }
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
            return actor.IsMonster ? ContentName(actor.ContentId) : AdventurerPartyCompositionResolver.ResolveClassLabel(actor.ContentId, root.Content.GetString);
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
            if (replay?.Combat == null || IsEditing || legacy)
            { foreach (var label in replayActorLabels) label.parent.style.display = DisplayStyle.None; return; }
            bool changed = actorTextIndex != replay.Index; actorTextIndex = replay.Index;
            for (int i=0;i<replay.Combat.Actors.Count;i++)
            {
                if (replayActorLabels.Count == i)
                {
                    var group=new VisualElement { pickingMode=PickingMode.Ignore }; group.AddToClassList("replay-actor-group");
                    document.rootVisualElement.Q("mapArea").Add(group);
                    var label=new Label { pickingMode=PickingMode.Ignore,enableRichText=false,displayTooltipWhenElided=false }; label.AddToClassList("replay-actor");
                    var health=new Label { pickingMode=PickingMode.Ignore,enableRichText=false }; health.AddToClassList("replay-actor-health");
                    group.Add(label); group.Add(health); replayActorLabels.Add(label); replayActorHealth.Add(health); changed=true;
                }
                var view=replayActorLabels[i]; var actor=replay.Combat.Actors[i]; var state=actor.At(replay.Index);
                var hp=replayActorHealth[i]; var container=view.parent;
                bool visible=actor.IsMonster && actor.FloorId==selectedFloor;
                if (changed)
                {
                    view.text=ActorName(i); hp.text=Format("ui.dungeon.replay.monster_health",state.Health,actor.MaxHealth); view.userData=actor.Id;
                    view.tooltip=Format("ui.dungeon.replay.monster_hp",ActorName(i),state.Health,actor.MaxHealth);
                }
                float font=Mathf.Max(presentationPolicy.ReplayActorTextSize,presentationPolicy.Text(textSize)*presentationPolicy.ReplayActorTextScale);
                view.style.fontSize=hp.style.fontSize=font;
                var measured=view.MeasureTextSize(view.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
                var healthSize=hp.MeasureTextSize(hp.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
                var size=new Vector2(Mathf.Max(healthSize.x,Mathf.Min(presentationPolicy.ReplayActorLabelWidth,measured.x)),measured.y+healthSize.y);
                Rect placed=default;
                visible &= i<replayActorRects.Count && TryPlaceCombatOverlay(replayActorRects[i],size,true,out placed);
                container.style.display=visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (visible)
                { container.style.width=placed.width; container.style.left=placed.x; container.style.top=placed.y; replayTagRects.Add(placed); }
                view.style.color=hp.style.color=state.Health==0 ? presentationPolicy.ReplayCasualtyColor : presentationPolicy.ReplayPartyColor;
            }
            for (int i=replay.Combat.Actors.Count;i<replayActorLabels.Count;i++) replayActorLabels[i].parent.style.display=DisplayStyle.None;
        }
        private Vector2 ReplayPanelPoint(Vector3 point)
        {
            var screen=worldCamera.WorldToScreenPoint(point);
            return replayDamage.parent.WorldToLocal(new Vector2(screen.x,Screen.height-screen.y)/document.panelSettings.scale);
        }
        private void CaptureReplayCombatantRects()
        {
            replayActorRects.Clear();
            if(replayRegionRender!=world.ReconstructionCount || replayRegionCamera!=worldCamera.transform.position ||
                replayRegionSize!=worldCamera.orthographicSize || replayRegionViewport!=worldCamera.pixelRect || replayRegionScale!=document.panelSettings.scale)
            {
                replayRegionRects.Clear(); var regions=world.ReplayOccupiedRegions; var viewport=ReplayViewportRect();
                for(int i=0;i<regions.Count;i++) { var projected=ReplayPanelRect(regions[i]); if(projected.Overlaps(viewport)) replayRegionRects.Add(projected); }
                replayRegionRender=world.ReconstructionCount; replayRegionCamera=worldCamera.transform.position;
                replayRegionSize=worldCamera.orthographicSize; replayRegionViewport=worldCamera.pixelRect; replayRegionScale=document.panelSettings.scale;
            }
            for (int i=0;i<replay.Combat.Actors.Count;i++)
            {
                if (!world.TryReplayCombatantBounds(replay,i,out var bounds)) { replayActorRects.Add(default); continue; }
                var min=ReplayPanelPoint(bounds.min); var max=ReplayPanelPoint(bounds.max);
                replayActorRects.Add(Rect.MinMaxRect(Mathf.Min(min.x,max.x),Mathf.Min(min.y,max.y),Mathf.Max(min.x,max.x),Mathf.Max(min.y,max.y)));
            }
        }
        private void PresentCombatFeedback()
        {
            replayFeedbackRect=default;
            var e=replay.Current.Combat;
            var trap=replay.Current.Step.Encounter;
            if (IsEditing || legacy || replay.Current.FloorId!=selectedFloor || !replay.DamageVisible ||
                (e?.Kind!=CombatC0EventKind.Attack && trap==null))
            { replayDamage.style.display=DisplayStyle.None; return; }
            replayDamage.EnableInClassList("replay-combat-damage",true);
            if (damageTextIndex!=replay.Index)
            {
                damageTextIndex=replay.Index;
                int damage=e?.Damage ?? trap.Damage,after=e?.HealthAfter ?? trap.HealthAfter;
                feedbackTarget=e!=null ? replay.Current.TargetIndex : replay.Combat.ActorIndex(replayOutcome.RunId+"/member/"+trap.MemberOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                replayDamage.text=Format(after==0 ? "ui.dungeon.replay.damage_defeat" :
                    damage==0 ? "ui.dungeon.replay.damage_zero" : "ui.dungeon.replay.damage_number",damage);
                replayDamage.style.color=damage==0 ? presentationPolicy.ReplayMitigatedColor : presentationPolicy.ReplayCombatDamageColor;
            }
            replayDamage.style.fontSize=Mathf.Max(presentationPolicy.ReplayCombatFeedbackTextSize,
                presentationPolicy.Text(textSize)*presentationPolicy.ReplayCombatFeedbackTextScale);
            var measured=replayDamage.MeasureTextSize(replayDamage.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
            float width=Mathf.Min(presentationPolicy.ReplayCombatFeedbackWidth,measured.x);
            var size=new Vector2(width,measured.y+replayDamage.resolvedStyle.paddingTop+replayDamage.resolvedStyle.paddingBottom);
            bool visible=feedbackTarget>=0 && TryPlaceCombatOverlay(replayActorRects[feedbackTarget],size,false,out replayFeedbackRect);
            replayDamage.style.display=visible ? DisplayStyle.Flex : DisplayStyle.None;
            float progress=Mathf.Clamp01(replay.Elapsed/Mathf.Max(float.Epsilon,presentationPolicy.ReplayDamageFloatSeconds));
            replayDamage.style.opacity=Mathf.Lerp(1,presentationPolicy.ReplayDamageEndOpacity,progress);
            if (visible)
            {
                var floated=replayFeedbackRect; floated.y-=presentationPolicy.ReplayDamageRise*progress;
                if(ReplayViewportRect().Contains(floated.min) && ReplayViewportRect().Contains(floated.max) && !CombatOverlayBlocked(floated)) replayFeedbackRect=floated;
                replayDamage.style.width=width; replayDamage.style.left=replayFeedbackRect.x; replayDamage.style.top=replayFeedbackRect.y;
            }
        }
        private Rect ReplayPanelRect(Rect worldRect)
        {
            var min=ReplayPanelPoint(new Vector3(worldRect.xMin,worldRect.yMin));
            var max=ReplayPanelPoint(new Vector3(worldRect.xMax,worldRect.yMax));
            return Rect.MinMaxRect(Mathf.Min(min.x,max.x),Mathf.Min(min.y,max.y),Mathf.Max(min.x,max.x),Mathf.Max(min.y,max.y));
        }
        private Rect ReplayViewportRect()
        {
            var layer=replayDamage.parent; var pixels=worldCamera.pixelRect;
            var topLeft=layer.WorldToLocal(new Vector2(pixels.xMin,Screen.height-pixels.yMax)/document.panelSettings.scale);
            var bottomRight=layer.WorldToLocal(new Vector2(pixels.xMax,Screen.height-pixels.yMin)/document.panelSettings.scale);
            return Rect.MinMaxRect(topLeft.x,topLeft.y,bottomRight.x,bottomRight.y);
        }
        private bool CombatOverlayBlocked(Rect candidate)
        {
            var rail=document.rootVisualElement.Q("floorRail");
            if(rail.resolvedStyle.display!=DisplayStyle.None && candidate.Overlaps(replayDamage.parent.WorldToLocal(rail.worldBound))) return true;
            if(replayPartyInfoRect.width>0 && candidate.Overlaps(replayPartyInfoRect)) return true;
            foreach(var other in replayActorRects) if(other.width>0 && candidate.Overlaps(other)) return true;
            foreach(var other in replayTagRects) if(candidate.Overlaps(other)) return true;
            return false;
        }
        private void PresentReplayPartyInfo()
        {
            replayPartyInfoRect=default;
            if(IsEditing || legacy || replay.Current.FloorId!=selectedFloor || !replay.Current.HasPosition ||
                !world.TryReplayLocationBounds(replay,out var location)) { replayPartyInfo.style.display=DisplayStyle.None; return; }
            bool changed=partyTextIndex!=replay.Index; partyTextIndex=replay.Index;
            float font=Mathf.Max(presentationPolicy.ReplayPartyTextSize,presentationPolicy.Text(textSize)*presentationPolicy.ReplayPartyTextScale);
            float width=Mathf.Min(presentationPolicy.ReplayPartyInfoWidth,ReplayViewportRect().width),height=0;
            int row=0;
            for(int i=0;i<replay.Combat.Actors.Count;i++)
            {
                var actor=replay.Combat.Actors[i]; if(actor.IsMonster) continue;
                if(row==replayPartyRows.Count)
                {
                    var line=new VisualElement { pickingMode=PickingMode.Ignore }; line.AddToClassList("replay-party-line"); replayPartyInfo.Add(line);
                    var label=new Label { pickingMode=PickingMode.Ignore,enableRichText=false,displayTooltipWhenElided=false }; label.AddToClassList("replay-party-row"); line.Add(label); replayPartyRows.Add(label);
                    var health=new Label { pickingMode=PickingMode.Ignore,enableRichText=false }; health.AddToClassList("replay-party-health"); line.Add(health); replayPartyHealth.Add(health); changed=true;
                }
                var view=replayPartyRows[row++]; var state=actor.At(replay.Index);
                var hp=replayPartyHealth[row-1];
                if(changed)
                {
                    view.text=ProductionDungeonPresenter.ReplayPartyIdentity(Text,Culture,ActorName(i));
                    hp.text=Format("ui.dungeon.replay.party_health",state.Health,actor.MaxHealth);
                    view.tooltip=ProductionDungeonPresenter.ReplayPartyRow(Text,Culture,ActorName(i),state.Health,actor.MaxHealth); view.userData=actor.Id;
                }
                view.parent.style.display=DisplayStyle.Flex; view.style.fontSize=hp.style.fontSize=font;
                view.style.color=hp.style.color=state.Health==0 ? presentationPolicy.ReplayResolvedColor : replay.Current.ActorIndex==i ? presentationPolicy.ReplayEngagingColor : presentationPolicy.ReplayPartyColor;
                var measured=view.MeasureTextSize(view.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
                height+=Mathf.Max(measured.y,hp.MeasureTextSize(hp.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined).y);
            }
            for(int i=row;i<replayPartyRows.Count;i++) replayPartyRows[i].parent.style.display=DisplayStyle.None;
            var area=ReplayPanelRect(location); var anchor=ReplayPanelPoint(replay.DisplayPosition);
            var viewport=ReplayViewportRect(); float gap=presentationPolicy.ReplayActorLabelOffset;
            bool found=false;
            for(int side=0;side<4&&!found;side++)
                for(int shift=0;shift<3&&!found;shift++)
                {
                    float x=side==0 ? area.xMax+gap : side==1 ? area.xMin-gap-width : anchor.x-width*.5f;
                    float y=side<2 ? anchor.y-height*.5f : side==2 ? area.yMin-gap-height : area.yMax+gap;
                    if(side<2) y+=(shift==0?0:shift==1?-1:1)*height*.5f;
                    else x+=(shift==0?0:shift==1?-1:1)*width*.5f;
                    for(int pass=0;pass<presentationPolicy.ReplayPartyPlacementPasses&&!found;pass++)
                    {
                        var candidate=new Rect(Mathf.Clamp(x,viewport.xMin,Mathf.Max(viewport.xMin,viewport.xMax-width)),Mathf.Clamp(y,viewport.yMin,Mathf.Max(viewport.yMin,viewport.yMax-height)),width,height);
                        if(height>viewport.height) break;
                        float gapX=Mathf.Max(0,Mathf.Max(area.xMin-candidate.xMax,candidate.xMin-area.xMax));
                        float gapY=Mathf.Max(0,Mathf.Max(area.yMin-candidate.yMax,candidate.yMin-area.yMax));
                        if(gapX>presentationPolicy.ReplayPartyMaximumGap || gapY>presentationPolicy.ReplayPartyMaximumGap) break;
                        bool blocked=CombatOverlayBlocked(candidate); float nextX=x,nextY=y;
                        foreach(var region in replayRegionRects)
                            if(candidate.Overlaps(region))
                            {
                                blocked=true;
                                if(side==0) nextX=Mathf.Max(nextX,region.xMax+gap);
                                else if(side==1) nextX=Mathf.Min(nextX,region.xMin-gap-width);
                                else if(side==2) nextY=Mathf.Min(nextY,region.yMin-gap-height);
                                else nextY=Mathf.Max(nextY,region.yMax+gap);
                            }
                        if(!blocked) { replayPartyInfoRect=candidate; found=true; }
                        if(nextX==x&&nextY==y) break;
                        x=nextX;y=nextY;
                    }
                }
            replayPartyInfo.style.display=found?DisplayStyle.Flex:DisplayStyle.None;
            if(found) { replayPartyInfo.style.left=replayPartyInfoRect.x; replayPartyInfo.style.top=replayPartyInfoRect.y; replayPartyInfo.style.width=width; }
        }
        private bool TryPlaceCombatOverlay(Rect body, Vector2 size, bool tag, out Rect placed)
        {
            placed=default;
            if (body.width<=0 || body.height<=0 || !float.IsFinite(size.x) || !float.IsFinite(size.y)) return false;
            var viewport=ReplayViewportRect();
            if (!viewport.Overlaps(body) || size.x>viewport.width || size.y>viewport.height) return false;
            float gap=presentationPolicy.ReplayActorLabelOffset;
            for (int side=0;side<4;side++)
            {
                int edge=tag ? (side==1?3:side==3?1:side) : side;
                float x=edge<2 ? body.center.x-size.x*.5f : edge==2 ? body.xMax+gap : body.xMin-gap-size.x;
                float y=edge==0 ? body.yMin-gap-size.y : edge==1 ? body.yMax+gap : body.center.y-size.y*.5f;
                var candidate=new Rect(Mathf.Clamp(x,viewport.xMin,viewport.xMax-size.x),Mathf.Clamp(y,viewport.yMin,viewport.yMax-size.y),size.x,size.y);
                if (!CombatOverlayBlocked(candidate)) { placed=candidate; return true; }
            }
            return false;
        }
    }
}
