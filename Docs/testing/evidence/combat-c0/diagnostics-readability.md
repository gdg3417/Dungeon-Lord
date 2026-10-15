# C0 diagnostics readability correction — 2026-10-15

> **Later owner-approved scope correction (2026-10-15):** the raw play-by-play is developer evidence, not a player-facing C0 acceptance feature. Focused C0 paging and global TMP masking below were reverted after review; no new navigation UI is required. Numeric coordinate formatting, compact localized aliases and complete immutable records remain. This document's earlier qualification is historical, not proof of the later reversion. See [authoritative evidence and current qualification](authoritative-20261015/qualification.md) and the revised [owner acceptance plan](integration/owner-uat.md). C1 owns visible attacks, HP changes and graphical playback.

PR #238, existing branch `codex/combat-c0-deterministic-combat`, reviewed baseline `164fc6cb97bf48a9731a4e797858ff0a1e418c9b`. The owner's first Editor UAT loaded two existing floors and completed reciprocal C0 run `run-68`; that observation is owner evidence, not an automated gameplay acceptance claim. This correction addresses presentation only. Keep the PR draft and request external re-review before additional owner UAT.

## Display contract

`RunPartyDiagnosticsPresenter` explicitly formats nullable room-local tile coordinates with invariant culture as `(x,y)`; a missing endpoint is a localized dash. It no longer formats the `TileCoordinate` struct through its default object string representation.

Participants have run-wide aliases: `A<n>` uses the numeric member ordinal plus one; `M<n>` enumerates distinct nonmember canonical identities in ordinal string order. `E<n>` enumerates encounter identities in ordinal string order. No suffix-based shortening can conflate assignments on different floors. A legend maps each alias to its complete identity once. Existing member rows identify adventurer classes. All event rows retain their original chronological sequence; aliases do not reorder events. `CombatC0Event` and `CombatC0Diagnostics` retain complete canonical IDs unchanged.

Before (synthetic formatting fixture):

```text
Combat #7 (run step 12) floor.b/room/encounter/0, round 1: Attack / Legal; test.c0/member/0 → test.c0/monster/floor.b/same-suffix, HP 3 → 0, damage 3, DungeonBuilder.M0.Gameplay.DungeonSpatial.TileCoordinate → DungeonBuilder.M0.Gameplay.DungeonSpatial.TileCoordinate
```

After (synthetic formatting fixture; labels resolve from the localization table):

```text
A1 = test.c0/member/0
M1 = test.c0/monster/floor.b/same-suffix
E1 = floor.b/room/encounter/0
E1 R1 #7 A1 → M1: Attack: HP 3 → 0, damage 3; defeated
  (1,2) → (2,2)
```

Eligibility, skip, bypass, start and completion use localized kind/reason text, without fabricated zero-HP attacks. Coordinates occupy a separate logical row only when an endpoint exists. Start/bypass rows include floor/room context; bypass events with no encounter ID explicitly remain outside an encounter. Existing B1 member/hazard/branch/floor diagnostic strings are unchanged.

The focused C0 Run Diagnostics view now uses the existing four-logical-line paging and Page Up/Page Down/mouse-wheel controls. Historical focused diagnostics retain their previous behavior. The existing TMP overlay wraps at its safe-area width and masks overflow, preventing text from painting outside its rectangle. This is the existing overlay, not a new combat screen. Localization may wrap into additional visual lines; paging remains by logical row.

Replay eligibility is unchanged: `DungeonRunReplay.TryCreate` still rejects C0 before constructing playback. Its localized message explains C1 deferral and points to Run Diagnostics; historical outcomes without retained steps retain the saved-history explanation. No C1/C2 work, save/schema/checkpoint changes, simulation, geometry, tuning, settlement or reward change is included. Frozen B1 fixtures must not be regenerated.

## Qualification

Final runtime/test source commit: `27dc09b991e6354ce64735ea3a47e11279c3cce5` (initial presentation correction `2ef232b503d3969617be774d9096e6a5c3f07634`). All 11 new focused EditMode checks pass on identical Unity inputs. They cover invariant positive/negative/missing positions, compact deterministic attacks, HP/damage/defeat, same-suffix identity collisions, reason localization, exact B1 member formatting, encounter-free bypass, distinct replay explanations, focused paging and TMP line layout at 1024×768, 1280×720 and 1920×1080. Full EditMode on identical final Unity inputs: **1,990 passed, zero failed, one existing Windows-only inverse skip** (1,991 total; 33.4992316 seconds in NUnit). The existing skip is `CurrentNonWindowsRuntimeFailsClosed` / `gd66.test.windows_only_inverse`. The post-run checkpoint verifies 1,296 exact inputs with zero generated differences. Full PlayMode: **3,416 passed, zero failed, 10 skips** (3,426 total; 577.1051492 seconds in NUnit). Windows StandaloneWindows64 Development build succeeded: **zero errors, one existing native-symbol upload warning** (`UriFormatException`, empty URI). Report size: **181,626,411 bytes**. Bootstrap is the sole included scene. The player was not launched.

New project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm`. New disposable namespace: `combat-c0-diagnostics-validation-20261015.json`. The owner's existing integration project, C0 UAT save, original primary, backups and prior Windows player are preserved. The established preserving helper is wrapped by `Tools/Presentation/qualify_combat_c0_diagnostics.py`; outputs use `TestResults/combat-c0-diagnostics` and unique XML/log filenames. There are 1,296 exact Unity inputs. Strict-input helper tests: 12 passed.

Initial attempts are retained: one invocation before preparation finished ran no tests; an initial class-name filter selected zero Editor wrappers; the original scene assertion expected the retired `Combat #` display; a new wrapper initially referenced the wrong test namespace; new UI assertions initially skipped over the requested row and then attempted TMP layout without a Canvas/font. These were corrected without changing gameplay. Failed/empty runs are not passing qualification evidence.

Model recommendation: GPT-6.1 Sol High, Complex presentation integration and preserving qualification with established ownership. No substitution requested. Full qualification, Windows build and source/save preservation results will be recorded here before publication. Owner re-testing waits for external re-review; no gameplay acceptance or merge readiness is claimed.


### Validation environment findings

The first combined regression run stalled with native import-worker assertions (`pred(*previous, *i)`). Its logs and stopped-process ownership records were preserved. A retry and a fresh-project combined regression also failed to produce results and were stopped after preserving evidence. The first full EditMode attempt progressed into later tests but timed out at 300 seconds; it produced no NUnit results and is not passing evidence.

The TMP layout probe initially populated a shared dynamic fallback atlas. It now creates an in-memory font from the existing LiberationSans source, clears the displayed text and lets TMP_FontAsset.OnDestroy own texture/material cleanup. The final focused run passes all 11 checks; its strict-input checkpoint had zero generated differences. The font change did not fully resolve the combined-run stall; its exact cause remains unproven.

Production build-gate tests pass independently: 65 passed, zero failed/skipped. The complementary EditMode group retains all other tests; the collector verifies disjointness and exact union coverage against the preserved historical full-suite inventory plus the 11 new focused cases. Partitioned results will be labeled explicitly; no timed-out run is counted as passing, and no assertion, parser limit, B1 fixture or production gate is relaxed.


### Final test-fixture and environment contract

The layout probe uses the existing persistent static SDF font and pre-baked ASCII glyphs, including a long unbroken identity label. It neither creates/destroys fonts nor populates the shared dynamic fallback atlas. Exact Unicode arrows/dashes and invariant numeric strings are tested separately by the formatter tests. This avoids coupling native deferred font work to a synchronous EditMode probe; no runtime font or combat behavior changes.

Qualification uses `-c0DiagnosticsSerialImports`, an opt-in Editor-test-only hook guarded to the two diagnostics disposable directory names. It requests zero asset-import workers using Unity's EditorUserSettings API, retains the original value across domain reloads with SessionState and restores it on normal exit. Forced termination cannot run that callback; any remaining setting is confined to the disposable project's user settings. No production/default project setting, source input, gameplay limit or parser/save contract is changed. The [Unity API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUserSettings-desiredImportWorkerCount.html) defines the worker-count setting.

The cold-project attempts and the zero-worker profile alone did not complete broad qualification. Windows denied renaming a locked failed Library; it was left intact. A closed qualified project's Library was subsequently copied read-only into a separate warm-cache disposable. SourceAssetDB SHA-256 before/after the copy matches, recorded in `warm-cache-copy.json`; the owner project, saves, builds and logs were not moved. Exact source comparison and inclusion of the new diagnostic cases verify current code rather than stale compiled cache contents. Only the final static-font full-suite report is passing evidence; its scope includes the 65 production gates and all 11 new cases. The precise cause of earlier native/interrupted runs is not asserted as proven.


### Final broad-suite results

The final static-font single-process full EditMode run completed successfully; no partition is used for its passing result. Complete historical case comparison: zero removed, zero changed existing results or skip reasons, 11 added passing cases. Full PlayMode also passes. A separate 65-case production gate run passed, and those cases remain included in the full EditMode result. Source checkpoints after full EditMode and PlayMode match all 1,296 inputs; PlayMode's two known generated differences were archived before exact restoration.

The earlier font/native/cache attempts remain failed or incomplete evidence. Neither requesting zero workers nor copying the cache alone completed them; no single underlying engine cause is asserted. The final probe uses only existing static glyphs, and exact Unicode output remains covered by the formatter tests. The qualification hook affects only Editor user settings under the explicit flag/project-name guard; it is absent from the player and introduces no production gameplay/configuration authority.


### Reproduction commands

Windows 11 x64; Unity 6000.3.2f1 (a9779f353c9b); Unity CLI 1.0.0-beta.12. `R` below is `C:/Dev/Dungeon-Lord`; `P` is `R/Temp/ui-composition-combat-c0-diagnostics-validation-warm`. Use the installed Unity CLI and bundled Python, as in the initial integration qualification.

```text
python Tools/Presentation/qualify_combat_c0_diagnostics.py prepare
python Tools/Presentation/qualify_combat_c0_diagnostics.py refresh
python Tools/Presentation/qualify_combat_c0_diagnostics.py check
unity test P --mode EditMode --filter CombatC0Diagnostics --output R/TestResults/combat-c0-diagnostics-focused-static.xml --timeout 600 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c0-diagnostics-focused-static.log
unity test P --mode EditMode --filter ProductionSpatialContentBuildGateTests --output R/TestResults/combat-c0-diagnostics-build-gates.xml --timeout 300 --no-color -- -logFile R/Temp/combat-c0-diagnostics-build-gates.log
unity test P --mode EditMode --output R/TestResults/combat-c0-diagnostics-full-editmode-static.xml --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c0-diagnostics-full-editmode-static.log
unity test P --mode PlayMode --output R/TestResults/combat-c0-diagnostics-full-playmode-static.xml --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c0-diagnostics-full-playmode-static.log
unity run P --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC0IntegrationWindows -logFile R/Temp/combat-c0-diagnostics-build.log
python -m unittest test_qualification_inputs.py  (cwd R/Tools/Presentation; 12 passed)
python Tools/Presentation/qualify_combat_c0_diagnostics.py checkpoint full-editmode-static
python Tools/Presentation/qualify_combat_c0_diagnostics.py checkpoint full-playmode-static
```

Earlier XML/log filenames remain separate. The successful filtered run followed full-suite qualification on identical inputs. The direct gate run used the r2 disposable before the warm-cache change; its 65 cases are also re-executed in the final full suite. No frozen expected B1 checkpoint ZIP is regenerated.

### Review and subsequent owner validation

External re-review is required before any additional owner UAT. Keep PR #238 draft and unmerged. After review, refresh an isolated owner project with the corrected source while preserving the existing C0 namespace and run-68 evidence. The new qualification project/player uses its own disposable namespace and is not automatically seeded with the owner's dungeon.

Future owner checks: open Bootstrap, complete a disposable manual run, open Run Diagnostics, use Page Up/Page Down or the mouse wheel to inspect the one-time ID legends and subsequent encounter/round rows. Verify numeric `(x,y)` room-local endpoints, HP before/after, damage, defeat and eligibility/completion reasons. Check multiple defenders and floors, including long IDs. Capture the run ID and representative diagnostic rows before closing because detailed evidence is transient. Confirm new C0 replay is disabled with its C1 explanation; after reopen, saved history without retained evidence uses the historical explanation. Do not enable C0 graphical replay. No further owner UAT or gameplay acceptance is claimed by this correction.


### Artifacts and compatibility

Final reports, redacted logs, skip reasons, interrupted attempts and source checkpoints are collected under [diagnostics-20261015](diagnostics-20261015/report-summary.json). The Windows artifact manifest records every output hash, byte count, packed namespace and executable path. The build keeps its own namespace, `combat-c0-diagnostics-validation-20261015.json`; the owner's integration namespace is preserved. No original primary save, recovery sidecar, prior player or UAT evidence is reset. The supported fixture restore operation applies only to the 22 backed-up static disposable test files.

Actual combat resolution, HP, damage, initiative, targeting, geometry/LOS, grouping, B1/B2 executable behavior, checkpoint formats, writable schema 14 and economy/settlement have zero implementation changes. Complete canonical IDs and chronological combat evidence are retained. The frozen B1 checkpoint ZIP is byte-unchanged. C0 graphical replay remains rejected by the original eligibility guard; C1/C2 remain separate work.

No additional owner gameplay run or standalone launch occurred during this correction. Automated tests use isolated saves, including normal scripted run execution inside tests. External re-review is required before further owner Editor UAT; this is not gameplay acceptance or permission to merge.


### Preservation and final artifact result

The final audit verified **14,138 pre-existing save/build files unchanged**. Twenty-two established static disposable fixtures were restored from their byte-verified backups; modified test copies were retained. The original primary SHA-256 remains `39224B12446EC30990D043385E77634E45CC7DA1DC5E50E75237B33F25C79DF2`. The owner C0 save, original recovery files, old players and earlier UAT evidence are included in the unchanged inventory. Final comparisons against the initial integration qualification show zero removed or changed historical results/skip reasons in both suites, and 11 added passing cases in each.

The final focused static-font run is **11 passed / 0 failed / 0 skipped**. The player artifact comprises **303 files / 182,099,951 bytes**; build report size is **181,626,411 bytes**. Its disposable namespace is verified in packed assets. All 52 required artwork dependencies passed the existing packed-art build gate. Executable: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c0-integration/Dungeon Lord.exe`. It was not launched.

```text
python Tools/Presentation/qualify_combat_c0_diagnostics.py checkpoint windows-build
python Tools/Presentation/qualify_combat_c0_diagnostics.py restore-fixtures  (outside sandbox; static test fixtures only)
python Tools/Presentation/qualify_combat_c0_diagnostics.py audit
python Tools/Presentation/collect_combat_c0_diagnostics_evidence.py
git diff --check
```

No blocking assertion or source-preservation finding remains on the final qualified inputs. Earlier native/import/font/cache interruptions remain documented qualification history and do not count as passes. The final successful reports use the explicitly documented test-only import profile and static-font probe; default-profile batch reimport stability is not newly qualified. Qualitative readability, Editor owner testing of the corrected rows and standalone gameplay parity remain subsequent owner work after external re-review. Keep the PR draft/unmerged.
