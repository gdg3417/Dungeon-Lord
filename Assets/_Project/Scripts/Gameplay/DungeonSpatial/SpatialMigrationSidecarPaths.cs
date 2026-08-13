using System;
using System.Collections.Generic;
using System.IO;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class SpatialMigrationSidecarNames
    {
        internal SpatialMigrationSidecarNames(string journal, string backup, string candidate, string receipt)
        { Journal = journal; OriginalBackup = backup; CandidateStaging = candidate; FinalizedReceipt = receipt; }
        public string Journal { get; }
        public string OriginalBackup { get; }
        public string CandidateStaging { get; }
        public string FinalizedReceipt { get; }
    }

    public static class SpatialMigrationSidecarPaths
    {
        public const int MaximumStemCharacters = 80;
        public const int MaximumGeneratedFilenameCharacters = 180;
        public const int WindowsMaximumAbsolutePathCharacters = 240;

        public static SpatialContractResult<string> EvidenceSearchPattern(string activeFilename)
        {
            var issues = new List<SpatialContractIssue>();
            if (!IsValidRelativeFilename(activeFilename, MaximumGeneratedFilenameCharacters) ||
                !string.Equals(Path.GetFileName(activeFilename), activeFilename, StringComparison.Ordinal))
                issues.Add(SpatialContractIssue.InvalidPath);
            string stem = issues.Count == 0 ? Path.GetFileNameWithoutExtension(activeFilename) : string.Empty;
            if (stem.Length == 0 || stem.Length > MaximumStemCharacters)
                issues.Add(SpatialContractIssue.InvalidPath);
            return new SpatialContractResult<string>(issues.Count == 0 ? EvidenceSearchPatternFromStem(stem) : null, issues);
        }

        internal static string EvidenceSearchPatternFromStem(string stem) => stem + ".gd66-*";

        public static bool IsOwnedEvidenceFilename(string activeFilename, string evidenceFilename)
        {
            SpatialContractResult<string> pattern = EvidenceSearchPattern(activeFilename);
            if (!pattern.IsValid || string.IsNullOrEmpty(evidenceFilename)) return false;
            string prefix = pattern.Value.Substring(0, pattern.Value.Length - 1);
            if (!evidenceFilename.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string remainder = evidenceFilename.Substring(prefix.Length);
            string[] suffixes = { ".journal.json", ".journal.json.next", ".original.bak",
                ".original.bak.restore.intent", ".original.bak.restore", ".candidate.tmp",
                ".finalized", ".evidence" };
            string suffix = null;
            foreach (string value in suffixes)
                if (remainder.EndsWith(value, StringComparison.Ordinal)) { suffix = value; break; }
            if (suffix == null) return false;
            string transaction = remainder.Substring(0, remainder.Length - suffix.Length);
            return SpatialMigrationTransactionIdentity.IsCanonicalTransactionId("gd66-" + transaction);
        }

        public static SpatialContractResult<SpatialMigrationSidecarNames> Derive(string activeFilename,
            string transactionId)
        {
            var issues = new List<SpatialContractIssue>();
            try
            {
                if (!IsValidRelativeFilename(activeFilename, MaximumGeneratedFilenameCharacters) ||
                    !string.Equals(Path.GetFileName(activeFilename), activeFilename, StringComparison.Ordinal))
                    issues.Add(SpatialContractIssue.InvalidPath);
                if (!SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(transactionId))
                    issues.Add(SpatialContractIssue.InvalidIdentity);
                string stem = issues.Count == 0 ? Path.GetFileNameWithoutExtension(activeFilename) : string.Empty;
                if (stem.Length == 0 || stem.Length > MaximumStemCharacters)
                    issues.Add(SpatialContractIssue.InvalidPath);
                if (issues.Count != 0) return new SpatialContractResult<SpatialMigrationSidecarNames>(null, issues);

                var names = new SpatialMigrationSidecarNames(
                    stem + "." + transactionId + ".journal.json",
                    stem + "." + transactionId + ".original.bak",
                    stem + "." + transactionId + ".candidate.tmp",
                    stem + "." + transactionId + ".finalized");
                if (!IsValidRelativeFilename(names.Journal, MaximumGeneratedFilenameCharacters) ||
                    !IsValidRelativeFilename(names.OriginalBackup, MaximumGeneratedFilenameCharacters) ||
                    !IsValidRelativeFilename(names.CandidateStaging, MaximumGeneratedFilenameCharacters) ||
                    !IsValidRelativeFilename(names.FinalizedReceipt, MaximumGeneratedFilenameCharacters))
                    issues.Add(SpatialContractIssue.InvalidPath);
                return new SpatialContractResult<SpatialMigrationSidecarNames>(issues.Count == 0 ? names : null, issues);
            }
            catch
            { issues.Add(SpatialContractIssue.InvalidPath); return new SpatialContractResult<SpatialMigrationSidecarNames>(null, issues); }
        }

        public static bool TryResolveContained(string saveDirectory, string relativeFilename,
            int maximumAbsolutePathCharacters, out string absolutePath)
        {
            absolutePath = null;
            if (string.IsNullOrEmpty(saveDirectory) || maximumAbsolutePathCharacters <= 0 ||
                !IsValidRelativeFilename(relativeFilename, MaximumGeneratedFilenameCharacters)) return false;
            try
            {
                string directory = Path.GetFullPath(saveDirectory);
                string combined = Path.Combine(directory, relativeFilename);
                string normalized = Path.GetFullPath(combined);
                if (!string.Equals(combined, normalized, StringComparison.Ordinal)) return false;
                string prefix = directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                    ? directory : directory + Path.DirectorySeparatorChar;
                if (!normalized.StartsWith(prefix, StringComparison.Ordinal) ||
                    normalized.Length > maximumAbsolutePathCharacters) return false;
                absolutePath = normalized;
                return true;
            }
            catch { return false; }
        }

        public static bool IsValidRelativeFilename(string value, int maximumCharacters)
        {
            if (string.IsNullOrEmpty(value) || value.Length > maximumCharacters || value == "." || value == ".." ||
                value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0 || value.IndexOf(':') >= 0 ||
                value.EndsWith(".", StringComparison.Ordinal) || value.EndsWith(" ", StringComparison.Ordinal) ||
                Uri.IsWellFormedUriString(value, UriKind.Absolute) || Path.IsPathRooted(value)) return false;
            foreach (char character in value)
                if (character < 0x20 || "<>\"|?*".IndexOf(character) >= 0) return false;
            string deviceToken = value.Split('.')[0];
            if (IsWindowsReservedDeviceToken(deviceToken)) return false;
            try
            {
                return string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal) &&
                    string.Equals(Path.GetFullPath(value), Path.Combine(Path.GetFullPath("."), value),
                        StringComparison.Ordinal);
            }
            catch { return false; }
        }

        private static bool IsWindowsReservedDeviceToken(string value)
        {
            if (string.Equals(value, "CON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "PRN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "AUX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "NUL", StringComparison.OrdinalIgnoreCase)) return true;
            if (value == null || value.Length != 4) return false;
            char suffix = value[3];
            if (suffix < '1' || suffix > '9') return false;
            string prefix = value.Substring(0, 3);
            return string.Equals(prefix, "COM", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(prefix, "LPT", StringComparison.OrdinalIgnoreCase);
        }
    }
}
