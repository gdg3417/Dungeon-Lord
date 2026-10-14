# PR #236 visual interaction correction qualification

Reviewed parent: `ff9f0a7ed9b0cc3efea793bab53e2615df075f40`. Refreshed `origin/main` remains `fc281885f68f0ff15be4f3ee9bca9205b3cc6c59`. Existing branch: `codex/phase7-visual-run-replay`. Recommendation: GPT-6.1 Sol / High, Complex (spatial feedback, evidence identity and lifecycle integration); no availability fallback invoked.

Status: required automated qualification passed. Ready for independent re-review and owner targeted Editor/Windows retest. No owner gameplay acceptance or merge is claimed.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused replay/content/gate EditMode | 258 | 0 | 0 |
| Affected production Dungeon PlayMode | 53 | 0 | 0 |
| Full EditMode, final Unity source | 1,897 | 0 | 1 existing |
| Full PlayMode, final Unity source | 3,324 | 0 | 10 existing |
| Production build gates, included in full EditMode | 65 | 0 | 0 |

Both full suites use the final Unity source, including the subsequent pooled sorting reset and active-replay scope check. Each comparison against the reviewed parent's full report adds 10 passing cases, removes none and changes no existing outcome or skip reason. Exact NUnit reports are in `qualification-reports.zip`; exact failure/skip messages and kernel outputs are in `report-summary.json`. `baseline-*.json` retain leaf-result comparisons. Frozen Phase A fixtures/projectors and migrations remain unchanged. The unchanged exact-input helper's earlier 12/12 qualification is reused; it is not counted as a newly executed suite.

EditMode's skip remains `PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed` / `gd66.test.windows_only_inverse`. PlayMode retains eight synchronous EditMode-fixture skips, that inverse-platform skip and the Windows-player-only preflight skip. Every new correction case passes. Final full-suite/build logs contain zero C# compiler errors or warnings and no new UXML/USS parse diagnostics. Existing package minimum-version overrides and ordinary licensing/test-runner diagnostics are retained in redacted logs; package manifests/locks are unchanged.

Windows **StandaloneWindows64 Development Build succeeded**, Unity **6000.3.2f1**, Bootstrap only: **181,528,263 report bytes**, **0 errors / 1 warning**. The warning is the established Unity services native-symbol upload failure due to an empty URI. The complete **303-file / 182,000,858-byte** output inventory verifies all **52 required art assets** and the existing isolated save filename packed in `sharedassets0.assets`. Player not launched. Build reports, packed-asset lists and hashes are retained here.

## Corrections and evidence provenance

The first focused run passed 19/23: the longer legend pushed a control outside the viewport, and three exact floating-point start-position assertions encountered carried presentation-time fractions. The legend moved into existing scrollable Details; bounded scrolling also protects descriptions/controls. Animation comparisons now use an explicit small test tolerance, while canonical projected path-point assertions remain exact. The second run passed 22/23; its damage expectation incorrectly ignored existing trap mitigation. The third passed 22/24; an explicit mitigation fixture used a coefficient outside the supported range. It now uses valid coefficient/expertise values of one and records positive unmitigated damage with zero applied damage. Production validation, simulation damage and party rules were not weakened. All original failed XML reports remain archived.

Intermediate captures are preserved locally in `C:/Dev/Dungeon-Lord/TestResults/visual-feedback/screenshots-focused03.zip`, with a SHA-256 manifest in the support archive. Original parent screenshots and qualification evidence in `../phase7-visual-run-replay/` remain unchanged. The new screenshot archive retains 45 actual Unity captures across **1920×1080, 1280×720 and 720×1280**: recorded approach, sequential engagements, party damage, trap trigger/mitigation, loot, resolved encounters, casualty and completion. Large-text control bounds are also tested. Casualty/mitigation captures are explicit synthetic engine fixtures with valid test config, not owner gameplay or production tuning changes.

The measured pure cursor example is **14 recorded steps / 528 display updates / 0.0313 ms** in the final full EditMode report, excluding projection construction, views, UI and rendering. Scene tests verify stable pooled view counts across replay/floor/edit/restart/Close. Default bounds are 8,192 steps/total approach points and 256 interaction/content markers. This does not claim physical-mobile performance or allocation-free whole frames.

## Isolation and preservation

Qualification project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-validation`; disposable namespace `phase7-visual-feedback-validation.json`. Owner project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat`; existing namespace `dungeon-lord-visual-run-manual-uat-20261014.json`.

The final audit verifies **all 13,228 preexisting save/build files unchanged**, including current owner UAT progress and the previous Phase 7 player. Exactly 22 established disposable test-fixture files were restored from verified backups after archiving their test-modified bytes; owner saves are excluded from that operation. All **33 existing non-fixture save/backup files** remain byte-identical to this task's starting state. The existing UAT save was not recopied, reset, migrated or opened. This is a file audit, not a whole-registry/settings claim.

Before owner-project refresh, 15 changed input files were archived. Both projects finish with **1,266 exact Unity/content inputs, zero mismatches and zero unexpected files**, under established namespace/text normalization. The four build-generated differences (URP prefilter settings, URP runtime settings, batching/application-ID ordering and UnityConnect state) were inspected, archived and refreshed only in the disposable owner project. The qualification project's fallback-font serialization upgrade and application-ID ordering were likewise archived/refreshed after both suites. Support archives contain input inventories, hashes, generated-input archives and 17 redacted logs, not owner save contents. Archive scanning found zero unredacted access-token/JWT patterns.

Updated player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat/Builds/Development/Windows-visual-run-feedback/Dungeon Lord.exe`. Earlier players, backups and original evidence remain preserved.

Implementation and unsupported-evidence limits: [implementation.md](implementation.md). Owner targeted Editor retest and outstanding Windows manual qualification: [owner-uat.md](owner-uat.md). No monster HP/death, tactical combat, simultaneous attacks, individual member motion, durable replay, automatic arrivals, B3A or offline simulation is introduced. Independent re-review and owner acceptance remain separate judgments.
