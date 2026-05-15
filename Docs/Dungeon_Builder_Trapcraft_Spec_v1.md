Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Research branch rules, data model, MVP node set, and expansion policy.

# 1. Purpose

Trapcraft research unlocks trap types, trap behavior profiles, and limited synergy hooks. In MVP it supports simple trap variety that teaches risk and flow control without heavy simulation complexity.

# 2. Branch structure

- Trapcraft is organized into: Trap Unlocks, Trap Tuning, Trap Synergy.

- MVP should keep trap count small and emphasize clarity of cause and effect.

- Trapcraft must interface with heat through lethality and perceived unfairness messaging.

# 3. Research unlocking contract

- Each research node unlocks exactly one effect profile.

- Effects are applied through the global stacking order and may be clamped or soft capped.

- Research progress continues offline but completion is pending until online verification.

# 4. Runtime behavior

- On completion, add the effect profile to active modifiers and unlocked trap sets.

- Unlocked traps become available in the editor placement UI.

- If a trap id is missing on load, replace with safe fallback and flag for review.

# 5. Balance guardrails

- Traps should not create unavoidable wipes in MVP.

- Trap unlocks must not bypass dungeon difficulty estimate constraints.

- Synergy nodes should be small and additive, not combo explosions in MVP.

# 6. MVP node set

MVP includes a minimal, readable node set that demonstrates the branch fantasy and connects to the core loop.

## Trap Unlocks

- tr_000: Trapcraft Fundamentals

<!-- -->

- Unlock: Enables Trapcraft research category

- Prereqs: None

- Notes: Root node

<!-- -->

- tr_100: Spike Trap

<!-- -->

- Unlock: Unlock Spike trap placement

- Prereqs: tr_000

- Notes: Future hooks: poison spikes, armor pierce

<!-- -->

- tr_110: Snare Trap

<!-- -->

- Unlock: Unlock Snare trap placement

- Prereqs: tr_000

- Notes: Future hooks: sticky tar, frost snare

## Trap Tuning

- tr_200: Trigger Reliability I

<!-- -->

- Unlock: Reduce trap misfire chance and improve predictability

- Prereqs: tr_000

- Notes: Future hooks: reliability II, advanced triggers

## Trap Synergy

- tr_300: Trap Monster Coordination I

<!-- -->

- Unlock: Minor bonus when traps and monsters share a room tag

- Prereqs: tr_100

- Notes: Future hooks: room combo rules

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
