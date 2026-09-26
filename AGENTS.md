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
9. 9\. AI-assisted development for this repository must follow `Docs/process/AI\_Model\_Selection\_Policy.md`.
10. Implementation and correction prompts must identify the recommended model, reasoning level, task classification, and reason before execution.
11. Do not silently substitute a different model or reasoning level from the policy defaults. Any exception must be justified by the task classification or by a formally updated policy.
12. Do not change a model recommendation merely because it is questioned. A recommendation may change only when new task information, a changed optimization goal, repository evidence, task misclassification, or materially changed official model guidance justifies the change.

