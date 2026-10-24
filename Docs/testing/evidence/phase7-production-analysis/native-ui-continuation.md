# PR #249 native UI continuation — 2026-10-23

Status: **still blocked at native qualification**. No production code, tests, tuning, localization, schema, migrations or save authority were changed. No owner UAT was launched or prepared. PR #249 remains open, draft and unmerged.

## Exact applicability

The user dismissed the Windows Security dialog. Work resumed on clean branch `feature/phase7-production-analysis`, evidence HEAD `b6bfb7a4358cf733fe9fd0d0ea929ad9bdeab02b`. A fresh fetch reconfirmed main `fbe4bdff0cba9fea72b42fb6a05714327f880df4` and that PR HEAD. The original baseline remains `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`; main was not incorporated into the branch. The 1,418 original Unity inputs remain identical to qualified source `c870c5dc92aee87e854af350aa0a73653d045ff2`, previous execution source `d4071924993c67f2b22722a539be37eea781017e` and implementation `948a38dc6f47b1419164fdfe8f44a2b3c9fd31f5`.

The unchanged sealed Development build is `Temp/ui-composition-analysis-p249r2-native/Builds/Development/Windows/Dungeon Lord.exe`, artifact `P249R2-fe8daae0264348579db73d3402254215`. Its 303 hashes were rechecked after native shutdown. Product is explicitly isolated: `DL B3B2 Smoke Two Floor P249R2`, with only the six previously declared disposable input transformations. No ordinary player build is distributed.

## Results retained from preparation recovery

No full suite or additional sustained campaign was rerun for this evidence-only continuation.

| Gate at qualified source c870c5dc… | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Focused preparation prerequisites | 4 | 0 | 0 |
| Complete EditMode | 2,352 | 0 | 1 |
| Complete PlayMode | 3,741 | 0 | 10 |
| Native Windows filesystem fixture | 1 | 0 | 0 |

All three full-suite Editor profiles completed their required 68 runs. No separate sustained Windows campaign or audit instrumentation was added. The production build succeeded with 0 errors and 1 symbol-upload warning. The four existing opt-in native continuation phases exited normally with passing reports. Their source applicability and exact counts remain in [preparation recovery](preparation-recovery.md) and its immutable packet. The earlier **2,345 / 7 / 1** failed full EditMode report remains failed and unchanged.

## Visible native observations

Computer Use operated only the selected isolated game window. These are native Windows mouse/scroll observations, not physical phone acceptance or new NUnit counts.

| Case | Observed result |
| --- | --- |
| Ordinary Dungeon landing, retained summary and automatic party | Passed visually; real current-session completions and active parties were present. The retained window reached its configured 10-record cap during the long ordinary-clock session. |
| Compatible Replay Run button | Opened an actual completed-run replay with Pause / Next action / Close Replay controls. |
| Analysis from replay, then Dungeon return | Analysis opened; returning restored the normal compact header and live Dungeon presentation without restarting replay. |
| Covered Dungeon Edit button while Analysis open | Clicking its former screen location did not enter Edit or expose Dungeon controls. |
| Display and Default/Large text in landscape | Opened and changed text through actual buttons. Analysis remained scrollable and return remained available. |
| Clean Edit draft → Display → Analysis | Navigated immediately without a save/discard prompt; normal Dungeon controls returned afterward. |
| Requested 360×640 portrait, Default | Header scroll exposed Display; Display controls worked and Dungeon return was reachable. |
| Portrait, Large | Compact Analysis entry remained actionable despite ellipsis. Navigation wrapped; report scrolled through all totals and historical-evidence availability text. Display could restore Default and Dungeon return worked. |
| Dirty valid/invalid/pending/failed/stale native dialogs | Not completed; later native stages stopped at the exit failure. Their automated coverage is not relabeled as native verification. |
| Empty/incomplete-record native presentation | Not completed; next exact authority-generated starter installation was blocked before any fixture mutation. |
| Strict visible-player save/close/reopen | **Not qualified**, for the process-lifetime and preparation reasons below. |

Requested landscape was 1280×720; captured outer window was 1282×752. Requested portrait was 360×640; captured outer window was 362×672. These outer captures are consistent with the requested client sizes plus Windows decorations, but no independent client-resolution report was captured. Native safe area was the desktop client; the injected phone inset `(10,18,340,604)` remains covered by the earlier focused automated tests, not by a physical phone claim.

Screenshots in the continuation packet show actual replay, Analysis at both landscape text sizes, and Large portrait Dungeon/report top/report end. The compact portrait entry deliberately ellipsizes. The Dungeon header and detailed report require scrolling at this size; no new UI defect was demonstrated by these interactions.

## New failure and procedure correction

Both ordinary visible players closed their windows and logged successful AppQuit saves, input shutdown, physics cleanup and `PlayerConnection::Cleanup`, but did not terminate:

- Landscape PID 38808: AppQuit at 13:13:41 UTC; still alive with no window at 13:22:12 UTC.
- Portrait PID 39392: AppQuit at 13:17:12 UTC; still alive with no window at 13:22:12 UTC.

CPU totals were unchanged over the final three-second sample (28455.703125 and 81.828125 seconds respectively). This is a native shutdown failure, not a passing close/reopen. Production versus environment/Development-player cause is **unclassified**. The raw logs contain the existing ComputeBuffer-disposal warning; that warning alone does not establish the hang's cause or a memory leak. No speculative production correction was made.

A procedure error also occurred: the first post-window-close sandbox process query returned no player, so the landscape save was provisionally copied and portrait was started without verifying actual host process exit. The stronger host-level guard subsequently found both processes and rejected the next fixture preparation before creating its directory or copying/moving any save. Consequently `10-native-ui-post-exit.save.json` and its archive description are **provisional/mislabeled**: the process had not actually exited. Neither that snapshot nor the portrait launch establishes strict non-overlapping reopen qualification. The scripted `06`–`09` continuation executions did wait for real process exit and retain their separate applicability.

Attempt `11-native-ui-portrait` failed to launch usefully in sandbox low-integrity mode (312-byte startup log, no game window); it is not a UI pass. Attempt `12` used normal integrity and produced the portrait observations above. All attempts and failures are retained.

After recording exact executable paths, commands, PIDs, absent windows, unchanged CPU totals and shutdown log markers, Codex terminated only those two campaign processes. No unrelated player, Unity Editor or Hub service was terminated. Only after verified termination was the complete new-product directory archived. Further native launches, fixture substitutions, owner preparation and UAT stopped pending independent review. No save bytes were rewritten to manufacture a pass.

## Preservation and remaining gate

The new product directory is archived under `TestResults/phase7-analysis-recovery-20261022-r2/13-native-closed-product`, with exact file hashes. All five disposable projects now match **1,418 original source files each**, byte-for-byte; the six native qualified versions were archived before restoring their verified original source bytes. The sealed build is unchanged. The final comparison verifies **350 original persistent files** and **26,181 protected historical build/report/evidence files**, with zero hash differences. No protected file required restoration.

See [machine-readable continuation applicability](native-ui-continuation.json) and [screenshots, process proof and closeout reports](native-ui-continuation-reports.zip). Raw original logs and canonical snapshots remain in the isolated local output directory; the packet includes hashed, selected log evidence without network identifiers. The earlier recovery packet and historical failed campaign are preserved.

Independent review must resolve the shutdown failure and approve a host-level exit guard before repeating strict visible-player reopen and the remaining native draft/empty-history cases. Full suites remain applicable while Unity source is unchanged; rerun scope must follow any actual correction. An owner build/fixture/launcher and sequential Moonlight procedure are **not ready**. The owner should not launch the current technical build as a prepared acceptance environment. Recommendation: **still blocked**.
