# Phase B2 implementation self-review

Reviewed against `af464e05d63acfa73a57786888266d5901feac2f`. Independent review remains required.

| Concern | Result |
| --- | --- |
| Historical contracts | Explicit frozen 13 reader and pinned 12→13 writer; original save-budget resource retained byte-for-byte for schemas 7–13. No schema-13 allocator normalization during migration. |
| Ownership | One eighth canonical owner; nonserialized runtime projection; public scheduling accepts no caller checkpoint/hash. B1 start snapshot appears only inside checkpoint bytes. |
| Transactions | Complete predecessor comparison; exact candidate validation; existing durable replacement/readback/rollback authority; publish only after proven success. Uncertain rollback retains evidence and requires recovery. |
| Ordering | Earliest eligible integer time, numeric sequence and ordinal; existing events including terminal release before one opportunity; newborn events after admission; closure last. No saved rounds/frontiers/counts. |
| Atomic allocation | Existing history sequence reserves once with initial checkpoint; manual outcomes share it. Explicit canonical JSON counter and unambiguous identity shapes precede typed conversion. Checked increment, strict activated history consistency and no recycling. Dormant historical behavior preserved. Scheduler updates only the original counter node; ordinary writes retain history bytes and manual writes preserve its extensions. |
| Bounds | Versioned technical profile; independent base parser budget, owner/checkpoint/aggregate replay budgets and lifetime reservation/preflight. No production structural-capacity or demand formula. |
| Edits/lifecycle | Unfinished horizons block gameplay commits; lifecycle metadata can persist the last committed prefix. Closed-boundary edits retain older starts and affect later captures. |
| Scaling | Higher compatible injected profiles preserve checkpoint bytes. Synthetic scale routes exceed initial checkpoint and envelope limits; no compression/deduplication/journal added. No canonical 25/100-floor snapshot qualification claimed. |
| Scope | No TimeService/Update clock wiring, automatic recurrence, offline processing, Heat formula, progression, settlement or rendering. |

Qualification limitations, failed intermediate runs, final counts, allocation-counter limitations and performance evidence are retained in [qualification.md](qualification.md) and the report archives. Test expectation changes target current schema 14; frozen-schema assertions remain explicit. The room-content preview fixture pauses its existing TimeService before taking an exact-byte baseline, preventing autonomous timestamps from invalidating that assertion.

Final review corrections cover legacy 1–6 owner collisions before an intermediate schema-7 write and shared allocator source normalization/null preservation. The 18 focused cases passed after correcting an invalid trailing-comma test fixture. Frozen legacy preparation and schema 7–13 readers remain unchanged by these coordinator/activation guards.

The continuation also corrected two existing PlayMode screen-input fixture preconditions. The recovered-invalid-intent case inherited a tiny Game View whose viewport lay below the screen; it now explicitly selects 1920×1080. Both affected cases wait for bounded stable viewport/camera geometry before calculating screen coordinates, accounting for Unity's subpixel camera-rectangle rounding. Production UI code and all original input, viewport, recovery and save assertions remain unchanged. The affected rerun passed 2/0/0; the final full PlayMode report records suite-order verification. Earlier failed reports are retained.
