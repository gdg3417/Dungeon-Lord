# H1-F3 implementation and blocked qualification

2026-10-26. **BLOCKED: qualification is incomplete; no PR created.**

Base and freshly reverified main: `443319983a746b4a1670bc8a87d7434a14f7a492` (#257). Branch: `codex/h1-f3-stable-recovery-precision`. Runtime/test implementation: commit `17924ab2`; exact attempted qualification source including campaign preparation tooling: `a68451bcebaab04a25dafeac223a075e4c190081`. Subsequent evidence-only commits are not new Unity qualification. Actual model: GPT-6.1 Sol High, explicitly confirmed by the owner; Complex, with GPT-6 Sol High as availability fallback. No substitution/escalation.

## Implementation and dependency decision

The [implementation contract](../planning/h1-f3-stable-recovery-precision.md) distinguishes approved D1–D6/H1-C01-A/H1-C02-A from actual behavior. Inspection found no unresolved save-authority contradiction for this inactive slice. Connecting the new endpoint to live grants would require the separately deferred full recovery sequence and durable episode identity/closure, so production integration remains inactive.

Changes are confined to the existing offline calculation service, a narrow recovery-observation value/factory, deterministic tests and their established Editor discovery bridge, campaign-specific preservation preparation, and documentation/evidence. No GameRoot/TimeService/SaveService/writer/session transaction signatures change.

R captures one complete injected precise sample after validated provenance is supplied, refuses invalid/backward observations and known foreground/stall classifications, and remains immutable during processing. The future coordinator's supported boundary is after unique canonical recovery and before accepted-work draining. **No production caller captures R in this PR.** It is process-local and does not survive another crash; once-per-durable-episode capture is not implemented.

Precise arithmetic uses checked integer 100 ns subtraction, then quotient/remainder conversion into the existing formula's numeric domain. It shares the canonical online rate, configured efficiency and capacity/rounding behavior. A distinct immutable calculation result cannot enter the existing grant publisher. Legacy calls preserve operation order and whole-second results, including long intervals beyond exact double integer tick representation. Calculations do not mutate or publish wallet/time/session state.

Live gameplay, boot grant timing/order, focus/resume coordination, canonical save semantics, monster recovery and Research durability remain unchanged. Schema 16 remains `legacyWholeSeconds` with exactly zero persisted fraction. Frozen schemas 7–15, exact 15→16 migration and supported v1/v2 A>C obligations are unchanged. No schema/migration/configuration/tuning/localization/feature/version-gate, content, renderer or graphics change is included in committed implementation. Temporary qualification settings are separately inventoried below.

## Actual qualification

Unity 6000.3.2f1; CLI 1.0.0-beta.14, confirmed current through its read-only official update check; Python 3.12.14. The initial isolated import compiled and its command exited zero. It was preparation, not a test gate or standalone qualification.

The existing supervisor owns campaign:

`C:/Users/gdg34/.codex/worktrees/h1-f3-stable-recovery/Dungeon-Lord/TestResults/qualification-supervisor/b343351f516b4a19b8992ad00910ae40`

The reviewed plan pins tool versions/hashes, exact source, all **1,462** Unity inputs, fresh product/save identity, explicit stage selection, exact expected test counts, the named platform skip and preservation dependencies. Input identity: `bec21d0dfa7f58555fe284aa3b377e8ac7f25a9d98782a3f70b1f7bb51e81964`. Selected stages were focused, integration and migration, each with a direct preflight, followed by final preservation. No sustained profile or full unfiltered suite was selected.

| Stage | Actual result | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| Focused preservation preflight | PASS | N/A | N/A | N/A |
| Focused Unit/SIT XML | Complete XML; **supervisor gate INCOMPLETE** | 188 | 0 | 0 |
| New H1-F3 cases within focused XML | Included above, not additional invocations | 34 | 0 | 0 |
| Integration / Research | NOT RUN; planned 436 cases | — | — | — |
| Migration / Windows EditMode filesystem | NOT RUN; planned 224 cases | — | — | — |
| Final campaign preservation | NOT RUN | — | — | — |

Focused includes F2 paired cuts/provenance, frozen-15 source cases and the new precise interval/R/publication-fence cases. The new cases cover same-second and single-100-ns intervals, carry/borrow, zero/backward/unsupported endpoints, constructor range/overflow rejection, maximal positive interval, uncapped long durations, exact legacy parity, fractional wallets/full/clamped capacity, canonical rate/efficiency, nonmutation and stable complete capture/classification refusal. All validated positive endpoints fit signed 64-bit interval subtraction; no artificial overflow-producing endpoint is accepted.

The unexecuted integration selection includes actual canonical boot/passive publication/reopen, lifecycle boundaries, foreground/A>C, actions, stale predecessors, failed publication/retry and Research Start/Claim. The unexecuted migration selection includes established failure/readback/uncertainty and historical compatibility paths. **Those required gates remain missing.** Its sole planned skip was `DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`; it was not executed or counted as skipped.

PlayMode: **NOT RUN; N/A for this inactive slice**, because lifecycle coordination is unchanged. Windows Development standalone: **NOT RUN; N/A**, because boot, grant publication, save lifecycle, writable bytes and filesystem primitives are unchanged. This is distinct from unexecuted Windows filesystem EditMode coverage, which remains required. Owner UAT: **N/A**, because no player-facing flow or playable behavior is activated; no UAT build/scenarios/session were prepared or launched. Applicability decisions remain subject to independent review and do not substitute for missing Unit/SIT gates.

## Specific completion blocker

The focused XML completed 18:27:18–18:27:30 UTC. Its owned CLI child PID 45544 exited zero at 18:27:34.783646 UTC; the owned host also exited zero. The supervisor's bounded completion drain nevertheless recorded at 18:27:39.794464 UTC:

`INCOMPLETE / UNCERTAIN: Owned descendants still active; completion uncertain`

A subsequent read-only process inventory observed CLI executable PID **3568**, parent **45544**, created 18:27:05.261223 UTC, and console host PID **44988**, parent **3568**. They were absent at the next inspection. No process was terminated. The exact descendant command line was not established before it disappeared; it is not conclusively labeled an authentication broker or a game shutdown defect.

The campaign remains recorded INCOMPLETE. No automatic resume, new campaign, gate relabeling, reservation release or force-unlock was attempted. After execution, `Temp/qualification-supervisor` retained `execution.lock` but `active.json` was absent; its removal cause was not established. This observation is retained for targeted orchestration review, not treated as permission to reuse the project. The historical intermittent Windows player shutdown issue remains separate; no native forensic campaign was started.

Continuation requires a reviewed remedy for Unity CLI descendant completion and the incomplete campaign's safe disposition under the supervisor rules. Do not broaden H1-F3 into a process/concurrency framework or alter frozen evidence. Required integration/migration qualification must then complete before draft PR creation. No product defect or save-authority contradiction has been established by this orchestration failure.

## Evidence, inventory and preservation

The [sanitized summary and artifact inventory](evidence/h1-f3/summary.json) and [focused XML/plan/identity records](evidence/h1-f3/focused-reports.zip) retain the actual source, results and blocker. Archive SHA-256: `ec5f8f1750a6499f9d7420495318ee1bdcea1496a2b615490ff509e6c89e4218`. Raw Unity logs and private preservation hashes remain local; no raw logs or owner saves are published.

| Exact identity | Owner / purpose / source | Retention |
| --- | --- | --- |
| `C:/Users/gdg34/.codex/worktrees/h1-f3-stable-recovery/Dungeon-Lord` | Codex; managed implementation project; attempted source a68451bcebaab04a25dafeac223a075e4c190081 | Preserve for blocked review |
| Campaign directory above | Supervisor; exact process/plan/XML/raw evidence | INCOMPLETE; preserve unchanged |
| Worktree `TestResults/h1-f3-preparation` | Codex; original settings, import log, full qualified-input ZIP, private preservation inventory | Preserve |
| `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F3 20261026` | Codex qualification; fresh synthetic test namespace | Preserve; no cleanup after incomplete qualification |
| PIDs 7992, 45544, 3568, 44988 | Owned host/CLI and observed descendants; identities in summary | Exited/absent; no termination |
| Existing Unity Hub/broker processes, including PID 26016 at preflight | Owner environment; unrelated | Untouched |
| `C:/Dev/Dungeon-Lord` | Owner checkout; pre-existing planning modification | Untouched; proposal hash unchanged |
| Existing owner/prior save namespaces under `C:/Users/gdg34/AppData/LocalLow/gdg3417` | Owner/prior qualification; private exact manifest | All 503 files across 51 other namespaces hash-identical |
| Existing builds, old worktrees and retained evidence | Owner/prior campaigns; never used as output targets | Untouched; no cleanup |
| `Docs/testing/evidence/h1-f3` in this worktree | Codex; sanitized evidence from attempted source | Retained |

The owner's proposal SHA-256 remains `b04136e1b8375bef6dc2105bacd3e19062b4365be10e043b91c2d87670bd267d`. A separate read-only post-stop comparison confirmed all 1,462 inputs still match the qualification seal and protected owner/save hashes still match preparation. It is **not** the unexecuted supervisor final preservation stage.

Temporary productName substitution and two line-ending-only Unity settings rewrites remain uncommitted in this isolated worktree. They are preserved with exact original backup/input archive; restoration was not performed after the supervisor STOP. They must not enter any implementation PR. No builds or owner-UAT scenarios were created. No artifact cleanup, source restoration, save modification, process termination or campaign duplication occurred.

## Handoff

No PR number/URL exists: build-promotion policy forbids creating it before required Unit/SIT gates pass. The implementation is available locally for diagnosis/review, but **is not ready for independent PR review**. Remaining H1 work is full recovery ordering, durable identity/closure, fractional publication, interrupted-write qualification and measured overload policy; H2 and O1 remain later dependencies. No next PR is started.

**BLOCKED — SPECIFIC DEPENDENCY OR CORRECTION REQUIRED**
