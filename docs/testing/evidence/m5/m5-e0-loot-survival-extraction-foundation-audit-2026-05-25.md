# M5-E0 Loot/Survival/Extraction Foundation Audit (2026-05-25)

## Scope
Reviewed merged foundations from PR #37, #38, and #39 across:
- Run outcome summary models (`RunLootSummary`, `RunSurvivalSummary`, `RunLootExtractionSummary`)
- Services (`LootRollResolver`, `LootExtractionResolver`, `RunSimulationService` integration)
- Config (`loot_config.json`, `run_simulation_config.json`, `string_table_en.json`)
- Tests (`LootConfigValidatorTests`, `LootRollResolverTests`, `LootExtractionResolverTests`, `RunSimulationTests`)

## Checklist assessment

1. **No hardcoded tunable numbers in runtime logic** — **PASS (with one scoped watch item).**
   - Core run/loot/extraction tunables are loaded from config assets (`RunSimulationConfig`, `LootConfig`) and consumed via injected data.
   - Extraction uses a hardcoded stable policy ID (`loot_extraction.round_floor`) and survival rule source ID (`run.survival.rule.v1`), which behave as identifiers rather than balance values.
   - **Watch item:** `BuildSurvivalSummary` currently hardcodes `RuleSourceId = "run.survival.rule.v1"`; extraction source ID is config-driven. Keep this asymmetry visible for future data-driven rule versioning.

2. **No new hardcoded player-facing English in runtime code** — **PASS.**
   - Runtime simulation code emits localization keys (`run.reason.*`, `run.feedback.*`), not display strings.

3. **Player-facing display uses localization keys** — **PASS.**
   - Run reason keys and run feedback tag keys are present in string table entries.

4. **Stable IDs are used consistently** — **PASS.**
   - Loot item/table IDs and rule/policy IDs are stable string IDs through models, config, and resolver/service wiring.

5. **Error codes are named enum values** — **PASS.**
   - Survival, extraction, and loot resolver failure modes map to explicit enums with deterministic integer storage in summaries.

6. **Summary data is deterministic** — **PASS.**
   - Loot and extraction use deterministic seeded resolution.
   - Run simulation tests assert deterministic outcomes for identical input.

7. **Save/load compatibility is preserved** — **PASS (for additive schema behavior).**
   - Summary fields are additive to `RunOutcomeRecord` and are nullable reference-type fields, preserving compatibility for older saved outcomes that may omit them.

8. **Legacy null summaries are safe** — **PASS.**
   - Extraction resolver handles missing loot/survival summaries via deterministic failure summaries rather than exceptions.

9. **RunSimulationService remains thin enough** — **PASS.**
   - Service orchestrates config-based chance math and composes specialized resolvers.
   - No cross-domain side effects or persistence/UI logic were introduced here.

10. **LootExtractionResolver is pure and independently testable** — **PASS.**
    - Static resolver takes explicit inputs, returns value object summary, and uses no time/global/random singleton dependencies.

11. **No early additions of heat/attraction/inventory/crafting/research/economy/merchant/combat/pathing/production UI behavior** — **PASS.**
    - Reviewed classes/tests/config are constrained to deterministic run, loot, survival, and extraction summaries only.

12. **M5-F can safely consume extracted tradeable world value later** — **PASS (foundation ready).**
    - `TotalExtractedTradeableWorldValue` is emitted deterministically with guarded failure codes for lookup/overflow paths.

## Findings

### Required fixes before downstream consumption
- **None.**

### Recommended follow-ups (non-blocking)
1. Consider moving survival `RuleSourceId` to config (similar to extraction `LootExtractionRuleSourceId`) to keep rule provenance fully data-driven.
2. Add/retain an explicit regression test that deserializes legacy run outcomes with null summaries to lock compatibility behavior.
3. If additional extraction rounding policies are planned in M5-F+, promote policy lookup from single known ID branch to a config-backed policy registry.

## Conclusion
**Recommendation: Proceed.**
The M5-E0 foundation is deterministic, scoped correctly, localization-safe, and sufficiently isolated for downstream systems to consume extraction summaries in M5-F without introducing premature behavior coupling.
