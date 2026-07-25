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

12\. Production Dungeon Spatial pipeline ownership (approved, not implemented)

12.1 Paths and writable authority

GD65B0C6 approves `Assets/_Project/Data/Production/DungeonSpatial/` as the sole initial production Dungeon Spatial directory. The future generated, committed JSON `TextAsset` outputs are `dungeon_spatial_content.json`, `string_table_en.json`, and `content_manifest.json` in that directory. The separately authored configuration is `validation_limits.json` in the same directory and is read, never generated or replaced, by export. The catalog is one complete writable domain export; no per-definition or parallel writable source is approved. This does not weaken §3.1: implementation must reconcile export with the actual master authoring source rather than inventing a second workbook, JSON, C# source, or editor asset.

12.2 Format, manifest, and registration

Future generated outputs are deterministic pretty JSON, Unity-imported `TextAsset` files, UTF-8 without BOM, LF-only, and terminated by exactly one newline. They are reproducible committed artifacts, not manually edited output. The domain manifest has `schema = "content_manifest"`, `schemaVersion = 1`, and `contentVersion = "0.1.0"`; its ordinal `requiredSchemas` entries are `dungeon_spatial_content` v1 and `string_table` v1. That collection is the single production schema registry for these files. Bootstrap manifests/schema maps and `Assets/_Project/Data/Schemas/` remain non-authoritative, with no Bootstrap fallback.

12.3 Loading, assignment, and validation gates

`ContentService.LoadProductionSpatialContent(...)` owns dedicated loading from an explicitly injected manifest, catalog, read-only collection of production string-table `TextAsset`s, limits configuration, and diagnostics. It parses every table, ordinally validates each serialized `language`, rejects null/blank/duplicate entries, requires exactly one `language = "en"` mandatory fallback, validates catalog keys against English and every additional pack, and publishes no partial collection. Future packs append by serialized assignment without changing `GameRoot`, `ContentService`, the loader signature, spatial IDs, or localization keys. `GameRoot` owns four logical serialized inputs—manifest, catalog, language-table collection, and limits—and passes them to that loader; editor fallback assignment, runtime discovery, `Resources`, `StreamingAssets`, path guessing, and Bootstrap lookup are prohibited. One shared validation service gates export before and after serialization, every player build without regeneration, and defensive runtime loading.

12.4 Canonical recoverable transactional publication boundary

The future editor command is `Tools/Dungeon Lord/Content/Export Production Spatial Content`, with a Unity command-line-callable editor entry point. It canonicalizes via `SpatialContentCanonicalizer.TryCanonicalize`, orders stable definition IDs and approved nested collections canonically, and orders localization keys and manifest schema IDs ordinally. It reparses and revalidates generated bytes, stages all three generated outputs on the target filesystem, preserves complete backups, and durably writes and flushes a journal identifying every target, staged file, backup, expected content version, and phase before installing any target. It refreshes the asset database only after installation, validates the installed matching set, marks completion, and only then removes recovery material. Pre-install failures leave targets unchanged; after installation begins, journaled recovery deterministically restores the complete old set or completes the fully validated new set. Before later export or build, an unfinished journal blocks mixed-set use until recovery and revalidation succeed, failing closed if neither complete set is recoverable. No single OS operation is claimed to replace all files. Generated catalog, language-table outputs, and manifest are reviewed and committed together; a mixed working tree is not a published release. Identical authoritative input and approved limits must produce byte-identical output, without culture, timestamp, random, discovery, dictionary, filesystem, hash, or source-row ordering authority.

12.5 Current implementation boundary

This is documentation-only ownership approval: no exporter, files, records, loader, build hook, scene assignment, or runtime behavior exists yet. Loading a future validated catalog will not activate gameplay, UI, saves, placement, capacity, route graphs, construction, or simulation. Save schema remains 6 and existing ordered two-room state remains authority. GD65B0C7 subsequently approves rows 66–70 and 72 without implementing them; GD65B0 is complete and GD65B implementation is unblocked but absent.

12.6 GD65B0C7 approved workload and test gate

GD65B0C7 completes the 72-row GD65B0 approval register without implementing the production pipeline. The single future production authority is the separately authored Unity-imported `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json` `TextAsset`, with `MaximumTopLevelRecords = 128`, `MaximumNestedRecords = 512`, `MaximumMaterializedTiles = 4096`, `MaximumIssues = 256`, and `MaximumStringCharacters = 32768`. Export, pre-build, runtime loading, validation, and canonicalization must consume the same explicitly supplied parsed configuration. No compiled/build-tool/static/constructor/test/Bootstrap fallback, second writable asset, schema/catalog/save copy, or generated replacement is permitted. Missing, malformed, incomplete, nonpositive, overflowing, ambiguous, stage-inconsistent, or exceeded configuration fails closed and publishes no partial catalog or language collection.

The bounds are validation safety envelopes, not schema, save, gameplay, progression, floor-count, floor-space, content-capacity, remote-payload, or permanent ceilings. Future increases normally update configuration, rerun evidence, and ship the updated imported content asset; they do not by themselves require code/schema/save migration, new stable IDs, canonical ordering, or validation reasons. Under the current Unity `TextAsset` pipeline a deployed update requires a content/application release; remote or independently downloadable limits are not approved.

The future **Production Spatial Content Pipeline EditMode Suite** owns focused workload tests plus deterministic export, recoverable journal/transaction, production loading, every-entry-point pre-build, and scalability stages. Required responsibilities remain identifiable as `ProductionSpatialContentWorkloadLimitTests`, `ProductionSpatialContentExportTests`, `ProductionSpatialContentRecoveryTests`, `ProductionSpatialContentLoadingTests`, `ProductionSpatialContentBuildGateTests`, and `ProductionSpatialContentScalabilityTests`. Export evidence must include repeated byte hashes and canonical diff/no-diff; recovery evidence must cover deterministic interruptions throughout journal/staging/backup/install/validation/cleanup. QA owns matrix, evidence, verdict, and release gate; Engineering owns tests/seams/failure injection; Data owns production fixtures and canonical expectations; the primary developer runs Unity validation and supplies evidence at `docs/testing/evidence/gd65b/`.

The gate is build-blocking: missing tests/evidence, changed limits, fallback authority, nondeterministic or invalid generated bytes, unresolved localization, mismatched manifest/schema/content versions, mixed sets, ignored journals, lossy recovery, bypassable pre-build checks, partial publication, Bootstrap fallback, generated limits, or a scalability fixture requiring code/schema changes blocks merge. Tests and evidence do not exist yet. No Unity validation is claimed by this documentation approval; GD65B0 is complete and GD65B implementation is unblocked but not complete.
