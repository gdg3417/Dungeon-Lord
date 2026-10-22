#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using UnityEngine;

namespace DungeonBuilder.M0
{
    [Serializable] internal sealed class TwoFloorSeedManifest
    {
        public string Product, SaveFileName, SeedPath, SeedSha256, HistoricalStartIdentity;
        public long HistoricalOrdinal;
        public int DescendingSamples;
        public long LongestFloorTwoDuration, InitialOpportunityInterval;
    }
    public partial class GameRoot
    {
        internal IEnumerator QualifyTwoFloor(string phase, ForegroundSmokeReport report, string previous)
        {
            if (!Application.productName.Contains("B3B2 Smoke Two Floor") ||
                !Path.GetFileName(SaveService.SavePath).StartsWith("living-dungeon-b3b2-qualification-two-floor-", StringComparison.Ordinal) ||
                SaveService.ForegroundConfiguration?.TwoFloor != true || foregroundFailure != null)
                throw new InvalidOperationException("two_floor.smoke.isolation_required");
            report.Product = Application.productName; report.SavePath = SaveService.SavePath;
            var prior = phase == "two-floor-wait" || phase == "v1-upgrade" ? null : JsonUtility.FromJson<ForegroundSmokeReport>(File.ReadAllText(previous));
            if (prior != null) {
                if (!prior.Passed || Save.livingDungeon.Scheduler.AcceptedThrough != prior.Accepted ||
                    Save.livingDungeon.Parties.Count != prior.PartySequences.Length)
                    throw new InvalidOperationException("two_floor.smoke.reopen_mismatch");
                for (int i = 0; i < prior.PartySequences.Length; i++)
                    if (Save.livingDungeon.Parties[i].Sequence != prior.PartySequences[i] ||
                        Save.livingDungeon.Parties[i].CheckpointIdentity != prior.PartyCheckpoints[i])
                        throw new InvalidOperationException("two_floor.smoke.reopen_mismatch");
                report.BlockerSequence = prior.BlockerSequence; report.WaitingSequence = prior.WaitingSequence;
                report.AStartIdentity = prior.AStartIdentity; report.BStartIdentity = prior.BStartIdentity;
                report.BlockerAdmittedAt = prior.BlockerAdmittedAt; report.WaiterAdmittedAt = prior.WaiterAdmittedAt;
                report.BlockedAt = prior.BlockedAt; report.BlockerTerminalAt = prior.BlockerTerminalAt; report.BlockerTerminalOrdinal = prior.BlockerTerminalOrdinal;
            }
            if (phase == "v1-upgrade") {
                var seed = JsonUtility.FromJson<TwoFloorSeedManifest>(File.ReadAllText(previous));
                var party = Save.livingDungeon.Parties.Single();
                if (Save.livingDungeon.Scheduler.ProfileId != LivingDungeonForegroundConfig.ProfileId ||
                    party.StartIdentity != seed.HistoricalStartIdentity || party.NextOrdinal != seed.HistoricalOrdinal)
                    throw new InvalidOperationException("two_floor.smoke.historical_prefix_changed");
                report.AStartIdentity = party.StartIdentity;
                var old = LivingDungeonContracts.Restore(party, LivingDungeonForegroundConfig.Load().CreateProfile(SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized));
                try {
                    if (old.TransferContract || old.Snapshot.Floors.Count != 1 || old.TimingMatches(SaveService.ForegroundConfiguration.Timing))
                        throw new InvalidOperationException("two_floor.smoke.historical_contract_changed");
                } finally { old.Abandon(); }
            }
            double sample = 1000; // same opt-in injected monotonic source as the B3B2 qualification
            TimeService = new TimeService(Logger, _configuredSimulationTickSeconds,
                Content.Bootstrap.timeRules.detectClockSkewSeconds, new SystemTimeSource(), () => sample);
            TimeService.AttachSave(Save); TimeService.SetForegroundFocus(true);
            TimeService.BeginForeground(SaveService.ForegroundConfiguration.UnitsPerSecond, Save.livingDungeon.Scheduler?.AcceptedThrough ?? -1);
            bool complete = false;
            for (int frame = 0; frame < 40000; frame++) {
                foregroundApplicationFocused = true; foregroundApplicationPaused = false;
                TimeService.SetForegroundFocus(true); TimeService.OnResume(); sample += .25;
                UpdateForeground();
                if (foregroundFailure != null || foregroundRetryPending) throw new InvalidOperationException(foregroundFailure ?? "two_floor.smoke.retry_pending");
                if (phase == "v1-upgrade") {
                    if (Save.livingDungeon.Scheduler.ProfileId == LivingDungeonForegroundConfig.TwoFloorProfileId) {
                        if (Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-1") != 1 ||
                            Save.runHistory.NextRunSequence != 2 || Save.livingDungeon.Parties.Count != 0)
                            throw new InvalidOperationException("two_floor.smoke.historical_settlement_conflict");
                        complete = true;
                    }
                } else {
                    var profile = SaveService.ForegroundConfiguration.CreateProfile(SaveSpatialMigrationLimits.Canonical.ForSchema(13).Serialized);
                    foreach (var party in Save.livingDungeon.Parties) {
                        var execution = LivingDungeonContracts.Restore(party, profile);
                        try {
                            if (phase == "two-floor-wait" && execution.TransferWaiting) {
                                var blocker = Save.livingDungeon.Parties.Single(p => p.Sequence == execution.TransferGate.BlockerSequence);
                                var other = LivingDungeonContracts.Restore(blocker, profile);
                                try {
                                    if (execution.OccupiedFloor != Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId ||
                                        other.OccupiedFloor != execution.TransferGate.Destination || Save.livingDungeon.Parties.Count != 2)
                                        throw new InvalidOperationException("two_floor.smoke.occupancy_conflict");
                                    report.BlockerSequence = blocker.Sequence; report.WaitingSequence = party.Sequence;
                                    report.AStartIdentity = blocker.StartIdentity; report.BStartIdentity = party.StartIdentity;
                                    report.BlockerAdmittedAt = blocker.AdmittedAt; report.WaiterAdmittedAt = party.AdmittedAt;
                                    report.BlockedAt = checked(party.AdmittedAt + execution.TransferGate.Deadline);
                                    // Bounded reference prediction, using the same captured
                                    // executable. It never authorizes or publishes a vacancy.
                                    other.DrainToCompletion();
                                    report.BlockerTerminalAt = checked(blocker.AdmittedAt + other.Inspect().LogicalTime);
                                    report.BlockerTerminalOrdinal = other.Inspect().LastStep.Value.Ordinal;
                                    complete = true;
                                } finally { other.Abandon(); }
                            }
                            if (phase == "two-floor-release" && party.Sequence == report.WaitingSequence && execution.TransferGate?.ReleaseOrdinal >= 0) {
                                var cause = Save.runHistory.RecentOutcomes.Single(o => o.RunId == "run-" + report.BlockerSequence);
                                if (execution.StartIdentity != report.BStartIdentity || execution.TransferWaiting || execution.TransferReleased ||
                                    execution.OccupiedFloor != execution.TransferGate.Destination || execution.TransferGate.VacancySequence != report.BlockerSequence ||
                                    execution.TransferGate.VacancyOrdinal != report.BlockerTerminalOrdinal ||
                                    report.BlockerTerminalAt != checked(party.AdmittedAt + execution.TransferGate.ReleaseTime) ||
                                    report.BlockedAt != checked(party.AdmittedAt + execution.TransferGate.Deadline))
                                    throw new InvalidOperationException("two_floor.smoke.vacancy_causality_conflict");
                                report.ReleasedAt = checked(party.AdmittedAt + execution.TransferGate.ReleaseTime);
                                report.WaitingResumed = true; complete = true;
                            }
                        } finally { execution.Abandon(); }
                    }
                    if (phase == "two-floor-finish" && Save.runHistory.RecentOutcomes.Any(o => o.RunId == "run-" + report.WaitingSequence)) {
                        if (Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-" + report.WaitingSequence) != 1 ||
                            Save.runHistory.RecentOutcomes.Count(o => o.RunId == "run-" + report.BlockerSequence) != 1 ||
                            Save.livingDungeon.Parties.Any(p => p.Sequence == report.WaitingSequence || p.Sequence == report.BlockerSequence))
                            throw new InvalidOperationException("two_floor.smoke.duplicate_settlement");
                        complete = true;
                    }
                }
                if (complete) break;
                yield return null;
            }
            if (!complete) throw new InvalidOperationException("two_floor.smoke.no_matching_production_boundary");
            if (phase == "two-floor-wait") {
                var controller = UnityEngine.Object.FindFirstObjectByType<ProductionDungeonController>();
                if (controller == null) throw new InvalidOperationException("two_floor.smoke.presentation_missing");
                var original = SaveService.CanonicalSession.GetCurrentBytes();
                controller.SelectFloor(Save.validatedCanonicalSpatialState.Floors[1].FloorInstanceId); yield return null;
                if (!controller.World.ForegroundPartyVisible) throw new InvalidOperationException("two_floor.smoke.floor_two_marker");
                controller.SelectFloor(Save.validatedCanonicalSpatialState.Floors[0].FloorInstanceId); yield return null;
                if (!controller.World.ForegroundPartyVisible || !original.SequenceEqual(SaveService.CanonicalSession.GetCurrentBytes()))
                    throw new InvalidOperationException("two_floor.smoke.floor_switch_mutated_simulation");
                report.MarkerVisible = true;
                controller.EnterEdit(); yield return null;
                var floor = Save.validatedCanonicalSpatialState.Floors[0];
                var monster = floor.RoomContents.Assignments.First(a => a.CategoryId == MvpDungeonPlacementIds.MonsterCategoryId);
                var checkpoints = Save.livingDungeon.Parties.Select(p => p.CheckpointIdentity).ToArray();
                if (!controller.Draft.Move(floor.FloorInstanceId, monster.RoomInstanceId, monster.AssignmentId, new TileCoordinate(3, 2)))
                    throw new InvalidOperationException("two_floor.smoke.active_edit_failed");
                while (controller.Draft.Durability == DraftDurability.Pending) { if (!controller.Draft.FlushNext()) throw new InvalidOperationException("two_floor.smoke.draft_write_failed"); yield return null; }
                if (!controller.Commit() || !checkpoints.SequenceEqual(Save.livingDungeon.Parties.Select(p => p.CheckpointIdentity)))
                    throw new InvalidOperationException("two_floor.smoke.edit_rewrote_snapshot");
                report.ActiveEditCommitted = true;
            }
            ApplyPauseState(true);
            if (!Save.livingDungeon.IsClosed || foregroundFailure != null) throw new InvalidOperationException("two_floor.smoke.unclosed");
            report.PartySequences = Save.livingDungeon.Parties.Select(p => p.Sequence).ToArray();
            report.PartyCheckpoints = Save.livingDungeon.Parties.Select(p => p.CheckpointIdentity).ToArray();
            report.Accepted = Save.livingDungeon.Scheduler.AcceptedThrough; report.Closed = Save.livingDungeon.Scheduler.ClosedThrough;
            report.HistoryCount = Save.runHistory.RecentOutcomes.Length; report.NextRunSequence = Save.runHistory.NextRunSequence;
            report.Mana = Save.structureRuntime.ManaReserve; report.Heat = Save.structureRuntime.Heat;
            report.Passed = true;
        }
    }
}
#endif
