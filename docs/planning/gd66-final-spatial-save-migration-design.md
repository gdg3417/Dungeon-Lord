# GD66 final spatial save and migration design

## 1. Status and approval boundary

**Status: CANDIDATE FOR APPROVAL in PR #187.** This documentation-only packet is based on merged PR #186 at `7f62709c9c73164c549ee31a403c410f8c05c902`. Until PR #187 is approved and merged, save schema remains **6**, production Dungeon Spatial content remains **inactive**, and the existing ordered two-room route models, independent `dungeonLayout` economic structures, and `structureRuntime` remain their current runtime/save authorities. No migration, serialized-shape change, runtime activation, or writable-authority transition occurs here. Phase 2 exclusively owns implementation after this design merges.

Labels below are **Fact**, **Observed**, **GD66 decision**, **Unsupported**, and **Phase 2**.

## 2. Current repository and save baseline

| Item | Reconciled state |
|---|---|
| Repository | `main` through merged PR #186 at `7f62709c9c73164c549ee31a403c410f8c05c902`; GD66 is a candidate for approval in PR #187 |
| Save root | `SaveRoot.schemaVersion`; `SaveMigration.LatestSchemaVersion = 6` |
| Route topology | ordered MVP representations; no spatial graph authority |
| Economic structures | `dungeonLayout` placements plus `structureRuntime`, concurrently active and independent of route topology |
| Spatial content/layout | production catalog loaded behind an inactive boundary; `FloorSpatialLayout` contains rooms/nodes/edges only |

## 3. Sources inspected

This packet reconciles `AGENTS.md`, the active status/planning files, Specs 00/19/28/29/38 and the invariant glossary; `SaveService`, both `SaveMigration` classes, `SaveRoot`, `SaveData`, migration/lifecycle fixtures; the three MVP route representations and their writers/readers; `GameRoot`, `PlacementService`, `StructureSimulationPass`, and structure configuration; spatial contracts, validators and canonical ordering; normalized authoring tables, generated production JSON; and commit history that introduced schema versions 1–6 and each legacy field. Geometry is derived from committed production records, never display names.

## 4. Current legacy data models and observed runtime behavior

| Subsystem/model | Observed persistence and runtime behavior |
|---|---|
| `mvpRoomSlotAssignments` | Room option plus monster/trap/loot arrays per `(FloorIndex, RoomIndex)`; current persisted normalization groups by room index and uses last list record; active route/run readers consume these values. |
| `mvpDungeonFloorLayout` | Nodes keyed by floor/node, category/option and `Revision`; compatibility readers can overlay missing categories from placements. |
| `mvpDungeonPlacements` | Category/option entries with `Revision`; supports one-room pre-room-slot outcomes and supplies floor-node backfill. |
| `dungeonLayout` | Independent nonspatial economic-structure slots. `GameRoot` writes them through `PlacementService` and `StructureSimulationPass` reads them during the active MVP loop. Supported current IDs include `structure.mana_generator.basic`, `structure.heat_scrubber.basic`, and `structure.risk_lab.basic`. |
| `structureRuntime` | Independent mutable mana/heat/flags consumed with `dungeonLayout`; it is not room topology or graph content. |
| ordinary save path | Normalization/backfill occurs during `LoadOrCreate`/`MigrateToLatest`; configured temporary writes, post-write backups, and corruption fallback do not prove a recoverable spatial migration transaction. |

**GD66 decision:** `dungeonLayout` and `structureRuntime` must survive the complete candidate byte-for-byte in semantic value and remain writable by their existing subsystem after route migration. They are not lower route authorities, do not generate route conflict diagnostics, and receive no speculative spatial mapping.

## 5. Approved whole-model route-topology precedence

Only competing route/room-topology representations participate:

1. `mvpRoomSlotAssignments`
2. `mvpDungeonFloorLayout`
3. `mvpDungeonPlacements`

The highest semantically present route representation is the sole route migration input. No cross-model category or record overlay is permitted. Lower route representations are diagnostics/rollback evidence only. A valid winner is not rejected when a lower route model disagrees (`gd66.route.lower_authority_conflict`, nonfatal); an invalid winner fails without fall-through.

`dungeonLayout` is expressly outside this precedence. Repository history provides no evidence that `placement.option.room.basic` or another room option was persisted as a `DungeonSlot.StructureId`; therefore GD66 approves no historical `dungeonLayout` route fixture or room mapping. Any later evidence must identify an exact source schema and persisted ID in a separately reviewed amendment.

## 6. Semantic-presence rules

Presence is captured from raw JSON member evidence before constructors, field initializers, `MigrateToLatest`, starter-node creation, backfill, filtering, or option fallback:

| Route model | Absent | Present |
|---|---|---|
| assignments | member absent/null; `Rooms` absent/null/empty; only null elements | any non-null room record, including malformed/out-of-range records |
| floor layout | member absent/null; or exact empty starter shell: expected four Floor 0 nodes, expected slot IDs/indices, blank category/option, revision 0, no extras | any assignment, nonzero revision, unexpected/missing/extra identity, or malformed non-null node |
| placements | member absent/null; `Entries` absent/null/empty; only null elements | any non-null entry, including malformed IDs/revisions |

An entirely absent route produces no active spatial floor. It does not manufacture endpoints or an implicit Basic room. Independently present `dungeonLayout`/`structureRuntime` never makes route topology present.

## 7. Duplicate and conflict rules

Strings use ordinal equality/sorting without trimming or case folding.

| Model | Duplicate key | Resolution |
|---|---|---|
| assignments | `(FloorIndex, RoomIndex)` | fatal `gd66.route.duplicate_room_slot`; no per-record revision exists, so current last-list-record behavior is not durable authority |
| floor layout | `(FloorIndex, NodeIndex)` or nonempty `SlotId` | uniquely greatest nonnegative `Revision` wins; a tied maximum is fatal `gd66.route.duplicate_floor_node_revision` |
| placements | `CategoryId` | uniquely greatest nonnegative `Revision` wins; a tied maximum is fatal `gd66.route.duplicate_category_revision` |

Candidate floor, room, fixed, content-assignment, node, or edge ID collisions fail `gd66.id.collision`. Valid concurrent economic structures are neither duplicates nor conflicts with route state.

## 8. Exact schemas 1 through 6 fixture matrix

Commit history establishes these writer eras: v1 predates the wrapped v2 dungeon-layout foundation; v2 introduced/backfilled `dungeonLayout` and then `structureRuntime`; v3 added `mvpDungeonPlacements`; v4 added `mvpDungeonFloorLayout`; v5 added `mvpRoomSlotAssignments`; v6 changed no route representation. Raw member evidence—not what the current initializer would synthesize—controls each fixture.

`O` is original, `B` verified backup, `C` complete future candidate. `E`, `R1`, and `R2` are §12 geometry. Every successful legacy fixture migrates once to the as-yet-unselected future target schema; only a complete future-target payload is an already-current no-op.

| Source | Fields that may genuinely exist / historical default | Required exact fixtures and winner | Expected result; preserved independent state; transaction behavior |
|---|---|---|---|
| v1 | unwrapped or early root `SaveData`; no repository evidence of an MVP route member; no route default is inferred | `v1-unwrapped-empty`, `v1-corrupt`, `v1-unwrapped-with-unknown-extra` → no winner | empty route → `gd66.success.no_layout`; exact non-route fields preserved. Corrupt → `gd66.payload.unreadable`. Unwrapped source schema must be classified as v1, never current. No graph commit for no-layout. |
| v2 | `dungeonLayout`; later v2 payloads may include `structureRuntime`; migration backfilled empty 5×6 structure slots and runtime shell | `v2-empty-structures`, `v2-three-supported-structures`, `v2-malformed-structure-slot`; no route winner | no spatial route. Preserve exact supported structure placement/runtime state and continued writability; malformed independent structure state is handled by its existing subsystem, not converted to route. No route conflict or graph commit. |
| v3 | v2 fields plus `mvpDungeonPlacements`; null collection/list and `NextRevision<1` were backfilled | `v3-empty-placement-member`, `v3-basic-room0`, `v3-basic-plus-content`, `v3-narrow-hall`, `v3-duplicate-revision`, each crossed with populated v2 structures; placements wins when present | empty → no layout. Basic → R1 and room-content migration. Narrow Hall → unmapped. Duplicate → fatal. Always preserve structures/runtime. Attempted migration: B; success one C; failure O remains; identical retry deterministic. |
| v4 | v3 fields plus `mvpDungeonFloorLayout`; initial empty starter nodes were backfilled; later backfill could derive nodes from placements | `v4-empty-starter`, `v4-floor-basic`, `v4-floor-and-placement-agree`, `v4-floor-and-placement-conflict`, `v4-floor-duplicate`, with/without supported structures; floor wins only when raw-present | exact raw empty starter lets placements win or no route. Valid floor Basic → R1. Lower placements only diagnostic. Invalid present floor fails without placements fallback. Structures/runtime preserved. |
| v5 | v4 fields plus `mvpRoomSlotAssignments`; null rooms/list and revision shell backfilled | `v5-empty-assignments`, `v5-room0-basic`, `v5-two-basic`, `v5-assignments-and-lower-agree`, `v5-assignments-and-lower-conflict`, `v5-duplicate-room`, `v5-room-gap`, `v5-content-over-capacity`, with populated structures/runtime | raw-empty assignments do not block floor/placements. Present assignments wins: R1/R2 plus exact content. Narrow Hall/unmapped, duplicate/gap/capacity failures leave O. Structures/runtime unchanged and writable. |
| v6 | same route fields as v5; current normalizer can synthesize all shells and overlay floor nodes from placements | all v5 cases as v6 plus `v6-normalized-shell-vs-raw-absence`, `v6-interrupted-before-replace`, `v6-interrupted-after-replace`, `v6-retry-failed-candidate` | raw evidence prevents synthesized fields becoming winner. A legacy v6 success migrates exactly once; it is not “current-version idempotence.” Journal recovery selects verified O or verified complete C. |
| future target | saved floor binding, fixed structures, room contents, complete graph, target schema and canonical marker all present | `target-complete-current`, `target-marker-missing`, `target-schema-missing`, `target-partial-graph`, `target-contradictory-legacy-write` | complete verified target → `gd66.success.already_committed`, no new backup/write. Any incomplete/contradictory target recovers verified O/C or fails `gd66.transaction.unrecoverable`; never legacy-fallbacks silently. |

Every schema also requires malformed ID, missing/invalid production Basic definition, ID collision, corrupt JSON, backup-verification failure, durable-verification failure, retry, and lower-route agreement/conflict variants where that schema can contain the relevant route field.

## 9. Semantic compatibility route

Supported topology is distinct entrance → internal legacy Room 0 → optional internal legacy Room 1 → distinct completion. Localized “Room 1/Room 2” labels never define identity. Room 1 without Room 0 fails `gd66.route.gap`. Empty route evidence produces no floor graph.

## 10. Content compatibility mapping

| Legacy value | Decision |
|---|---|
| `placement.option.room.basic` | direct room mapping to verified `spatial.room.basic` (4×4; production capacities 2 monster, 2 trap, 2 loot) |
| implicit Basic created only by current resolver/default | no migration input; raw absence remains no route |
| blank/malformed room option | `gd66.content.invalid_legacy_room` |
| `placement.option.room.narrow_hall` | `gd66.content.unmapped_legacy_room`; it is a room, never `spatial.corridor.straight_stone`, Rectangle Room, or another substitute |
| `dungeonLayout` structure IDs | no room mapping; preserve independently, including the three supported economic IDs |

No safe fallback is approved. Missing mapped production definition fails `gd66.content.missing_production_room`; an invalid record fails `gd66.content.invalid_production_room`. O remains active and no partial graph/content state is published.

## 11. Stable ID derivation and saved floor binding

Canonical identifier inputs must match lowercase ASCII `[a-z0-9]+(?:[._-][a-z0-9]+)*`; indices use invariant zero-based `D2`; separators are literal dots; all comparisons are ordinal.

| Identity | Template |
|---|---|
| floor instance | `compat.floor.{floorIndex:D2}` → `compat.floor.00` |
| entrance fixed instance/node | `{floorId}.fixed.entrance`; `{floorId}.node.entrance` |
| room instance/node | `{floorId}.legacy-room.{roomIndex:D2}`; `{floorId}.node.legacy-room.{roomIndex:D2}` |
| completion fixed instance/node | `{floorId}.fixed.completion`; `{floorId}.node.completion` |
| direct edge | `{floorId}.edge.direct.{sourceRole}.{destinationRole}` |
| reserved physical corridor instance/node/edge | `{floorId}.corridor.{sourceRole}.{destinationRole}`; `{floorId}.node.corridor.{sourceRole}.{destinationRole}`; `{floorId}.edge.corridor.{segment:D2}.{sourceRole}.{destinationRole}` |
| room-content assignment | `{roomInstanceId}.content.{category}.{sequence:D4}` where category is `monster`, `trap`, or `loot` |

Runtime hashes, `GetHashCode`, GUIDs, timestamps, localization/UI text, player numbering, catalog positions, dictionary iteration, and incidental collection order are prohibited.

### Future saved floor contract

**Phase 2 serialized owner:** `SaveData.spatialFloors`, an array of `SavedSpatialFloor`. Each record owns `FloorInstanceId`, `FloorDefinitionId`, `FloorIndex`, `Layout`, `FixedStructures`, and `RoomContents`. For compatibility Floor 0 these are `compat.floor.00`, `spatial.floor.01`, and `0`.

Records canonicalize by `FloorIndex`, then ordinal `FloorInstanceId`; duplicate index or instance ID is fatal. `FloorDefinitionId` resolves ordinally in the validated production catalog and its authored `FloorIndex` must equal the saved index. Rooms, fixed structures, content assignments, nodes and edges must all reference that record's `FloorInstanceId`; cross-floor references fail. Missing/invalid definitions fail before C is committed. Migration derives the binding only from the approved compatibility target, never catalog position. The first Phase 2 serialized-shape PR must add and round-trip this inactive binding together with fixed structures and room contents.

## 12. Exact coordinate and orientation proof

Production `spatial.floor.01` is `[0,12)×[0,12)`, capacity 60, with no unavailable mask. All compatibility instances use `Zero` orientation.

### R1

| Role / definition | Anchor | Canonical occupied tiles / derivation | Sockets | Use / cumulative |
|---|---|---|---|---:|
| entrance / `spatial.fixed.entrance_hall` | (0,0) | (0,0),(0,1),(1,0),(1,1),(2,0),(2,1) | route (1,1) North | 6 / 6 |
| room0 / `spatial.room.basic` | (0,2) | x=0..3, y=2..5, X then Y | south (1,2); north (1,5) | 16 / 22 |
| completion / `spatial.fixed.completion_terminal` | (1,6) | (1,6),(1,7),(2,6),(2,7) | route (1,6) South | 4 / 26 |

### R2

| Role / definition | Anchor | Canonical occupied tiles / derivation | Sockets | Use / cumulative |
|---|---|---|---|---:|
| entrance | (0,0) | as R1 | route (1,1) North | 6 / 6 |
| room0 | (0,2) | x=0..3, y=2..5 | south (1,2); north (1,5) | 16 / 22 |
| room1 / `spatial.room.basic` | (0,6) | x=0..3, y=6..9 | south (1,6); north (1,9) | 16 / 38 |
| completion | (1,10) | (1,10),(1,11),(2,10),(2,11) | route (1,10) South | 4 / 42 |

All tiles are in bounds and X-then-Y canonical order; disjoint Y bands prove no overlap; reserved-offset tables are empty; 26 and 42 are ≤60 and include fixed structures. Paired standard-passage sockets at (1,1)/(1,2), (1,5)/(1,6), and (1,9)/(1,10) are adjacent and oppositely faced. Identical source semantics produce identical layout.

## 13. Direct-doorway and physical-corridor decisions

Every supported connection is a required `DirectDoorway` with blank `CorridorDefinitionId`, null footprint, empty optional-branch ID, and §11 edge ID. No supported legacy route uses a physical corridor. Reserved corridor IDs require a later exact, separately proven mapping.

## 14. Fixed-structure and canonical room-content persistence requirements

### Fixed structures

Each `SavedSpatialFloor.FixedStructures` record contains `FixedStructureInstanceId`, `FixedStructureDefinitionId`, `FloorInstanceId`, `Anchor`, `Orientation`, and semantic kind Entrance/Completion. It canonicalizes by ordinal instance ID and rejects duplicates.

### Nonspatial room contents

**Phase 2 serialized owner:** `SavedSpatialFloor.RoomContents`, a `FloorRoomContentState` containing `Assignments` and `NextSequence`. Each `RoomContentAssignment` contains `AssignmentId`, `RoomInstanceId`, `CategoryId`, `OptionId`, and nonnegative `Sequence`. This is a separate writable **room-content authority**, not route-topology authority and not tile occupancy. It adds no monster/trap/loot coordinates.

- Categories are exactly `placement.category.monster`, `.trap`, and `.loot_node`; room options remain owned by room instances.
- Records canonicalize by ordinal `RoomInstanceId`, fixed category rank monster/trap/loot, ascending `Sequence`, then ordinal `AssignmentId` and `OptionId`.
- `AssignmentId` uses §11. Duplicate IDs or duplicate `(RoomInstanceId, CategoryId, Sequence)` fail. `NextSequence` must exceed every sequence and future writers allocate it monotonically; deletion never renumbers survivors.
- Option IDs must be exact known legacy gameplay options in the stated category. Blank, malformed, category-wrong, or missing configured effect/capacity references fail; there is no fallback.
- Counts per room/category must not exceed the mapped production room's Monster/Trap/Loot capacity. Capacity failure is `gd66.content.room_capacity_exceeded`.
- Assignments migration copies `MonsterOptionIds`, `TrapOptionIds`, and `LootNodeOptionIds` from each winning room in exact stored array order into increasing sequence values. Array index is source evidence only; the resulting explicit sequence becomes authority.
- Winning floor nodes or placements have at most one resolved item per non-room category. They migrate to Room 0 in current category order using sequences 0 per category. A category without a record remains empty. The room option itself maps separately to the room instance.
- Candidate validation compares the ordered legacy route projection and canonical room-content projection, including option multiplicity/order and run-input sequence, so current deterministic run outcomes are preserved. Any unrepresentable difference fails rather than silently changing outcomes.
- Before atomic replacement, legacy readers/writers remain active. After replacement, run readers project ordered content from `RoomContents`; placement writers target only `RoomContents`; topology writers target only graph/floor records. Legacy route/content fields become read-only rollback evidence. `dungeonLayout` economic writers remain independent and active.
- Rollback atomically restores O, returning route and room-content readers/writers together to legacy authority.

The first Phase 2 shape PR must round-trip floor binding, fixed structures, and room contents while inactive, before any candidate builder.

## 15. Raw-load interception and candidate validation sequence

The spatial transaction runs **before** existing `MigrationRunner`, `SaveMigration.MigrateToLatest`, field initializers/backfills, or runtime normalization can erase presence evidence:

1. Read exact O bytes.
2. Recover/classify any spatial migration journal using hashes and complete-payload checks.
3. Parse an envelope that retains the actual root schema token/version and raw JSON member-presence map.
4. Capture raw presence/null/array-element evidence for all route fields and preserve exact independent structure fields.
5. If payload is unwrapped `SaveData`, classify it by the historical shape/evidence as v1 (or fail ambiguous-source classification); never assign current schema merely because `TryParseSaveRoot` wraps it.
6. Select the route winner from raw evidence.
7. Preserve `dungeonLayout`, `structureRuntime`, and every unrelated field in detached C.
8. Build the complete saved floor, fixed, room-content and graph candidate.
9. Canonicalize and validate IDs, definition binding/index, mappings, contents/capacity/outcome projection, footprints, bounds, overlap, endpoints, adjacency/sockets, reachability, terminal semantics and ordering.
10. Commit C or restore O under §16; only afterward expose normalized runtime state.
11. For a successful future-target payload, run only target-schema normalization that cannot rewrite authority. For no-layout/no-migration legacy payloads, existing migrations may run after raw evidence is retained. Existing legacy migration must never run before candidate selection.

## 16. Atomic migration transaction

There is exactly one payload replacement and it is the sole schema and route/room-content writable-authority switch:

1. Read/preserve O and compute transaction ID `gd66-{sha256 of exact O bytes in lowercase hex}`.
2. Create, flush, reread, deserialize and hash-verify `{activeFile}.migration.{transactionId}.original` as B.
3. Construct detached complete C.
4. Put every required future field, selected future target schema, and canonical authority marker in C.
5. Canonicalize, serialize, deserialize and fully validate C before active replacement.
6. Persist C with one journaled same-directory atomic replacement and required durability operations.
7. Treat that replacement—not a later marker write—as the sole schema and writable-authority transition.
8. Reread and hash/semantic-verify durable committed bytes.
9. If durable verification fails, atomically recover verified O; do not expose C.
10. Never perform a second schema-marker or authority-marker write.
11. Never commit graph/content fields without their target schema and marker.
12. Never expose staging, journal, or partial candidate data to gameplay.

The sidecar journal records transaction ID, O/C hashes and stage solely for recovery; it is never layout or room-content authority.

## 17. Writable-authority transition

Before replacement, legacy route and legacy room-content fields are the writable authorities; the detached candidate has none. The single atomic replacement simultaneously makes the complete canonical floor/graph the sole route-topology authority and `RoomContents` the sole room-content authority. Readers and writers bind once at load from the canonical marker and never fall back during a session. Legacy route/content fields become read-only evidence. `dungeonLayout`/`structureRuntime` remain the separate writable economic-structure subsystem on both sides of the replacement.

Marker/schema/graph/content disagreement is `gd66.authority.contradictory_state` and triggers verified recovery. Simultaneous legacy/canonical route writers, simultaneous legacy/canonical room-content writers, split category authority, or a second marker write are prohibited.

## 18. Schema-version policy

Current schema is 6. GD66 neither increments it nor approves a specific future number. Phase 2 must select a version greater than the then-current `LatestSchemaVersion` only when the complete serialized floor/fixed/content/marker shape is finalized. That selected version appears only in fully validated C and becomes active only through §16's single replacement.

## 19. Failure reason-code taxonomy

Append-only stable families are: `gd66.payload.*` (unreadable, ambiguous_unwrapped_schema); `gd66.route.*` (duplicate, range, gap, lower conflict); `gd66.id.*` (malformed, collision); `gd66.floor.*` (missing_definition, invalid_definition, index_mismatch, duplicate); `gd66.content.*` (invalid/unmapped room, invalid/category-wrong/missing option, duplicate_assignment, room_capacity_exceeded, outcome_mismatch); `gd66.geometry.*`; `gd66.graph.*`; `gd66.transaction.*` (backup, candidate, commit, durable verification, interrupted, unrecoverable); `gd66.authority.contradictory_state`; and `gd66.success.no_layout`, `.migrated`, `.already_committed`, `.recovered_original`.

## 20. Player messaging and localization ownership

Save/Migration Engineering emits codes; UX/Localization owns future `save.migration.spatial.<reason>.player_message` mappings and language tables. GD66 adds no entry or English. IDs, UI labels, localization keys and localized text never determine migration identity or gameplay.

## 21. Backup, rollback, retry, and idempotence

O stays active until the one replacement. Failed C never changes authority. Retry of identical O reuses verified B/transaction identity and produces byte-identical C. Recovery chooses only verified O or complete verified C. A complete future-target C is the only already-current no-op. Rollback restores O atomically and switches topology/content readers and writers together; independent economic structures restore as part of O without conversion. Backup retention remains configuration-owned Phase 2 policy, but B cannot be pruned before transaction finalization.

## 22. Phase 2 dependency breakdown

1. Add and round-trip the inactive `SavedSpatialFloor` binding, fixed structures, `FloorRoomContentState`, complete canonical marker, and transaction metadata shape; verify then-current schema and select the target version.
2. Add raw-byte/root-version/member-presence interception before all existing legacy normalization, including unwrapped v1 classification.
3. Implement pure schema-specific route selection and complete detached candidate construction with exact fixtures in §8.
4. Implement definition/content/outcome/geometry/graph canonical validation and stable codes.
5. Implement verified B, journal, one atomic C replacement, durable reread and O recovery with failure injection.
6. In one activation packet, bind canonical topology and room-content readers/writers after replacement while preserving independent economic-structure readers/writers.
7. Add localization mappings and full lifecycle/edit-mode immediate-save, recovery, EditMode/PlayMode/build evidence before activation approval.

## 23. Explicit non-goals

No migration, save field, exact future schema number, code, fixture/test, content, localization, runtime activation, structure spatial mapping, corridor gameplay, cost, Floor 2, UI, tuning, scene/asset/settings/package change, or fun claim is included.

## 24. Acceptance checklist

- [x] Candidate status while PR #187 is open; schema 6/current authorities unchanged.
- [x] Route precedence excludes independent `dungeonLayout`/`structureRuntime`.
- [x] Exact nonspatial canonical room-content ownership precedes migration builder.
- [x] Saved floor instance binds explicitly to production definition/index.
- [x] Raw presence precedes every current normalization/backfill.
- [x] Schema-specific v1–v6 fixtures and true future-target idempotence are defined.
- [x] R1/R2 production geometry remains proven.
- [x] One complete atomic replacement is the sole schema/authority switch.
- [x] No hidden fallback, partial publication, localized identity, or dual route/content writers.

## 25. Candidate approval statement

PR #187 proposes approval of this route precedence, independent economic-structure preservation, canonical room-content and saved-floor contracts, raw-load interception, schema-specific fixtures, stable identities/geometry, and one-replacement transaction for later Phase 2 implementation. It is not repository-approved until merged and changes no present behavior. Unsupported states retain O with stable diagnostics; Phase 2 remains blocked until this candidate is approved and merged.
