using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal enum SpatialMigrationEvidenceFilenameKind
    {
        Journal, JournalNext, OriginalBackup, RestorationIntent, RestoreStaging,
        CandidateStaging, FinalizationReceipt, ExistingQuarantine, Unknown
    }

    /// <summary>
    /// Pure startup probe sharing the transaction's broad discovery and suffix authority. It does
    /// not parse, quarantine, or recover evidence; it only prevents unsafe native creation.
    /// </summary>
    internal static class SpatialMigrationRecoveryEvidenceProbe
    {
        internal static bool HasRecoveryRelevantEvidence(string activePath,
            ISpatialMigrationFileSystem fileSystem, int maximumCollectionRecords)
        {
            if (string.IsNullOrEmpty(activePath) || fileSystem == null || maximumCollectionRecords <= 0)
                throw new IOException("Invalid GD66 evidence probe input.");
            string directory = Path.GetFullPath(Path.GetDirectoryName(activePath));
            string stem = Path.GetFileNameWithoutExtension(activePath);
            IReadOnlyList<string> paths = fileSystem.EnumerateFiles(directory,
                SpatialMigrationSidecarPaths.EvidenceSearchPatternFromStem(stem),
                maximumCollectionRecords + 1);
            if (paths.Count > maximumCollectionRecords)
                throw new IOException("GD66 evidence limit exceeded.");
            foreach (string enumerated in paths.OrderBy(value => value, StringComparer.Ordinal))
            {
                string path = Path.GetFullPath(enumerated);
                // DiscoverEvidence classifies every redirected broad-pattern result as unresolved.
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, path))
                    throw new IOException("GD66 evidence redirected.");
                if (ClassifyFilename(Path.GetFileName(path)) !=
                    SpatialMigrationEvidenceFilenameKind.Unknown) return true;
            }
            return false;
        }

        internal static SpatialMigrationEvidenceFilenameKind ClassifyFilename(string name)
        {
            if (name == null) return SpatialMigrationEvidenceFilenameKind.Unknown;
            if (name.EndsWith(".journal.json", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.Journal;
            if (name.EndsWith(".journal.json.next", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.JournalNext;
            if (name.EndsWith(".original.bak.restore.intent", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.RestorationIntent;
            if (name.EndsWith(".original.bak.restore", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.RestoreStaging;
            if (name.EndsWith(".original.bak", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.OriginalBackup;
            if (name.EndsWith(".candidate.tmp", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.CandidateStaging;
            if (name.EndsWith(".finalized", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.FinalizationReceipt;
            if (name.EndsWith(".evidence", StringComparison.Ordinal))
                return SpatialMigrationEvidenceFilenameKind.ExistingQuarantine;
            return SpatialMigrationEvidenceFilenameKind.Unknown;
        }
    }
}
