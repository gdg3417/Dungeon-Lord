# AGENTS.md

## Project

Dungeon Builder is a Unity mobile idle dungeon management game.

The player is a living dungeon core. The MVP goal is to prove that building, observing, and optimizing a dungeon ecosystem is fun.

## Mandatory development rules

Before coding, read the repo-root rules file:

- `docs/development/codex-development-rules.md` from the repository root
- `../docs/development/codex-development-rules.md` when working from `dungeon-builder/`

These rules control localization, data-driven tuning, feature flags, event configuration, scalable system boundaries, MVP scope control, and test evidence requirements.

## Current development target

Build the MVP vertical slice only.

Do not implement post-MVP systems unless explicitly requested.

Sprint 2A implementation must not begin until Sprint 1 closeout evidence is complete and linked.

## Authoritative docs

Read these before making changes:

1. `docs/development/codex-development-rules.md`
2. `Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md`
3. `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md`
4. `Docs/What is the smallest version of Dungeon Builder that proves the fantasy is fun.md`
5. `Docs/37 - QA_Strategy_and_Test_Harness.md`
6. `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`
7. `docs/planning/build-promotion-policy.md`
8. `docs/planning/test-stage-matrix.md`
9. `docs/planning/actionable-backlog.md`
10. `docs/planning/backlog-to-sprint-traceability-matrix.md`
11. `docs/planning/sprint-2-ticket-backlog.md`
12. `docs/planning/issues/sprint-2/execution-order.md`
13. `docs/planning/issues/sprint-2/risks-and-blockers.md`

When working a specific sprint ticket, also read the relevant issue file under:

- `docs/planning/issues/sprint-2/`

If a request conflicts with locked specs, sprint planning, or Codex development rules, stop and explain the conflict before making changes.

## Engine

Unity.

## Core MVP constraints

MVP includes:

1. One main dungeon
2. Up to five floors
3. Undead and Goblinoid monster families
4. Mana generation and spending
5. Heat states: Peace, Notice, Concern
6. Adventurer classes: Warrior, Rogue, Mage, Cleric, Ranger
7. Loot tiers up to Steel
8. One research slot
9. Offline idle mana calculation
10. One sub dungeon type: Mana Farm

MVP excludes:

1. Prestige
2. Seasonal events
3. PvP
4. Leaderboards
5. Hero adventurers
6. Advanced diplomacy
7. More than one sub dungeon type
8. Expanded boss sets
9. Real monetization
10. Social or competitive systems
11. Player-facing live-ops systems unless explicitly approved in a later sprint

## Sprint gate rules

1. Sprint 1 testing must be completed before Sprint 2A implementation begins.
2. Sprint 2A work must follow `docs/planning/issues/sprint-2/execution-order.md`.
3. Sprint 2A starts with `S2-T00A-I01-dungeon-layout-and-placement-mvp-foundation.md`.
4. Sprint 2B work must not be mixed into Sprint 2A unless explicitly requested.
5. Build promotion must follow `docs/planning/build-promotion-policy.md`.
6. Unit, SIT, UAT, and build gate evidence must follow `docs/planning/test-stage-matrix.md`.
7. Sprint closeout must follow the relevant sprint closeout checklist.

## Coding rules

1. Prefer small, testable changes.
2. Do not introduce systems outside the active milestone.
3. Keep gameplay formulas data-driven.
4. Use stable string IDs for content.
5. Do not hardcode player-facing text.
6. Do not hardcode gameplay tuning values.
7. Do not rename content IDs without adding a migration path.
8. Offline play is allowed, but online verification is required for restricted actions described in the specs.
9. Modifier stacking order must follow Spec 30 and the global formula framework.
10. Keep simulation and domain logic separate from UI presentation.
11. Keep content loading separate from gameplay resolution.
12. Keep save migration logic separate from gameplay rules.
13. Prefer narrow interfaces and small services over large manager classes.
14. Do not use feature flags to hide broken architecture or bypass tests.
15. Do not create player-facing event, live-ops, leaderboard, event pass, or social systems during MVP implementation unless explicitly approved.

## Localization rules

1. No player-facing English strings may be hardcoded in gameplay, UI, tutorial, error, tooltip, item, monster, room, event, or notification code.
2. All player-facing text must use stable localization keys.
3. Localization keys must not be derived from display text.
4. Missing localization keys must follow Spec 27 fallback behavior.
5. Developer-only logs, test names, diagnostics, and code comments may use plain English, but they must not be surfaced to players.
6. New player-facing UI or state changes must add or reference localization keys.

## Data and tuning rules

1. Gameplay tuning values must come from content tables, constants tables, JSON exports, ScriptableObjects, or equivalent data assets.
2. Do not embed magic numbers in gameplay code for mana, heat, difficulty, monster strength, adventurer stats, loot odds, research timing, offline caps, event modifiers, or economy balance.
3. Technical constants are allowed only when they are not gameplay tuning values, are clearly named, and are documented.
4. Test fixtures may use literal values only when local to the test and clearly named.
5. New formulas must cite the table, config key, or data asset that owns each tunable value.
6. New tuning data must include validation where applicable.

## Content pipeline

Source tables live in:

- `Content/SourceTables`

Runtime JSON exports live in:

- `Content/ExportedJson`

Schemas live in:

- `Content/Schemas`

Validators live in:

- `Tools/Validators`

Content rules:

1. All content records must use stable string IDs.
2. Saves should store IDs and player progress, not copied numeric tuning snapshots, unless a spec explicitly requires snapshotting.
3. Content changes must preserve migration safety.
4. Content tables must be validated before build promotion.
5. Missing, duplicate, renamed, or removed IDs require a migration path or safe fallback.

## Feature flag rules

Feature flags are allowed for Dev, Test, and controlled release gating.

Server-driven flags are required for features affecting:

1. Economy
2. Competition
3. Purchases
4. Online verification
5. Events
6. Release availability

Local flags are allowed only in Dev builds for debugging or iteration.

Any flagged feature must define:

1. Flag ID
2. Default state by environment
3. Owner
4. Expiry or review condition
5. Affected systems
6. Test expectations

Save schema version and content version must remain separate from feature flag state.

## Event rules

Do not hardcode event start dates, end dates, modifiers, rewards, or targeted systems in gameplay code.

Events must be defined through data records with stable IDs.

Event activation must respect online verification and server time rules.

Offline event changes apply only after online verification.

During MVP, do not accidentally ship:

1. Player-facing live-ops
2. Seasonal systems
3. Leaderboards
4. Event passes
5. Social systems

Internal test override hooks are allowed only where planning docs explicitly allow them.

## Testing expectations

When changing gameplay logic, add or update tests under `Tests`.

At minimum, cover:

1. Mana formula behavior
2. Heat tier boundaries
3. Offline progression rules
4. Research pending completion
5. Save migration safety
6. Loot extraction edge cases
7. Localization key fallback behavior when relevant
8. Content validation when content or data references change
9. Feature flag enabled and disabled behavior when relevant
10. Deterministic replay for systems using seeded or ordered simulation

Testing stage rules:

1. Gameplay logic changes require Unit tests.
2. Cross-system flows require SIT coverage.
3. Player-facing flows require UAT evidence when applicable.
4. If Unit, SIT, or UAT is not applicable, explain why in the PR summary.
5. Evidence requirements are governed by `docs/planning/test-stage-matrix.md`.

## Pull request expectations

Implementation PRs must include:

1. Active ticket or issue ID
2. Source specs relied on
3. Files changed
4. Behavior implemented
5. Localization keys added or referenced
6. Tuning or config entries added or referenced
7. Feature flags added or referenced
8. Unit/SIT/UAT applicability
9. Tests run and results
10. Known limitations
11. Non-goals
12. Confirmation that MVP scope was not expanded

Documentation-only PRs may mark Unit, SIT, and UAT as Not Applicable if no executable code, game content, schemas, or build configuration changed. Markdown validation and human review evidence are still required.

## Codex pre-code checklist

Before coding, summarize:

1. Active ticket or issue ID
2. Source specs relied on
3. Tables, localization keys, or config assets affected
4. Whether player-facing text is touched
5. Whether gameplay tuning values are touched
6. Whether feature flags or version gates are touched
7. Unit/SIT/UAT expectations
8. Risks and non-goals

## Codex post-code checklist

After coding, summarize:

1. Files changed
2. Rules checked
3. Localization keys added or referenced
4. Tuning/config entries added or referenced
5. Feature flags added or referenced
6. Tests added or updated
7. Unit/SIT/UAT results or Not Applicable rationale
8. Known limitations
9. Follow-up work, if any

## Response style for Codex tasks

Before coding, summarize:

1. Files to change
2. Spec sections relied on
3. Risk level
4. Test plan

After coding, summarize:

1. Files changed
2. Behavior implemented
3. Tests added or skipped
4. Known limitations