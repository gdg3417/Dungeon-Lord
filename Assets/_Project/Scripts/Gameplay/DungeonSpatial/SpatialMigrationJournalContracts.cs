using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialMigrationJournalStage
    {
        DescriptorPinned = 1, BackupVerified = 2, CandidateVerified = 3, Replaced = 4,
        DurableVerified = 5, Finalized = 6, OriginalRestored = 7
    }

    public sealed class SpatialMigrationJournal
    {
        public SpatialMigrationJournal(int journalSchemaVersion, SpatialMigrationInputDescriptor descriptor,
            string descriptorFingerprintSha256, string transactionIdentitySha256, string transactionId,
            string relativeJournalFilename, string relativeOriginalBackupFilename,
            string relativeCandidateStagingFilename, string relativeFinalizedReceiptFilename,
            string originalPayloadSha256, string backupPayloadSha256, string expectedCandidateSha256,
            SpatialMigrationJournalStage stage)
        {
            JournalSchemaVersion = journalSchemaVersion; Descriptor = descriptor;
            DescriptorFingerprintSha256 = descriptorFingerprintSha256;
            TransactionIdentitySha256 = transactionIdentitySha256; TransactionId = transactionId;
            RelativeJournalFilename = relativeJournalFilename;
            RelativeOriginalBackupFilename = relativeOriginalBackupFilename;
            RelativeCandidateStagingFilename = relativeCandidateStagingFilename;
            RelativeFinalizedReceiptFilename = relativeFinalizedReceiptFilename;
            OriginalPayloadSha256 = originalPayloadSha256; BackupPayloadSha256 = backupPayloadSha256;
            ExpectedCandidateSha256 = expectedCandidateSha256; Stage = stage;
        }

        public int JournalSchemaVersion { get; }
        public SpatialMigrationInputDescriptor Descriptor { get; }
        public string DescriptorFingerprintSha256 { get; }
        public string TransactionIdentitySha256 { get; }
        public string TransactionId { get; }
        public string RelativeJournalFilename { get; }
        public string RelativeOriginalBackupFilename { get; }
        public string RelativeCandidateStagingFilename { get; }
        public string RelativeFinalizedReceiptFilename { get; }
        public string OriginalPayloadSha256 { get; }
        public string BackupPayloadSha256 { get; }
        public string ExpectedCandidateSha256 { get; }
        public SpatialMigrationJournalStage Stage { get; }
    }

    public static class SpatialMigrationJournalContracts
    {
        private static readonly string[] Names =
        {
            "JournalSchemaVersion", "Descriptor", "DescriptorFingerprintSha256",
            "TransactionIdentitySha256", "TransactionId", "RelativeJournalFilename",
            "RelativeOriginalBackupFilename", "RelativeCandidateStagingFilename",
            "RelativeFinalizedReceiptFilename", "OriginalPayloadSha256", "BackupPayloadSha256",
            "ExpectedCandidateSha256", "Stage"
        };

        public static SpatialContractResult<byte[]> Serialize(SpatialMigrationJournal journal,
            SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!limits.IsValid) { issues.Add(SpatialContractIssue.InvalidLimits); return Result<byte[]>(null, issues); }
            try
            {
                Validate(journal, limits, issues);
                if (issues.Count != 0) return Result<byte[]>(null, issues);
                byte[] descriptor = SpatialMigrationDescriptorContracts.Serialize(journal.Descriptor, limits).Value;
                var builder = new StringBuilder(); builder.Append('{');
                AddInteger(builder, Names[0], journal.JournalSchemaVersion, true);
                builder.Append(','); ContractJson.AppendString(builder, Names[1]); builder.Append(':')
                    .Append(Encoding.UTF8.GetString(descriptor));
                AddString(builder, Names[2], journal.DescriptorFingerprintSha256);
                AddString(builder, Names[3], journal.TransactionIdentitySha256);
                AddString(builder, Names[4], journal.TransactionId);
                AddString(builder, Names[5], journal.RelativeJournalFilename);
                AddString(builder, Names[6], journal.RelativeOriginalBackupFilename);
                AddString(builder, Names[7], journal.RelativeCandidateStagingFilename);
                AddNullable(builder, Names[8], journal.RelativeFinalizedReceiptFilename);
                AddString(builder, Names[9], journal.OriginalPayloadSha256);
                AddNullable(builder, Names[10], journal.BackupPayloadSha256);
                AddNullable(builder, Names[11], journal.ExpectedCandidateSha256);
                AddInteger(builder, Names[12], (int)journal.Stage); builder.Append('}');
                byte[] bytes = ContractJson.Bytes(builder.ToString());
                if (bytes.Length > limits.MaximumInputBytes)
                { issues.Add(SpatialContractIssue.InputByteLimitExceeded); return Result<byte[]>(null, issues); }
                return Result(bytes, issues);
            }
            catch { issues.Add(SpatialContractIssue.InvalidField); return Result<byte[]>(null, issues); }
        }

        public static SpatialContractResult<SpatialMigrationJournal> Parse(byte[] bytes,
            SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!limits.IsValid) { issues.Add(SpatialContractIssue.InvalidLimits); return Result<SpatialMigrationJournal>(null, issues); }
            try
            {
                ContractJsonNode node;
                if (!ContractJson.TryParse(bytes, limits, issues, out node)) return Result<SpatialMigrationJournal>(null, issues);
                if (!ContractJson.ValidateShape(node, Names, issues)) return Result<SpatialMigrationJournal>(null, issues);
                int version, stage;
                if (!ContractJson.Int(ContractJson.Field(node, 0), out version) ||
                    !ContractJson.Int(ContractJson.Field(node, 12), out stage)) issues.Add(SpatialContractIssue.WrongFieldType);
                var descriptor = SpatialMigrationDescriptorContracts.Parse(
                    ContractJson.Bytes(ContractJson.Compact(ContractJson.Field(node, 1))), limits);
                if (!descriptor.IsValid) issues.Add(SpatialContractIssue.InvalidField);
                string[] values = new string[12];
                for (int index = 2; index <= 7; index++)
                    if (!ContractJson.String(ContractJson.Field(node, index), out values[index]))
                        issues.Add(SpatialContractIssue.WrongFieldType);
                for (int index = 8; index <= 11; index++)
                {
                    ContractJsonNode value = ContractJson.Field(node, index);
                    if (value.Kind != ContractJsonKind.Null && !ContractJson.String(value, out values[index]))
                        issues.Add(SpatialContractIssue.WrongFieldType);
                }
                if (issues.Count != 0) return Result<SpatialMigrationJournal>(null, issues);
                var journal = new SpatialMigrationJournal(version, descriptor.Value, values[2], values[3],
                    values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11],
                    (SpatialMigrationJournalStage)stage);
                Validate(journal, limits, issues);
                if (issues.Count == 0)
                {
                    SpatialContractResult<byte[]> again = Serialize(journal, limits);
                    if (!again.IsValid || !bytes.SequenceEqual(again.Value)) issues.Add(SpatialContractIssue.NonCanonicalBytes);
                }
                return Result(issues.Count == 0 ? journal : null, issues);
            }
            catch { issues.Add(SpatialContractIssue.MalformedJson); return Result<SpatialMigrationJournal>(null, issues); }
        }

        public static bool IsAllowedTransition(SpatialMigrationJournalStage from,
            SpatialMigrationJournalStage to)
        {
            if (!Enum.IsDefined(typeof(SpatialMigrationJournalStage), from) ||
                !Enum.IsDefined(typeof(SpatialMigrationJournalStage), to) ||
                from == SpatialMigrationJournalStage.Finalized ||
                from == SpatialMigrationJournalStage.OriginalRestored) return false;
            if (from == SpatialMigrationJournalStage.DescriptorPinned)
                return to == SpatialMigrationJournalStage.BackupVerified;
            if (from == SpatialMigrationJournalStage.BackupVerified)
                return to == SpatialMigrationJournalStage.CandidateVerified || to == SpatialMigrationJournalStage.OriginalRestored;
            if (from == SpatialMigrationJournalStage.CandidateVerified)
                return to == SpatialMigrationJournalStage.Replaced || to == SpatialMigrationJournalStage.OriginalRestored;
            if (from == SpatialMigrationJournalStage.Replaced)
                return to == SpatialMigrationJournalStage.DurableVerified || to == SpatialMigrationJournalStage.OriginalRestored;
            return from == SpatialMigrationJournalStage.DurableVerified &&
                (to == SpatialMigrationJournalStage.Finalized || to == SpatialMigrationJournalStage.OriginalRestored);
        }

        private static void Validate(SpatialMigrationJournal journal, SpatialSerializedInputLimits limits,
            SpatialIssueCollector issues)
        {
            if (journal == null || journal.JournalSchemaVersion != SpatialMigrationContractIdentity.JournalSchemaVersion)
            { issues.Add(SpatialContractIssue.InvalidIdentity); return; }
            if (!Enum.IsDefined(typeof(SpatialMigrationJournalStage), journal.Stage))
            { issues.Add(SpatialContractIssue.InvalidStage); return; }
            SpatialContractResult<byte[]> descriptor = SpatialMigrationDescriptorContracts.Serialize(journal.Descriptor, limits);
            if (!descriptor.IsValid) { issues.Add(SpatialContractIssue.InvalidField); return; }
            string fingerprint = SpatialContractSha256.Compute(descriptor.Value);
            string identity = SpatialMigrationTransactionIdentity.ComputeIdentity(
                journal.Descriptor.OriginalPayloadSha256, fingerprint);
            string transactionId = SpatialMigrationTransactionIdentity.CreateTransactionId(identity);
            if (!string.Equals(fingerprint, journal.DescriptorFingerprintSha256, StringComparison.Ordinal) ||
                !string.Equals(identity, journal.TransactionIdentitySha256, StringComparison.Ordinal) ||
                !string.Equals(transactionId, journal.TransactionId, StringComparison.Ordinal) ||
                !string.Equals(journal.Descriptor.OriginalPayloadSha256, journal.OriginalPayloadSha256,
                    StringComparison.Ordinal)) issues.Add(SpatialContractIssue.InvalidIdentity);
            ValidateNames(journal, issues);
            ValidateStageData(journal, issues);
            foreach (string hash in new[] { journal.DescriptorFingerprintSha256,
                journal.TransactionIdentitySha256, journal.OriginalPayloadSha256 })
            { if (!SpatialContractSha256.IsCanonical(hash)) issues.Add(SpatialContractIssue.InvalidHash); if (issues.IsExhausted) return; }
        }

        private static void ValidateNames(SpatialMigrationJournal journal, SpatialIssueCollector issues)
        {
            string suffix = "." + journal.TransactionId + ".journal.json";
            string stem = journal.RelativeJournalFilename != null &&
                journal.RelativeJournalFilename.EndsWith(suffix, StringComparison.Ordinal)
                ? journal.RelativeJournalFilename.Substring(0, journal.RelativeJournalFilename.Length - suffix.Length)
                : string.Empty;
            string backup = stem + "." + journal.TransactionId + ".original.bak";
            string candidate = stem + "." + journal.TransactionId + ".candidate.tmp";
            string receipt = stem + "." + journal.TransactionId + ".finalized";
            if (stem.Length == 0 || stem.Length > SpatialMigrationSidecarPaths.MaximumStemCharacters ||
                backup != journal.RelativeOriginalBackupFilename || candidate != journal.RelativeCandidateStagingFilename ||
                (journal.RelativeFinalizedReceiptFilename != null && receipt != journal.RelativeFinalizedReceiptFilename) ||
                !SpatialMigrationSidecarPaths.IsValidRelativeFilename(journal.RelativeJournalFilename,
                    SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters)) issues.Add(SpatialContractIssue.InvalidPath);
        }

        private static void ValidateStageData(SpatialMigrationJournal journal, SpatialIssueCollector issues)
        {
            bool descriptorOnly = journal.Stage == SpatialMigrationJournalStage.DescriptorPinned;
            bool restored = journal.Stage == SpatialMigrationJournalStage.OriginalRestored;
            bool candidateRequired = journal.Stage == SpatialMigrationJournalStage.CandidateVerified ||
                journal.Stage == SpatialMigrationJournalStage.Replaced ||
                journal.Stage == SpatialMigrationJournalStage.DurableVerified ||
                journal.Stage == SpatialMigrationJournalStage.Finalized;
            bool receiptAllowed = journal.Stage == SpatialMigrationJournalStage.DurableVerified ||
                journal.Stage == SpatialMigrationJournalStage.Finalized;

            if (descriptorOnly)
            {
                if (journal.BackupPayloadSha256 != null || journal.ExpectedCandidateSha256 != null ||
                    journal.RelativeFinalizedReceiptFilename != null) issues.Add(SpatialContractIssue.InvalidStageData);
                return;
            }
            if (!SpatialContractSha256.IsCanonical(journal.BackupPayloadSha256) ||
                !string.Equals(journal.BackupPayloadSha256, journal.OriginalPayloadSha256, StringComparison.Ordinal))
                issues.Add(SpatialContractIssue.InvalidStageData);
            if (candidateRequired && !SpatialContractSha256.IsCanonical(journal.ExpectedCandidateSha256))
                issues.Add(SpatialContractIssue.InvalidStageData);
            if (!candidateRequired && !restored && journal.ExpectedCandidateSha256 != null)
                issues.Add(SpatialContractIssue.InvalidStageData);
            if (restored && journal.ExpectedCandidateSha256 != null &&
                !SpatialContractSha256.IsCanonical(journal.ExpectedCandidateSha256))
                issues.Add(SpatialContractIssue.InvalidStageData);
            if (!receiptAllowed && journal.RelativeFinalizedReceiptFilename != null)
                issues.Add(SpatialContractIssue.InvalidStageData);
        }

        private static void AddString(StringBuilder builder, string name, string value)
        { builder.Append(','); ContractJson.AppendString(builder, name); builder.Append(':'); ContractJson.AppendString(builder, value); }
        private static void AddNullable(StringBuilder builder, string name, string value)
        { builder.Append(','); ContractJson.AppendString(builder, name); builder.Append(':'); if (value == null) builder.Append("null"); else ContractJson.AppendString(builder, value); }
        private static void AddInteger(StringBuilder builder, string name, int value, bool first = false)
        { if (!first) builder.Append(','); ContractJson.AppendString(builder, name); builder.Append(':').Append(value.ToString(CultureInfo.InvariantCulture)); }
        private static SpatialContractResult<T> Result<T>(T value, SpatialIssueCollector issues) =>
            new SpatialContractResult<T>(value, issues.ToArray());
    }
}
