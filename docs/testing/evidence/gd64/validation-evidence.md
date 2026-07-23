# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. Explicit caller-supplied tile-materialization limits now bound geometry resolution, validation of supplied tile collections, and layout canonicalization. The domain remains inactive and non-authoritative.

## Validated implementation and documentation heads

The successfully tested implementation head is `620f8de348e198f3c6bc83f8f43b5bbe24f4f293`. The documentation-only finalization commit is reported in the final delivery record and PR metadata after commit. No separate PR is opened and no merge is performed.

## Bounded spatial processing

`SpatialValidationWorkloadLimits` remains the immutable caller-supplied contract for `MaximumMaterializedTileCount`; it has no default, inferred, global, or production value. Rectangle and straight-corridor resolution reject over-limit tile counts before proportional allocation. Validation rejects oversized raw corridor and reserved-tile collections before sorting, deduplication, set insertion, diagnostics, capacity, or graph authority.

Unlimited layout canonicalization was removed. `FloorSpatialLayout.TryCanonicalize(limits, out canonical)` starts with a null result, fails closed for nonpositive limits, preflights every supplied edge footprint before copying or sorting any footprint, and returns no partial layout if any footprint is oversized. Accepted canonicalization retains detached copies, established room/node/edge ordering, X/Y footprint ordering, optional-string normalization, valid blank-doorway normalization, and preservation of bounded invalid doorway payloads.

The Unity serialization correction distinguishes absent footprints from explicitly present empty and nonempty footprints. It preserves null valid doorway footprints, explicit invalid empty footprint shells, invalid nonempty doorway footprints, and physical-corridor footprints through bounded canonicalization and Unity JSON round-trip. One-tile physical corridors remain supported.

## Static validation completed on 2026-07-23

- `git status --short`: run before and after the documentation commit.
- `git diff --check`: passed with no output before the documentation commit.
- `git diff --check origin/main...HEAD`: run after commit against the verified GD63 baseline.
- `git diff --name-only HEAD^`: run after commit to confirm that finalization changes this evidence file only.
- Prior GD64 static checks remain passed: no `FloorSpaceCost`, schema version 6, no SaveData spatial field, no outside-domain DungeonSpatial use, exact reason values 1 through 45, and no prohibited/generated paths.

## Unity validation passed

Unity compiled normally and did not enter Safe Mode for corrected implementation head `620f8de348e198f3c6bc83f8f43b5bbe24f4f293`.

- `DungeonSpatialContractTests`: passed.
- `FloorLayoutValidatorTests`: passed.
- Complete Unity EditMode suite: passed.
- No failed, skipped, or inconclusive tests were reported.
- All executed tests passed; exact successful-run totals and fixture-level counts were not supplied.

The three tests that failed in the earlier run now pass:

1. `DungeonSpatialContractTests.CanonicalizationAndUnityJson_PreserveMixedConnectionKindsAndDoorwayNulls`
2. `DungeonSpatialContractTests.Canonicalization_MixedKindsUseEstablishedKeysAndRoundTripDeterministically`
3. `FloorLayoutValidatorTests.StructurallyInvalidEdge_DoesNotProveReachabilityOrConsumeConnectionLimit("footprint")`

### Superseded failed-run history

The earlier complete EditMode run on failing head `ba8a05ea3f595415552ccc72db6d639588d08a0e` compiled and executed 1,189 tests: 1,186 passed, 3 failed, 0 skipped, and 0 inconclusive. Its failures exposed Unity nested-object null round-trip semantics and a stale one-tile-invalid test assumption. That result is superseded by the successful corrected-head rerun above.

No gameplay, PlayMode, save-lifecycle, standalone-build, localization, resolution, or player-experience testing was required for this inactive domain-foundation change.

## Deliberately deferred validation capabilities

GD64 does not validate direct-doorway adjacency; doorway orientation, connection points, reserved tiles, or passability; corridor-to-endpoint geometric attachment; complete optional-branch shape, loops, nesting, dead-end behavior, or alternate terminals; or buildable/unavailable tile masks and expansion policy. These remain later approved gates. GD65 must not claim complete spatial-layout validation solely from GD64.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. Existing ordered-room systems remain runtime/save authority.
