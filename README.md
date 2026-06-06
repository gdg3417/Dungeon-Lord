# Dungeon-Lord

Dungeon-Lord is a Unity dungeon-management MVP project focused on deterministic, config-owned simulation systems and legacy-safe iteration.

## Current status

Main is merged through PR 73 / M7-C2. The repository now contains M6 heat closeout evidence, M7 active research lifecycle scaffolding, and a read-only research verification boundary scaffold. Backend scaffolding is ahead of the original low-hour timeline, but the playable player-facing MVP has not yet been validated.

## Operating rules

- Keep PRs small and evidence-backed.
- Keep resolver behavior deterministic.
- Keep gameplay tuning in config or typed config assets, not runtime logic.
- Keep player-facing text localization-owned.
- Preserve legacy-safe saves and additive compatibility patterns.

## Validation expectations

- Run Unity EditMode tests for code PRs.
- Run Bootstrap smoke tests for UI or diagnostics PRs.
- Attach validation evidence under `docs/testing/evidence`.
- Documentation-only PRs should run available text or formatting checks and confirm that no runtime, tuning, scene, prefab, asset, or `.meta` changes were introduced.

VS4 first-session MVP smoke documentation:

- [VS4 first-session MVP smoke test runbook](docs/testing/runbooks/vs4-first-session-mvp-smoke-test-runbook.md)
- [VS4 first-session MVP smoke test evidence template](docs/testing/evidence/vs/vs4-first-session-mvp-smoke-test-evidence-template.md)

## Next intended work

The next phase is MVP vertical-slice integration, not deeper scaffold expansion. Upcoming PRs should compose existing structure placement, run simulation, mana, loot extraction, active heat, and single-slot research outputs into the smallest coherent player-facing loop before adding broader systems.
