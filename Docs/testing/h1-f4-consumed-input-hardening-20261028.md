# H1-F4 consumed-input hardening — 2026-10-28

PR #259 remains draft on `codex/h1-f4-recovery`, base main
`3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Model recommendation: GPT-6.1 Sol
High, Complex; availability fallback GPT-6 Sol High. No merge or owner UAT.

The remaining economy gate is already qualified at `9bc0fd0c`: original nine
plus three adjacent negatives 12/0/0, and the complete affected cohort 236/0/0.
Both completed all five stages and independent preservation verification. See
[the complete economy report](h1-f4-economy-nine-correction-20261028.md).

## Confirmed runtime defect and correction

The subsequent bounded floor run at `b1eb96a5` completed 2/4/0. Source tracing
identified reloads of the consumed loot owner, an unbound reflection-installed
SaveService and an obsolete legacy-event assertion on a current C2 run. A
separate deterministic writer reproduction was required before assuming all
floor problems were fixtures.

At `1a2fa9a8`, campaign `337bb7e8b7f2435887508649cd0c2fa6` reproduced canonical
publication after mutation of the already-bound success threshold or loot world
value. Both had `digestUnchanged=True, published=True`; reference equality did
not prove immutable values. This establishes a writer guard defect. It does not
claim a reproduced duplicated reward or changed wallet balance.

Commit `66edd5b2` checks retained parsed values against representations derived
immediately from the validated input bundle. Explicit CombatC0 checking covers
its intentional exclusion from the frozen legacy configuration serializer.
Bootstrap, loot, bundle occupancy and contextual occupancy are also checked.
Current-schema contextual validation rejects mutation; recovery entry rejects it
before capturing R. Existing stable rejection reasons are retained. No repair,
reload, automatic context adoption or competing writable authority is introduced.

These representations are process-local mutation guards. The persistent digest
still binds the original twelve exact validated byte arrays. Schemas 7–16,
migrations, schema-17 owner fields/590-byte envelope, W/R/A/C/S/Q semantics,
wallet, configured tuning and localization are unchanged. Additional validation
serialization cost must be included in the mandatory matched runtime assessment;
test duration provides no performance evidence.

## Exact qualification

| Campaign | Source | Pass/fail/skip | Outcome |
| --- | --- | --- | --- |
| 337bb7e8b7f2435887508649cd0c2fa6 | 1a2fa9a8 | 0/2/0 | Original reproduced defect; FAILED preserved |
| 7396222b4b2c40c68702ea7c414f4d9f | 66edd5b2 | 0/2/0 | Initial reproductions reject; new phase checks mutated the fixture's separate bundle, not SaveService's owner; FAILED preserved |
| 20f9768641d04d10a4546df3f379586a | 829ef0d5 | 2/0/0 | All five stages and independent verification PASSED |
| fd0f9ad5ca444761871bbdc37ccb6f56 | 829ef0d5 | 234/0/0 | All five stages and independent verification PASSED |

Commit `829ef0d5` corrects that test ownership error without changing runtime.
The two cases include closed/originalPending/residualPending saves, existing and
new writers, Goblin HP, tick cadence, Research progression, loot and both
occupancy references. They require no writes, unchanged canonical bytes/W/R,
rejection before new R capture, and validity after restoring the test DTO value.
The 234-case cohort includes those two, the full 111-case H1-F4 cohort, 60
time-provenance and 63 C2 tests. Counts overlap and must not be added.

Both failed campaigns completed native/helper/Job exit, kept qualified inputs
unchanged and underwent normal guarded restoration after their reservations had
already been released. Their campaign bytes/verdicts and all prior evidence
remain unchanged. Both passing campaigns independently verified recorded process
identities, absent completed Jobs, original complete inputs/font, pinned tools,
protected owner/save/historical evidence and exact released reservation owner.
The JSON companion contains full source/tool identities, input/archive hashes,
original failure stacks, stage records and preservation receipts.

## Remaining work

Floor fixture corrections are proceeding under the existing approved context
contract. The historical null-Draft exception remains historically observed but
unreproduced; this finding does not invent a cause for it. Full current EditMode,
applicable PlayMode, final interruption/reopen, isolated Windows runtime/exit and
matched main-versus-H1-F4 performance remain incomplete. No final readiness or
performance claim is made.
