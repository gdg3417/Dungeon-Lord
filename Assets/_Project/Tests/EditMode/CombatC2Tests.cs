#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class CombatC2Tests
    {
        private static MonsterRecoveryRules Rules => MonsterRecoveryRules.EditorFallback();
        private static Fixture Deployed()
        {
            var f = Fixture.Create(null); f.Configuration = CombatC0IntegrationTests.Config();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId,
                null, null, new TileCoordinate(2, 2))));
            return f;
        }
        internal static void SetState(Fixture f, int hp, bool revival, double mana)
        {
            var state = f.Runtime.monsterContinuity.Copy(); var r = state.Records.Single(); r.Health = hp; r.RevivalRequired = revival;
            var snapshot = DetachedRecognizedSaveStateSnapshot.CaptureWithMana(f.Runtime, mana, f.Profile);
            var update = f.Session.PrepareLiveReplacement(snapshot, f.State, monsterContinuity: state);
            Assert.That(update.IsSuccess, Is.True, update.Reason);
            f.FileSystem.Seed(f.ActivePath, update.Update.GetBytes()); f.Reopen();
        }
        private static MonsterContinuityRecord Monster(Fixture f) => f.Runtime.monsterContinuity.Records.Single();
        [Test] public void MigrationStageDiagnostics()
        {
            var source=Gd66DetachedSpatialMigrationTransactionTests.PrepareEmptyFixture(6);
            var limits=source.Limits; var bytes=source.Result.Attempt.Candidate.GetBytes();
            Assert.That(SchemaSevenToEightUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"7 to 8");
            Assert.That(SchemaEightToNineUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"8 to 9");
            Assert.That(SchemaNineToTenUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"9 to 10");
            Assert.That(SchemaTenToElevenUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"10 to 11");
            Assert.That(SchemaElevenToTwelveUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"11 to 12");
            Assert.That(SchemaTwelveToThirteenUpgrade.TryPrepare(bytes,limits,PhaseFourTestSupport.PositionProfiles(limits),out bytes),Is.True,"12 to 13");
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(bytes,limits,out bytes),Is.True,"13 to 14");
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(bytes,limits,Rules,out bytes),Is.True,"14 to 15");
        }
        private static DetachedCanonicalWriteResult Fund(Fixture f, double mana)
        {
            var current = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(f.Runtime));
            current.structureRuntime.ManaReserve = mana;
            return f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, current);
        }
        [TestCase(MvpDungeonPlacementIds.GoblinOptionId, 8, 6)]
        [TestCase(MvpDungeonPlacementIds.SkeletonOptionId, 10, 7)]
        public void ReadinessUsesExactConfiguredCeiling(string option, int maximum, int threshold)
        { Assert.That(Rules.MaximumHealth(option), Is.EqualTo(maximum)); Assert.That(Rules.Readiness(option), Is.EqualTo(threshold)); }

        [Test] public void MigrationAddsHealthyStateWithoutChangingAnyHistoricalOwner()
        {
            var f = Deployed(); SetState(f, 2, true, 0.25);
            byte[] fourteen = PhaseFourTestSupport.FrozenFourteen(f.Session.GetCurrentBytes());
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(fourteen, f.Profile.Canonical, Rules, out var current), Is.True);
            CollectionAssert.AreEqual(fourteen, PhaseFourTestSupport.FrozenFourteen(current));
            var validated = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(current, f.Context);
            Assert.That(validated.IsValid, Is.True); var r = validated.MonsterContinuity.Records.Single();
            Assert.That(r.Health, Is.EqualTo(8)); Assert.That(r.Level, Is.EqualTo(1)); Assert.That(r.RevivalRequired, Is.False);
        }
        [TestCase("missing")] [TestCase("duplicate")] [TestCase("orphan")] [TestCase("negative")]
        [TestCase("excess")] [TestCase("zero_living")] [TestCase("ready_reviving")] [TestCase("level")]
        [TestCase("alias")] [TestCase("fractional")]
        public void InvalidContinuityCannotOpenOrPublish(string kind)
        {
            var f = Deployed(); string text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            var r = Monster(f); string id = r.AssignmentId;
            if (kind == "missing") text = text.Replace("{\"AssignmentId\":\"" + id + "\",\"Level\":1,\"Health\":8,\"RevivalRequired\":false}", "");
            if (kind == "duplicate") text = text.Replace("\"Records\":[", "\"Records\":[{\"AssignmentId\":\""+id+"\",\"Level\":1,\"Health\":8,\"RevivalRequired\":false},");
            if (kind == "orphan") text = text.Replace("\"Records\":[{\"AssignmentId\":\""+id, "\"Records\":[{\"AssignmentId\":\"orphan");
            if (kind == "negative") text = text.Replace("\"Health\":8", "\"Health\":-1");
            if (kind == "excess") text = text.Replace("\"Health\":8", "\"Health\":9");
            if (kind == "zero_living") text = text.Replace("\"Health\":8", "\"Health\":0");
            if (kind == "ready_reviving") text = text.Replace("\"RevivalRequired\":false", "\"RevivalRequired\":true");
            if (kind == "level") text = text.Replace("\"Level\":1", "\"Level\":2");
            if (kind == "alias") text = text.Replace("\"monsterContinuity\":", "\"MonsterContinuity\":");
            if (kind == "fractional") text = text.Replace("\"Health\":8", "\"Health\":1.5");
            Assert.That(DetachedCanonicalSaveSession.Open(Encoding.UTF8.GetBytes(text), f.Context, f.Profile).IsSuccess, Is.False, kind);
        }
        [TestCase(0, true, 2.75, 2, true, .75)]
        [TestCase(2, true, 4.25, 6, false, .25)]
        [TestCase(1, false, 1.5, 2, false, .5)]
        [TestCase(6, false, 100, 6, false, 100)]
        [TestCase(7, false, 100, 7, false, 100)]
        [TestCase(0, true, .99, 0, true, .99)]
        public void FundingRestoresWholeHealthOnlyAndClearsRevivalExactlyAtReadiness(int initial, bool revival, double funding, int expected, bool pending, double remaining)
        {
            var f = Deployed(); SetState(f, initial, revival, 0);
            f.Accept(Fund(f, funding)); Assert.That(Monster(f).Health, Is.EqualTo(expected));
            Assert.That(Monster(f).RevivalRequired, Is.EqualTo(pending)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(remaining));
            f.Reopen(); Assert.That(Monster(f).Health, Is.EqualTo(expected));
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(remaining));
        }
        private static (DetachedCanonicalSpatialSaveState, MonsterContinuityState) Synthetic()
        {
            var spatial = new DetachedCanonicalSpatialSaveState { Floors = new[] {
                new SavedSpatialFloor { FloorInstanceId="b", FloorIndex=1, ActivationState=FloorActivationState.Active,
                    RoomContents=new FloorRoomContentState { Assignments=new[] { Assignment("b", MvpDungeonPlacementIds.SkeletonOptionId, 0) } } },
                new SavedSpatialFloor { FloorInstanceId="a", FloorIndex=0, ActivationState=FloorActivationState.Active,
                    RoomContents=new FloorRoomContentState { Assignments=new[] { Assignment("a2", MvpDungeonPlacementIds.GoblinOptionId, 2), Assignment("a1", MvpDungeonPlacementIds.GoblinOptionId, 1) } } }
            } };
            var monsters = MonsterContinuity.Initialize(spatial, Rules);
            return (spatial, monsters);
        }
        private static RoomContentAssignment Assignment(string id, string option, long sequence) => new RoomContentAssignment {
            AssignmentId=id, OptionId=option, CategoryId=MvpDungeonPlacementIds.MonsterCategoryId, Sequence=sequence };
        [Test] public void LivingFirstThenCanonicalFloorSequenceAndIdentityWithPartialFunding()
        {
            var (spatial, state) = Synthetic(); var d=state.Records.ToDictionary(r=>r.AssignmentId);
            d["a1"].Health=0; d["a1"].RevivalRequired=true; d["a2"].Health=4; d["b"].Health=5;
            double balance=MonsterContinuity.Recover(state,spatial,Rules,5.5,new HashSet<string>());
            Assert.That(d["a2"].Health,Is.EqualTo(6)); Assert.That(d["b"].Health,Is.EqualTo(7));
            Assert.That(d["a1"].Health,Is.EqualTo(1)); Assert.That(d["a1"].RevivalRequired,Is.True); Assert.That(balance,Is.EqualTo(.5));
            Assert.That(MonsterContinuity.RequiredReserve(spatial,Rules),Is.EqualTo(19));
        }
        [Test] public void OccupiedAndInactiveFloorsAreIsolatedWithoutChangingObligationsForDefeat()
        {
            var (spatial,state)=Synthetic(); foreach(var r in state.Records) { r.Health=0; r.RevivalRequired=true; }
            double wallet=MonsterContinuity.Recover(state,spatial,Rules,100,new HashSet<string>{"a"});
            Assert.That(state.Records.Single(r=>r.AssignmentId=="b").Health,Is.EqualTo(7));
            Assert.That(state.Records.Where(r=>r.AssignmentId!="b").All(r=>r.Health==0),Is.True);
            Assert.That(wallet,Is.EqualTo(93)); Assert.That(MonsterContinuity.RequiredReserve(spatial,Rules),Is.EqualTo(19));
            spatial.Floors[0].ActivationState=FloorActivationState.Inactive;
            Assert.That(MonsterContinuity.RequiredReserve(spatial,Rules),Is.EqualTo(12));
        }
        [Test] public void EqualPlacementSequencesUseOrdinalIdentityRatherThanCollectionOrder()
        {
            var (spatial,state)=Synthetic(); foreach(var a in spatial.Floors.Single(f=>f.FloorIndex==0).RoomContents.Assignments) a.Sequence=1;
            foreach(var r in state.Records) r.Health=1;
            Assert.That(MonsterContinuity.Recover(state,spatial,Rules,1.25,new HashSet<string>()),Is.EqualTo(.25));
            Assert.That(state.Records.Single(r=>r.AssignmentId=="a1").Health,Is.EqualTo(2));
            Assert.That(state.Records.Single(r=>r.AssignmentId=="a2").Health,Is.EqualTo(1));
        }
        [Test] public void ActivationFundsIncreasedObligationAndDeactivationRemainsFreeDuringDeficit()
        {
            var f=PhaseSixA4Tests.Eligible(); PhaseSixA4Tests.AddContent(f,1,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId);
            SetState(f,0,true,5.99); var writer=PhaseSixA4Tests.Writer(f); string floor=PhaseSixA4Tests.FloorTwo(f);
            var denied=writer.CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,FloorLifecycleAction.Activate,floor);
            Assert.That(denied.IsSuccess,Is.False); Assert.That(denied.Reason,Is.EqualTo(MonsterRecoveryRules.Insufficient));
            f.Accept(Fund(f,6)); Assert.That(Monster(f).Health,Is.Zero,"Inactive floor preserves HP while funding arrives");
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,FloorLifecycleAction.Activate,floor));
            Assert.That(Monster(f).Health,Is.EqualTo(6)); Assert.That(Monster(f).RevivalRequired,Is.False);
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.Zero); Assert.That(MonsterContinuity.RequiredReserve(f.State,Rules),Is.EqualTo(6));
            f.Accept(writer.CommitFloorLifecycle(f.ActivePath,f.FileSystem,f.Session,f.Runtime,FloorLifecycleAction.Deactivate,floor));
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.Zero); Assert.That(MonsterContinuity.RequiredReserve(f.State,Rules),Is.Zero);
            Assert.That(Monster(f).Health,Is.EqualTo(6));
        }
        [Test] public void AcquisitionRequiresDirectPriceAndAddedReserveAndReliefNeverDeadlocks()
        {
            var f=Deployed(); SetState(f,0,true,0);
            string id=Monster(f).AssignmentId; var denied=f.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId,null,null,new TileCoordinate(3,2)));
            Assert.That(denied.IsSuccess,Is.False);
            var draft=TransactionalDungeonDraft.Create(f.State,PhaseSevenA4TransactionalEditorTests.Context(f),
                new PhaseSevenA4TransactionalEditorTests.Store());
            Assert.That(draft.Move(f.State.Floors[0].FloorInstanceId,f.State.Floors[0].Layout.Rooms[0].RoomInstanceId,id,new TileCoordinate(3,2)),Is.True);
            while(draft.Durability==DraftDurability.Pending) Assert.That(draft.FlushNext(),Is.True);
            f.Accept(f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,draft)); f.Reopen();
            Assert.That(Monster(f).AssignmentId,Is.EqualTo(id)); Assert.That(Monster(f).Health,Is.Zero);
            Assert.That(Monster(f).Level,Is.EqualTo(1)); Assert.That(Monster(f).RevivalRequired,Is.True);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(id)));
            Assert.That(Monster(f).AssignmentId,Is.EqualTo(id)); Assert.That(Monster(f).Health,Is.Zero); Assert.That(Monster(f).RevivalRequired,Is.True);
            Assert.That(MonsterContinuity.RequiredReserve(f.State,Rules),Is.Zero);
            var redeploy=f.Execute(DetachedCanonicalMutationRequest.Redeploy(id,f.State.Floors[0].Layout.Rooms[0].RoomInstanceId,null,new TileCoordinate(2,2)));
            Assert.That(redeploy.IsSuccess,Is.False); Assert.That(redeploy.Reason,Is.EqualTo(MonsterRecoveryRules.Insufficient));
            f.Accept(Fund(f,6)); Assert.That(Monster(f).Health,Is.Zero,"Custody does not heal or consume funding");
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(6));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(id,f.State.Floors[0].Layout.Rooms[0].RoomInstanceId,null,new TileCoordinate(2,2))));
            f.Reopen(); Assert.That(Monster(f).AssignmentId,Is.EqualTo(id)); Assert.That(Monster(f).Level,Is.EqualTo(1));
            Assert.That(Monster(f).Health,Is.EqualTo(6)); Assert.That(Monster(f).RevivalRequired,Is.False);
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.Zero); Assert.That(MonsterContinuity.RequiredReserve(f.State,Rules),Is.EqualTo(6));
        }
        [Test] public void LivingBelowReadinessCanFightWhilePartiallyRevivedCannotAndConsecutiveRunsRetainConsequences()
        {
            var f=Deployed(); SetState(f,2,false,0);
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); var first=f.Runtime.runHistory.LatestOutcome;
            Assert.That(CombatC0Diagnostics.IsCombatC2(first),Is.True);
            Assert.That(first.CombatMonsters.Single().InitialHealth,Is.EqualTo(2));
            Assert.That(first.CombatEvents.Any(e=>e.Kind==CombatC0EventKind.Attack),Is.True);
            Assert.That(Monster(f).Health,Is.Zero); Assert.That(Monster(f).RevivalRequired,Is.True);
            // Spend only actually available funding; no anticipated reward finances revival.
            f.Accept(Fund(f,2.25)); Assert.That(Monster(f).Health,Is.EqualTo(2)); Assert.That(Monster(f).RevivalRequired,Is.True);
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); var second=f.Runtime.runHistory.LatestOutcome;
            Assert.That(second.CombatMonsters.Single().InitialHealth,Is.EqualTo(2)); Assert.That(second.CombatMonsters.Single().InitialRevivalRequired,Is.True);
            Assert.That(second.CombatEvents,Is.Empty); Assert.That(second.CombatMonsters.Single().Health,Is.EqualTo(2));
        }
        [Test] public void ASubthresholdSurvivorNeverAcquiresRevivalStateWithoutDefeat()
        {
            var f=Deployed(); SetState(f,2,false,0);
            // Explicit synthetic combat setup: a surviving defender and bounded party wipe.
            foreach(var c in f.Configuration.CombatC0.Classes) c.Damage=0;
            foreach(var m in f.Configuration.CombatC0.Monsters) { m.Damage=100; m.Initiative=100; }
            for(int run=0;run<2;run++)
            {
                f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); Assert.That(Monster(f).Health,Is.EqualTo(2));
                Assert.That(Monster(f).RevivalRequired,Is.False);
                Assert.That(f.Runtime.runHistory.LatestOutcome.CombatEvents.Any(e=>e.Kind==CombatC0EventKind.Attack),Is.True);
            }
        }
        [Test] public void RecoveryAndReservePlayerStringsArePublished()
        {
            var table=UnityEngine.JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            foreach(string key in new[]{MonsterRecoveryRules.Invalid,MonsterRecoveryRules.Insufficient,"combat.c2.reserve","combat.c2.monster",
                "combat.c2.revival_pending","combat.c2.living","combat.c2.replay_start","combat.c2.recovery_spent"})
                Assert.That(table.entries.Single(e=>e.key==key).text,Is.Not.Empty,key);
        }
        [TestCase("write")] [TestCase("partial")] [TestCase("replace")] [TestCase("readback")] [TestCase("rollback")]
        public void FailedRecoveryPublishesNothingAndRetryDebitsOnlyOnce(string failure)
        {
            var f=Deployed(); SetState(f,0,true,0); var before=f.Session.GetCurrentBytes();
            var oldSession=f.Session; var oldMonster=Monster(f);
            f.FileSystem=new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(); f.FileSystem.Seed(f.ActivePath,before);
            var type=Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write;
            if(failure=="partial") f.FileSystem.EnablePartialWriteFailure(10);
            else if(failure=="replace") f.FileSystem.EnableTargetedFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace,p=>true,1,false);
            else if(failure=="readback") f.FileSystem.EnableTargetedFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read,p=>p[0]==f.ActivePath,3,false);
            else if(failure=="rollback") f.FileSystem.EnableFailureSequence(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush,1,
                Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace,2);
            else f.FileSystem.EnableTargetedFailure(type,p=>true,1,false);
            var result=Fund(f,3.75); Assert.That(result.IsSuccess,Is.False); Assert.That(result.RuntimeProjection,Is.Null);
            Assert.That(f.Session,Is.SameAs(oldSession)); Assert.That(Monster(f),Is.SameAs(oldMonster)); Assert.That(oldMonster.Health,Is.Zero);
            f.FileSystem.DisableFailure();
            // A rollback uncertainty is resolved by reopening the durable complete state.
            if(failure=="rollback") { f.Reopen(); Assert.That(Monster(f).Health,Is.EqualTo(0).Or.EqualTo(3)); return; }
            CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Accept(Fund(f,3.75)); Assert.That(Monster(f).Health,Is.EqualTo(3)); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(.75));
            var stale=FundWithOldSession(f,oldSession); Assert.That(stale.IsSuccess,Is.False);
        }
        private static DetachedCanonicalWriteResult FundWithOldSession(Fixture f, DetachedCanonicalSaveSession old) =>
            f.Authority.SaveRecognizedState(f.ActivePath,f.FileSystem,old,f.Runtime);
        [Test] public void OfflineFundingConsumesTimestampAndRecoveryTogetherWithoutASecondGrant()
        {
            var f=Deployed(); SetState(f,0,true,0); f.Runtime.lastSavedUtcUnix=1000;
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath,f.FileSystem,f.Session,f.Runtime));
            var old=f.Session; f.Accept(f.Authority.SaveOfflinePassiveMana(f.ActivePath,f.FileSystem,f.Session,f.Runtime,3.75,1000,4600));
            Assert.That(Monster(f).Health,Is.EqualTo(3)); Assert.That(Monster(f).RevivalRequired,Is.True);
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(.75)); Assert.That(f.Runtime.lastSavedUtcUnix,Is.EqualTo(4600));
            var bytes=f.Session.GetCurrentBytes(); f.Reopen();
            var duplicate=f.Authority.SaveOfflinePassiveMana(f.ActivePath,f.FileSystem,old,f.Runtime,3.75,1000,4600);
            Assert.That(duplicate.IsSuccess,Is.False); CollectionAssert.AreEqual(bytes,f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test] public void SchemaFifteenBudgetIsExplicitAndEarlierParserBudgetsStayFrozen()
        {
            var profile=SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            Assert.That(profile,Is.Not.Null); Assert.That(profile.Canonical.ForSchema(13).Serialized.MaximumInputBytes,Is.EqualTo(262144));
            Assert.That(profile.Canonical.ForSchema(14).Serialized.MaximumInputBytes,Is.EqualTo(655360));
            Assert.That(profile.Canonical.ForSchema(15).Serialized.MaximumInputBytes,Is.EqualTo(1376256));
            // Independent maximum owner envelope: existing 165-record spatial limit and
            // raw 4096-byte IDs. Actual canonical geometry also consumes spatial records.
            var records=Enumerable.Range(0,165).Select(i=>new MonsterContinuityRecord(i.ToString("D3")+new string('x',4093),1,8,false)).ToArray();
            var writer=new ContractJsonWriter(profile.Canonical.Serialized); MonsterContinuity.Write(writer,new MonsterContinuityState(records));
            byte[] owner=writer.Finish(); Assert.That(owner.Length,Is.LessThanOrEqualTo(720896));
            TestContext.WriteLine("C2 maximum independent owner records=165; idBytes=4096; ownerBytes="+owner.Length+"; historical14Envelope=655360; targetEnvelope=1376256");
        }
        [Test] public void OnlyContinuityArrayCanExceedTheUnchangedOrdinaryArrayBoundary()
        {
            var f=Deployed(); f.Profile=SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context=new DetachedCurrentTargetValidationContext(f.Compatibility,f.Production,LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration),f.Profile.Canonical,f.Occupancy);
            f.Reopen();
            var spatial=UnityEngine.JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(UnityEngine.JsonUtility.ToJson(f.State));
            spatial.LifecycleAndOwnership.ReturnedContents=Enumerable.Range(0,128).Select(i=>new ReturnedStructuralContent {
                AssignmentId="test.custody."+i.ToString("D3"),CategoryId=MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId=MvpDungeonPlacementIds.GoblinOptionId,Sequence=i,RemovalDisposition=StructuralContentRemovalDisposition.ReturnToPlayerCustody }).ToArray();
            var update=f.Session.PrepareSpatialOnlyReplacement(spatial);
            Assert.That(update.IsSuccess,Is.True,update.Reason); var bytes=update.Update.GetBytes();
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes,f.Context).MonsterContinuity.Records.Length,Is.EqualTo(129));
            var raw=RawSavePayloadClassifier.Classify(bytes,f.Profile.Raw,new RawSaveEnvelopeVersionContract(1,15),Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);
            Assert.That(raw.IsSuccess,Is.True,raw.FailureReason);
            var text=Encoding.UTF8.GetString(bytes);
            text=text.Substring(0,text.Length-1)+",\"extra\":["+string.Join(",",Enumerable.Repeat("0",129))+"]}";
            Assert.That(RawSavePayloadClassifier.Classify(Encoding.UTF8.GetBytes(text),f.Profile.Raw,new RawSaveEnvelopeVersionContract(1,15),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator).IsSuccess,Is.False);
        }
        [TestCase(2,false)] [TestCase(0,true)] [TestCase(2,true)]
        public void ReplayUsesRecordedInjuredAndInactiveStartingStateAndRemainsTransient(int hp,bool revival)
        {
            var f=Deployed(); SetState(f,hp,revival,0); f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            var run=f.Runtime.runHistory.LatestOutcome; var before=f.Session.GetCurrentBytes();
            var policy=UnityEngine.ScriptableObject.CreateInstance<DungeonPresentationPolicy>();
            try
            {
                var replay=VisualRunReplayTests.Create(f,policy); var actor=replay.Combat.Actors.Single(a=>a.IsMonster);
                Assert.That(actor.At(-1).Health,Is.EqualTo(hp)); Assert.That(actor.At(-1).RevivalRequired,Is.EqualTo(revival));
                Assert.That(actor.InitialHealth,Is.EqualTo(hp)); Assert.That(actor.MaxHealth,Is.EqualTo(8));
                Assert.That(run.CombatMonsters.Single().Health,Is.LessThanOrEqualTo(hp));
                CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); f.Reopen();
                Assert.That(f.Runtime.runHistory.LatestOutcome.CombatMonsters,Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(policy); }
        }
        [Test] public void WindowsSchemaFourteenMigrationAndNativeFifteenReopenUseIsolatedFiles()
        {
            if(UnityEngine.Application.platform!=UnityEngine.RuntimePlatform.WindowsEditor) Assert.Ignore("Windows qualification only");
            var f=Deployed(); var directory=Path.GetFullPath(Path.Combine("Temp","combat-c2-windows",Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); var path=Path.Combine(directory,"migration.json");
            var source=PhaseFourTestSupport.FrozenFourteen(f.Session.GetCurrentBytes()); File.WriteAllBytes(path,source);
            var preflight=SpatialMigrationFileSystemSelector.Evaluate(path); Assert.That(preflight.IsSupported,Is.True,preflight.Reason);
            var legacy=LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration);
            var coordinator=new DetachedSpatialSaveLoadCoordinator(f.Profile,f.Compatibility,f.Production,legacy,
                new Dictionary<string,byte[]>(),new RawSaveEnvelopeVersionContract(1,6),Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator,
                f.PositionProfiles,f.Occupancy,monsterRecovery:Rules);
            var migrated=coordinator.Load(path,preflight); Assert.That(migrated.IsSuccess,Is.True,migrated.Reason);
            Assert.That(migrated.RuntimeProjection.monsterContinuity.Records.Single().Health,Is.EqualTo(8));
            var bytes=File.ReadAllBytes(path); CollectionAssert.AreEqual(source,PhaseFourTestSupport.FrozenFourteen(bytes));
            var reopened=coordinator.Load(path,preflight); Assert.That(reopened.IsSuccess,Is.True,reopened.Reason); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
            var nativePath=Path.Combine(directory,"native.json"); var nativeFs=SpatialMigrationFileSystemSelector.Evaluate(nativePath);
            var native=NativeCanonicalSaveCreator.Create(nativePath,nativeFs.FileSystem,new SaveData(),f.Compatibility,f.Production,legacy,f.Profile,f.Acquisition,f.Occupancy,Rules);
            Assert.That(native.IsSuccess,Is.True,native.Reason); Assert.That(native.RuntimeProjection.monsterContinuity.Records,Is.Empty);
            Assert.That(DetachedCanonicalSaveSession.Open(File.ReadAllBytes(nativePath),f.Context,f.Profile).IsSuccess,Is.True);
            TestContext.WriteLine("Isolated Windows evidence: "+directory+"; migratedBytes="+bytes.Length);
        }
    }
}
#endif
