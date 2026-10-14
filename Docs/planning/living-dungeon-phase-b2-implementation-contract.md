# Living Dungeon Phase B2 implementation contract

Baseline: `af464e05d63acfa73a57786888266d5901feac2f`, merged PR #234. Clean main and fetched origin/main agreed before branch `codex/living-dungeon-phase-b2` was created. The corrected Stage 1 report was retrieved from the task **Lock B2 Persistence Contract**. The owner implementation instruction authorizes schema 14, Profile A and the revised next-enabled-action contract. Recommended configuration: GPT-6.1 Sol, High; Complex, specified cross-system persistence/scheduling implementation. No fallback selected.

## Scope and configuration

This is a durable foundation. Production creates, migrates, loads and writes a dormant owner. Nonempty saves and scheduling use an explicitly injected qualification registry. The default production context does not install that registry. Unsupported profiles or checkpoints are preserved and rejected, rather than discarded. Scheduling has no production caller, clock conversion, TimeService/Update connection, recurring arrivals, offline catch-up, settlement or rendering.

Keep three authorities separate:

1. Future structural potential capacity from accessible floors/structure and approved configuration.
2. Future demand and suitability from attraction, danger, Heat and approved configuration.
3. Current technical storage, parser, replay and processing qualification limits.

No formula for the first two is implemented. The owner scenarios of 25 attractive floors/20 parties and 100 dangerous floors/2–5 powerful parties are future architectural/content qualification requirements, not balance values. Two active parties is the initial technical qualification bound, not a schema or identity ceiling.

The versioned injected profile selects immutable technical limits and qualification admission inputs. Timing and posture belong to this registered definition; advancement cannot replace them on each batch or after restart. Qualification supplies synthetic timing/posture only. A future admission policy requires a new supported contract and approved values. The registry must retain old definitions while pending saves depend on them; changing a definition under the same ID is not an upgrade.

Profile A data is `Assets/_Project/Data/Production/Save/living_dungeon_qualification_profile.json`: two active parties, four retained records, 65,536 checkpoint bytes, 2,048 replay events per party, 8,192 aggregate replay events, a 262,144-byte base envelope and a 357,996-byte living-owner reservation. The complete envelope is 655,360 bytes; raw input/scan work are 1,310,720/2,621,440; serialized nodes/collection records are 9,216/4,352. Depth, object-member, string-byte, raw-array, spatial, tile and unknown-extension limits retain their prior values. Exactly one additional schema-14 primary member is permitted without relaxing nested object limits.

## Canonical ownership and encoding

Schema 14 appends `primary.livingDungeon` after the existing seven owners, in order:

`canonicalSpatialAuthority`, `spatialFloors`, `structuralLifecycleAndOwnership`, `structuralInvestment`, `corridorContent`, `sharedBranchKnowledge`, `sharedFloorKnowledge`, `livingDungeon`.

| Owner fields, in order | Contract |
| --- | --- |
| `FormatVersion` | Integer 1 |
| `Revision` | Nonnegative checked Int64 local semantic revision |
| `Scheduler` | Null for dormant state, otherwise the object below |
| `Parties` | Strict numeric sequence order, bounded unique records |

| Scheduler fields, in order | Contract |
| --- | --- |
| `ProfileId` | Supported immutable injected definition |
| `MinimumOpportunitySpacingUnits` | Positive retained integer input |
| `AcceptedThrough` | Accepted horizon, initially −1 |
| `ClosedThrough` | Completed horizon, initially −1; no greater than accepted |
| `NextOpportunityOrdinal` | Monotonic checked identity, initially 0 |
| `NextOpportunityTime` | Nullable single pending opportunity |
| `LastOpportunityTime` | Nullable consumed-opportunity time for spacing |

Party fields are `Sequence` (positive Int32), `AdmittedAt` (nonnegative Int64), `StartIdentity`, `CheckpointIdentity` (canonical SHA-256), `NextOrdinal` (nonnegative Int64), and `CheckpointChunks`. The two pins and ordinal are owned by the trusted complete save. Public scheduling methods accept no checkpoint bytes or caller-supplied identity hashes.

Binary chunks are 3,072 bytes, yielding 4,096 base64 characters. All except the final chunk have full length; standard alphabet/padding, no whitespace and exact re-encoding are required. B1 format 1 and `run.executable.living-dungeon-b1.v1` are reused unchanged. The retained start exists only inside that checkpoint. No iterator, additional start table, mutable simulator, event journal or party file is serialized.

Active/terminal status is reconstructed from the checkpoint. Terminal outcome consumption and record retirement are prohibited. Terminal transition frees active capacity but retains storage/replay obligations. Unsupported executable identities, formats, contradictory evidence, ordinals or pins fail closed.

## Scheduler and transactions

Derive the earliest party event from `AdmittedAt + NextDueTime()`. At equal time, existing events precede the opportunity, with numeric sequence and local ordinal ordering. Terminal events release active capacity. The opportunity reserves a shared sequence and initial checkpoint atomically, or is consumed without reservation when throttled. Newborn events become eligible after admission. Close the accepted horizon only when no eligible work remains. No rounds, phase frontier, cached next-event identity or persisted party count exists.

Use `AdvanceThrough(relative event due time, 1, expected ordinal)` exclusively for scheduled effects. Passing the complete horizon to a party first would violate B1's nondecreasing deadline contract. An action budget includes opportunity/closure transitions and only changes transaction grouping.

Revision increments once for activation/profile upgrade, opportunity installation, accepted-horizon change, each party event, opportunity consumption (including admission), and closure. Duplicate/no-op requests, rejected/failed writes, ordinary preserved-owner mutations and reopen do not add increments. Split batches retain the same total.

Install opportunities only at an eligible closed boundary, strictly later than closed time. An identical pending request is a no-op; reused/contradictory opportunities fail. An unfinished horizon is resumed, rather than extended or retrospectively injected. Canonical gameplay mutations wait for closure; drafts remain available. Lifecycle timestamp/app-state saves preserve the unfinished prefix.

Every transaction compares complete predecessor bytes, prepares detached state, validates the exact candidate, uses the existing `ExactCompleteSaveAtomicPersistence`, validates durable readback, and publishes a new session/projection only after success. Its existing rollback/evidence/recovery-required contract remains authoritative. Orphan files are never promoted by timestamps. An uncertain rollback cannot be described as guaranteed preservation of disk bytes.

Admission preflight uses a private B1 reconstruction to prove every reachable checkpoint fits the byte/replay bounds before reservation. This is the same engine, with no shared publication or retained second start. It is bounded qualification work, not an acceleration technique. Whole lifetime byte/replay reservations prevent admission from filling ordinary mutation headroom. Full base parsing is independently bounded by the registered profile; validation is not reduced to improve performance.

## Migration and identity compatibility

The frozen schema-13 reader is explicit. The 12→13 upgrade pins schema-13 member serialization and output validation. Frozen schemas 7–13 use the original production budget resource `Assets/_Project/Resources/save_spatial_migration_limits_schema13.json`, copied byte-for-byte from the baseline. Injected historical test limits keep their own contract. Raising current budgets does not change the production historical reader/scanner budget. Spatial schema-13 representation is unchanged.

13→14 validates the source, changes only the schema version and appends `{"FormatVersion":1,"Revision":0,"Scheduler":null,"Parties":[]}`. It preserves existing owners, extensions, lexical values, accepted nulls and allocator/history values. Exact or case-ambiguous owner-name collisions fail without overwriting the source. Native creation uses the same dormant owner.

The current load coordinator also rejects a new-owner extension collision in legacy schemas 1–6 before the historical schema-7 transaction can write. Frozen raw recognition and historical preparation remain unchanged; wrapped 1–6 and unwrapped schema-1 collision tests prove classification remains valid and no migration write occurs. Frozen canonical 7–13 upgrade chains prepare the current candidate before replacement and reject the same collision without an intermediate write.

`runHistory.NextRunSequence` is the only allocator. An automatic admission increments it with the initial checkpoint; a completed manual run increments it with its outcome. Reservations do not allocate again at terminal transition. Activation requires positive, available allocation and consistent visible history/reservations; exhaustion, duplicates and contradictions fail. Bounded history cannot prove all pruned lifetime identities, so lineage still depends on the established allocator.

When recent history is populated, activated allocation requires a non-null latest outcome with the final recent record's canonical RunId and full serialized outcome data. An absent, earlier or conflicting alias is invalid, including during current active-save validation. This guard does not apply retroactively to dormant history or frozen schemas. Empty-history latest representations retain the compatibility behavior below.

Activation and active validation require an explicit canonical integer counter in the original history JSON, unambiguous known property names and properly shaped visible identities; Unity typed conversion is not allocation authority. Missing, exponent/fractional or ambiguous counters remain dormant-compatible but cannot enable shared allocation. Scheduler writes patch only the counter in the original history node. Ordinary active writes verify unchanged known history semantics and retain its original bytes; manual writes replace the known allocator/outcome members while preserving history extensions. Explicit historical nulls and lexical extensions therefore survive activation, admission and ordinary writes.

Dormant historical saves retain the previous manual `Math.Max(1, ...)` behavior. The exact default `LatestOutcome` record that Unity historically serializes for an empty class slot, with empty recent history, is interpreted as absence without changing its bytes. Populated blank identities are rejected. Generic active-owner saves cannot reset/renumber allocation; manual and scheduler allocation use explicit writer paths.

Compatible larger profiles require explicit larger parser/complete-save envelopes and preserved supported definitions. The profile switch changes one semantic revision and retains all existing checkpoint bytes. IDs, owner fields and ordering are independent of the initial count two.

## Qualification and remaining dependencies

Evidence is under `Docs/testing/evidence/living-dungeon-phase-b2`. Qualification includes complete-save batching/restart, canonical preservation, boundaries, lifecycle, Windows atomic persistence, frozen migrations, Phase A/B1 regressions, build gates and a distinct preserving Windows output.

Synthetic scale models use the real B1 compatibility-route engine with one spatial room per tagged floor; they do not create production floors or canonical 25/100-floor snapshots. Current canonical floor/content qualification and executable support remain separate constraints. Storage estimates include base64/chunk growth; measured timings and workload characteristics are recorded without claiming mobile readiness or balance.

Future work requires approved structural-capacity/demand mappings, production timing, supported content/floor/executable expansion, retained-terminal settlement/retirement, mixed-period Heat attribution and individually eligible economic outcomes, offline catch-up and presentation. No compression, deduplication, replacement simulator or new journal is introduced. Large-scale performance/storage must be independently approved and qualified before activation.
