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
using HistoricalModel = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseB2Tests.HistoricalModel;

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
                F = PhaseFiveBBranchIntegrationTests.Fixture(author: config => {
                    config.MaxRunHistoryEntries = retention;
                    if (wipe) {
                        config.PhaseFiveB.MinPartySize = config.PhaseFiveB.MaxPartySize = 3;
                        foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1;
                        foreach (var d in config.PhaseFiveB.DamageProfiles) { d.MinimumDamage = 100; d.MaximumDamage = 100; }
                    }
                });
                if (wipe)
                {
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
        private static HistoricalModel HistoricalTerminal(int parties = 1, int retention = 10)
        {
            var model = new HistoricalModel(PhaseFiveBBranchIntegrationTests.Fixture(author: c => c.MaxRunHistoryEntries = retention));
            for (int i = 0; i < parties; i++) model.Admit(i, i);
            model.Complete(); model.FrozenReaderBytes(); model.AssertSourceUnchanged();
            return model;
        }
        // Pure historical composition only. This calls the existing selector and
        // consequence component; it has no current session, publication result,
        // request capability, persistence operation or historical producer proof.
        private static void ApplyHistoricalConsequencesAndRetire(HistoricalModel model, RunSimulationConfig currentConfig = null)
        {
            var f = model.Source; var source = f.Session.GetCurrentBytes();
            var owned = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(model.FrozenReaderBytes(),
                PhaseFourTestSupport.ProductionProfile().Canonical.ForSchema(14), new[] { model.Profile });
            var selected = LivingDungeonTerminalSettlement.Select(model.State, model.Profile);
            Assert.That(selected, Is.Not.Null);
            var execution = LivingDungeonContracts.Restore(selected, model.Profile);
            try { LivingDungeonTerminalSettlement.Apply(model.Candidate, owned, execution, execution.TakeOutcome(),
                currentConfig ?? f.Configuration, f.Production, f.Profile.Canonical); }
            finally { execution.Abandon(); }
            Assert.That(model.State.Parties.Remove(selected), Is.True);
            LivingDungeonScheduling.Change(model.State);
            CollectionAssert.AreEqual(source, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(source, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        private static RunSimulationConfig IndependentConfiguration(HistoricalModel model)
        {
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(
                Encoding.UTF8.GetString(model.Source.Context.RecoveryInputs.Identity.GetInput(0)), out var copy), Is.True);
            return copy;
        }
        private static byte[] HistoricalOwnerBytes(HistoricalModel model)
        {
            var writer = new ContractJsonWriter(PhaseFourTestSupport.ProductionProfile().Canonical.ForSchema(14).Serialized);
            LivingDungeonContracts.Write(writer, model.State); return writer.Finish();
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
            var before = f.Session.GetCurrentBytes();
            int operations = f.FileSystem.Operations.Count();
            Assert.That(() => new Scope(f.Authority, f.Session), Throws.InvalidOperationException);
            var historicalProfile = Profile();
            Assert.That(LivingDungeonForegroundConfig.IsProduction(historicalProfile.Id), Is.False);
            var unsupported = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical,
                f.Occupancy, livingDungeonProfiles: new[] { historicalProfile });
            Assert.That(() => unsupported.BindRecoveryInputs(f.Context.RecoveryInputs),
                Throws.ArgumentException.With.Message.EqualTo(RecoveryEpisode.ContextMismatch));
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations + 1), "Only this explicit verification read may touch storage");
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
            var model = HistoricalTerminal(2); var owner = model.State;
            var p1 = owner.Parties[0]; var p2 = owner.Parties[1];
            long Time(LivingDungeonParty p)
            { var e = LivingDungeonContracts.Restore(p, Profile()); try { return e.Inspect().LogicalTime; } finally { e.Abandon(); } }
            long t1 = Time(p1), t2 = Time(p2), common = Math.Max(t1, t2) + 100;
            p1.AdmittedAt = common - t1; p2.AdmittedAt = common - t2 - (equal ? 0 : 1);
            owner.Parties.Reverse();
            Assert.That(LivingDungeonTerminalSettlement.Select(owner, Profile()).Sequence, Is.EqualTo(equal ? 1 : 2));
            model.AssertSourceUnchanged();
        }
        [Test] public void AbsoluteCompletionOverflowFailsClosed()
        {
            var model = HistoricalTerminal(); var owner = model.State; owner.Parties[0].AdmittedAt = long.MaxValue;
            Assert.That(() => LivingDungeonTerminalSettlement.Select(owner, Profile()), Throws.TypeOf<OverflowException>());
            model.AssertSourceUnchanged();
        }
        [TestCase(1)] [TestCase(2)] [TestCase(7)]
        public void SplitBudgetAndReopenGroupingsProduceByteIdenticalSettlement(int budget)
        {
            var a = HistoricalTerminal(2); ApplyHistoricalConsequencesAndRetire(a); ApplyHistoricalConsequencesAndRetire(a);
            var b = new HistoricalModel(); b.Admit(0, 0); b.Admit(1, 1); b.Complete(budget, true);
            b.Reopen(); ApplyHistoricalConsequencesAndRetire(b); b.Reopen(); ApplyHistoricalConsequencesAndRetire(b);
            CollectionAssert.AreEqual(a.FrozenReaderBytes(), b.FrozenReaderBytes());
            a.AssertSourceUnchanged(); b.AssertSourceUnchanged();
        }
        [Test] public void RetiredBytesAndReplayReleaseCapacityForLaterAdmission()
        {
            var model = HistoricalTerminal(2); model.Admit(2, 10001); model.Admit(3, 10002); model.Complete(through: 20000);
            Assert.That(model.State.RetainedCount, Is.EqualTo(4));
            int bytes = model.FrozenReaderBytes().Length; long replay = model.State.Parties.Sum(p => p.NextOrdinal);
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(model.FrozenReaderBytes().Length, Is.LessThan(bytes));
            Assert.That(model.State.Parties.Sum(p => p.NextOrdinal), Is.LessThan(replay));
            model.Admit(4, 20001); Assert.That(model.State.RetainedCount, Is.EqualTo(4));
            Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(6)); model.AssertSourceUnchanged();
        }
        [Test] public void InterleavedManualAndDurableHistoryRetainsAllocatorAndRolloverRules()
        {
            var model = new HistoricalModel(PhaseFiveBBranchIntegrationTests.Fixture(author: c => c.MaxRunHistoryEntries = 2));
            model.Admit(0, 0); model.Manual(); model.Admit(1, 1); model.Complete();
            Assert.That(model.State.Parties.Select(p => p.Sequence), Is.EqualTo(new[] { 1, 3 }));
            model.Manual(); int next = model.Candidate.runHistory.NextRunSequence;
            ApplyHistoricalConsequencesAndRetire(model); ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(model.Candidate.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-1", "run-3" }));
            Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(next));
            Assert.That(next, Is.EqualTo(5));
            Assert.That(Json(model.Candidate.runHistory.LatestOutcome), Is.EqualTo(Json(model.Candidate.runHistory.RecentOutcomes.Last())));
            model.FrozenReaderBytes(); model.AssertSourceUnchanged();
        }
        [Test] public void CurrentHeatUsesResolvedDeltaThenIndependentCoolingWithoutStartOverwrite()
        {
            var model = HistoricalTerminal(); var p = model.State.Parties.Single(); var e = LivingDungeonContracts.Restore(p, Profile());
            RunOutcomeRecord historical; RunSimulationConfig config;
            try { config = e.Snapshot.Configuration; historical = e.TakeOutcome(); } finally { e.Abandon(); }
            model.Candidate.structureRuntime.Heat = 47.375;
            var expectedHeat = RunHeatStateApplyResolver.Resolve(config, 47.375, historical.RunHeatDeltaSummary);
            var expectedCooling = LootHeatCoolingResolver.Resolve(config, historical.LootExtractionSummary,
                expectedHeat.HeatAfter, historical.LootExtractionSummary.DeterministicSeed);
            ApplyHistoricalConsequencesAndRetire(model); var outcome = model.Candidate.runHistory.LatestOutcome;
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.EqualTo(47.375));
            Assert.That(outcome.RunHeatApplicationSummary.HeatAfter, Is.EqualTo(expectedHeat.HeatAfter));
            Assert.That(outcome.LootHeatCoolingSummary.HeatBeforeCooling, Is.EqualTo(expectedHeat.HeatAfter));
            Assert.That(model.Candidate.structureRuntime.Heat, Is.EqualTo(Math.Max(0, expectedHeat.HeatAfter + expectedCooling.AppliedHeatDelta)));
            Assert.That(Json(outcome.LootExtractionSummary), Is.EqualTo(Json(historical.LootExtractionSummary)));
            Assert.That(outcome.RunHeatApplicationSummary.HeatBefore, Is.Not.EqualTo(historical.RunHeatApplicationSummary.HeatBefore));
            model.FrozenReaderBytes(); model.AssertSourceUnchanged();
        }
        [Test] public void SettlementUsesCurrentHistoryRetentionRatherThanTheOlderStartBound()
        {
            var model = HistoricalTerminal(retention: 3); model.Manual();
            var independent = IndependentConfiguration(model); independent.MaxRunHistoryEntries = 1;
            int next = model.Candidate.runHistory.NextRunSequence;
            ApplyHistoricalConsequencesAndRetire(model, independent);
            Assert.That(model.Candidate.runHistory.RecentOutcomes.Select(o => o.RunId), Is.EqualTo(new[] { "run-1" }));
            Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(next)); model.AssertSourceUnchanged();
        }
        [Test] public void OnlyTheIndividuallyVerifiedConfiguredFirstSessionObjectiveIsCompleted()
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture(author: config => {
                var objective = config.MvpFirstSessionObjective;
                objective.RequiredRunCount = 1; objective.RequiredRecoveredLootValue = 1;
                objective.RequiredCompletePath = true; objective.RequireResearchAnalysisUnlocked = false;
                objective.AllowedMaxHeatTierId = CurrentHeatTierResolver.ConcernTierId;
            });
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            var model = new HistoricalModel(f); var candidate = model.Candidate;
            candidate.completedObjectives = new CompletedObjectiveState { ObjectiveIds = new[] { "test.b3a.unrelated" },
                LastCompletedObjectiveId = "test.b3a.unrelated", LastCompletionRuleSourceId = "test.b3a.previous" };
            string research = Json(candidate.completedResearch);
            model.Admit(0, 0); model.Complete(); double mana = candidate.structureRuntime.ManaReserve;
            ApplyHistoricalConsequencesAndRetire(model);
            CollectionAssert.AreEquivalent(new[] { "test.b3a.unrelated", f.Configuration.MvpFirstSessionObjective.ObjectiveId }, candidate.completedObjectives.ObjectiveIds);
            Assert.That(candidate.completedObjectives.LastCompletionRuleSourceId,
                Is.EqualTo(CompletedObjectiveStateResolver.FirstSessionObjectiveCompletionRuleSourceId));
            Assert.That(Json(candidate.completedResearch), Is.EqualTo(research));
            Assert.That(candidate.structureRuntime.ManaReserve, Is.EqualTo(mana)); model.AssertSourceUnchanged();
        }
        [Test] public void CompatibleSurvivorLearningUsesCanonicalRecordsAndNoFabricatedC2State()
        {
            var model = HistoricalTerminal(); var candidate = model.Candidate;
            string monsters = Json(candidate.monsterContinuity), research = Json(candidate.completedResearch);
            double mana = candidate.structureRuntime.ManaReserve;
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(candidate.sharedBranchKnowledge.Records.Single().LastConfirmedRunId, Is.EqualTo("run-1"));
            Assert.That(candidate.sharedFloorKnowledge.Records.Single().LastConfirmedRunId, Is.EqualTo("run-1"));
            Assert.That(Json(candidate.monsterContinuity), Is.EqualTo(monsters));
            Assert.That(candidate.structureRuntime.ManaReserve, Is.EqualTo(mana));
            Assert.That(Json(candidate.completedResearch), Is.EqualTo(research));
            Assert.That(CombatC0Diagnostics.IsCombatC2(candidate.runHistory.LatestOutcome), Is.False); model.AssertSourceUnchanged();
        }
        [Test] public void ContentChangedBranchAndFloorCannotLearnFromMatchingTopology()
        {
            var model = HistoricalTerminal(); var f = model.Source; var candidate = model.Candidate;
            var assignment = f.Runtime.corridorContent.Assignments.First();
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(assignment.AssignmentId)));
            var edited = f.Session.GetCurrentBytes();
            string branch = Json(candidate.sharedBranchKnowledge), floor = Json(candidate.sharedFloorKnowledge);
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(Json(candidate.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(candidate.sharedFloorKnowledge), Is.EqualTo(floor));
            CollectionAssert.AreEqual(edited, f.Session.GetCurrentBytes());
        }
        [Test] public void LaterManualLearningIsPreservedAgainstOlderDurableSnapshot()
        {
            var model = HistoricalTerminal(); model.Manual();
            string branch = Json(model.Candidate.sharedBranchKnowledge), floor = Json(model.Candidate.sharedFloorKnowledge);
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(Json(model.Candidate.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(model.Candidate.sharedFloorKnowledge), Is.EqualTo(floor)); model.AssertSourceUnchanged();
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
            var model = HistoricalTerminal();
            string before = Encoding.UTF8.GetString(model.FrozenReaderBytes());
            string old = Encoding.UTF8.GetString(HistoricalOwnerBytes(model));
            var p = model.State.Parties.Single();
            if (kind == "ordinal") p.NextOrdinal--;
            if (kind == "identity") p.StartIdentity = new string('0', 64);
            if (kind == "checkpoint") p.Checkpoint[p.Checkpoint.Length - 1] ^= 1;
            string bad = Encoding.UTF8.GetString(HistoricalOwnerBytes(model));
            var bytes = Encoding.UTF8.GetBytes(before.Replace(old, bad));
            Assert.That(bytes, Is.Not.EqualTo(Encoding.UTF8.GetBytes(before)), "The negative frame must contain the intended contradiction");
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(bytes,
                PhaseFourTestSupport.ProductionProfile().Canonical.ForSchema(14), new[] { model.Profile }).IsValid, Is.False);
            model.AssertSourceUnchanged();
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
            var f = PhaseFiveBBranchIntegrationTests.Fixture(author: config => {
                config.PhaseFiveB.MinPartySize = config.PhaseFiveB.MaxPartySize = 3;
                foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1;
                foreach (var d in config.PhaseFiveB.DamageProfiles) { d.MinimumDamage = 100; d.MaximumDamage = 100; }
            });
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId);
            var model = new HistoricalModel(f); model.Admit(0, 0); model.Complete();
            var e = LivingDungeonContracts.Restore(model.State.Parties.Single(), Profile());
            RunOutcomeRecord outcome;
            try { outcome = e.TakeOutcome(); } finally { e.Abandon(); }
            Assert.That(outcome.Party.ActiveCount, Is.Zero);
            string branch = Json(model.Candidate.sharedBranchKnowledge), floor = Json(model.Candidate.sharedFloorKnowledge);
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(Json(model.Candidate.sharedBranchKnowledge), Is.EqualTo(branch));
            Assert.That(Json(model.Candidate.sharedFloorKnowledge), Is.EqualTo(floor));
            Assert.That(Json(model.Candidate.runHistory.LatestOutcome.LootExtractionSummary), Is.EqualTo(Json(outcome.LootExtractionSummary)));
            model.AssertSourceUnchanged();
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
            var model = HistoricalTerminal(); var candidate = model.Candidate;
            string completed = Json(candidate.completedObjectives), pending = Json(candidate.researchPending), progress = Json(candidate.researchProgress);
            double mana = candidate.structureRuntime.ManaReserve;
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(Json(candidate.completedObjectives), Is.EqualTo(completed));
            Assert.That(Json(candidate.researchPending), Is.EqualTo(pending));
            Assert.That(Json(candidate.researchProgress), Is.EqualTo(progress));
            Assert.That(candidate.structureRuntime.ManaReserve, Is.EqualTo(mana)); model.AssertSourceUnchanged();
        }
        [Test] public void ChangedObjectiveConfigurationCannotAuthorizeOldObjectiveCompletion()
        {
            var model = HistoricalTerminal(); string objectives = Json(model.Candidate.completedObjectives);
            var independent = IndependentConfiguration(model);
            independent.MvpFirstSessionObjective = new MvpFirstSessionObjectiveConfig {
                ObjectiveId = "test.b3a.changed_objective", RequiredRunCount = 1,
                RequiredRecoveredLootValue = 1, RequiredCompletePath = false };
            ApplyHistoricalConsequencesAndRetire(model, independent);
            Assert.That(Json(model.Candidate.completedObjectives), Is.EqualTo(objectives)); model.AssertSourceUnchanged();
        }
        [Test] public void ReplayAndParserLimitsRemainIndependentOfQualification()
        {
            var model = HistoricalTerminal(); var f = model.Source; var p = model.State.Parties.Single();
            var restored = new RunSimulationService(f.Configuration).RestoreCheckpoint(p.Checkpoint,
                p.CheckpointIdentity, p.StartIdentity, p.NextOrdinal, new RunCheckpointLimits(p.Checkpoint.Length, p.NextOrdinal - 1));
            Assert.That(restored.IsValid, Is.False); Assert.That(restored.FailureReason, Is.EqualTo("run.checkpoint.replay_limit"));
            restored = new RunSimulationService(f.Configuration).RestoreCheckpoint(p.Checkpoint,
                p.CheckpointIdentity, p.StartIdentity, p.NextOrdinal, new RunCheckpointLimits(p.Checkpoint.Length - 1, p.NextOrdinal));
            Assert.That(restored.IsValid, Is.False);
            var bytes = new byte[f.Profile.Canonical.Serialized.MaximumInputBytes + 1];
            Assert.That(DetachedCanonicalSaveSession.Open(bytes, f.Context, f.Profile).IsSuccess, Is.False);
            model.AssertSourceUnchanged();
        }
        [Test] public void SettlementWorkAndReleasedStorageAreMeasuredWithinProfileBounds()
        {
            var model = HistoricalTerminal(2); int before = model.FrozenReaderBytes().Length;
            long replay = model.State.Parties.Sum(p => p.NextOrdinal);
            var watch = Stopwatch.StartNew(); var selected = LivingDungeonTerminalSettlement.Select(model.State, model.Profile);
            Assert.That(selected, Is.Not.Null); double selection = watch.Elapsed.TotalMilliseconds;
            watch.Restart(); ApplyHistoricalConsequencesAndRetire(model); double composition = watch.Elapsed.TotalMilliseconds;
            Assert.That(replay, Is.LessThanOrEqualTo(Profile().MaximumTotalReplay));
            Assert.That(before, Is.LessThanOrEqualTo(PhaseFourTestSupport.ProductionProfile().Canonical.ForSchema(14).Serialized.MaximumInputBytes));
            Assert.That(model.State.RetainedCount, Is.EqualTo(1));
            Assert.That(model.FrozenReaderBytes().Length, Is.LessThan(before)); model.AssertSourceUnchanged();
            TestContext.WriteLine($"Historical B3A retainedBefore=2 retainedAfter=1 replayEventsBefore={replay} bytesBefore={before} bytesAfter={model.FrozenReaderBytes().Length} selectionMs={selection:R} pureCompositionMs={composition:R}; includesFrozenValidationAndReplay=true; atomicPublicationMeasured=false");
        }

        [Test] public void EqualCompletionOrdersRun2BeforeRun10UsingNumericIdentity()
        {
            var model = HistoricalTerminal(2); var owner = model.State;
            var original = LivingDungeonContracts.Restore(owner.Parties[0], Profile());
            var ten = new RunSimulationService(model.Source.Configuration, model.Source.Context.RecoveryInputs.Loot).StartSnapshot(10, original.Snapshot, Timing);
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
                model.AssertSourceUnchanged();
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
