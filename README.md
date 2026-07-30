# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

PR #184 / GD65B3B is merged at `04515d5c7c5a35d869bb725cd76d2a7c317403ee`. Owner validation passed 169 of 169 EditMode tests with zero PlayMode failures; the exact final PlayMode count was not retained, and the two PR #184 evidence placeholders remain unverified. GD65B4 in the current PR implements strict production spatial loading, atomic inactive `ContentService` publication, and explicit `GameRoot`/`Bootstrap.unity` composition, pending review, Unity validation, and merge. The catalog remains inactive; existing abstract placements, ordered two-room state, and room-slot assignments remain runtime/save authority; save schema remains 6. Pre-build recovery and validation, supported build-entry integration, evidence closeout, and final GD65B closure remain next. GD66 remains blocked.

The current prototype supports a deterministic, player-completable first-session loop; configurable room/monster/trap/loot choices; an ordered, persistent two-room route; run analysis and route outcomes; research progress; heat, mana, and spoils feedback; and development-build validation. It does **not** yet activate physical tile footprints, corridors, a saved route graph, spatial capacity, multiple floors, or production dungeon-building UI. Floor 2 is only the first multi-floor foundation; the locked MVP remains one main dungeon with up to five floors.

Normal play still depends on the temporary Bootstrap overlay and simple MVP screen. These are validation surfaces, not the intended production editor, and will be replaced only after spatial contracts and editing behavior stabilize.

## Operating rules

- Keep resolver behavior deterministic and changes evidence-backed.
- Keep gameplay tuning in config and player-facing text in localization.
- Preserve additive, legacy-safe saves.

## Validation expectations

- Run Unity EditMode tests for code PRs.
- Run Bootstrap smoke tests for UI or diagnostics PRs.
- Attach validation evidence under `docs/testing/evidence`.
- Documentation-only PRs should run available text or formatting checks and confirm that no runtime, tuning, scene, prefab, asset, or `.meta` changes were introduced.

VS4 first-session MVP smoke documentation:

- [VS4 first-session MVP smoke test runbook](docs/testing/runbooks/vs4-first-session-mvp-smoke-test-runbook.md)
- [VS4 first-session MVP smoke test evidence template](docs/testing/evidence/vs/vs4-first-session-mvp-smoke-test-evidence-template.md)

## Active GD65B implementation gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD65B0C7 approves exactly rows 66–70 and 72 and closes the register at 72 of 72 `APPROVED`. The sole production workload-limit configuration authority, `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json`, will provide `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768` to export, pre-build, runtime-load validation, and canonicalization. These are configuration-owned workload safety bounds—not gameplay, floor-count, floor-space, schema, save, or permanent post-MVP ceilings—and missing or invalid configuration fails closed without a hardcoded or test-default fallback. The 4,096 tile bound applies to one materialized footprint or floor boundary, never cumulative dungeon capacity or Floor 1 capacity 60.

The **Production Spatial Content Pipeline EditMode Suite** covers workload/scalability, deterministic export, recoverable publication, and GD65B4 loading responsibilities; pre-build and complete evidence responsibilities remain pending. Its test-only 80-floor fixture remains a scalability contract, not approval for production floors or geometry. GD65B4 is pending review, Unity validation, and merge. Save schema remains 6, the catalog remains inactive, runtime/save authority is unchanged, and GD66 remains blocked until all required GD65B stages and evidence exist.


## Dungeon Spatial authoring source

[GD65B2A](docs/planning/gd65b-production-authoring-source-contract.md) historically approved `ContentAuthoring/DungeonSpatial/` as the single logical writable authority for normalized CSV records, package metadata, machine-readable schema, and production English spatial localization. Generated Unity JSON remains deterministic derived output; `validation_limits.json` remains separately authored configuration authority. GD65B2B implemented that source package and its strict editor-only projection boundary; deterministic generated-set construction followed in PR #182, recoverable publication in PR #183, and export invocation plus committed outputs in PR #184. Workbooks, Google Sheets, CMSs, and web editors may only propose normalized, validated, Git-reviewable changes and are never production authority or runtime/build dependencies.
