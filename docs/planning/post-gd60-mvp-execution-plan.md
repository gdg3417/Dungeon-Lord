# Post-GD60 MVP Execution Plan

| Field | Decision |
|---|---|
| Status | **Authoritative active roadmap after GD61** |
| Baseline | Main through PR #158 / GD60 |
| Supersedes | Sprint 2-4 execution order, post-GD9 sequence, and earlier vertical-slice forecasts |
| Spatial authority | [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) |
| Last reconciled | 2026-07-21 |

## 1. Authority and purpose

This is the single active dependency order from the GD60 prototype to a fun MVP, a usable graphical editor, and external testing. Older sprint plans, closeouts, evidence records, and vertical-slice plans remain historical traceability: they retain useful acceptance criteria, but are **not** the authoritative execution sequence. A planned status is not implementation evidence. The merged repository and committed tests/evidence are authoritative; uncertain closure is marked **requires confirmation**.

GD61 changes documentation only. It does not implement spatial models, migration, economy, routing, additional floors, content, or UI.

## 2. Current repository baseline after GD60

### Implemented capabilities

Merged history establishes the following at prototype scope:

- Deterministic/config-owned core simulation foundations, heat and mana feedback, placement effects, composition-driven outcomes, and localized player feedback.
- A simple MVP screen and Bootstrap validation surface supporting a build/run/inspect/adjust loop.
- Room, monster, trap, and loot choices; deterministic loot; a minimal research bridge and player-completable research activity.
- Persistent room-slot assignments, selection and fit feedback, two-room expansion, ordered room traversal, and GD60 ordered two-room outcome/effect resolution.
- Player-facing intent, party, run previews, analysis, next-action guidance, contract completion, spoils ledger, pacing improvements, save/lifecycle hardening, and development-build readiness.
- Automated deterministic, save, localization, presenter, journey, and route regressions. Exact manual closeout coverage remains tied to the committed evidence record.

### Partially implemented capabilities

- **Layout:** two ordered room slots imply route order, but are not physical tile geometry or a general graph.
- **Construction:** choices and replacement exist, but spatial placement, corridors, structural removal/renovation, and mana-backed costs do not.
- **Research:** a minimal bridge and completion flow exist; Architecture branching/expansion and a meaningful research interface do not.
- **Economy/offline:** prototype mana/heat flows exist; construction spending, floor expansion, and production-grade offline behavior remain incomplete or require confirmation.
- **UI:** the simple screen is usable for validation, but Bootstrap remains a temporary control/diagnostic dependency and is not a production dungeon editor.
- **Saves:** lifecycle integrity is hardened for current models; graph-version migration has not been designed or implemented.

### Missing MVP capabilities

Physical footprints and overlap checks; authoritative floor capacity; corridor structures; entrance/exit nodes; saved same-floor route graphs; reachability validation; structural editing and containment resolution; construction economy; one optional branch and route decision; Floor 2 transition; graphical editor parity; broader room/environment/content choices; Architecture progression; onboarding/accessibility; mobile profiling; and external fun-test evidence.

### Deferred capabilities

Curved/freeform corridors, intra-floor elevations, teleporters, secret rooms, locked-door puzzles, procedural generation, unrestricted mazes, complex pathfinding AI, multiple entrances/descents, advanced environmental simulation, floor-to-floor backtracking, full production art, diplomacy depth, live operations, prestige, social/competitive systems, monetization implementation, and speculative backend/release dashboards.

### Immediate risks and controls

| Risk | Control |
|---|---|
| Ordered slots become a second, incompatible layout model | Define Spec 38 contracts first; migrate once; retain a compatibility adapter only as needed. |
| Save breakage or nondeterministic graph ordering | Approve version/migration plan before schema changes; stable IDs, canonical ordering, legacy fixtures. |
| Two capacity systems disagree | Tiles/masks own geometry; required player-facing floor-space costs/capacities are configured and derived from or validated against the same footprint rules. |
| Tuning leaks into runtime | Put every limit, coefficient, cost and modifier in approved typed/content configuration. |
| Full UI rewrite begins before domain behavior stabilizes | Complete Phases 1-6 contracts/behavior first; Phase 7 proves parity before Bootstrap retirement. |
| More scaffolding without fun | Every phase ends in observable capability; run player-fantasy checkpoints at Phases 3, 5, 7, and 9. |
| Hardening crowds out playability | Schedule only dependency-critical reliability before Phase 9; defer production operations. |
| Prototype evidence is mistaken for fun | Automated tests prove correctness only; moderated/unmoderated observation answers fun questions. |

## 3. Execution rules

- Each packet should normally be one reviewable PR with tests and evidence; split it if migration, UI, or content changes become hard to review together.
- Do not start a phase until its dependency exit criteria pass, except isolated content authoring behind inactive contracts.
- Preserve deterministic behavior, additive/explicitly migrated saves, localization ownership, and config-owned tuning.
- Do not permanently add player controls to Bootstrap. It remains a diagnostic/fallback surface until Phase 7 parity is demonstrated.
- A phase is not complete merely because types or tests exist: its player-observable outcome and exit criteria must be met.

## 4. Dependency-ordered phases and PR-sized packets

### Phase 0 — GD61 planning reset (current)

**Packets:** (1) audit/reconciliation and authority banners; (2) Spec 38; (3) this roadmap and matrices.

**Exit:** required documents agree on baseline, authority, scope and sequence; links validate; diff is documentation-only. **Technical gate:** documentation validation passes. **Fantasy gate:** questions to test are explicit; no claim that fun is proven.

### Phase 1 — Spatial domain foundation

1. **GD62 — Spatial contracts and validator foundation:** typed/config contracts for floor capacity, tile footprints/masks, rooms, simple corridors, graph nodes/edges, stable IDs and canonical ordering; pure capacity/overlap/connectivity/endpoint validators.
2. **Save/migration design gate:** version proposal, old-to-new mapping, failure/rollback policy and fixtures, without migrating live state unless kept as a separately reviewable PR.
3. **Content contract/export update:** approved authoring fields, foreign-key/schema validation and test configuration; all numbers data-driven.

**Exit:** contracts can represent the MVP layout, reject every Spec 38 invariant violation deterministically, and round-trip a test graph. Save migration is reviewed before persistence changes. **Technical gate:** deterministic validator/schema tests; no gameplay constants or player-facing literals. **Fantasy gate:** paper/prototype review confirms capacity and fit explanations are understandable.

### Phase 2 — Backward-compatible migration

1. Add the new save version and migrate/adapter-map the current ordered two-room state to entrance → rooms → completion.
2. Switch existing simulation/read models to canonical graph order while preserving current player flow and outcomes.
3. Add legacy/current fixtures, idempotence, corrupt-state policy, and rollback/recovery evidence.

**Exit:** old saves load without losing assignments; current saves round-trip; identical legacy inputs preserve deterministic route behavior; no dual writable authority remains. **Technical gate:** migration matrix, journey, route and save tests. **Fantasy gate:** existing build/run/inspect flow shows no regression.

### Phase 3 — Structural editing rules

1. Place rooms and the MVP corridor with footprint, capacity, connection and reachability preview.
2. Replace/renovate/remove structures; explicitly resolve contents and attached corridors using approved policies.
3. Expose deterministic validation reasons via localization-backed presentation contracts (Bootstrap may host temporary diagnostics only).

**Exit:** Floor 1 supports entrance, Basic Room, Narrow Hall, one additional room archetype, required route and completion node; valid edits persist and invalid edits are atomic. **Technical gate:** boundary, overlap, reachability, connection, containment and ordering tests. **Fantasy gate:** players can predict fit failures and compare compact versus spreading layouts in a low-fidelity test.

### Phase 4 — Mana-backed construction and offline mana completion

1. Data-author room/corridor build, renovation, removal/refund and expansion policies.
2. Apply transactional mana spending and Architecture/floor/theme modifiers through the formula framework.
3. Add localized cost, remaining-capacity, consequence and affordability previews before commit.
4. Complete and evidence idle offline mana calculation before final MVP validation; exact existing coverage requires confirmation.

**Exit:** every structural edit previews and atomically applies the configured cost/policy; insufficient mana cannot partially mutate layout. **Technical gate:** formula order, affordability, rollback, migration and localization tests. **Fantasy gate:** observe whether spatial growth competes meaningfully with monsters, traps, loot and research.

### Phase 5 — Basic branching and route choice

1. Connect Basic Branching research to an allowance of at most one optional branch per floor.
2. Classify required/optional edges and implement a narrow deterministic route selector.
3. Report path taken and branch-specific loot, danger and heat contributions.

**Exit:** all rooms remain reachable; required route terminates correctly; branch allowance is enforced; repeated inputs choose/report the same route. **Technical gate:** graph, unlock, route-order, outcome, save and localization regressions. **Fantasy gate:** test whether branch placement changes understandable decisions/outcomes. Advanced AI and secrets are not dependencies.

### Phase 6 — Additional floor foundation

1. Author Floor 2 unlock and configured larger base capacity without tying floor index to monster level.
2. Give each active MVP floor exactly one entrance and add terminal semantics and survivor/run-state transfer.
3. At a non-final-floor terminal, implement the approved choice to exit with defined survivors/loot/run state or descend with defined survivors/run state; define its deterministic selection formula and exact transfer contract at this Phase 6 gate.
4. Add deterministic multi-floor summary. The final active floor ends at an exit or run-completion terminal.

**Exit:** two floors remain independently valid same-floor graphs; survivors transition without backtracking; save/replay ordering is stable. **Technical gate:** transition, capacity, save, outcome and bounded-work tests. **Fantasy gate:** validate compact first/large second, dangerous first/weak second, boss, training and loot-focused concepts remain possible.

### Phase 7 — Graphical dungeon editor vertical slice

1. Floor navigation, room catalog/selection and footprint placement with touch/safe-area behavior.
2. Corridor placement, capacity/slot display, validity reasons and route visualization.
3. Mana and consequence previews; build/run/inspect/revise flow; accessibility baseline.
4. Demonstrate behavior parity, then remove normal player dependence on Bootstrap controls while retaining diagnostics behind development boundaries.

**Exit:** a player can complete the loop without developer instructions on target aspect ratios; Bootstrap is not the normal player path. **Technical gate:** presenter/UI tests, smoke evidence, localization, safe-area and performance checks. **Fantasy gate:** players understand remaining capacity and voluntarily revise layouts; a full art pass is not required.

### Phase 8 — Content and research expansion

Use small vertical content packets: Floors 3 through the locked maximum of 5; additional rooms and environmental blocked-area features; Undead and Goblinoid launch-family breadth with no more than one boss set per family; five adventurer classes; loot progression through Steel; Architecture nodes and research interface; and the one Mana Farm sub-dungeon after main-dungeon spatial and construction-economy contracts stabilize. Every packet includes authoring validation, balance hypothesis and a player-observable choice. Exact current coverage for these requirements requires confirmation rather than inference.

**Exit:** enough combinations exist to form multiple viable archetypes without one dominant layout; content limits remain configured. **Technical gate:** content integrity, modifier, save and performance tests. **Fantasy gate:** large boss room versus several small rooms and environmental placement tradeoffs are legible and meaningful.

### Phase 9 — MVP validation and hardening

1. Instrumented internal fun tests and balance passes; onboarding/accessibility iteration.
2. Save migration/content-integrity hardening and low-end mobile profiling.
3. Android device tests, external-test build, evidence and release decision.

**MVP completion exit:** the core fantasy is technically stable **and** observed as understandable and replayable; a player can build, run, inspect, revise and rerun without instruction. Passing automation alone is insufficient. **Technical gate:** clean journey, migration, deterministic replay, content, performance, accessibility, Android and build evidence. **Fantasy gate:** external participants demonstrate meaningful choices and voluntary experimentation.

## 5. Fun-validation question set

At Phase 3/5/7/9 checkpoints, record observation and evidence rather than yes/no developer assertion:

- Does compact versus sprawling create a meaningful tradeoff? Do more corridors have benefits and costs?
- Can players understand remaining capacity and predict why a room fits or fails?
- Does room order change outcomes, and does branch placement influence route selection?
- Does a large boss room feel different from several small rooms?
- Do environmental features create understandable placement tradeoffs?
- Can players create multiple viable archetypes (dangerous first floor, weak later floor, compact boss, maze, training, loot attraction)?
- Can a player build, run, inspect, revise and run again without developer instructions?
- Does the player voluntarily continue experimenting, and can they explain the tradeoff they are testing?

## 6. MVP boundary versus post-MVP hardening

**MVP requires:** the complete locked checklist below, narrow Spec 38 spatial scope, progression through the approved maximum of five floors, one branch, deterministic runs, meaningful configurable content, usable graphical editor, onboarding/accessibility baseline, low-end performance evidence and an external test build whose observed play supports the fantasy.

**Post-MVP production hardening begins only after that proof:** larger graphs and richer route AI, advanced transitions/environments, full production art, extensive device matrices, service scaling, live operations, prestige, social features, monetization implementation, anti-cheat expansion, release dashboards and operational automation. Critical security/data-loss fixes may move earlier when they block safe testing.

## 7. Locked MVP Scope Checklist

Source authority: [What is the smallest version of Dungeon Builder that proves the fantasy is fun](../../Docs/What%20is%20the%20smallest%20version%20of%20Dungeon%20Builder%20that%20proves%20the%20fantasy%20is%20fun.md). This checklist preserves the full locked scope; Floor 2 is only the first multi-floor foundation and does not reduce the up-to-five-floor MVP.

| Locked requirement | Current status / GD evidence | Remaining gap | Phase | MVP disposition |
|---|---|---|---|---|
| Dungeon core | Partial; prototype core loop exists, exact locked-scope closure requires confirmation | Validate integration with spatial progression and final journey | 1-9 | Required |
| One main dungeon | Partial; current prototype operates one simple dungeon/layout | Complete spatial construction, multi-floor progression and editor | 1-9 | Required |
| Up to five floors | Remaining beyond current one-floor prototype; GD60 is an ordered two-room route, not multi-floor evidence | Floor 2 foundation, then configured Floors 3-5 and final validation | 6, 8, 9 | Required; maximum is five |
| Undead monster family | Partial or requires confirmation; starter content is not evidence of full launch-family breadth | Author/validate launch family and one allowed boss set | 8, 9 | Required |
| Goblinoid monster family | Requires confirmation | Author/validate launch family and one allowed boss set | 8, 9 | Required |
| One boss set per launch family | Remaining/requires confirmation | At most one boss set for Undead and one for Goblinoid | 8, 9 | Required where the locked family scope calls for bosses; sets beyond one per family excluded |
| Mana generation | Partial; prototype mana feedback/simulation exists | Confirm locked formulas, integration and final journey evidence | 4, 9 | Required |
| Idle offline mana | Partial or requires confirmation; no completion claim from GD61 | Complete and evidence calculation before final validation | 4, 9 | Required |
| Peace, Notice and Concern heat states | Partial; prototype heat system exists, exact three-state journey evidence requires confirmation | Validate only the three locked MVP states | 8, 9 | Required; more than three excluded |
| Five adventurer classes | Partial or requires confirmation | Complete/validate exactly five MVP classes in content and runs | 8, 9 | Required |
| Loot progression through Steel | Partial; deterministic loot/spoils exist but tier breadth requires confirmation | Author/validate progression through Steel | 8, 9 | Required |
| One research slot | Implemented at prototype scope through the single-slot lifecycle/research bridge and GD59; final integration still required | Architecture progression/UI and final journey evidence | 5, 8, 9 | Required; retain one slot |
| One Mana Farm sub-dungeon | Remaining; historical planning deferred it until core-loop stability | Implement one type after main spatial and construction economy stabilize | 8, 9 | Required; more than one excluded |

**Locked explicit MVP exclusions:** prestige; seasonal events; PvP or leaderboards; hero adventurers; more than one sub-dungeon; advanced diplomacy; more than three heat states; and boss sets beyond one per family. GD61 does not amend these exclusions.

## 8. Genuine open decisions (owners and decision gates)

| Question | Needed by | Required evidence/owner |
|---|---|---|
| Tile scale, Floor 1 capacity, footprints, costs and coefficients | Phase 1 content contract | Design/Data; authored tuning, never runtime constants |
| Exact mapping between occupied tiles and displayed space units | Phase 1 | Design/UX/Data; one projection with rounding rules |
| Third MVP room archetype and simple corridor identity | Phase 1 content packet | Design/Content |
| Removal/refund and contained-placement resolution policy | Before Phase 3 implementation | Design + save/UX review |
| Legacy two-room migration default coordinates and IDs | Before Phase 2 | Engineering/Data; fixture review |
| Narrow branch selection formula and tie-break | Phase 5 | Design/Data; deterministic test cases |
| Exit-versus-descend state carried between floors | Phase 6 | Design/Engineering; cross-spec review |
| Exact active-floor/content/device limits within the locked maximum of five floors | Phases 6/8/9 | Existing approved sources where present; otherwise Design/QA data decision |

## 9. Next implementation recommendation

**GD62: Establish spatial domain contracts and layout validation foundations.** Limit it to typed or configuration-consumed floor-capacity, room/corridor footprint, and graph node/edge representations; stable identifiers; deterministic canonical ordering; and pure deterministic validators for capacity, overlap, endpoints, connections and same-floor reachability. Include save-version and migration planning only. GD62 must not migrate saves, change the current player loop, add structural editing UI, mana costs, branching, additional floors, the Mana Farm, or production UI. This prevents premature migration and editor rewrites.
