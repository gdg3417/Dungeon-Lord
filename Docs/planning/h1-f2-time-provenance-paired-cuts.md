# H1-F2 time provenance and paired cuts

Implementation candidate for independent review, 2026-10-25. Baseline main: `c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d` (#255), following Research durability #254. Schema 16 and its fields are technical choices subject to this PR's review. This packet does not complete H1, H2, O1 or Phase 7.

## Approved policy versus implementation

These eight owner-approved directions supersede contrary proposed wording in the older hybrid proposal. Dated evidence remains historical.

| Policy | Approved direction | H1-F2 status |
| --- | --- | --- |
| D1 | Fractional precision time accounting | Checked integer 100 ns representation and paired capture foundation; fractional settlement deferred. |
| D2 | Preserve qualified history without artificial support for disposable fixtures | Frozen 7–15 readers/transforms retained; explicit contextual 15→16 route. Missing historical time is refused. |
| D3 | Eligible foreground time accrues during ordinary successful saves | Full captured cuts bound candidate work; sampler retains storage elapsed time for subsequent processing. |
| D4 | Original grant plus one grouped residual grant during unfinished recovery | Deferred; existing whole-second passive resolver remains the sole grant authority. |
| D5 | Drain accepted work, original grant/recovery, residual grant/recovery, then ordinary actions | Recovery sequence deferred; existing action barriers retained. |
| D6 | Preserve confirmed work; reject ambiguous clock credit | Existing exact persistence authority retained; invalid/backward paired observations fail closed. Full recovery deferred. |
| H1-C01-A | End an episode atomically on durable residual settlement | Policy only; no episode runtime. |
| H1-C02-A | Normal accrual continues; execution/memory bounded; overload declared prospectively using measured configuration thresholds | Policy only; no thresholds, suspension or overload UI. |

## Schema and authority

Schema 16 appends the tenth canonical owner after `primary.monsterContinuity`:

```json
"timeProvenance":{"FormatVersion":1,"Mode":"legacyWholeSeconds","AccountedFraction100ns":0}
```

`lastSavedUtcUnix` remains the **only seconds component** of the compound accounted/passive boundary. The owner supplies the fraction and interpretation, not another timestamp or independently writable W. Seconds must be positive canonical decimal integers through `253402300799`. The fraction is exactly integer zero in legacy mode. Missing/duplicate/reordered/extended owners, case aliases, unsupported formats/modes, floating/exponent values and future schemas fail closed. No writable downgrade exists.

Immutable `UnixTimePoint` uses seconds and a fraction in `[0,9999999]`, checked integer arithmetic and explicit negative-remainder borrow. Its DateTimeOffset representation range is `-62135596800` through `253402300799` seconds; gameplay persistence uses the positive legacy subset. Representation precision does not establish clock accuracy, trust or server verification.

All current writers share `DetachedCanonicalSaveSession.PrepareReplacement`. It validates changed seconds, rejects backward movement and emits the same legacy zero fraction. Ordinary Save, Research Start/Claim, manual runs, spatial edits, QA mana, foreground stages and passive publication therefore cannot leave a stale fractional owner. Native creation uses the same mode. Runtime provenance is immutable derived state excluded from recognized writable snapshots. No Research or monster authority is added.

Target workload reserve follows measured grammar overhead: the appended member is 92 UTF-8 bytes, four parsed nodes, four collection records and 71 string characters. Current serialized/candidate ceilings become 1,376,348 bytes, parsed nodes 10,244, collection records 5,380 and string characters 1,376,327. Existing raw-to-serialized and scan-to-raw ratios remain two, giving raw bytes 2,752,696 and scan work 5,505,392. Spatial, copied-value and unknown-extension budgets remain unchanged. The dedicated new owner is excluded from the historical schema-13 base-size charge, just as monster continuity already is. Frozen-15 resources and selection retain their exact old budgets. These are schema grammar reserves, not gameplay tuning, overload thresholds or a larger execution envelope.

## Migration and activation

Frozen-15 contextual validation precedes transformation. Only the root version changes and the owner is appended. Historical fields, compatible root/primary extensions, ordering, IDs, allocation, ordinals and checkpoint bytes remain exact. Dormant and active v1/v2 A>C owners retain their obligations. Config, compatible content, occupancy, active contract and supported executable/profile remain mandatory. New owner-name collisions, including aliases, are refused. No historical fraction is reconstructed.

Existing 7→15 transforms remain separate, including pinned byte-identical 14→15 outputs and their existing active-source rejection. Their outputs then enter 15→16. Historical validation grants no writable session. Pinned fixtures omitting lastSavedUtcUnix keep historical acceptance but cannot establish the new boundary; separately identified timestamp-bearing test variants do not edit the corpus. Production never invents a missing boundary.

The coordinator validates/opens a detached candidate, then uses existing exact atomic persistence, rollback and durable readback. Failure preserves the proven predecessor or requires independent recovery. Future schemas cannot fall through to legacy projection. Source bytes remain exact rollback evidence.

`legacyWholeSeconds` is an explicit activation fence: zero is historical whole-second interpretation, not a reconstructed precise observation. Fractional observations neither advance a fine boundary nor award mana. A future reviewed transition requires precision-aware passive resolution and classification before nonzero accounted fractions become writable.

## Paired observation cuts

At the GameRoot coordinator boundary, TimeService captures an immutable exact predecessor SHA-256, eligible logical horizon, predecessor accepted/closed horizons, fractional wall endpoint and eligible/suspended context. Captured work is provisional; acceptance/closure remain scheduler authorities. UTC does not advance execution.

SaveService receives the entire observation, verifies predecessor/horizons and uses captured wall seconds for newly accepted work. It does not resample UTC after serialization. There is no public subdivision/interpolation API. Already accepted unfinished work retains its boundary; existing suspended pause caps remain. Each later publication needs a newly bound cut; newly elapsed storage time remains for subsequent processing. Complete equal-time ordering, bounded pumps and explicit failure retry remain.

Ordinary/action saves retain legacy whole-second save-time behavior. This does not claim complete H1 coverage/classification for all saves. Focus/pause/resume still exclude background logical credit. No offline party catch-up, scheduler or independent elapsed ledger is added.

## Remaining dependencies

One wallet, capacity, efficiency, tick mana, monster healing/recovery frequency, party identity and Research acknowledgement remain unchanged. Precision itself awards no extra mana; no gameplay coefficient changes.

Remaining H1: precision-aware settlement/classification, original/residual grants, stable recovery entry, episode closure, crash-cut qualification and measured configuration-owned overload. H2 retention/batching and O1 parties remain later work. New writable bytes require isolated Windows save/close/reopen qualification before merge approval. Actual commands, source manifests, results and limitations are recorded in the [qualification packet](../testing/h1-f2-time-provenance-qualification.md). Automated results do not replace independent review.
