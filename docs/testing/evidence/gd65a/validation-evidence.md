# GD65A validation evidence

## Commit and environment classification

- Confirmed local base: `0d2d29460aa0c73d924de55e35b5b7fcd81b39cf` (merged PR #166 / GD64).
- Reviewed PR #167 head supplied with the Unity failure report: `452b36955a852a6d637efb8eb160dbaecb8fe111`.
- The environment has no configured remote URL, so the supplied remote head could not be fetched independently. The preloaded local equivalent was `cef588b`.
- Scratch-payload correction implementation/test head statically tested: `8e303910dc80a6467b45195099552117ba039e81`.
- The commit containing this evidence update follows the tested implementation head and changes this documentation file only; its SHA is reported in the PR description and final report.
- Repository Unity version: `6000.3.2f1`. Codex host: Linux x86_64.

## First full Unity EditMode run — preserved failure history

The user executed the complete EditMode suite with Unity `6000.3.2f1` at remote commit `452b36955a852a6d637efb8eb160dbaecb8fe111`:

- Total: **1,211**
- Passed: **1,205**
- Failed: **6**
- Inconclusive: **0**
- Skipped: **0**

All six failures were in `DungeonBuilder.M0.Tests.EditMode.SpatialContentValidationTests`.

Five were null-preservation/canonical-copy failures caused by using a `JsonUtility` round trip as the detached copy:

1. `CanonicalizationPreservesBoundedInvalidPayloadAndNullCollections` — a null reserved-offset array became empty.
2. `FloorBoundsAndDuplicateIndexes_AreValidated` — null floor bounds became a default bounds object, changing `FootprintMissing` to `FootprintDimensionsInvalid`.
3. `FootprintsReservedTilesAndOrientations_AreValidatedIndependently` — a null room footprint became a default footprint object, changing the stable reason.
4. `MetadataMissingAndMalformed_AreReported` — null metadata became a default metadata object, changing `MetadataMissing` into malformed-metadata reasons.
5. `MissingDuplicateAndNullDefinitions_AreReportedInEveryNamespace` — null top-level elements became default definitions, suppressing `DefinitionMissing`.

One was a test-helper failure:

6. `NullCatalogAndInvalidLimits_FailClosed` — nullable helper fallback replaced the intended zero-valued limit with valid test limits, so production invalid-limit behavior was never invoked.

This failed run is historical evidence and is not replaced by the correction. Corrected Unity results remain pending.

## Second full Unity EditMode run — one remaining failure

The user reran the complete EditMode suite with Unity `6000.3.2f1` at remote commit `5b9b88679c9da5232e02350de4153bc5af85bebc`:

- Total: **1,213**
- Passed: **1,212**
- Failed: **1**
- Inconclusive: **0**
- Skipped: **0**

The only failure was `DetachedCopyPreservesCompleteNullTopologyAndStableReasons`. A null room connection-point element was materialized as a default `SpatialConnectionPointDefinition` while `JsonUtility.ToJson` generated a deterministic tie-break payload from the live canonical room. The explicit deep copy itself had preserved the null correctly.

The correction now builds topology from the canonical record, creates a second disposable type-specific scratch copy, and serializes only that scratch copy. Unity serialization can therefore mutate only discarded payload input, never the canonical result. All top-level and nested connection-point `SortRecords` calls supply their existing explicit copy helper. Corrected Unity results remain pending.

## Correction implemented

`SpatialContentCanonicalizer` no longer uses `JsonUtility.ToJson` / `FromJson` as its detached-copy mechanism. It now performs an explicit deep copy after successful workload preflight. Small copy helpers cover the catalog, metadata, every top-level definition, bounds, footprints, connection points, string arrays, coordinate/enum arrays, and all scalar fields.

The copy preserves exactly:

- Null metadata.
- Null versus empty top-level collections.
- Null top-level definition elements.
- Null floor bounds and null room/fixed footprints.
- Null versus empty nested arrays.
- Null connection-point elements and null string-array elements.
- Invalid scalar/enum values, IDs, versions, capacities, coordinates, duplicates, and input order before intentional sorting.

Nested collections are canonicalized only after copying. Top-level ordering remains ordinal by stable ID with deterministic canonical-payload tie-breaking. The payload key includes explicit null-topology markers from the canonical record. Unity JSON scalar payload representation is generated only from a disposable explicit scratch copy, so serialization cannot mutate the canonical record. The supplied source is never mutated.

The invalid-limit test now directly calls `SpatialContentValidator.Validate` with `default(SpatialContentValidationWorkloadLimits)`, bypassing the nullable convenience helper.

Focused coverage now explicitly asserts null metadata, bounds, room/fixed footprints, top-level elements, connection-point elements, nested arrays, string entries, null top-level collections, empty arrays, source non-mutation, stable reasons before/after canonicalization, and deterministic sorting. It distinguishes `MetadataMissing` from malformed metadata, `FootprintMissing` from invalid dimensions, `DefinitionMissing` from missing IDs, and null arrays from empty arrays.

Unity JSON round-trip behavior remains separately tested, but is not claimed to preserve every in-memory null distinction.

## Static validation actually executed

The following commands completed successfully against implementation/test head `8e303910dc80a6467b45195099552117ba039e81`:

```text
git status --short
git diff --check
git diff --check 0d2d29460aa0c73d924de55e35b5b7fcd81b39cf...HEAD
git diff --name-only 0d2d29460aa0c73d924de55e35b5b7fcd81b39cf...HEAD
git log --oneline --decorate --max-count=8
git show --stat --oneline HEAD
rg -n 'LatestSchemaVersion = 6' Assets/_Project/Scripts/Save/SaveMigration.cs
sed -n '7,25p' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLayoutValidation.cs
rg -n 'DirectDoorway = 1, PhysicalCorridor = 2' Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorRouteGraph.cs
git diff --name-only 0d2d29460aa0c73d924de55e35b5b7fcd81b39cf...HEAD -- Assets/_Project/Scripts/Save Assets/_Project/Data/Bootstrap Assets/_Project/Scenes Assets/_Project/Prefabs ProjectSettings Assets/_Project/UI
rg -n 'SpatialContentCatalog' Assets/_Project/Scripts
find Assets -type f -iname '*spatial*.json' -print
for f in $(git diff --name-only 0d2d29460aa0c73d924de55e35b5b7fcd81b39cf...HEAD -- 'Assets/**/*.cs'); do test -f "$f.meta"; done
git rev-parse HEAD
```

Results:

- Status was clean before the evidence update.
- Both diff checks passed with no output.
- The PR diff contains exactly the ten intended GD65A files.
- The scratch-payload correction commit changes only `SpatialContentValidation.cs` and `SpatialContentValidationTests.cs`.
- Save schema remains 6.
- `FloorLayoutValidationReason` remains exactly 1 through 45.
- `FloorRouteConnectionKind` remains `DirectDoorway = 1` and `PhysicalCorridor = 2`.
- `SpatialContentValidationReason` remains exactly 1 through 46.
- No save, migration, Bootstrap, manifest, schema-registry, scene, prefab, build-setting, or UI file changed.
- No runtime catalog consumer or production spatial JSON was added.
- Every new Unity C# file has its matching `.meta` file.

## Unity status after correction

Unity was unavailable in the Codex environment, so the corrected tests were not executed here. No corrected pass/failure totals are claimed.

The user must rerun with Unity `6000.3.2f1`:

1. `DungeonBuilder.M0.Tests.EditMode.SpatialContentValidationTests`
2. `DungeonBuilder.M0.Tests.EditMode.DungeonSpatialContractTests`
3. `DungeonBuilder.M0.Tests.EditMode.FloorLayoutValidatorTests`
4. The complete EditMode suite

## Scope confirmations and limitations

- No production spatial IDs, numeric values, localization records, export registration, or content-version decision was added; those remain GD65B responsibilities.
- Test fixture values remain under `test.gd65a.*`.
- The catalog remains inactive and disconnected from runtime content loading, saves, migration, scenes, prefabs, UI, gameplay, Bootstrap, manifests, and schema registration.
- Existing ordered two-room runtime/save authority remains unchanged.
- Remaining uncertainty is corrected Unity compilation and execution; static checks cannot substitute for that rerun or prove gameplay fun.
