# Completion-driven qualification supervisor

Codex runs these commands, manages the plan and investigates failures. The owner
makes material project decisions, performs applicable gameplay UAT and merges
independently approved PRs. The supervisor requires Windows, Python 3.11+, Git
and the existing qualification tools.

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
   owns the sequential campaign while Windows remains running. Retain the branch,
   source and input identities, campaign path, last verified checkpoint and
   remaining gates for resumption.
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

The H2 PR 1 adapter supports one qualification report with `planned_stop`.
It pins the `canonical-post-replace` rendezvous and a unique 128-bit nonce. The
supervisor verifies the marker, actual executable and creation identity, live
membership in its held job, isolated save path and exact active/rollback hashes
before stopping that job. It records the observed nonzero OS exits and empty job,
then captures the original save directory before the adapter can reopen a clone.
The report's terminal evidence is the planned-stop receipt in place of the
ordinary child exit record. The campaign still requires two ordinary Bootstrap
reopens and final preservation. A normal exit is a failed interruption, and a
wrong or missing marker cannot trigger it. Ordinary cancellation/timeout and
uncertain recovery keep their established meanings.

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

Record the readiness stage, changed systems and affected dependencies, required
checks, optional diagnostics, reused evidence, Not Applicable checks with reasons,
applicable final gates deferred until owner UAT acceptance, and material blockers.
Use the [test-stage matrix](../../docs/planning/test-stage-matrix.md) for Unit,
SIT and UAT applicability and the [build-promotion policy](../../docs/planning/build-promotion-policy.md)
for promotion. A Not Applicable entry supplies no passing evidence. A focused
selection cannot replace a mandatory full-suite gate.

Choose the smallest sequence that establishes every applicable gate and material
risk. Run diagnostics or exploratory tests when they resolve a stated uncertainty
or inform a decision; do not silently promote them to gates or report them as
required evidence. Prefer static inspection when it fully proves a specific
documentation or source claim and runtime evidence is not required. Static review
does not replace required Unity execution, persistence, native filesystem, save
lifecycle, reopen, or standalone checks.

### Readiness stages

1. **Implementation iteration.** Compile when executable inputs change and use
   static inspection, affected Unit tests, focused SIT or diagnostics, and relevant
   data and localization validation. Use targeted reproduction and verification
   after corrections. Run a complete suite, product build, sustained profile or
   lifecycle campaign here when necessary for the affected risk or an applicable gate.
2. **Owner UAT readiness.** For applicable player-facing work, establish stable
   compilation, directly affected behavior and validation, material cross-system
   integration, and actual production interaction. Cover determinism, atomicity,
   persistence and save/reopen, localization, accessibility and representative
   rendering wherever affected. Supply a suitable isolated qualified build or
   approved Editor setup with verified process and artifact preservation. Run
   earlier gates required by the risk or a governing authority. Defer the other
   mandatory final gates explicitly. Independent review determines whether the
   owner may begin using the [prepared handoff](owner-uat-preparation-and-artifact-lifecycle.md).
3. **Final merge readiness.** After owner acceptance, normally run the remaining
   expensive gates for player-facing work. Mark owner UAT Not Applicable for
   technical work when justified. Establish every applicable mandatory Unit, SIT,
   UAT, build, persistence, platform, performance and preservation gate. After a
   correction, reconcile automated evidence and review the accepted interaction.
   A material change to accepted gameplay requires renewed acceptance of the
   affected behavior. An unrelated correction may retain acceptance with a
   documented applicability decision. Final independent review is mandatory
   before the owner merges.

### Applicability examples

Apply every mandatory gate triggered by the actual change and dependencies,
regardless of the PR label or changed directory.

| Change | Iteration and owner UAT readiness where applicable | Final applicability |
| --- | --- | --- |
| Documentation only | Validate structure, links, references, claims and exact changed scope. | Independent review and applicable repository checks. Unity gates and owner UAT may be Not Applicable after verifying inputs, executable authorities and player-facing behavior. |
| Qualification tooling | Run the affected Windows helper tests and disposable positive and negative integration cases. Trace any change to plan, inputs, ownership, verdict or preservation contracts. | Prove the affected executable gates, including real Unity execution if the changed contract requires it. Justify owner UAT applicability. |
| Presentation-only correction | Exercise production input, representative layouts and affected accessibility/localization. Verify no unauthorized state mutation where controls reach transactional flows. Prepare gameplay-focused UAT where applicable. | Retain mandatory rendering, platform, build and regression coverage for the affected surface and dependencies. |
| Gameplay simulation or construction | Run focused deterministic valid/invalid and atomicity cases, production interaction and relevant scheduler, save and reopen checks. Prepare representative gameplay UAT. | Run or reconcile all applicable full-suite, native, lifecycle, persistence and performance gates. |
| Content or configuration | Validate schema, stable IDs, references, ordering, localization and loading. Verify affected production behavior and prepare UAT for player-visible changes. | Protect export, build validation, compatibility, required platforms and relevant performance limits. |
| Save, schema, migration or recovery | Exercise exact readback, applicable supported migrations or obsolete-version refusal, interrupted publication, fail-closed recovery and isolated production reopen. Include native coverage early where the safety risk requires it. | Retain frozen historical contracts, relevant Windows Editor and standalone native filesystem evidence, lifecycle and preservation gates. Include UAT when the player-operated flow changes. |
| Performance-sensitive | Use focused, comparable workloads with pinned configuration, sampling and context. Include interaction responsiveness where it affects meaningful UAT. | Keep applicable binding thresholds, target platforms and sustained profiles, including separate workloads where required. |

### Evidence identity and reuse

Reuse a completed passing result only after verifying the recorded source SHA,
exact runtime input identity, complete pinned tool and adapter chain, execution
configuration, platform, isolated product and save identities, and applicable
process and preservation evidence. Verify exact test identities, scope and
assertions, report completeness and named expected skips, and artifact file-sets
and hashes. Record which current gate it satisfies and why. Different toolchains,
products, modes, graphics or isolation arrangements require an explicit supported
applicability decision before reuse.

Documentation-only and narrowly scoped external qualification-metadata changes
may use documented applicability review when source, inputs, tools and the gate
remain valid. Preserve the result's recorded execution SHA and verdict. Clearly
label prior-source results and reused evidence with their original identities.

After each correction, compare affected dependencies and identity changes against
collected evidence. Record which results still apply and which need fresh
execution. If a complete suite already exercised a required historical
regression or profile, map the exact NUnit case identity and result to its gate.
Editor, native filesystem, standalone and genuinely distinct workloads retain
their separate requirements.

### Focused smoke tests and Unity-generated inputs

Do not launch a separate Unity smoke campaign automatically after every small
correction. Use a focused smoke test when it materially reduces risk or avoids a
likely expensive failure, and do not repeat a campaign whose assertions are
already covered by applicable accepted evidence. If static inspection completely
establishes a narrow claim, it may be sufficient for that claim only; retain all
runtime and persistence verification required by governance.

Known TMP, URP, PlayerSettings and UnityConnect serialization patterns can guide
diagnosis. Restoration requires the exact authorization, identity and preservation
checks under [bounded operational recovery](#bounded-operational-recovery).

### Process ownership, failure, and recovery

Retain exact process identity checks, executable and creation identity checks,
Windows Job Object ownership, persistent reservations, and fail-closed recovery.
Documented Unity authentication brokers and ADB helpers may be recognized during
diagnosis; a similar process name alone does not establish campaign ownership.
Recheck executable identity, creation identity, and ownership wherever the
existing supervisor requires it. Do not repeat a completed root-cause investigation
unless new evidence materially changes the diagnosis.

A campaign passes only its selected gates. `FAILED` records a failed execution
and stops remaining stages, including dependent restoration or preservation.
`INCOMPLETE` preserves uncertainty and qualifies no mandatory gate. Keep recorded
verdicts, failed and partial evidence, and any separate preservation receipts.
The development task can continue with diagnosis and authorized corrections.
An unresolved source, lease, process or preservation conflict blocks affected
project writes and further campaign execution while safe read-only investigation
continues. Report final preservation only when verified.

### Build and performance evidence

Consider reusing a successful build only when every applicable authority confirms
the exact source identity, toolchain and build configuration, complete artifact
file-set and hashes, required build-report authority, valid output and product
identity, and required process and preservation evidence. Do not rebuild merely
because an unrelated reporting artifact changed. Reuse a sealed qualified build
for another isolated scenario when the approved preparation authority supports it.
A prior build requires an explicit supported applicability decision to qualify
another source or tool identity. Applicable standalone, save lifecycle, reopen
and native filesystem gates require their own execution evidence.

Run benchmarks when a change affects a relevant workload, an explicit performance
requirement applies, a measured regression exists, or necessary performance
evidence is missing. Do not repeat successful comparable measurements solely
because unrelated qualification metadata changed. Separate measurement-only
results from binding performance gates and retain source, environment, sampling
method, comparability, and measurement limitations. Do not infer improvement from
non-comparable measurements. Existing mandatory performance gates remain intact.

Report stage and campaign elapsed times from retained timestamps. Use NUnit
case durations for expensive embedded workloads and identify distinct executions
separately. Embedded durations are already inside their suite totals. Record
executed versus reused gates and material reasons for retries. Where measured,
identify build, test, preparation, repeated work, recovery and review or owner
waits. Report timing gaps and measure resource consumption before claiming
savings. See the [headless suitability assessment](#headless-suitability-assessment)
before proposing a change to a graphics mode.

### Autonomous execution and review checkpoints

Codex inspects the applicable authorities and implementation, implements the
approved scope, then compiles and runs the applicable checks. On failure, retain
the original evidence, identify the root cause, correct it within scope, run the
smallest appropriate verification and reconcile affected evidence. Continue the
remaining applicable gates. Compilation, test, discovery, fixture, build, argument,
artifact, localization and accessibility defects, and environmental failures with
a safe approved recovery path, ordinarily follow this loop. Routine investigation,
corrections, reruns and authorized scenario preparation need no new owner approval
or independent review per attempt. Apply the [model policy](../process/AI_Model_Selection_Policy.md)
to choose the lowest appropriate configuration for bounded corrections.

Base each meaningful retry on new evidence, a correction or verified recovery.
Use focused baseline comparison or applicable retained evidence to establish
whether a defect is inherited and blocks this scope. Preserve failed and
incomplete results after resolution. Avoid repeated expensive runs without new
evidence, speculative cross-system fixes, wider baseline repair, and model
escalation prompted solely by a failed test. A retry cannot gain a pass by
extending its timeout without evidence, weakening assertions, adding unapproved
expected skips or changing approved behavior to match an incorrect implementation.

### When to pause development

Pause the affected work at the following boundaries. Safe independent work may
continue where the boundary permits.

- **Completion.** The implementation and applicable checks are ready for the
  next required review or handoff.
- **Material owner decision.** Gameplay behavior, tuning, scope, a specification
  conflict or another consequential project decision requires owner direction.
- **Technical blocker.** Tools, permissions, dependencies or environment prevent
  meaningful progress after reasonable in-scope diagnosis.
- **Safety or authorization.** Further action requires uncertain process handling,
  unauthorized restoration, owner save access, reservation release or an
  unsupported preservation substitution.
- **Material architecture or integrity uncertainty.** A correction would change
  an approved authority, save or migration contract, architecture boundary or
  determinism invariant, or materially expand the approved task.
- **Independent review or acceptance.** Independent ChatGPT review evaluates
  material design and correctness risks, UAT readiness, and final merge
  readiness. The owner performs gameplay acceptance and merges approved PRs.
  A material change that invalidates an earlier review returns to the affected gate.
- **Investigation exhausted.** Further attempts would repeat an unsuccessful
  approach without new evidence, consume disproportionate resources or require
  speculative changes outside scope.

### Qualification infrastructure

Before proposing a test adapter, validator, inspector, script, supervisor, or
framework, inspect existing authoritative components and prefer their fixtures,
collectors, preflight tools, validators, and supervisors. Do not duplicate an
assertion already guaranteed by a canonical authority without a demonstrated
distinct failure-detection benefit, or build layers that only reverify earlier
verification layers. If current tools cannot establish a required property,
document the specific gap before proposing infrastructure. Do not implement a
second qualification framework under this policy.

### Concise reports and test-suite maintenance

Give concise progress updates and consolidate resolved failures at the next
meaningful review, UAT or final qualification report. Retain the full technical
evidence, diagnosis, correction identities and verification locally. Include
material failures and dispositions alongside source, gate applicability, results,
durations, limitations and preservation status. Link the retained records.
For a genuine blocker or exceptional authorization, consolidate the specific
decision, affected identities, evidence, attempted diagnosis, practical impact and
safe next action into one packet where practical.

Maintain tests for distinct failure-detection value, not cumulative case count.
Periodically review semantic duplication, obsolete or superseded assumptions,
expensive repeated setup, disproportionate runtime, redundant parameterizations,
similar tests with different execution contexts, and historical regression
protection. Do not remove a test only because it is old, similarly named, or
currently passing. Preserve protection for determinism and canonical ordering;
invalid states and atomicity; stable IDs and versioned saves; frozen migrations
and backward compatibility; corruption and recovery boundaries; localization and
player trust; platform-specific persistence; and required integration contracts.
Test consolidation needs separate review, evidence and a separate PR.

## Resume, failure and cancellation

### Persistent reservation location (H1-F3 completion correction, 2026-10-26)

New campaigns keep `active.json` and the OS execution lock in
`TestResults/qualification-supervisor-reservation`, outside Unity-owned `Temp`.
The old `Temp/qualification-supervisor/active.json`, when present with an owner,
blocks new claims; it is never migrated or cleared automatically. A missing old
marker supplies no authority to replace a recorded INCOMPLETE campaign. Historical
or legacy reservations use the exceptional recovery path below. Keep one
supervisor version per project.

The five-second Job Object completion drain is unchanged. The installed Unity CLI
documents a resident authentication broker with a two-minute idle lifetime. An
explicit campaign-specific runner may set `UNITY_NO_AUTH_BROKER=1` only for its CLI
child, using documented direct OS-keyring access, rather than creating that resident
helper inside a test job. Pin the runner and record this invocation choice in the
plan. This does not exempt any descendant from OS completion, extend waits, change
authentication configuration globally, or retroactively complete an old campaign.

A usage reset leaves a verified detached supervisor running. On resumption, first
inspect `status`, recover the recorded checkpoint and continue the approved work
without asking the owner to restate it. Leave verified active work running.
If an interrupted supervisor is gone **between verified completed stages**, Codex
can explicitly use:

```text
python Tools/Presentation/qualification_supervisor.py resume <campaign-path>
```

This dispatches a detached replacement supervisor only after checking that no
previous dispatch/supervisor is still alive and every previously started stage has
verified passing completion. Completed stages are reused. An atomic stage result
published immediately before a supervisor crash can be recovered by matching its
identities, exit record and hashes. `run` is the synchronous diagnostic equivalent.
Nothing automatically relaunches failed or uncertain work.

A surviving host or child after supervisor loss, mismatched source or inputs,
missing terminal evidence, or a failed identity query leaves the affected work
`INCOMPLETE`. Compare a recorded PID against creation FILETIME and actual
executable. Preserve the unresolved reservation and evidence. Use the bounded
recovery policy before any affected mutation or replacement campaign.

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

Keep normal owner saves outside qualification and preserve active or uncertain
processes, historical evidence and qualified builds. Exceptional preservation
substitutions and orphan termination require explicit owner approval under the
existing authority. The supervisor runs trusted qualification code and cannot
sandbox arbitrary Python.

## Bounded operational recovery

The following standing authorization applies prospectively only after independent
approval and owner merge of this policy, and only through an existing approved
authority that can positively establish every applicable condition. It grants no
retroactive authority over prior campaigns. Ambiguity or missing evidence keeps
the affected work blocked. Continue safe read-only diagnosis and consolidate the
exceptional owner authorization request where needed. Force-unlock, blanket
process termination and automatic orphan handling remain outside this policy.

### Predeclared temporary input restoration

Before execution, identify the exact project, worktree, campaign and
campaign-owned temporary paths. Retain the complete original bytes and
predeclare the exact modified identity. At recovery, require all of the following:

1. The existing execution lease is held. Applicable processes and jobs are
   positively quiescent. Verify records against the exact project and campaign.
2. Path containment and reparse protections pass. The complete four-root input
   inventory, including ignored and generated files, has no unexplained change.
3. Current bytes match the predeclared modified identity exactly. Archive both
   byte sets and hashes before writing. Verify protected saves, qualified
   artifacts and retained evidence against their preservation records through
   the authorized authority.
4. Restore only those declared files with the approved restoration authority.
   Read back the exact original bytes, verify preservation again and retain a
   durable receipt with campaign, lease and process evidence, exact paths and
   hashes, and the outcome.

Known serialization patterns alone supply no authority. Missing backups,
unexpected generated changes, changed hashes, additional files, uncertain
provenance or a partial or unverified restoration require exceptional review
and owner authorization before further affected writes. Preserve the originals,
observed state and receipts. Follow the authority's verified receipt if the
operation has already completed. Broad Git reset, checkout, clean and restoration
are outside this authorization.

### Inactive reservation recovery

The existing supervisor releases its reservation during normal terminal handling
for recorded `PASSED` or `FAILED` outcomes. A released reservation does not establish
input restoration or final preservation. A reserved, abandoned campaign requires
positive proof before any separate release:

1. Acquire the existing execution lease. Establish the exact reservation and
   campaign identity and confirm that no newer or conflicting reservation exists.
2. Verify exact recorded dispatch, supervisor, host, child, relevant job and
   applicable descendant identities. Positively establish that no owned workload,
   descendant or relevant job remains active and that process inspection had no
   unresolved error.
3. Verify the retained campaign records and evidence, and complete required
   restoration and preservation disposition. Revalidate the lease, reservation
   and quiescence immediately before releasing only the matching reservation.
   Record the verification, release and preservation outcome in a durable
   recovery receipt.

An expired or absent PID, available OS lock, absent window, normal quit marker or
missing named job handle alone supplies no proof of quiescence. If descendant
absence cannot be verified, preserve the reservation and use the exceptional
owner-authorization path. Any exceptional operation still requires a safe,
approved procedure. Preserve `FAILED` and `INCOMPLETE` verdicts and evidence
through recovery. After safe resolution, Codex may create a fresh applicable plan
with an unused output identity within approved scope. The supervisor never
restarts an interrupted workload automatically. Verified completed-stage resume
remains governed by the existing rules above.

### Existing authority and capability limits

The [supervisor](../../Tools/Presentation/qualification_supervisor.py) pins exact
input maps, including predeclared transitions, holds the execution lease and
checks retained stage evidence. [Input inventory](../../Tools/Presentation/qualification_inputs.py),
[preflight](../../Tools/Presentation/b3a_preservation_preflight.py) and reviewed
campaign checkpoint and finish commands cover specific preparations. Those
campaign-specific commands retain only their approved scope. An input-map
declaration by itself neither archives original bytes nor provides a post-failure
restoration transaction or receipt. Use routine restoration after failure only
where an approved authority can prove the full contract above. Otherwise use
exceptional approval.

[Windows process handling](../../Tools/Presentation/qualification_windows.py)
can verify an empty job while the supervisor owns its handle. Read-only
`status` checks recorded identities and evidence. After a supervisor crash,
these authorities cannot in general prove absence of all descendants, and there
is no recovery command that revalidates and releases an abandoned reservation
with a receipt. Such releases remain exceptional. The [planned-stop adapter](../../Tools/Presentation/qualification_planned_stop.py)
is a separate exact owned-job test operation with its own capture and receipt.
It supplies no abandoned-job recovery authority. This policy adds no executable
recovery path or automatic cleanup.

### Detached continuation limits

A detached supervisor can finish its selected campaign without model polling.
The Codex environment has no verified automatic continuation of this task after
campaign completion or a usage interruption. Continue from the saved checkpoint
when the conversation resumes. [Retained verification](evidence/qualification-supervisor/verification.md)
documents detached execution and the limits on conversation delivery. Keep
status inspection read-only and use the current supervisor without adding a
scheduler, queue, recurring poll or background framework.

## Headless suitability assessment

The project and installed Windows Editor use Unity `6000.3.2f1`. The inspected
Unity CLI was `1.0.0-beta.17`, whose `test --help` shows argument forwarding
with `-- -nographics`. Unity batch mode automates the Editor, and adding
`-nographics` omits initialization of the graphics device. Treat graphics
mode as part of the execution identity. See the [Unity 6.3 Editor arguments](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/editor).

| Classification | Inspected workload and implications |
| --- | --- |
| Rendering independent | `SpatialContentValidationTests.ValidMinimalCatalog_IsValid` and `NullCatalogAndInvalidLimits_FailClosed` assert data validation without a rendered scene. They are static candidates for a future focused comparison. The existing H1-F4 inventory command already selects `-nographics` for discovery, which supplies no execution verdict for the discovered cases. |
| Rendering dependent | `ProductionDungeonFloorSceneTests` sizes Game Views and captures frames. Production pointer, layout and accessibility cases exercising rendered UI retain the assertions and graphics mode they were qualified with. |
| Native player or graphics dependent | `Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification` requires the Windows Player platform and native filesystem. Even if its file operations need no pixels, an Editor run cannot replace it. Keep native lifecycles, actual input and owner observation in their relevant player environments. |
| Unknown pending evidence | Complete EditMode and PlayMode suites and embedded 68-run profiles need case and workload parity evidence. The H2 preparation adapter's existing headless build is scoped to its pinned build command and does not establish parity for test or player execution. |

This is a static assessment. No test was executed in a new graphics mode.
A future focused comparison must preserve equivalent assertions, exact discovery
and named skips, production behavior, platform contracts and evidence identity
before any promotion. This assessment leaves the existing execution configuration
and performance thresholds in force.

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

When executable supervisor, input or preflight contracts change, run the applicable
focused tests on Windows, including planned-stop coverage when that authority is
affected. Exercise newly authorized recovery against disposable projects with
positive and negative preservation, identity, lease and quiescence cases.
The existing core suites are:

```text
python -m unittest discover -s Tools/Presentation -p test_qualification_supervisor.py -v
python -m unittest discover -s Tools/Presentation -p test_qualification_inputs.py -v
python -m unittest discover -s Tools/Presentation -p test_b3a_preservation_preflight.py -v
```

See [retained verification](evidence/qualification-supervisor/verification.md).
The disposable proof covers orchestration. Select Unity, build and profile gates
separately when the changed contract requires them.
