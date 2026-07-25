# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0C4 approval update is main through merged PR #174 at `4bec48d4da068a6c3d738f35f9cddd5093a5899e`. GD64 completed the inactive spatial contract and deterministic layout-validation alignment; GD65A completed the inactive serializable spatial content schema and bounded deterministic export validator/canonicalizer; PRs #168–#173 established and progressively approved the production-value gate; and PR #174 / GD65B0C3 approved Straight Stone Corridor geometry and required fixed-structure footprints. No production spatial records, export registration, or runtime spatial-catalog consumer exist. The save schema remains version 6, the spatial domain remains non-authoritative, and ordered two-room models remain runtime and save authority.

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

GD64 and GD65A are complete, and PRs #169–#174 completed GD65B0 identity, metadata, room-profile, bounds-policy, corridor-geometry, and fixed-footprint approval groups. GD65B0C4 approves exactly 18 further register rows: Floor 1 capacity 60 and the remaining Design/Data room, corridor, socket, Entrance Hall, and Completion Terminal connectable-shape values, including topology-derived capped-dead-end corridor-loot eligibility. This leaves 19 rows unapproved: localization rows 53–58, production pipeline ownership rows 59–65, workload limits 66–70, and production test ownership row 72. **GD65B remains blocked.** This packet authors no records and activates no spatial behavior. Save schema remains 6; the catalog remains inactive; ordered two-room models remain runtime/save authority; GD66 remains after GD65B; and Phase 2 exclusively owns migration and authority transition. The likely next separately scoped owner group is localization rows 53–58; no final packet label or localization value is assigned here.
