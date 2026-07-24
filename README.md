# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0C3 approval update is main through merged PR #173 at `1a45e1a1454fcd7455d34e633bd735fc924fce7e`. GD64 completed the inactive spatial contract and deterministic layout-validation alignment; GD65A completed the inactive serializable spatial content schema and bounded deterministic export validator/canonicalizer; PR #168 established the production-value approval gate; PR #169 approved the first identity group; PR #170 / GD65B0A preserved optional-branch allowance extensibility; PR #171 / GD65B0B approved catalog metadata and Floor 1 references; PR #172 / GD65B0C1 approved the initial room footprints and category capacities; and PR #173 / GD65B0C2 approved Floor 1 bounds and structural capacity-accounting policy while deferring exact capacity. No production spatial records, export registration, or runtime spatial-catalog consumer exist. The save schema remains version 6, the spatial domain remains non-authoritative, and ordered two-room models remain runtime and save authority.

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

## Active GD65B0 approval gate

The authoritative execution sequence is the [post-GD60 MVP execution plan](docs/planning/post-gd60-mvp-execution-plan.md). The spatial contract is [System Spec 38](Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md).

GD64 and GD65A are complete. PR #169 approved the first GD65B0 identity group; PR #170 corrected generic optional-branch field extensibility; PR #171 completed GD65B0B; PR #172 completed GD65B0C1; and PR #173 completed GD65B0C2 by approving Floor 1 legal bounds and structural capacity-accounting policy. GD65B0C3 approves Straight Stone Corridor width and initial MVP length limits plus the Entrance Hall and Completion Terminal footprints. It does **not** approve exact final capacity, author records, or activate spatial behavior. **GD65B remains blocked** by the incomplete [GD65B production spatial content approval record](docs/planning/gd65b-production-spatial-content-approval.md): final floor-space capacity; room reserved tile offsets, allowed orientations, maximum connection counts, and connection points; corridor trap/loot capacities, orientations, and compatible sockets; remaining fixed-structure reserved-tile, orientation, and connection data; socket compatibility; localization; export, manifest, registry, loading, validation, and canonical serialization ownership; workload limits; and production pipeline test ownership. The next dependency-correct packet is **GD65B0C4: approve exact Floor 1 final floor-space capacity**, reassessing the approved structural composition envelope without choosing a value here. GD65B0C3 authors no records and activates no spatial behavior. Save schema remains 6; the catalog remains inactive; ordered two-room models remain runtime/save authority; GD66 remains after GD65B; and Phase 2 exclusively owns migration and authority transition.
