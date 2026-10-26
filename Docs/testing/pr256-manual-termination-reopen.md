# PR256 same-save reopen after owner-forced termination

Date: 2026-10-26. PR: #256. UAT implementation/build source: `83254c8b857cfc26461cd72961a1114f5ce3cfe2`. Product: `Dungeon Lord PR256 UAT 20261026-112751-1198f0`; executable: `Builds/UAT/PR256-20261026-112751-1198f0/Dungeon Lord UAT.exe`; previously qualified 299-file payload manifest SHA-256: `36fa42d4985aa64c9e13b9d68ec36df669a532437786ae9589a3a1c1e5b5727c`. No rebuild or runtime/data change occurred.

## Termination and exact reopen

The owner used Task Manager to end the stalled original UAT process, PID 9616. This is a **forced termination**, not a successful ordinary shutdown. Before relaunch, host-level process inspection confirmed PID 9616 absent and no `Dungeon Lord UAT.exe` instance running. The separate diagnostic-only Visual Studio helper from the [targeted shutdown diagnosis](pr256-targeted-shutdown-diagnosis.md) was also absent; Codex did not terminate either process. There was no concurrent UAT player.

The original second-AppQuit save remained 124,849 bytes, with SHA-256 `7c48d4dace940c22e34dcd663414b5a811dc7bed17a2daadb63610d07378f520`, identical to its existing read-only evidence copy. Strict double-read/JSON/shape checks found one canonical save at the namespace root before launch. Codex launched the exact same executable and product identity without resetting, moving, repairing, replacing or renaming that save. The new player was PID 29244, created at 2026-10-26 16:02:06 UTC. Its normal product `Player.log` and canonical save are in the same namespace.

The application loaded the owner-modified dungeon and published one offline passive-mana grant and one Boot save at 16:02:11 UTC. The normal 1280×720 interactive window displayed Floor 1, two rooms, active adventurers and retained run history, without an invalid-save or recovery banner. A reversible Collapse Details / Expand Details interaction succeeded and restored the original view. The game was left open for Moonlight interaction.

## Durable state comparison

A stable post-reopen canonical capture at 16:05:30 UTC has SHA-256 `f702c2c2fb55277654a9a79bbb68a0e09bd7ed721df56f8512e3499d1701b3d5`. The application itself loaded and continued writing this save; the independent inspector additionally rejected duplicate JSON keys/nonfinite values and checked basic owner/reference shape. Raw save bytes and full semantic snapshots remain private under `TestResults/pr256-uat-manual-termination-reopen/20261026-01`.

| Authority | Before forced-termination reopen | Stable post-reopen capture |
| --- | ---: | ---: |
| Schema / mode / fractional component | 16 / `legacyWholeSeconds` / 0 | 16 / `legacyWholeSeconds` / 0 |
| Accounted Unix seconds | 1793023481 | 1793030729 |
| Accepted / closed logical horizon | 8421882 / 8421882 | 8620093 / 8620093 |
| Total simulation ticks | 842 | 862 |
| Mana reserve | 103.86916666666652 | 75.73666666666652 |
| Rooms / content assignments / returned custody | 2 / 6 / 0 | 2 / 6 / 0 |

The complete permanent-owner digest is unchanged, covering layout, room/content assignment identities and positions, structural lifecycle/custody and the original 105-mana structural investment. The Research/progression digest is unchanged. Accepted and closed horizons advanced monotonically and remain equal; no A>C recovery case was created by this session. The same two monster assignments are present at readiness health 6 and 7.

Run IDs advanced without duplication. The captured new completed outcomes are `run-8` through `run-14`, each successful and clearing both configured rooms. Previously retained `run-5` through `run-7` have identical outcome hashes. Earlier `run-1` through `run-4` aged out of the configured ten-entry recent-history window as new outcomes arrived; they were not replayed. The next-run allocator advanced from 9 to 16, and the prior admitted party sequence 8 settled before the new active party sequence 15. This is foreground progression after reopen, not offline adventurer catch-up.

## Mana and authority reconciliation

Configured peaceful passive mana is 180/hour; legacy offline efficiency is 0.15, or 27/hour. The snapshot equation reconciles exactly:

`103.86916666666652 + 52.8675 offline + 10.0 from 20 foreground ticks − 91 recovery = 75.73666666666652`.

The offline amount corresponds to an inferred 7,049 whole seconds between the old accounted boundary and the boot observation, consistent with the 16:02:11 grant log. Seven new completed two-room runs, each restoring monsters with health 6+7 at the configured one mana per health, account for 7×13=91 recovery mana. The arithmetic residual is zero. The interval and recovery debit are **inferences** from canonical snapshots, configured rates, outcomes and one grant log; this qualification did not add event-local debit logging. The player log contains exactly one offline-grant persistence entry, one Boot save, no error-level message, no invalid-save/current-schema refusal and no `gd66.authority.contradictory_state`.

The preservation audit found zero missing, added or altered files among 477 protected files in other save namespaces. The 304-file earlier-qualification inventory has no missing/added files; its known active session log only has the previously recorded append, and all other files match. The owner's original planning-document modification retains its previous hash. No existing qualification result was overwritten.

## Lifecycle classification and review boundary

The first PR256 ordinary close exited with code 0 and its same-save reopen passed. The second ordinary close durably saved but left the process running without a window; that failure remains recorded. The owner-forced termination followed by this exact same-save reopen proves the durable save survived that termination. It does **not** count as a second graceful shutdown or establish the cause of the intermittent native hang.

The symptoms closely match [PR249's pre-H1-F2 intermittent visible shutdown findings](evidence/phase7-production-analysis/visible-quit-thread-diagnosis.md), including successful AppQuit save and a process remaining after native cleanup. Neither investigation captured a current blocking game stack. No concrete H1-F2 implementation regression or save-authority contradiction has been identified. This known native lifecycle limitation requires explicit consideration in independent merge review; it is not silently marked passed. The evidence supports **independent review of PR #256**, while leaving the PR open and unmerged for that decision. No new H1 task began.
