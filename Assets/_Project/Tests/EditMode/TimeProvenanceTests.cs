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
            var next = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(candidate, context);
            Assert.That(next.CurrentTargetValidated, Is.True);
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
            Assert.That(DetachedCanonicalSaveSession.Open(candidate, context, f.Profile).IsSuccess, Is.True);
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
            var f = Fixture.Create(null); var text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            text = text.Replace("\"" + field + "\":" + oldValue, "\"" + field + "\":" + nextValue);
            Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(text), f.Context).IsValid, Is.False);
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
            var f = Fixture.Create(null); string text = Encoding.UTF8.GetString(f.Session.GetCurrentBytes());
            string owner = "\"timeProvenance\":" + TimeProvenance.LegacyJson;
            foreach (string malformed in new[] { text.Replace(owner, owner + "," + owner),
                text.Replace("legacyWholeSeconds", "fractional"), text.Replace("\"schemaVersion\":16", "\"schemaVersion\":17"),
                text.Replace("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":0,\"extra\":1") })
                Assert.That(DetachedCompleteSaveContract.ParseValidateAndRoundTrip(Encoding.UTF8.GetBytes(malformed), f.Context).IsValid, Is.False);
        }
        [Test] public void EveryRecognizedWriterSynchronizesTheOneLegacyBoundaryAndRejectsBackwardWrites()
        {
            var f = Fixture.Create(null);
            foreach (long seconds in new[] { 1L, 2L, 1000L }) {
                f.Runtime.lastSavedUtcUnix = seconds;
                f.Accept(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime)); f.Reopen();
                Assert.That(f.Runtime.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(seconds, 0)));
            }
            var prior = f.Session.GetCurrentBytes(); f.Runtime.lastSavedUtcUnix = 999;
            Assert.That(f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime).IsSuccess, Is.False);
            CollectionAssert.AreEqual(prior, f.FileSystem.ReadAllBytes(f.ActivePath));
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
                bytes => DetachedCompleteSaveContract.ParseValidateAndRoundTrip(bytes, context).CurrentTargetValidated);
            Assert.That(result, Is.EqualTo(uncertain ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(uncertain ? candidate : source, fs.ReadAllBytes(path)); fs.DisableFailure();
            var recovered = fs.ReadAllBytes(path);
            if (uncertain) Assert.That(DetachedCanonicalSaveSession.Open(recovered, context, f.Profile).IsSuccess, Is.True);
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
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-load-" + name); fs.Seed(path, source);
            var loaded = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(loaded.IsSuccess, Is.True, loaded.Reason);
            Assert.That(loaded.Disposition, Is.EqualTo(DetachedSpatialSaveLoadDisposition.Migrated));
            CollectionAssert.AreEqual(expected, loaded.Session.GetCurrentBytes());
            CollectionAssert.AreEqual(expected, fs.ReadAllBytes(path));
            var reopened = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(reopened.IsSuccess, Is.True, reopened.Reason);
            CollectionAssert.AreEqual(expected, reopened.Session.GetCurrentBytes());
            Assert.That(reopened.RuntimeProjection.lastSavedUtcUnix, Is.EqualTo(1));
            Assert.That(reopened.RuntimeProjection.timeProvenance.AccountedBoundary.Fraction100ns, Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void CoordinatorFailureThenIndependentRecoveryNeverPublishesPartialAuthority(bool uncertain)
        {
            var f = Fixture.Create(null); var context = Context(f, "dormant"); var source = Source(f, "dormant");
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("h1-f2-load-failure"); fs.Seed(path, source);
            if (uncertain) fs.EnableFailureSequence(FS.OperationType.Flush, 1, FS.OperationType.Replace, 2);
            else fs.EnableTargetedFailure(FS.OperationType.Replace, _ => true, 1, false);
            var failed = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(failed.IsSuccess, Is.False); Assert.That(failed.RuntimeProjection, Is.Null); Assert.That(failed.Session, Is.Null);
            if (!uncertain) CollectionAssert.AreEqual(source, fs.ReadAllBytes(path));
            fs.DisableFailure();
            var recovered = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.RuntimeProjection.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1, 0)));
            CollectionAssert.AreEqual(recovered.Session.GetCurrentBytes(), fs.ReadAllBytes(path));
        }
        [Test] public void FutureSchemaAndInvalidHistoricalBoundaryLeaveExactSourceUntouched()
        {
            var f = Fixture.Create(null); var context = Context(f, "dormant");
            foreach (var bytes in new[] { Read("dormant"), Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(f.Session.GetCurrentBytes())
                .Replace("\"schemaVersion\":16", "\"schemaVersion\":17")) }) {
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
            var loaded = Coordinator(f, context).Load(path, Supported(fs, path));
            Assert.That(loaded.IsSuccess, Is.True, loaded.Reason);
            f.Context = context; f.Configuration = context.Configuration; f.FileSystem = fs; f.ActivePath = path;
            f.Session = loaded.Session; f.State = loaded.Validation.State; f.Runtime = loaded.RuntimeProjection;
            long accepted = f.Runtime.livingDungeon.Scheduler.AcceptedThrough;
            f.Accept(PhaseSixA4Tests.Writer(f).AdvanceForeground(path, fs, f.Session, f.Runtime,
                LivingDungeonForegroundConfig.LoadTwoFloor(), accepted, 10, (candidate, tick) => { }, 1));
            Assert.That(f.Runtime.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(accepted));
            Assert.That(f.Runtime.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(accepted));
            Assert.That(f.Runtime.timeProvenance.AccountedBoundary, Is.EqualTo(new UnixTimePoint(1, 0)));
            f.Reopen(); CollectionAssert.AreEqual(f.Session.GetCurrentBytes(), fs.ReadAllBytes(path));
        }
    }
}
#endif
