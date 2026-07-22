# GD63 spatial and progression design decisions

| Field | Decision |
|---|---|
| Status | **Authoritative approved design direction** |
| Baseline | Main through merged PR #160 / GD62 |
| Reconciled | 2026-07-22 |
| Spatial specification | [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) |
| Execution sequence | [Post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md) |

## 1. Authority and boundary

This record locks the approved spatial, structural-editing, route, progression, and MVP-content direction for later content and implementation packets. Where older active planning language conflicts, this record and the reconciled Spec 38 control. Numeric tuning remains content/configuration-owned.

GD63 is documentation only. The save root remains schema version **6**. The GD62 `DungeonSpatial` contracts and pure validator remain inactive and non-authoritative; the ordered two-room models remain the runtime and save authorities. Nothing here implements or proves player-visible behavior or fun.

## 2. Locked spatial-capacity direction

- One occupied physical tile equals one floor-space unit. Used floor space is derived directly from occupied physical tiles; a physical tile never counts as multiple units.
- Geometry/floor-space consumption and mana price are separate concepts.
- Rectangular floor boundaries and rectangular or square rooms are the MVP content restriction. The domain may support irregular masks after MVP, but irregular rooms are not an MVP content requirement.
- Reserved structural or environmental tiles, such as a lava pool, reduce usable placement area. Features may also apply authored content-capacity and gameplay modifiers. They do not increase the external footprint unless the feature belongs to a separately authored larger room variant.
- Gross footprint, reserved internal tiles, usable placement area, and monster/trap/loot capacities are distinct authorities.
- The editor must eventually expose final, used, remaining, and prospective floor-space use. Player-facing labels and explanations use localization references.

Exact floor dimensions, room dimensions, footprints, content capacities, and costs are not approved here.

## 3. Locked MVP structural identities

These are design identities, not final content IDs or numeric definitions.

### Entrance Hall

- Required entrance structure; internally a distinct entrance node with stable structure identity.
- May be presented to players as Room 0, but is not an ordinary buildable room and cannot be removed.
- Trophy or decoration support is a post-MVP extension, not a GD63 or MVP requirement.

### Basic Room

- Square, medium-footprint room with a balanced monster, trap, and loot profile.

### Rectangle Room

- Rectangular and approximately in the Basic Room's floor-space class.
- Monster-favored, with lower trap capacity than the Basic Room.
- Rotation changes fit and orientation, not content identity or capacity profile.

### Large Chamber

- Large rectangular room with higher total content capacity and greater spatial and mana commitment.
- Supports concentrated defenses and must enable a meaningful large-room-versus-several-small-rooms tradeoff.

### Narrow Hall / Straight Stone Corridor

These names identify one MVP structural category: a narrow, variable-length connection.

- It permits traps but no monster placement during MVP. An optional dead-end corridor may hold loot.
- Its occupied tiles consume floor space directly.
- Additional length may provide more trap and attrition opportunities, while consuming more floor space and construction mana.
- Future variants may change configured mana price or gameplay behavior without changing one-tile-one-space accounting.

### Completion Terminal

- Required terminal after the final required room; it is not an ordinary room and contains no normal room content.
- It controls run completion, exit, or later descent behavior.

Final IDs, dimensions, capacities, costs, and socket counts remain authored-data gates.

## 4. Doorways, connections, and compatibility layout

- Adjacent compatible rooms may connect with a direct doorway. This is a graph connection with no independent corridor footprint or corridor loot capacity.
- Physically separated rooms require a corridor.
- Authored rules may permit a compatible trap at or directly beside a doorway.
- A doorway cannot open into an impassable reserved tile or invalid environmental area.
- Connection validation must consider orientation, authored connection points, reserved tiles, passability, and same-floor validity.
- The initial route and migrated compatibility route use a deterministic, simple straight-line arrangement. Later branching may introduce other directions.

Doorway geometry and validator changes are later implementation work.

## 5. Player-facing numbering and persistent identity

Room labels are derived presentation, never persistent identity authority:

1. Entrance node → player-facing Room 0.
2. Legacy internal Room 0 → first buildable room → player-facing Room 1.
3. Legacy internal Room 1, when present → second buildable room → player-facing Room 2.
4. Completion terminal.

The graph uses stable entrance, room-instance, node, edge, and terminal identities. This decision does not approve textual ID templates.

## 6. Locked structural-editing policy

### Draft transactions and atomic commit

- Placement, movement, renovation, and deletion occur in draft state; a draft is not the active authoritative layout.
- Preview discloses affected and downstream structures, removed or returned contents, affected connections, floor-space changes, mana cost/refund, and validation failures.
- Invalid drafts cannot commit. Mana is spent only after complete successful validation.
- Commit is atomic. Failure cannot partially change layout, inventory, mana, or save.

### Movement

- Moving a room preserves its contained content and stable instance identities.
- It also moves every connected downstream structure that would otherwise disconnect; with branches this is the affected descendant subtree.
- Relative placement within the group is preserved where valid, and the complete group must fit and validate before commit.
- Movement is renovation, not demolition followed by reconstruction.

### Removal

- Structural deletion is leaf-first. A room or corridor cannot be removed if that would disconnect downstream active structures; downstream structures must be removed first.
- Removing a room resolves its own contents under the ownership policy below. Successful deletion restores its full physical floor space.
- A future acknowledgement popup may be disabled, but the final draft consequence summary must always disclose removals and refunds.

### Content ownership

- Reusable monsters return to the roster or inventory; reusable traps return to inventory.
- Reusable owned bait loot returns when its content definition says it remains owned.
- Consumed, spent, generated, or already resolved run resources do not return.
- Renovation cannot silently destroy reusable owned content. A future consumable-on-placement item may override this only through explicit authored policy.

Inventory, roster, and deletion behavior remain unimplemented.

## 7. Construction and refunds

- Every structural edit previews separate floor-space use and mana price. Insufficient mana blocks the entire transaction.
- Movement uses a configured renovation policy; room replacement is a renovation transaction.
- Corridor pricing may use configured base, per-tile, archetype, and environmental/material components.
- Refund percentages are configuration-owned. The conceptual refund basis is recorded invested mana multiplied by the configured refund percentage, not current catalog price.
- Future structure save state needs a reviewed representation of relevant invested construction and renovation mana. GD63 adds no field and changes no schema.
- Rounding, clamping, and exact pricing/refund formulas remain later data-contract decisions.

## 8. Corridor gameplay purpose

Corridors trade compactness for defensive opportunities. Direct-door layouts cost less mana and floor space; corridor-heavy layouts can provide more trap and attrition opportunities at greater mana and space cost. Corridor traps may require adventurers to detect, defuse, resist, survive, or bypass them; may consume mana or other configured resources; and may injure or kill adventurers before later rooms. Downstream performance may reflect injury, class, level, awareness, health, and abilities. Exact simulation formulas remain configuration-owned later gameplay gates.

## 9. Locked MVP branch model

### MVP restriction

- One required route and at most one optional detour per floor.
- The optional detour splits from the required route and may end with traps and loot.
- No loops, nested branches, alternate entrance, or alternate descent/floor-completion terminal.
- Adventurers, not the player, make one branch choice per run.
- Automatic return from a completed dead end is allowed and is not a second branch decision. Resolved traps, monsters, and loot do not trigger again on return.
- No discretionary backtracking and no floor-to-floor backtracking.

The exact selection formula and tie-break remain the Phase 5 data gate.

### Post-MVP extensibility

The domain may later support multiple optional detours, side bosses/special rewards, multiple branch visits, general backtracking, and increased cumulative danger. There remains no alternate entrance to the next floor unless a future approved specification changes that rule.

## 10. Exit, descent, and floor progression

Adventurers make the exit-versus-descend decision. Potential authored inputs include party strength, health/injuries, survivors, carried loot, perceived next-floor danger, intent, class behavior, and dungeon pressure. Descending carries reviewed run identity and survivor state forward. Exact fields, coefficients, thresholds, tie-break, and save representation remain the Phase 6 gate.

The following are capacity-authoring composition targets, not fixed object-count limits:

| Floor | Approximate rooms | Approximate corridors |
|---|---:|---:|
| 1 | 2 | 0–1 |
| 2 | 3 | 1–2 |
| 3 | 3–4 | 2–3 |
| 4 | 4–5 | 3–4 |
| 5 | 5–6 | 3–4 |

Floor 5 targets approximately 7–9 buildable rooms and corridors combined. Entrance and completion terminals do not count as buildable route pieces. Configured capacity may accommodate fewer large rooms or more small rooms. Floor index does not determine monster level or layout style. Exact floor capacities remain unapproved.

## 11. Monster-family direction and MVP content budget

The long-term family architecture progresses through base units, basic specialty branches, specialist branches, elite branches, and later leaders, rulers, champions, religious units, and other advanced branches.

Each required MVP family budgets one base unit, two basic specialty units, one specialist unit, and one elite unit. Across the Undead and Goblinoid families this targets ten MVP monster units. Second specialist and elite paths are approved post-MVP opportunities. This is a content/research-tree budget, not authored monsters, stats, nodes, or tuning.

## 12. Offline mana direction

- Offline mana uses a configured percentage of the applicable online passive generation rate.
- Research may improve offline efficiency, eligible duration, storage, or Mana Farm production.
- It should support continued expansion without replacing active play. The design target is that a normal overnight absence generally funds one meaningful construction or renovation decision, not a whole floor.
- Percentage, cap, duration, clock-manipulation handling, and rounding remain tunable gates.
- Future results must explain elapsed time, effective rate, cap, and awarded mana through localization-backed presentation.

## 13. Architecture requirements

Later implementation must preserve deterministic simulation; stable IDs; ordinal canonical ordering; versioned saves and explicit migration; atomic state changes; a single writable layout authority; side-effect-free validation; stable reason codes; configuration-owned tuning; localization-backed text; mobile-bounded workloads; save compatibility; and physical tiles as both geometry and floor-space authority.

## 14. Remaining implementation and authored-content gates

- **GD64:** inactive MVP spatial content contract, final content IDs/footprints, and export/schema/foreign-key validation.
- **GD65:** final migration mapping, stable textual ID rules, coordinates/orientations, fixtures, fallback/content-missing policy, backup/recovery UX, and atomic recovery evidence.
- **Later migration implementation:** separately reviewed schema change and authority transition; no version is approved by GD63.
- Exact floor/room dimensions and capacities; content IDs; connection points; socket/content capacities; construction/renovation/corridor costs; refund percentage, rounding and clamping; environmental modifiers; and workload/device limits.
- Doorway geometry/validation, invested-mana save representation, editor transactions, inventory/roster consequences, corridor simulation, Phase 5 branch formula/tie-break, and Phase 6 transfer/save details.
- Offline mana percentage, cap, duration, safeguards, rounding, and result presentation.

These gates must not be guessed in runtime code. Phase 3, 5, 7, and 9 observation gates remain responsible for testing whether the spatial fantasy is understandable and fun.
