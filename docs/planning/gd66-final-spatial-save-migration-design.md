# GD66 final spatial save and migration design

**Phase 2B3 foundation (2026-08-02):** Phase 2B3 added detached, inactive canonical-spatial byte serialization, pinned migration-descriptor and identity contracts, pure relative sidecar naming, and migration-journal validation. It did not activate migration, filesystem transactions, canonical spatial readers, writers, or runtime authority.

**Current Phase 2B6B working status (2026-08-12):** Schema 7 live activation is implemented in PR #195: existing saves enter through the raw-before-legacy recovery/migration coordinator, new saves are created as native empty canonical complete saves, ordinary lifecycle and player spatial writes use the lossless canonical session and qualified Windows atomic writer, production spatial content owns canonical capacity, and legacy spatial members are frozen evidence. Activation remains restricted to qualified Windows Editor/Standalone local NTFS paths. GD66 is not complete or merge-ready until owner Unity EditMode, PlayMode, and Windows durability/lifecycle validation passes.

**Historical Phase 2B6A status:** PR #194 is merged at `2bcc336f5fbbb9797f6f319f738e7b9f7d0613bd`; it includes the authoritative detached candidate, transaction, recovery, activation preflight, and qualified Windows durability implementation. Phase 2B6A adds an explicit activation preflight and a Windows filesystem strategy using documented write-through file creation/flush and handle-based same-directory rename. Rename opens the source with `DELETE | GENERIC_WRITE`, read/write/delete sharing, `OPEN_EXISTING`, and `FILE_FLAG_WRITE_THROUGH`; submits an absolute UTF-16 `FILE_RENAME_INFO` through `SetFileInformationByHandle(FileRenameInfo)`; then calls `FlushFileBuffers` on that same renamed file handle and verifies source absence/destination presence. Microsoft documents `FileRenameInfo` as the handle-based rename operation, `FILE_FLAG_WRITE_THROUGH` as bypassing intermediate write caching, and `FlushFileBuffers` as flushing buffered information for the file to the device; together these are the selected NTFS metadata-persistence contract rather than the copy-oriented `MOVEFILE_WRITE_THROUGH` guarantee. No directory-handle flush or Windows directory-`fsync` equivalent is claimed. Selection is limited to Windows Editor and Windows Standalone on a local, nonredirected NTFS path; every other platform, filesystem, redirected/reparse path, invalid path, and failed native probe fails closed. The operating-system guarantee cannot correct storage hardware or drivers that falsely acknowledge cache flushes. Windows Editor and Windows Standalone durability qualification passed for PR #194; this does not qualify a future activated schema-7 lifecycle. Live `SaveMigration.LatestSchemaVersion` remains **6**; schema 7 remains detached and inactive; `SaveService`, `GameRoot`, native creation, canonical runtime readers/writers, and legacy writable authority are unchanged. Phase 2B6B remains the final live-activation packet. GD66 is not complete.

Labels below are **Fact**, **Observed**, **GD66 decision**, **Unsupported**, and **Phase 2**.

## 2. Current repository and save baseline

| Item | Reconciled state |
|---|---|
| Repository | `main` through merged PR #194 at `2bcc336f5fbbb9797f6f319f738e7b9f7d0613bd`; detached candidate/transaction and Windows durability qualification complete |
| Save root | `SaveRoot.schemaVersion`; live `SaveMigration.LatestSchemaVersion = 7` through GD66 only |
| Route topology | validated schema-7 spatial graph/content authority projected into the ordered MVP runtime view |
| Economic structures | `dungeonLayout` placements plus `structureRuntime`, concurrently active and independent of route topology |
| Spatial content/layout | validated production catalog owns canonical definitions and capacities; `FloorSpatialLayout` owns rooms/nodes/edges |

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

### Distinct migration and native-starter profiles with shared geometry authority

The future direct-authored configuration file `Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json` is the single writable authority for three record collections:

1. immutable `CompatibilityLayoutGeometryRecord` records own definition IDs, R1/R2 anchors/orientations, connection-point/socket expectations, route roles/direct doorways, and expected recomputable totals;
2. `SpatialMigrationCompatibilityProfile` records reference one geometry-record ID and are selected **only** for legacy migration by raw legacy source-schema range;
3. `CanonicalStarterLayoutProfile` records reference one geometry-record ID and are selected **only** for native canonical empty-to-R1/content-container construction by exact `(TargetSchemaVersion, CanonicalLayoutContractVersion)`.

Geometry values occur once in a geometry record, never duplicated between profile types or runtime constants. The initial shared record retains entrance `(0,0)/Zero/route`, Basic Room 0 `(0,2)/Zero`, Basic Room 1 `(0,6)/Zero`, R1 completion `(1,6)/Zero/route`, R2 completion `(1,10)/Zero/route`, `spatial.floor.01` index 0, and §12 definitions/sockets/totals.

Both profile types use append-only `Lifecycle = Active | Retired`. Fresh migration considers only Active migration profiles and requires exactly one nonoverlapping source-range match. Native first-write considers only Active starter profiles and requires exactly one match for the target-schema/layout-contract key. Retired records are excluded from fresh selection. Unfinished migration recovery resolves a migration profile and shared geometry by exact ID/version/canonical hash; it never substitutes a starter profile or newer migration version. Migration selection uses `gd66.profile.missing`, `gd66.profile.duplicate`, `gd66.profile.invalid`, or `gd66.profile.version_mismatch`; starter selection uses `gd66.starter_profile.missing`, `gd66.starter_profile.duplicate`, `gd66.starter_profile.invalid`, or `gd66.starter_profile.version_mismatch`. Phase 2 fixtures prove selection purposes cannot cross.

The same direct-authored file also owns exactly one Active `CanonicalLayoutContractSelection` per selected target schema. Save/Data Engineering selects its positive `CanonicalLayoutContractVersion` for a brand-new save as release configuration. `CreateNew` first resolves that selection, then resolves exactly one Active starter profile for `(TargetSchemaVersion, CanonicalLayoutContractVersion)`, and writes the version into the canonical marker before first persistence. A migration profile/shared geometry explicitly owns its `TargetCanonicalLayoutContractVersion`; migrated C writes that value, including empty C.

The canonical marker is the persistent owner and contains positive `CanonicalLayoutContractVersion`, `CreationKind = NativeCanonical | Migrated`, and optional immutable migrated audit identity (`MigrationTransactionId`, `MigrationDescriptorFingerprint`). It never embeds the candidate's own byte hash, avoiding a self-hash cycle. First-write selection uses the marker version, never the application's newer current selection. Application updates cannot silently move an existing empty save; changing the persisted version requires an explicit future target-schema/content migration. Missing/duplicate release selection emits `gd66.layout_contract.selection_missing`/`gd66.layout_contract.selection_duplicate`; a missing marker version emits `gd66.marker.layout_contract_missing`; an unsupported persisted version emits `gd66.marker.layout_contract_unsupported`; starter/marker mismatch emits `gd66.starter_profile.marker_mismatch`.

Phase 2 fixtures cover native create+first write in one release; native create/close/update/first write; migrated empty C then first write; missing version; unsupported old version; profile-marker mismatch; and explicit future layout-contract migration.

### Profile pipeline and build-gate ownership

The file is a **direct-authored, separately loaded production configuration input**, not registered in or generated by the spatial content manifest. Save/Data Engineering owns future input schema `spatial_layout_compatibility_profiles` version 1 and canonical JSON rules; this input-schema version is not a save-schema number. Phase 2 adds exactly one serialized `GameRoot`/production-composition assignment, `spatialLayoutCompatibilityProfilesJson`, alongside—not discovered from—the manifest/catalog/limits inputs. There is no export transformation: editor validation parses, ordinally canonicalizes in memory, reserializes, and requires byte equality.

Every player-build entry point must validate the explicit assignment, schema/canonical bytes, shared-record uniqueness/references, migration Active-range uniqueness, starter Active-key uniqueness, production references, geometry, and descriptor hashes. Defensive runtime composition repeats the pure validation and publishes neither migration nor canonical starter writer on failure. Runtime/editor/Bootstrap discovery, fallback asset, hardcoded default, generated replacement, and partial publication are prohibited.

Retired migration records and shared geometry must ship only for the supported unfinished-transaction recovery horizon established by release policy; finalized saves do not require historical profile/catalog/config bytes. Retirement/removal is blocked while a supported non-finalized journal may pin the record. Phase 2 requires focused EditMode selection/lifecycle/reference/canonicalization/geometry tests and build-gate evidence for missing, duplicate, invalid, cross-purpose selection, retired-pin during the recovery horizon, and every entry point.

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
| First Basic room from `spatialFloors=[]` | Create Floor 0 binding, R1 fixed endpoints/room/edges from the selected Active `CanonicalStarterLayoutProfile` and its shared geometry record; semantics `CanonicalPlayerPlaced`; apply explicit room-placement effects once; immediate edit-mode save. |
| First supported monster/trap/loot from empty | Select the same Active `CanonicalStarterLayoutProfile` by target-schema/layout-contract key; create its R1 Basic content container with semantics `ImplicitCompatibilityContainer`; add content; do not apply room-placement effects; immediate save. This value denotes implicit compatibility behavior regardless of whether created during migration or canonical content-first writing. |
| Later explicit Basic placement into implicit container | Atomically change semantics to `CanonicalPlayerPlaced`, retain valid contents, and apply explicit room-placement effects exactly once. |
| Room-option replacement | Resolve an approved production mapping and native starter/layout rule, validate footprint/connections/capacity/effects, then replace atomically; unsupported Narrow Hall is rejected before mutation. |
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

## 16. Migration input descriptor, compact paths, and atomic transaction

### Immutable input descriptor and compact identity

Every migration attempt pins `SpatialMigrationInputDescriptor`: exact `OriginalPayloadSha256`; raw source version/envelope; selected target schema; authority-marker and migration-contract versions; migration-profile ID/version/hash; shared-geometry ID/version/hash; production manifest/catalog hashes; ordinal relevant validation-input hashes; legacy gameplay-config hash; and canonical serializer ID/version.

Descriptor bytes are fixed-order, whitespace-free/BOM-free UTF-8 JSON with invariant integers, lowercase 64-character hashes, ordinal arrays, and no unknown/duplicate fields. `InputFingerprintSha256` hashes these bytes. `TransactionIdentitySha256` is SHA-256 of canonical bytes `{OriginalPayloadSha256,InputFingerprintSha256}` in that exact field order. Compact transaction ID is `gd66-{TransactionIdentitySha256}`: exactly 69 lowercase ASCII characters.

Same O and fingerprint may reuse verified B and must reproduce byte-identical C. Changed O creates a new attempt/B. Changed dependency, schema, marker, algorithm, profile, geometry, or serializer creates a distinct identity and cannot reuse candidate identity. Recovery never rebuilds C and resolves pinned dependencies exactly.

### Relative path contract and one-live-attempt rule

For active file `<stem><ext>` inside the normalized save directory, relative sibling names are:

- journal: `<stem>.<transactionId>.journal.json`;
- backup B: `<stem>.<transactionId>.original.bak`;
- staging C: `<stem>.<transactionId>.candidate.tmp`;
- finalization receipt: `<stem>.<transactionId>.finalized`.

`stem` is the already-approved active filename stem, limited to 80 UTF-16 code units. Each generated filename must be at most 180 UTF-16 code units, and the normalized absolute path must be at most 240 UTF-16 code units on Windows; platforms with a smaller reported limit use that smaller limit. Mobile paths use platform APIs, never manual separators. IDs/relative names must contain only approved ASCII filename characters; reject `/`, `\\`, drive/URI prefixes, `.`/`..` segments, rooted paths, normalization changes, symlink/reparse escape, or any resolved path outside the exact save directory. Backup, staging, and active files share that directory so replacement is same-volume/same-directory atomic. The journal records normalized relative filenames only and never trusts externally supplied absolute paths. Violations emit `gd66.transaction.path_invalid`.

At most one non-finalized journal may exist for an active save path. Before repaired-O or dependency-changed work starts, the previous attempt must be completed, restored to verified O, or quarantined after verified O is established. Multiple live journals are never selected by timestamp, directory order, filename order, or hash order; emit `gd66.transaction.multiple_live_attempts` and fail closed until deterministic evidence selects verified O or C. Finalized retained audit records are not live.

### Exact journal and stage sequence

The journal stores `JournalSchemaVersion`, canonical descriptor/fingerprint/transaction ID, normalized relative filenames, O/B/expected-C hashes, and `Stage`. Each stage write is flushed and reread before proceeding; no stage precedes its filesystem verification:

1. Read and verify O.
2. Construct the immutable descriptor and compact transaction ID.
3. Create, flush, reread, and verify journal at `DescriptorPinned`.
4. Create, flush, reread, parse, and hash-verify B.
5. Advance, flush, reread, and verify `BackupVerified`.
6. Construct, canonicalize, serialize/deserialize, and fully validate C; record expected C hash.
7. Advance, flush, reread, and verify `CandidateVerified`.
8. Perform the single atomic replacement.
9. Advance, flush, reread, and verify `Replaced`.
10. Reread and durably verify active C against expected hash/descriptor.
11. Advance, flush, reread, and verify `DurableVerified`.
12. Best-effort create, flush, reread, and hash-verify the optional immutable audit receipt containing transaction ID, descriptor fingerprint, and the initially finalized C hash. Receipt creation or verification failure is diagnostic only: quarantine any partial receipt and continue.
13. Advance, flush, reread, and verify the journal at `Finalized`, then apply cleanup/retention. If that journal write fails, retry it while the exact live journal remains; a restart with no valid live journal classifies the active payload by the rule below. If restoration occurs, verify O before `OriginalRestored`. Neither the receipt nor this stage rewrites C or becomes canonical authority.

The one replacement is the sole schema/topology/content authority transition. A sidecar journal coordinates recovery only.

### Pin lifetime, self-contained finalization, and mutable canonical loads

Exact descriptor dependencies are mandatory only while a non-finalized live journal exists. `DurableVerified` remains live; after its journal advances to `Finalized`, pin requirements end and the journal may be archived/pruned. The optional receipt certifies that one transaction finalized and stores only transaction ID, descriptor fingerprint, and the **initially finalized candidate hash**. It is immutable audit/recovery evidence, never rewritten by gameplay, and never schema/topology/content/save authority.

The migrated save is self-contained because its initial C already stores supported target schema, canonical authority marker, `CreationKind=Migrated`, positive `CanonicalLayoutContractVersion`, immutable transaction ID/fingerprint audit identifiers, canonical data, and definitions—never its own hash. A native save stores `CreationKind=NativeCanonical`, the positive layout-contract version, and no migration audit identity or receipt.

Load first discovers and validates journals for this exact normalized active-save path. A **valid live journal** is a parseable, path-contained, non-`Finalized`/non-`OriginalRestored` journal whose transaction identity, descriptor fingerprint, filenames, stage evidence, and O/expected-C hashes are internally valid and bind to this active path. Exactly one valid live journal takes recovery precedence under §21; multiple valid live journals fail closed. A malformed, stale, finalized, differently bound, or orphan journal is not a valid live journal and cannot claim authority merely by existing.

When there is no valid live journal, classification uses the active payload's own bytes. A canonical save—migrated, native, empty, later ordinarily modified, or copied without sidecars—loads with `gd66.success.already_committed` when its supported schema, marker, creation kind, persisted layout-contract version, IDs/order, saved definition references, graph/contents, and current supported content rules validate. `CreationKind=Migrated`, transaction ID, or descriptor fingerprint never implies an unfinished transaction. Historical candidate-hash equality and journal, B, staging-file, or receipt presence/freshness are not checked. Orphan sidecars are quarantined or pruned as audit cleanup and never override self-valid C. Ordinary atomic gameplay saves freely change active bytes while preserving marker/audit identifiers and layout-contract version.

If active C fails current-target validation and deterministic verified O/B is available, restore O; contradictory marker/schema/authority evidence emits `gd66.authority.contradictory_state`, and absence of any trusted recovery payload emits `gd66.transaction.no_trusted_active_payload`. Loss of a journal after `Replaced` or `DurableVerified` is therefore deterministic on restart: self-valid active C is `Verified C`/already committed, while invalid C follows those exact recovery rules. Absence of a receipt or journal is never evidence that C is unfinished.

Cloud copy, manual backup, reinstall restore, and save-file-only transfer therefore work from the complete active save alone, without journal, B, staging file, or receipt. Historical profiles, geometry, manifests/catalog bytes, configs, and serializer implementations need not remain shipped after finalization; future compatibility uses explicit target-schema/content migrations.

## 17. Writable-authority transition

Before replacement, legacy route and legacy room-content fields are the writable authorities; the detached candidate has none. The single atomic replacement simultaneously makes the complete canonical floor/graph the sole route-topology authority and `RoomContents` the sole room-content authority. Readers and writers bind once at load from the canonical marker and never fall back during a session. Legacy route/content fields become read-only evidence. `dungeonLayout`/`structureRuntime` remain the separate writable economic-structure subsystem on both sides of the replacement.

Marker/schema/graph/content disagreement is `gd66.authority.contradictory_state` and triggers verified recovery. Simultaneous legacy/canonical route writers, simultaneous legacy/canonical room-content writers, split category authority, or a second marker write are prohibited.

### Direct canonical new-save path after activation

When no save file exists, `CreateNew` resolves the release-owned `CanonicalLayoutContractSelection`, directly constructs the selected target schema and canonical marker with `CreationKind=NativeCanonical`, its positive `CanonicalLayoutContractVersion`, and `spatialFloors=[]`, empty canonical room-content authority, and approved defaults for independent economic/unrelated state. It creates no writable legacy route fields, O, B, migration descriptor, or journal. First persistence uses the ordinary durable atomic save path. Reload validates marker/schema and emits `gd66.success.already_committed` without legacy migration. Successful creation emits `gd66.success.native_canonical_save_created`; first persistence failure emits `gd66.write.native_save_persist_failed`, exposes no unsaved gameplay authority, and retries the no-file path without migration artifacts.

Phase 2 fixtures cover no-file creation, default preservation, empty canonical readers/writers, first ordinary save failure, successful reload, and proof that migration backup/journal paths were untouched. The exact future schema remains selected only in Phase 2.

## 18. Schema-version policy

Historical live schema was 6. Phase 2B2 selected schema 7 as the GD66 target; Phase 2B6B activates it only through §16's raw-before-legacy replacement and contextually validated complete-save authority.

## 19. Authoritative exact reason-code table

This table is the sole append-only ordinal registry. Phase 2 must provide at least one emitting fixture for every row; therefore a row with no exact condition is an unused alias and fails validation. “Trusted active state” is restricted to `Verified O`, `Verified C`, `Unchanged verified O`, `Unchanged verified C`, `No trusted active payload`, or `Not applicable`.

| Exact code | Classification | Continue C? | Trusted active state after emission | Gameplay | Player key? | Exact condition |
|---|---|---:|---|---|---:|---|
| `gd66.preflight.ready` | nonfatal diagnostic | Yes | Unchanged verified O | Allowed | No | Windows Editor or Windows Standalone targets a normalized, contained path on the lexical root's actual fixed NTFS volume, with no reparse component from that volume root through the save path; qualification may begin. |
| `gd66.preflight.platform_unsupported` | recoverable failure | No | Unchanged verified O | Allowed | Yes | Runtime is neither Windows Editor nor Windows Standalone; retain schema 6 and retry only on a supported target. |
| `gd66.preflight.path_invalid` | recoverable failure | No | Unchanged verified O | Allowed | Yes | Active-save path is null/empty, non-normalized, malformed, unsupported by `Path`, or exceeds the approved absolute path bound; correct the path before retry. |
| `gd66.preflight.path_redirected` | recoverable failure | No | Unchanged verified O | Allowed | Yes | Save containment fails, any existing component from the actual volume root through the save path is a reparse point, or `GetVolumePathNameW` resolves a mounted volume root different from the lexical drive root; select a local nonredirected directory before retry. |
| `gd66.preflight.volume_unsupported` | recoverable failure | No | Unchanged verified O | Allowed | Yes | The actual destination volume is not a fixed drive or its filesystem name is not ordinal-ignore-case `NTFS`; move the save to a qualified local NTFS volume before retry. |
| `gd66.preflight.native_probe_failed` | recoverable failure | No | Unchanged verified O | Allowed | Yes | A required Windows volume/path native probe fails or capability evaluation throws after path parsing; retain schema 6 and retry only after the OS/filesystem error is resolved. |
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
| `gd66.success.migrated` | success | No | Verified C | Allowed | No | Live journal reaches Finalized after initial C durable verification; pin lifetime ends. |
| `gd66.success.already_committed` | success | No | Verified C | Allowed | No | With no live journal, self-contained migrated/native/later-modified canonical save passes current target validation; receipt and historical byte equality are irrelevant. |
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
| `gd66.transaction.journal_malformed_with_verified_original` | recoverable failure | No | Unchanged verified O | Allowed | Yes | Exactly one malformed live journal exists, O is independently verified, and no more precise stage/hash/path condition applies. |
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
| `gd66.diagnostic.finalized_backup_quarantined` | nonfatal diagnostic | No | Verified C | Allowed | No | With no valid live journal, active C is self-valid and an invalid/orphan retained B is quarantined without affecting load; historical finalization need not be inferred. |
| `gd66.diagnostic.orphan_staging_quarantined` | nonfatal diagnostic | No | Verified C | Allowed | No | With no valid live journal, active C is self-valid and an orphan staging file is quarantined without affecting load. |
| `gd66.diagnostic.orphan_receipt_quarantined` | nonfatal diagnostic | No | Verified C | Allowed | No | With no valid live journal, active C is self-valid and a stale/malformed orphan receipt is quarantined without affecting load. |
| `gd66.transaction.rollback_source_missing` | recoverable failure | No | Verified C | Blocked | Yes | Before finalization B is invalid and no separate verified O exists; provisional C may only finish with all exact live evidence. |
| `gd66.layout_contract.selection_missing` | recoverable failure | No | No trusted active payload | Blocked | Yes | Brand-new save release configuration has no Active canonical layout-contract selection for target schema. |
| `gd66.layout_contract.selection_duplicate` | fatal failure | No | No trusted active payload | Blocked | Yes | Brand-new save release configuration has multiple Active layout-contract selections for target schema. |
| `gd66.marker.layout_contract_missing` | fatal failure | No | No trusted active payload | Blocked | Yes | Canonical marker omits/nonpositively encodes `CanonicalLayoutContractVersion`. |
| `gd66.marker.layout_contract_unsupported` | recoverable failure | No | Unchanged verified C | Blocked | Yes | Persisted positive layout-contract version is unsupported until explicit target/content migration. |
| `gd66.starter_profile.marker_mismatch` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Selected starter profile target schema/layout-contract key differs from persisted marker. |
| `gd66.starter_profile.missing` | recoverable failure | No | Unchanged verified C | Allowed | Yes | No Active starter profile matches target schema/layout-contract key. |
| `gd66.starter_profile.duplicate` | fatal failure | No | Unchanged verified C | Allowed | Yes | Multiple Active starter profiles match one target key. |
| `gd66.starter_profile.invalid` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Starter profile/shared-geometry reference or production validation fails. |
| `gd66.starter_profile.version_mismatch` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Required starter layout-contract/profile version is unsupported. |
| `gd66.transaction.multiple_live_attempts` | fatal failure | No | No trusted active payload | Blocked | Yes | More than one fully valid non-finalized journal binds to one normalized active save path. |
| `gd66.transaction.path_invalid` | recoverable failure | No | Unchanged verified O | Allowed | Yes | Generated relative filename/path violates grammar, normalization, containment, platform length, or same-directory rules. |
| `gd66.diagnostic.replaced_candidate_pending_durability` | nonfatal diagnostic | No | Verified C | Blocked | No | Journal stage Replaced and active C hash matches, but durable verification is incomplete. |
| `gd66.diagnostic.durable_candidate_pending_finalization` | nonfatal diagnostic | Yes | Verified C | Blocked | No | Exactly one valid live journal is at DurableVerified; advance it to Finalized regardless of optional receipt availability. |
| `gd66.diagnostic.finalization_receipt_write_failed` | nonfatal diagnostic | Yes | Verified C | Blocked | No | Exactly one valid live journal is at DurableVerified and optional receipt creation/flush/readback fails; quarantine partial bytes and continue to Finalized. |
| `gd66.transaction.finalization_receipt_invalid` | nonfatal diagnostic | Yes | Verified C | Blocked | No | Exactly one valid live journal identifies the exact attempt and its optional receipt is malformed, premature, or conflicts with that attempt; quarantine it and continue without distrusting C. |
| `gd66.transaction.finalized_stage_write_failed` | recoverable failure | Yes | Verified C | Blocked | No | Durable C is verified but writing/flushing/rereading the exact live journal at Finalized fails; retry that stage, never rewrite C. |
| `gd66.transaction.original_restored_stage_write_failed` | recoverable failure | Yes | Verified O | Blocked | No | Active O was atomically restored, flushed, reread, and hash-verified, but persisting `OriginalRestored` failed; retry only the terminal journal advancement while retaining O and B. |
| `gd66.success.native_canonical_save_created` | success | No | Verified C | Allowed | No | No-file path directly creates/persists canonical empty save. |
| `gd66.write.native_save_persist_failed` | recoverable failure | No | No trusted active payload | Blocked | Yes | No-file canonical first persistence fails; no active save exists. |
| `gd66.write.unsupported_room_selection` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Canonical writer/UI attempts unsupported Narrow Hall. |
| `gd66.write.first_write_validation_failed` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Detached canonical first-write candidate fails validation. |
| `gd66.write.atomic_save_failed` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Canonical mutation durable atomic save fails; prior C retained. |
| `gd66.write.room_removal_has_contents` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Room removal requested while assignments remain. |
| `gd66.write.capacity_reduction_invalid` | recoverable failure | No | Unchanged verified C | Allowed | Yes | Replacement would make retained contents exceed capacity. |
| `gd66.diagnostic.canonical_write_noop` | nonfatal diagnostic | No | Unchanged verified C | Allowed | No | Requested canonical placement is semantically identical. |
| `gd66.content.migration_blocked_narrow_hall` | recoverable failure | No | Verified O | Allowed | Yes | Legacy O contains effective Narrow Hall with no production room mapping. |
| `gd66.payload.invalid_primary` | fatal failure | No | No trusted active payload | Blocked | Yes | Wrapped root has a present, non-null `primary` whose JSON value is not an object. |
| `gd66.payload.workload_exceeded` | fatal failure | No | No trusted active payload | Blocked | Yes | Detached raw classification exceeds a caller-supplied input, nesting, per-object member, per-array element, per-string byte, or total scan-work limit. |

## 20. Player messaging and localization ownership

Save/Migration Engineering emits only exact §19 codes. For each row with “Player key?” Yes, the key is `save.migration.spatial.` followed by the full code unchanged, including dots and underscores—for example `save.migration.spatial.gd66.content.outcome_mismatch`. No punctuation/case transformation or alias is allowed. UX/Localization must author that exact key before activation; GD66 adds no entries. Text never determines identity or gameplay.

## 21. Stage-exact interruption, recovery, rollback, retry, and idempotence

Journal discovery precedes every row on initial load and every application restart. “No valid live journal” means zero journals satisfy §16's complete binding rule; orphan files provide no authority. Each observed condition below is mutually exclusive after applying the rows top-to-bottom. Registry codes not concerned with interruption are emitted by their exact §8/§19 fixtures rather than being overloaded here.

| Observed state/stage | Deterministic trusted evidence | Trusted active state / gameplay | Retain or quarantine | Exact code | Auto retry? | Intervention? |
|---|---|---|---|---|---:|---:|
| More than one valid live journal | two or more fully valid non-finalized bindings to this active path | No trusted active payload / blocked | retain all untouched for explicit selection | `gd66.transaction.multiple_live_attempts` | No | Yes |
| Exactly one valid journal at `DescriptorPinned`; B absent/incomplete | journal/descriptor valid and active O matches | Verified O / allowed after attempt quarantine | retain O; quarantine partial B/journal before fresh retry | `gd66.transaction.backup_incomplete` | Yes | No |
| Exactly one valid journal at `BackupVerified`; C absent | verified O/B and stage | Verified O / allowed | retain O/B/journal | `gd66.transaction.candidate_absent` | Yes | No |
| Exactly one valid journal at `CandidateVerified`; staged C valid and O active | B/C/descriptor hashes and stage | Unchanged verified O / allowed | retain all; resume replacement | `gd66.diagnostic.staged_candidate_verified` | Yes | No |
| Exactly one valid journal at `Replaced`; active C matches | expected C hash/descriptor and verified B | Verified C / blocked until durability step completes | retain all | `gd66.diagnostic.replaced_candidate_pending_durability` | Yes | No |
| Exactly one valid journal at `DurableVerified`; no receipt attempted | durable C, journal/pins, B | Verified C / blocked only while finalization step runs | retain all | `gd66.diagnostic.durable_candidate_pending_finalization` | Yes | No |
| Optional receipt creation/flush/readback fails at `DurableVerified` | exact live journal and durable verified C | Verified C / blocked only while continuing to Finalized | quarantine partial receipt; retain other evidence | `gd66.diagnostic.finalization_receipt_write_failed` | Yes, immediately | No |
| Optional receipt exists but conflicts with exact live attempt | exact live journal and durable verified C | Verified C / blocked only while continuing to Finalized | quarantine receipt; retain other evidence | `gd66.transaction.finalization_receipt_invalid` | Yes, immediately | No |
| Writing/flushing/rereading `Finalized` fails | exact live journal at DurableVerified and durable verified C | Verified C / blocked while stage retry runs | retain C and exact live evidence | `gd66.transaction.finalized_stage_write_failed` | Yes | No |
| Journal reaches `Finalized` after durable verification | journal/descriptor and initial C hash valid | Verified C / allowed | retain C; archive/prune optional audit evidence | `gd66.success.migrated` | No | No |
| No valid live journal; active canonical payload is self-valid—migrated, native, later modified, copied without sidecars, or restarted after journal deletion at `Replaced`/`DurableVerified`/failed Finalized advancement | complete current-target validation from active bytes alone | Verified C / allowed | retain C; ignore/quarantine orphan sidecars | `gd66.success.already_committed` | No | No |
| No valid live journal; self-valid C plus invalid/orphan B | current-target validation of C; B has no live binding | Verified C / allowed | quarantine/remove B | `gd66.diagnostic.finalized_backup_quarantined` | No | No |
| No valid live journal; self-valid C plus orphan staging file | current-target validation of C; staging has no live binding | Verified C / allowed | quarantine staging file | `gd66.diagnostic.orphan_staging_quarantined` | No | No |
| No valid live journal; self-valid C plus stale/malformed receipt | current-target validation of C; receipt has no live binding | Verified C / allowed | quarantine/prune receipt | `gd66.diagnostic.orphan_receipt_quarantined` | No | No |
| No valid live journal; valid legacy O | O source/schema/semantic validation | Unchanged verified O / allowed | retain O; start fresh migration | `gd66.diagnostic.no_journal_legacy_valid` | Yes | No |
| One malformed journal, independently verified O, and no more precise path/hash case | O source/hash valid; malformed file is not live | Unchanged verified O / allowed | quarantine journal | `gd66.transaction.journal_malformed_with_verified_original` | Yes | No |
| One valid live journal but active hash matches neither pinned O nor expected C | descriptor/B valid, active bytes unrecognized | No trusted active payload / blocked until verified O restore | quarantine active/staged; retain B/journal | `gd66.transaction.active_payload_unknown` | No | Yes if restore fails |
| Invalid B before finalization; separate O verified | O independently matches descriptor | Unchanged verified O / allowed after abandoning attempt | retain O; quarantine B/C/journal | `gd66.transaction.backup_failed` | Fresh attempt | No |
| Invalid B before finalization; no verified O; provisional C and all other exact live evidence can still complete | live descriptor/pins/C through Replaced or DurableVerified | Verified C / blocked until completion | quarantine B; retain exact live evidence | `gd66.transaction.rollback_source_missing` | Exact attempt only | Maybe |
| Invalid active C; deterministic verified O/B exists | B matches descriptor O bytes and restores successfully | Verified O / allowed | quarantine C; retain restored O and audit evidence | `gd66.success.recovered_original` | Yes | No |
| Durable active-C verification fails and O/B is recoverable | exact live descriptor and verified O/B | Verified O / allowed after restoration | quarantine C; retain attempt evidence | `gd66.transaction.durability_failed` | Same pins only | No |
| Restoration attempted but restored O cannot be verified | neither restoration nor C is trusted | No trusted active payload / blocked | retain all for support | `gd66.transaction.recovery_failed` | No | Yes |
| Invalid active C, no valid live journal, and schema/marker/writer evidence is internally contradictory | parseable evidence directly conflicts | No trusted active payload / blocked | quarantine active and orphans | `gd66.authority.contradictory_state` | No | Yes |
| Invalid active C and no deterministic verified O, B, staged C, or active C exists | all available bytes fail applicable validation | No trusted active payload / blocked | quarantine all | `gd66.transaction.no_trusted_active_payload` | No | Yes |
| Sole journal pins a different O than independently verified active O | active O hash/source valid | Unchanged verified O / allowed | quarantine stale journal | `gd66.transaction.stale_journal_original_mismatch` | New attempt | No |
| Sole journal descriptor fingerprint is invalid | descriptor bytes/fingerprint disagree; O independently valid | Verified O / allowed | quarantine attempt | `gd66.transaction.input_fingerprint_mismatch` | Fresh attempt | No |
| Required unfinished generic input absent | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_input_missing` | When exact pin returns | Maybe |
| Required unfinished generic input hash/version mismatch | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_input_hash_mismatch` | When exact pin returns | Maybe |
| Required unfinished migration profile absent | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_profile_missing` | When exact pin returns | Maybe |
| Required unfinished migration profile hash mismatch | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_profile_hash_mismatch` | When exact pin returns | Maybe |
| Required unfinished manifest/catalog absent | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_spatial_input_missing` | When exact pin returns | Maybe |
| Required unfinished manifest/catalog hash mismatch | exact descriptor plus independently verified O/B | Verified O / allowed after restoration | quarantine C/journal; retain O/B | `gd66.transaction.pinned_spatial_input_hash_mismatch` | When exact pin returns | Maybe |
| Generated transaction path fails grammar, length, normalization, containment, or same-directory validation | independently verified O | Unchanged verified O / allowed | create no attempt files | `gd66.transaction.path_invalid` | After repair | No |

Before any changed-dependency or repaired-O attempt, the sole prior live attempt must reach `Finalized`, restore verified O, or be quarantined after verified O is established. Finalized audit evidence is never counted as live. Same O/same fingerprint retries are deterministic; dependency or O changes produce a new compact identity only after this one-live-attempt gate.

### Missing production save-workload configuration authority

Phase 2B6B cannot activate until Save/Data, Performance, and QA approve one dedicated, explicitly injected production configuration contract for the save-specific validation workload. The existing spatial-content `validation_limits.json` is not this authority and cannot be copied, translated, inferred, or used as a fallback. No numeric values are approved by this working PR.

The owner-review sizing analysis and provisional, explicitly unapproved numeric proposal are recorded in [`gd66-save-spatial-migration-limit-sizing-evidence.md`](gd66-save-spatial-migration-limit-sizing-evidence.md); that evidence does not create production authority.

`TryPublishValidated` is only a runtime projection seam; its `JsonUtility` materialization is not final persistence ownership. Before activation, the live canonical save session must retain the complete validated save bytes or equivalent lossless root/primary extension evidence and merge that evidence into every ordinary canonical save. Otherwise later canonical mutations could drop unknown members that migration preserved.

The smallest proposed contract is one separately authored `save_spatial_migration_limits` versioned record with these semantically explicit positive fields:

| Consumer | Required field | Unit / distinct meaning |
|---|---|---|
| `RawSavePayloadClassifier` | `MaximumRawSaveBytes` | Exact active-payload bytes inspected before deserialization |
| `RawSavePayloadClassifier` | `MaximumRawNestingDepth` | Simultaneously open raw containers |
| `RawSavePayloadClassifier` | `MaximumRawObjectMembers` | Members in one raw object |
| `RawSavePayloadClassifier` | `MaximumRawArrayElements` | Elements in one raw array |
| `RawSavePayloadClassifier` | `MaximumRawStringBytes` | Encoded bytes in one raw string token |
| `RawSavePayloadClassifier` | `MaximumRawScanWork` | Total lexical scan-work charges |
| `SpatialSerializedInputLimits` and strict canonical/journal/descriptor parsers | `MaximumSerializedInputBytes` | Bytes accepted or emitted by one strict serialized contract |
| same | `MaximumSerializedParsedNodes` | Logical nodes parsed by one strict contract |
| same | `MaximumSerializedCollectionRecords` | Collection records parsed or emitted by one strict contract |
| same | `MaximumSerializedStringCharacters` | Cumulative decoded UTF-16 property-name and value units |
| same | `MaximumSerializedDiagnostics` | Collected validation issues before stable exhaustion |
| `CanonicalSpatialSaveContracts` | `MaximumCanonicalSpatialRecords` | Canonical floors/rooms/nodes/edges/fixed/content/semantics records |
| `CanonicalSpatialSaveContracts` | `MaximumCanonicalMaterializedTiles` | Canonical saved edge-footprint tiles inspected/materialized |
| `DetachedWholeSaveCandidateSerializer` | `MaximumWholeSaveCandidateBytes` | Complete schema-7 candidate output bytes |
| same | `MaximumCopiedSourceValueBytes` | Cumulative recognized source-value bytes copied losslessly |
| same | `MaximumUnknownMembers` | Preserved unknown root plus primary member count |
| same | `MaximumUnknownMemberBytes` | Cumulative preserved unknown-member value bytes |

All seventeen fields may share one configuration **authority and lifecycle**, but their units and accounting semantics remain distinct and code must not derive one from another. Owner approval may deliberately assign equal numeric values to byte or record fields, but equality is configuration, not an implicit fallback. In particular, raw string bytes cannot be inferred from decoded UTF-16 characters; raw scan work cannot be inferred from input bytes; canonical spatial records cannot be inferred from strict parsed nodes/collection records; candidate, copied-source, and unknown-member byte totals have different accumulation rules; and the canonical saved-tile workload is not the existing production-content materialized-tile limit. The composed canonical serialization and recovery/transaction contexts must receive these parsed fields explicitly. This working PR removes the detached-era derivation in `DetachedSpatialMigrationTransaction.IsCanonicalSchemaSeven` that reused `SpatialSerializedInputLimits.MaximumCollectionRecords` for both canonical fields; transaction validation now preserves the caller-injected composed limits, while live production composition remains blocked until the dedicated values exist. Missing, malformed, incomplete, duplicated, nonpositive, or unapproved configuration fails closed before filesystem selection or save mutation.

## 22. Phase 2 dependency breakdown

1. Add the direct-authored shared geometry, migration-profile, and native-starter-profile schema/input; distinct selection/lifecycle validation; explicit composition; runtime validation; expanded pre-build gate; focused tests/evidence.
2. Add/round-trip inactive saved-floor/fixed/content/room-semantics shapes, canonical marker/native creation kind, and then select the target save schema.
3. Add canonical serializer, compact `SpatialMigrationInputDescriptor` identity, relative-path validator, one-live-attempt discovery, exact journal stages, finalization receipt, and unfinished-attempt pin lookup.
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
- [x] Migration and native starter profiles have distinct selection keys and share one immutable geometry authority.
- [x] Receipt is optional audit evidence; self-valid C loads without any sidecar, while an exact valid live journal alone invokes pinned recovery.
- [x] Journal stages follow filesystem verification order and only one live attempt is permitted.
- [x] Compact transaction filenames are relative, contained, length-bounded, and same-directory.
- [x] Profile input has direct-authoring, composition, runtime, and expanded build-gate ownership.
- [x] One complete atomic replacement is the sole schema/authority switch.
- [x] No hidden fallback, partial publication, localized identity, or dual route/content writers.

## 25. Candidate approval statement

PR #187 proposes approval of this route precedence, independent economic-structure preservation, canonical room-content and saved-floor contracts, raw-load interception, schema-specific fixtures, stable identities/geometry, and one-replacement transaction for later Phase 2 implementation. It is not repository-approved until merged and changes no present behavior. Unsupported states retain O with stable diagnostics; Phase 2 remains blocked until this candidate is approved and merged.

### Phase 2B3 inactive serialization and transaction-metadata contracts

Phase 2B3 establishes technical identities in the single `SpatialMigrationContractIdentity` authority: canonical serializer `gd66.serializer.canonical_spatial_save` version **1**, authority-marker contract version **1**, migration contract version **1**, and journal schema version **1**. These contracts remain detached from `SaveRoot`, `SaveData`, and `SaveService`.

Canonical detached spatial bytes are compact strict UTF-8 without BOM, whitespace, or trailing newline. Objects use declared fixed field order, arrays retain canonical ordinal/domain order, integers and enum numeric values use invariant decimal form, and strings use deterministic JSON escaping. Parsing rejects malformed UTF-8/JSON, BOM, boundary whitespace, unknown/duplicate/case-ambiguous/out-of-order fields, wrong types, unsupported/overflowing integers, undefined enums, workload excess, structurally invalid state, noncanonical collection ordering, and any bytes that differ from reserialization. The authority marker contains only layout contract/creation identity and, for migrated state, transaction ID plus descriptor fingerprint; it never stores candidate-byte hashes.

`SpatialMigrationInputDescriptor` pins, in fixed order: original payload SHA-256; raw source schema and envelope classification; selected target schema; authority-marker and migration contract versions; migration profile ID/version/hash; shared geometry ID/version/hash; production manifest and catalog hashes; ordinal validation-input ID/hash records; legacy gameplay-configuration hash; and canonical serializer ID/version. Its fingerprint is SHA-256 over those exact canonical bytes. Transaction identity bytes are exactly `{"OriginalPayloadSha256":"<hash>","InputFingerprintSha256":"<hash>"}`; their SHA-256 is represented as the exact 69-character lowercase ID `gd66-<hash>`.

For migration contract version 1, the required external `ValidationInputHashes` ID set is empty. Profile, geometry, manifest, catalog, legacy gameplay configuration, canonical serializer, marker contract, and migration contract are not duplicated into that extension set because each is already a mandatory named descriptor authority above. Any extension entry is therefore unexpected for contract 1; a later contract version must register stable IDs and canonical byte authorities here before accepting nonempty extension pins.

For active `<stem><ext>`, pure naming derives `<stem>.<transactionId>.journal.json`, `.original.bak`, `.candidate.tmp`, and `.finalized`. Inputs are single normalized relative filenames, the stem limit is 80 UTF-16 code units, generated-name limit is 180, and normalized absolute resolution uses a caller-supplied limit (240 for the Windows contract), exact-directory containment, platform path APIs, and ordinal comparisons. This pure check performs no I/O and cannot prove symlink/reparse-point containment; the future filesystem executor must verify that separately.

Journal stages append numerically as `DescriptorPinned`, `BackupVerified`, `CandidateVerified`, `Replaced`, `DurableVerified`, `Finalized`, and terminal recovery outcome `OriginalRestored`. Forward transitions are adjacent; `BackupVerified`, `CandidateVerified`, `Replaced`, or `DurableVerified` may transition to `OriginalRestored`; `Finalized` and `OriginalRestored` are terminal. Backup evidence is prohibited at `DescriptorPinned` and otherwise must equal the original hash. Candidate evidence is required from `CandidateVerified` through `Finalized`, prohibited before verification, and optional only for the restoration outcome because restoration may occur before or after candidate verification. Receipt naming is absent before `DurableVerified`, optional at `DurableVerified`/`Finalized`, and absent for restoration. Validation recomputes every descriptor/transaction identity and deterministic filename. Caller-provided limits own bytes, parsed nodes, collection records, cumulative strings, diagnostics, and the existing canonical spatial record/materialized-tile budgets. Cumulative JSON string accounting includes every decoded property name and string value in UTF-16 code units, so a valid non-BMP pair consumes two units. Lone or incorrectly paired surrogates fail as malformed JSON. Diagnostic collection stops at the caller boundary; the first additional issue replaces the last available slot with stable `WorkloadExceeded`, including the exact one-diagnostic case, and public contract APIs fail closed rather than leaking malformed-input exceptions. JSON structure parsing uses explicit heap-backed frames rather than input-controlled CLR recursion. Parsing and emission share byte, logical-node, array-record, UTF-16 string, and diagnostic budgets; journal parsing consumes the already parsed descriptor node once, while canonical descriptor emission supplies exact fingerprint bytes without reparsing. Migrated authority markers require the exact `gd66-` transaction grammar and lowercase descriptor SHA-256, and relative filenames reject Windows reserved device tokens platform-independently.

This packet performs no filesystem reads, writes, discovery, backup, replacement, receipt creation, journal advancement, recovery, whole-save serialization, or runtime activation. Live schema remains 6 and GD66 Phase 2 is not complete.
