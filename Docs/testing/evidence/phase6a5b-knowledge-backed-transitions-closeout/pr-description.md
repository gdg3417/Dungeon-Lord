## Baseline and objective

Baseline: merged PR #219 on `main`, `5e8eeadbdd8c4c7c22309d1e709e7603a85dbdd8`. This is the **final planned Phase 6 implementation packet**: correct schema-12 floor-knowledge validation, use applicable pre-run shared floor knowledge in later transitions, explain transition outcomes, and report aggregate multi-floor results. Phase 6 remains open pending external review and owner Editor/Windows qualification. Do not merge before qualification.

## Knowledge and transition authority

For a known reward and independently for a known danger, `EffectiveConfidence = clamp(SharedConfidence * Party.IntelligenceInterpretationFactor, 0, 1)`. A known component contributes uncertainty `1 - EffectiveConfidence`; an unknown component contributes `1`. Overall transition uncertainty is the mean of the two component uncertainties. Applicable stored perceived reward/danger scores pass unchanged to the locked A0/A4 decision: **confidence affects uncertainty, not the scores**. The record must match the actual next floor's A5A applicability fingerprint in the immutable pre-run snapshot. Missing, stale, or unusable knowledge is fully unknown (uncertainty 1 under current tuning), with no hidden live floor read. Learning in a run cannot change that run's earlier or later transition inputs. Trap expertise is intentionally excluded from floor-level interpretation; Phase 5B branch interpretation remains unchanged. No transition weights, thresholds, normalization, objective rules, identity, roll, or comparison semantics were retuned.

The unresolved PR #219 P1 review thread identified that contextual complete-save validation limited durable knowledge records using current gameplay `MaximumActiveFloors`. That could reject valid historical Floor 2 knowledge when only Floor 1 is Active or a rollout lowers the active limit. Contextual validation now uses the existing `PhaseSixRunConfigValidation.MaximumSupportedActiveFloors` fixed five-floor ceiling. A valid one-Active-floor config may retain an unavailable target-depth objective with zero selection weight; production tuning is unchanged. The regression validates and reopens a schema-12 save with Floor 1 and Floor 2 records, proves one-floor simulation, and checks the fixed ceiling.

## Save and reporting boundary

Writable schema remains **12**. No schema 13, migration, new save owner, or durable detailed-cause field was added. A5A's survivor-gated learning, complete-run atomic publication, stale-session/concurrent-byte protection, canonical ordering, unknown extensions, and readback/reopen checks remain authoritative. Existing durable run history supports coarse floor reach, rooms, survivors/deaths, loot, and final route outcome after reopen. Detailed causal transition evidence is same-session only; the UI does not reconstruct a historical cause from incomplete durable data.

The existing MVP result presenters now show reached floor, major route outcome and attrition/loot context, and a localized cause when transient transition evidence exists. Reason categories distinguish clear descent, survivability refusal, clear exit, marginal descent/exit, and final Active-floor completion. Player text uses the canonical English string table; normal output omits raw hashes, rolls, keys, and internal reason codes. The terminal result does not describe nonexistent next-floor knowledge. No large analytics screen or Phase 7 UI was added.

## Automated validation

All commands used `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe` and project `C:/Dev/Dungeon-Lord`:

| Command after executable | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseSixA5B --output TestResults/phase6a5b-focused.xml --timeout 600 --no-color` | 5 | 5 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseSixA --output TestResults/phase6a5b-phasesix.xml --timeout 600 --no-color` | 219 | 219 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseFiveB --output TestResults/phase6a5b-phasefiveb.xml --timeout 600 --no-color` | 107 | 107 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode EditMode --filter Detached --output TestResults/phase6a5b-detached.xml --timeout 600 --no-color` | 5 | 5 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseFour --output TestResults/phase6a5b-phasefour-save.xml --timeout 600 --no-color` | 735 | 735 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode PlayMode --filter BootstrapOverlayPagingTests --output TestResults/phase6a5b-overlay-focused.xml --timeout 600 --no-color` | 89 | 89 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode EditMode --output TestResults/phase6a5b-final-editmode.xml --timeout 1200 --no-color` | 1298 | 1298 | 0 | 0 |
| `test C:/Dev/Dungeon-Lord --mode PlayMode --output TestResults/phase6a5b-final-playmode.xml --timeout 1200 --no-color` | 2797 | 2787 | 0 | 10 |

The first full PlayMode pass had one one-floor fake-localization fixture failure after the new reach line was introduced. The fixture's map now includes that canonical key; the affected 89-case selection and the final full PlayMode pass. No test was skipped or suppressed to resolve the failure. The ten final skips match the established set: eight synchronous EditMode-only GameRoot fixtures, one non-Windows inverse check, and one Windows Player-only standalone test. `git diff --check` passes; Git prints only line-ending conversion advisories.

Windows x86_64 Development Build command: `unity.exe build C:/Dev/Dungeon-Lord --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --log-file C:/Dev/Dungeon-Lord/TestResults/phase6a5b-windows-development-build.log --provenance-path C:/Dev/Dungeon-Lord/Builds/Development/Windows/build-provenance.json --allow-dirty-build --no-tail`. Result: wrapper exit 0; Unity 6000.3.2f1 Succeeded, Development Build, Bootstrap scene only, 170,696,236 reported bytes, 0 build errors, 1 established Unity Cloud symbol-upload credential warning. Output: `Builds/Development/Windows/Dungeon Lord.exe`.

## Changed-file inventory

```
Assets/_Project/Data/Bootstrap/string_table_en.json
Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs
Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs
Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionDecision.cs
Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionPerceptionResolver.cs
Assets/_Project/Scripts/Gameplay/RunSimulation/FloorTransitionPerceptionResolver.cs.meta
Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseSixRunConfig.cs
Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.cs
Assets/_Project/Scripts/Services/MvpPlayerLoopSummaryPresenter.cs
Assets/_Project/Scripts/Services/MvpRouteResultPresenter.cs
Assets/_Project/Scripts/Util/Models.cs
Assets/_Project/Tests/EditMode/BootstrapOverlayPagingTests.cs
Assets/_Project/Tests/EditMode/PhaseSixA5BTests.cs
Assets/_Project/Tests/EditMode/PhaseSixA5BTests.cs.meta
docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md
docs/planning/post-gd60-mvp-execution-plan.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/pr-description.md
```

## Qualification and limits

The [sequential owner UAT](Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md) is prepared; it has **not** yet been executed or signed off by the owner. External review and Editor/Windows standalone manual qualification remain. Production gameplay remains Floors 1–2. Deferred non-goals: Floors 3–5 production content; schema 13 or migration; persistent detailed transition causes, expeditions, checkpoints, or concurrent parties; cross-floor edges/backtracking/stair effects; floor-level trap expertise; new balance/intelligence factors; Phase 7 editor/UI; broader content, economy, research, or analytics redesign. Unrelated Unity-generated render-pipeline and project changes were removed from the PR diff. Owner-controlled `ProjectSettings/UnityConnectSettings.asset` was neither restored nor staged, and unrelated TMP/font differences were not touched.
