# Qualification — 2026-10-08

Recommended configuration: GPT-6.1 Sol / High, Complex. Continue the established implementation without changing its ownership boundaries. Fallback GPT-6 Sol / High; report availability before another substitution. Baseline/main remains `72924bdf54d222332b256cab254a22126f3b4bfd`; refreshed before delivery. Unity CLI `1.0.0-beta.10`, Editor `6000.3.2f1` (`a9779f353c9b`).

## Continuation boundary

Construction, durable invalid intent/correction, legacy format DTOs, final-state economics, production interaction, Bootstrap retirement boundaries and focused regression coverage were already implemented. The pending final scene command had not launched when execution was interrupted. This continuation reran that affected scene check, completed full suites and production gates, inspected final screenshots/source equality, and prepared delivery evidence. No new gameplay or UI scope was added.

## Observed results

All paths below are relative to `C:/Dev/Dungeon-Lord`. Counts are observed NUnit results, not inferred from command exit alone. Successful CLI processes exited 0. The full PlayMode process and both clipboard reruns exited 1 (Unity test process exit 2).

| Check | Total | Passed | Failed | Skipped | XML report |
|---|---:|---:|---:|---:|---|
| Focused draft/economy/loading | 170 | 170 | 0 | 0 | `TestResults/room-construction-final-economy-loading.xml` |
| Canonical/A4/A5/save/run/knowledge regression | 462 | 462 | 0 | 0 | `TestResults/room-construction-canonical-regression.xml` |
| Construction + existing draft durability | 72 | 72 | 0 | 0 | `TestResults/room-construction-durability.xml` |
| Actual production EditMode scenes | 20 | 20 | 0 | 0 | `TestResults/room-construction-scene-editmode.xml` |
| Final genuine production PlayMode scenes/input | 22 | 22 | 0 | 0 | `TestResults/room-construction-final-scene-playmode.xml` |
| Full EditMode | 1,600 | 1,599 | 0 | 1 | `TestResults/room-construction-full-editmode.xml` |
| Full PlayMode | 3,044 | 3,032 | **2** | 10 | `TestResults/room-construction-full-playmode.xml` |
| Isolated clipboard rerun | 2 | 0 | **2** | 0 | `TestResults/room-construction-clipboard-playmode.xml` |
| Same clipboard tests with documented `-nographics` | 2 | 0 | **2** | 0 | `TestResults/room-construction-clipboard-headless-playmode.xml` |
| Production gates | 291 | 291 | 0 | 0 | `TestResults/room-construction-production-gates.xml` |

The full EditMode fixture contains 53 new construction-domain cases, all passing. Production gates comprise build 65, export 112, recovery 57, loading 37, actual scene 20. The EditMode one-skip and PlayMode ten-skip sets exactly match the qualified A5 guidance reports; no failing test was hidden, deleted or relaxed. Full report headers, hashes, durations, skip names and failed names/messages are preserved in `report-summary.json`.

## Remaining qualification blocker

Both full PlayMode failures are existing `DungeonBuilder.Tests.EditMode.BootstrapOverlayPagingTests` assertions:

- `CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition`
- `F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent`

Their smoke-text composition assertions succeed. Reading `GUIUtility.systemCopyBuffer` returns empty instead of the complete string. Both tests pass in the full EditMode run, and both reproduce in isolated PlayMode runs with graphics and `-nographics`. The clipboard function and assertions are unchanged by this PR. Unity documents this property as access to the [system-wide clipboard](https://docs.unity3d.com/kr/6000.0/ScriptReference/GUIUtility-systemCopyBuffer.html).

A native availability probe, executed outside the filesystem sandbox with approval, observed `OpenClipboard` false / Win32 error **5 (Access denied)** and no existing open-clipboard window. It neither read nor changed clipboard contents. This establishes an environment access blocker; the precise desktop/security cause remains unverified. Desktop lock/disconnection status was requested from the owner. No clipboard substitute, assertion relaxation or new skip was introduced.

The [follow-up execution-context diagnosis](clipboard-context-diagnosis.md) verifies an active console session and accessible `WinSta0/Default` input desktop. Clipboard denial persists in approved, non-AppContainer medium-integrity execution with station clipboard rights, no thread impersonation and immediate job UI mask 0. Unlocking alone is not a demonstrated fix. A NULL open-window query cannot exclude a NULL-window clipboard holder. The smallest safe next action is the documented probe from a separately owner-launched normal Windows terminal; no security setting change is requested. No tests were unnecessarily repeated during this diagnosis.

**Owner-authorized sequencing exception:** the Windows Development Build now succeeded with 0 errors and 1 warning. Full PlayMode remains 3,044 total / 3,032 passed / 2 failed / 10 skipped. No tests were changed or rerun, and clipboard investigation stopped. See [Windows build evidence](windows-build.md) for the exact command, artifact, warnings and continued disposable-save isolation. The historical clipboard diagnosis above is retained; its proposed next investigation is superseded by the owner's decision to proceed to build/manual qualification.

## Exact commands

All commands use the existing executable `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe` against disposable project `C:/Dev/Dungeon-Lord/Temp/room-construction-validation`.

```powershell
# Final production input/scene check
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --filter 'PhaseSevenA4ProductionShellPlayModeTests' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-final-scene-playmode.xml' --timeout 1200 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-final-scene-playmode.log'
# Full qualification
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode EditMode --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-full-editmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-full-editmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-full-playmode.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-full-playmode.log'
# Demonstrated-failure investigation
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --filter 'CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition;F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-clipboard-playmode.xml' --timeout 600 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-clipboard-playmode.log'
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --filter 'CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition;F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-clipboard-headless-playmode.xml' --timeout 600 --no-color -- -nographics -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-clipboard-headless-playmode.log'
# Independent production gates
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode EditMode --filter 'ProductionSpatialContentBuildGateTests;ProductionSpatialContentExportTests;ProductionSpatialContentRecoveryTests;PhaseFourContent;PhaseSevenA4ProductionScene' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-production-gates.xml' --timeout 1800 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-production-gates.log'
```

Retained earlier successful reports include `room-construction-focused.xml` (23), `room-construction-draft-economy.xml` (203), `room-construction-final-focused.xml` (53) and inactive-floor (1). Retained failures include initial full EditMode (`room-construction-full-editmode-first.xml`: missing consumer partial allowlist, corrected with stronger partial-class assertions), starter-boundary/context-continuity fixture attempts, and the earlier scene save/schema assertion report. Later focused/full results cover those demonstrated corrections. The full PlayMode failure is also preserved as `room-construction-full-playmode-first.xml`. Logs are under `Temp/room-construction-*.log`; authentication arguments have been redacted. Test shutdown logs include Mono abort/debugger-port messages; no new C# compilation warnings were observed. Negative test cases intentionally log validation/write failures.

## Safety, deterministic behavior and presentation

The disposable copy starts from a Git archive of the verified baseline, overlaid with intended files. Its config and SaveService fallback both name `room-construction-validation-fallback.json` before any boot. Production scene tests additionally create a GUID destination after entering PlayMode and install a scene-loaded callback **before** loading Bootstrap; assert SaveService has not started; verify the actual path; and retain isolation through reload and root destruction. Cleanup is restricted to that GUID prefix. The owner primary save and both existing owner draft records were read-only hash checked before and after qualification and remain byte-identical. No owner save was cleaned or changed.

`qualified-source-manifest.json` verifies all 25 intended Assets files against the tested copy; SaveService comparison reverses only the validation filename substitution. Root ProjectSettings and TMP settings have no Git changes. ProjectSettings SHA256 remains `34DA6D701E4C4629CA7B1CECB638F801D9C5EA4F40D33B09FECA46777073B993`; TMP Settings Git blob remains `92a60536387caf4a8caaed785b4c07b48abdf201`. Owner-local ignored files and the validation/evidence directories are retained. No owner files were reset, stashed, normalized, deleted or committed.

Construction-domain coverage verifies authoritative anchors, supported authored alternatives, unsupported/invalid/capacity/workload cases, consecutive stable allocation, mixed histories, invalid correction/cancellation, v2/v3 predecessor preservation, corruption/budgets/prefixes, failed/unknown durability, stale rules/baseline, final-state room/corridor pricing, no experimental new-room renovation, exact balance, current config/wallet recheck, investment alignment, atomic write failures, duplicate-charge prevention, active-run isolation and knowledge/lifecycle preservation. Canonical schema stays 13.

Measured selected-configuration search on configured Floor 1: 144 previews, 100–160 ms in the final focused report across five authored room/orientation combinations; 196 previews / 218 ms on an existing inactive Floor 2 fixture. No per-frame/all-room/all-orientation scan or invented performance threshold. Searches reject envelopes exceeding existing configured tile limits.

Production tests verify transient preview versus durable confirmation versus Save, restart recovery, invalid blockers accessible while collapsed, actual touch input, retained option-button identity during wallet updates, Small/Default/Large text and safe area at 1080×1920 / 1920×1080. Long Japanese-ready text renders with existing fonts. Final representative [portrait](screenshots/construction-preview-large-1080x1920.png) and [landscape](screenshots/construction-preview-large-1920x1080.png) captures were visually inspected. Existing missing-key behavior and localization guards remain covered. These are evidence images, not imported game sprites.

External review, owner acceptance of retained first-room Bootstrap setup, comprehension/UAT and equivalent Windows behavior remain outstanding. This branch is not merge-ready.
