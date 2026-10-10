# Automated and layout qualification

**Latest qualification:** [translucent floor HUD](floor-hud-refinement/qualification.md), implementation `b85daa6`: full EditMode 1,620 passed / 0 failed / 1 skipped, full PlayMode 3,047 passed / 0 failed / 10 skipped, Windows Development build succeeded with all 52 referenced PNGs packed. Earlier [floating overlay/readability](overlay-room-readability/qualification.md), [owner corrections](owner-uat-corrections/qualification.md) and reports below remain historical. Owner retest is pending.

**Historical qualification:** the original comparison did not reject target-only inputs. These reports and counts are retained; current qualification and strict inventory guarantees are in [PR #230 review corrections](review-corrections/README.md).

Unity CLI `1.0.0-beta.10`; Unity Windows Editor `6000.3.2f1`. Starting main is `7b5911e5482aa20851e8329c0bb311f9aa6e7206` (merged #229). Tests use the disposable final project at `C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation`, with a new Library and clean asset import. `ContentAuthoring` and required repository documents are included alongside Assets, Packages and ProjectSettings.

## Results

The [report summary](report-summary.json) retains every completed XML report, its attributes, SHA256, failures and skip reasons. The [XML archive](qualification-reports.zip) contains the actual reports. [Redacted Unity logs](unity-logs-redacted.zip) and [log index](log-index.json) retain diagnostics and compiler warning/error history. Authentication arguments alone are redacted. CLI exit status is not used as a substitute for NUnit counts.

| Check | Total | Passed | Failed | Skipped | Report |
|---|---:|---:|---:|---:|---|
| Baseline actual runtime checkpoint | 1 | 1 | 0 | 0 | `ui-composition-baseline.xml` |
| Visual checkpoint, imported art and sprite replacement | 8 | 8 | 0 | 0 | `ui-composition-qualified-visual-assets.xml` |
| Genuine production scene/Input System | 28 | 28 | 0 | 0 | `ui-composition-qualified-input-playmode.xml` |
| Full EditMode before final header correction | 1612 | 1611 | 0 | 1 | `ui-composition-full-editmode.xml` |
| Full PlayMode before final header correction | 3050 | 3040 | 0 | 10 | `ui-composition-full-playmode.xml` |
| Header correction, actual scenes and imported art | 32 | 32 | 0 | 0 | `ui-composition-header-correction-editmode.xml` |
| Full EditMode after header correction | 1612 | 1611 | 0 | 1 | `ui-composition-final-full-editmode.xml` |
| **Final full EditMode, committed bounded renderer** | **1612** | **1611** | **0** | **1** | `ui-composition-bounded-full-editmode.xml` |
| **Final full PlayMode** | **3050** | **3040** | **0** | **10** | `ui-composition-final-full-playmode.xml` |

Final Unity/content inputs are commit `5d850dfe3f2befb4e708c02b6829fcdf1c7bf9b6`. The final full EditMode report includes 6 imported-art cases, 26 actual production scenes, 37 A4 transactional-editor cases, 29 draft-durability cases, 52 A5 movement cases, 53 room-construction domain cases, and production build/export/recovery/loading gates (65/112/57/37), all passing. The established skip is `PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`, reason `gd66.test.windows_only_inverse`.

Both existing clipboard tests passed unchanged in the first full PlayMode run: `CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition` and `F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent`. The historic PR #229 exception is not applied. The exact clipboard implementation and its test file have no diff from starting main. The earlier [execution-context diagnosis](../phase7-transactional-room-construction/clipboard-context-diagnosis.md) remains historical evidence; this run does not prove the underlying environment cause has been resolved.

Both clipboard tests also passed in the **final** full PlayMode run, and all 28 genuine production scene/Input System cases passed there. No qualification exception is requested. The ten skips and their reasons are retained verbatim in the report summary; no new skip attribute was introduced. Final full-suite CLI processes exited 0.

## Retained correction history

The first focused presentation run had two layout failures; subsequent measured cases exposed landscape viewport shrinkage, Large-text action overflow and wrapped Cancel/Confirm rows. They were corrected in USS while preserving the existing reachability, safe-area and minimum-viewport assertions. The first broad gate run was missing `ContentAuthoring` in the disposable copy (168 recovery/export failures); the complete clean-copy rerun passed those gates and exposed one new 48-pixel floating-point comparison (`47.999996`). Only that new pixel assertion rounds to four decimals; existing hit-region assertions remain intact.

The first full production input checks exposed Normal Mode retaining the empty-floor camera size after Bootstrap starter publication. A presentation-only canonical-publication callback now refits overview geometry while retaining deliberate pan/zoom/focus. A genuine tap asserts the room center is inside the world viewport and that initial size equals Fit Floor. Device binding and explicit resolution warmups make the floor-navigation fixture independent of prior Game View state. The corrected 28-case input report passed.

An additional long-HUD screenshot then exposed portrait sheet-header compression overlapping the status line. The header now resists flex shrink and the existing long-localization test additionally asserts categories end above status. Updated captures are reviewed before refreshing full qualification. No failing test, gameplay authority, clipboard test, skip or historical evidence was removed or weakened.

The final source audit changed the surrounding-rock fill to widened loop indices, matching the existing grid iteration. This prevents an authored extent ending at `int.MaxValue` from wrapping the induction variable. No dimensions, legal bounds or workload authorities change. Both final full suites and the build use this committed input, superseding the earlier full reports above.

## Layout, input and performance evidence

Actual scene tests cover empty/starter dependency; read-only floor/room/content/corridor inspection; selection without camera movement or canonical writes; explicit Focus Room and Fit Floor; descending floor rail and collapse; sheet collapse recovering viewport area; portrait/landscape state retention; three text sizes; long English/Japanese test strings; safe-area insets; and critical action bounds. Construction regressions retain local preview/confirm versus whole-dungeon Save, durable invalid intents, restart/stale/failed recovery, out-of-bounds reachability, no drafting spend, one-time publication and deletion/quiescence boundaries. The scene fixture installs a GUID save config before Bootstrap Start and asserts the actual save path.

Real Input System devices exercise room/content taps, Focus/Fit buttons, floor buttons, mouse wheel/drag, touch pan/pinch, chrome-origin input isolation and construction confirmation/cancellation. Pure camera and UI operations assert no draft commands or canonical file changes. The retained production diagnostics/Return controls remain usable.

In the full EditMode report, 12 focus/fit/sheet/resource-refresh cycles retained reconstruction count 4, pooled renderers 3 and draft commands 0. The floor-switch input scenario asserts exactly 24 selected-floor reconstructions and stable renderer-pool size across selection, category, sheet and tick changes. Cached Tiles and imported textures replace repeated material/texture creation. No FPS, zero-allocation, low-end device or whole-application memory claim is made; selection/read-only summaries still allocate bounded collections during explicit refreshes. Mobile compression/readability and physical-device performance remain future qualification.

## Exact qualification commands

Commands use approved execution because sandboxed Unity launch cannot complete licensing/hardware initialization. No owner Editor/project is opened. The baseline checkpoint used the separate baseline disposable copy before source overlay. Complete interim Unity command arguments are retained in the redacted logs.

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/prepare_validation.py ui-composition-final-validation
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode EditMode --filter 'CompositionVisualCheckpoint;DungeonVisualCatalogTests;CompositionSpriteReplacement' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-qualified-visual-assets.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-qualified-visual-assets.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-qualified-input-playmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-qualified-input-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode EditMode --filter 'PhaseSevenA4ProductionScene;DungeonVisualCatalogTests' --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-header-correction-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-header-correction-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode EditMode --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-final-full-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-full-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode EditMode --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-bounded-full-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-bounded-full-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-validation' --mode PlayMode --output 'C:/Dev/Dungeon-Lord/TestResults/ui-composition-final-full-playmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/ui-composition-final-full-playmode.log'
```

## Isolation and source identity

The validation config **and** SaveService fallback use `phase7-ui-composition-validation-01a11d08.json`; the normal source namespace is unchanged. Production scene tests additionally retain their `phase7a4-scene-<GUID>.json` namespace through restart and teardown. No fixture is allowed to silently boot the owner primary save. Owner save, both retained draft records, root ProjectSettings and TMP settings are read-only SHA256 checked in [owner preservation](owner-preservation.json).

The source manifest compares every tracked Unity/content input and added Assets against the tested/build copy. Permitted differences are the isolated save filename and text CRLF/LF/BOM normalization. Temporary validation-only settings are restored before final comparison. No owner settings, fonts, packages or TMP artifacts are committed. Source identity is tied to the implementation commit; subsequent evidence-only commits retain the same Unity/content inputs.

Post-PlayMode comparison detected two disposable output drifts: PlayerSettings reordered the unchanged Android/Standalone application-identifier entries; Unity/TMP upgraded serialization and cleared the empty dynamic fallback atlas to 1 × 1. The [drift manifest](post-test-drift-manifest.json) preserves those two mismatches. Neither root file changed. The copy was restored from source before building, then all **1,188** inputs matched again in the [prebuild manifest](prebuild-source-manifest.json). This is an explicit validation-output restore, not a source/font/package change or concealed test failure.

The [Windows Development build](windows-build.md) succeeded with 0 errors and 1 Cloud symbol-upload warning and verified all 50 referenced artwork PNGs in the actual packed report. Its four derived serialization outputs are retained separately and restored only in the disposable copy before [final source comparison](qualified-source-manifest.json). The complete adjacent player folder is retained locally; it has not been manually qualified by the owner.

External review and [owner UAT](owner-uat.md) are pending. Neither automated desktop success nor screenshots establish release readiness.
