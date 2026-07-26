# GD65B production authoring source contract

| Field | Decision |
|---|---|
| Status | **APPROVED by owner — architecture contract only; not implemented** |
| Packet | GD65B2A — version-controlled production authoring source approval |
| Baseline | Main through merged PR #179 at `917b763dc0e5315fdd5d835da4b5f5de43f9ba59` |
| Approved future path | `ContentAuthoring/DungeonSpatial/` |
| Approved | 2026-07-25 |

## 1. Decision summary

The single logical writable authority for production Dungeon Spatial catalog records and production English spatial localization will be a normalized, version-controlled text package at `ContentAuthoring/DungeonSpatial/`. CSV owns flat and relational authoring records; JSON owns package metadata and the machine-readable schema. This location is outside Unity's `Assets` directory so source files are not treated as runtime assets or accidentally included in builds.

GD65B2A approves this contract only. The path, package, tables, schema, manifest, exporter, and generated records do not yet exist. GD65B1 is complete; GD65B overall remains incomplete, the spatial catalog remains inactive, save schema remains 6, and current runtime/save authority is unchanged.

## 2. Problem and rejected master-workbook authority

Spec 19 historically named one master spreadsheet as source of truth and described spreadsheet-to-JSON export. A workbook cannot safely provide normalized Git diffs, deterministic relational ownership, schema-controlled structure, or protection from hidden records and formulas. That production-authority model is superseded by this approved amendment.

A workbook can remain a generated editor or review surface, but it is rejected as an independent writable authority. The same applies to Google Sheets, live databases, CMSs, and web editors. Unity runtime, player builds, validation, and export require no online authoring service.

## 3. Approved canonical source location

The exact future canonical root is:

`ContentAuthoring/DungeonSpatial/`

Only the manifest, schema, and manifest-listed tables described below will be writable production authoring authority. No filesystem discovery decides authority. This packet does **not** create the directory or any file beneath it.

## 4. Authority matrix

| Artifact or system | Authority status | Boundary |
|---|---|---|
| `authoring_manifest.json` | Writable package-level authority | Sole writable owner of the selected authoring-package schema identity/version; also owns version expectations and the ordered table list |
| `authoring_schema.json` | Writable structural authority | Owns structural and validation rules applicable to the manifest-selected authoring schema; does not duplicate the selected identity/version |
| Manifest-listed CSV files under `tables/` | Writable record authority | Own normalized catalog records and production English spatial localization |
| `validation_limits.json` | Separate writable configuration authority | Remains at its approved Unity path; never generated, copied into, or duplicated by this package |
| Three approved runtime JSON files | Deterministic derived output | Reviewed and committed together; never manually edited |
| Excel/Google Sheets/CMS/web editor | Optional adapter only | May propose normalized package changes; cannot override authority |
| Balance simulators, localization glossary workbooks, planning/financial workbooks, lore/canon indexes | Non-authoritative support | May read or inform review; cannot own production records |
| C# record factories, duplicate ScriptableObjects/editor assets, Bootstrap data, test fixtures, runtime caches | Non-authoritative | Must not duplicate writable production content |
| Imported/downloaded unnormalized content | Non-authoritative | Gains authority only after normalization, validation, review, and commit |

Supporting tools may read the package but may not silently become another authority. Every serialized production field has exactly one writable owner, and derived fields are not written back as independent authority.

## 5. Future package structure and metadata ownership

The approved future structure is:

```text
ContentAuthoring/
  DungeonSpatial/
    README.md
    authoring_manifest.json
    authoring_schema.json
    tables/
      <normalized UTF-8 CSV tables>
```

`authoring_manifest.json` is the sole writable authority for the selected authoring-package schema identity and selected authoring-package schema version, explicit ordered table file list, production content version used to derive records, expected catalog schema identity/version, expected string-table schema identity/version, and initial required language identity. A compatibility or minimum supported authoring-tool version may be added only when explicitly documented; this packet invents no value. These writable values must not be duplicated in CSV or schema files.

The authoring manifest is distinct from generated runtime `content_manifest.json`; the latter is derived. `authoring_schema.json` is the sole machine-readable definition of table names and exact paths; fixed column order and names; data types; required/optional status; primary, owner-scoped compound, and foreign keys; enums; normalization and null/blank rules; canonical row-order keys; string/integer representations; uniqueness; child relationships; and validation rules applicable to the authoring schema identity/version selected by the manifest. It must not contain an independently writable duplicate of the selected package schema identity or version. Validation fails if the manifest selects an unsupported identity or version. GD65B2B will define exact tables and columns; this packet invents no authoring-package schema identity or numeric version.

## 6. Record families and relational decomposition

Flat scalar records belong in root CSV tables. Repeated arrays and nested records belong in child CSV tables. CSV cells must not contain serialized lists, JSON fragments, comma-separated IDs, coordinate arrays, or other mini-languages. Child rows use explicit parent stable IDs and stable child keys. Owner-scoped identifiers remain owner-scoped rather than becoming a false catalog-wide namespace. All foreign keys are validated before export.

The future table map must cover only: catalog/package metadata; floors; floor allowed-room and allowed-corridor references; rooms, orientations, reserved offsets, and connection points; corridors, orientations, and compatible-socket references; fixed structures, orientations, reserved offsets, and connection points; socket types and compatibility; and English spatial localization.

Every GD65A serialized production field must have exactly one authoring owner and no second writable representation. This approval adds no categories, suffixes, translations, definitions, geometry, capacities, coordinates, tuning, IDs, or English values.

## 7. Text normalization contract

Canonical source will use UTF-8 without BOM, LF line endings, exactly one trailing newline, comma-delimited CSV, and a mandatory header row. `authoring_schema.json` owns fixed column order. Duplicate headers, unknown columns, undocumented extra files, locale-dependent numbers, formulas, macros, timestamps, random values, runtime hashes, and machine-specific paths are forbidden.

Authority never comes from filesystem enumeration. Comparison and sorting are stable ordinal, never culture-sensitive. Tables are committed in canonical primary-key order, but valid source row order has no semantic authority: after canonicalization, any valid row permutation must generate byte-identical output. Whitespace, quoting, null, blank, Boolean, integer, enum, and line-ending behavior must be explicit and deterministic in the implemented schema/parser. This contract does not claim implementation of an external CSV standard.

## 8. Workbook and external-editor boundary

A workbook may be generated from canonical tables; provide filters, dropdowns, formulas, validation highlighting, visualization, joined review views, or balance analysis; and produce a proposed import. It may not be production truth, required for export, read by runtime or player-build validation, contain hidden production-only records, override committed tables, write generated JSON, or silently write the package.

Any future workbook import must: (1) read a known source-package version; (2) accept only explicitly supported editable cells; (3) write normalized text-table changes; (4) validate the complete package; (5) show a Git-reviewable diff; (6) fail without partial writes; and (7) never replace the package merely because the workbook is newer. Google Sheets, a CMS, or a web editor follows the same adapter boundary and must produce changes on a Git branch. No online service is required to build, validate, export, or run the game.

## 9. Export and validation flow

The future exporter must read the exact explicitly assigned manifest and only its ordered file list; validate against the exact schema; reject missing, duplicate, unknown, malformed, or unlisted required tables, unknown columns, unsupported versions, invalid keys/foreign keys, and duplicate authority; and build a detached in-memory `SpatialContentCatalog` plus localization table. It then invokes existing production validation/canonicalization with parsed `validation_limits.json`, generates the approved three-file transaction, reparses/revalidates it, and preserves deterministic bytes, recovery, loading, and pre-build gates.

```text
Version-controlled authoring package
    -> strict package validation
    -> detached production records
    -> existing spatial validation and canonicalization
    -> deterministic generated JSON
    -> reparse and revalidate
    -> recoverable transactional installation
    -> committed generated outputs
    -> pre-build validation
    -> defensive runtime loading
```

The exporter may not treat directory enumeration, workbook sheet order, source row order, dictionary iteration, locale, timestamps, random state, runtime hashes, or machine metadata as authority.

The unchanged generated outputs are:

```text
Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json
Assets/_Project/Data/Production/DungeonSpatial/string_table_en.json
Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json
```

They remain deterministic Unity `TextAsset` outputs, reviewed and committed together, and never manually edited. Separately authored `Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json` remains outside the generated transaction and authoring package.

## 10. Version and compatibility boundaries

Authoring-package schema version, catalog schema version, string-table schema version, production content version, application version, save schema version, and feature flags are independent concepts. This packet changes none of their existing values. Catalog schema remains 1, production content version remains `0.1.0`, and save schema remains 6. No authoring-tool compatibility value is invented. The catalog remains inactive.

## 11. Source-control and review requirements

Every production content change must result in a normalized, reviewable text diff on a Git branch and pass the full validation/export flow. Authoritative source and its generated output set are reviewed together when output exists. A workbook, cloud editor, or future CMS may improve editing UX but cannot bypass validation, canonicalization, transactional publication, pre-build validation, or defensive loading.

## 12. Failure behavior

Missing, duplicate, unknown, malformed, unlisted, unsupported, non-normalized, referentially invalid, or ambiguously owned package input fails closed before publication. Import and export publish no partial source or output. An adapter cannot resolve conflicts by age or silently overwrite source. Existing journaled recovery and mixed-set rejection remain required once export is implemented.

## 13. Implementation sequence

1. **GD65B2A (this packet):** approve and reconcile documentation only.
2. **GD65B2B — Implement normalized production spatial authoring package and approved Floor 1 records:** create schema and normalized package, author only approved Floor 1 records and English entries, and add source-package parsing/validation tests; do not activate runtime/save spatial authority.
3. Later GD65B packets implement deterministic export, recovery, loading/assignment, build gates, and complete evidence under existing approvals.
4. GD66 remains blocked until every GD65B stage, record, test, and item of evidence is complete.

## 14. Non-goals

This packet creates no package directory, CSV, JSON schema/manifest, production records, generated output, limits change, parser, exporter, recovery/loading/assignment/build hook, test/tool/workbook, `.gitignore` rule, online integration, activation, save/migration change, gameplay/UI/scene/prefab/asset/settings change, or new content/tuning value.

## 15. Owner approval record

The project owner approved this architecture in GD65B2A on 2026-07-25: normalized version-controlled tables and schemas at the exact future path are canonical; workbooks/cloud editors are adapters; runtime JSON is derived; Unity remains editor-service independent; and future integrations must produce validated Git-branch changes. This amendment sits outside and does not renumber or modify the existing 72-row production-value register.

## 16. Current implementation boundary

At baseline `917b763dc0e5315fdd5d835da4b5f5de43f9ba59`, GD65B1's limits asset, strict parser/conversion boundary, and workload/scalability tests exist. The authoring package described here does not. Production records, English source entries, generated catalog/table/manifest, exporter, recovery, loader, composition assignment, pre-build integration, and complete evidence remain incomplete. Current abstract placement, ordered two-room state, and room-slot assignments remain runtime/save authority; save schema is 6; the spatial catalog is inactive; and GD66 is blocked.

## 14. GD65B2B implementation status

GD65B2B implements the approved package at `ContentAuthoring/DungeonSpatial/`, including the manifest-selected `dungeon_spatial_authoring` version 1 boundary, machine-readable schema, 17 normalized CSV tables, approved Floor 1 records, six English entries, and a strict editor-only parser/projector. The architecture decisions in this contract remain unchanged. Generated runtime JSON, deterministic output serialization, recoverable publication, loading/composition assignment, pre-build gating, activation, and complete GD65B evidence remain absent. Save schema remains 6, existing runtime/save authority is unchanged, the catalog remains inactive, and GD66 remains blocked.
