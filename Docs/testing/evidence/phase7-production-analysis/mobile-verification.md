# PR #249 focused phone and replay verification

Reviewed starting HEAD: `d3e3c5c0e948834aac7f3bfa767ea3aaf728f151`. Continue on `feature/phase7-production-analysis`; this packet updates the same PR. Standard classification, recommended GPT-6.1 Sol Medium, as specified by the active model policy. No model fallback was used.

## Coverage

The existing production Analysis fixture now drives a 360×640 Game View with an injected bottom-left safe rect `(10,18,340,604)`, yielding a 340×604 usable region. Default and Large text both run. Injection is reapplied after navigation/layout boundaries because Editor `Screen.safeArea` represents the monitor; the test does not claim physical-device DPI or owner phone acceptance. Layout is settled before scrolling targets into view.

Ordinary Input System touch press/release events, through the existing `CompositionTap` fixture helper, exercise:

- Compact Dungeon entry, Analysis return and Display controls from both destinations; usable-safe-area bounds, panel picking and at least 48-pixel button dimensions are checked.
- All three valid modified-draft choices, entered through Display → Analysis. Continue retains the exact draft and canonical file; Discard preserves canonical bytes; Save publishes the additional room and clears the existing sidecar through the existing authority.
- Invalid two-choice dialogs, plus stale drafts whose Save button stays disabled under actual touch. Localized structural/stale reasons and every required choice are revealed within the dialog's bounded ScrollView. Expanded test-only localization guarantees wrapping and a nonzero scroll range. Scrolling uses the existing UITK `ScrollTo` API before touch-target checks; this is not a physical swipe-gesture qualification.
- Compatible Replay Run entry through its actual button, followed by Analysis. Replay closes, the replay-specific header class clears, the normal summary returns exactly, and Dungeon return never restarts replay. Live/canonical save comparisons remain unchanged.
- Touches at the covered Edit, Replay and floor-control positions and map position while Analysis is open. Covered controls remain disabled and their click observers record zero activations; selected floor and rail state remain unchanged. Return restores control availability. No unused programmatic replay entry path is exercised or changed.

Test setup creates draft geometry and a competing canonical edit through the established fixture/domain authorities. Only the player navigation/choice/Display/replay actions under verification use touch; setup does not introduce a draft, save or gameplay owner.

## Demonstrated defect and minimal fix

The populated compact summary wrapped into a tall header at 360×640. Replay Run's bottom edge reached y=625 while the usable safe area ended at y=622, so the original phone replay workflow failed before replay could be entered. Captures and panel bounds reproduced this on the unchanged production UI.

The existing compact-HUD class now selects a single-line, ellipsized summary, using the same full localized text in the tooltip and dedicated Analysis destination. Layout boundaries refresh this bounded form. Wide layouts retain their existing summary; replay-specific state still clears independently. Production changes are confined to `ProductionDungeonAnalysis.cs`, one layout refresh in `ProductionDungeonController.cs`, and the existing summary USS selector. No new strings, gameplay numbers, schema, migrations, persistence authority, simulation or reporting math changed.

An initial Display hit-test failure was a fixture issue: `ScrollTo` ran before the injected safe-area layout settled. Settling first revealed the existing button correctly. Configuring the phone/touch device before creating a modified draft also corrected a fixture device/state-ordering failure. These are not reported as production draft-authority defects.

## Run applicability

| Run | Source | Selection | Passed | Failed | Skipped |
| --- | --- | --- | ---: | ---: | ---: |
| 01 | `b5bcf8842c04fc43c40b96ab3e37df60f0e7e562` | Four new phone cases, unchanged production UI | 2 | 2 | 0 |
| 02 | `744804450b3e9ad10a053d8f2c8f9c03adf2a2d9` | Two reproducers with captures | 0 | 2 | 0 |
| 03 | `a5f2127369afb6671f6c30f609955dd5fe69c3cd` | Two reproducers after safe-area scroll settling | 0 | 2 | 0 |
| 04 | `b26681785baccb34fad7a477f0a75d79ad905b00` | Two reproducers after bounded header and fixture ordering | 2 | 0 | 0 |
| 05 | `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5` | All Analysis, reporting, replay and affected phone/layout EditMode filters | 48 | 0 | 0 |
| 06 | `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5` | Selected Analysis/replay/layout PlayMode wrappers | 19 | 0 | 0 |

Run 01 passed navigation/covered-input and invalid/stale-dialog scenarios in both text sizes. Runs 01–03 retain their actual failures; they do not count as qualification passes. Run 04 passed the valid-choice and replay scenarios in both text sizes. The final source is `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`, adding successful-state captures and an exact normal-summary restoration assertion. Later evidence-only commits must have identical Unity inputs.

## Isolation and boundaries

Fresh disposable: `Temp/ui-composition-analysis-mobile-20261022-01`. Fresh output root: `TestResults/phase7-analysis-mobile-20261022`. The existing `qualification_inputs.py` helper sets only the copied config/SaveService filename to `phase7-analysis-mobile-20261022-01.json`; the source filename is unchanged. Every Assets/Packages/ProjectSettings/ContentAuthoring input, including metadata, is inventoried. GUID-scoped production-scene saves provide additional boot isolation.

All 60 original persistent-data files were hashed and backed up before launch, including the default test report affected by Unity's result writer. The 47 existing files in the approved PR evidence/local output packet were separately hashed. Raw logs and private backups remain local. New output identities and capture GUIDs never overwrite previous campaigns or captures.

Only bounded focused CLI invocations are used. The supervisor's frozen historical adapters were not repurposed. No full EditMode/PlayMode suite, 68-run profile, Windows build, owner UAT or merge is launched by this packet. Independent re-review precedes mandatory final qualification and owner UAT.

## Final evidence and readiness

Runs 05/06 use Unity 6000.3.2f1 and CLI 1.0.0-beta.13, each through `unity test <disposable> --mode <EditMode|PlayMode> --filter <selection> --output <fresh XML> --timeout 1800 --no-color -- -logFile <fresh log>`.

EditMode selection: `ProductionAnalysisPresenterTests;PhaseSevenA4ProductionScene.Analysis;VisualRun;UatContentHeightCardsDisplayAndSafeActions;FloorHudStatesAndInputCapture;CompositionLayoutsAndPresentationWorkRemainBounded`.

PlayMode selection: `PhaseSevenA4ProductionShellPlayModeTests.Analysis;PhaseSevenA4ProductionShellPlayModeTests.VisualRun;PhaseSevenA4ProductionShellPlayModeTests.UatContentHeight;PhaseSevenA4ProductionShellPlayModeTests.FloorHud;PhaseSevenA4ProductionShellPlayModeTests.CompositionNormal;PhaseSevenA4ProductionShellPlayModeTests.CompositionLayouts`.

All 1,418 disposable Unity inputs match the source after closeout. EditMode required no generated-input restoration. PlayMode only reordered the Android/Standalone identifier entries in the disposable PlayerSettings file; its diff was checked, generated copy archived, and original source bytes restored after process exit. No source asset/settings changed. Unity's regenerated default persistent-data test report was archived and restored from the SHA-verified pre-run backup with filesystem approval. All 60 protected files are byte-identical, and all 47 pre-existing evidence files retain their original hashes. No save/recovery file needed restoration.

See [source/selection/report hashes and preservation](mobile-source-applicability.json). Final-source PlayMode captures: [Dungeon Default](phone-dungeon-default.png), [Analysis Large](phone-analysis-large.png), [valid choices Large](phone-valid-dialog-large.png), [stale choices Large](phone-stale-dialog-large.png). The dialog captures include intentionally expanded test-only localization, with choices reached by scrolling; they are stress fixtures, not new production copy.

The focused gaps are closed and the branch is ready for independent re-review. Final qualification remains a subsequent gate after approval; no owner UAT or merge has occurred. Historical qualification retains its original source applicability.
