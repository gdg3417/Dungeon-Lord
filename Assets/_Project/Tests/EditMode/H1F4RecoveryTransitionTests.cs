#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class H1F4RecoveryTransitionTests : H1F4RecoveryContractTests
    {
        // Explicit absence, rather than Unity's synthetic default-class encoding.
        // These are test-produced historical inputs, never producer evidence for
        // an arbitrary production save.
        static Fixture HistoricalFixture() => Fixture.Create("\"researchPending\":null,\"researchProgress\":null");
        internal static RecoveryRuntimeInputs Bundle(Fixture f, byte[][] bytes = null) => new RecoveryRuntimeInputs(bytes ?? Inputs(),
            f.Production, ProductionSpatialContentWorkloadLimitParser.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits, f.Profile.Canonical);
        [Test] public void ConsumedBundleConstructsEveryRecoveryDependencyFromRetainedBytes()
        {
            var f = HistoricalFixture(); var inputs = Inputs(); var bundle = Bundle(f, inputs);
            var context = HistoricalContext(f, bundle);
            inputs[0][0] ^= 1;
            Assert.That(bundle.Identity.Digest, Is.EqualTo(new RecoveryContextIdentity(Inputs()).Digest));
            Assert.That(bundle.Configuration.CombatC0, Is.Not.Null); Assert.That(bundle.Bootstrap.tickSeconds, Is.EqualTo(10));
            Assert.That(bundle.TwoFloor.TwoFloor, Is.True); Assert.That(bundle.OneFloor.TwoFloor, Is.False);
            Assert.That(bundle.ResearchValid(f.Runtime), Is.True);
            Assert.That(bundle.ResearchValid(new SaveData { researchPending = new ResearchPendingState(),
                researchProgress = new ResearchProgressState() }), Is.False);
            Assert.Throws<ArgumentException>(() => context.BindRecoveryInputs(bundle));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(8)] [TestCase(11)]
        public void MalformedDependencyCannotEstablishValidatedBundle(int index)
        {
            var f = Fixture.Create(null); var bytes = Inputs(); bytes[index] = Encoding.UTF8.GetBytes("{}");
            Assert.Throws<ArgumentException>(() => Bundle(f, bytes));
        }
        [Test] public void GroupedDurationUsesLegacyOrderAndPreciseFractionOnceIncludingZero()
        {
            var f = Fixture.Create(null); var bundle = Bundle(f);
            var online = new CanonicalPassiveManaService(bundle.Passive, bundle.Economy, new FormulaEngine(), f.Profile.Canonical.Spatial, 10);
            var mana = new CanonicalOfflinePassiveManaService(bundle.Passive, online);
            f.Runtime.lastSavedUtcUnix = 10;
            Assert.That(mana.ResolveRecoveryDuration(f.Runtime, bundle.Configuration, 20000000, true).WalletAfter,
                Is.EqualTo(mana.Resolve(f.Runtime, bundle.Configuration, 12).WalletAfter));
            var grouped = mana.ResolveRecoveryDuration(f.Runtime, bundle.Configuration, 72500000, false);
            Assert.That(grouped.WalletAfter, Is.EqualTo(mana.CalculatePrecise(f.Runtime, bundle.Configuration,
                new UnixTimePoint(10, 0), new UnixTimePoint(17, 2500000)).WalletAfter));
            Assert.That(mana.ResolveRecoveryDuration(f.Runtime, bundle.Configuration, 0, false).CalculationAccepted, Is.True);
            Assert.That(mana.ResolveRecoveryDuration(f.Runtime, bundle.Configuration, 1, true).CalculationAccepted, Is.False);
        }
        static TimeProvenance Pending(string phase) => new TimeProvenance(new UnixTimePoint(110, 0), new string('a', 64),
            new RecoveryEpisode(new string('b', 64), new UnixTimePoint(80, 0), new UnixTimePoint(100, 0),
                RecoveryEpisode.UnknownPostCrash, RecoveryEpisode.Precise, phase, 200000000, 42500000));
        [Test] public void ResidualTransitionRetainsExactGroupedSumAndClosesAtS()
        {
            var prior = Pending(RecoveryEpisode.ResidualPending); var bytes = Encoding.UTF8.GetBytes("bound predecessor");
            var transition = CanonicalTimeTransition.Recover(bytes, prior, new UnixTimePoint(112, 0), 15000000,
                LivingDungeonState.Empty(), closeResidual: true);
            Assert.That(transition.SettlementEligible100ns, Is.EqualTo(57500000));
            Assert.That(transition.Successor.Episode, Is.Null);
            Assert.That(transition.Successor.AccountedBoundary, Is.EqualTo(new UnixTimePoint(112, 0)));
            Assert.That(transition.Matches(bytes, prior), Is.True);
            Assert.That(transition.Matches(Encoding.UTF8.GetBytes("stale"), prior), Is.False);
            Assert.That(transition.Matches(bytes, Pending(RecoveryEpisode.OriginalPending)), Is.False);
        }
        [TestCase(-1L)] [TestCase(20000001L)] [TestCase(long.MaxValue)]
        public void RecoveryTransitionRejectsUnprovenDuration(long eligible)
        {
            Assert.Throws<ArgumentException>(() => CanonicalTimeTransition.Recover(new byte[] { 1 },
                Pending(RecoveryEpisode.OriginalPending), new UnixTimePoint(112, 0), eligible, LivingDungeonState.Empty()));
        }

        sealed class Clock : IPreciseTimeSource
        {
            public UnixTimePoint UtcNow() => new UnixTimePoint(12, 2500000);
            public long UtcNowUnixSeconds() => 12;
        }
        sealed class TestProducerEvidence : IHistoricalRecoveryEvidence
        {
            readonly byte[] source; readonly RecoveryContextIdentity inputs;
            internal TestProducerEvidence(byte[] bytes, RecoveryContextIdentity identity) { source = (byte[])bytes.Clone(); inputs = identity; }
            public bool TryGetProvenInputs(byte[] bytes, out RecoveryContextIdentity original)
            { original = inputs; return ExactCompleteSaveAtomicPersistence.Same(source, bytes); }
        }
        static DetachedCurrentTargetValidationContext HistoricalContext(Fixture f, RecoveryRuntimeInputs bundle,
            IHistoricalRecoveryEvidence evidence = null)
        {
            var frozen = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(
                "Assets/_Project/Resources/save_spatial_migration_limits_schema13.json"));
            Assert.That(frozen.IsSuccess, Is.True);
            var baseBudget = frozen.Profile.Canonical.ForSchema(13).Serialized;
            var context = new DetachedCurrentTargetValidationContext(bundle.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(bundle.Configuration), f.Profile.Canonical, bundle.Occupancy,
                livingDungeonProfiles: new[] { bundle.OneFloor.CreateProfile(baseBudget),
                    bundle.TwoFloor.CreateProfile(baseBudget) }, monsterRecovery: bundle.Monsters);
            context.BindRecoveryInputs(bundle, evidence); return context;
        }
        static byte[] AcceptedSource(Fixture f, RecoveryRuntimeInputs bundle)
        {
            var context = HistoricalContext(f, bundle);
            var owner = LivingDungeonState.Empty();
            LivingDungeonScheduling.Activate(owner, context.LivingDungeonProfiles[0], bundle.OneFloor.MinimumArrival, f.Runtime.runHistory);
            LivingDungeonScheduling.Accept(owner, 100);
            var issues = new SpatialIssueCollector(8);
            Assert.That(ContractJson.TryParse(f.FrozenSixteen, f.Profile.Canonical.Serialized, issues, out var root), Is.True);
            var w = new ContractJsonWriter(f.Profile.Canonical.Serialized); w.Node(); w.Token("{");
            for (int i = 0; i < root.Fields.Count; i++) {
                if (i != 0) w.Token(","); var field = root.Fields[i]; w.String(field.Key); w.Token(":");
                if (field.Key != "primary") DetachedCompleteSaveContract.WriteCanonicalNode(w, field.Value);
                else {
                    w.Node(); w.Token("{");
                    for (int j = 0; j < field.Value.Fields.Count; j++) {
                        if (j != 0) w.Token(","); var p = field.Value.Fields[j]; w.String(p.Key); w.Token(":");
                        if (p.Key == LivingDungeonContracts.OwnerName) LivingDungeonContracts.Write(w, owner);
                        else DetachedCompleteSaveContract.WriteCanonicalNode(w, p.Value);
                    }
                    w.Token("}");
                }
            }
            w.Token("}"); return w.Finish(); // Test-produced history, never production attribution.
        }
        [Test] public void HistoricalClosedActivationRetainsRAndWholeSecondCarryWithoutProducerInference()
        {
            var f = HistoricalFixture(); var bundle = Bundle(f); var context = HistoricalContext(f, bundle);
            var source = f.FrozenSixteen; var untouched = (byte[])source.Clone();
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(source, context);
            var entry = RecoveryEntryObservation.Capture(new Clock(), prior.TimeProvenance, RecoveryEntryContext.UnknownPostCrash);
            Assert.That(SchemaSixteenRecoveryActivation.Prepare(source, source, context, entry, entry.WallEndpoint, 0, out var first), Is.Null);
            Assert.That(SchemaSixteenRecoveryActivation.Prepare(source, source, context, entry, entry.WallEndpoint, 0, out var repeated), Is.Null);
            CollectionAssert.AreEqual(first, repeated); CollectionAssert.AreEqual(untouched, source);
            var issues = new SpatialIssueCollector(8);
            Assert.That(ContractJson.TryParse(first, f.Profile.Canonical.Serialized, issues, out var root), Is.True);
            Assert.That(TimeProvenance.TryRead(root.Fields[2].Value, 17, out var time), Is.True);
            Assert.That(time.Episode.Entry, Is.EqualTo(entry.WallEndpoint));
            Assert.That(time.Episode.OriginalEligible100ns, Is.EqualTo((12 - prior.TimeProvenance.AccountedBoundary.Seconds) * 10000000));
            Assert.That(time.Episode.ResidualEligible100ns, Is.EqualTo(2500000));
            Assert.That(time.Episode.EntryPredecessorSha256, Is.EqualTo(SpatialContractSha256.Compute(source)));
        }
        [TestCase(false)] [TestCase(true)]
        public void HistoricalAcceptedActivationRequiresExactProducerBinding(bool proved)
        {
            var f = HistoricalFixture(); var bundle = Bundle(f); var source = AcceptedSource(f, bundle);
            var context = HistoricalContext(f, bundle, proved ? new TestProducerEvidence(source, bundle.Identity) : null);
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(source, context);
            Assert.That(prior.IsValid, Is.True); Assert.That(prior.LivingDungeon.IsClosed, Is.False);
            var entry = RecoveryEntryObservation.Capture(new Clock(), prior.TimeProvenance, RecoveryEntryContext.UnknownPostCrash);
            var reason = SchemaSixteenRecoveryActivation.Prepare(source, source, context, entry, entry.WallEndpoint, 0, out var candidate);
            if (proved) Assert.That(reason, Is.Null);
            else Assert.That(reason, Is.EqualTo(RecoveryEpisode.HistoricalProofRequired));
            Assert.That(candidate != null, Is.EqualTo(proved));
            CollectionAssert.AreEqual(source, AcceptedSource(f, bundle));
        }
        [TestCase(0, "\"MaxHealth\": 8,", "\"MaxHealth\": 10,")]
        [TestCase(10, "\"tickSeconds\": 10", "\"tickSeconds\": 5")]
        [TestCase(10, "\"progressPerActiveSecond\": 0.01", "\"progressPerActiveSecond\": 0.02")]
        [TestCase(11, "\"worldValue\": 2,", "\"worldValue\": 3,")]
        public void HistoricalAcceptedProducerCannotAuthorizeChangedRules(int index, string from, string to)
        {
            var f = HistoricalFixture(); var old = Bundle(f); var source = AcceptedSource(f, old); var changed = Inputs();
            string before = Encoding.UTF8.GetString(changed[index]); string after = before.Replace(from, to);
            Assert.That(after, Is.Not.EqualTo(before)); changed[index] = Encoding.UTF8.GetBytes(after);
            var next = Bundle(f, changed); var context = HistoricalContext(f, next, new TestProducerEvidence(source, old.Identity));
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(source, context);
            Assert.That(prior.IsValid, Is.True);
            var entry = RecoveryEntryObservation.Capture(new Clock(), prior.TimeProvenance, RecoveryEntryContext.UnknownPostCrash);
            Assert.That(SchemaSixteenRecoveryActivation.Prepare(source, source, context, entry, entry.WallEndpoint, 0, out var candidate),
                Is.EqualTo(RecoveryEpisode.HistoricalProofRequired));
            Assert.That(candidate, Is.Null);
        }
        [Test] public void HistoricalTimestampZeroIsReaderHistoryAndNeverActivationEvidence()
        {
            var f = HistoricalFixture(); var bundle = Bundle(f); var context = HistoricalContext(f, bundle);
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(f.FrozenSixteen, context);
            var entry = RecoveryEntryObservation.Capture(new Clock(), prior.TimeProvenance, RecoveryEntryContext.UnknownPostCrash);
            var invalid = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(f.FrozenSixteen).Replace("\"lastSavedUtcUnix\":1,", "\"lastSavedUtcUnix\":0,"));
            Assert.That(SchemaSixteenRecoveryActivation.Prepare(invalid, invalid, context, entry, entry.WallEndpoint, 0, out var candidate),
                Is.EqualTo(RecoveryEpisode.Invalid)); Assert.That(candidate, Is.Null);
        }
    }
}
#endif
