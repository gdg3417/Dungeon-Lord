# PR 231 compact positioning and grid contrast correction

Starting local/remote PR HEAD: `163c32fdbfc7aad9b844a0997fef0b2b83f1c307`. Fetched main: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch `codex/phase7-graphical-room-content-management`; initial working tree clean. Existing PR #231 remains open, not merged. No new PR or imported Unity project is created.

Configuration: GPT-6.1 Sol, High; Standard focused UI correction. Retain the owner's suggested/current High for multi-state measured layout and real-scene rendering integration. Standard policy default is Medium; the explicit High recommendation is retained for this correction matrix. Availability fallback: GPT-6 Sol, otherwise GPT-5.6 Sol at equivalent appropriate effort. No substitution. AGENTS.md, canonical model policy, A0 bottom-sheet/content/positioning locks, mobile UI vision, current production layout/policy/catalog, scene tests and prior qualification were inspected.

## Cause and production changes

The preceding correction imposed `height: 50%` in portrait, `height: 58%` in landscape, and `flex-grow: 1` on the information ScrollView. A short Move instruction therefore occupied a predetermined fraction of the screen and stretched its unused information area. That reduced the map until positioning ended.

The fixed heights and expanding information rule are removed. Existing zero-grow, shrinkable information determines natural sheet height; portrait is capped at 54%, landscape positioning at 46%. These are upper limits, not requested heights. Long information scrolls within the remaining space, above nonshrinking Cancel/Confirm and Save/Discard. Category header remains visible. During landscape positioning, the HUD and navigation use separate full-width rows, compact card/label vertical spacing, and a nonwrapping Save/Discard group beside the draft summary. This avoids narrow Large-text wrapping consuming the map. Placement information drops redundant vertical label padding. These styles apply only while positioning content. UXML, controller behavior, accepted New/Owned browsing, normal inspection, structural construction and the deferred right inspector are unchanged. No per-frame measuring loop is added.

Grid color was coupled to blue-gray `FloorBoundaryColor`. A single narrowly scoped `RoomPlacementGridColor` field now belongs to the typed `DungeonPresentationPolicy`, authored in `Presentation.asset` as opaque muted gold `(0.86, 0.79, 0.57)`. `DungeonFloorWorldView` uses it for the existing cached hollow tile. Floor boundaries, selection, valid/invalid colors, Visuals mappings, transparent interior, layers, line width, exact transformed cells and allocation lifetime remain unchanged. No art pipeline/theme framework or gameplay tuning is introduced.

Only layout, presentation color, regression tests, editor-only correction build destination and evidence tooling change. Save schema **13**, draft format **5**, record version **4**, identity, custody, pricing/restrictions, capacities, occupancy, atomic commit, recovery and current runs are unchanged.

## Qualification status

Frozen implementation: `834bf3b7031caa8f17bd99d17516ffde3692cc45`. Later documentation/evidence commits retain these exact four Unity input trees.

Focused development failures are retained in raw XML. They are not passing qualification. Original non-overlap, transactional, recovery, identity/custody, renderer reconstruction and input assertions are retained.

| Focused run | Mode | Passed / failed | Disposition |
|---|---|---|---|
| 01 | EditMode | 2 / 2 | Compactness measurement omitted resolved outer label margins; corrected accounting. |
| 02 | EditMode | 2 / 2 | Revealed actual Large-text map pressure; bounded the landscape sheet and calibrated the new minimum usable-map assertion. |
| 03 | EditMode | 2 / 2 | Second-room fixture needed durable acknowledgement before committing; narrow Large-text viewport still required layout correction. |
| 04–05 | EditMode | 2 / 0 each | Intermediate passes. Screenshot review found insufficient visible information despite positive dimensions; strengthened the new assertion to two complete rendered lines. |
| 06 | PlayMode production | 41 / 2 | All previous production cases passed. Two new cases exposed insufficient information height; reduced redundant placement label padding. |
| 07 | PlayMode | 2 / 2 | Readable information passed; narrow Large-text top header consumed too much map. |
| 08 | PlayMode | 2 / 2 | Wrapped navigation attempt enlarged the header and squeezed information; replaced with full-width HUD/navigation rows. |
| 09–10 | PlayMode | 2 / 2 each | Remaining narrow Large-text map shortage; compacted placement-only HUD/card/label spacing. Run 10 was nine pixels below the retained map threshold. |
| 11 | PlayMode | 4 / 0 | Final focused compactness, overflow, grid/multicell and existing measured-layout cases pass; all six refreshed screenshots reviewed before full qualification. |

The final regression matrix covers Move instructions, valid/invalid Move, confirmed reposition, New monster/trap/loot, Owned redeployment, explicit Reassign, a test-local 20-line translation expansion, cancel/confirm transitions, Save/Discard after positioning, repeated selection/focus and canonical byte/wallet preservation. It exercises 720×1280, 1920×1080 and 1024×600 with Small/Default/Large text and both empty and purchase-bearing drafts. No test-only layout overrides exist. Every layout sample asserts no filler gap, two complete information lines, actual action hit reachability, non-overlap and at least one-quarter usable map height. Overflow is explicitly scrolled and tested. Grid tests retain rectangular orientation alignment, transparent interiors, layer order, target/cancel/floor cleanup and valid→invalid→valid multi-cell transitions.

Final full EditMode: **1,669 total, 1,668 passed, zero failed, one skipped**. Final full PlayMode: **3,100 total, 3,090 passed, zero failed, ten skipped**. Against the preceding qualified PR HEAD, no old case or outcome is removed/changed; two cases are added in each mode. The one EditMode skip is the non-Windows inverse on Windows. PlayMode skips retain eight synchronous EditMode fixtures, the same non-Windows inverse, and one Windows-player-only filesystem preflight. They are established execution-surface exclusions, not new regressions; exact names/reasons are retained in [report summary](report-summary.json). Standalone owner runtime UAT remains required. Final test/build logs contain no C# compiler errors or warnings.

[Thirteen original XML reports](qualification-reports.zip), [redacted Unity logs](unity-logs-redacted.zip), [unchanged-outcome comparison](test-outcome-comparison.json) and [log index](log-index.json) retain focused failures, final totals and skips. Full suites were run once after the implementation stabilized; no input-root changes followed the freeze.

### Actual measured layout and screenshot review

[All 378 final PlayMode geometry samples](measured-layout.json) span the complete placement matrix. Maximum unused information-region space is **zero panel units**. Minimum usable map height across all states is **25.5%** of screen height. Negative blank-space values mean bounded information scrolls, not a reserved empty area. Pixel values below apply the actual panel/native scale; each short-instruction row covers all three assigned categories and both draft-cost states.

| Resolution | Text | Short Move sheet height (px) | Minimum Move map height (px) |
|---|---|---:|---:|
| 720×1280 | Small | 338 | 767 |
| 720×1280 | Default | 382 | 684 |
| 720×1280 | Large | 420 | 599 |
| 1920×1080 | Small | 295 | 605 |
| 1920×1080 | Default | 324 | 555 |
| 1920×1080 | Large | 430 | 412 |
| 1024×600 | Small | 244 | 221 |
| 1024×600 | Default | 270 | 180 |
| 1024×600 | Large | 276 | 153 |

Narrow Large text reaches the content ceiling; its information scrolls within a 78-unit viewport, above controls, without filler space. Longer translated information uses the same bounded scrolling behavior. Default portrait/wide landscape short instructions size naturally to approximately 30% of screen height instead of the old 50%/58% requests.

All [six final production captures](screenshots.md) were freshly generated during final PlayMode and inspected again after the suite. They show compact portrait/wide Move instructions, narrow Large-text valid placement with a purchase-bearing draft, long information scrolled above reachable controls, and warm grid at ordinary floor/focused-room zoom. Masonry remains visible through transparent cell interiors; sprites, walls and doorway thresholds remain legible. Valid mint/invalid red and selection styling remain distinct. Measured luminance contrast against the average of each of the three actual authored masonry textures is **4.39 / 4.05 / 4.82**, versus **1.59 / 1.47 / 1.75** for the former floor-boundary color. These are texture-average presentation measurements, not text-contrast certification.

### Windows Development build

Unity **6000.3.2f1**, StandaloneWindows64, Development: **Succeeded, zero errors, one warning**. The warning is empty Unity Cloud access token for native symbol upload; symbols were not uploaded. No Cloud credential is needed for the local player. All **52** referenced production art assets are packed. [Original build reports](build-reports-original.zip), readable [build report](build-report.json), [messages](build-messages.txt), packed asset lists and [303-file SHA-256 player manifest](windows-artifact-manifest.json) are retained. The artifact totals **181,826,735 bytes** including qualification reports.

Final player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-placement-compact/Dungeon Lord.exe`.

## Isolation and preservation

Reuse: `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation`, initial **2.683 GiB**, imported Library retained. It was closed, with no Unity.exe process or UnityLockfile. Strict preflight matched all **1,208** inputs, zero target-only files. Eight existing Temp projects were inventoried with exact paths, sizes, Library/lock status; the unused room-construction project has a lock and is not touched. No new clone is needed.

All files/metadata under Assets, Packages, ProjectSettings and ContentAuthoring remain subject to strict comparison. Existing helpers/namespace transformations are unchanged. Source checkpoints and complete mismatch diffs are captured before restoration; target-only files are not ignored. Unique scene fixture namespaces override the isolated filename before GameRoot boots. Current primary/validation save/sidecar filenames and owner source settings receive fresh read-only hashes; historical build files receive fresh hashes. The owner has changed validation draft sidecars since earlier UAT, so this packet uses the current inventory rather than old sidecar names.

Strict source checks immediately precede and follow both complete suites and the final build. Post-EditMode equality is exact. Post-PlayMode detected generated TMP fallback-font and ProjectSettings changes; post-build detected URP asset/global settings, ProjectSettings and UnityConnectSettings changes. All six complete serialization diffs were captured before restoring the disposable input files. Focused checkpoints and their complete diffs are retained too. [Thirty source inventories and mismatch diffs](source-inventories-and-diffs.zip), [checkpoint index](source-checkpoint-index.json) and [frozen input trees](qualified-input-trees.json) retain exact per-file hashes and permitted namespace/text normalization transformations. Final restored check: **1,208 expected/actual inputs, zero mismatches, zero target-only files**. Existing isolation helpers were not relaxed.

[Fresh preservation audit](owner-preservation.json): all **eight** audited primary/isolated save files, draft sidecars and owner source settings retain their before hashes; both save-prefix filename inventories remain identical. [Prior player preservation](earlier-build-preservation.json): all **606** files in the two existing Windows builds retain their hashes and counts. This is an explicit file audit, not a whole-registry claim. The primary owner project was never executed for qualification.

[Final disk inventory](disk-inventory-after.json): reused project **2.859 GiB**, imported Library **2.223 GiB**, final player **0.169 GiB**; both prior **0.169 GiB** players remain preserved. No Unity Editor or qualification invocation remains, and no validation UnityLockfile exists. The CLI internal authentication broker is the only remaining Unity-named process; it owns no project checkout. [Initial project inventory](project-inventory-before.json) retains all eight exact existing Temp paths, sizes and Library/lock status. The seven unused projects and historical builds/caches are cleanup candidates only, pending explicit owner authorization. No project/cache/build/evidence/owner-file deletion or quarantine occurred.

Latest main and remote PR starting HEAD were fetched again before publication and remain `5932a9fe0b7cec87c81f09e8e7d619df62c53101` / `163c32fdbfc7aad9b844a0997fef0b2b83f1c307`. No newer merged implementation intervened. The prior stale-footprint thread `PRRT_kwDOQxUKE86rHSUp` remains resolved; its unchanged fix is requalified by the grid/multicell transitions. No new specification or ownership approval is required.

## Limited owner rerun

Use the isolated Editor project and final corrected Windows player sequentially against the isolated validation save.

1. Select a monster, trap and loot object and enter Move. Check the short instruction, valid/invalid preview, Cancel and Confirm. The panel should follow content instead of reserving blank space.
2. Repeat with a purchase-bearing draft; check Save/Discard after positioning ends. Try New, explicit Owned redeployment and explicit Reassign without changing the accepted browser/target behavior.
3. Check 720×1280 portrait, 1920×1080 landscape and 1024×600 fallback at Small/Default/Large. Scroll overflow information; Cancel/Confirm/Save/Discard must remain reachable without overlap.
4. Inspect the warm grid at Fit Floor and Focus Room zoom over textured masonry. Pan/zoom, choose valid/invalid cells, cancel, change target/floor and confirm overlay cleanup and distinct validity/selection colors.

Accepted placeholder art remains unchanged: Goblin, Chilling Sigil, Snare Trap, Hidden Cache and Basic Loot Node still use permitted category fallbacks. Future language packs and physical-device comprehension require later product validation; test-local language expansion verifies overflow mechanics. No right-side inspector, domain/save/transaction change or broad UI refactor is included. Automated qualification is complete and this correction is ready for external review. The limited owner Editor/Windows rerun above remains required; do not merge.
