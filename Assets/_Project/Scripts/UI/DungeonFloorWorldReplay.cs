using System.Collections.Generic;
using System.Linq;
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
        private readonly List<SpriteRenderer> replayDefeatPool = new List<SpriteRenderer>();
        private readonly List<CombatCasualtyBinding> combatCasualties = new List<CombatCasualtyBinding>();
        private readonly List<ReplayBinding> replayBindings = new List<ReplayBinding>();
        private DungeonRunReplay boundReplay;
        private readonly List<CombatBinding> combatBindings = new List<CombatBinding>();
        private LineRenderer replayAttack;
        private Material replayAttackMaterial;
        private readonly List<Rect> replayOccupiedRegions=new List<Rect>();
        private readonly Dictionary<string,Rect> replayLocationRegions=new Dictionary<string,Rect>();
        public IReadOnlyList<Rect> ReplayOccupiedRegions => replayOccupiedRegions;
        private void CacheReplayRegions(Dictionary<string,HashSet<TileCoordinate>> locations,HashSet<TileCoordinate> visible)
        {
            replayOccupiedRegions.Clear(); replayLocationRegions.Clear();
            foreach(var location in locations)
            {
                var tiles=location.Value;
                replayLocationRegions.Add(location.Key,Rect.MinMaxRect(tiles.Min(t=>t.X),tiles.Min(t=>t.Y),tiles.Max(t=>t.X)+1f,tiles.Max(t=>t.Y)+1f));
            }
            // Compress the already bounded rendered footprint once; no geometry/path work during playback.
            foreach(var row in visible.GroupBy(t=>t.Y).OrderBy(g=>g.Key))
            {
                int start=0,last=0; bool first=true;
                foreach(var tile in row.OrderBy(t=>t.X))
                {
                    if(first) { start=last=tile.X; first=false; }
                    else if((long)tile.X==(long)last+1) last=tile.X;
                    else { replayOccupiedRegions.Add(new Rect(start,row.Key,last-start+1,1)); start=last=tile.X; }
                }
                if(!first) replayOccupiedRegions.Add(new Rect(start,row.Key,last-start+1,1));
            }
        }
        public bool TryReplayLocationBounds(DungeonRunReplay replay,out Rect bounds)
        {
            bounds=default;
            if(replay==null || replay.Current.FloorId!=FloorInstanceId || !replay.Current.HasPosition) return false;
            var point=replay.DisplayPosition;
            if(replay.Current.Step.PositionSpace!=RunPositionSpace.Floor)
            {
                if(replay.Current.Step.RoomId!=null && replayLocationRegions.TryGetValue(replay.Current.Step.RoomId,out bounds) && bounds.Contains(point)) return true;
            }
            foreach(var area in replayLocationRegions.Values) if(area.Contains(point)) { bounds=area; return true; }
            bounds=new Rect(point-Vector2.one*.5f,Vector2.one); return true;
        }
        private sealed class CombatBinding
        { internal int Actor; internal SpriteRenderer View, Defeat; internal Vector3 Position; internal Color Color; internal int Order; }
        private sealed class CombatCasualtyBinding
        {
            internal SpriteRenderer View;
            internal readonly List<int> Frames = new List<int>();
            internal int Count;
        }
        public int ReplayCasualtySites => combatCasualties.Count;
        public int ReplayDefeatedMonsterCount { get; private set; }
        public bool TryReplayCasualtyBounds(int site,out Bounds bounds,out int count)
        {
            bounds=default; count=0;
            if(site<0 || site>=combatCasualties.Count) return false;
            var binding=combatCasualties[site]; count=binding.Count;
            if(count==0 || !binding.View.gameObject.activeSelf) return false;
            bounds=binding.View.bounds; return true;
        }
        public bool TryReplayDefeatBounds(int actor,out Bounds bounds)
        {
            bounds=default;
            foreach(var binding in combatBindings)
                if(binding.Actor==actor && binding.Defeat.gameObject.activeSelf) { bounds=binding.Defeat.bounds; return true; }
            return false;
        }
        public bool ReplayAttackVisible => replayAttack != null && replayAttack.gameObject.activeSelf;
        public int ReplayDefenderCount => combatBindings.Count;
        public bool TryReplayCombatantBounds(DungeonRunReplay replay, int actor, out Bounds bounds)
        {
            bounds=default;
            if (boundReplay!=replay || replay?.Combat==null || actor<0 || actor>=replay.Combat.Actors.Count) return false;
            if (!replay.Combat.Actors[actor].IsMonster)
            { if (replayParty==null || replay.Current.FloorId!=FloorInstanceId) return false; bounds=replayParty.bounds; return true; }
            foreach (var binding in combatBindings)
                if (binding.Actor==actor && binding.View.gameObject.activeSelf)
                { bounds=binding.View.bounds; return true; }
            return false;
        }
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
            foreach (var binding in combatBindings)
            { binding.View.transform.localPosition=binding.Position; binding.View.color=binding.Color; binding.View.sortingOrder=binding.Order; }
            combatBindings.Clear();
            combatCasualties.Clear(); ReplayDefeatedMonsterCount=0;
            foreach(var view in replayCasualtyPool) view.gameObject.SetActive(false);
            foreach(var view in replayDefeatPool) view.gameObject.SetActive(false);
            if (replayAttack != null) replayAttack.gameObject.SetActive(false);
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
            if (replay.Combat != null)
                for (int a=0;a<replay.Combat.Actors.Count;a++)
                {
                    var actor=replay.Combat.Actors[a];
                    if (!actor.IsMonster || actor.FloorId!=FloorInstanceId) continue;
                    for (int i=0;i<identities.Count;i++)
                        if (identities[i].AssignmentId==actor.AssignmentId)
                        {
                            var view=pool[i]; var defeat=ReplayView(replayDefeatPool,combatBindings.Count,"RecordedMonsterDefeat",6);
                            defeat.sprite=policy.Visuals!=null ? policy.Visuals.Invalid : anchorSprite;
                            defeat.color=policy.ReplayCasualtyColor; defeat.transform.localScale=Vector3.one*policy.ReplayDeathMarkerSize;
                            defeat.gameObject.SetActive(false);
                            combatBindings.Add(new CombatBinding { Actor=a, View=view, Defeat=defeat,
                                Position=view.transform.localPosition, Color=view.color, Order=view.sortingOrder }); break;
                        }
                }
            if(replay.Combat!=null) BindCombatCasualties(replay);
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
                if (replay.Combat==null && frame.Step.Encounter?.HealthBefore > 0 && frame.Step.Encounter.HealthAfter == 0)
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
        private void BindCombatCasualties(DungeonRunReplay replay)
        {
            var sites=new Dictionary<Vector2,CombatCasualtyBinding>(); var members=new HashSet<int>();
            for(int i=0;i<replay.FrameCount;i++)
            {
                var frame=replay.Frames[i];
                if(frame.FloorId!=FloorInstanceId || !frame.HasPosition) continue;
                int member=-1;
                if(frame.Combat?.Defeated==true && !replay.Combat.Actors[frame.TargetIndex].IsMonster) member=frame.TargetIndex;
                else if(frame.Combat==null && frame.Step.Encounter?.HealthBefore>0 && frame.Step.Encounter.HealthAfter==0)
                    for(int a=0;a<replay.Combat.Actors.Count;a++)
                        if(!replay.Combat.Actors[a].IsMonster && replay.Combat.Actors[a].Ordinal==frame.Step.Encounter.MemberOrdinal) { member=a; break; }
                if(member<0 || !members.Add(member)) continue;
                if(!sites.TryGetValue(frame.Position,out var site))
                {
                    var view=ReplayView(replayCasualtyPool,combatCasualties.Count,"RecordedAdventurerCasualty",6);
                    view.sprite=policy.Visuals!=null ? policy.Visuals.Invalid : anchorSprite;
                    view.color=policy.ReplayCasualtyColor; view.transform.localPosition=frame.Position;
                    view.transform.localScale=Vector3.one*policy.ReplayDeathMarkerSize; view.gameObject.SetActive(false);
                    site=new CombatCasualtyBinding { View=view }; sites.Add(frame.Position,site); combatCasualties.Add(site);
                }
                site.Frames.Add(i);
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
            PresentCombat(replay,onFloor);
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
        private void PresentCombat(DungeonRunReplay replay, bool onFloor)
        {
            if (replay.Combat==null) return;
            ReplayDefeatedMonsterCount=0;
            foreach (var binding in combatBindings)
            {
                var actor=replay.Combat.Actors[binding.Actor]; var state=actor.At(replay.Index);
                binding.View.transform.localPosition=state.Position;
                binding.View.color=state.Health==0 ? policy.ReplayDefeatedMonsterColor : binding.Color;
                binding.View.sortingOrder=state.Health==0 ? binding.Order : 3;
                binding.Defeat.gameObject.SetActive(state.Health==0);
                binding.Defeat.transform.localPosition=state.Position+policy.ReplayMonsterDefeatOffset;
                if(state.Health==0) ReplayDefeatedMonsterCount++;
            }
            foreach(var site in combatCasualties)
            {
                site.Count=0;
                foreach(int frame in site.Frames) { if(frame>replay.Index) break; site.Count++; }
                site.View.gameObject.SetActive(site.Count>0); ReplayCasualtyCount+=site.Count;
            }
            var e=replay.Current.Combat;
            bool attack=onFloor && e?.Kind==CombatC0EventKind.Attack && replay.DamageVisible;
            if (attack && replayAttack==null)
            {
                var child=new GameObject("RecordedAttack",typeof(LineRenderer)); child.layer=gameObject.layer;
                child.transform.SetParent(transform,false); replayAttack=child.GetComponent<LineRenderer>();
                replayAttack.useWorldSpace=false; replayAttack.positionCount=2; replayAttack.sortingOrder=7;
                replayAttackMaterial=new Material(Shader.Find("Sprites/Default")); replayAttack.sharedMaterial=replayAttackMaterial;
                replayAttack.startWidth=replayAttack.endWidth=policy.ReplayAttackWidth;
                replayAttack.startColor=replayAttack.endColor=policy.ReplayEngagingColor;
            }
            if (replayAttack==null) return;
            replayAttack.gameObject.SetActive(attack);
            if (!attack) return;
            replayAttack.SetPosition(0,replay.Combat.Actors[replay.Current.ActorIndex].IsMonster ?
                replay.Combat.Actors[replay.Current.ActorIndex].At(replay.Index).Position : replay.Current.Position);
            replayAttack.SetPosition(1,replay.Combat.Actors[replay.Current.TargetIndex].IsMonster ?
                replay.Combat.Actors[replay.Current.TargetIndex].At(replay.Index).Position : replay.Current.Position);
        }
    }
}
