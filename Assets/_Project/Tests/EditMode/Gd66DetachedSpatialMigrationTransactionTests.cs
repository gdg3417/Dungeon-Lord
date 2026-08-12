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
        public enum RestorationIntentMismatch
        {
            Malformed,
            TransactionId,
            DescriptorFingerprint,
            OriginalSha,
            BackupSha,
            JournalFilename,
            JournalStage
        }

        public enum DurableCandidateAuthorityMismatch
        {
            CrossTransactionCandidate,
            ExpectedHashMismatch,
            MissingAuthorityMarker,
            NonCanonicalBytes
        }

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

        [TestCase("{{\"TransactionId\":\"{0}\",\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}}")]
        [TestCase("{{\"transactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\"}}")]
        [TestCase("{{\"TransactionId\":\"{0}\",\"DescriptorFingerprintSha256\":\"{1}\",\"CandidateSha256\":\"{2}\",\"FinalStage\":6}}")]
        public void FinalizationReceipt_RejectsDuplicateCaseAmbiguousAndExtraFields(string format)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format(format,
                TransactionId('1'), Hash('2'), Hash('3')));

            Assert.That(DetachedFinalizationReceiptContract.Parse(bytes, Limits), Is.Null);
        }

        [Test]
        public void ActivePath_LongIdentityIsBoundedDeterministicAndDistinct()
        {
            string identity = new string('a', 1024);
            string path = ActivePath(identity);
            string differentPath = ActivePath(new string('b', 1024));
            string quarantine = Path.Combine(Path.GetDirectoryName(path),
                "save.gd66-quarantine-" + Hash('c') + "-" + new string('d', 16) + ".evidence");

            Assert.That(ActivePath(identity), Is.EqualTo(path));
            Assert.That(differentPath, Is.Not.EqualTo(path));
            Assert.That(path, Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(quarantine.Length,
                Is.LessThanOrEqualTo(SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters));
        }

        [Test]
        public void TargetedReplacePredicate_IgnoresPrecedingSinglePathOperations()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(activePath), fixture.Result.Attempt.TransactionId).Value;
            string candidatePath = Path.Combine(Path.GetDirectoryName(activePath), names.CandidateStaging);
            int predicateInvocations = 0;
            fileSystem.EnableTargetedFailure(OperationType.Replace, paths =>
            {
                predicateInvocations++;
                return PathComparer().Equals(paths[0], candidatePath) &&
                    PathComparer().Equals(paths[1], activePath);
            }, 1, false);

            DetachedSpatialMigrationOutcome outcome = new DetachedSpatialMigrationTransaction(
                fileSystem, Recovery(fixture)).Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(fileSystem.Operations.Any(operation => operation.Paths.Length == 1), Is.True);
            Assert.That(predicateInvocations, Is.GreaterThan(0));
            Assert.That(fileSystem.FailedOperation, Is.Not.Null);
            Assert.That(fileSystem.FailedOperation.Type, Is.EqualTo(OperationType.Replace));
            Assert.That(PathComparer().Equals(
                fileSystem.FailedOperation.Paths[0], candidatePath), Is.True);
            Assert.That(PathComparer().Equals(
                fileSystem.FailedOperation.Paths[1], activePath), Is.True);
            Assert.That(fileSystem.FailedTargetOccurrence, Is.EqualTo(1));
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.Replaced,
                includeBackup: false, includeStaging: false, activeCandidate: true);
            EnablePendingDurabilityFailure(fileSystem, activePath);
            var transaction = new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture));

            DetachedSpatialMigrationOutcome first = transaction.Recover(activePath);
            AssertPendingDurabilityFailurePhase(fileSystem, fixture, activePath);
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.Replaced,
                includeBackup: false, includeStaging: false, activeCandidate: true);
            EnablePendingDurabilityFailure(fileSystem, activePath);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);
            AssertPendingDurabilityFailurePhase(fileSystem, fixture, activePath);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome recovered =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(recovered.IsSuccess, Is.True, recovered.Reason);
            Assert.That(recovered.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(recovered.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Any(path => path.EndsWith(".restore",
                StringComparison.Ordinal) || path.EndsWith(".restore.intent", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void PinFailure_ConfigChangedWithCandidateAndBackupRestoresOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.Replaced, includeBackup: false, includeStaging: false,
                activeCandidate: true);
            EnablePendingDurabilityFailure(fileSystem, activePath);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);
            AssertPendingDurabilityFailurePhase(fileSystem, fixture, activePath);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            fileSystem.Seed(Path.Combine(Path.GetDirectoryName(activePath), names.OriginalBackup), fixture.Original);
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
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.Replaced,
                includeBackup: false, includeStaging: false, activeCandidate: true);
            EnablePendingDurabilityFailure(fileSystem, activePath);
            DetachedSpatialMigrationOutcome pending =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);
            AssertPendingDurabilityFailurePhase(fileSystem, fixture, activePath);
            AssertPendingDurability(pending, fixture, fileSystem, activePath);
            fileSystem.DisableFailure();

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
            Assert.That(executed.Diagnostics, Is.EqualTo(new[]
            {
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic,
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic
            }));
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
        public void Recovery_NoJournalLegacyTrustDoesNotDependOnMigrationConfiguration()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            var recoveryContext = new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility,
                fixture.Production, new Dictionary<string, byte[]>(), null, fixture.Limits, RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor(), WholeLimits());

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, recoveryContext).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Reason,
                Is.EqualTo(DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(outcome.Stage, Is.Null);
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths, Is.EqualTo(new[] { activePath }));
            Assert.That(fileSystem.Operations.Any(operation =>
                operation.Type == OperationType.Write ||
                operation.Type == OperationType.Replace ||
                operation.Type == OperationType.Move), Is.False);
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
            Assert.That(fileSystem.Paths.Count(), Is.EqualTo(1));
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
        public void Recovery_CandidateReplacementFailureAfterMutationRestoresOriginalDeterministically()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true, includeStaging: true,
                activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string journalPath = Path.Combine(directory, names.Journal);
            string journalNextPath = journalPath + ".next";
            string backupPath = Path.Combine(directory, names.OriginalBackup);
            string candidatePath = Path.Combine(directory, names.CandidateStaging);
            string restoreStaging = backupPath + ".restore";
            string intentPath = backupPath + ".restore.intent";
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            StringComparer comparer = PathComparer();
            SpatialMigrationJournal initialJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            byte[] intentBytes = DetachedRestorationIntentContract.Serialize(new DetachedRestorationIntent(
                initialJournal.TransactionId, initialJournal.DescriptorFingerprintSha256,
                initialJournal.OriginalPayloadSha256, SpatialContractSha256.Compute(fixture.Original),
                initialJournal.RelativeJournalFilename, (int)initialJournal.Stage), Limits);
            string intentQuarantinePath = QuarantinePath(directory, intentPath, intentBytes);
            fileSystem.EnableTargetedFailure(OperationType.Replace,
                paths => comparer.Equals(paths[0], candidatePath) &&
                    comparer.Equals(paths[1], activePath), 1, afterMutation: true);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(first.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.RecoveredOriginalReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(first.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            FileOperation failed = fileSystem.FailedOperation;
            Assert.That(failed, Is.Not.Null);
            Assert.That(failed.Type, Is.EqualTo(OperationType.Replace));
            Assert.That(failed.Paths[0], Is.EqualTo(candidatePath));
            Assert.That(failed.Paths[1], Is.EqualTo(activePath));
            Assert.That(failed.MutationCompleted, Is.True);
            Assert.That(failed.FailedAfterMutation, Is.True);
            Assert.That(fileSystem.FailedTargetOccurrence, Is.EqualTo(1));
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(fixture.Original));
            SpatialMigrationJournal liveJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(liveJournal.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(liveJournal.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(liveJournal.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
            Assert.That(fileSystem.Exists(candidatePath), Is.False);
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(intentPath), Is.False);
            Assert.That(fileSystem.Exists(intentQuarantinePath), Is.True);
            Assert.That(fileSystem.Exists(receiptPath), Is.False);
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(2));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(retry.Stage, Is.Null);
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(2));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            SpatialMigrationJournal retainedJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(retainedJournal.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retainedJournal.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(retainedJournal.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
        }

        [Test]
        public void Recovery_RestoreReplacementFailureAfterMutationConvergesWithoutSecondAuthorityChange()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string journalPath = Path.Combine(directory, names.Journal);
            string journalNextPath = journalPath + ".next";
            string backupPath = Path.Combine(directory, names.OriginalBackup);
            string candidatePath = Path.Combine(directory, names.CandidateStaging);
            string restoreStaging = backupPath + ".restore";
            string intentPath = backupPath + ".restore.intent";
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            StringComparer comparer = PathComparer();
            SpatialMigrationJournal initialJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            fileSystem.EnableTargetedFailure(OperationType.Replace,
                paths => comparer.Equals(paths[0], restoreStaging) &&
                    comparer.Equals(paths[1], activePath), 1, afterMutation: true);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            FileOperation failed = fileSystem.FailedOperation;
            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(first.Diagnostics, Is.EqualTo(new[]
            {
                DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic,
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic
            }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(failed, Is.Not.Null);
            Assert.That(failed.Type, Is.EqualTo(OperationType.Replace));
            Assert.That(failed.Paths[0], Is.EqualTo(restoreStaging));
            Assert.That(failed.Paths[1], Is.EqualTo(activePath));
            Assert.That(failed.MutationCompleted, Is.True);
            Assert.That(failed.FailedAfterMutation, Is.True);
            Assert.That(fileSystem.FailedTargetOccurrence, Is.EqualTo(1));
            SpatialMigrationJournal liveBeforeRetry =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(liveBeforeRetry.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(liveBeforeRetry.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(liveBeforeRetry.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(intentPath), Is.True);
            DetachedRestorationIntent intent =
                DetachedRestorationIntentContract.Parse(fileSystem.ReadAllBytes(intentPath), Limits);
            Assert.That(intent, Is.Not.Null);
            Assert.That(intent.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(intent.DescriptorFingerprint,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
            Assert.That(intent.OriginalSha256, Is.EqualTo(initialJournal.OriginalPayloadSha256));
            Assert.That(intent.BackupSha256, Is.EqualTo(initialJournal.OriginalPayloadSha256));
            Assert.That(intent.JournalFilename, Is.EqualTo(initialJournal.RelativeJournalFilename));
            Assert.That(intent.JournalStage, Is.EqualTo((int)SpatialMigrationJournalStage.DurableVerified));
            Assert.That(fileSystem.Exists(candidatePath), Is.False);
            Assert.That(fileSystem.Exists(receiptPath), Is.False);
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Any(path => path.IndexOf(".gd66-quarantine-",
                StringComparison.Ordinal) >= 0), Is.False);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(retry.IsSuccess, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            SpatialMigrationJournal liveAfterRetry =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(liveAfterRetry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(liveAfterRetry.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(liveAfterRetry.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(candidatePath), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
        }

        [TestCase(RestorationIntentMismatch.Malformed)]
        [TestCase(RestorationIntentMismatch.TransactionId)]
        [TestCase(RestorationIntentMismatch.DescriptorFingerprint)]
        [TestCase(RestorationIntentMismatch.OriginalSha)]
        [TestCase(RestorationIntentMismatch.BackupSha)]
        [TestCase(RestorationIntentMismatch.JournalFilename)]
        [TestCase(RestorationIntentMismatch.JournalStage)]
        public void Recovery_OriginalActiveRequiresFullyBoundRestorationIntentForStageAdvance(
            RestorationIntentMismatch mismatch)
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name + "-" + mismatch);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string journalPath = Path.Combine(directory, names.Journal);
            string journalNextPath = journalPath + ".next";
            string backupPath = Path.Combine(directory, names.OriginalBackup);
            string candidatePath = Path.Combine(directory, names.CandidateStaging);
            string restoreStaging = backupPath + ".restore";
            string intentPath = backupPath + ".restore.intent";
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            StringComparer comparer = PathComparer();
            SpatialMigrationJournal initialJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            byte[] intentBytes = RestorationIntentBytes(mismatch, initialJournal, fixture.Original);
            fileSystem.Seed(intentPath, intentBytes);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            SpatialMigrationJournal liveJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(liveJournal.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(liveJournal.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(liveJournal.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.ReadAllBytes(intentPath), Is.EqualTo(intentBytes));
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(candidatePath), Is.False);
            Assert.That(fileSystem.Exists(receiptPath), Is.False);
            Assert.That(fileSystem.Paths.Any(path => path.IndexOf(".gd66-quarantine-",
                StringComparison.Ordinal) >= 0), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Any(operation =>
                operation.Type == OperationType.Write ||
                operation.Type == OperationType.Replace ||
                operation.Type == OperationType.Move), Is.False);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
        }

        [TestCase(DurableCandidateAuthorityMismatch.CrossTransactionCandidate)]
        [TestCase(DurableCandidateAuthorityMismatch.ExpectedHashMismatch)]
        [TestCase(DurableCandidateAuthorityMismatch.MissingAuthorityMarker)]
        [TestCase(DurableCandidateAuthorityMismatch.NonCanonicalBytes)]
        public void Recovery_DurableVerifiedRejectsCandidateAuthorityMismatchAndRestoresOriginal(
            DurableCandidateAuthorityMismatch mismatch)
        {
            PreparedFixture primary = PrepareEmptyFixture(6);
            PreparedFixture foreign = mismatch == DurableCandidateAuthorityMismatch.CrossTransactionCandidate
                ? PrepareEmptyFixture(5) : null;
            byte[] activeBytes = DurableMismatchActiveBytes(mismatch, primary, foreign);
            string expectedCandidateSha = DurableMismatchExpectedHash(mismatch, activeBytes);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name + "-" + mismatch);
            SpatialMigrationSidecarNames names = MaterializeDurableVerifiedJournal(fileSystem, primary,
                activePath, activeBytes, expectedCandidateSha);
            string directory = Path.GetDirectoryName(activePath);
            string journalPath = Path.Combine(directory, names.Journal);
            string journalNextPath = journalPath + ".next";
            string backupPath = Path.Combine(directory, names.OriginalBackup);
            string candidatePath = Path.Combine(directory, names.CandidateStaging);
            string restoreStaging = backupPath + ".restore";
            string intentPath = backupPath + ".restore.intent";
            string receiptPath = Path.Combine(directory, names.FinalizedReceipt);
            StringComparer comparer = PathComparer();
            SpatialContractResult<SpatialMigrationJournal> parsedJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits);
            Assert.That(parsedJournal.IsValid, Is.True, mismatch.ToString());
            SpatialMigrationJournal initialJournal = parsedJournal.Value;
            AssertDurableMismatchPreconditions(mismatch, primary, foreign, activeBytes,
                expectedCandidateSha, initialJournal);
            byte[] intentBytes = DetachedRestorationIntentContract.Serialize(new DetachedRestorationIntent(
                initialJournal.TransactionId, initialJournal.DescriptorFingerprintSha256,
                initialJournal.OriginalPayloadSha256, SpatialContractSha256.Compute(primary.Original),
                initialJournal.RelativeJournalFilename, (int)initialJournal.Stage), Limits);
            string intentQuarantinePath = QuarantinePath(directory, intentPath, intentBytes);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(primary)).Recover(activePath);

            Assert.That(first.IsSuccess, Is.True, first.Reason);
            Assert.That(first.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.RecoveredOriginalReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(first.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(primary.Original));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Any(operation => operation.Type == OperationType.Replace &&
                comparer.Equals(operation.Paths[0], candidatePath) &&
                comparer.Equals(operation.Paths[1], activePath)), Is.False);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(primary.Original));
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(intentPath), Is.False);
            Assert.That(fileSystem.Exists(intentQuarantinePath), Is.True);
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.Exists(candidatePath), Is.False);
            Assert.That(fileSystem.Exists(receiptPath), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            SpatialMigrationJournal restoredJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(restoredJournal.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(restoredJournal.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(restoredJournal.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(primary)).Recover(activePath);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.NoJournalLegacyDiagnostic));
            Assert.That(retry.Stage, Is.Null);
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(primary.Original));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            SpatialMigrationJournal retainedJournal =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits).Value;
            Assert.That(retainedJournal.TransactionId, Is.EqualTo(initialJournal.TransactionId));
            Assert.That(retainedJournal.DescriptorFingerprintSha256,
                Is.EqualTo(initialJournal.DescriptorFingerprintSha256));
        }

        [Test]
        public void Recovery_CandidateVerifiedRollbackRestoreWriteFailureTrustsExactCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            fileSystem.EnableFailureSequence(OperationType.Replace, 2, OperationType.Write, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            MaterializeJournal(fileSystem, fixture, activePath, SpatialMigrationJournalStage.CandidateVerified,
                includeBackup: true, includeStaging: true, activeCandidate: false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
        }

        [Test]
        public void Recovery_CandidateVerifiedRollbackRestoreStagingMismatchPreservesCommitReason()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Replace, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.CandidateVerified, includeBackup: true, includeStaging: true,
                activeCandidate: false);
            string directory = Path.GetDirectoryName(activePath);
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            byte[] corruptRestore = Encoding.UTF8.GetBytes("{\"not\":\"original\"}");
            fileSystem.Seed(restoreStaging, corruptRestore);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture)).Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.ReadAllBytes(Path.Combine(directory, names.OriginalBackup)),
                Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(QuarantinePath(directory, restoreStaging, corruptRestore)), Is.True);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(2));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[0], restoreStaging) &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[0],
                    Path.Combine(directory, names.CandidateStaging)) &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal)), Is.EqualTo(0));
            Assert.That(PathComparer().Equals(fileSystem.Operations.Last(operation =>
                operation.Type == OperationType.Replace && operation.MutationCompleted &&
                PathComparer().Equals(operation.Paths[1], activePath)).Paths[0], restoreStaging), Is.True);
        }

        [Test]
        public void Recovery_ChangedPinsRollbackStagingMismatchTrustsJournalBoundCandidate()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.Replaced, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            byte[] corruptRestore = Encoding.UTF8.GetBytes("{\"not\":\"original\"}");
            fileSystem.Seed(restoreStaging, corruptRestore);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.ReplacedPendingDurabilityDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.ReadAllBytes(Path.Combine(directory, names.OriginalBackup)),
                Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Exists(QuarantinePath(directory, restoreStaging, corruptRestore)), Is.True);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[0], restoreStaging) &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[0],
                    Path.Combine(directory, names.CandidateStaging)) &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal)), Is.EqualTo(0));
            Assert.That(PathComparer().Equals(fileSystem.Operations.Last(operation =>
                operation.Type == OperationType.Replace && operation.MutationCompleted &&
                PathComparer().Equals(operation.Paths[1], activePath)).Paths[0], restoreStaging), Is.True);
        }

        [Test]
        public void Recovery_ChangedPinsDurableVerifiedCorruptRestoreStagingQuarantinesAndRestoresOriginal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            fileSystem.Seed(restoreStaging, Encoding.UTF8.GetBytes("{\"not\":\"original\"}"));

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Does.Contain(
                DetachedSpatialMigrationTransaction.DurableCandidatePendingFinalizationDiagnostic));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths.Any(path => path.IndexOf(".gd66-quarantine-", StringComparison.Ordinal) >= 0 &&
                path.EndsWith(".evidence", StringComparison.Ordinal)), Is.True);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal)), Is.EqualTo(0));
        }

        [Test]
        public void Recovery_ChangedPinsDurableVerifiedCorruptRestoreStagingQuarantineFailureRetries()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Move, 1);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            fileSystem.Seed(restoreStaging, Encoding.UTF8.GetBytes("{\"not\":\"original\"}"));

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.ReadAllBytes(restoreStaging),
                Is.EqualTo(Encoding.UTF8.GetBytes("{\"not\":\"original\"}")));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".candidate.tmp",
                StringComparison.Ordinal)), Is.EqualTo(0));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(retry.IsSuccess, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal)), Is.EqualTo(0));
        }

        [Test]
        public void Recovery_OriginalRestoredJournalAdvanceFailureRetriesUsingPersistedIntent()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string journalPath = Path.Combine(directory, names.Journal);
            string journalNextPath = journalPath + ".next";
            string backupPath = Path.Combine(directory, names.OriginalBackup);
            string candidatePath = Path.Combine(directory, names.CandidateStaging);
            string restoreStaging = backupPath + ".restore";
            string intentPath = backupPath + ".restore.intent";
            StringComparer comparer = PathComparer();
            fileSystem.EnableTargetedFailure(OperationType.Replace,
                paths => comparer.Equals(paths[0], journalNextPath) &&
                    comparer.Equals(paths[1], journalPath), 1, afterMutation: false);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo(
                DetachedSpatialMigrationTransaction.OriginalRestoredStageWriteFailedReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            SpatialContractResult<SpatialMigrationJournal> liveBefore =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits);
            Assert.That(liveBefore.IsValid, Is.True);
            Assert.That(liveBefore.Value.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(fileSystem.Exists(journalNextPath), Is.True);
            SpatialContractResult<SpatialMigrationJournal> nextBefore =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalNextPath), Limits);
            Assert.That(nextBefore.IsValid, Is.True);
            Assert.That(nextBefore.Value.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(fileSystem.ReadAllBytes(backupPath), Is.EqualTo(fixture.Original));
            DetachedRestorationIntent intentBefore =
                DetachedRestorationIntentContract.Parse(fileSystem.ReadAllBytes(intentPath), Limits);
            Assert.That(intentBefore, Is.Not.Null);
            Assert.That(intentBefore.TransactionId, Is.EqualTo(liveBefore.Value.TransactionId));
            Assert.That(intentBefore.DescriptorFingerprint,
                Is.EqualTo(liveBefore.Value.DescriptorFingerprintSha256));
            Assert.That(intentBefore.JournalStage, Is.EqualTo((int)liveBefore.Value.Stage));
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(retry.IsSuccess, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && comparer.Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                comparer.Equals(operation.Paths[0], candidatePath)), Is.EqualTo(0));
            SpatialContractResult<SpatialMigrationJournal> liveAfter =
                SpatialMigrationJournalContracts.Parse(fileSystem.ReadAllBytes(journalPath), Limits);
            Assert.That(liveAfter.IsValid, Is.True);
            Assert.That(liveAfter.Value.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(fileSystem.Exists(journalNextPath), Is.False);
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(liveAfter.Value.TransactionId, Is.EqualTo(nextBefore.Value.TransactionId));
            Assert.That(liveAfter.Value.DescriptorFingerprintSha256,
                Is.EqualTo(nextBefore.Value.DescriptorFingerprintSha256));
        }

        [Test]
        public void Execute_FreshCandidateVerifiedJournalFailurePreservesStagedDiagnostic()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Replace, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.CandidateFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.BackupVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".candidate.tmp",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
        }

        [Test]
        public void Execute_FreshCandidateReplacementFailureWithOriginalIntactPreservesStagedDiagnostic()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(activePath), fixture.Result.Attempt.TransactionId).Value;
            string candidateStaging = Path.Combine(Path.GetDirectoryName(activePath), names.CandidateStaging);
            fileSystem.EnableTargetedFailure(OperationType.Replace, paths =>
                PathComparer().Equals(paths[0], candidateStaging) &&
                PathComparer().Equals(paths[1], activePath), 1, false);

            DetachedSpatialMigrationOutcome outcome =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture))
                    .Execute(activePath, fixture.Result.Attempt);

            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(outcome.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(outcome.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(outcome.Diagnostics, Is.EqualTo(new[]
            { DetachedSpatialMigrationTransaction.StagedCandidateVerifiedDiagnostic }));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".candidate.tmp",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.FailedOperation, Is.Not.Null);
            Assert.That(PathComparer().Equals(fileSystem.FailedOperation.Paths[0], candidateStaging), Is.True);
            Assert.That(PathComparer().Equals(fileSystem.FailedOperation.Paths[1], activePath), Is.True);
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
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


        [TestCase(OperationType.Write, 1, SpatialMigrationJournalStage.DurableVerified, SpatialTrustedPayload.Candidate)]
        [TestCase(OperationType.Flush, 1, SpatialMigrationJournalStage.DurableVerified, SpatialTrustedPayload.Candidate)]
        [TestCase(OperationType.Read, 1, SpatialMigrationJournalStage.DurableVerified, SpatialTrustedPayload.Candidate)]
        public void Recovery_ChangedPinsDurableVerifiedCorruptRestoreStagingRecreationFailuresRetry(
            OperationType failureType, int failureIndex, SpatialMigrationJournalStage expectedStage,
            SpatialTrustedPayload expectedTrust)
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name + failureType + failureIndex);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            fileSystem.Seed(restoreStaging, Encoding.UTF8.GetBytes("{\"not\":\"original\"}"));
            fileSystem.EnableTargetedFailure(failureType, paths =>
            {
                if (failureType == OperationType.Flush)
                    return PathComparer().Equals(paths[0], directory) &&
                        fileSystem.Operations.Any(operation => operation.Type == OperationType.Write &&
                            operation.MutationCompleted &&
                            PathComparer().Equals(operation.Paths[0], restoreStaging));
                if (failureType == OperationType.Read)
                    return PathComparer().Equals(paths[0], restoreStaging) &&
                        fileSystem.Operations.Any(operation => operation.Type == OperationType.Write &&
                            operation.MutationCompleted &&
                            PathComparer().Equals(operation.Paths[0], restoreStaging));
                return PathComparer().Equals(paths[0], restoreStaging);
            }, failureIndex, false);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(first.Stage, Is.EqualTo(expectedStage));
            Assert.That(first.TrustedPayload, Is.EqualTo(expectedTrust));
            Assert.That(fileSystem.FailedOperation, Is.Not.Null);
            Assert.That(fileSystem.FailedOperation.Type, Is.EqualTo(failureType));
            Assert.That(fileSystem.FailedTargetOccurrence, Is.EqualTo(1));
            Assert.That(PathComparer().Equals(fileSystem.FailedOperation.Paths[0],
                failureType == OperationType.Flush ? directory : restoreStaging), Is.True);
            Assert.That(fileSystem.Operations.Any(operation => operation.Type == OperationType.Move &&
                PathComparer().Equals(operation.Paths[0], restoreStaging) && operation.MutationCompleted), Is.True);
            Assert.That(fileSystem.Operations.Any(operation => operation.Type == OperationType.Write &&
                PathComparer().Equals(operation.Paths[0], restoreStaging) && operation.MutationCompleted),
                Is.EqualTo(failureType != OperationType.Write));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal)), Is.EqualTo(0));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(retry.IsSuccess, Is.False);
            Assert.That(retry.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
        }

        [Test]
        public void Recovery_ChangedPinsDurableVerifiedCorruptRestoreStagingExistingConflictingQuarantineBlocksThenConverges()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            byte[] corrupt = Encoding.UTF8.GetBytes("{\"not\":\"original\"}");
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            fileSystem.Seed(restoreStaging, corrupt);
            fileSystem.Seed(QuarantinePath(directory, restoreStaging, corrupt), Encoding.UTF8.GetBytes("different"));

            DetachedSpatialMigrationOutcome blocked =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(blocked.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(blocked.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));

            fileSystem.RemoveSeededEvidence(QuarantinePath(directory, restoreStaging, corrupt));
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
        }

        [Test]
        public void Recovery_ChangedPinsDurableVerifiedCorruptRestoreStagingExactQuarantineReplacementFailureRetries()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            SpatialMigrationSidecarNames names = MaterializeJournal(fileSystem, fixture, activePath,
                SpatialMigrationJournalStage.DurableVerified, includeBackup: true, includeStaging: false,
                activeCandidate: true);
            string directory = Path.GetDirectoryName(activePath);
            byte[] corrupt = Encoding.UTF8.GetBytes("{\"not\":\"original\"}");
            string restoreStaging = Path.Combine(directory, names.OriginalBackup + ".restore");
            fileSystem.Seed(restoreStaging, corrupt);
            fileSystem.Seed(QuarantinePath(directory, restoreStaging, corrupt), corrupt);
            string journalPath = Path.Combine(directory, names.Journal);
            SpatialMigrationJournal journal = SpatialMigrationJournalContracts.Parse(
                fileSystem.ReadAllBytes(journalPath), Limits).Value;
            string intentPath = Path.Combine(directory, names.OriginalBackup + ".restore.intent");
            byte[] intentBytes = DetachedRestorationIntentContract.Serialize(new DetachedRestorationIntent(
                journal.TransactionId, journal.DescriptorFingerprintSha256, journal.OriginalPayloadSha256,
                SpatialContractSha256.Compute(fixture.Original), journal.RelativeJournalFilename,
                (int)journal.Stage), Limits);
            string restoreQuarantine = QuarantinePath(directory, restoreStaging, corrupt);
            string intentQuarantine = QuarantinePath(directory, intentPath, intentBytes);
            fileSystem.EnableFailure(OperationType.Replace, 1);

            DetachedSpatialMigrationOutcome first =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);

            Assert.That(first.Reason, Is.EqualTo("gd66.transaction.pinned_input_hash_mismatch"));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.DurableVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Candidate));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));

            fileSystem.DisableFailure();
            DetachedSpatialMigrationOutcome retry =
                new DetachedSpatialMigrationTransaction(fileSystem, Recovery(fixture, new byte[] { 1 }))
                    .Recover(activePath);
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(retry.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(fileSystem.Exists(restoreStaging), Is.False);
            Assert.That(fileSystem.Exists(restoreQuarantine), Is.True);
            Assert.That(fileSystem.ReadAllBytes(restoreQuarantine), Is.EqualTo(corrupt));
            Assert.That(fileSystem.Exists(intentPath), Is.False);
            Assert.That(fileSystem.Exists(intentQuarantine), Is.True);
            Assert.That(fileSystem.ReadAllBytes(intentQuarantine), Is.EqualTo(intentBytes));
            Assert.That(fileSystem.Paths.Count(path => PathComparer().Equals(path, restoreQuarantine)),
                Is.EqualTo(1));
            Assert.That(fileSystem.Paths.Count(path => PathComparer().Equals(path, intentQuarantine)),
                Is.EqualTo(1));
            Assert.That(fileSystem.Paths.Where(path => path.IndexOf(".gd66-quarantine-",
                    StringComparison.Ordinal) >= 0).OrderBy(path => path, PathComparer()),
                Is.EqualTo(new[] { restoreQuarantine, intentQuarantine }.OrderBy(path => path, PathComparer())));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.OriginalRestored));
            Assert.That(fileSystem.Operations.Any(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[0], restoreStaging) &&
                PathComparer().Equals(operation.Paths[1], restoreQuarantine) && operation.MutationCompleted), Is.True);
        }

        [Test]
        public void Execute_FreshCandidateVerifiedJournalFailureRetriesToFinalizedWithoutSecondJournal()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem(OperationType.Replace, 2);
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            DetachedSpatialMigrationOutcome first = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture)).Execute(activePath, fixture.Result.Attempt);
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture)).Execute(activePath, fixture.Result.Attempt);

            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.BackupVerified));
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
        }

        [Test]
        public void Execute_FreshCandidateReplacementFailureRetriesToFinalizedWithoutRepeatedTransition()
        {
            PreparedFixture fixture = PrepareEmptyFixture(6);
            var fileSystem = new DeterministicFileSystem();
            string activePath = ActivePath(TestContext.CurrentContext.Test.Name);
            fileSystem.Seed(activePath, fixture.Original);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(activePath), fixture.Result.Attempt.TransactionId).Value;
            string candidateStaging = Path.Combine(Path.GetDirectoryName(activePath), names.CandidateStaging);
            fileSystem.EnableTargetedFailure(OperationType.Replace, paths =>
                PathComparer().Equals(paths[0], candidateStaging) &&
                PathComparer().Equals(paths[1], activePath), 1, false);
            DetachedSpatialMigrationOutcome first = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture)).Execute(activePath, fixture.Result.Attempt);
            Gd66DetachedSpatialMigrationTransactionTests.FileOperation failedOperation = fileSystem.FailedOperation;
            Assert.That(failedOperation, Is.Not.Null);
            Assert.That(failedOperation.Type, Is.EqualTo(OperationType.Replace));
            Assert.That(PathComparer().Equals(failedOperation.Paths[0], candidateStaging), Is.True);
            Assert.That(PathComparer().Equals(failedOperation.Paths[1], activePath), Is.True);
            Assert.That(failedOperation.MutationCompleted, Is.False);
            Assert.That(failedOperation.FailedAfterMutation, Is.False);
            Assert.That(fileSystem.FailedTargetOccurrence, Is.EqualTo(1));
            Assert.That(fileSystem.ReadAllBytes(activePath), Is.EqualTo(fixture.Original));
            Assert.That(fileSystem.ReadAllBytes(candidateStaging),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            int candidateWrites = fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                PathComparer().Equals(operation.Paths[0], candidateStaging) && operation.MutationCompleted);
            fileSystem.DisableFailure();

            DetachedSpatialMigrationOutcome retry = new DetachedSpatialMigrationTransaction(fileSystem,
                Recovery(fixture)).Execute(activePath, fixture.Result.Attempt);

            Assert.That(first.IsSuccess, Is.False);
            Assert.That(first.Reason, Is.EqualTo(DetachedSpatialMigrationTransaction.ReplacementFailedReason));
            Assert.That(first.Stage, Is.EqualTo(SpatialMigrationJournalStage.CandidateVerified));
            Assert.That(first.TrustedPayload, Is.EqualTo(SpatialTrustedPayload.Original));
            Assert.That(retry.IsSuccess, Is.True, retry.Reason);
            Assert.That(retry.Stage, Is.EqualTo(SpatialMigrationJournalStage.Finalized));
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Paths.Count(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(1));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                PathComparer().Equals(operation.Paths[0], candidateStaging) && operation.MutationCompleted),
                Is.EqualTo(candidateWrites));
        }

        private static string Hash(char value) => new string(value, 64);
        private static string TransactionId(char value) => "gd66-" + Hash(value);
        private static byte[] RestorationIntentBytes(RestorationIntentMismatch mismatch,
            SpatialMigrationJournal journal, byte[] backup)
        {
            if (mismatch == RestorationIntentMismatch.Malformed)
                return Encoding.UTF8.GetBytes("{\"TransactionId\":");
            string transactionId = journal.TransactionId;
            string descriptor = journal.DescriptorFingerprintSha256;
            string original = journal.OriginalPayloadSha256;
            string backupHash = SpatialContractSha256.Compute(backup);
            string filename = journal.RelativeJournalFilename;
            int stage = (int)journal.Stage;
            switch (mismatch)
            {
                case RestorationIntentMismatch.TransactionId:
                    transactionId = TransactionId('2'); break;
                case RestorationIntentMismatch.DescriptorFingerprint:
                    descriptor = Hash('2'); break;
                case RestorationIntentMismatch.OriginalSha:
                    original = Hash('2'); break;
                case RestorationIntentMismatch.BackupSha:
                    backupHash = Hash('2'); break;
                case RestorationIntentMismatch.JournalFilename:
                    filename = "save." + TransactionId('2') + ".journal.json"; break;
                case RestorationIntentMismatch.JournalStage:
                    stage = (int)SpatialMigrationJournalStage.Replaced; break;
            }
            return DetachedRestorationIntentContract.Serialize(new DetachedRestorationIntent(
                transactionId, descriptor, original, backupHash, filename, stage), Limits);
        }

        private static byte[] DurableMismatchActiveBytes(DurableCandidateAuthorityMismatch mismatch,
            PreparedFixture primary, PreparedFixture foreign)
        {
            if (mismatch == DurableCandidateAuthorityMismatch.CrossTransactionCandidate)
                return foreign.Result.Attempt.Candidate.GetBytes();
            if (mismatch == DurableCandidateAuthorityMismatch.MissingAuthorityMarker)
                return Encoding.UTF8.GetBytes("{\"schema\":\"save_root\",\"schemaVersion\":7,\"primary\":{}}");
            if (mismatch == DurableCandidateAuthorityMismatch.NonCanonicalBytes)
                return Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(
                    primary.Result.Attempt.Candidate.GetBytes()) + " \n");
            return primary.Result.Attempt.Candidate.GetBytes();
        }

        private static string DurableMismatchExpectedHash(DurableCandidateAuthorityMismatch mismatch,
            byte[] activeBytes)
        {
            string activeHash = SpatialContractSha256.Compute(activeBytes);
            if (mismatch != DurableCandidateAuthorityMismatch.ExpectedHashMismatch) return activeHash;
            string zero = new string('0', 64);
            return activeHash == zero ? new string('1', 64) : zero;
        }

        private static void AssertDurableMismatchPreconditions(DurableCandidateAuthorityMismatch mismatch,
            PreparedFixture primary, PreparedFixture foreign, byte[] activeBytes, string expectedCandidateSha,
            SpatialMigrationJournal journal)
        {
            string activeHash = SpatialContractSha256.Compute(activeBytes);
            if (mismatch == DurableCandidateAuthorityMismatch.CrossTransactionCandidate)
            {
                string foreignFingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                    foreign.Result.Attempt.Descriptor, Limits);
                string foreignIdentity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                    foreign.Result.Attempt.Descriptor.OriginalPayloadSha256, foreignFingerprint);
                Assert.That(activeHash, Is.EqualTo(expectedCandidateSha));
                Assert.That(SpatialMigrationTransactionIdentity.CreateTransactionId(foreignIdentity),
                    Is.Not.EqualTo(journal.TransactionId));
                Assert.That(foreignFingerprint, Is.Not.EqualTo(journal.DescriptorFingerprintSha256));
                return;
            }
            if (mismatch == DurableCandidateAuthorityMismatch.ExpectedHashMismatch)
            {
                Assert.That(activeBytes, Is.EqualTo(primary.Result.Attempt.Candidate.GetBytes()));
                Assert.That(activeHash, Is.Not.EqualTo(expectedCandidateSha));
                return;
            }
            Assert.That(activeHash, Is.EqualTo(expectedCandidateSha));
            if (mismatch == DurableCandidateAuthorityMismatch.MissingAuthorityMarker)
            {
                Assert.That(activeBytes, Is.Not.EqualTo(primary.Original));
                Assert.That(activeBytes, Is.Not.EqualTo(primary.Result.Attempt.Candidate.GetBytes()));
            }
            if (mismatch == DurableCandidateAuthorityMismatch.NonCanonicalBytes)
                Assert.That(activeBytes, Is.Not.EqualTo(primary.Result.Attempt.Candidate.GetBytes()));
        }

        private static SpatialMigrationSidecarNames MaterializeDurableVerifiedJournal(
            DeterministicFileSystem fileSystem, PreparedFixture fixture, string activePath, byte[] activeBytes,
            string expectedCandidateSha256)
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
                names.OriginalBackup, names.CandidateStaging, names.FinalizedReceipt,
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256,
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256, expectedCandidateSha256,
                SpatialMigrationJournalStage.DurableVerified);
            byte[] journalBytes = SpatialMigrationJournalContracts.Serialize(journal, Limits).Value;
            Assert.That(SpatialMigrationJournalContracts.Parse(journalBytes, Limits).IsValid, Is.True);
            string directory = Path.GetDirectoryName(activePath);
            fileSystem.Seed(activePath, activeBytes);
            fileSystem.Seed(Path.Combine(directory, names.Journal), journalBytes);
            fileSystem.Seed(Path.Combine(directory, names.OriginalBackup), fixture.Original);
            return names;
        }
        internal static string QuarantinePath(string directory, string path, byte[] bytes)
        {
            string evidenceHash = SpatialContractSha256.Compute(bytes);
            string evidenceName = Path.GetFileName(path);
            string pathHash = SpatialContractSha256.Compute(Encoding.UTF8.GetBytes(evidenceName)).Substring(0, 16);
            int marker = evidenceName.IndexOf(".gd66-", StringComparison.Ordinal);
            string stem = marker > 0 ? evidenceName.Substring(0, marker) : "gd66";
            return Path.Combine(directory, stem + ".gd66-quarantine-" + evidenceHash + "-" + pathHash + ".evidence");
        }

        internal static StringComparer PathComparer() => Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        internal static SpatialMigrationSidecarNames MaterializeJournal(DeterministicFileSystem fileSystem,
            PreparedFixture fixture, string activePath, SpatialMigrationJournalStage stage, bool includeBackup,
            bool includeStaging, bool activeCandidate)
        {
            PersistedJournalFixture persisted = BuildPersistedJournalFixture(fixture, activePath, stage);
            SpatialMigrationSidecarNames names = persisted.Names;
            string directory = Path.GetDirectoryName(activePath);
            fileSystem.Seed(activePath, activeCandidate ? fixture.Result.Attempt.Candidate.GetBytes() : fixture.Original);
            fileSystem.Seed(Path.Combine(directory, names.Journal), persisted.JournalBytes);
            if (includeBackup) fileSystem.Seed(Path.Combine(directory, names.OriginalBackup), fixture.Original);
            if (includeStaging) fileSystem.Seed(Path.Combine(directory, names.CandidateStaging),
                fixture.Result.Attempt.Candidate.GetBytes());
            return names;
        }

        internal static PersistedJournalFixture BuildPersistedJournalFixture(PreparedFixture fixture,
            string activePath, SpatialMigrationJournalStage stage)
        {
            string fingerprint = SpatialMigrationDescriptorContracts.ComputeInputFingerprint(
                fixture.Result.Attempt.Descriptor, Limits);
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256, fingerprint);
            string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
            SpatialMigrationSidecarNames names = SpatialMigrationSidecarPaths.Derive(
                Path.GetFileName(activePath), transactionId).Value;
            bool candidateVerified = stage == SpatialMigrationJournalStage.CandidateVerified ||
                stage == SpatialMigrationJournalStage.Replaced ||
                stage == SpatialMigrationJournalStage.DurableVerified ||
                stage == SpatialMigrationJournalStage.Finalized;
            var journal = new SpatialMigrationJournal(SpatialMigrationContractIdentity.JournalSchemaVersion,
                fixture.Result.Attempt.Descriptor, fingerprint, identity, transactionId, names.Journal,
                names.OriginalBackup, names.CandidateStaging,
                stage == SpatialMigrationJournalStage.DurableVerified || stage == SpatialMigrationJournalStage.Finalized
                    ? names.FinalizedReceipt : null,
                fixture.Result.Attempt.Descriptor.OriginalPayloadSha256,
                stage == SpatialMigrationJournalStage.DescriptorPinned
                    ? null : fixture.Result.Attempt.Descriptor.OriginalPayloadSha256,
                candidateVerified ? fixture.Result.Attempt.CandidateSha256 : null,
                stage);
            SpatialContractResult<byte[]> serialized = SpatialMigrationJournalContracts.Serialize(journal, Limits);
            Assert.That(serialized.IsValid, Is.True,
                "Journal serialization issues: " + string.Join(",", serialized.Issues));
            byte[] journalBytes = serialized.Value;
            SpatialContractResult<SpatialMigrationJournal> parsed =
                SpatialMigrationJournalContracts.Parse(journalBytes, Limits);
            Assert.That(parsed.IsValid, Is.True,
                "Journal parse issues: " + string.Join(",", parsed.Issues));
            return new PersistedJournalFixture(names, journalBytes);
        }

        internal sealed class PersistedJournalFixture
        {
            internal PersistedJournalFixture(SpatialMigrationSidecarNames names, byte[] journalBytes)
            { Names = names; JournalBytes = journalBytes; }
            internal SpatialMigrationSidecarNames Names { get; }
            internal byte[] JournalBytes { get; }
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
        private static void EnablePendingDurabilityFailure(DeterministicFileSystem fileSystem,
            string activePath)
        {
            string directory = Path.GetDirectoryName(activePath);
            fileSystem.EnableTargetedFailure(OperationType.Flush, paths =>
                PathComparer().Equals(paths[0], directory), -1, false);
        }
        private static void AssertPendingDurabilityFailurePhase(DeterministicFileSystem fileSystem,
            PreparedFixture fixture, string activePath)
        {
            string directory = Path.GetDirectoryName(activePath);
            FileOperation failed = fileSystem.FailedOperation;
            Assert.That(failed, Is.Not.Null);
            Assert.That(failed.Type, Is.EqualTo(OperationType.Flush));
            Assert.That(PathComparer().Equals(failed.Paths[0], directory), Is.True);
            Assert.That(fileSystem.ReadAllBytes(activePath),
                Is.EqualTo(fixture.Result.Attempt.Candidate.GetBytes()));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Replace &&
                operation.MutationCompleted && PathComparer().Equals(operation.Paths[1], activePath)), Is.EqualTo(0));
            Assert.That(fileSystem.Operations.Count(operation => operation.Type == OperationType.Write &&
                (operation.Paths[0].EndsWith(".original.bak", StringComparison.Ordinal) ||
                 operation.Paths[0].EndsWith(".candidate.tmp", StringComparison.Ordinal))), Is.EqualTo(0));
            string journalPath = fileSystem.Paths.Single(path => path.EndsWith(".journal.json",
                StringComparison.Ordinal));
            SpatialContractResult<SpatialMigrationJournal> parsed = SpatialMigrationJournalContracts.Parse(
                fileSystem.ReadAllBytes(journalPath), Limits);
            Assert.That(parsed.IsValid, Is.True, "Journal parse issues: " + string.Join(",", parsed.Issues));
            Assert.That(parsed.Value.Stage, Is.EqualTo(SpatialMigrationJournalStage.Replaced));
        }
        internal static string ActivePath(string identity)
        {
            string key = SpatialContractSha256.Compute(
                Encoding.UTF8.GetBytes(identity ?? string.Empty)).Substring(0, 24);
            return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DL-GD66", key, "save.json"));
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
                Limits = fixture.Limits, WholeLimits = fixture.WholeLimits,
                LegacyBytes = fixture.LegacyBytes, ValidationInputs = new Dictionary<string, byte[]>() };
        }

        internal static PreparedFixture PrepareEmptyFixture(int schema, bool unwrapped = false,
            byte[] originalOverride = null,
            RawSavePayloadClassificationLimits? rawClassificationLimits = null,
            DetachedWholeSaveLimits? wholeSaveLimits = null,
            CanonicalSpatialSerializationLimits? serializationLimits = null)
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
                rawClassificationLimits ?? RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor());
            CanonicalSpatialSerializationLimits limits = serializationLimits ?? new CanonicalSpatialSerializationLimits(
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
            DetachedWholeSaveLimits selectedWholeLimits = wholeSaveLimits ?? WholeLimits();
            var inputs = new DetachedSpatialMigrationPreparationInputs(original, classification, descriptor,
                compatibility, production, legacy, new Dictionary<string, byte[]>(), limits,
                selectedWholeLimits);
            return new PreparedFixture(original, classification, production, compatibility, legacyBytes, limits,
                selectedWholeLimits, DetachedSpatialMigrationPreparer.Prepare(inputs));
        }

        internal static DetachedSpatialMigrationRecoveryContext Recovery(PreparedFixture fixture,
            byte[] legacyBytes = null) =>
            new DetachedSpatialMigrationRecoveryContext(fixture.Compatibility, fixture.Production,
                new Dictionary<string, byte[]>(), legacyBytes ?? fixture.LegacyBytes, fixture.Limits, RawLimits(),
                new RawSaveEnvelopeVersionContract(1, 6), BlankFloor(), fixture.WholeLimits);

        private static RawLegacyBlankFloorContract BlankFloor() => new RawLegacyBlankFloorContract(1,
            Enumerable.Range(0, 4).Select(index => new RawLegacyBlankFloorNodeContract(
                0, index, "slot." + index, "", "", 0)), true, true,
            new[] { "Nodes", "NextRevision" },
            new[] { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" });

        private static RawSavePayloadClassificationLimits RawLimits() =>
            new RawSavePayloadClassificationLimits(100000, 32, 100, 100, 10000, 500000);
        internal static RawLegacyBlankFloorContract BlankFloorForCoordinator => BlankFloor();
        internal static RawSavePayloadClassificationLimits RawLimitsForCoordinator => RawLimits();
        private static DetachedWholeSaveLimits WholeLimits() =>
            new DetachedWholeSaveLimits(1000000, 100000, 1000, 100000);

        private static TextAsset Asset(string path)
        { TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path); Assert.That(asset, Is.Not.Null, path); return asset; }

        internal sealed class PreparedFixture
        {
            internal PreparedFixture(byte[] original, RawSavePayloadClassification classification,
                ProductionSpatialContentSnapshot production,
                SpatialLayoutCompatibilitySnapshot compatibility, byte[] legacyBytes,
                CanonicalSpatialSerializationLimits limits, DetachedWholeSaveLimits wholeLimits,
                DetachedSpatialMigrationPreparationResult result)
            { Original = original; Classification = classification; Production = production; Compatibility = compatibility;
              LegacyBytes = legacyBytes; Limits = limits; WholeLimits = wholeLimits; Result = result; }
            internal byte[] Original { get; }
            internal RawSavePayloadClassification Classification { get; }
            internal ProductionSpatialContentSnapshot Production { get; }
            internal SpatialLayoutCompatibilitySnapshot Compatibility { get; }
            internal byte[] LegacyBytes { get; }
            internal CanonicalSpatialSerializationLimits Limits { get; }
            internal DetachedWholeSaveLimits WholeLimits { get; }
            internal DetachedSpatialMigrationPreparationResult Result { get; }
        }

        public enum OperationType { Exists, Read, Write, Replace, Move, Flush, Enumerate, Containment, Delete }

        internal sealed class FileOperation
        {
            internal FileOperation(OperationType type, int index, params string[] paths)
            { Type = type; Index = index; Paths = paths; }
            internal OperationType Type { get; }
            internal int Index { get; }
            internal string[] Paths { get; }
            internal bool MutationCompleted { get; private set; }
            internal bool FailedAfterMutation { get; private set; }
            internal void MarkMutationCompleted() { MutationCompleted = true; }
            internal void MarkFailedAfterMutation() { FailedAfterMutation = true; }
        }

        internal sealed class DeterministicFileSystem : ISpatialMigrationFileSystem
        {
            private static readonly StringComparer PathComparer = Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            private readonly Dictionary<string, byte[]> files =
                new Dictionary<string, byte[]>(PathComparer);
            private readonly Dictionary<OperationType, int> counts = new Dictionary<OperationType, int>();
            private readonly List<FileOperation> operations = new List<FileOperation>();
            private readonly Dictionary<OperationType, int> targetedCounts = new Dictionary<OperationType, int>();
            private OperationType? failureType;
            private int failureIndex;
            private OperationType? secondFailureType;
            private int secondFailureIndex;
            private bool failureAfterMutation;
            private Predicate<string[]> failurePathPredicate;
            private FileOperation failedOperation;
            private int failedTargetOccurrence;
            private readonly Dictionary<string, Queue<byte[]>> readSubstitutions =
                new Dictionary<string, Queue<byte[]>>(PathComparer);
            internal DeterministicFileSystem(OperationType? failureType = null, int failureIndex = 0)
            { this.failureType = failureType; this.failureIndex = failureIndex; }
            internal void DisableFailure()
            {
                failureType = null; failureIndex = 0; failureAfterMutation = false; failurePathPredicate = null; failedOperation = null; failedTargetOccurrence = 0; targetedCounts.Clear();
                secondFailureType = null; secondFailureIndex = 0;
            }
            internal void EnableFailure(OperationType type, int index)
            {
                failureType = type; failureIndex = index; failureAfterMutation = false;
                secondFailureType = null; secondFailureIndex = 0; failurePathPredicate = null; targetedCounts.Clear();
            }
            internal void EnableFailureAfterMutation(OperationType type, int index)
            {
                failureType = type; failureIndex = index; failureAfterMutation = true;
                secondFailureType = null; secondFailureIndex = 0; failurePathPredicate = null; targetedCounts.Clear();
            }
            internal void EnableTargetedFailure(OperationType type, Predicate<string[]> pathPredicate, int occurrence, bool afterMutation)
            {
                failureType = type; failureIndex = occurrence; failureAfterMutation = afterMutation;
                failurePathPredicate = pathPredicate; failedOperation = null; failedTargetOccurrence = 0; targetedCounts.Clear();
                secondFailureType = null; secondFailureIndex = 0;
            }
            internal FileOperation FailedOperation => failedOperation;
            internal int FailedTargetOccurrence => failedTargetOccurrence;
            internal void SubstituteNextRead(string path, byte[] bytes)
            {
                path = Normalize(path);
                if (!readSubstitutions.TryGetValue(path, out Queue<byte[]> queue))
                { queue = new Queue<byte[]>(); readSubstitutions[path] = queue; }
                queue.Enqueue((byte[])bytes.Clone());
            }
            internal int PendingReadSubstitutions(string path)
            {
                path = Normalize(path);
                return readSubstitutions.TryGetValue(path, out Queue<byte[]> queue) ? queue.Count : 0;
            }
            internal void EnableFailureSequence(OperationType firstType, int firstIndex,
                OperationType secondType, int secondIndex)
            {
                failureType = firstType; failureIndex = firstIndex;
                secondFailureType = secondType; secondFailureIndex = secondIndex;
            }
            internal void Seed(string path, byte[] bytes) { files[Normalize(path)] = (byte[])bytes.Clone(); }
            internal void RemoveSeededEvidence(string path) { files.Remove(Normalize(path)); }
            internal IEnumerable<string> Paths => files.Keys.OrderBy(value => value, PathComparer).ToArray();
            internal IEnumerable<FileOperation> Operations => operations.ToArray();
            public bool Exists(string path)
            { path = Normalize(path); Record(OperationType.Exists, path); return files.ContainsKey(path); }
            public byte[] ReadAllBytes(string path)
            {
                path = Normalize(path); Record(OperationType.Read, path);
                if (readSubstitutions.TryGetValue(path, out Queue<byte[]> queue) && queue.Count != 0)
                    return queue.Dequeue();
                return (byte[])files[path].Clone();
            }
            public void WriteAllBytesDurable(string path, byte[] bytes)
            {
                path = Normalize(path); Record(OperationType.Write, path);
                files.Add(path, (byte[])bytes.Clone()); MarkMutation(OperationType.Write); FailAfter(OperationType.Write);
            }
            public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath)
            {
                stagingPath = Normalize(stagingPath); activePath = Normalize(activePath);
                Record(OperationType.Replace, stagingPath, activePath); SameDirectory(stagingPath, activePath);
                files[activePath] = (byte[])files[stagingPath].Clone(); files.Remove(stagingPath);
                MarkMutation(OperationType.Replace); FailAfter(OperationType.Replace);
            }
            public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath)
            {
                sourcePath = Normalize(sourcePath); destinationPath = Normalize(destinationPath);
                Record(OperationType.Move, sourcePath, destinationPath); SameDirectory(sourcePath, destinationPath);
                files.Add(destinationPath, (byte[])files[sourcePath].Clone()); files.Remove(sourcePath);
                MarkMutation(OperationType.Move); FailAfter(OperationType.Move);
            }
            public void DeleteFile(string path)
            {
                path = Normalize(path); Record(OperationType.Delete, path);
                files.Remove(path); MarkMutation(OperationType.Delete); FailAfter(OperationType.Delete);
            }
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
                counts[type] = index;
                var operation = new FileOperation(type, index, paths);
                operations.Add(operation);
                bool pathMatches = failureType == type &&
                    (failurePathPredicate == null || failurePathPredicate(paths));
                int targetedIndex = 0;
                if (pathMatches)
                {
                    targetedIndex = targetedCounts.TryGetValue(type, out int targetedPrevious) ? targetedPrevious + 1 : 1;
                    targetedCounts[type] = targetedIndex;
                }
                if (!failureAfterMutation && failureType == type && pathMatches &&
                    (failureIndex < 0 || failureIndex == targetedIndex))
                { failedOperation = operation; failedTargetOccurrence = targetedIndex; throw new IOException(type + "#" + targetedIndex); }
                if (secondFailureType == type && (secondFailureIndex < 0 || secondFailureIndex == index))
                { failedOperation = operation; failedTargetOccurrence = index; throw new IOException(type + "#" + index); }
            }
            private void MarkMutation(OperationType type)
            {
                for (int index = operations.Count - 1; index >= 0; index--)
                    if (operations[index].Type == type) { operations[index].MarkMutationCompleted(); return; }
            }
            private void FailAfter(OperationType type)
            {
                if (!failureAfterMutation || failureType != type) return;
                FileOperation operation = operations.LastOrDefault(value => value.Type == type);
                if (operation == null) return;
                bool pathMatches = failurePathPredicate == null || failurePathPredicate(operation.Paths);
                int targetedIndex = targetedCounts.TryGetValue(type, out int value) ? value : operation.Index;
                if (pathMatches && (failureIndex < 0 || failureIndex == targetedIndex))
                { operation.MarkFailedAfterMutation(); failedOperation = operation; failedTargetOccurrence = targetedIndex; throw new IOException(type + "#" + targetedIndex + " after"); }
            }
        }
    }
}
#endif
