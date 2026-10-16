#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class CombatC1ReplayTests
    {
        private DungeonPresentationPolicy policy;
        [SetUp] public void SetUp() => policy=ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase("Aric")] [TestCase("Elara the Wise - Очень длинное имя <size=100> 名称")]
        public void PartyRowFormatterUsesOnlySuppliedIdentityAndLocalizedTemplates(string identity)
        {
            string Text(string key)=>key.EndsWith("party_named_row")?"{0} - {1} - {2}/{3}":"{0} - {1}/{2}";
            var value=ProductionDungeonPresenter.ReplayPartyRow(Text,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),"Warrior",7,8,identity);
            Assert.That(value,Is.EqualTo(string.IsNullOrWhiteSpace(identity)?"Warrior - 7/8":identity+" - Warrior - 7/8"));
        }
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(policy);
        internal static Fixture Completed(bool two=false, bool wipe=false)
        {
            var f=two ? PhaseSixA4Tests.Eligible() : PhaseFiveBBranchIntegrationTests.Fixture(false);
            f.Configuration.CombatC0=CombatC0IntegrationTests.Config().CombatC0;
            PhaseSixA4Tests.ForceDescent(f.Configuration,true);
            if (wipe) { foreach(var m in f.Configuration.CombatC0.Monsters) { m.Damage=100; m.Initiative=100; m.MaxHealth=100; } }
            PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId);
            if(!two) {
                PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId);
                PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.TrapCategoryId,MvpDungeonPlacementIds.SpikeTrapOptionId); }
            if(two) { PhaseSixA4Tests.AddContent(f,1,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId);
                f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,FloorLifecycleAction.Activate,PhaseSixA4Tests.FloorTwo(f))); }
            f.Accept(PhaseSixA4Tests.Writer(f).CommitPhaseFiveBRun(f.ActivePath,f.FileSystem,f.Session,f.Runtime,
                PhaseFiveBBranchIntegrationTests.Service(f.Configuration),RunPostureResolver.BalancedId,Math.Max(1,f.Runtime.lastSavedUtcUnix))); return f;
        }
        private DungeonRunReplay Create(Fixture f)
        {
            var r=f.Runtime.runHistory.LatestOutcome;
            if(r.PresentationLayoutFingerprint!="test.c1.synthetic") return VisualRunReplayTests.Create(f,policy);
            Assert.That(DungeonRunReplay.TryCreate(r,f.State,f.Production.Catalog,r.PresentationLayoutFingerprint,
                r.PresentationCorridorFingerprint,policy,out var replay,out var reason),Is.True,reason); return replay;
        }
        private static Fixture Synthetic(Action<RunSimulationConfig> configure, RunRoomAssignment[] assignments, bool isolate=false)
        {
            var f=PhaseFiveBBranchIntegrationTests.Fixture(false); var floor=f.State.Floors[0]; var saved=floor.Layout.Rooms[0];
            var config=CombatC0IntegrationTests.Config(); configure(config);
            var room=LivingDungeonPhaseABaselineTests.Room(0,true,assignments);
            room.RoomInstanceId=saved.RoomInstanceId; room.Spatial.RoomInstanceId=saved.RoomInstanceId;
            room.Spatial.FloorInstanceId=floor.FloorInstanceId; room.Spatial.RoomDefinitionId=saved.RoomDefinitionId;
            if(isolate) room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(new TileCoordinate(3,2))&&!t.Equals(new TileCoordinate(2,3))).ToArray();
            floor.RoomContents.Assignments=assignments.Select(a=>new RoomContentAssignment { AssignmentId=a.AssignmentId,RoomInstanceId=saved.RoomInstanceId,
                CategoryId=a.CategoryId,OptionId=a.OptionId,Sequence=a.Sequence,RoomLocalPosition=a.RoomLocalPosition }).ToArray();
            var outcome=new RunSimulationService(config,PhaseSixA4Tests.Loot()).StartCombatC0Route(f.Runtime.structureRuntime,42,1,
                RunPostureResolver.BalancedId,new[]{room},new[]{new CombatC0RoomBounds(floor.FloorInstanceId,saved.RoomInstanceId,4,4)}).DrainToCompletion(true);
            // Synthetic evidence uses explicitly synthetic identity, never canonical publication.
            outcome.PresentationLayoutFingerprint="test.c1.synthetic";
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(outcome,f.Runtime.corridorContent,f.Profile.Canonical,out var corridor),Is.True);
            outcome.PresentationCorridorFingerprint=corridor; f.Runtime.runHistory.LatestOutcome=outcome; return f;
        }
        private static RunRoomAssignment Monster(string id,int x,int y,long sequence=0) => LivingDungeonPhaseABaselineTests.Assignment(
            id,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId,x,y,sequence);
        private static void JoinChangedEvents(RunOutcomeRecord r)
        {
            r.PresentationSteps=r.PresentationSteps.Select(s=>new RunStep(s.Kind,s.FloorIndex,s.RoomId,s.AssignmentId,s.Position,s.Distance,
                s.Encounter,s.RunId,s.Ordinal,s.LogicalTime,s.PositionSpace,s.ContinueChosen,s.CarriedLootValueAfter,
                combatEvents:s.CombatEvents==null?null:r.CombatEvents.Where(e=>e.RunOrdinal==s.Ordinal).ToArray())).ToArray();
        }
        private void Reject(Fixture f, string expected=DungeonRunReplay.Invalid)
        {
            var r=f.Runtime.runHistory.LatestOutcome;
            Assert.That(DungeonRunReplay.TryCreate(r,f.State,f.Production.Catalog,r.PresentationLayoutFingerprint,
                r.PresentationCorridorFingerprint,policy,out var replay,out var reason),Is.False);
            Assert.That(replay,Is.Null); Assert.That(reason,Is.EqualTo(expected));
        }
        [TestCase(false)] [TestCase(true)]
        public void ChronologicalRouteRoundsStableActorsAndExactRecordedHealth(bool two)
        {
            var f=Completed(two); var r=f.Runtime.runHistory.LatestOutcome; var replay=Create(f);
            CollectionAssert.AreEqual(r.CombatEvents,replay.Combat.Events);
            CollectionAssert.AreEqual(r.PresentationSteps.Select(s=>s.Ordinal),replay.Frames.Select(s=>s.Step.Ordinal).Distinct());
            CollectionAssert.AreEqual(r.CombatEvents.Where(e=>e.Kind!=CombatC0EventKind.Eligibility),replay.Frames.Where(f=>f.Combat!=null).Select(f=>f.Combat));
            Assert.That(replay.Combat.Actors.Select(a=>a.Id),Is.Unique);
            var hp=replay.Combat.Actors.ToDictionary(a=>a.Id,a=>a.MaxHealth);
            for(int i=0;i<replay.FrameCount;i++)
            {
                var frame=replay.Frames[i]; var e=frame.Combat;
                if(e?.Kind==CombatC0EventKind.Attack)
                {
                    Assert.That(hp[e.ActorId],Is.GreaterThan(0)); Assert.That(e.HealthBefore,Is.EqualTo(hp[e.TargetId]));
                    Assert.That(replay.Combat.Actors[frame.TargetIndex].Id,Is.EqualTo(e.TargetId));
                    Assert.That(replay.Combat.Actors[frame.TargetIndex].At(i-1).Health,Is.EqualTo(e.HealthBefore));
                    hp[e.TargetId]=e.HealthAfter;
                }
                if(frame.Step.Encounter!=null) hp[r.RunId+"/member/"+frame.Step.Encounter.MemberOrdinal]=frame.Step.Encounter.HealthAfter;
                foreach(var a in replay.Combat.Actors) Assert.That(a.At(i).Health,Is.EqualTo(hp[a.Id]));
            }
            Assert.That(replay.Combat.Actors.Any(a=>a.IsMonster&&a.At(replay.FrameCount-1).Health==0),Is.True);
            foreach(var m in r.Party.Members) Assert.That(hp[r.RunId+"/member/"+m.MemberOrdinal],Is.EqualTo(m.CurrentHealth));
            if(two)
            {
                Assert.That(replay.Frames.Any(f=>f.Step.Kind==RunStepKind.FloorTransfer),Is.True);
                Assert.That(r.CombatEvents.Select(e=>e.FloorId).Distinct().Count(),Is.EqualTo(2));
            }
        }
        [Test] public void PartyWipeEndsCombatAndCannotInventLootOrMovement()
        {
            var f=Completed(false,true); var replay=Create(f);
            Assert.That(replay.Frames.Last().Survivors,Is.Zero);
            int death=Array.FindLastIndex(replay.Frames.ToArray(),f=>f.Combat?.Defeated==true);
            Assert.That(replay.Frames.Skip(death+1).All(f=>f.Combat==null || f.Combat.Kind==CombatC0EventKind.EncounterComplete),Is.True);
        }
        [TestCase("events")] [TestCase("party")] [TestCase("steps")] [TestCase("monsters")]
        public void MissingTransientEvidenceIsUnavailableAfterRestart(string missing)
        {
            var f=Completed(); var r=f.Runtime.runHistory.LatestOutcome;
            if(missing=="events") r.CombatEvents=null;
            if(missing=="party") r.Party=null;
            if(missing=="steps") r.PresentationSteps=null;
            if(missing=="monsters") r.CombatMonsters=null;
            Reject(f,DungeonRunReplay.Missing);
        }
        [TestCase("duplicate")] [TestCase("reordered")] [TestCase("missing")] [TestCase("null")]
        [TestCase("hp")] [TestCase("target")] [TestCase("boundary")] [TestCase("round")] [TestCase("action")]
        [TestCase("position")] [TestCase("final")]
        public void InconsistentEvidenceFailsClosedWithoutMutation(string change)
        {
            var f=Completed(); var r=f.Runtime.runHistory.LatestOutcome;
            var events=r.CombatEvents.ToArray(); int at=Array.FindIndex(events,e=>e.Kind==CombatC0EventKind.Attack); var e=events[at];
            if(change=="duplicate") events=events.Concat(new[]{events.Last()}).ToArray();
            if(change=="reordered") (events[at],events[at+1])=(events[at+1],events[at]);
            if(change=="missing") events=events.Skip(1).ToArray();
            if(change=="null") events[at]=null;
            if(new[]{"hp","target","boundary","round","action","position"}.Contains(change))
                events[at]=Copy(e,before:change=="hp"?e.HealthBefore+1:e.HealthBefore,target:change=="target"?"unknown":e.TargetId,
                    boundary:change=="boundary"?e.RunOrdinal+1:e.RunOrdinal,round:change=="round"?e.Round+2:e.Round,
                    action:change=="action"?e.ActionOrdinal+1:e.ActionOrdinal,source:change=="position"?new TileCoordinate(99,99):e.SourcePosition);
            if(change=="final") r.Party.Members.First(m=>m.CurrentHealth>0).ApplyDamage(1);
            r.CombatEvents=events;
            if(new[]{"hp","target","boundary","round","action","position"}.Contains(change)) JoinChangedEvents(r);
            var before=JsonUtility.ToJson(f.Runtime); var bytes=f.FileSystem.ReadAllBytes(f.ActivePath);
            Reject(f); Assert.That(JsonUtility.ToJson(f.Runtime),Is.EqualTo(before)); CollectionAssert.AreEqual(bytes,f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        internal static CombatC0Event Copy(CombatC0Event e,int? before=null,string target=null,long? boundary=null,int? round=null,int? action=null,TileCoordinate? source=null) =>
            new CombatC0Event(e.RunId,e.EncounterId,e.Ordinal,action??e.ActionOrdinal,round??e.Round,e.FloorId,e.RoomId,e.Kind,e.Reason,
                e.ActorId,target??e.TargetId,e.AttackId,source??e.SourcePosition,e.TargetPosition,before??e.HealthBefore,e.HealthAfter,e.ExecutableId,e.TargetingId,boundary??e.RunOrdinal);
        [TestCase("events")] [TestCase("route")] [TestCase("actors")] [TestCase("ledger")] [TestCase("markers")]
        public void GraphicalCapacityNeverSilentlyTruncates(string bound)
        {
            var f=Completed();
            if(bound=="events") policy.MaximumReplayCombatEvents=1;
            if(bound=="route") policy.MaximumReplaySteps=1;
            if(bound=="actors") policy.MaximumReplayCombatActors=1;
            if(bound=="ledger") policy.MaximumReplayCombatStateChanges=1;
            if(bound=="markers") policy.MaximumReplayMarkers=1;
            Reject(f,DungeonRunReplay.Capacity);
        }
        [Test] public void RetainedEvidenceReprojectsIdenticallyAndPlaybackHasNoAuthority()
        {
            var f=Completed(true); var r=f.Runtime.runHistory.LatestOutcome;
            string state=JsonUtility.ToJson(f.Runtime); var bytes=f.FileSystem.ReadAllBytes(f.ActivePath);
            var first=Create(f); var second=Create(f);
            CollectionAssert.AreEqual(first.Frames.Select(f=>(f.Step.Ordinal,f.Combat?.Ordinal,f.Position,f.Survivors)),second.Frames.Select(f=>(f.Step.Ordinal,f.Combat?.Ordinal,f.Position,f.Survivors)));
            first.TogglePause(); first.Advance(float.MaxValue); Assert.That(first.Index,Is.Zero);
            first.Next(); Assert.That(first.Paused,Is.True); first.TogglePause(); first.Advance(float.MaxValue); Assert.That(first.Complete,Is.True); first.Close();
            Assert.That(JsonUtility.ToJson(f.Runtime),Is.EqualTo(state)); CollectionAssert.AreEqual(bytes,f.FileSystem.ReadAllBytes(f.ActivePath));
            var clone=JsonUtility.FromJson<SaveData>(state); RunTransientEvidence.Retain(f.Runtime,clone);
            Assert.That(clone.runHistory.LatestOutcome.CombatEvents,Is.SameAs(r.CombatEvents));
            f.Reopen(); Assert.That(f.Runtime.runHistory.LatestOutcome.PresentationSteps,Is.Null);
        }
        [TestCase(CardinalOrientation.Zero)] [TestCase(CardinalOrientation.Ninety)] [TestCase(CardinalOrientation.OneEighty)] [TestCase(CardinalOrientation.TwoSeventy)]
        public void RoomTransformsApplyToBothAttackEndpointsWithoutIndependentPartyPaths(CardinalOrientation orientation)
        {
            var f=Completed(); var floor=f.State.Floors[0]; floor.Layout.Rooms[0].Orientation=orientation; floor.Layout.Rooms[0].Anchor=new TileCoordinate(13,17);
            var r=f.Runtime.runHistory.LatestOutcome;
            Assert.That(DungeonRunReplay.TryCreate(r,f.State,f.Production.Catalog,r.PresentationLayoutFingerprint,r.PresentationCorridorFingerprint,policy,out var replay,out var reason),Is.True,reason);
            for(int i=0;i<replay.FrameCount;i++)
            {
                var frame=replay.Frames[i]; var e=frame.Combat;
                if(e?.Kind!=CombatC0EventKind.Attack) continue;
                foreach(var endpoint in new[]{(frame.ActorIndex,e.SourcePosition),(frame.TargetIndex,e.TargetPosition)})
                {
                    Assert.That(DungeonRunReplay.TryProject(endpoint.Item2.Value,RunPositionSpace.RoomLocal,e.RoomId,floor,f.Production.Catalog,out var point),Is.True);
                    var actor=replay.Combat.Actors[endpoint.Item1]; Assert.That(actor.IsMonster?actor.At(i).Position:frame.Position,Is.EqualTo(point));
                }
            }
        }
        [Test] public void MultipleSameRoomEncountersRetainMonsterDefeatsAndRouteOrder()
        {
            var f=Synthetic(c=> { foreach(var p in c.CombatC0.Classes) { p.AttackType=CombatC0Config.Melee;p.Range=1;p.Damage=10; }
                foreach(var p in c.CombatC0.Monsters) { p.AttackType=CombatC0Config.Ranged;p.Range=1; } },new[]{Monster("a",1,0),Monster("b",3,2,1),
                LivingDungeonPhaseABaselineTests.Assignment("loot-first",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,3,1),
                LivingDungeonPhaseABaselineTests.Assignment("loot-return",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,0,1,1)});
            var replay=Create(f); Assert.That(replay.Combat.Events.Count(e=>e.Kind==CombatC0EventKind.EncounterStart),Is.EqualTo(2));
            foreach(var actor in replay.Combat.Actors.Where(a=>a.IsMonster))
            {
                int death=Array.FindIndex(replay.Frames.ToArray(),f=>f.Combat?.Defeated==true&&f.Combat.TargetId==actor.Id);
                Assert.That(death,Is.GreaterThanOrEqualTo(0)); Assert.That(Enumerable.Range(death,replay.FrameCount-death).All(i=>actor.At(i).Health==0),Is.True);
            }
        }
        [Test] public void TrapDamageJoinsPartyLedgerBetweenCombatAndTerminal()
        {
            var f=Synthetic(c=> { foreach(var p in c.CombatC0.Classes) p.Damage=100; foreach(var p in c.CombatC0.Monsters) p.Damage=0; },
                new[]{Monster("a",1,0),LivingDungeonPhaseABaselineTests.Assignment("trap",MvpDungeonPlacementIds.TrapCategoryId,MvpDungeonPlacementIds.SpikeTrapOptionId,1,1)});
            var replay=Create(f); int at=Array.FindIndex(replay.Frames.ToArray(),f=>f.Step.Kind==RunStepKind.TrapTrigger);
            Assert.That(at,Is.GreaterThan(0)); var trap=replay.Frames[at].Step.Encounter;
            var actor=replay.Combat.Actors.Single(a=>a.Id==f.Runtime.runHistory.LatestOutcome.RunId+"/member/"+trap.MemberOrdinal);
            Assert.That(actor.At(at-1).Health,Is.EqualTo(trap.HealthBefore)); Assert.That(actor.At(at).Health,Is.EqualTo(trap.HealthAfter));
        }
        [TestCase(CombatC0Reason.OutOfRange)] [TestCase(CombatC0Reason.LineOfSightBlocked)]
        [TestCase(CombatC0Reason.MeleePositionUnavailable)] [TestCase(CombatC0Reason.PartyAnchorConflict)]
        public void RecordedDenialIsPresentedWithoutRecheckingCurrentTuningOrGeometry(CombatC0Reason reason)
        {
            var f=Synthetic(c=>{ foreach(var p in c.CombatC0.Classes) { p.AttackType=CombatC0Config.Melee;p.Range=1; }
                foreach(var p in c.CombatC0.Monsters) { p.AttackType=CombatC0Config.Ranged;p.Range=4;p.Damage=100; } },new[]{Monster("a",3,3)});
            var r=f.Runtime.runHistory.LatestOutcome;
            int at=Array.FindIndex(r.CombatEvents,e=>e.Kind==CombatC0EventKind.Eligibility&&e.Reason!=CombatC0Reason.Legal);
            Assert.That(at,Is.GreaterThan(0)); var e=r.CombatEvents[at];
            r.CombatEvents[at]=new CombatC0Event(e.RunId,e.EncounterId,e.Ordinal,e.ActionOrdinal,e.Round,e.FloorId,e.RoomId,e.Kind,reason,
                e.ActorId,e.TargetId,e.AttackId,e.SourcePosition,e.TargetPosition,e.HealthBefore,e.HealthAfter,e.ExecutableId,e.TargetingId,e.RunOrdinal);
            JoinChangedEvents(r); var replay=Create(f);
            Assert.That(replay.Combat.Denials(e.EncounterId).Any(e=>e.Reason==reason),Is.True);
            Assert.That(replay.Frames.Any(f=>f.Combat?.Reason==CombatC0Reason.NoLivingLegalTarget),Is.True);
        }
        [Test] public void BypassHasItsOwnRecordedPositionAndNoInventedAttack()
        {
            var f=Synthetic(c=> { foreach(var p in c.CombatC0.Classes) { p.AttackType=CombatC0Config.Melee;p.Range=1; } },new[]{Monster("isolated",3,3)},true);
            var replay=Create(f); var bypass=replay.Frames.Single(f=>f.Combat?.Kind==CombatC0EventKind.Bypass);
            Assert.That(bypass.Combat.Reason,Is.EqualTo(CombatC0Reason.MeleePositionUnavailable));
            Assert.That(replay.Frames.Any(f=>f.Combat?.Kind==CombatC0EventKind.Attack),Is.False);
            Assert.That(replay.Combat.Actors[bypass.ActorIndex].At(replay.FrameCount-1).Health,Is.EqualTo(replay.Combat.Actors[bypass.ActorIndex].MaxHealth));
        }
        [Test] public void DefeatedActorSkippedBeforeItsActionRemainsDefeatedAcrossRounds()
        {
            var f=Synthetic(c=> { foreach(var p in c.CombatC0.Classes) { p.Initiative=p.Id.EndsWith("ranger")?40:20; p.Damage=p.Id.EndsWith("ranger")?100:0; }
                foreach(var p in c.CombatC0.Monsters) { p.MaxHealth=1;p.Initiative=30;p.Damage=0; } },new[]{Monster("a",2,2),Monster("b",3,3,1)});
            var replay=Create(f); var skipped=replay.Frames.Where(f=>f.Combat?.Reason==CombatC0Reason.DefeatedBeforeAction).ToArray();
            Assert.That(skipped,Is.Not.Empty);
            foreach(var frame in skipped)
            {
                int index=Array.IndexOf(replay.Frames.ToArray(),frame); var actor=replay.Combat.Actors[frame.ActorIndex];
                Assert.That(actor.At(index).Health,Is.Zero);
                Assert.That(replay.Frames.Skip(index).Any(f=>f.Combat?.Kind==CombatC0EventKind.Attack&&f.Combat.ActorId==actor.Id),Is.False);
            }
        }
        [TestCase(false)] [TestCase(true)] public void FullAndCorridorFingerprintsAreBothRequired(bool corridor)
        {
            var f=Completed(); var r=f.Runtime.runHistory.LatestOutcome;
            Assert.That(DungeonRunReplay.TryCreate(r,f.State,f.Production.Catalog,corridor?r.PresentationLayoutFingerprint:"changed",
                corridor?"changed":r.PresentationCorridorFingerprint,policy,out _,out var reason),Is.False);
            Assert.That(reason,Is.EqualTo(DungeonRunReplay.Layout));
        }
        [Test] public void NoRunAndRestartedHistoryHaveDifferentLocalizedReasons()
        {
            Assert.That(DungeonRunReplay.TryCreate(null,null,null,null,null,policy,out _,out var reason),Is.False);
            Assert.That(reason,Is.EqualTo(DungeonRunReplay.NoRun));
            var f=Completed(); f.Reopen(); Reject(f,DungeonRunReplay.Missing);
        }
        [TestCase(false)] [TestCase(true)]
        public void MalformedFloorGlobalMovementCannotEscapeCapturedFloorBounds(bool secondFloor)
        {
            var f=Completed(secondFloor); var r=f.Runtime.runHistory.LatestOutcome;
            Assert.That(Create(f),Is.Not.Null);
            int at=Array.FindIndex(r.PresentationSteps,s=>s.Kind==RunStepKind.Movement&&s.FloorIndex==(secondFloor?1:0));
            Assert.That(at,Is.GreaterThanOrEqualTo(0)); var s=r.PresentationSteps[at];
            r.PresentationSteps[at]=new RunStep(s.Kind,s.FloorIndex,s.RoomId,s.AssignmentId,new TileCoordinate(int.MaxValue,int.MaxValue),
                s.Distance,s.Encounter,s.RunId,s.Ordinal,s.LogicalTime,RunPositionSpace.Floor,s.ContinueChosen,s.CarriedLootValueAfter,combatEvents:s.CombatEvents);
            Reject(f);
        }
        [TestCase(RunStepKind.TrapTrigger)] [TestCase(RunStepKind.LootInteraction)]
        public void CorridorInteractionPositionMustMatchItsCapturedAssignment(RunStepKind kind)
        {
            var f=PhaseFiveBBranchIntegrationTests.Fixture(); f.Configuration.CombatC0=CombatC0IntegrationTests.Config().CombatC0;
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); var r=f.Runtime.runHistory.LatestOutcome;
            Assert.That(Create(f),Is.Not.Null);
            int at=Array.FindIndex(r.PresentationSteps,s=>s.Kind==kind&&s.PositionSpace==RunPositionSpace.Floor);
            Assert.That(at,Is.GreaterThan(0)); var s=r.PresentationSteps[at];
            r.PresentationSteps[at]=new RunStep(s.Kind,s.FloorIndex,s.RoomId,s.AssignmentId,new TileCoordinate(s.Position.X+1,s.Position.Y),
                s.Distance,s.Encounter,s.RunId,s.Ordinal,s.LogicalTime,s.PositionSpace,s.ContinueChosen,s.CarriedLootValueAfter,combatEvents:s.CombatEvents);
            Reject(f);
        }
    }
}
#endif
