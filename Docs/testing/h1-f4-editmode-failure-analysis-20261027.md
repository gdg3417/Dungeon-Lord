# H1-F4 exceptional recovery and prioritized EditMode failure analysis

Date: 2026-10-27. Branch: `codex/h1-f4-recovery`. Main base:
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`.

## Exceptional recovery: independently verified complete

Owner approval applied only to campaign `61c2a2cd452e4a5c837496834f680e55`.
The exact reviewed script (SHA-256
`52d72bc462b4f2b6ae7c47200e074471416e6e645c04fd4d512f94e513708dd6`)
and diagnostic (`374f0c03f72c1d5ef9f5d7f19ca80ffe79390ffb27857a2e5e7b98dfb7a98134`)
matched at approved source `16955152fcdd5a5f8af006dc38887a75f1f83266`.
Only the approved operation ran. Its receipt timestamp is
`2026-10-27T19:22:49.456974+00:00`.

All four prior versions were preserved and hash-verified before replacement.
Only the three named ProjectSettings members and the TMP fallback font were
restored from the verified original archive. Existing Lease authority released
only this reservation after input and protected-evidence verification. Independent
verification at `2026-10-27T19:23:30.521148+00:00` passed all ten checks: exact
source, four restored hashes, four preserved copies, complete original input map,
unchanged campaign evidence/verdict, released ownership, complete receipt,
protected evidence and original 9ed872 campaign evidence.

The [public recovery receipt](h1-f4-fulledit-recovery-receipt-20261027.json)
contains the full operation and independent verification. Local receipt:
`TestResults/h1-f4-20261027-fulledit01/owner-approved-recovery01/receipt.json`,
SHA-256 `9b47948a32d8ebee969aa3f2408e6891e728150611bf5b61fe25395ca9a6c58a`.
No partial restoration/release occurred. Neither incomplete campaign was reused,
relabelled, deleted or reinterpreted as descendant completion. No process was
terminated, force unlock used or supervisor rule changed. The original blocker
document and diagnosis remain historical records, unchanged.

## What the original full run established

At source `16955152fcdd5a5f8af006dc38887a75f1f83266`, raw NUnit reported 2,772
cases: **1,966 passed, 805 failed, 1 named skip, 0 inconclusive**. This is not a
passing campaign. All 102 H1-F4 cases passed within that raw report.
The font rewrite after a failed CLI exit caused the separate preservation blocker;
it has not been shown to cause the test failures. There is no confirmed OS,
installation, licensing or compiler cause that dismisses these 805 failures.
The 21 NUnit SetUp failures are application fixture setup failures.

The following disjoint categories use each failure's leading message, not presumed
root cause. Counts remain historical until actual affected cases are rerun.

| Category | Cases |
| --- | ---: |
| First-write validation rejection | 487 |
| Canonical authority contradictions (including 21 SetUp failures) | 44 |
| Recovery context/state rejection | 56 |
| Lifecycle, candidate and gameplay-state rejection | 58 |
| Explicit schema/count/limit literal expectations | 17 |
| Null-reference exceptions | 4 |
| Other assertions and behavior differences | 139 |
| Total failed | 805 |

Evidence: `full-edit/results.xml` in the original campaign, SHA-256
`aac1bab11bf2ba8b942c5c9828250f02cec636f29456b70921c87a2abc0a1f53`;
`TestResults/h1-f4-20261027-fulledit01/incomplete-investigation/failure-details.json`,
805 complete failure records, SHA-256
`ab2e4d0dafa65cdb645dd5232864d58cc5f32ed60a028965f46cbc0247c9ede6`.

## Priorities, confirmed findings and remaining work

### P0: canonical publication ownership and lifecycle/stall correctness

Confirmed runtime gap: SaveService.PrepareForegroundAction allowed an ordinary
lifecycle save while ForegroundWriting was true. GameRoot.FlushForeground returns
early during an existing pump; a callback during episode-entry publication could
therefore attempt a second canonical writer against the outstanding pair. Commit
`57096bebfc930595668dba372ae84b832007a8a8` rejects ordinary reentrancy at the common
service gate. Its deterministic integration case delivers focus loss from the
actual filesystem flush callback and requires one entry candidate, preserved R/W,
originalPending and no new acceptance. It uses the existing coordinator and clock.

Still open: TimeService marks a metadata publication failure as deferred/stalled.
GameRoot.UpdateForeground/FlushForeground also route deferred observations toward
BeginCanonicalRecovery(observedBackground:true), while BeginObservedRecovery
correctly requires actual observed background. Add a controlled ordinary-failure
case and resolve this branch without per-frame storage retries or converting a
known foreground stall to offline credit. This is a source-backed correctness
concern, not yet a claim of a passing reproduction or a new policy decision.

### P1: shared first-write and context fixtures

420 of the 487 first-write failure stacks include Fixture.Accept; 107 include
PhaseSixA4Tests.AddContent. These are overlapping stack observations, not counts
of separately proven root causes.

Confirmed fixture defect: PhaseSixA4Tests.Writer passed a freshly parsed Loot()
object. The strict successor writer requires the consumed bound loot object and
rejects a different object before ValidateSession can return authority. Commit
`d55139dacc338c08636bf607991c86d1b474297a` uses f.Context.RecoveryInputs.Loot.
An actual shared floor-content write is now exercised in the focused gate.

Other old factories replace f.Configuration, independently construct contexts or
pass separately loaded economy/loot. For example LivingDungeonTwoFloorTests.Dungeon
and LivingDungeonB3B2Tests.Dungeon overwrite Configuration, and Support creates an
unbound context. Correct these as explicitly authored synthetic current fixtures
with their exact consumed bytes before their canonical context is established.
Do not add production auto-rebinding, infer historical producer evidence, accept
unsupported profiles or disable reference/context checks. The 487 are not yet
claimed resolved.

### P1: Research setup and durability

The 21 SetUp failures comprise 18 ResearchDurableAcknowledgement cases and three
PlayerResearchSaveServiceIntegration cases. Their shared OpenService configured
only the older canonical path. Commit d55139da now binds its existing validated
inputs to one fixed test clock, configures recovery before loading, binds the
loaded horizon once and attaches each published save to that owner.
The new metadata integration case verifies a successful save retains context and W.

The full 21 cases remain unqualified. Their test-specific Research project/rate
scaffolds must also be represented in the exact authored bundle, rather than
silently replacing production rules after binding. Original/progress/claim and
fault/reopen behavior still require actual reruns and exact-once assertions.

### P1: four null references

Three are source-backed rejected-fixture dereferences: StructuralEditServiceTests
Preview_DerivedIdentityCollisionFailsBeforeCandidatePublication and the two
LiveCanonicalTargetUsesSelectedRoomAuthorityNotEconomicSlot cases parse synthetic
schema-17 bytes with an unbound DetachedCurrentTargetValidationContext, then use
the rejected State. Update their current-schema context construction and assert
validation success before dereferencing; preserve the original behavioral checks.

The fourth, RoomContentMapReassignmentCapacityMoveOwnedAndDiscard, dereferences
the scene draft after a coroutine. Its setup disables the root, which now delivers
an observed suspension, and requires lifecycle-aware setup investigation. It is
not yet dismissed as a fixture failure or fixed.

### P1: timing differences and scene clock identity

PauseResumeReanchorsWithoutBackgroundLogicalCredit expected 500, got 250;
SameSecondForegroundAndBackgroundRetainCapturedFractionsAndEligibility expected
7500001, got 2500001. These older paired-observation tests now use synthetic
precise-recovery saves, so unresolved background is held at the foreground prefix.
Keep historical format-1 assertions on explicit frozen readers/fixtures and add
successor expectations for the full recovery obligation; do not restore immediate
progression through unresolved absence merely to satisfy old assertions.

InstallForegroundBoundaryTestClock replaces GameRoot.TimeService after SaveService
has bound the original owner. Its coarse UTC override can also precede fractional W.
This explains why the current harness cannot prove successor lifecycle behavior.
Inject a controlled clock before binding, retain the single owner and validated
foreground policy, then rerun bounded pause/resume/retry/Research barriers.
The observed living.foreground.clock_invalid remains a failing case until rerun.

### P2: schema literals, diagnostics and remaining assertions

Separate obsolete current-target 16 expectations from genuine frozen schema-16
contracts. Historical readers, serializers, migrations, limits, fixtures and hashes
must remain unchanged. Update current-target/future-version and explicit frozen
loader selection as appropriate; do not change historical outputs. Trace the
remaining 139 assertions and downstream UI/diagnostic failures after shared setup
is valid. A rejection reason or null projection is not permission to waive an
economy, deterministic-replay, locality or localization assertion.

## Fresh focused qualification

Campaign `09e4ca62452a4a6ba781debd85766018`, source
`d55139dacc338c08636bf607991c86d1b474297a`: **104 passed, 0 failed, 0 skipped**.
All five supervisor stages passed, including process/evidence completion, exact
settings restoration and final preservation. It is scoped to H1-F4 plus the two
new shared fixture integration cases; it does not clear the 805 historical failures.
Ten preparation-tool unit tests also passed.

Campaign `19fdd2a03134484191251af755dc047d`, source
`57096bebfc930595668dba372ae84b832007a8a8`: **104 passed, 1 failed, 0 skipped**.
The sole failure was the new test's expectation of one total write. The existing
ExactCompleteSaveAtomicPersistence protocol writes a rollback copy and candidate
(lines 47 and 50), so the actual two writes are correct. All prior state assertions
passed. The assertion is now corrected to require those exact two write kinds and
explicit refusal of a nested save, but that correction has not yet run in Unity.
The recorded FAILED result and verified owned process completion remain preserved.
No Unity input changed during that run; fresh post-failure preflight, exact settings
restoration and final preservation passed in its preparation's distinct
post-failure-preflight01/post-failure-restore01/post-failure-final01 directories.
No uncertain reservation recovery was needed for this terminal FAILED run.

No broad/full EditMode, PlayMode, Windows or benchmark workload was started after
the 104-case gate. Those gates remain required after blocking correctness findings
and affected fixture failures are resolved. No owner UAT, merge or H2/O1 work began.
The owner now authorizes an unfinished draft PR at this safe checkpoint, followed
by incremental development and independent reviews; this is not promotion or a
claim that the automated gates passed.

## Artifact ownership

All paths below are managed-worktree-relative, owned by H1-F4, and retained.
No disposable artifact cleanup is authorized by these incomplete investigations.

| Path | Purpose / retention |
| --- | --- |
| TestResults/h1-f4-20261027-fulledit01 | Original input archive, diagnosis, approved recovery, four prior versions; preserve |
| TestResults/qualification-supervisor/61c2a2cd452e4a5c837496834f680e55 | Original INCOMPLETE/UNCERTAIN campaign; preserve permanently |
| TestResults/h1-f4-20261027-fixture01 | Sealed 104-case inputs, isolated identity and restoration maps; preserve |
| TestResults/qualification-supervisor/09e4ca62452a4a6ba781debd85766018 | Passing 104-case campaign and completion evidence; preserve |
| TestResults/h1-f4-20261027-reentrant01 | Sealed 105-case inputs and isolated identity; preserve |
| TestResults/qualification-supervisor/19fdd2a03134484191251af755dc047d | Reentrancy campaign and process/evidence records; preserve |

No new recovery policy approval is required. Automated qualification remains
blocked by the specific unresolved correctness and regression work above.
