## Baseline and objective

Baseline: merged PR #219 on `main`, `5e8eeadbdd8c4c7c22309d1e709e7603a85dbdd8`. This is the **final planned Phase 6 implementation packet**: correct schema-12 floor-knowledge validation, use applicable pre-run shared floor knowledge in later transitions, explain transition outcomes, and report aggregate multi-floor results. Implementation, automated qualification, and owner Editor/Windows standalone UAT passed. No known blocking Phase 6 finding remains. PR #220 awaits final external merge review; Phase 6 closes when it is merged. Do not merge as part of this documentation reconciliation.

## Knowledge and transition authority

For a known reward and independently for a known danger, `EffectiveConfidence = clamp(SharedConfidence * Party.IntelligenceInterpretationFactor, 0, 1)`. A known component contributes uncertainty `1 - EffectiveConfidence`; an unknown component contributes `1`. Overall transition uncertainty is the mean of the two component uncertainties. Applicable stored perceived reward/danger scores pass unchanged to the locked A0/A4 decision: **confidence affects uncertainty, not the scores**. The record must match the actual next floor's A5A applicability fingerprint in the immutable pre-run snapshot. Missing, stale, or unusable knowledge is fully unknown (uncertainty 1 under current tuning), with no hidden live floor read. Learning in a run cannot change that run's earlier or later transition inputs. Trap expertise is intentionally excluded from floor-level interpretation; Phase 5B branch interpretation remains unchanged. No transition weights, thresholds, normalization, objective rules, identity, roll, or comparison semantics were retuned.

The unresolved PR #219 P1 review thread identified that contextual complete-save validation limited durable knowledge records using current gameplay `MaximumActiveFloors`. That could reject valid historical Floor 2 knowledge when only Floor 1 is Active or a rollout lowers the active limit. Contextual validation now uses the existing `PhaseSixRunConfigValidation.MaximumSupportedActiveFloors` fixed five-floor ceiling. A valid one-Active-floor config may retain an unavailable target-depth objective with zero selection weight; production tuning is unchanged. The regression validates and reopens a schema-12 save with Floor 1 and Floor 2 records, proves one-floor simulation, and checks the fixed ceiling.

## Save and reporting boundary

Writable schema remains **12**. No schema 13, A5B migration, new save owner, or durable detailed-cause field was added. A5A's survivor-gated learning, complete-run atomic publication, stale-session/concurrent-byte protection, canonical ordering, unknown extensions, and readback/reopen checks remain authoritative. Existing durable run history supports coarse floor reach, rooms, survivors/deaths, loot, and final route outcome after reopen. Detailed causal transition evidence is same-session only; the UI does not reconstruct a historical cause from incomplete durable data.

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
README.md
docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md
docs/planning/post-gd60-mvp-execution-plan.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md
Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/pr-description.md
```

## Qualification and limits

### PR #220 review correction

The qualification correction adds three deterministic A5B tests, with no runtime, configuration, localization, or presentation code change. Fixed `run-10` uses the locked production Phase 6 tuning, same generated party, current/next floor, full carried-loot reference, and selected target-depth pull. Fully unknown perception gives `exit_marginal`; applicable knowledge with stored reward at the configured reference, danger zero, and initial confidence gives `descend_appeal`. Expected survivability and appeal increase; repeated known inputs give identical evidence. No ForceDescent or altered balance is used. A direct configuration test proves an in-bound positive target valid, an out-of-bound positive target invalid, and an out-of-bound zero-weight target valid and never selected across fixed RunIds; production target and weights remain unchanged. This is disabled-objective configuration validity, documented in the design lock. A separate fixed-ceiling test uses six distinct, otherwise valid floor-knowledge records with matching spatial identities: five pass at ceiling five, six pass at test ceiling six, and six fail at fixed ceiling five. The one-Active-floor schema-12 save/reopen and runtime-limit regression remains.

Existing A4 tests already cover unknown hidden-content isolation, immutable snapshots, objective and transition identities, threshold/marginal semantics, lifecycle/Active-floor behavior, party transfer, wipe/retreat, and transient evidence after reopen. Existing A5A tests cover fingerprint stability and invalidation, survivor-gated learning, pre-run snapshot, canonical records, wipe behavior, atomic settlement, stale-session rejection, and concurrent-byte failure. The [implementation evidence](Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md) maps named tests to each final acceptance contract. The [revised owner UAT](Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md) supplies executable Editor/standalone steps and all six locked aggregate comparisons, with automated coverage named for internal conditions that have no manual selector.

Correction rerun commands use `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe test C:/Dev/Dungeon-Lord` as prefix:

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

An attempted `--mode EditMode --filter MvpRouteResultPresenter` selected zero tests and is not counted. The reporting selection and full suites cover localization and presentation. The ten PlayMode skips are the established eight EditMode-only GameRoot fixtures, one non-Windows inverse filesystem case, and one Windows Player-only standalone case; no new skip was added. The Windows Development Build was **not rerun** because the review correction changed tests and docs only; the prior successful A5B build result above remains. Correction changed-file inventory since reviewed `2a6c334c026935e3495421611571a16961c284c4`: `Assets/_Project/Tests/EditMode/PhaseSixA5BTests.cs`; `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`; `Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/implementation-evidence.md`, `manual-uat.md`, and `pr-description.md`. Owner-controlled `ProjectSettings/UnityConnectSettings.asset` remains excluded.

### Owner qualification and closeout status

The owner reported **Editor PASS** and **Windows standalone PASS**. An existing valid two-floor schema-12 save loaded durable Floor 2 knowledge; its applicable Floor 1→2 transition reported reward/danger known with an understandable localized reason and no raw hash, roll, localization key, or reason code. A material Goblin change made the stored record inapplicable: the next run reported reward/danger unknown, descended, and completed the changed floor. A surviving completion relearned it; the following run reported both known again. Observed sequence: **known → material edit → unknown → surviving completion → known**. A survivability-based EXIT had a distinct understandable cause. Deactivation prevented Floor 2 entry and made Floor 1 terminal; reactivation without content edits preserved knowledge, and lifecycle persistence/reopen passed.

The owner completed repeated-run Editor comparisons for Floor 2 Inactive/Active, dangerous Floor 1/easier Floor 2, easier Floor 1/dangerous Floor 2, loot-heavy Floor 1, deeper continuation, and player-controlled design changes. Outcomes varied reasonably across parties and configurations; controlled changes gave understandable directional differences and consistent explanations. This is a **qualitative aggregate PASS**; individual per-run tallies were not retained because manual recording was impractical, and no fixed DESCEND/EXIT count was required. Exact identities, objectives, marginal boundaries, intelligence interpretation, absent-knowledge behavior, and perception-driven decision changes remain qualified by automated A4/A5B tests. Normal Editor and 1280×720 presentation passed. The owner observed no new gameplay-related Console errors or unexpected gameplay-related warnings; no claim is made about unrelated established Unity warnings.

The planned Dev Panel **Delete Save** fresh baseline was intentionally not executed because it would remove the research progression needed to unlock/build Floor 2. Manual unknown-state presentation passed through the material-change stale-fingerprint case; the separate truly absent/no-record case remains covered automatically without hidden-state inspection. No save files were manually edited. This deviation is not a failed acceptance criterion.

The [owner UAT record](Docs/testing/evidence/phase6a5b-knowledge-backed-transitions-closeout/manual-uat.md) contains the actual results and retained procedure. Windows standalone used the accepted Development Build from `2a6c334c026935e3495421611571a16961c284c4`; later changes were tests/docs only, so the executable is runtime-equivalent and is not claimed to be built from final PR HEAD. This final documentation reconciliation required no EditMode, PlayMode, or Windows build rerun. Writable schema remains **12**, no A5B migration was added, and detailed transition cause remains transient. Production gameplay remains Floors 1–2. Deferred non-goals: Floors 3–5 production content; schema 13; persistent detailed transition causes, expeditions, checkpoints, or concurrent parties; cross-floor edges/backtracking/stair effects; floor-level trap expertise; new balance/intelligence factors; Phase 7 editor/UI; broader content, economy, research, or analytics redesign. Pre-existing local Unity asset and ProjectSettings differences were not staged. PR #220 remains open for final external merge review; Phase 6 closes on merge.
