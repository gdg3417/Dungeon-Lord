Status: Locked for MVP implementation support. Document date: January 15, 2026.

Scope: Monster families, family members, evolutions, and their research unlocking rules. Forward compatible with post-MVP expansions.

# 1. Purpose

Monsterology is the research and content structure that governs which monsters the player can field, how those monsters specialize, and how the dungeon identity evolves through monster choices.

Monsterology does not directly grant raw power for free. Unlocks provide access and optionality, while power comes from player choices that carry upkeep and heat consequences.

# 2. Definitions and hierarchy

Monsterology uses a three layer hierarchy. Each layer has different pacing, costs, and player meaning.

## 2.1 Monster family

- A foundational theme unlock that introduces a new baseline line of monsters.

- Unlocking a family grants one baseline species automatically.

- A family unlock is a major milestone and should be rare.

## 2.2 Monster species (family member)

- A distinct monster type within a family, not an upgrade of another species.

- Each species has its own stats, upkeep profile, AI profile, and tags.

- Species unlocks expand the player toolbox without invalidating earlier species.

## 2.3 Monster evolution

- A specialization path for a specific species (example: Goblin Archer).

- Research unlocks permission to evolve; evolution is applied per monster via a cost.

- Evolutions are tradeoffs, not linear upgrades.

# 3. Research unlocking contract

This contract must be implemented once and reused for all future content.

- A research node unlocks exactly one target.

- Family unlock grants baseline species automatically.

- Species unlock enables placement of that species and nothing else.

- Evolution unlock enables evolution of individual monsters into that evolution and nothing else.

- Unlocks are additive and never remove previously unlocked content.

- Saves store unlocked IDs, not tuned stats. Stats and costs resolve from current tables at load time.

# 4. Runtime behavior

## 4.1 Unlock application

- On research completion, add the unlocked target ID to the relevant unlocked set (families, species, evolutions).

- If a family is unlocked, also add its baseline species IDs.

- If the unlock is an evolution, the player must still pay an evolution cost per monster to apply it.

## 4.2 Evolving a monster

- Player selects an eligible monster instance and an unlocked evolution.

- The game validates: species match, evolution unlocked, capacity and reserve constraints satisfied.

- The game applies: stat modifier profile, AI profile, upkeep modifier profile, and role tags.

## 4.3 Save and content versioning

- IDs are stable. Do not rename IDs post-ship.

- If an ID must change, add a migration mapping entry from old ID to new ID.

- If a referenced ID is missing, replace with safe fallback and flag for player review.

# 5. Balance guardrails

- No species should fully obsolete another species in the same family.

- Evolutions must include at least one meaningful downside (upkeep, survivability, heat risk, or role constraints).

- Families increase complexity and therefore should increase upkeep and heat exposure as a soft limiter.

- Champion or elite evolutions are post-MVP and should be gated by both research and higher costs.

# 6. MVP content set

MVP uses two families and a minimal set of species and evolutions to validate the fantasy.

## 6.1 Families

- Goblinoid (baseline species: Goblin)

- Undead (baseline species: Skeleton)

## 6.2 Optional MVP species expansion

- Hobgoblin (Goblinoid)

- Zombie (Undead)

## 6.3 MVP evolutions

- Goblin Warrior, Goblin Archer, Goblin Shaman

- Skeleton Warrior, Skeleton Archer

# 7. Data model and tables

Monsterology is data driven. These tables are authored in the content workbook and exported to JSON.

- MonsterFamily

- MonsterSpecies

- MonsterEvolution

- ResearchNode (monsterology category)

- Profiles: StatProfiles, AIProfiles, UpkeepProfiles, CostProfiles

# 8. Linting and validation rules

A linter must run before every build export. The linter fails the build for hard errors and emits warnings for soft issues.

## 8.1 Hard fail checks

- Research unlock target does not exist.

- Circular research prerequisites.

- Family baseline species missing or belongs to a different family.

- Evolution references missing species.

- Negative upkeep values.

- Duplicate IDs in any table.

## 8.2 Warning checks

- Species with identical roles and very similar stat profiles (possible redundancy).

- Evolution with no downside tags.

- Family with baseline species count outside allowed range (target is 1 to 2).

# 9. How to add new content

This checklist is the repeatable authoring workflow. It is intended to be mechanical.

## 9.1 Add a new family

1.  Create a MonsterFamily row with stable family_id and localization keys.

2.  Create baseline MonsterSpecies rows for 1 to 2 starter species.

3.  Create Stat, AI, and Upkeep profiles for each baseline species.

4.  Create a family ResearchNode that unlocks the family.

5.  Add optional species ResearchNodes for additional family members.

6.  Add evolutions, evolution profiles, and evolution ResearchNodes.

7.  Run the linter and fix all hard failures.

8.  Add at least one test scenario for unlock and evolve flow.

## 9.2 Add a new species to an existing family

9.  Add MonsterSpecies row and required profiles.

10. Add a species ResearchNode that unlocks the species.

11. Optionally add evolutions and evolution ResearchNodes.

12. Run linter and add a test scenario.

## 9.3 Add a new evolution

13. Add MonsterEvolution row and modifier profiles.

14. Add an evolution ResearchNode that unlocks the evolution.

15. Ensure the evolution includes a meaningful downside tag and cost increase.

16. Run linter and add a test scenario for evolution application.

# 10. MVP implementation notes

- UI can be a simple categorized list for MVP. A full tree map is not required.

- Research nodes should be pure data. Code should not hardcode monster families or evolution paths.

- Debug tools recommended: force unlock panel, and a monster sandbox room for quick encounter tests.
