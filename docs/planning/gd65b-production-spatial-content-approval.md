# GD65B production spatial content approval record

| Field | Decision |
|---|---|
| Status | **BLOCKED — approval register incomplete; GD65B production implementation is not authorized** |
| Baseline | `a547a30affd839b5780986c66e440f6f219773a3` (main through merged PR #167 / GD65A) |
| Packet | GD65B0 — Floor 1 spatial content authority and pipeline decision record |
| Scope | Approval authority for Floor 1 production spatial records and their production pipeline |
| Last reconciled | 2026-07-24 |

## 1. Scope and authority

This is the single authoritative approval register that must be completed before GD65B production spatial content is authored, exported, registered, loaded, validated, or assigned. It records existing qualitative authority without turning a candidate, spreadsheet value, prototype constant, test fixture, enum representation, or example into production authority. An `APPROVED` row applies only to the value stated in that row; a contract that can represent a value does not approve it.

The GD64 layout contracts and GD65A content schema, validator, and canonicalizer remain inactive. No production spatial catalog or runtime consumer exists. Ordered two-room models remain runtime and save authority, save schema remains 6, and this record authorizes no migration, graph activation, Bootstrap production dependency, or duplicate writable authority.

The only register statuses are `APPROVED`, `INFERRED_NOT_APPROVED`, `UNAPPROVED`, `CONFLICTING`, and `DEFERRED`. GD65B stays blocked while any required production row is not `APPROVED` with evidence.

## 2. Dependencies

- Merged PR #166 / GD64 supplies inactive rectangular-bound, physical-tile-capacity, direct-doorway/corridor, bounded-validation, and canonical layout contracts.
- Merged PR #167 / GD65A supplies the inactive serializable content schema and pure, bounded, deterministic validation and detached ordinal canonicalization.
- [GD63](gd63-spatial-and-progression-design-decisions.md) and [Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) own approved qualitative spatial direction.
- Specs 19, 27, 28, 36, and 37 constrain content pipeline, localization, saves, workloads, and evidence; none supplies missing production values.
- GD66 depends on approved GD65B records for final migration mapping. Phase 2 alone may implement migration and switch runtime/save authority.

## 3. Already approved structural constraints

These constraints do **not** approve an unstated exact ID, coordinate, dimension, capacity, socket, connection, localization record, version, path, owner, or workload limit:

1. One occupied physical tile equals one floor-space unit.
2. Floor bounds are rectangular.
3. MVP rooms are rectangular or square.
4. Entrance Hall is required, non-buildable, and non-removable.
5. Basic Room is square, medium, and balanced.
6. Rectangle Room is rectangular, approximately in the Basic Room space class, monster-favored, and has lower trap capacity.
7. Rectangle Room rotation affects fit rather than identity or capacities.
8. Large Chamber is larger and has higher total capacity and commitment.
9. Narrow Hall and Straight Stone Corridor are one straight corridor category.
10. The MVP straight corridor allows traps and no monsters.
11. Corridor loot is intended only for an optional dead end.
12. Completion Terminal is required and contains no normal room content.
13. Adjacent compatible structures may use a direct doorway with no corridor footprint.
14. Separated structures require a physical corridor.
15. The optional branch maximum is one per floor.
16. Stable IDs and ordinal canonical ordering are required.
17. Player-facing text is localization-owned.
18. Numeric tuning and workload limits are configuration-owned.
19. Save schema remains 6.
20. The spatial catalog remains inactive.
21. Ordered two-room models remain runtime and save authority.

*Medium*, *large*, *narrow*, *balanced*, *approximately*, *higher*, and *lower* remain qualitative and must not be converted into numbers without approval.

## 4. Complete approval register

“Pending signoff in §14” means no approval evidence exists. Contract/design references identify constraints only.

| # | Field | Status | Current authority | Approved value | Missing decision | Downstream systems affected | Recommended owner | Approval evidence location |
|---:|---|---|---|---|---|---|---|---|
| 1 | Catalog schema identity | `UNAPPROVED` | GD65A contract shape only; no production value | — | Exact stable production schema ID | Export, validation, manifest, schema registry, loading | Data/Content Pipeline | Pending signoff in §14 |
| 2 | Catalog schema version | `UNAPPROVED` | GD65A contract shape only; no production value | — | Exact initial version | Export, registry, compatibility | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 3 | Production content version | `UNAPPROVED` | Spec 19 and GD65A require an explicit version | — | Exact version and versioning policy | Export, manifest, saves, loading | Data/Release | Pending signoff in §14 |
| 4 | Stable Floor 1 definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production stable floor 1 definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 5 | Basic Room definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production basic room definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 6 | Rectangle Room definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production rectangle room definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 7 | Large Chamber definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production large chamber definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 8 | Straight corridor definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production straight corridor definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 9 | Entrance Hall structure definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production entrance hall structure definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 10 | Completion Terminal structure definition ID | `UNAPPROVED` | Stable/qualitative identity only | — | Exact production completion terminal structure definition id | Catalog, editor, saves, migration, localization mapping | Design/Data | Pending signoff in §14 |
| 11 | Socket type IDs | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete production socket ID set | Compatibility, connections, editor, migration | Design/Data | Pending signoff in §14 |
| 12 | Connection point IDs | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete production point ID set and scope rule | Connections, canonical export, editor, migration | Design/Data | Pending signoff in §14 |
| 13 | Serialized Floor 1 index | `UNAPPROVED` | Nonnegative unique index contract; presentation numbering is not authority | — | Exact serialized index | Export, lookup, saves, migration | Design/Data + Save | Pending signoff in §14 |
| 14 | Floor bounds (`Minimum`, `Width`, and `Height`) | `UNAPPROVED` | `RectangularFloorBounds` serializes `Minimum`, `Width`, and `Height`; rectangular bounds are approved but exact Floor 1 values are absent | — | Exact `Minimum`, positive `Width`, and positive `Height`; upper bounds are derived and exclusive: `exclusiveMaximumX = Minimum.X + Width`, `exclusiveMaximumY = Minimum.Y + Height` | Placement, capacity, validation, editor, migration | Design/Data | Pending signoff in §14 |
| 15 | Final floor-space capacity | `UNAPPROVED` | Occupied-tile accounting approved; exact capacity absent | — | Exact configured capacity and bounds/unavailable-tile relationship | Placement, progression, editor, performance | Design/Data | Pending signoff in §14 |
| 16 | Floor 1 `AllowedRoomDefinitionIds` | `UNAPPROVED` | GD65A serializes and independently validates this room-reference collection | — | Exact ordinal-canonical production collection of allowed room definition IDs | Floor validation, authoring, editor catalog, canonical export | Design/Data | Pending signoff in §14 |
| 17 | Floor 1 `AllowedCorridorDefinitionIds` | `UNAPPROVED` | GD65A serializes and independently validates this corridor-reference collection | — | Exact ordinal-canonical production collection of allowed corridor definition IDs | Floor validation, authoring, editor catalog, canonical export | Design/Data | Pending signoff in §14 |
| 18 | Floor 1 `EntranceStructureDefinitionId` | `UNAPPROVED` | GD65A serializes a distinct fixed-structure foreign key and validates the Entrance kind | — | Exact production assignment to an approved Entrance structure definition | Floor validation, route endpoint, authoring, canonical export | Design/Data | Pending signoff in §14 |
| 19 | Floor 1 `CompletionStructureDefinitionId` | `UNAPPROVED` | GD65A serializes a distinct fixed-structure foreign key and validates the CompletionTerminal kind | — | Exact production assignment to an approved Completion Terminal definition | Floor validation, route endpoint, authoring, canonical export | Design/Data | Pending signoff in §14 |
| 20 | Optional branch allowance | `UNAPPROVED` | GD63/Spec 38 approve a global maximum of one; they do not authorize the exact serialized Floor 1 value or the later research-gated effective allowance | — | Exact Floor 1 `OptionalBranchAllowance` value within the maximum; Phase 5 separately owns the effective research-gated allowance | Graph validation, route selection, editor | Design/Data | Pending signoff in §14 |
| 21 | Basic Room width and height | `UNAPPROVED` | Square/medium only | — | Exact dimensions | Footprint, placement, capacity, editor | Design/Data | Pending signoff in §14 |
| 22 | Rectangle Room width and height | `UNAPPROVED` | Rectangular/approximately Basic space class only | — | Exact dimensions | Footprint, placement, capacity, editor | Design/Data | Pending signoff in §14 |
| 23 | Large Chamber width and height | `UNAPPROVED` | Larger/higher commitment only | — | Exact dimensions | Footprint, placement, capacity, editor | Design/Data | Pending signoff in §14 |
| 24 | Reserved tile offsets for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 25 | Allowed orientations for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 26 | Maximum connection count for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 27 | Monster capacity for every room | `UNAPPROVED` | Qualitative/relative profile only | — | Exact capacity per room | Content placement, balance, UI, simulation | Design/Data | Pending signoff in §14 |
| 28 | Trap capacity for every room | `UNAPPROVED` | Qualitative/relative profile only | — | Exact capacity per room | Content placement, balance, UI, simulation | Design/Data | Pending signoff in §14 |
| 29 | Loot capacity for every room | `UNAPPROVED` | Qualitative/relative profile only | — | Exact capacity per room | Content placement, balance, UI, simulation | Design/Data | Pending signoff in §14 |
| 30 | Corridor category | `APPROVED` | GD63 §3 and Spec 38 | `CorridorSpatialCategory.Straight` (serialized enum value `1`) | No missing serialized category decision; the production corridor ID remains independently unapproved | Catalog, routing, editor, validation | Design/Data | `docs/planning/gd63-spatial-and-progression-design-decisions.md` §3; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs` |
| 31 | Corridor width | `UNAPPROVED` | “Narrow” is qualitative only | — | Exact width | Footprint, placement, capacity, editor | Design/Data | Pending signoff in §14 |
| 32 | Corridor minimum length | `UNAPPROVED` | Variable length approved; bound absent | — | Exact minimum | Placement, workload, editor | Design/Data | Pending signoff in §14 |
| 33 | Corridor maximum length | `UNAPPROVED` | Variable length approved; bound absent | — | Exact maximum | Placement, workload, editor | Design/Data + Performance | Pending signoff in §14 |
| 34 | Corridor monster capacity | `APPROVED` | GD63 §3 and GD65A validator | `MonsterCapacity = 0` | No missing serialized monster-capacity decision for the MVP straight corridor | Content placement, validation, simulation | Design/Data | `docs/planning/gd63-spatial-and-progression-design-decisions.md` §3; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs` |
| 35 | Corridor trap capacity | `UNAPPROVED` | Traps allowed; exact capacity absent | — | Exact capacity | Content placement, balance, UI, simulation | Design/Data | Pending signoff in §14 |
| 36 | Corridor loot capacity | `UNAPPROVED` | Loot intended only for optional dead ends | — | Exact capacity and dead-end eligibility representation | Branch content, placement, validation, simulation | Design/Data | Pending signoff in §14 |
| 37 | Corridor allowed orientations | `UNAPPROVED` | Straight geometry only | — | Exact orientation set | Placement, export, editor, migration | Design/Data | Pending signoff in §14 |
| 38 | Straight corridor `CompatibleSocketTypeIds` | `UNAPPROVED` | GD65A serializes and independently validates this socket-reference collection | — | Exact ordinal-canonical production collection of compatible socket type IDs for the straight corridor | Corridor validation, placement, editor, canonical export | Design/Data | Pending signoff in §14 |
| 39 | Socket compatibility matrix | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete compatibility matrix and directionality | Doorways, corridors, validation, editor | Design/Engineering/Data | Pending signoff in §14 |
| 40 | Every room connection point position, facing, and socket reference | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete tuple for every point of every room | Fit, connections, editor, migration | Design/Data | Pending signoff in §14 |
| 41 | Entrance footprint | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact footprint | Occupancy, placement, migration, editor | Design/Data | Pending signoff in §14 |
| 42 | Entrance reserved tiles | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact reserved tiles | Passability, connections, editor | Design/Data | Pending signoff in §14 |
| 43 | Entrance orientations | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact orientations | Placement, migration, export | Design/Data | Pending signoff in §14 |
| 44 | Entrance connection points | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact connection points | Required route, editor, migration | Design/Data | Pending signoff in §14 |
| 45 | Entrance maximum connections | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact maximum connections | Graph validity, topology, editor | Design/Data | Pending signoff in §14 |
| 46 | Completion footprint | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact footprint | Occupancy, placement, migration, editor | Design/Data | Pending signoff in §14 |
| 47 | Completion reserved tiles | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact reserved tiles | Passability, connections, editor | Design/Data | Pending signoff in §14 |
| 48 | Completion orientations | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact orientations | Placement, migration, export | Design/Data | Pending signoff in §14 |
| 49 | Completion connection points | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact connection points | Required route, editor, migration | Design/Data | Pending signoff in §14 |
| 50 | Completion maximum connections | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact maximum connections | Graph validity, topology, editor | Design/Data | Pending signoff in §14 |
| 51 | Localization key naming convention | `UNAPPROVED` | Spec 27 requires stable namespaces; examples are not authority | — | Exact spatial namespace and naming rule | Export, localization QA, UI | Localization/Data | Pending signoff in §14 |
| 52 | Production localization key for each room | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 53 | Production localization key for the corridor | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 54 | Production localization key for Entrance Hall | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 55 | Production localization key for Completion Terminal | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 56 | Actual production English localization entry for every key | `UNAPPROVED` | Spec 27 makes text localization-owned | — | Reviewed English entry for every key | English UI, fallback, localization QA | Localization/Design | Pending signoff in §14 |
| 57 | Export file or Unity asset location | `UNAPPROVED` | GD65A defines an envelope, not a destination | — | Exact non-Bootstrap production path and format | Authoring, build, loading, tests | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 58 | Content manifest ownership | `UNAPPROVED` | Bootstrap manifest does not register spatial content | — | Production manifest and accountable owner/update step | Build integrity, loading, versions | Data/Content Pipeline | Pending signoff in §14 |
| 59 | Schema registration ownership | `UNAPPROVED` | Bootstrap schema map does not register spatial content | — | Production registry and accountable owner/update step | Validation, compatibility, loading | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 60 | Content service loading ownership | `UNAPPROVED` | ContentServices has no spatial consumer | — | Loader, lifecycle, failure policy, owner | Runtime availability, diagnostics | Engineering/Data | Pending signoff in §14 |
| 61 | GameRoot or non Bootstrap production assignment ownership | `UNAPPROVED` | GameRoot has no spatial assignment; Bootstrap is not production authority | — | Composition root and accountable owner | Dependency injection, scenes, tests | Engineering | Pending signoff in §14 |
| 62 | Production catalog validation invocation path | `UNAPPROVED` | Pure validator exists; no production caller | — | Import/build/load gate, limits source, failure handling | Authoring, build, loading, diagnostics | Engineering/QA/Data | Pending signoff in §14 |
| 63 | Production canonical serialization path | `UNAPPROVED` | Canonicalizer exists; no exporter owns it | — | Export invocation, encoding/file policy, owner | Reproducible exports, diffs, hashing | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 64 | Maximum top-level records | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 65 | Maximum nested records | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 66 | Maximum materialized tiles | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 67 | Maximum issues | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | QA/Performance/Data | Pending signoff in §14 |
| 68 | Maximum string characters | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 69 | Localization lookup workload accounting (contract mapping; no independent limit) | `APPROVED` | GD65A `SpatialContentWorkload.TryPreflight` accounts supplied localization lookup data through the existing five-field workload contract | Lookup entry count consumes `MaximumNestedRecords`; lookup key characters consume `MaximumStringCharacters` | No sixth limit, new API, or independent localization workload value; exact numeric bounds remain in the nested-record and string-character rows | Validation preflight, localization lookup, memory bounds | Engineering/QA | `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs` |
| 70 | Production pipeline test ownership | `UNAPPROVED` | GD65A tests cover inactive contracts only | — | Named suite, stage, fixtures, evidence owner, failure gate | Export, registry, loading, release confidence | QA/Data/Engineering | Pending signoff in §14 |

## 5. Prototype ID warning and semantic conflict

`placement.option.room.basic` and `placement.option.room.narrow_hall` are MVP **placement option IDs**. They are not approved production spatial definition IDs, naming templates, migration targets, localization keys, or catalog authority. GD65B0 does not rename or migrate them.

A semantic conflict remains: the prototype classifies Narrow Hall as a room option, while GD63/Spec 38 classifies Narrow Hall and Straight Stone Corridor as one corridor category. This context is `CONFLICTING`; it is not resolved by silently reusing, renaming, or mapping either ID. Design/Data must approve production identity, and GD66/Phase 2 must separately approve and implement any compatibility mapping.

## 6. Pipeline ownership decisions

Rows 57–63 and 70 are `UNAPPROVED`. They require accountable owners and exact non-Bootstrap paths. The pipeline must retain one writable content authority, explicit schema/content versions, ordinal canonical serialization, pure validation, and caller-supplied bounded workloads. It must not depend on dictionary iteration, locale, filesystem enumeration, runtime hashes, source-row ordering, arbitrary duplicate resolution, or source mutation.

Existing Bootstrap manifests, schema maps, content versions, and strings are prototype/validation infrastructure, not candidate/default production authority.

## 7. Localization ownership decisions

Rows 51–56 are `UNAPPROVED`. Localization/Data must approve the namespace, keys, and reviewed English entries; lookup workload uses the existing nested-record and string-character limits recorded by row 69. Definition IDs and localization keys remain separate. Production English must not be embedded as display text or fallback literals; languages must remain addable without code changes.

## 8. Workload limit decisions

Rows 64–68 are `UNAPPROVED`; row 69 records how localization data consumes two of those existing limits and creates no sixth limit. GD65A deliberately receives limits from its caller. Performance/QA/Data must approve exact conservative configuration-owned limits with written rationale based on the approved Floor 1 content envelope and identify later validation stages. GD65B must provide production export and pipeline-execution evidence. Low-end mobile profiling and Android device evidence remain Phase 9 work unless a measured problem requires earlier investigation. Fixtures/examples are not authority. Validation must fail closed, remain pure/deterministic, and retain bounded diagnostics.

## 9. Downstream effects

- **GD65B:** blocked by every non-approved required row.
- **GD66:** follows GD65B and later owns legacy stable-ID derivation, migration coordinates/orientations, fixtures, fallbacks, recovery, and compatibility mapping. GD66 review/signoff is not a GD65B0 or GD65B prerequisite.
- **Phase 2:** exclusively owns migration, runtime-reader switch, save authority transition, and removal of duplicate writable authority.
- **Phase 3:** depends on geometry, sockets, connections, localization, and production validation.
- **Phase 5:** depends on the maximum-one branch constraint plus later selection data.
- **Build/QA:** depend on manifest, registry, loading, canonicalization, configured bounds, and pipeline evidence.

## 10. Architecture constraints preserved

Later work must preserve deterministic validation; stable IDs; ordinal canonical ordering; configuration-owned numbers; localization-owned player text; no duplicated writable authority; pure validation; caller-supplied bounds; explicit schema/content versions; save schema 6 until separately migrated; no runtime graph activation here; no Bootstrap production dependency; no arbitrary duplicate resolution; no dictionary, locale, filesystem, runtime-hash, or source-row ordering dependency; no source mutation; no hardcoded production English; and no unapproved tuning.

## 11. Explicit implementation readiness gate

**GD65B is NOT READY.** All 70 rows must be reviewed. Every production-required row must be `APPROVED` with precise evidence; informational contract-mapping row 69 is satisfied by its cited GD65A implementation and creates no new production value. The prototype conflict must be explicitly resolved, and assigned Design, Data, Engineering, Localization, QA, Performance, and current-scope Save owners must sign off. `INFERRED_NOT_APPROVED`, `UNAPPROVED`, `CONFLICTING`, or `DEFERRED` never passes the gate.

After current-scope signoff, GD65B must author and validate production records and execute the production pipeline without activating graph/save authority. GD66 signoff is not required first; GD66 begins after GD65B records exist. Activation or migration requires a separate packet.

## 12. Non-goals

- Authoring production records or choosing IDs, versions, dimensions, capacities, sockets, connections, text, limits, or paths.
- Promoting spreadsheet candidates, prototypes, fixtures, examples, enums, Bootstrap values, or code constants.
- Renaming/migrating prototype IDs or defining legacy mapping.
- Modifying code, JSON, tests, assets, UI, settings, saves, schemas, manifests, localization, or versions.
- Adding a consumer, graph, migration, gameplay/UI behavior, or speculative framework.
- Proving balance, performance, comprehension, or fun.

## 13. Change control rules

1. Approve a row only with its owner’s explicit decision and durable evidence.
2. Record approved values verbatim; never promote examples, candidates, fixtures, prototypes, or inference.
3. Update affected rows/downstream effects atomically and retain review history.
4. Keep numbers in content/config and player text in localization.
5. Stable-ID renames/removals require explicit deprecation/migration review.
6. Keep conflicts `CONFLICTING` until recorded resolution; never resolve by ordering, fallback, or first/last duplicate selection.
7. Schema, save, runtime-authority, or Bootstrap-production changes require separate review/evidence.

## 14. Approval signoff

No signoff is supplied. Empty signoff intentionally keeps the gate closed.

| Authority | Required decisions | Approver/evidence | Status |
|---|---|---|---|
| Design | Identities, geometry/capacity, structures, sockets/connections, prototype conflict | — | `UNAPPROVED` |
| Data / Content Pipeline | Schema/content versions, IDs, export, manifest/registry, serialization | — | `UNAPPROVED` |
| Engineering | Production loading/assignment/validation and non-Bootstrap composition | — | `UNAPPROVED` |
| Localization | Convention, keys, English entries, lookup ownership | — | `UNAPPROVED` |
| Performance / QA | Conservative production validation limits, Floor 1 envelope rationale, later validation stages, and pipeline test ownership; device profiling remains Phase 9 | — | `UNAPPROVED` |
| Save | Serialized Floor 1 index semantics; confirm save schema remains 6 and GD65B adds no saved spatial state or migration | — | `UNAPPROVED` |
