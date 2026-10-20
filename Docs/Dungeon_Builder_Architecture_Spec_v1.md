Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Research branch rules, data model, MVP node set, and expansion policy.

# 1. Purpose

Architecture research unlocks structural capacity, room features, and limited flow control. In MVP it primarily supports unlocking additional floors and a small set of room upgrade levers.

# 2. Branch structure

- Architecture is organized into: Expansion, Room Upgrades, Flow Tools.

- Expansion nodes are the primary gates for floor growth.

- Flow tools should be minimal in MVP and focused on clarity.

# 3. Research unlocking contract

- Each research node unlocks exactly one effect profile.

- Effects are applied through the global stacking order and may be clamped or soft capped.

- Research progress continues offline but completion is pending until online verification.

# 4. Runtime behavior

- On completion, add the effect profile to active modifiers and unlocked build options.

- Expansion unlocks increase allowed max floors and possibly floor size constraints via config.

- If a save exceeds new limits due to data changes, degrade gracefully and prompt review.

# 5. Balance guardrails

- Expansion must increase upkeep pressure indirectly through existing expansion upkeep rules.

- Flow tools must not create a dominant layout pattern that always wins.

- Room upgrades should be modest and should not bypass difficulty estimates.

# 6. MVP node set

MVP includes a minimal, readable node set that demonstrates the branch fantasy and connects to the core loop.

## Expansion

- ac_000: Architecture Fundamentals

<!-- -->

- Unlock: Enables Architecture research category

- Prereqs: None

- Notes: Root node

<!-- -->

- ac_100: Second Floor Permit

<!-- -->

- Unlock: Allow unlocking Floor 2

- Prereqs: ac_000

- Notes: Future hooks: Floor 3 to 5 permits

## Room Upgrades

- ac_200: Room Reinforcement I

<!-- -->

- Unlock: Small room durability or stability modifier

- Prereqs: ac_000

- Notes: Future hooks: reinforcement II, themed rooms

## Flow Tools

- ac_300: Basic Branching

<!-- -->

- Unlock: Enable one optional branch room per floor

- Prereqs: ac_000

- Notes: Future hooks: advanced path control

- Spatial contract: Spec 38 limits the first implementation to one reachable optional branch per floor, explicit required/optional route classification, deterministic selection, and saved same-floor graph validity. This unlock grants an allowance; it does not define hardcoded geometry or tuning.

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

<!-- P8-00-2026-10-20 -->

### Phase 8 owner-approved design precedence (2026-10-20; after merged PR #245)

D174 **supersedes the former future design** of independent `ac_100` Floor 2 permission followed by unrelated ranks: the approved Phase 8 target is **one unified ranked Architecture floor-permission progression covering Floors 1–5**, with separate per-rank authorized timed/start-paid research; a rank grants permission only, not floor construction or activation (D01–D04, D174–D176). Existing `ac_100` permission, in-flight project and already constructed Floor 2 must receive **explicit identity/entitlement compatibility review**, avoiding double charge, free rank escalation, lost saved data or accidental permission removal; **the exact migration encoding is not approved here**. Floors 3–5 require new authored floor records, with Floor 5 a noticeable capacity milestone rather than a mandatory boss room. Pillar Chamber and Flooded Basic Room are distinct **separately researched Architecture options**, not a new environmental research tree; gameplay geometry/time remains content-authored (D177–D181). Do not assume the runtime already supports ranked nodes. See [Phase 8 owner lock](../docs/planning/decisions/phase-8-owner-design-lock.md).
