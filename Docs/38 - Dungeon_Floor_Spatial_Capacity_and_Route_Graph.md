**System Spec 38: Dungeon Floor Spatial Capacity and Route Graph**

*Dungeon Builder, locked candidate specification for implementation planning*

| Field | Decision |
|---|---|
| Status | **Locked candidate** — approved system decisions; numeric tuning and unresolved policies remain content/design gates |
| Scope | Floor capacity, tile footprints, rooms, corridors, entrances/exits, route graphs, branching, layout validity, progression and performance |
| Primary goal | Make dungeon construction spatial, strategically legible, deterministic and saveable without exceeding mobile-safe bounds |
| Invariants referenced | INV-04 stable IDs/content-owned tuning; INV-06 modifier order; INV-12 dungeon-edit save safety; INV-15 spatial validity |
| Related specs | 01, 09, 15, 17, 19, 28, 30, 36, 37; Architecture research spec |

# 1. Purpose and authority

Define the missing spatial contract between authored construction content, saved dungeon state, deterministic run resolution and the future graphical editor. This specification authorizes planning, not implementation in GD61. Physical tile occupancy is authoritative. All capacities, limits, costs, dimensions, coefficients, modifiers and allowances are data-driven and tunable through approved content tables, exports, or typed configuration; runtime logic must not embed tuning values.

# 2. Terms

- **Floor:** one same-floor construction space and route graph.
- **Tile footprint:** occupied tile coordinates derived from a rectangle or irregular mask; the collision authority.
- **Gross room footprint:** all tiles enclosed/claimed by the room structure.
- **Blocked/reserved internal area:** footprint tiles unavailable for ordinary contents because of structure or environment.
- **Usable placement area:** gross internal area minus blocked/reserved area under authored rules.
- **Content-specific capacity:** independently authored monster, trap and loot capacity; it is not inferred solely from area.
- **Floor-space cost / space units:** the required player-facing, configured capacity consumption of every buildable room and corridor, derived from or validated against physical footprint rules.
- **Node:** entrance, room, exit/descent, or completion vertex.
- **Edge/corridor:** a structural connection between valid same-floor endpoints.
- **Required route:** connectivity that must reach a valid terminal.
- **Optional branch:** a reachable alternate path permitted by an unlock allowance.
- **Active:** included in validation, simulation and persistence rather than draft/uncommitted editing state.

# 3. Capacity model

Each active floor exposes configured final floor-space capacity plus deterministically calculated used and remaining capacity. Later floors generally have greater configured base capacity, but exact values are authored. Final capacity may be modified by Architecture research, mana-funded expansion, floor upgrades, and content/theme modifiers through INV-06 and Spec 30 ordering. Every buildable room and corridor exposes a configured floor-space cost.

The physical footprint is the source of truth for geometry, fit, occupancy and overlap. Required floor-space costs are derived from or validated against the same authored footprint rules. Used capacity is the deterministic total of active structure costs under configured accounting rules; remaining capacity is final capacity minus used capacity. The UI must eventually display final, used and remaining floor-space capacity and each prospective structure cost. Values, conversion rules, coefficients and rounding rules are content-owned and tunable under INV-04. Floor-space values must never become a second geometry authority.

Capacity increases expand construction possibility; they do not silently relocate existing structures. Content-limit reductions must be handled gracefully: preserve readable saved state, mark invalid/excess state explicitly, prevent worsening edits, and offer a deterministic repair path rather than deleting content silently.

# 4. Room contract

A room archetype/instance must be able to reference or store, as appropriate:

- stable room/archetype/instance IDs;
- width and height or an irregular tile mask, orientation and anchor;
- configured floor-space cost and derived occupied tile count;
- maximum connections;
- monster, trap and loot capacities;
- room modifiers and environmental features;
- theme tags and placement restrictions;
- build cost, renovation rules, removal rules and unlock requirements.

Larger footprint may support more contents, but no automatic one-to-one rule is authoritative. Gross footprint, blocked/reserved area, usable placement area, and each content-specific slot capacity are distinct. Environmental features consume or reserve usable space and may modify slot capacity. For illustration only and **not authoritative tuning**, a nominal 40-by-40-foot room with a 20-by-20-foot lava or water feature would generally expose less usable placement area/capacity than an otherwise empty room. Authoritative dimensions, conversion scale and capacities must come from approved data.

Contents require stable instance identity and deterministic ordering. Placement must pass both usable-area rules and the relevant monster/trap/loot capacity. A valid empty tile does not itself grant a content slot.

# 5. Corridor contract

Corridors consume capacity according to physical footprint or length/width accounting, with every coefficient configured. They are saved structural edges, not free visual lines. A corridor contract remains extensible for stable ID, length, width, direction/orientation, source and destination endpoints, occupied tiles, trap sockets, environmental hazards, movement modifiers, visibility, and required/optional classification.

The MVP may author one simple corridor type. Its endpoints must exist, permit the connection, be on the same floor, and match connection geometry. Corridor footprints participate in overlap and capacity validation. Future curved/freeform geometry must not be assumed by the initial representation.

# 6. Floor route graph

Every active MVP floor has exactly one entrance node. Every active room must be reachable from that floor's entrance through a path entirely contained on that floor. Reaching a room must never require descending to another floor and returning. Branches and alternate paths are allowed, but disconnected active rooms are invalid for the MVP. Multiple entrances remain deferred. Future secret-room or alternate-entry rules require an explicit specification amendment and are not MVP behavior.

Every required path leads to a valid exit, descent or completion terminal. Once multiple floors exist, a non-final floor generally ends at a terminal where adventurers may choose either to exit the dungeon and end the run with the defined survivors, loot and run state, or to descend and carry the defined survivors and run state to the next floor. The exact choice formula and transferred state are Phase 6 design gates. The final active floor uses an exit or run-completion terminal. Shortcuts, escape routes, portals and secret transitions remain future extensibility points.

Graph serialization uses stable IDs and canonical deterministic ordering independent of map/dictionary enumeration or UI selection order. Route resolution and summaries consume this canonical order and explicit tie-break rules.

# 7. Branching and route selection

Architecture research gates branching tools and reconciles the existing `ac_300` Basic Branching concept with graph allowance. The first implementation permits at most one optional branch per floor, clearly classifies required versus optional routes, uses a narrow deterministic selection rule, and reports the path taken in localized player-facing results. It excludes secret-room logic and unrestricted maze simulation.

The model may later consider loot attraction, perceived danger, current health, party composition, greed/caution, known length, objectives and previous knowledge. These are future data inputs, not an immediate advanced-AI dependency. Phase 5 must approve a small data-driven formula and deterministic tie-break before implementation.

# 8. Layout validity invariants

A committed active layout is valid only when all apply:

1. Used floor capacity does not exceed final configured floor capacity.
2. Structures do not occupy overlapping tiles except where an explicitly authored endpoint/junction rule permits it.
3. Every active room is reachable from the floor entrance on the same floor.
4. Every required path reaches a valid exit, descent or completion node.
5. Corridors connect valid, compatible endpoints on the same floor.
6. No room exceeds its maximum connection count.
7. Room contents do not exceed monster, trap or loot capacity and occupy only allowed usable area.
8. Blocked environmental area is never counted as usable placement area.
9. Branch count does not exceed the unlocked allowance.
10. Structural changes preserve validity or remain an uncommitted draft until validity is restored.
11. Removing/replacing a room explicitly resolves contained monsters, traps, loot and connected corridors under an approved deterministic policy; nothing is silently orphaned.
12. Saved state uses stable IDs, canonical ordering and versioned migration.

**INV-15:** Every active dungeon floor must remain within its configured spatial capacity, and every active room must be reachable from that floor's entrance through a valid saved same-floor route graph.

Validation is deterministic, side-effect-free before commit, and produces stable reason codes. Player-facing explanations map those codes through localization; they are not hardcoded by the validator. An edit commits atomically only after all affected invariants pass or an explicit transaction resolves all consequences.

# 9. Progression and economy relationship

Floor index influences authored base capacity but never caps monster level. The system must permit a highly dangerous first floor, weak later floor, compact boss floor, sprawling maze floor, training floor, or loot-attraction floor. Later capacity enables complexity without prescribing layout style.

Architecture research and mana-funded expansion create tradeoffs among more rooms, larger rooms, corridors/branches, room improvement, and investment in monsters, traps, loot or research. Construction, corridor, renovation, removal/refund and expansion costs are configuration-owned and previewed before commit. Spec 01 owns mana transaction integrity; Spec 09/Architecture owns unlocks; Spec 30 owns modifier ordering.

# 10. Save and migration contract

INV-12 requires immediate save safety for committed dungeon tile placement/movement; Spec 28 governs versioning. Saved spatial state must include stable floor/node/edge/structure identities, footprint/orientation data or stable content references plus required instance state, graph relationships, and canonical order. A migration must map the current ordered two-room layout to a valid graph (entrance → ordered rooms → completion) without changing deterministic outcomes unless explicitly versioned and evidenced.

Before schema implementation, approve default coordinates/footprints, stable-ID derivation, content-missing behavior, rollback/recovery behavior and legacy fixtures. Migration is deterministic and idempotent. Runtime must not maintain ordered slots and a graph as competing writable authorities.

# 11. UI and information exposure

Spec 15 governs trust and Spec 27 governs text. The editor must expose configured final, used and remaining floor-space capacity, each prospective room/corridor floor-space cost, footprint preview, overlap/fit result, room-specific capacities, blocked area, connection availability, required/optional route classification, route taken, construction cost and edit consequences. Reasons use localization keys/table references.

Bootstrap is a temporary diagnostics/control surface. It may validate domain behavior during Phases 1-6 but must not become the permanent editor. The graphical vertical slice begins after contracts and structural behavior stabilize and replaces normal Bootstrap controls only after feature parity and smoke evidence.

# 12. Performance and device rules

Consistent with Spec 36, use configured limits for active floors, rooms, edges and workload. MVP operation uses limited active floors/rooms, bounded graph complexity, batched simulation, cached/incremental route validation after structural changes, no expensive continuous pathfinding, and UI refresh separated from simulation ticks. Exact graph limits are not locked here unless an approved source owns them.

Validation and route selection occur on edits/run boundaries rather than per frame. Rendering should avoid rebuilding the full dungeon on simulation ticks. When authored limits change, load and repair handling is deterministic and graceful. Profiling on low-end target devices is a Phase 9 gate.

# 13. MVP implementation profile

The initial spatial implementation profile supports Floor 1; configured fixed initial capacity; Basic Room; Narrow Hall; one additional room archetype; one simple corridor; entrance; required path; exit/completion; at most one optional branch; tile/footprint, capacity and same-floor reachability validation; save compatibility; deterministic route ordering; and player-visible capacity/route results.

Explicitly deferred: curved/freeform corridors, multiple elevations, teleporters, secret rooms, locked-door puzzles, procedural generation, unrestricted mazes, complex pathfinding AI, multiple entrances, multiple descent points, advanced environmental simulation, floor-to-floor backtracking and full production art.

# 14. Cross-spec ownership

| Concern | Authority / integration rule |
|---|---|
| Numeric authoring, IDs, schemas, exports | Spec 19; validation rejects missing/invalid references |
| Research and Basic Branching | Spec 09 and Architecture spec; Spec 38 defines spatial allowance semantics |
| Mana costs | Spec 01; atomic transaction around a valid edit |
| Saves/migration/offline | Specs 17 and 28; graph is stable, versioned, deterministic |
| Formula/modifiers | Spec 30; base then ordered modifiers, with clamping/rounding defined in data contract |
| Player information/localization | Specs 15, 27 and 35; reason codes mapped outside domain logic |
| Performance | Spec 36; bounded work, batching, cached validation, configured limits |
| QA | Spec 37; validator boundaries, property/invariant, migration, deterministic replay and journey evidence |

# 15. Acceptance gates for implementation planning

- Every field and invariant has a clear domain/content/save/UI owner.
- Every buildable room/corridor has a configured floor-space cost, and every floor exposes final, used and remaining floor-space capacity.
- No unlabeled numeric example is treated as authoritative tuning.
- The tile footprint and displayed capacity relationship has one source of truth.
- Migration is reviewed before current saves are changed.
- The first branch does not require advanced AI.
- Structural mutations are atomic and cannot orphan contents/edges.
- Tests distinguish technical correctness from player research; automation does not claim the fantasy is fun.

# 16. Open questions (not locked tuning)

The exact tile scale, capacities, dimensions/masks, display-unit projection, costs, modifier coefficients, third room, corridor content ID, removal/refund policy, migration coordinates/IDs, branch formula/tie-break, transfer state and configured workload limits require their named roadmap decision gates. Until approved in content/config and evidence, they must not be guessed in runtime code.
