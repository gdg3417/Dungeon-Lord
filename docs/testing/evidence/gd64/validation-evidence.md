# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. Explicit caller-supplied tile-materialization limits now bound geometry resolution, validation of supplied tile collections, and layout canonicalization. The domain remains inactive and non-authoritative.

## Reviewed and final heads

The previous reviewed remote head is `6cdce520cbb73ff2f0c0f77ae032754607543acb`. Corrections are committed to `codex/implement-gd64-spatial-contracts-and-validation`; the resulting final branch-head SHA is reported in the final delivery record and PR metadata after commit. No separate PR is opened and no merge is performed.

## Bounded spatial processing

`SpatialValidationWorkloadLimits` remains the immutable caller-supplied contract for `MaximumMaterializedTileCount`; it has no default, inferred, global, or production value. Rectangle and straight-corridor resolution reject over-limit tile counts before proportional allocation. Validation rejects oversized raw corridor and reserved-tile collections before sorting, deduplication, set insertion, diagnostics, capacity, or graph authority.

Unlimited layout canonicalization was removed. `FloorSpatialLayout.TryCanonicalize(limits, out canonical)` starts with a null result, fails closed for nonpositive limits, preflights every supplied edge footprint before copying or sorting any footprint, and returns no partial layout if any footprint is oversized. Accepted canonicalization retains detached copies, established room/node/edge ordering, X/Y footprint ordering, optional-string normalization, valid blank-doorway normalization, and preservation of bounded invalid doorway payloads.

## Static validation completed on 2026-07-22

- `git status --short`: run before and after commit.
- `git diff --check`: passed with no output before commit.
- `git diff --check origin/main...HEAD`: passed with no output against the verified GD63 baseline.
- `git diff --name-only origin/main...HEAD`: returned the four inactive-domain source files, two focused EditMode tests, README, roadmap, and this evidence file.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- Save-schema inspection confirmed version 6; SaveData inspection found no spatial field.
- DungeonSpatial reference-boundary inspection found no use outside the inactive domain and focused EditMode tests.
- Automated reason extraction returned exactly contiguous values 1 through 45.
- Prohibited-path, generated-file, and unlimited-canonicalization scans returned no matches.

## Unity validation pending

Unity is not installed in this execution environment (`command -v unity-editor || command -v Unity` returns no executable). No Unity execution or pass counts are claimed:

- `DungeonSpatialContractTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- `FloorLayoutValidatorTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- Complete Unity EditMode suite: pending; pass/fail/skipped/inconclusive counts unavailable.

## Deliberately deferred validation capabilities

GD64 does not validate direct-doorway adjacency; doorway orientation, connection points, reserved tiles, or passability; corridor-to-endpoint geometric attachment; complete optional-branch shape, loops, nesting, dead-end behavior, or alternate terminals; or buildable/unavailable tile masks and expansion policy. These remain later approved gates. GD65 must not claim complete spatial-layout validation solely from GD64.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. Existing ordered-room systems remain runtime/save authority.
