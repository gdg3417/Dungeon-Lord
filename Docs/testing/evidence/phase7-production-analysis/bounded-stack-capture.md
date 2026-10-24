# PR #249: bounded stack capture and interrupted-task recovery

2026-10-23. **Still blocked; cause unresolved (category 5).** The existing D3 campaign used both authorized ordinary-player attempts. No accepted hung-game stack was captured. No further player attempt or new campaign was started. No production correction is justified by these results.

## Exact source and artifact

Branch: `feature/phase7-production-analysis`. Starting and resumed HEAD: `e201717acdbeff664e1b0befa9b4fca4be4993d7`. Main: `fbe4bdff0cba9fea72b42fb6a05714327f880df4`. Qualified Unity source: `c870c5dc92aee87e854af350aa0a73653d045ff2`; original Unity inputs remain identical to implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. This delivery changes qualification documentation only.

Sealed artifact `P249R2-fe8daae0264348579db73d3402254215` remains the existing 303-file Development build. Executable SHA-256: `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`. UnityPlayer.dll SHA-256: `0b3ca5a98c9d9424ff7d3b311d50c5a2afb1282ad5d095e5535f29aed6b00874`. No rebuild, Unity input transformation, graphics/security setting change or tool installation occurred. Matching Unity and launcher PDB GUID/age and file hashes were reverified against the exact binaries.

The existing campaign is `TestResults/phase7-analysis-shutdown-20261023-d3`. Preflight protected 29,153 files, 1,418 original Unity inputs and the complete 47-file isolated product inventory. It also rechecked all 28,866 previously protected files without differences. The successful preflight was reused after interruption.

## State recovered after interruption

Before any mutating resume action, existing logs, terminal records, source identity and host process inventory were inspected. The first readiness gate had passed and player PID 46128 had been launched. No ordinary close action had occurred: the raw log has no AppQuit marker, no verified-hang or debugger-attachment record exists, and the 600.578-second observation still shows a visible 1280×720 game client with both native handles unsignaled. The preconfigured 600-second overall bound then terminated only the owned player handle. This is an **interrupted diagnostic procedure**, not a reproduced shutdown hang.

Both handles recorded actual exit `3796434945 / 0xE2490001` at 17:07:27.448 UTC. The fixture lease was restored after actual exit. The debugger target wait expired and the debugger closed. Native identity checks on resume confirmed the player, non-Unity child PID 19752 and debugger PID 51204 absent/exited. No relevant diagnostic helper remained. The existing Unity Hub service was identified and left untouched. All 303 artifact hashes and the original leased-save hash matched.

Exactly one player launch had occurred, leaving one authorized attempt. A new, separately named diagnostic-script revision allowed only `attempt-02` after verifying this specific interrupted outcome; it did not reset the counter or modify the original attempt's script/evidence. The first debugger's nullable Process.Path field was also replaced by a host CIM executable-path read for the second launch record. No successful preflight or historical output was overwritten. Fresh readiness was necessary because the first debugger had already exited.

## Readiness and process procedure

Each fresh Visual Studio instance used the corrected **EnvDTE.Program.Threads** reader with explicit one-based COM thread/frame enumeration. Null IDs, empty/failed frame enumeration and zero/failed native contexts are rejected. The debugger captured two samples separated by five seconds from a disposable non-Unity child, detached, and awaited independently verified child exit before issuing launch readiness. Each gate produced **four threads, 34 frames and four nonzero instruction pointers in each sample**; each child exited **0** through both native handles. These are debugger setup checks, not Unity qualification results.

The same live debugger connection remained available for the subsequent player. Readiness was checked immediately before launch, with a 30-second immediate-launch handshake. The game-target timeout began only after `game-launched.json` existed. A heartbeat recorded ongoing COM availability and absence of an attached target.

The monitor retained the launch handle and an independent OpenProcess query/synchronize handle. Exact executable/hash, command line, PID and creation FILETIME were checked against host identity. Query errors remain unknown. Attachment required logged AppQuit, disappearance of visible owned windows, at least 15 seconds of continued matching native-handle liveness, an exact identity recheck and a recent live debugger heartbeat. No game attachment was made before these conditions.

Each player used a fresh exclusive lease of the approved compiled qualification namespace in product `DL B3B2 Smoke Two Floor P249R2`. Both loaded the same canonical `08-native-finish-post-exit.save.json` bytes, SHA-256 `5b4db54b895ac2c0e2e0ee4c8e502fa028914b6ea196c966089fa4c5dd9729ab`, using the ordinary clock and unchanged windowed 1280×720 options. No historical checkpoint/history content was manufactured. Post-exit save archiving and lease restoration require both handles to signal.

## Two-attempt outcome

| Attempt | Player | Result |
| --- | --- | --- |
| 01, before interruption | PID 46128 | Readiness passed. Ordinary window was never closed. Overall 600-second bound forced exit **0xE2490001**; no AppQuit or verified shutdown hang. Failed/incomplete procedure, retained as such. |
| 02, after recovery | PID 25988 | Fresh readiness passed. Normal Alt+F4 closure; AppQuit at **18:16:45 UTC**, both handles signaled actual exit **0** at **18:16:46.571 UTC**. Unity memory report followed PlayerConnection cleanup. No attachment and no surviving child. |

For attempt 02, an initial accessibility Close-button click returned `coordinate input geometry is unavailable`; a fresh accessibility observation confirmed the player remained visible, then normal Alt+F4 was sent. No screenshot was requested. The immediate UI refresh still listed the window during shutdown; it was not treated as evidence of either process exit or a hang. The native monitor supplied the terminal result.

The main visible game client was independently measured as 1280×720. Its HWND-owning thread was recorded (attempt 02: TID 35772), but there is no game stack to justify a stronger engine-main-thread attribution. The successful exit did not remain alive for the 15-second hang threshold. Both namespace leases were restored after terminal records. The two-attempt allowance is exhausted.

## Stack evidence and classification

**Accepted native game samples: zero.** Neither attempt supplied a verified hung game to the debugger. There are no accepted game main/worker stack frames, current instruction pointers, resolved synchronization chains or comparable stable waits from D3. Exact PDB availability does not demonstrate resolution of a game stack that was never captured. Non-Unity reader samples remain setup evidence only.

Confidence is high in the recorded process outcomes and in the interrupted-procedure classification of attempt 01. Application, UnityPlayer/Development-player, driver and environment ownership of the previously reproduced intermittent shutdown failure remain unresolved. The first attempt's deadline and the unavailable UI click geometry are diagnostic procedure/input issues; neither identifies the cause of the historical shutdown hangs. The normal second exit does not erase those failures or complete strict UI/reopen qualification.

No production method, resource owner or failure mechanism is identified, so no production correction or graphics workaround is proposed. No raw memory dump, private save, complete process command line or sensitive debugger activity content is published. Sanitized findings and evidence hashes are included in the accompanying packet; raw material remains local in D3.

## Preservation and remaining gates

All recorded players, non-Unity children and debugger instances exited; native host checks confirmed their terminal/absent state. The initial product audit retained ten changed original Unity SDK cache files: eight pruned Analytics files and two rewritten Insights/shader files. Four new cache files were also recorded. After process exit, generated replacements and new files were archived with verified provenance, and the ten originals restored non-overwriting from the D3 preflight backup. The original 47-file product inventory matches exactly. No owner save or recovery file needed corrective restoration; ordinary namespace leases are separately recorded.

The final complete recheck passed: **29,153 protected files with zero differences**, **1,418 original Unity inputs unchanged**, all **five disposable projects matching 1,418 original files each**, and the unchanged **303-file sealed build**. Original player saves, recovery files, owner fixtures, historical builds/reports and default TestResults.xml are unchanged.

The [machine-readable record](bounded-stack-capture.json) and [sanitized packet](bounded-stack-capture-reports.zip) contain source applicability and evidence hashes. Initial differences and unsuccessful outcomes remain separate from the final restoration verdict. Previous D1/D2 reports and failures retain their original classifications.

No full EditMode/PlayMode suite, Windows filesystem test, 68-run profile or Windows build was rerun. Existing qualified-source results remain EditMode **2352/0/1**, PlayMode **3741/0/10**, Windows filesystem **1/0/0**, and three completed mandatory 68-run Editor profiles. Source applicability is unchanged.

The native lifecycle gate remains unresolved/failed. Dirty-draft Save/Discard/Continue, invalid/pending/failed/stale safety, empty/incomplete-history Analysis, strict graceful close/reopen with canonical/draft comparison, remaining orientation/text checks and owner Moonlight acceptance remain outstanding. No owner UAT fixture or launcher was prepared. PR #249 remains open, draft, unmerged and blocked from owner UAT.

**One recommended next step:** independent review of the accumulated shutdown evidence and D3's exhausted attempt budget to decide the next diagnostic or qualification disposition. Do not infer authorization for another reproduction campaign from this report.
