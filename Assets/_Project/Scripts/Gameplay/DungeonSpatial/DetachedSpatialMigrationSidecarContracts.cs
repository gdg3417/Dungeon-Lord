using System;
using System.Collections.Generic;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal sealed class DetachedFinalizationReceipt
    {
        internal DetachedFinalizationReceipt(string transactionId, string descriptorFingerprint,
            string candidateSha256)
        { TransactionId = transactionId; DescriptorFingerprint = descriptorFingerprint;
          CandidateSha256 = candidateSha256; }

        internal string TransactionId { get; }
        internal string DescriptorFingerprint { get; }
        internal string CandidateSha256 { get; }
    }

    internal static class DetachedFinalizationReceiptContract
    {
        internal static byte[] Serialize(DetachedFinalizationReceipt receipt,
            SpatialSerializedInputLimits limits)
        {
            if (receipt == null || !Valid(receipt)) return null;
            var writer = new ContractJsonWriter(limits);
            writer.Node(); writer.Token("{");
            writer.String("TransactionId"); writer.Token(":"); writer.String(receipt.TransactionId);
            writer.Token(","); writer.String("DescriptorFingerprintSha256"); writer.Token(":");
            writer.String(receipt.DescriptorFingerprint);
            writer.Token(","); writer.String("CandidateSha256"); writer.Token(":");
            writer.String(receipt.CandidateSha256); writer.Token("}");
            return writer.Finish();
        }

        internal static DetachedFinalizationReceipt Parse(byte[] bytes, SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits, issues, out ContractJsonNode root) ||
                root.Kind != ContractJsonKind.Object || root.Fields.Count != 3 ||
                !Field(root, 0, "TransactionId") ||
                !Field(root, 1, "DescriptorFingerprintSha256") ||
                !Field(root, 2, "CandidateSha256")) return null;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ContractJsonNode> field in root.Fields)
                if (!names.Add(field.Key)) return null;
            var receipt = new DetachedFinalizationReceipt(root.Fields[0].Value.Text,
                root.Fields[1].Value.Text, root.Fields[2].Value.Text);
            byte[] canonical = Serialize(receipt, limits);
            return Valid(receipt) && Same(bytes, canonical) ? receipt : null;
        }

        private static bool Field(ContractJsonNode root, int index, string name) =>
            root.Fields[index].Key == name && root.Fields[index].Value.Kind == ContractJsonKind.String;
        private static bool Valid(DetachedFinalizationReceipt value) =>
            SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(value.TransactionId) &&
            SpatialContractSha256.IsCanonical(value.DescriptorFingerprint) &&
            SpatialContractSha256.IsCanonical(value.CandidateSha256);
        private static bool Same(byte[] left, byte[] right)
        { if (left == null || right == null || left.Length != right.Length) return false;
          for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
          return true; }
    }

    internal sealed class DetachedRestorationIntent
    {
        internal DetachedRestorationIntent(string transactionId, string descriptorFingerprint,
            string originalSha256, string backupSha256, string journalFilename, int journalStage)
        { TransactionId = transactionId; DescriptorFingerprint = descriptorFingerprint;
          OriginalSha256 = originalSha256; BackupSha256 = backupSha256;
          JournalFilename = journalFilename; JournalStage = journalStage; }
        internal string TransactionId { get; }
        internal string DescriptorFingerprint { get; }
        internal string OriginalSha256 { get; }
        internal string BackupSha256 { get; }
        internal string JournalFilename { get; }
        internal int JournalStage { get; }
    }

    internal static class DetachedRestorationIntentContract
    {
        internal static byte[] Serialize(DetachedRestorationIntent intent, SpatialSerializedInputLimits limits)
        {
            if (!Valid(intent)) return null;
            var writer = new ContractJsonWriter(limits);
            writer.Node(); writer.Token("{");
            String(writer, "TransactionId", intent.TransactionId, false);
            String(writer, "DescriptorFingerprintSha256", intent.DescriptorFingerprint, true);
            String(writer, "OriginalSha256", intent.OriginalSha256, true);
            String(writer, "BackupSha256", intent.BackupSha256, true);
            String(writer, "JournalFilename", intent.JournalFilename, true);
            writer.Token(","); writer.String("JournalStage"); writer.Token(":");
            writer.Token(intent.JournalStage.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.Token("}"); return writer.Finish();
        }

        internal static DetachedRestorationIntent Parse(byte[] bytes, SpatialSerializedInputLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits, issues, out ContractJsonNode root) ||
                root.Kind != ContractJsonKind.Object || root.Fields.Count != 6) return null;
            string[] names = { "TransactionId", "DescriptorFingerprintSha256", "OriginalSha256",
                "BackupSha256", "JournalFilename", "JournalStage" };
            for (int index = 0; index < names.Length; index++)
                if (root.Fields[index].Key != names[index] || root.Fields[index].Value.Kind !=
                    (index == 5 ? ContractJsonKind.Number : ContractJsonKind.String)) return null;
            if (!int.TryParse(root.Fields[5].Value.Text, out int stage)) return null;
            var intent = new DetachedRestorationIntent(root.Fields[0].Value.Text, root.Fields[1].Value.Text,
                root.Fields[2].Value.Text, root.Fields[3].Value.Text, root.Fields[4].Value.Text, stage);
            return Valid(intent) && Same(bytes, Serialize(intent, limits)) ? intent : null;
        }

        private static void String(ContractJsonWriter writer, string name, string value, bool comma)
        { if (comma) writer.Token(","); writer.String(name); writer.Token(":"); writer.String(value); }
        private static bool Valid(DetachedRestorationIntent value) => value != null &&
            SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(value.TransactionId) &&
            SpatialContractSha256.IsCanonical(value.DescriptorFingerprint) &&
            SpatialContractSha256.IsCanonical(value.OriginalSha256) &&
            SpatialContractSha256.IsCanonical(value.BackupSha256) &&
            SpatialMigrationSidecarPaths.IsValidRelativeFilename(value.JournalFilename,
                SpatialMigrationSidecarPaths.MaximumGeneratedFilenameCharacters) &&
            value.JournalStage >= (int)SpatialMigrationJournalStage.DescriptorPinned &&
            value.JournalStage <= (int)SpatialMigrationJournalStage.DurableVerified;
        private static bool Same(byte[] left, byte[] right)
        { if (left == null || right == null || left.Length != right.Length) return false;
          for (int index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
          return true; }
    }
}
