# H1-F4 floor, draft and custody qualification — 2026-10-28

Branch `codex/h1-f4-recovery`, draft PR #259, base main
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Recommended GPT-6.1 Sol High,
Complex; availability fallback GPT-6 Sol High. No merge or owner UAT.

## Corrections

The first six-case reproduction at `b1eb96a5` completed 2/4/0. Two GameRoot
activation cases reloaded loot and installed an unbound SaveService by
reflection; the reconfiguration case likewise reloaded loot and expected a
post-binding reset to acquire authority. The current C2 manual-run case expected
a B1 EncounterEvents entry. Commit `4c86f251` uses the existing prebound service
factory and consumed loot, authors two-floor configuration before ownership,
rejects post-binding reconfiguration, and verifies the positive path through a
fresh same-context load. Legacy B1 assertions remain in explicit read-only
execution; the current C2 path additionally checks health at the actual transfer
boundary and exact final party health. The corrected six cases passed at
`3031027b`, including all four original failures.

Commit `a51be8c9` moves the other floor fixture configuration edits before
ownership. Shared root callers select production-compatible resource profiles
before constructing their synthetic owner; explicit small-budget negatives keep
their injected profiles. A test-only factory overload receives a precise UTC and
monotonic provider before configuring recovery/loading. Offline floor tests now
execute actual entry, original settlement and residual closure rather than
editing lastSavedUtcUnix or invoking an unproven legacy interval writer. The
two-floor rate, wallet, activation/deactivation and transient-replay/reopen
assertions remain. Actual C2 CombatEvents/CombatMonsters are retained across
same-session publication; legacy transient assertions remain covered separately.

The broader 287-case run at that source completed 275/12/0. Commit `cd298ab2`
corrects the exact first invalid fixture conditions:

| Cases | Verified cause | Correction |
| --- | --- | --- |
| Eight EveryAuthoredReusableOptionMovesExactIdentityWithoutOtherStateChanges cases | A JsonUtility SaveData clone materializes null Research state as an empty object; the byte comparison then reports researchPending `{}` versus the correct persisted `null`. | Capture the actual pre-action recognized-state snapshot directly. Compare every recognized field, structural runtime, run history, wallet, stable identity, sequence and investment without an unsafe clone. |
| DurableWriteReopensSchemaNineWithoutManaOrInvestmentEffectsOrReturnedCopy | Current writer was asserted equal to the schema-16 introduction constant. | Require the approved schema-17 introduction and exact current serialized version. Add an independent positive frozen schema-16 read; retain identity, wallet, ledger and duplicate-redeployment rejection assertions. |
| CanonicalRoomTargetCyclesThroughLargeChamberAndTargetsItsStableIdentity | A synthetic schema-17 payload is inspected with an unbound current validation context. | Use the existing complete synthetic-current context factory; retain all room-cycle and stable-target placement assertions. No historical producer proof is fabricated. |
| IncompleteConstructedFloorShowsExactReadableBlockerThroughGameRoot | This separate root fixture still selected obsolete limits incompatible with production foreground profiles. | Author compatible production resource limits before owner creation. Retain exact readable blocker and unchanged canonical-byte assertions. |
| ConstructionSaveRechecksConfiguredPricesAndInsufficientManaPreservesEvidence | Replacing the consumed economy after draft creation correctly fails authority validation before reaching the expected affordability guard. | Author cost333 into validated synthetic input bytes before ownership. Explicitly reject foreign cost334 without writes/draft loss, then retain cost333 insufficient-balance rejection and exact successful debit. Reserve derives from the actual bound monster rules. |

These are fixture/expectation corrections. No further production defect was
established in this cohort. The separately reproduced consumed-input runtime
guard correction remains covered by its passing 234-case gate; see
[its source-pinned report](h1-f4-consumed-input-hardening-20261028.md).

## Qualification and preservation

| Campaign | Source | Pass/fail/skip | Final outcome |
| --- | --- | --- | --- |
| 21c9223e25c94999a3aeb44e181425c5 | b1eb96a5 | 2/4/0 | FAILED retained; normal guarded restoration completed |
| 22edf73196f4476fb38a931aae228ec4 | 3031027b | 6/0/0 | All five stages and independent verification PASSED |
| d64eb88e23234731803d28ef19f34da2 | a51be8c9 | 275/12/0 | FAILED retained; normal guarded restoration completed |
| d95f78dc9642463db75245c70412095a | cd298ab2 | 12/0/0 | All five stages and independent verification PASSED |
| 66bb380669ae46bebb9782feff8e8c95 | cd298ab2 | 287/0/0 | All five stages and independent verification PASSED |

The complete cohort comprises PhaseSixA4 68, PhaseSixA2FloorConstruction 63,
PhaseFourBootstrap 28, PhaseFourRedeployment 23, PhaseFourUnassignment 31,
PhaseSevenFloorLifecycleTests 21 and TransactionalRoomConstruction 53. Its exact
287 NUnit names match retained discovery; no identity was skipped or removed.
The focused results overlap the 287 and are not added as independent coverage.

Independent checks verified source/tools, recorded native/helper/Job completion,
exact original full input inventory and TMP font, original three settings,
released reservation owner, protected owner/save/build/historical evidence and
every prior preparation/campaign directory. Normal finalization of FAILED runs
required completed process/Job evidence, unchanged qualified inputs and an
already released reservation; neither campaign/verdict nor reservation was
altered. No exceptional recovery was needed. The JSON companion retains full
source/tool/input/archive identities, all failures/stacks, exact test lists and
complete receipts/stage records. Raw Unity logs remain preserved locally.

## Implications and remaining gates

Production tuning, localization, canonical ordering, stable identities, frozen
schemas7–16, schema17, the twelve-component context, W/R/A/C/S/Q and atomic
settlement are unchanged by these fixture corrections. Synthetic cost and clock
values are test inputs authored before binding. No automatic rebinding, new
wallet/clock/save authority, journal, migration or unsupported historical writer
was introduced.

The historical scene null-Draft exception is not explained by these findings.
It remains historically observed but unreproduced on the qualified ordered
scene cases; final full-suite evidence is still required. The original
1966/805/1 full attempt remains INCOMPLETE / UNCERTAIN. These affected cohorts do
not establish a new full-suite failure count.

Remaining shared historical/current fixtures and the development profiling
clock ownership must be resolved before full qualification. Fresh full
EditMode, applicable full PlayMode, final interruption/reopen, Windows Player
startup/gameplay/reopen/complete exit and matched main-versus-H1-F4 performance
remain pending. No responsiveness or memory improvement is claimed. PR259 stays
draft and automated completion work continues; owner UAT is not authorized.
