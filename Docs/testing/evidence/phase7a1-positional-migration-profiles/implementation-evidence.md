# Phase 7A1 positional migration profiles implementation evidence

## Scope and baseline

- Original baseline: `b87de8ef3cdb0dbc5eff1f926a11e9dc9073eb97` (merged PR #221).
- Qualified implementation HEAD: `ad9397bf46f6ffa73b307941bd5231783efb8461` (evidence-only follow-up commit excluded).
- Objective: freeze and validate schema-12 legacy room-content placement compatibility data without activating positional save state.
- Compatibility asset: `Assets/_Project/Data/Production/Save/room_content_position_migration_profiles.json`.
- Contract: `room_content_position_migration_profiles`, version 1, profile-set version 1.
- Source save schema: 12.
- Writable save schema before/after: 12 -> 12.

## Architecture

The source-controlled asset directly authors five room/orientation profiles. Strict UTF-8/JSON parsing, workload limits, exact canonical byte validation, per-profile SHA-256 integrity, whole-set SHA-256 integrity, production coverage validation, frozen footprint/reserved-tile/capacity checks, category compatibility, and extensible occupancy-footprint validation all fail closed. An immutable validated snapshot returns detached values. The pure planner accepts one validated profile and legacy assignments, reuses `CanonicalSpatialSaveContracts.CanonicalOrderAssignments`, and returns either a complete detached assignment-to-slot mapping or an empty failure result.

The explicit authoring operation uses the same `Canonicalize`, `ComputeProfileHash`, `ComputeSetHash`, and `SerializeCanonical` implementation used by validation. Repeated regeneration produced identical bytes and hashes. Validation never rewrites an asset. Future production tuning changes can make the frozen context fail validation but cannot silently choose different migration coordinates. Only the explicitly listed slots are authorized migration placement area; gross unreserved room tiles are not treated as automatically usable.

Canonical assignment order is:

1. `RoomInstanceId`, ordinal.
2. Category rank: monster, trap, loot.
3. `Sequence`, ascending.
4. `AssignmentId`, ordinal.
5. `OptionId`, ordinal.

Every current production slot allows exactly one ordinary category and occupies offset `(0,0)`. The contract supports multiple occupied offsets; synthetic validation proves a non-overlapping multi-tile footprint is representable and an overlapping multi-tile footprint is rejected. No production multi-tile content is introduced.

## Production coverage and capacities

| Profile | Oriented footprint | Monster | Trap | Loot | Maximum envelope | Slots |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `spatial.room.basic` / Zero | 4 x 4 | 2 | 2 | 2 | 6 | 6 |
| `spatial.room.rectangle` / Zero | 3 x 5 | 3 | 1 | 2 | 6 | 6 |
| `spatial.room.rectangle` / Ninety | 5 x 3 | 3 | 1 | 2 | 6 | 6 |
| `spatial.room.large_chamber` / Zero | 5 x 6 | 4 | 4 | 4 | 12 | 12 |
| `spatial.room.large_chamber` / Ninety | 6 x 5 | 4 | 4 | 4 | 12 | 12 |

All five frozen reserved-tile collections are empty, matching current production authority.

## Exact authored migration slots

Slot order below is the exact compatibility order in the source asset.

### Basic Zero

| Order | Slot ID | Category | Anchor | Occupied offsets |
| ---: | --- | --- | --- | --- |
| 0 | `monster.00` | monster | `(1,1)` | `(0,0)` |
| 1 | `monster.01` | monster | `(2,1)` | `(0,0)` |
| 2 | `trap.00` | trap | `(1,2)` | `(0,0)` |
| 3 | `trap.01` | trap | `(2,2)` | `(0,0)` |
| 4 | `loot.00` | loot | `(0,0)` | `(0,0)` |
| 5 | `loot.01` | loot | `(3,3)` | `(0,0)` |

### Rectangle Zero

| Order | Slot ID | Category | Anchor | Occupied offsets |
| ---: | --- | --- | --- | --- |
| 0 | `monster.00` | monster | `(1,1)` | `(0,0)` |
| 1 | `monster.01` | monster | `(1,2)` | `(0,0)` |
| 2 | `monster.02` | monster | `(1,3)` | `(0,0)` |
| 3 | `trap.00` | trap | `(0,1)` | `(0,0)` |
| 4 | `loot.00` | loot | `(2,1)` | `(0,0)` |
| 5 | `loot.01` | loot | `(2,3)` | `(0,0)` |

### Rectangle Ninety

| Order | Slot ID | Category | Anchor | Occupied offsets |
| ---: | --- | --- | --- | --- |
| 0 | `monster.00` | monster | `(1,1)` | `(0,0)` |
| 1 | `monster.01` | monster | `(2,1)` | `(0,0)` |
| 2 | `monster.02` | monster | `(3,1)` | `(0,0)` |
| 3 | `trap.00` | trap | `(1,2)` | `(0,0)` |
| 4 | `loot.00` | loot | `(1,0)` | `(0,0)` |
| 5 | `loot.01` | loot | `(3,0)` | `(0,0)` |

### Large Chamber Zero

| Order | Slot ID | Category | Anchor | Occupied offsets |
| ---: | --- | --- | --- | --- |
| 0 | `monster.00` | monster | `(1,1)` | `(0,0)` |
| 1 | `monster.01` | monster | `(2,1)` | `(0,0)` |
| 2 | `monster.02` | monster | `(3,1)` | `(0,0)` |
| 3 | `monster.03` | monster | `(1,2)` | `(0,0)` |
| 4 | `trap.00` | trap | `(2,2)` | `(0,0)` |
| 5 | `trap.01` | trap | `(3,2)` | `(0,0)` |
| 6 | `trap.02` | trap | `(1,3)` | `(0,0)` |
| 7 | `trap.03` | trap | `(2,3)` | `(0,0)` |
| 8 | `loot.00` | loot | `(3,3)` | `(0,0)` |
| 9 | `loot.01` | loot | `(1,4)` | `(0,0)` |
| 10 | `loot.02` | loot | `(2,4)` | `(0,0)` |
| 11 | `loot.03` | loot | `(3,4)` | `(0,0)` |

### Large Chamber Ninety

| Order | Slot ID | Category | Anchor | Occupied offsets |
| ---: | --- | --- | --- | --- |
| 0 | `monster.00` | monster | `(1,3)` | `(0,0)` |
| 1 | `monster.01` | monster | `(1,2)` | `(0,0)` |
| 2 | `monster.02` | monster | `(1,1)` | `(0,0)` |
| 3 | `monster.03` | monster | `(2,3)` | `(0,0)` |
| 4 | `trap.00` | trap | `(2,2)` | `(0,0)` |
| 5 | `trap.01` | trap | `(2,1)` | `(0,0)` |
| 6 | `trap.02` | trap | `(3,3)` | `(0,0)` |
| 7 | `trap.03` | trap | `(3,2)` | `(0,0)` |
| 8 | `loot.00` | loot | `(3,1)` | `(0,0)` |
| 9 | `loot.01` | loot | `(4,3)` | `(0,0)` |
| 10 | `loot.02` | loot | `(4,2)` | `(0,0)` |
| 11 | `loot.03` | loot | `(4,1)` | `(0,0)` |

## Failure and regression coverage

Focused tests cover missing/empty/unreadable input, invalid UTF-8 and framing, malformed/unexpected JSON, wrong contract/profile/source versions, duplicate profile IDs and room/orientation pairs, unknown rooms, unsupported orientations, missing production coverage, frozen-context mismatch, capacity mismatch, insufficient slots, duplicate slot IDs/orders, invalid category compatibility, invalid/overlapping/out-of-footprint/reserved occupancy, integrity mismatch, input-size limits, deterministic canonicalization, input permutation, maximum envelopes, identity preservation, source immutability, and complete-or-failure planning.

Save regressions confirm `CanonicalSaveSchemaVersions.CurrentWritableTarget == 12`, `SaveMigration.LatestSchemaVersion == 12`, the public `RoomContentAssignment` shape remains exactly `AssignmentId`, `RoomInstanceId`, `CategoryId`, `OptionId`, and `Sequence`, no schema-12-to-13 upgrade type exists, and `GameRoot`/`SaveService` do not reference the inactive profile owner. Existing schema-11-to-12 and schema-12 serialization/load/save behavior remain covered by the complete regression suites.

## Automated qualification

| Run | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Focused A1 EditMode | 13 | 13 | 0 | 0 |
| Production spatial build-gate fixture | 59 | 59 | 0 | 0 |
| Full EditMode | 1,315 | 1,315 | 0 | 0 |
| Full PlayMode | 2,801 | 2,791 | 0 | 10 |

The 10 PlayMode skips are existing environment-qualified cases: eight synchronous EditMode-only GameRoot fixtures, one non-Windows inverse filesystem fixture, and one Windows-player-only standalone qualification fixture. No A1 test was skipped. Unity emitted benign licensing-client signature/reconnect and shutdown debugger/thread messages; no compile or test error resulted.

## Boundary confirmations

- No live positional migration was activated and no schema 13 migration exists.
- Historical schema-12 `RoomContentAssignment` and its serialized bytes remain non-positional.
- Native schema-12 load/save does not require the new asset; only the production build gate validates it.
- Profile failure cannot mutate or rewrite an existing save.
- No SaveService or GameRoot authority changed.
- No placement, unassignment, redeployment, custody, mana, investment, layout, lifecycle, or activation behavior changed.
- `PhaseSixFloorKnowledge` and `FloorKnowledgeApplicability` were not changed. A2 must add authoritative room-local positions to material canonical floor state while continuing to exclude `ActivationState`.
- No player-facing UI, positional controls, localization strings, or intraroom simulation were added.
- No runtime-generated or fallback migration coordinates exist.
- `ProjectSettings` has no final diff; Unity's temporary application-identifier ordering rewrite was restored.

## Changed files

- `Assets/_Project/Data/Production/Save/room_content_position_migration_profiles.json` and metadata.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/RoomContentPositionMigrationProfiles.cs` and metadata.
- `Assets/_Project/Editor/DungeonSpatial/RoomContentPositionMigrationProfileAuthoring.cs` and metadata.
- `Assets/_Project/Editor/DungeonSpatial/ProductionSpatialContentBuildGate.cs`.
- `Assets/_Project/Editor/DungeonSpatial/Tests/RoomContentPositionMigrationProfileTests.cs` and metadata.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSpatialSaveContracts.cs`.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/ProductionSpatialGeneratedSet.cs`.
- This evidence document.

## Known limitations and next dependency

A1 intentionally does not publish positions, migrate saves, change floor-knowledge fingerprints, or provide placement UI. Limited owner validation remains: clean import/compile, load an existing schema-12 save, exercise unchanged placement/unassignment/redeployment, save/reopen, confirm schema 12, and confirm no new positional/editor UI. A Windows standalone build is not required because runtime composition and player-facing behavior did not change.

Phase 7A2 remains required. It may begin only after A1 review, qualification, and merge, and must use these frozen profiles for the explicit schema-12-to-positional migration.
