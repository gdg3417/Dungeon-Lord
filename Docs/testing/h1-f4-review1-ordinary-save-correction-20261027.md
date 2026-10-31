# H1-F4 review 1: ordinary-save rollback and observed background

Branch `codex/h1-f4-recovery`, existing draft PR #259, main base
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. No schema, tuning, lifecycle or
time-accounting policy change. Recommendation remains GPT-6.1 Sol High (Complex),
availability fallback GPT-6 Sol High. Owner UAT, merge and broad Windows/benchmark
qualification remain prohibited while correctness blockers are open.

## Confirmed runtime defect and correction

The real SaveService/GameRoot test first closes recovery, activates the dormant
scheduler at W=105 / A=C=0, and changes test-only metadata. A candidate write fails
before mutation, and the existing atomic protocol proves rollback to the exact
predecessor. TimeService retains a known foreground stall at the already covered
cut. An ordinary frame at 110 used to see HasDeferredRecovery and attempt
BeginCanonicalRecovery(observedBackground:true), although no background observation
existed. BeginObservedRecovery correctly rejected this, producing a root clock fault.

Source `1ee3c8e42b5085197725172baa29868191c6233e` reproduces the defect:
105 passed, 1 failed, with living.foreground.clock_invalid at that first frame.
The failure is preserved as FAILED, not reclassified as a fixture or environment
issue. No generated Unity input changed; exact settings restoration and final
preservation were separately verified after the terminal failure.

Runtime correction `3bb229b97e2d45751152739c34cd32f40f386b20`:

- Expose read-only views of the existing bounded TimeService stall/background
  flags. No new writable state, cursor, wallet, interval log or durable field.
- Hold a known foreground persistence stall in UpdateForeground. No per-frame
  storage retry, new acceptance or offline episode is inferred from the stall.
- An explicit save/lifecycle barrier forces proof of the retained foreground
  prefix even when W and A already cover it. Successful authority proof releases
  the stall at Q without moving W to Q.
- Require actual observed background before entering known-background recovery
  from either deferred coordinator branch. Preserve old accepted obligations and
  the existing recovery-required behavior for unresolved authority.

The first correction run passed 106/107; only a test assertion wrongly expected
the next captured second to be immediately durably accepted by a frame with no
due event. `5c52f301bbe919611966347ef15dfe3640cfd358` adds the explicit save barrier
before asserting durable acceptance/reopen. Runtime code is unchanged from 3bb229b9.

## Deterministic coverage and authority audit

At the final qualified runtime/test source 5c52f301:

- Foreground-only failure: exact predecessor unchanged, no frame-loop write,
  no background episode, explicit retry succeeds, stalled time earns no mana or
  new logical time, W remains 105 at retry Q=110. At 111 the captured horizon is
  1000; an explicit barrier durably accepts it, and reopen preserves wallet/A.
- Genuine background after failure: [105,110) is known stalled foreground and
  excluded. The pause barrier proves the prefix. Observed background [110,120)
  yields exactly 100,000,000 eligible 100-ns units. Original and residual settle
  once; [120,122) recovery processing contributes the grouped residual only.
  A remains zero. Reopen at 130 starts only the unknown [122,130) suffix and no
  adventurer progression; it does not replay the known stalled interval.
- Actual original/residual committed-candidate/lost-acknowledgement tests now
  inject corrupt/unrecognized active bytes while retaining the pending pair.
  Resolution refuses them without write, replacement or evidence deletion and
  keeps recovery required. Restoring the exact committed candidate permits proof
  without another write/replacement, changes no accepted/closed horizon, and a
  second resolution cannot issue another settlement.

ResolveUncertainRecoveryPublication first compares exact active bytes against its
retained predecessor/candidate, flushes the directory, and uses the existing
Persist(active,active) semantic-readback path. That path may delete bounded owned
canonical-write evidence after recognizing active authority, but does not write or
replace active when original/candidate are that same byte array. It does not call
mana/healing settlement again. Corrupt/third-state refusal occurs before cleanup.
These new checks do not replace the still-required complete protocol-cut matrix,
historical active v1/v2 qualification or accepted-party/repeated-crash regressions.

## Actual qualification

| Source | Campaign | Result |
| --- | --- | --- |
| eb95f2506b9982e675c5c53adc5545c1fc133205 | f87a3cd4f3594da0a320c88ad1054d19 | 105 passed, 0 failed, 0 skipped; all stages PASSED |
| 1ee3c8e42b5085197725172baa29868191c6233e | 7a2e083b24994e4880d971b98b4f58f5 | 105 passed, 1 failed, 0 skipped; expected defect reproduction, FAILED |
| 3bb229b97e2d45751152739c34cd32f40f386b20 | d9e8d4add7ac411b834e5c1fda5c217e | 106 passed, 1 failed, 0 skipped; test's durable-acceptance assertion, FAILED |
| 5c52f301bbe919611966347ef15dfe3640cfd358 | 6e05f8f9c07c46b4aacd601a1d754051 | 107 passed, 0 failed, 0 skipped; all stages PASSED |

Ten preparation-tool unit tests passed after the discovery-count corrections;
they are not gameplay qualification. The [source-pinned qualification report](h1-f4-review1-qualification-20261027.json)
retains stage verdicts, process identities/exits, input identities, isolated product
names and exact evidence hashes. Passing campaigns include full process completion,
restoration and final preservation. Failed campaigns retain their verdicts and
separate fresh preflight/restoration/final reports in their owned preparation dirs.
No process was terminated or uncertain reservation released in this milestone.

## Remaining blockers and next checkpoint

### Research and additional authority milestone

Runtime/test source `b9e455a430095326b02a704036296d5a54b52549` passed
**268/268, 0 failed, 0 skipped** in campaign
`7608b6e14bce46c5a2a8121cecd2fc20`, all stages PASSED. This contains 111 recovery,
77 structural-preview, 59 canonical-write and all 21 durable Research cases.
Fourteen preparation-tool tests passed. The
[Research/authority qualification report](h1-f4-review1-research-authority-20261027.json)
records the intermediate failures, complete process/stage evidence, input hashes
and final passing result. It maps all 21 original Research failures to passing cases.

The Research fixes are fixture-only: author the existing test project/scaffold
rules into exact full input bytes before constructing a synthetic current owner;
retain CombatC0 explicitly when authoring the test configuration; use supported
production budgets; preserve run-1's allocator at NextRunSequence=2; assert schema
17 for this current-writer test. No historical serializer or asset changed.
Research01 remains FAILED at 243/264 (21 generic-budget setup failures), and
research02 remains FAILED at 263/264 (one obsolete current-schema assertion).

The targeted source `a878d6a03c0630b45266ac075aa7f414afeda454` then reproduced
three actual runtime failures: 264/267 passed. TimeService dereferenced the absent
scheduler of a valid dormant owner during ordinary acknowledgement; proven retry
threw and committed-candidate/lost-ack proof could not complete. A focus callback
during ordinary metadata publication could also attempt another writer and fault
the root. All failed inputs were unchanged and restoration/preservation verified.

Correction: only a dormant zero prefix can be acknowledged without a scheduler;
any nonzero captured work still requires actual foreground closure. W stays at S,
not Q. SaveService now exposes a read-only pending view of existing ForegroundWriting
and pendingCandidate, and uses it at preparation/foreground entry points. GameRoot
holds direct flush/frame entry while publication is pending. No second authority
or new persisted field is added. Focus and disable observations still enter the
one TimeService at their observation time; nested publication is denied.
The 268-case pass verifies both dormant dispositions, zero-write committed-candidate
proof, Q reanchoring without stalled credit, and focus/disable during ordinary IO.

Preserve the four Research/authority campaigns named in the JSON and preparations
TestResults/h1-f4-20261027-research01, -research02, -dormantrepro01 and -dormantfix01,
including all archives and distinct post-failure preflight/restoration/final reports.

Subsequent fixture-only milestone `9319d4bce5ca3a57603cb5bbc194a3b59c653f30`
passed **243/243, 0 failed, 0 skipped** in campaign
`0c26db5fd0ad4bc89ca46241a095ce77`, all stages PASSED. The explicit cohort is
107 recovery + 77 structural-preview + 59 canonical-write cases. It corrects
unbound current-schema fixture contexts, current native creation, valid bound
Research state for nullable clearing, and metadata expectations that previously
advanced W. Production validation and frozen serializers are unchanged.

The [fixture qualification report](h1-f4-review1-shared-fixtures-20261027.json)
maps 87 earlier failed cases to passing affected cases (including the renamed
current-schema native-creation test). Three earlier null-reference cases now pass.
This does not establish a new full-suite failure count. Twelve preparation-tool
unit tests passed. Preserve this campaign and its
TestResults/h1-f4-20261027-sharedfixtures01 preparation/input archive and receipts.

The last full EditMode report remains **1966 passed, 805 failed, 1 skipped** at
16955152, campaign INCOMPLETE/UNCERTAIN. No broad rerun or reduced full failure count
is claimed. Across the two affected cohorts, 108 previously failing test positions
now pass; no new full-suite total is inferred. Next: complete remaining
consumed-configuration fixture factories, lifecycle/clock identity, the fourth null reference,
historical/current schema assertions and remaining individual failures. Strict
production validation and frozen history are unchanged.

Keep draft PR #259; push coherent milestones and request independent review of
the P0 transition and authority-resolution behavior. Full EditMode, complete SIT,
PlayMode, isolated Windows and the three-way performance comparison remain pending.

Artifact owner: H1-F4. Preserve all four campaigns under
TestResults/qualification-supervisor/<ID> above and their corresponding
TestResults/h1-f4-20261027-reviewbase01, -stallrepro01, -stallfix01 and -stallfix02
preparations, archives and preservation receipts. No cleanup is authorized for
these retained results or any historical INCOMPLETE/FAILED state.
