# AGENTS.md

## Repository-wide guardrails

Follow `docs/development/codex-development-rules.md`, the applicable approved specifications, `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md`, and current dated amendments. Historical sprint gates apply only to work that remains in their scope.

1. Preserve deterministic simulation, stable content IDs, and canonical ordering.
2. Keep state changes atomic with one writable authority per state domain; preserve versioned saves, explicit migrations, historical compatibility, and fail-closed recovery.
3. Keep validation side-effect-free, use stable reason codes, and test invalid states.
4. Keep gameplay tuning in content/config tables or typed assets; runtime consumes injected or loaded config. Inline fake values are test-only and clearly scoped.
5. Use stable localization keys for all player-facing text; follow Spec 27 fallback behavior and support future language packs without code changes. Developer-only diagnostics may use English as allowed by the canonical rules.
6. Preserve separation of simulation, content loading, UI, and save migration; keep performance within the applicable platform requirements.
7. Stay within approved active scope. All sub-dungeons, including Mana Farm, are excluded from the five-floor MVP and initial public release; do not implement or scaffold them. See `Docs/planning/decisions/mvp-mana-farm-post-launch-scope-amendment.md`.
8. Follow `docs/planning/test-stage-matrix.md` and `docs/planning/build-promotion-policy.md`; report Unit/SIT/UAT applicability and evidence. Player-facing UAT remains required where applicable.
9. For automated qualification, follow `Docs/testing/qualification-supervisor.md`, including exact input/evidence preservation and process ownership.
10. Follow `Docs/process/AI_Model_Selection_Policy.md`. Before implementation or correction, state the recommended model, reasoning level, task classification, reason, and availability fallback. Disclose substitutions and justify exceptions or recommendation changes under that policy.
11. For player-facing PRs, once implementation is stable and automated qualification passes, prepare the normal owner-UAT handoff when applicable: isolated scenarios, a qualified build or suitable Editor setup, exact launch steps, expected results, and save/reopen checks where relevant. Prefer approved tools and fixtures; do not make the owner edit JSON, saves, or configuration or create routine test conditions. Independent review decides whether UAT may begin. Follow `Docs/testing/owner-uat-preparation-and-artifact-lifecycle.md`.
12. Track every disposable artifact's exact path, owner, purpose, and retention status. Preserve owner saves, active processes, qualified builds, historical evidence, and unresolved failure-reproduction states. After successful qualification and evidence retention, clean up only verified, owned disposable artifacts no longer needed; unknown or uncertain cleanup requires owner approval. Follow `Docs/testing/owner-uat-preparation-and-artifact-lifecycle.md`.
