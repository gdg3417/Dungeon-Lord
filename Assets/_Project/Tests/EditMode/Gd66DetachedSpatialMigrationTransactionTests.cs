#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedSpatialMigrationTransactionTests
    {
        private static readonly SpatialSerializedInputLimits Limits =
            new SpatialSerializedInputLimits(32768, 256, 32, 4096, 16);

        [Test]
        public void FinalizationReceipt_RoundTripsCanonicalThreeFieldContract()
        {
            var receipt = new DetachedFinalizationReceipt(TransactionId('1'), Hash('2'), Hash('3'));

            byte[] bytes = DetachedFinalizationReceiptContract.Serialize(receipt, Limits);
            DetachedFinalizationReceipt parsed = DetachedFinalizationReceiptContract.Parse(bytes, Limits);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.TransactionId, Is.EqualTo(receipt.TransactionId));
            Assert.That(parsed.DescriptorFingerprint, Is.EqualTo(receipt.DescriptorFingerprint));
            Assert.That(parsed.CandidateSha256, Is.EqualTo(receipt.CandidateSha256));
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Not.Contain("FinalStage"));
        }

        [TestCase("{\"TransactionId\":\"{0}\",\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}")]
        [TestCase("{\"transactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}")]
        [TestCase("{\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\",\"FinalStage\":6}")]
        public void FinalizationReceipt_RejectsDuplicateCaseAmbiguousAndExtraFields(string format)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format(format,
                TransactionId('1'), Hash('2'), Hash('3')));

            Assert.That(DetachedFinalizationReceiptContract.Parse(bytes, Limits), Is.Null);
        }

        [Test]
        public void RestorationIntent_BindsAttemptBackupAndPersistedStage()
        {
            var intent = new DetachedRestorationIntent(TransactionId('1'), Hash('2'), Hash('3'),
                Hash('3'), "save.gd66-" + new string('1', 64) + ".journal.json",
                (int)SpatialMigrationJournalStage.Replaced);

            byte[] bytes = DetachedRestorationIntentContract.Serialize(intent, Limits);
            DetachedRestorationIntent parsed = DetachedRestorationIntentContract.Parse(bytes, Limits);

            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.TransactionId, Is.EqualTo(intent.TransactionId));
            Assert.That(parsed.JournalFilename, Is.EqualTo(intent.JournalFilename));
            Assert.That(parsed.JournalStage, Is.EqualTo((int)SpatialMigrationJournalStage.Replaced));
        }

        [Test]
        public void DeterministicFileSystem_InjectsWriteReadReplaceMoveFlushAndEnumerationFailures()
        {
            foreach (FailurePoint point in Enum.GetValues(typeof(FailurePoint)).Cast<FailurePoint>())
            {
                if (point == FailurePoint.None) continue;
                var fileSystem = new DeterministicFileSystem(point);
                Assert.That(() => Exercise(fileSystem, point), Throws.TypeOf<IOException>(), point.ToString());
            }
        }

        [Test]
        public void GenericRuntimeFileSystem_CannotClaimDirectoryDurability()
        {
            Assert.That(() => new RuntimeSpatialMigrationFileSystem().FlushDirectory(Path.GetTempPath()),
                Throws.TypeOf<PlatformNotSupportedException>());
        }

        [Test]
        public void PrepareExecuteRecover_EmptyMigrationPersistsAndTrustsCanonicalCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            Assert.That(fixture.Result.Attempt.IsEmptyMigration, Is.True);

            var fileSystem = new DeterministicFileSystem(FailurePoint.None);
            const string activePath = "/save/save.json";
            fileSystem.Seed(activePath, fixture.Original);
            var recovery = new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility,
                fixture.Production, new Dictionary<string, byte[]>(), fixture.LegacyBytes, fixture.Limits,
                RawLimits(), new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());
            var transaction = new DetachedSpatialMigrationTransaction(fileSystem, recovery);

            DetachedSpatialMigrationOutcome executed = transaction.Execute(activePath, fixture.Result.Attempt);

            Assert.That(executed.IsSuccess, Is.True, executed.Reason);
            Assert.That(executed.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.EmptySuccessReason));
            Assert.That(executed.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(executed.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json")), Is.EqualTo(1));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".finalized")), Is.EqualTo(1));

            var restarted = new DetachedSpatialMigrationTransaction(fileSystem, recovery);
            DetachedSpatialMigrationOutcome recovered = restarted.Recover(activePath);

            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Preparation_WrappedEmptySchemasProduceExecutableAttempts(int schema)
        {
            PreparedFixture fixture = PrepareEmptyFixture(schema);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            Assert.That(fixture.Result.Attempt.Descriptor.RawSourceSchemaVersion, Is.EqualTo(schema));
            Assert.That(fixture.Result.Attempt.CandidateSha256,
                Is.EqualTo(SpatialContractSha256.Compute(fixture.Result.Attempt.Candidate.GetBytes())));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void Recovery_NoJournalWrappedLegacyIsNonfatalAndTrusted(int schema)
        {
            PreparedFixture fixture = PrepareEmptyFixture(schema);
            var fileSystem = new DeterministicFileSystem(FailurePoint.None);
            const string activePath = "/save/save.json";
            fileSystem.Seed(activePath, fixture.Original);
            var transaction = new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture));

            DetachedSpatialMigrationOutcome outcome = transaction.Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(outcome.Diagnostics, Does.Contain(DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void PreparationAndRecovery_UnwrappedSchemaOneRemainExecutable()
        {
            PreparedFixture fixture = PrepareEmptyFixture(1, true);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            var fileSystem = new DeterministicFileSystem(FailurePoint.None);
            const string activePath = "/save/save.json";
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
        }

        [Test]
        public void Recovery_MalformedJournalWithVerifiedLegacyQuarantinesEvidence()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(FailurePoint.None);
            const string activePath = "/save/save.json";
            const string malformedPath = "/save/save.gd66-bad.journal.json";
            fileSystem.Seed(activePath, fixture.Original);
            fileSystem.Seed(malformedPath, Encoding.UTF8.GetBytes("{malformed"));

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason,
                Is.EqualTo("gd66.transaction.journal_malformed_with_verified_original"));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.Exists(malformedPath), Is.False);
            Assert.That(fileSystem.Paths.Any(path => path.StartsWith(
                malformedPath + ".quarantine.", StringComparison.Ordinal)), Is.True);
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        private static void Exercise(DeterministicFileSystem fileSystem, FailurePoint point)
        {
            if (point == FailurePoint.Write) { fileSystem.WriteAllBytesDurable("/s/a", new byte[] { 1 }); return; }
            fileSystem.Seed("/s/a", new byte[] { 1 });
            if (point == FailurePoint.Read) { fileSystem.ReadAllBytes("/s/a"); return; }
            if (point == FailurePoint.Replace) { fileSystem.ReplaceSameDirectoryAtomic("/s/a", "/s/b"); return; }
            if (point == FailurePoint.Move) { fileSystem.MoveSameDirectoryAtomic("/s/a", "/s/b"); return; }
            if (point == FailurePoint.Flush) { fileSystem.FlushDirectory("/s"); return; }
            if (point == FailurePoint.Enumerate) { fileSystem.EnumerateFiles("/s", "*", 2); return; }
            fileSystem.IsPathContainedWithoutRedirection("/s", "/s/a");
        }

        private static string Hash(char value) => new string(value, 64);
        private static string TransactionId(char value) => "gd66-" + Hash(value);

        private static PreparedFixture PrepareEmptyFixture(int schema, bool unwrapped = false)
        {
            const string root = "Assets/_Project/Data/Production/DungeonSpatial/";
            TextAsset limitAsset = Asset(root + "validation_limits.json");
            SpatialContentValidationWorkloadLimits contentLimits =
                ProductionSpatialContentWorkloadLimitParser.Parse(limitAsset).Limits;
            ProductionSpatialContentSnapshot production = ProductionSpatialContentLoader.Load(
                Asset(root + "content_manifest.json"), Asset(root + "dungeon_spatial_content.json"),
                new[] { Asset(root + "string_table_en.json") }, limitAsset).Value;
            SpatialLayoutCompatibilityResult compatibilityResult =
                SpatialLayoutCompatibilityProfiles.ParseAndValidate(
                    Asset(SpatialLayoutCompatibilityProfiles.ProductionPath), production, contentLimits);
            Assert.That(compatibilityResult.Success, Is.True);
            SpatialLayoutCompatibilitySnapshot compatibility = compatibilityResult.Value;
            SpatialMigrationCompatibilityProfile profile = compatibility.Value.MigrationProfiles.Single();
            CompatibilityLayoutGeometryRecord geometry = compatibility.Value.GeometryRecords.Single();
            RunSimulationConfig legacy = JsonUtility.FromJson<RunSimulationConfig>(
                Asset("Assets/_Project/Data/Bootstrap/run_simulation_config.json").text);
            byte[] legacyBytes = LegacyGameplayConfigurationContract.SerializeCanonical(legacy);
            byte[] original = Encoding.UTF8.GetBytes(unwrapped ? "{\"saveVersion\":1}" :
                "{\"schema\":\"save_root\",\"schemaVersion\":" + schema + ",\"primary\":{}}");
            RawSavePayloadClassification classification = RawSavePayloadClassifier.Classify(original,
                RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());
            var limits = new CanonicalSpatialSerializationLimits(
                new SpatialSerializedInputLimits(1000000, 100000, 10000, 100000, 100),
                new CanonicalSpatialSaveWorkloadLimits(10000, 10000));
            var descriptor = new SpatialMigrationInputDescriptor(SpatialContractSha256.Compute(original), schema,
                unwrapped ? SpatialRawEnvelopeClassification.UnwrappedSaveData :
                    SpatialRawEnvelopeClassification.WrappedSaveRoot, 7,
                SpatialMigrationContractIdentity.AuthorityMarkerContractVersion,
                SpatialMigrationContractIdentity.MigrationContractVersion, profile.ProfileId,
                profile.ProfileVersion, profile.CanonicalHash, geometry.GeometryId, geometry.GeometryVersion,
                geometry.CanonicalHash,
                SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Manifest)),
                SpatialContractSha256.Compute(ProductionSpatialGeneratedSetParser.SerializeCanonical(production.Catalog)),
                Array.Empty<SpatialValidationInputHash>(), SpatialContractSha256.Compute(legacyBytes),
                SpatialMigrationContractIdentity.CanonicalSerializerId,
                SpatialMigrationContractIdentity.CanonicalSerializerVersion);
            var inputs = new DetachedSpatialMigrationPreparationInputs(original, classification, descriptor,
                compatibility, production, legacy, new Dictionary<string, byte[]>(), limits,
                new DetachedWholeSaveLimits(1000000, 100000, 1000, 100000));
            return new PreparedFixture(original, production, compatibility, legacyBytes, limits,
                DetachedSpatialMigrationPreparer.Prepare(inputs));
        }

        private static DetachedSpatialMigrationRecoveryContext Recovery(PreparedFixture fixture) =>
            new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility, fixture.Production,
                new Dictionary<string, byte[]>(), fixture.LegacyBytes, fixture.Limits, RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            Enumerable.Range(0, 4).Select(index => new RawLegacyBlankFloorNodeContract(
                0, index, "slot." + index, "", "", 0)), true, true,
            new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });

        private static RawSavePayloadClassificationLimits RawLimits() =>
            new RawSavePayloadClassificationLimits(100000, 32, 100, 100, 10000, 500000);

        private static TextAsset Asset(string path)
        { TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path); Assert.That(asset, Is.Not.Null, path); return asset; }

        private sealed class PreparedFixture
        {
            internal PreparedFixture(byte[] original, ProductionSpatialContentSnapshot production,
                SpatialLayoutCompatibilitySnapshot compatibility, byte[] legacyBytes,
                CanonicalSpatialSerializationLimits limits, DetachedSpatialMigrationPreparationResult result)
            { Original = original; Production = production; Compatibility = compatibility;
              LegacyBytes = legacyBytes; Limits = limits; Result = result; }
            internal byte[] Original { get; }
            internal ProductionSpatialContentSnapshot Production { get; }
            internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
            internal byte[] LegacyBytes { get; }
            internal CanonicalSpatialSerializationLimits Limits { get; }
            internal DetachedSpatialMigrationPreparationResult Result { get; }
        }

        private enum FailurePoint { None, Write, Read, Replace, Move, Flush, Enumerate, Containment }

        private sealed class DeterministicFileSystem : ISpatialMigrationFileSystem
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            private readonly FailurePoint failure;
            internal DeterministicFileSystem(FailurePoint failure) { this.failure = failure; }
            internal void Seed(string path, byte[] bytes) { files[path] = (byte[])bytes.Clone(); }
            internal IEnumerable<string> Paths => files.Keys.ToArray();
            public bool Exists(string path) => files.ContainsKey(path);
            public byte[] ReadAllBytes(string path)
            { Fail(FailurePoint.Read); return (byte[])files[path].Clone(); }
            public void WriteAllBytesDurable(string path, byte[] bytes)
            { Fail(FailurePoint.Write); files.Add(path, (byte[])bytes.Clone()); }
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath)
            { Fail(FailurePoint.Replace); files[activePath] = files[stagingPath]; files.Remove(stagingPath); }
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath)
            { Fail(FailurePoint.Move); files.Add(destinationPath, files[sourcePath]); files.Remove(sourcePath); }
            public void FlushDirectory(string directoryPath) { Fail(FailurePoint.Flush); }
            public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern,
                int maximumResults)
            {
                Fail(FailurePoint.Enumerate);
                int wildcard = searchPattern.IndexOf('*');
                string prefix = wildcard < 0 ? searchPattern : searchPattern.Substring(0, wildcard);
                string suffix = wildcard < 0 ? "" : searchPattern.Substring(wildcard + 1);
                return files.Keys.Where(path => Path.GetDirectoryName(path) == directoryPath &&
                    Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal) &&
                    Path.GetFileName(path).EndsWith(suffix, StringComparison.Ordinal))
                    .OrderBy(value => value).Take(maximumResults).ToArray();
            }
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
            { Fail(FailurePoint.Containment); return path.StartsWith(directoryPath + "/", StringComparison.Ordinal); }
            private void Fail(FailurePoint point) { if (failure == point) throw new IOException(point.ToString()); }
        }
    }
}
#endif
