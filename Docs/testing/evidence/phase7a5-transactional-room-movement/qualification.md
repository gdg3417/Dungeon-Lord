# Phase 7A5 qualification

This report retains the original implementation qualification at `84f2654181eae2bdeb906dea8f9e46bd7ea1d29f`. Subsequent owner UAT exposed a blocking floor-space/movement-guidance comprehension issue. The correction's exact commands/results, new Windows build, copy, bounded work and owner recheck are in [floor-grid/guidance correction](owner-uat-floor-guidance-correction.md). The original Windows player is historical evidence and must not be used for the corrected UAT.

Repository `gdg3417/Dungeon-Lord`; branch `codex/phase-7a5-transactional-room-movement-economics`; starting baseline `52c1eb00af241c4be9f8b117e96294a71dce82bb`. Local main and remote main were verified at that exact commit before editing. The prior owner branch was `codex/phase-7a4-transactional-editor-production-dungeon`, HEAD `7dc72528ae01188e38b1bffaca1612cdc105b984`; its tracked tree exactly matched the merge baseline. Staged changes were empty. The two unrelated owner files listed below were preserved while creating the dedicated branch from the merge commit.

Validation checkout: `C:/Dev/Dungeon-Lord/Temp/phase7a5-validation`, detached from the exact baseline with only intentional A5 source/assets/meta files copied in. Owner saves and unrelated changes were not used. CLI verified at `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`, version `1.0.0-beta.10`; project/editor version `6000.3.2f1`. All tests used the established CLI. XML verdicts were inspected individually; a CLI return alone is not the acceptance criterion.

## Commands and reports

Every test invocation has this exact PowerShell form, with the mode, optional filter, output and timeout supplied in the history table:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/phase7a5-validation' --mode <mode> [--filter '<filter>'] --output TestResults/<report>.xml --timeout <seconds> --no-color
```

Filters:

- `A5`: `PhaseSevenA5`
- `Third`: `PhaseSevenA5;PhaseSevenA4;StructuralEditService;StructuralEconomy`
- `Affected`: `PhaseSevenA5;PhaseSevenA4;PhaseFourSpatial;PhaseFourEconomy;PhaseFourWrites;PhaseFourSessions;PhaseFourLoad;PhaseFourCompleteSave;PhaseFourSaveSemantics;PhaseSevenA2Positional;PhaseSevenA3Intraroom;FloorKnowledge;PhaseFourBootstrap;PhaseFourDeletion;Localization;LongText`
- `UI`: `PhaseSevenA5;PhaseSevenA4ProductionScene`
- `Matrix`: `PhaseSevenA4;PhaseSevenA5`
- `Gates`: `ProductionSpatialContentBuildGateTests;ProductionSpatialContentExportTests;ProductionSpatialContentRecoveryTests;PhaseFourContent;PhaseSevenA4ProductionScene`
- `Complete save`: `PhaseFourWrites;PhaseFourSessions;PhaseFourLoad;PhaseFourCompleteSave;PhaseFourSaveSemantics`

The initial third filter used source class names for structural tests; NUnit discovers those fixtures as PhaseFourSpatial/PhaseFourEconomy. The subsequent Affected filter and both complete suites include all 77 cases in each structural fixture. No assertion was skipped or reclassified to compensate.

### Complete run history

The generated table below records every completed A5 test attempt, including intermediate failures. Report paths are relative to `C:/Dev/Dungeon-Lord`. Final logs are under `Temp/phase7a5-*.log`; UI captures are retained under `TestResults/phase7a5-screenshots` after qualification.

| Report (TestResults/) | Mode | Filter | Timeout | Total | Passed | Failed | Skipped |
|---|---|---|---:|---:|---:|---:|---:|
| `phase7a5-first-domain.xml` | EditMode | A5 | 1200 | 15 | 5 | 10 | 0 |
| `phase7a5-second-focused.xml` | EditMode | A5 | 1200 | 31 | 31 | 0 | 0 |
| `phase7a5-third-affected.xml` | EditMode | Third | 1200 | 128 | 127 | 1 | 0 |
| `phase7a5-fourth-affected.xml` | EditMode | Affected | 1800 | 535 | 535 | 0 | 0 |
| `phase7a5-final-affected.xml` | EditMode | Affected | 1800 | 538 | 538 | 0 | 0 |
| `phase7a5-full-editmode.xml` | EditMode | none | 1800 | 1530 | 1529 | 0 | 1 |
| `phase7a5-final-ui-balance.xml` | EditMode | UI | 1200 | 55 | 55 | 0 | 0 |
| `phase7a5-full-playmode.xml` | PlayMode | none | 1800 | 2973 | 2963 | 0 | 10 |
| `phase7a5-final-full-editmode.xml` | EditMode | none | 1800 | 1530 | 1529 | 0 | 1 |
| `phase7a5-production-gates.xml` | EditMode | Gates | 1800 | 285 | 285 | 0 | 0 |
| `phase7a5-canonical-save-integration.xml` | EditMode | Complete save | 1800 | 160 | 160 | 0 | 0 |
| `phase7a5-release-full-editmode.xml` | EditMode | none | 1800 | 1530 | 1529 | 0 | 1 |
| `phase7a5-release-full-playmode.xml` | PlayMode | none | 1800 | 2973 | 2963 | 0 | 10 |
| `phase7a5-final-record-matrix.xml` | EditMode | Matrix | 1800 | 124 | 124 | 0 | 0 |
| `phase7a5-qualified-full-editmode.xml` | EditMode | none | 1800 | 1533 | 1532 | 0 | 1 |
| `phase7a5-qualified-full-playmode.xml` | PlayMode | none | 1800 | 2976 | 2966 | 0 | 10 |
| `phase7a5-final-bounded-matrix.xml` | EditMode | Matrix | 1800 | 125 | 125 | 0 | 0 |
| `phase7a5-final-editmode.xml` | EditMode | none | 1800 | 1534 | 1533 | 0 | 1 |
| `phase7a5-final-playmode.xml` | PlayMode | none | 1800 | 2977 | 2967 | 0 | 10 |
| `phase7a5-final-production-gates.xml` | EditMode | Gates | 1800 | 285 | 285 | 0 | 0 |

### Intermediate failures and corrections

1. The first sandboxed test launch could not use Unity licensing paths. An elevated retry then found the owned batch instance still holding the isolated checkout. An initial process-stop attempt also returned an error; the confirmed owned batch process was then stopped with Force. No owner Editor or worktree was reset. These attempts produced no completed test verdict.
2. The first completed domain run passed 5/15 and failed 10. Unity serialization omitted StructuralMovementRequest because it lacked Serializable. The attribute and explicit payload round-trip regression corrected that failure. Two fixtures also assumed the wrong authoritative entrance-overlap reason and an invalid Large Chamber setup; they were corrected to the existing FixedOverlap reason and valid construction authority/anchor. No validator or production geometry was weakened.
3. The third affected run passed 127/128 and failed the A4 maximum-journal durability test. Expanded content-only records exceeded its existing aggregate byte budget. Content-only format 2/record 1 encoding was retained exactly; the explicit first structural command introduces format 3/record 2. The unchanged maximum-journal assertion then passed. No limit was increased.
4. A source-copy safety guard initially rejected a mixed slash representation before copying any files. Resolving the validation root to its absolute native path corrected the guard; no validation result came from that attempt.
5. Screenshot review found asynchronous capture initially recorded the next state and a teardown frame. Bounded frame completion after each capture corrected both files. Final invalid and valid long-text images were inspected; no pixel-golden gate was added.
6. Final review moved cached wallet-balance refresh into presentation so commit failure feedback is current without waiting a frame, preserved invalid footprints on reselection, and strengthened exact localized-room-name checks. Both complete suites were rerun.
7. A4 corruption fixtures were changed to mutate the exact historical record DTO, preserving their intended binding assertions. Three additional expanded-record cases check context contradiction with rebound candidate hash, vocabulary downgrade and duplicate fields. A derived one-byte-below aggregate evidence budget verifies refusal before mutation and recoverability of the earlier acknowledged prefix. The final complete suites and gates were rerun after the store preflight.
8. GitHub CLI was unavailable; PR publication uses the connected GitHub tool after branch/commit preparation.
9. An evidence-audit query initially required unique NUnit fullnames. An existing parameterized economy fixture emits two passing cases with the same formatted fullname; the audit was corrected to compare case multisets. Exact 160-case save, 285-case gate, 125-case A4/A5 and 538-case affected multisets all occur passing in final EditMode. This was an audit-query assumption, not a test failure; no test was changed.

## Final affected coverage

| Final EditMode subset | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| A5 domain | 45 | 45 | 0 | 0 |
| A4 transactional domain | 37 | 37 | 0 | 0 |
| A4 durability/store fault matrix | 29 | 29 | 0 | 0 |
| Structural renovation / edit | 77 | 77 | 0 | 0 |
| Structural economy | 77 | 77 | 0 | 0 |
| A2 positional canonical | 13 | 13 | 0 | 0 |
| A3 intraroom / immutable snapshots | 36 | 36 | 0 | 0 |
| Save/session/complete-save | 160 | 160 | 0 | 0 |
| Floor knowledge A5A/A5B | 31 | 31 | 0 | 0 |
| Actual production scene (10 A4 + 4 A5) | 14 | 14 | 0 | 0 |
| Production build gate | 65 | 65 | 0 | 0 |
| Production export gate | 112 | 112 | 0 | 0 |
| Production recovery gate | 57 | 57 | 0 | 0 |
| Production loading gate | 37 | 37 | 0 | 0 |
| Bootstrap smoke localized text | 7 | 7 | 0 | 0 |

Localization, long-text and Bootstrap retirement/parity cases also execute within the A5 domain and actual-scene fixtures and the complete regression suites; exact names are retained in the inventory. These rows are overlapping subsets of the full report, not extra test runs. All 160 complete-save cases and all 285 explicit gate cases are present and passing in final EditMode.

The 160-case explicit complete-save/session integration run passed separately; its exact case set is also required to pass in the final full EditMode report. The explicit five production gates comprise 65 build, 112 export, 57 recovery, 37 loading and 14 actual-scene cases. The final full suite rechecks the exact gate case set. A4 domain 37, durability 29 and its ten original scene cases remain passing; four new A5 scene cases run in EditMode and genuine PlayMode wrappers.

## Skips

Exact fullname sets are compared with `TestResults/phase7a4-theme-full-editmode.xml` and `TestResults/phase7a4-theme-full-playmode.xml`, the qualified merged A4 evidence. New or changed skips: **none**. The final exact sets and A5 case inventory are in [test/localization inventory](test-and-localization-inventory.md).

## Windows Development Build

Exact command (working directory `C:/Dev/Dungeon-Lord/Temp/phase7a5-validation`):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' build 'C:/Dev/Dungeon-Lord/Temp/phase7a5-validation' --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --allow-dirty-build --log-file 'C:/Dev/Dungeon-Lord/Temp/phase7a5-windows-development-build.log' --no-tail --timeout 1800 --no-color
```

Succeeded, CLI exit 0, Unity **6000.3.2f1**, **StandaloneWindows64**, **Development=true**, only `Assets/_Project/Scenes/Bootstrap.unity`. Build report: **0 errors, 1 warning, 171,651,663 bytes**. Complete retained folder (including report/support files): **171,891,223 bytes**. Player: `Builds/Phase7A5-2026-10-06/Windows/Dungeon Lord.exe` with adjacent dependencies. Report: `TestResults/phase7a5-windows-build-report.json`; complete log: `Temp/phase7a5-windows-development-build.log`. The repository-owned ProductionSpatialContentBuildPreprocessor executes the real gate during the successful BuildPlayer pipeline; its explicit 65-case fixture also passes.

The one build warning is exactly: “Access token is empty. Native symbols will not be uploaded for this build. Please make sure you are signed in to the Unity Cloud.” The symbol-upload helper also logs unavailable Unity Cloud Diagnostics credentials. This is the same qualified A4 warning; nothing was suppressed. Other log diagnostics are the existing empty `Assets/_Project/Tests 1/Tests 1.asmdef`, licensing access-token refresh failure followed by successfully resolved entitlement/updated license, and Mono `abort_threads` / debugger port 3492 shutdown messages. These are disclosed separately from BuildReport's single warning and zero build errors; no runtime theme warning or C# compiler error was found.

Unity generated post-build Standalone batching defaults/application-identifier ordering in isolated ProjectSettings.asset and enabled isolated UnityConnectSettings.asset. Both were inspected and restored exactly to baseline after retaining the player; they were never copied to the owner checkout or staged. The build started with baseline settings, using the required unmodified utility.

The repository-owned `DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment` and real production pre-build gate passed. CLI build ran from the isolated checkout, preserving existing owner builds. The test runner's temporary EditorBuildSettings/ShaderGraphSettings changes and application-identifier ordering in ProjectSettings were restored exactly to baseline only in the isolated checkout before build and excluded from the commit. Owner ProjectSettings and packages remain unchanged.

## Owner-local exclusions and source checks

- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`: owner modification, SHA-256 `F53586850A70C4F308E7E71B99B63881A49F38DE4BD40F4EDB7FE7AE6ECF8A52`.
- `Assets/_Project/Tests/ProductionDungeon.meta`: owner untracked file, SHA-256 `E613DC5C53CE71350F1D87E56A1FDA82C5C85EB34C5547A15BDFB0F7D3101CAA`.

These files are excluded from copying, staging, commit and build qualification. Intentional-diff and staged/commit diff checks pass. Whole-owner-tree diff checking retains the six pre-existing TMP trailing-whitespace findings; that asset is not normalized. No destructive cleanup, stash or owner reset was performed. The retained validation checkout and ignored reports/builds are evidence, not additional canonical writers. All **23** intentional source/asset/meta files match the qualified isolated checkout byte-for-byte; hashes are retained at `TestResults/phase7a5-qualified-source-hashes.json`.

Schema remains 13; no migrations, packages, owner ProjectSettings or structural-investment schema changes. Canonical schema-13 saves remain compatible; A5 structural draft journals require the new format-3 reader, while A4 format-2 evidence retains explicit recovery. Older readers cannot reinterpret the expanded vocabulary.

Manual owner UAT, external review, native mobile feel/filesystem qualification and low-end device performance remain outstanding. Automated passing and screenshots do not establish usability or fun. Follow [owner UAT](owner-uat.md); [implementation](implementation.md) records the exact replay, normalization, context and writer contracts.
