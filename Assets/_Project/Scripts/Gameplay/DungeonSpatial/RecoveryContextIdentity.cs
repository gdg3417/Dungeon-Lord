using System;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // Derived immutable identity of the twelve inputs already validated by their
    // owning loaders. This object grants no save, migration or settlement authority.
    internal sealed class RecoveryContextIdentity
    {
        internal const int ComponentCount = 12;
        internal const string Domain = "DL-H1F4-context-v3\n";
        private readonly byte[][] inputs;
        internal string Digest { get; }
        internal RecoveryContextIdentity(params byte[][] validatedInputs)
        {
            if (validatedInputs == null || validatedInputs.Length != ComponentCount ||
                validatedInputs.Any(b => b == null || b.Length == 0)) throw new ArgumentException(RecoveryEpisode.ContextMismatch);
            inputs = validatedInputs.Select(b => (byte[])b.Clone()).ToArray();
            var prefix = Encoding.ASCII.GetBytes(Domain);
            var framed = new byte[prefix.Length + ComponentCount * 32];
            Buffer.BlockCopy(prefix, 0, framed, 0, prefix.Length);
            for (int i = 0; i < inputs.Length; i++)
            {
                using var hash = System.Security.Cryptography.SHA256.Create();
                byte[] digest = hash.ComputeHash(inputs[i]);
                Buffer.BlockCopy(digest, 0, framed, prefix.Length + i * 32, 32);
            }
            Digest = SpatialContractSha256.Compute(framed);
        }
        internal byte[] GetInput(int index) => (byte[])inputs[index].Clone();
        internal bool Matches(TimeProvenance time) => time?.Mode == TimeProvenance.PreciseMode && time.RecoveryContextSha256 == Digest;

        // Schema activation may add only successor registration records to the
        // compatibility table. No numerical rules or existing records are replaced.
        internal bool SupportsHistorical(RecoveryContextIdentity original, SpatialSerializedInputLimits limits)
        {
            if (original == null) return false;
            for (int i = 0; i < ComponentCount; i++)
                if (i != 9 && !inputs[i].AsSpan().SequenceEqual(original.inputs[i])) return false;
            if (inputs[9].AsSpan().SequenceEqual(original.inputs[9])) return true;
            var issues = new SpatialIssueCollector(limits.MaximumDiagnostics);
            if (!ContractJson.TryParse(original.inputs[9], limits, issues, out var before, true) ||
                !ContractJson.TryParse(inputs[9], limits, issues, out var after, true) || before.Kind != ContractJsonKind.Object ||
                after.Kind != ContractJsonKind.Object || before.Fields.Count != after.Fields.Count) return false;
            for (int i = 0; i < before.Fields.Count; i++)
            {
                var oldField = before.Fields[i]; var newField = after.Fields[i];
                if (oldField.Key != newField.Key) return false;
                if (oldField.Value.Kind == ContractJsonKind.Array && newField.Value.Kind == ContractJsonKind.Array)
                {
                    var retained = newField.Value.Items.Where(n => !SuccessorRegistration(n)).ToArray();
                    if (retained.Length != oldField.Value.Items.Count) return false;
                    for (int j = 0; j < retained.Length; j++) if (!SameNode(oldField.Value.Items[j], retained[j], limits)) return false;
                }
                else if (!SameNode(oldField.Value, newField.Value, limits)) return false;
            }
            return true;
        }
        private static bool SuccessorRegistration(ContractJsonNode node) => node.Kind == ContractJsonKind.Object &&
            node.Fields.Any(f => f.Key == "TargetSchemaVersion" && f.Value.Kind == ContractJsonKind.Number && f.Value.Text == "17");
        private static bool SameNode(ContractJsonNode a, ContractJsonNode b, SpatialSerializedInputLimits limits)
        {
            var wa = new ContractJsonWriter(limits); var wb = new ContractJsonWriter(limits);
            DetachedCompleteSaveContract.WriteCanonicalNode(wa, a); DetachedCompleteSaveContract.WriteCanonicalNode(wb, b);
            return wa.Finish().AsSpan().SequenceEqual(wb.Finish());
        }
    }

    // Supplied only by an independently trusted producer/evidence integration.
    // Save labels, a user-supplied digest, or hashing current defaults cannot
    // implement this authority. No default production evidence is fabricated.
    internal interface IHistoricalRecoveryEvidence
    {
        bool TryGetProvenInputs(byte[] exactSourceSave, out RecoveryContextIdentity originalInputs);
    }
}
