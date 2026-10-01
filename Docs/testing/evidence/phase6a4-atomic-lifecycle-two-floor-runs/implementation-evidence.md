# Phase 6A4 implementation and qualification evidence

Baseline: merged PR #217, `756607cca58aac584f69cdc3b4c27d30b58eefad`.
Branch: `codex/phase-6a4-atomic-lifecycle-two-floor-runs`.

A4 makes an eligible constructed Floor 2 safely usable through atomic lifecycle controls and deterministic two-floor runs. It is not Phase 6 closeout. Schema remains 11; there is no migration or new durable owner for party HP, snapshots, objectives, between-floor state, or floor knowledge.

## Owner manual qualification interruption and follow-up

Owner Unity Editor qualification at reviewed head `269d2d3760f592aabbc08c023afa49485777a421` stopped when selected constructed-Inactive Floor 2 did not visibly activate after direct Activate or Activate All. No lifecycle result appeared at the distant structural-feedback location, and no red Console error was reported. This was an actual activation failure as well as a feedback defect.

Read-only diagnostics of the owner's save confirmed valid schema-11 bytes, successful reopen, ac_100, and an Entrance → room → Completion route with Skeleton and Hidden Cache. The first rejecting stage was `DetachedCompleteSaveContract.ParseValidateAndRoundTripCore` calling `CanonicalSpatialSaveSerializer.Parse` on the assembled canonical members: it returned `NonCanonicalBytes`, surfaced as `gd66.transaction.candidate_invalid`. The original state, detached post-activation canonical validation, member serialization result, and recognized-state capture all passed. Investment, Phase Five, production semantics, and compatibility/current-target validation had not yet been reached.

The save has NativeCanonical authority with null migration transaction/fingerprint fields. The lifecycle candidate's Unity JSON clone materializes these absent strings as empty strings. Full spatial serialization already normalizes native migration metadata to null; `SerializeMembers` made a fresh canonical copy but omitted that normalization, emitting empty strings into the complete-save candidate. The strict parse/round-trip correctly rejected those noncanonical bytes. The correction calls the existing normalization on that detached member copy before emission. It does not relax validation, repair invalid saves, change investment/content/custody/geometry, or add a persistence owner. Schema remains 11 with no migration. Existing lifecycle fixtures used Migrated authority with populated metadata, so they missed this native-save shape. Skeleton/Hidden Cache was representative content, not the cause.

With the correction, the temporary owner-save diagnostic passed raw complete-save, investment, Phase Five, production, current-target preparation, and actual lifecycle snapshot preview. It only read owner bytes and prepared in-memory candidates; the owner's save was never written. All temporary instrumentation and owner-specific test code were removed. Permanent GameRoot/SaveService coverage now runs the representative constructed-Inactive route/content against both native and migrated authority, with preview, commit, exact readback/reopen, Active count 2, deactivation to 1, reactivation to 2, unchanged mana/investment/identities/content/custody, persistent success feedback, and Activate All no-op. Invalid-layout and stale-session regressions remain enforced.

Independent localized lifecycle feedback now appears immediately below the three lifecycle buttons and survives structural publication/reconciliation. A reason-coverage test verifies every A4 eligibility/lifecycle/technical-save key has readable English text. `floor.activation.layout_invalid` already matches the authority and table at this head; the alleged longer key is absent and was not introduced. Eight exact technical-save keys, including `gd66.transaction.candidate_invalid`, were added; English now has 976 entries and no duplicate keys.

The NativeCanonical member normalization and dedicated adjacent lifecycle feedback correction was requalified successfully. No debug instrumentation remains in production.

## Runtime authorities

`RequiredFloorTraversal` is shared by A3 eligibility and `CanonicalRunnableFloorProjection`. It resolves one floor's required path from Entrance, fails closed on pre-terminal ambiguity/cycles, and returns immediately at the first reached Completion. Required edges beyond Completion are not executed. The compatibility projection delegates to the same per-floor authority and exposes Floor 1 only; it never concatenates floors.

`PhaseFiveBRouteProjection.ResolveFloor` builds each floor's required rooms and optional forks independently. RequiredSuffix and RemainingRequiredDanger use only later required rooms in that same plan. Branch fingerprints, assignment ordering, knowledge, and branch decision identities remain floor-local.

`ActiveFloorRunSnapshot.Create` validates the canonical Active prefix, production semantics, configuration, encounter profiles, loot inputs, and workload before party formation. Floors are ordered by index and stable identity. Each immutable floor snapshot defensively captures its spatial inputs and branch plan. The complete snapshot also captures configuration, loot configuration/table, branch knowledge, initial runtime, ticks, and posture. Real execution consumes this snapshot through `RunSimulationService.Floors`; activation preflight calls the same factory. No cross-floor graph edge is created or persisted.

The coordinator forms one RunParty and selects one transient depth objective. It resolves each floor's required rooms and optional branches, stops on retreat/wipe, and resolves EXIT/DESCEND only after survivors reach Completion. Descent reuses the same party object, RunId, member ordinals, classes, levels, exact current/max HP, behavior, capabilities, trap expertise, intelligence, and deterministic surviving formation. Original-member personality aggregation remains intact. There is no healing, replacement, refill, transition encounter, damage, loot, fatigue, or mana event.

Loot and casualty evidence accumulate across floors. Extraction, attraction, forecast, demand, and Heat resolve after the route loop. Cooling, branch-learning publication, history append, sequence increment, and objective evaluation then execute once on the detached complete-run candidate, followed by one durable publication. Tests check one history entry, one sequence increment, one atomic replacement, carried loot, and exact boundary HP/formation. No floor-boundary checkpoint is written.

`Success` retains its full-clear meaning. Deliberate nonfinal EXIT has `run.reason.floor_exit` and `run.route.floor_exit`, with localized presenter handling that avoids ordinary failure wording. Final completion, retreat, wipe, and technical rejection remain separate outcomes; technical rejection publishes no gameplay candidate.

## Determinism and configuration

Production `run_simulation_config.json` owns the approved Phase Six weights, references, thresholds, behavior minima, uncertainty, objective entries, rule sources, and workload limits. Objective definitions own selection `Weight`, normalized `PullStrength`, and an applicable zero-based `TargetFloorIndex`: shallow is `0.20 / 0.00`, target_depth is `0.50 / 0.75 / 1` (Floor 2), and deepest_reasonable is `0.30 / 0.50`. Selection Weight controls assignment frequency, PullStrength controls the active objective's normalized deeper pressure, and the separate global ObjectiveWeight controls that pressure's importance in descent appeal. Runtime reads both target and pull from the selected definition. Target-depth pull also requires its configured target to fall within the immutable Active-prefix snapshot; a next floor alone cannot make an absent target reachable. Validation rejects malformed/nonfinite/out-of-range/negative/duplicate/unsupported/zero-total values, missing or contradictory targets, unsupported rule sources, more than five Active floors, and more than four transitions. Appeal derives its total weight from configuration; objective selection normalizes configured weights in ordinal mode order.

Transition identity is `(RuleSourceId, RunId, CurrentFloorInstanceId, NextFloorInstanceId)` using configured `run.floor_transition_decision.phase6.v1`. Objective identity is `(RuleSourceId, RunId)` using configured `run.depth_objective.phase6.v1`. Each field is UTF-8 with unsigned 32-bit big-endian byte length, followed by SHA-256; the first unsigned big-endian word divided by 4294967296.0 gives [0,1). These are independent of Phase 5B branch rolls and call order. Marginal equality exits; appeal equality exits at ExitThreshold and descends at DescendThreshold. Survivability equality proceeds to appeal. No next floor exits without a marginal roll.

Transition perception is explicit and separate from actual snapshotted content. A4 supplies unknown reward/danger and configured uncertainty. Hidden Floor 2 content cannot affect Floor 1's unknown-perception decision; it can affect later Floor 2 encounters after descent. Durable shared floor knowledge is unimplemented, and sharedBranchKnowledge remains branch-specific.

`RunTransientEvidence.Retain` is the single same-session retention helper for Party, EncounterEvents, BranchOutcomes, FloorTransitions, and DepthObjective. `GameRoot.PublishCanonicalRuntime` invokes it before replacing the live Save reference, matching records by RunId and TickStarted. A later offline/canonical publication therefore retains complete A4 diagnostics in memory. Those properties remain unserialized, and canonical reopen does not fabricate them.

## Lifecycle and persistence

`FloorLifecycleAuthority` composes the existing detached writer/session, canonical validation, A3 eligibility, production validation, and exact runtime snapshot factory. Commit verifies current durable bytes, re-resolves eligibility, prepares an isolated candidate, validates/materializes the entire Active prefix, and prepares exact complete-save bytes. Atomic replacement is followed by exact durable readback and reopen validation while rollback bytes remain available. Live publication occurs only after those checks. Fault tests cover stale sessions, write/replace/flush/readback/reopen failures and unchanged durable/live state.

Only schema-11 SavedSpatialFloor.ActivationState is mutated. Floor 1 cannot deactivate. Deactivation changes the requested suffix; manual activation changes only its requested floor. A4 Activate All Eligible supports the configured Floor 2 progression and stops at its first blocker. When Floor 2 is already Active, Activate All validates the current run snapshot and succeeds as a no-op after exact session-byte verification; it writes no replacement bytes, replaces no session, and publishes no new runtime. Direct Activate retains A3's already-active blocker. Synthetic deeper fixtures prove cascade semantics without adding production Floors 3–5. Layout, assignments, custody, investment, stable IDs, and mana remain unchanged.

Bootstrap's localized Activate, Deactivate, and Activate All Eligible controls call the lifecycle authority and refresh from published state. Existing Locked / Unlocked-Not-Constructed / Inactive / Active distinctions remain. CanonicalActiveFloorResolver remains the only Active-floor count for online/offline mana. Startup now configures loot before save loading/offline publication, so a two-Active-floor candidate can materialize its snapshot at startup.

## Qualification

Local XML reports are under TestResults and are not committed artifacts.

Current native-save/feedback correction (Unity test XML under TestResults):

| Run | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Focused A4 and affected writer/session/complete-save/semantic authorities | 172 | 172 | 0 | 0 |
| Relevant Phase 4–6 / route / save / UI regression selection | 1271 | 1271 | 0 | 0 |
| Full EditMode | 1271 | 1271 | 0 | 0 |
| Full PlayMode | 2770 | 2760 | 0 | 10 |
| Explicit production/configuration/localization/build-gate EditMode | 173 | 173 | 0 | 0 |
| Explicit layout/migration-localization PlayMode | 82 | 82 | 0 | 0 |

The ten PlayMode skip identities are unchanged: eight synchronous EditMode-only GameRoot fixtures, one non-Windows inverse filesystem test, and one Windows Player-only standalone test. The English Bootstrap table contains 976 unique entries. Windows x86_64 Development Build passed under Unity 6000.3.2f1: wrapper exit 0, Bootstrap scene only, Development Build, 170,671,076-byte output, zero build errors, and one known Unity Cloud symbol-upload credential warning. The final scoped git diff --check passed.

Owner Editor qualification then passed direct Floor 2 activation, Activate All already-Active no-op, deactivation/reactivation, and Floor 1 deactivation rejection. Lifecycle feedback was visible and localized. Passive mana followed one/two/one Active-floor behavior: two floors produced 228 mana/hour at Notice 95% and one floor produced 171 mana/hour at Notice 95%; lifecycle changes themselves granted or refunded no mana. Floor 2 layout, content, and investment survived lifecycle changes; Active and constructed-Inactive state each survived close/reopen with no save corruption or recovery warning.

Owner gameplay qualification passed the one-floor baseline run, deliberate Floor 1 EXIT with EXIT presentation, and DESCEND. The same RunId and party crossed floors with exact HP continuity, no refill, healing, replacement, or resurrection, and one final settlement. Same-session canonical publication retained Party, EncounterEvents, BranchOutcomes, FloorTransitions, and DepthObjective.

Owner Windows Development Build qualification also passed: lifecycle controls, Floor 2 Active/Inactive behavior, two-floor run behavior, DESCEND evidence, exact HP continuity, one final settlement, and lifecycle save/reopen persistence all worked. There was no crash, assertion, save-recovery warning, or new runtime error. Prototype diagnostics can be difficult to view at some Game-view resolutions and may require a larger Game view or display size; this is a readability limitation, not an A4 correctness failure.

Historical qualification on earlier reviewed heads:

| Run | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Review 2 targeted objective/lifecycle tests | 4 | 4 | 0 | 0 |
| Review 2 SaveService no-op publication test | 1 | 1 | 0 | 0 |
| Review 2 complete focused A4 | 64 | 64 | 0 | 0 |
| Review 2 relevant Phase 5B / Phase 6 / lifecycle / save / route regressions | 620 | 620 | 0 | 0 |
| Review 2 full EditMode | 1267 | 1267 | 0 | 0 |
| Review 2 full PlayMode | 2766 | 2756 | 0 | 10 |
| Review 2 explicit production/configuration/localization/layout/build-gate validation | 62 | 62 | 0 | 0 |
| PR-review objective/configuration and evidence-retention corrections | 25 | 25 | 0 | 0 |
| Complete focused A4 after review corrections | 61 | 61 | 0 | 0 |
| Relevant Phase 5B / Phase 6 / route / save / mana / validation regressions after review corrections | 617 | 617 | 0 | 0 |
| Full EditMode after review corrections | 1264 | 1264 | 0 | 0 |
| Full PlayMode after review corrections | 2763 | 2753 | 0 | 10 |
| Explicit production/configuration/localization/layout/build-gate validation after review corrections | 62 | 62 | 0 | 0 |
| Targeted startup/offline publication | 1 | 1 | 0 | 0 |
| Complete focused A4 | 49 | 49 | 0 | 0 |
| Initial relevant regression suite | 556 | 555 | 1 | 0 |
| Corrected A2 compatibility assertion | 1 | 1 | 0 | 0 |
| Initial full EditMode | 1252 | 1248 | 4 | 0 |
| Corrected canonical-run fixtures | 4 | 4 | 0 | 0 |
| Final full EditMode | 1252 | 1252 | 0 | 0 |
| Relevant regression cases in final EditMode | 556 | 556 | 0 | 0 |
| Initial full PlayMode | 2751 | 2733 | 8 | 10 |
| Corrected Phase Six config fixtures | 8 | 8 | 0 | 0 |
| PlayMode RunSimulationTests regression | 126 | 126 | 0 | 0 |
| Final full PlayMode | 2751 | 2741 | 0 | 10 |

The initial regression failure was a stale A2 expectation that any second Active floor must be rejected. A4 deliberately removes that limitation. The updated test preserves the Floor 1 compatibility-route check and verifies two Active floors are counted. The startup regression's earlier failure was a missing structural-economy fixture input; supplying the existing fixture authority resolved it without a production redesign.

The initial full EditMode failures were four older canonical-run fixtures that omitted loot configuration. They now use the existing Phase 5B service helper with production loot inputs; all four targeted cases pass. No production redesign was required.

The initial full PlayMode failures were eight tests sharing one legacy `BuildConfig` fixture that omitted the new required typed Phase Six configuration. The fixture now uses the validated A4 test configuration. The eight targeted cases, the 126-case containing regression class, and the final full PlayMode suite pass. The ten final skips after review corrections are unchanged from the prior qualified set: eight synchronous EditMode-only GameRoot fixtures, the non-Windows inverse filesystem check, and the Windows Player-only standalone qualification test.

Review 2 targeted coverage exercises an unreachable configured target, synthetic reachable depth, a source-state mutation after snapshot creation, an already-Active Activate All no-op, direct Activate's existing blocker, deactivation/reactivation, unchanged durable bytes and runtime contents, and SaveService's absence of a second publication. The current full PlayMode run's ten skips exactly match the preceding ten-skip set; no new skip appeared.

English localization parses with 968 entries, including 29 new keys and zero duplicate keys. Production run configuration JSON parses, typed Phase Six validation passes, and the combined production spatial build-gate, Bootstrap configuration, localization/string-table, and floor-layout validation selection passed 79/79 in the final EditMode run.

Final `git diff --check` passes. Git emits line-ending conversion advisories for existing working-copy settings but reports no whitespace errors.

The review 2 Windows x86_64 Development Build passed with wrapper exit 0 under Unity 6000.3.2f1. The Bootstrap-only `StandaloneWindows64` Development Build reported Success and 162.8 MB; its output tree is 170,912,558 bytes. Provenance SHA-256 is `7E8AE34B05555ED3BE545BFC72F675EF8EC577B782FECA8F0C2D5B00E78CA892`. The established missing Unity Cloud symbol-upload token warning did not fail the build. Unity-generated render-pipeline and ProjectSettings changes were removed from the PR working diff after the build.

The post-review Windows x86_64 Development Build passed with wrapper exit 0 under Unity 6000.3.2f1. `BuildWindowsDevelopment` produced a Development Build for `StandaloneWindows64` with only `Assets/_Project/Scenes/Bootstrap.unity`, result Success, a 162.8 MB Unity report / 170,911,938-byte output tree, 0 build errors, and the established warning that Unity Cloud native-symbol upload credentials are unavailable. The production spatial preprocessor/build gate passed. Output: `Builds/Development/Windows/Dungeon Lord.exe`; executable SHA-256 `ABC8179E345B70C9D7CA423C54E02739ED9B9ADCA668C2C745FFB81AADC05CC6`; provenance SHA-256 `A3E9B1507E47B3EC7DCF930549B5F084B9804AEB60AE8578D1E187D819F7E9AD`.

Owner Editor and Windows Development Build qualification passed after the correction. The documented Game-view diagnostic readability limitation remains accepted and non-blocking.

## Accepted limits and follow-up

Production progression remains Floors 1 and 2. No durable floor knowledge, richer aggregate multi-floor reporting, graphical editor expansion, migration, cross-floor pathfinding/backtracking, or between-floor persistence is included. A5 or an equivalent reviewed packet owns shared floor knowledge, richer explanation, final qualification, and Phase 6 closeout. No new tuning or persistence owner was invented to resolve a specification conflict.

## Changed-file inventory

Owner-local ProjectSettings/UnityConnectSettings.asset is excluded and was not edited or staged.

- `Assets/_Project/Data/Bootstrap/run_simulation_config.json`
- `Assets/_Project/Data/Bootstrap/string_table_en.json`
- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs`
- `Assets/_Project/Scripts/Core/GameRoot.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/ExactCompleteSaveAtomicPersistence.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorActivationEligibilityAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLifecycleAuthority.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLifecycleAuthority.cs.meta`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/RequiredFloorTraversal.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/RequiredFloorTraversal.cs.meta`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalMvpRouteProjection.cs`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalRunnableFloorProjection.cs`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalRunnableFloorProjection.cs.meta`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/MvpOrderedRoomRouteResolver.cs`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/MvpRoomSlotLayout.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs.meta`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionDecision.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionDecision.cs.meta`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseFiveBRouteProjection.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseSixRunConfig.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseSixRunConfig.cs.meta`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Branches.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Floors.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Floors.cs.meta`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.cs`
- `Assets/_Project/Scripts/Gameplay/RunSimulation/RunTransientEvidence.cs`
- `Assets/_Project/Scripts/Services/BootstrapConfigValidationService.cs`
- `Assets/_Project/Scripts/Services/MvpLoopSummaryPanelPresenter.cs`
- `Assets/_Project/Scripts/Services/MvpPlayableScreenPresenter.cs`
- `Assets/_Project/Scripts/Services/MvpRunResultFeedbackPresenter.cs`
- `Assets/_Project/Scripts/Services/RunPartyDiagnosticsPresenter.cs`
- `Assets/_Project/Scripts/Services/SaveService.cs`
- `Assets/_Project/Scripts/UI/BootstrapOverlay.cs`
- `Assets/_Project/Scripts/Util/Models.cs`
- `Assets/_Project/Tests/EditMode/Gd66CanonicalRuntimeProjectionTests.cs`
- `Assets/_Project/Tests/EditMode/PassiveOnlineManaTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA2FloorConstructionTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA3FloorActivationEligibilityTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA4Tests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixA4Tests.cs.meta`
- `Assets/_Project/Tests/EditMode/RunSimulationTests.cs`
- `Assets/_Project/Tests/EditMode/StructuralEconomyTests.cs`
- `Docs/28 - Save_Data_Model_Versioning_and_Migration.md`
- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`
- `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md`
- `Docs/testing/evidence/phase6a4-atomic-lifecycle-two-floor-runs/implementation-evidence.md`
- `Docs/testing/evidence/phase6a4-atomic-lifecycle-two-floor-runs/pr-description.md`
- `README.md`
- `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`
- `docs/planning/post-gd60-mvp-execution-plan.md`
