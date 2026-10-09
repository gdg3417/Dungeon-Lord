# Strict disposable qualification

Frozen Unity/content implementation: `c88c076064f045fef464a45ab63bf86b79c7f496`, continuing PR #230 from reviewed `03c553a`. The four Git input trees are recorded in `qualified-input-trees.json`. Final evidence commits must preserve these trees. Previous PR qualification packets remain historical and unchanged.

## Inputs and isolation

`Temp/ui-composition-overlay-validation` did not exist before this task's baseline preparation. The corrected strict helpers inventory the complete `Assets`, `Packages`, `ProjectSettings` and `ContentAuthoring` roots, including intended nonignored untracked source and Unity metadata, rejecting target-only source, missing/changed inputs, unsafe paths and reparse redirection. Preparation performs no deletion. Every launch follows exact inventory verification. The only explicit transformations are the existing isolated save filename (`phase7-ui-composition-validation-01a11d08.json`) in the save service/config and documented JSON/BOM/newline handling. Generated Library/build/test outputs are outside input roots and cannot mask stale source. No owner project or save directory is a validation target.

Baseline had 1,188 inputs; corrected implementation has **1,196**. Eight additions are the preview/test C# files with metadata and two imported threshold PNGs with metadata. Pre-focused, pre-full EditMode and pre-full PlayMode inventories each report zero mismatches and zero target-only files. Post-full EditMode also verifies exactly. Raw inventories and any Unity-produced serialization differences are retained in `source-inventories-and-output-diffs.zip`; `source-checkpoint-index.json` records results rather than silently treating output modifications as input equality.

`owner-preservation.json` rehashes the owner's primary save, both draft evidence files, primary ProjectSettings and TMP settings, and compares the exact primary-save prefix inventory. Owner manual UI/Windows UAT is not automated qualification.

## Reproducible commands

PowerShell, repository root `C:/Dev/Dungeon-Lord`; Python is the bundled runtime, CLI is installed Unity CLI beta 10 using Editor **6000.3.2f1**. XML paths resolve to repository `TestResults`; all Unity commands target the disposable clone. Raw CLI/Editor logs are archived with authentication arguments redacted.

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py -v
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/prepare_validation.py ui-composition-overlay-validation
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' Tools/Presentation/verify_qualified_source.py ui-composition-overlay-validation TestResults/ui-overlay-pre-full-play-source.json
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation' --mode EditMode --filter 'DungeonVisualCatalogTests;CompositionVisualCheckpoint;UatRoomBoundariesRespectSavedConnectionsAndRotation;OverlayFullWidthSafeAreasAndTransparentInput;OverlayCompactFootprintCardsAndRuntimeCaptures;UatCorridorVisualCheckpoint' --output 'TestResults/ui-overlay-focused-editmode.xml' --timeout 1200 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'TestResults/ui-overlay-focused-06-playmode.xml' --timeout 1800 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation' --mode EditMode --output 'TestResults/ui-overlay-full-editmode.xml' --timeout 1800 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation' --mode PlayMode --output 'TestResults/ui-overlay-full-playmode.xml' --timeout 1800 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' run 'C:/Dev/Dungeon-Lord/Temp/ui-composition-overlay-validation' --timeout 1800 --no-color -- -buildTarget StandaloneWindows64 -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildWindows -logFile C:/Dev/Dungeon-Lord/Temp/ui-overlay-windows-build.log
```

Preparation/verification repeat between runs, with distinct checkpoint output names in the index. Baseline uses `CompositionVisualCheckpoint` before editing. The build executes existing production content/prebuild gates and the Bootstrap-only Windows Development utility, then asserts each referenced PNG occurs in actual packed assets. No alternate build configuration or owner save path is used.

## Observed results

| Run | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Isolated helper regressions | 12 | 0 | 0 |
| Baseline Unity composition capture | 1 | 0 | 0 |
| Final focused EditMode assets/scene | 12 | 0 | 0 |
| Final focused genuine production PlayMode | 34 | 0 | 0 |
| Full EditMode | 1,619 | 0 | 1 |
| Full PlayMode | 3,046 | 0 | 10 |

Full suite case-by-case comparisons, actual skips/failures and emitted layout/input observations are retained in `report-summary.json`, `test-outcome-comparison.json` and raw XML. Both unchanged clipboard tests (`CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition` and `DiagnosticsGate_BlocksAndPermitsOverlayDiagnosticsAndSmokeCopy`) pass; no historical exception was applied. PlayMode retains eight synchronous EditMode fixture skips, the Windows inverse-runtime skip and standalone-player-only skip. Content/build gates, graphical construction/movement/invalid recovery, quote/publication, save lifecycle and run snapshot tests remain intact. Genuine mouse/touch fixtures cover transparent rail input, blocked button input, selection/focus/fit/pan/zoom, guidance and draft isolation; no gameplay rules or tests were weakened to accommodate presentation.

Post-full PlayMode comparison fails closed on two modified clone inputs: Unity rewrites the TMP fallback asset's serialization/texture fields and reorders the Android/Standalone identifier map in ProjectSettings. The complete 9,003-character diff is retained. Neither is treated as qualified source; preparation restores the intended clone inputs and pre-build verification again reports 1,196 exact inputs, zero differences and zero target-only files. Owner originals are not restored or modified.

Compared with the previous owner-correction packet, EditMode adds three passing cases and PlayMode adds two. **No prior case is removed or changes outcome.** Full EditMode includes 65 passing production spatial build/content gate cases; full PlayMode includes all 17 BuildReadiness cases. New footprint/boundary scene tests run with existing scene, construction and recovery fixtures.

Intermediate runs are retained: focused-01 compile ambiguity between two MouseButton types (no XML); focused-02 camera-clamped drag fixture; focused-03 exact subpixel rectangle equality and oversized card; focused-04 Unity default label padding still exceeded compact height. Corrections used explicit Input System type, an inward drag, a 0.01-pixel comparison tolerance and removal of label margins/padding. Focused-05 and focused-06 pass all 34 cases. The no-longer-reserved-column assertion was changed to full-width invariant plus measured occlusion recovery, as required by the approved overlay design; canonical/mana/input assertions remain.

The post-focused-05 checkpoint also records TMP/ProjectSettings output differences and a source test edit made before that comparison. Subsequent focused/frozen-run preparation restores the clone and re-establishes equality; that historical intermediate checkpoint is not claimed to qualify final source.

## Windows Development player

**Succeeded**, StandaloneWindows64, Development, Unity 6000.3.2f1, current Bootstrap-only entry. Build report: **181,299,820 bytes**, **0 errors**, **1 warning**. The warning is Unity services native-symbol upload (`UriFormatException: Invalid URI: The URI is empty`), retained verbatim in `build-messages.txt`; it does not change the successful build result. Batch exit logs additionally retain Mono thread-abort/debugger-listen shutdown messages; no claim of warning-free logs.

All **52** referenced art PNGs occur in actual packed output, including imported doorway/corridor thresholds. See `packed-dungeon-art.txt`, `packed-source-assets.txt` and `build-report.json`. Complete adjacent player files are retained at `Builds/Phase7UIComposition-Overlay-c88c076-20261009/Windows/` (**303 files**), with per-file SHA-256 and complete-copy comparison in `windows-artifact-manifest.json`. Owner Windows UAT has not been performed.

Post-build verifier rejects four changed clone inputs with no target-only files: URP keyword prefilter/runtime-settings serialization, ProjectSettings batching/identifier serialization and UnityConnect enablement. The 4,593-character diff is retained; no such setting is committed to primary source. Preparation restores only the explicitly named clone inputs. Final source verification again reports **1,196 intended = 1,196 actual, zero failures, zero target-only**. The qualified source trees remain those of `c88c076`; subsequent evidence/tooling commits leave all four trees unchanged.

## Limits

Static first-pass art and imported readable textures, not final art or physical-device performance qualification. No FPS/allocation claim follows from desktop suites. Existing pool/reconstruction checks and repeated UI interaction observations are retained. Owner UAT remains pending after external review. Neither bent-corridor gameplay, entrance controls, floor capacity expansion nor mana reservations is implemented.
