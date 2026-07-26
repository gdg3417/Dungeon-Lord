# Dungeon Spatial production authoring package

This normalized, version-controlled package is the canonical writable source for the approved Floor 1 spatial catalog records and English spatial display names. `authoring_manifest.json` owns package identity/version and the ordered table list. `authoring_schema.json` owns table structure and validation rules. Manifest-listed CSV files own records. This README is explanatory only.

The package is editor-authoring input, is outside Unity `Assets`, and is not included in player builds. It does not contain or generate runtime JSON. Rows are stored in schema-defined ordinal canonical order; source row order is not semantic authority.

## Version 1 format contracts

The schema selects stable, ordinal implementation contracts: `lowercase_dot_identifier_v1`, `lowercase_owner_identifier_v1`, `display_name_localization_key_v1`, `nonblank_source_text_v1`, `invariant_int32_v1`, and `utf8_lf_single_newline_v1`. These identifiers are compatibility tokens, not editable prose.
