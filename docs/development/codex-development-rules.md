# Codex Development Rules

## Purpose
Define mandatory implementation rules for Codex and human contributors so future development stays localization-ready, data-driven, scalable, testable, and aligned to locked specs.

These rules are anchored to locked specifications, including Spec 19 for content pipeline and data authoring, Spec 27 for localization and fallback behavior, Spec 30 for formula stacking order, Spec 32 for event framework and overrides, and Spec 33 for build, release, and environment strategy.

## Rule 1: No hardcoded player-facing text
Requirements:
- No player-facing English strings may be hardcoded in gameplay, UI, tutorial, error, tooltip, item, monster, room, event, or notification code.
- All player-facing text must use stable localization keys.
- Localization keys must not be derived from display text.
- Missing localization keys must follow the fallback behavior defined by Spec 27.
- Logs, test names, developer-only diagnostics, and internal comments may use plain English, but should not be shown to players.

Required evidence:
- New UI or player-facing state changes include localization key additions or references.
- Tests or validation checks cover missing-key behavior where relevant.

## Rule 2: No hardcoded gameplay tuning values
Requirements:
- Gameplay tuning values must be loaded from content tables, constants tables, JSON exports, ScriptableObjects, or equivalent data assets.
- Do not embed magic numbers in gameplay code for mana, heat, difficulty, monster strength, adventurer stats, loot odds, research timing, offline caps, event modifiers, or economy balance.
- Technical constants are allowed only when they are not gameplay tuning values, are named clearly, and are documented.
- Test fixtures may use literal values only when the values are local to the test and clearly named.

Required evidence:
- New gameplay formulas cite the table, config key, or data asset that owns each tunable value.
- New tuning data includes validation where applicable.

## Rule 3: Formula and modifier order must use the global framework
Requirements:
- All formulas must follow Spec 30 stacking order:
  1. Base value computation.
  2. Heat layer.
  3. Research layer.
  4. Event or season layer.
  5. Clamps and soft caps.
  6. Final rounding and display formatting.
- No per-system formula exceptions are allowed in MVP unless explicitly approved in a locked spec update.
- Formula output should expose enough debug/test detail to verify layer order.

Required evidence:
- Unit tests verify formula order for new formula logic.
- Any formula change documents source tables and layers used.

## Rule 4: Stable IDs and data-driven content
Requirements:
- All content records must use stable string IDs.
- Save data stores IDs and player progress, not copied numeric tuning snapshots, unless a spec explicitly requires snapshotting.
- Renaming, splitting, or deleting content IDs requires a migration or fallback path.
- Content tables must be validated before build promotion.

Required evidence:
- New content includes schema/validation coverage.
- Removed or renamed IDs include migration mapping or explicit safe fallback.

## Rule 5: Feature flags and version gates
Requirements:
- Feature flags are allowed for Dev, Test, and controlled release gating.
- Server-driven flags are required for features affecting economy, competition, purchases, online verification, events, or release availability.
- Local flags are allowed only in Dev builds for debugging or iteration.
- Feature flags must not be used to hide broken architecture or permanently bypass tests.
- Any flagged feature must define:
  - flag ID
  - default state by environment
  - owner
  - expiry or review condition
  - affected systems
  - test expectations
- Save schema version and content version must remain separate from feature flag state.

Required evidence:
- New flags are registered in a feature flag registry or equivalent config table.
- Tests cover enabled and disabled behavior where relevant.

## Rule 6: Event configuration must be data-driven
Requirements:
- Do not hardcode event start/end dates, event modifiers, rewards, or targeted systems in gameplay code.
- Events must be defined through data records with stable IDs.
- Event activation must respect online verification and server time rules.
- Offline event changes apply only after online verification.
- MVP implementation must not accidentally ship live-ops, seasonal, leaderboard, event pass, or social features.

Required evidence:
- Event-like behavior must cite Spec 32 and the relevant config table.
- Data validation must reject invalid event windows, missing IDs, unsupported modifiers, and unsafe reward definitions.

## Rule 7: Scalable system boundaries
Requirements:
- Keep simulation/domain logic separate from UI presentation.
- Keep content loading separate from gameplay resolution.
- Keep save migration logic separate from gameplay rules.
- Use narrow interfaces for systems that other systems consume, such as mana, heat, encounter events, loot, research, verification, localization, and content lookup.
- Avoid direct cross-system writes where an event or service boundary should be used.
- Prefer small, testable services over large manager classes.

Required evidence:
- New Sprint 2A work defines clear input/output contracts.
- Integration points are documented in issue summaries or implementation notes.

## Rule 8: Test and governance requirements
Requirements:
- Any gameplay logic change must include or update Unit tests.
- Any cross-system flow must include SIT coverage.
- Any player-facing flow must include UAT evidence when applicable.
- Build promotion must follow docs/planning/build-promotion-policy.md.
- Sprint closeout must follow the relevant sprint closeout checklist.

Required evidence:
- PR summaries must list Unit/SIT/UAT applicability.
- If a stage is Not Applicable, the PR must explain why.

## Rule 9: MVP scope control
Requirements:
- Do not implement post-MVP systems unless explicitly requested.
- Prestige, live-ops, broad seasonal content, PvP, leaderboards, hero adventurers, advanced diplomacy, real monetization, and social systems remain out of MVP implementation scope.
- Internal test hooks may exist only where planning docs allow them and must not become player-facing features.

Required evidence:
- PR summaries must state whether the change affects MVP scope or deferred scope.

## Codex pre-code checklist
Before coding, Codex must summarize:
1. Active ticket or issue ID.
2. Source specs relied on.
3. Tables, localization keys, or config assets affected.
4. Whether player-facing text is touched.
5. Whether gameplay tuning values are touched.
6. Whether feature flags or version gates are touched.
7. Unit/SIT/UAT expectations.
8. Risks and non-goals.

## Codex post-code checklist
After coding, Codex must summarize:
1. Files changed.
2. Rules checked.
3. Localization keys added or referenced.
4. Tuning/config entries added or referenced.
5. Feature flags added or referenced.
6. Tests added or updated.
7. Unit/SIT/UAT results or Not Applicable rationale.
8. Known limitations.
9. Follow-up work, if any.
