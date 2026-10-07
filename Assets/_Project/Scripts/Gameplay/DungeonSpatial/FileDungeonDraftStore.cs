using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    internal sealed class DungeonDraftCandidate
    {
        public int Version;
        public string DraftId;
        public string Baseline;
        public long Sequence;
        public string PredecessorCommit;
        public DungeonDraftCommand[] Commands;
    }

    [Serializable]
    internal sealed class DungeonDraftCommitRecord
    {
        public int Version;
        public string DraftId;
        public string Baseline;
        public long Sequence;
        public string PredecessorCommit;
        public string CandidateHash;
    }

    /// <summary>
    /// Immutable, bounded delta journal. A candidate never commits itself. Each separate commit
    /// binds the exact candidate and preceding commit. No rename, replacement or rollback is used.
    /// The injected filesystem owns platform durability qualification; this protocol adds none.
    /// </summary>
    public sealed class FileDungeonDraftStore : IDungeonDraftStore
    {
        private const int RecordVersion = 1;
        private readonly string path, directory, prefix;
        private readonly ISpatialMigrationFileSystem files;
        private readonly SaveSpatialMigrationLimitsProfile limits;
        private readonly int maximumFiles;
        public bool OutcomeUnknown { get; private set; }

        public FileDungeonDraftStore(string path, ISpatialMigrationFileSystem files,
            SaveSpatialMigrationLimitsProfile limits)
        {
            this.path = Path.GetFullPath(path); directory = Path.GetDirectoryName(this.path);
            prefix = Path.GetFileName(this.path) + ".draft-";
            this.files = files; this.limits = limits;
            maximumFiles = checked((limits.Raw.MaximumArrayElements + 1) * 2);
        }

        private sealed class Evidence
        {
            internal string Id, CommitHash;
            internal DungeonDraftJournal Journal;
            internal readonly List<string> Paths = new List<string>();
        }

        public byte[] Read()
        {
            try { return Encode(ReadEvidence().Journal); }
            catch { throw new IOException(TransactionalDungeonDraft.RecoveryFailedReason); }
        }

        public bool Write(byte[] expected, byte[] next)
        {
            if (OutcomeUnknown) return false; // Resolve an uncertain result through a new recovery session.
            try
            {
                var intended = Parse<DungeonDraftJournal>(next);
                ValidateJournal(intended);
                var evidence = ReadEvidence();
                if (!SameNullable(expected, Encode(evidence.Journal))) return false;
                long sequence = intended.PrefixSequence;
                if (evidence.Journal == null)
                {
                    if (sequence != 0 || evidence.Paths.Count != 0) return false;
                }
                else
                {
                    var previous = evidence.Journal;
                    if (intended.DraftId != previous.DraftId || intended.BaselineFingerprint != previous.BaselineFingerprint ||
                        sequence != previous.PrefixSequence + 1 ||
                        previous.Commands.Where((command, index) =>
                            !Encode(command).SequenceEqual(Encode(intended.Commands[index]))).Any())
                        return false;
                }
                string stem = Stem(intended.DraftId, sequence);
                string candidatePath = stem + ".candidate", commitPath = stem + ".commit";
                if (files.Exists(commitPath)) return false;
                var candidate = new DungeonDraftCandidate { Version = RecordVersion, DraftId = intended.DraftId,
                    Baseline = intended.BaselineFingerprint, Sequence = sequence,
                    PredecessorCommit = evidence.CommitHash ?? string.Empty,
                    Commands = sequence == 0 ? Array.Empty<DungeonDraftCommand>() : new[] { intended.Commands[(int)sequence - 1].Copy() } };
                byte[] payload = Encode(candidate);
                var commit = new DungeonDraftCommitRecord { Version = RecordVersion, DraftId = candidate.DraftId,
                    Baseline = candidate.Baseline, Sequence = sequence, PredecessorCommit = candidate.PredecessorCommit,
                    CandidateHash = SpatialContractSha256.Compute(payload) };
                byte[] receipt = Encode(commit);
                if (payload.Length + receipt.Length > limits.Raw.MaximumInputBytes) return false;
                OutcomeUnknown = true;
                // Recovery has proved that this slot has no commit. Removing only its orphan
                // candidate cannot remove a committed command or its predecessor evidence.
                if (files.Exists(candidatePath)) { files.DeleteFile(candidatePath); files.FlushDirectory(directory); }
                files.WriteAllBytesDurable(candidatePath, payload);
                files.FlushDirectory(directory);
                if (!payload.SequenceEqual(files.ReadAllBytes(candidatePath))) return false;
                // The only transition that can establish a new committed generation.
                // Its uncertain return is resolved from this exact record on restart.
                files.WriteAllBytesDurable(commitPath, receipt);
                files.FlushDirectory(directory);
                if (!receipt.SequenceEqual(files.ReadAllBytes(commitPath))) return false;
                if (!next.SequenceEqual(Read())) return false;
                OutcomeUnknown = false;
                return true;
            }
            catch { return false; }
        }

        public bool Delete(byte[] expected)
        {
            try
            {
                // A null expected payload is the explicit Discard action offered after
                // fail-closed recovery. It may remove bounded owned evidence, never canonical data.
                var evidence = expected == null ? new Evidence() : ReadEvidence();
                if (expected == null)
                {
                    if (!files.IsPathContainedWithoutRedirection(directory, path)) return false;
                    OutcomeUnknown = true;
                    if (files.Exists(path)) files.DeleteFile(path); // Unqualified old draft format, no migration.
                    evidence.Paths.AddRange(Discover());
                }
                if (expected != null && evidence.Journal != null)
                {
                    var owned = Parse<DungeonDraftJournal>(expected);
                    if (owned.DraftId != evidence.Id || owned.BaselineFingerprint != evidence.Journal.BaselineFingerprint)
                        return false;
                }
                // Newest commit first, then its candidate. Interruption cannot leave a later
                // committed generation with a deliberately removed predecessor.
                OutcomeUnknown = true;
                foreach (string entry in evidence.Paths.OrderByDescending(value => value, StringComparer.Ordinal))
                    files.DeleteFile(entry);
                files.FlushDirectory(directory);
                if (Discover().Count != 0) return false;
                OutcomeUnknown = false;
                return true;
            }
            catch { return false; }
        }

        private Evidence ReadEvidence()
        {
            var result = new Evidence();
            var candidates = new SortedDictionary<long, string>();
            var commits = new SortedDictionary<long, string>();
            foreach (string entry in Discover())
            {
                string name = Path.GetFileName(entry).Substring(prefix.Length);
                string[] parts = name.Split('.');
                if (parts.Length != 3 || !Hex(parts[0], 32) ||
                    !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long sequence) ||
                    sequence < 0 || sequence > limits.Raw.MaximumArrayElements ||
                    parts[1] != sequence.ToString("D6", CultureInfo.InvariantCulture) ||
                    (parts[2] != "candidate" && parts[2] != "commit")) throw new IOException();
                if (result.Id != null && result.Id != parts[0]) throw new IOException();
                result.Id = parts[0]; result.Paths.Add(entry);
                (parts[2] == "candidate" ? candidates : commits).Add(sequence, entry);
            }
            if (commits.Count == 0)
            {
                // No implicit new session over an interrupted initial transaction. Recovery
                // surfaces a stable failure and explicit Discard can remove its owned evidence.
                if (candidates.Count != 0) throw new IOException();
                return result;
            }
            long expected = 0;
            string predecessor = string.Empty, baseline = null;
            var commands = new List<DungeonDraftCommand>();
            int remaining = limits.Raw.MaximumInputBytes;
            foreach (var entry in commits)
            {
                if (entry.Key != expected || !candidates.TryGetValue(expected, out string candidatePath)) throw new IOException();
                byte[] commitBytes = ReadBounded(entry.Value, ref remaining);
                var commit = Parse<DungeonDraftCommitRecord>(commitBytes);
                if (commit.Version != RecordVersion || commit.DraftId != result.Id || commit.Sequence != expected ||
                    commit.PredecessorCommit != predecessor || !Hex(commit.CandidateHash, 64) || !Hex(commit.Baseline, 64))
                    throw new IOException();
                if (baseline != null && baseline != commit.Baseline) throw new IOException();
                baseline = commit.Baseline;
                byte[] payload = ReadBounded(candidatePath, ref remaining);
                if (SpatialContractSha256.Compute(payload) != commit.CandidateHash) throw new IOException();
                var candidate = Parse<DungeonDraftCandidate>(payload);
                if (candidate.Version != RecordVersion || candidate.DraftId != result.Id || candidate.Sequence != expected ||
                    candidate.Baseline != baseline || candidate.PredecessorCommit != predecessor ||
                    candidate.Commands == null || candidate.Commands.Length != (expected == 0 ? 0 : 1) ||
                    (expected > 0 && (candidate.Commands[0] == null || candidate.Commands[0].Sequence != expected)))
                    throw new IOException();
                if (expected > 0) commands.Add(candidate.Commands[0].Copy());
                predecessor = SpatialContractSha256.Compute(commitBytes);
                expected++;
            }
            // Only the immediate next uncommitted candidate is a possible interrupted write.
            if (candidates.Keys.Any(sequence => sequence > expected)) throw new IOException();
            result.CommitHash = predecessor;
            result.Journal = new DungeonDraftJournal { FormatVersion = TransactionalDungeonDraft.CurrentFormatVersion,
                DraftId = result.Id, BaselineFingerprint = baseline, PrefixSequence = expected - 1, Commands = commands.ToArray() };
            return result;
        }

        private List<string> Discover()
        {
            if (!files.IsPathContainedWithoutRedirection(directory, path) || files.Exists(path)) throw new IOException();
            var entries = files.EnumerateFiles(directory, prefix + "*", maximumFiles + 1).ToList();
            if (entries.Count > maximumFiles || entries.Any(entry =>
                Path.GetFullPath(entry) != entry || Path.GetDirectoryName(entry) != directory ||
                !Path.GetFileName(entry).StartsWith(prefix, StringComparison.Ordinal) ||
                !files.IsPathContainedWithoutRedirection(directory, entry))) throw new IOException();
            return entries;
        }

        private byte[] ReadBounded(string entry, ref int remaining)
        {
            if ((files is WindowsSpatialMigrationFileSystem || files is RuntimeSpatialMigrationFileSystem) &&
                new FileInfo(entry).Length > remaining) throw new IOException();
            byte[] bytes = files.ReadAllBytes(entry);
            if (bytes == null || bytes.Length > remaining) throw new IOException();
            remaining -= bytes.Length;
            return bytes;
        }

        private T Parse<T>(byte[] bytes) where T : class
        {
            if (bytes == null || bytes.Length > limits.Raw.MaximumInputBytes) throw new IOException();
            var issues = new SpatialIssueCollector(limits.Canonical.Serialized.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits.Canonical.Serialized, issues, out _)) throw new IOException();
            var value = JsonUtility.FromJson<T>(new UTF8Encoding(false, true).GetString(bytes));
            if (value == null || !bytes.SequenceEqual(Encode(value))) throw new IOException();
            return value;
        }

        private void ValidateJournal(DungeonDraftJournal journal)
        {
            if (journal.FormatVersion != TransactionalDungeonDraft.CurrentFormatVersion || !Hex(journal.DraftId, 32) ||
                !Hex(journal.BaselineFingerprint, 64) || journal.Commands == null ||
                journal.Commands.Length > limits.Raw.MaximumArrayElements || journal.PrefixSequence != journal.Commands.Length ||
                journal.Commands.Where((command, index) => command == null || command.Sequence != index + 1L).Any())
                throw new IOException();
        }

        private string Stem(string id, long sequence) => Path.Combine(directory,
            prefix + id + "." + sequence.ToString("D6", CultureInfo.InvariantCulture));
        private static byte[] Encode(object value) => value == null ? null : Encoding.UTF8.GetBytes(JsonUtility.ToJson(value));
        private static bool SameNullable(byte[] left, byte[] right) => left == null ? right == null : right != null && left.SequenceEqual(right);
        private static bool Hex(string value, int length) => value != null && value.Length == length &&
            value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
    }
}
