# AGENTS.md

## Project

## Mandatory development rules

Before coding, read:
- docs/development/codex-development-rules.md

These rules control localization, data-driven tuning, feature flags, event configuration, scalable system boundaries, and test evidence requirements.

Dungeon Builder is a Unity mobile idle dungeon management game.

The player is a living dungeon core. The MVP goal is to prove that building, observing, and optimizing a dungeon ecosystem is fun.

## Current development target

Build the MVP vertical slice only.

Do not implement post-MVP systems unless explicitly requested.

## Authoritative docs

Read these before making changes:

1. Docs/01_MVP_Scope.md
2. Docs/03_Global_Invariants.md
3. Docs/04_Spec_Lock_Summary.md
4. Docs/Specs/
5. Planning/Milestones/

If a request conflicts with locked specs, stop and explain the conflict.

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

## Coding rules

1. Prefer small, testable changes.
2. Do not introduce systems outside the active milestone.
3. Keep gameplay formulas data-driven where practical.
4. Use stable string IDs for content.
5. Do not hard-code player-facing text.
6. Do not rename content IDs without adding a migration path.
7. Offline play is allowed, but online verification is required for restricted actions described in the specs.
8. Modifier stacking order must follow the global formula framework.

## Content pipeline

Source tables live in Content/SourceTables.

Runtime JSON exports live in Content/ExportedJson.

Schemas live in Content/Schemas.

Validators live in Tools/Validators.

## Testing expectations

When changing gameplay logic, add or update test cases under Tests.

At minimum, cover:

1. Mana formula behavior
2. Heat tier boundaries
3. Offline progression rules
4. Research pending completion
5. Save migration safety
6. Loot extraction edge cases

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
