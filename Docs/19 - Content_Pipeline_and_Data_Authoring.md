# System Spec 19: Content Pipeline and Data Authoring

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines how game content is authored, validated, exported, versioned, and loaded at runtime. The goal is fast balance iteration, safe live updates, and strong forward compatibility.

2\. Engine and Runtime

2.1 Engine

The project uses Unity.

2.2 Data driven runtime

Core gameplay data is loaded from externalized tables so that balance and content can change without code changes when feasible.

3\. Authoring Source of Truth

3.1 Master spreadsheet

A single master spreadsheet is the source of truth, with one tab per content domain: constants, rooms, tiles, monsters, loot items, loot tables, research nodes, events, and localization keys.

3.2 Solo authoring

In MVP, only the primary developer edits content. The pipeline still supports future collaborator workflows through validation and id stability.

4\. Export Format

4.1 Export target

The spreadsheet is exported to JSON for runtime loading. Export can be manual or scripted as a build step.

4.2 Schema

All tables use stable string ids. Localization uses the same ids or dedicated localization keys that map to glossary terms.

4.3 Content version

Every export produces a content version string that is embedded in the build and stored in saves.

5\. Runtime Loading and Caching

5.1 Load order

Load constants first, then foundational types (tiles, room archetypes), then monsters and loot, then research and events.

5.2 Save resolution

Saves store ids and player progress, not numeric balance snapshots. On load, ids are resolved against the current content tables so balance updates apply to existing saves.

5.3 Missing id handling

If an id referenced by a save is missing in the current content build, the game must either map it through an explicit migration table, or replace it with a safe fallback that does not break the economy.

6\. Room and Tile Granularity

6.1 Tile grid model

Dungeon construction is a modular tile grid. Tiles can host traps, monsters, and room modifiers, within placement rules.

6.2 Room instances

A room is an instance defined by a set of tiles. Room instance data includes a per instance loot table, trap lethality score, and monster threat score.

6.3 Difficulty estimate

Room difficulty is computed from trap lethality plus monster threat. This estimate is used to seed expected adventurer level band and to constrain player loot table choices.

7\. Loot Table Authoring Rules

7.1 Per room instance loot table

Each room instance owns its own loot table configuration. Two rooms with similar monsters may still differ due to levels, modifiers, and player tuning.

7.2 Player editability

The content pipeline defines the available loot entries and constraints. The player edits per room loot tables in game within those constraints.

8\. Build Validation and Linting

8.1 Linter requirement

A data linter runs before every build and can fail the build if validation rules are violated.

8.2 Must never ship broken

The linter must detect and block: circular research prerequisites, negative upkeep values, and loot value ranges that break the economy.

8.3 Additional recommended checks

Recommended checks include missing ids, duplicate ids, unreachable research nodes, and invalid probability sums in loot tables.

9\. Live Updates and Offline Rules

9.1 Offline allowed

The game is playable offline.

9.2 Offline restrictions

While offline: events, leaderboards, and purchases are unavailable; research cannot be started or completed.

9.3 Content freshness

When online, the client checks for content updates and applies them. The player should not be able to run seasonal or leaderboard features on outdated content.

10\. Tuning Workflow

10.1 Iteration speed target

A few minutes per balance change is acceptable. The workflow prioritizes correctness and safety over instant hot reload.

10.2 Constants tab

All key coefficients are centralized in a constants tab so balance tuning does not require editing multiple tables.

11\. Forward Compatibility

11.1 Migration tables

Support explicit migration tables that map old ids to new ids when content is renamed or split.

11.2 Deprecation policy

Content removal should be rare. Prefer deprecating entries and mapping them to safe alternatives rather than deleting outright.
