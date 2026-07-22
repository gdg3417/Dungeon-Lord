# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. This final hardening correction permits one-tile physical corridors, makes coordinate arithmetic overflow-safe, normalizes semantically blank doorway IDs, preserves nonblank invalid doorway payloads, deduplicates raw-coordinate diagnostics, and strengthens deterministic ordering tests. It does not activate spatial runtime authority, alter saves or migration, author gameplay content, or add player-facing behavior.

## Reviewed head

The requested reviewed PR head is `addc13de2a7aee320556616dccef264d5d67a15a`. This checkout had no configured GitHub remote and did not contain that object, so the remote SHA could not be independently resolved. The correction was applied to the GD64 snapshot supplied in the checkout on the requested `codex/implement-gd64-spatial-contracts-and-validation` branch. The final local commit and PR metadata are reported after commit; no separate PR is opened.

## Static validation completed on 2026-07-22

- `git status --short`: completed before commit; the correction changes three inactive-domain source files, two focused EditMode test files, and this evidence file.
- `git diff --check`: passed with no output before commit.
- `git diff --check origin/main...HEAD`: passed with no output against the verified GD63 baseline.
- `git diff --name-only origin/main...HEAD`: returned exactly `FloorLayoutValidation.cs`, `FloorRouteGraph.cs`, `SpatialLayoutContracts.cs`, `TileGeometry.cs`, `DungeonSpatialContractTests.cs`, `FloorLayoutValidatorTests.cs`, `README.md`, the roadmap, and this evidence file.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- `git grep -n 'LatestSchemaVersion = 6' -- Assets/_Project/Scripts/Save/SaveMigration.cs`: passed; save schema remains 6.
- SaveData inspection found no spatial, floor-route, tile, or graph field.
- DungeonSpatial reference inspection found no use outside the inactive domain and its two focused EditMode tests.
- Automated reason-code extraction returned exactly the contiguous values 1 through 45.
- Prohibited-path and generated-file scans returned no nested `dungeon-builder/`, Library, logs, builds, test-result exports, scenes, prefabs, GameRoot, Bootstrap, build settings, or ordered-room-system changes.

## Unity validation pending

Unity is not installed in this execution environment (`command -v unity-editor || command -v Unity` returns no executable). No Unity execution or pass counts are claimed:

- `DungeonSpatialContractTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- `FloorLayoutValidatorTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- Complete Unity EditMode suite: pending; pass/fail/skipped/inconclusive counts unavailable.

## Deliberately deferred validation capabilities

GD64 does not validate direct-doorway adjacency; doorway orientation, connection points, reserved tiles, or passability; corridor-to-endpoint geometric attachment; complete optional-branch shape, loops, nesting, dead-end behavior, or alternate terminals; or buildable/unavailable tile masks and expansion policy. These remain later approved gates. GD65 must not claim complete spatial-layout validation solely from the GD64 validator.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. This remains an inactive domain-foundation change, and the graph remains non-authoritative.
