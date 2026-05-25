# M5-A / M5-B Loot Foundation Audit (2026-05-25)

## 1) Scope reviewed

Audit target:
- PR #33 (loot data models + deterministic config validation)
- PR #34 (deterministic loot roll resolving)

Validation goals checked:
1. Loot data model stability for M5-C foundations.
2. Stable ID consistency.
3. Validator determinism and specificity.
4. Resolver determinism and isolation from nondeterministic/global sources.
5. Test coverage for exact behaviors.
6. Unity `.meta` coverage for new loot assets/scripts/tests.
7. Cross-system coupling risk to run/saves/heat/UI/inventory/crafting/research/adventurer behavior.
8. Small hardening fixes needed before M5-C.

## 2) Files reviewed

Implementation and model files:
- `Assets/_Project/Scripts/Util/Models.cs`
- `Assets/_Project/Scripts/Services/LootConfigValidator.cs`
- `Assets/_Project/Scripts/Services/LootRollResolver.cs`

Test files:
- `Assets/_Project/Tests/EditMode/LootConfigValidatorTests.cs`
- `Assets/_Project/Tests/EditMode/LootRollResolverTests.cs`

Planning/reference:
- `docs/planning/issues/sprint-2/S2-T06-I01-loot-table-resolution-and-validation.md`

Unity metadata files confirmed present for new loot scripts/tests:
- `Assets/_Project/Scripts/Services/LootConfigValidator.cs.meta`
- `Assets/_Project/Scripts/Services/LootRollResolver.cs.meta`
- `Assets/_Project/Tests/EditMode/LootConfigValidatorTests.cs.meta`
- `Assets/_Project/Tests/EditMode/LootRollResolverTests.cs.meta`

## 3) Pass/fail checklist

- [PASS] **Loot data models stable enough for M5-C foundation**
  - `LootConfig`, `LootItemRecord`, `LootTableRecord`, and `LootTablePoolEntry` are cleanly separated from runtime run/outcome integration and expose required IDs/values for deterministic resolution.
- [PASS] **Stable IDs used consistently**
  - IDs are string-based (`id`, `itemId`, `tierId`, `rarityId`, `categoryId`) and validator enforces required/duplicate/reference checks.
- [PASS] **Validator deterministic and specific**
  - Validator iterates in array order, emits path-based deterministic error keys, and includes deterministic error-order test coverage.
- [PASS] **Resolver deterministic and not dependent on `System.Random`, `UnityEngine.Random`, time, or mutable global state**
  - Resolver uses private seeded deterministic RNG (`DeterministicRng`) with explicit seed + table ID combination; no use of `System.Random`, `UnityEngine.Random`, `DateTime`, static mutable global runtime state, or external services.
- [PASS] **Tests lock exact behavior where appropriate**
  - Exact-sequence deterministic tests exist for known seeds; deterministic failure modes and mutation-safety tests exist; validator includes deterministic error-order assertions.
- [PASS] **Unity `.meta` files exist for new Unity assets**
  - All newly added loot script/test assets reviewed have paired `.meta` files.
- [PASS] **No accidental gameplay coupling to excluded systems**
  - Loot validator/resolver operate only on `LootConfig` inputs and value aggregation outputs; no direct dependency observed on run simulation orchestration, save pipeline, heat, UI, inventory, crafting, research state, or adventurer behavior.

## 4) Risks found

1. **Validator enum-like category/tier/rarity constraints are hardcoded in code** (low-medium risk).
   - This is deterministic and currently aligned with MVP constraints, but extending content sets in M5-C+ requires code edits rather than data-only changes.
2. **No overflow guard on summed integer totals** (low risk for current MVP values).
   - `totalGeneratedWorldValue`, `totalGeneratedReserveCost`, and `totalGeneratedTradeableWorldValue` are `int` accumulators; extreme data could overflow without explicit checked behavior.
3. **Missing explicit tests for some failure branches** (low risk).
   - Resolver paths `ConfigNull`, `TableIdMissing`, and `InvalidWeight`/`ItemNotFound` are implemented, but not all appear directly asserted in dedicated resolver tests.

## 5) Required fixes before M5-C

**Required before M5-C begins:**
- None.

**Recommended hardening (small, pre-M5-C if time permits):**
1. Add targeted resolver tests for `ConfigNull`, `TableIdMissing`, `InvalidWeight` (NaN/Infinity/<=0), and `ItemNotFound` error codes.
2. Add a numeric-boundary test documenting expected behavior for large aggregate totals.
3. Capture a follow-up backlog item to move tier/rarity/category admissible sets from hardcoded validator methods into config/schema-driven validation if/when content expansion beyond current MVP set is expected.

## 6) Recommendation

**Recommendation: M5-C can begin.**

Foundation status is sufficiently stable and deterministic for next-step integration work, provided M5-C continues to keep loot resolution isolated from downstream systems (run outcome wiring, extraction, heat/adventurer effects, inventory/crafting/UI), which are explicitly out of scope for this stage.
