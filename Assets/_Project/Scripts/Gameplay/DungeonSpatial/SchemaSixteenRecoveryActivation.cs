using System;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    // The sole 16 -> 17 activation. Preparation is detached; its caller must use
    // the actual original predecessor in ExactCompleteSaveAtomicPersistence.
    internal static class SchemaSixteenRecoveryActivation
    {
        internal static string Prepare(byte[] exactSource, byte[] frozenSixteen,
            DetachedCurrentTargetValidationContext context, RecoveryEntryObservation entry,
            UnixTimePoint through, long eligibleAfterEntry100ns, out byte[] candidate)
        {
            candidate = null;
            if (exactSource == null || context?.RecoveryContext == null || entry == null || through == null ||
                entry.Context != RecoveryEntryContext.UnknownPostCrash || through.CompareTo(entry.WallEndpoint) < 0 ||
                eligibleAfterEntry100ns < 0 || eligibleAfterEntry100ns > through.Elapsed100nsSince(entry.WallEndpoint))
                return RecoveryEpisode.Invalid;
            var prior = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(frozenSixteen, context);
            if (!prior.IsValid || context.RecoveryInputs == null ||
                !DetachedCompleteSaveContract.ResearchStateValid(prior, context.RecoveryInputs)) return RecoveryEpisode.Invalid;
            if (!prior.LivingDungeon.IsClosed) {
                if (context.HistoricalRecoveryEvidence == null ||
                    !context.HistoricalRecoveryEvidence.TryGetProvenInputs((byte[])exactSource.Clone(), out var producer) ||
                    !context.RecoveryContext.SupportsHistorical(producer, context.Limits.Serialized))
                    return RecoveryEpisode.HistoricalProofRequired;
            }
            try {
                var source = prior.TimeProvenance.AccountedBoundary;
                var originalEnd = new UnixTimePoint(entry.WallEndpoint.Seconds, 0);
                var episode = new RecoveryEpisode(SpatialContractSha256.Compute(exactSource), source, entry.WallEndpoint,
                    RecoveryEpisode.UnknownPostCrash, RecoveryEpisode.Legacy, RecoveryEpisode.OriginalPending,
                    originalEnd.Elapsed100nsSince(source), checked(entry.WallEndpoint.Fraction100ns + eligibleAfterEntry100ns));
                var time = new TimeProvenance(through, context.RecoveryContext.Digest, episode);
                var issues = new SpatialIssueCollector(context.Limits.Serialized.MaximumDiagnostics);
                if (!ContractJson.TryParse(frozenSixteen, context.Limits.ForSchema(16).Serialized, issues, out var root))
                    return RecoveryEpisode.Invalid;
                var writer = new ContractJsonWriter(context.Limits.Serialized); writer.Node(); writer.Token("{");
                for (int i = 0; i < root.Fields.Count; i++) {
                    if (i != 0) writer.Token(","); var field = root.Fields[i]; writer.String(field.Key); writer.Token(":");
                    if (field.Key == "schemaVersion") writer.Token("17");
                    else if (field.Key == "primary") {
                        writer.Node(); writer.Token("{");
                        for (int j = 0; j < field.Value.Fields.Count; j++) {
                            if (j != 0) writer.Token(","); var member = field.Value.Fields[j]; writer.String(member.Key); writer.Token(":");
                            if (member.Key == TimeProvenance.Owner) time.Write(writer);
                            else if (member.Key == "lastSavedUtcUnix") writer.Token(through.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            else DetachedCompleteSaveContract.WriteCanonicalNode(writer, member.Value);
                        }
                        writer.Token("}");
                    } else DetachedCompleteSaveContract.WriteCanonicalNode(writer, field.Value);
                }
                writer.Token("}"); candidate = writer.Finish(); return null;
            } catch { return RecoveryEpisode.Invalid; }
        }
    }
}
