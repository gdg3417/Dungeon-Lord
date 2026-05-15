Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Research branch rules, data model, MVP node set, and expansion policy.

# 1. Purpose

Arcanology research governs loot blueprints, limited enchant hooks, and reserve efficiency for loot crafting. In MVP it stays lightweight and focuses on making loot feel like a strategic dial rather than a faucet.

# 2. Branch structure

- Arcanology is organized into: Loot Access, Loot Control, Craft Reserve Efficiency.

- Loot blueprint depth is post-MVP. MVP nodes should teach the loop and UI.

- Arcanology should connect to heat and reputation through extracted tradeable value.

# 3. Research unlocking contract

- Each research node unlocks exactly one effect profile.

- Effects are applied through the global stacking order and may be clamped or soft capped.

- Research progress continues offline but completion is pending until online verification.

# 4. Runtime behavior

- On completion, add the effect profile to active modifiers and unlock lists for loot tags when applicable.

- Blueprint style unlocks add permitted_loot_tags or permitted_items to the player unlock sets.

- If a save loads with a missing effect profile, disable the modifier and flag for review.

# 5. Balance guardrails

- Arcanology must not allow high tier loot without discovery gates in post-MVP.

- Reserve reductions must not trivialize the reserve pressure loop.

- Control nodes should improve clarity and tuning options more than raw output.

# 6. MVP node set

MVP includes a minimal, readable node set that demonstrates the branch fantasy and connects to the core loop.

## Loot Access

- ar_000: Arcanology Fundamentals

<!-- -->

- Unlock: Enables Arcanology research category

- Prereqs: None

- Notes: Root node

<!-- -->

- ar_100: Steel Loot Authorization

<!-- -->

- Unlock: Allow Steel tier items to appear in eligible loot tables

- Prereqs: ar_000

- Notes: MVP tier cap, future hooks: Mithril Authorization

## Loot Control

- ar_200: Room Loot Calibration I

<!-- -->

- Unlock: Unlock basic per room loot tuning controls

- Prereqs: ar_000

- Notes: Future hooks: calibration II, rarity shaping

## Craft Reserve Efficiency

- ar_300: Craft Reserve Efficiency I

<!-- -->

- Unlock: Small reduction to loot craft reserve costs

- Prereqs: ar_000

- Notes: Future hooks: efficiency II, category specific reductions

# 7. Data model and tables

- ResearchNode table is the single authoritative list of research entries.

- Branch specific effect tables store tunable effects referenced by ResearchNode.effect_profile_id.

- Profiles are data driven so tuning does not require code edits.

- Saves store unlocked IDs and completed research IDs, not numeric outputs.

# 8. Linting requirements

- FAIL if a ResearchNode references a missing effect_profile_id.

- FAIL on circular prerequisites.

- FAIL on duplicate IDs.

- WARN if a node has no future hooks declared in notes.

- WARN if a node effect is strictly positive with no tradeoff tags.

# 9. How to add new content

1.  Add a new effect profile row in CoreEffects with a stable eff_id.

2.  Add a ResearchNodes row that references the effect profile.

3.  Declare prerequisites and ui_group.

4.  Run lint checks, fix FAIL items.

5.  Add one test scenario verifying the modifier applies in the correct formula layer.
