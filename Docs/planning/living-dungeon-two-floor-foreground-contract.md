# Automatic two-floor foreground gameplay

Owner-authorized Stage B baseline: `76f78c5c2d17d3aab916f297aecb5c25cf146d76`, merged PRs #245 and #246. Branch: `codex/living-dungeon-two-floor-foreground`. Recommended configuration: GPT-6.1 Sol High; Complex, specified deterministic execution/persistence integration. GPT-6 Sol High is an explicitly selected availability fallback only; none selected.

## Approved contract

The production `living.foreground.c2.v2` definition is separate from historical `living.foreground.c2.v1`. The new resource owns `FloorTransferUnits = 1000` and the existing `UnitsPerSecond = 1000`. Existing movement, interaction, arrival, economy and combat tuning is retained. Capacity is two parties on distinct captured floors; Floor 1 admission is gated by actual vacancy rather than traffic forecast concurrency.

Format 3 wraps immutable captured C2 start inputs with a separate execution/evidence contract. It reuses the existing iterator, Phase 6 decision and C2 combat kernel. Historical formats 1/2, event numbers 0–12, existing evidence folding and executable behavior remain compatibility authorities. Blocked/released kinds append explicit numbers 13/14.

An original transfer deadline includes the captured transfer duration. If the destination is occupied, a Blocked event consumes one ordinal without consuming the iterator transfer. Waiting has no actionable execution deadline. It retains source-floor occupancy. A verified vacancy transition permits Released at that boundary, consuming another ordinal and still retaining the source. The original transfer then completes at the release time without another duration charge. Only this completed event changes occupied floor identity. Future deadlines start from actual completion time. A single bounded control record retains captured floor identities, decision/block/release ordinals, original deadline, blocker and vacancy causality through terminal execution. No waiting heartbeat, elapsed-time event stream or combat work is generated.

The canonical writer must establish vacancy through an actual pinned terminal predecessor and exact authorized settlement/retirement lineage. Self-consistent checkpoint replay is insufficient. The release permit binds the precise predecessor bytes and released checkpoint. Ordinary candidate preparation must refuse a newly released checkpoint without that authorization. Repeated notifications cannot add another event or change bytes.

Global event order is deadline, numeric RunSequence and local ordinal. At a shared timestamp: existing configured passive tick; existing party events with immediate verified terminal settlement/retirement; eligible vacancy recovery; previously blocked-party release and completed transfer with same-time consequences until stable; admission opportunity and newborn work; closure. An ordinary transfer at its original deadline remains in the existing-event phase and can enter before recovery if an earlier ordered party has just vacated its destination. A previously blocked transfer releases after recovery. Complete timestamps publish atomically under bounded hard event limits.

Existing B2 revision behavior remains. Determinism comparisons may normalize documented partition-dependent revision bookkeeping while comparing identical external inputs and every gameplay-relevant field.

Occupancy derives from authenticated captured floor identities and transfer progress. Editing or changing current activation cannot alter a running party's captured route. Recovery separately requires current active/assigned/vacant eligibility. Terminal occupancy remains protected until verified settlement and retirement.

Each party preserves its original multi-floor monster inputs. Verified net damage is applied once to then-current stable identities: `max(0, currentHP - (capturedInitialHP - verifiedFinalHP))`, with existing readiness/revival, custody, retirement and replacement rules. No additional monster authority is introduced.

Historical v1 runs continue unchanged, with replacement admissions suppressed while draining. Due opportunities consume no new sequence during draining. Future pending opportunity identities/times survive. Once empty and closed, the existing atomic writer automatically transitions to v2 independently of current dungeon eligibility. Dormant saves activate directly under v2.

Writable schema remains 15 only if strict checkpoint and cross-party validation qualify. No JSON owner, party field, sidecar journal or canonical schema migration is authorized. Unsupported new formats fail closed in older applications; application downgrade does not convert v2 parties to v1.

## Qualification obligations

Admission privately checks normal execution and, only at authentic DESCEND, an alternative bounded blocked/released execution using the same engine. Every prefix is exported and reconstructed. Fixed-width control fields and captured bounded floor IDs make checkpoint size independent of elapsed wait. The two additional controls count toward event and replay envelopes. No real blocker is progressed or retired by the probe, and no canonical effects publish.

Required tests cover historical golden compatibility; source-floor occupancy in Waiting/Released; vacancy causality and stale/forged evidence; global ordering and partition equivalence; overlapping snapshots and current-state reconciliation; v1 drain/upgrade; schema and atomic rollback; current editing/lifecycle; selected-floor presentation; real Windows close/reopen; equivalent one/two-floor measured workloads; and preservation.

This record describes the approved contract and implementation target. It is not a passing qualification claim. Qualification results, failures, source applicability, build/native evidence, performance and preservation must be recorded before the implementation PR is ready for independent review. Owner gameplay testing follows resolution of blocking review findings and technical qualification. No broader Living Dungeon, Phase 7 or Phase 8 completion is claimed.
