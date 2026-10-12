#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Store = DungeonBuilder.M0.Tests.EditMode.PhaseSevenA4TransactionalEditorTests.Store;
using Operation = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class TransactionalRoomContentTests
    {
        private static Fixture Room()
        {
            var f = Fixture.Create(null);
            var room = f.Prepare(DetachedCanonicalMutationRequest.Place("placement.category.room", "placement.option.room.basic", null, null, null));
            Assert.That(room.IsSuccess, Is.True, room.Reason); return f.Rebase(room.State);
        }
        private static DungeonDraftContext Context(Fixture f) => new DungeonDraftContext(f.Production, f.Configuration, f.Occupancy, f.Profile, f.Compatibility, f.RemovalPolicy);
        private static DungeonDraftRoomContent Add(Fixture f, string category = "placement.category.monster", string option = "placement.option.monster.skeleton", int x = 0, int y = 0) =>
            Context(f).ContentIntent(DungeonDraftContentOperation.Acquire, f.State.Floors[0].FloorInstanceId,
                f.State.Floors[0].Layout.Rooms[0].RoomInstanceId, categoryId: category, optionId: option, position: new TileCoordinate(x, y));
        private static void Flush(TransactionalDungeonDraft d) { while (d.Durability == DraftDurability.Pending) Assert.That(d.FlushNext(), Is.True, d.Reason); }
        private static GateEvaluationInput Allowed => new GateEvaluationInput(RestrictedActionType.Purchase, true, false);
        private static StructuralEconomyPreview Quote(Fixture f, TransactionalDungeonDraft d) => StructuralEconomyService.PreviewFinalContentDraft(f.State, d.ReadModel,
            StructuralInvestment.Zero(f.State), d.NormalizedMovementTargets(), f.Runtime.structureRuntime.ManaReserve, f.Economy, f.Acquisition);

        [TestCase("placement.category.monster", "placement.option.monster.skeleton")]
        [TestCase("placement.category.trap", "placement.option.trap.spike")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.basic")]
        public void PreviewConfirmDurableRecoverAndAtomicPurchaseRetainExactIdentity(string category, string option)
        {
            var f = Room(); var store = new Store(); var context = Context(f); var d = TransactionalDungeonDraft.Create(f.State, context, store);
            var before = f.Session.GetCurrentBytes(); double mana = f.Runtime.structureRuntime.ManaReserve;
            var intent = Add(f, category, option, 3, 3); var preview = d.PreviewContent(intent);
            Assert.That(preview.IsValid, Is.True, preview.Reason); Assert.That(d.CommandCount, Is.Zero);
            Assert.That(d.ChangeContent(intent), Is.True); Assert.That(d.CanSave, Is.False); Flush(d);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            d = TransactionalDungeonDraft.Recover(store.Read(), f.State, context, store, out var reason); Assert.That(d, Is.Not.Null, reason);
            Assert.That(d.ReadModel.Floors[0].RoomContents.Assignments.Single().AssignmentId, Is.EqualTo(preview.Assignment.AssignmentId));
            Assert.That(f.Acquisition.TryPrice(category, option, out double cost), Is.True);
            Assert.That(Quote(f, d).Cost, Is.EqualTo(cost));
            var result = f.Authority.CommitDungeonDraft(f.ActivePath, f.FileSystem, f.Session, f.Runtime, d, Allowed);
            Assert.That(result.IsSuccess, Is.True, result.Reason); f.Accept(result); f.Reopen();
            Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().RoomLocalPosition, Is.EqualTo(new TileCoordinate(3, 3)));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana - cost)); Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(13));
        }
        [TestCase(-1,0)][TestCase(4,0)][TestCase(0,4)][TestCase(int.MaxValue,int.MinValue)]
        public void InvalidCoordinatesNeverAllocateOrJournal(int x, int y)
        {
            var f = Room(); var s = new Store(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), s); var before = s.Read();
            Assert.That(d.ChangeContent(Add(f, x:x, y:y)), Is.False); Assert.That(d.CommandCount, Is.Zero);
            Assert.That(Context(f).Fingerprint(d.ReadModel), Is.EqualTo(Context(f).Fingerprint(f.State))); CollectionAssert.AreEqual(before, s.Read());
        }
        [TestCase("floor")][TestCase("room")][TestCase("category")][TestCase("option")][TestCase("identity")]
        public void InvalidIdsFailClosed(string kind)
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); var intent = Add(f);
            if (kind == "floor") intent.FloorInstanceId = "missing";
            if (kind == "room") intent.RoomInstanceId = "missing";
            if (kind == "category") intent.CategoryId = "missing";
            if (kind == "option") intent.OptionId = "missing";
            if (kind == "identity") intent.AssignmentId = "fabricated";
            Assert.That(d.ChangeContent(intent), Is.False); Assert.That(d.HasChanges, Is.False);
        }
        [Test]
        public void RepeatedMonstersHaveDistinctIdentitiesAndFullCategoryRejectsWithoutAllocation()
        {
            var f = Room(); var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store());
            Assert.That(d.ChangeContent(Add(f)), Is.True); Assert.That(d.ChangeContent(Add(f, x:1)), Is.True);
            var a = d.ReadModel.Floors[0].RoomContents.Assignments;
            Assert.That(a.Select(v => v.AssignmentId).Distinct().Count(), Is.EqualTo(2)); Assert.That(a[0].Sequence, Is.LessThan(a[1].Sequence));
            var before = Context(f).Fingerprint(d.ReadModel); Assert.That(d.ChangeContent(Add(f, x:2)), Is.False);
            Assert.That(Context(f).Fingerprint(d.ReadModel), Is.EqualTo(before)); Assert.That(Quote(f,d).ContentAcquisitionCost, Is.EqualTo(50));
        }
        [TestCase("placement.category.trap", "placement.option.trap.spike")]
        [TestCase("placement.category.trap", "placement.option.trap.snare")]
        [TestCase("placement.category.trap", "placement.option.trap.chilling_sigil")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.basic")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.hidden_cache")]
        [TestCase("placement.category.loot_node", "placement.option.loot_node.glittering_hoard")]
        public void SameOptionInstancesRetainIdentityCustodyEconomicsRecoveryAndReopen(string category, string option)
        {
            var f = Room(); var context=Context(f); var store=new Store();
            var d = TransactionalDungeonDraft.Create(f.State, context, store); var before=f.Session.GetCurrentBytes();
            double mana=f.Runtime.structureRuntime.ManaReserve; Assert.That(f.Acquisition.TryPrice(category,option,out double price),Is.True);
            Assert.That(d.ChangeContent(Add(f,category,option)),Is.True);
            Assert.That(d.ChangeContent(Add(f,category,option)),Is.False,"Same option cannot overlap its sibling");
            Assert.That(d.Reason,Is.EqualTo(DetachedCanonicalSpatialMutation.PositionInvalidReason));
            Assert.That(d.ChangeContent(Add(f,category,option,1)),Is.True);
            var assignments=d.ReadModel.Floors[0].RoomContents.Assignments; var first=assignments[0]; var sibling=assignments[1];
            Assert.That(first.AssignmentId,Is.Not.EqualTo(sibling.AssignmentId)); Assert.That(first.Sequence,Is.LessThan(sibling.Sequence));
            string unchangedSibling=JsonUtility.ToJson(sibling); var fingerprint=context.Fingerprint(d.ReadModel);
            Assert.That(d.ChangeContent(Add(f,category,option,2)),Is.False); Assert.That(d.Reason,Is.EqualTo(DetachedSpatialMigrationPreparer.CapacityReason));
            Assert.That(context.Fingerprint(d.ReadModel),Is.EqualTo(fingerprint)); Assert.That(d.CommandCount,Is.EqualTo(2));
            Assert.That(Quote(f,d).ContentAcquisitions,Is.EqualTo(2)); Assert.That(Quote(f,d).Cost,Is.EqualTo(2*price));
            var floor=f.State.Floors[0].FloorInstanceId;
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Reposition,floor,first.RoomInstanceId,first.AssignmentId,position:new TileCoordinate(2,1))),Is.True);
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Unassign,floor,assignmentId:first.AssignmentId)),Is.True);
            var owned=d.ReadModel.LifecycleAndOwnership.ReturnedContents.Single(); Assert.That(owned.AssignmentId,Is.EqualTo(first.AssignmentId)); Assert.That(owned.Sequence,Is.EqualTo(first.Sequence));
            Assert.That(JsonUtility.ToJson(d.ReadModel.Floors[0].RoomContents.Assignments.Single()),Is.EqualTo(unchangedSibling));
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Redeploy,floor,first.RoomInstanceId,first.AssignmentId,position:new TileCoordinate(2,1))),Is.True);
            Assert.That(Quote(f,d).Cost,Is.EqualTo(2*price)); Flush(d);
            d=TransactionalDungeonDraft.Recover(store.Read(),f.State,context,store,out var reason); Assert.That(d,Is.Not.Null,reason);
            CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana));
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed); Assert.That(result.IsSuccess,Is.True,result.Reason); f.Accept(result); f.Reopen();
            assignments=f.State.Floors[0].RoomContents.Assignments; Assert.That(assignments.Select(a=>a.AssignmentId),Is.EqualTo(new[] {first.AssignmentId,sibling.AssignmentId}));
            Assert.That(assignments[0].Sequence,Is.EqualTo(first.Sequence)); Assert.That(assignments[0].RoomLocalPosition,Is.EqualTo(new TileCoordinate(2,1)));
            Assert.That(JsonUtility.ToJson(assignments[1]),Is.EqualTo(unchangedSibling)); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana-2*price));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents,Is.Empty);
            var discard=TransactionalDungeonDraft.Create(f.State,Context(f),new Store()); Assert.That(discard.ChangeContent(Context(f).ContentIntent(DungeonDraftContentOperation.Unassign,floor,assignmentId:first.AssignmentId)),Is.True);
            Assert.That(discard.Discard(),Is.True); Assert.That(f.State.Floors[0].RoomContents.Assignments.Length,Is.EqualTo(2));
            var corrupt=Context(f).Copy(f.State); corrupt.Floors[0].RoomContents.Assignments[1].AssignmentId=first.AssignmentId;
            Assert.That(CanonicalSpatialSaveContracts.Validate(corrupt,f.Profile.Canonical.Spatial,true).IsValid,Is.False,"Duplicate identities remain corrupt even when options may repeat");
        }
        [Test]
        public void AcquireUnassignRedeployChargesOnceAndDiscardNeverCreatesOwnership()
        {
            var f = Room(); var context = Context(f); var s = new Store(); var d = TransactionalDungeonDraft.Create(f.State, context, s);
            Assert.That(d.ChangeContent(Add(f)), Is.True); var a = d.ReadModel.Floors[0].RoomContents.Assignments.Single();
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Unassign, f.State.Floors[0].FloorInstanceId, assignmentId:a.AssignmentId)), Is.True);
            var returned = d.ReadModel.LifecycleAndOwnership.ReturnedContents.Single();
            Assert.That(returned.Sequence, Is.EqualTo(a.Sequence)); Assert.That(returned.AssignmentId, Is.EqualTo(a.AssignmentId));
            Assert.That(Quote(f,d).ContentAcquisitions, Is.EqualTo(1)); Assert.That(Quote(f,d).Cost, Is.EqualTo(25));
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Redeploy, f.State.Floors[0].FloorInstanceId,
                a.RoomInstanceId, a.AssignmentId, position:new TileCoordinate(1,1))), Is.True);
            Assert.That(d.ReadModel.LifecycleAndOwnership.ReturnedContents, Is.Empty); Assert.That(Quote(f,d).Cost, Is.EqualTo(25));
            Flush(d); Assert.That(d.Discard(), Is.True); Assert.That(f.State.Floors[0].RoomContents.Assignments, Is.Empty);
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents, Is.Empty);
        }
        [Test]
        public void ExistingReturnedIdentityRedeploysOfflineWithoutCharge()
        {
            var f = PhaseSevenA4TransactionalEditorTests.Content(); var a = f.State.Floors[0].RoomContents.Assignments.Single();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(a.AssignmentId, f.State.Floors[0].FloorInstanceId)));
            var d = TransactionalDungeonDraft.Create(f.State, Context(f), new Store()); double mana = f.Runtime.structureRuntime.ManaReserve;
            Assert.That(d.ChangeContent(Context(f).ContentIntent(DungeonDraftContentOperation.Redeploy, f.State.Floors[0].FloorInstanceId,
                a.RoomInstanceId,a.AssignmentId,position:new TileCoordinate(1,1))), Is.True); Flush(d);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,new GateEvaluationInput(RestrictedActionType.Purchase,false,true));
            Assert.That(result.IsSuccess,Is.True,result.Reason); f.Accept(result); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana)); Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId,Is.EqualTo(a.AssignmentId));
        }
        [TestCase("placement.category.trap","placement.option.trap.spike")]
        [TestCase("placement.category.trap","placement.option.trap.snare")]
        [TestCase("placement.category.trap","placement.option.trap.chilling_sigil")]
        [TestCase("placement.category.loot_node","placement.option.loot_node.basic")]
        [TestCase("placement.category.loot_node","placement.option.loot_node.hidden_cache")]
        [TestCase("placement.category.loot_node","placement.option.loot_node.glittering_hoard")]
        public void ExistingRepeatedInstanceReturnsAndRedeploysWithoutRepurchase(string category,string option)
        {
            var f=Room(); string room=f.State.Floors[0].Layout.Rooms[0].RoomInstanceId; var floor=f.State.Floors[0].FloorInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(category,option,room,floor,new TileCoordinate(0,0))));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(category,option,room,floor,new TileCoordinate(1,0))));
            var first=f.State.Floors[0].RoomContents.Assignments[0]; string sibling=JsonUtility.ToJson(f.State.Floors[0].RoomContents.Assignments[1]); double mana=f.Runtime.structureRuntime.ManaReserve;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(first.AssignmentId,floor)));
            Assert.That(f.State.LifecycleAndOwnership.ReturnedContents.Single().AssignmentId,Is.EqualTo(first.AssignmentId));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Redeploy(first.AssignmentId,room,floor,new TileCoordinate(2,1)))); f.Reopen();
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana)); Assert.That(f.State.LifecycleAndOwnership.ReturnedContents,Is.Empty);
            Assert.That(f.State.Floors[0].RoomContents.Assignments[0].AssignmentId,Is.EqualTo(first.AssignmentId));
            Assert.That(f.State.Floors[0].RoomContents.Assignments[0].Sequence,Is.EqualTo(first.Sequence));
            Assert.That(JsonUtility.ToJson(f.State.Floors[0].RoomContents.Assignments[1]),Is.EqualTo(sibling));
        }
        [TestCase("unaffordable")][TestCase("offline")][TestCase("pending")][TestCase("persistence")]
        public void TwoSameOptionPurchasesStillFailAtomicallyUnderExistingRestrictions(string failure)
        {
            var f=Room(); var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store()); var before=f.Session.GetCurrentBytes();
            if(failure=="unaffordable") f.Runtime.structureRuntime.ManaReserve=0; double mana=f.Runtime.structureRuntime.ManaReserve;
            Assert.That(d.ChangeContent(Add(f,"placement.category.loot_node","placement.option.loot_node.glittering_hoard")),Is.True);
            Assert.That(d.ChangeContent(Add(f,"placement.category.loot_node","placement.option.loot_node.glittering_hoard",1)),Is.True); Flush(d);
            if(failure=="persistence") f.FileSystem.EnableFailure(Operation.Replace,1);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,new GateEvaluationInput(RestrictedActionType.Purchase,failure!="offline",failure=="pending"));
            Assert.That(result.IsSuccess,Is.False); Assert.That(result.RuntimeProjection,Is.Null); Assert.That(d.CanSave,Is.True);
            CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana));
            Assert.That(f.State.Floors[0].RoomContents.Assignments,Is.Empty); Assert.That(f.State.LifecycleAndOwnership.ReturnedContents,Is.Empty);
        }
        [TestCase(false,false,"gate.error.offline_required")][TestCase(true,true,"gate.error.verification_pending")]
        public void RestrictedFinalPurchaseRetainsCanonicalAndDraft(bool online,bool pending,string reason)
        {
            var f=Room(); var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store()); var before=f.Session.GetCurrentBytes();
            Assert.That(d.ChangeContent(Add(f)),Is.True); Flush(d);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,new GateEvaluationInput(RestrictedActionType.Purchase,online,pending));
            Assert.That(result.IsSuccess,Is.False); Assert.That(result.Reason,Is.EqualTo(reason)); CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(d.CanSave,Is.True);
        }
        [Test]
        public void UnaffordableFinalCombinedQuotePublishesNothing()
        {
            var f=Room(); f.Runtime.structureRuntime.ManaReserve=0; var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store()); var before=f.Session.GetCurrentBytes();
            Assert.That(d.ChangeContent(Add(f)),Is.True); Flush(d); Assert.That(Quote(f,d).IsAffordable,Is.False);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed);
            Assert.That(result.IsSuccess,Is.False); CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.Zero);
        }
        [TestCase(Operation.Write,2)][TestCase(Operation.Replace,1)][TestCase(Operation.Flush,1)]
        public void ContentAtomicPersistenceFailurePublishesNothing(Operation op,int occurrence)
        {
            var f=Room(); var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store()); var before=f.Session.GetCurrentBytes(); double mana=f.Runtime.structureRuntime.ManaReserve;
            Assert.That(d.ChangeContent(Add(f)),Is.True); Flush(d); f.FileSystem.EnableFailure(op,occurrence);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed);
            Assert.That(result.IsSuccess,Is.False); Assert.That(result.RuntimeProjection,Is.Null); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(mana)); CollectionAssert.AreEqual(before,f.Session.GetCurrentBytes());
        }
        [Test]
        public void CrossRoomReassignIsOneCommandAndPreservesIdentityAfterRecovery()
        {
            var f=TransactionalRoomConstructionTests.Room(); var context=Context(f); var s=new Store(); var d=TransactionalDungeonDraft.Create(f.State,context,s);
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"),TransactionalRoomConstructionTests.Request(f)),Is.True); Flush(d);
            var a=d.ReadModel.Floors[0].RoomContents.Assignments.Single(); var room=d.ReadModel.Floors[0].Layout.Rooms.Single(r=>r.RoomInstanceId!=a.RoomInstanceId);
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Reassign,f.State.Floors[0].FloorInstanceId,room.RoomInstanceId,a.AssignmentId,position:new TileCoordinate(1,1))),Is.True); Flush(d);
            d=TransactionalDungeonDraft.Recover(s.Read(),f.State,context,s,out var reason); Assert.That(d,Is.Not.Null,reason);
            var moved=d.ReadModel.Floors[0].RoomContents.Assignments.Single(); Assert.That(moved.AssignmentId,Is.EqualTo(a.AssignmentId)); Assert.That(moved.Sequence,Is.EqualTo(a.Sequence)); Assert.That(moved.OptionId,Is.EqualTo(a.OptionId));
            Assert.That(Quote(f,d).ContentAcquisitions,Is.Zero); Assert.That(moved.RoomInstanceId,Is.EqualTo(room.RoomInstanceId));
        }
        [Test]
        public void FormatFourEvidenceRemainsImmutableWhenUpgradedToContentVocabulary()
        {
            var f=Room(); var context=Context(f); var s=new FileDungeonDraftStore(f.ActivePath+".editor-draft",f.FileSystem,f.Profile); var d=TransactionalDungeonDraft.Create(f.State,context,s);
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"),TransactionalRoomConstructionTests.Request(f)),Is.True); Flush(d);
            var paths=f.FileSystem.Paths.Where(p=>p.Contains(".editor-draft")).ToDictionary(p=>p,p=>f.FileSystem.ReadAllBytes(p));
            Assert.That(d.ChangeContent(Add(f)),Is.True); Flush(d);
            foreach(var pair in paths) CollectionAssert.AreEqual(pair.Value,f.FileSystem.ReadAllBytes(pair.Key));
            var recovered=TransactionalDungeonDraft.Recover(s.Read(),f.State,context,s,out var reason); Assert.That(recovered,Is.Not.Null,reason);
            Assert.That(recovered.CommandCount,Is.EqualTo(2)); Assert.That(recovered.ReadModel.Floors[0].RoomContents.Assignments.Length,Is.EqualTo(1));
        }
        [Test]
        public void CollisionCategorySharingAndMultiCellBoundaryUseInjectedOccupancy()
        {
            var f=Room(); var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store());
            Assert.That(d.ChangeContent(Add(f)),Is.True);
            Assert.That(d.ChangeContent(Add(f,option:"placement.option.monster.goblin")),Is.False);
            var rule=f.Occupancy.Value.Records.Single(r=>r.OptionId=="placement.option.trap.spike");
            rule.OccupiedTileOffsets=new[] {new TileCoordinate(0,0),new TileCoordinate(1,0)};
            d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store());
            Assert.That(d.ChangeContent(Add(f,"placement.category.trap","placement.option.trap.spike",3,1)),Is.False);
            Assert.That(d.ChangeContent(Add(f,"placement.category.trap","placement.option.trap.spike",2,1)),Is.True);
            Assert.That(d.ChangeContent(Add(f,"placement.category.loot_node","placement.option.loot_node.basic",3,1)),Is.False);
        }
        [Test]
        public void CombinedConstructionAndContentCommitHasOneWalletChargeAndNoContentInvestment()
        {
            var f=Room(); f.Runtime.structureRuntime.ManaReserve=1000;
            var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store());
            Assert.That(d.ConstructRoom(Guid.NewGuid().ToString("N"),TransactionalRoomConstructionTests.Request(f)),Is.True);
            Assert.That(d.ChangeContent(Add(f)),Is.True); Flush(d);
            var quote=Quote(f,d); Assert.That(quote.IsAffordable,Is.True,quote.Reason); Assert.That(quote.ContentAcquisitionCost,Is.EqualTo(25));
            double structural=StructuralEconomyService.PreviewFinalDraft(f.State,d.ReadModel,StructuralInvestment.Zero(f.State),d.NormalizedMovementTargets(),1000,f.Economy).Cost;
            Assert.That(quote.Cost,Is.EqualTo(structural+25));
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed); Assert.That(result.IsSuccess,Is.True,result.Reason);
            f.Accept(result); f.Reopen(); Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(quote.ResultingMana));
            Assert.That(f.State.Floors[0].Layout.Rooms.Length,Is.EqualTo(2)); Assert.That(f.State.Floors[0].RoomContents.Assignments.Length,Is.EqualTo(1));
            Assert.That(result.Validation.Investment.Sum(i=>i.ConstructionMana+i.RenovationMana),Is.EqualTo(structural));
        }
        [Test]
        public void AcquisitionChangesLaterKnowledgeAndRunsButCannotRewriteExistingRunSnapshot()
        {
            var f=PhaseSevenA4TransactionalEditorTests.Content(); var old=PhaseSixA4Tests.Snapshot(f);
            string plan=JsonUtility.ToJson(old.Floors[0].MaterializePlan());
            Assert.That(FloorKnowledgeApplicability.TryCompute(f.State,new CorridorContentAuthority(),f.Profile.Canonical,f.State.Floors[0].FloorInstanceId,out var fingerprint),Is.True);
            var knowledge=new FloorKnowledgeRecord { FloorInstanceId=f.State.Floors[0].FloorInstanceId,ApplicabilityFingerprint=fingerprint };
            Assert.That(FloorKnowledgeApplicability.IsApplicable(knowledge,f.State,new CorridorContentAuthority(),f.Profile.Canonical),Is.True);
            var d=TransactionalDungeonDraft.Create(f.State,Context(f),new Store());
            Assert.That(d.ChangeContent(Add(f,option:"placement.option.monster.goblin",x:1,y:1)),Is.True); Flush(d);
            var result=f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed); Assert.That(result.IsSuccess,Is.True,result.Reason); f.Accept(result);
            Assert.That(JsonUtility.ToJson(old.Floors[0].MaterializePlan()),Is.EqualTo(plan));
            Assert.That(JsonUtility.ToJson(PhaseSixA4Tests.Snapshot(f).Floors[0].MaterializePlan()),Is.Not.EqualTo(plan));
            Assert.That(FloorKnowledgeApplicability.IsApplicable(knowledge,f.State,new CorridorContentAuthority(),f.Profile.Canonical),Is.False);
        }
        [Test]
        public void ContentJournalUnknownDurabilityBlocksCommitAndRecoversOnlyProvenPrefix()
        {
            var f=Room(); var context=Context(f); var store=new FileDungeonDraftStore(f.ActivePath+".editor-draft",f.FileSystem,f.Profile);
            var d=TransactionalDungeonDraft.Create(f.State,context,store); var before=f.Session.GetCurrentBytes();
            Assert.That(d.ChangeContent(Add(f)),Is.True); f.FileSystem.EnableFailure(Operation.Flush,1);
            Assert.That(d.FlushNext(),Is.False); Assert.That(d.Durability,Is.EqualTo(DraftDurability.Unknown)); Assert.That(d.AcknowledgedSequence,Is.Zero);
            Assert.That(f.Authority.CommitDungeonDraft(f.ActivePath,f.FileSystem,f.Session,f.Runtime,d,Allowed).IsSuccess,Is.False);
            f.FileSystem.DisableFailure(); CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath));
            store=new FileDungeonDraftStore(f.ActivePath+".editor-draft",f.FileSystem,f.Profile);
            var recovered=TransactionalDungeonDraft.Recover(store.Read(),f.State,context,store,out var reason);
            Assert.That(recovered,Is.Not.Null,reason); Assert.That(recovered.CommandCount,Is.Zero); Assert.That(recovered.ReadModel.Floors[0].RoomContents.Assignments,Is.Empty);
        }
        [Test]
        public void ContentRecoveryRejectsStaleBaselineAndChangedCustodyPolicy()
        {
            var f=PhaseSevenA4TransactionalEditorTests.Content(); var context=Context(f); var s=new Store(); var d=TransactionalDungeonDraft.Create(f.State,context,s);
            var a=f.State.Floors[0].RoomContents.Assignments.Single();
            Assert.That(d.ChangeContent(context.ContentIntent(DungeonDraftContentOperation.Unassign,f.State.Floors[0].FloorInstanceId,assignmentId:a.AssignmentId)),Is.True); Flush(d);
            var changed=context.Copy(f.State); changed.Floors[0].RoomContents.Assignments[0].RoomLocalPosition=new TileCoordinate(1,1);
            Assert.That(TransactionalDungeonDraft.Recover(s.Read(),changed,context,s,out var reason),Is.Null); Assert.That(reason,Is.EqualTo(TransactionalDungeonDraft.StaleReason));
            f.RemovalPolicy.Value.Records.Single(r=>r.OptionId==a.OptionId).Policy=StructuralContentRemovalPolicy.DoesNotSurviveStructuralRemoval;
            Assert.That(TransactionalDungeonDraft.Recover(s.Read(),f.State,Context(f),s,out _),Is.Null);
        }
        [Test]
        public void ConfiguredCommandBudgetAndInvalidSourceCannotMutateDraft()
        {
            var f=Room(); var raw=f.Profile.Raw;
            var profile=new SaveSpatialMigrationLimitsProfile(new RawSavePayloadClassificationLimits(raw.MaximumInputBytes,
                raw.MaximumNestingDepth,raw.MaximumObjectMembers,1,raw.MaximumStringBytes,raw.MaximumScanWork),f.Profile.Canonical,f.Profile.Whole);
            var context=new DungeonDraftContext(f.Production,f.Configuration,f.Occupancy,profile,f.Compatibility,f.RemovalPolicy);
            var d=TransactionalDungeonDraft.Create(f.State,context,new Store());
            Assert.That(d.ChangeContent(Add(f)),Is.True); var fingerprint=context.Fingerprint(d.ReadModel);
            Assert.That(d.ChangeContent(Add(f,x:1)),Is.False); Assert.That(d.Reason,Is.EqualTo(TransactionalDungeonDraft.LimitReason));
            Assert.That(context.Fingerprint(d.ReadModel),Is.EqualTo(fingerprint)); Assert.That(d.CommandCount,Is.EqualTo(1));
            var invalid=context.Copy(f.State); invalid.Floors[0].Layout.Rooms[0].RoomDefinitionId="missing";
            Assert.That(context.PreviewContent(invalid,Add(f)).IsValid,Is.False);
            CollectionAssert.AreEqual(f.Session.GetCurrentBytes(),f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void UnassignmentPreviewDoesNotExposeMutableDraftOwnership()
        {
            var f=PhaseSevenA4TransactionalEditorTests.Content(); var context=Context(f);
            var d=TransactionalDungeonDraft.Create(f.State,context,new Store());
            var a=f.State.Floors[0].RoomContents.Assignments.Single(); var fingerprint=context.Fingerprint(d.ReadModel);
            var preview=d.PreviewContent(context.ContentIntent(DungeonDraftContentOperation.Unassign,f.State.Floors[0].FloorInstanceId,assignmentId:a.AssignmentId));
            Assert.That(preview.IsValid,Is.True,preview.Reason);
            preview.Assignment.RoomLocalPosition=new TileCoordinate(2,2); preview.Assignment.AssignmentId="mutated preview";
            Assert.That(context.Fingerprint(d.ReadModel),Is.EqualTo(fingerprint)); Assert.That(d.CommandCount,Is.Zero);
            Assert.That(d.HasChanges,Is.False); Assert.That(f.State.Floors[0].RoomContents.Assignments.Single().AssignmentId,Is.EqualTo(a.AssignmentId));
        }
        [TestCase("commit-after")][TestCase("commit-barrier")][TestCase("commit-readback")]
        public void UnknownContentAcknowledgementRecoversOnlyIndependentCommittedEvidence(string fault)
        {
            var f=Room(); var context=Context(f); string path=f.ActivePath+".editor-draft";
            var store=new FileDungeonDraftStore(path,f.FileSystem,f.Profile); var d=TransactionalDungeonDraft.Create(f.State,context,store);
            Assert.That(d.ChangeContent(Add(f)),Is.True);
            if(fault=="commit-after") f.FileSystem.EnableTargetedFailure(Operation.Write,p=>p[0].EndsWith(".000001.commit"),1,true);
            else if(fault=="commit-barrier") f.FileSystem.EnableFailure(Operation.Flush,2);
            else f.FileSystem.EnableTargetedFailure(Operation.Read,p=>p[0].EndsWith(".000001.commit"),1,false);
            Assert.That(d.FlushNext(),Is.False); Assert.That(d.Durability,Is.EqualTo(DraftDurability.Unknown));
            Assert.That(d.ChangeContent(Add(f,x:1)),Is.False); f.FileSystem.DisableFailure();
            store=new FileDungeonDraftStore(path,f.FileSystem,f.Profile);
            var recovered=TransactionalDungeonDraft.Recover(store.Read(),f.State,context,store,out var reason);
            Assert.That(recovered,Is.Not.Null,reason); Assert.That(recovered.AcknowledgedSequence,Is.EqualTo(1));
            Assert.That(recovered.ReadModel.Floors[0].RoomContents.Assignments.Length,Is.EqualTo(1));
            Assert.That(f.State.Floors[0].RoomContents.Assignments,Is.Empty);
        }
    }
}
#endif
