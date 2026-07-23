# GD65A corrected validation evidence

## Commit and environment classification

- Remote reviewed head supplied for PR #167: `3a9e5b93f21a3fb274a853aadaad2136194691be`.
- Remote fetch command was attempted before editing but the provided environment had no configured `origin`; therefore remote verification could not be completed locally.
- Preloaded local PR patch head: `19dd39b` on top of `0d2d29460aa0c73d924de55e35b5b7fcd81b39cf`.
- Confirmed `main` baseline represented merged PR #166 / GD64 at `0d2d29460aa0c73d924de55e35b5b7fcd81b39cf`.
- Corrected implementation and test head statically tested: `1139660ec34acd5c6742811898cd6df39ebc780a`.
- The commit following that implementation head changes this evidence file only; no implementation file changed after the recorded checks.
- Host: Linux x86_64. Repository Unity version: `6000.3.2f1`.
- Manual validation classification: static source, contract, determinism, and scope audit only.

## Commands actually run

Commands run before and during inspection:

```text
git status --short --branch
git remote -v
git fetch origin codex/define-inactive-spatial-content-schema
git branch -avv
git log --oneline --decorate -12
git cat-file -t 3a9e5b93f21a3fb274a853aadaad2136194691be
git checkout -b codex/define-inactive-spatial-content-schema
git diff --stat 0d2d294...HEAD
sed -n '1,260p' Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs
sed -n '1,160p' Assets/_Project/Tests/EditMode/SpatialContentValidationTests.cs
sed -n '1,120p' docs/testing/evidence/gd65a/validation-evidence.md
sed -n '1,100p' docs/planning/gd63-spatial-and-progression-design-decisions.md
sed -n '1,120p' Assets/_Project/Tests/EditMode/FloorLayoutValidatorTests.cs
rg -n "SpatialContentValidationWorkloadLimits\(" Assets --glob '*.cs'
rg -n "GetHashCode|RuntimeHelpers|FirstOrDefault|ToDictionary" Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialContentValidation.cs
command -v dotnet
command -v csc
command -v mcs
```

Static checks run against corrected implementation and test head `1139660ec34acd5c6742811898cd6df39ebc780a`:

```text
git status --short
git diff --check
git diff --check origin/main...HEAD
git diff --name-only origin/main...HEAD
sed -n '7,25p' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLayoutValidation.cs
rg -n 'DirectDoorway = 1, PhysicalCorridor = 2' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorRouteGraph.cs
rg -n 'LatestSchemaVersion = 6' Assets/_Project/Scripts/Save/SaveMigration.cs
rg -n 'SpatialContentCatalog' Assets/_Project/Scripts --glob '!Gameplay/DungeonSpatial/SpatialContentContracts.cs' --glob '!Gameplay/DungeonSpatial/SpatialContentValidation.cs'
git diff --name-only origin/main...HEAD -- Assets/_Project/Data/Bootstrap Assets/_Project/Scripts/Save Assets/_Project/Scenes Assets/_Project/Prefabs ProjectSettings
find Assets -type f -iname '*spatial*.json' -print
rg -n 'placement\.option\.room\.(basic|narrow_hall)' Assets/_Project/Scripts/Gameplay/DungeonSpatial Assets/_Project/Tests/EditMode/SpatialContentValidationTests.cs
for f in $(git diff --name-only origin/main...HEAD -- 'Assets/**/*.cs'); do test -f "$f.meta"; done
command -v unity-editor || command -v Unity || find /opt /usr/local -maxdepth 3 -iname Unity -type f
git rev-parse HEAD
git branch --show-current
uname -a
cat ProjectSettings/ProjectVersion.txt
```

Results:

- `git status --short` was clean at the corrected implementation head.
- Both `git diff --check` commands passed with no output.
- The name-only diff contained only the ten intended GD65A files.
- GD64 `FloorLayoutValidationReason` remains exactly 1 through 45.
- `FloorRouteConnectionKind` remains `DirectDoorway = 1` and `PhysicalCorridor = 2`.
- Save schema remains version 6.
- No SaveData, migration, Bootstrap data, manifest, schema registry, scene, prefab, build-setting, or production spatial JSON changed.
- No active runtime consumer was added; matches were confined to the inactive content contract/validator.
- No prototype placement ID became spatial content authority.
- Every new Unity C# file has its matching `.meta` file.
- No `FirstOrDefault`, per-definition `ToDictionary`, runtime hash, or `GetHashCode` tie-break remains in content validation/canonicalization.

## Content-validation reason map

These values are explicit and become stable when GD65A merges:

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

## Tests actually run

No Unity tests were run. `command -v unity-editor`, `command -v Unity`, and the bounded `/opt` and `/usr/local` search found no Unity executable. No compilation, pass count, failure count, skipped count, or inconclusive count is claimed.

## Unity tests pending

The blocking code corrections and test additions are committed. The following remain pending in an environment with Unity `6000.3.2f1`:

1. `DungeonBuilder.M0.Tests.EditMode.SpatialContentValidationTests`
2. `DungeonBuilder.M0.Tests.EditMode.DungeonSpatialContractTests`
3. `DungeonBuilder.M0.Tests.EditMode.FloorLayoutValidatorTests`
4. Complete EditMode suite

PlayMode, standalone-build, save-lifecycle, localization rendering, gameplay, and player-experience validation were not run and are outside this inactive packet.

## Scope confirmations

- All fixture IDs and fake localization keys remain inline under the `test.gd65a.*` test namespace.
- No production spatial ID, schema/content version, floor bound, footprint, capacity, connection, socket, corridor range, localization key, cost, modifier, or allowance was authored.
- The catalog remains inactive and is not registered with `ContentService`, Bootstrap, the content manifest, or the schema registry.
- Save schema remains 6; SaveData, migration, ordered-room authority, and runtime layout readers are unchanged.
- No scene, prefab, UI, Bootstrap control, construction economy, route selection, additional floor, or player-facing text was added.
- GD65B still owns approved authored records and production export registration; GD66 and Phase 2 remain out of scope.
- Static validation does not prove gameplay fun.

## Known limitations

Unity compilation and EditMode execution remain pending because the required editor is unavailable. Remote PR head verification also remains pending because the environment had no configured `origin`; the remote reviewed SHA above is the actual SHA supplied for PR #167. The implementation remains schema/validation-only and cannot establish production content correctness until GD65B approves and supplies authored values.
