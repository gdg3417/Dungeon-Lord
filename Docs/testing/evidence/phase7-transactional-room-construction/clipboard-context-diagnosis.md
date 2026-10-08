# Clipboard execution-context diagnosis — 2026-10-08

Recommended Codex configuration: GPT-6.1 Sol / Medium. Standard: focused environment diagnosis and qualification of existing PR #229. Fallback GPT-6 Sol / Medium, then GPT-5.6 Sol / Medium. No model substitution or feature expansion was performed.

Starting implementation HEAD: `a1cebe2cb24c4ec573e3ad6f7ec7f2622ca92d95`; branch `codex/phase-7-transactional-graphical-room-construction`; main `72924bdf54d222332b256cab254a22126f3b4bfd`. PR #229 remains open and draft. The worktree was clean at entry.

## Code and evidence checks

`BootstrapOverlay.CopyFullSmokeTextToClipboard` is byte-equivalent to starting main after line-ending comparison. The entire `BootstrapOverlayPagingTests.cs` file matches starting main, Git blob `44696635312bd5d37bf4e3532a1b3d468c40d08e`. No clipboard implementation, assertion, fake, skip, or system security setting was changed.

The two failures occur at `Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(copied))`. Complete smoke-text composition assertions have already succeeded. Historical test process: Unity CLI test launcher, Unity Windows Editor `6000.3.2f1`, `-batchmode -runTests -testPlatform PlayMode`, disposable project `C:/Dev/Dungeon-Lord/Temp/room-construction-validation`, approved Codex execution. The historical Unity child's access token was not captured; current launcher measurements must not be presented as a direct historical child-token measurement.

Latest valid test evidence remains unchanged:

| Report | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `TestResults/room-construction-clipboard-playmode.xml` | 2 | 0 | 2 | 0 |
| `TestResults/room-construction-clipboard-headless-playmode.xml` | 2 | 0 | 2 | 0 |
| `TestResults/room-construction-full-playmode.xml` | 3,044 | 3,032 | 2 | 10 |

No tests were rerun in this diagnosis: native availability remained denied, so a clipboard-capable interactive test context was not established. Qualified EditMode, construction/economy/durability, production scene/input and gate results remain valid. All 25 qualified Assets source hashes match; no source change invalidates them.

## Native observations

The [read-only probe](clipboard-context-probe.ps1) neither reads nor changes clipboard contents. It opens and immediately closes the clipboard only if allowed. It reads process token, desktop, window-station handle access and immediate job information; it does not modify any of them.

| Property | Default sandbox command | Approved command (`require_escalated`) |
|---|---|---|
| Session | 1 | 1 |
| Window station / thread desktop / input desktop | `WinSta0 / Default / Default` | Same |
| Input desktop query | Succeeds | Succeeds |
| Integrity SID | `S-1-16-4096` (Low) | `S-1-16-8192` (Medium) |
| AppContainer | Yes | **No** |
| Restricted-token flag | False | False |
| Thread impersonation token | None (error 1008) | None (error 1008) |
| Assigned window-station granted access | `131879` | `983935` (`0xF037F`) |
| Assigned handle includes `WINSTA_ACCESSCLIPBOARD` | Yes | Yes |
| Open separate station handle requesting clipboard access | Denied (error 5) | Succeeds; handle closed |
| Immediate job UI restriction query | Succeeds, mask 0 | Succeeds, mask 0 |
| `OpenClipboard(NULL)` | **False, error 5** | **False, error 5** |

`query session` reports the owner's console session 1 **Active**. The current input desktop is accessible `Default`, not an inaccessible locked input desktop. Unlocking alone is therefore not a demonstrated fix.

Approved execution reproduces the failure outside the filesystem/AppContainer sandbox, with a medium-integrity non-AppContainer process and station clipboard rights. It still originates from Codex's launcher; a separately owner-launched Windows terminal has **not** been tested. This distinction prevents claiming a machine-wide failure based only on Codex ancestry.

The exact reproducible failing operation is native `OpenClipboard(IntPtr.Zero)` returning false with `GetLastError() == 5` (`ERROR_ACCESS_DENIED`). The assigned station-right bit, current thread impersonation and queried immediate job clipboard bits do not explain the approved-process denial. The ultimate cause (other inherited restrictions, system clipboard contention, or other environment policy) is unresolved; no policy change is justified by these observations.

`GetOpenClipboardWindow` returns NULL. This does **not** exclude clipboard contention: Microsoft explicitly documents that a task can open the clipboard with a NULL window, in which case this query also returns NULL. No clipboard-holding application was identified or terminated. [GetOpenClipboardWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getopenclipboardwindow), [OpenClipboard](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-openclipboard).

Reference interpretation: clipboard use requires `WINSTA_ACCESSCLIPBOARD`; job read/write clipboard restriction flags are `0x2 / 0x4`; querying a NULL job handle reports the immediate job, not every possible ancestor. [Window-station rights](https://learn.microsoft.com/en-us/windows/win32/winstation/window-station-security-and-access-rights), [job UI restrictions](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_ui_restrictions), [job query semantics](https://learn.microsoft.com/en-us/windows/win32/api/jobapi2/nf-jobapi2-queryinformationjobobject).

Raw final snapshots: [sandbox](clipboard-context-sandbox.json), [approved](clipboard-context-approved.json). Process IDs identify those measurements only.

## Smallest safe owner action

Open a normal Windows Terminal/PowerShell **from Start**, independently of the Codex task terminal. No administrator elevation or security changes are requested. Run this availability comparison:

```powershell
& 'C:/Dev/Dungeon-Lord/Docs/testing/evidence/phase7-transactional-room-construction/clipboard-context-probe.ps1' -ReportPath 'C:/Dev/Dungeon-Lord/TestResults/room-construction-clipboard-context-owner-terminal.json'
```

If script execution or clipboard access is denied, stop and report that result; do not change execution policy or other security settings. If `CanOpenClipboard` is true, that terminal provides the missing independent context. Run **only** the two failing cases there, using the unchanged CLI and isolated project:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord/Temp/room-construction-validation' --mode PlayMode --filter 'CopyFullSmokeTextToClipboard_PreservesFullSmokeComposition;F6BuildsAndCopiesFullSmokeTextIncludingOutcomeCueWhenPresent' --output 'C:/Dev/Dungeon-Lord/TestResults/room-construction-clipboard-owner-terminal-playmode.xml' --timeout 600 --no-color -- -logFile 'C:/Dev/Dungeon-Lord/Temp/room-construction-clipboard-owner-terminal-playmode.log'
```

Only a successful two-case report permits the full PlayMode rerun. Only a successful complete suite permits the Windows Development Build. No successful rerun or build is claimed here. New logs can contain CLI authentication arguments; redact them before sharing.

## Save preservation and delivery boundary

Before any next test, the validation copy still has `room-construction-validation-fallback.json` in both build config and SaveService fallback. Production scene GUID overrides remain armed before scene boot and through shutdown. The owner primary save and both existing owner draft records still match the original SHA256 values. Root ProjectSettings hash remains `34DA6D701E4C4629CA7B1CECB638F801D9C5EA4F40D33B09FECA46777073B993`; TMP Settings blob remains `92a60536387caf4a8caaed785b4c07b48abdf201`. No unrelated owner files were changed.

At the time of this diagnosis, the remaining stages were full PlayMode qualification and Windows Development Build; the PR was draft and no owner UAT approval was claimed. This is historical status only. The later owner decision and completed qualifications below supersede it.

## Later owner decision — 2026-10-08

The diagnosis above is a historical account of the clipboard investigation at that time. On October 8, the owner directed that clipboard investigation stop, accepted the two existing failures as a narrowly documented qualification exception for PR #229, and approved carrying the first-room Bootstrap dependency for this PR. The corrected Windows Development Build and agreed Editor/standalone UAT subsequently completed, and external review completed. The full PlayMode result remains 3,044 total / 3,032 passed / 2 failed / 10 established skips; neither clipboard test was changed, skipped or represented as passing. See [qualification.md](qualification.md) and [owner-uat.md](owner-uat.md) for the current closeout. PR #229 is approved for merge but is not yet merged.


## Subsequent owner decision

The owner authorized stopping clipboard investigation and proceeding to Windows build/manual qualification despite the unchanged failures. The historical next-action advice above is superseded. See [windows-build.md](windows-build.md): build succeeded under this sequencing exception; full PlayMode is still not qualified.
