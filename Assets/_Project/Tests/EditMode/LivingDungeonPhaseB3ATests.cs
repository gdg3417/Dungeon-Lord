#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Scope = DungeonBuilder.M0.Gameplay.DungeonSpatial.DetachedCanonicalWriteAuthority.SyntheticForegroundQualification;
using Op = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseB3ATests
    {
        private static readonly RunLogicalTiming Timing = new RunLogicalTiming(3, 0, 0, 0, 3);
        private static LivingDungeonProfile Profile() => LivingDungeonProfile.ParseQualificationProfile(
            File.ReadAllBytes("Assets/_Project/Data/Production/Save/living_dungeon_qualification_profile.json"),
            new SpatialSerializedInputLimits(8192, 128, 32, 2048, 8), Timing, RunPostureResolver.BalancedId,
            SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile.Canonical.ForSchema(13).Serialized);
        private sealed class Harness
        {
            internal Fixture F;
            internal DetachedCanonicalWriteAuthority Writer;
            internal Scope Q;
            internal Harness(int retention = 10, bool wipe = false)
            {
                F = PhaseFiveBBranchIntegrationTests.Fixture();
                F.Configuration.MaxRunHistoryEntries = retention;
                if (wipe)
                {
                    F.Configuration.PhaseFiveB.MinPartySize = F.Configuration.PhaseFiveB.MaxPartySize = 3;
                    foreach (var c in F.Configuration.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1;
                    foreach (var d in F.Configuration.PhaseFiveB.DamageProfiles) { d.MinimumDamage = 100; d.MaximumDamage = 100; }
                    PhaseSixA4Tests.AddContent(F, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                    PhaseSixA4Tests.AddContent(F, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                    PhaseSixA4Tests.AddContent(F, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
                }
                F.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
                F.Context = new DetachedCurrentTargetValidationContext(F.Compatibility, F.Production,
                    LegacyGameplayConfigurationContract.SerializeCanonical(F.Configuration), F.Profile.Canonical,
                    F.Occupancy, livingDungeonProfiles: new[] { Profile() });
                F.Reopen();
                Writer = new DetachedCanonicalWriteAuthority(F.Production, F.Compatibility, F.Configuration,
                    F.Context, F.Profile, F.RemovalPolicy, F.Economy, acquisition: F.Acquisition,
                    branchingResearch: F.BranchingResearch, runLoot: PhaseSixA4Tests.Loot());
                Q = new Scope(Writer, F.Session);
                F.Accept(Q.Configure(F.ActivePath, F.FileSystem, F.Session, Profile().Id, 1));
            }
            internal LivingDungeonState Owner => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(F.Session.GetCurrentBytes(), F.Context).LivingDungeon;
            internal void Admit(long ordinal = 0, long time = 0)
            {
                F.Accept(Q.Install(F.ActivePath, F.FileSystem, F.Session, ordinal, time));
                F.Accept(Q.Accept(F.ActivePath, F.FileSystem, F.Session, time));
                F.Accept(Q.Advance(F.ActivePath, F.FileSystem, F.Session, 1000));
            }
            internal void Complete(int budget = 1000, bool reopen = false, long horizon = 10000)
            {
                F.Accept(Q.Accept(F.ActivePath, F.FileSystem, F.Session, horizon));
                int calls = 0;
                while (!Owner.IsClosed)
                {
                    F.Accept(Q.Advance(F.ActivePath, F.FileSystem, F.Session, budget));
                    if (reopen) F.Reopen();
                    Assert.That(++calls, Is.LessThan(1000));
                }
            }
            internal DetachedCanonicalWriteResult Settle(Scope.Request request = null) =>
                Q.Settle(F.ActivePath, F.FileSystem, F.Session, request ?? Q.Prepare(F.Session));
            internal void Save() => F.Accept(Q.Save(F.ActivePath, F.FileSystem, F.Session, F.Runtime));
            internal void Manual() => F.Accept(Q.Manual(F.ActivePath, F.FileSystem, F.Session, F.Runtime, RunPostureResolver.BalancedId, 1));
        }
        private static Harness Terminal(int parties = 1, int retention = 10)
        {
            var h = new Harness(retention);
            for (int i = 0; i < parties; i++) h.Admit(i, i);
            h.Complete(); return h;
        }
        private static byte[] OwnerBytes(Harness h, LivingDungeonState owner = null)
        { var w = new ContractJsonWriter(h.F.Profile.Canonical.Serialized); LivingDungeonContracts.Write(w, owner ?? h.Owner); return w.Finish(); }
        private static string Json(object value) => JsonUtility.ToJson(value);
        private static void Unchanged(Harness h, byte[] bytes, DetachedCanonicalWriteResult result)
        {
            Assert.That(result.IsSuccess, Is.False, result.Reason);
            Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(bytes, h.F.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(bytes, h.F.FileSystem.ReadAllBytes(h.F.ActivePath));
        }

        [Test] public void QualifiedTerminalPublishesExistingIdentityAndOneRevisionThenRetires()
        {
            var h = Terminal(2); var f = h.F; var before = h.Owner;
            var selected = LivingDungeonTerminalSettlement.Select(before, Profile());
            var request = h.Q.Prepare(f.Session); int next = f.Runtime.runHistory.NextRunSequence;
            byte[] other = before.Parties.Single(p => p.Sequence != selected.Sequence).Checkpoint;
            string scheduler = Encoding.UTF8.GetString(OwnerBytes(h)).Split(new[] { ",\"Parties\":" }, StringSplitOptions.None)[0].Split(new[] { "\"Scheduler\":" }, StringSplitOptions.None)[1];
            f.Accept(h.Settle(request));
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-" + selected.Sequence));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(next));
            Assert.That(h.Owner.Revision, Is.EqualTo(before.Revision + 1));
            Assert.That(h.Owner.Parties.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(other, h.Owner.Parties.Single().Checkpoint);
            Assert.That(Encoding.UTF8.GetString(OwnerBytes(h)), Does.Contain("\"Scheduler\":" + scheduler));
            var persisted = f.Session.GetCurrentBytes();
            Unchanged(h, persisted, h.Settle(request)); // cannot accidentally select the next terminal party
            f.Reopen(); CollectionAssert.AreEqual(persisted, f.Session.GetCurrentBytes());
        }
        [Test] public void ReopenedB2TerminalCannotAcquireProvenanceAfterTheFact()
        {
            var h = Terminal(); var before = h.F.Session.GetCurrentBytes(); h.F.Reopen();
            var fresh = new Scope(h.Writer, h.F.Session);
            Assert.That(() => fresh.Prepare(h.F.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonTerminalSettlement.Ineligible));
            CollectionAssert.AreEqual(before, h.F.FileSystem.ReadAllBytes(h.F.ActivePath));
        }
        [Test] public void FakeRequestAndForegroundAppStateCannotAuthorizeAnything()
        {
            var h = Terminal(); h.F.Runtime.lastKnownAppState = "Foreground"; h.Save();
            var p = h.Owner.Parties.Single(); var before = h.F.Session.GetCurrentBytes();
            Unchanged(h, before, h.Settle(new Scope.Request(p.Sequence, p.CheckpointIdentity)));
            Assert.That(typeof(SaveService).GetMethods().Any(m => m.Name.Contains("Settlement") || m.Name.Contains("ForegroundQualification")), Is.False);
            Assert.That(typeof(GameRoot).GetMethods().Any(m => m.Name.Contains("ForegroundQualification")), Is.False);
        }
        [Test] public void ProductionDefaultContextCannotInstallQualification()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture();
            Assert.That(() => new Scope(f.Authority, f.Session), Throws.InvalidOperationException);
        }
        [Test] public void UnfinishedHorizonRejectsBeforeEligibility()
        {
            var h = new Harness(); h.Admit();
            h.F.Accept(h.Q.Accept(h.F.ActivePath, h.F.FileSystem, h.F.Session, 10000));
            Assert.That(() => h.Q.Prepare(h.F.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonScheduling.Boundary));
        }
        [Test] public void EarliestUnqualifiedTerminalBlocksLaterQualifiedParty()
        {
            var h = Terminal(); h.Q = new Scope(h.Writer, h.F.Session); // forget run-1 provenance
            h.Admit(1, 10001); h.Complete(horizon: 20000);
            Assert.That(h.Owner.RetainedCount, Is.EqualTo(2));
            Assert.That(() => h.Q.Prepare(h.F.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonTerminalSettlement.Ineligible));
        }
        [TestCase(false)] [TestCase(true)]
        public void OrderingUsesAbsoluteCompletionAndNumericSequenceRegardlessOfEnumeration(bool equal)
        {
            var h = Terminal(2); var owner = h.Owner;
            var p1 = owner.Parties[0]; var p2 = owner.Parties[1];
            long Time(LivingDungeonParty p)
            { var e = LivingDungeonContracts.Restore(p, Profile()); try { return e.Inspect().LogicalTime; } finally { e.Abandon(); } }
            long t1 = Time(p1), t2 = Time(p2), common = Math.Max(t1, t2) + 100;
            p1.AdmittedAt = common - t1; p2.AdmittedAt = common - t2 - (equal ? 0 : 1);
            owner.Parties.Reverse();
            Assert.That(LivingDungeonTerminalSettlement.Select(owner, Profile()).Sequence, Is.EqualTo(equal ? 1 : 2));
        }
        [Test] public void AbsoluteCompletionOverflowFailsClosed()
        {
            var h = Terminal(); var owner = h.Owner; owner.Parties[0].AdmittedAt = long.MaxValue;
            Assert.That(() => LivingDungeonTerminalSettlement.Select(owner, Profile()), Throws.TypeOf<OverflowException>());
        }
        [TestCase(1)] [TestCase(2)] [TestCase(7)]
        public void SplitBudgetAndReopenGroupingsProduceByteIdenticalSettlement(int budget)
        {
            var a = Terminal(2); a.F.Accept(a.Settle()); a.F.Accept(a.Settle());
            var b = new Harness(); b.Admit(); b.Admit(1, 1); b.Complete(budget, true);
            b.F.Reopen(); b.F.Accept(b.Settle()); b.F.Reopen(); b.F.Accept(b.Settle());
            CollectionAssert.AreEqual(a.F.Session.GetCurrentBytes(), b.F.Session.GetCurrentBytes());
        }
        [Test] public void RetiredBytesAndReplayReleaseCapacityForLaterAdmission()
        {
            var h = Terminal(2); h.Admit(2, 10001); h.Admit(3, 10002); h.Complete(horizon: 20000);
            Assert.That(h.Owner.RetainedCount, Is.EqualTo(4));
            int bytes = h.F.Session.GetCurrentBytes().Length; long replay = h.Owner.Parties.Sum(p => p.NextOrdinal);
            h.F.Accept(h.Settle());
            Assert.That(h.F.Session.GetCurrentBytes().Length, Is.LessThan(bytes));
            Assert.That(h.Owner.Parties.Sum(p => p.NextOrdinal), Is.LessThan(replay));
            h.Admit(4, 20001); Assert.That(h.Owner.RetainedCount, Is.EqualTo(4));
            Assert.That(h.F.Runtime.runHistory.NextRunSequence, Is.EqualTo(6));
        }
        [Test] public void InterleavedManualAndDurableHistoryRetainsAllocatorAndRolloverRules()
        {
            var h = new Harness(2); h.Admit(); h.Manual(); h.Admit(1, 1); h.Complete();
            Assert.That(h.Owner.Parties.Select(p => p.Sequence), Is.EqualTo(new[] { 1, 3 }));
            h.Manual(); int next = h.F.Runtime.runHistory.NextRunSequence;
            h.F.Accept(h.Settle()); h.F.Accept(h.Settle());
            Assert.That(h.F.Runtime.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-1", "run-3" }));
            Assert.That(h.F.Runtime.runHistory.NextRunSequence, Is.EqualTo(next));
            Assert.That(next, Is.EqualTo(5));
            Assert.That(Json(h.F.Runtime.runHistory.LatestOutcome), Is.EqualTo(Json(h.F.Runtime.runHistory.RecentOutcomes.Last())));
        }
        [Test] public void CurrentHeatUsesResolvedDeltaThenIndependentCoolingWithoutStartOverwrite()
        {
            var h = Terminal(); var p = h.Owner.Parties.Single(); var e = LivingDungeonContracts.Restore(p, Profile());
            RunOutcomeRecord historical; RunSimulationConfig config;
            try { config = e.Snapshot.Configuration; historical = e.TakeOutcome(); } finally { e.Abandon(); }
            h.F.Runtime.structureRuntime.Heat = 47.375; h.Save();
            var expectedHeat = RunHeatStateApplyResolver.Resolve(config, 47.375, historical.RunHeatDeltaSummary);
            var expectedCooling = LootHeatCoolingResolver.Resolve(config, historical.LootExtractionSummary,
                expectedHeat.HeatAfter, historical.LootExtractionSummary.DeterministicSeed);
            h.F.Accept(h.Settle()); var outcome = h.F.Runtime.runHistory.LatestOutcome;
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(47.375));
            Assert.That(outcome.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(expectedHeat.HeatAfter));
            Assert.That(outcome.LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(expectedHeat.HeatAfter));
            Assert.That(h.F.Runtime.structureRuntime.Heat, Is.EqualTo(Math.Max(0, expectedHeat.HeatAfter + expectedCooling.AppliedHeatDelta)));
            Assert.That(Json(outcome.LootExtractionSummary), Is.EqualTo(Json(historical.LootExtractionSummary)));
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.Not.EqualTo(historical.RunHeatApplicationSummary.HeatBefore));
        }
        [Test] public void SettlementUsesCurrentHistoryRetentionRatherThanTheOlderStartBound()
        {
            var h = Terminal(retention: 3); h.Manual();
            h.F.Configuration.MaxRunHistoryEntries = 1;
            int next = h.F.Runtime.runHistory.NextRunSequence;
            h.F.Accept(h.Settle());
            Assert.That(h.F.Runtime.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-1" }));
            Assert.That(h.F.Runtime.runHistory.NextRunSequence, Is.EqualTo(next));
        }
        [Test] public void OnlyTheIndividuallyVerifiedConfiguredFirstSessionObjectiveIsCompleted()
        {
            var h = new Harness(); var f = h.F; var objective = f.Configuration.MvpFirstSessionObjective;
            // Test-owned qualification thresholds; production configuration is unchanged.
            objective.RequiredRunCount = 1; objective.RequiredRecoveredLootValue = 1;
            objective.RequiredCompletePath = true; objective.RequireResearchAnalysisUnlocked = false;
            objective.AllowedMaxHeatTierId = CurrentHeatTierResolver.ConcernTierId;
            string room = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            foreach (var item in new[] {
                (MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId, new TileCoordinate(1,1)),
                (MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId, new TileCoordinate(1,2)),
                (MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId, new TileCoordinate(0,0)) })
                f.Accept(h.Q.Edit(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    DetachedCanonicalMutationRequest.Place(item.Item1, item.Item2, room, null, item.Item3)));
            f.Runtime.completedObjectives = new CompletedObjectiveState { ObjectiveIds = new[] { "test.b3a.unrelated" },
                LastCompletedObjectiveId = "test.b3a.unrelated", LastCompletionRuleSourceId = "test.b3a.previous" };
            h.Save(); string research = Json(f.Runtime.completedResearch);
            h.Admit(); h.Complete(); double mana = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(h.Settle());
            CollectionAssert.AreEquivalent(new[] { "test.b3a.unrelated", objective.ObjectiveId }, f.Runtime.completedObjectives.ObjectiveIds);
            Assert.That(f.Runtime.completedObjectives.LastCompletionRuleSourceId,
                Is.EqualTo(CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId));
            Assert.That(Json(f.Runtime.completedResearch), Is.EqualTo(research));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
        }
        [Test] public void CompatibleSurvivorLearningUsesCanonicalRecordsAndNoFabricatedC2State()
        {
            var h = Terminal(); var f = h.F;
            string monsters = Json(f.Runtime.monsterContinuity), research = Json(f.Runtime.completedResearch);
            double mana = f.Runtime.structureRuntime.ManaReserve;
            h.F.Accept(h.Settle());
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().LastConfirmedRunId, Is.EqualTo("run-1"));
            Assert.That(f.Runtime.sharedFloorKnowledge.Records.Single().LastConfirmedRunId, Is.EqualTo("run-1"));
            Assert.That(Json(f.Runtime.monsterContinuity), Is.EqualTo(monsters));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(Json(f.Runtime.completedResearch), Is.EqualTo(research));
            Assert.That(CombatC0Diagnostics.IsCombatC2(f.Runtime.runHistory.LatestOutcome), Is.False);
        }
        [Test] public void ContentChangedBranchAndFloorCannotLearnFromMatchingTopology()
        {
            var h = Terminal(); var f = h.F; var assignment = f.Runtime.corridorContent.Assignments.First();
            f.Accept(h.Q.Edit(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                DetachedCanonicalMutationRequest.Unassign(assignment.AssignmentId)));
            string branch = Json(f.Runtime.sharedBranchKnowledge), floor = Json(f.Runtime.sharedFloorKnowledge);
            f.Accept(h.Settle());
            Assert.That(Json(f.Runtime.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(f.Runtime.sharedFloorKnowledge), Is.EqualTo(floor));
        }
        [Test] public void LaterManualLearningIsPreservedAgainstOlderDurableSnapshot()
        {
            var h = Terminal(); h.Manual();
            string branch = Json(h.F.Runtime.sharedBranchKnowledge), floor = Json(h.F.Runtime.sharedFloorKnowledge);
            h.F.Accept(h.Settle());
            Assert.That(Json(h.F.Runtime.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(h.F.Runtime.sharedFloorKnowledge), Is.EqualTo(floor));
        }
        [TestCase(Op.Write, 1)] [TestCase(Op.Write, 2)] [TestCase(Op.Replace, 1)] [TestCase(Op.Flush, 1)]
        public void FailedAtomicSettlementPublishesNothingAndProvenRollbackCanRetry(Op operation, int occurrence)
        {
            var h = Terminal(2); var f = h.F; var request = h.Q.Prepare(f.Session);
            var bytes = f.Session.GetCurrentBytes(); var runtime = f.Runtime; var session = f.Session;
            f.FileSystem.EnableFailure(operation, occurrence);
            var result = h.Settle(request); f.FileSystem.DisableFailure();
            Unchanged(h, bytes, result); Assert.That(f.Runtime, Is.SameAs(runtime)); Assert.That(f.Session, Is.SameAs(session));
            f.Accept(h.Settle(request)); Assert.That(h.Owner.RetainedCount, Is.EqualTo(1));
        }
        [Test] public void UncertainRollbackBlocksTheQualificationScope()
        {
            var h = Terminal(); var f = h.F; var request = h.Q.Prepare(f.Session);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, f.Session.GetCurrentBytes());
            f.FileSystem.EnableFailureSequence(Op.Flush, 1, Op.Replace, 2);
            var result = h.Settle(request); f.FileSystem.DisableFailure();
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(h.Settle(request).Reason, Is.EqualTo(LivingDungeonTerminalSettlement.Recovery));
        }
        [Test] public void UnobservedCanonicalWriteInvalidatesExactPrefixAuthorization()
        {
            var h = Terminal(); var f = h.F; var request = h.Q.Prepare(f.Session); var old = f.Session;
            f.Runtime.totalTicks++; f.Accept(h.Writer.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            Unchanged(h, f.Session.GetCurrentBytes(), h.Settle(request));
            var result = h.Q.Settle(f.ActivePath, f.FileSystem, old, request);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
        }
        [TestCase("ordinal")] [TestCase("checkpoint")] [TestCase("identity")]
        public void ContradictoryTerminalPinsAndOrdinalsAreRejectedByCompleteSave(string kind)
        {
            var h = Terminal(); var owner = h.Owner; var p = owner.Parties.Single();
            if (kind == "ordinal") p.NextOrdinal--;
            if (kind == "identity") p.StartIdentity = new string('0', 64);
            if (kind == "checkpoint") p.Checkpoint[p.Checkpoint.Length - 1] ^= 1;
            string before = Encoding.UTF8.GetString(h.F.Session.GetCurrentBytes());
            string old = Encoding.UTF8.GetString(OwnerBytes(h)); string bad = Encoding.UTF8.GetString(OwnerBytes(h, owner));
            var bytes = Encoding.UTF8.GetBytes(before.Replace(old, bad));
            Assert.That(DetachedCanonicalSaveSession.Open(bytes, h.F.Context, h.F.Profile).IsSuccess, Is.False);
        }
        [Test] public void GenericHistoryMutationStillCannotPublishAnAllocatedDurableIdentity()
        {
            var h = Terminal(); var f = h.F; var bytes = f.Session.GetCurrentBytes();
            var e = LivingDungeonContracts.Restore(h.Owner.Parties.Single(), Profile());
            try { f.Runtime.runHistory.AppendOutcome(e.TakeOutcome(), 10); } finally { e.Abandon(); }
            Unchanged(h, bytes, h.Writer.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
        }
        [Test] public void UnknownHistoryAndRetainedOutcomeExtensionsSurviveAlongsideCompatibleNulls()
        {
            var h = new Harness(); h.Manual(); var f = h.F;
            string text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes())
                .Replace("\"runHistory\":{", "\"runHistory\":{\"HistoryExtra\":null,\"HistoryLexical\":1.00,")
                .Replace("\"RunId\":\"run-1\"", "\"OutcomeExtra\":null,\"RunId\":\"run-1\"");
            f.FileSystem.Seed(f.ActivePath, Encoding.UTF8.GetBytes(text)); f.Reopen(); h.Q = new Scope(h.Writer, f.Session);
            h.Admit(); h.Complete(); f.Accept(h.Settle());
            string after = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            Assert.That(after, Does.Contain("\"HistoryExtra\":null"));
            Assert.That(after, Does.Contain("\"HistoryLexical\":1.00"));
            Assert.That(after, Does.Contain("\"OutcomeExtra\":null"));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
        }
        [Test] public void PartyWipeRetainsHistoricalExtractionButPublishesNoSurvivorLearning()
        {
            var h = new Harness(wipe: true); h.Admit(); h.Complete();
            var e = LivingDungeonContracts.Restore(h.Owner.Parties.Single(), Profile());
            RunOutcomeRecord outcome;
            try { outcome = e.TakeOutcome(); } finally { e.Abandon(); }
            Assert.That(outcome.Party.ActiveCount, Is.Zero);
            string branch = Json(h.F.Runtime.sharedBranchKnowledge), floor = Json(h.F.Runtime.sharedFloorKnowledge);
            h.F.Accept(h.Settle());
            Assert.That(Json(h.F.Runtime.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(h.F.Runtime.sharedFloorKnowledge), Is.EqualTo(floor));
            Assert.That(Json(h.F.Runtime.runHistory.LatestOutcome.LootExtractionSummary), Is.EqualTo(Json(outcome.LootExtractionSummary)));
        }
        [TestCase("partial")] [TestCase("readback")]
        public void PartialCandidateAndDurableReadbackFailuresDoNotRetireOrPublish(string mode)
        {
            var h = Terminal(); var f = h.F; var request = h.Q.Prepare(f.Session); var before = f.Session.GetCurrentBytes();
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(); f.FileSystem.Seed(f.ActivePath, before);
            if (mode == "partial") f.FileSystem.EnablePartialWriteFailure(10);
            else f.FileSystem.EnableTargetedFailure(Op.Read, paths => paths[0] == f.ActivePath, 4, false);
            var result = h.Settle(request); f.FileSystem.DisableFailure(); Unchanged(h, before, result);
            f.Accept(h.Settle(request)); Assert.That(h.Owner.RetainedCount, Is.Zero);
        }
        [Test] public void SettlementDoesNotCompleteUnmetObjectivesOrResearchOrGrantMana()
        {
            var h = Terminal(); var f = h.F;
            string completed = Json(f.Runtime.completedObjectives), pending = Json(f.Runtime.researchPending), progress = Json(f.Runtime.researchProgress);
            double mana = f.Runtime.structureRuntime.ManaReserve;
            f.Accept(h.Settle());
            Assert.That(Json(f.Runtime.completedObjectives), Is.EqualTo(completed));
            Assert.That(Json(f.Runtime.researchPending), Is.EqualTo(pending));
            Assert.That(Json(f.Runtime.researchProgress), Is.EqualTo(progress));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(mana));
        }
        [Test] public void ChangedObjectiveConfigurationCannotAuthorizeOldObjectiveCompletion()
        {
            var h = Terminal(); var f = h.F; string objectives = Json(f.Runtime.completedObjectives);
            f.Configuration.MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig {
                ObjectiveId = "test.b3a.changed_objective", RequiredRunCount = 1,
                RequiredRecoveredLootValue = 1, RequiredCompletePath = false };
            f.Accept(h.Settle());
            Assert.That(Json(f.Runtime.completedObjectives), Is.EqualTo(objectives));
        }
        [Test] public void ReplayAndParserLimitsRemainIndependentOfQualification()
        {
            var h = Terminal(); var f = h.F; var p = h.Owner.Parties.Single();
            var restored = new RunSimulationService(f.Configuration).RestoreCheckpoint(p.Checkpoint,
                p.CheckpointIdentity, p.StartIdentity, p.NextOrdinal, new RunCheckpointLimits(p.Checkpoint.Length, p.NextOrdinal - 1));
            Assert.That(restored.IsValid, Is.False); Assert.That(restored.FailureReason, Is.EqualTo("run.checkpoint.replay_limit"));
            restored = new RunSimulationService(f.Configuration).RestoreCheckpoint(p.Checkpoint,
                p.CheckpointIdentity, p.StartIdentity, p.NextOrdinal, new RunCheckpointLimits(p.Checkpoint.Length - 1, p.NextOrdinal));
            Assert.That(restored.IsValid, Is.False);
            var bytes = new byte[f.Profile.Canonical.Serialized.MaximumInputBytes + 1];
            Assert.That(DetachedCanonicalSaveSession.Open(bytes, f.Context, f.Profile).IsSuccess, Is.False);
        }
        [Test] public void SettlementWorkAndReleasedStorageAreMeasuredWithinProfileBounds()
        {
            var h = Terminal(2); var f = h.F; int before = f.Session.GetCurrentBytes().Length;
            long replay = h.Owner.Parties.Sum(p => p.NextOrdinal);
            var watch = Stopwatch.StartNew(); var request = h.Q.Prepare(f.Session); double selection = watch.Elapsed.TotalMilliseconds;
            watch.Restart(); f.Accept(h.Settle(request)); double transaction = watch.Elapsed.TotalMilliseconds;
            Assert.That(replay, Is.LessThanOrEqualTo(Profile().MaximumTotalReplay));
            Assert.That(before, Is.LessThanOrEqualTo(f.Profile.Whole.MaximumCandidateBytes));
            TestContext.WriteLine($"B3A retainedBefore=2 retainedAfter=1 replayEventsBefore={replay} bytesBefore={before} bytesAfter={f.Session.GetCurrentBytes().Length} selectionMs={selection:R} atomicSettlementMs={transaction:R}; includesFullValidationAndReplay=true");
        }

        [Test] public void EqualCompletionOrdersRun2BeforeRun10UsingNumericIdentity()
        {
            var h = Terminal(2); var owner = h.Owner;
            var original = LivingDungeonContracts.Restore(owner.Parties[0], Profile());
            var ten = new RunSimulationService(h.F.Configuration, PhaseSixA4Tests.Loot()).StartSnapshot(10, original.Snapshot, Timing);
            original.Abandon();
            try
            {
                while (!ten.IsTerminal) ten.AdvanceThrough(ten.NextDueTime().Value, 1, ten.NextOrdinal);
                var p10 = new LivingDungeonParty { Sequence = 10, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(p10, ten, Profile());
                var p2 = owner.Parties[1]; var two = LivingDungeonContracts.Restore(p2, Profile());
                long t2; try { t2 = two.Inspect().LogicalTime; } finally { two.Abandon(); }
                long common = Math.Max(t2, ten.Inspect().LogicalTime) + 100;
                p10.AdmittedAt = common - ten.Inspect().LogicalTime; p2.AdmittedAt = common - t2;
                owner.Parties = new System.Collections.Generic.List<LivingDungeonParty> { p10, p2 };
                Assert.That(LivingDungeonTerminalSettlement.Select(owner, Profile()).Sequence, Is.EqualTo(2));
            }
            finally { ten.Abandon(); }
        }
        [Test] public void CurrentOccupiedFloorBlocksRecoveryDuringAnOlderPartySettlement()
        {
            var h = new Harness(); var f = h.F;
            f.Accept(h.Q.Edit(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId)));
            CombatC2Tests.SetState(f, 0, true, 0); h.Q = new Scope(h.Writer, f.Session);
            h.Admit(); h.Complete(); h.Admit(1, 10001);
            f.Runtime.structureRuntime.ManaReserve = 3.25; h.Save();
            string monsters = Json(f.Runtime.monsterContinuity); long reserve = MonsterContinuity.RequiredReserve(f.State, f.Context.MonsterRecovery);
            f.Accept(h.Settle());
            Assert.That(h.Owner.RetainedCount, Is.EqualTo(1));
            Assert.That(Json(f.Runtime.monsterContinuity), Is.EqualTo(monsters));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(3.25));
            Assert.That(MonsterContinuity.RequiredReserve(f.State, f.Context.MonsterRecovery), Is.EqualTo(reserve));
        }
        [Test] public void RealWindowsAtomicReplacementRollbackRetryAndReopenAreExactlyOnce()
        {
            var h = Terminal(); var f = h.F; var request = h.Q.Prepare(f.Session);
            var fs = new FailingWindowsFileSystem();
            string directory = Path.GetFullPath(Path.Combine("TestResults", "living-dungeon-b3a-20261017", "windows", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "qualified.json");
            var before = f.Session.GetCurrentBytes(); fs.WriteAllBytesDurable(path, before); fs.FailAfterReplacement = true;
            var failed = h.Q.Settle(path, fs, f.Session, request);
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Session, Is.Null);
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before, fs.ReadAllBytes(path));
            var success = h.Q.Settle(path, fs, f.Session, request); f.Accept(success);
            var durable = fs.ReadAllBytes(path);
            var reopened = DetachedCanonicalSaveSession.Open(durable, f.Context, f.Profile);
            Assert.That(reopened.IsSuccess, Is.True);
            Assert.That(success.Validation.LivingDungeon.RetainedCount, Is.Zero);
            Assert.That(h.Q.Settle(path, fs, reopened.Session, request).IsSuccess, Is.False);
            CollectionAssert.AreEqual(durable, fs.ReadAllBytes(path));
            TestContext.WriteLine($"B3A Windows NTFS actual replacement+rollback+retry+reopen bytes={durable.Length}; path={path}");
        }
        private sealed class FailingWindowsFileSystem : ISpatialMigrationFileSystem
        {
            private readonly WindowsSpatialMigrationFileSystem inner = new WindowsSpatialMigrationFileSystem();
            internal bool FailAfterReplacement;
            public bool Exists(string path) => inner.Exists(path);
            public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
            public void WriteAllBytesDurable(string path, byte[] bytes) => inner.WriteAllBytesDurable(path, bytes);
            public void ReplaceSameDirectoryAtomic(string source, string destination)
            {
                inner.ReplaceSameDirectoryAtomic(source, destination);
                if (FailAfterReplacement) { FailAfterReplacement = false; throw new IOException("test.b3a.interrupted_after_replace"); }
            }
            public void FlushDirectory(string path) => inner.FlushDirectory(path);
            public System.Collections.Generic.IReadOnlyList<string> EnumerateFiles(string directory, string pattern, int maximum) => inner.EnumerateFiles(directory, pattern, maximum);
            public bool IsPathContainedWithoutRedirection(string directory, string path) => inner.IsPathContainedWithoutRedirection(directory, path);
            public void MoveSameDirectoryAtomic(string source, string destination) => inner.MoveSameDirectoryAtomic(source, destination);
            public void DeleteFile(string path) => inner.DeleteFile(path);
        }

        public static object CreateForPlayMode() => Terminal();
        public static string FingerprintForPlayMode(object value) => SpatialContractSha256.Compute(((Harness)value).F.Session.GetCurrentBytes());
        public static void SettleForPlayMode(object value)
        { var h = (Harness)value; var request = h.Q.Prepare(h.F.Session); h.F.Accept(h.Settle(request)); h.F.Reopen(); Unchanged(h, h.F.Session.GetCurrentBytes(), h.Settle(request)); }
    }
}
#endif
