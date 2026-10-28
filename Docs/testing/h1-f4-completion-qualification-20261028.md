# H1-F4 coordinated completion qualification

Branch `codex/h1-f4-recovery`; draft PR #259. Automated qualification remains
in progress. No owner UAT or merge is authorized by this report.

## Scene clock and draft isolation

Qualified source: `94c8a2d7cbfab6fefd1c5c3f31c97d179a4fae9d`.
Campaign `29c8369fa6d24768aded762b44355beb`: **9 passed, 0 failed, 0 skipped**.
All five supervisor stages passed. Independent completion verification passed:
exact original input inventory, nine pinned tools, recorded host/runner exits,
absent named Job Objects, released reservation, TMP hash, protected owner inputs,
historical full-suite XML and all prior evidence trees.

Local evidence (retained; raw Unity logs are not published):

- `TestResults/qualification-supervisor/29c8369fa6d24768aded762b44355beb/`
- `TestResults/h1-f4-20261027-complete-scene-clock03/plan.json`
- `TestResults/h1-f4-20261027-complete-scene-clock03/independent-final-verification01.json`

The approved Editor-only, instance-local injection supplies precise UTC and
monotonic providers before Start, recovery configuration and native save creation.
The real bootstrap creates one TimeService and binds that same instance to
SaveService. The fixture advances the provider rather than replacing the owner.
Invalid, duplicate, late and non-isolated contexts are rejected. Synthetic bounded
foreground policy bytes are authored and cloned before immutable context binding.
Normal production and development-build clock selection is unchanged.

The fixture previously observed foreground before boot finished. Completing boot
then sampled the platform's unfocused state and left recovery waiting. It now
delivers legitimate pause/focus callbacks at the controlled observation time after
boot initialization. No private recovery state, W, R, A or C is reset.

The original capacity test's historical null Draft did not reproduce. On the first
integrated nine-case run, that test instead failed its unchanged-byte assertion:
the queued foreground publication after QA funding serialized ManaReserve as
`1000.0` rather than `1000`. Substituting only that lexical token in memory
reproduced the exact earlier baseline SHA; gameplay and time fields were unchanged.
The correction completes that real foreground publication before capturing the
baseline. The original exact-byte, wallet, identity, capacity, move and discard
assertions remain intact; no serializer or expected-byte normalization was added.

A new composite reproduction that manually invoked fixture teardown/setup failed
to establish the next save identity. It was replaced with direct selection of the
three original NUnit tests. The passing XML records their order:
RoomContentLootRejectionReasons, RoomContentMapReassignmentCapacityMoveOwnedAndDiscard,
RoomContentMapReassignmentCategoriesInvalidRetryAndSave. NUnit owns each lifecycle.
This establishes bounded ordered coverage, not a fabricated explanation for the
historical null reference. Complete-suite confirmation remains pending.

Other passing leaves cover cached-boundary nonpublication, native fractional time
and owner identity, eligibility rejection, invalid UTC/monotonic fail-closed saves,
and repeated fractional ordinary cuts without duplicate credit.

## Earlier attempts retained

| Campaign | Source | NUnit pass/fail/skip | Recorded verdict |
| --- | --- | --- | --- |
| 571ebad9c28641dd941977b991a33e92 | aed7df93 | 0/9/0 | FAILED |
| 932acba86f4f4f00bc32e5403ea6721a | 7a030fbf | 0/1/0 | FAILED |
| 0bde9dee1ae746b081de0ddb1c1f684a | 6dba9207 | 1/0/0 | FAILED: empty artifact directory |
| ea094b94a3c2467ca198cbcf72cfc0bb | 23743947 | 7/2/0 | FAILED |

All four have guarded normal post-failure restoration receipts under their own
preparation directory's `post-failure-finalize01/receipt.json`. No uncertain
reservation was released or historical verdict changed. The empty artifact issue
was fixed by creating an explicit campaign-owned export manifest, not by relaxing
supervisor artifact validation. Failed scene saves from the later attempts are
preserved in their sealed fixture-inputs artifacts and isolated product namespace.
The first attempt predates that failed-save export improvement; its raw results,
source and input evidence remain retained.

## Remaining gates

Affected scene lifecycle/Research integration, shared immutable configuration and
nullable-state fixtures, current versus historical schema expectations, fresh full
EditMode discovery and qualification, applicable PlayMode, interruption/reopen,
isolated Windows runtime/process exit and matched main-versus-H1-F4 performance
remain pending. These nine passes do not establish a current full-suite failure
count, Windows correctness or a performance improvement.

Schemas 7–16, schema-17 grammar, persistent twelve-component context-v3 digest,
single W and recovery semantics are unchanged by these corrections.
