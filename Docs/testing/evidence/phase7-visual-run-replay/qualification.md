# Phase 7 visual manual-run replay qualification

Baseline: clean local `main` and refreshed `origin/main` at `fc281885f68f0ff15be4f3ee9bca9205b3cc6c59` (merged #235). Branch: `codex/phase7-visual-run-replay`. AGENTS/model policy, Phase 7A0 lock, living-dungeon lock/roadmap and merged #230/#231/#233/#234/#235 were inspected. The overnight interruption damaged no source/evidence; no recovery/reset was performed.

Final automated qualification passed. Ready for independent code review and owner Editor/Windows UAT; owner gameplay acceptance is not claimed. No merge is authorized.

| Check | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused final contract/Phase A/replay EditMode | 16 | 0 | 0 |
| Focused production Dungeon PlayMode | 52 | 0 | 0 |
| Strict input helper regressions | 12 | 0 | 0 |
| Full final EditMode | 1,887 | 0 | 1 existing |
| Full final PlayMode | 3,314 | 0 | 10 existing |
| Production build gates, within EditMode | 65 | 0 | 0 |

Windows StandaloneWindows64 Development Build: **Succeeded**, Unity **6000.3.2f1**, Bootstrap only, **181,509,140 report bytes**, **0 errors / 1 warning**. Complete output inventory: **303 files / 181,981,733 bytes**, including reports. All **52 required artwork assets** and the isolated owner-UAT filename are verified packed. The player was not launched.

The single build warning is the established Unity services native-symbol upload failure (`System.UriFormatException: Invalid URI: The URI is empty`). Final full EditMode, full PlayMode and build logs contain **zero C# compiler errors/warnings**. Fresh-project package resolution also logs the existing minimum-version overrides for six packages; no package source/lock change is committed. Standard licensing-client/debugger/test-runner diagnostics remain in redacted logs. No gameplay warning is silently suppressed.

## Exact evidence and corrections

`qualification-reports.zip` contains complete NUnit XML, including original failures. `report-summary.json` retains counts, exact failure/skip messages and measurements. `baseline-editmode.json` and `baseline-playmode.json` compare every leaf occurrence/result/skip reason with #235's final alias-correction reports: each adds 14 passing cases, with no removed or changed existing outcomes. Both full suites were run from the final corrected source. The original frozen Phase A fixture SHA-256 remains `53045ad58ab1dbebf8ea07f83480db02604eb79ae3b243f1169df2ae2e950198`; neither fixture nor projector was changed.

The first focused run passed 11/13. The wipe fixture's unsupported party size was corrected to the existing three-member fatal-encounter pattern. The two-floor issue was a subsequent manual commit using a single-floor helper without `runLoot`, not failed activation; the established multi-floor writer fixes the missing dependency. The first screenshot fixture needed explicit purchase funding. The first full EditMode report had 1,885 passes / 2 failures / 1 existing skip: the exact consumer allowlist and public structured-evidence contract required the narrowly documented integration corrections. Both then passed focused qualification, followed by the clean full EditMode run. No production activation rule, gameplay formula, migration or frozen equivalence data was weakened.

The initial sandbox Unity launch stalled before a verdict. Two test-authoring compile errors were corrected and their logs retained. The 12 input-helper tests initially could not initialize/clean sandbox temporary directories (24 setup/cleanup permission errors); the unchanged helpers passed 12/12 outside the sandbox. Those setup errors are not gameplay assertion failures.

EditMode's established skip is `PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed` with reason `gd66.test.windows_only_inverse`. NUnit root duration resets around EditMode-to-PlayMode transitions; do not interpret its short root duration as total command wall time. Reports retain individual case durations and logs.

PlayMode's unchanged skips comprise eight `gd66.test.synchronous_edit_mode_fixture` cases, the non-Windows inverse case, and `gd66.test.windows_player_only` standalone preflight. Exact names/reasons are in the report summary. Production build gates passed 65/65 within full EditMode. Existing graphical construction/content management, Phase A/B1/B2, canonical save/migration/recovery and native filesystem regressions remain in the full reports.

## Isolation and preservation

Qualification project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-validation`; disposable filename `phase7-visual-replay-validation.json`. All 1,266 Unity/content input files and metadata are compared exactly under the established filename and text-normalization substitutions. Generated settings/font/URP differences are archived before refreshing only disposable inputs. Missing/changed/unexpected input checks are not bypassed.

The initial and final audits verify **all 12,924 protected save/player files unchanged**, including primary, permanent manual-UAT and current B2 UAT progress, save-directory backups and previous players. Exactly **22 preexisting disposable test fixtures** were restored from verified current backups after archiving their test-modified bytes. Owner primary, permanent manual-UAT and B2 manual-UAT files were never restored, reset, migrated or opened for agent gameplay. Previous players/caches and prior evidence remain. Support archives contain hashes/inventories, not owner save contents. This is a file audit, not a whole-registry/settings claim.

Prepared owner project: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat`. New filename: `dungeon-lord-visual-run-manual-uat-20261014.json`. It is an exclusive byte copy of `dungeon-lord-phase-b2-manual-uat.json`, with SHA-256 `5FA3717B7C6891A012BD7E46E8DA5E18E9A0D9BCA7ACC445AAA8F8FC746327D7`; source unchanged and application not opened.

Qualified player: `C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat/Builds/Development/Windows-visual-run-replay/Dungeon Lord.exe`. The built `sharedassets0.assets` contains the isolated filename. Both projects finish with **1,266 exact inputs / zero mismatches / zero target-only files**. Build-generated URP prefilter/runtime-setting lists, batching/application-ID ordering and UnityConnect state were inspected, archived and refreshed in the disposable project; source assets/settings were unchanged. Owner qualification instructions are in [owner-uat.md](owner-uat.md).

## Visual and workload evidence

`screenshots.zip` and `screenshots.json` retain 15 actual Unity captures: movement, monster encounter, trap trigger, reached loot and completion at 1920×1080, 1280×720 and 720×1280. Reviewed samples are directly available in [screenshots/](screenshots/). The gold/white hollow diamond is the party; localized text identifies it and reports exact recorded HP/survivor/outcome facts. Controls and wrapping remain usable in the reviewed default-text desktop/portrait captures. Existing broader production UI tests cover construction/content management, input, safe areas and text-size regressions. This is automated fixture evidence, not owner gameplay acceptance.

The final focused Windows Editor kernel measurement was 14 recorded steps / 416 display updates / 0.0527 ms, excluding projection construction, view/UI, rendering and Editor overhead. The scene fixture verifies view count does not grow across replay/floor/edit lifecycles. No per-frame collection construction/path search or invisible per-floor graphical animation is introduced. These bounded checks are not a physical-mobile performance or allocation-free whole-frame claim.

Known limits and authority details: [implementation.md](implementation.md). Diff review: [self-review.md](self-review.md). Owner Editor/Windows UAT and independent code review remain pending. Do not merge or begin B3A/another feature from this packet.
