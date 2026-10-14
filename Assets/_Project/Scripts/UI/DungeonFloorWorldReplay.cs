using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0
{
    public sealed partial class DungeonFloorWorldView
    {
        private SpriteRenderer replayParty, replayMarker;
        private readonly List<SpriteRenderer> replayContentPool = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> replayBadgePool = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> replayCasualtyPool = new List<SpriteRenderer>();
        private readonly List<ReplayBinding> replayBindings = new List<ReplayBinding>();
        private DungeonRunReplay boundReplay;
        private sealed class ReplayBinding
        {
            internal int FrameIndex;
            internal SpriteRenderer Content, Badge, Casualty;
            internal Vector3 Position;
            internal Color Color;
            internal int Order;
            internal bool Canonical;
        }
        public bool ReplayPartyVisible => replayParty != null && replayParty.gameObject.activeSelf;
        public Vector2 ReplayPartyPosition => replayParty != null ? (Vector2)replayParty.transform.localPosition : default;
        public bool ReplayMarkerVisible => replayMarker != null && replayMarker.gameObject.activeSelf;
        public int ReplayCasualtyCount { get; private set; }
        public void HideReplay()
        {
            foreach (var binding in replayBindings)
            {
                if (!binding.Canonical) binding.Content.gameObject.SetActive(false);
                else { binding.Content.transform.localPosition = binding.Position; binding.Content.color = binding.Color; binding.Content.sortingOrder = binding.Order; }
                binding.Badge.gameObject.SetActive(false);
                if (binding.Casualty != null) binding.Casualty.gameObject.SetActive(false);
            }
            replayBindings.Clear(); boundReplay = null; ReplayCasualtyCount = 0;
            if (replayParty != null) replayParty.gameObject.SetActive(false);
            if (replayMarker != null) replayMarker.gameObject.SetActive(false);
        }
        private SpriteRenderer ReplayView(List<SpriteRenderer> views, int index, string name, int order)
        {
            if (index == views.Count) views.Add(Entity(name, order));
            views[index].sortingOrder = order;
            return views[index];
        }
        private void BindReplay(DungeonRunReplay replay)
        {
            HideReplay(); boundReplay = replay;
            int added = 0, casualties = 0;
            foreach (int index in replay.InteractionIndices)
            {
                var frame = replay.Frames[index];
                if (frame.FloorId != FloorInstanceId) continue;
                SpriteRenderer content = null;
                for (int i = 0; i < identities.Count; i++)
                    if (identities[i].AssignmentId == frame.Step.AssignmentId) { content = pool[i]; break; }
                bool canonical = content != null;
                if (!canonical)
                {
                    content = ReplayView(replayContentPool, added++, "RecordedCorridorContent", 2);
                    content.sprite = policy.Visuals != null ? policy.Visuals.Resolve(frame.Category, frame.Option) : sprite;
                    content.transform.localScale = Vector3.one * policy.EntitySize;
                    content.transform.localPosition = frame.MarkerPosition;
                    content.color = Color.white;
                }
                var binding = new ReplayBinding { FrameIndex = index, Content = content, Canonical = canonical,
                    Position = content.transform.localPosition, Color = content.color, Order = content.sortingOrder,
                    Badge = ReplayView(replayBadgePool, replayBindings.Count, "RecordedInteractionState", 4) };
                binding.Badge.sprite = anchorSprite;
                binding.Badge.transform.localScale = Vector3.one * policy.ReplayStateBadgeSize;
                if (frame.Step.Encounter?.HealthBefore > 0 && frame.Step.Encounter.HealthAfter == 0)
                {
                    binding.Casualty = ReplayView(replayCasualtyPool, casualties++, "RecordedAdventurerCasualty", 6);
                    binding.Casualty.sprite = anchorSprite;
                    binding.Casualty.color = policy.ReplayCasualtyColor;
                    binding.Casualty.transform.localPosition = frame.Position;
                    binding.Casualty.transform.localScale = Vector3.one * policy.ReplayCasualtySize;
                }
                replayBindings.Add(binding);
            }
            // Branch snapshots also prove configured content that was never reached (for example after a wipe).
            foreach (var content in replay.CorridorContents)
            {
                if (content.FloorId != FloorInstanceId) continue;
                bool bound = false;
                foreach (var binding in replayBindings)
                    if (binding.FrameIndex >= 0 && replay.Frames[binding.FrameIndex].Step.AssignmentId == content.AssignmentId) { bound = true; break; }
                if (bound) continue;
                var view = ReplayView(replayContentPool, added++, "RecordedCorridorContent", 2);
                view.sprite = policy.Visuals != null ? policy.Visuals.Resolve(content.Category, content.Option) : sprite;
                view.transform.localPosition = content.Position; view.transform.localScale = Vector3.one * policy.EntitySize; view.color = Color.white;
                var badge = ReplayView(replayBadgePool, replayBindings.Count, "RecordedInteractionState", 4);
                badge.gameObject.SetActive(false);
                replayBindings.Add(new ReplayBinding { FrameIndex = -1, Content = view, Badge = badge, Position = content.Position,
                    Color = Color.white, Order = 2 });
            }
        }
        public void PresentReplay(DungeonRunReplay replay, bool editing)
        {
            if (editing || replay == null || replay.Closed) { HideReplay(); return; }
            if (boundReplay != replay) BindReplay(replay);
            if (replayParty == null)
            {
                replayParty = Entity("RecordedParty", 5); replayParty.sprite = anchorSprite;
                replayParty.color = policy.ReplayPartyColor;
                replayParty.transform.localScale = Vector3.one * policy.ReplayPartySize;
                replayMarker = Entity("RecordedInteraction", 4); replayMarker.sprite = anchorSprite;
                replayMarker.transform.localScale = Vector3.one * policy.ReplayMarkerSize;
            }
            var current = replay.Current;
            bool onFloor = current.FloorId == FloorInstanceId && current.HasPosition;
            replayParty.gameObject.SetActive(onFloor && replay.DisplaySurvivors > 0);
            if (onFloor) replayParty.transform.localPosition = replay.DisplayPosition;
            replayMarker.gameObject.SetActive(onFloor && current.HasMarker);
            if (onFloor) replayMarker.transform.localPosition = current.MarkerPosition;
            replayMarker.color = policy.ReplayEngagingColor;
            ReplayCasualtyCount = 0;
            foreach (var binding in replayBindings)
            {
                if (binding.FrameIndex < 0) { binding.Content.gameObject.SetActive(true); continue; }
                var frame = replay.Frames[binding.FrameIndex];
                bool active = binding.FrameIndex == replay.Index;
                bool past = binding.FrameIndex < replay.Index;
                bool reached = frame.Step.Kind != RunStepKind.UnreachableInteraction;
                bool monster = frame.Step.Kind == RunStepKind.MonsterEngagement;
                binding.Content.gameObject.SetActive(true);
                binding.Content.sortingOrder = active ? 3 : binding.Order;
                binding.Content.transform.localPosition = monster && active ? replay.MonsterPosition :
                    monster && past ? frame.MarkerPosition : binding.Position;
                binding.Content.color = active ? policy.ReplayEngagingColor : !past ? binding.Color :
                    !reached ? policy.InvalidColor : monster ? policy.ReplayResolvedColor :
                    frame.Step.Kind == RunStepKind.TrapTrigger ? policy.ReplayTriggeredColor : policy.ReplayLootReachedColor;
                binding.Badge.gameObject.SetActive(past);
                binding.Badge.transform.localPosition = binding.Content.transform.localPosition;
                binding.Badge.sprite = !reached && policy.Visuals != null ? policy.Visuals.Invalid : anchorSprite;
                binding.Badge.color = binding.Content.color;
                if (binding.Casualty != null)
                {
                    bool visible = !replay.Complete && (past || active && replay.DamageApplied);
                    binding.Casualty.gameObject.SetActive(visible);
                    if (visible) ReplayCasualtyCount++;
                }
            }
        }
    }
}
