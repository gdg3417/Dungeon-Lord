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
