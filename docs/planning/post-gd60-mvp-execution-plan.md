# Post-GD60 MVP Execution Plan

| Field | Decision |
|---|---|
| Status | **Authoritative active roadmap after GD63 reconciliation** |
| Baseline | Main through merged PR #160 / GD62 |
| Supersedes | Sprint 2-4 execution order, post-GD9 sequence, and earlier vertical-slice forecasts |
| Spatial authority | [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) |
| Last reconciled | 2026-07-22 |

## 1. Authority and purpose

This is the single active dependency order from the GD60 prototype to a fun MVP, a usable graphical editor, and external testing. Older sprint plans, closeouts, evidence records, and vertical-slice plans remain historical traceability: they retain useful acceptance criteria, but are **not** the authoritative execution sequence. A planned status is not implementation evidence. The merged repository and committed tests/evidence are authoritative; uncertain closure is marked **requires confirmation**.

GD63 records approved decisions and reconciles planning documentation only. It does not activate the GD62 graph or implement migration, economy, routing, additional floors, content, or UI. The save schema remains version 6, and the existing ordered two-room models remain runtime and save authorities.

## 2. Current repository baseline after GD62

### Implemented capabilities

Merged history establishes the following at prototype scope:

- Deterministic/config-owned core simulation foundations, heat and mana feedback, placement effects, composition-driven outcomes, and localized player feedback.
- A simple MVP screen and Bootstrap validation surface supporting a build/run/inspect/adjust loop.
- Room, monster, trap, and loot choices; deterministic loot; a minimal research bridge and player-completable research activity.
- Persistent room-slot assignments, selection and fit feedback, two-room expansion, ordered room traversal, and GD60 ordered two-room outcome/effect resolution.
- Player-facing intent, party, run previews, analysis, next-action guidance, contract completion, spoils ledger, pacing improvements, save/lifecycle hardening, and development-build readiness.
- Automated deterministic, save, localization, presenter, journey, and route regressions. Exact manual closeout coverage remains tied to the committed evidence record.

### Partially implemented capabilities

- **Layout:** GD62 provides inactive tile/footprint, floor, room, corridor, node, and edge contracts plus a pure deterministic validator with stable reason codes 1–39. Those inactive contracts still use independent configured floor-space costs, lack rectangular floor bounds, and represent only corridor edges—not footprint-free direct doorways—so GD64 alignment is required before content or migration design. Two ordered room slots still own runtime route order; the graph is not active or authoritative.
- **Construction:** choices and replacement exist, but spatial placement, corridors, structural removal/renovation, and mana-backed costs do not.
- **Research:** a minimal bridge and completion flow exist; Architecture branching/expansion and a meaningful research interface do not.
- **Economy/offline:** prototype mana/heat flows exist; construction spending, floor expansion, and production-grade offline behavior remain incomplete or require confirmation.
- **UI:** the simple screen is usable for validation, but Bootstrap remains a temporary control/diagnostic dependency and is not a production dungeon editor.
- **Saves:** lifecycle integrity is hardened for current models; the GD62 migration proposal exists, but final mapping is not approved and no graph-version migration is implemented. Schema version remains 6.

### Missing MVP capabilities

Active physical footprints and overlap enforcement; authoritative runtime floor capacity; corridor structures; entrance/exit nodes; saved same-floor route graphs; reachability validation; structural editing and containment resolution; construction economy; one optional branch and route decision; Floor 2 transition; graphical editor parity; broader room/environment/content choices; Architecture progression; onboarding/accessibility; mobile profiling; and external fun-test evidence.

### Deferred capabilities

Curved/freeform corridors, intra-floor elevations, teleporters, secret rooms, locked-door puzzles, procedural generation, unrestricted mazes, complex pathfinding AI, multiple entrances/descents, advanced environmental simulation, floor-to-floor backtracking, full production art, diplomacy depth, live operations, prestige, social/competitive systems, monetization implementation, and speculative backend/release dashboards.

### Immediate risks and controls

| Risk | Control |
|---|---|
| Ordered slots become a second, incompatible layout model | Define Spec 38 contracts first; migrate once; retain a compatibility adapter only as needed. |
| Save breakage or nondeterministic graph ordering | Approve version/migration plan before schema changes; stable IDs, canonical ordering, legacy fixtures. |
| Two capacity systems disagree | One occupied physical tile is both geometry authority and one floor-space unit; mana price and content capacities remain separate authored concepts. |
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

### Phase 0 — GD61 planning reset (complete)

**Packets:** (1) audit/reconciliation and authority banners; (2) Spec 38; (3) this roadmap and matrices.

**Status: complete.** Required documents agreed on baseline, authority, scope, and sequence; documentation validation passed. The fantasy questions remain explicit and fun was not claimed as proven.

### Phase 1 — Spatial domain foundation (in progress)

1. **GD62 — Spatial contracts and validator foundation (complete but inactive):** tile coordinates, rectangular and straight-corridor footprints, floor/room/corridor/node/edge contracts, stable IDs, ordinal canonical ordering, pure deterministic validation with stable reason codes 1–39, and Unity JSON round-trip coverage. Its capacity and edge contracts predate the GD63 decisions and require GD64 alignment.
2. **GD63 — Approved decisions and planning reconciliation (current documentation packet):** lock spatial, editing, route, progression, and MVP-content direction; document the inactive GD62 delta; reconcile Spec 38 and migration planning.
3. **GD64 — Inactive spatial contract and validator alignment:** add or approve rectangular floor-boundary representation; derive used space from occupied physical tiles; remove, replace, deprecate, or strictly validate independent floor-space costs; and represent direct doorways separately from physical corridors. Preserve canonical ordering and reason values 1–39, append new reasons, add focused EditMode tests, and keep the graph inactive. No schema, runtime-authority, UI, tuning, content, or migration changes. Final class names and architecture require implementation review.
4. **GD65 — Inactive MVP spatial content contract and export validation:** after GD64, approve authored identities, IDs, rectangular footprints, connection points, capacities, and schema/foreign-key/export validation.
5. **GD66 — Final save/migration design gate:** after GD65 content exists, approve stable ID derivation, deterministic straight-line coordinates/orientations, direct-doorway mapping, fixtures, missing-content/fallback policy, backup, rollback, and recovery design without migrating live state.

**Phase boundary:** Phase 1 ends with GD66 design approval. It does not change schema, migrate legacy state, switch runtime readers, transition writable authority, or provide migration implementation evidence. Those actions belong exclusively to Phase 2.

**Exit:** aligned inactive contracts and approved content represent the MVP layout; deterministic validation and exports pass; and final migration design is reviewed before persistence changes. **Technical gate:** focused contract/validator and schema tests; no gameplay constants or player-facing literals. **Fantasy gate:** paper/prototype review may test whether capacity and fit explanations are understandable; correctness does not prove fun.

### Phase 2 — Backward-compatible migration

Phase 2 exclusively owns migration implementation and authority transition:

1. Change the save schema and migrate/adapter-map legacy ordered two-room state to entrance → rooms → completion.
2. Switch runtime readers to canonical graph order and transition to one writable authority while preserving current player flow and outcomes.
3. Add legacy/current fixtures, idempotence, corrupt-state policy, rollback, and migration/recovery evidence.

**Exit:** old saves load without losing assignments; current saves round-trip; identical legacy inputs preserve deterministic route behavior; no dual writable authority remains. **Technical gate:** migration matrix, journey, route and save tests. **Fantasy gate:** existing build/run/inspect flow shows no regression.

### Phase 3 — Structural editing rules

1. Place rooms and the MVP corridor with footprint, capacity, connection and reachability preview.
2. Replace/renovate/remove structures; explicitly resolve contents and attached corridors using approved policies.
3. Expose deterministic validation reasons via localization-backed presentation contracts (Bootstrap may host temporary diagnostics only).

**Exit:** Floor 1 supports Entrance Hall, Basic Room, Rectangle Room, Large Chamber, the Narrow Hall / Straight Stone Corridor category, a required route, and Completion Terminal; valid edits persist and invalid edits are atomic. **Technical gate:** boundary, overlap, reachability, connection, containment and ordering tests. **Fantasy gate:** players can predict fit failures and compare compact versus spreading layouts in a low-fidelity test.

### Phase 4 — Mana-backed construction and offline mana completion

1. Data-author room/corridor build, renovation, removal/refund and expansion policies.
2. Apply transactional mana spending and Architecture/floor/theme modifiers through the formula framework.
3. Add localized cost, remaining-capacity, consequence and affordability previews before commit.
4. Complete and evidence idle offline mana using the configured percentage in Spec 29’s locked single-grant calculation: no offline time cap, with mana storage capacity as the output clamp. Research may improve efficiency percentage, storage capacity, or Mana Farm production, not eligible duration. Explain elapsed time, effective rate, storage-cap clamp, and awarded mana; retain the overnight-one-edit statement only as a non-authoritative balance hypothesis.

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
| Undead monster family | Partial or requires confirmation; starter content is not evidence of full launch-family breadth | Author and validate the launch family | 8, 9 | Required |
| Goblinoid monster family | Requires confirmation | Author and validate the launch family | 8, 9 | Required |
| Boss-set constraint | Optional content; no boss-set completion evidence is asserted | If another authoritative content specification requires a boss set, validate it without exceeding the cap | 8, 9 | Not independently required; no more than one boss set per monster family |
| Mana generation | Partial; prototype mana feedback/simulation exists | Confirm locked formulas, integration and final journey evidence | 4, 9 | Required |
| Idle offline mana | Partial or requires confirmation; no completion claim from GD61 | Complete and evidence calculation before final validation | 4, 9 | Required |
| Peace, Notice and Concern heat states | Partial; prototype heat system exists, exact three-state journey evidence requires confirmation | Validate only the three locked MVP states | 8, 9 | Required; more than three excluded |
| Five adventurer classes | Partial or requires confirmation | Complete/validate exactly five MVP classes in content and runs | 8, 9 | Required |
| Loot progression through Steel | Partial; deterministic loot/spoils exist but tier breadth requires confirmation | Author/validate progression through Steel | 8, 9 | Required |
| One research slot | Implemented at prototype scope through the single-slot lifecycle/research bridge and GD59; final integration still required | Architecture progression/UI and final journey evidence | 5, 8, 9 | Required; retain one slot |
| One Mana Farm sub-dungeon | Remaining; historical planning deferred it until core-loop stability | Implement one type after main spatial and construction economy stabilize | 8, 9 | Required; more than one excluded |

**Locked explicit MVP exclusions:** prestige; seasonal events; PvP or leaderboards; hero adventurers; more than one sub-dungeon; advanced diplomacy; more than three heat states; and boss sets beyond one per family. GD61 does not amend these exclusions.

**Boss-set constraint:** The MVP may include no more than one boss set per monster family. A boss set is not independently required unless another authoritative content specification explicitly requires it.

## 8. Genuine open decisions (owners and decision gates)

Approved policy is recorded in the [GD63 decision record](gd63-spatial-and-progression-design-decisions.md), not repeated as unresolved. The following implementation/data gates remain open:

| Question | Needed by | Required evidence/owner |
|---|---|---|
| Exact legal bounds, buildable/unavailable-tile unlock model, expansion/modifier behavior, room/corridor footprints, content capacities, connection points, final content IDs, costs and modifiers | GD64 alignment / GD65 content contract | Design/Data; authored content and schema validation, never runtime constants |
| Stable textual ID derivation, default migration coordinates/orientations, exact legacy fixtures, fallback IDs and missing-content policy | GD66 migration design gate | Engineering/Data; deterministic fixture and recovery review |
| Invested construction/renovation mana representation; refund percentage, rounding and clamping | Before Phase 2/4 implementation | Design/Data/Save; reviewed data contract |
| Doorway geometry and placement validation details | Before Phase 3 implementation | Design/Engineering; deterministic validation cases |
| Narrow branch selection formula and tie-break | Phase 5 | Design/Data; deterministic test cases |
| Exact exit/descent transfer fields, coefficients, thresholds, tie-break and save representation | Phase 6 | Design/Engineering; cross-spec review |
| Offline efficiency percentage, timestamp/clock-manipulation safeguards and rounding; storage-cap tuning where otherwise authorized | Phase 4 | Design/Data/Save; preserve Spec 29 no-time-cap and storage-clamp policy |
| Exact active-floor/content/device workload limits within the maximum five floors | Phases 6/8/9 | Design/QA data decision and device profiling |

## 9. Next packet recommendation

**GD63: Lock approved spatial design decisions and reconcile the post-GD62 plan.** Keep the packet documentation-only. It records approved policy without choosing tuning, keeps the inactive graph non-authoritative, and establishes the dependency-correct sequence GD63 → GD64 inactive contract/validator alignment → GD65 content/export validation → GD66 final migration design → Phase 2 migration implementation. No player-facing behavior changes, and fun remains for the Phase 3, 5, 7, and 9 observation gates.
