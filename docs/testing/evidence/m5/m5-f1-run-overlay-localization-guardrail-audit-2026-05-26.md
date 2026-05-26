# M5-F1 Run Overlay Localization Guardrail Cleanup & Post-Heat-Cooling Audit (2026-05-26)

## Scope
- Reviewed run summary display path in `GameRoot.RefreshRunLine()` and helper builders for loot, survival, extraction, and heat cooling lines.
- Removed remaining player-facing English fallback strings from run summary runtime code path.
- Expanded null-Content fallback tests to ensure localization-key fallback behavior remains safe and deterministic.

## Status Summary

### Generated loot summary status
- **Status:** Ready.
- Loot summary display now follows key-fallback guardrail (`ui.run.loot_summary_format`) when Content is null/missing, rather than using English fallback text in runtime code.

### Survival summary status
- **Status:** Ready.
- Existing null-Content fallback coverage remains in place and still validates key fallback (`ui.run.survival_summary_format`).

### Extraction summary status
- **Status:** Ready.
- Existing null-Content fallback coverage remains in place and still validates key fallback (`ui.run.extraction_summary_format`).

### Heat cooling summary status
- **Status:** Ready.
- Existing null-Content fallback coverage remains in place and still validates key fallback (`ui.run.heat_cooling_summary_format`).

## Additional run overlay localization cleanup completed
- Run latest line now uses key fallback (`ui.run.latest_format`) rather than English fallback text.
- Run history line now uses key fallback (`ui.run.history_position_format`) rather than English fallback text.
- Run breakdown line now uses key fallback (`ui.run.breakdown_format`) rather than English fallback text.
- Run feedback line now uses key fallback (`ui.run.feedback_format`) rather than English fallback text.

## Current known non-blocking cleanup items
- `RefreshStructureRuntimeLines()` still uses direct English debug labels (`Heat`, `Mana`, `Tick`) for runtime diagnostics; this is outside current M5-F1 run summary display scope but should be tracked if strict localization is later required for all overlay lines.
- No gameplay-rule or tuning-constant changes were introduced in this cleanup slice.

## Recommended next implementation slice
- Proceed to **M5-G0: adventurer attraction signal scaffold from expected extracted value**, using the now-stable run-summary localization behavior and validated loot/survival/extraction/heat-cooling summary foundations as the integration baseline.
