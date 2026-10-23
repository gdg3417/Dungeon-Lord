# PR #249: visible Application.Quit and native-stack diagnosis

Date: 2026-10-23. Recommendation: **still blocked**. Classification **E: unresolved**, with high confidence that ordinary shutdown is intermittent. No application correction is justified by the available evidence. PR #249 remains open, draft and unmerged; owner UAT is not ready.

## Source and preservation boundary

Branch `feature/phase7-production-analysis` began this investigation at `adf0ffab189fc11edb77a88997e3ac548d8ee88c`. Current main is `fbe4bdff0cba9fea72b42fb6a05714327f880df4`; the original implementation baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`. No merge, rebase or cherry-pick occurred. Qualified source remains `c870c5dc92aee87e854af350aa0a73653d045ff2`, whose original Unity inputs match implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. This delivery adds evidence only.

The existing sealed artifact `P249R2-fe8daae0264348579db73d3402254215` contains 303 files. Executable SHA-256 is `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`; UnityPlayer.dll SHA-256 is `0b3ca5a98c9d9424ff7d3b311d50c5a2afb1282ad5d095e5535f29aed6b00874`. The build location remains `Temp/ui-composition-analysis-p249r2-native/Builds/Development/Windows`. Product remains `DL B3B2 Smoke Two Floor P249R2`, with its approved company/application identity. The sealed artifact was not rebuilt or modified.

The new output root is `TestResults/phase7-analysis-shutdown-20261023-d2`. Preflight hashed 28,866 protected files, 1,418 Unity inputs and the complete 47-file isolated product inventory; product originals were independently backed up before launch. Historical player PIDs 38808/39392 and D1 players 7384/44508/27080 were confirmed gone. Original failures, provisional snapshots and D1 classifications remain untouched. The [prior investigation](shutdown-investigation.md) and [native UI record](native-ui-continuation.md) retain their original applicability.

## Controlled comparison and actual process outcomes

All launches retain the actual launch process handle and a separately opened native query/synchronize handle. Executable/hash, complete command line, kernel creation time and host CIM identity are recorded locally. Both native handles must signal before reading post-exit canonical bytes. Query errors and inaccessible/uncertain identity fail closed. No subsequent player starts until its predecessor exits and its namespace lease is restored. No surviving child was reported in any terminal record.

| Attempt / PID | Conditions | Actual exit and observation |
| --- | --- | --- |
| `01-visible-application-quit` / 47108 | Existing canonical-writer two-floor-wait seed, technical clock and smoke `Application.Quit`; removed only batchmode and SW_HIDE relative to the scripted control, plus fresh output/save identities | Smoke report Passed; normal exit **0** through both handles at **15:08:58.469 UTC**, monitor interval **25.453 s**. |
| `02-ordinary-debugged` / 24432 | Ordinary completed-history/active-party fixture, ordinary clock; screenshot and Windows X; debugger preparation failed before attachment | Still alive after AppQuit; failed graceful exit. Owned-handle termination **3796434945 / 0xE2490001** at **15:26:43.435 UTC**, interval **344.985 s**. |
| `03-ordinary-stack-target` / 30588 | Same ordinary fixture bytes, no screenshot, Alt+F4; native debugger attached after the hang | Failed graceful exit; diagnostic forced code **0xE2490001** at **15:33:16.964 UTC**, interval **339.797 s**. Debugger interference prevents normal lifecycle qualification. |
| `04-ordinary-valid-stacks` / 32436 | Ordinary fixture, no screenshot, Alt+F4 | Actual normal exit **0** before any attachment at **15:49:26.752 UTC**, interval **43.516 s**. The debugger readiness window had already expired before launch; identity handoff correctly rejected the exited target. |
| `05-coordinated-stacks` / 50872 | Ordinary fixture, no screenshot, Alt+F4; readiness coordinated within the host launch operation | Actual normal exit **0** before attachment at **15:54:25.627 UTC**, interval **32.500 s**. No hung target supplied; debugger wait expired safely. |
| `06-mouse-close-stacks` / 11108 | Ordinary fixture, screenshot and Windows X; fresh coordinated debugger readiness | Actual normal exit **0** before attachment at **16:01:16.569 UTC**, interval **42.516 s**. No hung target supplied; debugger wait expired safely. |

Intervals begin at monitor observation after launch identity verification, not at process creation. Ordinary diagnostic bounds were 600 seconds total / 300 seconds after observed AppQuit to accommodate bounded debugger preparation. Forced termination is always a failed graceful exit. Attempts 04–06 conservatively retain `normal_lifecycle_eligible=false` because debugger interference was planned; their actual exit-0 observations are valid, but they are not a completed native UI/reopen qualification.

The visible smoke control initialized D3D12 and an independently measured 1280×720 visible client. Its canonical AppQuit save was logged at **15:08:57 UTC**; its fast exit occurred between monitor samples (`appquit_seen=false` is a sampling result, not missing log evidence). The later Unity memory report appears after `PlayerConnection::Cleanup`. Attempts 04–06 also emit the memory report; attempts 02–03 stop without it. All selected ordinary clients measured 1280×720. Attempt 06 also briefly had a 33×17 owned helper window; this is not a portrait measurement.

All ordinary fixtures came from the same genuine canonical `08-native-finish-post-exit.save.json` bytes, with exclusive namespace leases and no overlapping player. The visible smoke preserves the approved smoke driver, technical clock, canonical seed and Application.Quit exit. It proves that this sealed build can terminate visibly under that smoke configuration. It does **not** prove Windows X/Alt+F4 is defective: seed, clock, workload and trigger differ. Later ordinary exits show that neither closure gesture deterministically hangs. Timing/workload and environment involvement remain uncontrolled; no causal attribution follows.

## Installed debugger and exact symbols

Visual Studio 2022 Community **17.14.36811.4 / 17.14.23** and its native debugger are installed. Known SDK Debuggers locations and the user WinDbg alias did not contain CDB/WinDbg; WinDbg Appx lookup returned none. WindowsApps directory enumeration was denied and no permissions were changed. No debugger, symbol package or driver was installed, and no firewall/security/graphics configuration was altered.

The exact Unity 6000.3.2f1 installation contains matching native PDBs. PE CodeView RSDS GUID/age was compared with the PDB MSF7 information stream, rather than relying on filenames:

| Binary / PDB | Matching GUID, age | PDB SHA-256 |
| --- | --- | --- |
| UnityPlayer.dll / `UnityPlayer_Win64_player_development_mono_x64.pdb` | `e022d895-f6f4-4370-bb7f-2d4ddb9133df`, 1 | `16363fda7c32fd41595891341971ff3a298781363a88663a296990e09d547257` |
| Launcher / `WindowsPlayer_player_Release_mono_x64.pdb` | `15b56a56-7533-475a-9bed-d5871f75659b`, 1 | `9fc0c380b9e2725f163115872bd14de45e330a2040a2b28ce823f93e89f8bb90` |

The installed UnityPlayer.dll is byte-identical to the sealed DLL. Symbols were not copied into the artifact. Existing explicit Windows symbol caches contained no matching ntdll/KernelBase PDB candidates; that limited inventory is not an exhaustive disk search or a claim that public symbols are unavailable. A separate offline DbgHelp capability probe did not resolve its static lookup (deferred module, no loaded PDB); this is retained as a loader/probe limitation, not evidence that the validated Unity PDBs mismatch or that a particular shutdown function is blocked.

Only fresh campaign-owned IDE instances were used, through their debugger COM API, without opening a repository solution or editing source. The existing local Unity PDB path was supplied only to the new debugger child environment; the parent environment was immediately restored and no user/system symbol setting changed. Debugger attachment/break can suspend threads or alter timing and is explicitly excluded from normal lifecycle acceptance.

## Thread evidence: useful game stacks were not obtained

The early debugger preparations failed separately: invalid ROT binding context, unavailable optional interface casts, COM enumeration/unrolling errors, and an unavailable enhanced Attach2 dispatch. The documented core Process.Attach API did attach to PID 30588, but early readers queried the wrong thread collection. One capture had an invalid/null thread ID and later captures were empty. Those records and failures are retained; successful automation exit is **not** a successful stack capture.

The reader was corrected to enumerate **EnvDTE.Program.Threads**, explicitly index the COM thread/frame collections, and reject empty threads, null IDs and zero native instruction pointers. A disposable non-Unity child self-check obtained two samples, **three native threads per sample with nonzero instruction pointers**, then exited **0** through two handles. That validates the reader mechanics only, not Unity shutdown or symbol resolution. Self-check frames were largely unresolved addresses.

The first corrected-reader readiness interval expired before the player launch; this preparation failure is retained. Coordinating fresh readiness inside the same host launch operation corrected that timing problem for attempts 05–06. Both players then exited before attachment; bounded workers recorded no target and closed. Therefore there are **no accepted hung-game main-thread or worker-thread stacks, current instruction pointers, resolved wait chains or synchronization call sites** from this campaign. No useful loaded-module capture accompanied an accepted game stack. Prior D1 module/start-address observations retain their narrower meaning. No memory dump, private memory, save content, Locals/Arguments or expression evaluation is published.

Two owned IDEs remained after their earlier Quit attempts. Closeout verified exact path and creation time against campaign launch records, no debugged target, no saved solution path and zero projects, then gracefully requested Quit and observed exit. An empty debugger-created solution state was not mistaken for user work. Their exit-code property was unavailable; no IDE exit code is invented. The main game exit codes above come from native process handles.

## Ownership and next action

Observed: two additional ordinary hangs, one visible smoke exit and three later ordinary exit-0 observations under the unchanged artifact. Managed save success and native cleanup log markers show shutdown progress, not completion of every callback or process termination. No exact application method/state/failure mechanism is implicated. UnityPlayer, Development-player, graphics-driver and launch/Computer Use/Windows environment ownership remain unresolved. A thread's starting module, missing memory report or WCT without a cycle cannot assign blame.

The prior qualification process-observation defect is confirmed and corrected; the diagnostic reader preparation defects above are additional harness limitations. Neither explains away the real native hangs. No production change or workaround is justified, and normal lifecycle acceptance is not weakened.

**One next action:** at the next independently authorized, verified ordinary AppQuit hang, use the corrected, self-checked Program.Threads reader with coordinated fresh readiness to capture two comparable main/worker stack and instruction-pointer samples using the validated Unity symbols before owned-process termination. This task stops reproductions here rather than continuing until a desired result appears.

## Preservation, applicability and remaining gates

All players and campaign-owned IDEs are exited. Every ordinary namespace lease was restored only after both game handles signaled. The product audit retained **ten changed original SDK cache files** (eight pruned Analytics files, two rewritten Insights/shader files) and **eight newly generated cache files**. After process exit, generated replacements/new files were archived with their hashes and provenance; ten originals were copied non-overwriting from the independently preflight-hashed backup. The exact original **47-file inventory** is restored. No save or recovery file needed corrective restoration in this cache closeout.

The first final checker failed before a verdict because it used Git source enumeration on an ignored disposable project. That checker failure is retained. The corrected checker uses the existing target-inventory API and completed a new full recheck: **28,866 protected files with zero differences**, **1,418 source inputs unchanged**, all **five disposable projects matching 1,418 original files each**, and the unchanged **303-file sealed build**. Historical evidence, normal owner saves/recovery data and default TestResults.xml are unchanged. No Unity test framework invocation occurred in this task.

The full preservation report and exact evidence hashes are in the [machine-readable record](visible-quit-thread-diagnosis.json) and [sanitized packet](visible-quit-thread-diagnosis-reports.zip). Raw diagnostic material, logs, commands, save snapshots and SDK archives remain local in the fresh D2 directory.

No EditMode/PlayMode suite, Windows build or sustained profile was repeated. Existing qualified-source results remain focused **4/0/0**, full EditMode **2352/0/1**, full PlayMode **3741/0/10**, native filesystem **1/0/0**, and the three already-completed mandatory Editor 68-run methods. Their original source/input applicability is unchanged; this diagnosis does not requalify them or erase the historical **2345/7/1** failure.

The native lifecycle gate remains **failed/intermittent**. Outstanding native draft Save/Discard/Continue, invalid/pending/failed/stale safety, empty/incomplete history, strict graceful close/reopen and canonical/draft comparison, and remaining orientation/text checks were not resumed. No owner UAT build, fixture or launcher was prepared or handed off. Scripted smoke, native desktop diagnostics, Editor synthetic touches and physical Moonlight acceptance remain distinct.
