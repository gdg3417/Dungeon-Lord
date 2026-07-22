# GD64 validation evidence

## Scope

GD64 aligns the inactive spatial contracts and deterministic validator with the approved GD63 direction. It does not activate spatial runtime authority, alter saves or migration, author gameplay content, or add player-facing behavior.

## Static validation completed on 2026-07-22

- `git status --short`: completed; only the focused GD64 domain, EditMode test, status/roadmap, and evidence files were present.
- `git diff --check`: passed with no output.
- `git diff --name-only origin/main...HEAD`: run against the verified required baseline; the pre-commit result was empty as expected, and the working-tree comparison listed only the focused GD64 files. The command is repeated after commit.
- `git grep -n "FloorSpaceCost" -- Assets/_Project || true`: passed with no matches.
- `git grep -n 'LatestSchemaVersion = 6' -- Assets/_Project/Scripts/Save/SaveMigration.cs`: passed; schema version remains 6.
- SaveData spatial-field inspection: passed with no spatial, tile, floor-route, or graph field.
- DungeonSpatial reference boundary check: passed with no reference outside the inactive domain and its two EditMode fixtures.
- Reason-code inspection: passed; values 1–39 are unchanged and values 40–45 are appended in the approved order.
- Changed-path boundary check: passed with no nested `dungeon-builder/`, generated Unity, build, log, result-export, Library, or local-save path.

## Unity validation pending

Unity is not installed in this execution environment (`command -v unity-editor || command -v Unity` returned no executable). No Unity pass counts are claimed. These checks remain pending in a Unity-capable environment:

- `DungeonSpatialContractTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- `FloorLayoutValidatorTests`: pending; pass/fail/skipped/inconclusive counts unavailable.
- Complete Unity EditMode suite: pending; pass/fail/skipped/inconclusive counts unavailable.

## Manual validation classification

No gameplay, save-lifecycle, standalone-build, player-facing, or fun validation is required or claimed. This is an inactive domain-foundation change.
