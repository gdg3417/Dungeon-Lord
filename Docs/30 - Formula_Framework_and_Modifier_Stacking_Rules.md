**System Spec 30: Formula Framework and Modifier Stacking Rules**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Economy formulas, modifiers, stacking order, caps, advanced view |
| Primary goal | Consistency and predictability across all systems |

# 1. Purpose

Define the standard format for modifiers and the stacking order used by all formulas. This spec ensures that balance changes are predictable, advanced view explanations remain accurate, and event rules can be applied safely.

# 2. Modifier sources

- Research

- Heat state

- Dungeon identity

- Room tags

- Monster family traits

- Seasonal rules

- Contracts

- Sub dungeon modifiers

# 3. Standard modifier types

- Additive flat: adds or subtracts a fixed value

- Additive percent: adds percent in a bucket that is summed, for example plus 10 percent plus 20 percent equals plus 30 percent

- Multiplicative percent: multiplies by a factor, for example times 1.10

- Clamp: min and max bounds applied after stacking

- Soft cap: diminishing returns curve applied after stacking but before rounding

# 4. Stacking philosophy

All systems use a layered stacking model with the same bucket order.

# 5. Bucket order

1.  Base value computation (including per entity base stats and level scaling inputs)

2.  Heat layer (state effects and heat linked multipliers)

3.  Research layer (unlocks and numeric modifiers)

4.  Event or season layer (world event modifiers, season dungeon overrides where applicable)

5.  Clamps and soft caps

6.  Final rounding and display formatting

# 6. Consistency rule

- No per system exceptions to stacking order in MVP.

- If a future exception is required, it must be declared explicitly in that system spec and reflected in advanced view.

# 7. Soft caps and diminishing returns

## 7.1 Where soft caps apply

- Mana per hour

- Loot odds

- Heat reduction effects

## 7.2 Layout stacking diminishing returns

- Diminishing returns are based on threat concentration within a room.

- Threat concentration is computed from monster threat plus trap lethality density.

- The curve is smooth, meaning output still increases but at a slower rate.

Upkeep curves should scale reasonably with progression and are not intended to hard cap play.

# 8. Advanced view requirements

- Show the final formula with substituted values.

- Show modifier breakdown by source and by layer.

- Do not show a delta explanation since last run in MVP.

# 9. Telemetry hooks

- formula_evaluated (formula_id, result_value, layer_summary_hash)

- advanced_view_opened (panel_id, formula_count)

# 10. MVP constraints

- One global stacking order across all systems.

- Layer reporting must match formula evaluation.

- Soft caps are used instead of hard caps where possible.

# 11. Open questions

None.
