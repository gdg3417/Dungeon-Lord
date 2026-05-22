# AGENTS.md

## Repository-wide AI/developer guardrails

1. No gameplay tuning values may be hardcoded in simulation/runtime code.
2. Numeric tuning must live in content/config tables or typed config assets.
3. Runtime systems must consume injected config, loaded config, or test config; do not embed gameplay tuning constants in runtime logic.
4. Structure IDs may be stable identifiers, but player-facing text must not be hardcoded.
5. Any player-facing English text must come from string table/localization references.
6. UI/debug/player messages should use localization keys or table references.
7. New systems must be designed so additional languages can be plugged in without code changes.
8. Tests may use inline fake config/localization keys only when clearly scoped to tests.
