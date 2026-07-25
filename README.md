# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

The starting baseline for the GD65B0C5 approval update is main through merged PR #175 at `2b8a4073075236bf6b144fb59c3299dba136da3e`. GD64 completed the inactive spatial contract and deterministic layout-validation alignment; GD65A completed the inactive serializable spatial content schema and bounded deterministic export validator/canonicalizer; PRs #168–#174 established and progressively approved the production-value gate; and PR #175 / GD65B0C4 approved Floor 1 capacity and the remaining MVP connectable-shape profiles. No production spatial records, export registration, production localization table, or runtime spatial-catalog consumer exist. The save schema remains version 6, the spatial domain remains non-authoritative, and ordered two-room models remain runtime and save authority.

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

GD64 and GD65A are complete, and PRs #169–#175 completed GD65B0 identity, metadata, room-profile, bounds-policy, geometry, capacity, and connectable-shape approval groups. GD65B0C5 approves exactly rows 53–58: six explicitly authored production display-name localization keys and six reviewed English display names. It authors no localization table or production content record, changes no code, and activates no runtime behavior. Thirteen rows remain unapproved: production pipeline ownership rows 59–65, workload limits 66–70, and production test ownership row 72. **GD65B remains blocked.** Save schema remains 6; the catalog remains inactive; ordered two-room models remain runtime/save authority; GD66 remains after GD65B; and Phase 2 exclusively owns migration and authority transition. The likely next owner group is production pipeline ownership rows 59–65; its exact values and final packet label remain unassigned.
