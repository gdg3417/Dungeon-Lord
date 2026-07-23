# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. Explicit caller-supplied tile-materialization limits now bound geometry resolution, validation of supplied tile collections, and layout canonicalization. The domain remains inactive and non-authoritative.

## Reviewed and final heads

The previous failing remote head is `ba8a05ea3f595415552ccc72db6d639588d08a0e`. Corrections are committed to `codex/implement-gd64-spatial-contracts-and-validation`; the resulting branch-head SHA is reported in the final delivery record and PR metadata after commit. No separate PR is opened and no merge is performed.

## Bounded spatial processing

`SpatialValidationWorkloadLimits` remains the immutable caller-supplied contract for `MaximumMaterializedTileCount`; it has no default, inferred, global, or production value. Rectangle and straight-corridor resolution reject over-limit tile counts before proportional allocation. Validation rejects oversized raw corridor and reserved-tile collections before sorting, deduplication, set insertion, diagnostics, capacity, or graph authority.

Unlimited layout canonicalization was removed. `FloorSpatialLayout.TryCanonicalize(limits, out canonical)` starts with a null result, fails closed for nonpositive limits, preflights every supplied edge footprint before copying or sorting any footprint, and returns no partial layout if any footprint is oversized. Accepted canonicalization retains detached copies, established room/node/edge ordering, X/Y footprint ordering, optional-string normalization, valid blank-doorway normalization, and preservation of bounded invalid doorway payloads.

## Static validation completed on 2026-07-23

- `git status --short`: run before and after commit.
- `git diff --check`: passed with no output before commit.
- `git diff --check origin/main...HEAD`: passed with no output against the verified GD63 baseline.
- `git diff --name-only origin/main...HEAD`: returned the four inactive-domain source files, two focused EditMode tests, README, roadmap, and this evidence file.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- Save-schema inspection confirmed version 6; SaveData inspection found no spatial field.
- DungeonSpatial reference-boundary inspection found no use outside the inactive domain and focused EditMode tests.
- Automated reason extraction returned exactly contiguous values 1 through 45.
- Prohibited-path, generated-file, and unlimited-canonicalization scans returned no matches.
- Repository-wide `FloorLayoutValidator.Validate(` call-site inspection confirmed that every call now supplies the mandatory explicit workload-limit argument.

## Unity validation requiring rerun

The complete Unity EditMode run on the previous failing head compiled and executed 1,189 tests: 1,186 passed, 3 failed, 0 skipped, and 0 inconclusive. The failures were:

1. `DungeonSpatialContractTests.CanonicalizationAndUnityJson_PreserveMixedConnectionKindsAndDoorwayNulls`
2. `DungeonSpatialContractTests.Canonicalization_MixedKindsUseEstablishedKeysAndRoundTripDeterministically`
3. `FloorLayoutValidatorTests.StructurallyInvalidEdge_DoesNotProveReachabilityOrConsumeConnectionLimit("footprint")`

The first two failures exposed Unity's nested-serializable-object null round-trip behavior: a null footprint was restored as a non-null empty shell. The third retained the stale assumption that a one-tile physical corridor was invalid. The correction adds a private serialized footprint-presence discriminator and replaces the stale fixture with a diagonal footprint. These failures remain unresolved until the corrected branch is rerun in Unity. Unity is unavailable in this execution environment, so no corrected-run result is claimed.

## Deliberately deferred validation capabilities

GD64 does not validate direct-doorway adjacency; doorway orientation, connection points, reserved tiles, or passability; corridor-to-endpoint geometric attachment; complete optional-branch shape, loops, nesting, dead-end behavior, or alternate terminals; or buildable/unavailable tile masks and expansion policy. These remain later approved gates. GD65 must not claim complete spatial-layout validation solely from GD64.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. Existing ordered-room systems remain runtime/save authority.
