using System;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    internal static class LivingDungeonLifetimeAssurance
    {
        internal static void Check(RunExecution probe, LivingDungeonProfile profile)
        {
            using var preparation = new CombatStartCapture.AdmissionPreparation();
            Check(probe, profile, false, preparation);
        }
        private static void Check(RunExecution probe, LivingDungeonProfile profile, bool blockedPath,
            CombatStartCapture.AdmissionPreparation preparation)
        {
            using var diagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.AdmissionAssurance,
                blockedPath ? ForegroundQualificationMetrics.Origin.LifetimeAlternative : ForegroundQualificationMetrics.Origin.LifetimeNormal);
            long? alternativeWork = null;
            while (!probe.IsTerminal) {
                if (probe.NextOrdinal >= profile.MaximumPartyReplay) throw new InvalidOperationException("run.checkpoint.replay_limit");
                var due = probe.NextDueTime();
                if (!due.HasValue) throw new InvalidOperationException("living.transfer.invalid_probe");
                if (!blockedPath && probe.TransferContract && probe.AnnouncedAction == RunStepKind.FloorTransfer) {
                    using var branchDiagnostic = ForegroundQualificationMetrics.Trace(ForegroundQualificationMetrics.Boundary.LifetimeBranch,
                        ForegroundQualificationMetrics.Origin.LifetimeAlternative);
                    var cp = probe.ExportCheckpoint(profile.Checkpoints);
                    var alternative = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal, profile.Checkpoints, preparation: preparation);
                    if (!alternative.IsValid) throw new InvalidOperationException(alternative.FailureReason);
                    try {
                        var e = alternative.Execution;
                        // Private hypothetical occupancy. No canonical blocker, history,
                        // wallet or vacancy capability is created by this size/work probe.
                        e.BlockTransfer(due.Value, int.MaxValue); Verify(e, profile, preparation);
                        long release = checked(due.Value + 1);
                        e.ReleaseTransfer(release, int.MaxValue, profile.MaximumPartyReplay - 1, release); Verify(e, profile, preparation);
                        Check(e, profile, true, preparation);
                        alternativeWork = e.CommittedCombatWork;
                    } finally { alternative.Execution.Abandon(); }
                }
                probe.AdvanceThrough(due.Value, 1, probe.NextOrdinal); Verify(probe, profile, preparation);
            }
            if (alternativeWork.HasValue && alternativeWork.Value != probe.CommittedCombatWork)
                throw new InvalidOperationException("living.transfer.invalid_probe_work");
        }
        private static void Verify(RunExecution e, LivingDungeonProfile profile, CombatStartCapture.AdmissionPreparation preparation)
        {
            // Fixed-width control integers and captured floor IDs make the released
            // gate's encoded size independent of wait duration or blocker identity.
            var cp = e.ExportCheckpoint(profile.Checkpoints);
            if (!e.TransferContract) return; // retain the historical admission-probe workload
            var restored = CombatCheckpointCodec.RestoreSupported(cp.GetBytes(), cp.Identity, cp.StartIdentity, cp.NextOrdinal, profile.Checkpoints, preparation: preparation);
            if (!restored.IsValid) throw new InvalidOperationException(restored.FailureReason);
            restored.Execution.Abandon();
        }
    }
}
