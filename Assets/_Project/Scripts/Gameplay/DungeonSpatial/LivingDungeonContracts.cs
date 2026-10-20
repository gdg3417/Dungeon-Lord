using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Immutable technical qualification policy, independently injected from gameplay demand/capacity.
    public sealed class LivingDungeonProfile
    {
        public string Id { get; }
        public int MaximumActive { get; }
        public int MaximumRetained { get; }
        public int MaximumCheckpointBytes { get; }
        public long MaximumPartyReplay { get; }
        public long MaximumTotalReplay { get; }
        public int MaximumBaseSaveBytes { get; }
        public int MaximumOwnerBytes { get; }
        public RunLogicalTiming QualificationTiming { get; }
        public string QualificationPostureId { get; }
        public SpatialSerializedInputLimits BaseBudget { get; }
        public long MaximumCombatWork { get; }
        public LivingDungeonProfile(string id, int active, int retained, int checkpointBytes,
            long partyReplay, long totalReplay, int baseSaveBytes, int ownerBytes,
            RunLogicalTiming qualificationTiming = null, string qualificationPostureId = null,
            SpatialSerializedInputLimits baseBudget = default, long maximumCombatWork = 0)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64 || active <= 0 || retained < active ||
                checkpointBytes <= 0 || partyReplay <= 0 || totalReplay < partyReplay ||
                baseSaveBytes <= 0 || ownerBytes <= 0 || maximumCombatWork < 0 ||
                maximumCombatWork != 0 && id == "living.qualification.profile_a.v1") throw new ArgumentException("living.profile.invalid");
            Id = id; MaximumActive = active; MaximumRetained = retained;
            MaximumCheckpointBytes = checkpointBytes; MaximumPartyReplay = partyReplay;
            MaximumTotalReplay = totalReplay; MaximumBaseSaveBytes = baseSaveBytes; MaximumOwnerBytes = ownerBytes;
            QualificationTiming = qualificationTiming; QualificationPostureId = qualificationPostureId;
            if (baseBudget.IsValid && baseBudget.MaximumInputBytes != baseSaveBytes) throw new ArgumentException("living.profile.invalid");
            BaseBudget = baseBudget;
            MaximumCombatWork = maximumCombatWork;
        }
        internal RunCheckpointLimits Checkpoints => new RunCheckpointLimits(MaximumCheckpointBytes, MaximumPartyReplay, MaximumCombatWork);
        public static LivingDungeonProfile ParseQualificationProfile(byte[] bytes, SpatialSerializedInputLimits limits,
            RunLogicalTiming qualificationTiming = null, string qualificationPostureId = null,
            SpatialSerializedInputLimits baseBudget = default)
        {
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(bytes, limits, issues, out var root, true) || !ContractJson.ValidateShape(root,
                new[] { "FormatVersion", "ProfileId", "MaximumActive", "MaximumRetained", "MaximumCheckpointBytes",
                    "MaximumPartyReplay", "MaximumTotalReplay", "MaximumBaseSaveBytes", "MaximumOwnerBytes" }, issues) ||
                !ContractJson.Int(root.Fields[0].Value, out var version) || version != 1 ||
                !ContractJson.String(root.Fields[1].Value, out var id)) throw new ArgumentException("living.profile.invalid");
            var v = new int[7];
            for (int i = 0; i < v.Length; i++)
                if (!ContractJson.Int(root.Fields[i + 2].Value, out v[i])) throw new ArgumentException("living.profile.invalid");
            return new LivingDungeonProfile(id, v[0], v[1], v[2], v[3], v[4], v[5], v[6], qualificationTiming, qualificationPostureId, baseBudget);
        }
    }

    // No Unity serialization of these authorities. Only the strict complete-save reader/writer owns them.
    public sealed class LivingDungeonState
    {
        public long Revision { get; internal set; }
        internal LivingDungeonScheduler Scheduler;
        internal List<LivingDungeonParty> Parties = new List<LivingDungeonParty>();
        public bool IsDormant => Scheduler == null;
        public bool IsClosed => Scheduler == null || Scheduler.AcceptedThrough == Scheduler.ClosedThrough;
        public int RetainedCount => Parties.Count;
        internal static LivingDungeonState Empty() => new LivingDungeonState();
    }
    internal sealed class LivingDungeonScheduler
    {
        internal string ProfileId;
        internal long MinimumOpportunitySpacingUnits, AcceptedThrough = -1, ClosedThrough = -1;
        internal long NextOpportunityOrdinal;
        internal long? NextOpportunityTime, LastOpportunityTime;
    }
    internal sealed class LivingDungeonParty
    {
        internal int Sequence;
        internal long AdmittedAt, NextOrdinal;
        internal string StartIdentity, CheckpointIdentity;
        internal byte[] Checkpoint;
    }

    internal static class LivingDungeonContracts
    {
        internal const string OwnerName = "livingDungeon";
        internal const int Format = 1;
        // Canonical format constants, not simulation or capacity tuning.
        internal const int BinaryChunkBytes = 3072;
        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
        internal static void Write(ContractJsonWriter w, LivingDungeonState state)
        {
            w.Node(); w.Token("{\"FormatVersion\":1,\"Revision\":"); w.Token(Number(state.Revision));
            w.Token(",\"Scheduler\":");
            var s = state.Scheduler;
            if (s == null) w.Token("null");
            else
            {
                w.Node(); w.Token("{\"ProfileId\":"); w.String(s.ProfileId);
                w.Token(",\"MinimumOpportunitySpacingUnits\":"); w.Token(Number(s.MinimumOpportunitySpacingUnits));
                w.Token(",\"AcceptedThrough\":"); w.Token(Number(s.AcceptedThrough));
                w.Token(",\"ClosedThrough\":"); w.Token(Number(s.ClosedThrough));
                w.Token(",\"NextOpportunityOrdinal\":"); w.Token(Number(s.NextOpportunityOrdinal));
                w.Token(",\"NextOpportunityTime\":"); Nullable(w, s.NextOpportunityTime);
                w.Token(",\"LastOpportunityTime\":"); Nullable(w, s.LastOpportunityTime); w.Token("}");
            }
            w.Token(",\"Parties\":["); w.Node();
            bool first = true;
            foreach (var p in state.Parties)
            {
                if (!first) w.Token(","); first = false; w.Node();
                w.Token("{\"Sequence\":"); w.Token(Number(p.Sequence));
                w.Token(",\"AdmittedAt\":"); w.Token(Number(p.AdmittedAt));
                w.Token(",\"StartIdentity\":"); w.String(p.StartIdentity);
                w.Token(",\"CheckpointIdentity\":"); w.String(p.CheckpointIdentity);
                w.Token(",\"NextOrdinal\":"); w.Token(Number(p.NextOrdinal));
                w.Token(",\"CheckpointChunks\":["); w.Node();
                for (int offset = 0; offset < p.Checkpoint.Length; offset += BinaryChunkBytes)
                {
                    if (offset != 0) w.Token(","); w.Node();
                    w.String(Convert.ToBase64String(p.Checkpoint, offset, Math.Min(BinaryChunkBytes, p.Checkpoint.Length - offset)));
                }
                w.Token("]}");
            }
            w.Token("]}");
        }
        private static void Nullable(ContractJsonWriter w, long? value) => w.Token(value.HasValue ? Number(value.Value) : "null");
        private static bool Shape(ContractJsonNode n, params string[] keys) => n?.Kind == ContractJsonKind.Object &&
            n.Fields.Select(f => f.Key).SequenceEqual(keys);
        private static long Integer(ContractJsonNode n)
        {
            if (n.Kind != ContractJsonKind.Number || !long.TryParse(n.Text, NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out long v) || n.Text != Number(v)) throw new FormatException();
            return v;
        }
        private static long? Optional(ContractJsonNode n) => n.Kind == ContractJsonKind.Null ? (long?)null : Integer(n);
        private static string Text(ContractJsonNode n)
        { if (n.Kind != ContractJsonKind.String) throw new FormatException(); return n.Text; }

        internal static bool TryRead(ContractJsonNode n, IReadOnlyList<LivingDungeonProfile> profiles,
            out LivingDungeonState result)
        {
            result = null;
            try
            {
                if (!Shape(n, "FormatVersion", "Revision", "Scheduler", "Parties") || Integer(n.Fields[0].Value) != Format)
                    return false;
                var state = new LivingDungeonState { Revision = Integer(n.Fields[1].Value) };
                if (state.Revision < 0 || n.Fields[3].Value.Kind != ContractJsonKind.Array) return false;
                var scheduler = n.Fields[2].Value;
                if (scheduler.Kind != ContractJsonKind.Null)
                {
                    if (!Shape(scheduler, "ProfileId", "MinimumOpportunitySpacingUnits", "AcceptedThrough", "ClosedThrough",
                        "NextOpportunityOrdinal", "NextOpportunityTime", "LastOpportunityTime")) return false;
                    state.Scheduler = new LivingDungeonScheduler {
                        ProfileId = Text(scheduler.Fields[0].Value), MinimumOpportunitySpacingUnits = Integer(scheduler.Fields[1].Value),
                        AcceptedThrough = Integer(scheduler.Fields[2].Value), ClosedThrough = Integer(scheduler.Fields[3].Value),
                        NextOpportunityOrdinal = Integer(scheduler.Fields[4].Value), NextOpportunityTime = Optional(scheduler.Fields[5].Value),
                        LastOpportunityTime = Optional(scheduler.Fields[6].Value) };
                }
                var profile = Profile(state, profiles);
                if (state.Scheduler == null)
                {
                    if (state.Revision != 0 || n.Fields[3].Value.Items.Count != 0) return false;
                    result = state; return true;
                }
                if (profile == null || n.Fields[3].Value.Items.Count > profile.MaximumRetained) return false;
                foreach (var item in n.Fields[3].Value.Items)
                {
                    if (!Shape(item, "Sequence", "AdmittedAt", "StartIdentity", "CheckpointIdentity", "NextOrdinal", "CheckpointChunks")) return false;
                    var p = new LivingDungeonParty {
                        Sequence = checked((int)Integer(item.Fields[0].Value)), AdmittedAt = Integer(item.Fields[1].Value),
                        StartIdentity = Text(item.Fields[2].Value), CheckpointIdentity = Text(item.Fields[3].Value),
                        NextOrdinal = Integer(item.Fields[4].Value) };
                    var chunks = item.Fields[5].Value;
                    if (chunks.Kind != ContractJsonKind.Array || chunks.Items.Count == 0 ||
                        chunks.Items.Count > (profile.MaximumCheckpointBytes - 1) / BinaryChunkBytes + 1) return false;
                    using (var stream = new MemoryStream())
                    {
                        for (int i = 0; i < chunks.Items.Count; i++)
                        {
                            string encoded = Text(chunks.Items[i]);
                            if (encoded.Length == 0 || encoded.Length > 4096) return false;
                            byte[] bytes = Convert.FromBase64String(encoded);
                            if (bytes.Length == 0 || bytes.Length > BinaryChunkBytes ||
                                (i < chunks.Items.Count - 1 && bytes.Length != BinaryChunkBytes) ||
                                Convert.ToBase64String(bytes) != encoded || stream.Length + bytes.Length > profile.MaximumCheckpointBytes) return false;
                            stream.Write(bytes, 0, bytes.Length);
                        }
                        p.Checkpoint = stream.ToArray();
                    }
                    state.Parties.Add(p);
                }
                if (!Validate(state, profiles)) return false;
                result = state; return true;
            }
            catch { return false; }
        }
        internal static LivingDungeonProfile Profile(LivingDungeonState state, IReadOnlyList<LivingDungeonProfile> profiles) =>
            profiles?.SingleOrDefault(p => p.Id == state.Scheduler?.ProfileId);
        internal static RunExecution Restore(LivingDungeonParty p, LivingDungeonProfile profile, bool retainPresentation = false)
        {
            using var measurement = ForegroundQualificationMetrics.Measure(ForegroundQualificationMetrics.Area.Restore);
            var restored = CombatCheckpointCodec.RestoreSupported(p.Checkpoint, p.CheckpointIdentity, p.StartIdentity, p.NextOrdinal, profile.Checkpoints, retainPresentation);
            if (!restored.IsValid) throw new InvalidOperationException(restored.FailureReason);
            if (profile.Id == LivingDungeonForegroundConfig.ProfileId &&
                (restored.Execution.TransferContract || restored.Execution.Combat?.Kernel.RulesId != MonsterRecoveryRules.Executable ||
                 restored.Execution.Snapshot?.Floors.Count != 1 || restored.Execution.Snapshot.Floors[0].FloorIndex != 0 ||
                 restored.Execution.Snapshot.PostureId != RunPostureResolver.BalancedId ||
                 !restored.Execution.TimingMatches(profile.QualificationTiming)))
            { restored.Execution.Abandon(); throw new InvalidOperationException("living.foreground.provenance_required"); }
            if (profile.Id == LivingDungeonForegroundConfig.TwoFloorProfileId &&
                (!restored.Execution.TransferContract || restored.Execution.Combat?.Kernel.RulesId != MonsterRecoveryRules.Executable ||
                 restored.Execution.Snapshot.Floors.Count < 1 || restored.Execution.Snapshot.Floors.Count > 2 ||
                 restored.Execution.Snapshot.Floors.Where((f, i) => f.FloorIndex != i).Any() ||
                 restored.Execution.Snapshot.PostureId != RunPostureResolver.BalancedId ||
                 !restored.Execution.TimingMatches(profile.QualificationTiming)))
            { restored.Execution.Abandon(); throw new InvalidOperationException("living.foreground.provenance_required"); }
            return restored.Execution;
        }
        internal static bool Validate(LivingDungeonState state, IReadOnlyList<LivingDungeonProfile> profiles)
        { try { return ValidateCore(state, profiles); } catch { return false; } }
        private static bool ValidateCore(LivingDungeonState state, IReadOnlyList<LivingDungeonProfile> profiles)
        {
            if (state == null || state.Revision < 0) return false;
            if (state.Scheduler == null) return state.Revision == 0 && state.Parties.Count == 0;
            var s = state.Scheduler; var profile = Profile(state, profiles);
            if (profile == null || s.MinimumOpportunitySpacingUnits <= 0 || s.ClosedThrough < -1 ||
                s.AcceptedThrough < s.ClosedThrough || s.NextOpportunityOrdinal < 0 || state.Parties.Count > profile.MaximumRetained ||
                (s.NextOpportunityTime.HasValue && (s.NextOpportunityTime < 0 || s.NextOpportunityTime <= s.ClosedThrough)) ||
                (s.LastOpportunityTime.HasValue && (s.LastOpportunityTime < 0 || s.LastOpportunityTime > s.AcceptedThrough)) ||
                (s.NextOpportunityOrdinal == 0) != !s.LastOpportunityTime.HasValue ||
                (s.NextOpportunityTime.HasValue && s.LastOpportunityTime.HasValue &&
                    s.NextOpportunityTime.Value < checked(s.LastOpportunityTime.Value + s.MinimumOpportunitySpacingUnits))) return false;
            int sequence = 0, active = 0; long total = 0;
            foreach (var p in state.Parties)
            {
                if (p.Sequence <= sequence || p.AdmittedAt < 0 || p.AdmittedAt > s.AcceptedThrough || p.NextOrdinal < 0 ||
                    !SpatialContractSha256.IsCanonical(p.StartIdentity) || !SpatialContractSha256.IsCanonical(p.CheckpointIdentity)) return false;
                sequence = p.Sequence; total = checked(total + p.NextOrdinal);
                if (total > profile.MaximumTotalReplay) return false;
            }
            // Reject aggregate work before any reconstruction.
            var occupancy = new Dictionary<string, int>(StringComparer.Ordinal);
            var waiters = new List<(string destination, int blocker)>();
            foreach (var p in state.Parties)
            {
                var e = Restore(p, profile);
                try
                {
                    var view = e.Inspect();
                    if (view.RunId != "run-" + Number(p.Sequence) || !view.HasTiming || e.OutcomeTaken ||
                        checked(p.AdmittedAt + view.LogicalTime) > s.AcceptedThrough) return false;
                    if (e.Combat != null && e.AcceptedDeadline >= 0 &&
                        checked(p.AdmittedAt + e.AcceptedDeadline) > s.AcceptedThrough) return false;
                    var due = e.NextDueTime();
                    if (due.HasValue && checked(p.AdmittedAt + due.Value) <= s.ClosedThrough) return false;
                    if (e.TransferContract) {
                        // Format-3 alone supports Released for exact prefix replay;
                        // complete-save publication requires its transfer in the same boundary.
                        if (e.TransferReleased) return false;
                        if (occupancy.ContainsKey(e.OccupiedFloor)) return false;
                        occupancy.Add(e.OccupiedFloor, p.Sequence);
                        if (e.TransferWaiting) {
                            if (e.TransferGate.BlockerSequence == p.Sequence) return false;
                            waiters.Add((e.TransferGate.Destination, e.TransferGate.BlockerSequence));
                        }
                    }
                    if (!e.IsTerminal) active++;
                }
                finally { e.Abandon(); }
            }
            return active <= profile.MaximumActive && (!state.IsClosed || waiters.All(w =>
                occupancy.TryGetValue(w.destination, out int blocker) && blocker == w.blocker));
        }
    }
}
