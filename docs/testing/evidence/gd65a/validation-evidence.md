# GD65A final validation evidence

## Commit and environment classification

- Reviewed remote PR #167 head supplied for this correction: `367c89395b97ff374d9b7ab29cdeb5653f1528ed`.
- The environment had no configured remote URL, so that remote SHA could not be fetched or independently verified.
- Confirmed local GD64/main reference: `origin/main` at `0d2d29460aa0c73d924de55e35b5b7fcd81b39cf` (merged PR #166).
- Pre-correction local branch head: `4ad63b9e46b36b2f6298879939106c6e6acad1ee`.
- Final corrected implementation and test head statically tested: `01dae2fff8d1a7f4fa1eabccbbe6052cc84704b7`.
- The commit containing this rewritten evidence file is separate from the tested implementation commit and changes documentation only; its SHA is reported in the PR description and final delivery report because a commit cannot contain its own final SHA.
- Host: Linux x86_64. Repository Unity version: `6000.3.2f1`.
- Manual validation classification: static source, contract, determinism, workload, and scope audit only.

## Workload correction

`SpatialContentWorkload.TryPreflight` now uses one directly reviewable bounded-add operation for every cumulative total. It rejects before addition when `additional < 0` or `current > maximum - additional`, then adds only after the bound succeeds. It does not use exceptions as validation flow.

The fail-closed accumulator is applied individually and immediately to:

- Every top-level floor, room, corridor, fixed-structure, and socket array count.
- Supplied localization-key count.
- Floor allowed-room and allowed-corridor references.
- Room reserved offsets, orientations, and connection points.
- Corridor orientations and compatibility references.
- Fixed-structure reserved offsets, orientations, and connection points.
- Socket compatibility references.
- Metadata, definition, localization, reference, connection-point, and socket string characters.
- Supplied localization-key characters.

A failed addition returns immediately, so later proportional collections are not enumerated or allocated. All maxima remain explicitly caller-supplied; no production default was introduced.

Focused tests calculate cumulative nested and character totals across all catalog collection types plus localization entries. They prove exact limits pass, one-under limits return only `WorkloadExceeded`, localization contributes to both totals, and existing invalid-limit behavior remains `WorkloadLimitsInvalid` only. No impossible arrays are allocated.

## Successful commands actually run

The following commands completed successfully against implementation/test head `01dae2fff8d1a7f4fa1eabccbbe6052cc84704b7`:

```text
git status --short
git diff --check
git diff --check origin/main...HEAD
git diff --name-only origin/main...HEAD
git log --oneline --decorate --max-count=6
git show --stat --oneline HEAD
sed -n '7,25p' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLayoutValidation.cs
rg -n 'DirectDoorway = 1, PhysicalCorridor = 2' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorRouteGraph.cs
rg -n 'LatestSchemaVersion = 6' Assets/_Project/Scripts/Save/SaveMigration.cs
git diff --name-only origin/main...HEAD -- Assets/_Project/Scripts/Save Assets/_Project/Data/Bootstrap Assets/_Project/Scenes Assets/_Project/Prefabs ProjectSettings
rg -n 'SpatialContentCatalog' Assets/_Project/Scripts
find Assets -type f -iname '*spatial*.json' -print
rg -n 'placement\.option\.room\.(basic|narrow_hall)' Assets/_Project/Scripts/Gameplay/DungeonSpatial
for f in $(git diff --name-only origin/main...HEAD -- 'Assets/**/*.cs'); do test -f "$f.meta"; done
git rev-parse HEAD
```

Results:

- `git status --short` was clean.
- Both diff checks passed with no output.
- The name-only PR diff contained exactly the ten intended GD65A files.
- The implementation commit changed only `SpatialContentValidation.cs` and `SpatialContentValidationTests.cs`.
- `FloorLayoutValidationReason` remains exactly 1 through 45.
- `FloorRouteConnectionKind` remains `DirectDoorway = 1` and `PhysicalCorridor = 2`.
- Save schema remains version 6.
- No save, migration, Bootstrap, manifest, schema-registry, scene, prefab, or build-setting file changed.
- Catalog references in production scripts remain confined to the inactive contract, validator, preflight, and canonicalizer; no runtime consumer exists.
- No production spatial JSON exists.
- No prototype placement ID became spatial content authority.
- Every new Unity C# file has a matching `.meta` file.

## Content-validation reason map

`SpatialContentValidationReason` retains explicit values 1 through 46:

| Value | Reason | Value | Reason |
|---:|---|---:|---|
| 1 | `CatalogMissing` | 24 | `MaximumConnectionsNegative` |
| 2 | `WorkloadLimitsInvalid` | 25 | `MaximumConnectionsExceedPoints` |
| 3 | `WorkloadExceeded` | 26 | `ConnectionPointSetMissing` |
| 4 | `DiagnosticLimitExceeded` | 27 | `ConnectionPointIdDuplicate` |
| 5 | `MetadataMissing` | 28 | `ConnectionPointOffsetInvalid` |
| 6 | `SchemaIdentityMissing` | 29 | `ConnectionPointBoundaryInvalid` |
| 7 | `SchemaIdentityMalformed` | 30 | `ConnectionPointFacingInvalid` |
| 8 | `SchemaVersionNonpositive` | 31 | `ConnectionPointOnReservedTile` |
| 9 | `ContentVersionMissing` | 32 | `ConnectionPointPositionDuplicate` |
| 10 | `DefinitionMissing` | 33 | `ForeignKeyMissing` |
| 11 | `StableIdMissing` | 34 | `ForeignKeyAmbiguous` |
| 12 | `DuplicateStableId` | 35 | `CorridorLengthInvalid` |
| 13 | `DuplicateFloorIndex` | 36 | `CorridorWidthInvalid` |
| 14 | `UnknownEnumValue` | 37 | `CorridorMonsterCapacityInvalid` |
| 15 | `FootprintMissing` | 38 | `FixedStructureKindInvalid` |
| 16 | `FootprintDimensionsInvalid` | 39 | `LocalizationKeyMissing` |
| 17 | `FootprintTileCountExceeded` | 40 | `LocalizationReferenceMissing` |
| 18 | `ReservedTileDuplicate` | 41 | `LocalizationLookupEntryMissing` |
| 19 | `ReservedTileOutsideFootprint` | 42 | `FloorIndexNegative` |
| 20 | `OrientationSetMissing` | 43 | `FloorCapacityNegative` |
| 21 | `OrientationDuplicate` | 44 | `FloorCapacityExceedsBounds` |
| 22 | `OrientationInvalid` | 45 | `FloorBranchAllowanceNegative` |
| 23 | `CapacityNegative` | 46 | `FloorBranchAllowanceExceeded` |

## Review-thread reinspection

The current code was reinspected after the workload correction:

- Invalid floor configuration remains covered: negative index/capacity/allowance, allowance above one, missing/invalid/over-limit bounds, capacity above bounds, duplicate IDs/indexes, and foreign keys.
- Localization keys remain mandatory; an optional supplied lookup is preflight-bounded and indexed once with ordinal identity.
- Duplicate and missing-ID canonical ties still use canonical serialized payloads with ordinal comparison after nested canonicalization.

These three findings remain corrected. The environment exposes no review-thread API, so thread resolution status could not be changed here; no thread is claimed resolved merely because it is outdated.

## Unity status

No Unity compilation or tests were run. `unity-editor` and `Unity` were unavailable in this environment. No pass, failure, skipped, or inconclusive totals are claimed. Unity testing is planned only after the next ChatGPT review.

## Scope confirmations and limitations

- No production spatial identity, numeric value, localization record, export registration, or content-version decision was added; those remain GD65B gates.
- Test fixtures remain inline under `test.gd65a.*`.
- The catalog remains inactive and disconnected from ContentService, Bootstrap, manifests, schema registration, saves, migration, scenes, prefabs, UI, gameplay, and runtime authority.
- Save schema remains 6 and ordered two-room runtime/save authority remains unchanged.
- Static validation does not prove Unity compilation, gameplay correctness, or fun.
- Remote SHA verification remains unavailable because no remote URL is configured.
