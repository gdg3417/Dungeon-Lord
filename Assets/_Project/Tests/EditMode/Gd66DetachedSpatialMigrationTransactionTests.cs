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
        public void GenericRuntimeFileSystem_CannotClaimDirectoryDurability()
        {
            Assert.That(() => new RuntimeSpatialMigrationFileSystem().FlushDirectory(Path.GetTempPath()),
                Throws.TypeOf<PlatformNotSupportedException>());
        }

        [Test]
        public void RuntimeFileSystem_ReplacedIsNotTerminalSuccess()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            string root = Path.GetDirectoryName(activePath);
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllBytes(activePath, fixture.Original);
                var transaction = new DetachedSpatialMigrationTransaction(
                    new RuntimeSpatialMigrationFileSystem(), Recovery(fixture));

                DetachedSpatialMigrationOutcome outcome =
                    transaction.Execute(activePath, fixture.Result.Attempt);

                Assert.That(outcome.IsSuccess, Is.False);
                Assert.That(outcome.Stage, Is.Not.EqualTo(SpatialMigrationJournalStage.DurableVerified));
                Assert.That(outcome.Stage, Is.Not.EqualTo(SpatialMigrationJournalStage.Finalized));
                Assert.That(outcome.Reason, Is.Not.EqualTo(DetachedSpatialMigrationTransaction.SuccessReason));
                Assert.That(outcome.Reason, Is.Not.EqualTo(DetachedSpatialMigrationTransaction.EmptySuccessReason));
                byte[] active = File.ReadAllBytes(activePath);
                Assert.That(active.SequenceEqual(fixture.Original) ||
                    active.SequenceEqual(fixture.Result.Attempt.Candidate.GetBytes()), Is.True);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void ReplacedPendingDurability_RetryDoesNotRewriteCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, -1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            var transaction = new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture));

            DetachedSpatialMigrationOutcome first = transaction.Execute(activePath, fixture.Result.Attempt);
            int activeReplacements = fileSystem.Operations.Count(operation => operation.Type ==
                OperationType.Replace && PathComparer().Equals(operation.Paths[1], activePath));
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            AssertPendingDurability(first, fixture, fileSystem, activePath);
            AssertPendingDurability(retry, fixture, fileSystem, activePath);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(activeReplacements));
        }

        [Test]
        public void ReplacedPendingDurability_DurableFilesystemResumesToFinalized()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, -1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".restore",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".restore.intent",
                StringComparison.Ordinal)), Is.EqualTo(1));
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome recovered =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Any(path => path.EndsWith(".restore",
                StringComparison.Ordinal) || path.EndsWith(".restore.intent", StringComparison.Ordinal)),
                Is.False);
            Assert.That(fileSystem.Paths.Count(path => Path.GetFileName(path).StartsWith(
                "gd66-quarantine-", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void PinFailure_ConfigChangedWithCandidateAndBackupRestoresOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, -1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome outcome = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture, new byte[] { 1 })).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void PinFailure_ConfigChangedWithCandidateAndMissingBackupTrustsJournalBoundCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, -1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            fileSystem.DisableFailure();
            string backup = fileSystem.Paths.Single(path => path.EndsWith(".original.bak",
                StringComparison.Ordinal));
            fileSystem.DeleteFile(backup);

            DetachedSpatialMigrationOutcome outcome = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture, new byte[] { 1 })).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason,
                Is.EqualTo(DetachedSpatialMigrationTransaction.RollbackSourceMissingReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Replaced));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
        }

        [Test]
        public void PrepareExecuteRecover_NormalizedPlatformPathPersistsAndTrustsCanonicalCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            Assert.That(fixture.Result.Attempt.IsEmptyMigration, Is.True);

            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            var recovery = new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility,
                fixture.Production, new Dictionary<string, byte[]>(), fixture.LegacyBytes, fixture.Limits,
                RawLimits(), new RawSaveEnvelopeVersionContract(1, 6), BlankFloor(), WholeLimits());
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            string malformedPath = Path.Combine(Path.GetDirectoryName(activePath), "save.gd66-bad.journal.json");
            fileSystem.Seed(activePath, fixture.Original);
            fileSystem.Seed(malformedPath, Encoding.UTF8.GetBytes("{malformed"));

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason,
                Is.EqualTo("gd66.transaction.journal_malformed_with_verified_original"));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.Exists(malformedPath), Is.False);
            Assert.That(fileSystem.Paths.Any(path => Path.GetFileName(path).Contains(
                ".gd66-quarantine-")), Is.True);
            Assert.That(fileSystem.Operations.Count(operation =>
                operation.Type == OperationType.Enumerate), Is.EqualTo(1));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void Recovery_NoJournalMalformedEffectiveRouteIsNotTrustedAsLegacyOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            byte[] malformed = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{" +
                "\"mvpDungeonPlacements\":{\"Entries\":{},\"NextRevision\":0}}}");
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, malformed);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationPreparer.OutcomeMismatchReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(malformed));
            Assert.That(fileSystem.Paths, Has.Count.EqualTo(1));
        }

        [TestCase("{malformed", RawSavePayloadClassifier.UnreadableReason)]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":0,\"primary\":{}}",
            "gd66.payload.unsupported_legacy_version")]
        public void Recovery_NoJournalInvalidLegacyPreservesExactClassifierReason(string json,
            string expectedReason)
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            byte[] original = Encoding.UTF8.GetBytes(json);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(expectedReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(original));
        }

        [Test]
        public void Recovery_NoJournalLegacyWorkloadFailurePreservesExactReason()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            byte[] original = Encoding.UTF8.GetBytes("{\"saveVersion\":1,\"padding\":\"" +
                new string('x', 100001) + "\"}");
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.Reason, Is.EqualTo(RawSavePayloadClassifier.WorkloadExceededReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(original));
        }

        [Test]
        public void Execute_FirstJournalWriteFailurePreservesVerifiedOriginalAndOperationIndex()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Write, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.BackupFailedReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Operations.Any(value => value.Type == OperationType.Write &&
                value.Index == 1), Is.True);
        }

        [Test]
        public void Recovery_EnumerationFailureWithOriginal_PreservesOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Enumerate, -1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.PathInvalidReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void Recovery_RedirectedEvidenceContainmentFailure_PreservesOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Containment, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            string evidencePath = Path.Combine(Path.GetDirectoryName(activePath),
                "save.gd66-redirected.original.bak");
            fileSystem.Seed(activePath, fixture.Original);
            fileSystem.Seed(evidencePath, fixture.Original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.PathInvalidReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(evidencePath), Is.True);
        }

        [Test]
        public void Recovery_EnumerationFailureWithCandidate_IsNonterminal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialMigrationOutcome executed =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            Assert.That(executed.IsSuccess, Is.True);
            fileSystem.EnableFailure(OperationType.Enumerate, -1);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.PathInvalidReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
        }

        [Test]
        public void Recovery_NoLiveJournalWithCandidate_IsAlreadyCommitted()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Result.Attempt.Candidate.GetBytes());

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            Assert.That(outcome.Stage, Is.Null);
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
        }





        [Test]
        public void Recovery_CandidateVerifiedSuccessfulFinalizationPreservesChronologicalDiagnostics()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.CandidateVerified,
                includeBackup: true, includeStaging: true, activeCandidate: false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True, outcome.Reason);
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            {
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic,
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic
            }));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
        }

        [Test]
        public void Recovery_CandidateVerifiedReplacementFailurePreservesStagedDiagnostic()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Replace, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.CandidateVerified,
                includeBackup: true, includeStaging: true, activeCandidate: false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void Recovery_CandidateVerifiedWithValidStagedCandidateAndMissingBackupDoesNotReplaceOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: false, includeStaging: true,
                activeCandidate: false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.BackupFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(Path.Combine(Path.GetDirectoryName(activePath), names.CandidateStaging)), Is.True);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [Test]
        public void Recovery_ReplacedExactCandidateMissingBackupFlushFailureKeepsCandidatePending()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.Replaced,
                includeBackup: false, includeStaging: false, activeCandidate: true);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Replaced));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [Test]
        public void Recovery_DurableVerifiedExactCandidateMissingBackupFinalizesWithoutRewriteCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.DurableVerified,
                includeBackup: false, includeStaging: false, activeCandidate: true);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True, outcome.Reason);
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [Test]
        public void Recovery_NoLiveJournalWithInvalidCanonicalTargetIsContradictory()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            byte[] invalidTarget = Encoding.UTF8.GetBytes(
                "{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{}}");
            fileSystem.Seed(activePath, invalidTarget);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.ContradictoryAuthorityReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(invalidTarget));
        }







        [TestCase(SpatialMigrationJournalStage.DescriptorPinned, false, false)]
        [TestCase(SpatialMigrationJournalStage.BackupVerified, true, false)]
        [TestCase(SpatialMigrationJournalStage.CandidateVerified, true, true)]
        [TestCase(SpatialMigrationJournalStage.Replaced, true, false)]
        public void Execute_RepairedOriginalQuarantinesStaleLiveJournalAtStage(
            SpatialMigrationJournalStage stage, bool includeBackup, bool includeStaging)
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name + stage);
            MaterializeJournal(fileSystem, stale, activePath, stage, includeBackup, includeStaging,
                activeCandidate: false);
            fileSystem.Seed(activePath, repaired.Original);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(first.Stage, Is.EqualTo(stage));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(repaired.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json", StringComparison.Ordinal)),
                Is.EqualTo(0));

            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json", StringComparison.Ordinal)),
                Is.EqualTo(1));
        }



        [Test]
        public void Execute_StaleReceiptCleanupUsesContainedReceiptPath()
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, stale, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: false, includeStaging: false,
                activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            fileSystem.Seed(receiptPath, Encoding.UTF8.GetBytes("{malformed"));
            fileSystem.Seed(activePath, repaired.Original);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.Journal)), Is.False);
            Assert.That(fileSystem.Exists(receiptPath), Is.False);
            Assert.That(fileSystem.Operations.SelectMany(operation => operation.Paths),
                Has.None.EqualTo(names.FinalizedReceipt));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(repaired.Original));
        }

        [Test]
        public void Execute_StaleReceiptMoveFailureLeavesContainedReceiptButRemovesJournal()
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem(OperationType.Move, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, stale, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: false, includeStaging: false,
                activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            fileSystem.Seed(receiptPath, Encoding.UTF8.GetBytes("{malformed"));
            fileSystem.Seed(activePath, repaired.Original);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.Journal)), Is.False);
            Assert.That(fileSystem.Exists(receiptPath), Is.True);
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(repaired.Original));
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
        }

        [Test]
        public void Execute_DurableVerifiedFinalizedWriteFailureRetriesWithoutReplacingCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Replace, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.DurableVerified,
                includeBackup: false, includeStaging: false, activeCandidate: true);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            int replacementsAfterFailure = fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath));
            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.FinalizationFailedReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(first.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic));
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(replacementsAfterFailure));
        }

        [Test]
        public void Execute_StaleJournalMoveFailureLeavesSidecarsAndRetriesSameStaleJournal()
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem(OperationType.Move, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, stale, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true, includeStaging: true,
                activeCandidate: false);
            fileSystem.Seed(activePath, repaired.Original);
            string directory = Path.GetDirectoryName(activePath);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.Journal)), Is.True);
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.OriginalBackup)), Is.True);
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.CandidateStaging)), Is.True);
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(repaired.Original));
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(retry.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.Journal)), Is.False);
        }

        [Test]
        public void Execute_StaleSidecarMoveFailureAfterJournalRemovalAllowsRetryToCreateAttempt()
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem(OperationType.Move, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, stale, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true, includeStaging: true,
                activeCandidate: false);
            fileSystem.Seed(activePath, repaired.Original);
            string directory = Path.GetDirectoryName(activePath);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.Journal)), Is.False);
            Assert.That(fileSystem.Exists(Path.Combine(directory, names.OriginalBackup)) ||
                fileSystem.Exists(Path.Combine(directory, names.CandidateStaging)), Is.True);
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
        }

        [Test]
        public void Recovery_SelfValidCandidateKeepsAuthorityWhenMalformedJournalCleanupMoveFails()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Move, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            string malformedPath = Path.Combine(Path.GetDirectoryName(activePath), "save.gd66-bad.journal.json");
            byte[] candidate = fixture.Result.Attempt.Candidate.GetBytes();
            byte[] malformed = Encoding.UTF8.GetBytes("{malformed");
            fileSystem.Seed(activePath, candidate);
            fileSystem.Seed(malformedPath, malformed);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True, outcome.Reason);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.AlreadyCommittedReason));
            Assert.That(outcome.Stage, Is.Null);
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(candidate));
            Assert.That(fileSystem.Exists(malformedPath), Is.True);
        }

        [Test]
        public void Execute_RepairedOriginalQuarantinesStaleLiveJournalBeforeRetryCreatesNewAttempt()
        {
            PreparedFixture stale = PrepareEmptyFixture(6);
            PreparedFixture repaired = PrepareEmptyFixture(5);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, stale, activePath, SpatialMigrationJournalStage.BackupVerified,
                includeBackup: true, includeStaging: false, activeCandidate: false);
            fileSystem.Seed(activePath, repaired.Original);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.StaleJournalOriginalMismatchReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.BackupVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(repaired.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json", StringComparison.Ordinal)),
                Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Any(path => Path.GetFileName(path).Contains(
                ".gd66-quarantine-")), Is.True);

            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(repaired))
                    .Execute(activePath, repaired.Result.Attempt);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(repaired.Result.Attempt.Candidate.GetBytes()));
        }

        [Test]
        public void Execute_CandidateVerifiedWithValidStagedCandidateAndMissingBackupUsesRecoveryPolicy()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.CandidateVerified,
                includeBackup: false, includeStaging: true, activeCandidate: false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.BackupFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json", StringComparison.Ordinal)),
                Is.EqualTo(1));
        }

        [Test]
        public void Execute_ReplacedExactCandidateMissingBackupFlushFailureUsesRecoveryPolicy()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Flush, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.Replaced,
                includeBackup: false, includeStaging: false, activeCandidate: true);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Replaced));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [Test]
        public void Execute_DurableVerifiedExactCandidateMissingBackupFinalizesWithoutReplacement()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.DurableVerified,
                includeBackup: false, includeStaging: false, activeCandidate: true);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.True, outcome.Reason);
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"unknown\":\"spatialFloors\"}}")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"unknown\":\"\\\"schemaVersion\\\":7\"}}")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"primary\":{\"unknown\":{\"spatialFloors\":[]}}}")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"spatialFloors\":[],\"primary\":{}}")]
        public void Recovery_StructuralClassifierIgnoresLegacyFalsePositiveCanonicalNames(string json)
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            fileSystem.Seed(activePath, bytes);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True, outcome.Reason);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(bytes));
        }

        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"spatialFloors\":[]}", "gd66.payload.missing_primary")]
        [TestCase("{\"schema\":\"save_root\",\"schemaVersion\":6,\"spatialFloors\":[],\"primary\":1}", RawSavePayloadClassifier.InvalidPrimaryReason)]
        public void Recovery_StructuralClassifierPreservesWrappedPrimaryReasons(string json, string expectedReason)
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            fileSystem.Seed(activePath, bytes);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(expectedReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(bytes));
        }

        [Test]
        public void Recovery_MalformedUtf8PreservesUnreadableReason()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            byte[] bytes = { 0xff, 0xfe, 0xfd };
            fileSystem.Seed(activePath, bytes);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(RawSavePayloadClassifier.UnreadableReason));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.None));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(bytes));
        }

        private static string Hash(char value) => new string(value, 64);
        private static string TransactionId(char value) => "gd66-" + Hash(value);
        private static StringComparer PathComparer() => Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        private static SpatialMigrationSidecarNames MaterializeJournal(DeterministicFileSystem fileSystem,
            PreparedFixture fixture, string activePath, SpatialMigrationJournalStage stage, bool includeBackup,
            bool includeStaging, bool activeCandidate)
        {
            string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                fixture.Result.Attempt.Descriptor, Limits);
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256, fingerprint);
            string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(activePath), transactionId).Value;
            var journal = new SpatialMigrationJournal(SpatialMigrationContractIdentity.JournalSchemaVersion,
                fixture.Result.Attempt.Descriptor, fingerprint, identity, transactionId, names.Journal,
                names.OriginalBackup, names.CandidateStaging,
                stage == SpatialMigrationJournalStage.DurableVerified || stage == SpatialMigrationJournalStage.Finalized
                    ? names.FinalizedReceipt : null,
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256,
                includeBackup ? fixture.Result.Attempt.Descriptor.OriginalPayloadSha256 : null,
                (int)stage >= (int)SpatialMigrationJournalStage.CandidateVerified
                    ? fixture.Result.Attempt.CandidateSha256 : null,
                stage);
            byte[] journalBytes = SpatialMigrationJournalContracts.Serialize(journal, Limits).Value;
            Assert.That(SpatialMigrationJournalContracts.Parse(journalBytes, Limits).IsValid, Is.True);
            string directory = Path.GetDirectoryName(activePath);
            fileSystem.Seed(activePath, activeCandidate ? fixture.Result.Attempt.Candidate.GetBytes() : fixture.Original);
            fileSystem.Seed(Path.Combine(directory, names.Journal), journalBytes);
            if (includeBackup) fileSystem.Seed(Path.Combine(directory, names.OriginalBackup), fixture.Original);
            if (includeStaging) fileSystem.Seed(Path.Combine(directory, names.CandidateStaging),
                fixture.Result.Attempt.Candidate.GetBytes());
            return names;
        }
        private static void AssertPendingDurability(DetachedSpatialMigrationOutcome outcome,
            PreparedFixture fixture, DeterministicFileSystem fileSystem, string activePath)
        {
            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.Replaced));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
        }
        private static string ActivePath(string identity)
        {
            string safe = new string(identity.Select(character => char.IsLetterOrDigit(character) ?
                character : '-').ToArray());
            return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DungeonLord-GD66-Tests", safe,
                "save.json"));
        }

        internal static DetachedSpatialMigrationPreparationResult PrepareSemanticResult(int schema,
            string primaryMembers)
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":" + schema + "," +
                "\"primary\":{" + primaryMembers + "}}");
            return PrepareEmptyFixture(schema, false, original).Result;
        }

        internal sealed class SemanticFixtureExecution
        {
            internal RawSavePayloadClassification Classification;
            internal DetachedPreparedSpatialMigrationAttempt Attempt;
            internal DetachedCanonicalSpatialSaveState State;
            internal string BasicRoomDefinitionId;
            internal DetachedSpatialMigrationOutcome Execute;
            internal DetachedSpatialMigrationOutcome FirstRecovery;
            internal DetachedSpatialMigrationOutcome SecondRecovery;
            internal DetachedCurrentTargetValidationContext CurrentContext;
            internal DetachedUnfinishedAttemptValidationContext UnfinishedContext;
            internal ProductionSpatialContentSnapshot Production;
            internal SpatialLayoutCompatibilitySnapshot Compatibility;
            internal CanonicalSpatialSerializationLimits Limits;
            internal DetachedWholeSaveLimits WholeLimits;
            internal byte[] LegacyBytes;
            internal IReadOnlyDictionary<string, byte[]> ValidationInputs;
        }

        internal static SemanticFixtureExecution RunPopulatedSemanticFixture(string identity, int schema,
            string primaryMembers)
        {
            byte[] original = Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":" + schema + "," +
                "\"primary\":{" + primaryMembers + "}}");
            PreparedFixture fixture = PrepareEmptyFixture(schema, false, original);
            Assert.That(fixture.Result.IsSuccess, Is.True, fixture.Result.Reason);
            Assert.That(fixture.Result.Attempt.IsEmptyMigration, Is.False);
            byte[] separatelyAllocatedOriginal = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original));
            PreparedFixture equivalent = PrepareEmptyFixture(schema, false, separatelyAllocatedOriginal);
            Assert.That(equivalent.Result.Attempt.Candidate.GetBytes(),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(equivalent.Result.Attempt.CandidateSha256,
                Is.EqualTo(fixture.Result.Attempt.CandidateSha256));
            Assert.That(equivalent.Result.Attempt.Descriptor.RawSourceSchemaVersion,
                Is.EqualTo(fixture.Result.Attempt.Descriptor.RawSourceSchemaVersion));
            Assert.That(equivalent.Result.Attempt.Descriptor.MigrationProfileId,
                Is.EqualTo(fixture.Result.Attempt.Descriptor.MigrationProfileId));
            Assert.That(equivalent.Result.Attempt.Descriptor.MigrationProfileVersion,
                Is.EqualTo(fixture.Result.Attempt.Descriptor.MigrationProfileVersion));
            Assert.That(equivalent.Result.Attempt.Diagnostics, Is.EqualTo(fixture.Result.Attempt.Diagnostics));
            Assert.That(equivalent.Result.Attempt.DescriptorFingerprint,
                Is.EqualTo(fixture.Result.Attempt.DescriptorFingerprint));
            Assert.That(equivalent.Result.Attempt.TransactionId,
                Is.EqualTo(fixture.Result.Attempt.TransactionId));
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath("semantic-" + identity);
            fileSystem.Seed(activePath, original);
            var currentContext = new DetachedCurrentTargetValidationContext(fixture.Compatibility,
                fixture.Production, fixture.LegacyBytes, fixture.Limits);
            DetachedCompleteSaveValidationResult parsed = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(
                fixture.Result.Attempt.Candidate.GetBytes(), currentContext);
            Assert.That(parsed.IsValid, Is.True, parsed.Reason);
            DetachedSpatialMigrationOutcome executed =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);
            Assert.That(executed.IsSuccess, Is.True, executed.Reason);
            Assert.That(executed.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            DetachedSpatialMigrationOutcome recovered =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);
            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            DetachedSpatialMigrationOutcome recoveredAgain =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);
            Assert.That(recoveredAgain.IsSuccess, Is.True, recoveredAgain.Reason);
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            CompatibilityLayoutGeometryRecord geometry = fixture.Compatibility.Value.GeometryRecords.Single(value =>
                value.GeometryId == fixture.Result.Attempt.Descriptor.SharedGeometryId &&
                value.GeometryVersion == fixture.Result.Attempt.Descriptor.SharedGeometryVersion);
            DetachedSpatialMigrationRecoveryContext recoveryContext = Recovery(fixture);
            Assert.That(recoveryContext.TryCreateUnfinishedValidationContext(fixture.Result.Attempt.Descriptor,
                fixture.Result.Attempt.TransactionId, fixture.Result.Attempt.DescriptorFingerprint,
                fixture.Result.Attempt.CandidateSha256, out DetachedUnfinishedAttemptValidationContext unfinished), Is.True);
            return new SemanticFixtureExecution { Classification = fixture.Classification,
                Attempt = fixture.Result.Attempt, State = parsed.State, Execute = executed,
                FirstRecovery = recovered, SecondRecovery = recoveredAgain,
                BasicRoomDefinitionId = geometry.BasicRoomDefinitionId, CurrentContext = currentContext,
                UnfinishedContext = unfinished, Production = fixture.Production, Compatibility = fixture.Compatibility,
                Limits = fixture.Limits, WholeLimits = WholeLimits(),
                LegacyBytes = fixture.LegacyBytes, ValidationInputs = new Dictionary<string, byte[]>() };
        }

        private static PreparedFixture PrepareEmptyFixture(int schema, bool unwrapped = false,
            byte[] originalOverride = null)
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
            CompatibilitySelectionResult<CanonicalLayoutContractSelection> selectedContract =
                compatibility.SelectContract(7);
            Assert.That(selectedContract.Success, Is.True);
            CompatibilitySelectionResult<SpatialMigrationCompatibilityProfile> selectedProfile =
                compatibility.SelectMigration(schema, 7, selectedContract.Value.CanonicalLayoutContractVersion);
            Assert.That(selectedProfile.Success, Is.True);
            SpatialMigrationCompatibilityProfile profile = selectedProfile.Value;
            CompatibilityLayoutGeometryRecord geometry = compatibility.Value.GeometryRecords.FirstOrDefault(
                value => value.GeometryId == profile.GeometryId &&
                    value.GeometryVersion == profile.GeometryVersion &&
                    value.CanonicalHash == profile.GeometryCanonicalHash);
            Assert.That(geometry, Is.Not.Null);
            RunSimulationConfig legacy = JsonUtility.FromJson<RunSimulationConfig>(
                Asset("Assets/_Project/Data/Bootstrap/run_simulation_config.json").text);
            byte[] legacyBytes = LegacyGameplayConfigurationContract.SerializeCanonical(legacy);
            byte[] original = originalOverride ?? Encoding.UTF8.GetBytes(unwrapped ? "{\"saveVersion\":1}" :
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
                WholeLimits());
            return new PreparedFixture(original, classification, production, compatibility, legacyBytes, limits,
                DetachedSpatialMigrationPreparer.Prepare(inputs));
        }

        private static DetachedSpatialMigrationRecoveryContext Recovery(PreparedFixture fixture,
            byte[] legacyBytes = null) =>
            new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility, fixture.Production,
                new Dictionary<string, byte[]>(), legacyBytes ?? fixture.LegacyBytes, fixture.Limits, RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor(), WholeLimits());

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            Enumerable.Range(0, 4).Select(index => new RawLegacyBlankFloorNodeContract(
                0, index, "slot." + index, "", "", 0)), true, true,
            new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });

        private static RawSavePayloadClassificationLimits RawLimits() =>
            new RawSavePayloadClassificationLimits(100000, 32, 100, 100, 10000, 500000);
        private static DetachedWholeSaveLimits WholeLimits() =>
            new DetachedWholeSaveLimits(1000000, 100000, 1000, 100000);

        private static TextAsset Asset(string path)
        { TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path); Assert.That(asset, Is.Not.Null, path); return asset; }

        private sealed class PreparedFixture
        {
            internal PreparedFixture(byte[] original, RawSavePayloadClassification classification,
                ProductionSpatialContentSnapshot production,
                SpatialLayoutCompatibilitySnapshot compatibility, byte[] legacyBytes,
                CanonicalSpatialSerializationLimits limits, DetachedSpatialMigrationPreparationResult result)
            { Original = original; Classification = classification; Production = production; Compatibility = compatibility;
              LegacyBytes = legacyBytes; Limits = limits; Result = result; }
            internal byte[] Original { get; }
            internal RawSavePayloadClassification Classification { get; }
            internal ProductionSpatialContentSnapshot Production { get; }
            internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
            internal byte[] LegacyBytes { get; }
            internal CanonicalSpatialSerializationLimits Limits { get; }
            internal DetachedSpatialMigrationPreparationResult Result { get; }
        }

        private enum OperationType { Exists, Read, Write, Replace, Move, Delete, Flush, Enumerate, Containment }

        private sealed class FileOperation
        {
            internal FileOperation(OperationType type, int index, params string[] paths)
            { Type = type; Index = index; Paths = paths; }
            internal OperationType Type { get; }
            internal int Index { get; }
            internal string[] Paths { get; }
        }

        private sealed class DeterministicFileSystem : ISpatialMigrationFileSystem
        {
            private static readonly StringComparer PathComparer = Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            private readonly Dictionary<string, byte[]> files =
                new Dictionary<string, byte[]>(PathComparer);
            private readonly Dictionary<OperationType, int> counts = new Dictionary<OperationType, int>();
            private readonly List<FileOperation> operations = new List<FileOperation>();
            private OperationType? failureType;
            private int failureIndex;
            internal DeterministicFileSystem(OperationType? failureType = null, int failureIndex = 0)
            { this.failureType = failureType; this.failureIndex = failureIndex; }
            internal void DisableFailure() { failureType = null; failureIndex = 0; }
            internal void EnableFailure(OperationType type, int index)
            { failureType = type; failureIndex = index; }
            internal void Seed(string path, byte[] bytes) { files[Normalize(path)] = (byte[])bytes.Clone(); }
            internal IEnumerable<string> Paths => files.Keys.OrderBy(value => value, PathComparer).ToArray();
            internal IEnumerable<FileOperation> Operations => operations.ToArray();
            public bool Exists(string path)
            { path = Normalize(path); Record(OperationType.Exists, path); return files.ContainsKey(path); }
            public byte[] ReadAllBytes(string path)
            { path = Normalize(path); Record(OperationType.Read, path); return (byte[])files[path].Clone(); }
            public void WriteAllBytesDurable(string path, byte[] bytes)
            { path = Normalize(path); Record(OperationType.Write, path); files.Add(path, (byte[])bytes.Clone()); }
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath)
            {
                stagingPath = Normalize(stagingPath); activePath = Normalize(activePath);
                Record(OperationType.Replace, stagingPath, activePath); SameDirectory(stagingPath, activePath);
                files[activePath] = (byte[])files[stagingPath].Clone(); files.Remove(stagingPath);
            }
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath)
            {
                sourcePath = Normalize(sourcePath); destinationPath = Normalize(destinationPath);
                Record(OperationType.Move, sourcePath, destinationPath); SameDirectory(sourcePath, destinationPath);
                files.Add(destinationPath, (byte[])files[sourcePath].Clone()); files.Remove(sourcePath);
            }
            public void DeleteFile(string path)
            { path = Normalize(path); Record(OperationType.Delete, path); files.Remove(path); }
            public void FlushDirectory(string directoryPath)
            { directoryPath = Normalize(directoryPath); Record(OperationType.Flush, directoryPath); }
            public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern,
                int maximumResults)
            {
                directoryPath = Normalize(directoryPath); Record(OperationType.Enumerate, directoryPath);
                int wildcard = searchPattern.IndexOf('*');
                string prefix = wildcard < 0 ? searchPattern : searchPattern.Substring(0, wildcard);
                string suffix = wildcard < 0 ? "" : searchPattern.Substring(wildcard + 1);
                string[] matches = files.Keys.Where(path => PathComparer.Equals(Path.GetDirectoryName(path), directoryPath) &&
                    Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal) &&
                    Path.GetFileName(path).EndsWith(suffix, StringComparison.Ordinal))
                    .OrderBy(value => value, PathComparer).ToArray();
                if (matches.Length > maximumResults) throw new IOException("enumeration limit");
                return matches;
            }
            public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
            {
                directoryPath = Normalize(directoryPath); path = Normalize(path);
                Record(OperationType.Containment, directoryPath, path);
                string prefix = directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString(),
                    StringComparison.Ordinal) ? directoryPath : directoryPath + Path.DirectorySeparatorChar;
                return path.StartsWith(prefix, Path.DirectorySeparatorChar == '\\' ?
                    StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            }
            private static string Normalize(string path) => Path.GetFullPath(path);
            private static void SameDirectory(string left, string right)
            { if (!PathComparer.Equals(Path.GetDirectoryName(left), Path.GetDirectoryName(right))) throw new IOException("cross-directory"); }
            private void Record(OperationType type, params string[] paths)
            {
                int index = counts.TryGetValue(type, out int previous) ? previous + 1 : 1;
                counts[type] = index; operations.Add(new FileOperation(type, index, paths));
                if (failureType == type && (failureIndex < 0 || failureIndex == index))
                    throw new IOException(type + "#" + index);
            }
        }
    }
}
#endif
