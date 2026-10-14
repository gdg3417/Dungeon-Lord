# Combat and Reserve-Backed Floor Recovery Owner Design Lock

| Field | Decision |
| --- | --- |
| Status | **Owner-approved gameplay direction; implementation deferred** |
| Approval date | 2026-10-14 |
| Verified repository baseline | `1a6861c1a77493822540b051c5d1db739d68e39e` (`main`, clean; merged PR #236) |
| Scope | Deterministic combat, monster identity and recovery, reserve-backed mana, per-floor party occupancy, and Living Dungeon sequencing |
| Authority | This lock governs future implementation where older future-facing wording conflicts. Merged behavior, frozen evidence, save contracts, and unrelated specifications remain in force. |

## 1. Purpose and player-facing combat fantasy

Combat should be fast, deterministic, and D&D-like: the player can understand who acted, in what order, and why a monster or adventurer fell. Multiple monsters can participate in a real overlapping encounter when geometry lets them engage. Position, attack eligibility, range, and line of sight explain what can happen. A monster that cannot legally engage may be bypassed; merely occupying an encounter room does not force an attack.

Combat evidence is authoritative chronological output that a graphical replay can present without running combat again. Replay is an observer of recorded outcomes, not a second simulation.

This lock records approved direction only. It adds no gameplay, tuning values, assets, runtime authority, or save contract.

## 2. Approved gameplay decisions

### Combat

- Initially, each eligible living combatant gets one basic action per round. Deterministic initiative orders actions. A combatant defeated before its turn cannot later complete an action.
- Multiple eligible monsters participate in genuinely overlapping encounters. Legal engagement depends on geometry and configured content; do not impose a universal hardcoded participant or adjacency limit.
- Monsters need legal positioning and attack eligibility. A monster that cannot engage may be bypassed.
- Ranged attacks require valid configured range and line of sight. Solid barriers block line of sight; allies do not automatically block ranged shots. A ranged attacker does not need an empty adjacent melee tile.
- Start with basic attacks and simple class-aware targeting. Complex abilities, synergies, and behavioral pursuit are deferred. Baseline Goblin and Skeleton content comes first.
- Future players may individually choose and pay for monster levels, including high-level starting monsters. Special boss experience and discounted progression locking are deferred.
- Preserve current retreat behavior initially. Consider disengagement at combat-round boundaries later.
- Do not add direct mana rewards or duplicate existing casualty, Heat, trap, or loot authorities.
- Emit chronological authoritative combat evidence suitable for graphical replay without presentation-side resimulation.

### Monster state, recovery, and floor occupancy

- Combat C0 establishes run-local monster HP and defeat before adding persistent monster state.
- Eventually, monster HP and level persist against stable monster identity across movement, unassignment, custody, and redeployment. Legitimate upgrades that change maximum HP preserve proportional injury with deterministic rounding.
- Defeated monsters remain identifiable, assigned, and combat-inactive until an approved recovery or replacement process handles them.
- MVP permits at most one adventurer party on a floor at a time. Different floors may each host a party.
- No automatic recovery, healing, revival, trap reset, or reusable loot replenishment occurs while a party occupies that floor.
- When a floor becomes vacant, eligible injured and defeated monsters recover automatically toward a configurable readiness threshold, subject to affordability. Recovery may revive the same defeated monster and preserve its identity and committed progression. A new monster identity is a separate eventual replacement option, not the automatic default.
- Resetting a reusable loot node prepares new loot for a later party; it does not undo prior extraction.
- If full recovery is unaffordable, restore functional readiness before optional top-ups using stable deterministic ordering, and leave the remainder pending. Retry pending recovery automatically when sufficiently funded, only while the floor is vacant.
- Players may eventually unlock healing policies and recovery priorities through research. Monster leveling remains individually player-funded.
- If funding is insufficient, defense remains inactive/pending. The exact future slot and identity representation requires save-design review.

### Mana

- Keep one canonical total-mana wallet. Required reserved mana is a protected floor derived from applicable configured dungeon obligations. Usable mana is `max(0, TotalMana - RequiredReservedMana)`.
- Ordinary discretionary spending must respect that floor. Recovery may spend protected mana and debits total mana exactly once. Spending reserve does not lower the required amount; usable mana remains zero while total mana is below the requirement, until balance recovery or legitimate obligation reduction.
- MVP construction and upgrades must be affordable for both direct cost and the additional reserve obligation. Bounded overcommitment is deferred. The reserve is global across the dungeon, not a wallet per floor.
- An incompletely recovered floor may still receive a party once vacant.
- Preserve existing mana sources, including passive mana, skill spill, and adventurer-death mana. Any new canonical grant requires separate implementation approval.
- `StructureRuntimeState.ManaReserve` currently stores a spendable wallet, not a protected allocation. This lock distinguishes future required reserve from that field without renaming it or creating a competing balance.

### Living Dungeon

- Adventurers ultimately arrive automatically. Manual runs and completed-run playback are transitional capabilities, not the intended final loop.
- Multiple parties may be active on different floors. A descending party waits when its destination floor is occupied; it neither skips that floor nor exits merely because of occupancy.
- Recovery uses mana actually available at authoritative logical boundaries, never anticipated future rewards.
- Offline-attributed monster injuries and deaths do not cause new permanent dungeon losses or lasting recovery costs. Existing foreground injuries and legitimate prior costs survive an offline interval.
- Preserve the Living Dungeon owner lock's offline Heat, exact-event progression, and canonical ordering restrictions.

## 3. Preserved repository invariants

- Configuration owns gameplay tuning, including initiative inputs, attack/range eligibility, geometry/capacity rules, readiness thresholds, recovery costs/order policies, and progression values. No universal numeric limit or new numeric balance value is approved here. Runtime consumes injected or loaded configuration.
- Stable IDs and canonical ordering remain authoritative. Player-facing text continues to use localization references.
- Existing casualty, Heat, trap, loot, mana-source, atomic transaction, and floor-geometry authorities remain singular; no parallel resolver or reward path is introduced.
- The existing spendable wallet field, save schemas, and frozen checkpoint contracts are unchanged by this documentation.
- Current manual run behavior and the actual B1 executable semantics remain available to their existing callers and historical evidence.
- Historical evidence is retained. Newer owner-approved direction supersedes only named future-facing clauses, never frozen outcomes or already committed transactions.
- The Living Dungeon lock's exact deterministic event ordering, immutable run inputs/executable compatibility, offline Heat tier boundary, no run-derived login rebound, exact resumable catch-up, individually eligible settlement, and no offline permanent dungeon losses remain binding.

## 4. Explicit specification supersedence map

The following references are forward-looking addenda, not rewrites of historical evidence:

| Source | Future-facing conflict resolved by this lock |
| --- | --- |
| [Spec 01 — Mana System](../../../Docs/01%20-%20Mana%20System%20v3.md) | Older “three visible pools” and subtraction wording does not mean `ManaReserve` is an implemented protected allocation. This lock governs required reserve, usable-mana floor, and recovery-authorized spending. Existing mana sources remain unchanged. |
| [Spec 03 — Monster Stats and Upkeep](../../../Docs/03%20-%20Moster%20Stats%20and%20Upkeep%20v3.md) | The old automatic replacement/empty-slot wording is superseded for future recovery: first recover the same identified monster when eligible and affordable; insufficient funding leaves an inactive pending defense. Replacement with a new identity is a separate future option. |
| [Spec 06 — Adventurer Behavior and Party AI](../../../Docs/06%20-%20Adventurer%20Behavior.%20Evaluation.%20and%20Party%20AI.md) | Generalized role behavior and existing transient adventurer combat do not define monster combat. This lock governs initial monster combat and per-floor party occupancy. Existing adventurer casualty and mana authorities remain. |
| [Spec 11 — Failure, Recovery and Soft Loss](../../../Docs/11%20-%20Failure_Recovery_and_Soft_Loss.md) | “Recovery is primarily driven by player action with minimal passive assistance” is superseded only for approved automatic, vacant-floor monster recovery. Other collapse and recovery policies remain unchanged. |
| [Spec 21 — Economy Sinks](../../../Docs/21%20-%20Economy_Sinks_and_Late_Game_Deflation_Control.md) | Existing upkeep and discretionary economy remain. Future affordability applies both direct cost and additional protected-reserve obligation; recovery may spend reserve once. No sink or grant is added here. |
| [Spec 28 — Save Data Versioning and Migration](../../../Docs/28%20-%20Save_Data_Model_Versioning_and_Migration.md) | Stable monster state, pending recovery, identity/slot representation, and any schema/migration remain unapproved until a separately reviewed canonical persistence design. No field or schema is selected here. |
| [Spec 38 — Floor Spatial Capacity and Route Graph](../../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) | Geometry remains the spatial authority and determines legal engagement. One-party-per-floor and waiting on descent are gameplay occupancy policy, not construction capacity or a technical workload limit. |
| [Monsterology](../../../Docs/Dungeon_Builder_Monsterology_Spec_v1.md) | Species and content authoring remains its authority. Baseline Goblin and Skeleton are first; this lock grants no new species, abilities, levels, or values. |
| [Cross-Spec Invariant Glossary](../../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md) | This lock supplies the current cross-spec precedence for combat, recovery, mana, and floor occupancy. Existing invariants remain in force. |
| [Post-GD60 execution plan](../post-gd60-mvp-execution-plan.md) | Sequence C0/C1/C2 and Living Dungeon activation below supersedes conflicting future sequencing. Merged PR status is reconciled there. |

## 5. Initial Combat C0 scope and exclusions

Combat C0 is a new versioned deterministic combat implementation for new manual runs. It establishes run-local monster HP, attacks, defeat, overlapping eligible group participation, basic ranged eligibility, and canonical chronological combat evidence. It changes no writable schema.

C0 preserves historical B1 executable behavior. Existing checkpoints remain replayable under the actual executable semantics that produced them. Do not silently change B1's executable or checkpoint format to implement C0. C0 does not add persistent monster HP/level, floor vacancy recovery, automatic arrivals, new mana grants, complex abilities, pursuit behavior, new player controls, or a new save contract.

## 6. Later Combat C1 and C2 dependencies

- **Combat C1:** read-only graphical replay of new combat evidence, showing HP, attacks, deaths, and positional explanation. Presentation does not simulate combat.
- **Combat C2:** reviewed, versioned canonical monster-state and mana-reservation persistence, stable identity preservation, vacant-floor recovery, and atomic retries. Split persistent settlement from this scope if design review or evidence requires it. A save schema, migration, identity slot model, transaction grouping, and retry encoding are not chosen by this lock.

C1 depends on the evidence emitted by C0. C2 depends on reviewed canonical save ownership and compatibility design; C0's run-local state alone is not authority for persistence.

## 7. Living Dungeon occupancy, offline, and settlement dependencies

Living Dungeon activation depends on one-party-per-floor automatic admission, durable waiting at occupied floor transitions, appropriate exact-event settlement, offline protection, and the existing specified run-event mana. It must preserve exact deterministic ordering and use funds actually available at each authoritative boundary. Floor recovery runs only at vacancy; settlement or future arrival cannot erase a foreground injury or charge lasting costs for offline-attributed monster casualties. Occupancy does not turn B2's technical qualification profile into gameplay policy.

The implemented B1 checkpoint/reconstruction behavior and B2 dormant persistence/scheduling infrastructure are prerequisites and compatibility constraints, not activation of this gameplay policy. B2's qualified two-active profile is a technical profile only. Automatic arrivals, occupancy scheduling, settlement activation, and background/offline simulation are not approved by this lock for immediate implementation.

## 8. Technical and numerical details that remain unapproved

This lock does not select combat formulas, HP or damage values, initiative values, range/duration, LOS geometry data, exact legal capacity, tie-break encoding, encounter start/end conditions, round limit, recovery price, readiness threshold, recovery amount/order details beyond functional-readiness-first stable ordering, rounding representation, protected-obligation formula/data fields, save schema, migration, event encoding, or performance limits. Each requires the named owner/configuration/save review before implementation. Existing approved values and formulas remain owned by their current authorities.

## 9. Acceptance criteria

### Automated

- Deterministic combat produces identical ordered actions and evidence for identical immutable inputs; defeated combatants cannot act later in the round.
- Geometry, range, line of sight, bypass, multi-monster participation, and configuration-driven legal engagement are covered without a universal hardcoded capacity.
- C0 output is sufficient for C1 to explain HP changes, attacks, defeats, and positional eligibility without resimulation.
- Historical B1 checkpoints reconstruct under their original actual executable semantics; new C0 versioning does not alter frozen outcomes.
- Recovery tests prove floor vacancy gating, stable identity revival, functional-readiness-first partial funding, exact-once debit, pending retry only while vacant, and no new persistent loss from offline-attributed monster casualties.
- Mana tests prove one wallet, global obligations, discretionary floor protection, authorized recovery spending, unchanged reserve requirement after spending, and purchase affordability for direct cost plus added obligation.
- Save/migration tests are required only in a separately approved C2 packet with reviewed schema and migration contracts.

### Manual and gameplay experience

- Manual inspection makes turn order, actions, valid positioning, ranged eligibility, defeats, and why an eligible monster did or did not engage understandable.
- Graphical replay follows the authoritative event chronology and never changes HP or re-resolves combat.
- A vacant floor restores eligible defense in deterministic stages when funding is partial; an occupied floor receives no automatic recovery or reusable-resource reset.
- Players can understand protected usable mana, recovery spending, and pending defense without implying that the current `ManaReserve` field already stores a protected allocation.
- Gameplay-experience review confirms the intended fast, readable, D&D-like fantasy and that a party waits at an occupied destination floor.

Passing these criteria is future implementation acceptance, not evidence that the approved gameplay is currently implemented.
