# GD65A validation evidence

## Classification and tested head

- Packet: GD65A inactive schema and deterministic content-export validation.
- Baseline confirmed locally: `0d2d29460aa0c73d924de55e35b5b7fcd81b39cf` (PR #166 / GD64).
- Remote fetch: attempted, but the environment's HTTPS tunnel returned HTTP 403; the preloaded baseline exactly matched the required SHA.
- Implementation head statically tested: `111f3ffb4a46e2dfe56b9f6e075a66198804c46b`.
- Documentation-only commits after testing: the evidence-record update commit follows the tested implementation commit; no implementation file changed after the recorded checks.
- Manual validation classification: static contract and scope audit only; no gameplay or player-experience conclusion.

## Commands actually run

The following commands were run from `/workspace/Dungeon-Lord`. Both `git diff --check` commands passed with no output; status was clean; the name-only diff contained exactly the ten files listed by the implementation commit. Searches confirmed reason values 1–45, route-kind values 1–2, save schema 6, matching `.meta` files, no runtime catalog consumer, no prototype placement ID in the new domain, and no prohibited path change:

```text
git fetch origin
git rev-parse HEAD
git status --short
git diff --check
git diff --check origin/main...HEAD
git diff --name-only origin/main...HEAD
rg -n "FloorLayoutValidationReason|FloorRouteConnectionKind" Assets/_Project/Scripts/Gameplay/DungeonSpatial
rg -n "SchemaVersion|CurrentSchemaVersion|schema_version" Assets/_Project/Scripts Assets/_Project/Data/Bootstrap/schema_versions.json
rg -n "SpatialContentCatalog" Assets/_Project/Scripts --glob '!Gameplay/DungeonSpatial/SpatialContent*'
git diff --name-only origin/main...HEAD -- Assets/_Project/Data/Bootstrap Assets/_Project/Scripts/Save Assets/_Project/Scenes Assets/_Project/Prefabs ProjectSettings
rg -n "placement\.option\.room\.(basic|narrow_hall)" Assets/_Project/Scripts/Gameplay/DungeonSpatial
find Assets/_Project/Scripts/Gameplay/DungeonSpatial Assets/_Project/Tests/EditMode -maxdepth 1 -type f \( -name 'SpatialContent*' -o -name 'SpatialLayoutContracts.cs' \) -print
command -v unity-editor || command -v Unity || find /opt /usr/local -maxdepth 3 -iname Unity -type f
```

## Tests actually run

No Unity tests have been run in this environment. No pass totals are claimed.

## Tests not run

Unity Test Runner execution is pending because `command -v unity-editor`, `command -v Unity`, and the bounded `/opt`/`/usr/local` search found no executable Unity installation. The repository declares Unity `6000.3.2f1`; the host was Linux x86_64. A reviewer must use Unity `6000.3.2f1` and run EditMode filters:

1. `DungeonBuilder.M0.Tests.EditMode.SpatialContentValidationTests`
2. `DungeonBuilder.M0.Tests.EditMode.DungeonSpatialContractTests`
3. `DungeonBuilder.M0.Tests.EditMode.FloorLayoutValidatorTests`
4. The complete EditMode suite.

Record passed, failed, skipped, and inconclusive totals, including compilation or Safe Mode issues. PlayMode, standalone-build, save-lifecycle, localization rendering, and player-experience validation were not run and are outside this inactive packet.

## Scope and boundary confirmations

- No production spatial authored values, catalog JSON, localization entries, or content-version bump were added.
- Test fixture identifiers use the `test.gd65a.` namespace and live only in EditMode test code.
- The catalog is not registered with bootstrap/manifest/schema files and is not consumed by `ContentService.LoadAll`, `GameRoot`, simulation, saves, or runtime lookup services.
- Save schema remains version 6; no save or migration file is changed.
- No scene, prefab, build-setting, or generated file is changed.
- Existing ordered two-room runtime/save authority remains unchanged.
- Existing layout reason values 1–45 and route-kind values 1–2 remain unchanged.
- No production default workload limit exists; callers must supply limits.

## Known limitations

GD65A supplies representation and validation only. Production schema identity/version, content version, IDs, floor bounds, footprints, reserved tiles, capacities, connections, sockets, compatibility, corridor ranges, localization keys, costs, modifiers, and allowances remain unresolved for GD65B. The work does not activate spatial behavior and does not prove gameplay fun.
