# Phase 6A5B implementation evidence

Status: final planned Phase 6 implementation packet; external review and owner manual qualification pending. Starting baseline: merged PR #219, `5e8eeadbdd8c4c7c22309d1e709e7603a85dbdd8`. Phase 6 remains open.

## Implementation and design authority

The Phase 6 design/tuning lock now states the owner-approved interpretation formula. A pure resolver selects the actual next floor's record from the immutable pre-run shared knowledge snapshot and compares the record's A5A applicability fingerprint to the snapshotted next-floor fingerprint. Missing, stale, or unusable records are fully unknown; no hidden floor content is read. A known score is passed unchanged. For each known component, effective confidence is `clamp(shared confidence × immutable party intelligence interpretation factor, 0, 1)`; unknown components contribute uncertainty 1; overall uncertainty is the mean of the two component uncertainties. Trap expertise is excluded. Phase 5B branch interpretation and A0/A4 transition weights, thresholds, objective, identity, roll, and comparisons remain unchanged.

The PR #219 unresolved P1 finding was confirmed in the inline review thread on `DetachedCompleteSaveContract.cs`: contextual validation used `MaximumActiveFloors` to limit durable records. Contextual validation now uses `PhaseSixRunConfigValidation.MaximumSupportedActiveFloors`, matching the schema parser's fixed five-floor workload ceiling. A regression covers two records with Floor 2 Inactive, a valid config allowing one Active floor, contextual validation, session reopen, preservation of both records, one-floor runtime simulation, and rejection beyond the fixed ceiling. A zero-weight target-depth objective may remain configured when its target is currently unavailable, allowing a valid one-Active-floor configuration without changing production tuning or selecting that objective.

The current run's structured transition evidence gains perception known flags and uncertainty for localized same-session explanation. The existing result presenter shows reached floor and the localized major transition reason; the existing room history retains coarse reach after reopen. Exact transition cause remains transient and is never inferred from incomplete durable history. Writable schema stays 12; there is no schema 13, migration, new save owner, or new durable reporting field. Knowledge learning and atomic settlement remain owned by A5A's complete-run path.

## Automated qualification

| Command suffix after `unity.exe test C:/Dev/Dungeon-Lord` | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `--mode EditMode --filter PhaseSixA5B --output TestResults/phase6a5b-focused.xml --timeout 600 --no-color` | 5 | 5 | 0 | 0 |
| `--mode EditMode --filter PhaseSixA --output TestResults/phase6a5b-phasesix.xml --timeout 600 --no-color` | 219 | 219 | 0 | 0 |
| `--mode EditMode --filter PhaseFiveB --output TestResults/phase6a5b-phasefiveb.xml --timeout 600 --no-color` | 107 | 107 | 0 | 0 |
| `--mode EditMode --filter Detached --output TestResults/phase6a5b-detached.xml --timeout 600 --no-color` | 5 | 5 | 0 | 0 |
| `--mode EditMode --filter PhaseFour --output TestResults/phase6a5b-phasefour-save.xml --timeout 600 --no-color` | 735 | 735 | 0 | 0 |
| `--mode EditMode --output TestResults/phase6a5b-final-editmode.xml --timeout 1200 --no-color` | 1298 | 1298 | 0 | 0 |
| `--mode PlayMode --filter BootstrapOverlayPagingTests --output TestResults/phase6a5b-overlay-focused.xml --timeout 600 --no-color` | 89 | 89 | 0 | 0 |
| `--mode PlayMode --output TestResults/phase6a5b-final-playmode.xml --timeout 1200 --no-color` | 2797 | 2787 | 0 | 10 |

The initial full PlayMode run had one presentation failure: a one-floor legacy result was given a new floor-reach line without the new key in that fixture's small fake localization map. The fixture now supplies the new English key. The aggregate presenter names the reached floor for Floor 1 wipe/retreat as well as deeper runs, and terminal completion omits next-floor information. The affected `BootstrapOverlayPagingTests` selection passed 89/89 after correction; the final full suite passed. No failure was suppressed or converted to a skip.

Windows x86_64 Development Build: `unity.exe build C:/Dev/Dungeon-Lord --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --log-file C:/Dev/Dungeon-Lord/TestResults/phase6a5b-windows-development-build.log --provenance-path C:/Dev/Dungeon-Lord/Builds/Development/Windows/build-provenance.json --allow-dirty-build --no-tail` exited 0. Unity 6000.3.2f1 reported Succeeded, Bootstrap scene only, Development Build, 170,696,236 bytes, zero errors, and one established Unity Cloud symbol-upload credential warning. The generated managed assembly timestamp is current. Unity-generated project/render-pipeline changes were removed from the working diff; owner-controlled `ProjectSettings/UnityConnectSettings.asset` was left untouched and excluded.

No new skips were added. The established PlayMode skips are eight synchronous EditMode-only GameRoot fixtures, one non-Windows inverse filesystem case, and one Windows Player-only standalone case. Unity line-ending conversion advisories are not whitespace errors. `git diff --check` is required before PR publication.

## Qualification boundary

### PR #220 review correction

The focused production-rule comparison uses fixed `run-10`, the same generated party, current/next floor IDs, carried loot at the configured reference, selected `target_depth` pull, and unchanged production tuning. The only input difference is fully unknown perception versus applicable knowledge with stored reward at the production reward reference, danger zero, and initial shared confidence. The unknown case exits on the deterministic marginal decision; the knowledge case clearly descends. Its survivability and appeal evidence also increase. Repeating the known case yields identical evidence. No ForceDescent override, changed threshold, or test tuning is involved. The earlier identical-input A4/A5B tests remain.

The zero-weight `target_depth` case is now tested directly: production target/weight remain fixed and valid, an in-bound positive target is valid, an out-of-bound positive target is invalid, and the same unavailable target with zero weight is valid in a one-Active-floor configuration. Selection over fixed `run-1` through `run-64` never chooses the disabled target, selects both remaining positive modes, and repeats identically. This is configuration-validity semantics, not a balance change. A separate synthetic ceiling test constructs six distinct, otherwise valid knowledge records and matching spatial floor identities. Five validate at the fixed ceiling; all six validate when the test-supplied limit is six, then fail specifically at the fixed five-record limit. The one-Active-floor schema-12 save/reopen/runtime regression remains intact.

| Final acceptance contract | Existing automated owner, without duplicated A5B test |
| --- | --- |
| Unknown floor never consults hidden content; floor snapshot remains immutable | A4 `HiddenContentCannotChangeUnknownTransitionAndSnapshotSurvivesSourceMutation`, `SnapshotCopiesEveryFloorAndConfiguration` |
| Objective selection, target pull, deterministic identity and marginal/threshold semantics | A4 `ObjectiveWeightsAreNormalizedAndConfigOwned`, `ObjectivePullStrengthAndTargetDepthAreConfigOwned`, `ObjectiveApplicabilityUsesImmutableSnapshotDepth`, `TransitionObjectiveAndBranchHashesHaveNoCallOrderState`, `NoNextFloorExitsWithoutRollAndMarginalEqualityExits`, `AppealThresholdEqualityIsDeterministic` |
| Lifecycle activation, Active-floor bound, floor transfer, exit, retreat, wipe, and one settlement | A4 `LifecyclePreviewIsPureAndActivationReopensWithoutManaOrInvestmentChanges`, `RealTwoFloorRunUsesOnePartyAndPublishesOneSettlement`, `WipeTerminatesWithoutDecisionAtThatFloor`, `RetreatBeforeCompletionHasNoDecisionAtThatFloor`, `CasualtiesStayDeadAndOriginalMemberTraitsAndFormationPersist` |
| Same-session transient evidence and coarse reopened history | A4 `SameSessionOfflinePublicationRetainsAllTransientRunEvidenceButReopenDoesNotFabricateIt` |
| A5A applicability: lifecycle stable, material room/topology and optional corridor changes stale | A5A `RequiredContentInvalidatesButLifecycleAloneDoesNot`, `MaterialRoomTopologyChangeInvalidatesFloorKnowledge`, `OptionalCorridorContentChangeInvalidatesOnlyItsFloor`, `CanonicalOrderingMakesFingerprintIndependentOfInputArrayOrder` |
| Pre-run snapshot, survivor-gated and wipe-safe learning | A5A `ChangedObservedContentStartsNewConfidenceAndSnapshotCopiesKnowledge`, `SurvivorLearningReconfirmsClampsAndOnlyPublishesAfterCompleteRun`, `DescendedTwoFloorRunPublishesApplicableKnowledgeForBothCompletedFloors`, `ExitBeforeFloorTwoDoesNotLearnItsHiddenSnapshot`, `FinalWipeDoesNotCreatePreciseFloorKnowledge`, `FinalWipeDoesNotUpdateExistingApplicableFloorKnowledge` |
| Atomic settlement, stale/concurrent save rejection, canonical durable records | A5A `RecordsRoundTripInStableFloorOrderAndBranchKnowledgeStaysSeparate`, `MalformedFloorKnowledgeRejectsBeforePublication`, `StaleSessionAndFailedReplaceLeaveKnowledgeAndSettlementUnchanged`, `ConcurrentBytesAndFailedReadbackCannotPublishKnowledge` |

The [revised owner UAT](manual-uat.md) gives sequential Editor/standalone actions, a six-case aggregate comparison matrix, lifecycle/material-change and save/reopen steps, expected results, and an explicit owner return packet. Conditions without a current manual selector (objective identity, intelligence factor, marginal RunId, active-floor config value) cite deterministic automated tests. Phase 6 remains open pending owner UAT.

### PR #220 correction rerun (2026-10-01)

Executable prefix for every command: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe test C:/Dev/Dungeon-Lord`. All commands exited 0. Counts are from the generated NUnit XML.

| Command suffix | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `--mode EditMode --filter PhaseSixA5B --output TestResults/phase6a5b-review-focused.xml --timeout 600 --no-color` | 8 | 8 | 0 | 0 |
| `--mode EditMode --filter PhaseSixA --output TestResults/phase6a5b-review-phasesix.xml --timeout 600 --no-color` | 222 | 222 | 0 | 0 |
| `--mode EditMode --filter PhaseFiveB --output TestResults/phase6a5b-review-phasefiveb.xml --timeout 600 --no-color` | 107 | 107 | 0 | 0 |
| `--mode EditMode --filter PhaseFour --output TestResults/phase6a5b-review-phasefour.xml --timeout 600 --no-color` | 735 | 735 | 0 | 0 |
| `--mode EditMode --filter Detached --output TestResults/phase6a5b-review-detached.xml --timeout 600 --no-color` | 5 | 5 | 0 | 0 |
| `--mode PlayMode --filter BootstrapOverlayPagingTests --output TestResults/phase6a5b-review-overlay.xml --timeout 600 --no-color` | 89 | 89 | 0 | 0 |
| `--mode EditMode --output TestResults/phase6a5b-review-full-editmode.xml --timeout 1200 --no-color` | 1301 | 1301 | 0 | 0 |
| `--mode PlayMode --output TestResults/phase6a5b-review-full-playmode.xml --timeout 1200 --no-color` | 2800 | 2790 | 0 | 10 |

An attempted `--mode EditMode --filter MvpRouteResultPresenter` selected zero tests; it is not counted as qualification. The 89-case reporting selection and full suites cover presenter/localization behavior. The ten PlayMode skips are the same established eight synchronous EditMode-only GameRoot fixtures, one non-Windows inverse filesystem case, and one Windows Player-only standalone case. No new skip or suppression was added. The correction modified tests and documentation only; the successful Windows Development Build from the initial A5B packet was not rerun.

Files changed by this correction since reviewed HEAD `2a6c334c026935e3495421611571a16961c284c4`: `Assets/_Project/Tests/EditMode/PhaseSixA5BTests.cs`, `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`, this implementation evidence, `manual-uat.md`, and `pr-description.md`. The pre-existing owner-controlled `ProjectSettings/UnityConnectSettings.asset` diff remains excluded.

The [manual UAT](manual-uat.md) is prepared for Editor and Windows standalone. The owner must record actual results after external review. Production gameplay content remains Floors 1 and 2. No persistent detailed transition cause, Phase 7 editor work, broader balance pass, or Floors 3–5 production content is included.

## Changed-file inventory

- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionDecision.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionPerceptionResolver.cs` and `.meta`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseSixRunConfig.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.cs`
- `Assets/_Project/Scripts/Services/MvpPlayerLoopSummaryPresenter.cs`
- `Assets/_Project/Scripts/Services/MvpRouteResultPresenter.cs`
- `Assets/_Project/Scripts/Util/Models.cs`
- `Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA5BTests.cs` and `.meta`
- `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`
- `docs/planning/post-gd60-mvp-execution-plan.md`
- `Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md`
- `Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md`
- `Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/pr-description.md`
