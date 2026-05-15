Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Research branch rules, data model, MVP node set, and expansion policy.

# 1. Purpose

Diplomacy research improves recovery and reputation stability through passive tools. In MVP it focuses on heat decay assistance and basic reputation smoothing without introducing active negotiation systems.

# 2. Branch structure

- Diplomacy is organized into: Heat Recovery, Reputation Stability, Contract Foundations.

- MVP keeps contracts as placeholder hooks and avoids active diplomacy actions.

- Diplomacy is a pressure management branch, not a profit branch.

# 3. Research unlocking contract

- Each research node unlocks exactly one effect profile.

- Effects are applied through the global stacking order and may be clamped or soft capped.

- Research progress continues offline but completion is pending until online verification.

# 4. Runtime behavior

- On completion, add the effect profile to active modifiers.

- Effects apply to heat decay, rebound bounds within tier, or reputation volatility values.

- If online verification is unavailable, progress may continue but completion remains pending.

# 5. Balance guardrails

- Heat recovery must not negate consequences of reckless play.

- Stability must reduce volatility, not remove it.

- Diplomacy must not create a free low risk high reward strategy.

# 6. MVP node set

MVP includes a minimal, readable node set that demonstrates the branch fantasy and connects to the core loop.

## Heat Recovery

- dp_000: Diplomacy Fundamentals

<!-- -->

- Unlock: Enables Diplomacy research category

- Prereqs: None

- Notes: Root node

<!-- -->

- dp_100: Passive Decay I

<!-- -->

- Unlock: Increase passive heat decay slightly

- Prereqs: dp_000

- Notes: Future hooks: Passive Decay II, III

## Reputation Stability

- dp_200: Guild Rapport I

<!-- -->

- Unlock: Reduce reputation volatility from small events

- Prereqs: dp_000

- Notes: Future hooks: rapport II, contract reputation boosts

## Contract Foundations

- dp_300: Contract Ledger

<!-- -->

- Unlock: Enable contract UI hooks with no active contracts by default

- Prereqs: dp_000

- Notes: Future hooks: merchant contracts, civic contracts

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
