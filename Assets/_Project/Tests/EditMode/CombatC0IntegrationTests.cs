#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class CombatC0IntegrationTests
    {
        private static string Json => File.ReadAllText("Assets/_Project/Data/Bootstrap/run_simulation_config.json");
        internal static RunSimulationConfig Config()
        {
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(Json, out var c), Is.True);
            PhaseSixA4Tests.ForceDescent(c, true); return c;
        }
        private static RunRoomAssignment Monster(string id = "g", int x = 2, int y = 2, long order = 0) =>
            B.Assignment(id, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, x, y, order);
        private static RunExecution Start(RunSimulationConfig c, params MvpOrderedRouteRoom[] rooms) =>
            new RunSimulationService(c, PhaseSixA4Tests.Loot()).StartCombatC0Route(new StructureRuntimeState { ManaReserve = 100 },
                42, 1, RunPostureResolver.BalancedId, rooms, rooms.Select(r => new CombatC0RoomBounds(
                    r.Spatial.FloorInstanceId, r.RoomInstanceId, 4, 4)).ToArray());
        private static RunParty Party(params string[] classes)
        {
            var c = Config().PhaseFiveB;
            var members = classes.Select((id,i) => new RunPartyMember(i,c.Classes.Single(p=>p.ClassId=="adventurer.class."+id),c.Behaviors[0],1)).ToArray();
            return new RunParty("test.c0",c.PartyRuleSourceId,0,members,classes.Select(_=>c.Behaviors[0]).ToArray(),c.IntelligenceBands[0]);
        }
        private static CombatC0Kernel Kernel(RunSimulationConfig c, RunParty party, params RunRoomAssignment[] monsters) =>
            new CombatC0Kernel(party,c.CombatC0.Capture(),monsters.Select(a=>new CombatC0MonsterPlacement(a.AssignmentId,a.OptionId,
                "phase.a.floor","phase.a.room.0",a.Sequence)).ToArray());

        [Test] public void ApprovedProductionProfilesAndPhaseFiveBHealthHaveOneAuthority()
        {
            var c=Config(); var rule=c.CombatC0.Capture(); Assert.That(rule.RulesId,Is.EqualTo(CombatC0Config.Executable));
            Assert.That(rule.MaximumRounds,Is.EqualTo(32)); Assert.That(rule.MaximumParticipants,Is.EqualTo(64));
            Assert.That(rule.MaximumEncounters,Is.EqualTo(30)); Assert.That(rule.MaximumEvents,Is.EqualTo(32768)); Assert.That(rule.MaximumWork,Is.EqualTo(1000000));
            string[] names={"warrior","rogue","ranger","cleric","mage"}; int[] hp={10,7,7,8,6},damage={2,3,2,1,3},initiative={30,50,40,10,20},range={1,1,4,1,4};
            for(int i=0;i<names.Length;i++) { var p=rule.Classes.Single(x=>x.Id=="adventurer.class."+names[i]);
                Assert.That(c.PhaseFiveB.Classes.Single(x=>x.ClassId==p.Id).LevelOneMaxHealth,Is.EqualTo(hp[i]));
                Assert.That(p.MonsterHealth,Is.Zero); Assert.That(p.Damage,Is.EqualTo(damage[i])); Assert.That(p.Initiative,Is.EqualTo(initiative[i])); Assert.That(p.Range,Is.EqualTo(range[i])); }
        }
        [TestCase("absent")] [TestCase("disabled")] [TestCase("enabled")]
        public void ExplicitBootstrapSelectionRetainsAbsentAndDisabledB1(string mode)
        {
            string json=mode=="absent" ? JsonUtility.ToJson(PhaseFiveBTestConfig.Production()) : mode=="disabled" ? Json.Replace("\"Enabled\": true","\"Enabled\": false") : Json;
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(json,out var c),Is.True);
            Assert.That(c.CombatC0?.Capture()!=null,Is.EqualTo(mode=="enabled"));
        }
        [TestCase("missing")] [TestCase("duplicate")] [TestCase("negative")] [TestCase("unknown")] [TestCase("null")] [TestCase("bad_enabled")]
        public void MalformedEnabledConfigurationCannotCreateGameplayService(string mode)
        {
            string json=Json;
            if(mode=="missing") json=json.Replace("\"Range\": 4","\"RemovedRange\": 4");
            if(mode=="duplicate") json=json.Replace("\"MaximumRounds\": 32","\"MaximumRounds\": 32, \"MaximumRounds\": 32");
            if(mode=="negative") json=json.Replace("\"MaximumWork\": 1000000","\"MaximumWork\": -1");
            if(mode=="unknown") json=json.Replace("run.combat.c0.basic.v1","run.combat.c0.unknown");
            if(mode=="null") json=json.Replace("\"Range\": 4","\"Range\": null");
            if(mode=="bad_enabled") json=json.Replace("\"Enabled\": true","\"Enabled\": 1");
            Assert.That(BootstrapConfigValidationService.TryCreateRunSimulationService(json,File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json"),out var service),Is.False);
            Assert.That(service,Is.Null);
        }
        [Test] public void MultipleRealDefendersShareRoundsAndEvidenceIsChronological()
        {
            var run=Start(Config(),B.Room(0,true,Monster("a",2,2),Monster("b",3,3,1))).DrainToCompletion();
            var e=CombatC0Diagnostics.Events(run); Assert.That(e.Count,Is.GreaterThan(0));
            Assert.That(e.Count(x=>x.Kind==CombatC0EventKind.EncounterStart),Is.EqualTo(1));
            Assert.That(e.Where(x=>x.Kind==CombatC0EventKind.Attack).Select(x=>x.ActorId).Distinct().Count(),Is.GreaterThan(2));
            Assert.That(e.Select(x=>x.Ordinal),Is.EqualTo(Enumerable.Range(0,e.Count)));
            Assert.That(e.All(x=>x.ExecutableId==CombatC0Config.Executable&&x.RuleSourceId==CombatC0Config.Rule&&x.RunOrdinal>=0),Is.True);
            Assert.That(e.Zip(e.Skip(1),(a,b)=>a.RunOrdinal<=b.RunOrdinal).All(x=>x),Is.True);
            Assert.That(CombatC0Diagnostics.Monsters(run).All(m=>m.Defeated),Is.True);
            Assert.That(run.EncounterEvents,Is.Empty); // No historical one-sided monster damage.
        }
        [Test] public void CapturedRulesCannotBeChangedAfterStartAndC0CannotExportB1()
        {
            var c=Config(); var room=B.Room(0,true,Monster()); var execution=Start(c,room);
            c.CombatC0.Classes[0].Damage=int.MaxValue; c.CombatC0.Monsters[0].MaxHealth=int.MaxValue;
            room.Assignments[0].RoomLocalPosition=new TileCoordinate(99,99);
            Assert.That(execution.StartIdentity,Is.Null);
            Assert.Throws<InvalidOperationException>(()=>execution.ExportCheckpoint(new RunCheckpointLimits(1000000,10000)));
            var actual=execution.DrainToCompletion(); var expected=Start(Config(),B.Room(0,true,Monster())).DrainToCompletion();
            Assert.That(B.Structured(CombatC0Diagnostics.Events(actual)),Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(expected))));
        }
        [Test] public void SourceCollectionReorderingCannotChangeOutcomeOrEvidence()
        {
            var c=Config(); var a=Monster("a",2,2,10); var b=Monster("b",3,3,2);
            var expected=Start(c,B.Room(0,true,a,b)).DrainToCompletion();
            Array.Reverse(c.CombatC0.Classes); Array.Reverse(c.CombatC0.Monsters); Array.Reverse(c.PhaseFiveB.Classes);
            var room=B.Room(0,true,b,a); Array.Reverse(room.Spatial.TraversableTiles); Array.Reverse(room.Spatial.Assignments);
            var actual=Start(c,room).DrainToCompletion();
            Assert.That(B.Structured(actual),Is.EqualTo(B.Structured(expected)));
            Assert.That(B.Structured(CombatC0Diagnostics.Events(actual)),Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(expected))));
        }
        [TestCase(false)] [TestCase(true)] public void SolidSupercoverObstructsStraightAndCornerShots(bool corner)
        {
            var c=Config(); var party=Party("ranger"); var m=Monster(); var k=Kernel(c,party,m);
            var room=B.Room(0,true,m); var solid=corner ? new TileCoordinate(1,0) : new TileCoordinate(1,1);
            room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(solid)).ToArray();
            var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),k,new[]{solid});
            var from=corner ? new TileCoordinate(0,0) : new TileCoordinate(0,1); var to=new TileCoordinate(2,corner?2:1);
            Assert.That(geometry.LineOfSight(from,to),Is.False); Assert.That(geometry.LineOfSight(to,from),Is.False);
            Assert.That(geometry.Eligibility(k.MemberProfile("adventurer.class.ranger"),from,to),Is.EqualTo(CombatC0Reason.LineOfSightBlocked));
        }
        [Test] public void MovementRestrictionsAreNotInventedOpacityAndFriendsAreTransparent()
        {
            var c=Config(); var party=Party("ranger","mage"); var m=Monster(); var k=Kernel(c,party,m); var room=B.Room(0,true,m);
            room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(new TileCoordinate(1,1))).ToArray();
            var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),k);
            Assert.That(geometry.LineOfSight(new TileCoordinate(0,1),new TileCoordinate(2,1)),Is.True);
            Assert.That(geometry.Eligibility(k.MemberProfile("adventurer.class.ranger"),new TileCoordinate(0,0),new TileCoordinate(3,3)),Is.EqualTo(CombatC0Reason.OutOfRange));
            Assert.That(geometry.Eligibility(k.MemberProfile("adventurer.class.mage"),new TileCoordinate(0,1),new TileCoordinate(2,1)),Is.EqualTo(CombatC0Reason.Legal));
        }
        [Test] public void PartyAnchorOverlapMovesToDistinctPhysicalPosition()
        {
            var c=Config(); var party=Party("warrior"); var a=Monster("a",0,1); var b=Monster("b",1,1,1); var k=Kernel(c,party,a,b); var room=B.Room(0,true,a,b);
            var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),k);
            var group=geometry.Collect(room.Spatial.Ingress,new[]{a,b},new Dictionary<string,TileCoordinate>{{"a",a.RoomLocalPosition},{"b",b.RoomLocalPosition}},new HashSet<string>());
            Assert.That(group.Participating,Has.Length.EqualTo(2)); Assert.That(group.Positions.Values.Distinct().Count(),Is.EqualTo(2));
            Assert.That(group.Positions.Values.Contains(room.Spatial.Ingress),Is.False);
        }
        [Test] public void MonsterDeathDoesNotGrantExtraLootAndTrapAuthorityRemainsSingular()
        {
            var room=B.Room(0,true,Monster("a",2,2),Monster("b",3,3,1),B.Assignment("trap",MvpDungeonPlacementIds.TrapCategoryId,MvpDungeonPlacementIds.SpikeTrapOptionId,1,1),
                B.Assignment("loot",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,3,1));
            var run=Start(Config(),room).DrainToCompletion(true); Assert.That(run.EncounterEvents,Has.Length.EqualTo(1));
            Assert.That(run.EncounterEvents[0].OptionId,Is.EqualTo(MvpDungeonPlacementIds.SpikeTrapOptionId));
            var table=PhaseSixA4Tests.Loot().tables.Single(t=>t.id==run.LootSummary.LootTableId);
            Assert.That(run.LootSummary.RollCount,Is.InRange(table.minRollCount,table.maxRollCount));
            var kinds=run.PresentationSteps.Select(s=>s.Kind).ToArray();
            Assert.That(Array.IndexOf(kinds,RunStepKind.MonsterEngagement),Is.LessThan(Array.IndexOf(kinds,RunStepKind.TrapTrigger)));
            Assert.That(Array.IndexOf(kinds,RunStepKind.TrapTrigger),Is.LessThan(Array.IndexOf(kinds,RunStepKind.LootInteraction)));
        }
        [Test] public void SeparatedEncountersOnOneRepeatedRouteKeepDefeatsAndDoNotReplayActions()
        {
            var c=Config(); foreach(var p in c.CombatC0.Classes) { p.AttackType=CombatC0Config.Melee; p.Range=1; p.Damage=10; }
            foreach(var p in c.CombatC0.Monsters) { p.AttackType=CombatC0Config.Ranged; p.Range=1; }
            var room=B.Room(0,true,Monster("a",1,0),Monster("b",3,2,1),
                B.Assignment("loot-first",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,3,1),
                B.Assignment("loot-return",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,0,1,1));
            var run=Start(c,room).DrainToCompletion(); var events=CombatC0Diagnostics.Events(run);
            Assert.That(events.Count(e=>e.Kind==CombatC0EventKind.EncounterStart),Is.EqualTo(2));
            var deaths=events.Where(e=>e.Defeated&&e.TargetId.Contains("/monster/")).ToArray();
            Assert.That(deaths,Has.Length.EqualTo(2)); Assert.That(deaths.Select(e=>e.EncounterId).Distinct().Count(),Is.EqualTo(2));
            Assert.That(events.Where(e=>e.Kind==CombatC0EventKind.Attack&&e.ActorId.EndsWith("/a")).All(e=>e.Ordinal<deaths[0].Ordinal),Is.True);
            Assert.That(CombatC0Diagnostics.Monsters(run).All(m=>m.Health==0),Is.True);
        }
        [Test] public void IsolatedMeleeDefenderIsBypassedWithoutRewardsOrHealthMutation()
        {
            var c=Config(); foreach(var p in c.CombatC0.Classes) { p.AttackType=CombatC0Config.Melee; p.Range=1; }
            var room=B.Room(0,true,Monster("isolated",3,3));
            room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(new TileCoordinate(3,2))&&!t.Equals(new TileCoordinate(2,3))).ToArray();
            var run=Start(c,room).DrainToCompletion(); var events=CombatC0Diagnostics.Events(run);
            Assert.That(events.Select(e=>e.Kind),Is.EqualTo(new[]{CombatC0EventKind.Bypass}));
            Assert.That(events[0].Reason,Is.EqualTo(CombatC0Reason.MeleePositionUnavailable));
            Assert.That(CombatC0Diagnostics.Monsters(run).Single().Health,Is.EqualTo(8));
            Assert.That(run.LootSummary.RollCount,Is.Zero); Assert.That(run.Party.Members.All(m=>m.CurrentHealth==m.MaxHealth),Is.True);
        }
        [TestCase("zero")] [TestCase("round")] [TestCase("event")] [TestCase("work")]
        public void TechnicalFailureCannotExposeAnOutcome(string limit)
        {
            var c=Config(); foreach(var p in c.CombatC0.Monsters) { p.Damage=0; p.MaxHealth=int.MaxValue; }
            if(limit=="zero") foreach(var p in c.CombatC0.Classes) p.Damage=0;
            if(limit=="round") c.CombatC0.MaximumRounds=1;
            if(limit=="event") c.CombatC0.MaximumEvents=1;
            if(limit=="work") c.CombatC0.MaximumWork=1;
            RunExecution execution=null;
            Assert.Throws<InvalidOperationException>(()=> { execution=Start(c,B.Room(0,true,Monster())); execution.DrainToCompletion(); });
            if(execution!=null) Assert.Throws<InvalidOperationException>(()=>execution.TakeOutcome());
        }
        private static DetachedCanonicalWriteAuthorityTests.Fixture Canonical(bool two=false)
        {
            void Author(RunSimulationConfig config) {
                config.CombatC0=Config().CombatC0;
                PhaseSixA4Tests.ForceDescent(config,true);
            }
            var f=two ? PhaseSixA4Tests.Eligible(PhaseFourTestSupport.AuthoredInputs((config, _) => Author(config)))
                : PhaseFiveBBranchIntegrationTests.Fixture(false, Author);
            PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId);
            if(two) { PhaseSixA4Tests.AddContent(f,1,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.SkeletonOptionId);
                f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,FloorLifecycleAction.Activate,PhaseSixA4Tests.FloorTwo(f))); }
            return f;
        }
        [Test] public void CanonicalManualRunPublishesC0OnceKeepsManaHistoryAndTransientEvidenceOnly()
        {
            var f=Canonical(); int sequence=f.Runtime.runHistory.NextRunSequence;
            PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId);
            double mana=f.Runtime.structureRuntime.ManaReserve;
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); var run=f.Runtime.runHistory.LatestOutcome;
            Assert.That(CombatC0Diagnostics.IsCombatC0(run),Is.True); Assert.That(CombatC0Diagnostics.Events(run).Count,Is.GreaterThan(0));
            Assert.That(CombatC0Diagnostics.Monsters(run),Has.Count.EqualTo(2));
            Assert.That(CombatC0Diagnostics.Events(run).Count(e=>e.Kind==CombatC0EventKind.EncounterStart),Is.EqualTo(1));
            Assert.That(run.RunId,Is.EqualTo("run-"+sequence)); Assert.That(f.Runtime.runHistory.NextRunSequence,Is.EqualTo(sequence+1));
            double settledMana = mana - f.Runtime.monsterContinuity.Inspect().Sum(m => m.Health - run.CombatMonsters.Single(c => c.AssignmentId == m.AssignmentId).Health);
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(settledMana)); Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget,Is.EqualTo(CanonicalSaveSchemaVersions.TimeProvenanceIntroduction));
            string aggregate=JsonUtility.ToJson(run); Assert.That(aggregate,Does.Not.Contain("Combat").And.Not.Contain("Health"));
            var clone=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(f.Runtime)); RunTransientEvidence.Retain(f.Runtime,clone);
            Assert.That(B.Structured(CombatC0Diagnostics.Events(clone.runHistory.LatestOutcome)),Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(run))));
            f.Reopen(); Assert.That(CombatC0Diagnostics.Events(f.Runtime.runHistory.LatestOutcome),Is.Empty);
            Assert.That(JsonUtility.ToJson(f.Runtime.runHistory.LatestOutcome),Is.EqualTo(aggregate));
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(settledMana));
        }
        [Test] public void C0ManualSelectionAndScheduledB1SelectionUseSameCapturedGeometryWithoutChangingCheckpoint()
        {
            var f=Canonical(); var snapshot=PhaseSixA4Tests.Snapshot(f); var service=PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var scheduled=service.StartSnapshot(1,snapshot,LivingDungeonPhaseAExecutionTests.Timing());
            Assert.That(scheduled.ExportCheckpoint(new RunCheckpointLimits(1000000,10000)),Is.Not.Null);
            var b1=scheduled.DrainToCompletion(); Assert.That(CombatC0Diagnostics.IsCombatC0(b1),Is.False); Assert.That(b1.EncounterEvents,Has.Length.EqualTo(1));
            var manual=service.StartManualSnapshot(1,snapshot); Assert.Throws<InvalidOperationException>(()=>manual.ExportCheckpoint(new RunCheckpointLimits(1000000,10000)));
            Assert.That(CombatC0Diagnostics.IsCombatC0(manual.DrainToCompletion()),Is.True);
        }
        [Test] public void TwoFloorsShareActualPartyHealthAndImmutableCapturedConfiguration()
        {
            var f=Canonical(true); var snapshot=PhaseSixA4Tests.Snapshot(f); var service=PhaseFiveBBranchIntegrationTests.Service(f.Configuration);
            var expected=service.StartManualSnapshot(1,snapshot).DrainToCompletion();
            Assert.That(expected.FloorTransitions.Any(t=>t.Descend),Is.True);
            Assert.That(CombatC0Diagnostics.Events(expected).Where(e=>e.Kind==CombatC0EventKind.EncounterStart).Select(e=>e.FloorId).Distinct().Count(),Is.EqualTo(2));
            f.Configuration.CombatC0.Classes[0].Damage=int.MaxValue;
            var actual=service.StartManualSnapshot(1,snapshot).DrainToCompletion();
            Assert.That(B.Structured(CombatC0Diagnostics.Events(actual)),Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(expected))));
            var attacks=CombatC0Diagnostics.Events(actual).Where(e=>e.Kind==CombatC0EventKind.Attack&&e.TargetId.Contains("/member/")).ToArray();
            foreach(var group in attacks.GroupBy(e=>e.TargetId))
                Assert.That(group.Zip(group.Skip(1),(a,b)=>a.HealthAfter==b.HealthBefore).All(x=>x),Is.True);
        }
        [Test] public void LaterCombatTechnicalFailureRollsBackEarlierDamageLootAndAllCanonicalState()
        {
            var f=Canonical(true); foreach(var p in f.Configuration.CombatC0.Monsters) { p.Damage=0; p.MaxHealth=p.Id==MvpDungeonPlacementIds.GoblinOptionId?1:int.MaxValue; }
            var snapshot=PhaseSixA4Tests.Snapshot(f); var staged=PhaseFiveBBranchIntegrationTests.Service(f.Configuration).StartHistoricalCombatC0Snapshot(1,snapshot);
            RunStep step; do { step=staged.AdvanceToNextEvent(staged.NextOrdinal); } while(step.Kind!=RunStepKind.RoomComplete);
            Assert.That(staged.Inspect().CarriedLootValue,Is.GreaterThan(0));
            Assert.Throws<InvalidOperationException>(()=>staged.DrainToCompletion());
            byte[] before=f.FileSystem.ReadAllBytes(f.ActivePath); string live=B.Structured(f.Runtime); var session=f.Session;
            var result=PhaseFiveBBranchIntegrationTests.Run(f); Assert.That(result.IsSuccess,Is.False); Assert.That(result.RuntimeProjection,Is.Null);
            CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Session,Is.SameAs(session)); Assert.That(B.Structured(f.Runtime),Is.EqualTo(live));
        }
        [Test] public void AtomicSaveFailureAfterC0CombatPublishesNothing()
        {
            var f=Canonical(); var before=f.FileSystem.ReadAllBytes(f.ActivePath); var live=B.Structured(f.Runtime);
            f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write,2);
            var result=PhaseFiveBBranchIntegrationTests.Run(f); Assert.That(result.IsSuccess,Is.False);
            Assert.That(result.Reason,Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(B.Structured(f.Runtime),Is.EqualTo(live));
        }
        [Test] public void ExactProductionEventAndRoundLimitsPassAndOneLessFailsWithoutHealthMutation()
        {
            var c=Config(); c.CombatC0.MaximumRounds=4; foreach(var p in c.CombatC0.Monsters) p.Damage=0;
            var party=Party("warrior"); var m=Monster(); var kernel=Kernel(c,party,m); var room=B.Room(0,true,m);
            var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),kernel);
            var group=geometry.Collect(room.Spatial.Ingress,new[]{m},new Dictionary<string,TileCoordinate>{{m.AssignmentId,m.RoomLocalPosition}},new HashSet<string>());
            var reference=kernel.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links);
            Assert.That(reference.Events.Last().Round,Is.EqualTo(4));
            c.CombatC0.MaximumEvents=reference.Events.Count;
            var exact=Kernel(c,party,m); Assert.That(exact.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links).Events.Count,Is.EqualTo(reference.Events.Count));
            c.CombatC0.MaximumEvents--; var over=Kernel(c,party,m);
            Assert.Throws<InvalidOperationException>(()=>over.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links)); Assert.That(over.MonsterHealth(m.AssignmentId),Is.EqualTo(8));
            c.CombatC0.MaximumEvents=32768; c.CombatC0.MaximumRounds=3; var rounds=Kernel(c,party,m);
            Assert.Throws<InvalidOperationException>(()=>rounds.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links)); Assert.That(rounds.MonsterHealth(m.AssignmentId),Is.EqualTo(8));
        }
        [Test] public void NewEncounterIdentityKeepsRunLocalHealthAndCompletedReceiptCannotAttackTwice()
        {
            var c=Config(); var party=Party("warrior","rogue"); var a=Monster("a",1,1); var b=Monster("b",2,2,1);
            var kernel=Kernel(c,party,a,b); var room=B.Room(0,true,a,b); var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),kernel);
            var positions=new Dictionary<string,TileCoordinate>{{"a",a.RoomLocalPosition},{"b",b.RoomLocalPosition}};
            var first=geometry.Collect(room.Spatial.Ingress,new[]{a},positions,new HashSet<string>());
            var receipt=kernel.Resolve("phase.a.floor",room.RoomInstanceId,0,first.Assignments,first.Links); var hp=party.Members.Select(m=>m.CurrentHealth).ToArray(); int events=kernel.EventCount;
            Assert.That(kernel.Resolve("phase.a.floor",room.RoomInstanceId,0,first.Assignments,first.Links),Is.SameAs(receipt));
            CollectionAssert.AreEqual(hp,party.Members.Select(m=>m.CurrentHealth)); Assert.That(kernel.EventCount,Is.EqualTo(events));
            var second=geometry.Collect(new TileCoordinate(3,1),new[]{b},first.Positions,new HashSet<string>{"a"});
            var later=kernel.Resolve("phase.a.floor",room.RoomInstanceId,1,second.Assignments,second.Links);
            Assert.That(later.Events[0].EncounterId,Is.Not.EqualTo(receipt.Events[0].EncounterId));
            Assert.That(kernel.MonsterHealth("a"),Is.Zero); Assert.That(kernel.MonsterHealth("b"),Is.Zero);
        }
        [Test] public void InjectedRangedDefenderAttacksWithoutAdjacentMeleeSlot()
        {
            var c=Config(); foreach(var p in c.CombatC0.Monsters) { p.AttackType=CombatC0Config.Ranged; p.Range=4; }
            var party=Party("ranger"); var m=Monster("ranged",3,1); var kernel=Kernel(c,party,m); var room=B.Room(0,true,m);
            var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),kernel);
            var group=geometry.Collect(room.Spatial.Ingress,new[]{m},new Dictionary<string,TileCoordinate>{{m.AssignmentId,m.RoomLocalPosition}},new HashSet<string>());
            Assert.That(group.Movement,Is.Empty); var result=kernel.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links);
            Assert.That(result.Events.Any(e=>e.Kind==CombatC0EventKind.Attack&&e.ActorId.EndsWith("/ranged")),Is.True);
            Assert.That(result.Events.Where(e=>e.Kind==CombatC0EventKind.Attack).All(e=>!Nullable.Equals(e.SourcePosition,e.TargetPosition)),Is.True);
        }
        [Test] public void ReplayRetainsHistoricalSupportedEventsAndRejectsMismatchedC0Layout()
        {
            var policy=ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try { var old=VisualRunReplayTests.Completed(); Assert.That(VisualRunReplayTests.Create(old,policy),Is.Not.Null);
                var f=Canonical(); f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
                Assert.That(DungeonRunReplay.TryCreate(f.Runtime.runHistory.LatestOutcome,f.State,f.Production.Catalog,"unused","unused",policy,out _,out var reason),Is.False);
                Assert.That(reason,Is.EqualTo(DungeonRunReplay.Layout)); }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [Test] public void EnabledC0CannotChangeInjectedB2OpportunityExecutionOrItsDurableCheckpoint()
        {
            var f=Canonical(); var t=typeof(LivingDungeonPhaseB2Tests); var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
            f=(DetachedCanonicalWriteAuthorityTests.Fixture)t.GetMethod("Qualify",flags).Invoke(null,new object[]{f});
            t.GetMethod("Activate",flags).Invoke(null,new object[]{f}); t.GetMethod("Admit",flags).Invoke(null,new object[]{f,0L,0L});
            foreach(var p in f.Runtime.livingDungeon.Parties)
            {
                var profile=(LivingDungeonProfile)t.GetMethod("ProfileA",flags).Invoke(null,null);
                var execution=LivingDungeonContracts.Restore(p,profile);
                Assert.That(execution.StartIdentity,Is.Not.Null); Assert.That(execution.Combat,Is.Null);
                Assert.That(execution.ExportCheckpoint(profile.Checkpoints),Is.Not.Null); execution.Abandon();
            }
            Assert.That(f.Runtime.livingDungeon.Parties,Is.Not.Empty);
        }
        [Test] public void ExecutionBatchSizeCannotChangeCompletedCombatOrChronology()
        {
            var first=Start(Config(),B.Room(0,true,Monster("a",2,2),Monster("b",3,3,1)));
            var second=Start(Config(),B.Room(0,true,Monster("a",2,2),Monster("b",3,3,1)));
            while(!first.IsTerminal) first.Advance(1,first.NextOrdinal);
            while(!second.IsTerminal) second.Advance(7,second.NextOrdinal);
            var a=first.TakeOutcome(); var b=second.TakeOutcome();
            Assert.That(B.Structured(a),Is.EqualTo(B.Structured(b)));
            Assert.That(B.Structured(CombatC0Diagnostics.Events(a)),Is.EqualTo(B.Structured(CombatC0Diagnostics.Events(b))));
            Assert.That(CombatC0Diagnostics.ChargedWork(a),Is.EqualTo(CombatC0Diagnostics.ChargedWork(b)));
        }
        [Test] public void ProductionC0WipeStopsUnreachedTrapLootAndMovement()
        {
            var c=Config(); foreach(var p in c.CombatC0.Monsters) { p.Damage=int.MaxValue; p.MaxHealth=int.MaxValue; p.Initiative=int.MaxValue; }
            var room=B.Room(0,true,Monster(),B.Assignment("trap",MvpDungeonPlacementIds.TrapCategoryId,MvpDungeonPlacementIds.SpikeTrapOptionId,1,1),
                B.Assignment("loot",MvpDungeonPlacementIds.LootNodeCategoryId,MvpDungeonPlacementIds.HiddenCacheOptionId,3,1));
            var run=Start(c,room).DrainToCompletion(true);
            Assert.That(run.Party.IsWiped,Is.True); Assert.That(run.SurvivalSummary.DeathCount,Is.EqualTo(run.Party.Members.Count));
            Assert.That(run.Success,Is.False); Assert.That(run.EncounterEvents,Is.Empty); Assert.That(run.LootSummary.RollCount,Is.Zero);
            Assert.That(run.PresentationSteps.Any(s=>s.Kind==RunStepKind.TrapTrigger||s.Kind==RunStepKind.LootInteraction||s.Kind==RunStepKind.Movement),Is.False);
            Assert.That(CombatC0Diagnostics.Events(run).Last().Reason,Is.EqualTo(CombatC0Reason.PartyWiped));
        }
        [Test] public void DeadRangedMemberCannotAdmitAnOtherwiseIneligibleDefender()
        {
            var c=Config(); var party=Party("warrior","ranger"); party.Members[1].ApplyDamage(party.Members[1].MaxHealth);
            var a=Monster("legal",1,2); var b=Monster("isolated",3,3,1); var room=B.Room(0,true,a,b);
            room.Spatial.Ingress=new TileCoordinate(1,1);
            room.Spatial.TraversableTiles=room.Spatial.TraversableTiles.Where(t=>!t.Equals(new TileCoordinate(3,2))&&!t.Equals(new TileCoordinate(2,3))).ToArray();
            var kernel=Kernel(c,party,a,b); var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,4,4),kernel);
            var group=geometry.Collect(room.Spatial.Ingress,new[]{a,b},new Dictionary<string,TileCoordinate>{{a.AssignmentId,a.RoomLocalPosition},{b.AssignmentId,b.RoomLocalPosition}},new HashSet<string>());
            Assert.That(group.Participating,Is.EqualTo(new[]{a.AssignmentId}));
            var result=kernel.Resolve("phase.a.floor",room.RoomInstanceId,0,group.Assignments,group.Links);
            Assert.That(result.Completion,Is.EqualTo(CombatC0Reason.DefendersDefeated)); Assert.That(kernel.MonsterHealth(b.AssignmentId),Is.EqualTo(8));
            Assert.That(result.Events.Any(e=>e.Kind==CombatC0EventKind.Attack&&e.ActorId.EndsWith("/isolated")),Is.False);
        }
        [Test] public void ActualProductionRoomAndFloorEnvelopeFitsApprovedWorkAndParticipantBounds()
        {
            long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
            var c=Config(); var f=Canonical(); var catalog=f.Production.Catalog;
            string[] classes={"warrior","rogue","ranger","cleric","mage"}; var party=Party(classes);
            Assert.That(c.PhaseFiveB.MaxPartySize,Is.EqualTo(classes.Length));
            var records=new List<(int Area,int Monsters,long Work)>();
            foreach(var definition in catalog.Rooms.Where(r=>r.MonsterCapacity>0))
            {
                Assert.That(definition.ReservedTileOffsets,Is.Empty);
                int width=definition.GrossFootprint.Width,height=definition.GrossFootprint.Height,vertices=width*height;
                var assignments=Enumerable.Range(0,definition.MonsterCapacity).Select(i=>Monster("m"+i,i%width,i/width,i)).ToArray();
                var room=B.Room(0,true,assignments); room.Spatial.TraversableTiles=IntraroomSnapshot.DeriveTraversableTiles(definition,64);
                room.Spatial.MaximumMaterializedTiles=64;
                var kernel=Kernel(c,party,assignments); var geometry=new CombatC0Geometry(room.Spatial,new CombatC0RoomBounds("phase.a.floor",room.RoomInstanceId,width,height),kernel);
                var positions=assignments.ToDictionary(a=>a.AssignmentId,a=>a.RoomLocalPosition);
                // Synthetic pessimistic repeated collection: no defender is ever removed.
                // Max path uses one shortest segment per loot node plus egress.
                int pathBound=(definition.LootCapacity+1)*(vertices-1)+1;
                for(int i=0;i<pathBound+definition.MonsterCapacity;i++) geometry.Collect(room.Spatial.TraversableTiles[i%vertices],assignments,positions,new HashSet<string>());
                long n=assignments.Length+definition.TrapCapacity+definition.LootCapacity;
                long planner=2*(vertices*vertices+6*vertices+n*n+n*vertices+(definition.LootCapacity+1)*(6*vertices+pathBound)+n*pathBound+n*n);
                long actors=classes.Length+definition.MonsterCapacity,links=2L*classes.Length*definition.MonsterCapacity;
                long combat=definition.MonsterCapacity*(actors*actors*actors+links*links+links*actors+actors+links+c.CombatC0.MaximumRounds*actors*(actors+1));
                long work=kernel.WorkCount+planner+combat;
                records.Add((vertices,definition.MonsterCapacity,work));
                TestContext.WriteLine(definition.RoomDefinitionId+": conservative room work "+work+", path "+pathBound);
            }
            long total=0; int participants=classes.Length,encounters=0;
            foreach(var floor in catalog.Floors)
            {
                // Unbounded integer knapsack ignores fixed structures/corridors, so it
                // overestimates every actually placeable layout in this floor capacity.
                int capacity=floor.FinalFloorSpaceCapacity; var works=new long[capacity+1]; var counts=new int[capacity+1];
                for(int space=1;space<=capacity;space++) foreach(var r in records.Where(r=>r.Area<=space)) {
                    works[space]=Math.Max(works[space],works[space-r.Area]+r.Work);
                    counts[space]=Math.Max(counts[space],counts[space-r.Area]+r.Monsters); }
                total+=works[capacity]; participants+=counts[capacity]; encounters+=counts[capacity];
            }
            Assert.That(total,Is.LessThan(c.CombatC0.MaximumWork)); Assert.That(participants,Is.LessThanOrEqualTo(c.CombatC0.MaximumParticipants));
            Assert.That(encounters,Is.LessThanOrEqualTo(c.CombatC0.MaximumEncounters));
            long allocated=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;
            if(allocated>0) Assert.That(allocated,Is.LessThan(128L*1024*1024)); // Test-only regression ceiling, not a platform promise.
            TestContext.WriteLine("Production envelope upper work "+total+", participants "+participants+", encounters "+encounters+
                (allocated>0 ? "; fixture/adapter thread allocations "+allocated : "; thread allocation counter unavailable on this Unity Mono runtime"));
        }
        [Serializable] private sealed class GoldenEntry { public string Scenario,File,Identity,StartIdentity,Inspection; public long Ordinal; public bool Consumed; }
        [Serializable] private sealed class GoldenManifest { public string Source,UnityVersion; public GoldenEntry[] Entries; }
        [Test] public void FrozenPreIntegrationB1BytesRestoreAtEveryBoundaryAndKeepOutcomes()
        {
            using(var zip=ZipFile.OpenRead("Assets/_Project/Tests/EditMode/combat-c0-b1-golden.zip"))
            {
                string Read(string name) { using(var r=new StreamReader(zip.GetEntry(name).Open())) return r.ReadToEnd(); }
                var manifest=JsonUtility.FromJson<GoldenManifest>(Read("manifest.json"));
                Assert.That(JsonUtility.ToJson(PhaseFiveBTestConfig.Production()),Is.EqualTo(Read("historical-config.json")));
                Assert.That(manifest.Source,Is.EqualTo("c0dba584f1d1b9f10ccc50bf51fc26c831c5f81e"));
                var service=new RunSimulationService(Config(),PhaseSixA4Tests.Loot()); var limits=new RunCheckpointLimits(1000000,10000);
                foreach(var item in manifest.Entries)
                {
                    byte[] bytes; using(var stream=zip.GetEntry(item.File).Open()) using(var memory=new MemoryStream()) { stream.CopyTo(memory); bytes=memory.ToArray(); }
                    var restored=service.RestoreCheckpoint(bytes,item.Identity,item.StartIdentity,item.Ordinal,limits);
                    Assert.That(restored.IsValid,Is.True,item.File+": "+restored.FailureReason);
                    CollectionAssert.AreEqual(bytes,restored.Execution.ExportCheckpoint(limits).GetBytes(),item.File);
                    Assert.That(B.Structured(restored.Execution.Inspect()),Is.EqualTo(item.Inspection),item.File);
                    if(item.Consumed) Assert.Throws<InvalidOperationException>(()=>restored.Execution.TakeOutcome());
                    else Assert.That(B.Structured(restored.Execution.DrainToCompletion()),Is.EqualTo(Read(item.Scenario+"-outcome.txt")),item.File);
                }
                TestContext.WriteLine("Frozen historical checkpoint boundaries: "+manifest.Entries.Length);
            }
        }
    }
}
#endif
