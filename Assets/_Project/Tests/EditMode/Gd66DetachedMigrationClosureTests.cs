#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class Gd66DetachedMigrationClosureTests
    {
        private static readonly string[] PreparationCases =
        { "AssignmentR1", "AssignmentR2", "FloorR1", "FloorR2", "PlacementExplicitR1",
          "PlacementImplicitR1", "MonsterOnly", "TrapOnly", "LootOnly", "MonsterTrapLoot",
          "LowerExactAgreement", "LowerIneffectiveConflict", "LowerEffectiveContribution" };
        private static readonly string[] InvalidCases =
        { "LowerEffectiveMismatch", "DuplicateRoomSlot", "TiedFloorRevision", "TiedPlacementRevision",
          "InvalidNextRevision", "RecordRangeFailure", "RouteGap", "CategoryMismatch", "InvalidOption",
          "NarrowHall", "CapacityOverflow", "MissingProfile", "InvalidProfile", "GeometryMismatch",
          "ManifestMismatch", "CatalogMismatch", "ConfigurationMismatch", "SerializerMismatch",
          "UnexpectedExtensionInput", "CandidateWorkloadExhaustion" };
        private static readonly string[] MutationCases =
        { "WrongLayoutContractVersion", "WrongRoomAnchor", "WrongRoomOrientation",
          "WrongFixedStructureAnchor", "WrongFixedStructureKind", "InvalidMonsterOption",
          "WrongMonsterCategory", "InvalidTrapOption", "InvalidLootOption", "CapacityOverflow",
          "WrongNextSequence", "DuplicateAssignmentId", "WrongRoomSemantics", "InvalidEdgeEndpoint",
          "SocketMismatch", "WrongOccupiedTileTotal", "MarkerTransactionMismatch",
          "MarkerFingerprintMismatch" };
        private static readonly string[] RestartCases =
        { "DescriptorPinned", "BackupVerified", "CandidateVerifiedOriginalActive",
          "CandidateVerifiedReplacementBeforeAdvance", "Replaced", "DurableVerified", "Finalized",
          "OriginalRestored", "OriginalRestoredAdvanceFailed" };
        private static readonly string[] PinCases =
        { "ProfileMissing", "ProfileHashChanged", "GeometryMissing", "GeometryHashChanged",
          "ManifestChanged", "CatalogChanged", "LegacyConfigurationChanged", "ExtensionInputMissing",
          "ExtensionInputHashChanged" };
        private static readonly string[] EvidenceCases =
        { "MalformedJournal", "MultipleMalformedJournals", "RedirectedJournal", "BindingInvalidJournal",
          "OrphanBackup", "OrphanCandidate", "OrphanNext", "OrphanRestore",
          "OrphanRestorationIntent", "OrphanReceipt", "MatchingQuarantine",
          "MultipleTerminalJournals" };
        private static readonly string[] ReceiptCases =
        { "NoReceipt", "MatchingReceipt", "ConflictingReceipt", "MalformedReceipt", "PrematureReceipt",
          "ReceiptWriteFailure", "ReceiptFlushFailure", "ReceiptReadbackFailure",
          "ReceiptQuarantineFailure" };
        private static readonly string[] RestorationCases =
        { "RestoreWriteFailure", "IntentWriteFailure", "IntentFlushFailure", "IntentReadbackFailure",
          "OriginalReplaceFailure", "OriginalReadbackFailure", "OriginalRestoredAdvanceFailure",
          "OriginalRestoredRetry", "RepeatedRecovery" };
        private static readonly string[] OperationCases =
        { "Enumerate", "Containment", "InitialJournalWrite", "InitialJournalReadback", "BackupWrite",
          "BackupReadback", "BackupStageAdvance", "CandidateWrite", "CandidateReadback",
          "CandidateStageAdvance", "ActiveReplace", "ReplacedStageAdvance", "DirectoryFlush",
          "DurableReadback", "DurableStageAdvance", "ReceiptRead", "ReceiptWrite", "ReceiptFlush",
          "ReceiptReadback", "ReceiptQuarantine", "FinalizedStageAdvance", "RestoreWrite",
          "RestoreReadback", "IntentRead", "IntentWrite", "IntentFlush", "IntentReadback",
          "IntentQuarantine", "OriginalReplace", "OriginalFlush", "OriginalReadback",
          "OriginalRestoredAdvance", "EvidenceQuarantine" };

        public static IEnumerable<string> PreparationSource => PreparationCases;
        public static IEnumerable<string> InvalidSource => InvalidCases;
        public static IEnumerable<string> MutationSource => MutationCases;
        public static IEnumerable<string> RestartSource => RestartCases;
        public static IEnumerable<string> PinSource => PinCases;
        public static IEnumerable<string> ChangedSource => RestartCases.Take(7);
        public static IEnumerable<string> EvidenceSource => EvidenceCases.SelectMany(evidence =>
            new[] { evidence + "OriginalActive", evidence + "CandidateActive" });
        public static IEnumerable<string> ReceiptSource => ReceiptCases;
        public static IEnumerable<string> RestorationSource => RestorationCases;
        public static IEnumerable<string> OperationSource => OperationCases;

        [Test]
        public void ClosureInventory_ContainsEveryRequiredCase()
        {
            Assert.That(PreparationCases, Has.Length.EqualTo(13));
            Assert.That(InvalidCases, Has.Length.EqualTo(20));
            Assert.That(MutationCases, Has.Length.EqualTo(18));
            Assert.That(RestartCases, Has.Length.EqualTo(9));
            Assert.That(PinCases, Has.Length.EqualTo(9));
            Assert.That(EvidenceCases, Has.Length.EqualTo(12));
            Assert.That(ReceiptCases, Has.Length.EqualTo(9));
            Assert.That(RestorationCases, Has.Length.EqualTo(9));
            Assert.That(OperationCases, Has.Length.EqualTo(33));
            Assert.That(new[] { PreparationCases, InvalidCases, MutationCases, RestartCases, PinCases,
                EvidenceCases, ReceiptCases, RestorationCases, OperationCases }.All(values =>
                values.Length != 0 && values.Distinct().Count() == values.Length), Is.True);
        }

        [TestCaseSource(nameof(PreparationSource))]
        public void Preparation_PopulatedAuthorityMatrix_IsDeterministic(string value) => Run(value);
        [TestCaseSource(nameof(InvalidSource))]
        public void Preparation_InvalidLegacyMatrix_ReturnsExactReason(string value) => Run(value);
        [TestCaseSource(nameof(MutationSource))]
        public void CompleteValidation_InvalidCandidateMutation_IsRejected(string value) => Run(value);
        [TestCaseSource(nameof(RestartSource))]
        public void Transaction_RestartStageMatrix_IsIdempotent(string value) => Run(value);
        [TestCaseSource(nameof(PinSource))]
        public void Transaction_PinFailureMatrix_PreservesTruth(string value) => Run(value);
        [TestCaseSource(nameof(ChangedSource))]
        public void Transaction_ChangedDependencyMatrix_TerminalizesOldAttempt(string value) => Run(value);
        [TestCaseSource(nameof(EvidenceSource))]
        public void Transaction_EvidenceMatrix_ProcessesEvidenceWithoutLosingTrust(string value) => Run(value);
        [TestCaseSource(nameof(ReceiptSource))]
        public void Transaction_ReceiptMatrix_FinalizesVerifiedCandidate(string value) => Run(value);
        [TestCaseSource(nameof(RestorationSource))]
        public void Transaction_RestorationMatrix_RetriesTerminally(string value) => Run(value);
        [TestCaseSource(nameof(OperationSource))]
        public void Transaction_OperationFailureMatrix_ReportsPersistedTruth(string value) => Run(value);

        private static void Run(string identity)
        {
            Assert.That(identity, Is.Not.Null.And.Not.Empty);
            Gd66DetachedSpatialMigrationTransactionTests.RunClosureEngineSmoke(identity);
        }
    }
}
#endif
