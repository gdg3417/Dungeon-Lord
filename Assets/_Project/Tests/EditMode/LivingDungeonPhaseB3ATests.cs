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
        // Holds a real current producer immediately before its actual terminal.
        // All transitions still come from the existing source-bound authority.
        private sealed class CurrentTerminal
        {
            internal readonly Fixture F = LivingDungeonB3B2Tests.Dungeon(tickSeconds: 100000);
            internal readonly long Through;
            internal CurrentTerminal()
            {
                CurrentThrough(F, 0);
                CurrentThrough(F, F.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
                Through = LivingDungeonB3B2Tests.Finish(F);
                Assert.That(Through, Is.LessThan(100000L * F.Context.RecoveryInputs.OneFloor.UnitsPerSecond));
                CurrentThrough(F, Through - 1);
            }
            internal DetachedCanonicalWriteResult Pump(string path = null, ISpatialMigrationFileSystem fs = null,
                Action<DetachedCanonicalSaveSession, TimeProvenance> candidatePrepared = null) =>
                PhaseFourTestSupport.PumpForeground(F, F.Context.RecoveryInputs.OneFloor, Through, activePath: path,
                    fileSystem: fs, candidatePrepared: candidatePrepared);
        }
        private static void CurrentThrough(Fixture f, long through)
        {
            for (int i = 0; i < 100; i++)
            {
                f.Accept(PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.OneFloor, through));
                if (f.Runtime.livingDungeon.IsClosed && f.Runtime.livingDungeon.Scheduler.AcceptedThrough == through) return;
            }
            Assert.Fail("Current foreground fixture did not close its bounded horizon");
        }
        private static void CurrentUnchanged(Fixture f, byte[] bytes, DetachedCanonicalWriteResult result)
        {
            Assert.That(result.IsSuccess, Is.False, result.Reason);
            Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(bytes, f.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(bytes, f.FileSystem.ReadAllBytes(f.ActivePath));
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
        private static string Json(object value) => JsonUtility.ToJson(value);

        [Test] public void QualifiedTerminalPublishesExistingIdentityAndOneRevisionThenRetires()
        {
            var model = HistoricalTerminal(2); var before = model.State;
            var selected = LivingDungeonTerminalSettlement.Select(before, Profile());
            int next = model.Candidate.runHistory.NextRunSequence; long revision = before.Revision;
            byte[] other = before.Parties.Single(p => p.Sequence != selected.Sequence).Checkpoint;
            string scheduler = Encoding.UTF8.GetString(HistoricalOwnerBytes(model)).Split(new[] { ",\"Parties\":" }, StringSplitOptions.None)[0].Split(new[] { "\"Scheduler\":" }, StringSplitOptions.None)[1];
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(model.Candidate.runHistory.LatestOutcome.RunId, Is.EqualTo("run-" + selected.Sequence));
            Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(next));
            Assert.That(model.State.Revision, Is.EqualTo(revision + 1));
            Assert.That(model.State.Parties.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(other, model.State.Parties.Single().Checkpoint);
            Assert.That(Encoding.UTF8.GetString(HistoricalOwnerBytes(model)), Does.Contain("\"Scheduler\":" + scheduler));
            model.Reopen(); model.AssertSourceUnchanged();

            var current = new CurrentTerminal(); var f = current.F; var writer = f.Authority;
            var scope = new Scope(writer, f.Session); var party = f.Runtime.livingDungeon.Parties.Single();
            var stale = new Scope.Request(party.Sequence, party.CheckpointIdentity);
            next = f.Runtime.runHistory.NextRunSequence; f.Accept(current.Pump());
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-1"));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(next));
            Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            var persisted = f.Session.GetCurrentBytes();
            var rejected = scope.Settle(f.ActivePath, f.FileSystem, f.Session, stale);
            CurrentUnchanged(f, persisted, rejected);
            Assert.That(rejected.Reason, Is.EqualTo(LivingDungeonTerminalSettlement.Stale));
            f.Reopen(); CurrentThrough(f, current.Through);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }
        [Test] public void ReopenedB2TerminalCannotAcquireProvenanceAfterTheFact()
        {
            var historical = HistoricalTerminal(); var frozen = historical.FrozenReaderBytes(); historical.Reopen();
            CollectionAssert.AreEqual(frozen, historical.FrozenReaderBytes());
            Assert.That(DetachedCanonicalSaveSession.Open(frozen, historical.Source.Context, historical.Source.Profile).IsSuccess, Is.False);
            historical.AssertSourceUnchanged();
            var current = new CurrentTerminal(); var f = current.F; var before = f.Session.GetCurrentBytes(); f.Reopen();
            var fresh = new Scope(f.Authority, f.Session);
            Assert.That(() => fresh.Prepare(f.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonTerminalSettlement.Ineligible));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test] public void FakeRequestAndForegroundAppStateCannotAuthorizeAnything()
        {
            var current = new CurrentTerminal(); var f = current.F; f.Runtime.lastKnownAppState = "Foreground";
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            var p = f.Runtime.livingDungeon.Parties.Single(); var before = f.Session.GetCurrentBytes();
            var scope = new Scope(f.Authority, f.Session);
            CurrentUnchanged(f, before, scope.Settle(f.ActivePath, f.FileSystem, f.Session, new Scope.Request(p.Sequence, p.CheckpointIdentity)));
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
            var f = (Fixture)LivingDungeonPhaseB2Tests.CreateUnfinishedForPlayMode();
            var scope = new Scope(f.Authority, f.Session); var before = f.Session.GetCurrentBytes();
            Assert.That(() => scope.Prepare(f.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonScheduling.Boundary));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test] public void EarliestUnqualifiedTerminalBlocksLaterQualifiedParty()
        {
            var historical = HistoricalTerminal(); historical.Admit(1, 10001); historical.Complete(through: 20000);
            Assert.That(historical.State.RetainedCount, Is.EqualTo(2));
            Assert.That(LivingDungeonTerminalSettlement.Select(historical.State, historical.Profile).Sequence, Is.EqualTo(1));
            var frozen = historical.FrozenReaderBytes();
            // Neither later nor earlier B1 parties may acquire current authority.
            // The obsolete fixture's later-qualified current B1 state is unsupported.
            Assert.That(DetachedCanonicalSaveSession.Open(frozen, historical.Source.Context, historical.Source.Profile).IsSuccess, Is.False);
            var current = new CurrentTerminal(); var f = current.F; var scope = new Scope(f.Authority, f.Session);
            var before = f.Session.GetCurrentBytes();
            Assert.That(() => scope.Prepare(f.Session), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonTerminalSettlement.Ineligible));
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); historical.AssertSourceUnchanged();
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
            var current = new CurrentTerminal(); var f = current.F;
            var bytes = f.Session.GetCurrentBytes(); var runtime = f.Runtime; var session = f.Session;
            f.FileSystem.EnableFailure(operation, occurrence);
            var result = current.Pump();
            Assert.That(f.FileSystem.FailedOperation.Type, Is.EqualTo(operation)); f.FileSystem.DisableFailure();
            CurrentUnchanged(f, bytes, result); Assert.That(f.Runtime, Is.SameAs(runtime)); Assert.That(f.Session, Is.SameAs(session));
            f.Accept(current.Pump()); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            f.Reopen(); CurrentThrough(f, current.Through);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }
        [Test] public void UncertainRollbackBlocksTheQualificationScope()
        {
            var current = new CurrentTerminal(); var f = current.F;
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem();
            f.FileSystem.Seed(f.ActivePath, f.Session.GetCurrentBytes());
            var service = PhaseFiveBBranchIntegrationTests.CanonicalSaveService(f);
            f.FileSystem.EnableFailureSequence(Op.Flush, 1, Op.Replace, 2);
            var result = service.AdvanceForeground(f.Runtime, current.Through, f.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => { });
            f.FileSystem.DisableFailure();
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(service.RequiresCanonicalRecovery, Is.True);
            int operations = f.FileSystem.Operations.Count();
            Assert.That(service.AdvanceForeground(f.Runtime, current.Through, f.Context.RecoveryInputs.Bootstrap.tickSeconds,
                (_, __) => Assert.Fail("Uncertain authority cannot execute a tick")).Reason,
                Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations));
        }
        [Test] public void UnobservedCanonicalWriteInvalidatesExactPrefixAuthorization()
        {
            var current = new CurrentTerminal(); var f = current.F; var writer = f.Authority;
            var scope = new Scope(writer, f.Session); var old = f.Session;
            var party = f.Runtime.livingDungeon.Parties.Single(); var request = new Scope.Request(party.Sequence, party.CheckpointIdentity);
            var prior = f.Runtime.timeProvenance;
            var observation = ForegroundObservation.ForDeterministicServiceTest(old.GetCurrentBytes(), current.Through,
                f.Runtime.livingDungeon.Scheduler.AcceptedThrough, f.Runtime.livingDungeon.Scheduler.ClosedThrough,
                prior.AccountedBoundary, ForegroundObservationContext.EligibleForeground);
            var time = CanonicalTimeTransition.Foreground(old.GetCurrentBytes(), prior, observation, f.Runtime.livingDungeon);
            f.Runtime.structureRuntime.ManaReserve++;
            f.Accept(writer.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            var before = f.Session.GetCurrentBytes();
            var stale = scope.Settle(f.ActivePath, f.FileSystem, f.Session, request);
            CurrentUnchanged(f, before, stale); Assert.That(stale.Reason, Is.EqualTo(LivingDungeonTerminalSettlement.Stale));
            Assert.That(time.Matches(before, f.Runtime.timeProvenance), Is.False);
            var rejected = writer.AdvanceForeground(f.ActivePath, f.FileSystem, f.Session, f.Runtime, f.Context.RecoveryInputs.OneFloor,
                current.Through, f.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail(),
                prior.AccountedBoundary.Seconds, time);
            CurrentUnchanged(f, before, rejected);
            var result = scope.Settle(f.ActivePath, f.FileSystem, old, request);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(LivingDungeonTerminalSettlement.Stale));
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
            var current = new CurrentTerminal(); var f = current.F; var bytes = f.Session.GetCurrentBytes();
            var e = LivingDungeonContracts.Restore(f.Runtime.livingDungeon.Parties.Single(),
                f.Context.RecoveryInputs.OneFloor.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized));
            try { f.Runtime.runHistory.AppendOutcome(e.DrainToCompletion(), f.Configuration.MaxRunHistoryEntries); }
            finally { e.Abandon(); }
            CurrentUnchanged(f, bytes, f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
        }
        [Test] public void UnknownHistoryAndRetainedOutcomeExtensionsSurviveAlongsideCompatibleNulls()
        {
            var f = LivingDungeonB3B2Tests.Dungeon(tickSeconds: 100000);
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            string text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes())
                .Replace("\"runHistory\":{", "\"runHistory\":{\"HistoryExtra\":null,\"HistoryLexical\":1.00,")
                .Replace("\"RunId\":\"run-1\"", "\"OutcomeExtra\":null,\"RunId\":\"run-1\"");
            f.FileSystem.Seed(f.ActivePath, Encoding.UTF8.GetBytes(text)); f.Reopen();
            CurrentThrough(f, 0); CurrentThrough(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
            CurrentThrough(f, LivingDungeonB3B2Tests.Finish(f));
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
            var current = new CurrentTerminal(); var f = current.F; var before = f.Session.GetCurrentBytes();
            var runtime = f.Runtime; var session = f.Session; string runtimeBefore = Json(runtime);
            f.FileSystem = new Gd66DetachedSpatialMigrationTransactionTests.DeterministicFileSystem(); f.FileSystem.Seed(f.ActivePath, before);
            if (mode == "partial") f.FileSystem.EnablePartialWriteFailure(10, path => path.EndsWith(".candidate", StringComparison.Ordinal));
            else f.FileSystem.EnableTargetedFailure(Op.Read, paths => paths[0] == f.ActivePath &&
                f.FileSystem.Operations.Any(o => o.Type == Op.Replace && o.Paths[1] == f.ActivePath), 1, false);
            byte[] prepared = null; int preparations = 0;
            var result = current.Pump(candidatePrepared: (candidate, _) => { preparations++; prepared = candidate.GetCurrentBytes(); });
            Assert.That(preparations, Is.EqualTo(1)); Assert.That(prepared, Is.Not.Null);
            Assert.That(prepared.Length, Is.GreaterThan(10));
            string rollbackPath = null, candidatePath = null;
            if (mode == "partial")
            {
                var writes = f.FileSystem.Operations.Where(o => o.Type == Op.Write).ToArray();
                Assert.That(writes.Length, Is.EqualTo(2));
                Assert.That(writes[0].Index, Is.EqualTo(1)); Assert.That(writes[1].Index, Is.EqualTo(2));
                rollbackPath = writes[0].Paths.Single(); candidatePath = writes[1].Paths.Single();
                Assert.That(rollbackPath, Does.EndWith(".rollback")); Assert.That(candidatePath, Does.EndWith(".candidate"));
                Assert.That(writes.All(o => o.MutationCompleted), Is.True);
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(rollbackPath));
                CollectionAssert.AreEqual(prepared.Take(10).ToArray(), f.FileSystem.ReadAllBytes(candidatePath));
                CollectionAssert.AreEquivalent(new[] { rollbackPath, candidatePath },
                    ExactCompleteSaveAtomicPersistence.DiscoverOwnedEvidence(f.ActivePath, f.FileSystem, 64));
                Assert.That(f.FileSystem.Operations.Any(o => o.Type == Op.Replace), Is.False);
            }
            if (mode == "readback")
            {
                Assert.That(f.FileSystem.FailedOperation.Type, Is.EqualTo(Op.Read));
                Assert.That(f.FileSystem.FailedOperation.Paths[0], Is.EqualTo(f.ActivePath));
                Assert.That(f.FileSystem.FailedTargetOccurrence, Is.EqualTo(1));
            }
            f.FileSystem.DisableFailure(); CurrentUnchanged(f, before, result);
            Assert.That(f.Runtime, Is.SameAs(runtime)); Assert.That(f.Session, Is.SameAs(session));
            Assert.That(Json(f.Runtime), Is.EqualTo(runtimeBefore));
            int retryStart = f.FileSystem.Operations.Count(); f.Reopen();
            f.Accept(current.Pump()); Assert.That(f.Runtime.livingDungeon.Parties, Is.Empty);
            CollectionAssert.AreEqual(prepared, f.Session.GetCurrentBytes());
            if (mode == "partial")
            {
                var retry = f.FileSystem.Operations.Skip(retryStart).ToArray();
                int firstWrite = Array.FindIndex(retry, o => o.Type == Op.Write);
                Assert.That(firstWrite, Is.GreaterThan(0));
                var recovery = retry.Take(firstWrite).ToArray();
                Assert.That(recovery.Any(o => o.Type == Op.Read && o.Paths[0] == f.ActivePath), Is.True);
                Assert.That(recovery.Any(o => o.Type == Op.Enumerate), Is.True);
                CollectionAssert.AreEquivalent(new[] { rollbackPath, candidatePath },
                    recovery.Where(o => o.Type == Op.Delete).Select(o => o.Paths.Single()));
            }
            var durable = f.Session.GetCurrentBytes();
            f.Reopen(); CurrentThrough(f, current.Through);
            CollectionAssert.AreEqual(durable, f.Session.GetCurrentBytes());
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
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
            var source = PhaseFiveBBranchIntegrationTests.Fixture();
            PhaseSixA4Tests.AddContent(source, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            CombatC2Tests.SetState(source, 0, true, 0);
            var model = new HistoricalModel(source); model.Admit(0, 0); model.Complete(); model.Admit(1, 10001);
            model.Candidate.structureRuntime.ManaReserve = 3.25;
            string monsters = Json(model.Candidate.monsterContinuity);
            long reserve = MonsterContinuity.RequiredReserve(source.State, source.Context.MonsterRecovery);
            ApplyHistoricalConsequencesAndRetire(model);
            Assert.That(model.State.RetainedCount, Is.EqualTo(1));
            Assert.That(Json(model.Candidate.monsterContinuity), Is.EqualTo(monsters));
            Assert.That(model.Candidate.structureRuntime.ManaReserve, Is.EqualTo(3.25));
            Assert.That(MonsterContinuity.RequiredReserve(source.State, source.Context.MonsterRecovery), Is.EqualTo(reserve));
            model.AssertSourceUnchanged();

            // Actual current ordinary publication must also exclude the floor
            // occupied by an independently pinned, unconsumed C2 executable.
            var current = new CurrentTerminal(); var f = current.F;
            CombatC2Tests.SetState(f, 0, true, 3.25); monsters = Json(f.Runtime.monsterContinuity);
            reserve = MonsterContinuity.RequiredReserve(f.State, f.Context.MonsterRecovery);
            var checkpoint = f.Runtime.livingDungeon.Parties.Single().Checkpoint;
            f.Runtime.lastKnownAppState = "Foreground";
            f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            Assert.That(Json(f.Runtime.monsterContinuity), Is.EqualTo(monsters));
            Assert.That(f.Runtime.structureRuntime.ManaReserve, Is.EqualTo(3.25));
            Assert.That(MonsterContinuity.RequiredReserve(f.State, f.Context.MonsterRecovery), Is.EqualTo(reserve));
            CollectionAssert.AreEqual(checkpoint, f.Runtime.livingDungeon.Parties.Single().Checkpoint);
        }
        [Test] public void RealWindowsAtomicReplacementRollbackRetryAndReopenAreExactlyOnce()
        {
            var current = new CurrentTerminal(); var f = current.F;
            var old = f.Session; var runtime = f.Runtime;
            var fs = new FailingWindowsFileSystem();
            string directory = Path.GetFullPath(Path.Combine("TestResults", "living-dungeon-b3a-current-windows", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "qualified.json");
            var before = f.Session.GetCurrentBytes(); fs.WriteAllBytesDurable(path, before); fs.FailAfterReplacement = true;
            var failed = current.Pump(path, fs);
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Session, Is.Null);
            Assert.That(failed.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(before, fs.ReadAllBytes(path));
            var success = current.Pump(path, fs); f.Accept(success);
            var durable = fs.ReadAllBytes(path);
            var reopened = DetachedCanonicalSaveSession.Open(durable, f.Context, f.Profile);
            Assert.That(reopened.IsSuccess, Is.True);
            Assert.That(success.Validation.LivingDungeon.RetainedCount, Is.Zero);
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
            var stale = f.Authority.AdvanceForeground(path, fs, old, runtime, f.Context.RecoveryInputs.OneFloor,
                current.Through, f.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail(), runtime.lastSavedUtcUnix);
            Assert.That(stale.IsSuccess, Is.False); Assert.That(stale.Session, Is.Null);
            CollectionAssert.AreEqual(durable, fs.ReadAllBytes(path));
            File.WriteAllText(Path.Combine(directory, "ownership.json"), "{\"owner\":\"H1-F4 current B3A fixture\",\"purpose\":\"native atomic replacement rollback retry and exact reopen\",\"retention\":\"preserve qualification evidence\"}");
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

        public static object CreateForPlayMode() => new CurrentTerminal();
        public static string FingerprintForPlayMode(object value) => SpatialContractSha256.Compute(((CurrentTerminal)value).F.Session.GetCurrentBytes());
        public static void SettleForPlayMode(object value)
        {
            var current = (CurrentTerminal)value; var f = current.F; var scope = new Scope(f.Authority, f.Session);
            var party = f.Runtime.livingDungeon.Parties.Single(); var request = new Scope.Request(party.Sequence, party.CheckpointIdentity);
            f.Accept(current.Pump()); f.Reopen();
            CurrentUnchanged(f, f.Session.GetCurrentBytes(), scope.Settle(f.ActivePath, f.FileSystem, f.Session, request));
            Assert.That(f.Runtime.runHistory.RecentOutcomes.Count(r => r.RunId == "run-1"), Is.EqualTo(1));
        }
    }
}
#endif
