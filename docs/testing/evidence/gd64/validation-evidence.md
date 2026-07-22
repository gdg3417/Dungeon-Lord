# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. The review correction preserves invalid direct-doorway data through canonicalization and separates physical-corridor raw-geometry diagnostics from capacity and graph authority. It does not activate spatial runtime authority, alter saves or migration, author gameplay content, or add player-facing behavior.

## Reviewed-head note

The supplied PR head was `a6e42d39df6e8d1c11cd7fa3f2f31504a4c9b4d9`. The repository snapshot available in this environment contained equivalent GD64 changes at local head `e00a95048909f4fb04d912be709935adc67253ed`, while the supplied object and GitHub remote were unavailable locally. Corrections were committed on the requested `codex/implement-gd64-spatial-contracts-and-validation` branch without opening a separate PR.

## Static validation completed on 2026-07-22

- `git status --short`: completed before commit; only the two inactive-domain source files, two focused EditMode test files, and this evidence file were changed by the correction.
- `git diff --check`: passed with no output before commit.
- `git diff --check origin/main...HEAD`: passed with no output after commit.
- `git diff --name-only origin/main...HEAD`: completed after commit; the full PR contains only the four inactive-domain source files, two focused EditMode test files, README, roadmap, and this evidence file.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- `git grep -n 'LatestSchemaVersion = 6' -- Assets/_Project/Scripts/Save/SaveMigration.cs`: passed; schema version remains 6.
- SaveData spatial-field inspection: passed with no spatial, tile, floor-route, or graph field.
- DungeonSpatial reference boundary check: passed with no reference outside the inactive domain and its two focused EditMode fixtures.
- Reason-code inspection: passed; values 1–45 remain contiguous and unchanged, with no new reason.
- Changed-path boundary check: passed with no nested `dungeon-builder/`, generated Unity, build, log, result-export, Library, local-save, GameRoot, Bootstrap, scene, prefab, content, localization, build-setting, or ordered-room-system path.

## Unity validation pending

Unity is not installed in this execution environment (`command -v unity-editor || command -v Unity` returned no executable). No Unity execution or pass counts are claimed:

- `DungeonSpatialContractTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- `FloorLayoutValidatorTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- Complete Unity EditMode suite: pending; pass/fail/skipped/inconclusive counts unavailable.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. This remains an inactive domain-foundation change, and the graph remains non-authoritative.
