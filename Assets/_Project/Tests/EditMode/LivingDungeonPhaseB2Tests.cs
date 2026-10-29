#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using B = DungeonBuilder.M0.Tests.EditMode.LivingDungeonPhaseABaselineTests;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class LivingDungeonPhaseB2Tests
    {
        private static LivingDungeonProfile ProfileA() => LivingDungeonProfile.ParseQualificationProfile(
            File.ReadAllBytes("Assets/_Project/Data/Production/Save/living_dungeon_qualification_profile.json"),
            new SpatialSerializedInputLimits(8192, 128, 32, 2048, 8), Timing, RunPostureResolver.BalancedId,
            SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile.Canonical.ForSchema(13).Serialized);
        private static readonly RunLogicalTiming Timing = new RunLogicalTiming(3, 0, 0, 0, 3);
        private static DetachedCanonicalWriteAuthority Writer(Fixture f) => new DetachedCanonicalWriteAuthority(
            f.Production, f.Compatibility, f.Configuration, f.Context, f.Profile, f.RemovalPolicy, f.Economy,
            acquisition: f.Acquisition, branchingResearch: f.BranchingResearch, runLoot: f.Context.RecoveryInputs.Loot);
        // Current writable tests use the supported executable and the exact
        // consumed inputs. Profile A below remains a historical reader/model.
        private static Fixture Current(bool admissionContent = true)
        {
            var f = PhaseFiveBBranchIntegrationTests.Fixture();
            // Automatic demand requires a monster on the main floor and a
            // positive loot signal; optional-corridor content alone cannot
            // authorize an arrival. Place both through the canonical editor.
            if (admissionContent)
            {
                PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
                PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            }
            f.ReopenWithSupportedForegroundProfiles(); return f;
        }
        private static void CurrentThrough(Fixture f, long through)
        {
            for (int i = 0; i < 100; i++)
            {
                f.Accept(PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.OneFloor, through));
                if (Owner(f).IsClosed && Owner(f).Scheduler.AcceptedThrough == through) return;
            }
            Assert.Fail("The bounded current foreground pump did not close its accepted horizon");
        }
        private static long CurrentAdmission(Fixture f)
        {
            CurrentThrough(f, 0);
            long arrival = Owner(f).Scheduler.NextOpportunityTime.Value;
            CurrentThrough(f, arrival); return arrival;
        }
        private static byte[] Bytes(LivingDungeonState state, SpatialSerializedInputLimits limits)
        { var writer = new ContractJsonWriter(limits); LivingDungeonContracts.Write(writer, state); return writer.Finish(); }
        private static LivingDungeonState Reopen(LivingDungeonState state, LivingDungeonProfile profile)
        {
            var limits = new SpatialSerializedInputLimits(8000000, 30000, 30000, 8000000, 64);
            var bytes = Bytes(state, limits); var issues = new SpatialIssueCollector(64);
            Assert.That(ContractJson.TryParse(bytes, limits, issues, out var node), Is.True);
            Assert.That(LivingDungeonContracts.TryRead(node, new[] { profile }, out var reopened), Is.True);
            CollectionAssert.AreEqual(bytes, Bytes(reopened, limits)); return reopened;
        }
        private static RunExecution Route(int sequence, int floors = 1)
        {
            var config = B.Configuration("clear");
            foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1000000;
            // Explicit synthetic workload: one spatial room per tagged floor, no canonical inter-floor geometry.
            var rooms = Enumerable.Range(0, floors).Select(i => {
                var room = B.Room(i, true, B.Content()); room.FloorIndex = i;
                room.Spatial.FloorInstanceId = "synthetic.floor." + i; return room;
            }).ToArray();
            return new RunSimulationService(config, PhaseSixA4Tests.Loot()).StartRoute(
                new StructureRuntimeState { Heat = 9.75, ManaReserve = 100 }, 42, sequence,
                RunPostureResolver.BalancedId, rooms, Timing);
        }
        // Historical B1 retention is a pure scheduler/frozen-reader contract.
        // This fixture has no persistence or current-session publication API.
        // Its newly authored schema-14 bytes are not historical producer proof.
        internal sealed class HistoricalModel
        {
            internal readonly Fixture Source;
            internal readonly SaveData Candidate;
            internal readonly LivingDungeonProfile Profile = ProfileA();
            internal LivingDungeonState State = LivingDungeonState.Empty();
            private readonly byte[] sourceBytes;
            internal HistoricalModel(Fixture source = null)
            {
                Source = source ?? PhaseFiveBBranchIntegrationTests.Fixture();
                sourceBytes = Source.Session.GetCurrentBytes();
                var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(sourceBytes, Source.Context);
                Assert.That(owned.CurrentTargetValidated, Is.True, owned.Reason);
                Assert.That(CanonicalMvpRouteProjection.TryPublishValidated(owned, Source.Production,
                    out var candidate, out var reason), Is.True, reason);
                Candidate = candidate;
                LivingDungeonScheduling.Activate(State, Profile, 1, Candidate.runHistory);
            }
            internal void Admit(long ordinal, long time)
            {
                LivingDungeonScheduling.Install(State, ordinal, time);
                LivingDungeonScheduling.Accept(State, time);
                Advance(1000);
            }
            internal void Advance(int budget) => LivingDungeonScheduling.Advance(State, Profile, Candidate.runHistory, budget, sequence =>
            {
                var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Source.Session.GetCurrentBytes(), Source.Context);
                var snapshot = ActiveFloorRunSnapshot.Create(owned, Source.Production, Source.Configuration,
                    Source.Profile.Canonical, Source.Context.RecoveryInputs.Loot, executionInputs: Candidate);
                return new RunSimulationService(Source.Configuration, Source.Context.RecoveryInputs.Loot).StartSnapshot(sequence, snapshot, Timing);
            });
            internal void Complete(int budget = 1000, bool reopen = false, long through = 10000)
            {
                LivingDungeonScheduling.Accept(State, through);
                int calls = 0;
                while (!State.IsClosed)
                {
                    Advance(budget);
                    if (reopen) Reopen();
                    Assert.That(++calls, Is.LessThan(1000));
                }
            }
            internal byte[] FrozenReaderBytes()
            {
                var limits = Source.Profile.Canonical.ForSchema(14).Serialized;
                var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
                Assert.That(ContractJson.TryParse(PhaseFourTestSupport.FrozenFourteen(Source.Session.GetCurrentBytes()),
                    limits, issues, out var root), Is.True);
                var captured = DetachedRecognizedSaveStateSnapshot.Capture(Candidate, Source.Profile);
                Assert.That(captured.IsSuccess, Is.True, captured.Reason);
                var writer = new ContractJsonWriter(limits); writer.Node(); writer.Token("{");
                for (int i = 0; i < root.Fields.Count; i++)
                {
                    if (i != 0) writer.Token(",");
                    var field = root.Fields[i]; writer.String(field.Key); writer.Token(":");
                    if (field.Key != "primary") { DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value); continue; }
                    writer.Node(); writer.Token("{");
                    for (int j = 0; j < field.Value.Fields.Count; j++)
                    {
                        if (j != 0) writer.Token(",");
                        var member = field.Value.Fields[j]; writer.String(member.Key); writer.Token(":");
                        if (member.Key == LivingDungeonContracts.OwnerName) LivingDungeonContracts.Write(writer, State);
                        else if (member.Key == PhaseFiveSaveContracts.KnowledgeOwnerName) PhaseFiveSaveContracts.Write(writer, Candidate.sharedBranchKnowledge);
                        else if (member.Key == PhaseSixFloorKnowledge.OwnerName) PhaseSixFloorKnowledge.Write(writer, Candidate.sharedFloorKnowledge);
                        else if (captured.Snapshot.TryGet(member.Key, out var value))
                        {
                            Assert.That(ContractJson.TryParse(value, limits, new SpatialIssueCollector(limits.MaximumDiagnostics), out var node), Is.True);
                            DetachedCompleteSaveContract.WriteCanonicalNode(writer, node);
                        }
                        else DetachedCompleteSaveContract.WriteCanonicalNode(writer, member.Value);
                    }
                    writer.Token("}");
                }
                writer.Token("}"); var bytes = writer.Finish();
                var read = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(bytes,
                    Source.Profile.Canonical, new[] { Profile });
                Assert.That(read.IsValid, Is.True, read.Reason);
                Assert.That(read.CurrentTargetValidated, Is.False, "A frozen read does not confer current writable authority");
                CollectionAssert.AreEqual(bytes, read.GetBytes()); return bytes;
            }
            internal void Reopen() => State = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(
                FrozenReaderBytes(), Source.Profile.Canonical, new[] { Profile }).LivingDungeon;
            internal void AssertSourceUnchanged()
            {
                CollectionAssert.AreEqual(sourceBytes, Source.Session.GetCurrentBytes());
                CollectionAssert.AreEqual(sourceBytes, Source.FileSystem.ReadAllBytes(Source.ActivePath));
            }
        }
        private static Fixture Controlled()
        {
            return Qualify(PhaseFiveBBranchIntegrationTests.Fixture());
        }
        private static Fixture Qualify(Fixture f)
        {
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            f.Context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), f.Profile.Canonical,
                f.Occupancy, livingDungeonProfiles: new[] { ProfileA() });
            f.Reopen(); return f;
        }
        public static object CreateUnfinishedForPlayMode()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0);
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, 10000));
            f.Accept(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1));
            return f;
        }
        public static string PrefixForPlayMode(object value)
        { var f = (Fixture)value; return SpatialContractSha256.Compute(Bytes(Owner(f), f.Profile.Canonical.Serialized)); }
        public static bool ClosedForPlayMode(object value) => Owner((Fixture)value).IsClosed;
        public static void SaveLifecycleForPlayMode(object value, bool quit)
        {
            var f = (Fixture)value; var service = PhaseFiveBBranchIntegrationTests.CanonicalSaveService(f);
            service.CanonicalRuntimePublished += runtime => f.Runtime = runtime;
            if (!quit) f.Runtime.lastPausedUtcUnix = 123;
            Assert.That(service.Save(f.Runtime, quit ? SaveReason.AppQuit : SaveReason.AppPause), Is.True);
            f.Session = (DetachedCanonicalSaveSession)typeof(SaveService).GetField("_canonicalSession",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(service);
            f.Reopen();
        }
        public static object CreateRetainedLimitForPlayMode()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0); Admit(f, 1, 1); Complete(f, 1000, false);
            Admit(f, 2, 10001); Admit(f, 3, 10002); return f;
        }
        public static void ValidateForPlayMode(object value)
        {
            var f = (Fixture)value;
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context).IsValid, Is.True);
        }
        private static LivingDungeonState Owner(Fixture f) => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
            f.Session.GetCurrentBytes(), f.Context).LivingDungeon;
        private static void Activate(Fixture f) => f.Accept(Writer(f).ConfigureLivingDungeon(
            f.ActivePath, f.FileSystem, f.Session, ProfileA().Id, 1));
        private static void Admit(Fixture f, long ordinal, long time)
        {
            f.Accept(Writer(f).InstallLivingDungeonOpportunity(f.ActivePath, f.FileSystem, f.Session, ordinal, time));
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, time));
            f.Accept(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1000));
        }
        private static void Complete(Fixture f, int budget, bool restart)
        {
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, 10000));
            int writes = 0;
            while (!Owner(f).IsClosed)
            {
                f.Accept(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, budget));
                if (restart) f.Reopen(); Assert.That(++writes, Is.LessThan(1000));
            }
        }
        [Test]
        public void DormantFixtureAllocatorDiagnostics()
        {
            var f = Current(admissionContent: false);
            Assert.That(LivingDungeonScheduling.AllocatorValid(f.Runtime.runHistory, Owner(f), true), Is.True,
                UnityEngine.JsonUtility.ToJson(f.Runtime.runHistory));
            Assert.That(Owner(f).IsDormant, Is.True);
            CurrentThrough(f, 0);
            Assert.That(Owner(f).IsDormant, Is.False);
            Assert.That(Owner(f).Scheduler.ProfileId, Is.EqualTo(f.Context.RecoveryInputs.OneFloor.Id));
            string history = HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized);
            long through = f.Context.RecoveryInputs.OneFloor.MinimumArrival;
            CurrentThrough(f, through);
            Assert.That(Owner(f).Scheduler.NextOpportunityTime, Is.Null);
            Assert.That(Owner(f).Parties, Is.Empty);
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized), Is.EqualTo(history));

            // A legal edit at the closed boundary may enable future demand.
            // It cannot retroactively admit a party into the closed prefix.
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId);
            PhaseSixA4Tests.AddContent(f, 0, MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.HiddenCacheOptionId);
            Assert.That(Owner(f).Parties, Is.Empty);
            CurrentThrough(f, through);
            Assert.That(Owner(f).Scheduler.NextOpportunityTime.HasValue, Is.True);
            long arrival = Owner(f).Scheduler.NextOpportunityTime.Value;
            Assert.That(arrival, Is.GreaterThan(through));
            CurrentThrough(f, arrival);
            Assert.That(Owner(f).Parties.Single().Sequence, Is.EqualTo(1));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(2));
        }
        [Test] public void C2FundingWaitsForProvenCanonicalVacancyAtClosedB2Boundary()
        {
            var f=Controlled(); PhaseSixA4Tests.AddContent(f,0,MvpDungeonPlacementIds.MonsterCategoryId,MvpDungeonPlacementIds.GoblinOptionId);
            Activate(f); CombatC2Tests.SetState(f,0,true,0); Admit(f,0,0);
            Assert.That(Owner(f).IsClosed,Is.True);
            f.Runtime.structureRuntime.ManaReserve=3.25;
            f.Accept(Writer(f).SaveRecognizedState(f.ActivePath,f.FileSystem,f.Session,f.Runtime));
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health,Is.Zero);
            Complete(f,1000,false);
            Assert.That(f.Runtime.monsterContinuity.Records.Single().Health,Is.EqualTo(3));
            Assert.That(f.Runtime.monsterContinuity.Records.Single().RevivalRequired,Is.True);
            Assert.That(f.Runtime.structureRuntime.ManaReserve,Is.EqualTo(.25));
        }
        [Test]
        public void DormantMigrationPreservesEveryPredecessorByteAndHistoricalAllocator()
        {
            var f = Fixture.Create("\"unknownPrimary\":[null,{\"number\":1.00}],\"runHistory\":{\"NextRunSequence\":0}",
                "\"extension\":{\"lexical\":1.00}");
            var original = PhaseFourTestSupport.FrozenThirteen(f.Session.GetCurrentBytes()); var copy = (byte[])original.Clone();
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaThirteenAndRoundTrip(original, f.Profile.Canonical).IsValid, Is.True);
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(original, f.Profile.Canonical, out var current), Is.True);
            CollectionAssert.AreEqual(copy, original); CollectionAssert.AreEqual(original, PhaseFourTestSupport.FrozenThirteen(current));
            Assert.That(Owner(f).IsDormant, Is.True); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.Zero);
            Assert.That(Encoding.UTF8.GetString(current), Does.Contain("\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":0,\"Scheduler\":null,\"Parties\":[]}"));
        }
        [TestCase("livingDungeon")]
        [TestCase("LivingDungeon")]
        public void FrozenHistoricalOwnerCollisionIsPreservedAndMigrationFails(string name)
        {
            var f = Fixture.Create(null); var source = Encoding.UTF8.GetString(PhaseFourTestSupport.FrozenThirteen(f.Session.GetCurrentBytes()));
            // Unknown historical members follow recognized members. The new
            // synthetic fixture authors time/Research before its frozen owners;
            // prefix insertion would test bad ordering rather than collision.
            source = source.Replace("\"canonicalSpatialAuthority\":", "\"" + name + "\":null,\"canonicalSpatialAuthority\":");
            var bytes = Encoding.UTF8.GetBytes(source); var copy = (byte[])bytes.Clone();
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaThirteenAndRoundTrip(bytes, f.Profile.Canonical).IsValid, Is.True);
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(bytes, f.Profile.Canonical, out _), Is.False);
            CollectionAssert.AreEqual(copy, bytes);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(7)] [TestCase(1000)]
        public void CompleteSaveSplitBatchAndRestartEqualUninterruptedBytes(int budget)
        {
            var reference = new HistoricalModel(); reference.Admit(0, 0); reference.Admit(1, 1); reference.Complete();
            var split = new HistoricalModel(); split.Admit(0, 0); split.Admit(1, 1); split.Complete(budget, true);
            CollectionAssert.AreEqual(reference.FrozenReaderBytes(), split.FrozenReaderBytes());
            var owner = split.State; Assert.That(owner.RetainedCount, Is.EqualTo(2));
            Assert.That(split.Candidate.runHistory.NextRunSequence, Is.EqualTo(3));
            foreach (var p in owner.Parties)
            { var e = LivingDungeonContracts.Restore(p, ProfileA()); Assert.That(e.IsTerminal && !e.OutcomeTaken, Is.True); e.Abandon(); }
            reference.AssertSourceUnchanged(); split.AssertSourceUnchanged();
        }
        [Test]
        public void ActiveCapacityAndRetainedCapacityRemainIndependent()
        {
            var model = new HistoricalModel(); model.Admit(0, 0); model.Admit(1, 1); model.Admit(2, 2);
            Assert.That(model.State.RetainedCount, Is.EqualTo(2)); Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(3));
            model.Complete(1000, true); model.Admit(3, 10001); model.Admit(4, 10002);
            Assert.That(model.State.RetainedCount, Is.EqualTo(4));
            LivingDungeonScheduling.Accept(model.State, 20000); model.Advance(1000);
            model.Admit(5, 20001); Assert.That(model.State.RetainedCount, Is.EqualTo(4));
            Assert.That(model.Candidate.runHistory.NextRunSequence, Is.EqualTo(5));
            model.FrozenReaderBytes(); model.AssertSourceUnchanged();
        }
        [Test]
        public void UnfinishedLifecycleSavePreservesPrefixAndDefersGameplayMutation()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0);
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, 10000));
            f.Accept(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1));
            var prefix = Bytes(Owner(f), f.Profile.Canonical.Serialized); var session = f.Session;
            f.Runtime.lastPausedUtcUnix = 123; f.Runtime.lastKnownAppState = "Paused";
            f.Accept(Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            CollectionAssert.AreEqual(prefix, Bytes(Owner(f), f.Profile.Canonical.Serialized)); f.Reopen();
            Assert.That(Writer(f).InstallLivingDungeonOpportunity(f.ActivePath, f.FileSystem, f.Session, 1, 2).IsSuccess, Is.False);
            Assert.That(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)).IsSuccess, Is.False);
            Assert.That(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, session, 1).Reason,
                Is.EqualTo("living.scheduler.stale_session"));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void SharedAllocatorCannotActivateHistoricalAnomalyOrExhaustion(int next)
        {
            var f = Current(); f.Runtime.runHistory.NextRunSequence = next;
            f.Accept(Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            var before = f.Session.GetCurrentBytes();
            Assert.That(PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.OneFloor, 0).IsSuccess, Is.False);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(Owner(f).IsDormant, Is.True);
        }
        [Test]
        public void ManualAndAutomaticRunsUseOneSequenceAndManualMutationPreservesParty()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0);
            var prefix = Bytes(Owner(f), f.Profile.Canonical.Serialized);
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f)); f.Reopen();
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-2"));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(3));
            Assert.That(UnityEngine.JsonUtility.ToJson(f.Runtime.runHistory.LatestOutcome), Is.EqualTo(
                UnityEngine.JsonUtility.ToJson(f.Runtime.runHistory.RecentOutcomes.Last())));
            CollectionAssert.AreEqual(prefix, Bytes(Owner(f), f.Profile.Canonical.Serialized));
            Admit(f, 1, 1); CollectionAssert.AreEqual(new[] { 1, 3 }, Owner(f).Parties.Select(p => p.Sequence));
        }
        private static RunHistoryState AliasHistory(string scenario)
        {
            var history = new RunHistoryState { NextRunSequence = 3 };
            history.AppendOutcome(new RunOutcomeRecord { RunId = "run-1", Score = 1 }, 10);
            if (scenario != "absent")
                history.AppendOutcome(new RunOutcomeRecord { RunId = "run-2", Score = 2 }, 10);
            history.LatestOutcome = UnityEngine.JsonUtility.FromJson<RunOutcomeRecord>(
                UnityEngine.JsonUtility.ToJson(history.RecentOutcomes.Last()));
            switch (scenario)
            {
                case "absent": history.LatestOutcome = new RunOutcomeRecord { RunId = "run-2", Score = 2 }; break;
                case "earlier": history.LatestOutcome = history.RecentOutcomes[0]; break;
                case "score": history.LatestOutcome.Score++; break;
                case "tags": history.LatestOutcome.FeedbackTagKeys = new[] { "test.history.conflict" }; break;
                case "null": case "omitted": history.LatestOutcome = null; break;
                case "default": history.LatestOutcome = new RunOutcomeRecord(); break;
            }
            return history;
        }
        private static string AliasHistoryJson(string scenario)
        {
            string json = UnityEngine.JsonUtility.ToJson(AliasHistory(scenario));
            if (scenario == "null" || scenario == "omitted")
            {
                string field = "\"LatestOutcome\":" + UnityEngine.JsonUtility.ToJson(new RunOutcomeRecord()) + ",";
                Assert.That(json, Does.Contain(field));
                json = json.Replace(field, scenario == "null" ? "\"LatestOutcome\":null," : "");
            }
            return json;
        }
        [TestCase("absent")] [TestCase("earlier")] [TestCase("score")] [TestCase("tags")]
        [TestCase("null")] [TestCase("default")] [TestCase("omitted")]
        public void ActivatedAllocatorRejectsMissingOrContradictoryFinalAlias(string scenario)
        {
            var history = AliasHistory(scenario);
            string before = UnityEngine.JsonUtility.ToJson(history);
            var owner = LivingDungeonState.Empty();
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, owner, true), Is.False);
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, owner, false), Is.False);
            Assert.That(UnityEngine.JsonUtility.ToJson(history), Is.EqualTo(before));
            Assert.That(owner.IsDormant, Is.True); Assert.That(owner.Revision, Is.Zero);
        }
        [Test]
        public void ActivatedAllocatorAcceptsEqualFinalRecordAliasWithoutReferenceIdentity()
        {
            var history = AliasHistory("valid");
            Assert.That(history.LatestOutcome, Is.Not.SameAs(history.RecentOutcomes.Last()));
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, LivingDungeonState.Empty(), true), Is.True);
            var f = Current(); string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized), UnityEngine.JsonUtility.ToJson(history));
            f.FileSystem.Seed(f.ActivePath, Encoding.UTF8.GetBytes(json)); f.Reopen(); CurrentThrough(f, 0); f.Reopen();
            CurrentAdmission(f);
            Assert.That(Owner(f).Parties.Single().Sequence, Is.EqualTo(3));
            Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(4));
        }
        [TestCase(false)] [TestCase(true)]
        public void HistoricalEmptyLatestRepresentationsRemainActivatableWithoutRewritingHistory(bool defaultRecord)
        {
            var history = new RunHistoryState { LatestOutcome = defaultRecord ? new RunOutcomeRecord() : null };
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, LivingDungeonState.Empty(), true), Is.True);
            var f = Current(); string historyJson = defaultRecord ? UnityEngine.JsonUtility.ToJson(history) :
                "{\"NextRunSequence\":1,\"LatestOutcome\":null,\"RecentOutcomes\":[]}";
            string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized), historyJson);
            f.FileSystem.Seed(f.ActivePath, Encoding.UTF8.GetBytes(json)); f.Reopen(); CurrentThrough(f, 0); f.Reopen();
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized), Is.EqualTo(historyJson));
        }
        [TestCase("absent")] [TestCase("earlier")] [TestCase("score")] [TestCase("tags")]
        [TestCase("null")] [TestCase("default")] [TestCase("omitted")]
        public void DormantSchemaThirteenPreservesConflictingAliasAndActivationRejectsWithoutMutation(string scenario)
        {
            var f = Controlled(); string history = AliasHistoryJson(scenario);
            string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(
                HistoryBytes(f.Session.GetCurrentBytes(), f.Profile.Canonical.Serialized), history);
            byte[] source = PhaseFourTestSupport.FrozenThirteen(Encoding.UTF8.GetBytes(json));
            var before = (byte[])source.Clone();
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaThirteenAndRoundTrip(source, f.Profile.Canonical).IsValid, Is.True);
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(source, f.Profile.Canonical, out var migrated), Is.True);
            CollectionAssert.AreEqual(before, source);
            CollectionAssert.AreEqual(source, PhaseFourTestSupport.FrozenThirteen(migrated));
            Assert.That(HistoryBytes(migrated, f.Profile.Canonical.Serialized), Is.EqualTo(history));
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(migrated,f.Profile.Canonical,MonsterRecoveryRules.EditorFallback(),out migrated),Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(migrated, f.Context, out migrated), Is.True);
            f.FileSystem.Seed(f.ActivePath, migrated); f.Reopen(); var session = f.Session;
            var result = Writer(f).ConfigureLivingDungeon(f.ActivePath, f.FileSystem, session, ProfileA().Id, 1);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Reason, Is.EqualTo(LivingDungeonScheduling.Invalid));
            Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(migrated, session.GetCurrentBytes());
            CollectionAssert.AreEqual(migrated, f.FileSystem.ReadAllBytes(f.ActivePath));
            f.Reopen(); Assert.That(Owner(f).IsDormant, Is.True); Assert.That(Owner(f).Revision, Is.Zero);
        }
        [TestCase("absent")] [TestCase("earlier")] [TestCase("score")] [TestCase("tags")]
        [TestCase("null")] [TestCase("default")] [TestCase("omitted")]
        public void ActivatedCanonicalSaveRejectsConflictingAliasWithoutRewritingSource(string scenario)
        {
            var f = Current(); CurrentThrough(f, 0); var committed = f.Session.GetCurrentBytes();
            string json = Encoding.UTF8.GetString(committed).Replace(
                HistoryBytes(committed, f.Profile.Canonical.Serialized), AliasHistoryJson(scenario));
            byte[] candidate = Encoding.UTF8.GetBytes(json); var before = (byte[])candidate.Clone();
            f.FileSystem.Seed(f.ActivePath, candidate);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, f.Context).IsValid, Is.False);
            var opened = DetachedCanonicalSaveSession.Open(candidate, f.Context, f.Profile);
            Assert.That(opened.IsSuccess, Is.False); Assert.That(opened.Session, Is.Null);
            CollectionAssert.AreEqual(before, candidate);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
            CollectionAssert.AreEqual(committed, f.Session.GetCurrentBytes());
        }
        private static string HistoryBytes(byte[] bytes, SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            Assert.That(ContractJson.TryParse(bytes, limits, issues, out var root), Is.True);
            var writer = new ContractJsonWriter(limits);
            DetachedCompleteSaveContract.WriteCanonicalNode(writer, root.Fields[2].Value.Fields.Single(f => f.Key == "runHistory").Value);
            return Encoding.UTF8.GetString(writer.Finish());
        }
        [TestCase("1.0")] [TestCase("1e0")] [TestCase("omitted")]
        public void DormantNoncanonicalAllocatorCannotBeNormalizedByActivation(string value)
        {
            var f = Current(); string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            json = value == "omitted" ? json.Replace("\"NextRunSequence\":1,", "") :
                json.Replace("\"NextRunSequence\":1", "\"NextRunSequence\":" + value);
            byte[] source = Encoding.UTF8.GetBytes(json); f.FileSystem.Seed(f.ActivePath, source);
            var opened = DetachedCanonicalSaveSession.Open(source, f.Context, f.Profile);
            Assert.That(opened.IsSuccess, Is.True, opened.Reason); f.Session = opened.Session;
            Assert.That(Owner(f).IsDormant, Is.True);
            Assert.That(PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.OneFloor, 0).IsSuccess, Is.False);
            CollectionAssert.AreEqual(source, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void SchedulerAndOrdinaryWritesPreserveHistoricalHistoryNullsAndExtensions()
        {
            var f = Controlled(); var limits = f.Profile.Canonical.Serialized;
            const string old = "{\"NextRunSequence\":1,\"LatestOutcome\":null,\"RecentOutcomes\":null,\"legacyExtension\":{\"lexical\":1.00,\"explicitNull\":null}}";
            string json = Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace(HistoryBytes(f.Session.GetCurrentBytes(), limits), old);
            f.FileSystem.Seed(f.ActivePath, Encoding.UTF8.GetBytes(json)); f.Reopen(); Activate(f);
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), limits), Is.EqualTo(old));
            Admit(f, 0, 0); string reserved = old.Replace("\"NextRunSequence\":1", "\"NextRunSequence\":2");
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), limits), Is.EqualTo(reserved));
            f.Runtime.structureRuntime.ManaReserve++;
            f.Accept(Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), limits), Is.EqualTo(reserved));
            var spatial = f.Session.PrepareSpatialOnlyReplacement(f.State); Assert.That(spatial.IsSuccess, Is.True, spatial.Reason);
            Assert.That(HistoryBytes(spatial.Update.GetBytes(), limits), Is.EqualTo(reserved));
            f.Accept(PhaseFiveBBranchIntegrationTests.Run(f));
            Assert.That(f.Runtime.runHistory.LatestOutcome.RunId, Is.EqualTo("run-2"));
            Assert.That(HistoryBytes(f.Session.GetCurrentBytes(), limits), Does.Contain("\"legacyExtension\":{\"lexical\":1.00,\"explicitNull\":null}"));
        }
        [TestCase("{\"NextRunSequence\":1,\"RecentOutcomes\":7}")]
        [TestCase("{\"NextRunSequence\":1,\"LatestOutcome\":7}")]
        [TestCase("{\"NextRunSequence\":1,\"NextRunSequence\":2}")]
        [TestCase("{\"nextRunSequence\":1}")]
        [TestCase("{\"NextRunSequence\":3,\"RecentOutcomes\":[{\"RunId\":\"run-1\",\"RunId\":\"run-2\"}]}")]
        public void ActivatedAllocatorSourceRejectsAmbiguousOrCoercibleAuthority(string json)
        {
            var limits = new SpatialSerializedInputLimits(4096, 100, 100, 4096, 64);
            bool parsed = ContractJson.TryParse(Encoding.UTF8.GetBytes(json), limits, new SpatialIssueCollector(64), out var node);
            Assert.That(parsed && LivingDungeonScheduling.HistorySequence(node, out _), Is.False);
        }
        [TestCase(1)] [TestCase(2)]
        public void FailedAdmissionPersistencePublishesNeitherSequenceNorCheckpoint(int write)
        {
            var f = Controlled(); Activate(f);
            f.Accept(Writer(f).InstallLivingDungeonOpportunity(f.ActivePath, f.FileSystem, f.Session, 0, 0));
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, 0));
            var before = f.Session.GetCurrentBytes(); f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, write);
            var failed = Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1);
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.Session, Is.Null);
            CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); Assert.That(f.Runtime.runHistory.NextRunSequence, Is.EqualTo(1));
        }
        [TestCase("readback")] [TestCase("replace")] [TestCase("partial")] [TestCase("rollback")]
        public void AtomicFailurePreservesPublicationAndRequiresRecoveryWhenRollbackIsUnproven(string failure)
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0);
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(f.ActivePath, f.FileSystem, f.Session, 10000));
            var before = f.Session.GetCurrentBytes();
            var read = Gd66DetachedSpatialMigrationTransactionTests.OperationType.Read;
            var replace = Gd66DetachedSpatialMigrationTransactionTests.OperationType.Replace;
            var flush = Gd66DetachedSpatialMigrationTransactionTests.OperationType.Flush;
            if (failure == "readback") f.FileSystem.EnableTargetedFailure(read, paths => paths[0] == f.ActivePath, 4, false);
            if (failure == "replace") f.FileSystem.EnableTargetedFailure(replace, paths => paths[1] == f.ActivePath, 1, false);
            if (failure == "partial") f.FileSystem.EnablePartialWriteFailure(7);
            if (failure == "rollback") f.FileSystem.EnableFailureSequence(flush, 1, replace,
                f.FileSystem.Operations.Count(o => o.Type == replace) + 2);
            var result = Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Session, Is.Null); Assert.That(result.RuntimeProjection, Is.Null);
            CollectionAssert.AreEqual(before, f.Session.GetCurrentBytes());
            f.FileSystem.DisableFailure();
            if (failure == "rollback")
            {
                Assert.That(result.Reason, Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
                Assert.That(f.FileSystem.Paths.Any(path => path.Contains(".canonical-write-")), Is.True);
                Assert.That(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1).IsSuccess, Is.False);
            }
            else
            {
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath)); f.Reopen();
                f.Accept(Writer(f).AdvanceLivingDungeon(f.ActivePath, f.FileSystem, f.Session, 1));
            }
        }
        [Test]
        public void FrozenProductionBudgetsRemainDistinctFromCurrentEnvelope()
        {
            Assert.That(SpatialContractSha256.Compute(File.ReadAllBytes("Assets/_Project/Resources/save_spatial_migration_limits_schema13.json")),
                Is.EqualTo("e3ba04771aea8955afec08f52f67b7b5f124aec46d2803802d66208435c750ea"));
            var profile = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            // Schema 16 already reserves its frozen 92-byte provenance owner.
            // Schema 17 replaces that reserve with the approved 590-byte owner.
            Assert.That(profile.Canonical.ForSchema(16).Serialized.MaximumInputBytes, Is.EqualTo(1376256 + 92));
            Assert.That(profile.Canonical.Serialized.MaximumInputBytes, Is.EqualTo(1376256 + 590));
            Assert.That(profile.Canonical.ForSchema(14).Serialized.MaximumInputBytes, Is.EqualTo(655360));
            Assert.That(profile.Canonical.ForSchema(13).Serialized.MaximumInputBytes, Is.EqualTo(262144));
            Assert.That(profile.Canonical.ForSchema(12).Serialized.MaximumParsedNodes, Is.EqualTo(8192));
            var f = Fixture.Create(null, workloadProfile: profile); var old = PhaseFourTestSupport.FrozenThirteen(f.Session.GetCurrentBytes());
            Assert.That(profile.Raw.ForPayload(old).MaximumInputBytes, Is.EqualTo(524288));
            Assert.That(profile.Raw.ForPayload(PhaseFourTestSupport.FrozenFourteen(f.Session.GetCurrentBytes())).MaximumInputBytes, Is.EqualTo(1310720));
            Assert.That(profile.Raw.ForPayload(f.FrozenSixteen).MaximumInputBytes, Is.EqualTo(2752512 + 2 * 92));
            Assert.That(profile.Raw.ForPayload(f.Session.GetCurrentBytes()).MaximumInputBytes, Is.EqualTo(2752512 + 2 * 590));
            Assert.That(RawSavePayloadClassifier.Classify(old, profile.Raw, new RawSaveEnvelopeVersionContract(1, 14),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator).UnknownPrimaryMembers.Any(v => v.Name == "livingDungeon"), Is.False);
            var classified = RawSavePayloadClassifier.Classify(f.Session.GetCurrentBytes(), profile.Raw, new RawSaveEnvelopeVersionContract(1, 17),
                Gd66DetachedSpatialMigrationTransactionTests.BlankFloorForCoordinator);
            Assert.That(classified.Members.Single(m => m.Name == "livingDungeon").State, Is.EqualTo(RawSaveMemberState.NonNull));
        }
        [Test]
        public void CompatibleLargerProfilePreservesCheckpointBytes()
        {
            var model = new HistoricalModel(); model.Admit(0, 0);
            var before = model.State.Parties.Single().Checkpoint;
            var larger = new LivingDungeonProfile("synthetic.larger.v2", 20, 24, 131072, 4096, 98304, 262144, 5000000,
                Timing, RunPostureResolver.BalancedId, ProfileA().BaseBudget);
            model.FrozenReaderBytes();
            LivingDungeonScheduling.UpgradeProfile(model.State, model.Profile, larger);
            // Owner-only compatibility under explicit synthetic reader bounds.
            // This confers no current save/configuration rebind or writer profile.
            var reopened = Reopen(model.State, larger);
            CollectionAssert.AreEqual(before, reopened.Parties.Single().Checkpoint);
            Assert.That(model.Source.Context.RecoveryInputs.SupportsProfile(larger), Is.False);
            model.AssertSourceUnchanged();
        }
        [TestCase(25, 20)] [TestCase(100, 5)]
        public void SyntheticTaggedFloorScaleRetainsEngineOrderingAndReconstruction(int floors, int parties)
        {
            var profile = new LivingDungeonProfile("synthetic.scale." + floors, parties, parties, 4000000, 10000, parties * 10000L, 262144, 8000000);
            var state = new LivingDungeonState { Scheduler = new LivingDungeonScheduler { ProfileId = profile.Id,
                MinimumOpportunitySpacingUnits = 1, AcceptedThrough = 10000, ClosedThrough = -1 } };
            for (int i = 1; i <= parties; i++)
            { var e = Route(i, floors); var p = new LivingDungeonParty { Sequence = i, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(p, e, profile); state.Parties.Add(p); e.Abandon(); }
            state = Reopen(state, profile); var clone = Reopen(state, profile);
            var history = new RunHistoryState { NextRunSequence = parties + 1 };
            long initialBytes = state.Parties.Sum(p => p.Checkpoint.Length); long allocated = GC.GetAllocatedBytesForCurrentThread();
            var clock = Stopwatch.StartNew(); var reference = new List<string>();
            while (!state.IsClosed) reference.AddRange(LivingDungeonScheduling.Advance(state, profile, history, 100000, _ => null));
            double referenceMs = clock.Elapsed.TotalMilliseconds;
            var split = new List<string>(); clock.Restart();
            while (!clone.IsClosed)
            {
                var batch = LivingDungeonScheduling.Advance(clone, profile, history, 257, _ => null);
                Assert.That(batch.Count, Is.LessThanOrEqualTo(257)); split.AddRange(batch); clone = Reopen(clone, profile);
            }
            double restartMs = clock.Elapsed.TotalMilliseconds;
            CollectionAssert.AreEqual(reference, split);
            CollectionAssert.AreEqual(Bytes(state, new SpatialSerializedInputLimits(8000000, 30000, 30000, 8000000, 64)),
                Bytes(clone, new SpatialSerializedInputLimits(8000000, 30000, 30000, 8000000, 64)));
            Assert.That(state.Parties.Select(p => p.Sequence).Distinct().Count(), Is.EqualTo(parties));
            TestContext.WriteLine($"Synthetic tagged floors={floors}; parties={parties}; initialBinaryBytes={initialBytes}; events={reference.Count}; uninterruptedMs={referenceMs}; splitRestoreMs={restartMs}; allocations={GC.GetAllocatedBytesForCurrentThread() - allocated}; perCheckpointMax={state.Parties.Max(p => p.Checkpoint.Length)}; serializedTerminalOwnerBytes={Bytes(state, new SpatialSerializedInputLimits(8000000, 30000, 30000, 8000000, 64)).Length}; terminalReplayEvents={state.Parties.Sum(p => p.NextOrdinal)}; productionCompleteSaveQualified=false; canonicalFloorSnapshotsQualified=false");
            Assert.That(state.Parties.Max(p => p.Checkpoint.Length), Is.GreaterThan(ProfileA().MaximumCheckpointBytes));
        }
        [Test]
        public void TwoFloorCompleteSavePartiesRestoreActualFloorTransitions()
        {
            var inputs = PhaseFourTestSupport.AuthoredInputs((config, _) => {
                PhaseSixA4Tests.ForceDescent(config, true);
                config.BaseSuccessChance = 1; config.SuccessThreshold = 0;
                config.HeatPenaltyPerPoint = 0; config.ManaReserveBonusPerPoint = 0;
                foreach (var c in config.PhaseFiveB.Classes) c.LevelOneMaxHealth = 1000000;
            });
            var f = PhaseSixA4Tests.Eligible(inputs);
            f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            var model = new HistoricalModel(f); model.Admit(0, 0); model.Admit(1, 1); model.Complete(7, true);
            var owner = model.State; Assert.That(owner.RetainedCount, Is.EqualTo(2));
            foreach (var p in owner.Parties)
            {
                var e = LivingDungeonContracts.Restore(p, ProfileA());
                Assert.That(e.IsTerminal, Is.True); Assert.That(e.Outcome.FloorTransitions.Length, Is.EqualTo(2));
                Assert.That(e.Outcome.FloorTransitions.Count(t => t.Descend), Is.EqualTo(1));
                Assert.That(e.Outcome.FloorTransitions.Single(t => t.Descend).NextFloorInstanceId, Is.EqualTo(PhaseSixA4Tests.FloorTwo(f)));
                Assert.That(p.Checkpoint.Length, Is.LessThanOrEqualTo(ProfileA().MaximumCheckpointBytes)); e.Abandon();
            }
            model.FrozenReaderBytes(); model.AssertSourceUnchanged();
        }
        [Test]
        public void CompleteSaveProfileRetainedLimitMeasurement()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0); Admit(f, 1, 1); Complete(f, 1000, false);
            Admit(f, 2, 10001); Admit(f, 3, 10002);
            var bytes = f.Session.GetCurrentBytes(); var owner = Owner(f);
            Assert.That(owner.RetainedCount, Is.EqualTo(ProfileA().MaximumRetained));
            long allocations = GC.GetAllocatedBytesForCurrentThread(); var clock = Stopwatch.StartNew();
            for (int i = 0; i < 5; i++) Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, f.Context).IsValid, Is.True);
            double validate = clock.Elapsed.TotalMilliseconds / 5; long validationAllocations = (GC.GetAllocatedBytesForCurrentThread() - allocations) / 5;
            clock.Restart(); f.Reopen(); double reopen = clock.Elapsed.TotalMilliseconds;
            f.Runtime.lastSavedUtcUnix++; allocations = GC.GetAllocatedBytesForCurrentThread(); clock.Restart();
            f.Accept(Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            TestContext.WriteLine($"Profile A completeBytes={bytes.Length}; retained={owner.RetainedCount}; active=2; checkpointBytes={owner.Parties.Sum(p => p.Checkpoint.Length)}; replayEvents={owner.Parties.Sum(p => p.NextOrdinal)}; validateMeanMs={validate}; validateAllocations={validationAllocations}; reopenMs={reopen}; completeTransactionMs={clock.Elapsed.TotalMilliseconds}; transactionAllocations={GC.GetAllocatedBytesForCurrentThread() - allocations}; byteCeiling=655360; replayCeiling=8192; byteReplayCeilingsAreBoundsNotAttainedWorkloads=true");
        }
        [TestCase("spatial")] [TestCase("investment")] [TestCase("corridor")]
        [TestCase("branchKnowledge")] [TestCase("floorKnowledge")] [TestCase("recognizedManaResearch")]
        public void EveryCanonicalReplacementPathPreservesNonemptyPartyOwner(string path)
        {
            var f = Current(); CurrentAdmission(f);
            var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            byte[] owner = Bytes(validated.LivingDungeon, f.Profile.Canonical.Serialized);
            if (path == "recognizedManaResearch") f.Runtime.structureRuntime.ManaReserve++;
            var recognized = DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile);
            var prepared = path == "spatial" ? f.Session.PrepareSpatialOnlyReplacement(validated.State) :
                f.Session.PrepareLiveReplacement(recognized, validated.State,
                    path == "investment" ? validated.Investment : null,
                    path == "corridor" ? validated.CorridorContent : null,
                    path == "branchKnowledge" ? validated.BranchKnowledge : null,
                    path == "floorKnowledge" ? validated.FloorKnowledge : null);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            byte[] candidate = prepared.Update.GetBytes();
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(f.ActivePath, f.FileSystem,
                f.Session.GetCurrentBytes(), candidate, 64,
                b => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(b, f.Context).IsValid), Is.Null);
            var restored = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.FileSystem.ReadAllBytes(f.ActivePath), f.Context);
            CollectionAssert.AreEqual(owner, Bytes(restored.LivingDungeon, f.Profile.Canonical.Serialized));
            Assert.That(restored.LivingDungeon.Revision, Is.EqualTo(validated.LivingDungeon.Revision));
            Assert.That(DetachedCanonicalSaveSession.Open(candidate, f.Context, f.Profile).IsSuccess, Is.True);
        }
        [Test]
        public void RealWindowsMigrationAndUnfinishedPrefixReopenUseOneAtomicAuthority()
        {
            var f = Controlled(); var fs = new WindowsSpatialMigrationFileSystem();
            string directory = Path.GetFullPath(Path.Combine("TestResults", "living-dungeon-b2-windows", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "disposable-schema14.json");
            byte[] source = PhaseFourTestSupport.FrozenThirteen(f.Session.GetCurrentBytes());
            byte[] copy = (byte[])source.Clone(); fs.WriteAllBytesDurable(path, source);
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(source, f.Profile.Canonical, out var migrated), Is.True);
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, source, migrated, 64,
                b => DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(b, f.Profile.Canonical).IsValid), Is.Null);
            CollectionAssert.AreEqual(copy, source); CollectionAssert.AreEqual(migrated, fs.ReadAllBytes(path));
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(migrated,f.Profile.Canonical,MonsterRecoveryRules.EditorFallback(),out var fifteen),Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(fifteen, f.Context, out fifteen), Is.True);
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path,fs,migrated,fifteen,64,
                b=>DetachedCompleteSaveContract.ParseValidateAndRoundTrip(b,f.Context).IsValid),Is.Null);
            f.FileSystem.Seed(f.ActivePath,fifteen); f.Reopen();
            f.Accept(Writer(f).ConfigureLivingDungeon(path, fs, f.Session, ProfileA().Id, 1));
            f.Accept(Writer(f).InstallLivingDungeonOpportunity(path, fs, f.Session, 0, 0));
            f.Accept(Writer(f).AcceptLivingDungeonHorizon(path, fs, f.Session, 10000));
            f.Accept(Writer(f).AdvanceLivingDungeon(path, fs, f.Session, 2));
            var durable = fs.ReadAllBytes(path); var clock = Stopwatch.StartNew();
            var opened = DetachedCanonicalSaveSession.Open(durable, f.Context, f.Profile);
            Assert.That(opened.IsSuccess, Is.True); Assert.That(Owner(f).IsClosed, Is.False);
            CollectionAssert.AreEqual(durable, opened.Session.GetCurrentBytes());
            TestContext.WriteLine($"Windows migration/readback/unfinished-prefix reopen; durableBytes={durable.Length}; reopenMs={clock.Elapsed.TotalMilliseconds}; namespace={directory}; ownerFilesUsed=false");
        }
        [TestCase("pin")] [TestCase("ordinal")] [TestCase("format")] [TestCase("rules")] [TestCase("consumed")]
        public void UnsupportedOrContradictoryTrustedPartyFailsClosed(string mutation)
        {
            var profile = ProfileA(); var e = Route(1); var party = new LivingDungeonParty { Sequence = 1, AdmittedAt = 0 };
            if (mutation == "consumed") e.DrainToCompletion();
            LivingDungeonScheduling.Pin(party, e, profile); e.Abandon();
            var state = new LivingDungeonState { Scheduler = new LivingDungeonScheduler { ProfileId = profile.Id,
                MinimumOpportunitySpacingUnits = 1, AcceptedThrough = 10000, ClosedThrough = -1 } };
            state.Parties.Add(party);
            if (mutation == "pin") party.CheckpointIdentity = new string('0', 64);
            if (mutation == "ordinal") party.NextOrdinal++;
            if (mutation == "format" || mutation == "rules")
            {
                party.Checkpoint[mutation == "format" ? 0 : 8] ^= 1;
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    Array.Copy(hash.ComputeHash(party.Checkpoint, 0, party.Checkpoint.Length - 32), 0,
                        party.Checkpoint, party.Checkpoint.Length - 32, 32);
                party.CheckpointIdentity = SpatialContractSha256.Compute(party.Checkpoint);
            }
            Assert.That(LivingDungeonContracts.Validate(state, new[] { profile }), Is.False);
        }
        [Test]
        public void CheckpointByteAndReplayBoundariesUseExactLegalEngineBytes()
        {
            RunExecution Padded(int length)
            {
                var room = B.Room(0, true, B.Content()); room.Spatial.FloorInstanceId = new string('x', length);
                return new RunSimulationService(B.Configuration("clear"), PhaseSixA4Tests.Loot()).StartRoute(
                    new StructureRuntimeState { ManaReserve = 100 }, 42, 1, RunPostureResolver.BalancedId, new[] { room }, Timing);
            }
            var limits = ProfileA().Checkpoints; var first = Padded(1); int baseline = first.ExportCheckpoint(limits).GetBytes().Length; first.Abandon();
            var exact = Padded(1 + limits.MaximumBytes - baseline); var checkpoint = exact.ExportCheckpoint(limits);
            Assert.That(checkpoint.GetBytes().Length, Is.EqualTo(limits.MaximumBytes));
            var boundaryClock = Stopwatch.StartNew();
            Assert.That(RunCheckpointCodec.Restore(checkpoint.GetBytes(), checkpoint.Identity, checkpoint.StartIdentity, 0, limits).IsValid, Is.True);
            TestContext.WriteLine($"Exact checkpoint byte boundary bytes={checkpoint.GetBytes().Length}; replayEvents=0; restoreMs={boundaryClock.Elapsed.TotalMilliseconds}");
            exact.Abandon(); var over = Padded(2 + limits.MaximumBytes - baseline);
            Assert.That(Assert.Throws<InvalidOperationException>(() => over.ExportCheckpoint(limits)).Message, Is.EqualTo("run.checkpoint.byte_limit")); over.Abandon();
            var longRun = Route(1, 128); longRun.AdvanceThrough(1000000, 2048, 0);
            var replay = new RunCheckpointLimits(1000000, 2048); checkpoint = longRun.ExportCheckpoint(replay);
            Assert.That(checkpoint.NextOrdinal, Is.EqualTo(2048));
            boundaryClock.Restart();
            Assert.That(RunCheckpointCodec.Restore(checkpoint.GetBytes(), checkpoint.Identity, checkpoint.StartIdentity, 2048, replay).IsValid, Is.True);
            TestContext.WriteLine($"Exact replay boundary events=2048; checkpointBytes={checkpoint.GetBytes().Length}; restoreMs={boundaryClock.Elapsed.TotalMilliseconds}; syntheticLargerByteBudget=true; profileACoupledBoundaryQualified=false");
            longRun.AdvanceThrough(1000000, 1, 2048);
            Assert.That(Assert.Throws<InvalidOperationException>(() => longRun.ExportCheckpoint(replay)).Message, Is.EqualTo("run.checkpoint.replay_limit")); longRun.Abandon();
        }
        [Test]
        public void NearLimitFrozenSchemaThirteenMigratesWithReservedHeadroom()
        {
            var f = Controlled(); string original = Encoding.UTF8.GetString(PhaseFourTestSupport.FrozenThirteen(f.Session.GetCurrentBytes()));
            string ArrayValue(int characters)
            {
                var values = new List<string>();
                while (characters > 0) { int length = Math.Min(4096, characters); values.Add("\"" + new string('x', length) + "\""); characters -= length; }
                return "[" + string.Join(",", values) + "]";
            }
            // Remove exactly the final root brace; retain the existing complete primary object.
            string prefix = original.Substring(0, original.Length - 1);
            string Build(int characters, int paddingChars = 120000) => prefix.Replace("\"integrityFlags\":[]", "\"integrityFlags\":" + ArrayValue(characters)) +
                ",\"padding\":" + ArrayValue(paddingChars) + "}";
            int lo = 0, hi = 262144;
            while (lo < hi) { int mid = (lo + hi + 1) / 2; if (Encoding.UTF8.GetByteCount(Build(mid)) <= 262144) lo = mid; else hi = mid - 1; }
            string text = Build(lo); int remaining = 262144 - Encoding.UTF8.GetByteCount(text);
            if (remaining > 0) text = Build(lo, 120000 + remaining);
            var source = Encoding.UTF8.GetBytes(text); Assert.That(source.Length, Is.EqualTo(262144));
            var frozenLimits = new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(262144, 8192, 4096, 262144, 64), f.Profile.Canonical.Spatial);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaThirteenAndRoundTrip(source, frozenLimits).IsValid, Is.True);
            Assert.That(SchemaThirteenToFourteenUpgrade.TryPrepare(source, f.Profile.Canonical, out var candidate), Is.True);
            Assert.That(candidate.Length, Is.EqualTo(source.Length + 79)); CollectionAssert.AreEqual(source, PhaseFourTestSupport.FrozenThirteen(candidate));
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(candidate,f.Profile.Canonical,MonsterRecoveryRules.EditorFallback(),out candidate),Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(candidate, f.Context, out candidate), Is.True);
            // Independent complete-save parser/round-trip ceiling, using dormant synthetic extension padding.
            // This is not a claim that four active checkpoints can attain both ceilings simultaneously.
            int envelope = f.Profile.Whole.MaximumCandidateBytes;
            string currentPrefix = Encoding.UTF8.GetString(candidate);
            currentPrefix = currentPrefix.Substring(0, currentPrefix.Length - 1);
            string Envelope(int characters) => currentPrefix + ",\"ceilingPadding\":" + ArrayValue(characters) + "}";
            lo = 0; hi = envelope;
            while (lo < hi) { int mid = (lo + hi + 1) / 2; if (Encoding.UTF8.GetByteCount(Envelope(mid)) <= envelope) lo = mid; else hi = mid - 1; }
            byte[] ceiling = Encoding.UTF8.GetBytes(Envelope(lo));
            Assert.That(ceiling.Length, Is.EqualTo(envelope));
            var clock = Stopwatch.StartNew();
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(ceiling, f.Context).IsValid, Is.True);
            double validationMs = clock.Elapsed.TotalMilliseconds;
            clock.Restart(); Assert.That(DetachedCanonicalSaveSession.Open(ceiling, f.Context, f.Profile).IsSuccess, Is.True);
            TestContext.WriteLine($"Synthetic dormant complete-save ceiling bytes={ceiling.Length}; validateMs={validationMs}; reopenMs={clock.Elapsed.TotalMilliseconds}; activeReplayWorkload=false");
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(Envelope(lo + 1)), f.Context).IsValid, Is.False);
        }
        [TestCase("whitespace")] [TestCase("order")] [TestCase("version")] [TestCase("profile")]
        public void OwnerChunkAndContractContradictionsAreRejected(string mutation)
        {
            var model = new HistoricalModel(); model.Admit(0, 0); model.FrozenReaderBytes();
            var limits = model.Source.Profile.Canonical.ForSchema(14).Serialized;
            string json = Encoding.UTF8.GetString(Bytes(model.State, limits));
            if (mutation == "whitespace") json = json.Replace("\"CheckpointChunks\":[\"", "\"CheckpointChunks\":[\" ");
            if (mutation == "order") json = json.Replace("\"FormatVersion\":1,\"Revision\":", "\"Revision\":1,\"FormatVersion\":");
            if (mutation == "version") json = json.Replace("\"FormatVersion\":1", "\"FormatVersion\":2");
            if (mutation == "profile") json = json.Replace(ProfileA().Id, "unsupported.profile");
            var issues = new SpatialIssueCollector(64);
            Assert.That(ContractJson.TryParse(Encoding.UTF8.GetBytes(json), limits, issues, out var node), Is.True);
            Assert.That(LivingDungeonContracts.TryRead(node, new[] { ProfileA() }, out _), Is.False);
            model.AssertSourceUnchanged();
        }
        [Test]
        public void CollisionAndPopulatedBlankHistoryCannotEnableAllocation()
        {
            var model = new HistoricalModel(); model.Admit(0, 0); model.FrozenReaderBytes(); var owner = model.State;
            var history = new RunHistoryState { NextRunSequence = 2, LatestOutcome = new RunOutcomeRecord { RunId = "run-1" } };
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, owner, true), Is.False);
            history = new RunHistoryState { NextRunSequence = 2, LatestOutcome = new RunOutcomeRecord { Score = 1 } };
            Assert.That(LivingDungeonScheduling.AllocatorValid(history, LivingDungeonState.Empty(), true), Is.False);
            model.AssertSourceUnchanged();
        }
        [Test]
        public void DuplicateOpportunitiesNoOpsRetroactiveInstallationAndRevisionOverflowFailClosed()
        {
            var model = new HistoricalModel(); var state = model.State;
            LivingDungeonScheduling.Install(state, 0, 0);
            var limits = model.Source.Profile.Canonical.ForSchema(14).Serialized;
            var bytes = Bytes(state, limits); long revision = state.Revision;
            LivingDungeonScheduling.Install(state, 0, 0);
            Assert.That(state.Revision, Is.EqualTo(revision)); CollectionAssert.AreEqual(bytes, Bytes(state, limits));
            Assert.That(() => LivingDungeonScheduling.Install(state, 0, 1), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonScheduling.Invalid));
            LivingDungeonScheduling.Accept(state, 0); model.Advance(1000); model.FrozenReaderBytes();
            Assert.That(() => LivingDungeonScheduling.Install(state, 0, 0), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonScheduling.Invalid));
            Assert.That(() => LivingDungeonScheduling.Install(state, 1, 0), Throws.InvalidOperationException.With.Message.EqualTo(LivingDungeonScheduling.Invalid));
            state.Revision = long.MaxValue;
            Assert.Throws<OverflowException>(() => LivingDungeonScheduling.Change(state));
            model.AssertSourceUnchanged();

            // Retain the actual canonical no-op publication assertion through
            // the supported current foreground/time authority.
            var f = Current(); CurrentThrough(f, 0); bytes = f.Session.GetCurrentBytes();
            var result = PhaseFourTestSupport.PumpForeground(f, f.Context.RecoveryInputs.OneFloor, 0);
            Assert.That(result.IsSuccess && result.IsNoOp, Is.True, result.Reason);
            CollectionAssert.AreEqual(bytes, result.GetPersistedBytes());
        }
        [Test]
        public void CanonicalEditAffectsOnlyLaterPartyAndCannotResetActivatedAllocator()
        {
            var f = Controlled(); Activate(f); Admit(f, 0, 0); var before = Owner(f); byte[] checkpoint = before.Parties[0].Checkpoint;
            string roomId = f.State.Floors[0].Layout.Rooms[0].RoomInstanceId;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.GoblinOptionId,
                roomId, null, new TileCoordinate(0, 0))));
            Assert.That(Owner(f).Revision, Is.EqualTo(before.Revision)); CollectionAssert.AreEqual(checkpoint, Owner(f).Parties[0].Checkpoint);
            Admit(f, 1, 1); var after = Owner(f); Assert.That(after.Parties[0].StartIdentity, Is.Not.EqualTo(after.Parties[1].StartIdentity));
            CollectionAssert.AreEqual(checkpoint, after.Parties[0].Checkpoint);
            var oldBytes = f.Session.GetCurrentBytes(); f.Runtime.runHistory.NextRunSequence = 1;
            Assert.That(Writer(f).SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime).IsSuccess, Is.False);
            CollectionAssert.AreEqual(oldBytes, f.FileSystem.ReadAllBytes(f.ActivePath));
        }
        [Test]
        public void SameTimeTerminalReleaseAdmissionAndNewbornOrderSurvivesEveryActionRestart()
        {
            var profile = new LivingDungeonProfile("synthetic.same-time.v1", 2, 3, 65536, 2048, 6144, 262144, 300000);
            var e = Route(1); var steps = new List<RunStep>();
            while (!e.IsTerminal) steps.AddRange(e.AdvanceThrough(e.NextDueTime().Value, 1, e.NextOrdinal));
            long terminalTime = steps.Last().LogicalTime; e.Abandon();
            var state = new LivingDungeonState { Scheduler = new LivingDungeonScheduler { ProfileId = profile.Id,
                MinimumOpportunitySpacingUnits = 1, AcceptedThrough = 10000, ClosedThrough = -1,
                NextOpportunityOrdinal = 2, LastOpportunityTime = 0, NextOpportunityTime = terminalTime } };
            for (int i = 1; i <= 2; i++) { e = Route(i); var p = new LivingDungeonParty { Sequence = i, AdmittedAt = 0 }; LivingDungeonScheduling.Pin(p, e, profile); state.Parties.Add(p); e.Abandon(); }
            var history = new RunHistoryState { NextRunSequence = 3 }; var trace = new List<string>();
            while (!state.IsClosed)
            { trace.AddRange(LivingDungeonScheduling.Advance(state, profile, history, 1, sequence => Route(sequence))); state = Reopen(state, profile); }
            int opportunity = trace.IndexOf(terminalTime + ":opportunity:2");
            Assert.That(opportunity, Is.GreaterThan(trace.IndexOf(terminalTime + ":run-1:" + steps.Last().Ordinal)));
            Assert.That(opportunity, Is.GreaterThan(trace.IndexOf(terminalTime + ":run-2:" + steps.Last().Ordinal)));
            Assert.That(opportunity, Is.LessThan(trace.IndexOf(terminalTime + ":run-3:0")));
            Assert.That(history.NextRunSequence, Is.EqualTo(4)); Assert.That(state.Parties.Count, Is.EqualTo(3));
        }
    }
}
#endif
