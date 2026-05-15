**System Spec 6: Adventurer Behavior, Evaluation, and Party AI**

**Status:** Locked (v1 for MVP), forward compatible for expanded synergy\
**Scope:** MVP + forward compatible

------------------------------------------------------------------------

**1. Purpose of the Adventurer System**

Adventurers are the heart of the dungeon ecosystem. Their variety, motivations, and behaviors determine:

- Dungeon traffic and composition

- Mana generation via damage and deaths

- Heat generation via deaths and perceived unfairness

- Loot extraction based on survivors

- Emergent stories and dungeon identity

This system defines:

- How adventurers evaluate dungeons

- How parties form

- How they behave inside the dungeon

- How class and personality traits influence decisions

------------------------------------------------------------------------

**2. Adventurer Classes**

**2.1 Launch Classes (MVP)**

- Warrior (Defense)

- Rogue (Damage, melee physical)

- Mage (Damage, ranged magic)

- Cleric (Support)

- Ranger (Damage, ranged physical)

These map directly into the MVP synergy model (Defense, Damage, Support).

**2.2 Future Classes (Forward Compatible)**

Summoners, martial hybrids, advanced magic, and support specialists as defined in your design doc v2. These later hook into expanded synergy rules such as elemental school complementarity.

------------------------------------------------------------------------

**3. Adventurer Personality Traits**

**3.1 Trait Set**

Existing traits plus the new locked traits:

- Altruistic

- Goal oriented

- Gambler

**3.2 Trait Persistence (Locked for MVP)**

Traits are **fixed per adventurer** in MVP.

Forward compatible extension:

- Traits can evolve based on dungeon experiences, but this is deferred.

------------------------------------------------------------------------

**4. Party Formation and Synergy (Updated and Locked)**

**4.1 Role Buckets (MVP)**

Each class is assigned a primary role bucket:

- Defense: Warrior

- Support: Cleric

- Damage: Rogue, Mage, Ranger

Damage subtypes (for synergy variety):

- Physical melee: Rogue

- Physical ranged: Ranger

- Magic ranged: Mage

**4.2 Party Synergy Goal**

Parties attempt to form compositions that maximize survivability and efficiency.

High synergy example target:

- 2 Defense

- 2 Damage (ideally 1 physical and 1 ranged, or 1 physical and 1 magic)

- 1 Support

Other acceptable variations exist, but parties with only one role type are considered low synergy.

**4.3 Synergy Scoring (MVP)**

Each party gets a synergy score from 0 to 1 that influences:

- Expected deaths

- Retreat probability

- Loot extraction likelihood (through survival)

- Stability of behavior inside the dungeon

Proposed MVP rule based scoring:

Start at 0, add points:

- +0.30 if at least 1 Defense

- +0.20 if at least 2 Defense

- +0.25 if at least 1 Support

- +0.15 if at least 2 Damage

- +0.10 if damage includes at least 2 subtypes (melee physical, ranged physical, ranged magic)

Penalties:

- −0.40 if party has only Damage

- −0.40 if party has only Support

- −0.40 if party has only Defense

Clamp to 0 through 1.

This is intentionally simple, explainable, and easy to tune.

**4.4 Forward Compatible Synergy Expansion**

Later versions may add synergy based on:

- Complementary elements and schools of magic

- Counter coverage against dungeon monster types

- Status effect diversity

- Combo mechanics similar to type advantage logic

This can be layered on top of role synergy.

------------------------------------------------------------------------

**5. Adventurer Dungeon Evaluation**

Before entering a dungeon, adventurers evaluate:

- Expected extracted world value from loot (Spec 5)

- Dungeon heat level and reputation

- Known monster types and floor themes

- Dungeon rating

- Personal loot preferences by class

- Personality traits

Examples:

- Gamblers tolerate high danger for rare rewards

- Goal oriented adventurers prioritize shortest path to targets

- Altruistic adventurers avoid unfair lethal dungeons and retreat to save allies

Evaluation outputs:

- Enter or skip

- Party size selection

- Party role mix attempt

- Likelihood of repeat runs

------------------------------------------------------------------------

**6. In Dungeon AI Behavior**

**6.1 Navigation and Targeting (MVP)**

MVP path behavior is rule driven:

- Default parties clear rooms in order toward the depth goal

- Goal oriented trait increases preference for shortest path and skipping optional rooms

- Curious trait increases optional room exploration

**6.2 Combat Behavior (MVP)**

Combat behaviors are defined by role:

- Defense: draws threat, holds frontline

- Damage: prioritizes targets, seeks burst windows

- Support: healing, buffs, utility

Trait modifiers:

- Cautious retreats earlier

- Reckless pushes deeper and takes higher risk

- Gambler delays retreat and interacts with more loot and traps

- Altruistic attempts rescue behaviors

**6.3 Retreat Logic (MVP)**

Retreat decision uses:

- Party average health

- Number of downed allies

- Synergy score (high synergy reduces panic retreats)

- Trait modifiers

Retreat increases survival, reducing heat and increasing extracted loot.

------------------------------------------------------------------------

**7. Interfaces**

**7.1 Mana System**

- Damage drives skill spill

- Deaths drive death mana

- Gambler and reckless behavior increases volatility and spikes

**7.2 Heat System**

- Deaths drive heat, scaled by adventurer value

- Low survival means less loot extraction and less cooling

- More stable synergy reduces deaths and political danger

**7.3 Loot System**

- Loot preference influences who enters

- Survivors determine extracted loot

- Dungeon loot design shapes class distribution over time

------------------------------------------------------------------------

**8. MVP Constraints**

For MVP:

- Traits are fixed per adventurer

- Synergy uses role bucket scoring only

- No elemental school synergy yet

- AI uses rule based behavior
