# GD65B production spatial content approval record

| Field | Decision |
|---|---|
| Status | **BLOCKED — approval register incomplete; GD65B production implementation is not authorized** |
| Starting baseline for this approval update | `06aac95b99d043726f6dea6a8d40c5eb6b5399f7` (main through merged PR #171 / GD65B0B) |
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
- Merged PR #168 / GD65B0 established this 72-row authority and pipeline gate.
- Merged PR #169 approved the first identity group.
- Merged PR #170 / GD65B0A preserved generic optional-branch allowance extensibility above the MVP limit.
- Merged PR #171 / GD65B0B approved catalog metadata, Floor 1 references, the Floor 1 branch allowance, and the limited current-scope Save boundary.
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
15. MVP production content and active MVP behavior permit at most one optional branch per floor; the generic nonnegative per-floor authored field has no permanent schema maximum of 1.
16. Stable IDs and ordinal canonical ordering are required.
17. Player-facing text is localization-owned.
18. Numeric tuning and workload limits are configuration-owned.
19. Save schema remains 6.
20. The spatial catalog remains inactive.
21. Ordered two-room models remain runtime and save authority.

*Narrow* and any qualitative statement not covered by an approved row must not be converted into a number. Rows 23–25 and 29–31 now supply the only approved numeric interpretation for the scoped initial room profiles; descriptive words such as *medium*, *large*, *balanced*, *approximately*, *higher*, and *lower* do not independently authorize other values.

## 4. Complete approval register

“Pending signoff in §14” means no approval evidence exists. Contract/design references identify constraints only.

For the identities approved in rows 4–10, the production convention is exactly `spatial.<definition-kind>.<stable-name>`: IDs use lowercase ASCII; periods separate components; multiword stable names use underscores; and identity and ordering use ordinal strings. IDs are not derived from display text and must not contain dimensions, coordinates, capacities, tuning values, schema versions, or content versions. Definition IDs and localization keys remain separate authorities. Existing prototype `placement.option.*` IDs are not production spatial definition IDs, and `test.gd65a.*` fixture IDs are not production authority.

| # | Field | Status | Current authority | Approved value | Missing decision | Downstream systems affected | Recommended owner | Approval evidence location |
|---:|---|---|---|---|---|---|---|---|
| 1 | Catalog schema identity | `APPROVED` | Owner-approved stable production schema identity; lowercase snake_case, independent of Floor 1, versions, and the C# class name | `SchemaId = "dungeon_spatial_content"` | Renaming requires explicit compatibility and migration review | Export, validation, manifest, schema registry, loading | Data/Content Pipeline | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 2 | Catalog schema version | `APPROVED` | Increment only when serialized schema shape or field meaning changes incompatibly; ordinary compatible content changes do not increment it | `SchemaVersion = 1` | Schema changes require compatibility review and migration or rejection handling; a higher version does not replace migration planning | Export, registry, compatibility | Data/Content Pipeline + Engineering | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 3 | Production content version | `APPROVED` | Locale-independent semantic versioning: patch for compatible corrections/tuning; minor for additive compatible content; major for removal, rename, repurpose, or another breaking content change | `ContentVersion = "0.1.0"` | A major version does not replace save/content migration review | Export, manifest, saves, loading | Data/Release | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 4 | Stable Floor 1 definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.floor.01` | None for identity; geometry, index, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 5 | Basic Room definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.room.basic` | None for identity; geometry, capacities, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 6 | Rectangle Room definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.room.rectangle` | None for identity; geometry, capacities, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 7 | Large Chamber definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.room.large_chamber` | None for identity; geometry, capacities, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 8 | MVP straight corridor definition ID | `APPROVED` | Owner resolves the prior production identity conflict in §5: exactly one initial MVP straight-corridor definition; the prototype Narrow Hall remains non-authoritative | `spatial.corridor.straight_stone` | None for production identity; geometry, capacities, sockets, localization, compatibility, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §5 resolution; §14 owner approval record; this documentation PR |
| 9 | Entrance Hall structure definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.fixed.entrance_hall` | None for identity; geometry, connections, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 10 | Completion Terminal structure definition ID | `APPROVED` | Owner-approved production identity convention; identity only | `spatial.fixed.completion_terminal` | None for identity; geometry, connections, localization, migration, loading, and save representation remain separate | Catalog, editor, saves, migration, localization mapping | Design/Data | §14 owner approval record; this documentation PR |
| 11 | Entrance Hall fixed-structure `Kind` | `APPROVED` | GD65A independently serializes `FixedSpatialStructureDefinition.Kind`; GD63 and Spec 38 approve the fixed structure identity | `FixedSpatialStructureKind.Entrance` (serialized enum value `1`) | No missing serialized `Kind` decision; approving the structure definition ID or Floor 1 foreign key does not assign this field | Fixed-definition validation, Floor 1 endpoint type safety, canonical export | Design/Data | `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs`; `docs/planning/gd63-spatial-and-progression-design-decisions.md`; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md` |
| 12 | Completion Terminal fixed-structure `Kind` | `APPROVED` | GD65A independently serializes `FixedSpatialStructureDefinition.Kind`; GD63 and Spec 38 approve the fixed structure identity | `FixedSpatialStructureKind.CompletionTerminal` (serialized enum value `2`) | No missing serialized `Kind` decision; approving the structure definition ID or Floor 1 foreign key does not assign this field | Fixed-definition validation, Floor 1 endpoint type safety, canonical export | Design/Data | `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs`; `docs/planning/gd63-spatial-and-progression-design-decisions.md`; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md` |
| 13 | Initial production socket type IDs | `APPROVED` | Owner approves exactly one initial socket identity; later types may be added without renaming it | `["spatial.socket.standard_passage"]` | Identity only; row 41 compatibility matrix, corridor compatible-socket collections, point coordinates/facing/counts, doorway geometry, reserved-tile interaction, and runtime behavior remain unapproved | Compatibility, connections, editor, migration | Design/Data | §14 owner approval record; this documentation PR |
| 14 | Connection-point ID scope (informational contract mapping; no concrete ID) | `APPROVED` | GD65A contract: IDs are unique within their owning room or fixed-structure definition, not one catalog-wide namespace; rooms map to row 42, Entrance Hall to row 46, Completion Terminal to row 51; corridors reference compatible socket types and serialize no equivalent point collection | Owner-scoped identity: rows 42, 46, and 51 own exact IDs; no concrete production connection-point ID is approved in this packet | No independent global ID set; exact IDs and point geometry remain in rows 42, 46, and 51 | Connections, canonical export, editor, migration | Design/Data | `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs`; §14 owner approval record; this documentation PR |
| 15 | Serialized Floor 1 index | `APPROVED` | Player-facing Floor 1 maps to zero-based serialized/runtime index 0; presentation numbering remains separate; existing Floor 0 ordered-route behavior is supporting evidence, not spatial authority | `FloorIndex = 0` | No current runtime or save change; future persistence remains GD66/Phase 2 scope | Export, lookup, saves, migration | Design/Data + Save | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 16 | Floor bounds (`Minimum`, `Width`, and `Height`) | `UNAPPROVED` | `RectangularFloorBounds` serializes `Minimum`, `Width`, and `Height`; rectangular bounds are approved but exact Floor 1 values are absent | — | Exact `Minimum`, positive `Width`, and positive `Height`; upper bounds are derived and exclusive: `exclusiveMaximumX = Minimum.X + Width`, `exclusiveMaximumY = Minimum.Y + Height` | Placement, capacity, validation, editor, migration | Design/Data | Pending signoff in §14 |
| 17 | Final floor-space capacity | `UNAPPROVED` | Occupied-tile accounting approved; exact capacity absent | — | Exact configured capacity and bounds/unavailable-tile relationship | Placement, progression, editor, performance | Design/Data | Pending signoff in §14 |
| 18 | Floor 1 `AllowedRoomDefinitionIds` | `APPROVED` | Owner-approved Floor 1 foreign-key collection only; ordinal-canonical order | `["spatial.room.basic", "spatial.room.large_chamber", "spatial.room.rectangle"]` | Geometry, capacities, costs, localization, connections, loading, saves, and runtime assignment remain unapproved | Floor validation, authoring, editor catalog, canonical export | Design/Data | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 19 | Floor 1 `AllowedCorridorDefinitionIds` | `APPROVED` | Owner-approved Floor 1 foreign-key collection only; ordinal-canonical order | `["spatial.corridor.straight_stone"]` | Dimensions, lengths, capacities, sockets, orientations, costs, localization, and runtime behavior remain unapproved | Floor validation, authoring, editor catalog, canonical export | Design/Data | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 20 | Floor 1 `EntranceStructureDefinitionId` | `APPROVED` | Owner-approved Floor 1 foreign-key assignment only | `EntranceStructureDefinitionId = "spatial.fixed.entrance_hall"` | Geometry, coordinates, orientation, connections, localization, migration placement, and saved representation remain unapproved | Floor validation, route endpoint, authoring, canonical export | Design/Data | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 21 | Floor 1 `CompletionStructureDefinitionId` | `APPROVED` | Owner-approved Floor 1 foreign-key assignment only | `CompletionStructureDefinitionId = "spatial.fixed.completion_terminal"` | Geometry, coordinates, orientation, connections, localization, migration placement, and saved representation remain unapproved | Floor validation, route endpoint, authoring, canonical export | Design/Data | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 22 | Optional branch allowance | `APPROVED` | Authored Floor 1 MVP maximum; generic nonnegative per-floor field remains extensible above 1 post-MVP; architecture research may initially make effective allowance 0, and Phase 5 owns effective research gating and route selection | `OptionalBranchAllowance = 1` | This does not activate branching; active MVP behavior/content remain at most one branch, while later floors require separate approval for values above 1 | Graph validation, route selection, editor | Design/Data | §14 owner metadata and Floor 1 approval record; this documentation PR |
| 23 | Basic Room width and height | `APPROVED` | Owner-approved exact gross footprint for `spatial.room.basic`; square/medium role retained | `spatial.room.basic`:<br>`GrossFootprint.Width = 4`<br>`GrossFootprint.Height = 4` | Reserved tiles, usable area, orientations, connections, placement, and runtime activation remain excluded | Content authoring, footprint validation, fit preview, editor | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 24 | Rectangle Room width and height | `APPROVED` | Owner-approved exact gross footprint for `spatial.room.rectangle`; width 3 and height 5 are definition authority, while rotation remains separate | `spatial.room.rectangle`:<br>`GrossFootprint.Width = 3`<br>`GrossFootprint.Height = 5` | Reserved tiles, usable area, allowed rotations, connections, placement, and runtime activation remain excluded; do not canonicalize the definition to 5 × 3 | Content authoring, footprint validation, fit preview, editor | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 25 | Large Chamber width and height | `APPROVED` | Owner-approved exact gross footprint for `spatial.room.large_chamber`; large/high-commitment role retained | `spatial.room.large_chamber`:<br>`GrossFootprint.Width = 5`<br>`GrossFootprint.Height = 6` | Reserved tiles, usable area, orientations, connections, placement, and runtime activation remain excluded | Content authoring, footprint validation, fit preview, editor | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 26 | Reserved tile offsets for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 27 | Allowed orientations for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 28 | Maximum connection count for every room | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete exact value per room | Placement, connections, canonical export, editor | Design/Data | Pending signoff in §14 |
| 29 | Monster capacity for every room | `APPROVED` | Owner-approved independently authored category maxima; design authority only | `MonsterCapacity`:<br>`spatial.room.basic = 2`<br>`spatial.room.rectangle = 3`<br>`spatial.room.large_chamber = 4` | Individual footprints/coordinates, usable and reserved tiles, non-room capacities, placement validation, runtime consumption, and balance evidence remain excluded | Future content authoring, category-limit validation, fit preview, localized UI | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 30 | Trap capacity for every room | `APPROVED` | Owner-approved independently authored category maxima; design authority only | `TrapCapacity`:<br>`spatial.room.basic = 2`<br>`spatial.room.rectangle = 1`<br>`spatial.room.large_chamber = 4` | Individual footprints/coordinates, usable and reserved tiles, non-room capacities, placement validation, runtime consumption, and balance evidence remain excluded | Future content authoring, category-limit validation, fit preview, localized UI | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 31 | Loot capacity for every room | `APPROVED` | Owner-approved independently authored category maxima; design authority only | `LootCapacity`:<br>`spatial.room.basic = 2`<br>`spatial.room.rectangle = 2`<br>`spatial.room.large_chamber = 4` | Individual footprints/coordinates, usable and reserved tiles, non-room capacities, placement validation, runtime consumption, and balance evidence remain excluded | Future content authoring, category-limit validation, fit preview, localized UI | Design/Data | §14 GD65B0C1 owner room-profile approval; this documentation PR |
| 32 | Corridor category | `APPROVED` | GD63 §3 and Spec 38 | `CorridorSpatialCategory.Straight` (serialized enum value `1`) | No missing serialized category decision; production identity is separately approved in row 8. Geometry, dimensions, capacities other than row 36, orientations, sockets, localization, loading, migration, and save representation remain separate decisions. | Catalog, routing, editor, validation | Design/Data | `docs/planning/gd63-spatial-and-progression-design-decisions.md` §3; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs` |
| 33 | Corridor width | `UNAPPROVED` | “Narrow” is qualitative only | — | Exact width | Footprint, placement, capacity, editor | Design/Data | Pending signoff in §14 |
| 34 | Corridor minimum length | `UNAPPROVED` | Variable length approved; bound absent | — | Exact minimum | Placement, workload, editor | Design/Data | Pending signoff in §14 |
| 35 | Corridor maximum length | `UNAPPROVED` | Variable length approved; bound absent | — | Exact maximum | Placement, workload, editor | Design/Data + Performance | Pending signoff in §14 |
| 36 | Corridor monster capacity | `APPROVED` | GD63 §3 and GD65A validator | `MonsterCapacity = 0` | No missing serialized monster-capacity decision for the MVP straight corridor | Content placement, validation, simulation | Design/Data | `docs/planning/gd63-spatial-and-progression-design-decisions.md` §3; `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs` |
| 37 | Corridor trap capacity | `UNAPPROVED` | Traps allowed; exact capacity absent | — | Exact capacity | Content placement, balance, UI, simulation | Design/Data | Pending signoff in §14 |
| 38 | Corridor loot capacity | `UNAPPROVED` | Loot intended only for optional dead ends | — | Exact capacity and dead-end eligibility representation | Branch content, placement, validation, simulation | Design/Data | Pending signoff in §14 |
| 39 | Corridor allowed orientations | `UNAPPROVED` | Straight geometry only | — | Exact orientation set | Placement, export, editor, migration | Design/Data | Pending signoff in §14 |
| 40 | Straight corridor `CompatibleSocketTypeIds` | `UNAPPROVED` | GD65A serializes and independently validates this socket-reference collection | — | Exact ordinal-canonical production collection of compatible socket type IDs for the straight corridor | Corridor validation, placement, editor, canonical export | Design/Data | Pending signoff in §14 |
| 41 | Socket compatibility matrix | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete compatibility matrix and directionality | Doorways, corridors, validation, editor | Design/Engineering/Data | Pending signoff in §14 |
| 42 | Every room connection point position, facing, and socket reference | `UNAPPROVED` | GD65A contract shape only; no production value | — | Complete tuple for every point of every room | Fit, connections, editor, migration | Design/Data | Pending signoff in §14 |
| 43 | Entrance footprint | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact footprint | Occupancy, placement, migration, editor | Design/Data | Pending signoff in §14 |
| 44 | Entrance reserved tiles | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact reserved tiles | Passability, connections, editor | Design/Data | Pending signoff in §14 |
| 45 | Entrance orientations | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact orientations | Placement, migration, export | Design/Data | Pending signoff in §14 |
| 46 | Entrance connection points | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact connection points | Required route, editor, migration | Design/Data | Pending signoff in §14 |
| 47 | Entrance maximum connections | `UNAPPROVED` | Required fixed entrance identity; field shape only | — | Complete exact maximum connections | Graph validity, topology, editor | Design/Data | Pending signoff in §14 |
| 48 | Completion footprint | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact footprint | Occupancy, placement, migration, editor | Design/Data | Pending signoff in §14 |
| 49 | Completion reserved tiles | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact reserved tiles | Passability, connections, editor | Design/Data | Pending signoff in §14 |
| 50 | Completion orientations | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact orientations | Placement, migration, export | Design/Data | Pending signoff in §14 |
| 51 | Completion connection points | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact connection points | Required route, editor, migration | Design/Data | Pending signoff in §14 |
| 52 | Completion maximum connections | `UNAPPROVED` | Required fixed completion identity; field shape only | — | Complete exact maximum connections | Graph validity, topology, editor | Design/Data | Pending signoff in §14 |
| 53 | Localization key naming convention | `UNAPPROVED` | Spec 27 requires stable namespaces; examples are not authority | — | Exact spatial namespace and naming rule | Export, localization QA, UI | Localization/Data | Pending signoff in §14 |
| 54 | Production localization key for each room | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 55 | Production localization key for the corridor | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 56 | Production localization key for Entrance Hall | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 57 | Production localization key for Completion Terminal | `UNAPPROVED` | GD65A requires a localization reference | — | Exact production key or keys | Catalog, string lookup, editor/UI | Localization/Data | Pending signoff in §14 |
| 58 | Actual production English localization entry for every key | `UNAPPROVED` | Spec 27 makes text localization-owned | — | Reviewed English entry for every key | English UI, fallback, localization QA | Localization/Design | Pending signoff in §14 |
| 59 | Export file or Unity asset location | `UNAPPROVED` | GD65A defines an envelope, not a destination | — | Exact non-Bootstrap production path and format | Authoring, build, loading, tests | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 60 | Content manifest ownership | `UNAPPROVED` | Bootstrap manifest does not register spatial content | — | Production manifest and accountable owner/update step | Build integrity, loading, versions | Data/Content Pipeline | Pending signoff in §14 |
| 61 | Schema registration ownership | `UNAPPROVED` | Bootstrap schema map does not register spatial content | — | Production registry and accountable owner/update step | Validation, compatibility, loading | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 62 | Content service loading ownership | `UNAPPROVED` | ContentServices has no spatial consumer | — | Loader, lifecycle, failure policy, owner | Runtime availability, diagnostics | Engineering/Data | Pending signoff in §14 |
| 63 | GameRoot or non Bootstrap production assignment ownership | `UNAPPROVED` | GameRoot has no spatial assignment; Bootstrap is not production authority | — | Composition root and accountable owner | Dependency injection, scenes, tests | Engineering | Pending signoff in §14 |
| 64 | Production catalog validation invocation path | `UNAPPROVED` | Pure validator exists; no production caller | — | Import/build/load gate, limits source, failure handling | Authoring, build, loading, diagnostics | Engineering/QA/Data | Pending signoff in §14 |
| 65 | Production canonical serialization path | `UNAPPROVED` | Canonicalizer exists; no exporter owns it | — | Export invocation, encoding/file policy, owner | Reproducible exports, diffs, hashing | Data/Content Pipeline + Engineering | Pending signoff in §14 |
| 66 | Maximum top-level records | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 67 | Maximum nested records | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 68 | Maximum materialized tiles | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 69 | Maximum issues | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | QA/Performance/Data | Pending signoff in §14 |
| 70 | Maximum string characters | `UNAPPROVED` | Caller-supplied bound required; no production tuning | — | Exact configured bound and supporting evidence | Validation, canonicalization/loading, memory/performance | Performance/QA/Data | Pending signoff in §14 |
| 71 | Localization lookup workload accounting (contract mapping; no independent limit) | `APPROVED` | GD65A `SpatialContentWorkload.TryPreflight` accounts supplied localization lookup data through the existing five-field workload contract | Lookup entry count consumes `MaximumNestedRecords`; lookup key characters consume `MaximumStringCharacters` | No sixth limit, new API, or independent localization workload value; exact numeric bounds remain in the nested-record and string-character rows | Validation preflight, localization lookup, memory bounds | Engineering/QA | `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentContracts.cs`; `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs` |
| 72 | Production pipeline test ownership | `UNAPPROVED` | GD65A tests cover inactive contracts only | — | Named suite, stage, fixtures, evidence owner, failure gate | Export, registry, loading, release confidence | QA/Data/Engineering | Pending signoff in §14 |

### 4.1 GD65B0C1 approved construction and occupancy policy

One room or structure tile remains exactly one floor-space unit. For design guidance, one tile is an abstract construction and occupancy cell approximately equivalent to a 5 × 5-foot area; this approximation is not an exact movement, collision, combat, or tabletop-distance simulation, creates no serialized measurement or runtime conversion, and is not a second capacity authority.

For the MVP design, one standard monster placement, one standard trap placement, and one loot placement each occupy one authored usable room tile. Content occupancy is nested within the containing room's already-counted gross structural footprint: it consumes usable room-tile availability but does not add a second floor-space unit or independently increase used floor capacity. Floor-space use continues to count the deterministic union of active structural tiles once. Ordinary room-content placements cannot overlap one another and must remain within authored usable room tiles; occupying a usable tile inside the containing room is not an invalid structural overlap with that room. Both the independently authored category maximum and physical tile availability must permit a placement; an otherwise valid empty tile does not grant a category slot.

The current inactive `DungeonSpatial` schema does **not** serialize individual monster, trap, or loot spatial footprint shapes, per-instance spatial tile coordinates, or per-instance spatial occupancy. Existing abstract MVP placement selections, ordered-room layout, and room-slot assignment models remain serialized and remain the current runtime and save authority. These statements are design and data authority only: this packet does not implement any spatial placement rule, add contracts, activate a catalog or graph consumer, replace the serialized MVP models, or change save schema 6.

Later authored content may use multi-tile footprints, including large monsters, bosses, large traps, environmental features, and large loot structures or similar content. Such room content would consume several usable tiles inside its containing room but would not independently increase floor-space use beyond the containing room's already-counted gross footprint. Those examples approve direction, not production records, dimensions, or tuning. Multi-tile implementation remains post-MVP extensibility unless a later approved packet changes sequencing; GD65B0C1 adds no speculative fields or framework.

At simultaneous maximum standard monster, trap, and loot capacities, ordinary room content may occupy no more than 40% of gross footprint tiles, leaving at least 60% outside ordinary-content occupancy. This is a room-profile authoring constraint, not a runtime-hardcoded percentage, a second weighted capacity system, or an additional floor-space charge. Reserved and blocked tiles, usable-area rules, connection geometry, and clear connection-to-connection traversal remain separate authorities; remaining gross tiles are not automatically traversable, and exact reserved tiles and traversal validation remain unapproved and unimplemented.

The approved profiles and derived documentation facts are:

| Stable definition ID | Approved gross footprint | Approved monster / trap / loot capacities | Gross tiles | Approximate design scale | Maximum ordinary occupancy | Design role |
|---|---:|---:|---:|---:|---:|---|
| `spatial.room.basic` | Width 4 × Height 4 | 2 / 2 / 2 | 16 | 20 × 20 feet | 6 / 16 = 37.5% | Square, medium, balanced |
| `spatial.room.rectangle` | Width 3 × Height 5 | 3 / 1 / 2 | 15 | 15 × 25 feet | 6 / 15 = 40% | Approximately Basic space class, monster-favored, lower trap capacity; rotation changes fit, not identity/capacities |
| `spatial.room.large_chamber` | Width 5 × Height 6 | 4 / 4 / 4 | 30 | 25 × 30 feet | 12 / 30 = 40% | Large rectangular, general-purpose high capacity and greater commitment; preserve the large-room-versus-several-small-rooms tradeoff |

Rows 23–25 and 29–31 are the primary production-value authority. This summary supplies derived review facts without authoring production records.

## 5. Prototype ID warning and semantic conflict resolution

`placement.option.room.basic` and `placement.option.room.narrow_hall` are MVP **placement option IDs**. They are not approved production spatial definition IDs, naming templates, migration targets, localization keys, or catalog authority. `placement.option.room.narrow_hall` remains an unchanged legacy prototype room-option ID. This PR does not rename, remove, remap, reinterpret, or migrate it.

**Historical conflict:** the prototype classifies Narrow Hall as a room option, while GD63/Spec 38 classifies Narrow Hall and Straight Stone Corridor as one corridor category. The prototype value therefore could not be promoted automatically by silently reusing, renaming, reinterpreting, or mapping its ID.

**Resolved production identity:** production spatial content has exactly one initial MVP straight-corridor definition, with stable ID `spatial.corridor.straight_stone`. Its intended initial English display name is `Straight Stone Corridor`, but that English name is design guidance only until localization rows 53–58 are separately approved; it is not a completed localization entry. GD65B must not use the prototype Narrow Hall option as production corridor authority. No second production Narrow Hall room or corridor definition is authorized. This explicit owner approval resolves the production identity conflict recorded in row 8.

**Deferred legacy compatibility and migration:** GD66 will separately decide whether and how existing legacy Narrow Hall state maps to a corridor, room, or fallback during migration design. Phase 2 alone may implement any approved migration or runtime-authority transition. This deferred GD66 decision sits outside the numbered production-value statuses and does not alter the resolved production identity.

## 6. Pipeline ownership decisions

Rows 59–65 and 72 are `UNAPPROVED`. They require accountable owners and exact non-Bootstrap paths. The pipeline must retain one writable content authority, explicit schema/content versions, ordinal canonical serialization, pure validation, and caller-supplied bounded workloads. It must not depend on dictionary iteration, locale, filesystem enumeration, runtime hashes, source-row ordering, arbitrary duplicate resolution, or source mutation.

Existing Bootstrap manifests, schema maps, content versions, and strings are prototype/validation infrastructure, not candidate/default production authority.

## 7. Localization ownership decisions

Rows 53–58 are `UNAPPROVED`. Localization/Data must approve the namespace, keys, and reviewed English entries; lookup workload uses the existing nested-record and string-character limits recorded by row 71. Definition IDs and localization keys remain separate. Production English must not be embedded as display text or fallback literals; languages must remain addable without code changes.

## 8. Workload limit decisions

Rows 66–70 are `UNAPPROVED`; row 71 records how localization data consumes two of those existing limits and creates no sixth limit. GD65A deliberately receives limits from its caller. Performance/QA/Data must approve exact conservative configuration-owned limits with written rationale based on the approved Floor 1 content envelope and identify later validation stages. GD65B must provide production export and pipeline-execution evidence. Low-end mobile profiling and Android device evidence remain Phase 9 work unless a measured problem requires earlier investigation. Fixtures/examples are not authority. Validation must fail closed, remain pure/deterministic, and retain bounded diagnostics.

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

**GD65B is NOT READY.** The register contains 72 rows: 29 `APPROVED`, 43 `UNAPPROVED`, 0 `INFERRED_NOT_APPROVED`, 0 `CONFLICTING`, and 0 `DEFERRED`. Every production-required row must be `APPROVED` with precise evidence; informational contract-mapping rows 14 and 71 create no new production value. The production identity conflict is resolved, but the remaining blockers—Floor 1 bounds; final floor-space capacity; room reserved tile offsets, allowed orientations, maximum connection counts, and connection points; corridor geometry, remaining capacities, orientations, and sockets; entrance and completion geometry and connection data; socket compatibility; localization; export, manifest, registry, loading, validation, and canonical serialization ownership; workload limits; and production pipeline test ownership—still require assigned owner signoff. `INFERRED_NOT_APPROVED`, `UNAPPROVED`, `CONFLICTING`, or `DEFERRED` never passes the gate.

After all remaining signoff, GD65B must author and validate production records and execute the production pipeline without activating graph/save authority. GD66 signoff is not required first; GD66 begins after GD65B records exist. Activation or migration requires a separate packet.

## 12. Non-goals

- Authoring production records or choosing additional IDs, versions, geometry, capacities, connections, text, limits, or paths beyond the exact scoped values approved here.
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

### 2026-07-24 owner identity approval

- **Approver:** `gdg3417, primary developer and project owner`.
- **Roles:** Design and Data approver for the scoped decisions.
- **Decision source and durable evidence:** explicit project-owner approval supplied for this documentation PR, recorded verbatim in rows 4–10 and 13–14, the convention above the register, and §5.
- **Approved scope:** the stable ID convention, seven production definition IDs, one initial socket identity, the Narrow Hall production resolution, and the connection-point namespace mapping.
- **Excluded scope:** every remaining approval row, including geometry, capacities, localization, pipeline, workload, current-scope Save signoff, and downstream migration.

This partial signoff does not open the gate.

### 2026-07-24 owner metadata, Floor 1 references, and current-scope Save approval

- **Approver:** `gdg3417, primary developer and project owner`.
- **Roles:** Design, Data, Content Pipeline, and current-scope Save approver for these exact decisions.
- **Decision source and durable evidence:** explicit project-owner approval supplied for GD65B0B and recorded verbatim in rows 1–3, 15, and 18–22.
- **Approved scope:** catalog `SchemaId = "dungeon_spatial_content"`, `SchemaVersion = 1`, `ContentVersion = "0.1.0"`; `FloorIndex = 0`; the exact ordinal-canonical room and corridor collections; the exact entrance and completion foreign keys; `OptionalBranchAllowance = 1`; and the limited Save boundary below.
- **Excluded scope:** every other unapproved register row, future spatial persistence or migration, and all implementation work.

**Current-scope Save boundary:** Save schema remains version 6. GD65B0 and GD65B add no saved spatial state. `FloorIndex = 0` is currently content identity/index only. Ordered two-room models remain runtime and save authority; no route graph becomes writable or persistent, no migration step is added, no current save is rewritten, and no duplicate writable authority is introduced. GD66 owns final save/migration design. Phase 2 exclusively owns migration implementation, runtime-reader switching, and writable-authority transition. Future spatial persistence or migration is not approved.

This partial signoff does not open the gate.

### 2026-07-24 owner MVP room-profile approval (GD65B0C1)

- **Approver:** `gdg3417, primary developer and project owner`.
- **Roles:** Design, Data, and MVP room-profile authority for these exact decisions.
- **Decision source and durable evidence:** explicit project-owner approval supplied for GD65B0C1 and recorded verbatim in rows 23–25 and 29–31, §4.1, and Spec 38.
- **Approved scope:** the approximate construction-tile scale; the one-usable-tile standard MVP monster, trap, and loot spatial-footprint policy; nested occupancy with single-count structural floor-space accounting; non-overlap among ordinary room-content placements; future multi-tile extensibility within the containing room footprint; the 40% maximum ordinary-content occupancy authoring constraint; separate reserved/blocked/usable/traversal authority; the exact Basic Room, Rectangle Room, and Large Chamber dimensions; their exact monster, trap, and loot capacities; and the explicit limitation that the inactive `DungeonSpatial` schema does not serialize individual content spatial footprint shapes, per-instance spatial tile coordinates, or per-instance spatial occupancy. Existing abstract MVP placement selections, ordered-room layout, and room-slot assignment models remain serialized and remain current runtime/save authority.
- **Excluded scope:** Floor 1 bounds; final floor-space capacity; reserved tile offsets; allowed room orientations; maximum room connections; connection points; corridor dimensions and remaining capacities; fixed-structure geometry; socket compatibility; localization; production pipeline ownership; workload limits; production records; runtime content placement; saves; and migration.

This partial signoff does not open the gate, author records, activate placement, or claim current schema support. **GD65B remains blocked.** The next dependency-correct packet is **GD65B0C2: approve Floor 1 bounds and final floor-space capacity**; this packet does not choose those values.

| Authority | Required decisions | Approver/evidence | Status |
|---|---|---|---|
| Design | Identities, geometry/capacity, structures, sockets/connections, prototype conflict | `gdg3417, primary developer and project owner`; scoped owner record above and this documentation PR | `PARTIAL — identities, Floor 1 references/branch allowance, and initial room footprints/category capacities are approved; remaining geometry, sockets, connections, localization, and other required decisions remain UNAPPROVED` |
| Data / Content Pipeline | Schema/content versions, IDs, export, manifest/registry, serialization | `gdg3417, primary developer and project owner`; scoped owner record above and this documentation PR | `PARTIAL — schema metadata, identities, Floor 1 references, and initial room profiles are approved; export, manifest/registry, loading, canonical serialization ownership, and other required decisions remain UNAPPROVED` |
| Engineering | Production loading/assignment/validation and non-Bootstrap composition | — | `UNAPPROVED` |
| Localization | Convention, keys, English entries, lookup ownership | — | `UNAPPROVED` |
| Performance / QA | Conservative production validation limits, Floor 1 envelope rationale, later validation stages, and pipeline test ownership; device profiling remains Phase 9 | — | `UNAPPROVED` |
| Save | Current-scope boundary for Floor 1 index and unchanged schema/authority | `gdg3417, primary developer and project owner`; scoped owner record above and this documentation PR | `APPROVED — current scope only; future persistence/migration remains unapproved` |
