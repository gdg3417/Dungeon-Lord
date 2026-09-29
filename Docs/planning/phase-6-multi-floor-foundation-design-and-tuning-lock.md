# Phase 6A0: Multi-floor foundation design and tuning lock

| Field | Authority |
| --- | --- |
| Phase | Phase 6A0 |
| Status | Owner-approved Phase 6 implementation prerequisite |
| Prepared against | merged PR #213; `main` `7e94dee13ab0c4252531cf152241426065a9db75` |
| Scope | Documentation authority only; no runtime implementation |
| Current writable schema | 10; schema 11 is approved future direction only |

This document closes the Phase 6 design and initial-tuning gate for Additional Floor Foundation. It does not implement Floor 2, schema 11, migration, UI, or Phase 6 runtime behavior. Numeric values below are initial **configuration/content-owned tuning seeds**. They must not be copied into runtime constants or frozen as mutable save values. Phase 5 Decision 30 v2 is unchanged.

## 1. Configuration, save, and schema ownership

Configuration/content owns bounds, capacities, construction prices, decision formulas, weights, thresholds, normalization references, allowances, workload limits, and other tunable values. Durable save state owns stable floor identity, `FloorDefinition` reference, layout, fixed structures, content assignments/custody, structural investment, stable allocation/lifecycle state, and other approved durable player progress. It owns one per-floor activation state only when schema 11 is deliberately implemented; that state will be the sole writable activation authority, never duplicated by a writable active-floor-ID list.

Do not save derived/current tuning values merely to freeze gameplay behavior. A later schema-11 implementation must preserve the readable schema-10 compatibility contract and apply its deliberate migration behavior. This document neither changes schema 10 nor authorizes schema 11.

## 2. Multi-floor run continuity and settlement

A descending party retains its `RunId`. Surviving member identities, exact HP, capabilities, behavioral profiles, and intelligence carry unchanged; dead members remain dead for the run. MVP has no replacement, refill, resurrection, between-floor healing, tactical reorganization, stairs encounter, transition damage, loot, fatigue, mana charge, or other transition effect. Death closes formation using the existing deterministic ordering.

Per-floor counters reset where appropriate; complete-run counters accumulate. Original-party behavioral aggregation and minimum-survivability ownership remain based on original members. Live HP, active-member fraction, survivors, formation, and living-member capabilities remain dynamic.

One run starts at Floor 1 and ends only at exit, retreat, or wipe. Loot remains carried and at risk until termination. Survivor extraction happens once at `final survivors / original run party size`; retreat uses that ratio, wipe extracts nothing, and MVP adds no durable dropped-loot pile. Heat aggregates across the complete run and resolves once at externally knowable termination; floor transitions do not publish mid-run Heat. Phase 6 adds no run-event mana. Only active contiguous floors contribute active-floor passive mana; constructed inactive floors contribute zero.

## 3. Terminal exit-versus-descend decision

Resolve the current floor completely first. The decision occurs only after survivors reach its Completion Terminal. A wipe has no decision. If no valid, unlocked, active next floor exists, the result is EXIT.

Inputs are live party condition, survivor fraction, carried loot, perceived next-floor reward/danger, uncertainty, applicable behavioral-profile dimensions, and run depth/objective. The decision consumes snapshotted party knowledge, not secret live dungeon contents. Unknown information remains uncertainty, not safety. Intelligence affects interpretation; player and adventurer knowledge are distinct.

Survivability refusal precedes desirability, and reward/objective cannot bypass it. Carried loot supplies exit/preservation pressure; expected additional reward is a separate descent pull. Clearly unfavorable choices EXIT deterministically; clearly favorable choices DESCEND deterministically; only the strict marginal interval uses deterministic seeded variation.

### Marginal deterministic identity

The stable rule source is `run.floor_transition_decision.phase6.v1`. Ordered identity fields are `RuleSourceId`, `RunId`, `CurrentFloorInstanceId`, and `NextFloorInstanceId`. Encode each UTF-8 field with an unsigned 32-bit big-endian byte-length prefix; concatenate; SHA-256; read the first four digest bytes as an unsigned 32-bit big-endian word; divide by `4294967296.0`; roll domain is `[0,1)`. DESCEND only when `roll < DescendLikelihood`; equality is EXIT.

Health, loot, personality, knowledge, clock, collection order, Unity RNG, runtime hash, or call-order-dependent PRNG state may affect likelihood but not roll identity. Floor-transition and branch marginal hashes remain independent.

Impossible, corrupt, unsupported, stale, concurrent, workload-exceeded, or internally invalid candidate state is technical failure, not adventurer EXIT, retreat, wipe, or success. Failure is atomic: no Heat, loot, death, learning, run history, or other gameplay consequence publishes; no fabricated player-visible adventurer rationale or tight automatic retry loop.

## 4. Survivability, appeal, and perception tuning

All values in this section are initial configuration-owned tuning.

```
Condition = 0.65 * AverageSurvivingMemberHealthFraction + 0.35 * SurvivorFraction
Threat = 0.70 * PerceivedNextFloorDanger + 0.30 * Uncertainty
ExpectedSurvivability = clamp(Condition - 0.60 * Threat, 0, 1)
```

Trap expertise is not a generic complete-floor survivability modifier. The party minimum is the arithmetic mean of original members' configured floor-descent minima:

| Profile | Minimum |
| --- | ---: |
| cautious | 0.75 |
| greedy | 0.50 |
| curious | 0.55 |
| goal-oriented | 0.65 |
| gambler | 0.40 |

`ExpectedSurvivability < PartyMinimumSurvivability` means EXIT; equality proceeds to appeal. This is separate from Phase 5 branch-minimum configuration. Configuration validation requires every floor-descent minimum to be finite and in the formula-supported domain below `1.0`; no fallback runtime denominator is authorized.

```
RewardTerm = PerceivedAdditionalReward * RewardAppetite
SurvivabilityMarginTerm = clamp((ExpectedSurvivability - PartyMinimumSurvivability) / (1 - PartyMinimumSurvivability), 0, 1)
ObjectiveTerm = CurrentDepthObjectivePull
DangerTerm = PerceivedNextFloorDanger * (1 - RiskTolerance)
UncertaintyTerm = Uncertainty * (1 - UncertaintyTolerance)
CarriedLootTerm = CarriedLootPressure * (1 - RiskTolerance)
TotalAppealWeight = RewardWeight + SurvivabilityMarginWeight + ObjectiveWeight + DangerWeight + UncertaintyWeight + CarriedLootPreservationWeight
DescentAppeal = clamp((RewardWeight * RewardTerm + SurvivabilityMarginWeight * SurvivabilityMarginTerm + ObjectiveWeight * ObjectiveTerm - DangerWeight * DangerTerm - UncertaintyWeight * UncertaintyTerm - CarriedLootPreservationWeight * CarriedLootTerm) / TotalAppealWeight, -1, +1)
```

Initial weights are reward 1.00, survivability margin 1.25, depth/objective 1.00, danger 1.00, uncertainty 0.75, and carried-loot preservation 0.75; this initial configuration produces `TotalAppealWeight = 5.75`. Runtime derives the total from configuration. Validation requires each configured weight to be finite and nonnegative, and TotalAppealWeight to be finite and greater than zero. `ExitThreshold = -0.10`; `DescendThreshold = +0.10`. At/below ExitThreshold exits; at/above DescendThreshold descends; strictly between is marginal:

```
DescendLikelihood = (DescentAppeal - ExitThreshold) / (DescendThreshold - ExitThreshold)
```

`PerceivedAdditionalReward` is the sole canonical term for normalized additional next-floor reward. “Perceived next-floor reward” is descriptive shorthand only and does not define another authority. The configured references are RewardReference = 12 LootBonus units, DangerReference = 9 Danger units, and CarriedLootReference = 12 world-value units:

```
PerceivedAdditionalReward = clamp(PerceivedRewardScore / RewardReference, 0, 1)
PerceivedNextFloorDanger = clamp(PerceivedDangerScore / DangerReference, 0, 1)
CarriedLootPressure = clamp(CurrentCarriedLootWorldValue / CarriedLootReference, 0, 1)
```

Validation requires every reference value to be finite and positive. Unknown reward/danger must not query hidden Floor state.

## 5. Objectives and floor knowledge

At run formation, MVP deterministically assigns a transient party-level depth objective from stable run identity and an explicit configuration-owned rule source; it must not reuse branch/transition marginal rolls.

| Mode | Weight | Behavior |
| --- | ---: | --- |
| shallow | 0.20 | no deeper pull |
| target depth | 0.50 | targets Floor 2 in the Phase 6 two-floor game until reached, then resolves |
| deepest reasonable | 0.30 | pulls deeper while another active floor exists, subject to survivability |

Resolved or impossible objectives provide no pull. Future quests, activities, bosses, rescue goals, and item targets may influence formation without replacing the transition system.

Phase 6 approves shared floor-level knowledge, separate from branch knowledge, representing stable floor identity, topology/applicability fingerprint or equivalent, reward/danger known flags and perceptions, shared confidence, and last-confirmed run identity or equivalent evidence. Compatible survivor reports may create/reconfirm/update it. A wipe must not generate precise detailed knowledge from internal simulation; current coarse outside-world wipe/death evidence continues under its own authority. Material topology/content changes invalidate stale applicability.

Initial tuning: first reliable survivor observation confidence `0.75`; reconfirmation increase `0.125`; confidence clamp `1.0`; unknown-information uncertainty `1.0`. No passive confidence decay is required unless separately approved.

## 6. Floor 2 permission, construction, and lifecycle

`ac_100` remains the sole Floor 2 permission gate. Completion grants permission only, never automatic construction. Floor 2 is explicitly constructed. Initial floor-shell cost is 450 mana; activation/deactivation cost zero. Rooms, corridors, and contents retain their existing separate economic authorities.

Successful construction creates stable `FloorInstanceId`, a persistent inactive Floor 2 shell, and required Entrance/Completion fixed structures; rooms, corridors, and contents are built separately. An explicit configuration/content-owned floor-construction profile supplies anchors/orientations. Exact Floor 2 coordinates are content-authoring details, not a new gameplay-owner decision, provided they fit approved bounds, preserve useful construction space, and validate deterministically. Exactly one valid authored shell placement must resolve or construction fails atomically. Runtime must not hardcode coordinates or reuse a migration/starter profile with different lifecycle ownership.

Initial Floor 2 tuning is a 14 x 14 legal rectangular boundary and base `FinalFloorSpaceCapacity` 80. Both are configuration/content-owned, not mutable saved tuning. Floor index, including Floor 2, does not automatically increase monster level, encounter strength, damage, or general difficulty; difficulty arises only from approved authored/configured content and systems, never a hidden floor-number multiplier. Existing eligible MVP content is reusable; no Floor-2-exclusive content is required. Authored optional-branch allowance is 1; effective allowance is 0 before `ac_300`, 1 after.

Construction-valid requires stable valid IDs, legal bounds, no invalid overlaps, valid custody/ownership, category capacities, coherent records, canonical ordering, and no corrupt references; it does not require a full route. Activation-valid additionally requires research permission, constructed instance, one Entrance, one Completion Terminal, at least one buildable required-route room, a complete valid same-floor Entrance → required-route room(s) → Completion route, and normal active spatial invariants. Monsters, traps, and loot are not mandatory.

Floor 1 is always active. A constructed inactive floor remains persisted and editable, is inaccessible to adventurer runs, contributes zero active-floor passive mana, and retains its layout, content, investment, and IDs; active edits affect subsequent runs. Inactivity never deletes, resets, refunds, relocates, or destroys investment/IDs; whole-floor demolition/deletion/refund is not MVP behavior. Active floors form a contiguous prefix from Floor 1. Deactivating Floor N atomically deactivates N and deeper floors; manual activation of N does not reactivate cascaded floors. Activate All Eligible atomically activates the longest valid contiguous prefix, stops at the first blocker, leaves it/deeper floors inactive, and returns a stable localized reason. Lifecycle changes affect future runs only.

## 7. Spatial, persistence, determinism, and concurrency

Each floor is an independent same-floor route graph. Completion Terminal → next Entrance is a run-transition semantic, never a cross-floor corridor, graph edge, serialized pathfinding edge, or reverse route. There is no floor-to-floor backtracking; retreat from Floor 2+ ends the run directly.

Complete relevant active-floor state is snapshotted at run start under INV-08. A run cannot query mutable layout/activation afterward; edits/lifecycle changes affect later runs. Phase 6 persists no between-floor checkpoint or ordinary-party in-progress HP; it publishes gameplay once at complete termination. Lifecycle writes use the established detached-candidate pattern: validate candidate, persist, exact durable readback, then publish live state. Construction, activation, cascade, and Activate All are each atomic.

Future schema 11 gives each floor one explicit activation state as sole writable activation authority. Schema 10 → 11 maps existing saved floors active, never manufactures Floor 2, and fails invalid/corrupt saves under normal policy. It is not implemented here; current writable schema remains 10.

Canonical ordering uses stable floor identity/index, never dictionary/UI order. Rooms, edges, assignments, party members, encounters, and decisions retain canonical ordering. Same snapshot plus RunId must reproduce outcomes; no wall clock, Unity RNG, collection order, runtime hash, or shared/call-order PRNG. Validate configuration-owned per-floor and complete-run workload limits before simulation. Failure aborts atomically. MVP allows at most five active floors and four inter-floor decisions; these are scope boundaries, not permanent post-MVP architectural ceilings. Complete-run bounds must ultimately be sized from legitimate maximum authored Floors 1–5/content capacity; do not invent an arbitrary encounter ceiling before remaining definitions exist.

Runs are party-scoped and may resolve serially in MVP, but run-local state is scoped by RunId and no authority assumes a single party forever. Separate runs do not block/join/race for finite loot/share transient encounters/rescue/congest/mutate another snapshot. Completion may update shared Heat/knowledge for later runs. Concurrent scheduling is not implemented.

## 8. Reporting, qualification, and exclusions

Exit/descend resolution produces structured deterministic evidence and stable reason codes sufficient for tests, reproducibility/debugging, aggregate causal reporting, and localized player explanation. Technical failure remains distinguishable from adventurer EXIT, RETREAT, WIPE, and SUCCESS and never fabricates gameplay reasoning. Normal player reporting is aggregate: floor reach, deeper continuation, exit, retreat, wipe, attrition, loot, and common causal reasons. Presentation distinguishes perception from reality; raw hashes/rolls are diagnostic evidence, not normal UI, and per-party thought bubbles are not required. Lifecycle presentation distinguishes Locked, Unlocked / Not Constructed, Inactive, and Active. Player-facing text remains localization-owned. Bootstrap/development UI may qualify behavior; Phase 7 editor work is not authorized.

Future qualification covers schema/migration when introduced; construction and validity; lifecycle; active-floor mana; snapshot stability; exact HP/member transfer; terminal resolution; deterministic marginal decisions; knowledge; extraction; Heat; atomic failure; stale/concurrent persistence; workloads; reopen; ordering; localization; and invalid-state tests. Manual qualification compares aggregate patterns across deterministic RunIds for dangerous Floor 1/easier Floor 2, easier Floor 1/dangerous Floor 2, loot-heavy Floor 1, a deeper-target objective, meaningful player floor-design changes, and Floor 2 inactive versus active for newly starting runs. The pass criterion is not every party choosing an expected answer: outcomes must be deterministic/reproducible from approved inputs, causes understandable, and meaningful player-controlled dungeon changes able to produce understandable aggregate behavior changes.

This authority does not authorize schema 11, migration, Floor 2 runtime construction/gameplay, editor, Floors 3–5, exclusive content, new monsters/bosses, research UI, run-event mana, durable Core Level, persistent expeditions, concurrent scheduling, party interaction, occupancy, finite shared encounters, cross-floor edges/backtracking, stair encounters/healing, demolition/refunds, mid-run lifecycle changes, final balance, or broader quests/activities.

## References

- [Post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md)
- [GD63 spatial and progression decisions](gd63-spatial-and-progression-design-decisions.md)
- [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md)
- [Invariant glossary](../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md)
- [System Spec 15](../../Docs/15%20-%20UI%20Information%20Exposure%20and%20Player%20Trust.md)
- [System Spec 28](../../Docs/28%20-%20Save_Data_Model_Versioning_and_Migration.md)
- [Phase 5 branching authority](phase-5-branching-and-route-choice-design.md)
- [Phase 5B tuning and run-condition contract](phase-5b-production-tuning-and-run-condition-contract.md)
