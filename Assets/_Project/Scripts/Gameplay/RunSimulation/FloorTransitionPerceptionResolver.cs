using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    // Reads only the immutable pre-run knowledge and applicability fingerprint.
    public static class FloorTransitionPerceptionResolver
    {
        public static FloorTransitionPerception Resolve(ActiveFloorRunSnapshot snapshot, int nextFloorOrdinal,
            RunParty party)
        {
            if (snapshot == null || party == null) throw new ArgumentNullException(
                snapshot == null ? nameof(snapshot) : nameof(party));
            PhaseSixRunConfig config = snapshot.Configuration.PhaseSix;
            if (nextFloorOrdinal < 0 || nextFloorOrdinal >= snapshot.Floors.Count)
                return FloorTransitionPerception.Unknown(config);
            RunnableFloorSnapshot floor = snapshot.Floors[nextFloorOrdinal];
            FloorKnowledgeRecord record = snapshot.FloorKnowledge?.Records?.SingleOrDefault(item =>
                item != null && string.Equals(item.FloorInstanceId, floor.FloorInstanceId, StringComparison.Ordinal));
            return Resolve(record, floor.KnowledgeFingerprint, party.IntelligenceInterpretationFactor, config);
        }

        public static FloorTransitionPerception Resolve(FloorKnowledgeRecord record, string fingerprint,
            double intelligenceInterpretationFactor, PhaseSixRunConfig config)
        {
            if (!PhaseSixRunConfigValidation.IsValid(config) ||
                !PhaseSixRunConfigValidation.Finite(intelligenceInterpretationFactor) ||
                intelligenceInterpretationFactor < 0d)
                throw new ArgumentException(PhaseSixRunConfigValidation.Invalid);
            if (record == null || !string.Equals(record.ApplicabilityFingerprint, fingerprint, StringComparison.Ordinal) ||
                !SpatialContractSha256.IsCanonical(fingerprint) ||
                !record.ConfidenceKnown || !PhaseSixRunConfigValidation.Unit(record.Confidence) ||
                record.Confidence <= 0d ||
                !PhaseSixRunConfigValidation.Finite(record.PerceivedRewardScore) || record.PerceivedRewardScore < 0d ||
                !PhaseSixRunConfigValidation.Finite(record.PerceivedDangerScore) || record.PerceivedDangerScore < 0d ||
                !record.RewardKnown && !record.DangerKnown)
                return FloorTransitionPerception.Unknown(config);
            double effective = Math.Max(0d, Math.Min(1d, record.Confidence * intelligenceInterpretationFactor));
            double rewardUncertainty = record.RewardKnown ? 1d - effective : 1d;
            double dangerUncertainty = record.DangerKnown ? 1d - effective : 1d;
            return new FloorTransitionPerception(record.RewardKnown,
                record.RewardKnown ? record.PerceivedRewardScore : 0d,
                record.DangerKnown, record.DangerKnown ? record.PerceivedDangerScore : 0d,
                (rewardUncertainty + dangerUncertainty) / 2d);
        }
    }
}
