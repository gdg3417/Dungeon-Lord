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
| `v1-wrapped-missing-schema-token` | `gd66.payload.missing_schema`, fatal; do not reinterpret as unwrapped |
| `v1-wrapped-malformed-schema-token` | `gd66.payload.invalid_schema`, fatal |
| `v1-wrapped-missing-schema-version` | `gd66.payload.missing_schema_version`, fatal |
| `v1-wrapped-nonintegral-schema-version` | `gd66.payload.nonintegral_schema_version`, fatal |
| `v1-wrapped-missing-primary` | `gd66.payload.missing_primary`, fatal |
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

Canonical owner `SavedSpatialFloor.RoomContents.RoomSemantics` contains exactly one record per room with `RoomInstanceId` and `LegacyRoomOriginKind`, whose append-only allowed values are `MigratedExplicitLegacyRoom`, `ImplicitCompatibilityContainer`, and `CanonicalPlayerPlaced`. Records canonicalize by ordinal room ID; missing/duplicate semantics fail candidate validation. The field round-trips and participates in frozen-projection equality.

An implicit container does not enter the ordered room-placement effect projection and never invents Basic-room simulation effects; its contents retain historical ordering/outcomes. This is a narrow historical rule for valid supported content, not fallback for a blank/malformed/unmapped/missing explicit room ID. Narrow Hall stays unmapped. Any unsupported or outcome-changing projection fails `gd66.content.outcome_mismatch`.

## 10. Content compatibility mapping

| Legacy value | Decision |
|---|---|
| explicit `placement.option.room.basic` | direct `spatial.room.basic`; `LegacyRoomOriginKind=MigratedExplicitLegacyRoom` |
| supported content with no room record | approved R1 Basic container; `LegacyRoomOriginKind=ImplicitCompatibilityContainer`; no explicit-room effect |
| blank/malformed explicit room | `gd66.content.invalid_legacy_room`; never implicit-container fallback |
| `placement.option.room.narrow_hall` | `gd66.content.migration_blocked_narrow_hall`; never corridor/Rectangle/substitute |
| `dungeonLayout` structure IDs | no room mapping; preserved independently |

No general safe fallback exists. Production/profile resolution and content errors use exact §19 codes; O remains active on failure.


### Narrow Hall migration repair and activation boundary

Legacy O containing `placement.option.room.narrow_hall` fails `gd66.content.migration_blocked_narrow_hall`, remains the sole verified legacy authority, and receives the exact actionable localization key from §20. Legacy UI/writers remain available only for that verified O so the player may replace Narrow Hall with Basic and retry; changed O creates a new input descriptor, transaction ID, and B. No automatic mapping to Straight Stone Corridor, Rectangle Room, Large Chamber, or Basic Room is permitted.

After canonical activation, writers reject Narrow Hall before detached mutation with `gd66.write.unsupported_room_selection`; no room/content/sequence/revision/save change occurs. Canonical configuration/UI must omit it as a valid choice or show it disabled using that exact reason key. Phase 2 acceptance includes legacy repair-and-retry plus canonical selection/UI rejection tests.

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

### Profile lifecycle, pipeline, and build-gate ownership

Each profile record has append-only `Lifecycle = Active | Retired`. Fresh migration selection considers only `Active` profiles; their source ranges must not overlap and exactly one must apply. `Retired` profiles are excluded from fresh selection but remain addressable for retry/recovery by exact `(ProfileId, ProfileVersion, CanonicalProfileSha256)`. Pinned lookup never substitutes a newer active version. A profile may not retire or be removed while a supported journal, backup, or committed marker can reference it unless release-retention evidence proves the exact canonical bytes remain shipped and readable. C and the journal persist the exact identity, lifecycle-independent version, and hash.

The profile file is a **direct-authored, separately loaded production configuration input**, not registered in or generated by the spatial content manifest. Save/Data Engineering owns future schema `spatial_migration_compatibility_profiles` version 1 and canonical JSON rules; this profile-schema version is content-input metadata, not a save-schema number. Phase 2 adds one explicit serialized `GameRoot`/production-composition assignment named `spatialMigrationCompatibilityProfilesJson`, injected alongside—not discovered from—the manifest/catalog/limits inputs. There is no export transformation: authors edit the authoritative file, while editor validation parses, ordinally canonicalizes in memory, reserializes, and requires byte equality before merge.

The existing production pre-build gate must add the explicitly assigned profile bytes to every player-build entry point, validate schema/canonical bytes, active-range uniqueness, retired pinned lookup, production references, geometry and descriptor hashes, and fail the build on missing/invalid assignment. Defensive runtime composition repeats the same pure validation and publishes no migration service on failure. Runtime/editor/Bootstrap discovery, fallback asset, hardcoded default, generated replacement, and partial publication are prohibited. Phase 2 requires focused EditMode parser/canonicalization/profile-lifecycle/reference/geometry tests plus build-gate evidence for missing, duplicate, invalid, retired-pin, hash-mismatch, and every-entry-point enforcement before activation.

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

### Canonical first-write and room-origin transitions

After canonical activation, every edit builds a detached copy, validates the complete floor/topology/content projection, performs one ordinary atomic save, and publishes the new in-memory authority only after durable success. Failure emits the exact §19 code and leaves prior state, sequences, effects, and persisted bytes unchanged.

| Canonical action | Required atomic result |
|---|---|
| First Basic room from `spatialFloors=[]` | Create Floor 0 binding, R1 fixed endpoints/room/edges from the injected active profile; semantics `CanonicalPlayerPlaced`; apply explicit room-placement effects once; immediate edit-mode save. |
| First supported monster/trap/loot from empty | Create R1 Basic content container with semantics `ImplicitCompatibilityContainer`; add content; do not apply room-placement effects; immediate save. This value denotes implicit compatibility behavior regardless of whether created during migration or canonical content-first writing. |
| Later explicit Basic placement into implicit container | Atomically change semantics to `CanonicalPlayerPlaced`, retain valid contents, and apply explicit room-placement effects exactly once. |
| Room-option replacement | Resolve an approved production mapping/profile rule, validate footprint/connections/capacity/effects, then replace atomically; unsupported Narrow Hall is rejected before mutation. |
| Remove room with contents | Reject `gd66.write.room_removal_has_contents`; no cascade or partial edit. |
| Capacity-reducing replacement | Reject `gd66.write.capacity_reduction_invalid`; never truncate/reorder contents. |
| No-op placement | Emit `gd66.diagnostic.canonical_write_noop`; do not advance sequence/revision or write bytes. |
| Validation/save failure | Emit `gd66.write.first_write_validation_failed` or `gd66.write.atomic_save_failed`; rollback detached state completely. |
| Close/reopen | Immediate saved bytes round-trip identical canonical semantics; marker binds readers/writers deterministically without legacy fallback. |

Native rooms therefore always have a valid semantic value. Phase 2 lifecycle fixtures cover every row, immediate edit-mode persistence, save failure, close/reopen, and deterministic reader/writer rebinding.

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

## 16. Migration input descriptor and atomic transaction

### Immutable `SpatialMigrationInputDescriptor`

Every attempt owns this immutable descriptor:

1. `OriginalPayloadSha256` over exact O bytes.
2. `RawSourceSchemaVersion` and exact `EnvelopeClassification` (`WrappedSaveRoot` or `UnwrappedSaveData`).
3. selected `TargetSchemaVersion`.
4. `CanonicalAuthorityMarkerVersion`.
5. `MigrationContractVersion`.
6. `CompatibilityProfileId`, `CompatibilityProfileVersion`, and `CanonicalCompatibilityProfileSha256`.
7. `ProductionSpatialManifestSha256` and `ProductionSpatialCatalogSha256` over exact validated canonical bytes.
8. an ordinal array `AdditionalValidationInputHashes` of `(InputId,CanonicalSha256)` for production strings/limits only when they affect validation/output.
9. `LegacyGameplayContentConfigSha256` over the exact canonical config used for option/category/capacity/effect validation.
10. `CanonicalSerializerId` and `CanonicalSerializerSchemaVersion`.

Descriptor canonical serialization is UTF-8 JSON with the fixed field order above, invariant integers, lowercase 64-character SHA-256 hex, no insignificant whitespace/BOM, and `AdditionalValidationInputHashes` sorted ordinally by `InputId`; unknown/duplicate fields fail. `InputFingerprintSha256` is SHA-256 of those exact descriptor bytes. Transaction ID is `gd66-{OriginalPayloadSha256}-{InputFingerprintSha256}`. The journal stores `JournalSchemaVersion`, the full descriptor bytes, fingerprint, transaction ID, exact active/backup/staging paths, O/B/expected-C hashes, and append-only `Stage = DescriptorPinned | BackupVerified | CandidateVerified | Replaced | DurableVerified | OriginalRestored`; each transition is flushed before the next filesystem action. C's committed marker stores the complete canonical descriptor bytes and fingerprint plus `CreationKind=Migrated`; native new saves instead store `CreationKind=NativeCanonical` and no migration descriptor. The active file hash is verified externally from the journal to avoid a self-hash cycle.

Same O plus identical fingerprint may reuse verified B and must produce byte-identical C. Changed O creates a new transaction/B. Changed profile, catalog, manifest, relevant config, target schema, marker, algorithm, or serializer creates a different attempt, transaction ID, separately named/verified B, or candidate identity; it cannot reuse the prior attempt merely because O bytes match. Recovery never rebuilds C: it verifies staged/active C only against its journal-pinned descriptor and expected C hash. Pinned inputs are resolved by exact identity/hash, including Retired profiles; newer inputs are never substituted. Missing pinned input recovers verified O. Descriptor mismatch emits `gd66.transaction.input_fingerprint_mismatch`; a fresh attempt caused by dependency change emits `gd66.transaction.dependency_changed`; unavailable relevant config/serializer input emits `gd66.transaction.pinned_input_missing`, and mismatched canonical bytes/version emits `gd66.transaction.pinned_input_hash_mismatch`; profile and spatial pins use their more precise §19 codes.

### One complete replacement

1. Read/preserve O and construct/pin the descriptor.
2. Create, flush, reread, deserialize and hash-verify B at `{activeFile}.migration.{transactionId}.original`.
3. Write/flush journal containing the complete pinned descriptor and hashes.
4. Construct detached complete C with every future field, target schema, marker and descriptor fingerprint.
5. Canonicalize, serialize, deserialize and fully validate C against pinned inputs; record exact C hash.
6. Persist C with one journaled same-directory atomic replacement.
7. Treat that replacement—not a later marker write—as the sole schema/topology/content authority transition.
8. Reread and verify durable active bytes against pinned C hash/descriptor.
9. If verification fails, atomically restore verified O; never rebuild against newer dependencies.
10. Never perform a second schema/marker write or expose staging/journal/partial C.

The journal coordinates recovery only and never becomes gameplay authority.

## 17. Writable-authority transition

Before replacement, legacy route and legacy room-content fields are the writable authorities; the detached candidate has none. The single atomic replacement simultaneously makes the complete canonical floor/graph the sole route-topology authority and `RoomContents` the sole room-content authority. Readers and writers bind once at load from the canonical marker and never fall back during a session. Legacy route/content fields become read-only evidence. `dungeonLayout`/`structureRuntime` remain the separate writable economic-structure subsystem on both sides of the replacement.

Marker/schema/graph/content disagreement is `gd66.authority.contradictory_state` and triggers verified recovery. Simultaneous legacy/canonical route writers, simultaneous legacy/canonical room-content writers, split category authority, or a second marker write are prohibited.

### Direct canonical new-save path after activation

When no save file exists, `CreateNew` directly constructs the selected target schema and canonical marker with `spatialFloors=[]`, empty canonical room-content authority, and approved defaults for independent economic/unrelated state. It creates no writable legacy route fields, O, B, migration descriptor, or journal. First persistence uses the ordinary durable atomic save path. Reload validates marker/schema and emits `gd66.success.already_committed` without legacy migration. Successful creation emits `gd66.success.native_canonical_save_created`; first persistence failure emits `gd66.write.native_save_persist_failed`, exposes no unsaved gameplay authority, and retries the no-file path without migration artifacts.

Phase 2 fixtures cover no-file creation, default preservation, empty canonical readers/writers, first ordinary save failure, successful reload, and proof that migration backup/journal paths were untouched. The exact future schema remains selected only in Phase 2.

## 18. Schema-version policy

Current schema is 6. GD66 neither increments it nor approves a specific future number. Phase 2 must select a version greater than the then-current `LatestSchemaVersion` only when the complete serialized floor/fixed/content/marker shape is finalized. That selected version appears only in fully validated C and becomes active only through §16's single replacement.

## 19. Authoritative exact reason-code table

This table is the sole append-only ordinal registry. Phase 2 must provide at least one emitting fixture for every row; therefore a row with no exact condition is an unused alias and fails validation. “Trusted active state” is restricted to `Verified O`, `Verified C`, `Unchanged verified O`, `Unchanged verified C`, `No trusted active payload`, or `Not applicable`.

| Exact code | Classification | Continue C? | Trusted active state after emission | Gameplay | Player key? | Exact condition |
|---|---|---:|---|---|---:|---|
| `gd66.diagnostic.payload_wrapped_v1` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Valid wrapped schema-1 root classified. |
| `gd66.diagnostic.payload_unwrapped_v1` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Recognized unwrapped schema-1 `SaveData` classified. |
| `gd66.payload.unreadable` | fatal failure | No | No trusted active payload | Blocked | Yes | JSON bytes cannot be parsed. |
| `gd66.payload.ambiguous_envelope` | fatal failure | No | No trusted active payload | Blocked | Yes | Payload cannot be safely classified wrapped/unwrapped. |
| `gd66.payload.missing_schema` | fatal failure | No | No trusted active payload | Blocked | Yes | Root-shaped payload omits `schema`. |
| `gd66.payload.invalid_schema` | fatal failure | No | No trusted active payload | Blocked | Yes | Root-shaped payload has wrong or malformed `schema`. |
| `gd66.payload.null_primary` | fatal failure | No | No trusted active payload | Blocked | Yes | Wrapped root `primary` is explicitly null. |
| `gd66.payload.unknown_member_unpreservable` | fatal failure | No | Verified O | Allowed | Yes | Unknown source member cannot be losslessly retained. |
| `gd66.diagnostic.no_legacy_route` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Valid legacy payload has no semantic route; empty C begins. |
| `gd66.diagnostic.no_journal_legacy_valid` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | No journal exists and active legacy O is valid for a fresh attempt. |
| `gd66.success.empty_migrated` | success | No | Verified C | Allowed | No | Canonical empty C durably committed. |
| `gd66.success.migrated` | success | No | Verified C | Allowed | No | Populated canonical C durably committed. |
| `gd66.success.already_committed` | success | No | Verified C | Allowed | No | Complete verified target payload already authoritative. |
| `gd66.success.recovered_original` | success | No | Verified O | Allowed | Yes | Verified O restored after interruption/failure. |
| `gd66.route.duplicate_room_slot` | fatal failure | No | Verified O | Allowed | Yes | Duplicate assignment floor/room key. |
| `gd66.route.duplicate_floor_node_revision` | fatal failure | No | Verified O | Allowed | Yes | Floor-node key/slot has tied greatest revision. |
| `gd66.route.duplicate_placement_revision` | fatal failure | No | Verified O | Allowed | Yes | Placement category has tied greatest revision. |
| `gd66.route.record_out_of_range` | fatal failure | No | Verified O | Allowed | Yes | Floor, room, node, slot, revision, or sequence is out of range. |
| `gd66.route.gap` | fatal failure | No | Verified O | Allowed | Yes | Room 1 exists without Room 0. |
| `gd66.diagnostic.lower_model_agreement` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Effective lower projection exactly agrees. |
| `gd66.diagnostic.lower_ineffective_conflict` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Lower disagreement is historically filtered/ineffective. |
| `gd66.diagnostic.lower_effective_content_contributed` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Historical resolver makes lower content effective and it is copied. |
| `gd66.diagnostic.missing_explicit_room_supported_content` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Supported effective content has no explicit room. |
| `gd66.diagnostic.implicit_basic_container_created` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | R1 implicit Basic container created without room effect. |
| `gd66.content.outcome_mismatch` | fatal failure | No | Verified O | Allowed | Yes | Canonical projection cannot preserve effective run inputs/semantics exactly. |
| `gd66.content.invalid_legacy_room` | fatal failure | No | Verified O | Allowed | Yes | Explicit room ID is blank, malformed, or category-wrong. |
| `gd66.content.missing_production_room` | recoverable failure | No | Verified O | Allowed | Yes | Mapped production room definition is absent. |
| `gd66.content.invalid_production_room` | recoverable failure | No | Verified O | Allowed | Yes | Mapped production room definition fails validation. |
| `gd66.content.invalid_option` | fatal failure | No | Verified O | Allowed | Yes | Content option is blank, malformed, unknown, or lacks required config. |
| `gd66.content.category_mismatch` | fatal failure | No | Verified O | Allowed | Yes | Content option does not belong to recorded category. |
| `gd66.content.room_capacity_exceeded` | fatal failure | No | Verified O | Allowed | Yes | Per-room category count exceeds mapped room capacity. |
| `gd66.content.duplicate_assignment` | fatal failure | No | Verified O | Allowed | Yes | Assignment ID or room/category/sequence duplicates. |
| `gd66.content.room_semantics_invalid` | fatal failure | No | Verified O | Allowed | Yes | Room semantics missing, duplicated, or inconsistent with frozen projection. |
| `gd66.floor.missing_definition` | recoverable failure | No | Verified O | Allowed | Yes | Saved/profile floor definition is absent. |
| `gd66.floor.invalid_definition` | recoverable failure | No | Verified O | Allowed | Yes | Target floor definition fails production validation. |
| `gd66.floor.index_mismatch` | fatal failure | No | Verified O | Allowed | Yes | Saved/profile index differs from authored floor index. |
| `gd66.floor.duplicate_binding` | fatal failure | No | Verified O | Allowed | Yes | Floor instance ID or index duplicates. |
| `gd66.profile.missing` | recoverable failure | No | Verified O | Allowed | Yes | No profile applies to raw source schema. |
| `gd66.profile.duplicate` | fatal failure | No | Verified O | Allowed | Yes | Profile identity/version duplicates or applicability overlaps. |
| `gd66.profile.invalid` | recoverable failure | No | Verified O | Allowed | Yes | Profile structure, mapping, geometry, socket, total, or reference is invalid. |
| `gd66.profile.version_mismatch` | recoverable failure | No | Verified O | Allowed | Yes | Journal/source requires unsupported profile version. |
| `gd66.id.malformed` | fatal failure | No | Verified O | Allowed | Yes | Persistent ID violates exact grammar. |
| `gd66.id.collision` | fatal failure | No | Verified O | Allowed | Yes | Any canonical persistent ID collides. |
| `gd66.geometry.bounds` | fatal failure | No | Verified O | Allowed | Yes | Occupied tile lies outside profile target floor. |
| `gd66.geometry.overlap` | fatal failure | No | Verified O | Allowed | Yes | Physical footprints overlap. |
| `gd66.geometry.capacity` | fatal failure | No | Verified O | Allowed | Yes | Tile union exceeds capacity or expected total differs. |
| `gd66.geometry.socket` | fatal failure | No | Verified O | Allowed | Yes | Connection point/socket type/facing is incompatible. |
| `gd66.geometry.adjacency` | fatal failure | No | Verified O | Allowed | Yes | Direct-doorway endpoints are not adjacent. |
| `gd66.graph.endpoint` | fatal failure | No | Verified O | Allowed | Yes | Entrance/completion endpoint missing, duplicate, or wrong kind. |
| `gd66.graph.reachability` | fatal failure | No | Verified O | Allowed | Yes | Required semantic route is unreachable. |
| `gd66.graph.terminal` | fatal failure | No | Verified O | Allowed | Yes | Completion-terminal semantics are invalid. |
| `gd66.graph.ordering` | fatal failure | No | Verified O | Allowed | Yes | Candidate is not canonical/round-trip stable. |
| `gd66.transaction.backup_failed` | recoverable failure | No | Verified O | Allowed | Yes | B creation, flush, readback, hash, or parse verification fails. |
| `gd66.transaction.journal_invalid` | recoverable failure | No | Verified O | Allowed | Yes | Journal is missing, malformed, or hash-inconsistent. |
| `gd66.transaction.candidate_invalid` | fatal failure | No | Verified O | Allowed | Yes | Complete C serialization/deserialization or aggregate validation fails. |
| `gd66.transaction.commit_failed` | recoverable failure | No | Verified O | Allowed | Yes | Single atomic replacement fails. |
| `gd66.transaction.durability_failed` | recoverable failure | No | Verified O | Allowed | Yes | Committed bytes fail durable reread/hash/semantic verification. |
| `gd66.transaction.recovery_failed` | fatal failure | No | No trusted active payload | Blocked | Yes | Neither verified O nor complete verified C can be restored. |
| `gd66.authority.contradictory_state` | recoverable failure | No | No trusted active payload | Blocked | Yes | Schema, marker, canonical fields, or writer evidence disagree. |
| `gd66.payload.missing_schema_version` | fatal failure | No | No trusted active payload | Blocked | Yes | Wrapped/root-shaped payload omits `schemaVersion`. |
| `gd66.payload.nonintegral_schema_version` | fatal failure | No | No trusted active payload | Blocked | Yes | `schemaVersion` is not an integral JSON number. |
| `gd66.payload.unsupported_legacy_version` | fatal failure | No | No trusted active payload | Blocked | Yes | Source version is below/within legacy range but no migration contract supports it. |
| `gd66.payload.newer_than_application` | fatal failure | No | No trusted active payload | Blocked | Yes | Source save version is greater than application latest supported version. |
| `gd66.payload.missing_primary` | fatal failure | No | No trusted active payload | Blocked | Yes | Wrapped root omits `primary`. |
| `gd66.transaction.input_fingerprint_mismatch` | recoverable failure | No | Verified O | Allowed | Yes | Journal descriptor bytes do not hash to stored fingerprint or expected attempt. |
| `gd66.transaction.dependency_changed` | nonfatal diagnostic | No | Unchanged verified O | Allowed | No | Same O is evaluated with changed profile/catalog/config/schema/marker/algorithm/serializer; create new attempt. |
| `gd66.transaction.pinned_input_missing` | recoverable failure | No | Verified O | Allowed | Yes | Any descriptor-pinned required input is unavailable. |
| `gd66.transaction.pinned_input_hash_mismatch` | recoverable failure | No | Verified O | Allowed | Yes | Pinned relevant config/string/serializer input exists but canonical hash or version differs. |
| `gd66.transaction.pinned_profile_missing` | recoverable failure | No | Verified O | Allowed | Yes | Exact pinned profile ID/version/hash is unavailable. |
| `gd66.transaction.pinned_profile_hash_mismatch` | recoverable failure | No | Verified O | Allowed | Yes | Pinned profile identity exists but canonical hash differs. |
| `gd66.transaction.pinned_spatial_input_missing` | recoverable failure | No | Verified O | Allowed | Yes | Pinned production manifest or catalog bytes are unavailable. |
| `gd66.transaction.pinned_spatial_input_hash_mismatch` | recoverable failure | No | Verified O | Allowed | Yes | Pinned manifest or catalog canonical hash differs. |
| `gd66.transaction.stale_journal_original_mismatch` | recoverable failure | No | Unchanged verified O | Allowed | No | Journal O hash differs from active verified O. |
| `gd66.transaction.active_payload_unknown` | recoverable failure | No | No trusted active payload | Blocked | Yes | Active hash matches neither descriptor O nor expected C. |
| `gd66.transaction.no_trusted_active_payload` | fatal failure | No | No trusted active payload | Blocked | Yes | Neither O, B, staged C, nor active C can be trusted. |
| `gd66.transaction.backup_incomplete` | recoverable failure | No | Verified O | Allowed | No | Journal exists before B completed verification. |
| `gd66.transaction.candidate_absent` | recoverable failure | No | Verified O | Allowed | No | Verified B exists but no staged/active C exists. |
| `gd66.diagnostic.staged_candidate_verified` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Complete staged C and pins validate while O remains active. |
| `gd66.transaction.journal_missing_after_commit` | recoverable failure | No | Verified C | Allowed | No | Complete active C validates from marker/pins but journal is missing/malformed. |
| `gd66.transaction.invalid_backup_with_committed_candidate` | recoverable failure | No | Verified C | Allowed | No | B is invalid but active C matches expected descriptor/hash. |
| `gd66.success.native_canonical_save_created` | success | No | Verified C | Allowed | No | No-file path directly creates/persists canonical empty save. |
| `gd66.write.native_save_persist_failed` | recoverable failure | No | No trusted active payload | Blocked | Yes | No-file canonical first persistence fails; no active save exists. |
| `gd66.write.unsupported_room_selection` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Canonical writer/UI attempts unsupported Narrow Hall. |
| `gd66.write.first_write_validation_failed` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Detached canonical first-write candidate fails validation. |
| `gd66.write.atomic_save_failed` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Canonical mutation durable atomic save fails; prior C retained. |
| `gd66.write.room_removal_has_contents` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Room removal requested while assignments remain. |
| `gd66.write.capacity_reduction_invalid` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Replacement would make retained contents exceed capacity. |
| `gd66.diagnostic.canonical_write_noop` | nonfatal diagnostic | No | Unchanged verified C | Allowed | No | Requested canonical placement is semantically identical. |
| `gd66.content.migration_blocked_narrow_hall` | recoverable failure | No | Verified O | Allowed | Yes | Legacy O contains effective Narrow Hall with no production room mapping. |

## 20. Player messaging and localization ownership

Save/Migration Engineering emits only exact §19 codes. For each row with “Player key?” Yes, the key is `save.migration.spatial.` followed by the full code unchanged, including dots and underscores—for example `save.migration.spatial.gd66.content.outcome_mismatch`. No punctuation/case transformation or alias is allowed. UX/Localization must author that exact key before activation; GD66 adds no entries. Text never determines identity or gameplay.

## 21. Exact interruption, recovery, rollback, retry, and idempotence

Trusted evidence means exact bytes plus schema/marker/descriptor/hash validation. “Quarantine” retains bytes for support but never gameplay.

| Observed state | Trusted evidence required | Resulting active payload / gameplay | Retain or quarantine | Exact code | Auto retry? | Player intervention? |
|---|---|---|---|---|---:|---:|
| No journal; active legacy O valid | O parses and source classification valid | Verified O; gameplay allowed on legacy authority; fresh attempt may start | retain O | `gd66.diagnostic.no_journal_legacy_valid` | Yes | No |
| No journal; active complete target C valid | C schema/marker/descriptor fingerprint and current pinned inputs validate | Verified C; gameplay allowed | retain C | `gd66.success.already_committed` | No | No |
| No journal; active contradictory/unknown | no trusted descriptor or classification | No trusted active payload; gameplay blocked | quarantine active | `gd66.authority.contradictory_state` | No | Yes |
| Journal before B completion | journal descriptor valid; active O hash matches | Verified O; gameplay allowed after cleanup | retain O; quarantine journal/partial B | `gd66.transaction.backup_incomplete` | Yes | No |
| Valid B; no C | B matches descriptor O hash | restore/retain Verified O; gameplay allowed | retain B/O/journal | `gd66.transaction.candidate_absent` | Yes | No |
| Valid B and complete staged C | B and staged C match pinned descriptor/expected hashes | Verified O stays active; gameplay allowed; replacement may resume | retain all | `gd66.diagnostic.staged_candidate_verified` | Yes | No |
| Active O with complete staged C | active O/B/C all pinned and hash-valid | Verified O until replacement; gameplay allowed | retain all | `gd66.diagnostic.staged_candidate_verified` | Yes | No |
| Active C after replacement; valid journal | active C equals expected C hash/descriptor | Verified C; gameplay allowed; finalize cleanup | retain C/B per policy; journal until finalized | `gd66.success.migrated` | No | No |
| Active complete C; journal missing/malformed | C marker descriptor and exact shipped pinned inputs validate independently | Verified C; gameplay allowed; quarantine journal | retain C; quarantine malformed journal | `gd66.transaction.journal_missing_after_commit` | No | No |
| Active hash matches neither O nor C | B/journal may be valid but active is neither expected hash | restore Verified O if possible; gameplay only after restore | quarantine active/staged | `gd66.transaction.active_payload_unknown` | No | Yes if restore fails |
| Invalid B; active O valid | O hash/parse valid | Unchanged Verified O; gameplay allowed | quarantine B/journal | `gd66.transaction.backup_failed` | Yes with new B | No |
| Invalid B; active C valid | C matches descriptor/expected hash | Verified C; gameplay allowed | retain C; quarantine B | `gd66.transaction.invalid_backup_with_committed_candidate` | No | No |
| Invalid O; valid B | B matches pinned original hash | restore Verified O from B; gameplay allowed afterward | retain B; quarantine invalid O | `gd66.success.recovered_original` | Yes | No |
| Durable C verification failure | verified B/O | restore Verified O; gameplay allowed afterward | quarantine C; retain B/journal | `gd66.transaction.durability_failed` | Yes only with same pins | No |
| O restoration failure | neither active O nor restoration write trusted | No trusted active payload; gameplay blocked | retain/quarantine all | `gd66.transaction.recovery_failed` | No | Yes |
| Both O and C untrusted | no payload meets hashes/descriptor | No trusted active payload; gameplay blocked | quarantine all, never create replacement save | `gd66.transaction.no_trusted_active_payload` | No | Yes |
| Stale journal for different O | active O hash differs from journal descriptor | Unchanged Verified O; gameplay allowed; stale attempt ignored | retain O; quarantine journal/B/C | `gd66.transaction.stale_journal_original_mismatch` | Yes as new transaction | No |
| Journal input fingerprint mismatch | journal descriptor bytes/fingerprint disagree | Verified O if hash-valid; gameplay allowed on O | quarantine attempt | `gd66.transaction.input_fingerprint_mismatch` | No; fresh descriptor required | No |
| Pinned profile unavailable | exact profile identity/version/hash unavailable | recover Verified O; gameplay allowed on O | retain O/B; quarantine C/journal | `gd66.transaction.pinned_profile_missing` | No until pin restored | Yes |
| Pinned catalog/manifest unavailable | exact canonical bytes unavailable | recover Verified O; gameplay allowed on O | retain O/B; quarantine C/journal | `gd66.transaction.pinned_spatial_input_missing` | No until pin restored | Yes |

Same O/same fingerprint retries deterministically and may reuse verified B; dependency changes form a new attempt; changed O always forms a new transaction/B. A complete verified C is the only already-current state. Rollback restores O atomically and binds topology/content readers together while leaving independent economic state as preserved in O. Backup retention is configuration-owned, but B/journal/pinned inputs cannot be removed before finalization or their supported recovery-retention horizon.

## 22. Phase 2 dependency breakdown

1. Add the direct-authored profile schema/input, explicit composition assignment, active/retired lifecycle validation, production reference validation, runtime validation, expanded every-entry-point pre-build gate, focused tests, and evidence.
2. Add/round-trip inactive saved-floor/fixed/content/room-semantics shapes, canonical marker/native creation kind, and then select the target save schema.
3. Add canonical serializer plus `SpatialMigrationInputDescriptor`, journal schema, exact hashes/pins, and retained pinned-input lookup.
4. Add raw-byte/envelope/member interception before legacy normalization and direct no-file canonical `CreateNew`.
5. Implement schema-specific topology selection, `FrozenLegacyRouteProjection`, profile-injected candidate construction, and exact fixtures.
6. Implement detached canonical writer mutations and native first-write/origin transitions, including Narrow Hall UI/writer rejection and immediate-save rollback tests.
7. Implement §19 validation codes and §21 recovery matrix with failure injection for every interruption/pin/hash state.
8. Implement verified B, one atomic C replacement, durable verification/O recovery, and deterministic retry rules.
9. Activate canonical topology/content readers and writers together while preserving independent economic writers; add localization keys and full EditMode/PlayMode/build/lifecycle evidence.

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
- [x] Complete descriptor fingerprint and pinned-input lifecycle bind every attempt/recovery.
- [x] Recovery matrix names trusted state, gameplay, retention, retry, intervention, and exact code.
- [x] Direct native new-save path and canonical first writes never create writable legacy route state.
- [x] Native room semantics, mutation rollback, immediate save, and reopen behavior are exact.
- [x] Narrow Hall has distinct legacy-repair and canonical-write rejection policies.
- [x] Profile input has direct-authoring, composition, runtime, and expanded build-gate ownership.
- [x] One complete atomic replacement is the sole schema/authority switch.
- [x] No hidden fallback, partial publication, localized identity, or dual route/content writers.

## 25. Candidate approval statement

PR #187 proposes approval of this route precedence, independent economic-structure preservation, canonical room-content and saved-floor contracts, raw-load interception, schema-specific fixtures, stable identities/geometry, and one-replacement transaction for later Phase 2 implementation. It is not repository-approved until merged and changes no present behavior. Unsupported states retain O with stable diagnostics; Phase 2 remains blocked until this candidate is approved and merged.
