# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. The allocation-safety correction adds an explicit caller-supplied maximum materialized tile count and fails closed before oversized rectangle, corridor, raw-footprint, or reserved-tile processing. It preserves one-tile physical corridors, overflow-safe arithmetic, blank doorway-ID normalization, invalid doorway payload preservation, deduplicated raw diagnostics, and established edge ordering. The domain remains inactive and non-authoritative.

## Reviewed and final heads

The actual previous reviewed remote head is `b7602521f576b6000992535b7f691f4ccb678a90`. Corrections are committed to `codex/implement-gd64-spatial-contracts-and-validation`; the resulting branch-head SHA is recorded in the final delivery report and PR metadata after commit. No separate PR is opened and no merge is performed.

## Explicit allocation-workload contract

`SpatialValidationWorkloadLimits` is an immutable caller-supplied value containing `MaximumMaterializedTileCount`. There is no default, fallback, inferred floor-based value, mutable global, system-memory inspection, or production tuning value. Missing, zero, and negative limits fail closed. GD65 remains responsible for approving and supplying the production/export value.

Rectangle and straight-corridor resolvers reject tile counts above the limit before proportional allocation. The validator rejects oversized raw physical-corridor footprints with the existing `InvalidCorridorFootprint` reason and omits their per-coordinate processing, capacity, reachability, and connection authority. Oversized reserved-tile collections are rejected before set insertion or sorting. Focused tests use small fake limits only.

## Static validation completed on 2026-07-22

- `git status --short`: run before and after commit.
- `git diff --check`: passed with no output before commit.
- `git diff --check origin/main...HEAD`: passed with no output against the verified GD63 baseline.
- `git diff --name-only origin/main...HEAD`: returned the four inactive-domain source files, two focused EditMode tests, README, roadmap, and this evidence file.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- Save-schema inspection confirmed `SaveMigration.LatestSchemaVersion = 6`; SaveData inspection found no spatial field.
- DungeonSpatial reference-boundary inspection found no use outside the inactive domain and its focused EditMode tests.
- Automated reason extraction returned exactly contiguous values 1 through 45.
- Prohibited-path and generated-file scans returned no matches.

## Unity validation pending

Unity is not installed in this execution environment (`command -v unity-editor || command -v Unity` returns no executable). No Unity execution or pass counts are claimed:

- `DungeonSpatialContractTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- `FloorLayoutValidatorTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- Complete Unity EditMode suite: pending; pass/fail/skipped/inconclusive counts unavailable.

## Deliberately deferred validation capabilities

GD64 does not validate direct-doorway adjacency; doorway orientation, connection points, reserved tiles, or passability; corridor-to-endpoint geometric attachment; complete optional-branch shape, loops, nesting, dead-end behavior, or alternate terminals; or buildable/unavailable tile masks and expansion policy. These remain later approved gates. GD65 must not claim complete spatial-layout validation solely from the GD64 validator.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. Existing ordered-room systems remain runtime/save authority.
