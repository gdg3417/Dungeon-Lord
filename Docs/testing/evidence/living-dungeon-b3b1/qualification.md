# Living Dungeon B3B1 qualification

Status: **ready for independent PR review; unmerged, production inactive**.

Baseline: local/remote main `ce39ca44759f1f7d2338d9ec73aab9ea95b95534`, merged #242/#243, clean tree and no open PRs. Owner explicitly authorized implementation, validation and an unmerged PR. Branch `codex/living-dungeon-b3b1-durable-combat`. Recommended configuration GPT-6.1 Sol High, Complex; fallback GPT-6 Sol High then GPT-5.6 Sol High; no delegation or requested model switch. Runtime model metadata is not independently exposed here.

## Scope and compatibility

See the [implementation contract](../../../planning/living-dungeon-phase-b3b1-implementation-contract.md). The existing simulation executes injected timed C0/C2 snapshots and reconstructs a committed prefix under actual C0 versus C2 semantics. Format 2 and `run.combat.prefix.v1` are separate from frozen B1 format 1. Existing manual entry points stay untimed; Profile A stays B1-only. No schema/migration, production admission, settlement, mana/Heat tuning, content, player text or localization changes.

| Entry/executable | Checkpoint | Restoration/owner support | Settlement |
| --- | --- | --- | --- |
| Historical B1 / `run.executable.living-dungeon-b1.v1` | Frozen format 1 | Existing profiles and actual historical reader; golden bytes preserved | Existing B3A Editor-only qualified scope |
| Existing manual C0 / `run.executable.combat-c0.v1` | Export remains refused | Existing untimed manual behavior | Existing completed manual transaction |
| Existing manual C2 / `run.executable.combat-c2.v1` | Export remains refused | Existing untimed manual behavior | Existing completed manual transaction/recovery |
| Explicit timed C0 / `run.executable.combat-c0.v1` | Format 2, combat prefix v1 | Injected combat envelope; mixed records only under explicit compatible profile | B3A rejects; no production settlement |
| Explicit timed C2 / `run.executable.combat-c2.v1` | Format 2, combat prefix v1 | Same, with immutable initial continuity and option validation | B3A rejects; no production settlement |
| Unsupported format, executable or evidence version | Rejected | No usable execution or publication | None |

The C0/C2 kernel identities are actual retained implementations, not aliases for B1. Format 2 adds timing/durability around the same qualified atomic encounter. It selects no per-round or defender-motion duration: the existing engagement distance and injected `MonsterInteractionUnits` govern the boundary, while party movement uses existing tile-edge timing. Schema **15**, owner order, RunId allocator and frozen readers/migrations remain unchanged. `RunCheckpoint.cs` changes only the injected limits type; its historical codec body is untouched.

Geometry preparation may charge bounded computational work before an announcement. Only committed work enters evidence. Export never prepares; replay regenerates pending lookahead once. Tests compare full action receipts, monster/member condition, future steps, final structured outcomes and work, including repeated probing, insufficient deadlines, reopen and deterministic workload failure.

## Retained attempts

All campaign outputs are under `TestResults/living-dungeon-b3b1-20261018/`. Exact successful/failed Unity invocations, source commits, times and exits are in `*-command.json`; reports and logs are retained, including provisional failures.

| Attempt | Result |
| --- | --- |
| 01 | Preservation gate rejected the Unity CLI server process before Unity launched. The exact server command was identified and excluded from gameplay-process detection. |
| 02 focused | 21 passed / 4 failed. New decoder omitted Mono `InvalidDataException` from its fail-closed catch; low-work validation compared an unused tile ceiling; mixed-save fixture used its smaller default parser envelope. Corrected without changing B1 reader/kernel or production budgets. |
| 03 focused | 25 passed / 0 failed at `93b7738ec514e39d2ac66d35491c2ff18ad4c3b7`. |
| 04 focused | 31 passed / 1 failed: new standalone fixture generator requested a batch larger than its run ceiling. Corrected to bounded ordinary advancement. |
| 05 | Preservation gate identified a second CLI authentication broker before Unity launched; its exact command was independently verified and excluded. |
| 06 focused | 33 passed / 0 failed at `82f6cff49467eb15369f9ba30cc6068747a26f31`. |
| 07 full EditMode | 2,213 passed / 0 failed / 1 existing skip at `277388d13d8a565f095701ddb5b08b9c829bd784`. |
| 08 focused, final capture | 40 passed / 0 failed at `52152590c454bc822d3db775ce94cfdcf646cab0`. Explicit effective event ceiling, canonical deadline guard, exact C2 option/condition validation, committed post-start edits and exact 65,536-byte limit included. |
| 09 full EditMode, final source | 2,217 passed / 1 failed / 1 existing skip at `52152590`. Sole failure: unchanged PR #242 graphical test attempted a non-overwriting copy to `owner-uat-starter.json`, created by successful run 07. No file was overwritten. |
| 10 baseline reproduction | Verified `main` source `ce39ca44`, added feature scripts excluded and original namespace isolation retained: same test fails with the identical fixed-filename exception. Exact source proof and unchanged starter hash retained; feature sources restored exactly. |
| 11 affected graphical case | 1 passed / 0 failed at `0b262432`, whose Unity input tree matches `52152590`. Qualification-only substitution gives every starter copy a unique GUID filename, retaining `File.Copy(..., false)` and every gameplay/layout assertion. |
| 12 relevant PlayMode | 8 passed / 0 failed / 0 skipped at `0b262432` (same Unity input tree as `52152590`, with documented qualification substitutions). Covers new C2 reconstruction and existing A/B1/B2/B3A/C2 frame/save integration. |
| 13 Windows Development build | Succeeded at `0b262432`: 0 errors, 1 existing symbols-upload warning; original output retained. |
| 14 provisional standalone | All 4 detached comparisons passed, but Mono `Environment.Exit` stalled. After verifying the exact read-only process and flushed result, it was stopped before Bootstrap/save initialization. This attempt is provisional, not a clean smoke exit. |
| 15 launch gate | Rejected the missing original Unity shader-analytics cache before Unity launched. Owner subsequently accepted that single auxiliary-cache loss. |
| 16 isolated Windows Development build | Succeeded at `473e971ec31d9b0d56f5e61f8993e3a2c935bd3a`: 0 errors, 1 existing warning (symbols upload URI empty), report size 181,756,365 bytes. New unused `...20261018-smoke-fix/` output; native product `Dungeon Lord B3B1 Smoke 20261018`. |
| 17/18 smoke launch gates | Rejected reused log/archive names before a player launched; existing evidence retained and fresh names selected. |
| Final standalone | Exit **0**, **4/4** C0/C2 initial/partial fixture reconstructions match final Editor bytes, repeated deadlines/export/reopen included. Final command/result and original provisional result retained. Development-only native shutdown closes the report then exits before Bootstrap. |

Existing strict qualification-input tests: **12/12 passed**. Existing B3A preservation-gate regression tests: **24/24 passed**. These are tooling checks, not combat qualification. Code whitespace checks passed. A redundant sandbox rerun of the Python checks could not create/clean temporary Git repositories; rerunning outside the sandbox passed all 36 again. Full PlayMode, other operating systems, and new device-performance qualification were not run; the relevant existing PlayMode and Windows standalone boundaries were exercised. The B1 golden archive remains SHA-256 `197793b3b6f4dd127b0e96ce3003f3bee58138e58a81ee1d7dec13b2374dc8fb`; its fixtures were not regenerated.

## Observed workloads

Synthetic injected timing only, Windows Editor, Unity 6000.3.2f1. These representative canonical snapshots do not establish worst-device or maximum-content performance.

| Variant | Floors | Terminal checkpoint bytes | Committed events | Charged combat work |
| --- | ---: | ---: | ---: | ---: |
| C0 | 1 | 23,510 | 13 | 2,049 |
| C2 | 1 | 23,625 | 13 | 2,051 |
| C0 | 2 | 27,805 | 25 | 3,750 |
| C2 | 2 | 28,026 | 25 | 3,754 |

Mixed B1/C0/C2 complete save: **124,029 bytes**, **39 aggregate replay events**. Final focused sample C2 restore plus byte/inspection verification: **13.2838 ms**. No budget is increased: 65,536 checkpoint bytes, 2,048 replay events/party, 8,192 aggregate replay events, and existing parser/owner envelopes. The explicitly compatible test profile additionally injects 1,000,000 combat work/run; the frozen Profile A remains zero/unsupported. An exact 65,536-byte synthetic start restores, and one additional byte rejects. This independently qualifies the byte ceiling, not production content density. Existing B1 maximum-prefix/aggregate/parser tests remain regression authority; these small canonical combat snapshots do not claim simultaneous attainment of all maxima. Pending lookahead can be recomputed per restore; these are bounded call costs, not catch-up throughput claims. Parsing, hashing and validation CPU are additionally bounded by byte/replay/record limits and are not counted as kernel geometry work.

## Preservation and environment

The existing root project, Editor installation and Library are reused. Campaign preparation backs up **1,350** inputs, hashes **8,051** protected save/build files and **5,349** historical evidence files, and archives current historical evidence before launching Unity. New source files raise later applicability counts. Build config and SaveService fallback use `living-dungeon-b3b1-qualification-20261018.json`; static disposable fixture names and all shared screenshot helpers are redirected to fresh campaign paths. Every launch checks committed-source applicability, actual substitutions, protected hashes, gameplay processes and unused outputs. The frozen closed B3A campaign is never reopened.

The PR #242 starter-artifact path was an additional output owner absent from the older B3A helper inventory. First full run 07 created it; run 09 correctly refused to overwrite it. After actual main-source reproduction, the campaign adapter adds a verified, temporary GUID filename substitution for that one test. It is restored with the other test-source substitutions at closeout. The original artifact is retained and hash-checked. This is an output-isolation correction, not a weakened assertion or a waived gameplay failure.

No owner save or prior player is used as a fixture. Final restoration verified the original source/settings and starter artifact. **8,050/8,051** protected save/build files match; **5,349/5,349** historical evidence files match. One original auxiliary cache was consumed by native Unity startup before Bootstrap: `C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord\Unity\ShaderVariantAnalytics\ShaderRuntimeInfoEvent.json`, original SHA-256 `9cfa67716db34eba24c61af645d4d144a1f1daec6bb7108168d52300b837710a`. No exact backup was found. The owner explicitly answered **“Accept this cache exception; continue with isolated product”**. This loss remains a false hash row, with a separate approval record; it is not counted as preservation. Unique save filenames alone did not isolate native Unity startup. Remaining native qualification used a fresh product identity and restored both product settings and the strict build-identity constant afterward. No other recorded owner file changed. The audit covers the recorded save/build files and repository historical evidence. Transient Unity logs and the default LocalLow test-runner `TestResults.xml` are excluded; campaign XML/log outputs are retained separately. No preservation claim is made outside those inventories. The five accepted missing screenshot originals from PR #242 remain an existing historical exception; preserving the current baseline does not claim to recover them.

## Remaining activation and independent review

One automatic foreground C2 run still requires trusted foreground provenance/restart and configured time mapping; demand/admission and event/edit ordering; one-party-per-floor occupancy and durable descent waiting; qualified atomic C2 injury/economy/history/knowledge settlement and retirement; and end-to-end save/reopen/failure qualification. Later monster edits require an explicit identity/condition reconciliation contract. Mixed/offline periods require their causal attribution/loss-protection work before enabling them.

Independent review should inspect capture bounds, combat causality evidence, committed versus pending work, profile admission isolation, B3A rejection and unchanged historical executable behavior. Owner gameplay/UAT acceptance and merge are not claimed.

## Source applicability and artifacts

The final combat/domain source is exactly `52152590c454bc822d3db775ce94cfdcf646cab0`. Later commits change qualification tooling and two technical harness files: the Development-only early-exit smoke and the Editor's unused second output method. Build and standalone smoke qualify those at `473e971e`; no gameplay/test inputs changed after the full/focused suites. Product/save/screenshot/fixture substitutions are documented and restored, not committed. Original application product and save filename remain unchanged in the PR.

`qualification-reports.zip` retains all test XML, command/source-applicability records, baseline reproduction proof, audit rows and approvals. `report-summary.json`, `commands.json`, `source-applicability.json`, `preservation-summary.json`, `windows-package.json`, `build-reports.zip`, `log-index.json` and `changed-files.md` provide review indexes. Raw logs/builds, initial-input backup and the 3.67 GB historical-evidence backup remain under the ignored campaign path. No owner game-save bytes are added as new fixtures.
