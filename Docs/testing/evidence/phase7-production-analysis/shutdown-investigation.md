# PR #249 native shutdown investigation — 2026-10-23

**Classification: 4 — unresolved after bounded diagnosis.** A separate **category 3 process-observation defect** is confirmed and corrected in the diagnostic procedure. The fresh visible-player hang reproduces with reliable host handles, without overlap and without requesting a Computer Use screenshot. No production defect has been identified and no production correction is proposed. Native qualification remains failed; owner UAT is not ready.

## Source and preservation boundary

Branch `feature/phase7-production-analysis`, starting HEAD `5174e92c2c074d827b7296290ce71e5c3b6d7fef`; fetched main `fbe4bdff0cba9fea72b42fb6a05714327f880df4`. Original implementation baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`. No merge, rebase or cherry-pick occurred. Model recommendation: GPT-6 Astra Low, Ambiguous or High-Risk; no fallback requested.

Qualified source remains `c870c5dc92aee87e854af350aa0a73653d045ff2`, with original Unity inputs identical to implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. The existing 303-file Development build was verified before diagnosis and at closeout: artifact `P249R2-fe8daae0264348579db73d3402254215`, Unity 6000.3.2f1, isolated product `DL B3B2 Smoke Two Floor P249R2`. Executable: `Temp/ui-composition-analysis-p249r2-native/Builds/Development/Windows/Dungeon Lord.exe`, SHA-256 `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`. No Unity input transformation, new build, full suite, sustained profile or runtime instrumentation was performed.

New output identity: `TestResults/phase7-analysis-shutdown-20261023-d1`. Preflight reconfirmed the previous 26,181 historical and 350 original persistent-file hashes, then protected **28,780 existing files** including all current company-product files, the full prior recovery campaign, historical evidence and sealed build. Native handle checks confirmed PIDs 38808 and 39392 were already absent/exited, with no live player before the first attempt.

The ordinary player has a compiled qualification save filename, so a different ordinary filename cannot be selected without changing the sealed build. Each visible attempt therefore exclusively leased only that existing isolated namespace: move its original bytes to an unused attempt backup, install an exact copy of the genuine `08-native-finish-post-exit.save.json` fixture, launch once, archive fresh output only after both process handles signal, then restore the original bytes. Separate attempt directories and records identify the otherwise fixed active path. The prior campaign's original active-save bytes were backed up without being loaded or mutated; players loaded fresh fixture copies. No owner fixture was substituted and no checkpoint/history content was authored. The scripted attempt used a fresh approved-prefix filename and metadata-only seed-manifest substitution with the canonical writer's exact seed bytes.

## Prior evidence and static inspection

[The previous native UI record](native-ui-continuation.md), its JSON, archived PID records and raw log endings were reviewed. Preserve their failures and provisional snapshots as originally classified. The old `10-native-ui-post-exit.save.json` was copied before actual process exit and remains unsuitable for strict reopen qualification.

Relevant source paths remain unchanged:

- `GameRoot.cs:291–305`: `OnApplicationQuit` calls `ApplyApplicationQuit`, which flushes foreground work, performs the canonical AppQuit save and assigns presentation text.
- `GameRoot.Foreground.cs:118`: foreground flushing uses the existing configured maximum-boundary bound; failure returns through the existing authority.
- `ProductionDungeonController.cs:641–650`: pause/quit flushes pending draft transactions; destruction closes replay and unsubscribes presentation. Both new visible cases closed from ordinary Dungeon without entering Edit or Analysis, so a dirty draft or Analysis interaction is not required to reproduce this failure.
- `SaveService.cs:407`: the canonical success log occurs after the recognized-state write and publication succeed. This establishes that save call's success branch, not completion of every managed/native shutdown action.
- `GameRoot.ForegroundSmoke.cs:280`: the existing Development smoke coroutine writes its report/closed prefix, then calls normal `Application.Quit`.
- `qualification_windows.py:46`: the existing supervisor already distinguishes a signaled process handle from a process merely appearing in a PID query. Frozen campaign adapters were not reused.

No application-owned background shutdown worker or quit veto was found in these paths. That inspection does not prove all engine callbacks completed. `PlayerConnection::Cleanup` and ComputeBuffer messages show progress through cleanup; neither is an engine-return or process-termination proof. Unity documents `OnApplicationQuit` as a callback [before quitting](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.OnApplicationQuit.html).

## Corrected diagnostic process observation

The campaign-specific standard-library monitor retains the actual `subprocess.Popen` Windows process handle until a terminal record, and immediately opens a second query/synchronize handle. It records executable SHA-256, exact command line, PID, kernel creation FILETIME, both actual handle values, host CIM creation/parent/image data and script hash. Host command line must exactly equal the launch arguments. Both handle identities must agree.

Once per second, `WaitForSingleObject` and `GetExitCodeProcess` sample both handles. Only matching signaled handles authorize post-exit save reads. A failed wait, inaccessible/query error or identity disagreement is unknown and blocks acceptance. A missing window, cleanup log, silent sandbox query or `STILL_ACTIVE` check alone never establishes exit. These rules follow Microsoft's [native wait contract](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject) and [process exit-status contract](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getexitcodeprocess).

A small non-Unity self-check observed a live child through both handles, then independently obtained its actual exit code 23; an invalid handle raised unknown. This is a monitor self-check, not a Unity correctness-test result.

Bounds were 600 seconds total and 90 seconds after the observed AppQuit marker. Read-only thread/wait probes had separate 25-second bounds. A confirmed hang is explicitly failed before terminating only its actual owned launch handle, using diagnostic code `0xE2490001`. The terminal record then requires both handles to signal, obtains the actual code and checks for surviving children. No PID-only or generalized kill workflow was introduced. No next player launched before its predecessor's terminal and fixture-restoration records. No canonical snapshot was accepted before actual exit.

## Controlled outcomes

| Attempt | Conditions | Actual result |
| --- | --- | --- |
| `01-scripted`, PID 7384 | Existing `two-floor-wait` Development smoke; batchmode/hidden; injected technical clock; fresh canonical-writer seed and save; normal `Application.Quit` | Report Passed; both handles signaled, exit **0**, monitor elapsed **18.406 s**, no surviving children. |
| `02-visible`, PID 44508 | Ordinary clock, genuine completed-history/active-party fixture, fresh exclusive lease, no overlap; one Computer Use capture, Windows X close | AppQuit observed 14:21:07.708 UTC; still active with no window after 90 s. Forced exit at 14:22:38.772 UTC, code **3796434945 / 0xE2490001**, monitor elapsed **129.234 s**. **Graceful exit failed.** |
| `03-visible-no-capture`, PID 27080 | Same sealed build, ordinary fixture bytes/product/options and fresh exclusive lease, no overlap; no screenshot requested; Windows Alt+F4 close | Same hang; forced exit at 14:25:58.982 UTC, code **3796434945 / 0xE2490001**, monitor elapsed **135.281 s**. **Graceful exit failed.** |

Monitor elapsed begins after the launch identity check; it is not an independently measured creation-to-exit duration. Exact native creation times and timestamps remain in launch/observation/exit records. Forced codes are diagnostic termination results, never successful `Application.Quit` codes. No surviving children remained. The visible fixture source hashes match each other exactly; scripted versus visible startup differs in batchmode, technical clock, seed, filename and quit trigger, so it is not a single-variable proof of an engine defect.

The two fresh visible reproductions establish that earlier process overlap is not necessary. Their launch monitor remained alive throughout shutdown, so an already-exited launcher parent is also not necessary. The third attempt establishes that requesting a Computer Use screenshot is not necessary. It still uses the same Computer Use helper for selecting/closing the window, so it does not exclude every qualification-environment contribution. No security permission, profiler flag, graphics backend, driver setting, executable option or save validation rule was changed to obtain an exit.

Read-only `GetClientRect` measurements for the actual selected game HWNDs independently confirm **1280×720** clients in both visible attempts. Raw observations also retain a transient 33×17 owned window in attempt 03; it was not the selected game window or a portrait viewport. Independent portrait client measurement remains pending.

## Remaining wait state and limits of inference

At 10 and 70 seconds after AppQuit, attempt 02 had 65 then 62 threads; CPU totals were 25.53125 then 25.640625 seconds. Attempt 03 had 68 then 65 threads; CPU totals were 27.015625 then 27.140625 seconds. All sampled threads were in Windows Wait state, mainly UserRequest. WCT returned one-node chains with no reported cycles for every sampled thread. Microsoft notes that a [single WCT node can also mean an unsupported synchronization element](https://learn.microsoft.com/en-us/windows/win32/api/wct/nf-wct-getthreadwaitchain); it does not identify a blocker or prove absence of deadlock.

Read-only thread start-address mapping in attempt 03 found threads originating in UnityPlayer (21), NVIDIA `nvwgf2umx` (26), D3D12Core (4) and other Windows modules. Start modules are not current instruction pointers or call stacks and do not establish blame. The thread-description probe was inconclusive; no reliable thread-name conclusion is drawn. No threads were suspended, no code was injected, and no memory dump was captured or published.

The latest visible log endings again stop at `PlayerConnection::Cleanup`, whereas the normally exiting scripted run also emits its subsequent Unity memory report. This narrows the observed log progression, not a precise blocked function. Both visible attempts reported successful AppQuit canonical saves; only after forced exit were their schema-15 save bytes archived and hashed. That evidence is retained separately from the failed graceful exits and is not a read-only navigation/reopen pass.

Environment: Windows 11 Pro build 26300, NVIDIA RTX 3070 Ti driver 32.0.16.1692. A validated Application-log query for IDs 1000/1001/1002 during the diagnostic interval returned no matching events and no access/query error. Absence of a Windows event is not proof of correct termination. `cdb`, `windbg` and `procdump` were not found on PATH; nothing was installed or reconfigured. No symbolized current-thread stacks are available, so application versus Unity/Windows/driver/environment ownership remains unresolved.

## Closeout and next recommendation

All five recorded player PIDs (the two historical PIDs and three new attempts) are absent/exited by native-handle observation; no active player remains. Both visible namespace leases were restored exactly. The first full preservation scan failed on **ten isolated-product Unity SDK cache files**: eight archived Analytics files had been pruned, and an Insights session-end file and shader analytics file were rewritten. Save/recovery files and Unity inputs were unchanged. This failed scan is retained as `preservation-final.json`; it is not relabeled as a pass or treated as a cause of the hang.

After all processes exited, each of the ten originals was matched against both its preflight SHA-256 and the earlier closed-product archive. The two generated replacements and four new SDK cache files were moved to unused diagnostic archive paths; all ten originals were restored using non-overwriting copies from those verified backups. The test product's original 47-file inventory is restored. No setting was changed to suppress SDK behavior, and no save or recovery file required corrective restoration.

The separate full recheck covers **28,780 protected files with zero differences**, **1,418 original Unity inputs unchanged**, all five disposable projects still matching **1,418 original files each**, and the unchanged sealed **303-file build**. The prior report/build evidence, default TestResults.xml and normal owner saves/recovery files are unchanged. Failed exits, the first preservation failure and provisional historical snapshots retain their original classifications.

No full EditMode/PlayMode suite, Windows build or sustained profile was rerun. Existing qualified-source results retain their applicability: focused 4/0/0; complete EditMode 2352/0/1; complete PlayMode 3741/0/10; Windows filesystem 1/0/0. No owner UAT launcher or fixture was prepared.

Outstanding native cases remain blocked: valid modified Save/Discard/Continue, invalid two-choice dialog, pending/failed/stale draft handling, empty/incomplete-history Analysis, strict graceful close/reopen and canonical/draft comparison, and the remaining portrait/landscape Default/Large checks. Only the independent landscape client-resolution gap was closed in this diagnostic task. Scripted checks, Editor synthetic touch and physical Moonlight acceptance remain distinct.

Recommended next step is independent review of this evidence, followed by an explicitly scoped **symbolized main/worker-thread stack investigation** of the same sealed player. A proposed additional control would keep attempt 01's seed, phase, technical clock and `Application.Quit` trigger, but remove batchmode/hidden-window launch so that it runs visibly. A separate same-build comparison outside the Codex/Computer Use launch environment, using the same exclusive fixture and handle-based exit guard, could test remaining environment involvement. These are proposed diagnostic configurations, not executed comparisons or approved workarounds. Do not change production shutdown code, graphics/security settings or normal lifecycle acceptance criteria without evidence identifying the cause. If an application defect is identified, stop for review of its exact source and proposed correction before resuming native qualification.

See [machine-readable applicability](shutdown-investigation.json) and [monitor, process/wait evidence and preservation reports](shutdown-investigation-reports.zip). Full raw logs and canonical snapshots remain local under the new diagnostic output; the public packet contains selected log excerpts and hashes, not save contents or raw memory. PR #249 remains draft and unmerged. Recommendation: **still blocked**.
