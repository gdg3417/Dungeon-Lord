# PR256 owner UAT persistence closeout — blocked

Implementation/build source: `83254c8b857cfc26461cd72961a1114f5ce3cfe2`; qualified runtime/data commit `591b2c3cb27b6d71057cb648ae692609ab4346ab`; verified main/base `c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d`. This closeout changes evidence/documentation only. No runtime/data changes, rebuild, new PR or full-suite rerun occurred.

Product: `Dungeon Lord PR256 UAT 20261026-112751-1198f0`. Interactive executable: `Builds/UAT/PR256-20261026-112751-1198f0/Dungeon Lord UAT.exe`. Payload manifest SHA-256: `36FA42D4985AA64C9E13B9D68EC36DF669A532437786AE9589A3A1C1E5B5727C`; all 299 payload files were rechecked unchanged. The exact live instance and canonical save namespace were identified before any input. No older negative-test namespace was used.

Model: GPT-6.1 Sol Medium; Standard evidence closeout with established lifecycle/save boundaries. Fallback: GPT-6 Sol Medium. No save/recovery contradiction was found requiring escalation. The native shutdown issue below remains undiagnosed; no code correction is attempted.

## Owner results

| Owner checklist item | Result |
|---|---|
| Appearance | PASS |
| Room building/editing | PASS |
| Monster/trap/loot placement | PASS |
| Adventurer simulation | PASS |
| Phone usability | PASS |
| Research and mana, combined owner checklist | NOT TESTED; Research UI is intentionally unavailable |
| Player pause/resume | NOT TESTED; player-facing pause is intentionally unavailable |

There were no owner-reported gameplay/UI failures. Neither unavailable interface is treated as a defect or implemented here. Codex's controlled Windows focus test below is separate from the owner's untested player-pause item.

## Persistence observations

Stable double reads and strict JSON parsing preceded snapshots; originals were only read. Raw evidence copies were created separately and marked read-only. At every captured boundary, the namespace root contained only `save_primary.json`, with no journal/recovery/draft sidecar. Duplicate JSON keys and nonfinite values were rejected by the independent inspector. Schema/owner shape, positive supported seconds, C<=A, room/content identity uniqueness and assignment room references passed. Full contextual canonical acceptance is established by the real application reopen, not claimed from the independent JSON checks alone.

| Phase | Result | Saved seconds | A = C | Wallet | Save SHA-256 |
|---|---|---:|---:|---:|---|
| Owner state before activation/close | Stable, protected | 1793022543 | 8278618 | 155.4216666666665 | `c247c7521d362213e37451bbfcf3faf9b5643b6a0851739e88e43c03b0bde82e` |
| First normal close | PASS; exited code 0 | 1793023254 | 8327310 | 136.8866666666665 | `d891fc6cf0373180065a86f121eb60e4a1065a9d704ab7e6152d99d4231b781e` |
| First reopen | PASS; same usable UI and retained owner dungeon | 1793023382 | 8337673 | 138.2641666666665 | `5c72723b8b16b39657437d7ee4e4de3f69b5006eb981e307b8c78e5a65a9e05f` |
| Second AppQuit durable save | Save passed; process exit FAILED deadline | 1793023481 | 8421882 | 103.86916666666652 | `7c48d4dace940c22e34dcd663414b5a811dc7bed17a2daadb63610d07378f520` |
| Second reopen | NOT PERFORMED; existing process remained | — | — | — | — |

Every captured save is schema 16, FormatVersion 1, Mode `legacyWholeSeconds`, AccountedFraction100ns zero. Accounted seconds and A/C are monotonic. No captured A>C case occurred; that recovery path retains its existing automated evidence and is not newly claimed as Windows UAT coverage.

The permanent owner digest remains `d6ccca2cfa1213f8fa7b1d2184be71e3212066644641a120f206b2b0b043adc2` through all eight snapshots: one floor, two rooms, six assignments (two monsters, one trap, three loot), no returned custody items, exact layouts/assignment identities/positions and structural investment of 105 mana. Confirmed Research/progression state digest remains `4171dc70fddcbf43725b3890571fe7cc05a91b386abce7d1a681c93eb905c58e`. Raw gameplay state is retained privately, not committed.

Initially settled `run-1` and `run-2` remain exact, as do every subsequently settled outcome across later snapshots. New foreground activity settles run-3 through run-7 once; no duplicate IDs or changed prior outcome hashes appear. Active party sequence 5 retains its StartIdentity across the first reopen and advances checkpoint ordinal 10→22 through new eligible foreground work. Next run sequence progresses 4→9. This preserves unfinished party continuity without claiming offline adventurer catch-up.

## Time, mana and focus

The unchanged configured peace rate is 180 mana/hour, offline efficiency 0.15 (27/hour), and ordinary 10-second ticks contribute 0.5 mana. The two cleared monsters restore to readiness health 6+7 at one mana per health. The observed wallet deltas reconcile with those values and new run completions; permanent construction investment is unchanged. Event-local recovery debit logging was not added, so the detailed ledger in the summary is explicitly an arithmetic reconciliation under the existing policy, not a new independent time authority or instrumented debit trace.

The first close→reopen interval adds 1.3775 mana: one 0.5 tick plus 0.8775 offline mana for 117 seconds at 27/hour. The boot log records one successful offline grant, not repeated grants for that interval. Existing outcomes remain unchanged.

A controlled minimize/restore targeted only the isolated UAT window. Two minimized snapshots 13.541457 seconds apart are byte-identical: A=C=8355403, wallet=126.26416666666653, unchanged party/checkpoint/history and accounted seconds. Restore records one offline grant of 0.105 mana, consistent with the 14 whole-second background interval; no new outcome or tick occurs in that comparison. New logical time after return is eligible foreground work. This is Windows focus/lifecycle evidence, not a player-controlled pause feature.

The owner session includes the existing large-delta warning for its approximately eleven-minute background interval. No save-authority refusal, recovery-required condition or application ERROR was found in either inspected session log.

## Applicable automated evidence

Recomputed input seals for the focused, Research, integration and PlayMode reports each match all 325 runtime/data/scene files of this unchanged implementation. Research-03 remains 98 passing invocations. Integration-04 remains 436 passes, including 96 passing passive-mana/clock/save-lifecycle invocations. Focused-10 remains 154 passes; Play-03 remains 129 passes. These existing selections overlap and were not rerun. Their XML hashes and exact source applicability are in the summary. The prior [qualification packet](h1-f2-time-provenance-qualification.md) remains historical evidence.

The remaining material qualification gap is the second successful process exit/reopen and repeated-grant check. The smallest required follow-up is to resolve/qualify this shutdown issue and complete that same-build, same-save reopen. No broad suite or new Research UI is justified by these results.

## Blocking shutdown observation and correction boundary

The second ordinary Alt+F4 reached GameRoot.OnApplicationQuit, published `Saved canonical complete save. Reason: AppQuit` at **2026-10-26 14:04:41 UTC**, and removed the window. Process **9616** did not exit within the initial 45-second observation. A later 30-second read-only observation also expired with the process still present at **14:07:44 UTC**. Its log ends after physics/input shutdown, a ComputeBuffer-disposal warning and `PlayerConnection::Cleanup`. Exit code remains unavailable.

The second-launch guard correctly refused with `Unexpected UAT process remains`. No concurrent player was launched; no process was force-killed; no save was repaired or reset. The durable save itself is well formed, stable and consistent, but process termination and the second reopen are unqualified. Root cause is not established. Native Development Player/graphics/PlayerConnection cleanup is a plausible investigation area; the final log line does not prove which thread is blocked or implicate H1 save logic.

Smallest correction plan: inspect the exact stalled process's cleanup state using read-only diagnostics, establish the responsible shutdown path, and propose a focused correction or build-environment remedy before changing code. Preserve the owner save and all evidence. After resolution, repeat normal shutdown and same-save reopen, verify the existing IDs/history/resource effects, then restore a usable Moonlight instance. Do not add recovery behavior, change gameplay, repair the save, or use another product namespace to conceal this failure.

## Evidence and preservation

Published sanitized evidence: [summary and semantic comparisons](evidence/pr256-owner-uat-closeout-20261026/summary.json), [allowlisted log events](evidence/pr256-owner-uat-closeout-20261026/sanitized-log-events.json), [owner dungeon before close](evidence/pr256-owner-uat-closeout-20261026/owner-dungeon-before-close.png), [first reopen](evidence/pr256-owner-uat-closeout-20261026/owner-dungeon-first-reopen.png), and [controlled focus return](evidence/pr256-owner-uat-closeout-20261026/controlled-focus-return.png). Commands and private-artifact seals are in the summary. Raw saves, full semantic snapshots, process command lines, machine paths and raw logs remain in the private local PR256 owner-UAT closeout directory.

All 477 pre-existing files in other save namespaces are hash-identical, with no additions. Of 304 earlier evidence/session files, 303 remain byte-identical; the one active owner-session log only appended authorized resume/shutdown messages, and its entire prior prefix hashes identically. The complete closed log is separately preserved. No frozen qualification report was modified. The owner's original planning-document modification remains unchanged. No original owner checkout was edited.

The interactive environment cannot currently be left usable: its second process has no window and remains in shutdown. No save/recovery contradiction or rollback is observed, but this unresolved lifecycle failure blocks final closeout.

**BLOCKED — CORRECTIONS REQUIRED**
