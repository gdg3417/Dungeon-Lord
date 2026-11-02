#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using FS = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class TimeProvenanceTests
    {
        internal const string CorpusRoot = "Tools/Presentation/Fixtures/schema15-09a46810/";
        internal static byte[] Read(string name) => File.ReadAllBytes(CorpusRoot + name + ".json");
        internal static DetachedCurrentTargetValidationContext Context(Fixture f, string name)
        {
            f.Profile = SaveSpatialMigrationLimitsLoader.Load(Read("limits")).Profile;
            return new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production, Read(name + "-config"), f.Profile.Canonical,
                f.Occupancy, false, new[] {
                    LivingDungeonForegroundConfig.Load().CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized),
                    LivingDungeonForegroundConfig.LoadTwoFloor().CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },
                MonsterRecoveryRules.Capture(LegacyGameplayConfigurationContract.Parse(Read(name + "-config"))));
        }
        internal static byte[] Source(Fixture f, string name) => PhaseFourTestSupport.WithTestTimestamp(
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Read(name)).Replace("\"lastSavedUtcUnix\":0", "\"lastSavedUtcUnix\":1")), f.Profile.Canonical);

        [TestCase(0L, 0)] [TestCase(0L, 1)] [TestCase(1L, 9999999)] [TestCase(-1L, 9999999)]
        [TestCase(UnixTimePoint.MinimumSeconds, 0)] [TestCase(UnixTimePoint.MaximumSeconds, 9999999)]
        public void IntegerPrecisionRoundTrips(long seconds, int fraction)
        {
            var p = new UnixTimePoint(seconds, fraction);
            Assert.That(UnixTimePoint.FromUnix100ns(p.Unix100ns), Is.EqualTo(p));
            Assert.That(UnixTimePoint.FromDateTimeOffset(p.ToDateTimeOffset()), Is.EqualTo(p));
            Assert.That(UnixTimePoint.TryParseCanonicalJson(p.ToCanonicalJson(), out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(p));
        }
        [TestCase("{\"Seconds\":1,\"Fraction100ns\":10000000}")]
        [TestCase("{\"Seconds\":1,\"Fraction100ns\":-1}")]
        [TestCase("{\"Seconds\":1,\"Fraction100ns\":0.1}")]
        [TestCase("{\"Seconds\":1e0,\"Fraction100ns\":0}")]
        [TestCase("{\"Seconds\":01,\"Fraction100ns\":0}")]
        [TestCase("{\"Seconds\":1,\"Seconds\":1,\"Fraction100ns\":0}")]
        [TestCase("{\"Seconds\":1,\"Fraction100ns\":0,\"Extra\":1}")]
        [TestCase("{\"Seconds\":253402300800,\"Fraction100ns\":0}")]
        public void SerializedFractionalPointIsStrict(string text) => Assert.That(UnixTimePoint.TryParseCanonicalJson(text, out _), Is.False);
        [Test] public void ExplicitCarryBorrowAndCheckedOverflow()
        {
            Assert.That(new UnixTimePoint(9, 9999999).Add100ns(1), Is.EqualTo(new UnixTimePoint(10, 0)));
            Assert.That(new UnixTimePoint(10, 0).Add100ns(-1), Is.EqualTo(new UnixTimePoint(9, 9999999)));
            Assert.That(UnixTimePoint.FromUnix100ns(-1), Is.EqualTo(new UnixTimePoint(-1, 9999999)));
            Assert.That(new UnixTimePoint(10, 1).Elapsed100nsSince(new UnixTimePoint(9, 9999999)), Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => new UnixTimePoint(9, 0).Elapsed100nsSince(new UnixTimePoint(9, 1)));
            Assert.Throws<OverflowException>(() => new UnixTimePoint(1, 0).Add100ns(long.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(UnixTimePoint.MaximumSeconds, 9999999).Add100ns(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(UnixTimePoint.MinimumSeconds, 0).Add100ns(-1));
        }
        [TestCase(0L, -1)] [TestCase(0L, 10000000)]
        [TestCase(UnixTimePoint.MinimumSeconds - 1, 0)] [TestCase(UnixTimePoint.MaximumSeconds + 1, 0)]
        public void InvalidPointRejected(long seconds, int fraction) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixTimePoint(seconds, fraction));

        [TestCase("dormant")] [TestCase("dormant-monsters")] [TestCase("active-v1")] [TestCase("active-v2")]
        public void ExactMigrationPreservesEveryHistoricalByteExceptVersionAndAppendedOwner(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, name); var source = Source(f, name);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, context, out var candidate), Is.True);
            string expected = Encoding.UTF8.GetString(source).Replace("\"schemaVersion\":15", "\"schemaVersion\":16");
            string tail = ",\"timeProvenance\":" + TimeProvenance.LegacyJson;
            Assert.That(Encoding.UTF8.GetString(candidate).Replace(tail, ""), Is.EqualTo(expected));
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(source, context);
            var next = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(candidate, context);
            Assert.That(next.IsValid, Is.True, next.Reason);
            Assert.That(next.CurrentTargetValidated, Is.False, "Frozen migration is reader authority, not a current writable session");
            Assert.That(next.TimeProvenance.Mode, Is.EqualTo(TimeProvenance.LegacyMode));
            Assert.That(next.TimeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1, 0)));
            Assert.That(next.LivingDungeon.IsDormant, Is.EqualTo(prior.LivingDungeon.IsDormant));
            if (!prior.LivingDungeon.IsDormant) {
                Assert.That(next.LivingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(100));
                Assert.That(next.LivingDungeon.Scheduler.ClosedThrough, Is.EqualTo(-1));
                var a = prior.LivingDungeon.Parties.Single(); var b = next.LivingDungeon.Parties.Single();
                Assert.That(b.Sequence, Is.EqualTo(a.Sequence)); Assert.That(b.NextOrdinal, Is.EqualTo(a.NextOrdinal));
                Assert.That(b.StartIdentity, Is.EqualTo(a.StartIdentity)); Assert.That(b.CheckpointIdentity, Is.EqualTo(a.CheckpointIdentity));
                CollectionAssert.AreEqual(a.Checkpoint, b.Checkpoint);
            }
            Assert.That(DetachedCanonicalSaveSession.Open(candidate, context, f.Profile).IsSuccess, Is.False);
            Assert.That(DetachedCanonicalSaveSession.Open(source, context, f.Profile).IsSuccess, Is.False);
            CollectionAssert.AreEqual(source, Source(f, name));
        }
        [TestCase("dormant")] [TestCase("active-v1")] [TestCase("active-v2")]
        public void MissingHistoricalBoundaryIsNotReconstructed(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, name);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(Read(name), context).IsValid, Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(Read(name), context, out _), Is.False);
        }
        [TestCase("FormatVersion", "1", "2")] [TestCase("AccountedFraction100ns", "0", "1")]
        [TestCase("AccountedFraction100ns", "0", "-1")] [TestCase("AccountedFraction100ns", "0", "10000000")]
        [TestCase("AccountedFraction100ns", "0", "0.0")] [TestCase("AccountedFraction100ns", "0", "0e0")]
        public void UnsupportedOrMalformedOwnerFailsClosed(string field, string oldValue, string nextValue)
        {
            var f = Fixture.Create(null); var text = Encoding.UTF8.GetString(f.FrozenSixteen);
            text = text.Replace("\"" + field + "\":" + oldValue, "\"" + field + "\":" + nextValue);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(Encoding.UTF8.GetBytes(text), f.Context).IsValid, Is.False);
        }
        [TestCase("0")] [TestCase("-1")] [TestCase("253402300800")] [TestCase("9223372036854775808")]
        [TestCase("1.0")] [TestCase("1e0")] [TestCase("\"1\"")] [TestCase("null")]
        public void StrictSerializedSeconds(string token)
        {
            var f = Fixture.Create(null); string text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes()).Replace("\"lastSavedUtcUnix\":1", "\"lastSavedUtcUnix\":" + token);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(text), f.Context).IsValid, Is.False);
        }
        [TestCase("TimeProvenance")] [TestCase("timeProvenance")]
        public void HistoricalUnknownExtensionCannotCollideWithNewOwner(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, "dormant");
            string text = Encoding.UTF8.GetString(Source(f, "dormant")).Replace("\"extension\":", "\"" + name + "\":");
            var source = Encoding.UTF8.GetBytes(text);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(source, context).IsValid, Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, context, out _), Is.False);
        }
        [Test] public void DuplicateOwnerAndFutureSchemaAndIndependentActivationAreRejected()
        {
            var f = Fixture.Create(null); string text = Encoding.UTF8.GetString(f.FrozenSixteen);
            string owner = "\"timeProvenance\":" + TimeProvenance.LegacyJson;
            foreach (string malformed in new[] { text.Replace(owner, owner + "," + owner),
                text.Replace("legacyWholeSeconds", "fractional"), text.Replace("\"schemaVersion\":16", "\"schemaVersion\":17"),
                text.Replace("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":0,\"extra\":1") })
                Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(Encoding.UTF8.GetBytes(malformed), f.Context).IsValid, Is.False);
        }
        [Test] public void EveryRecognizedWriterSynchronizesTheOneLegacyBoundaryAndRejectsBackwardWrites()
        {
            var f = Fixture.Create(null);
            // The frozen legacy owner is still read exactly. Since activation,
            // ordinary current writers retain W and cannot invent a time cut.
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(f.FrozenSixteen,f.Context).IsValid,Is.True);
            foreach (long seconds in new[] { 1L, 2L, 1000L, 0L }) {
                var before=f.Session.GetCurrentBytes();
                f.Runtime.lastSavedUtcUnix = seconds;
                var result=f.Authority.SaveRecognizedState(f.ActivePath,f.FileSystem,f.Session,f.Runtime);
                Assert.That(result.IsSuccess,Is.EqualTo(seconds==1),result.Reason);
                if(result.IsSuccess) f.Accept(result);
                else CollectionAssert.AreEqual(before,f.FileSystem.ReadAllBytes(f.ActivePath));
                f.Reopen(); Assert.That(f.Runtime.timeProvenance.AccountedBoundary,Is.EqualTo(new UnixTimePoint(1,0)));
            }
        }
        [TestCase("active-v1")] [TestCase("active-v2")]
        public void MigrationRequiresExecutableAndConfiguration(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, name); var source = Source(f, name);
            var noExecutable = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production, Read(name + "-config"), f.Profile.Canonical, f.Occupancy);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, noExecutable, out _), Is.False);
            var noConfig = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production, null, f.Profile.Canonical, f.Occupancy,
                livingDungeonProfiles: context.LivingDungeonProfiles);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, noConfig, out _), Is.False);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void MigrationFailurePreservesTupleOrRequiresIndependentRecovery(bool afterReplace, bool uncertain)
        {
            var f = Fixture.Create(null); var context = Context(f, "dormant"); var source = Source(f, "dormant");
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, context, out var candidate), Is.True);
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-fault"); fs.Seed(path, source);
            if (uncertain) fs.EnableFailureSequence(FS.OperationType.Flush, 1, FS.OperationType.Replace, 2);
            else fs.EnableTargetedFailure(FS.OperationType.Replace, _ => true, 1, afterReplace);
            string result = ExactCompleteSaveAtomicPersistence.Persist(path, fs, source, candidate, 64,
                bytes => DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(bytes, context).IsValid);
            Assert.That(result, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(uncertain ? candidate : source, fs.ReadAllBytes(path)); fs.DisableFailure();
            var recovered = fs.ReadAllBytes(path);
            if (uncertain) {
                Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(recovered, context).IsValid,Is.True);
                Assert.That(DetachedCanonicalSaveSession.Open(recovered,context,f.Profile).IsSuccess,Is.False);
            }
            else Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(recovered, context, out var retried) && retried.SequenceEqual(candidate), Is.True);
        }
        static DetachedSpatialSaveLoadCoordinator Coordinator(Fixture f, DetachedCurrentTargetValidationContext context) =>
            new DetachedSpatialSaveLoadCoordinator(f.Profile, f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(context.Configuration), new Dictionary<string, byte[]>(),
                new RawSaveEnvelopeVersionContract(1, CanonicalSaveSchemaVersions.CurrentWritableTarget), FS.BlankFloorForCoordinator,
                f.PositionProfiles, f.Occupancy, context.LivingDungeonProfiles, context.MonsterRecovery);
        static SpatialMigrationActivationPreflight Supported(FS.DeterministicFileSystem fs, string path) =>
            new SpatialMigrationActivationPreflight(true, SpatialMigrationCapabilityReason.Ready, SpatialMigrationPlatform.WindowsEditor, fs, path);
        [TestCase("dormant")] [TestCase("dormant-monsters")] [TestCase("active-v1")] [TestCase("active-v2")]
        public void ActualLoadCoordinatorMigratesAndIndependentlyReopensExactTarget(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, name); var source = Source(f, name);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, context, out var expected), Is.True);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(expected,context).IsValid,Is.True);
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-load-" + name); fs.Seed(path, source);
            var loaded = Coordinator(f, context).Load(path, Supported(fs, path));
            // This frozen corpus proves its historical reader/executable, not
            // a full bootstrap producer binding for current activation.
            Assert.That(loaded.IsSuccess,Is.False); Assert.That(loaded.Reason,Is.EqualTo(RecoveryEpisode.ContextMismatch));
            Assert.That(loaded.Session,Is.Null); Assert.That(loaded.RuntimeProjection,Is.Null);
            CollectionAssert.AreEqual(source,fs.ReadAllBytes(path));
            var reopened = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(reopened.IsSuccess,Is.False); CollectionAssert.AreEqual(source,fs.ReadAllBytes(path));
            Assert.That(fs.Operations.Any(o=>o.Type==FS.OperationType.Write || o.Type==FS.OperationType.Replace),Is.False);
        }
        private sealed class ActivationClock : IPreciseTimeSource
        {
            public UnixTimePoint UtcNow() => new UnixTimePoint(1,0);
            public long UtcNowUnixSeconds() => 1;
        }
        static DetachedSpatialSaveLoadCoordinator BoundCoordinator(Fixture f)
        {
            var inputs=f.Context.RecoveryInputs;
            var coordinator=new DetachedSpatialSaveLoadCoordinator(f.Profile,inputs.Compatibility,f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(inputs.Configuration),new Dictionary<string,byte[]>(),
                new RawSaveEnvelopeVersionContract(1,CanonicalSaveSchemaVersions.CurrentWritableTarget),FS.BlankFloorForCoordinator,
                f.PositionProfiles,inputs.Occupancy,new[] { inputs.OneFloor.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized),
                    inputs.TwoFloor.CreateProfile(f.Profile.Canonical.ForSchema(13).Serialized) },inputs.Monsters);
            var time=new TimeService(new SimpleLogger(false),inputs.Bootstrap.tickSeconds,inputs.Bootstrap.timeRules.detectClockSkewSeconds,new ActivationClock(),()=>0);
            time.BeginRecoveryForeground(inputs.TwoFloor.UnitsPerSecond);coordinator.ConfigureRecovery(inputs,time);return coordinator;
        }
        [TestCase(false)] [TestCase(true)]
        public void CoordinatorFailureThenIndependentRecoveryNeverPublishesPartialAuthority(bool uncertain)
        {
            var profile=SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
            var f=Fixture.Create("\"researchPending\":null,\"researchProgress\":null",workloadProfile:profile);
            // Newly authored dormant source, with validated full current inputs;
            // never attribution of the old active corpus to today's defaults.
            var source=f.FrozenSixteen;
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-load-failure"); fs.Seed(path, source);
            if (uncertain) fs.EnableFailureSequence(FS.OperationType.Flush, 1, FS.OperationType.Replace, 2);
            else fs.EnableTargetedFailure(FS.OperationType.Replace, _ => true, 1, false);
            var failed = BoundCoordinator(f).Load(path, Supported(fs, path));
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(failed.Session, Is.Null);
            if (!uncertain) CollectionAssert.AreEqual(source, fs.ReadAllBytes(path));
            fs.DisableFailure();
            var recovered = BoundCoordinator(f).Load(path, Supported(fs, path));
            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.RuntimeProjection.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1, 0)));
            Assert.That(recovered.RuntimeProjection.timeProvenance.Episode.Phase,Is.EqualTo(RecoveryEpisode.OriginalPending));
            Assert.That(recovered.RuntimeProjection.timeProvenance.RecoveryContextSha256,Is.EqualTo(f.Context.RecoveryInputs.Identity.Digest));
            CollectionAssert.AreEqual(recovered.Session.GetCurrentBytes(), fs.ReadAllBytes(path));
        }
        [Test] public void FutureSchemaAndInvalidHistoricalBoundaryLeaveExactSourceUntouched()
        {
            var f = Fixture.Create(null); var context = Context(f, "dormant");
            foreach (var bytes in new[] { Read("dormant"), Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(f.Session.GetCurrentBytes())
                .Replace("\"schemaVersion\":17", "\"schemaVersion\":18")) }) {
                var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-refuse"); fs.Seed(path, bytes);
                var result = Coordinator(f, context).Load(path, Supported(fs, path));
                Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null);
                CollectionAssert.AreEqual(bytes, fs.ReadAllBytes(path));
                Assert.That(fs.Operations.Any(o => o.Type == FS.OperationType.Write || o.Type == FS.OperationType.Replace), Is.False);
            }
        }
        [TestCase("active-v1")] [TestCase("active-v2")]
        public void MigratedAcceptedWorkDrainsThroughTheExistingForegroundAuthority(string name)
        {
            var f = Fixture.Create(null); var context = Context(f, name); var source = Source(f, name);
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-drain-" + name); fs.Seed(path, source);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source,context,out var historical),Is.True);
            var frozen=DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(historical,context);
            Assert.That(frozen.IsValid,Is.True,frozen.Reason);
            long accepted=frozen.LivingDungeon.Scheduler.AcceptedThrough;
            long closed=frozen.LivingDungeon.Scheduler.ClosedThrough;
            var loaded = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(loaded.IsSuccess,Is.False);Assert.That(loaded.Reason,Is.EqualTo(RecoveryEpisode.ContextMismatch));
            CollectionAssert.AreEqual(source,fs.ReadAllBytes(path));
            Assert.That(accepted,Is.EqualTo(100));Assert.That(closed,Is.EqualTo(-1));
            foreach(var party in frozen.LivingDungeon.Parties) {
                var execution=LivingDungeonContracts.Restore(party,LivingDungeonContracts.Profile(frozen.LivingDungeon,context.LivingDungeonProfiles));
                try { Assert.That(execution.NextOrdinal,Is.EqualTo(party.NextOrdinal)); }
                finally { execution.Abandon(); }
            }
            Assert.That(fs.Operations.Any(o=>o.Type==FS.OperationType.Write || o.Type==FS.OperationType.Replace),Is.False,
                "Do not drain unproved historical inputs or abandon their accepted work");
        }
        [Test] public void TargetBudgetsReserveExactlyTheNewGrammarWithoutChangingFrozenFifteen()
        {
            var current = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes("Assets/_Project/Resources/save_spatial_migration_limits_schema16.json")).Profile;
            var historical = current.Canonical.ForSchema(15);
            Assert.That(TimeProvenance.LegacyMemberByteCount, Is.EqualTo(92));
            Assert.That(current.Canonical.Serialized.MaximumInputBytes - historical.Serialized.MaximumInputBytes, Is.EqualTo(92));
            Assert.That(current.Whole.MaximumCandidateBytes - historical.Serialized.MaximumInputBytes, Is.EqualTo(92));
            Assert.That(current.Canonical.Serialized.MaximumParsedNodes - historical.Serialized.MaximumParsedNodes, Is.EqualTo(4));
            Assert.That(current.Canonical.Serialized.MaximumCollectionRecords - historical.Serialized.MaximumCollectionRecords, Is.EqualTo(4));
            int strings = TimeProvenance.Owner.Length + "FormatVersion".Length + "Mode".Length +
                "AccountedFraction100ns".Length + TimeProvenance.LegacyMode.Length;
            Assert.That(strings, Is.EqualTo(71));
            Assert.That(current.Canonical.Serialized.MaximumStringCharacters - historical.Serialized.MaximumStringCharacters, Is.EqualTo(strings));
            Assert.That(current.Raw.MaximumInputBytes, Is.EqualTo(checked(current.Canonical.Serialized.MaximumInputBytes * 2)));
            Assert.That(current.Raw.MaximumScanWork, Is.EqualTo(checked(current.Raw.MaximumInputBytes * 2)));
            Assert.That(historical.Serialized.MaximumInputBytes, Is.EqualTo(1376256));
        }
        [Test] public void ExactLegacyByteCeilingMigratesAndRemainsWritableWithReservedOwnerBytes()
        {
            var f = Fixture.Create(null); f.Runtime.structureRuntime.ManaReserve = f.Acquisition.StartingMana;
            // Stabilize pre-existing Unity nullable projections before the size-boundary test.
            for (int i = 0; i < 3; i++) f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime));
            var source = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(f.FrozenSixteen)
                .Replace(",\"timeProvenance\":" + TimeProvenance.LegacyJson, "").Replace("\"schemaVersion\":16", "\"schemaVersion\":15"));
            var s = f.Profile.Canonical.Serialized; var whole = f.Profile.Whole;
            var oldCanonical = new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(source.Length,
                s.MaximumParsedNodes, s.MaximumCollectionRecords, s.MaximumStringCharacters, s.MaximumDiagnostics), f.Profile.Canonical.Spatial);
            var historical = new SaveSpatialMigrationLimitsProfile(f.Profile.Raw, oldCanonical,
                new DetachedWholeSaveLimits(source.Length, whole.MaximumCopiedValueBytes, whole.MaximumUnknownMembers, whole.MaximumUnknownMemberBytes));
            int targetBytes = checked(source.Length + TimeProvenance.LegacyMemberByteCount);
            var target = new CanonicalSpatialSerializationLimits(new SpatialSerializedInputLimits(targetBytes,
                s.MaximumParsedNodes + 4, s.MaximumCollectionRecords + 4, s.MaximumStringCharacters + 71, s.MaximumDiagnostics),
                f.Profile.Canonical.Spatial, historical, 15, true);
            var profile = new SaveSpatialMigrationLimitsProfile(f.Profile.Raw, target,
                new DetachedWholeSaveLimits(targetBytes, whole.MaximumCopiedValueBytes, whole.MaximumUnknownMembers, whole.MaximumUnknownMemberBytes));
            var context = new DetachedCurrentTargetValidationContext(f.Compatibility, f.Production,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration), target, f.Occupancy);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(source, context).IsValid, Is.True);
            Assert.That(SchemaFifteenToSixteenUpgrade.TryPrepare(source, context, out var candidate), Is.True);
            Assert.That(candidate.Length, Is.EqualTo(targetBytes));
            var opened = DetachedCanonicalSaveSession.Open(candidate, context, profile);
            Assert.That(opened.IsSuccess,Is.False,"Frozen schema 16 no longer creates a writable session");
            var frozen=DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(candidate,context);
            Assert.That(frozen.IsValid,Is.True,frozen.Reason);
            CollectionAssert.AreEqual(candidate,frozen.GetBytes());
            Assert.That(frozen.GetBytes().Length,Is.EqualTo(targetBytes));
        }
    }
}
#endif
