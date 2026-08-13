using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    /// <summary>Shared exact-byte atomic replacement authority for canonical saves and legacy repair.</summary>
    internal static class ExactCompleteSaveAtomicPersistence
    {
        internal static string Persist(string activePath, ISpatialMigrationFileSystem fileSystem,
            byte[] original, byte[] candidate, int maximumEvidence)
        {
            if (string.IsNullOrEmpty(activePath) || original == null || candidate == null)
                return DetachedCanonicalWriteAuthority.AtomicSaveFailedReason;
            string directory;
            try
            {
                string normalized = Path.GetFullPath(activePath);
                directory = Path.GetDirectoryName(normalized);
                if (normalized != activePath || !fileSystem.Exists(activePath))
                    return DetachedCanonicalWriteAuthority.AtomicSaveFailedReason;
                byte[] activeBefore = fileSystem.ReadAllBytes(activePath);
                if (!Same(activeBefore, original) && !Same(activeBefore, candidate))
                    return DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                string settlement;
                try { settlement = SettleAllEvidence(fileSystem, activePath, directory,
                    activeBefore, maximumEvidence); }
                catch { return DetachedCanonicalWriteAuthority.RecoveryRequiredReason; }
                if (settlement != null) return settlement;
                string token = SpatialContractSha256.Compute(original).Substring(0, 16) + "-" +
                    SpatialContractSha256.Compute(candidate).Substring(0, 16);
                string rollback = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".rollback");
                string staging = Path.Combine(directory, Path.GetFileName(activePath) +
                    ".canonical-write-" + token + ".candidate");
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, rollback) ||
                    !fileSystem.IsPathContainedWithoutRedirection(directory, staging))
                    return DetachedCanonicalWriteAuthority.AtomicSaveFailedReason;
                if (Same(activeBefore, candidate)) return null;
                if (!Same(activeBefore, original))
                    return DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                fileSystem.WriteAllBytesDurable(rollback, original);
                if (!Same(fileSystem.ReadAllBytes(rollback), original))
                    return DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                fileSystem.WriteAllBytesDurable(staging, candidate);
                if (!Same(fileSystem.ReadAllBytes(staging), candidate))
                    return DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                try
                {
                    fileSystem.ReplaceSameDirectoryAtomic(staging, activePath);
                    fileSystem.FlushDirectory(directory);
                    if (!Same(fileSystem.ReadAllBytes(activePath), candidate)) throw new IOException();
                }
                catch
                {
                    return Restore(fileSystem, rollback, activePath, directory, original)
                        ? DetachedCanonicalWriteAuthority.AtomicSaveFailedReason
                        : DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                }
                try { fileSystem.DeleteFile(rollback); fileSystem.FlushDirectory(directory); }
                catch { }
                return null;
            }
            catch
            {
                try
                {
                    byte[] active = fileSystem.ReadAllBytes(activePath);
                    return Same(active, original)
                        ? DetachedCanonicalWriteAuthority.AtomicSaveFailedReason
                        : DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
                }
                catch { return DetachedCanonicalWriteAuthority.RecoveryRequiredReason; }
            }
        }

        internal static IReadOnlyList<string> DiscoverOwnedEvidence(string activePath,
            ISpatialMigrationFileSystem fileSystem, int maximumEvidence)
        {
            if (maximumEvidence <= 0) throw new IOException();
            string directory = Path.GetDirectoryName(activePath);
            string prefix = Path.GetFileName(activePath) + ".canonical-write-";
            IReadOnlyList<string> discovered = fileSystem.EnumerateFiles(directory, prefix + "*",
                maximumEvidence + 1);
            if (discovered.Count > maximumEvidence) throw new IOException();
            var owned = new List<string>();
            foreach (string path in discovered.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!IsEvidenceName(Path.GetFileName(path), prefix)) continue;
                if (!fileSystem.IsPathContainedWithoutRedirection(directory, path)) throw new IOException();
                owned.Add(path);
            }
            return owned;
        }

        private static string SettleAllEvidence(ISpatialMigrationFileSystem fileSystem,
            string activePath, string directory, byte[] validatedActive, int maximumEvidence)
        {
            IReadOnlyList<string> evidence = DiscoverOwnedEvidence(activePath, fileSystem, maximumEvidence);
            foreach (string path in evidence) fileSystem.DeleteFile(path);
            if (evidence.Count != 0) fileSystem.FlushDirectory(directory);
            return Same(fileSystem.ReadAllBytes(activePath), validatedActive) ? null
                : DetachedCanonicalWriteAuthority.RecoveryRequiredReason;
        }

        private static bool IsEvidenceName(string name, string prefix)
        {
            if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string remainder = name.Substring(prefix.Length);
            string suffix = remainder.EndsWith(".rollback", StringComparison.Ordinal) ? ".rollback" :
                remainder.EndsWith(".candidate", StringComparison.Ordinal) ? ".candidate" : null;
            if (suffix == null) return false;
            string token = remainder.Substring(0, remainder.Length - suffix.Length);
            if (token.Length != 33 || token[16] != '-') return false;
            for (int index = 0; index < token.Length; index++)
                if (index != 16 && !((token[index] >= '0' && token[index] <= '9') ||
                    (token[index] >= 'a' && token[index] <= 'f'))) return false;
            return true;
        }

        private static bool Restore(ISpatialMigrationFileSystem fileSystem, string rollback,
            string activePath, string directory, byte[] original)
        {
            try
            {
                if (fileSystem.Exists(rollback))
                {
                    fileSystem.ReplaceSameDirectoryAtomic(rollback, activePath);
                    fileSystem.FlushDirectory(directory);
                    return Same(fileSystem.ReadAllBytes(activePath), original);
                }
            }
            catch { }
            return false;
        }

        internal static bool Same(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
                if (left[index] != right[index]) return false;
            return true;
        }
    }
}
