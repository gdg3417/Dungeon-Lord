# Dungeon Spatial production authoring package

This normalized, version-controlled package is the canonical writable source for the approved Floor 1 spatial catalog records and English spatial display names. `authoring_manifest.json` owns package identity/version and the ordered table list. `authoring_schema.json` owns table structure and validation rules. Manifest-listed CSV files own records. This README is explanatory only.

The package is editor-authoring input, is outside Unity `Assets`, and is not included in player builds. It does not contain or generate runtime JSON. Rows are stored in schema-defined ordinal canonical order; source row order is not semantic authority.
