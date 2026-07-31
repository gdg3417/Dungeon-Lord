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

The highest semantically present representation is the sole **topology winner**. Lower models never add rooms, room identity, endpoints, edges, or geometry. An invalid winner fails without topology fall-through. `dungeonLayout` remains expressly outside this precedence; repository history supplies no approved room ID in `DungeonSlot.StructureId`.

### Frozen effective-content compatibility projection

After topology selection and before candidate construction, Phase 2 must run a named `FrozenLegacyRouteProjection` stage using the exact resolver behavior applicable to the raw source schema:

1. Resolve winner revisions/duplicates first.
2. Compute the current ordered runtime projection without mutating O or invoking current backfills early.
3. Permit lower route models to contribute **room-content evidence only** when that schema's historical resolver makes the record effective—for example, v4–v6 floor nodes completed by effective placement categories.
4. Never let lower records change topology, room option/identity, explicit-versus-implicit semantics, or geometry; never revive filtered, invalid, or superseded records. An effective lower room record is representable only as exact agreement with the winner; otherwise it fails `gd66.content.outcome_mismatch` (including lower Narrow Hall or malformed room data).
5. Copy effective lower content into canonical `RoomContents` and emit `gd66.diagnostic.lower_effective_content_contributed`. Ineffective disagreement emits `gd66.diagnostic.lower_ineffective_conflict` and remains evidence only.
6. Emit agreement only when candidate topology, content multiplicity/order, explicit-room semantics, and ordered run inputs equal the frozen projection.
7. If an effective lower contribution cannot be represented exactly, fail `gd66.content.outcome_mismatch`; effective content is never silently discarded.

Thus lower disagreement is not blanket-nonfatal: only an ineffective conflict is nonfatal. Duplicate/revision ambiguity in either effective source fails before projection.

## 6. Semantic-presence and canonical empty-state rules

Presence is captured from raw JSON member evidence before constructors, initializers, `MigrateToLatest`, starter nodes, backfill, filtering, or fallback:

| Route model | Absent | Present |
|---|---|---|
| assignments | member absent/null; `Rooms` absent/null/empty; only null elements | any non-null record, including malformed/out-of-range records |
| floor layout | member absent/null; or exact empty starter shell with four expected blank Floor 0 nodes/revision 0/no extras | any assignment, nonzero revision, unexpected/missing/extra identity, or malformed non-null node |
| placements | member absent/null; `Entries` absent/null/empty; only null elements | any non-null entry, including malformed IDs/revisions |

Raw route absence creates no room, endpoint, or `SavedSpatialFloor`, but **does migrate authority**. Detached C contains the selected future schema, canonical authority marker, `spatialFloors = []`, and every unrelated field including `dungeonLayout`/`structureRuntime`. Empty canonical validation requires all graph, fixed, and room-content collections to be absent because there is no floor record. C is backed up, verified, and atomically committed exactly like a populated candidate, emitting `gd66.success.empty_migrated`. The replacement makes empty canonical topology/content writable authority; future construction starts canonically, never in legacy fields. Reload emits `gd66.success.already_committed`. No implicit room or endpoint-only graph is created.

## 7. Duplicate and conflict rules

Strings use ordinal equality/sorting without trimming or case folding.

| Model | Duplicate key | Resolution |
|---|---|---|
| assignments | `(FloorIndex, RoomIndex)` | fatal `gd66.route.duplicate_room_slot`; no per-record revision exists, so current last-list-record behavior is not durable authority |
| floor layout | `(FloorIndex, NodeIndex)` or nonempty `SlotId` | uniquely greatest nonnegative `Revision` wins; a tied maximum is fatal `gd66.route.duplicate_floor_node_revision` |
| placements | `CategoryId` | uniquely greatest nonnegative `Revision` wins; a tied maximum is fatal `gd66.route.duplicate_placement_revision` |

Candidate floor, room, fixed, content-assignment, node, or edge ID collisions fail `gd66.id.collision`. Valid concurrent economic structures are neither duplicates nor conflicts with route state.

## 8. Exact schemas 1 through 6 fixture matrix

`O`, `B`, and `C` mean original, verified backup, and complete candidate. Every valid empty schema emits `gd66.diagnostic.no_legacy_route`, then migrates to canonical `spatialFloors = []` through B and one atomic replacement; failures keep O; every successful reload emits `gd66.success.already_committed`.

### Schema 1 envelope fixtures

A wrapped v1 payload is a JSON object with an ordinal `schema = "save_root"`, integral `schemaVersion = 1`, and a present non-null `primary` object. An unwrapped v1 is a JSON object lacking root-envelope members but containing at least one recognized `SaveData` member. Raw bytes, recognized values, and unknown members are preserved in O/B; C must preserve all semantically representable unrelated values, and Phase 2 must use a lossless extension-data mechanism for unknown members or fail `gd66.payload.unknown_member_unpreservable`.

| Fixture | Expected classification/result |
|---|---|
| `v1-wrapped-valid-primary` | `gd66.diagnostic.payload_wrapped_v1`; empty canonical commit, `gd66.success.empty_migrated` |
| `v1-wrapped-empty-primary` | wrapped diagnostic; empty canonical commit |
| `v1-wrapped-null-primary` | `gd66.payload.null_primary`, fatal; B verified, O active, retry after repair |
| `v1-wrapped-missing-schema-token` | `gd66.payload.missing_schema_token`, fatal; do not reinterpret as unwrapped |
| `v1-wrapped-malformed-schema-token` | `gd66.payload.invalid_schema_token`, fatal |
| `v1-unwrapped-save-data` | `gd66.diagnostic.payload_unwrapped_v1`; preserve recognized/unknown members; empty canonical commit |
| `v1-unwrapped-empty-save` | unwrapped diagnostic only when an approved recognized member exists; empty canonical commit |
| `v1-unwrapped-unknown-members` | `gd66.payload.ambiguous_envelope` unless at least one recognized `SaveData` member disambiguates; if disambiguated, unknown-member preservation rule applies |
| `v1-ambiguous-envelope` | `gd66.payload.ambiguous_envelope`, fatal |
| `v1-corrupt-json` | `gd66.payload.unreadable`, fatal |

Wrapping an unwrapped payload in detached memory never assigns schema 6. Its raw classification/source version remains v1 through candidate validation.

### Schema eras and required fixtures

| Source | Genuine fields | Empty and populated fixtures / expected behavior |
|---|---|---|
| v2 | `dungeonLayout`; later `structureRuntime`; no route representation | `v2-empty`, `v2-supported-economic-structures`, `v2-malformed-economic-slot`: valid route-empty cases commit canonical empty authority; independent structures/runtime preserved and remain writable. |
| v3 | v2 plus `mvpDungeonPlacements` | `v3-empty`, Basic R1, Narrow Hall, duplicate revision, invalid/category-wrong content, every content-only and explicit-Basic combination below; all valid empty/content cases commit C. |
| v4 | v3 plus `mvpDungeonFloorLayout` and starter shells | all v3 cases plus floor Basic; floor+placement agreement; floor room plus lower monster/trap/loot; partial floor categories completed by placements; effective/ineffective conflicts; duplicate ambiguity; lower content without explicit room; lower Narrow Hall/malformed room. |
| v5 | v4 plus `mvpRoomSlotAssignments` | all v4 cases plus R2, duplicate room, route gap, assignment arrays, content over capacity; raw-empty assignments allow historical floor/placement projection before canonical commit. |
| v6 | same route representations as v5 | all v5 cases plus normalized-shell-vs-raw-absence, interruption before/after replacement, retry, and contradictory marker. v6 migrates once; it is not already-current. |
| future target | complete target schema/marker and canonical collections | complete populated or empty C → `gd66.success.already_committed`; missing schema/marker or partial state → contradictory/recovery codes. |

### Content-combination fixture set

For every applicable v3, v4, v5, and v6 winner, executable Phase 2 fixtures must cover: monster only; trap only; loot only; monster+trap; monster+loot; trap+loot; monster+trap+loot; explicit Basic plus each of those seven combinations; no explicit room plus effective lower-model content; each category over capacity; invalid option; category-wrong option. V4–v6 additionally cover floor room plus each lower category, partial floor categories completed by placements, agreement, ineffective disagreement, effective disagreement, revision ambiguity, lower content without explicit room, and lower Narrow Hall/malformed room.

Expected supported content-only geometry is R1. Exact content order/multiplicity and explicit/implicit semantics must equal `FrozenLegacyRouteProjection`. Capacity/invalid/unrepresentable cases fail their exact §19 code. Every schema also covers missing/invalid profile/content definitions, ID collision, corrupt JSON, backup/journal/candidate/commit/durability/recovery failure, retry, and populated independent economic state.

## 9. Semantic compatibility route and content-only compatibility

Normal supported topology is entrance → Room 0 → optional Room 1 → completion. Room 1 without Room 0 fails `gd66.route.gap`; labels never define identity.

A semantically present winner (or effective lower content admitted by `FrozenLegacyRouteProjection`) with supported monster/trap/loot content but no explicit room creates the R1 Basic room solely as an **implicit compatibility content container**. This emits `gd66.diagnostic.missing_explicit_room_supported_content` and `gd66.diagnostic.implicit_basic_container_created`.

Canonical owner `SavedSpatialFloor.RoomContents.RoomSemantics` contains exactly one record per room with `RoomInstanceId` and `LegacyRoomOriginKind`, whose allowed values are `ExplicitLegacyRoom` and `ImplicitCompatibilityContainer`. Records canonicalize by ordinal room ID; missing/duplicate semantics fail candidate validation. The field round-trips and participates in frozen-projection equality.

An implicit container does not enter the ordered room-placement effect projection and never invents Basic-room simulation effects; its contents retain historical ordering/outcomes. This is a narrow historical rule for valid supported content, not fallback for a blank/malformed/unmapped/missing explicit room ID. Narrow Hall stays unmapped. Any unsupported or outcome-changing projection fails `gd66.content.outcome_mismatch`.

## 10. Content compatibility mapping

| Legacy value | Decision |
|---|---|
| explicit `placement.option.room.basic` | direct `spatial.room.basic`; `LegacyRoomOriginKind=ExplicitLegacyRoom` |
| supported content with no room record | approved R1 Basic container; `LegacyRoomOriginKind=ImplicitCompatibilityContainer`; no explicit-room effect |
| blank/malformed explicit room | `gd66.content.invalid_legacy_room`; never implicit-container fallback |
| `placement.option.room.narrow_hall` | `gd66.content.unmapped_narrow_hall`; never corridor/Rectangle/substitute |
| `dungeonLayout` structure IDs | no room mapping; preserved independently |

No general safe fallback exists. Production/profile resolution and content errors use exact §19 codes; O remains active on failure.

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

### Injected compatibility-geometry profile

**GD66 decision:** all compatibility geometry/mapping values are owned by an injected typed `SpatialMigrationCompatibilityProfile`; candidate-building code contains no fallback constants. The future single writable authority is the version-controlled typed configuration asset `Assets/_Project/Data/Production/DungeonSpatial/spatial_migration_compatibility_profiles.json`, added only by Phase 2 after its schema review. It is configuration, not the production spatial-definition catalog or a generated output.

Each profile owns: ordinal `ProfileId`; positive `ProfileVersion`; inclusive source-schema minimum/maximum; target `FloorDefinitionId` and floor index; entrance definition, anchor, orientation, connection-point ID; ordinal legacy-room-to-production-definition mappings; Room 0/Room 1 anchors and orientations for R1/R2; completion definition, R1/R2 anchors, orientation and connection-point ID; ordered semantic route roles; direct-doorway source/destination role and socket definitions; expected socket type; and expected occupied totals 26/42 as assertions independently recomputed from definitions.

Initial profile values are exactly: entrance `(0,0)/Zero/route`; Basic Room 0 `(0,2)/Zero`; Basic Room 1 `(0,6)/Zero`; R1 completion `(1,6)/Zero/route`; R2 completion `(1,10)/Zero/route`; target `spatial.floor.01` index 0; definitions and sockets in §12. These values must not also appear as runtime constants.

The loader validates and canonicalizes profiles by source minimum, source maximum, profile version, then ordinal profile ID. Overlapping applicable ranges or duplicate `(ProfileId,ProfileVersion)` fail. Exactly one compatible profile must match the raw source schema; missing, duplicate/overlap, malformed/referentially invalid, and unsupported-version cases use exact §19 codes. References resolve ordinally against the separately validated production catalog; orientations, sockets, mappings, bounds, footprints, capacity totals, and route roles must validate before injection. The composition root injects the one validated immutable profile into the candidate builder; the builder cannot discover files or construct a default.

Profile versions are immutable compatibility contracts: edits that change migration output require a new version plus explicit source applicability; old versions remain available for deterministic retry/recovery as long as saves/journals reference them. Phase 2 tests must parse/canonical-round-trip the profile and independently recompute R1/R2 tiles, totals, adjacency, facings, bounds and capacity from profile plus production definitions.

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

**Phase 2 serialized owner:** `SavedSpatialFloor.RoomContents`, a `FloorRoomContentState` containing `Assignments`, `RoomSemantics`, and `NextSequence`. Each `RoomContentAssignment` contains `AssignmentId`, `RoomInstanceId`, `CategoryId`, `OptionId`, and nonnegative `Sequence`. This is a separate writable **room-content authority**, not route-topology authority and not tile occupancy. It adds no monster/trap/loot coordinates.

- Categories are exactly `placement.category.monster`, `.trap`, and `.loot_node`; room options remain owned by room instances.
- Records canonicalize by ordinal `RoomInstanceId`, fixed category rank monster/trap/loot, ascending `Sequence`, then ordinal `AssignmentId` and `OptionId`.
- `AssignmentId` uses §11. Duplicate IDs or duplicate `(RoomInstanceId, CategoryId, Sequence)` fail `gd66.content.duplicate_assignment`. `RoomSemantics` has exactly one ordinally sorted record per canonical room; missing, duplicate, unknown-kind, or projection-inconsistent records fail `gd66.content.room_semantics_invalid`. `NextSequence` must exceed every sequence and future writers allocate it monotonically; deletion never renumbers survivors.
- Option IDs must be exact known legacy gameplay options in the stated category. Blank, malformed, category-wrong, or missing configured effect/capacity references fail; there is no fallback.
- Counts per room/category must not exceed the mapped production room's Monster/Trap/Loot capacity. Capacity failure is `gd66.content.room_capacity_exceeded`.
- Assignments migration copies `MonsterOptionIds`, `TrapOptionIds`, and `LootNodeOptionIds` from each winning room in exact stored array order into increasing sequence values. Array index is source evidence only; the resulting explicit sequence becomes authority.
- Winning floor nodes or placements have at most one resolved item per non-room category. They migrate to Room 0 in current category order using sequences 0 per category. A category without a record remains empty. The room option itself maps separately to the room instance.
- Candidate validation compares the ordered legacy route projection and canonical room-content projection, including option multiplicity/order and run-input sequence, so current deterministic run outcomes are preserved. Any unrepresentable difference fails rather than silently changing outcomes.
- Before atomic replacement, legacy readers/writers remain active. After replacement, run readers project ordered content from `RoomContents`; placement writers target only `RoomContents`; topology writers target only graph/floor records. Legacy route/content fields become read-only rollback evidence. `dungeonLayout` economic writers remain independent and active.
- Rollback atomically restores O, returning route and room-content readers/writers together to legacy authority.

The first Phase 2 shape PR must round-trip floor binding, fixed structures, room assignments, and explicit/implicit `RoomSemantics` while inactive, before any candidate builder.

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
11. For a successful future-target payload, run only target-schema normalization that cannot rewrite authority. For a valid route-empty legacy payload, build and atomically commit canonical empty C before exposing runtime state; existing legacy normalization never retains legacy writable authority. Existing legacy migration must never run before candidate selection.

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

## 19. Authoritative exact reason-code table

This table is the sole code registry. Codes are append-only and ordinal. “O active” is state immediately after emission.

| Exact code | Classification | Continue C? | O active? | Player key? | Exact condition |
|---|---|---:|---:|---:|---|
| `gd66.diagnostic.payload_wrapped_v1` | nonfatal diagnostic | Yes | Yes | No | Valid wrapped schema-1 root classified. |
| `gd66.diagnostic.payload_unwrapped_v1` | nonfatal diagnostic | Yes | Yes | No | Recognized unwrapped schema-1 `SaveData` classified. |
| `gd66.payload.unreadable` | fatal failure | No | Yes | Yes | JSON bytes cannot be parsed. |
| `gd66.payload.ambiguous_envelope` | fatal failure | No | Yes | Yes | Payload cannot be safely classified wrapped/unwrapped. |
| `gd66.payload.missing_schema_token` | fatal failure | No | Yes | Yes | Root-shaped payload omits `schema`. |
| `gd66.payload.invalid_schema_token` | fatal failure | No | Yes | Yes | Root-shaped payload has wrong/malformed schema token or nonintegral version. |
| `gd66.payload.null_primary` | fatal failure | No | Yes | Yes | Wrapped root `primary` is explicitly null. |
| `gd66.payload.unknown_member_unpreservable` | fatal failure | No | Yes | Yes | Unknown source member cannot be losslessly retained. |
| `gd66.diagnostic.no_legacy_route` | nonfatal diagnostic | Yes | Yes | No | Valid legacy payload has no semantic route; empty C begins. |
| `gd66.success.empty_migrated` | success | No | No | No | Canonical empty C durably committed. |
| `gd66.success.migrated` | success | No | No | No | Populated canonical C durably committed. |
| `gd66.success.already_committed` | success | No | No | No | Complete verified target payload already authoritative. |
| `gd66.success.recovered_original` | success | No | Yes | Yes | Verified O restored after interruption/failure. |
| `gd66.route.duplicate_room_slot` | fatal failure | No | Yes | Yes | Duplicate assignment floor/room key. |
| `gd66.route.duplicate_floor_node_revision` | fatal failure | No | Yes | Yes | Floor-node key/slot has tied greatest revision. |
| `gd66.route.duplicate_placement_revision` | fatal failure | No | Yes | Yes | Placement category has tied greatest revision. |
| `gd66.route.record_out_of_range` | fatal failure | No | Yes | Yes | Floor, room, node, slot, revision, or sequence is out of range. |
| `gd66.route.gap` | fatal failure | No | Yes | Yes | Room 1 exists without Room 0. |
| `gd66.diagnostic.lower_model_agreement` | nonfatal diagnostic | Yes | Yes | No | Effective lower projection exactly agrees. |
| `gd66.diagnostic.lower_ineffective_conflict` | nonfatal diagnostic | Yes | Yes | No | Lower disagreement is historically filtered/ineffective. |
| `gd66.diagnostic.lower_effective_content_contributed` | nonfatal diagnostic | Yes | Yes | No | Historical resolver makes lower content effective and it is copied. |
| `gd66.diagnostic.missing_explicit_room_supported_content` | nonfatal diagnostic | Yes | Yes | No | Supported effective content has no explicit room. |
| `gd66.diagnostic.implicit_basic_container_created` | nonfatal diagnostic | Yes | Yes | No | R1 implicit Basic container created without room effect. |
| `gd66.content.outcome_mismatch` | fatal failure | No | Yes | Yes | Canonical projection cannot preserve effective run inputs/semantics exactly. |
| `gd66.content.invalid_legacy_room` | fatal failure | No | Yes | Yes | Explicit room ID is blank, malformed, or category-wrong. |
| `gd66.content.unmapped_narrow_hall` | fatal failure | No | Yes | Yes | Effective explicit Narrow Hall has no room mapping. |
| `gd66.content.missing_production_room` | recoverable failure | No | Yes | Yes | Mapped production room definition is absent. |
| `gd66.content.invalid_production_room` | recoverable failure | No | Yes | Yes | Mapped production room definition fails validation. |
| `gd66.content.invalid_option` | fatal failure | No | Yes | Yes | Content option is blank, malformed, unknown, or lacks required config. |
| `gd66.content.category_mismatch` | fatal failure | No | Yes | Yes | Content option does not belong to recorded category. |
| `gd66.content.room_capacity_exceeded` | fatal failure | No | Yes | Yes | Per-room category count exceeds mapped room capacity. |
| `gd66.content.duplicate_assignment` | fatal failure | No | Yes | Yes | Assignment ID or room/category/sequence duplicates. |
| `gd66.content.room_semantics_invalid` | fatal failure | No | Yes | Yes | Room semantics missing, duplicated, or inconsistent with frozen projection. |
| `gd66.floor.missing_definition` | recoverable failure | No | Yes | Yes | Saved/profile floor definition is absent. |
| `gd66.floor.invalid_definition` | recoverable failure | No | Yes | Yes | Target floor definition fails production validation. |
| `gd66.floor.index_mismatch` | fatal failure | No | Yes | Yes | Saved/profile index differs from authored floor index. |
| `gd66.floor.duplicate_binding` | fatal failure | No | Yes | Yes | Floor instance ID or index duplicates. |
| `gd66.profile.missing` | recoverable failure | No | Yes | Yes | No profile applies to raw source schema. |
| `gd66.profile.duplicate` | fatal failure | No | Yes | Yes | Profile identity/version duplicates or applicability overlaps. |
| `gd66.profile.invalid` | recoverable failure | No | Yes | Yes | Profile structure, mapping, geometry, socket, total, or reference is invalid. |
| `gd66.profile.version_mismatch` | recoverable failure | No | Yes | Yes | Journal/source requires unsupported profile version. |
| `gd66.id.malformed` | fatal failure | No | Yes | Yes | Persistent ID violates exact grammar. |
| `gd66.id.collision` | fatal failure | No | Yes | Yes | Any canonical persistent ID collides. |
| `gd66.geometry.bounds` | fatal failure | No | Yes | Yes | Occupied tile lies outside profile target floor. |
| `gd66.geometry.overlap` | fatal failure | No | Yes | Yes | Physical footprints overlap. |
| `gd66.geometry.capacity` | fatal failure | No | Yes | Yes | Tile union exceeds capacity or expected total differs. |
| `gd66.geometry.socket` | fatal failure | No | Yes | Yes | Connection point/socket type/facing is incompatible. |
| `gd66.geometry.adjacency` | fatal failure | No | Yes | Yes | Direct-doorway endpoints are not adjacent. |
| `gd66.graph.endpoint` | fatal failure | No | Yes | Yes | Entrance/completion endpoint missing, duplicate, or wrong kind. |
| `gd66.graph.reachability` | fatal failure | No | Yes | Yes | Required semantic route is unreachable. |
| `gd66.graph.terminal` | fatal failure | No | Yes | Yes | Completion-terminal semantics are invalid. |
| `gd66.graph.ordering` | fatal failure | No | Yes | Yes | Candidate is not canonical/round-trip stable. |
| `gd66.transaction.backup_failed` | recoverable failure | No | Yes | Yes | B creation, flush, readback, hash, or parse verification fails. |
| `gd66.transaction.journal_invalid` | recoverable failure | No | Yes | Yes | Journal is missing, malformed, or hash-inconsistent. |
| `gd66.transaction.candidate_invalid` | fatal failure | No | Yes | Yes | Complete C serialization/deserialization or aggregate validation fails. |
| `gd66.transaction.commit_failed` | recoverable failure | No | Yes | Yes | Single atomic replacement fails. |
| `gd66.transaction.durability_failed` | recoverable failure | No | Yes | Yes | Committed bytes fail durable reread/hash/semantic verification. |
| `gd66.transaction.recovery_failed` | fatal failure | No | Yes | Yes | Neither verified O nor complete verified C can be restored. |
| `gd66.authority.contradictory_state` | recoverable failure | No | Yes | Yes | Schema, marker, canonical fields, or writer evidence disagree. |

## 20. Player messaging and localization ownership

Save/Migration Engineering emits only exact §19 codes. For each row with “Player key?” Yes, the key is `save.migration.spatial.` followed by the full code unchanged, including dots and underscores—for example `save.migration.spatial.gd66.content.outcome_mismatch`. No punctuation/case transformation or alias is allowed. UX/Localization must author that exact key before activation; GD66 adds no entries. Text never determines identity or gameplay.

## 21. Backup, rollback, retry, and idempotence

O stays active until the one replacement. Failed C never changes authority. Retry of identical O reuses verified B/transaction identity and produces byte-identical C. Recovery chooses only verified O or complete verified C. A complete future-target C is the only already-current no-op. Rollback restores O atomically and switches topology/content readers and writers together; independent economic structures restore as part of O without conversion. Backup retention remains configuration-owned Phase 2 policy, but B cannot be pruned before transaction finalization.

## 22. Phase 2 dependency breakdown

1. Add, validate, and inject the inactive `SpatialMigrationCompatibilityProfile` configuration with independent R1/R2 recomputation and no runtime fallback constants.
2. Add and round-trip the inactive `SavedSpatialFloor` binding, fixed structures, `FloorRoomContentState` including `RoomSemantics`, complete canonical marker, and transaction metadata shape; verify then-current schema and select the target version.
3. Add raw-byte/root-version/member-presence interception before all existing legacy normalization, including unwrapped v1 classification.
4. Implement pure schema-specific route selection and complete detached candidate construction with exact fixtures in §8.
5. Implement definition/content/outcome/geometry/graph canonical validation and stable codes.
6. Implement verified B, journal, one atomic C replacement, durable reread and O recovery with failure injection.
7. In one activation packet, bind canonical topology and room-content readers/writers after replacement while preserving independent economic-structure readers/writers.
8. Add localization mappings and full lifecycle/edit-mode immediate-save, recovery, EditMode/PlayMode/build evidence before activation approval.

## 23. Explicit non-goals

No migration, save field, exact future schema number, code, fixture/test, content, localization, runtime activation, structure spatial mapping, corridor gameplay, cost, Floor 2, UI, tuning, scene/asset/settings/package change, or fun claim is included.

## 24. Acceptance checklist

- [x] Candidate status while PR #187 is open; schema 6/current authorities unchanged.
- [x] Route precedence excludes independent `dungeonLayout`/`structureRuntime`.
- [x] Exact nonspatial canonical room-content ownership precedes migration builder.
- [x] Saved floor instance binds explicitly to production definition/index.
- [x] Raw presence precedes every current normalization/backfill.
- [x] Schema-specific v1–v6 fixtures and true future-target idempotence are defined.
- [x] Content-only and effective-lower projections preserve explicit/implicit semantics and outcomes.
- [x] Injected compatibility profile owns all migration geometry with no runtime fallback.
- [x] Exact reason registry has no aliases, wildcard definitions, duplicates, or undefined references.
- [x] Wrapped/unwrapped v1 and canonical empty transitions are exact.
- [x] R1/R2 production geometry remains proven.
- [x] One complete atomic replacement is the sole schema/authority switch.
- [x] No hidden fallback, partial publication, localized identity, or dual route/content writers.

## 25. Candidate approval statement

PR #187 proposes approval of this route precedence, independent economic-structure preservation, canonical room-content and saved-floor contracts, raw-load interception, schema-specific fixtures, stable identities/geometry, and one-replacement transaction for later Phase 2 implementation. It is not repository-approved until merged and changes no present behavior. Unsupported states retain O with stable diagnostics; Phase 2 remains blocked until this candidate is approved and merged.
