# Post-GD60 MVP Execution Plan

**Current Phase 3 status (2026-09-01):** Phase 3B2B implements deterministic player-usable leaf deletion, schema-8 identity retirement and returned-content custody, standalone-safe removal-policy injection, detached preview/atomic commit, localized Bootstrap consequences, and Phase 3 closeout. Phase 3 exit criteria are implementation-complete pending external Unity validation; Phase 4 remains unimplemented.

**Superseded Phase 3 status (2026-08-27):** PR #197 / Phase 3B1 is merged and complete at `8341108124899c985849563fbc8421623af5bc66`. Phase 3B2 is split into exactly two substantive packets unless new evidence forces another split: Phase 3B2A owns schema-8 identity-lifecycle and returned-content ownership prerequisites; Phase 3B2B owns player-usable leaf deletion and Phase 3 closeout. Phase 4 remains blocked until Phase 3B2B and all Phase 3 exit criteria pass.**

**Historical Phase 2B6A status:** PR #194 is merged at `2bcc336f5fbbb9797f6f319f738e7b9f7d0613bd`; detached candidate, transaction, recovery, activation preflight, and Windows durability qualification are complete. Phase 2B6A adds the Windows durability implementation and activation-preflight boundary. It supports only Windows Editor and Windows Standalone with a local, nonredirected NTFS save directory. Durable creation uses a write-through file handle and explicit file-buffer flush; same-directory moves/replacements use `SetFileInformationByHandle(FileRenameInfo)` on a source handle opened with `DELETE | GENERIC_WRITE`, `OPEN_EXISTING`, and `FILE_FLAG_WRITE_THROUGH`, followed by `FlushFileBuffers` on the renamed handle and source/destination verification. No directory-fsync equivalent is claimed, and storage hardware that falsely acknowledges cache flushes remains outside the OS contract. Unsupported platforms, filesystem types, redirected/reparse paths, invalid paths, and native probe failures return stable fail-closed capability codes and no filesystem. Windows Editor and Windows Standalone durability qualification passed for PR #194; any activated schema-7 lifecycle still requires its own owner validation. Live schema remains **6**, schema 7 remains inactive, and `SaveService`, `GameRoot`, native creation, canonical runtime readers/writers, and legacy authority remain unchanged. Phase 2B6B is the final activation packet; GD66 is not complete.


| Field | Decision |
|---|---|
| Status | **Phase 3A / PR #196 merged and complete; Phase 3B1 movement and replacement implemented in this PR** |
| Historical approval baseline | Main through merged PR #179 / GD65B1 at `917b763dc0e5315fdd5d835da4b5f5de43f9ba59` |
| Current implementation baseline | Main at merged PR #196 `b6bd4a2dfa85a1b8899c617dd0d1982a91a879c1` |
| Supersedes | Sprint 2-4 execution order, post-GD9 sequence, and earlier vertical-slice forecasts |
| Spatial authority | [System Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) |
| Last reconciled | 2026-08-21 |

**Historical GD65B5 final status:** Implementation and required owner validation passed at `c5eefae61e9bf3b7bf0a200e343f383f0122743b` in PR #186. PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. The production spatial catalog remains inactive, existing runtime/save authority is unchanged, and save schema remains 6.

## 1. Authority and purpose

This is the single active dependency order from the GD60 prototype to a fun MVP, a usable graphical editor, and external testing. Older sprint plans, closeouts, evidence records, and vertical-slice plans remain historical traceability: they retain useful acceptance criteria, but are **not** the authoritative execution sequence. A planned status is not implementation evidence. The merged repository and committed tests/evidence are authoritative; uncertain closure is marked **requires confirmation**.

Historically, GD64 completed the inactive spatial contract and layout-validator alignment, and GD65A completed the inactive serializable spatial content schema plus bounded deterministic export validation and canonicalization. Later GD65B packets supplied production records and composition, and GD66/PR #195 activated schema 7. Validated canonical spatial state now owns writable route topology/content; the older ordered-room models remain frozen migration evidence rather than writable authority. Additional floors and native R1→R2 structural construction remain later-phase work.

**Historical GD65B0C7 approval scope:** Merged PRs #168–#176 progressively approved the GD65B0 register, and PR #177 / GD65B0C6 completed pipeline-ownership rows 59–65 at baseline `e1bae81649e73452c76946689b93ba48eaebcb7d`. GD65B0C7 approves rows 66–70 and 72: exact configuration-owned workload envelopes, future-scale protection, and complete production-pipeline test/evidence ownership. All 72 rows are now approved. This documentation adds no file, code, test, record, or activation and preserves abstract MVP placement selections, ordered-room layout, and room-slot assignments as runtime/save authority. GD65B1 is complete and remaining GD65B implementation is unblocked.

## 2. Repository baseline and current implementation state

### Implemented capabilities

Merged history establishes the following at prototype scope:

- Deterministic/config-owned core simulation foundations, heat and mana feedback, placement effects, composition-driven outcomes, and localized player feedback.
- A simple MVP screen and Bootstrap validation surface supporting a build/run/inspect/adjust loop.
- Room, monster, trap, and loot choices; deterministic loot; a minimal research bridge and player-completable research activity.
- Persistent room-slot assignments, selection and fit feedback, two-room expansion, ordered room traversal, and GD60 ordered two-room outcome/effect resolution.
- Player-facing intent, party, run previews, analysis, next-action guidance, contract completion, spoils ledger, pacing improvements, save/lifecycle hardening, and development-build readiness.
- Automated deterministic, save, localization, presenter, journey, and route regressions. Exact manual closeout coverage remains tied to the committed evidence record.

### Partially implemented capabilities

- **Layout:** GD64 established rectangular floor-bound, occupied-tile capacity, direct-doorway, physical-corridor, bounded canonicalization, and deterministic validation contracts. GD66 now projects the validated canonical R1/R2 graph as live route authority. Native structural editing, corridor construction, and R1→R2 construction remain deferred to Phase 3.
- **Construction:** choices and replacement exist, but spatial placement, corridors, structural removal/renovation, and mana-backed costs do not.
- **Research:** a minimal bridge and completion flow exist; Architecture branching/expansion and a meaningful research interface do not.
- **Economy/offline:** prototype mana/heat flows exist; construction spending, floor expansion, and production-grade offline behavior remain incomplete or require confirmation.
- **UI:** the simple screen is usable for validation, but Bootstrap remains a temporary control/diagnostic dependency and is not a production dungeon editor.
- **Saves:** schema 7 is live. Schemas 1–6 migrate through the production compatibility profile and raw-before-legacy transaction/recovery path; native canonical creation, exact-byte canonical writes, reopen, duplicate no-op, Narrow Hall repair, and Delete Save quiescence are validated. Windows activation remains fail-closed outside the qualified platform/filesystem boundary.

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

### Phase 1 — Spatial domain foundation (complete through approved GD66; runtime remains inactive)

1. **GD62 — Spatial contracts and validator foundation (complete but inactive):** tile coordinates, rectangular and straight-corridor footprints, floor/room/corridor/node/edge contracts, stable IDs, ordinal canonical ordering, pure deterministic validation with stable reason codes 1–39, and Unity JSON round-trip coverage. Its capacity and edge contracts predate the GD63 decisions and require GD64 alignment.
2. **GD63 — Approved decisions and planning reconciliation (complete):** lock spatial, editing, route, progression, and MVP-content direction; document the inactive GD62 delta; reconcile Spec 38 and migration planning.
3. **GD64 — Inactive spatial contract and validator alignment (complete):** rectangular floor bounds, occupied-tile-union capacity, distinct footprint-free direct doorways and physical corridors, explicit workload limits, stable reason values 1–45, and deterministic invalid-payload preservation are merged through PR #166. The graph remains inactive.
4. **GD65A — Inactive spatial content schema and deterministic export validation (complete through PR #167):** the Unity-serializable export envelope, metadata, floor/room/corridor/fixed-endpoint/socket/connection contracts, pure caller-bounded validation, foreign-key checks, detached ordinal canonicalization, JSON tests, and test-only fixtures are merged. No production authored values, registration, or consumer exists.
5. **GD65B0 — Production spatial content authority and pipeline decision record (complete after GD65B0C7):** PRs #168–#177 established and progressively approved the [approval register](gd65b-production-spatial-content-approval.md). GD65B0C7 approves final rows 66–70 and 72, closing all 72 rows with exact configuration-owned limits, an 80-floor test-only scalability contract, and named unit/export/recovery/loading/pre-build/scalability test and evidence ownership. Approval documentation creates no production file, record, code, test, activation, save change, or Unity evidence.

   The generic `OptionalBranchAllowance` field supports future nonnegative per-floor authored values and is not schema-capped at 1. Current MVP production content and active MVP behavior remain limited to at most one optional branch per floor; values above 1 remain post-MVP scope.
6. **GD65B1 — Production spatial workload configuration and scalability foundation (complete in PR #179):** implemented the separately authored limits asset, strict parser/conversion boundary, and initial workload/scalability tests without activation.
7. **GD65B2A — Production authoring-source contract (approved documentation packet):** approves normalized version-controlled CSV/schema authority at `ContentAuthoring/DungeonSpatial/`; creates no package or Unity evidence.
8. **GD65B2B — Implement normalized production spatial authoring package and approved Floor 1 records (complete in PR #181):** created the schema and normalized table package, authored only the approved Floor 1 production records and English entries, and added source-package parsing and validation tests without activating runtime/save spatial authority.
9. **GD65B2C / PR #182 — Deterministic generated output construction (complete):** constructs, canonically serializes, reparses, and validates the exact three-file set in memory.
10. **GD65B3A / PR #183 — Recoverable publication core (complete):** adds journaled staging, backup, installation, recovery, refresh, installed-set validation, and cleanup without runtime activation.
11. **GD65B3B / PR #184 — Export invocation and committed set (complete):** added the shared editor-menu/command-line invocation and committed the exact three generated production JSON files. Its historical evidence gaps are reconciled by the stronger final GD65B5 evidence.
12. **GD65B4 / PR #185 — Production loading and composition assignment (complete):** loads and atomically publishes the validated inactive catalog through the approved explicit composition boundary without activating gameplay.
13. **GD65B5 / PR #186 — Pre-build recovery, validation, and closeout (complete; merged):** all player builds are gated, required evidence passed, PR #186 merged, and GD65B is closed.
14. **GD66 — Final save/migration design gate (approved in merged PR #187):** after GD65B content exists, approve stable ID derivation, deterministic straight-line coordinates/orientations, direct-doorway mapping, fixtures, missing-content/fallback policy, backup, rollback, and recovery design without migrating live state.

**Phase boundary:** Phase 1 ends with GD66 design approval. It does not change schema, migrate legacy state, switch runtime readers, transition writable authority, or provide migration implementation evidence. Those actions belong exclusively to Phase 2.

**Exit:** aligned inactive contracts and approved content represent the MVP layout; deterministic validation and exports pass; and final migration design is reviewed before persistence changes. **Technical gate:** focused contract/validator and schema tests; no gameplay constants or player-facing literals. **Fantasy gate:** paper/prototype review may test whether capacity and fit explanations are understandable; correctness does not prove fun.

### Phase 2 — Backward-compatible migration (complete in PR #195 pending merge)

Phase 2 implementation and required validation passed at `c4ba1f68985c18c2a6a62bcfd84c217e0cf07b06`. It owns the completed migration implementation and authority transition:

1. Change the save schema and migrate/adapter-map legacy ordered two-room state to entrance → rooms → completion.
2. Switch runtime readers to canonical graph order and transition to one writable authority while preserving current player flow and outcomes.
3. Add legacy/current fixtures, idempotence, corrupt-state policy, rollback, and migration/recovery evidence.

**Exit:** old saves load without losing assignments; current saves round-trip; identical legacy inputs preserve deterministic route behavior; no dual writable authority remains. **Technical gate:** migration matrix, journey, route and save tests. **Fantasy gate:** existing build/run/inspect flow shows no regression.

### Phase 3 — Structural editing rules

**Packet status:** Phase 3A construction and Phase 3B1 movement/replacement are complete. Phase 3B2A locks the prerequisite contracts; Phase 3B2B alone implements deletion. Issued structural IDs are permanently retired. Per-floor room and edge allocation use persistent monotonic state. Removed logical edges are never reused, and a new predecessor-to-Completion relationship receives a fresh edge identity. Reusable owned content cannot be silently destroyed. Floor 1 must retain at least one buildable required-route room during Phase 3.

Phase 3B2B leaf deletion is the deterministic inverse of tail construction. It identifies the final removable player-built room and predecessor by graph identity, preserves the Completion Terminal and Completion node identities, and derives the terminal placement from the predecessor's authored outgoing connection geometry. Exactly one Direct Doorway or approved Straight Stone Corridor solution is required; zero or multiple solutions fail. There is no nearest repair, A*, arbitrary placement search, or unrelated movement, and validation/commit remain atomic.

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
| GD65B production records, pipeline implementation, tests, and evidence | GD65B before GD66 | Implement the fully approved GD65B0 contract; QA/Data/Engineering/primary-developer evidence at `docs/testing/evidence/gd65b/`; never use runtime constants or test defaults as production values |
| Buildable/unavailable-tile unlock model, expansion/modifier behavior, costs and modifiers | Later spatial progression/construction packets | Design/Data; reviewed configured content and formulas, never runtime constants |
| Stable textual ID derivation, default migration coordinates/orientations, exact legacy fixtures, fallback IDs and missing-content policy | GD66 migration design gate | Engineering/Data; deterministic fixture and recovery review |
| Invested construction/renovation mana representation; refund percentage, rounding and clamping | Before Phase 2/4 implementation | Design/Data/Save; reviewed data contract |
| Doorway geometry and placement validation details | Before Phase 3 implementation | Design/Engineering; deterministic validation cases |
| Narrow branch selection formula and tie-break | Phase 5 | Design/Data; deterministic test cases |
| Exact exit/descent transfer fields, coefficients, thresholds, tie-break and save representation | Phase 6 | Design/Engineering; cross-spec review |
| Offline efficiency percentage, timestamp/clock-manipulation safeguards and rounding; storage-cap tuning where otherwise authorized | Phase 4 | Design/Data/Save; preserve Spec 29 no-time-cap and storage-clamp policy |
| Exact active-floor/content/device workload limits within the maximum five floors | Phases 6/8/9 | Design/QA data decision and device profiling |

## 9. Current dependency packet

**GD66 implementation and required validation are complete in PR #195 pending merge; GD65B is closed through merged PR #186.** PR #177 / GD65B0C6 is merged at baseline `e1bae81649e73452c76946689b93ba48eaebcb7d`. GD65B0C7 approves final rows 66–70 and 72, leaving the register at 72 `APPROVED` and zero rows in every unresolved status. The exact future limits are 128 top-level records, 512 nested records, 4,096 tiles per individual footprint/bounds, 256 issues, and 32,768 characters, solely owned by `validation_limits.json`. The future pipeline suite includes unit, export, transaction recovery, loading, pre-build, and 80-floor scalability-contract stages.

GD65B supplied the separately authored spatial-content limits, production records, deterministic generated-set construction, recoverable publication, committed outputs, inactive loading/composition, and build-entry gate through merged PR #186. GD66 was approved in merged PR #187. PR #195 now supplies the distinct approved production save-workload profile and completes Phase 2 migration/authority activation: `LatestSchemaVersion = 7`, `LegacyCompatibilitySchemaVersion = 6`, schemas 1–6 migrate to canonical schema 7, and canonical spatial state is the sole writable spatial authority. Required validation passed at `c4ba1f68985c18c2a6a62bcfd84c217e0cf07b06`; merge of PR #195 is the remaining repository-integration step. Phase 3 retains native structural editing and R1→R2 construction.


### Historical GD65B2A reconciliation amendment

Main is reconciled through merged PR #182 at `803a9b86984a96890c372d364c20d0149eebf107`. GD65B1 and GD65B2B are complete, and GD65B2A remains the approved source contract. PR #182 completed deterministic in-memory generated-set construction and strict reparse; GD65B3A adds recoverable publication/recovery without runtime/save activation. Later MVP phase scope is unchanged. GD66 remains blocked until all GD65B records, stages, tests, generated outputs, and evidence are complete.

### Historical GD65B2B status

GD65B2B implements the normalized authoring package, the approved Floor 1 records, strict editor-only parsing/projection, and focused source-package tests. GD65B2A remains the architecture contract. PR #182 completed deterministic runtime-output serialization and strict reparse/revalidation, and GD65B3A completes the recoverable publication/recovery core. Production loading/composition assignment, pre-build gating, and complete evidence remain later GD65B packets. The catalog remains inactive, save schema remains 6, runtime/save authority is unchanged, and GD66 remains blocked.

### Historical deterministic generated-set step

PR #182 constructs the exact catalog, English table, and domain manifest in memory and strictly reparses, validates, canonicalizes, and byte-compares the complete set. GD65B3A adds recoverable filesystem publication/recovery but no generated committed file or editor/command-line entry point. Loading/assignment, pre-build gating, evidence closeout, GD66, migration, and activation remain incomplete. The catalog remains inactive, save schema remains 6, and existing runtime/save authority is unchanged.


### Historical GD65B3A recoverable publication status

PR #182 completed deterministic in-memory construction and strict complete-set reparse of the three approved generated outputs. GD65B3A adds the editor-only recoverable publication and recovery core with a strict durable journal, canonical three-file installation, complete-set readback validation, and deterministic failure injection. Editor-menu and Unity command-line entry points remain absent, and generated production files remain uncommitted. Production loading, `GameRoot` assignment, pre-build gating, and complete evidence remain incomplete. Save schema remains 6, the production spatial catalog remains inactive, runtime/save authority is unchanged, and GD66 remains blocked.

## Historical GD65B3B implementation status

**Historical PR #184–#185 status, reconciled by PR #186:** PR #184 / GD65B3B merged at `04515d5c7c5a35d869bb725cd76d2a7c317403ee`, and PR #185 subsequently completed strict inactive loading and explicit composition. The former PR #184 evidence gaps are fully reconciled in `docs/testing/evidence/gd65b/validation-evidence.md`. GD65B5 implementation and required validation passed at `c5eefae61e9bf3b7bf0a200e343f383f0122743b`; PR #186 is merged; GD65B is closed and GD66 was subsequently approved in merged PR #187. The catalog remains inactive, runtime/save authority is unchanged, and save schema remains 6.
