# PR #249: resumed functional qualification and owner preparation

2026-10-23. **Ready for independent review of functional results, the prepared owner environment and the coverage limits below. Owner UAT has not run.** PR #249 remains open, draft and unmerged. No PR-specific production defect was demonstrated, and no production correction was made.

## Source, disposition and preserved gates

Branch `feature/phase7-production-analysis`, starting HEAD `8978cb41ca5c4a8692fd225929bd34932e69a260`; main `fbe4bdff0cba9fea72b42fb6a05714327f880df4`. Qualified source remains `c870c5dc92aee87e854af350aa0a73653d045ff2`, with original Unity inputs identical to implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`. The original implementation baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`; no merge/rebase/cherry-pick occurred. This delivery adds qualification documentation only.

The sealed 303-file Windows Development artifact remains `P249R2-fe8daae0264348579db73d3402254215`. Executable SHA-256: `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6`. UnityPlayer.dll SHA-256: `0b3ca5a98c9d9424ff7d3b311d50c5a2afb1282ad5d095e5535f29aed6b00874`. Product remains `DL B3B2 Smoke Two Floor P249R2`; company/application identifiers and compiled qualification save namespace are unchanged.

Owner clarification changes the disposition of the unresolved Windows closing problem: the owner reports it is longstanding and has historically required Task Manager. The original pre-PR baseline already contains the native-stall comment and read-only technical-player ExitProcess path in `Assets/_Project/Scripts/BuildTools/CombatCheckpointDevelopmentSmoke.cs:60` (file last changed at `5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d`). That smoke concerns Mono Environment.Exit, so it supports historical host shutdown limitations without proving identical root cause for ordinary window closure. **All failed graceful exits remain failed; the graceful-shutdown gate is not passed.** Further root-cause/debugger investigation was stopped as directed. No lifecycle, graphics, security, simulation or tuning changes were made.

Existing exact-source gates were retained without rerun:

| Gate | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused preparation | 4 | 0 | 0 |
| Complete EditMode | 2352 | 0 | 1 |
| Complete PlayMode | 3741 | 0 | 10 |
| Windows filesystem | 1 | 0 | 0 |

The four passing scripted-native continuation phases and three completed mandatory 68-run Editor profiles retain their original evidence/applicability. No Editor, Unity test framework, profile, build or debugger was launched in this continuation. Earlier **2345/7/1** and all D1–D3 failed/inconclusive results remain immutable.

## Fresh isolation and controlled close

New output: `TestResults/phase7-analysis-functional-20261023-f1`. Preflight rechecked the D3 preservation boundary, protected **29,302 files**, captured **1,418 Unity inputs**, verified the **303 build files**, and backed up the exact **47-file product inventory** before launch. A preparation script stopped once before creating the functional monitor because a source-fragment match failed; it was corrected before any player launch and is not a game/test failure. The successful preflight was not repeated.

Each session exclusively leased only the compiled isolated save namespace. Exact fixture copies were installed; original save/recovery bytes were backed up rather than loaded or modified. Both the actual launch handle and an independently opened native query/synchronize handle remained monitored, with exact executable, PID, creation FILETIME, host identity and command checked. Unknown/error states never establish exit. No overlapping player was launched. Canonical snapshots and lease restoration occurred only after both handles signaled, with a surviving-child check.

Ordinary Alt+F4 initiated each close. Where a successful canonical AppQuit save was logged but the window disappeared and the process stayed alive for 30 seconds, the monitor first recorded failed graceful shutdown, then terminated only the exact verified launch handle with `0xE2490001`. An absent AppQuit success marker is not sufficient for that controlled termination path. No failed/incomplete expected save was bypassed. No new shutdown reproduction campaign or thread investigation was run.

| Session | PID | Actual exit | Functional purpose |
| --- | ---: | --- | --- |
| 01 empty/drafts | 52424 | **0**, normal | Empty report; valid Continue/Discard/Save; invalid intent; close with durable draft |
| 02 portrait recovery | 30712 | **0xE2490001**, failed graceful exit | Resume exact journal; invalid Default/Large dialog; Discard; portrait report |
| 03 exact forced-exit reopen | 7228 | **0**, normal | Reopen session 02 bytes; committed placement retained, discarded journal absent |
| 04 live history | 50548 | **0xE2490001**, failed graceful exit | Reporting, automatic parties, replay transitions, covered controls, desktop Large |
| 05 partial history | 54788 | **0**, normal | Optional-summary coverage; portrait Default/Large; saved partial report |
| 06 stale/partial reopen | 14496 | **0xE2490001**, failed graceful exit | Stale recovery; real filesystem-failed Discard and retry; partial report reopening |
| 07 retained reopen/phone landscape | 11660 | **0xE2490001**, failed graceful exit | Exact session 04 bytes; preserved outcomes; 640×360 Default/Large navigation |

Thus **3 normal exits and 4 failed graceful exits**, with no surviving children. These lifecycle counts are separate from functional check results.

## Native functional results

The machine-readable checklist records **28 passed native check rows**, **0 demonstrated functional failures**, and **3 exact edge cases not directly exercised through native input**. These are explicit manual check rows, not new NUnit totals. The separately failed graceful exits above are not hidden by that functional classification.

- **Valid draft:** an actual skeleton reposition was confirmed into an acknowledged draft. Continue retained it; Discard opened Analysis and restored its original position; a repeated move saved successfully and opened Analysis. Returning restored normal controls and the committed position. No direct controller invocation drove these choices.
- **Invalid draft:** an ordinary room move outside the floor produced a durable invalid intent, localized reason and disabled Save. Analysis offered only Discard Changes and Continue Editing. Continue preserved it; after verified close/reopen, normal recovery restored the intent and Discard navigated safely. Both choices and the reason fit the 360×640 Default/Large dialog.
- **Stale and failed storage:** an exact authority-written older journal was combined with a later, different canonical baseline as an explicit stale fixture. Recovery correctly refused resume/merge and offered no Save. A bounded, read-only share lock on only the copied journal receipt caused actual Discard deletion to fail. Recovery remained blocked with localized retry feedback. After releasing the lock, retry through the existing authority succeeded, removed the journal and restored normal controls. No canonical file was locked or altered to induce this failure.
- **Empty and partial history:** the empty funded starter came from the existing canonical writer/QA funding authority. The partial fixture is a detached synthetic copy of the genuine two-run save: only SurvivalSummary, LootExtractionSummary and FinalRouteOutcomeKey were omitted from its first retained record. Exact field omissions/source hashes are recorded separately; original history, spatial state, active checkpoints, chronology and LatestOutcome were untouched. The sealed player validated it on normal load. Analysis initially showed 1/2 coverage, five survivors and extracted value 35, then increased coverage correctly as genuine new runs completed. Missing values were excluded rather than synthesized as zeros or causes.
- **Living Dungeon:** Analysis stayed open while retained runs increased from 2 to 4, survivors 10→17 and extracted value 78→148. The process logged only one Boot initialization. Further completions continued normally. Analysis did not recreate gameplay authority.
- **Replay and input:** a compatible current-session run replay opened through its actual button. Opening Analysis closed it; Dungeon return restored the normal compact header without restarting replay. Clicking the covered Edit location while Analysis was open did not activate Dungeon editing. Saved history initially showed honest transient-evidence unavailability.
- **Responsive presentation:** native client observations independently confirm 1280×720, 360×640 and 640×360, with Default/Large text, Analysis/Display/Dungeon navigation, compact entry and report scrolling. Narrow Dungeon panels/HUD require their bounded scrolling; compact text can ellipsize. These are Windows desktop mouse/scroll observations, not physical phone input, injected phone safe-inset acceptance or a readability verdict from the owner.

### Explicit native coverage limits

1. **Transient Pending Save-disabled state:** not reliably held through ordinary desktop input; the unchanged controller flushes it on its next frame. The passing qualified scene tests exercise Save-disabled/no-navigation and acknowledgement behavior. No delay, clock adjustment or runtime instrumentation was added to hold that state.
2. **In-session stale navigation:** the exact live three-choice Save-disabled state retains passing automated coverage. Native stale **recovery** was tested above; it is not relabeled as the same state.
3. **Authoritative Save rejection:** the exact rejected-commit/no-navigation case retains passing automated coverage. Native filesystem-failed **Discard** and successful retry were tested; they are not relabeled as failed Save.

The exact retained EditMode cases are `PhaseSevenA4ProductionScene.AnalysisPendingAndStaleSaveRemainDisabledWithDraftIntact` and `AnalysisFailedDiscardAndFailedSaveNeverNavigateOrLoseDraft`. Their PlayMode wrappers are `PhaseSevenA4ProductionShellPlayModeTests.AnalysisPendingAndStaleDrafts` and `AnalysisFailedTransactionsKeepDraft`. All four case results are Passed in R2 `02-full-edit.xml` and `03-full-play.xml`; report SHA-256 values and fully qualified case identities are retained in the sanitized packet's `automated-coverage-applicability.json`. These are existing results against qualified source `c870c5dc92aee87e854af350aa0a73653d045ff2`, not reruns. The pending/stale case checks disabled Save, retained draft identity, no navigation and subsequent acknowledgement; the failed-transaction case drives the confirmation button after authoritative mana rejection and checks no navigation or draft loss.

Independent review should explicitly accept that coverage disposition or require a separately scoped native test mechanism before handoff. Automated coverage supports the unchanged authority behavior, but cannot establish native input accessibility for those exact transient states. No production defect justifies adding such instrumentation to this PR. Physical Moonlight acceptance remains unperformed.

## Save and history integrity

- Session 01 changed only the intended assignment X position plus legitimate clock/scheduler/lifecycle fields. The invalid room intent remained in the journal and was never committed to canonical floors.
- Session 02 resumed that journal, then discarded it. Canonical spatial floors and run history remained exactly equal to session 01. After its failed graceful exit, session 03 loaded the exact post-termination bytes: committed placement persisted and no discarded recovery remained. No original owner file was used as the active fixture.
- Session 05→06 preserved all **4** previously retained records exactly while normal progression grew the window to **8**. Independent coverage still excluded the missing first summaries. Canonical floors were exact and run IDs were unique.
- Session 04→07 preserved all **7** prior records exactly while normal progression grew the window to **9**. No duplicated or modified completed record was found, and canonical floors were exact. Next-run sequences were monotonic. Clock, passive mana, automatic party/checkpoint/settlement, loot and economy changes are legitimate progression, not attributed to read-only navigation.

These comparisons establish normal save/reopen correctness under verified process termination. They do not convert a forced process termination into a graceful-shutdown pass.

## Preservation and owner package

After all players exited, the initial product audit retained **10 original SDK cache differences** (8 pruned Analytics files; 2 rewritten Insights/shader files) and **4 new SDK cache files**. Generated replacements/new files were archived with provenance, then originals restored non-overwriting from preflight-hashed backups. The original 47-file inventory is exact. Save/recovery files needed no corrective restoration beyond the declared per-session namespace leases.

The full final recheck passed: **29,302 protected files unchanged**, **1,418 original Unity inputs unchanged**, all **five disposable projects matching 1,418 original files each**, and all **303 sealed-build hashes unchanged**. Historical reports/builds, default TestResults.xml, owner fixtures and normal save/recovery data are preserved. No Unity operation produced a new default test report.

Prepared owner mirror: `C:\Dev\Dungeon-Lord\Temp\pr249-analysis-owner-f1`, with all 303 files copied and hash-matched rather than rebuilt. Five isolated scenarios cover main automatic gameplay, funded editing/empty history, partial summaries, recovered invalid draft and stale recovery. **PR249 Analysis UAT** is a newly created desktop shortcut. A graphical launcher manages exclusive leases, process identity/exit, logs, saved-state archiving, per-scenario reopen and exact original-product restoration. It stops on incomplete saves, unmatched draft receipts, process uncertainty, duplicate retained IDs or preservation differences.

Its package/fixture hash checks, synthetic preservation dry run and native two-handle child exit-23 check passed. The launcher window/scenario controls were visually inspected and then closed; **zero owner game sessions were launched**. One Computer Use inspection approval timed out, and its refreshed retry succeeded. The mirror has not been represented as a separately rebuilt or newly qualified binary. Owner use is host-bound to the prepared runtime and remains acceptance work.

See [owner Moonlight steps](owner-moonlight-uat.md), [machine-readable results](functional-qualification.json) and [sanitized screenshots/process evidence](functional-qualification-reports.zip). Raw saves, complete commands, SDK backups and private logs remain local in F1.

## Subsequent owner acceptance

On 2026-10-24 the owner confirmed that PR #249 UAT passed. This is a later owner acceptance report; it does not alter the source applicability or counts above, and no scenario-by-scenario evidence, device, resolution, orientation, or platform details accompanied the confirmation. The earlier directly observed PC testing and its reported limitations remain recorded as such. See the [owner acceptance addendum](owner-uat-acceptance.md). No native iOS qualification is claimed.

Repository issue searches for shutdown/hang/quit did not locate a matching lifecycle issue. Recommend a separate tracked issue, **Windows player remains alive after AppQuit and window close**, linking the historical D1–D3/F1 evidence and owner report. Do not infer a resolved cause or implement its fix here.

Technical qualification complete, pending independent review of remaining native coverage and owner Moonlight acceptance.

This disposition does not accept the three native coverage exceptions on the reviewer's behalf or mark graceful shutdown passed. The prepared launcher is held at that independent-review boundary. Owner UAT and merge approval remain outstanding.
