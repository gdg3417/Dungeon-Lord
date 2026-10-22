# Supervisor tooling verification — 2026-10-22

Branch: `tooling/qualification-supervisor`  
Required and fetched `origin/main` baseline: `575d553b800e04600b2853899891cd55767cb8cd` (#247).  
Starting working tree: clean. Latest main was fetched again before publication and
still matched. The PR head contains the six paths listed below; its final SHA is
reported in the PR/conversation rather than embedded self-referentially here.

Recommended model: GPT-6.1 Sol, High. Classification: Complex. Reason: detached
process ownership, safe resumption and evidence integrity. Fallback: GPT-6 Sol,
High only with explicit owner approval; no fallback or reasoning increase was
requested. AGENTS.md and the active canonical policy were read before changes.

## Changed files and architecture

- `Tools/Presentation/qualification_supervisor.py`: explicit JSON plans, exclusive
  project lease, sequential stages, fingerprints, atomic records, read-only status,
  deliberate resume and cancellation.
- `Tools/Presentation/qualification_windows.py`: standard-library ctypes wrappers
  for process FILETIME/image identity, owned jobs, completion ports and events;
  the existing Unity broker exclusions are retained in the process conflict gate.
- `Tools/Presentation/test_qualification_supervisor.py`: disposable repositories,
  real fake-child processes and a fast demo fixture preparer.
- `Docs/testing/qualification-supervisor.md`: Codex-operated workflow and limits.
- `Docs/testing/evidence/qualification-supervisor/independent-completion.json`:
  retained final-source proof, terminal identities, report/log hashes and
  preservation comparison.
- This verification record.

Dependencies: Windows, Python 3.11+ standard library, Git and existing installed
qualification tools. No gameplay/config/localization/Unity test sources, adapter
chain, historical evidence, queue configuration or external service was changed.

Tested code SHA256 values:

| File | SHA256 |
| --- | --- |
| qualification_supervisor.py | `c4d83756ea1d381e2d6f493a6cd58127242468a9244b8e5410863c73e2ff234b` |
| qualification_windows.py | `862f888b074f7a01e3e3dcef80a1cbdf9ad0b6dd40ee7b1ad79b3757a0318dba` |
| unchanged qualification_inputs.py | `d684f97a4f2e6287f36620c308262fd1fcee39e23fa58295493bc52b803e167c` |

## Commands and exact results

Host: Windows 11 `10.0.26300`, Python `3.12.14`, AMD64. Commands used the installed
bundled Python at
`C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.
The sandbox's redirected temporary directory prevented Git fixture initialization;
the disposable Windows suites were therefore run outside that sandbox with
approval. No dependency installation was necessary.

| Command (after Python executable) | Final result |
| --- | --- |
| `-m unittest discover -s Tools/Presentation -p test_qualification_supervisor.py -v` | **28 passed**, 27.043 s, exit 0 |
| `-m unittest discover -s Tools/Presentation -p test_qualification_inputs.py -v` | **12 passed**, 5.022 s, exit 0 |
| `-m unittest discover -s Tools/Presentation -p test_b3a_preservation_preflight.py -v` | **24 passed**, 2.933 s, exit 0 |
| `git diff --check` | exit 0 |
| Real `qualification_windows.assert_no_unity()` | clear, exit 0; no Unity/player launched |

The 28 supervisor tests exercise success, multistage dependencies, nonzero/abrupt
child exit, timeout, malformed/missing/incomplete XML, wrong counts, exact expected
and unexpected skips, duplicate display names with distinct NUnit IDs, changed
source/inputs, declared input transitions, atomic replace interruption, completed
stage reuse, recovery between stage-result/campaign publication, duplicate dispatch,
concurrent reservation rejection, live-child supervisor crash, stale/reused PIDs,
unowned cancellation rejection, actual owned cancellation with an unrelated live
process preserved, detached resume, read-only repeated status, stage-local output
paths and a vanished dispatcher before RUNNING publication.

Read-only parser compatibility was also checked against the **existing** PR #247
reports, with their actual named skip lists: EditMode 2,332 discovered / 2,331
passed / 0 failed / 1 skipped; PlayMode 3,738 / 3,728 / 0 / 10. This parsed old XML;
it did not rerun Unity, establish new source applicability or replace either
historical qualification. Their `Skipped:Ignored` roots and duplicate parameter
display names are preserved without weakening leaf counts or expected skips.

## Independent completion

Final proof driver retained locally at
`TestResults/supervisor-tooling-20261022-reviewed/demonstrate.py`; it created two
disposable fake-input Git projects and dispatched the supervisors, then exited.
The launcher returned at **17:50:45 UTC**. No model completion polling occurred.
Independent work and documentation continued; terminal status was first inspected
at **17:51:27 UTC**.

| Fixture | Campaign | Host PID / actual child PID | Terminal UTC | Verdict |
| --- | --- | --- | --- | --- |
| Success | `3f8b22c78d884c5ca2feee380003e2a5` | 44316 / 13660 | 17:50:55.760704 | PASSED, exit 0, discovered/passed 1, failures/skips 0 |
| Partial XML | `2c80e680a9b84bceb6f381f82c838183` | 16104 / 13692 | 17:50:56.068120 | FAILED / INVALID_EVIDENCE despite child exit 0; counts unavailable |

The committed JSON records exact commands, source SHA of each **fixture repository**,
creation FILETIME/image identities, launcher identity, timestamps and hashes. Each
child ran **once**. A second read-only inspection returned the same result and left
invocation counts at one. No complete Unity suite, sustained profile, Development
build, or actual gameplay qualification was run. Earlier development demonstrations
remain local and unchanged; this record identifies the final tested tool hashes.

Crash/resume evidence is supplied by the real-process regressions: an interrupted
supervisor with a genuinely live child reports INCOMPLETE, holds the reservation
and refuses replacement; a verified completed stage is reused exactly once after
restart, including the atomic-publication gap. Cancellation terminates the owned
job while an unrelated disposable Python process stays alive.

## Preservation and limits

Before launch and after completion, exact file sets and SHA256 maps matched for
1,410 Unity input files (Assets 1,362; Packages 2; ProjectSettings 27;
ContentAuthoring 19), **1,015 pre-existing historical evidence files**, and **909
files in three pre-existing qualified builds** (B3B2 20261018, two-floor final
20261021, and two-floor performance 20261021; 303 files each). The aggregate hashes
are in the committed JSON; the full before map is retained locally in
`TestResults/supervisor-tooling-20261022-reviewed/preservation-before.json`.
The new evidence directory was published only after that comparison. No owner
save directory was opened. All four Unity input roots also have zero Git diff
against the required baseline.

Measured orchestration facts: zero model completion polls, one successful fake
child invocation, one intentionally partial fake invocation. Token savings were
**not measured**; there is no claim of faster Unity tests. Detached execution was
demonstrated on this Windows host, not on other hosts or under OS shutdown.

The existing adapters' fixed campaign names and source assertions remain frozen.
Fresh live Unity preparation/commands must still be reviewed; automatic conversion
of those adapters is outside this PR. Extracting sustained workloads from current
Unity fixtures, parameterizing historical adapters, orphan-recovery automation or
an optional notification callback require separate work. The retained duration
diagnosis records Windows Codex Queue socket error **10050** and no successful
exact-Desktop-thread delivery test. No queue retry/install/configuration change
was made. The owner still returns to the conversation for Codex to inspect status.
Independent review and all existing merge gates remain required; do not merge
this PR as part of this task.
