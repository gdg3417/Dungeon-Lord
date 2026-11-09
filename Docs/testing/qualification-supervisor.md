# Completion-driven qualification supervisor

Codex runs these commands and manages the plan. The owner only needs to return to
the conversation when convenient. No PowerShell operation or process supervision
is delegated to the owner. Requires Windows and Python 3.11+; Git and existing
qualification tools must already be installed. No packages, service, queue, or
Codex configuration changes are required.

## Launch and return later

1. Codex reviews a campaign-specific explicit JSON plan and its preservation
   preparation. Record the full source SHA, tool hashes/versions, approved
   substitutions, isolated product/save identity, unused output identities,
   exact discovery counts and named allowed skips. Plans are trusted executable
   instructions, not a sandbox. Every script/helper they invoke must be pinned
   in `tools`, including the entire existing adapter chain when used.
2. Create the campaign with an explicit selection:

   ```text
   python Tools/Presentation/qualification_supervisor.py create C:/Dev/Dungeon-Lord <plan.json> --selection focused
   ```

   Save the returned campaign path in the conversation. The supervisor allocates
   a UUID directory under `TestResults/qualification-supervisor`. One persistent
   project reservation and an OS execution lock prevent conflicting campaigns.
3. Launch once:

   ```text
   python Tools/Presentation/qualification_supervisor.py launch <campaign-path>
   ```

   The launcher returns immediately after dispatch. A detached Python supervisor
   owns the sequential campaign. Codex can end its turn; no polling messages,
   recurring automation or open model turn is needed. Windows must remain running.
4. When the conversation resumes, inspect once:

   ```text
   python Tools/Presentation/qualification_supervisor.py status <campaign-path>
   ```

   `PASSED` means every selected stage has a matching atomic terminal record,
   zero child exit, applicable source/inputs/tools and unchanged evidence. Test
   stages additionally require complete NUnit XML, exact discovered/leaf/root
   counts, no failures/inconclusive cases, and exactly the named expected skips.
   Unity's `Skipped:Ignored` root verdict is accepted only with those exact
   expected skips and matching counts; distinct parameterized cases are counted
   by their unique NUnit IDs even when their display names coincide.
   `RUNNING` identifies a verified live supervisor; return later without launching
   another campaign. `INCOMPLETE` never constitutes a passing test gate.

`status` is read-only, including repeated inspection. Historical recorded results
remain on disk if source/tools change, but current applicability becomes
`INCOMPLETE`. Inspect their recorded SHA separately; never relabel them as results
for a newer commit. Raw logs stay local because Unity CLI logs can contain auth
arguments; publish compact sanitized evidence rather than raw Unity logs.

## Plans and stage selection

The JSON has `purpose: "qualification"`, `source_sha`, `tools`, `isolation`,
`approved_substitutions`, an ordered `stages` array and a `selections` object.
`tools` maps absolute file paths to `{ "sha256": "...", "version": "..." }`.
`isolation` records `product`, `save`, and any build/disposable project identities.
The supervisor does not install substitutions or change project files.

Each stage has these fields:

| Field | Contract |
| --- | --- |
| `id`, `role` | Unique safe ID; `preflight`, `test`, `build`, `report`, or `final` |
| `depends` | Explicit IDs that precede this stage; all must be selected |
| `command` | Absolute executable followed by individual arguments; no shell splitting |
| `timeout_seconds` | Finite positive OS wait limit; set per workload |
| `xml`, `expected_tests`, `allowed_skips` | Test report relative to stage directory, exact positive leaf count and exact fully qualified skip names |
| `artifacts` | Other report/log filenames or artifact directories relative to the stage directory; required for build/report roles |
| `input_identity` | Optional SHA256 of the exact input map for a reviewed transition; defaults to campaign snapshot |
| `after_input_identity` | Optional predeclared input-map SHA256 after restoration/checkpointing; never learned from changed inputs |

Arguments can contain literal `{project}`, `{campaign}`, `{stage}`, and `{id}`.
Other braces remain literal. Every stage gets a fresh directory; artifacts must
stay within it and cannot use symlinks/junctions. `stdout.log`, launch identity,
actual child identity and exit record are retained and hashed automatically.
Artifact directories are sealed by exact file-set and content hashes, including
build outputs. Build/native source identity and semantic pass assertions remain
the existing build/native report authorities' responsibility; their commands must
return nonzero on failure and retain those reports as artifacts.

For example, a selection might explicitly name:

```json
{
  "focused": ["focused-preflight", "focused", "focused-preservation"],
  "required": ["edit-preflight", "full-edit", "play-preflight", "full-play", "build-preflight", "build", "native-preflight", "native", "required-preservation"]
}
```

Each workload needs a direct preflight dependency. The selected final preservation
stage must be last and depend on **every** selected preceding stage. Commands
with identical executable/argument arrays and duplicate stage IDs are rejected.
Selections never expand automatically. A passing `focused` campaign means only
its selected gates passed; it never substitutes for mandatory final qualification.

Use the existing campaign's `preflight`, checkpoint/restoration, `finish`, build
seal and evidence collectors as the authorities. Preserve their exact-input,
output/save/product isolation and final preservation assertions. Include fresh
preflight commands for each workload and retain their emitted reports. A stage
that changes Unity-generated inputs must restore them before returning, or declare
the exact reviewed before/after input identities. All files in Assets, Packages,
ProjectSettings and ContentAuthoring count, including ignored/generated metadata;
the existing `qualification_inputs.target_inventory` and reparse guards are reused.

The PR #245/#247 adapters and collectors contain frozen names, output paths and
source assertions. This supervisor deliberately does **not** reopen them or add
another source-text substitution wrapper. A future live campaign needs reviewed
fresh preparation/commands compatible with those authorities and unused identities.
The disposable proof below demonstrates process orchestration, not new Unity
qualification or automatic preparation of historical campaigns. Parameterizing
frozen adapters would be a separately scoped follow-up.

Focused plans must not select sustained profiles implicitly. Complete existing
EditMode/PlayMode suites remain available as explicit stages and retain every
mandatory test. Some existing full-suite methods include sustained 68-run profiles;
this tool does not split methods, change fixtures or add skip/filter exceptions.
Precise extraction of sustained workloads into separately selectable Unity methods
requires a separate PR. Documentation-only changes do not select those workloads.

## Qualification efficiency policy

This policy governs selection and use of the existing qualification authorities;
it does not change campaign execution, stage semantics, completion, cancellation,
resume, preservation, or merge requirements. A high cost or long duration alone
does not waive a required gate.

Campaign plans remain explicitly selected and source/tool-pinned. Every workload
keeps its direct preflight dependency, terminal records and evidence hashes remain
atomic, and NUnit discovery/counts/named expected skips remain exact. Focused and
required selections stay distinct; status remains read-only; resume remains
limited to verified completed-stage boundaries; reservations, process ownership,
and final preservation remain mandatory.

### Risk-based selection and reporting

Before qualification, identify the applicable scope and classify checks as:

1. Mandatory repository and merge gates established by current governance.
2. Tests of directly affected functionality.
3. Required cross-system integration checks.
4. Applicable platform, filesystem, persistence, and lifecycle compatibility checks.
5. Diagnostic-only tests that help explain behavior but are not gates.
6. Performance investigations, distinguished from binding performance gates.

Mandatory gates remain mandatory unless approved governance changes them. A
focused selection cannot replace a required full-suite selection. For each stage,
state whether it is required, optional, reused, or Not Applicable, with a concise
reason. Not Applicable means the stage's behavior is outside the changed scope;
it is not a pass. Preserve explicit Unit/SIT/UAT and build-promotion applicability
and the player-facing UAT handoff rules in the test-stage matrix, build-promotion
policy, and owner-UAT lifecycle authority.

Choose the smallest sequence that establishes every applicable gate and material
risk. Run diagnostics or exploratory tests when they resolve a stated uncertainty
or inform a decision; do not silently promote them to gates or report them as
required evidence. Prefer static inspection when it fully proves a specific
documentation or source claim and runtime evidence is not required. Static review
does not replace required Unity execution, persistence, native filesystem, save
lifecycle, reopen, or standalone checks.

### Evidence identity and reuse

Reuse a completed passing result only after verifying its exact source identity,
complete tool identity (including the pinned adapter/helper chain), configuration,
inputs, output artifacts and hashes, test applicability, and execution context.
The result must still satisfy the current gate. Record what was reused and why it
applies. Evidence from a different source or tool identity is historical or
comparative evidence, never a new pass for the current identity. Clearly
distinguish retained historical results from evidence qualifying the current PR.

Documentation-only changes, and narrowly scoped external qualification-metadata
changes, may not require rerunning unrelated tests when a documented applicability
review establishes that their source, inputs, tools, and gate remain valid. Do not
infer applicability from the label “documentation-only” alone if an input or
execution condition changed. Never relabel a prior result, alter its recorded
identity, or imply a stage ran when it was reused or Not Applicable.

### Focused smoke tests and Unity-generated inputs

Do not launch a separate Unity smoke campaign automatically after every small
correction. Use a focused smoke test when it materially reduces risk or avoids a
likely expensive failure, and do not repeat a campaign whose assertions are
already covered by applicable accepted evidence. If static inspection completely
establishes a narrow claim, it may be sufficient for that claim only; retain all
runtime and persistence verification required by governance.

TMP, URP, PlayerSettings, and UnityConnect serialization changes have known
previously observed patterns that can guide diagnosis, but pattern recognition
never authorizes ignoring or restoring a change. Establish exact provenance and
compare original and generated bytes. Preserve both byte sets and their hashes
before any explicitly authorized restoration. Distinguish a verified known
generated pattern from an unexplained difference; use the existing input inventory,
preflight, checkpoint/restoration, and preservation authorities. Never silently
revert unknown inputs, overwrite owner work, or disregard an unexpected project
change.

### Process ownership, failure, and recovery

Retain exact process identity checks, executable and creation identity checks,
Windows Job Object ownership, persistent reservations, and fail-closed recovery.
Documented Unity authentication brokers and ADB helpers may be recognized during
diagnosis; a similar process name alone does not establish campaign ownership.
Recheck executable identity, creation identity, and ownership wherever the
existing supervisor requires it. Do not repeat a completed root-cause investigation
unless new evidence materially changes the diagnosis.

An uncertain or incomplete campaign remains incomplete. Exceptional restoration,
process termination, reservation release, or preservation substitutions follow
the existing explicit owner-approval process. Never add force-unlock behavior,
blanket ADB termination, or unrestricted process termination. This efficiency
policy does not change timeout, completion, cancellation, or resume semantics.
Preserve unresolved evidence and artifacts, and do not continue past a failure in
a way that implies final preservation succeeded.

### Build and performance evidence

Consider reusing a successful build only when every applicable authority confirms
the exact source identity, toolchain and build configuration, complete artifact
file-set and hashes, required build-report authority, valid output and product
identity, and required process and preservation evidence. Do not rebuild merely
because an unrelated reporting artifact changed. A prior build cannot qualify
another source or tool identity without an explicit supported applicability
decision. Required executable standalone, save lifecycle, reopen, and native
filesystem checks remain required when applicable; static build inspection does
not replace them.

Run benchmarks when a change affects a relevant workload, an explicit performance
requirement applies, a measured regression exists, or necessary performance
evidence is missing. Do not repeat successful comparable measurements solely
because unrelated qualification metadata changed. Separate measurement-only
results from binding performance gates and retain source, environment, sampling
method, comparability, and measurement limitations. Do not infer improvement from
non-comparable measurements. Existing mandatory performance gates remain intact.

### Review checkpoints and qualification infrastructure

After independent review approves a testing sequence, continue through its
routine passing stages without requesting a separate independent review after
each stage. Stop and report unexpected test/build failures, material
implementation or scope/specification changes, input/artifact preservation or
process-ownership uncertainty, an authorization boundary, significant new risk,
or any change that invalidates the approved sequence. Final independent merge
review remains mandatory. No intermediate checkpoint is advance merge approval.

Before proposing a test adapter, validator, inspector, script, supervisor, or
framework, inspect existing authoritative components and prefer their fixtures,
collectors, preflight tools, validators, and supervisors. Do not duplicate an
assertion already guaranteed by a canonical authority without a demonstrated
distinct failure-detection benefit, or build layers that only reverify earlier
verification layers. If current tools cannot establish a required property,
document the specific gap before proposing infrastructure. Do not implement a
second qualification framework under this policy.

### Concise reports and test-suite maintenance

Qualification reports should be compact and decision-oriented while retaining
enough detail to verify the verdict. State the source and changes reviewed; tests
required, run, reused, or Not Applicable; results and evidence locations; blockers
and limitations; input, process, and artifact preservation disposition; and
independent-review or merge recommendation. Link retained investigations instead
of repeating long narratives. Do not omit facts needed to establish the result.

Maintain tests for distinct failure-detection value, not cumulative case count.
Periodically review semantic duplication, obsolete or superseded assumptions,
expensive repeated setup, disproportionate runtime, redundant parameterizations,
similar tests with different execution contexts, and historical regression
protection. Do not remove a test only because it is old, similarly named, or
currently passing. Preserve protection for determinism and canonical ordering;
invalid states and atomicity; stable IDs and versioned saves; frozen migrations
and backward compatibility; corruption and recovery boundaries; localization and
player trust; platform-specific persistence; and required integration contracts.
Test consolidation needs separate review, evidence, and a separate PR. This policy
PR changes no test or qualification executable.

## Resume, failure and cancellation

### Persistent reservation location (H1-F3 completion correction, 2026-10-26)

New campaigns keep `active.json` and the OS execution lock in
`TestResults/qualification-supervisor-reservation`, outside Unity-owned `Temp`.
The old `Temp/qualification-supervisor/active.json`, when present with an owner,
blocks new claims; it is never migrated or cleared automatically. A missing old
marker does not authorize replacement of a recorded INCOMPLETE campaign. Inspect
its immutable evidence and exact process identities and obtain the exceptional
recovery approval described below. Do not mix supervisor versions on one project.

The five-second Job Object completion drain is unchanged. The installed Unity CLI
documents a resident authentication broker with a two-minute idle lifetime. An
explicit campaign-specific runner may set `UNITY_NO_AUTH_BROKER=1` only for its CLI
child, using documented direct OS-keyring access, rather than creating that resident
helper inside a test job. Pin the runner and record this invocation choice in the
plan. This does not exempt any descendant from OS completion, extend waits, change
authentication configuration globally, or retroactively complete an old campaign.

A usage reset does not stop the detached supervisor. First use `status`; never
create another campaign because the model stopped responding. If an interrupted
supervisor is gone **between verified completed stages**, Codex can explicitly use:

```text
python Tools/Presentation/qualification_supervisor.py resume <campaign-path>
```

This dispatches a detached replacement supervisor only after checking that no
previous dispatch/supervisor is still alive and every previously started stage has
verified passing completion. Completed stages are reused. An atomic stage result
published immediately before a supervisor crash can be recovered by matching its
identities, exit record and hashes. `run` is the synchronous diagnostic equivalent.
Nothing automatically relaunches failed or uncertain work.

If a child/host is still running after its supervisor vanishes, source/inputs
mismatch, or any completion/exit/report identity is missing, preserve everything
and report `INCOMPLETE`. A stale PID is checked against creation FILETIME and actual
executable, not accepted as a living owned process. Access/query errors fail closed.
The unresolved project reservation remains; there is intentionally no force-unlock
or automatic repair/retry option. Codex must investigate and obtain owner approval
for any recovery that would release an uncertain campaign or terminate an orphan.
Failure stops later stages, including restoration/final checks; it never claims
final preservation passed. Use the existing authority for reviewed recovery rather
than silently restoring/overwriting changed inputs.

Explicit active cancellation is:

```text
python Tools/Presentation/qualification_supervisor.py cancel <campaign-path>
```

It signals the verified supervisor's private event. That supervisor terminates only
its currently held Job Object and records `CANCEL`; timeout uses the same owned
job mechanism. Hosts wait for a parent handshake before launching a workload, so
job assignment and durable ownership publication precede the actual child.
Descendants cannot opt out through the default job breakaway policy. Completion
uses OS process handles and job completion-port notifications, with a bounded
five-second accounting drain. Remaining descendants produce `INCOMPLETE`.
No kill-on-close policy is installed: a supervisor crash preserves live work.
Cancellation of abandoned jobs is deliberately refused; a job's named handle may
disappear when its supervisor dies even while descendants survive.

Do not access normal owner saves, delete historical evidence, overwrite existing
builds, terminate unrelated processes, install monitoring/queue dependencies or
change Codex configuration. Owner approval remains necessary for exceptional
preservation substitutions, orphan termination/reservation recovery and any model
fallback specified by the repository model policy. Commands themselves remain
reviewed qualification code; the supervisor cannot sandbox arbitrary Python.
Independent review and existing merge gates remain required.

## Disposable proof and verification

Codex can create a new fake-input Git project without touching the real Unity
project using:

```text
python Tools/Presentation/test_qualification_supervisor.py --prepare-demo <unused-directory> ok 10
```

Then `launch` the returned campaign, end the turn or perform independent work, and
later `status` it once. `partial` instead of `ok` emits malformed XML and must fail.
The `fixture` plan purpose is restricted to this exact disposable test helper; it
cannot be used to bypass production preflight gates with arbitrary commands.

Run the focused tests on Windows:

```text
python -m unittest discover -s Tools/Presentation -p test_qualification_supervisor.py -v
python -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py -v
python -m unittest discover -s Tools/Presentation -p test_b3a_preservation_preflight.py -v
```

See [retained verification](evidence/qualification-supervisor/verification.md).
No full Unity suite, sustained profile or Windows Development build is needed to
qualify this tooling change.

The retained duration diagnosis records Codex CLI `0.163.0-alpha.10.2`, queue help
availability and Windows socket error `10050`; exact Desktop-thread delivery has
not been demonstrated. There is no queue dependency, callback, socket retry or
scheduled model polling here. A future optional callback requires a successful
exact-thread delivery demonstration first, in a separate change; callback failure
must never change a verdict or trigger a rerun. The owner still returns to the
conversation and asks Codex to inspect results. No Unity speedup or token savings
is inferred from detached orchestration.
