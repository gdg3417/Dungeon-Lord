#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;
using FS = DungeonBuilder.M0.Tests.EditMode.Gd66DetachedSpatialMigrationTransactionTests;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class FrozenSchemaFifteenTests
    {
        const string Root = "Tools/Presentation/Fixtures/schema15-09a46810/";
        static byte[] Read(string name) => File.ReadAllBytes(Root + name);
        static SaveSpatialMigrationLimitsProfile Limits => SaveSpatialMigrationLimitsLoader.Load(Read("limits.json")).Profile;
        static LivingDungeonProfile[] Profiles(CanonicalSpatialSerializationLimits limits) => new[] {
            LivingDungeonForegroundConfig.Load().CreateProfile(limits.ForSchema(13).Serialized),
            LivingDungeonForegroundConfig.LoadTwoFloor().CreateProfile(limits.ForSchema(13).Serialized)
        };
        static DetachedCurrentTargetValidationContext Context(string name,
            CanonicalSpatialSerializationLimits? limits = null, bool profiles = true,
            bool occupancy = true, bool configuration = true, SpatialLayoutCompatibilitySnapshot compatibility = null)
        {
            var f = Fixture.Create(null); var selected = limits ?? Limits.Canonical;
            var config = Read(name + "-config.json");
            return new DetachedCurrentTargetValidationContext(compatibility ?? f.Compatibility, f.Production,
                configuration ? config : null, selected, occupancy ? f.Occupancy : null, false,
                profiles ? Profiles(selected) : null,
                MonsterRecoveryRules.Capture(LegacyGameplayConfigurationContract.Parse(config)));
        }
        public static IEnumerable<TestCaseData> Corpus() => File.ReadAllLines(Root + "verdicts.tsv")
            .Select(line => line.Split('\t')).Select(p => new TestCaseData(p[0], p[1], bool.Parse(p[2]), bool.Parse(p[3]))
                .SetName("FrozenSchemaFifteen.BaselineEquivalence." + p[0]));

        [TestCaseSource(nameof(Corpus))]
        public void HistoricalAcceptanceAndRejectionMatchBaseline(string name, string configuration,
            bool structuralExpected, bool contextualExpected)
        {
            var bytes = Read(name + ".json"); var context = Context(configuration);
            var structural = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(
                bytes, context.Limits, context.LivingDungeonProfiles);
            var contextual = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes, context);
            Assert.That(structural.IsValid, Is.EqualTo(structuralExpected));
            Assert.That(contextual.IsValid, Is.EqualTo(contextualExpected));
            Assert.That(structural.CurrentTargetValidated, Is.False);
            Assert.That(contextual.CurrentTargetValidated, Is.False);
            if (structuralExpected) CollectionAssert.AreEqual(bytes, structural.GetBytes());
            if (contextualExpected) CollectionAssert.AreEqual(bytes, contextual.GetBytes());
        }
        [Test] public void OriginalBytesAndLimitsRetainCapturedHashes()
        {
            foreach (string line in File.ReadAllLines(Root + "SHA256SUMS.tsv")) {
                var p = line.Split('\t'); if (!p[0].EndsWith(".json")) continue;
                Assert.That(SpatialContractSha256.Compute(Read(p[0])), Is.EqualTo(p[1]), p[0]);
            }
            CollectionAssert.AreEqual(Read("limits.json"), File.ReadAllBytes(
                "Assets/_Project/Resources/save_spatial_migration_limits_schema15.json"));
        }
        [TestCase("active-v1", 2)] [TestCase("active-v2", 3)]
        public void ActiveOwnerPreservesExactCheckpointAllocatorAndUnclosedHorizon(string name, int format)
        {
            var bytes = Read(name + ".json"); var context = Context(name);
            var frozen = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes, context);
            var baselineView = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes, context.Limits, context.LivingDungeonProfiles);
            Assert.That(frozen.IsValid && baselineView.IsValid, Is.True);
            var owner = frozen.LivingDungeon; Assert.That(owner.IsDormant, Is.False);
            Assert.That(owner.Scheduler.AcceptedThrough, Is.EqualTo(100));
            Assert.That(owner.Scheduler.ClosedThrough, Is.EqualTo(-1));
            Assert.That(owner.Parties.Count, Is.EqualTo(1));
            var p = owner.Parties.Single(); var original = baselineView.LivingDungeon.Parties.Single();
            Assert.That(p.Sequence, Is.EqualTo(1)); Assert.That(p.NextOrdinal, Is.EqualTo(0));
            Assert.That(p.StartIdentity, Is.EqualTo(original.StartIdentity));
            Assert.That(p.CheckpointIdentity, Is.EqualTo(original.CheckpointIdentity));
            CollectionAssert.AreEqual(original.Checkpoint, p.Checkpoint);
            Assert.That(BitConverter.ToInt32(p.Checkpoint, 0), Is.EqualTo(format));
            var e = LivingDungeonContracts.Restore(p, LivingDungeonContracts.Profile(owner, context.LivingDungeonProfiles));
            try { CollectionAssert.AreEqual(p.Checkpoint, e.ExportCheckpoint(
                LivingDungeonContracts.Profile(owner, context.LivingDungeonProfiles).Checkpoints).GetBytes()); }
            finally { e.Abandon(); }
            Assert.That(Encoding.UTF8.GetString(frozen.GetBytes()), Does.Contain("\"NextRunSequence\":2"));
            Assert.That(baselineView.CurrentTargetValidated, Is.EqualTo(
                CanonicalSaveSchemaVersions.CurrentWritableTarget == CanonicalSaveSchemaVersions.FrozenSchemaFifteen));
        }
        [TestCase("dormant")] [TestCase("dormant-monsters")]
        public void HistoricalDormantOwnerHasNoSynthesizedSchedulerOrParty(string name)
        {
            var result = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(Read(name + ".json"), Context(name));
            Assert.That(result.IsValid, Is.True); Assert.That(result.LivingDungeon.IsDormant, Is.True);
            Assert.That(result.LivingDungeon.Parties, Is.Empty);
        }
        [TestCase("active-v1")] [TestCase("active-v2")]
        public void MissingExecutableProfileCannotBeAccepted(string name)
        {
            var bytes = Read(name + ".json"); var context = Context(name, profiles: false);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes,
                context.Limits).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes, context).IsValid, Is.False);
        }
        [TestCase(null)] [TestCase("run.executable.unsupported.v1")]
        public void MissingOrUnsupportedCheckpointExecutableFailsDespiteRecomputedHash(string executable)
        {
            var bytes = Read("active-v1.json"); var context = Context("active-v1");
            var owner = DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(bytes, context).LivingDungeon;
            byte[] OwnerBytes() { var writer = new ContractJsonWriter(context.Limits.Serialized); LivingDungeonContracts.Write(writer, owner); return writer.Finish(); }
            var originalOwner = OwnerBytes(); var party = owner.Parties.Single();
            using (var reader = new BinaryReader(new MemoryStream(party.Checkpoint))) {
                int format = reader.ReadInt32(); Assert.That(format, Is.EqualTo(2));
                RunCheckpointCodec.Text(reader);
                var suffix = reader.ReadBytes(party.Checkpoint.Length - 32 - (int)reader.BaseStream.Position);
                var payload = RunCheckpointCodec.Bytes(w => { w.Write(format); RunCheckpointCodec.Text(w, executable); w.Write(suffix); });
                party.Checkpoint = RunCheckpointCodec.Bytes(w => { w.Write(payload); w.Write(RunCheckpointCodec.Hash(payload)); });
                party.CheckpointIdentity = RunCheckpointCodec.Identity(party.Checkpoint);
            }
            var altered = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace(
                Encoding.UTF8.GetString(originalOwner), Encoding.UTF8.GetString(OwnerBytes())));
            Assert.That(altered.SequenceEqual(bytes), Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(altered,
                context.Limits, context.LivingDungeonProfiles).IsValid, Is.False);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(altered, context).IsValid, Is.False);
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void MissingProductionConfigurationOrOccupancyFailsContext(bool configuration, bool occupancy)
        {
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(
                Read("dormant-monsters.json"), Context("dormant-monsters", configuration: configuration, occupancy: occupancy)).IsValid, Is.False);
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrRetiredSchema15CompatibilityContractFailsContext(bool retired)
        {
            var data = Fixture.Create(null).Compatibility.Value;
            if (retired) data.ContractSelections.Single(c => c.TargetSchemaVersion == 15).Lifecycle = CompatibilityProfileLifecycle.Retired;
            else data.ContractSelections = data.ContractSelections.Where(c => c.TargetSchemaVersion != 15).ToArray();
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(Read("dormant.json"),
                Context("dormant", compatibility: new SpatialLayoutCompatibilitySnapshot(data))).IsValid, Is.False);
        }
        [TestCase("dormant", true)] [TestCase("dormant-monsters", true)]
        [TestCase("active-v1", false)] [TestCase("active-v2", false)]
        public void FourteenUpgradePreservesBaselineBytesAndRejection(string name, bool accepted)
        {
            var source = Read(name + "-schema14.json"); var context = Context(name);
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFourteenAndRoundTrip(
                source, context.Limits, context.LivingDungeonProfiles).IsValid, Is.True);
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(source, context.Limits, context.MonsterRecovery,
                out var candidate, context.LivingDungeonProfiles), Is.EqualTo(accepted));
            if (accepted) {
                CollectionAssert.AreEqual(Read(name + "-candidate.json"), candidate);
                CollectionAssert.AreEqual(source, PhaseFourTestSupport.FrozenFourteen(candidate));
            } else Assert.That(candidate, Is.Null);
        }
        [TestCase(2)] [TestCase(-2)]
        public void FutureTargetLimitsCannotChangeHistoricalEnvelope(int scale)
        {
            string text = Encoding.UTF8.GetString(Read("limits.json"));
            text = Regex.Replace(text, "(\"Maximum[^\"]+\":)([0-9]+)", m => m.Groups[1].Value +
                (scale > 0 ? int.Parse(m.Groups[2].Value) * scale : int.Parse(m.Groups[2].Value) / -scale));
            var loaded = SaveSpatialMigrationLimitsLoader.Load(text); Assert.That(loaded.IsSuccess, Is.True, loaded.Reason);
            var selected = loaded.Profile; var pinned = Limits;
            // int.MaxValue is an unsupported selector used only to probe target-limit selection.
            Assert.That(selected.Canonical.ForSchema(int.MaxValue).Serialized.MaximumInputBytes,
                Is.Not.EqualTo(pinned.Canonical.ForSchema(15).Serialized.MaximumInputBytes));
            Assert.That(selected.Canonical.ForSchema(15).Serialized.MaximumInputBytes, Is.EqualTo(1376256));
            Assert.That(selected.Canonical.ForSchema(15).Spatial.MaximumRecords, Is.EqualTo(165));
            Assert.That(selected.Raw.ForPayload(Read("active-v2.json")).MaximumMonsterRecords, Is.EqualTo(165));
            Assert.That(selected.Raw.ForPayload(Read("active-v2-schema14.json")).MaximumInputBytes, Is.EqualTo(1310720));
            foreach (string name in new[] { "dormant", "active-v1", "active-v2" })
                Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(Read(name + ".json"),
                    Context(name, selected.Canonical)).IsValid, Is.True, name);
            var tooLarge = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Read("dormant.json")).Replace(
                "\"value\":17", "\"value\":\"" + new string('x', 1376256) + "\""));
            Assert.That(DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(tooLarge, selected.Canonical).IsValid, Is.False);
            Assert.That(SchemaFourteenToFifteenUpgrade.TryPrepare(Read("dormant-monsters-schema14.json"), selected.Canonical,
                Context("dormant-monsters").MonsterRecovery, out var candidate), Is.True);
            CollectionAssert.AreEqual(Read("dormant-monsters-candidate.json"), candidate);
        }
        static DetachedSpatialMigrationRecoveryContext Recovery(DetachedCurrentTargetValidationContext context) =>
            new DetachedSpatialMigrationRecoveryContext(context.Compatibility, context.Production, new Dictionary<string, byte[]>(),
                LegacyGameplayConfigurationContract.SerializeCanonical(context.Configuration), context.Limits, Limits.Raw,
                new RawSaveEnvelopeVersionContract(1, 6), FS.BlankFloorForCoordinator, Limits.Whole,
                context.RoomContentOccupancy, context.LivingDungeonProfiles, context.MonsterRecovery);
        [TestCase("active-v1", true)] [TestCase("active-v2", true)]
        [TestCase("active-v1-inconsistent-monster", false)] [TestCase("active-v2-unsupported-profile", false)]
        [TestCase("active-v2-unsupported-schema", false)]
        public void RecoveryRequiresHistoricalContextAndNeverWritesUnprovenBytes(string name, bool accepted)
        {
            var fs = new FS.DeterministicFileSystem(); string active = FS.ActivePath("frozen15-" + name);
            var bytes = Read(name + ".json"); fs.Seed(active, bytes);
            var result = new DetachedSpatialMigrationTransaction(fs, Recovery(Context(name.StartsWith("active-v1") ? "active-v1" : "active-v2"))).Recover(active);
            Assert.That(result.IsSuccess, Is.EqualTo(accepted), result.Reason);
            CollectionAssert.AreEqual(bytes, fs.ReadAllBytes(active));
            Assert.That(fs.Operations.Any(o => o.Type == FS.OperationType.Write || o.Type == FS.OperationType.Replace), Is.False);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void MigrationAtomicFailureRequiresExactPredecessorOrReportsUncertainty(bool afterReplace, bool rollbackFails)
        {
            var original = Read("dormant-monsters-schema14.json"); var candidate = Read("dormant-monsters-candidate.json");
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("frozen15-fault"); fs.Seed(path, original);
            if (rollbackFails) fs.EnableFailureSequence(FS.OperationType.Flush, 1, FS.OperationType.Replace, 2);
            else fs.EnableTargetedFailure(FS.OperationType.Replace, _ => true, 1, afterReplace);
            string reason = ExactCompleteSaveAtomicPersistence.Persist(path, fs, original, candidate, 64,
                b => DetachedCompleteSaveContract.ParseValidateFrozenSchemaFifteenAndRoundTrip(b, Context("dormant-monsters")).IsValid);
            Assert.That(reason, Is.EqualTo(rollbackFails ? DetachedCanonicalWriteAuthority.RecoveryRequiredReason : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(rollbackFails ? candidate : original, fs.ReadAllBytes(path));
        }
        [Test] public void MigrationReadbackMismatchRollsBackAndExactCandidateIsIdempotent()
        {
            var original = Read("dormant-schema14.json"); var candidate = Read("dormant-candidate.json");
            var fs = new FS.DeterministicFileSystem(); string path = FS.ActivePath("frozen15-proof"); fs.Seed(path, original);
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, original, candidate, 64, _ => false),
                Is.EqualTo(DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            CollectionAssert.AreEqual(original, fs.ReadAllBytes(path));
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, original, candidate, 64), Is.Null);
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, original, candidate, 64), Is.Null);
            CollectionAssert.AreEqual(candidate, fs.ReadAllBytes(path));
            fs.Seed(path, new byte[] { 0 });
            Assert.That(ExactCompleteSaveAtomicPersistence.Persist(path, fs, original, candidate, 64),
                Is.EqualTo(DetachedCanonicalWriteAuthority.RecoveryRequiredReason));
        }
    }
}
#endif
