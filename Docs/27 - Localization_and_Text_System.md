# System Spec 27: Localization and Text System

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines the localization architecture, glossary governance, text key conventions, number formatting rules, and UI constraints needed to ship English and Japanese and expand to additional languages safely.

2\. MVP Language Strategy

2.1 English and Japanese readiness

MVP is architected and ready to ship with English and Japanese. A language toggle is available in English speaking regions. Japan release can be concurrent or shortly after the English release.

2.2 Expansion ready

The text system must support adding additional languages without refactoring core UI or content ids.

3\. Text Key Architecture

3.1 No hard coded player facing strings

All player facing strings use stable localization keys. Keys are not derived from display text.

3.2 Key conventions

Keys follow a stable namespace style, such as ui, system, tooltip, item, monster, and lore.

3.3 Content id separation

Content ids, such as monster ids and loot ids, are separate from localization keys, but can map to them in tables.

4\. Glossary Driven Terminology

4.1 Glossary authority

A glossary defines core terms that must remain consistent across UI, tooltips, and tutorials.

4.2 Ownership and workflow

The primary developer owns final glossary decisions. Helpers or paid translators can propose translations marked needs review.

4.3 Term translation policy

Terms should be fully localized when that matches industry standards in the target language. Mana should be localized if that is the standard practice in Japanese game localization.

5\. Tone and Content Rules

5.1 Translation friendly default

Default UI and system text uses translation friendly tone.

5.2 Humor placement

Humor and slang are reserved for collectible lore and optional flavor content.

6\. Number Formatting

6.1 Default compact notation

Default views use compact notation, such as 1.2K, with locale aware separators and abbreviations.

6.2 Detailed explicit numbers

Detailed views show explicit numbers.

6.3 Japanese formatting

Japanese uses Japanese friendly units and formatting rules, not forced English abbreviations.

7\. UI Layout Constraints

7.1 Text expansion support

UI must accommodate text expansion, including longer languages, to reduce rework later.

7.2 Line break and truncation rules

Critical gameplay values must not be hidden by truncation. Use wrapping and responsive layout where possible.

8\. Implementation Requirements

8.1 Placeholder and fallback behavior

If a localization key is missing in a language pack, fall back to English and log a missing key event for internal diagnostics.

8.2 Content pipeline integration

Localization keys are stored alongside content tables and exported with the content pipeline so content and text remain aligned.

9\. Approved Initial Production Spatial Display Names

9.1 Key authority

GD65B0C5 approves the exact key convention `spatial.<definition-kind>.<stable-name>.display_name` for the initial production spatial display names. Keys are explicitly authored lowercase-ASCII data, with period-separated namespace components, underscores in multiword stable names, and the exact final suffix `display_name`. They are never generated at runtime from definition IDs, English text, type or enum names, or file names. Definition IDs and localization keys remain separate authorities even where their components are intentionally parallel. Future languages reuse these stable keys without code changes, and canonical collections use ordinal key ordering.

9.2 Approved English entries

The English-only approved entries, in ordinal key order, are:

| Production localization key | Reviewed English entry |
|---|---|
| `spatial.corridor.straight_stone.display_name` | `Straight Stone Corridor` |
| `spatial.fixed.completion_terminal.display_name` | `Completion Terminal` |
| `spatial.fixed.entrance_hall.display_name` | `Entrance Hall` |
| `spatial.room.basic.display_name` | `Basic Room` |
| `spatial.room.large_chamber.display_name` | `Large Chamber` |
| `spatial.room.rectangle.display_name` | `Rectangle Room` |

This approval authors no Japanese or other-language entry. Japanese remains required for release readiness, and later languages must reuse the same keys. Descriptions, tooltips, tutorials, error text, flavor, lore, accessibility text, editor help, short names, plurals, and other text types require separate approval.

9.3 Approved production-table pipeline boundary

GD65B0C6 later approves `Assets/_Project/Data/Production/DungeonSpatial/string_table_en.json` as the future production English spatial table, separate from `Assets/_Project/Data/Bootstrap/string_table_en.json`. It is the only initially approved generated language file and initially the sole member of the production language-table collection. The production domain manifest registers the schema type once as `string_table` version 1 beside the catalog, not once per language file. The dedicated loader accepts a read-only collection, parses every table, ordinally validates its serialized `language`, rejects null/blank/duplicate language entries, and requires exactly one `language = "en"` pack as mandatory fallback. It validates catalog references against English and every additional pack without merging into Bootstrap or publishing a partial collection. Future packs can be appended through serialized assignment without changing `GameRoot`, `ContentService`, the loader signature, content IDs, or localization keys. Export orders entries ordinally, validates references before serialization, and reparses and revalidates them before recoverable transactional publication; runtime loading repeats validation before publication.

GD65B0C5 did not approve this pipeline ownership; GD65B0C6 does not physically author the six entries, any production file, Japanese path/table/translation, locale-selection behavior, language toggle, fallback implementation, diagnostic implementation, new schema version, or runtime behavior. Japanese has not yet been authored. Rows 53–58 of the approval register remain the exact terminology authority, while numeric workload limits and production pipeline test ownership remain separate gates.
