using System;
using System.Globalization;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // One immutable obligation, not a second wallet or advancing time authority.
    internal sealed class RecoveryEpisode
    {
        internal const string Invalid = "time.recovery.invalid_state";
        internal const string ContextMismatch = "time.recovery.context_mismatch";
        internal const string HistoricalProofRequired = "time.recovery.historical_proof_required";
        internal const string OriginalPending = "originalPending", ResidualPending = "residualPending";
        internal const string KnownBackground = "knownBackground", UnknownPostCrash = "unknownPostCrash";
        internal const string Precise = "precise", Legacy = "legacyWholeSeconds";
        internal string EntryPredecessorSha256 { get; }
        internal UnixTimePoint Source { get; }
        internal UnixTimePoint Entry { get; }
        internal string EntryContext { get; }
        internal string OriginalMode { get; }
        internal string Phase { get; }
        internal long OriginalEligible100ns { get; }
        internal long ResidualEligible100ns { get; }
        internal UnixTimePoint OriginalEndpoint => OriginalMode == Legacy ? new UnixTimePoint(Entry.Seconds, 0) : Entry;

        internal RecoveryEpisode(string predecessor, UnixTimePoint source, UnixTimePoint entry,
            string context, string mode, string phase, long original, long residual)
        {
            EntryPredecessorSha256 = predecessor; Source = source; Entry = entry;
            EntryContext = context; OriginalMode = mode; Phase = phase;
            OriginalEligible100ns = original; ResidualEligible100ns = residual;
        }

        internal bool IsValidThrough(UnixTimePoint through)
        {
            try {
                if (!SpatialContractSha256.IsCanonical(EntryPredecessorSha256) || Source == null || Entry == null || through == null ||
                    Source.Seconds <= 0 || EntryContext != KnownBackground && EntryContext != UnknownPostCrash ||
                    OriginalMode != Precise && OriginalMode != Legacy || Phase != OriginalPending && Phase != ResidualPending ||
                    OriginalEligible100ns < 0 || ResidualEligible100ns < 0 ||
                    Source.CompareTo(OriginalEndpoint) > 0 || Entry.CompareTo(through) > 0 ||
                    OriginalEligible100ns > OriginalEndpoint.Elapsed100nsSince(Source) ||
                    ResidualEligible100ns > through.Elapsed100nsSince(OriginalEndpoint)) return false;
                return OriginalMode != Legacy || Source.Fraction100ns == 0 && EntryContext == UnknownPostCrash &&
                    OriginalEligible100ns == OriginalEndpoint.Elapsed100nsSince(Source);
            } catch { return false; }
        }

        internal RecoveryEpisode Advance(long eligible100ns, bool settleOriginal = false)
        {
            if (eligible100ns < 0 || settleOriginal && Phase != OriginalPending) throw new ArgumentException(Invalid);
            return new RecoveryEpisode(EntryPredecessorSha256, Source, Entry, EntryContext, OriginalMode,
                settleOriginal ? ResidualPending : Phase, OriginalEligible100ns,
                checked(ResidualEligible100ns + eligible100ns));
        }

        internal static bool TryInteger(ContractJsonNode node, out long value)
        {
            value = 0;
            return node?.Kind == ContractJsonKind.Number && long.TryParse(node.Text, NumberStyles.None,
                CultureInfo.InvariantCulture, out value) && value >= 0 && node.Text == value.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool TryRead(ContractJsonNode node, out RecoveryEpisode value)
        {
            value = null;
            try {
                var issues = new SpatialIssueCollector(8);
                if (!ContractJson.ValidateShape(node, new[] { "EntryPredecessorSha256", "SourceSeconds", "SourceFraction100ns", "EntrySeconds",
                    "EntryFraction100ns", "EntryContext", "OriginalMode", "Phase", "OriginalEligible100ns", "ResidualEligible100ns" }, issues)) return false;
                if (!ContractJson.String(node.Fields[0].Value, out string predecessor) ||
                    !TimeProvenance.TrySeconds(node.Fields[1].Value, out long source) ||
                    !TryInteger(node.Fields[2].Value, out long sf) || sf > 9999999 ||
                    !TimeProvenance.TrySeconds(node.Fields[3].Value, out long entry) ||
                    !TryInteger(node.Fields[4].Value, out long ef) || ef > 9999999 ||
                    !ContractJson.String(node.Fields[5].Value, out string context) || !ContractJson.String(node.Fields[6].Value, out string mode) ||
                    !ContractJson.String(node.Fields[7].Value, out string phase) ||
                    !TryInteger(node.Fields[8].Value, out long original) || !TryInteger(node.Fields[9].Value, out long residual)) return false;
                value = new RecoveryEpisode(predecessor, new UnixTimePoint(source, (int)sf), new UnixTimePoint(entry, (int)ef), context, mode, phase, original, residual);
                return true; // The enclosing W supplies the required interval/phase validation.
            } catch { return false; }
        }

        internal void Write(ContractJsonWriter w)
        {
            w.Node(); w.Token("{\"EntryPredecessorSha256\":"); w.String(EntryPredecessorSha256);
            Number(w, "SourceSeconds", Source.Seconds); Number(w, "SourceFraction100ns", Source.Fraction100ns);
            Number(w, "EntrySeconds", Entry.Seconds); Number(w, "EntryFraction100ns", Entry.Fraction100ns);
            w.Token(",\"EntryContext\":"); w.String(EntryContext); w.Token(",\"OriginalMode\":"); w.String(OriginalMode);
            w.Token(",\"Phase\":"); w.String(Phase); Number(w, "OriginalEligible100ns", OriginalEligible100ns);
            Number(w, "ResidualEligible100ns", ResidualEligible100ns); w.Token("}");
        }
        private static void Number(ContractJsonWriter w, string key, long value)
        { w.Token(","); w.String(key); w.Token(":"); w.Token(value.ToString(CultureInfo.InvariantCulture)); }
    }
}
