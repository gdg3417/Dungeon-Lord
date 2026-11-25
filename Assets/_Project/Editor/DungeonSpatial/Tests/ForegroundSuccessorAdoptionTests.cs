#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using DungeonBuilder.M0.Tests.EditMode;
using NUnit.Framework;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using Metrics = DungeonBuilder.M0.ForegroundQualificationMetrics;
using Op = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests.OperationType;
using Successor = DungeonBuilder.M0.Gameplay.DungeonSpatial.DetachedCanonicalWriteAuthority.ForegroundSuccessor;

namespace DungeonBuilder.M0.Editor.Tests
{
    public sealed class ForegroundSuccessorAdoptionTests
    {
        private const long Epoch = 1794000000;
        private const long PassiveBoundary = 40000;
        private const long AfterMovement = 41001;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private bool metricsEnabled, metricsDetailed;

        [SetUp] public void EnableMetrics()
        {
            metricsEnabled = Metrics.Enabled; metricsDetailed = Metrics.DetailedEnabled;
            Metrics.Enabled = Metrics.DetailedEnabled = true;
        }

        [TearDown] public void RestoreMetrics()
        {
            Metrics.Reset(); Metrics.Enabled = metricsEnabled; Metrics.DetailedEnabled = metricsDetailed;
        }

        private static ForegroundObservation Observe(SaveService save, SaveData current, long through, UnixTimePoint wall)
        {
            var scheduler = current.livingDungeon.Scheduler;
            return ForegroundObservation.ForDeterministicServiceTest(save.CanonicalSession.GetCurrentBytes(), through,
                scheduler?.AcceptedThrough ?? -1, scheduler?.ClosedThrough ?? -1, wall, ForegroundObservationContext.EligibleForeground);
        }

        private static UnixTimePoint WallAt(long units) =>
            new UnixTimePoint(Epoch + units / 1000, (int)(units % 1000) * 10000);

        private static Successor Prepare(SaveService save, SaveData current, ForegroundObservation observation,
            int tickSeconds, Action<SaveData, long> tick)
        {
            Assert.That(save.PrepareForegroundSuccessor(current, observation, tickSeconds, tick, out var successor), Is.Null);
            Assert.That(successor, Is.Not.Null);
            return successor;
        }

        private static void RequireSuccess(DetachedCanonicalWriteResult result) =>
            Assert.That(result.IsSuccess, Is.True, result.Reason);

        private static void RequireOneWriteAndReadback()
        {
            Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.EqualTo(1));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SemanticReadback], Is.EqualTo(1));
        }

        [TestCase(false, "56b0162e63a753dec62af0e5805a0a4fbe6d5d04344cab07b85060b90a03c392",
            "257b0fa7409f6b44b60dfa08206fb71064bee56917918971503bcb00392ccdfc")]
        [TestCase(true, "60fd5295ea07f306729a3c4c6b1b8488ac4d361cd88a269678621ff2472c5e5a",
            "f349fdd39221da29e4c47f8aac731fb7668b65479225e3fc498ada36cea8b776")]
        public void MatchesOriginalWholeSaveAtTickAndAfterReopen(bool twoFloors, string expectedTick, string expectedFinal)
        {
            // Exact pre-refactor normal-path bytes from 83c97f08 on main 1a477ed9.
            var f = PassiveSeed(twoFloors);
            var inputs = f.Context.RecoveryInputs;
            int seconds = inputs.Bootstrap.tickSeconds;
            var service = LivingDungeonB3B2Tests.Service(f, new PairedForegroundObservationTests.PreciseClock { Now = WallAt(0) });
            SaveData current = f.Runtime;
            int publications = 0, ticks = 0;
            long ordinalAtTick = -1;
            service.CanonicalRuntimePublished += next => {
                Assert.That(service.ForegroundWriting, Is.True);
                CollectionAssert.AreEqual(service.CanonicalSession.GetCurrentBytes(), f.FileSystem.ReadAllBytes(f.ActivePath));
                current = next; publications++;
            };
            using var passive = new PassiveTick(f);
            void Tick(SaveData candidate, long tick)
            {
                ordinalAtTick = candidate.livingDungeon.Parties.Single().NextOrdinal;
                Assert.That(tick, Is.EqualTo(4));
                ticks++; passive.Apply(candidate, tick);
            }

            Metrics.Reset();
            foreach (long through in new long[] { 31000, 34000, 35000, 36000, 37000, 38000, 39000 })
                RequireSuccess(service.AdvanceForeground(current, Observe(service, current, through, WallAt(through)), seconds, Tick));
            Assert.That(publications, Is.EqualTo(7));
            Assert.That(ticks, Is.Zero);
            Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.EqualTo(7));
            Assert.That(Metrics.BoundaryCalls[(int)Metrics.Boundary.SemanticReadback], Is.EqualTo(7));
            Metrics.Reset();
            RequireSuccess(service.AdvanceForeground(current, Observe(service, current, PassiveBoundary, WallAt(PassiveBoundary)), seconds, Tick));
            RequireOneWriteAndReadback();
            Assert.That(publications, Is.EqualTo(8));
            Assert.That(ordinalAtTick, Is.EqualTo(8));
            Assert.That(current.livingDungeon.Parties.Single().NextOrdinal, Is.EqualTo(9));
            Assert.That(current.researchProgress.CompletionPending, Is.True);
            Assert.That(current.structureRuntime.ManaReserve, Is.EqualTo(twoFloors ? 0.6666666666666666 : 0.5));
            byte[] tickBytes = service.CanonicalSession.GetCurrentBytes();
            AssertCurrentBytesMatchSchemaSeventeenGolden(f, tickBytes, expectedTick);
            Assert.That(current.timeProvenance.AccountedBoundary, Is.EqualTo(WallAt(PassiveBoundary)));

            service = LivingDungeonB3B2Tests.Service(f, new PairedForegroundObservationTests.PreciseClock { Now = WallAt(PassiveBoundary) });
            current = f.Runtime;
            CollectionAssert.AreEqual(tickBytes, service.CanonicalSession.GetCurrentBytes());
            service.CanonicalRuntimePublished += next => { current = next; publications++; };
            foreach (long through in new long[] { 41000, AfterMovement })
                RequireSuccess(service.AdvanceForeground(current, Observe(service, current, through, WallAt(through)), seconds, Tick));
            Assert.That(publications, Is.EqualTo(10));
            Assert.That(ticks, Is.EqualTo(1));
            Assert.That(current.totalTicks, Is.EqualTo(4));
            Assert.That(current.livingDungeon.Parties.Single().NextOrdinal, Is.EqualTo(10));
            Assert.That(current.researchProgress.ProgressUnits, Is.EqualTo(1.05).Within(1e-12));
            Assert.That(current.structureRuntime.ManaReserve, Is.EqualTo(twoFloors ? 0.6666666666666666 : 0.5));
            byte[] finalBytes = service.CanonicalSession.GetCurrentBytes();
            AssertCurrentBytesMatchSchemaSeventeenGolden(f, finalBytes, expectedFinal);
            CollectionAssert.AreEqual(finalBytes, f.FileSystem.ReadAllBytes(f.ActivePath));
        }

        private static void AssertCurrentBytesMatchSchemaSeventeenGolden(Fixture fixture, byte[] current,
            string expectedHistoricalHash)
        {
            var validated = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(current, fixture.Context);
            Assert.That(validated.CurrentTargetValidated, Is.True, validated.Reason);
            CollectionAssert.AreEqual(current, validated.GetBytes());

            // The deterministic fixture rebuilds migrated spatial state. Its
            // migration descriptor includes the exact production package identity.
            var authority = validated.State.Authority;
            Assert.That(authority.CreationKind, Is.EqualTo(CanonicalSpatialCreationKind.Migrated));
            Assert.That(authority.MigrationTransactionId,
                Is.EqualTo("gd66-392edc8eaaf51de04709bd3bacdffd547d79052dd9a96b77089a4e6c59d2be1b"));
            Assert.That(authority.MigrationDescriptorFingerprint,
                Is.EqualTo("2a6930acd0236e76377f2109b137a4bca3e21ff12785927fe484447d18aac9f2"));
            const string historicalTransaction = "gd66-6e016749a5c97b3b88a6791148d93126a36c36625dfc72c77e7b05624c92bd0c";
            const string historicalDescriptor = "33ef7de6b8babab6b28840102b16bfd068be66c8eb479db863fdb9b04e433a32";

            // Compare known schema, spatial wire and production-input identities.
            // The complete-save golden still covers every other byte, including
            // the captured runs, their checkpoint identities, and Research timing.
            const int frozenSchema = CanonicalSaveSchemaVersions.AtomicRecoveryIntroduction;
            Assert.That(validated.State.Floors.SelectMany(floor => floor.Layout.Edges)
                .All(edge => edge.AuthoredGeometry == null), Is.True,
                "The historical fixture may only contain inferred edges");
            // The exact qualified baseline and Amendment A packs differ in the
            // catalog and registration components of the twelve-input identity.
            const string historicalContext = "67559c631dc9057834888a010fd117faed7b1dcce9d4566c026d5fc0ecab354b";
            const string nativeContext = "deeec93232377bf9110ee595fb28f766a8c6162e986871a0024ad4e08e1f4f4a";
            Assert.That(validated.TimeProvenance.RecoveryContextSha256, Is.EqualTo(nativeContext));
            Assert.That(validated.TimeProvenance.Episode, Is.Null);
            var limits = fixture.Profile.Canonical;
            var frozen = CanonicalSpatialSaveSerializer.SerializeMembers(validated.State, limits, frozenSchema);
            Assert.That(frozen.IsValid, Is.True);
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            Assert.That(ContractJson.TryParse(current, limits.Serialized, issues, out var root), Is.True);
            Assert.That(root.Fields.Single(field => field.Key == "schemaVersion").Value.Text,
                Is.EqualTo("19"));
            var writer = new ContractJsonWriter(limits.Serialized);
            writer.Node(); writer.Token("{");
            for (int index = 0; index < root.Fields.Count; index++)
            {
                if (index != 0) writer.Token(",");
                var field = root.Fields[index];
                writer.String(field.Key); writer.Token(":");
                if (field.Key == "schemaVersion") writer.Token("17");
                else if (field.Key == "primary")
                {
                    writer.Node(); writer.Token("{");
                    for (int memberIndex = 0; memberIndex < field.Value.Fields.Count; memberIndex++)
                    {
                        if (memberIndex != 0) writer.Token(",");
                        var member = field.Value.Fields[memberIndex];
                        writer.String(member.Key); writer.Token(":");
                        if (member.Key == "spatialFloors") writer.Token(Encoding.UTF8.GetString(frozen.Value.Floors));
                        else if (member.Key == "canonicalSpatialAuthority")
                        {
                            // Preserve version, kind and member order from the
                            // validated authority. Substitute only the two proven IDs.
                            member.Value.Fields.Single(f => f.Key == nameof(CanonicalSpatialAuthorityMarker.MigrationTransactionId))
                                .Value.Text = historicalTransaction;
                            member.Value.Fields.Single(f => f.Key == nameof(CanonicalSpatialAuthorityMarker.MigrationDescriptorFingerprint))
                                .Value.Text = historicalDescriptor;
                            DetachedCompleteSaveContract.WriteCanonicalNode(writer, member.Value);
                        }
                        else if (member.Key == TimeProvenance.Owner)
                            new TimeProvenance(validated.TimeProvenance.AccountedBoundary, historicalContext, null).Write(writer);
                        else DetachedCompleteSaveContract.WriteCanonicalNode(writer, member.Value);
                    }
                    writer.Token("}");
                }
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
            }
            writer.Token("}");
            byte[] projected = writer.Finish();
            Assert.That(SpatialContractSha256.Compute(projected), Is.EqualTo(expectedHistoricalHash));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompleteObservationBeyondTickPublishesOnceAndLeavesLaterElapsedTimeForNextCut(bool twoFloors)
        {
            var f = PassiveSeed(twoFloors);
            using var passive = new PassiveTick(f);
            var seed = f.Session.GetCurrentBytes();
            var inputs = f.Context.RecoveryInputs;
            var referenceCut = ForegroundObservation.ForDeterministicServiceTest(seed, AfterMovement, 30000, 30000,
                WallAt(AfterMovement), ForegroundObservationContext.EligibleForeground);
            var transition = CanonicalTimeTransition.Foreground(seed, f.Runtime.timeProvenance, referenceCut, f.Runtime.livingDungeon);
            var reference = PhaseSixA4Tests.Writer(f).AdvanceForeground(f.ActivePath, f.FileSystem, f.Session, f.Runtime, inputs.TwoFloor,
                AfterMovement, inputs.Bootstrap.tickSeconds, passive.Apply, referenceCut.WallEndpoint.Seconds, transition);
            RequireSuccess(reference);
            f.FileSystem.Seed(f.ActivePath, seed);

            var wall = new PairedForegroundObservationTests.PreciseClock { Now = WallAt(0) };
            double monotonic = 0;
            var service = LivingDungeonB3B2Tests.Service(f, wall, out var time, () => monotonic);
            // Capture between logical units and verify the resulting full horizon.
            monotonic = 11.0015;
            wall.Now = WallAt(AfterMovement);
            var cut = time.CaptureForegroundObservation(service.CanonicalSession);
            Assert.That(cut.CapturedThrough, Is.EqualTo(AfterMovement));
            SaveData current = f.Runtime;
            int publications = 0, ticks = 0;
            service.CanonicalRuntimePublished += next => { current = next; publications++; };
            Metrics.Reset();
            RequireSuccess(service.AdvanceForeground(current, cut, inputs.Bootstrap.tickSeconds, (candidate, tick) => {
                Assert.That(candidate.livingDungeon.Parties.Single().NextOrdinal, Is.EqualTo(8));
                passive.Apply(candidate, tick); ticks++;
                monotonic = 11.5;
                wall.Now = WallAt(41500);
            }));
            RequireOneWriteAndReadback();
            Assert.That(ticks, Is.EqualTo(1)); Assert.That(publications, Is.EqualTo(1));
            Assert.That(current.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(AfterMovement));
            Assert.That(current.timeProvenance.AccountedBoundary, Is.EqualTo(cut.WallEndpoint));
            CollectionAssert.AreEqual(reference.GetPersistedBytes(), service.CanonicalSession.GetCurrentBytes());
            var nextCut = time.CaptureForegroundObservation(service.CanonicalSession);
            Assert.That(nextCut.CapturedThrough, Is.EqualTo(41500));
            Assert.That(nextCut.WallEndpoint, Is.EqualTo(wall.Now));
        }

        [Test]
        public void AcceptedDebtClosesBeforeNewObservationCanBeAccepted()
        {
            var f = LivingDungeonTwoFloorTests.Dungeon(oneBoundary: true);
            var service = LivingDungeonB3B2Tests.Service(f, new PairedForegroundObservationTests.PreciseClock());
            SaveData current = f.Runtime;
            service.CanonicalRuntimePublished += next => current = next;
            int ticks = 0;
            void Tick(SaveData _, long ordinal) { Assert.That(ordinal, Is.EqualTo(++ticks)); }
            RequireSuccess(service.AdvanceForeground(current, Observe(service, current, 20000, new UnixTimePoint(1001, 0)), 10, Tick));
            Assert.That(current.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(10000));
            var cut = Observe(service, current, 30000, new UnixTimePoint(1002, 0));
            using var candidate = Prepare(service, current, cut, 10, Tick);
            RequireSuccess(service.AdoptForegroundSuccessor(candidate));
            Assert.That(current.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(20000));
            Assert.That(current.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(20000));
            Assert.That(current.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1001, 0)));
            Assert.That(ticks, Is.EqualTo(2));
        }

        [TestCase("abandoned")]
        [TestCase("duplicate")]
        [TestCase("predecessor")]
        [TestCase("filesystem")]
        [TestCase("foreign_service")]
        [TestCase("configuration")]
        [TestCase("lifecycle")]
        public void StaleOrInvalidSuccessorHasNoAuthority(string invalidation)
        {
            var f = LivingDungeonTwoFloorTests.Dungeon();
            var wall = new PairedForegroundObservationTests.PreciseClock { Now = f.Runtime.timeProvenance.AccountedBoundary };
            var service = LivingDungeonB3B2Tests.Service(f, wall, out var time, () => 0);
            SaveData current = f.Runtime;
            int publications = 0;
            service.CanonicalRuntimePublished += next => { current = next; publications++; };
            var cut = time.CaptureForegroundObservation(service.CanonicalSession);
            var originalSession = service.CanonicalSession;
            var originalRuntime = current;
            int operations = f.FileSystem.Operations.Count();
            Metrics.Reset();
            using var candidate = Prepare(service, current, cut, 10, (_, __) => Assert.Fail("zero-horizon tick"));
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations));
            Assert.That(publications, Is.Zero);
            Assert.That(service.CanonicalSession, Is.SameAs(originalSession));
            Assert.That(current, Is.SameAs(originalRuntime));
            Assert.That(time.AttachedSaveForTests, Is.SameAs(originalRuntime));

            switch (invalidation) {
                case "abandoned": candidate.Dispose(); break;
                case "duplicate": RequireSuccess(service.AdoptForegroundSuccessor(candidate)); break;
                case "predecessor": RequireSuccess(service.AdvanceForeground(current, cut, 10, (_, __) => Assert.Fail())); break;
                case "filesystem":
                    var writer = PhaseSixA4Tests.Writer(f);
                    f.Runtime.lastKnownAppState = "Paused";
                    RequireSuccess(writer.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
                    break;
                case "foreign_service":
                    service = LivingDungeonB3B2Tests.Service(f, wall); current = f.Runtime;
                    break;
                case "configuration": f.Context.RecoveryInputs.Bootstrap.tickSeconds++; break;
                case "lifecycle": time.OnPause(); time.OnResume(); break;
            }

            int beforeCallbacks = publications;
            var predecessor = service.CanonicalSession;
            byte[] active = f.FileSystem.ReadAllBytes(f.ActivePath);
            var runtime = current;
            operations = f.FileSystem.Operations.Count();
            Metrics.Reset();
            try {
                var refused = service.AdoptForegroundSuccessor(candidate);
                Assert.That(refused.IsSuccess, Is.False);
                if (invalidation == "configuration") Assert.That(refused.Reason, Is.EqualTo(RecoveryEpisode.ContextMismatch));
                Assert.That(refused.Session, Is.Null); Assert.That(refused.RuntimeProjection, Is.Null);
                var io = f.FileSystem.Operations.Skip(operations).Select(operation => operation.Type);
                if (invalidation == "filesystem") CollectionAssert.AreEqual(new[] { Op.Read }, io);
                else Assert.That(io, Is.Empty);
                Assert.That(Metrics.Calls[(int)Metrics.Area.AtomicWrite], Is.Zero);
                Assert.That(publications, Is.EqualTo(beforeCallbacks));
                Assert.That(service.CanonicalSession, Is.SameAs(predecessor));
                Assert.That(current, Is.SameAs(runtime));
                CollectionAssert.AreEqual(active, f.FileSystem.ReadAllBytes(f.ActivePath));
                Assert.That(service.RequiresCanonicalRecovery || service.CanonicalPublicationPending, Is.False);
            } finally {
                if (invalidation == "configuration") f.Context.RecoveryInputs.Bootstrap.tickSeconds--;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SettlementFaultPreservesConfirmationAndResolvesExactlyOnce(bool uncertain)
        {
            var f = LivingDungeonB3B2Tests.Dungeon(tickSeconds: 100000);
            var inputs = f.Context.RecoveryInputs;
            f.Accept(PhaseFourTestSupport.PumpForeground(f, inputs.OneFloor, 0));
            f.Accept(PhaseFourTestSupport.PumpForeground(f, inputs.OneFloor, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value));
            long terminal = LivingDungeonB3B2Tests.Finish(f);
            f.Accept(PhaseFourTestSupport.PumpForeground(f, inputs.OneFloor, terminal - 1));
            var before = f.Session.GetCurrentBytes();
            var wall = new PairedForegroundObservationTests.PreciseClock { Now = f.Runtime.timeProvenance.AccountedBoundary };
            var service = LivingDungeonB3B2Tests.Service(f, wall);
            var predecessor = service.CanonicalSession;
            var original = f.Runtime;
            SaveData current = original;
            int publications = 0;
            service.CanonicalRuntimePublished += next => { current = next; publications++; };
            var cut = Observe(service, current, terminal, wall.Now);
            using var candidate = Prepare(service, current, cut, inputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail("unexpected tick"));
            if (uncertain) {
                int priorReplacements = f.FileSystem.Operations.Count(operation => operation.Type == Op.Replace);
                f.FileSystem.EnableFailureSequence(Op.Flush, 1, Op.Replace, priorReplacements + 2);
            }
            else f.FileSystem.EnableFailure(Op.Flush, 1);
            var failed = service.AdoptForegroundSuccessor(candidate);
            f.FileSystem.DisableFailure();
            Assert.That(failed.Reason, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason :
                DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(service.RequiresCanonicalRecovery, Is.EqualTo(uncertain));
            Assert.That(service.CanonicalPublicationPending, Is.EqualTo(uncertain));
            Assert.That(publications, Is.Zero);
            Assert.That(current, Is.SameAs(original));
            Assert.That(service.CanonicalSession, Is.SameAs(predecessor));
            Assert.That(current.livingDungeon.Parties.Single().Sequence, Is.EqualTo(1));
            int operations = f.FileSystem.Operations.Count();
            Assert.That(service.AdoptForegroundSuccessor(candidate).IsSuccess, Is.False);
            Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations));

            if (uncertain) {
                Assert.That(service.AdvanceForeground(current, cut, inputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail()).Reason,
                    Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
                Assert.That(service.Save(current, SaveReason.AppPause), Is.False);
                Assert.That(f.FileSystem.Operations.Count(), Is.EqualTo(operations));
                RequireSuccess(service.ResolveUncertainRecoveryPublication());
            } else {
                CollectionAssert.AreEqual(before, f.FileSystem.ReadAllBytes(f.ActivePath));
                RequireSuccess(service.AdvanceForeground(current, cut, inputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail()));
            }
            Assert.That(publications, Is.EqualTo(1));
            Assert.That(current.runHistory.RecentOutcomes.Count(run => run.RunId == "run-1"), Is.EqualTo(1));
            Assert.That(current.livingDungeon.Parties, Is.Empty);
            var after = service.CanonicalSession.GetCurrentBytes();

            service = LivingDungeonB3B2Tests.Service(f, wall);
            current = f.Runtime;
            CollectionAssert.AreEqual(after, service.CanonicalSession.GetCurrentBytes());
            RequireSuccess(service.AdvanceForeground(current, Observe(service, current, terminal, wall.Now),
                inputs.Bootstrap.tickSeconds, (_, __) => Assert.Fail()));
            CollectionAssert.AreEqual(after, service.CanonicalSession.GetCurrentBytes());
        }

        private static Fixture PassiveSeed(bool twoFloors)
        {
            var f = LivingDungeonTwoFloorTests.ProductionDungeon(Epoch);
            if (twoFloors)
                f.Accept(PhaseSixA4Tests.Writer(f).CommitFloorLifecycle(f.ActivePath, f.FileSystem, f.Session, f.Runtime,
                    FloorLifecycleAction.Activate, PhaseSixA4Tests.FloorTwo(f)));
            LivingDungeonTwoFloorTests.Through(f, 0);
            LivingDungeonTwoFloorTests.Through(f, f.Runtime.livingDungeon.Scheduler.NextOpportunityTime.Value);
            Assert.That(f.State.Floors.Count(x => x.ActivationState == FloorActivationState.Active), Is.EqualTo(twoFloors ? 2 : 1));
            f.Accept(PhaseSixA4Tests.Writer(f).SaveQaMana(f.ActivePath, f.FileSystem, f.Session, f.Runtime, false));
            var bootstrap = f.Context.RecoveryInputs.Bootstrap;
            var pending = bootstrap.researchPendingScaffold;
            var progress = bootstrap.researchProgressScaffold;
            f.Runtime.researchPending = new ResearchPendingState { SlotId = pending.slotId, ProjectId = pending.projectId };
            f.Runtime.researchProgress = new ResearchProgressState {
                SlotId = pending.slotId, ProjectId = pending.projectId, RuleSourceIdUsed = progress.ruleSourceId,
                ProgressUnits = bootstrap.researchCompletionEligibilityScaffold.requiredProgressUnits -
                    progress.progressPerActiveSecond * bootstrap.tickSeconds / 2
            };
            var prepared = f.Session.PrepareLiveReplacement(DetachedRecognizedSaveStateSnapshot.Capture(f.Runtime, f.Profile), f.State);
            Assert.That(prepared.IsSuccess, Is.True, prepared.Reason);
            f.FileSystem.Seed(f.ActivePath, prepared.Update.GetBytes());
            f.Reopen();
            return f;
        }

        private sealed class PassiveTick : IDisposable
        {
            private readonly GameObject host = new GameObject("Foreground successor tick fixture");
            internal readonly Action<SaveData, long> Apply;

            internal PassiveTick(Fixture f)
            {
                host.SetActive(false);
                try {
                    var inputs = f.Context.RecoveryInputs;
                    var root = host.AddComponent<GameRoot>();
                    var content = new ContentService();
                    content.BindRecoveryBootstrap(inputs);
                    typeof(GameRoot).GetProperty("Content").SetValue(root, content);
                    typeof(GameRoot).GetField("recoveryRuntimeInputs", Private).SetValue(root, inputs);
                    typeof(GameRoot).GetField("_configuredSimulationTickSeconds", Private).SetValue(root, inputs.Bootstrap.tickSeconds);
                    typeof(GameRoot).GetField("_restrictedActionGate", Private).SetValue(root, new RestrictedActionGateService());
                    typeof(GameRoot).GetField("_passiveManaService", Private).SetValue(root, new CanonicalPassiveManaService(
                        inputs.Passive, inputs.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, inputs.Bootstrap.tickSeconds));
                    Apply = (Action<SaveData, long>)Delegate.CreateDelegate(typeof(Action<SaveData, long>), root,
                        typeof(GameRoot).GetMethod("ApplyForegroundPassiveTick", Private));
                } catch { Dispose(); throw; }
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
#endif
