# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The implementation baseline before this PR is main through merged PR #183 / GD65B3A at `5316f07a9c87cc87cb23db39287cf36dedaf7171`; PR #183 passed owner-run Unity validation. GD65B3B adds one shared production export invocation, the approved `Tools/Dungeon Lord/Content/Export Production Spatial Content` menu, the Unity command-line entry point, and exactly the committed `content_manifest.json`, `dungeon_spatial_content.json`, and `string_table_en.json` derived outputs. `ContentAuthoring/DungeonSpatial/` remains the writable content/localization authority, while `validation_limits.json` remains separately authored configuration. Production loading, `ContentService` publication, `GameRoot` assignment, pre-build enforcement, and runtime spatial consumption remain absent; spatial gameplay remains inactive. Save schema remains version 6, existing abstract placement selections, ordered two-room layout, and room-slot assignments remain runtime/save authority, and GD66 remains blocked. Production loading and composition assignment is the next packet.

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

The **Production Spatial Content Pipeline EditMode Suite** covers workload/scalability, deterministic export, and recoverable publication responsibilities implemented through GD65B3B; loading and pre-build responsibilities remain pending. Its test-only 80-floor fixture remains a scalability contract, not approval for production floors or geometry. This PR commits the first deterministic three-file generated set and adds invocation-level and committed-output tests, but exact-head owner Unity validation remains required. Save schema remains 6, the catalog remains inactive, runtime/save authority is unchanged, and GD66 remains blocked until all required GD65B stages and evidence exist.


## Approved future Dungeon Spatial authoring source

[GD65B2A](docs/planning/gd65b-production-authoring-source-contract.md) approves `ContentAuthoring/DungeonSpatial/` as the future single logical writable authority for normalized CSV records, package metadata, machine-readable schema, and production English spatial localization. Generated Unity JSON remains deterministic derived output; `validation_limits.json` remains separately authored configuration authority. GD65B2B implements that source package and its strict editor-only projection boundary but creates no generated Unity JSON. Workbooks, Google Sheets, CMSs, and web editors may only propose normalized, validated, Git-reviewable changes and are never production authority or runtime/build dependencies.

GD65B2B, PR #182 deterministic output construction, and PR #183 / GD65B3A recoverable publication are complete. GD65B3B adds the shared editor/menu and command-line invocation and commits the three generated outputs. Production loading and composition assignment is next; pre-build recovery/validation and evidence closeout follow. Runtime activation, migration, and GD66 remain incomplete and blocked as applicable.

## GD65B4 current implementation status (2026-07-28)

PR #184 is merged at `04515d5c7c5a35d869bb725cd76d2a7c317403ee`. Owner validation for PR #184 passed 169 of 169 EditMode tests with zero PlayMode failures; the exact final PlayMode count was not retained, and the two PR #184 evidence placeholders remain unverified. GD65B4 implements strict runtime production spatial loading, atomic inactive `ContentService` publication, and explicit `GameRoot`/`Bootstrap.unity` composition of the generated manifest, catalog, collection-based language tables, and separately authored workload limits. The catalog remains inactive: existing abstract placement, ordered two-room state, and room-slot assignments remain runtime and save authority; gameplay, simulation, player-facing output, and save data are unchanged, and save schema remains 6.

GD65B remains incomplete. Pre-build recovery and validation, integration with every supported build entry point, evidence closeout, and the final GD65B gate remain next. GD66 remains blocked.
