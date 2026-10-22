# Production Analysis qualification

This is implementation evidence for independent review, not final Unity qualification or owner UAT.

## Source and isolation

Baseline: `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac` (#247 and #248 merged). Initial implementation source: `9b1635736cf7a7ea0f18fccb27a8cd42b1838146`. Replay-layout correction and PlayMode wrappers: `31733c030d0aa44733022211bf66e967f7f191ac`. Final implementation source: `e44ea6c103cd450b7a3433c29dff205d5808646f`, adding immediate replay-transition refresh and its direct-start regression. Subsequent evidence-only commits do not change Unity inputs.

Unity 6000.3.2f1, Unity CLI 1.0.0-beta.13. Focused and affected tests use the fresh disposable `Temp/ui-composition-analysis-20261022-01`, prepared with the existing `Tools/Presentation/qualification_inputs.py` helper. Its namespace was explicitly set to `phase7-analysis-focused-20261022-01.json` in memory before preparation; only the copied Bootstrap config and copied SaveService fallback receive that filename substitution. The source project retains its original filename. Production-scene fixtures additionally select their existing GUID-named disposable saves before boot.

Every input under Assets, Packages, ProjectSettings and ContentAuthoring is compared, including metadata. The prepared inventory contains 1,418 files. JSON config formatting, UTF-8 BOM and CRLF/LF normalization are the existing helper's declared comparison rules. Source hashes, input inventories, XML, isolated screenshots and raw logs are retained locally under `TestResults/phase7-production-analysis-20261022` and the disposable project. Raw Unity logs are not published because they may contain authentication arguments.

During attempt 08, Unity reserialized the disposable TMP fallback font asset (`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`). The generated version was archived, the unchanged source copy was restored into the disposable project after Unity exited, and all 1,418 inputs were checked again. This is a recorded restoration of generated test-project content, not a source edit or a permitted gameplay-input substitution. Later stages follow the same explicit audit and archive rule if Unity reserializes it again.

## Iteration history

| Attempt | Scope | Passed | Failed | Skipped | Applicability |
| --- | --- | ---: | ---: | ---: | --- |
| 01 | Initial focused launch | — | — | — | Compile abort: ambiguous NUnit comparer; no test verdict |
| 02 | Production Analysis scenes | 4 | 2 | 0 | Fixture admission/reopen assumptions corrected; presenter discovery corrected |
| 03 | Focused launch | — | — | — | Compile abort: missing fixture namespace import; no test verdict |
| 04 | Focused Analysis | 13 | 2 | 0 | Fixture snapshot-identity and competing-edit assumptions corrected |
| 05 | Focused Analysis | 15 | 0 | 0 | Intermediate input inventory 05 only |
| 06 | Affected regressions | 64 | 3 | 0 | Real layout regressions; compact entry moved into existing header |
| 07 | Focused Analysis, three layout reproducers, content gates | 84 | 0 | 0 | Intermediate input inventory 07 only |
| 08 | Affected EditMode, Analysis and content gates | 147 | 1 | 0 | `9b163573…`; dense replay safe-area overflow corrected at `31733c03…` |
| 09 | Affected EditMode, Analysis and content gates | 148 | 0 | 0 | `31733c03…` only; not relabeled as full affected coverage of `e44ea6c1…` |
| 10 | Focused Analysis and affected replay EditMode | 41 | 0 | 0 | Final implementation `e44ea6c1…`; includes immediate replay entry and all Analysis cases |
| 11 | Selected Analysis/composition/replay/foreground PlayMode | 44 | 0 | 0 | Final implementation `e44ea6c1…`; nine Analysis scene wrappers included |

Repeated runs are separate executions, not additional unique coverage. Earlier outputs retain their original applicability and are not relabeled for the corrected source.

The final EditMode filter was `ProductionAnalysisPresenterTests;PhaseSevenA4ProductionScene.Analysis;VisualRun`. The final PlayMode filter was `PhaseSevenA4ProductionShellPlayModeTests.Analysis;PhaseSevenA4ProductionShellPlayModeTests.Composition;PhaseSevenA4ProductionShellPlayModeTests.UatContentHeight;PhaseSevenA4ProductionShellPlayModeTests.FloorHud;VisualRun;LivingDungeonB3B2PlayModeTests;CombatC2PlayModeTests`. Both use `unity test <disposable> --mode <mode> --filter <filter> --output <fresh-XML> --timeout 1800 --no-color -- -logFile <fresh-log>`.

Attempt 09 additionally selected the 65 content build gates, existing draft durability matrix, normal/inspection/input composition, floor HUD, room-content input/recovery, construction layout/localization/recovery, live foreground labels, automatic admission/edit/settlement and cached-idle checks. Its 148 passing cases apply to `31733c03…`. The only subsequent Unity-input changes add immediate summary refresh at replay controls and its Editor/PlayMode regression; attempts 10/11 cover those changed paths. The earlier 148-case selection is not claimed rerun wholesale at the final implementation source.

## Preservation closeout and screenshots

Final comparison: 1,418 expected/disposable Unity inputs, zero mismatches or extras after recorded restoration. The source project's Unity inputs are unchanged from `e44ea6c1…`. Final PlayMode reserialized the disposable fallback font and reordered the Android/Standalone identifier entries in disposable PlayerSettings; the diff was checked to contain only that key-order change, generated copies were archived, and exact source bytes restored after process exit. Attempt 10 needed no restoration.

All 60 original files in the protected persistent-data directory are byte-identical at closeout; there are no additional files. Save/recovery files never required restoration. PlayMode did regenerate its default `TestResults.xml` there despite the explicit output path. The new report was archived, and an existing historical report (`TestResults/living-dungeon-b3b1-20261018/12-integration-play.xml`) was verified against the pre-run SHA-256 `f9eccd6bb418bc6f4f03b71dd0272384262aa088d59675e7c99fd9d6362efc6e` before restoring that exact report with filesystem approval. Historical source evidence was read, not modified. Future fresh campaigns must account for this default-report side effect in preservation preparation.

See [sanitized source, report hashes and preservation summary](source-applicability.json). Complete XML totals match unique leaf IDs, with zero failures, inconclusive cases or skips for attempts 09–11. Canonical models, schema/migrations, checkpoint code and history writers have no diff from the required baseline. No existing player build was launched or replaced.

Final-source PlayMode screenshots were visually inspected: [Large portrait](analysis-large-720x1280.png), [Large landscape](analysis-large-1280x720.png). These are automated Editor-rendered captures, not phone/Moonlight or Windows standalone acceptance.

## Remaining gates

Independent ChatGPT review comes next. After blocking corrections, all mandatory final full suites, any required sustained profiles, a new isolated Windows standalone build, native continuation/reopen checks and owner Moonlight UAT remain required. None has been claimed passed for this packet.

The #248 supervisor was read but not used: its frozen historical campaign adapters do not constitute safe fresh preparation for this source. Each iteration instead uses one bounded blocking Unity CLI test invocation, with a 1,800-second timeout and a fresh XML/log output identity. No closed historical campaigns or sustained 68-run profiles are selected. No model-polling automation or replacement qualification infrastructure is introduced.

Codex must prepare the standalone UAT environment and owner fixture after review/final qualification; the owner does not prepare saves, JSON, PowerShell, Unity or processes. See [implementation and UAT scope](implementation.md).
