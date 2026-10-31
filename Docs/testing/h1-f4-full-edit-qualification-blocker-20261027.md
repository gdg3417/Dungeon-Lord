# H1-F4 full EditMode qualification blocker — 2026-10-27

Branch: `codex/h1-f4-recovery`. Original base:
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Current and tested HEAD:
`16955152fcdd5a5f8af006dc38887a75f1f83266`.

Recommendation remains GPT-6.1 Sol High, Complex implementation/investigation;
GPT-6 Sol High is the availability fallback. No new architecture decision is
requested. The approved recovery semantics remain unchanged.

## Recorded result and immediate cause

Campaign `61c2a2cd452e4a5c837496834f680e55`, preparation `fulledit01`, is permanently
retained as **INCOMPLETE / UNCERTAIN: Inputs no longer applicable**. Its reservation
remains assigned. Restoration and final preservation stages did not run.

The complete raw NUnit XML reports **2,772 cases: 1,966 passed, 805 failed,
1 skipped, 0 inconclusive**. The sole skip is
`DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`
with reason `gd66.test.windows_only_inverse`. All 102 H1F4RecoveryContract cases
passed in that raw report; this does not make the campaign pass. The prior focused
102-case passing campaign is `1d496e09c71944d6be592056b99c3d86`, source
`c74f8a989501a56ab73cb11d6348bcb32de21fb7`.

Unity CLI reported Editor exit code 2 for test failures. The Python runner then
raised at `Tools/Presentation/qualify_h1_f4.py:226`, before writing `cli-exit.json`:
the approved font restoration branch requires a zero child exit. The only changed
qualified Unity input is the known TMP fallback-font rewrite, with the already
measured `f535...` hash below. Its bytes were preserved in place rather than
restored. The supervisor recorded host exit 0 and runner exit 1, then detected
the input mismatch and retained INCOMPLETE/UNCERTAIN. No verdict is overridden.

Failure messages include 487 `gd66.write.first_write_validation_failed`,
25 `time.recovery.invalid_state`, 25 `time.recovery.context_mismatch`, and older
schema/limit assertions. For example, AdmissionStartReconstruction fails in the
shared Fixture.Accept at DetachedCanonicalWriteAuthorityTests.cs:1558, called
from PhaseSixA4Tests.AddContent:427. Strict successor context binding is enforced
at DetachedCanonicalWriteAuthority.cs:80–84 and :410. These are investigation
leads, not proof that all failures are obsolete tests. Each group requires a
source-backed correction and qualification; validation must not be weakened.

This was an Editor test attempt. No Windows Development Player ran, and no native
Windows shutdown success or cause is inferred. The known intermittent shutdown
problem remains separate and unresolved.

## Preserved process, source and input evidence

The [sanitized diagnostic](h1-f4-full-edit-qualification-blocker-20261027.json)
contains the exact 19-file campaign manifest, nine pinned tool checks, input maps,
archive checks, process identities, reservation identity and protected-evidence
checks. Raw Unity logs and detailed failure stacks stay in the owned TestResults
directory because raw logs may contain authentication arguments.

At `2026-10-27T18:49:50.556339+00:00`, read-only handle queries found no live
matching process for:

| Role | PID | Creation FILETIME |
| --- | --- | --- |
| Supervisor | 46620 | 134375993693524398 |
| Host | 44588 | 134375993822635613 |
| Runner | 25920 | 134375993823730434 |

All recorded executable identities are the pinned bundled Python executable.
The approved no-Unity guard passed. Query-only opening of
`Local\DLQ-5efc1e89f6274ccbb3021ef461147897` returned Windows error 2. These later
absence checks do not independently prove descendant completion and do not
repair missing evidence or change the recorded verdict. Existing host/runner
exit records are retained exactly as written.

The reservation file is
`TestResults/qualification-supervisor-reservation/active.json`, SHA-256
`d4b76086a03df9339aa4741f67faf5304ed48870bcec48321c3389c9a2dad962`.
Its exact owner is this campaign's absolute managed-worktree path.

Product: `Dungeon Lord B3B2 Smoke H1F4 20261027 fulledit01`.
Save namespace:
`C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F4 20261027 fulledit01`.
Unity is pinned to 6000.3.2f1 and the CLI to beta.14 through the retained plan/tool
hashes. All nine expected tool hashes matched during diagnosis.

Preparation: `TestResults/h1-f4-20261027-fulledit01`.

| Identity | SHA-256 |
| --- | --- |
| original-inputs.zip | `a7472a408ae05497d37de8983dee0f2b09b15cce8f1a6e30f3d95c71f247a070` |
| original-inputs.json file | `0baeae84928ebda7eea2ac8fb0588155c96de9db3e7d07f9bf91c9c13fd3f899` |
| qualified-inputs.json file | `1aa136372a047e1c7b68796409d1c84e227aebabee5f850a1710c88e85268b4b` |
| Original map semantic signature | `6d44c97c7a1481866746adeff148b67c90ffa334092154702b3e7c53a262de6c` |
| Qualified map semantic signature | `854afc8c8c8743678a54563fe38de67011c9b3c598ec9d8765bebf0ff031b011` |
| Current map semantic signature | `afbbd9bbc1f67c6156a2b40b1378be134ac3fb706408bfa88950fb7601790185` |
| protected.json file | `153198c74d40d2676ec2dff34f605c14fcb4370146e8f597d23ef288fd9cd747` |

Every archive member and its exact file set matched the original input map.
The protected owner checkout, planning changes, other saves, historical fixtures,
qualified executable and retained review evidence matched. All 19 files of the
earlier `9ed872130a134ce7a5627f6c001b8e2f` INCOMPLETE campaign also matched its
original investigation manifest. Its previous exceptional recovery is not reused
as authorization for this different campaign.

## Smallest proposed recovery — NOT EXECUTED

After separate approval, preserve the four current files below under a new owned
receipt directory, then restore only these four archive members. This adds the
font to the three settings because the font is now the sole qualified-input
mismatch. No process termination, force unlock, deletion, supervisor-rule change,
test filtering, verdict change or campaign reuse is proposed.

| File | Original/archive SHA-256 | Current SHA-256 |
| --- | --- | --- |
| ProjectSettings/ProjectSettings.asset | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` | `0f13de06da5c719873aa0ca6b0801f56402538f81ad07b33ebe35d5aa1a1d9ae` |
| ProjectSettings/EditorBuildSettings.asset | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` | `f72e8fd550133184822260c85b62c371189f88e1d390fb95839292e9247ee7cf` |
| ProjectSettings/ShaderGraphSettings.asset | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` | `5c432c9c73fdf73fee91c8823ea02ab98e3b7a8edf341b5ae6901d699cce52a7` |
| Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset | `6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d` | `f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52` |

Reviewed proposal script:
`TestResults/h1-f4-20261027-fulledit01/incomplete-investigation/proposed_recovery.py`,
SHA-256 `52d72bc462b4f2b6ae7c47200e074471416e6e645c04fd4d512f94e513708dd6`.
It checks source/branch, all campaign evidence, exact reservation bytes/owner,
absence of a conflicting legacy reservation, complete current inputs, pinned
tools, process identities, no Unity, absent named Job Object, archive/map hashes,
every archive member, protected evidence, the old campaign and target hashes.
Unexpected/reused PIDs, errors or changed identities stop recovery. It repeats
checks under the existing exclusive Lease before modification. It preserves and
hash-verifies current file copies before replacing them, verifies the complete
restored input map and protected evidence, and only then uses `Lease.release`
for this campaign. The receipt reports attempted/confirmed modifications, actual
final hashes and reservation ownership even if an exception interrupts recovery.

The check-only invocation passed at `2026-10-27T18:52:26.286706+00:00`, with
`execute_requested=false`, zero modification attempts and `released=false`.
Receipt: `TestResults/h1-f4-20261027-fulledit01/review-check01/receipt.json`.
This is a diagnostic check, not execution authorization or a recovery success.

Proposed command, **only after approval** (run by Codex, not delegated to owner):

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B TestResults/h1-f4-20261027-fulledit01/incomplete-investigation/proposed_recovery.py --execute-approved-recovery --receipt owner-approved-recovery01
```

After any approved execution, independently re-read the four hashes, complete
original input map, campaign manifest, protected evidence and released reservation
(`campaign=null`, `previous=<this exact campaign>`). Preserve any partial failure;
do not claim success from script exit alone. Do not reuse this INCOMPLETE campaign.

The [qualification supervisor runbook](qualification-supervisor.md) explicitly says:
“Codex must investigate and obtain owner approval for any recovery that would
release an uncertain campaign or terminate an orphan.” It also requires preserving
source/input mismatches as INCOMPLETE. This new reservation recovery therefore
requires separate approval; the earlier campaign-specific approval does not apply.

After recovery, investigate the 805 failures, correct fixture/runtime defects
without relaxing frozen history or context validation, then run a fresh reviewed
campaign. No further Unity launch, restoration, reservation release, PR, merge or
owner UAT has occurred. PlayMode, Windows and the three-way performance comparison
remain pending. All owned diagnostic artifacts and incomplete states are retained;
no disposable artifact cleanup is authorized at this blocker.

BLOCKED — SPECIFIC CORRECTION REQUIRED
