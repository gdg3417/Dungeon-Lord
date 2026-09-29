#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using DungeonBuilder.M0.Gameplay.RunSimulation;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class PhaseFiveBBranchIntegrationTests
    {
        internal static DetachedCanonicalWriteAuthorityTests.Fixture Fixture(bool content = true)
        {
            var f = DetachedCanonicalWriteAuthorityTests.Fixture.Create(null);
            f.Configuration = PhaseFiveBTestConfig.Production();
            f.Configuration.BaseSuccessChance = 1d; f.Configuration.SuccessThreshold = 0d;
            f.Configuration.HeatPenaltyPerPoint = 0d; f.Configuration.ManaReserveBonusPerPoint = 0d;
            // Mathematical forcing belongs only to fixtures, not production controls.
            f.Configuration.PhaseFiveB.BranchDecision.SkipThreshold = -1d;
            f.Configuration.PhaseFiveB.BranchDecision.EnterThreshold = -.9d;
            foreach (var p in f.Configuration.PhaseFiveB.BranchDecision.ProfileMinimums) p.MinimumSurvivability = 0d;
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(MvpDungeonPlacementIds.RoomCategoryId, MvpDungeonPlacementIds.BasicRoomOptionId)));
            f.Runtime.completedResearch = new CompletedResearchState { ProjectIds = new[] { BasicBranchingResearchAuthority.ResearchId } };
            var floor = f.State.Floors.Single(); var origin = floor.Layout.Nodes.Single(n => n.Kind == FloorRouteNodeKind.Room);
            var preview = OptionalBranchStructuralEditService.PreviewConstruction(f.State,
                new OptionalBranchConstructionRequest { FloorInstanceId = floor.FloorInstanceId, OriginNodeId = origin.NodeId,
                    OriginConnectionPointId = "east", CorridorLength = 2 }, f.Runtime.completedResearch, f.BranchingResearch,
                f.Production, f.Configuration, f.Profile.Canonical);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.ConstructBranch(preview)));
            if (content)
            {
                var edge = f.State.Floors.Single().Layout.Edges.Single(e => e.Classification == RouteClassification.Optional);
                var tiles = edge.Footprint.OccupiedTiles.OrderBy(t => t.X).ToArray();
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.TrapCategoryId,
                    MvpDungeonPlacementIds.SpikeTrapOptionId, edge.FloorId, edge.OptionalBranchId, tiles[0])));
                f.Accept(f.Execute(DetachedCanonicalMutationRequest.PlaceCorridor(MvpDungeonPlacementIds.LootNodeCategoryId,
                    MvpDungeonPlacementIds.HiddenCacheOptionId, edge.FloorId, edge.OptionalBranchId, tiles[1])));
            }
            return f;
        }

        internal static SaveService CanonicalSaveService(DetachedCanonicalWriteAuthorityTests.Fixture f)
        {
            var service = new SaveService(new SimpleLogger(false, (level, message) => { }), null, Path.GetDirectoryName(f.ActivePath));
            service.ConfigureCanonical(f.Profile, f.Production, f.Compatibility, f.Configuration,
                LegacyGameplayConfigurationContract.SerializeCanonical(f.Configuration));
            typeof(SaveService).GetProperty("SavePath").SetValue(service, f.ActivePath);
            foreach (var pair in new[] { new System.Collections.Generic.KeyValuePair<string, object>("_canonicalSession", f.Session),
                new System.Collections.Generic.KeyValuePair<string, object>("_canonicalFileSystem", f.FileSystem),
                new System.Collections.Generic.KeyValuePair<string, object>("_validationContext", f.Context) })
                typeof(SaveService).GetField(pair.Key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(service, pair.Value);
            return service;
        }

        internal static RunSimulationService Service(RunSimulationConfig c) => new RunSimulationService(c,
            JsonUtility.FromJson<LootConfig>(File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json")));
        internal static DetachedCanonicalWriteResult Run(DetachedCanonicalWriteAuthorityTests.Fixture f) =>
            f.Authority.CommitPhaseFiveBRun(f.ActivePath, f.FileSystem, f.Session, f.Runtime, Service(f.Configuration),
                RunPostureResolver.BalancedId, Math.Max(1, f.Runtime.lastSavedUtcUnix));

        [Test]
        public void ProductionLikeUnknownBranchEntersOnEarlySequentialRun()
        {
            var f = Fixture();
            var production = PhaseFiveBTestConfig.Production().PhaseFiveB.BranchDecision;
            var decision = f.Configuration.PhaseFiveB.BranchDecision;
            decision.SkipThreshold = production.SkipThreshold;
            decision.EnterThreshold = production.EnterThreshold;
            foreach (var minimum in decision.ProfileMinimums)
                minimum.MinimumSurvivability = production.ProfileMinimums.Single(p => p.ProfileId == minimum.ProfileId).MinimumSurvivability;

            f.Accept(Run(f));
            var first = f.Runtime.runHistory.LatestOutcome.BranchOutcomes.Single();
            Assert.That(first.Decision.RunId, Is.EqualTo("run-1"));
            Assert.That(first.Reason, Is.EqualTo("branch.outcome.skipped"));
            var topologyOnly = f.Runtime.sharedBranchKnowledge.Records.Single();
            Assert.That(topologyOnly.TopologyKnown, Is.True);
            Assert.That(topologyOnly.IncentiveKnown || topologyOnly.DangerKnown, Is.False);

            f.Accept(Run(f));
            var second = f.Runtime.runHistory.LatestOutcome.BranchOutcomes.Single();
            Assert.That(second.Decision.RunId, Is.EqualTo("run-2"));
            Assert.That(second.Decision.DecisionRoll, Is.EqualTo(877973485d / 4294967296d));
            Assert.That(second.Decision.Reason, Is.EqualTo("branch.decision.marginal_enter"));
            Assert.That(second.Traversed, Is.True);
        }

        [Test]
        public void EnterAppliesOneTrapOneLootReturnsAndLearnsDurablyWithoutNewSchema()
        {
            var f = Fixture(); string corridor = JsonUtility.ToJson(f.Runtime.corridorContent);
            string topology = JsonUtility.ToJson(f.State);
            var before = f.Runtime; var result = Run(f); f.Accept(result);
            var run = f.Runtime.runHistory.LatestOutcome; var branch = run.BranchOutcomes.Single();
            Assert.That(branch.Decision.Enter, Is.True); Assert.That(branch.Traversed && branch.Returned, Is.True);
            Assert.That(branch.ReachedAssignments, Has.Length.EqualTo(2)); Assert.That(branch.Encounters, Has.Length.EqualTo(1));
            Assert.That(run.EncounterEvents, Has.Length.EqualTo(1)); Assert.That(branch.GeneratedLootValue, Is.GreaterThan(0));
            var lootConfig = JsonUtility.FromJson<LootConfig>(File.ReadAllText("Assets/_Project/Data/Bootstrap/loot_config.json"));
            var fork = branch.Fork; var assignment = fork.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.LootNodeCategoryId);
            var loot = LootRollResolver.Resolve(lootConfig, f.Configuration.LootTableId,
                RunSimulationService.BranchLootSeed(run.RunId, fork.FloorInstanceId, fork.OptionalBranchId, assignment.AssignmentId));
            Assert.That(run.LootSummary.RollCount, Is.EqualTo(loot.rollCount * 2)); // one required-room roll plus one reached branch roll
            Assert.That(run.ReachedRoutePlacementEffects.ContributingOptionIds.Count(id => id == MvpDungeonPlacementIds.SpikeTrapOptionId), Is.EqualTo(1));
            Assert.That(run.ReachedRoutePlacementEffects.ContributingOptionIds.Count(id => id == MvpDungeonPlacementIds.HiddenCacheOptionId), Is.EqualTo(1));
            Assert.That(branch.Reason, Is.EqualTo("branch.outcome.completed"));
            var knowledge = f.Runtime.sharedBranchKnowledge.Records.Single();
            Assert.That(knowledge.TopologyKnown && knowledge.IncentiveKnown && knowledge.DangerKnown && knowledge.ConfidenceKnown, Is.True);
            Assert.That(knowledge.Confidence, Is.EqualTo(.75d)); Assert.That(knowledge.LastConfirmedRunId, Is.EqualTo(run.RunId));
            Assert.That(JsonUtility.ToJson(before.runHistory), Is.Not.EqualTo(JsonUtility.ToJson(f.Runtime.runHistory)));
            Assert.That(JsonUtility.ToJson(f.State), Is.EqualTo(topology)); Assert.That(JsonUtility.ToJson(f.Runtime.corridorContent), Is.EqualTo(corridor));
            Assert.That(SaveMigration.LatestSchemaVersion, Is.EqualTo(11)); Assert.That(CanonicalSaveSchemaVersions.CurrentWritableTarget, Is.EqualTo(11));
            string aggregate = JsonUtility.ToJson(run); double heat = f.Runtime.structureRuntime.Heat;
            Assert.That(aggregate, Does.Not.Contain("BranchOutcomes").And.Not.Contain("EncounterEvents").And.Not.Contain("CurrentHealth"));
            f.Reopen(); Assert.That(f.Runtime.runHistory.LatestOutcome.BranchOutcomes, Is.Null);
            Assert.That(f.Runtime.runHistory.LatestOutcome.Party, Is.Null);
            Assert.That(JsonUtility.ToJson(f.Runtime.runHistory.LatestOutcome), Is.EqualTo(aggregate));
            Assert.That(f.Runtime.structureRuntime.Heat, Is.EqualTo(heat));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().Confidence, Is.EqualTo(.75d));
        }

        [Test]
        public void ReconfirmationAndContentContradictionUseOneSharedConfidence()
        {
            var f = Fixture(); f.Accept(Run(f)); f.Accept(Run(f));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().Confidence, Is.EqualTo(.875d));
            f.Accept(Run(f)); f.Accept(Run(f));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().Confidence, Is.EqualTo(1d));
            var trap = f.Runtime.corridorContent.Assignments.Single(a => a.CategoryId == MvpDungeonPlacementIds.TrapCategoryId);
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Unassign(trap.AssignmentId)));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().PerceivedDanger, Is.GreaterThan(0d));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().Confidence, Is.EqualTo(1d));
            f.Accept(Run(f));
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().PerceivedDanger, Is.Zero);
            Assert.That(f.Runtime.sharedBranchKnowledge.Records.Single().Confidence, Is.EqualTo(.75d));
        }

        [Test]
        public void SkipConfirmsOnlyTopologyAndEmptyBranchCanBeVisited()
        {
            var f = Fixture(); var d = f.Configuration.PhaseFiveB.BranchDecision;
            d.SkipThreshold = .9d; d.EnterThreshold = 1d;
            f.Accept(Run(f)); var run = f.Runtime.runHistory.LatestOutcome;
            Assert.That(run.BranchOutcomes.Single().Reason, Is.EqualTo("branch.outcome.skipped"));
            Assert.That(run.EncounterEvents, Is.Empty); Assert.That(run.BranchOutcomes.Single().ReachedAssignments, Is.Empty);
            var knowledge = f.Runtime.sharedBranchKnowledge.Records.Single();
            Assert.That(knowledge.TopologyKnown, Is.True); Assert.That(knowledge.IncentiveKnown || knowledge.DangerKnown || knowledge.ConfidenceKnown, Is.False);
            var empty = Fixture(false); empty.Accept(Run(empty));
            Assert.That(empty.Runtime.runHistory.LatestOutcome.BranchOutcomes.Single().Returned, Is.True);
            Assert.That(empty.Runtime.sharedBranchKnowledge.Records.Single().PerceivedIncentive, Is.Zero);
        }

        [TestCase(false)][TestCase(true)]
        public void AtomicWriteFailureAndStaleSessionPublishNothing(bool stale)
        {
            var f = Fixture(); var session = f.Session; var runtime = f.Runtime;
            if (stale)
            {
                var successful = Run(f); Assert.That(successful.IsSuccess, Is.True, successful.Reason);
                // Deliberately retain the old session/runtime against the newer durable bytes.
            }
            byte[] bytes = f.FileSystem.ReadAllBytes(f.ActivePath); string live = JsonUtility.ToJson(runtime);
            string knowledge = JsonUtility.ToJson(runtime.sharedBranchKnowledge);
            if (!stale) f.FileSystem.EnableFailure(Gd66DetachedSpatialMigrationTransactionTests.OperationType.Write, 2);
            var result = Run(f);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.RuntimeProjection, Is.Null); Assert.That(result.Session, Is.Null);
            Assert.That(result.Reason, Is.EqualTo(stale ? "branch.run.stale_session" : DetachedCanonicalWriteAuthority.AtomicSaveFailedReason));
            Assert.That(f.FileSystem.ReadAllBytes(f.ActivePath), Is.EqualTo(bytes)); Assert.That(f.Session, Is.SameAs(session));
            Assert.That(f.Runtime, Is.SameAs(runtime)); Assert.That(JsonUtility.ToJson(runtime), Is.EqualTo(live));
            Assert.That(JsonUtility.ToJson(runtime.sharedBranchKnowledge), Is.EqualTo(knowledge));
        }

        [Test]
        public void CorridorWorkloadOneOverRollsBackEvenAfterDamage()
        {
            var f = Fixture(); f.Configuration.PhaseFiveB.BranchDecision.MaximumAssignmentsPerBranch = 1;
            var bytes = f.Session.GetCurrentBytes(); string live = JsonUtility.ToJson(f.Runtime);
            var result = Run(f);
            Assert.That(result.Reason, Is.EqualTo(BranchRunWorkload.WorkloadExceeded)); Assert.That(result.RuntimeProjection, Is.Null);
            Assert.That(JsonUtility.ToJson(f.Runtime), Is.EqualTo(live)); Assert.That(f.FileSystem.ReadAllBytes(f.ActivePath), Is.EqualTo(bytes));
        }

        [Test]
        public void PhysicalOrderUsesDirectionThenSequenceThenOrdinalIdentity()
        {
            var values = new[] {
                new CorridorContentAssignment { AssignmentId = "z", Sequence = 0, Tile = new TileCoordinate(2, 0) },
                new CorridorContentAssignment { AssignmentId = "b", Sequence = 1, Tile = new TileCoordinate(3, 0) },
                new CorridorContentAssignment { AssignmentId = "a", Sequence = 1, Tile = new TileCoordinate(3, 0) },
                new CorridorContentAssignment { AssignmentId = "c", Sequence = 0, Tile = new TileCoordinate(3, 0) } };
            Assert.That(PhaseFiveBRouteProjection.Order(values, new TileCoordinate(4, 0)).Select(a => a.AssignmentId), Is.EqualTo(new[] { "c", "a", "b", "z" }));
            Array.Reverse(values);
            Assert.That(PhaseFiveBRouteProjection.Order(values, new TileCoordinate(4, 0)).Select(a => a.AssignmentId), Is.EqualTo(new[] { "c", "a", "b", "z" }));
        }

        [Test]
        public void SameSessionReadbackRetainsEvidenceAndLocalizedDiagnostics()
        {
            var f = Fixture(); f.Accept(Run(f)); var previous = f.Runtime;
            var run = previous.runHistory.LatestOutcome;
            var result = f.Authority.SaveRecognizedState(f.ActivePath, f.FileSystem, f.Session, f.Runtime);
            Assert.That(result.IsSuccess, Is.True, result.Reason);
            RunTransientEvidence.Retain(previous, result.RuntimeProjection);
            Assert.That(result.RuntimeProjection.runHistory.LatestOutcome.BranchOutcomes, Is.SameAs(run.BranchOutcomes));
            Assert.That(result.RuntimeProjection.runHistory.LatestOutcome.Party, Is.SameAs(run.Party));
            var table = JsonUtility.FromJson<StringTable>(File.ReadAllText("Assets/_Project/Data/Bootstrap/string_table_en.json"));
            Assert.That(table, Is.Not.Null);
            var requested = new System.Collections.Generic.List<string>();
            string text = RunPartyDiagnosticsPresenter.Build(run, (key, fallback) => { requested.Add(key); return fallback; });
            Assert.That(requested, Does.Contain("ui.run.branch.format"));
            Assert.That(requested, Does.Not.Contain("ui.run.branch.no_decision_format"));
            Assert.That(requested, Does.Contain("branch.outcome.completed"));
            Assert.That(requested, Does.Contain("branch.knowledge.observed"));
            f.Accept(result); f.Reopen(); Assert.That(f.Runtime.runHistory.LatestOutcome.BranchOutcomes, Is.Null);
        }

        [Test]
        public void RetreatPrecedenceDiagnosticsStateNoDecisionWithoutFabricatedMetrics()
        {
            var outcome = DiagnosticOutcome(null);
            var requested = new System.Collections.Generic.List<string>();
            string text = RunPartyDiagnosticsPresenter.Build(outcome, (key, fallback) =>
            {
                requested.Add(key);
                if (key == "ui.run.branch.no_decision_format") return "NO DECISION {0} | {1} | {2} | reached {3} | {4}";
                if (key == "ui.run.branch.format") return "DECISION {0} | condition {2:0.###} | survivability {3:0.###}";
                return key;
            });
            Assert.That(text, Does.Contain("NO DECISION branch-1"));
            Assert.That(text, Does.Contain("branch.decision.retreat_precedence"));
            Assert.That(text, Does.Not.Contain("condition").And.Not.Contain("survivability"));
            Assert.That(requested, Does.Contain("ui.run.branch.no_decision_format"));
            Assert.That(requested, Does.Not.Contain("ui.run.branch.format"));
        }

        [TestCase(false, "branch.decision.appeal_skip")]
        [TestCase(true, "branch.decision.appeal_enter")]
        public void ActualDecisionDiagnosticsRetainDetailedMetrics(bool enter, string reason)
        {
            var outcome = DiagnosticOutcome(new BranchDecisionEvidence {
                Enter = enter, Reason = reason, Condition = .625d, ExpectedSurvivability = .5d });
            var requested = new System.Collections.Generic.List<string>();
            string text = RunPartyDiagnosticsPresenter.Build(outcome, (key, fallback) =>
            {
                requested.Add(key);
                if (key == "ui.run.branch.no_decision_format") return "NO DECISION {0}";
                if (key == "ui.run.branch.format") return "DECISION {0} | condition {2:0.###} | survivability {3:0.###}";
                return key;
            });
            Assert.That(text, Does.Contain("DECISION branch-1 | condition 0.625 | survivability 0.5"));
            Assert.That(requested, Does.Contain("ui.run.branch.format"));
            Assert.That(requested, Does.Not.Contain("ui.run.branch.no_decision_format"));
        }

        private static RunOutcomeRecord DiagnosticOutcome(BranchDecisionEvidence decision) => new RunOutcomeRecord {
            Party = RunPartyGenerator.Create(PhaseFiveBTestConfig.Create(), "run-diagnostics"),
            BranchOutcomes = new[] { new BranchOutcomeEvidence {
                Fork = new PhaseFiveBFork { OptionalBranchId = "branch-1" }, Decision = decision,
                PrecedenceReason = decision == null ? "branch.decision.retreat_precedence" : null,
                Reason = decision == null ? "branch.outcome.stopped" : decision.Enter ? "branch.outcome.completed" : "branch.outcome.skipped",
                KnowledgeOutcome = "branch.knowledge.topology" } }
        };

        [Test]
        public void KnowledgeUnknownFactsResetSharedConfidenceAndSkipHasNoPassiveDecay()
        {
            var c = PhaseFiveBTestConfig.Create().BranchDecision;
            var fork = new PhaseFiveBFork { FloorInstanceId = "floor", OptionalBranchId = "branch", EdgeId = "edge", Fingerprint = new string('a', 64) };
            var existing = new BranchKnowledgeRecord { FloorInstanceId = "floor", OptionalBranchId = "branch", EdgeId = "edge",
                TopologyFingerprint = fork.Fingerprint, TopologyKnown = true, IncentiveKnown = true,
                PerceivedIncentive = .5d, ConfidenceKnown = true, Confidence = 1d };
            var owner = new SharedBranchKnowledgeAuthority { Records = new[] { existing } };
            var observation = new BranchOutcomeEvidence { Fork = fork, Returned = false };
            var skip = BranchKnowledgeLearning.Propose(owner, new[] { observation }, true, "run-2", c, new BranchRunWorkload(c));
            Assert.That(skip.Records.Single().Confidence, Is.EqualTo(1d)); Assert.That(skip.Records.Single().DangerKnown, Is.False);
            Assert.That(skip.Records.Single().LastConfirmedRunId, Is.EqualTo("run-2"));
            observation.Returned = true; observation.ActualIncentive = .5d; observation.ActualDanger = .25d;
            var learned = BranchKnowledgeLearning.Propose(skip, new[] { observation }, true, "run-3", c, new BranchRunWorkload(c));
            Assert.That(learned.Records.Single().Confidence, Is.EqualTo(.75d)); Assert.That(learned.Records.Single().DangerKnown, Is.True);
            var wiped = BranchKnowledgeLearning.Propose(learned, new[] { observation }, false, "run-4", c, new BranchRunWorkload(c));
            Assert.That(JsonUtility.ToJson(wiped), Is.EqualTo(JsonUtility.ToJson(learned)));
            fork.Fingerprint = new string('b', 64); observation.Returned = false;
            var topology = BranchKnowledgeLearning.Propose(learned, new[] { observation }, true, "run-5", c, new BranchRunWorkload(c));
            Assert.That(topology.Records.Single().IncentiveKnown || topology.Records.Single().DangerKnown || topology.Records.Single().ConfidenceKnown, Is.False);
        }

        [Test]
        public void ProjectionUsesOnlyRequiredSuffixAndKeepsStableRoomIdentities()
        {
            var f = Fixture();
            var preview = StructuralEditService.Preview(f.State, new StructuralConstructionRequest {
                RoomDefinitionId = "spatial.room.basic",
                Anchor = new TileCoordinate(0, 6), Orientation = CardinalOrientation.Zero,
                TerminalConnectionPointId = "north" }, f.Production, f.Compatibility, f.Configuration, f.Profile.Canonical);
            Assert.That(preview.IsValid, Is.True, string.Join(",", preview.ReasonCodes));
            f.Accept(f.Execute(DetachedCanonicalMutationRequest.Construct(preview)));
            var required = CanonicalMvpRouteProjection.Resolve(f.Runtime, f.Configuration, f.Production);
            foreach (var room in required) f.Accept(f.Execute(DetachedCanonicalMutationRequest.Place(
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, room.RoomInstanceId)));
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(f.Session.GetCurrentBytes(), f.Context);
            var plan = PhaseFiveBRouteProjection.Resolve(owned, f.Runtime, f.Production, f.Configuration);
            Assert.That(plan.RequiredRooms, Has.Length.EqualTo(2)); Assert.That(plan.RequiredRooms.All(r => !string.IsNullOrEmpty(r.NodeId) && !string.IsNullOrEmpty(r.FloorInstanceId)), Is.True);
            Assert.That(plan.Forks.Single().RequiredSuffix.Single().Room.RoomInstanceId, Is.EqualTo(required[1].RoomInstanceId));
            Assert.That(plan.Forks.Single().RemainingRequiredDanger, Is.EqualTo(3d));
        }
    }
}
#endif
