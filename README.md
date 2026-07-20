# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

Main is merged through PR #157 / GD59. GD60 is the active workstream: deterministic adventurer resolution across an ordered two-room route.

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

## Next likely gameplay sequence after GD60

1. Mana-backed building decisions
2. Real offline mana
3. Additional floor progression
4. Room-level loot depth
5. Monster-family and progression breadth
