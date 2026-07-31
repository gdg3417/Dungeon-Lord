# GD66 final spatial save and migration design

## 1. Status and approval boundary

**Status: APPROVED DESIGN; implementation remains blocked to Phase 2.** This documentation-only packet is based on merged PR #186 at `7f62709c9c73164c549ee31a403c410f8c05c902`. It changes no C#, serialized shape, content, localization, test, runtime behavior, or player outcome. Save schema remains **6**; production Dungeon Spatial content remains **inactive**; the existing ordered two-room state remains runtime and save authority. No migration and no writable-authority transition has occurred.

Labels used below are **Fact** (existing approved fact), **Observed** (current implementation behavior), **Decision** (new GD66 approval), **Assumption**, **Unsupported**, and **Phase 2** (deferred implementation).

## 2. Current repository and save baseline

| Item | Reconciled state |
|---|---|
| Repository | `main`/PR #186 merge commit `7f62709c9c73164c549ee31a403c410f8c05c902` |
| Dependency sequence | PRs #181–#186 are consecutive merged GD65B implementation packets; no later commit exists in this checkout |
| Save root | `SaveRoot.schemaVersion`; `SaveMigration.LatestSchemaVersion = 6` |
| Active layout | `mvpRoomSlotAssignments`, then compatibility readers over `mvpDungeonFloorLayout` and `mvpDungeonPlacements`; legacy `dungeonLayout` remains preserved |
| Spatial layout | inactive `FloorSpatialLayout` rooms/nodes/edges only |
| GD66 effect | design approval only; Phase 2 exclusively owns implementation |

**Verification limitation:** repository history proves that the checked-out baseline is the PR #186 merge and contains merged PRs #181–#186. The checkout has no Git remote; therefore hosted PR metadata and the absence of a newer remote merge cannot be independently queried here. The exact required commit and dependency sequence nevertheless match the committed graph, so this is not a material design blocker.

## 3. Sources inspected

The approval reconciles `AGENTS.md`, `README.md`, the post-GD60 plan, GD62/GD63/GD65B planning and authoring contracts, the test-stage matrix, GD65B evidence, locked Specs 00/19/28/29/38 and the cross-spec glossary. It also reconciles active `SaveMigration`, `SaveService`, `SaveRoot`, `SaveData`, migration/lifecycle tests, the four legacy models/resolvers/writers, `GameRoot`, spatial contracts/canonicalization/validators, all normalized `ContentAuthoring/DungeonSpatial/` tables, generated production JSON, and local merge/diff history for PRs #181–#186. Production geometry below is derived from the normalized records and checked against the generated catalog, not display names.

## 4. Current legacy data models and observed runtime behavior

| Model | Records and writer | Observed reader behavior |
|---|---|---|
| `mvpRoomSlotAssignments` | `(FloorIndex, RoomIndex)`, room option and category arrays; writers update a matching first record, add Room 1, and increment collection `NextRevision` (there is no per-room revision) | supported Floor 0 indices 0–1 are grouped by room index and **last list record wins**; malformed room option silently becomes Basic; category arrays normalize/clamp |
| `mvpDungeonFloorLayout` | nodes keyed by `(FloorIndex, NodeIndex)`, with `SlotId`, category, option, `Revision`; writer targets nodes and advances `NextRevision` | duplicate coordinate selects greatest revision with ordinal tie-breaks; nodes overlay categories from legacy placements |
| `mvpDungeonPlacements` | entries with category, option, `Revision`; placement writer replaces/records categories | valid entries are category-ordered then revision-ordered; compatibility composition can supply categories absent from floor nodes |
| `dungeonLayout` | `DungeonSlot(FloorIndex, SlotIndex, StructureId)`; placement service mutates a slot | ordered by numeric floor/slot; it is not consumed by the current ordered-room resolver |

**Observed:** ordinary saving writes a temporary file then deletes/moves when configured, creates timestamped backups only after the active write, archives corrupt input, and may create a new save on load failure. That is not a verified, recoverable graph-migration transaction. Some messages are currently literals; Phase 2 must use localization ownership specified below.

**Decision:** migration is intentionally stricter than current compatibility readers. It never overlays floor-layout and placement categories and never adopts list order as durable meaning.

## 5. Approved whole-model precedence

The sole whole-model precedence is:

1. `mvpRoomSlotAssignments`
2. `mvpDungeonFloorLayout`
3. `mvpDungeonPlacements`
4. `dungeonLayout`

After semantic-presence evaluation, the highest present model is the **only** candidate input. Lower models may be inspected only to emit diagnostics and are preserved byte-for-byte in the verified backup and unchanged legacy fields. Records, categories, rooms, and placements are never merged across models. A valid winner remains valid when a lower model disagrees (`gd66.legacy.lower_authority_conflict`, nonfatal). An invalid winner fails closed; there is no fall-through.

## 6. Semantic-presence rules

Presence is evaluated on deserialized original fields before defaults, normalization, filtering, or fallback:

| Model | Semantically absent | Semantically present |
|---|---|---|
| assignments | null collection; null/empty `Rooms`; or only null records | any non-null room record, including blank, out-of-range, or malformed records (corruption cannot be hidden) |
| floor layout | null state; null/empty nodes; or starter nodes whose category and option are both empty, revision is 0, and identity equals the expected empty starter coordinate/slot | any non-null node with an assigned field, nonzero revision, unexpected identity/coordinate, or any extra node |
| placements | null state; null/empty entries; or only null entries | any non-null entry, including blank/malformed IDs or revision |
| `dungeonLayout` | null; null/empty slots; or slots with empty `StructureId` and valid declared empty dimensions | any nonblank `StructureId`, or any slot/dimension inconsistency that must fail rather than disappear |

An entirely absent/semantically empty save produces **no active floor graph**. This preserves current absence of invented placed state; an endpoint-only graph is prohibited. Empty non-winning containers do not block a populated lower model.

## 7. Duplicate and conflict rules

All equality, comparison, and sorting of strings is `StringComparison.Ordinal`/`StringComparer.Ordinal`; IDs are not trimmed or case-folded into validity.

| Model | Duplicate identity | Decision |
|---|---|---|
| assignments | `(FloorIndex, RoomIndex)` | invalid: no per-record revision exists; current last-record-wins is not persistent authority |
| floor layout | `(FloorIndex, NodeIndex)` or duplicate nonempty `SlotId` | greatest `Revision` wins only when unique; equal greatest revisions are invalid even if payloads agree; negative revision invalid |
| placements | `CategoryId` | greatest `Revision` wins only when unique; equal greatest revisions invalid; negative revision invalid |
| `dungeonLayout` | `(FloorIndex, SlotIndex)` | invalid; no stable discriminator exists |

Duplicate stable instance/node/edge IDs in a candidate always fail `gd66.id.collision`. Out-of-range records fail `gd66.legacy.record_out_of_range`. A valid winner/lower disagreement is diagnostic only; an invalid winner is fatal regardless of a valid lower model.

## 8. Schemas 1 through 6 fixture matrix

Coordinates use variants `E` (none), `R1` (one Basic room), and `R2` (two Basic rooms) defined in §12. `D` means direct doorway. Every attempted legacy migration first creates and verifies backup `B`; success atomically commits `C`, failure leaves original `O` active. The same cases are instantiated at each source schema 1, 2, 3, 4, 5, and 6 unless a field did not yet exist, in which case its representation is absent and precedence selects the next present field.

| Fixture(s) | v | Present; winner | Expected route / geometry / connection | Classification | Backup; active; retry |
|---|---:|---|---|---|---|
| `empty`, `empty-dungeon-layout` | 1–6 | none | no graph (`E`) | `gd66.success.no_layout` | no migration backup; O; idempotent no-op |
| `dungeon-room0`, `dungeon-two-room` | 1–6 | dungeon; dungeon | entrance→room0→completion (`R1`, D); or →room1→completion (`R2`, D) | success only for approved IDs | B; C; already-committed no-op |
| `placements-only-basic` | 1–6 | placements; placements | `R1`, Basic room identities, D | `gd66.success.migrated` | B; C; deterministic retry |
| `floor-nodes-only-basic` | 1–6 | floor; floor | `R1`, Basic, D | success | B; C; deterministic retry |
| `assignments-room0-basic` | 1–6 | assignments; assignments | `R1`, Basic, D | success | B; C; deterministic retry |
| `assignments-two-basic` | 1–6 | assignments; assignments | `R2`, room indices 0/1, D | success | B; C; deterministic retry |
| `higher-lower-agree` | 1–6 | ≥2; highest | winner's `R1`/`R2` | success + `gd66.legacy.lower_authority_agrees` | B; C; no-op |
| `higher-lower-conflict` | 1–6 | ≥2; highest | winner only | success + `gd66.legacy.lower_authority_conflict` | B; C; no-op |
| `duplicate-*` | 1–6 | affected winner | none | model-specific `gd66.legacy.duplicate_*` | B; O; same failure until repaired |
| `out-of-range-*` | 1–6 | affected winner | none | `gd66.legacy.record_out_of_range` | B; O; retry allowed |
| `narrow-hall`, `missing-room` | 1–6 | any winner | none | `gd66.content.unmapped_legacy_room` / `missing_production_room` | B; O; retry after catalog/save repair |
| `missing-corridor` | 1–6 | graph-shaped/candidate | none; supported compatibility routes require no corridor | `gd66.content.missing_production_corridor` if a physical corridor is referenced | B; O; retry |
| `malformed-id`, `duplicate-id` | 1–6 | any winner/candidate | none | `gd66.id.malformed` / `gd66.id.collision` | B; O; retry |
| `partial-graph-shaped` | 1–6 | authority marker/graph evidence | none | `gd66.transaction.partial_state` | recovery selects verified O or verified committed candidate; never partial |
| `corrupt-payload` | 1–6 | unreadable | none | `gd66.payload.unreadable` | preserve/copy original if possible; no new active save under migration flow |
| `current-version-idempotence` | 6 | legacy authority | normal candidate once; repeated identical | success then `gd66.success.already_committed` | one B; C unchanged |
| `interrupted-*`, `recovery-*` | 1–6 | transaction journal | candidate never exposed | `gd66.transaction.interrupted` then `recovered_original` or `already_committed` | verified B/O restored or verified C retained |
| `retry-failed-candidate` | 1–6 | original winner | same deterministic candidate identity | original stable failure, then success only after cause repaired | existing verified B reused; O then C |
| `already-migrated` | future >6 | canonical marker + verified C | existing graph unchanged | `gd66.success.already_committed` | no new B/write |
| `contradictory-marker` | any | marker/payload mismatch | none | `gd66.authority.contradictory_state` | recover verified O/C; otherwise unrecoverable |

Expected identities are the §11 templates; exact anchors/orientations are §12. Monster/trap/loot assignments remain legacy evidence and must be preserved; spatial room migration does not invent unsupported per-content coordinates.

## 9. Semantic compatibility route

The only supported routes are distinct entrance → legacy internal Room 0 → optional legacy internal Room 1 → distinct completion terminal. “Room 1” and “Room 2” are localized presentation labels only. Persistent identity uses floor identity, semantic role, and zero-based legacy room index. Room 1 without Room 0 is invalid (`gd66.legacy.route_gap`). Empty state produces no graph.

## 10. Content compatibility mapping

| Legacy value | Class | Production definition | Decision |
|---|---|---|---|
| `placement.option.room.basic` | room | `spatial.room.basic` | direct mapping: both represent the Basic room; production 4×4 gross footprint and 2/2/2 capacities are compatible with the configured legacy Basic profile |
| implicit default Basic selected by compatibility readers but no semantic record | implicit behavior | none | **no fallback**; absence remains no graph |
| blank room ID in a semantically present room record | invalid legacy | none | fail `gd66.content.invalid_legacy_content`; do not inherit current resolver fallback |
| `placement.option.room.narrow_hall` | legacy room | none | **unmapped**; it is not a corridor and must not map to `spatial.corridor.straight_stone`, `spatial.room.rectangle`, or any substitute |
| any monster/trap/loot option | room contents | no spatial instance definition in current graph | preserve in legacy evidence; it does not choose room geometry |
| `dungeonLayout.StructureId = placement.option.room.basic` | room | `spatial.room.basic` | direct mapping when slots form supported indices 0/1 |
| any other `dungeonLayout.StructureId` or historical value | unknown structure | none | unmapped until separately approved evidence exists |

No safe fallback is approved. A known mapping whose production record is absent is `missing_production_content`; a present record failing catalog/geometry validation is `invalid_production_content`; malformed/category-wrong legacy input is `invalid_legacy_content`; a recognized legacy value without same-class mapping is `unmapped_legacy_content`. Every failure leaves O active, rewrites/deletes nothing, and publishes no partial graph.

## 11. Stable ID derivation rules

Inputs must already be valid canonical identifiers: exact lowercase ASCII `[a-z0-9]+(?:[._-][a-z0-9]+)*`, compared ordinal. Floor index is zero-based invariant decimal `D2` (`00` here); room index is invariant decimal `D2`. Separator is exactly `.`. No Unicode normalization, trimming, case conversion, or lossy escaping occurs: noncanonical input fails `gd66.id.malformed`.

| Identity | Exact template / example for Floor 0 |
|---|---|
| floor instance | `compat.floor.{floorIndex:D2}` → `compat.floor.00` |
| entrance structure | `{floorId}.fixed.entrance` → `compat.floor.00.fixed.entrance` |
| entrance node | `{floorId}.node.entrance` |
| room instance | `{floorId}.legacy-room.{roomIndex:D2}` → `compat.floor.00.legacy-room.00` |
| room node | `{floorId}.node.legacy-room.{roomIndex:D2}` |
| completion structure | `{floorId}.fixed.completion` |
| completion node | `{floorId}.node.completion` |
| direct edge | `{floorId}.edge.direct.{sourceRole}.{destinationRole}` where roles are `entrance`, `legacy-room-{index:D2}`, `completion` |
| physical corridor instance (reserved, unused by supported routes) | `{floorId}.corridor.{sourceRole}.{destinationRole}` |
| corridor node (reserved) | `{floorId}.node.corridor.{sourceRole}.{destinationRole}` |
| corridor edge (reserved) | `{floorId}.edge.corridor.{segment:D2}.{sourceRole}.{destinationRole}` |

Examples: empty has no IDs; R1 has `...node.entrance`, `...node.legacy-room.00`, `...node.completion` and edges `...edge.direct.entrance.legacy-room-00`, `...edge.direct.legacy-room-00.completion`; R2 inserts room/node `01` and edges 00→01 and 01→completion. Every generated ID is inserted into one ordinal set across all instance/node/edge namespaces; collision fails `gd66.id.collision` before serialization.

Runtime hashes, `GetHashCode`, random GUIDs, timestamps, localization keys/text, UI labels/player numbering, catalog array position, dictionary iteration, and incidental list order are prohibited. Definition IDs stay the exact production IDs; aliases are prohibited.

## 12. Exact coordinate and orientation proof

Production Floor 1 is `spatial.floor.01`, bounds `[0,12)×[0,12)`, capacity 60, with no unavailable mask. All approved compatibility instances use orientation `Zero`.

### R1 — entrance, Room 0, completion

| Role / definition / instance | Anchor | Occupied tiles in canonical `(X then Y)` order | Connections `(point: global coordinate, facing)` | Use / cumulative |
|---|---|---|---|---:|
| entrance / `spatial.fixed.entrance_hall` / `{floorId}.fixed.entrance` | (0,0) | (0,0),(0,1),(1,0),(1,1),(2,0),(2,1) | `route`: (1,1), North | 6 / 6 |
| room0 / `spatial.room.basic` / `{floorId}.legacy-room.00` | (0,2) | rectangle `x=0..3,y=2..5`, ordered X then Y | `south`: (1,2), South; `north`: (1,5), North | 16 / 22 |
| completion / `spatial.fixed.completion_terminal` / `{floorId}.fixed.completion` | (1,6) | (1,6),(1,7),(2,6),(2,7) | `route`: (1,6), South | 4 / 26 |

### R2 — entrance, Room 0, Room 1, completion

The first two rows equal R1.

| Role / definition / instance | Anchor | Occupied tiles in canonical `(X then Y)` order | Connections `(point: global coordinate, facing)` | Use / cumulative |
|---|---|---|---|---:|
| entrance | (0,0) | as R1 | route (1,1), North | 6 / 6 |
| room0 | (0,2) | `x=0..3,y=2..5` | south (1,2), South; north (1,5), North | 16 / 22 |
| room1 / `spatial.room.basic` / `{floorId}.legacy-room.01` | (0,6) | `x=0..3,y=6..9` | south (1,6), South; north (1,9), North | 16 / 38 |
| completion | (1,10) | (1,10),(1,11),(2,10),(2,11) | route (1,10), South | 4 / 42 |

**Proof:** transformed Zero tiles are anchor plus authored offsets. Every x is 0–3 and every y is 0–11, so all tiles are in bounds. Y ranges are disjoint, so footprints do not overlap. All reserved-offset tables are empty. Uses equal occupied structural tiles (6+16+4=26; 6+16+16+4=42), including both fixed structures, and are ≤60. Canonical `TileCoordinate.CompareTo` ordering is X then Y. Each paired boundary socket is Manhattan-adjacent: (1,1)/(1,2), (1,5)/(1,6), and (1,9)/(1,10); facings are opposite North/South and both use mutually compatible `spatial.socket.standard_passage`. Identical semantic input therefore produces identical anchors, tiles, and order. Empty state has no layout to place.

## 13. Direct-doorway and physical-corridor decisions

Every R1/R2 connection above is a footprint-free `DirectDoorway`, `Classification=Required`, blank `CorridorDefinitionId`, null footprint, empty optional-branch ID, exact source/destination nodes in semantic route order, and the §11 stable edge ID. No supported legacy state requires a physical corridor. A corridor must not be created merely to supply an edge. Reserved corridor ID templates may be activated only by a later approved, separately proven mapping using legal `spatial.corridor.straight_stone` width 1, length 1–4, orientation, sockets, footprint, bounds, and capacity.

## 14. Fixed-structure persistence requirements

**Observed gap:** `FloorSpatialLayout` persists rooms, nodes, and edges but no placed entrance/completion instances, anchors, or orientations. **Decision:** before any candidate builder, the first Phase 2 PR must add and Unity-JSON-round-trip an **inactive** fixed-structure collection containing, per record: `FixedStructureInstanceId`, `FixedStructureDefinitionId`, `FloorId`, `Anchor` (`TileCoordinate`), `Orientation` (`CardinalOrientation`), and `SemanticKind` (`Entrance` or `CompletionTerminal`). It must canonicalize by ordinal instance ID, reject duplicates, and remain inactive. Only a later Phase 2 step may build migration candidates after this complete serialized shape exists.

## 15. Candidate validation sequence

Detached validation must: validate source/presence/duplicates; resolve direct mappings; derive IDs and reject collisions; resolve fixed/room definitions; materialize oriented footprints; enforce bounds, reserved tiles, overlap and capacity; validate endpoint uniqueness/kinds, connection points, adjacency, opposite compatible sockets, direct/physical kind rules, required-route reachability/order and terminal semantics; canonicalize ordinal arrays and tiles; revalidate canonical output; serialize a complete candidate save separately; deserialize it and verify semantic identity. No step mutates active state.

## 16. Atomic migration transaction

1. Read untouched original payload O.
2. Determine the one winning authority.
3. Compute transaction ID `gd66-{sha256(lowercase hex of exact O bytes)}` and create recoverable backup before candidate construction.
4. Flush, reread, byte-hash, deserialize, and verify backup identity against O.
5. Construct candidate detached.
6. Canonicalize it.
7. Perform all §15 validations side-effect-free.
8. Serialize, reread, and validate complete candidate C separately; candidate identity is SHA-256 of exact canonical bytes plus transaction ID.
9. Persist C through a journaled durable atomic commit (same-directory staging, flush, atomic replace/rename appropriate to platform, directory durability where supported).
10. Reread and verify durable C.
11. Include the new schema marker only inside this verified complete commit.
12. Switch writable authority exactly once using the committed authority marker/equivalent.
13. Retain O backup and unchanged legacy fields for rollback until a separately approved cleanup policy.
14. On failure, keep or restore verified O as sole active authority.
15. Never expose partial graph state.

Backup naming is `{activeFile}.migration.{transactionId}.original`; a sidecar journal owned by Save/Migration Engineering records stage and hashes without becoming layout authority. Existing verified backup is reused on retry; mismatched backup fails closed. Diagnostics belong to migration telemetry/logging using stable codes, never identity. Current ordinary SaveService backup/corruption behavior is not accepted as proof of this protocol.

## 17. Writable-authority transition

1. Before migration: legacy-only reader/writer authority.
2. Candidate construction: legacy remains sole authority; C is detached.
3. Durable candidate payload may be staged/committed but is not readable authority.
4. One atomic marker/equivalent transitions authority in the complete durable commit.
5. Canonical graph becomes sole writable layout authority.
6. All gameplay readers use canonical graph order for the session.
7. All placement/structural writers target graph only.
8. Legacy fields are read-only compatibility/rollback evidence.
9. Cleanup is a later approval packet.

At load, canonical marker + incomplete/invalid graph, graph without marker, advanced schema without marker, or legacy write evidence after marker is contradictory and rejected/recovered. Reader choice is frozen for a session. Simultaneous writers, category split authority, silent reader fallback, schema advancement without transition, and graph authority after failed validation are prohibited.

## 18. Schema-version policy

Current `LatestSchemaVersion` is 6 and GD66 neither increments it nor approves “schema 7.” Migration requires a version greater than 6. The exact integer is deliberately selected by the first Phase 2 PR that finalizes/adds the serialized graph shape, after verifying then-current `LatestSchemaVersion`. The marker advances only inside the fully validated durable commit with completed authority transition.

## 19. Failure reason-code taxonomy

Stable dotted codes (append-only in Phase 2) include: `gd66.payload.unreadable`; `gd66.legacy.no_authority`; `duplicate_room_slot`, `duplicate_floor_node`, `duplicate_floor_slot`, `duplicate_category_revision`; `record_out_of_range`; `route_gap`; `lower_authority_agrees`; `lower_authority_conflict`; `gd66.id.malformed`; `gd66.id.collision`; `gd66.content.invalid_legacy_content`; `unmapped_legacy_room`; `missing_production_room`; `missing_production_corridor`; `invalid_production_content`; `gd66.geometry.bounds`, `.overlap`, `.reserved`, `.capacity`, `.socket`, `.adjacency`, `.corridor`; `gd66.graph.endpoint`, `.reachability`, `.terminal`, `.ordering`; `gd66.transaction.backup_create`, `.backup_verify`, `.candidate_verify`, `.commit`, `.durable_verify`, `.interrupted`, `.partial_state`, `.unrecoverable`; `gd66.authority.contradictory_state`; and success codes `gd66.success.no_layout`, `.migrated`, `.already_committed`, `.recovered_original`. Phase 2 may append narrower codes but must not renumber or reuse meanings.

## 20. Player messaging and localization ownership

Save/Migration Engineering owns reason emission; UX/Localization owns reason-to-key mapping and language tables. The required future recovery key namespace is `save.migration.spatial.<reason>.player_message`. GD66 adds no entries or English. Logs may include IDs/codes, while player messages resolve keys through injected tables. Localized text, keys, and UI labels never determine gameplay or identity.

## 21. Backup, rollback, retry, and idempotence

O remains active until verified commit/transition. A failed C is deleted/quarantined without changing O. On interruption, journal + hashes select only a verified O or fully verified committed C; ambiguity fails `unrecoverable` without inventing a new save. Retrying identical O reuses transaction ID/verified backup and constructs byte-identical C. A verified committed transaction is a no-op. Rollback after transition restores verified O and legacy authority atomically only under an explicit recovery operation; ordinary readers never oscillate. Retention duration/count is configuration-owned and must be approved in Phase 2, but at least the transaction's verified O cannot be pruned before migration finalization.

## 22. Phase 2 dependency breakdown

1. Add/round-trip inactive fixed-structure and authority/transaction serialized shape; select then-current schema number.
2. Add pure legacy presence, precedence, duplicate, mapping, ID and geometry candidate builder with fixtures for schemas 1–6.
3. Add detached canonical validation and stable reason codes.
4. Add verified backup/journal/durable commit/recovery and failure injection.
5. Add atomic authority transition, then switch all readers and writers together.
6. Add localized recovery keys/UI handling and lifecycle/edit-mode immediate-save evidence.
7. Activate only after complete EditMode/PlayMode/build/recovery evidence and separate approval.

## 23. Explicit non-goals

GD66 does not implement migration, transactions, graph fields, fixed instances, a schema number, runtime loading/activation, readers/writers, structural editing, corridors in gameplay, costs, Floor 2, UI/localization, tuning/content, fixtures/tests, refactors, scenes/assets/settings/packages, or fun validation. It does not claim automated correctness proves the dungeon fantasy is fun.

## 24. Acceptance checklist

- [x] Documentation only; schema 6 and inactive production catalog preserved.
- [x] One whole-model winner; no merge/fall-through/hidden fallback.
- [x] Schemas 1–6 fixture categories, retries, recovery, corruption, and idempotence specified.
- [x] Narrow Hall is unmapped and never a corridor/substitute room.
- [x] Exact collision-safe IDs and ordinal rules specified.
- [x] Production-derived bounds, footprints, capacity, coordinates, sockets, and direct edges proven.
- [x] Fixed persistence gap precedes candidate builder.
- [x] Verified O preservation, atomic commit, and exactly one writable authority required.
- [x] Future schema integer unassigned; advancement is complete-commit-only.
- [x] Player messaging is localization-owned; tuning remains configuration-owned.
- [x] Immediate edit-mode placement/movement save safety remains required by INV-12.

## 25. Approval statement

GD66 approves this precedence, fail-closed mapping, identity grammar, R1/R2 placement, direct-doorway graph, fixed-instance prerequisite, transaction protocol, and single-authority transition as the implementation contract for Phase 2. Unsupported states remain original-save authority with stable diagnostics. Approval changes no current behavior: schema 6, inactive spatial content, and ordered two-room authority remain in force until a separately reviewed Phase 2 implementation completes every durable-validation and authority-transition gate.
