# PR #242 — owner-usability correction qualification

The prior owner UAT failed usability at 1280×720/Large despite green automation. This correction is automated-qualified and awaits independent review and a repeat owner check. PR stays Draft. Actual model GPT-6.1 Sol High; Complex, well-defined UI integration. No fallback, delegation, new branch, Unity project copy or Editor installation.

Start: `46068bdcaf1d55e8fb8ff2a5d4c044f7f34266b9`. Final tested Unity inputs: `0997d553ada69d54c4f53891932edd8df9b4cee7`. Later tooling/evidence commits have no Unity-input changes. Protected B3A implementation/tests remain unchanged from `b5502291e9da3d229bbcc3d152f8bd56954d84eb`.

## Causes and focused correction

Several causes combined: Edit's legal-floor overview plus short map height made the active room tiny; entering content placement did not focus it. All native scrollers were hidden. Nested horizontal cards were taller than the available vertical viewport, and pinned inspection/actions obscured their lower rows. Narrow placement controls and repeated/wrapped information competed for the same bottom space. The old height assertion did not prove tile or card usability.

The existing map/sheet form a side-by-side workspace only during active landscape construction/content work. Normal mode, ordinary inspection, floor controls, room movement and replay retain the original root layout. The same elements are reparented without losing identity. The working sheet is content-sized and bottom-anchored, and optional Development Tools lives inside Display during Edit. Required confirmation/cancellation and whole-draft actions stay outside optional details scrolling. Native option/card horizontal scrolling and detail vertical scrolling are visible. Large remains the configured 26-unit text setting; no new localization or gameplay tuning was added.

Content category entry uses the existing Focus Room policy. Construction selection frames nearby existing/proposed geometry using the same policy. Explicit focus follows subsequent viewport geometry; deliberate pan/zoom and Fit Floor clear that retained focus. Existing bounds, clamp, coordinate conversion and zoom tuning remain authoritative. Production input dispatch/economics/draft mutation are unchanged.

At 1280×720/Large, before/after card heights are 142/142 while the enclosing viewport increases from 119 to 246. The selected content camera is 512×516, size 2.317969, about 111.30 pixels per tile. Funded construction camera is 512×559, size 5.175, about 54.01 pixels per tile. In the 600-high inset safe panel, construction changes from 195 collapsed / 156 expanded to 486×439 in both states; long unfunded details retain a 39-pixel scroll viewport and the existing full-line assertion. See [actual before/after captures and review](visual-review.md).

## Actual automated results

| Final attempt | Result | CLI |
| --- | --- | ---: |
| usability-focused-07 | 3 passed, 0 failed/skipped | 0 |
| usability-ui-edit-02 | 224 passed, 0 failed/skipped | 0 |
| usability-ui-play-01 | 72 passed, 0 failed/skipped | 0 |
| usability-build-01 | Windows Development succeeded; 0 errors, 1 warning | 0 |

The new actual-scene test and PlayMode wrapper cover all four resolutions × three texts, whole-card scrolling/reachability, native minimum tile size, actual mouse previews at two current cells with details changes, invalid target/Cancel without confirmed writes, all construction options and real terminal UI dispatch. Existing tests retain touch simulation, localization/safe-area/expanded/collapsed matrices, unaffordable Save, confirmation, acquisition/custody, reassignments, save/discard/resume, floor management, replay and C2 compatibility. No existing assertion or threshold was weakened. This is the affected filter, **not a full-suite pass**; old full EditMode 2,175/2/1 evidence remains historical and untouched.

Exact commands, source identities, NUnit reports, failure messages and restoration/index records are in [reports.zip](reports.zip) and [qualification.json](qualification.json). Every `run` calls read-only fail-closed preflight before Unity. Stage filters are `OwnerUsabilityConstructionAndContentReachability;RoomContentMouseTouchPreviewAndResumeKeepCanonicalIsolated;TransactionalRoomConstructionLayoutsLocalizationAndResume`, `PhaseSevenA4;ProductionDungeon;BootstrapOverlay;DungeonPresentation;CombatC1;CombatC2`, and `PhaseSevenA4ProductionShellPlayModeTests;CombatC2PlayModeTests`. Build uses the existing BuildLivingDungeonB3AWindows entry and Unity 6000.3.2f1.

Failed attempts are preserved: before-01 2/1 proves card clipping; focused-01 1/2 exposed nested-test scrolling and portrait overflow; focused-02 1/2 exposed portrait card and short details; focused-03 CLI6 had no verdict due a new ambiguous MouseButton diagnostic; focused-04 2/1 exposed wrapped unfunded controls; focused-05 and -06 each passed 3/0. ui-edit-01 passed 218/224 and exposed inspection width, blank compact-panel/tail and replay overflow. The placement-only root path/content sizing fixed those regressions; ui-edit-02 passes all 224. No failure report was rewritten. An attempted `focused-01` preparation was rejected before writing because the old product namespace existed; a distinct `usability-*` name passed the full gate. The disposable selftest passed outside the sandbox after a temporary-directory access denial.

## Build and preservation

Build: `Builds/Development/pr242-usability-20261018-usability-build-01/Dungeon Lord.exe`. StandaloneWindows64 Development, Bootstrap is the only included scene; 52 required artwork assets are packed. Actual assembly contains the corrected UI and excludes B3A qualification capability. Packaged product is `Dungeon Lord PR242 Graphics usability-build-01`; save namespace is `pr242-usability-20261018-usability-build-01.json`. 303 files / 182,203,092 bytes include report/debug files. The warning is the existing Unity-services native-symbol upload URI warning. This is an isolation-identity qualification build, not a release-identity build or owner-approved Windows usability result.

The new campaign checked 12,618/12,618 protected files unchanged through all tests/build/restorations. All 6,839 original save/player files match. Historical evidence remains 1,986/1,991: exactly the five accepted lost originals, hashes/incident unchanged; this does not claim restoration or retroactively successful original preservation. All attempts restore 1,340 exact authored inputs and the five original local Unity asset overrides. Source/archive hashes, separate product/save/static fixtures and screenshot helpers, unused outputs and missing metadata are fail-closed. No existing saves, builds or closed qualification outputs were overwritten.

The specifically requested update to the old owner procedure occurs **after** this campaign audit. Its exact prior bytes and SHA-256 (`a9697d6d9e3ddb141968a75642b219fab287f41bdd48d0a32378eb99968ebbd9`) are retained in reports.zip as `manual-uat-before-owner-failure.md`, as well as the reviewed Git commit. Historical test reports, qualification records and incident archives remain unchanged. A separate owner-only baseline protects the updated procedure/new artifacts before handoff.

## Owner handoff and limits

The actual scene fixture uses existing canonical starter/monster mutations and the existing QA fill authority. It preserves an exact complete-save artifact (`owner-uat-starter.json`) for a new isolated owner namespace. No original owner save is imported and no save JSON is authored. The owner gate verifies the indexed source artifact, identical committed Unity inputs, source/backups/protected files, exact CreateNew copy and unused launch outputs. Codex handles setup, restoration and Windows launch; the owner follows [the short click procedure](../pr242-graphics/manual-uat.md). Prior owner-editor-02 save, draft and log are preserved.

Owner approval and independent review remain pending. Starter-room graphical onboarding remains deferred; the ready fixture uses the existing Bootstrap authority. Physical touch is optional/unavailable, and native mobile/performance qualification remains separate. Long localized details still scroll; Fit Floor intentionally remains the legal Edit overview, and the collapsible floor overlay can cover geometry. B3A scheduling, settlement, checkpoints/history/schema, C2/continuity, reserve, mana/Heat/recovery and deterministic gameplay remain frozen and production living-dungeon integration remains disabled.
