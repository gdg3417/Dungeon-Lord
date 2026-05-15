Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Research branch rules, data model, MVP node set, and expansion policy.

# 1. Purpose

Core Tech research improves the dungeon foundation: mana stability, capacity, and recovery tools. It enables growth without directly defining dungeon identity.

# 2. Branch structure

- Core Tech is organized into three clusters: Capacity, Efficiency, Stability.

- Nodes should be broadly useful and rarely invalidate earlier choices.

- Core Tech is the default early research path for new players.

# 3. Research unlocking contract

- Each research node unlocks exactly one effect profile.

- Effects are applied through the global stacking order and may be clamped or soft capped.

- Research progress continues offline but completion is pending until online verification.

# 4. Runtime behavior

- On completion, add the effect profile to the active research modifiers list.

- Modifiers apply in the Research layer of the global stacking order.

- If a save loads with a missing effect profile, disable the modifier and flag for review.

# 5. Balance guardrails

- Capacity increases must not reduce fill time below the minimum guardrail target for the current stage.

- Efficiency increases should be modest and often paired with increased reserve exposure or slower recovery.

- Stability nodes must not eliminate the need for player intervention during collapse states.

# 6. MVP node set

MVP includes a minimal, readable node set that demonstrates the branch fantasy and connects to the core loop.

## Capacity

- ct_000: Core Tech Fundamentals

<!-- -->

- Unlock: Enables Core Tech research category

- Prereqs: None

- Notes: Root node

<!-- -->

- ct_100: Mana Reservoir I

<!-- -->

- Unlock: Increase max mana capacity

- Prereqs: ct_000

- Notes: Future hooks: Reservoir II, Reservoir III

## Efficiency

- ct_200: Conversion Tuning I

<!-- -->

- Unlock: Increase mana generation efficiency slightly

- Prereqs: ct_000

- Notes: Future hooks: Tuning II, offline efficiency

## Stability

- ct_300: Reserve Buffer I

<!-- -->

- Unlock: Increase reserve tolerance before research pauses

- Prereqs: ct_000

- Notes: Future hooks: Buffer II, crisis automation

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
