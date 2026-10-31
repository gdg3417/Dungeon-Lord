#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.RunSimulation;

namespace DungeonBuilder.M0.Tests
{
    public abstract partial class PhaseSevenA4ProductionSceneTests
    {
        [UnityTest]
        public IEnumerator CombatC0ProductionManualRunUsesRealGeometryAndKeepsTransientDiagnostics()
        {
            var root = GameRoot.Instance; root.Save.structureRuntime.ManaReserve = 1000;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                "placement.category.monster", "placement.option.monster.skeleton", room.RoomInstanceId,
                floor.FloorInstanceId, new TileCoordinate(0,3))).IsSuccess, Is.True);
            double mana = root.Save.structureRuntime.ManaReserve;
            int sequence = root.Save.runHistory.NextRunSequence;
            Assert.That(root.SimulateRunOnce(), Is.True);
            var run = root.Save.runHistory.LatestOutcome;
            Assert.That(CombatC0Diagnostics.IsCombatC0(run), Is.True);
            Assert.That(CombatC0Diagnostics.Monsters(run), Has.Count.EqualTo(2));
            Assert.That(CombatC0Diagnostics.Events(run).Count(e => e.Kind == CombatC0EventKind.EncounterStart), Is.EqualTo(1));
            Assert.That(CombatC0Diagnostics.Events(run).Any(e => e.Kind == CombatC0EventKind.Attack && e.Damage > 0), Is.True);
            Assert.That(root.Save.runHistory.NextRunSequence, Is.EqualTo(sequence+1));
            long recovery = CombatC0Diagnostics.Monsters(run).Sum(m => System.Math.Max(0,
                MonsterRecoveryRules.Capture(root.RunSimulationConfig).Readiness(m.OptionId) - m.Health));
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(mana - recovery));
            string diagnostics = RunPartyDiagnosticsPresenter.Build(run, root.Content.GetString);
            Assert.That(diagnostics, Does.Contain("E1 R").And.Contain("damage").And.Not.Contain("TileCoordinate"));
            Assert.That(controller.StartReplay(), Is.True);
            Assert.That(controller.Replay.Combat, Is.Not.Null);
            controller.CloseReplay();
            Assert.That(File.ReadAllText(root.SaveService.SavePath), Does.Not.Contain("CombatEvents").And.Not.Contain("CombatMonsters"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator VisualRunFeedbackCasualtyAndMitigatedDamageAreRecordedAndReadOnly()
        {
            var root = GameRoot.Instance; root.Save.structureRuntime.ManaReserve = 1000;
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            foreach (var value in new[] { ("placement.category.monster", "placement.option.monster.skeleton", new TileCoordinate(3,3)),
                ("placement.category.trap", "placement.option.trap.spike", new TileCoordinate(2,1)),
                ("placement.category.loot_node", "placement.option.loot_node.hidden_cache", new TileCoordinate(3,1)) })
                Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(value.Item1,
                    value.Item2, room.RoomInstanceId, floor.FloorInstanceId, value.Item3)).IsSuccess, Is.True);
            floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var bytes = File.ReadAllBytes(root.SaveService.SavePath);
            var savedOutcome = root.Save.runHistory.LatestOutcome;
            try
            {
                foreach (bool fatal in new[] { true, false })
                {
                    // Explicit synthetic engine fixture: known damage/mitigation, never owner gameplay or a persisted run.
                    var f = EditMode.VisualRunReplayTests.Completed();
                    var outcome = EditMode.VisualRunReplayTests.CombatEvidence(f, fatal, !fatal, floor);
                    outcome.PresentationLayoutFingerprint = root.SaveService.DungeonDraftContext.Fingerprint(root.Save.validatedCanonicalSpatialState);
                    Assert.That(RunTransientEvidence.TryCorridorFingerprint(outcome, root.Save.corridorContent,
                        root.SaveSpatialMigrationLimits.Canonical, out var corridor), Is.True);
                    outcome.PresentationCorridorFingerprint = corridor;
                    root.Save.runHistory.LatestOutcome = outcome;
                    var live = JsonUtility.ToJson(root.Save);
                    foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(720,1280) })
                    {
                        DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                        Assert.That(controller.StartReplay(), Is.True); controller.ToggleReplayPause();
                        for (int i = 0; i < 8; i++) yield return null;
                        bool captured = false;
                        while (!controller.Replay.Complete)
                        {
                            var current = controller.Replay.Current;
                            if (current.Step.Encounter != null)
                            {
                                controller.ToggleReplayPause(); controller.Replay.Advance(controller.Replay.ApproachSeconds);
                                controller.ToggleReplayPause(); yield return null;
                                var damage = controller.GetComponent<UIDocument>().rootVisualElement.Q<Label>("replayDamage");
                                Assert.That(damage.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                                if (fatal)
                                {
                                    Assert.That(current.Step.Encounter.HealthAfter, Is.Zero);
                                    Assert.That(controller.World.ReplayCasualtyCount, Is.GreaterThan(0));
                                }
                                else Assert.That(controller.World.ReplayCasualtyCount, Is.Zero);
                                if (!captured && (fatal || current.Step.Kind == RunStepKind.TrapTrigger))
                                {
                                    if (!fatal) { Assert.That(current.Step.Encounter.UnmitigatedDamage, Is.GreaterThan(0)); Assert.That(current.Step.Encounter.Damage, Is.Zero); }
                                    var position = controller.Replay.DisplayPosition; float elapsed = controller.Replay.Elapsed;
                                    yield return null; Assert.That(controller.Replay.Elapsed, Is.EqualTo(elapsed));
                                    Assert.That(controller.Replay.DisplayPosition, Is.EqualTo(position));
                                    yield return CompositionCapture(fatal ? "visual-feedback-casualty" : "visual-feedback-mitigated-trap", size);
                                    captured = true;
                                }
                            }
                            controller.ToggleReplayPause(); controller.Replay.Advance(controller.Replay.CurrentDuration - controller.Replay.Elapsed + .00001f);
                            controller.ToggleReplayPause(); yield return null;
                        }
                        Assert.That(captured, Is.True); Assert.That(controller.World.ReplayCasualtyCount, Is.Zero);
                        controller.CloseReplay(); Assert.That(controller.World.ReplayPartyVisible, Is.False);
                        CollectionAssert.AreEqual(bytes, File.ReadAllBytes(root.SaveService.SavePath)); Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
                    }
                }
            }
            finally { controller.CloseReplay(); root.Save.runHistory.LatestOutcome = savedOutcome; }
        }
        [UnityTest]
        public IEnumerator VisualRunReplayProductionLifecycleAndResponsiveControls()
        {
            var root = GameRoot.Instance;
            // PR #236's B1 renderer remains a read-only presentation contract.
            // The real canonical run still uses the consumed current C2 inputs.
            var simulation = (RunSimulationService)typeof(GameRoot).GetField("_runSimulationService",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root);
            Assert.That(controller.StartReplay(), Is.False);
            root.Save.structureRuntime.ManaReserve = 1000; // Explicit fixture funding before content purchases.
            var floor = root.Save.validatedCanonicalSpatialState.Floors[0]; var room = floor.Layout.Rooms[0];
            string existingMonster = floor.RoomContents.Assignments.Single().AssignmentId;
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Unassign(existingMonster, floor.FloorInstanceId)).IsSuccess, Is.True);
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Redeploy(existingMonster,
                room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(3,3))).IsSuccess, Is.True);
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                "placement.category.monster", "placement.option.monster.skeleton", room.RoomInstanceId,
                floor.FloorInstanceId, new TileCoordinate(0,3))).IsSuccess, Is.True);
            foreach (var value in new[] { ("placement.category.trap", "placement.option.trap.spike", 1),
                ("placement.category.loot_node", "placement.option.loot_node.hidden_cache", 2) })
            {
                var placed = root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                    value.Item1, value.Item2, room.RoomInstanceId, floor.FloorInstanceId, new TileCoordinate(value.Item3,1)));
                Assert.That(placed.IsSuccess, Is.True, placed.Reason);
            }
            Assert.That(root.SaveService.ExecuteCanonicalMutation(root.Save, DetachedCanonicalMutationRequest.Place(
                "placement.category.trap", "placement.option.trap.spike", room.RoomInstanceId,
                floor.FloorInstanceId, new TileCoordinate(1,3))).IsSuccess, Is.True);
            root.Save.completedResearch = new CompletedResearchState { ProjectIds = new[] { "ac_100" } };
            root.Save.structureRuntime.ManaReserve = 1000;
            Assert.That(root.SaveService.CommitFloorConstruction(root.Save, root.SaveService.PreviewFloorConstruction(root.Save)).IsSuccess, Is.True);
            string otherFloor = root.Save.validatedCanonicalSpatialState.Floors.Single(f => f.FloorIndex == 1).FloorInstanceId;
            var context = (DetachedCurrentTargetValidationContext)typeof(SaveService).GetField("_validationContext",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(root.SaveService);
            var beforeHistorical = root.SaveService.CanonicalSession.GetCurrentBytes();
            string historicalLive = JsonUtility.ToJson(root.Save);
            var owned = DetachedCompleteSaveContract.ParseValidateAndRoundTrip(beforeHistorical, context);
            Assert.That(owned.CurrentTargetValidated, Is.True);
            var snapshot = ActiveFloorRunSnapshot.Create(owned, root.ProductionSpatialContent,
                context.RecoveryInputs.Configuration, root.SaveSpatialMigrationLimits.Canonical,
                context.RecoveryInputs.Loot, executionInputs: root.Save);
            var historical = simulation.StartSnapshot(root.Save.runHistory.NextRunSequence, snapshot).DrainToCompletion(true);
            Assert.That(CombatC0Diagnostics.IsCombatC0(historical), Is.False);
            historical.PresentationLayoutFingerprint = root.SaveService.DungeonDraftContext.Fingerprint(root.Save.validatedCanonicalSpatialState);
            Assert.That(RunTransientEvidence.TryCorridorFingerprint(historical, root.Save.corridorContent,
                root.SaveSpatialMigrationLimits.Canonical, out var historicalCorridor), Is.True);
            historical.PresentationCorridorFingerprint = historicalCorridor;
            CollectionAssert.AreEqual(beforeHistorical, File.ReadAllBytes(root.SaveService.SavePath));
            Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(historicalLive), "Pure B1 execution cannot mutate the canonical owner");
            Assert.That(root.SimulateRunOnce(), Is.True);
            Assert.That(CombatC0Diagnostics.IsCombatC0(root.Save.runHistory.LatestOutcome), Is.True);
            // The manual run invalidates the next foreground opportunity. The
            // real Update coordinator must reconcile that scheduler state before the
            // stricter UI-only baseline (which never accepts substantive writes).
            var beforeTime = root.Save.timeProvenance;
            long beforeA = root.Save.livingDungeon.Scheduler.AcceptedThrough, beforeC = root.Save.livingDungeon.Scheduler.ClosedThrough;
            long beforeTicks = root.Save.totalTicks, beforeRevision = root.Save.livingDungeon.Revision;
            double beforeMana = root.Save.structureRuntime.ManaReserve;
            string beforeHistory = JsonUtility.ToJson(root.Save.runHistory), beforeMonsters = JsonUtility.ToJson(root.Save.monsterContinuity);
            string beforeRuntime = JsonUtility.ToJson(root.Save);
            var timeOwner = root.TimeService; var beforeUtc = sceneClock.Now; long beforeMonotonic = sceneClock.Elapsed100ns;
            bool initializeOpportunity = !root.Save.livingDungeon.Scheduler.NextOpportunityTime.HasValue;
            var rootFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object RootField(string name) => typeof(GameRoot).GetField(name, rootFlags).GetValue(root);
            bool PendingRunPublication() => (bool)RootField("foregroundDirty") || (bool)RootField("foregroundPumping");
            for (int frame = 0; frame < 8 && PendingRunPublication(); frame++) {
                yield return null;
                Assert.That(root.isActiveAndEnabled, Is.True);
                Assert.That(RootField("foregroundFailure"), Is.Null);
                Assert.That(RootField("foregroundRetryPending"), Is.EqualTo(false));
                Assert.That(root.TimeService.RecoveryClockActive || root.TimeService.HasDeferredRecovery ||
                    root.TimeService.HasForegroundPersistenceStall || root.SaveService.RequiresCanonicalRecovery, Is.False);
            }
            Assert.That(PendingRunPublication(), Is.False, "Normal Update must finish the manual run's pending reconciliation within the fixture bound");
            Assert.That(root.TimeService, Is.SameAs(timeOwner));
            Assert.That(sceneClock.Now, Is.EqualTo(beforeUtc)); Assert.That(sceneClock.Elapsed100ns, Is.EqualTo(beforeMonotonic));
            Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(beforeRuntime));
            Assert.That(root.Save.timeProvenance.SameAs(beforeTime), Is.True);
            Assert.That(root.Save.structureRuntime.ManaReserve, Is.EqualTo(beforeMana));
            Assert.That(root.Save.totalTicks, Is.EqualTo(beforeTicks));
            Assert.That(root.Save.livingDungeon.Scheduler.AcceptedThrough, Is.EqualTo(beforeA));
            Assert.That(root.Save.livingDungeon.Scheduler.ClosedThrough, Is.EqualTo(beforeC));
            Assert.That(root.Save.livingDungeon.IsClosed, Is.True);
            Assert.That(JsonUtility.ToJson(root.Save.runHistory), Is.EqualTo(beforeHistory));
            Assert.That(JsonUtility.ToJson(root.Save.monsterContinuity), Is.EqualTo(beforeMonsters));
            Assert.That(root.Save.livingDungeon.Scheduler.NextOpportunityTime.HasValue, Is.True);
            Assert.That(root.Save.livingDungeon.Scheduler.NextOpportunityTime.Value, Is.GreaterThan(beforeC));
            Assert.That(root.Save.livingDungeon.Revision, Is.EqualTo(beforeRevision + (initializeOpportunity ? 1 : 0)));
            CollectionAssert.AreEqual(root.SaveService.CanonicalSession.GetCurrentBytes(), File.ReadAllBytes(root.SaveService.SavePath));
            TestContext.WriteLine("H1F4_REPLAY_SETUP opportunityInitialized=" + initializeOpportunity +
                " walletPreserved=True WPreserved=True ACPreserved=True ticksPreserved=True historyPreserved=True monstersPreserved=True");
            yield return StabilizeReadOnlySceneBaseline();
            var canonicalOutcome = root.Save.runHistory.LatestOutcome;
            Assert.That(CombatC0Diagnostics.IsCombatC0(canonicalOutcome), Is.True);
            Assert.That(historical.RunId, Is.EqualTo(canonicalOutcome.RunId));
            floor = root.Save.validatedCanonicalSpatialState.Floors[0];
            var originals = floor.RoomContents.Assignments.ToDictionary(a => a.AssignmentId, a => {
                Assert.That(DungeonRunReplay.TryProject(a.RoomLocalPosition, RunPositionSpace.RoomLocal, a.RoomInstanceId,
                    floor, root.ProductionSpatialContent.Catalog, out var point), Is.True);
                return point; });
            var actors = originals.ToDictionary(p => p.Key, p => controller.World.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(v => v.name == "Content" && v.gameObject.activeSelf && (Vector2)v.transform.localPosition == p.Value));
            var bytes = File.ReadAllBytes(root.SaveService.SavePath); var live = JsonUtility.ToJson(root.Save);
            bool StartHistoricalPresentation()
            {
                // Existing transient presentation-input seam, held only during
                // this synchronous call. No yield, writer, clock or publication
                // runs with B1 evidence installed in the current runtime.
                Assert.That(root.Save.runHistory.LatestOutcome, Is.SameAs(canonicalOutcome));
                try { root.Save.runHistory.LatestOutcome = historical; return controller.StartReplay(); }
                finally {
                    root.Save.runHistory.LatestOutcome = canonicalOutcome;
                    Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
                    CollectionAssert.AreEqual(bytes, File.ReadAllBytes(root.SaveService.SavePath));
                    CollectionAssert.AreEqual(bytes, root.SaveService.CanonicalSession.GetCurrentBytes());
                }
            }
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(720,1280) })
            {
                DungeonBuilder.M0.EditorTools.ProductionDungeonScreenshots.SetGameViewSize(size.x, size.y);
                Assert.That(StartHistoricalPresentation(), Is.True); controller.ToggleReplayPause();
                int views = controller.World.GetComponentsInChildren<SpriteRenderer>(true).Length;
                foreach (var pair in originals) Assert.That((Vector2)actors[pair.Key].transform.localPosition, Is.EqualTo(pair.Value));
                for (int i = 0; i < 15; i++) yield return null;
                var ui = controller.GetComponent<UIDocument>().rootVisualElement;
                foreach (var name in new[] { "pauseReplay", "closeReplay", "edit" }) UatSafeAction(ui.Q<Button>(name));
                controller.SetTextSize(DungeonTextSize.Large); for (int i = 0; i < 3; i++) yield return null;
                foreach (var name in new[] { "pauseReplay", "closeReplay", "edit" }) UatSafeAction(ui.Q<Button>(name));
                controller.SetTextSize(DungeonTextSize.Default); for (int i = 0; i < 3; i++) yield return null;
                Assert.That(ui.Q<Label>("replayEvent").text, Does.Contain(root.Save.runHistory.LatestOutcome.RunId));
                int index = controller.Replay.Index; var position = controller.Replay.DisplayPosition;
                controller.SelectFloor(otherFloor); yield return null;
                Assert.That(controller.World.ReplayPartyVisible, Is.False);
                Assert.That(controller.Replay.Index, Is.EqualTo(index));
                controller.SelectFloor(floor.FloorInstanceId); yield return null;
                Assert.That(controller.World.ReplayPartyVisible, Is.True);
                controller.FitFloor(); yield return null;
                Assert.That(controller.Replay.Index, Is.EqualTo(index)); Assert.That(controller.Replay.DisplayPosition, Is.EqualTo(position));
                var captured = new System.Collections.Generic.HashSet<RunStepKind>();
                while (!controller.Replay.Complete)
                {
                    var current = controller.Replay.Current;
                    if (current.Step.Kind == RunStepKind.MonsterEngagement)
                    {
                        var actor = actors[current.Step.AssignmentId];
                        Assert.That(actor.gameObject.activeSelf, Is.True);
                        Assert.That(Vector2.Distance(actor.transform.localPosition, current.MonsterPath[0]), Is.LessThan(.001f));
                        controller.ToggleReplayPause(); controller.Replay.Advance(controller.Replay.ApproachSeconds * .5f);
                        controller.ToggleReplayPause(); yield return null;
                        var pausedMonster = actor.transform.localPosition; yield return null;
                        Assert.That(actor.transform.localPosition, Is.EqualTo(pausedMonster));
                        yield return CompositionCapture("visual-feedback-monster-approach-" + current.Step.Ordinal, size);
                        controller.ToggleReplayPause(); controller.Replay.Advance(controller.Replay.ApproachSeconds - controller.Replay.Elapsed);
                        controller.ToggleReplayPause(); yield return null;
                        Assert.That((Vector2)actor.transform.localPosition, Is.EqualTo(current.MarkerPosition));
                    }
                    if (current.Step.Encounter != null)
                    {
                        Assert.That(controller.Replay.DamageVisible, Is.True);
                        yield return null;
                        Assert.That(ui.Q<Label>("replayDamage").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                        Assert.That(ui.Q<Label>("replayDamage").text, Does.Contain(current.Step.Encounter.Damage.ToString()));
                        yield return CompositionCapture("visual-feedback-damage-" + current.Step.Kind + "-" + current.Step.Ordinal, size);
                    }
                    if ((current.Step.Kind == RunStepKind.Movement || current.HasMarker) && captured.Add(current.Step.Kind))
                    {
                        Assert.That(controller.World.ReplayPartyVisible, Is.EqualTo(current.Survivors > 0));
                        Assert.That(controller.World.ReplayMarkerVisible, Is.EqualTo(current.HasMarker));
                        yield return CompositionCapture("visual-feedback-" + current.Step.Kind, size);
                    }
                    controller.ToggleReplayPause();
                    controller.Replay.Advance(controller.Replay.CurrentDuration - controller.Replay.Elapsed + .00001f);
                    controller.ToggleReplayPause(); yield return null;
                    if (current.Step.Kind == RunStepKind.MonsterEngagement)
                    {
                        Assert.That(actors[current.Step.AssignmentId].gameObject.activeSelf, Is.True);
                        Assert.That((Vector2)actors[current.Step.AssignmentId].transform.localPosition, Is.EqualTo(current.MarkerPosition));
                        Assert.That(actors[current.Step.AssignmentId].color, Is.EqualTo(controller.presentationPolicy.ReplayResolvedColor));
                        yield return CompositionCapture("visual-feedback-resolved-" + current.Step.Ordinal, size);
                    }
                }
                foreach (var kind in new[] { RunStepKind.MonsterEngagement, RunStepKind.TrapTrigger, RunStepKind.LootInteraction })
                    Assert.That(captured, Does.Contain(kind));
                Assert.That(StartHistoricalPresentation(), Is.True); controller.ToggleReplayPause();
                controller.EnterEdit(); Assert.That(controller.Replay.Paused, Is.True); Assert.That(controller.World.ReplayPartyVisible, Is.False);
                Assert.That(controller.Discard(), Is.True); Assert.That(controller.Replay.Paused, Is.True);
                controller.ToggleReplayPause(); controller.Replay.Advance(float.MaxValue); yield return null;
                Assert.That(controller.Replay.Complete, Is.True);
                foreach (var assignment in floor.RoomContents.Assignments)
                {
                    bool touched = controller.Replay.Frames.Any(v => v.Step.AssignmentId == assignment.AssignmentId && v.HasMarker);
                    var color = actors[assignment.AssignmentId].color;
                    Assert.That(color, Is.EqualTo(!touched ? Color.white : assignment.CategoryId == "placement.category.monster" ?
                        controller.presentationPolicy.ReplayResolvedColor : assignment.CategoryId == "placement.category.trap" ?
                        controller.presentationPolicy.ReplayTriggeredColor : controller.presentationPolicy.ReplayLootReachedColor));
                }
                // Public controls refresh completion when a normal frame advances. Pause/resume is disabled there.
                controller.ToggleReplayPause(); yield return CompositionCapture("visual-feedback-complete", size);
                controller.CloseReplay(); Assert.That(controller.World.ReplayPartyVisible, Is.False);
                foreach (var pair in originals) { Assert.That((Vector2)actors[pair.Key].transform.localPosition, Is.EqualTo(pair.Value)); Assert.That(actors[pair.Key].color, Is.EqualTo(Color.white)); }
                Assert.That(controller.World.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(views), "Replay reuses bounded content/state views");
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(root.SaveService.SavePath));
                Assert.That(JsonUtility.ToJson(root.Save), Is.EqualTo(live));
            }
        }
    }
}
#endif
