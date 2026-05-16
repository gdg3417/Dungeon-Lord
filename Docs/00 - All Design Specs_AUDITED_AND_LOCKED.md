# SPEC LOCK SUMMARY (AUTHORITATIVE)

This document represents the locked and audited system design specification for the game.

All contradictions identified during cross-spec review have been resolved.

GLOBAL LOCKS:

\- Heat system uses two-layer model: Heat Tier (coarse band) and Heat State (numeric within tier).

\- Offline heat rebound is constrained strictly within the current Heat Tier.

\- Tile placement and movement in edit mode trigger immediate saves.

\- Offline crafting is deterministic only and may not unlock progression without verification.

\- Formula stacking order is globally enforced (Spec 30 is single source of truth).

\- Economy-critical actions require online verification; telemetry is buffered and idempotent.

VERSION STATUS:

\- Specs 1–37 reviewed

\- Contradictions resolved

\- Safe for implementation

Last audit status: CLEAN

# System Spec 1. Mana System 

## Purpose

Mana is the primary economic resource that fuels progression, optimization, and long term strategic identity. Mana rewards high adventurer engagement, strong dungeon throughput, and intelligent risk management rather than passive hoarding.

------------------------------------------------------------------------

## Inputs

- Dungeon core level

- Number of active floors

- Adventurer throughput per hour

- Adventurer level

- Adventurer elite status

- Total damage dealt by adventurers

- Heat state

- Reserved mana for upkeep

- Sub dungeon outputs

- Offline progression modifier

Offline Crafting Clarification

Offline crafting is allowed only if deterministic and non premium.

Progression relevant outputs are pending verification until online.

------------------------------------------------------------------------

## Outputs

- Gross mana per minute

- Total Mana

- Reserved mana

- Usable mana

- Offline mana gained

------------------------------------------------------------------------

## Core Rules

**Base Mana Generation**

Base mana per minute = Core Level × 2

**Floor Contribution**

Additional mana per minute = Active Floors × 1

**Adventurer Death Mana**

Base death mana = Adventurer Level × 5

Elite adventurer death mana = Base death mana × 2.5

Elite adventurers apply their multiplier to **both death mana and skill spill mana**.

------------------------------------------------------------------------

## Skill Spill Mana Online

Mana from skill spill = Total damage dealt × 0.15

There is no per adventurer cap on skill spill. Higher adventurer count and stronger adventurers always increase total skill spill.

Elite adventurers multiply their contribution to total damage, and therefore indirectly multiply skill spill mana. In practice this is implemented by applying the 2.5 multiplier to elite tagged runs when converting their damage into spill.

------------------------------------------------------------------------

## Offline Skill Spill

During offline progression, skill spill is calculated using **expected damage**, not simulated combat.

Offline skill spill mana = Expected damage × 0.15 × 0.30 (offline multiplier)

- The base coefficient is lower than online play

- Research can increase the retained percentage for offline (base 30%)

- Offline skill spill can never exceed online potential

This prevents incentivizing leaving the game open continuously.

------------------------------------------------------------------------

## Heat Efficiency Modifier

Peace\
Mana efficiency = 100 percent

Notice\
Mana efficiency = 95 percent

Concern\
Mana efficiency = 85 percent

All mana sources are multiplied by the current heat efficiency.

------------------------------------------------------------------------

## Mana Reservation and Availability

Mana is divided into three visible pools:

- Total Mana

- Reserved Mana

- Usable Mana

Reserved mana is allocated to monster upkeep and other persistent systems.

Usable Mana = Total Mana − Reserved Mana

If Reserved Mana exceeds incoming mana generation, the dungeon enters a fragile state where recovery depends on player intervention.

------------------------------------------------------------------------

## Player Decisions Enabled

- Attract many weaker adventurers versus fewer elite ones

- Optimize for skill spill versus death mana

- Balance monster density against reserved mana pressure

- Invest in research to improve offline efficiency

------------------------------------------------------------------------

## Failure Conditions

- Over investment in elites increases danger and risk

- Excessive reserved mana limits growth

- Poor layouts underperform regardless of monster strength

# System Spec 2. Heat System (Draft v3 Locked Pending Final Numbers)

## Purpose

Heat represents the world’s response to dungeon behavior. It ensures that danger, profit, and reputation remain in tension and prevents consequence free exploitation.

------------------------------------------------------------------------

## Inputs

- Adventurer deaths

- Elite adventurer deaths

- Party survival outcomes

- Party size

- Dungeon depth reached

- Tradeable loot value extracted

- Passive decay rate

- Research modifiers

------------------------------------------------------------------------

## Outputs

- Current heat value

- Heat state

- World response triggers

------------------------------------------------------------------------

## Core Rules

**Heat Gain**

Normal adventurer death\
+1 heat

Elite adventurer death\
+3 heat

Multiple deaths in a single run\
+1 bonus heat

------------------------------------------------------------------------

## Heat Reduction From Successful Runs

If an entire adventurer party survives a run, heat is reduced based on:

- Party size

- Depth reached

Deeper runs imply smoother difficulty progression and reduce heat more.

Larger parties reduce perceived danger and therefore **reduce the magnitude of heat reduction**.

This creates a balancing curve rather than a flat reward.

------------------------------------------------------------------------

## Heat Reduction From Tradeable Loot

Heat is reduced based on the **total value of tradeable loot successfully extracted**.

Higher value loot that enters the world economy increases goodwill, repeat traffic, and reputation.

------------------------------------------------------------------------

## Heat Floor

Heat is capped at a minimum of -15 in MVP. This cap is configurable for future expansion.

The minimum heat cap is intended to enable future leaderboard style tracking for lowest heat.

------------------------------------------------------------------------

## Passive Heat Decay

Heat decays over time at a slow baseline rate.

Research can increase passive decay speed and improve recovery from high heat states.

Passive decay can never fully counteract reckless play on its own.

------------------------------------------------------------------------

## Heat States (MVP)

Peace\
Heat 0 to 9

Notice\
Heat 10 to 24

Concern\
Heat 25 to 49

Higher states are intentionally excluded from MVP.

------------------------------------------------------------------------

## Player Decisions Enabled

- Allow parties to escape intentionally

- Design smoother difficulty curves

- Adjust loot tables to stabilize heat

- Invest in diplomacy and decay research

------------------------------------------------------------------------

## Failure Conditions

- Sustained Concern state reduces adventurer traffic

- Poor reputation slows progression

- Heat mismanagement forces reactive play

# System Spec 3. Monster Stats and Upkeep (Draft v3 Locked Pending Final Numbers)

## Purpose

Monsters define dungeon identity and difficulty. Their scaling and upkeep enforce tradeoffs between power, density, and sustainability.

------------------------------------------------------------------------

## Inputs

- Monster base stats

- Monster level

- Evolution tier

- Synergy bonuses

- Active monster count

------------------------------------------------------------------------

## Outputs

- Monster damage

- Monster health

- Summon cost

- Reserved mana upkeep

------------------------------------------------------------------------

## Core Rules

**Stat Scaling**

Health = Base Health × (1 + Level × 0.12)

Damage = Base Damage × (1 + Level × 0.10)

------------------------------------------------------------------------

**Summon Cost**

Summon cost = Base Cost + (Level × 10 mana)

------------------------------------------------------------------------

## Evolution Efficiency

Tier 2 evolution\
+20 percent stat efficiency

Tier 3 evolution\
+35 percent stat efficiency

------------------------------------------------------------------------

## Monster Upkeep

Each monster reserves mana based on:

- Monster level

- Evolution tier

Upkeep is a **reserved mana cost**, not a continuous drain.

Upkeep formula conceptually scales faster than linear to prevent infinite low level stacking.

------------------------------------------------------------------------

## Replacement Logic

When a monster dies:

- If sufficient mana exists, it is replaced automatically

- If insufficient mana exists, the slot remains empty

The dungeon continues operating with reduced effectiveness.

This can cascade into further danger if too many monsters are missing.

Monster deaths do not directly create penalties. The cost is implicit through upkeep pressure and replacement risk.

------------------------------------------------------------------------

## Player Decisions Enabled

- Fewer elite monsters versus many weak ones

- Managing reserved mana headroom

- Intentional monster loss as a recovery tool

- Timing expansions around upkeep capacity

------------------------------------------------------------------------

## Failure Conditions

- Excessive upkeep starves usable mana

- Poor monster composition weakens dungeon defense

- Over commitment increases systemic risk

# System Spec 4. Adventurer Evaluation and Behavior (Spec v1)

## Purpose

The Adventurer Evaluation and Behavior system determines who enters the dungeon, in what composition, how they move through it, when they retreat, and how they impact mana and heat. This system is the primary driver of emergent gameplay and dungeon identity.

------------------------------------------------------------------------

## Inputs

- Dungeon rating

- Heat value and heat state

- Active floor count

- Highest monster level present

- Monster family composition

- Loot table composition

- Mana efficiency

- Negative or positive heat modifiers

------------------------------------------------------------------------

## Outputs

- Adventurer party spawn rate

- Party size

- Adventurer class mix

- Elite adventurer probability

- Run outcome (clear, partial clear, retreat, death)

- Mana generation events

- Heat change events

------------------------------------------------------------------------

## Core Rules

**Dungeon Rating**

Dungeon rating is a composite value recalculated periodically.

Dungeon Rating Factors:

- Average monster level

- Monster density per floor

- Trap density

- Synergy bonuses

- Floor count

Dungeon rating does not directly gate content, but strongly influences adventurer selection.

------------------------------------------------------------------------

## Party Spawn Rate

Base adventurer party spawn rate is influenced by:

- Dungeon rating (higher rating attracts stronger but fewer parties)

- Heat state (higher heat reduces traffic)

- Negative heat bonus (increases low level traffic)

When heat is negative:

- Low level adventurer traffic increases

- Mana efficiency increases to 110 percent

- Adventurer death mana generation is reduced to compensate

------------------------------------------------------------------------

## Party Size

Early game party size range:

- 1 to 3 adventurers

Mid game party size range:

- 3 to 6 adventurers

Party size selection weights increase with dungeon rating and floor count.

------------------------------------------------------------------------

## Elite Adventurer Spawning

Baseline elite spawn chance:

- 1 percent per party

Elite spawn chance increases with:

- Dungeon rating

- Absence of low level monsters in the dungeon

In MVP:

- Heat does not directly increase elite spawn chance

- Very high dungeon ratings can result in elite only parties

Elite adventurers:

- Have higher stats

- Deal more damage

- Generate 2.5x death mana

- Generate increased skill spill via higher damage

## Adventurer Class Selection

Adventurer classes are selected based on:

- Loot table composition

- Monster families present

- Floor themes (if applicable)

Balanced parties are preferred at higher dungeon ratings.

------------------------------------------------------------------------

## Pathing and Depth

Adventurers attempt to progress until:

- Risk exceeds tolerance

- Objectives are met

- Party members die or retreat logic triggers

Depth reached is defined as:

- Highest floor entered during the run

------------------------------------------------------------------------

## Retreat Logic

Retreat chance increases when:

- Party health is low

- Multiple adventurers fall

- Difficulty spikes sharply

Surviving full party runs reduce heat based on:

- Party size

- Highest floor entered

------------------------------------------------------------------------

## Player Decisions Enabled

- Shape dungeon to attract elites or farmers

- Smooth difficulty curves to reduce heat

- Manipulate party survival for reputation

- Control traffic type via monster and loot choices

------------------------------------------------------------------------

## Failure Conditions

- Over tuned dungeons reduce traffic

- Excessive lethality spikes heat

- Poor dungeon signaling leads to mismatched parties

------------------------------------------------------------------------

## Design Notes

This system is intentionally probabilistic to encourage experimentation and emergent outcomes rather than deterministic farming.

# System Spec 5: Loot Tables, Value, and Attraction

**Status: Locked (v1)\
Scope: MVP + forward compatible**

## 1. Purpose of the Loot System

**The loot system exists to drive four things simultaneously:**

1.  **Adventurer attraction and traffic**

2.  **Political pressure and heat mitigation**

3.  **Mana reserve pressure through crafting**

4.  **Long term dungeon identity and progression**

**Loot is not a direct mana source. Instead, it is a traffic amplifier and a heat stabilizer that introduces risk, reserve tension, and strategic tradeoffs. Loot represents value returned to the world and defines how the dungeon is perceived politically and economically.**

## 2. Loot Generation Model

**2.1 Loot Rolls Basis**

**Loot rolls are generated per room cleared, not per run.**

- **Each room has its own loot table.**

- **The dungeon lord configures loot tables at the room level.**

- **Deeper rooms may have richer tables, higher tier weights, or rarity guarantees.**

**This allows:**

- **Early rooms to provide low risk, low value loot**

- **Later rooms to function as high risk, high reward targets**

- **Fine grained dungeon pacing and specialization**

## 3. Loot Tables

**3.1 Room Loot Tables**

**Each room has a configurable loot table consisting of:**

- **Loot roll count**

- **Tier weights**

- **Rarity weights**

- **Item pools**

- **Tradeable flags**

**Examples**

- **Room 1: copper coins only, variable quantity**

- **Floor boss room: rare minimum, small chance for epic weapons, consumables, and trade goods**

**Room level control is a core expression of player intent.**

## 4. Loot Extraction Rules

**4.1 Survivor Based Extraction**

**Loot extraction only occurs if there are survivors.**

**Extracted loot amount scales with survivor ratio:**

**ExtractedLoot = GeneratedLoot × (Survivors ÷ PartySize)**

**Example**

- **Party of 5 loses 1 member**

- **Extracts 80 percent of generated loot**

**If all adventurers die:**

- **No loot is extracted**

- **Loot does not reduce heat**

- **Loot still incurs crafting reserve cost**

**This directly reinforces the political danger model.**

## 5. Boss Loot Rules

**5.1 Boss Guarantees**

**Bosses guarantee:**

- **Minimum rarity: Rare**

**Bosses do not guarantee:**

- **A tier minimum**

- **A specific item category**

**This preserves excitement without forcing deterministic progression.**

# System Spec 6: Loot Categories (Expanded and Locked)

**Loot belongs to a family. Each family has tiers, rarity weights, economic impact, and attraction behavior.**

## 6.1 Materials

**Includes**

- **Copper, tin, iron, silver, gold**

- **Mithril, adamantine, orichalcum, meteorite**

- **Monster parts such as fangs, bones, claws**

- **Refined monster essences**

**Uses**

- **Crafting**

- **Research**

- **Enchanting**

- **Merchant contracts**

**Design Notes**

- **High volume, lower prestige**

- **Backbone of crafting and research pacing**

- **Moderate heat reduction when extracted in bulk**

## 6.2 Consumables

**Includes**

- **Food and alcohol**

- **Healing potions**

- **Stamina draughts**

- **Antidotes**

- **Elemental resistance elixirs**

- **Movement elixirs**

- **Experience boosting elixirs**

**Design Notes**

- **High demand and frequent consumption**

- **Can define dungeon identity through volume**

- **Strong traffic driver, moderate political impact**

## 6.3 Currency

**Includes**

- **Local kingdom coinage**

- **Dungeon minted tokens**

- **Rare collector coins from boss events**

**Design Notes**

- **Simple mana replacement cost**

- **Medium adventurer attraction impact**

- **Low prestige, high liquidity**

- **Useful for stabilizing heat without increasing lethality**

## 6.4 Utility Items

**Includes**

- **Rope, torches, climbing gear**

- **Backpacks, tents**

- **Magical utilities such as everbright lanterns or warm tents**

- **Multi function magical tools**

**Design Notes**

- **Attracts explorers, scouts, merchants, and caravan guards**

- **Low combat impact, high world flavor**

- **Enables non combat and logistics focused dungeon identities**

## 6.5 Gear and Equipment

**Includes**

- **Bronze, iron, steel weapons and armor**

- **Mithril gear**

- **Adamantine heavy gear**

- **Orichalcum resonance gear**

- **Meteorite tier end game gear**

**Properties**

- **One to three enchantment slots**

- **Base stat scaling**

- **Class tags influencing adventurer attraction**

**Design Notes**

- **Primary driver of high value adventurer traffic**

- **Core prestige vector for the dungeon**

- **Strong interaction with heat and elite adventurers**

## 6.6 Spell Scrolls

**Includes**

- **Elemental spells**

- **Protective spells**

- **Support spells**

- **Unique scrolls derived from adventurer drops**

**Design Notes**

- **Strongly attracts mages and scholars**

- **Synergizes with magic themed floors**

- **Enables spell focused dungeon specializations**

## 6.7 Boss Sets

**Definition\
Each monster family yields a themed boss set.**

**Examples**

- **Goblin trickster set**

- **Skeleton knight set**

- **Orc warlord set**

- **Minotaur guardian set**

- **Dragon sovereign set**

**Set Structure**

- **Mandatory stats**

- **Optional custom stats via research**

- **Two piece bonus**

- **Four piece bonus**

**Design Notes**

- **Huge adventurer traffic spikes**

- **High prestige and mid to late game monetization interest**

- **Boss sets guarantee minimum rarity: Rare**

## 7. Trade Goods (Hybrid Model)

**Trade goods exist in two forms:**

1.  **Dedicated trade items**

    - **Coffee beans, monster silk, magical plants**

2.  **Flagged items**

    - **Consumables or materials that are both usable and sellable**

**Tradeable status determines whether loot contributes to heat reduction and merchant traffic.**

## 8. Loot Value System

**8.1 World Value**

**World value represents economic benefit to the surrounding world.**

**WorldValue =**

**TierBaseValue × (TierGrowthFactor^(TierIndex − 1)) × RarityMultiplier**

**World value is used for:**

- **Heat reduction from extracted loot**

- **Attraction strength calculations**

**World value is not used for mana generation.**

## 9. Loot Crafting Reserve Cost

**Loot is created magically, not purchased. Cost reflects complexity and density.**

**9.1 Loot Crafting Reserve Formula**

**LootCraftReserve =**

**BaseCraftReserve**

**× (CraftTierFactor^(TierIndex − 1))**

**× (1 + RareTax)**

**RareTax**

- **Common: 0.00**

- **Uncommon: 0.05**

- **Rare: 0.10**

- **Epic: 0.20**

- **Legendary: 0.35**

**Reserve cost applies:**

- **Per loot item generated**

- **Regardless of extraction success**

**This ensures:**

- **High loot density creates reserve pressure**

- **Loot rich dungeons must plan capacity carefully**

## 10. Loot Progression and Research Lock

**10.1 Research Gated Loot**

**Loot cannot be created without research.**

**Progression Path**

1.  **Absorb an item dropped by an adventurer**

2.  **Analyze the item to unlock its blueprint**

3.  **Research the blueprint**

4.  **Add the item to loot tables at a mana reserve cost**

5.  **Optionally research enchantment variants**

6.  **Manage loot table probabilities**

**This guarantees:**

- **Long term pacing**

- **No early access to endgame loot**

- **Dungeon identity emerges through investment**

## 11. Attraction System Interface

**Loot contributes to attraction through expected extracted world value, not generated value.**

**Higher expected value:**

- **Increases traffic**

- **Increases average adventurer level**

- **Increases elite chance indirectly**

**Heat applies a penalty to attraction to ensure balance.**

## 12. Heat System Interface

- **Heat reduction from loot only applies to extracted tradeable loot**

- **No survivors means no cooling**

- **High value adventurer deaths increase heat more strongly**

**This enforces:**

- **Political danger over raw lethality**

- **Viable non lethal and hybrid playstyles**

## 13. Failure Conditions

**The system is considered broken if:**

- **Loot reduces heat without survival**

- **Loot crafting reserve is negligible**

- **High tier loot becomes free through traffic**

- **Loot attraction overwhelms heat penalties**

- **Dungeon identity collapses into a single optimal table**

## 14. MVP Constraints

**For MVP:**

- **Tier range limited**

- **Regional scarcity ignored**

- **Item pool sizes small**

- **Loot table UI simple but explicit**

**All systems are forward compatible.**

# System Spec 6: Adventurer Behavior, Evaluation, and Party AI

**Status:** Locked (v1 for MVP), forward compatible for expanded synergy\
**Scope:** MVP + forward compatible

------------------------------------------------------------------------

## 1. Purpose of the Adventurer System

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

# System Spec 7: Political, Economic, and Guild Reputation

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the System

This system governs how the outside world perceives, reacts to, and eventually intervenes in the dungeon.

It replaces a single overloaded heat value with three distinct reputation axes that together determine political pressure, adventurer behavior, and escalation risk.

2\. Reputation Axes Overview

The dungeon tracks three independent reputations. All three are good when high and bad when low.

Political Reputation

Represents how much the kingdom trusts the dungeon not to harm its people.

High = trust and stability.

Low = suspicion and fear.

Deaths reduce political reputation.

Economic Reputation

Represents the dungeon’s contribution to the surrounding economy.

Offsets political pressure but never fully negates it.

Guild Reputation

Represents how adventurer guilds perceive fairness, safety, and professionalism.

Strongly influences party quality and repeat traffic.

3\. Political Reputation Rules

Decreases from:

\- Adventurer deaths weighted by value

\- Elite or named deaths

\- Monster overflows

\- Unfair danger vs reward

Increases from:

\- Civic focused merchant contracts

\- High survival training events

\- Diplomacy actions

\- Successful raid defense

4\. Economic Reputation Rules

Increases from:

\- Extracted tradeable loot

\- Merchant contracts

\- Currency circulation

Decreases from:

\- Contract failures

\- Supply instability

5\. Guild Reputation Rules

Increases from:

\- High survival ratios

\- Predictable difficulty

\- Fair loot tables

Decreases from:

\- Unexpected wipes

\- Trap overuse

\- Sudden difficulty spikes

6\. Reputation States

Peace

Notice

Concern

Hostile

Raid

For MVP, Peace, Notice, and Concern are active.

7\. Merchant Contracts

Direct contracts with delivery plus safety requirements.

Player delivers specific loot quantities while maintaining survival ratios.

Rewards include reputation gains, reserve relief, and research boosts.

8\. Raids

Raids partially reduce political reputation.

They do not fully reset reputations.

Failure causes structural and progression losses.

9\. System Interfaces

Mana, Loot, and Adventurer systems all interact with reputation.

10\. MVP Constraints

Three numeric reputation values.

Diplomacy abstracted.

Raids disabled but tracked.

System Spec 7 is locked for MVP.

# System Spec 8: Raids and Escalation Encounters

Status: Locked v1

Scope: Post-MVP systems defined, MVP simulation only

1\. Purpose of the Raid System

Raids represent the ultimate consequence of sustained political failure.

They enforce long-term balance and create high-stakes pressure without hard game over.

Raids are dangerous multi-wave pressure events focused on protecting the dungeon core.

For MVP, raids are simulated only.

2\. Raid Triggering and Warning Phase

2.1 Trigger Conditions

A raid becomes eligible when Political Reputation drops below the raid threshold.

Raids never trigger offline and always include a warning window.

2.2 Warning Window

The player is notified and given time to prepare.

If Political Reputation is restored during this window, the raid is canceled.

Once begun, raids cannot be abandoned.

2.3 Raid Frequency Limits

Multiple raids may occur if defended successfully.

A maximum of two raids may occur consecutively.

3\. Raid Structure

3.1 Wave-Based Design

Raids are composed of multiple waves such as scouts, elites, heroes, and a final assault.

For MVP, wave resolution is simulated.

3.2 Dynamic Raid Composition

Raid forces use predefined templates modified dynamically by dungeon depth, layout, monsters, loot profile, and past outcomes.

Raids react dynamically to resistance.

4\. Dungeon Core Rules

4.1 Core Location

The dungeon core is located in the final room of the final floor and hidden until revealed.

4.2 Failure Condition

A raid fails when dungeon core health reaches zero.

4.3 Victory Condition

A raid is successfully defended only if all planned waves are completed.

5\. Dungeon Core Damage and Consequences

5.1 Effects of Damage

Core damage reduces mana generation, slows research, and applies reserve penalties.

5.2 Recovery Rules

Damage is temporary and fully recoverable over time.

5.3 Game Over Rules

Core destruction does not end the game.

6\. Player Agency During Raids

For MVP, player agency exists only during preparation.

Future versions may allow mana sacrifice for emergency boosts.

7\. Raid Research

Raid-specific research does not exist in MVP.

8\. Rewards and Penalties

8.1 Successful Defense

Successful defense grants partial political reputation recovery and a permanent prestige marker.

8.2 Failure Penalties

Failure applies temporary mana penalties and reduces active research progress.

9\. System Interfaces

Raids interact with Political, Economic, and Guild Reputation systems.

10\. MVP Constraints

Raids are simulated only.

No real-time raid gameplay.

No raid-specific UI beyond warnings and results.

11\. Failure Conditions

The system fails if raids can be farmed, fully reset reputation, or cause irreversible loss.

System Spec 8 is locked.

# System Spec 9: Dungeon Progression and Research

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the Progression System

The progression and research system governs how the dungeon grows in power, complexity, and identity over time.

It delivers fast early excitement while sustaining long-term engagement through depth and scarcity.

Progression is capacity-driven, not raw power-driven.

2\. Progression Philosophy

2.1 Overall Progression Curve

Dungeon progression follows a fast early, slow late curve.

Early unlocks arrive quickly while late game depth focuses on optimization and resilience.

2.2 Dungeon Core Leveling

Dungeon core level is capped by a combination of floors and research.

Adding floors increases potential core level, while research unlocks access to higher levels.

2.3 Core Level Effects

Core levels increase maximum mana capacity and unlock new research gated by mana thresholds.

No direct power bonuses are granted.

3\. Research Structure

3.1 Research Cost Model

Research requires both time and mana investment.

Mana is reserved while research is active.

Certain research paths require dungeon activity prerequisites.

3.2 Research Slots

Research is limited by concurrent slots.

Slots are expandable via premium currency.

3.3 Research Priority Rules

Research pauses when usable mana is insufficient.

Reserved mana for infrastructure and monsters always takes priority.

4\. Research Categories

4.1 MVP Research Categories

Mana and efficiency research.

Dungeon infrastructure and rooms.

Monster evolution and behavior (limited).

4.2 Non-MVP Categories

Loot blueprints and enchantments.

Adventurer manipulation and attraction.

Political, economic, and guild interaction research.

5\. Loot and Research Interaction

5.1 Loot Absorption

Loot absorption creates a temporary analysis state.

Blueprints become researchable after analysis.

5.2 Higher Tier Loot Requirements

Higher tier loot requires multiple samples and longer research.

5.3 Discovery Rules

Items must be discovered before research.

Merchant contracts may introduce new items.

6\. Long-Term Pacing and Balance

6.1 Research Caps

No hard caps exist.

Soft caps emerge through rising costs.

6.2 Failure Impact

Failed runs and raids slow research and may destroy partial progress.

7\. Player Choice and Identity

7.1 Specialization

Players may specialize deeply or research everything over time.

Specialization is faster and more efficient.

7.2 Research Permanence

Research choices are permanent in MVP.

8\. System Interfaces

Research interacts with Mana, Loot, Reputation, and Raid systems.

9\. Failure Conditions

The system fails if progression becomes trivial or ignores mana pressure.

System Spec 9 is locked.

# System Spec 10: Dungeon Identity and Specialization

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the Identity System

Dungeon identity defines what your dungeon is known for.

It shapes world perception, mechanical behavior, and player build fantasy.

Identity is emergent, not a buff system.

2\. What Dungeon Identity Is

Identity reflects how the world perceives the dungeon, how it behaves mechanically, and how the player expresses intent.

It is emergent from systems and summarized for the player.

The player never directly selects an identity.

3\. Identity Visibility

For MVP, identity is not shown via meters or sliders.

It is communicated through descriptive labels, NPC dialogue, tooltips, and world reactions.

4\. Identity Structure

4.1 Primary Identity

Each dungeon has one primary identity in MVP.

It emerges in mid game based on dominant systems.

4.2 Future Expansion

Secondary and hybrid identities are possible post-MVP.

5\. Sources of Identity in MVP

Monster families and evolutions.

Loot families produced.

Floor biomes and themes.

Adventurer survival versus lethality outcomes.

6\. Specialization Philosophy

There are no explicit specialization bonuses.

Depth of investment creates stronger outcomes.

Generalization creates flexibility at lower peak power.

7\. Permanence and Reversibility

Identity is reversible but costly.

Changing identity requires research, redesign, monster changes, loot changes, time, and mana.

8\. When Identity Matters

Identity becomes meaningful in mid game.

It strengthens in late game as costs increase.

9\. Identity Effects on the World

In MVP, identity influences adventurer class mix, level, elite chance, and loot expectations.

It does not directly affect politics, contracts, or raids.

10\. Player Agency

Players intentionally shape identity through dungeon building decisions.

There is no explicit identity control UI.

11\. Anti-Meta Safeguards

Soft caps, reputation pressure, mana tension, and research costs discourage a single optimal build.

World incentives may rotate post-MVP.

12\. Balance Philosophy

Some identities may be stronger early or late.

Niche identities are acceptable.

Fun and variety are prioritized over perfect symmetry.

13\. Failure Conditions

The system fails if identity is free to swap, purely cosmetic, or has no world impact.

System Spec 10 is locked.

# System Spec 11: Failure, Recovery, and Soft Loss

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the Failure System

Failure exists to punish reckless or imbalanced play while avoiding hard game over states.

All failure is recoverable given time.

2\. Failure Types in MVP

Mana collapse where reserved mana exceeds generation.

Research collapse due to sustained shortages.

Monster extinction on a floor.

3\. Mana Collapse

Occurs when reserved mana exceeds total capacity or usable mana is zero for a sustained period.

This places the dungeon into a crisis state.

4\. Crisis State Behavior

Systems shut down in priority order:

Research first.

Loot crafting second.

Infrastructure effects third.

Monster spawning last.

No systems are permanently destroyed.

5\. Research Collapse

Research progress may be slowed or partially lost due to sustained mana shortages.

6\. Recovery Philosophy

Recovery is primarily driven by player action with minimal passive assistance.

Reducing dungeon complexity and sacrificing efficiency speeds recovery.

7\. Penalties and Memory

No permanent mechanical penalties exist after recovery.

Failure history is retained for narrative and analytics purposes only.

8\. Player Messaging

The system provides advance warnings and post-failure explanations.

9\. Anti-Abuse Rules

Failure is never optimal.

Intentional collapse provides no benefits.

10\. Failure Conditions

The system fails if collapse is profitable or unrecoverable.

System Spec 11 is locked.

# System Spec 12: Monetization Philosophy and Premium Currency

## Status

Locked v1

Scope: MVP plus forward compatible

## 1. Core Philosophy

Monetization exists to support development without creating pay-to-win advantages.

Premium currency is the primary monetization vector.

All gameplay content must be reachable without spending money, given sufficient time.

Direct power purchases are not allowed.

## 2. Premium Currency

There is a single universal premium currency.

Premium currency can be earned slowly through gameplay such as achievements and daily or weekly challenges.

Ads are not required for premium currency acquisition.

## 3. Allowed Premium Uses

Purchase additional research slots, with each slot increasing in cost and a hard cap.

Instantly complete research projects.

Purchase cosmetics.

Purchase challenge or battle pass style progression tracks.

Purchase temporary mana accumulation boosts that do not bypass formulas.

## 4. Disallowed Premium Uses

Premium currency cannot be used to buy mana directly.

Premium currency cannot be used to buy monsters, floors, raids, or reputation resets.

Premium currency cannot unlock research categories early.

Premium currency cannot bypass mana or reserve constraints.

Premium currency cannot enable time skips.

## 5. Research Interaction

Premium currency impacts time only, never resource requirements.

Research pauses if mana or reserves are insufficient, regardless of premium usage.

## 6. Time and Offline Systems

Premium currency cannot extend offline progression caps in MVP.

Premium currency cannot simulate offline time.

Premium currency cannot reduce penalties from being offline.

## 7. Dungeon Identity and Fairness

Monetization cannot directly influence dungeon identity.

Indirect influence through convenience is acceptable.

Paying players may progress faster and have more flexibility but never higher ceilings.

## 8. Anti-Abuse and Trust Rules

The game must never become pay-to-win.

Selling mana, monsters, floors, raid immunity, or reputation resets is forbidden.

Hidden premium discounts or behind-the-scenes advantages are forbidden.

## 9. Future Considerations (Post-MVP)

Monster-assisted research using intelligence stats may be explored post-MVP.

Research-focused monster systems or gacha-style mechanics are optional and non-essential.

## 10. Failure Conditions

The system is broken if premium spending bypasses core formulas or creates irreversible advantage.

# System Spec 13: Live Operations and World Events

**Status:** Locked (v1)\
**Scope:** Post-MVP aware, MVP gated\
**Purpose:** Add long-term variety, meta disruption, and world storytelling without breaking balance or forcing participation.

------------------------------------------------------------------------

**1. Core Philosophy**

**1.1 Purpose of Live Events**

Live events exist to:

- Temporarily reshape dungeon building metas

- Add world-level narrative and lore

- Encourage adaptation without invalidating player identity

- Provide optional progression and rewards

Live events are **not required content** and should never hard block progression.

**1.2 Event Impact Style**

Live events are a **mix of**:

- Mechanical modifiers

- Meta disruption

- Narrative flavor

Examples:

- Magical heat wave increases fire damage, traps, and heat-based effects by 15%

- A lich ritual empowers undead with light resistance and a damage boost

**1.3 World Evolution**

The world:

- Slowly evolves over time

- Uses events as lore anchors

- Can reuse past events with contextual explanations

Events may reference:

- Dragon migrations

- Powerful NPC rituals

- Political shifts between kingdoms

------------------------------------------------------------------------

**2. Event Frequency and Scope**

**2.1 MVP Timing**

For MVP:

- No always-on events

- Events do not activate until players approach end-of-content

- Events may be introduced later to extend longevity

**2.2 Scope of Impact**

Events may affect:

- All players globally

- Specific kingdoms or regions

Events do **not** target individual dungeons directly.

Kingdom events may:

- Shift adventurer traffic

- Increase elite presence

- Move geographically over time

------------------------------------------------------------------------

**3. Allowed System Modifications**

Events **may modify**:

- Mana generation rates

- Loot rarity weights

- Monster effectiveness

- Adventurer behavior

Events **may not modify**:

- Heat generation

- Heat decay

- Core political thresholds

This preserves the integrity of the heat and failure systems.

**4. Identity and Meta Interaction**

**4.1 Identity Effects**

Events are allowed to:

- Favor different dungeon identities over time

- Occasionally suppress dominant metas

- Create temporary power spikes for specific builds

**4.2 Player Adaptation**

It is acceptable and intended that:

- Some identities thrive during events

- Others temporarily struggle

- Players adapt layouts, monsters, or loot tables

No event should permanently invalidate a dungeon identity.

------------------------------------------------------------------------

**5. Monetization Interaction**

**5.1 Event Access**

- All players can participate in events

- No premium currency gates event access

**5.2 Event Pass Structure**

Events include:

- A free reward track

- A premium reward track

Premium tracks may grant:

- Additional premium currency

- Cosmetics

- Limited access to monsters or loot items

Free players retain full event participation but earn fewer rewards.

**5.3 Restrictions**

Premium currency:

- Cannot speed up events

- Cannot bypass event challenges

- Cannot guarantee event rewards

------------------------------------------------------------------------

**6. Anti-Burnout and Fairness**

**6.1 Missed Events**

Missing an event:

- Does not permanently lock rewards

- Allows later catch-up through rotating rewards

- May only lock cosmetic prestige temporarily

**6.2 Risk Profile**

Events:

- May occasionally increase risk

- Should not meaningfully increase failure probability

- Are never guaranteed safe

------------------------------------------------------------------------

**7. MVP Boundary**

For MVP, events are:

- Simple numeric modifiers

- Lightweight quest chains

- Mostly hidden simulations

Events are **not released** until:

- Players approach the end of MVP content

- They are used to extend the lifecycle of the base game

# System Spec 14: Player Progression Loops & Retention

**Status: Locked (v1)**\
**Scope: MVP + forward compatible**

------------------------------------------------------------------------

**1. Retention Philosophy**

Player retention is driven by a layered mix of motivations:

Priority order:

1.  Long term goals spanning weeks to months that extend post-MVP

2.  Short term wins during early play and onboarding

3.  Ongoing mastery and optimization loops

4.  Narrative curiosity and evolving world lore

The game should feel:

- Occasionally stressful but rewarding

- Strategically engaging

- Calm during planning phases with intensity spikes during pressure moments

Players may feel “done” for the day depending on playstyle:

- Early game offers frequent touchpoints

- Late game supports long redesign sessions and extended planning windows

------------------------------------------------------------------------

**2. Core Progression Loops (MVP)**

The following loops are active in MVP:

- Build → observe → tweak dungeon layout

- Research → unlock → redesign

- Loot absorption → analysis → new options

- Heat pressure → adaptation → stabilization

Not included in MVP:

- Adventurer traffic driven identity loops

- Pure idle accumulation without decisions

Event or challenge driven bursts exist near the end of MVP to extend engagement.

------------------------------------------------------------------------

**3. Session Structure**

**Short sessions**

- Target length: 5 to 10 minutes

- Typical actions:

  - Checking outcomes

  - Making one or two decisions

  - Collecting rewards

  - Reacting to problems

  - Optional light planning

**Long sessions**

- Deep planning and optimization

- Large dungeon redesigns

- Multiple systems interacted with

- Narrative and lore exploration

------------------------------------------------------------------------

**4. Daily and Weekly Cadence**

- Daily tasks exist but are implicit through natural play

- Weekly challenges exist

- Long term milestones exist but are not the primary driver

Missing a day:

- Mildly slows progress

- Never blocks recovery or future access

Daily actions:

- Not explicit checklists

- Naturally occur through regular play

- Always ignorable without punishment

------------------------------------------------------------------------

**5. Player Goals and Motivation**

The system actively supports:

- Optimization and efficiency

- Collection completion

- Prestige and recognition

- Narrative discovery

Players can set:

- Explicit goals by pinning targets

- Implicit goals suggested by systems

The game may suggest improvements such as reducing heat, but must never force actions or interrupt play.

------------------------------------------------------------------------

**6. Anti-Burnout Design**

When players feel overwhelmed, the game:

- Suggests simplifications

- Allows systems to be temporarily ignored

A “maintenance mode” style is allowed:

- Minimal risk play

- Mostly hands-off dungeon operation

- Pressure systems like heat may be reduced or disabled

Tradeoff:

- Reduced mana gain or efficiency penalties apply to compensate reduced engagement

------------------------------------------------------------------------

**7. Monetization Interaction**

Premium features:

- Only shorten research loops

- Remove friction

- Never replace engagement

- Never add exclusive content

Paying players may:

- Run more concurrent research

- Gain planning flexibility

- Reach long term goals sooner

They must always experience the same gameplay beats as free players.

------------------------------------------------------------------------

**8. End-State Retention**

Late game retention is supported by:

- Identity perfection

- Reaction to world changes

- Long research chains

- Mastery challenges

- Prestige or legacy systems post-MVP

Players may:

- Pause for weeks

- Return without punishment

- Re-engage naturally

# System Spec 15: UI, Information Exposure, and Player Trust

**Status: Locked (v1)**\
**Scope: MVP + forward compatible**

------------------------------------------------------------------------

**1. Information Philosophy**

The UI follows a layered information approach:

- Exact numbers are shown where meaningful

- Math is hidden behind clear cause and effect explanations

- Ranges and abstract trend indicators are not used

Advanced players may view full formulas:

- Only through optional advanced views

- Never forced on casual players

Early game opacity is allowed:

- Some systems are intentionally opaque during onboarding

- Transparency increases as systems unlock

------------------------------------------------------------------------

**2. Player Trust Principles**

The system guarantees that players can always understand:

- Why an outcome occurred

- Which system caused it

- How they could influence it in the future

Outcomes may feel surprising:

- Only if they are explainable after the fact

- Never arbitrary or unknowable

Trust is preserved through explanation, not simplification.

------------------------------------------------------------------------

**3. Alerts and Warnings**

Warnings are context sensitive and severity based:

- Early soft warnings for mild risks

- Escalating alerts for serious threats

- Critical alerts near irreversible outcomes

Players are never blocked from ignoring warnings:

- Free will is preserved

- The game respects player agency within system constraints

------------------------------------------------------------------------

**4. Automation vs Control**

Players may:

- Lock configurations

- Create presets

Automation:

- Is not required for MVP

- If implemented later, should be neutral in efficiency

- Will be a late game, post-MVP unlock

- Never replaces player decision making

------------------------------------------------------------------------

**5. Cognitive Load Management**

UI complexity philosophy:

- Simple on the surface

- Deep underneath

- Systems unlock visually and mechanically over time

This ensures:

- Early clarity

- Late game mastery

- Reduced overwhelm

------------------------------------------------------------------------

**6. Failure Visibility**

After failures, the UI should:

- Highlight root causes

- Suggest corrective actions

- Summarize results clearly

Historical graphs and deep analytics are optional enhancements, not required for MVP.

------------------------------------------------------------------------

**7. Anti-Deception Rules**

The game must never:

- Apply silent nerfs

- Change formulas without notice

- Hide penalties intentionally

Visible consistency is prioritized over perfect balance:

- Predictability builds trust

- Balance adjustments must be communicated

# System Spec 16: Adventurer Economy and External World Simulation

Status: Locked (v1)\
Scope: MVP with forward compatibility

## 1. Purpose

This system defines how adventurers exist in the external world, how they circulate between regions, how loot and death affect the economy, and how the dungeon interfaces with a living but bounded world.

## 2. World Model

Adventurers are finite and simulated at a high level. In MVP, they are represented as pooled counts per region and per level band.

## 3. Adventurer Lifecycle

Adventurers persist across dungeon runs, gain levels and gear, and may retire. Death removes them from the region with a short cooldown before reappearance elsewhere.

## 4. Loot and Economy

Extracted loot circulates abstractly. Merchant availability is fixed in MVP. No global supply saturation occurs.

## 5. Dungeon Competition

Dungeons do not compete for a single global pool. Competition is indirect through reputation and archetypes.

## 6. MVP Boundary

The world economy is a background approximation with visible effects only.

# System Spec 17: Save State, Offline Simulation, and Time Handling

Heat Structure Definitions

Heat Tier: Coarse band such as Peace, Notice, Concern.

Heat State: Numeric value within a tier.

Offline rule: Heat cannot change tiers offline. On login, heat state may shift within the tier bounds only.

Status: Locked (v1)\
Scope: MVP with forward compatibility

## 1. Time Model

Hybrid time model with event-driven steps. Offline uses summarized approximations.

## 2. Offline Simulation

Offline systems include mana, research, adventurer traffic, and Loot crafting may progress offline **only if** it is deterministic, consumes no premium currency, and does not unlock progression gates.\
Any crafting output that affects competitive systems, economy balance, or progression is marked **pending verification** and finalized only after online validation. Raids and permanent losses never occur offline.

Offline crafting cannot unlock research, complete contracts, grant leaderboard relevant outcomes, or bypass online restricted actions.

## 3. Heat Handling

**Heat Tier and State Definitions**\
Heat is composed of two layers:

- **Heat Tier**: A coarse band representing dungeon danger level (Peace, Notice, Concern, etc.).

- **Heat State**: A numerical value within a tier representing intensity inside that tier.

**Offline Heat Rules**\
While offline, heat does not advance through gameplay events and cannot change heat tiers.\
On login, accumulated offline pressure may adjust heat **within the current tier only**, bounded by the tier’s minimum and maximum state values.

For example, if the player enters offline mode at Heat State 5 within the Peace tier, offline rebound may move heat between State 0 and State 9, but it may not cross into Notice (State 10).\
Heat tier changes only occur through active, online gameplay events.

## 4. Save Rules

Saves occur on fixed intervals and key actions. Save scumming is prevented.

## 5. Anti-Abuse

Protections exist against clock manipulation and offline farming abuse.

## 6. MVP Boundary

Time handling is simulation-light with generous but capped progression.

# System Spec 18: Analytics, Telemetry, and Balance Instrumentation

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines what gameplay data is captured, how it is structured, and how it is used to measure retention, balance health, and the success of the core fantasy in MVP and beyond.

The guiding goal is to learn whether players return on day 1 and day 7, and whether the short session loop creates a reliable 'one more thing' compulsion centered on layout optimization, loot table tuning, and monster placement decisions.

2\. Instrumentation Principles

2.1 Minimal but decisive

Only log events that answer questions tied to a decision, a pressure shift, a progression milestone, or a failure state transition. Favor outcome logging plus state snapshots over logging every intermediate coefficient.

2.2 Player trust and privacy

Telemetry is tied to account sign in to support cloud saves and seasons. No personally identifying information is required for gameplay analytics.

2.3 Debuggability for small scale tests

MVP includes a developer only export bundle that captures the current save state summary, plus the last 50 run summaries, plus the recent event log, enabling fast diagnosis of tester feedback.

3\. Data Retention and Storage

3.1 Retention window

Telemetry is retained for 90 days in MVP. After 90 days, events are either deleted or aggregated into coarse summaries, such as retention cohorts and balance percentiles.

3.2 Offline buffering

While offline, events are buffered locally and uploaded when online. Upload uses idempotent event ids to prevent duplicate ingestion.

3.3 Cloud preferred, local fallback

Cloud collection is preferred from day one. If cloud collection is not available for a small friends and family build, local logs plus copy to clipboard export is acceptable.

4\. Definitions

4.1 Active time

Active time is wall clock time while the app is in foreground and not paused. A return day counts only if the player accumulates at least 60 seconds of active time that day.

4.2 Session

A session begins at app foreground and ends after 30 seconds of background time or a clean quit, whichever occurs first.

4.3 Run

A run is an adventurer party entering the dungeon and resolving until exit, wipe, or retreat. MVP does not include any player triggered simulation or test run button.

5\. MVP Success Metrics

5.1 Retention

Primary success metrics are day 1 retention and day 7 retention, using the active time definition above.

5.2 Session intent

Target session length is 5 to 10 minutes. The session should end with a clear next optimization hook, such as an improved tile layout, a revised loot table, or a rebalanced monster placement.

5.3 Tutorial completion

Tutorial completion for analytics is defined as the first time the player starts research.

6\. Funnel and Milestones

6.1 MVP funnel events

The primary funnel is: install, first run, first research started, first new floor unlocked, first offline summary shown.

6.2 Milestone definitions

First run is the first resolved adventurer attempt. First new floor is the first time the player unlocks an additional floor. First offline summary is the first return session where offline summary data is displayed.

7\. Event Taxonomy

7.1 Required events

Each event payload should include: player id, session id, timestamp, content version, save version, and a small state snapshot.

Session events: session_start, session_end.

Account events: account_created, account_signed_in, cloud_save_enabled.

Tutorial events: tutorial_step_complete, tutorial_skipped, recommended_next_step_clicked.

Economy events: mana_earned, mana_spent, reserve_changed.

Pressure events: heat_changed, heat_tier_changed, pressure_rebound_applied.

Progression events: floor_unlocked, room_built, room_modified, monster_placed, monster_upgraded, loot_table_edited, research_started, research_completed, research_completion_pending.

Run events: party_spawned, run_started, run_ended, room_resolved.

Offline events: offline_entered, offline_exited, offline_summary_shown, offline_summary_viewed.

Security events: clock_anomaly_detected, save_integrity_warning, offline_grant_disabled, cheat_flagged.

7.2 Run end payload

run_ended should capture: outcome (clear, partial, retreat, wipe), depth reached, rooms cleared, deaths, survivors, loot_generated_value, loot_extracted_value, heat_delta, mana_delta, and a coarse layout signature.

7.3 Layout signature

Layout signatures are coarse, for example counts of tile types and tags per room, plus key placements such as core adjacency and choke points. Do not store full grids in telemetry.

8\. Balance Red Flags and Alerts

8.1 Early heat spike

Trigger a balance alert if a player reaches the Concern heat tier before unlocking floor 2.

8.2 Heat not moving

Trigger a balance alert if total heat delta in the first hour is below a configurable threshold across a meaningful sample.

8.3 Dominant strategy detection

Compute strategy clusters across layout signatures, monster rosters, and loot table patterns. Flag clusters that exceed a configurable advantage threshold, such as median mana per hour or win rate exceeding the next best cluster by more than a set margin.

9\. KPI Computations

9.1 Economy KPIs

Track mana per hour, usable mana versus reserved mana ratio, reserve composition in advanced diagnostics, and average renovation frequency per hour.

9.2 Pressure KPIs

Track heat tier distribution over time, heat volatility (absolute delta per hour), and time to recover to Peace from Concern among players who recover.

9.3 Run KPIs

Track run completion rate, retreat rate, wipe rate, average depth, loot extracted per run, and research setback incidence driven by raid failures.

10\. Reporting Surfaces

10.1 In game developer view

MVP includes a hidden developer view for testers that can export the debug bundle, and can display basic retention counters, last three heat change causes, and last run summary.

10.2 Dashboard requirements

The analytics backend must support: cohort retention, per build comparisons, and simple filters by dungeon core level, floor count, and heat tier.

# System Spec 19: Content Pipeline and Data Authoring

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines how game content is authored, validated, exported, versioned, and loaded at runtime. The goal is fast balance iteration, safe live updates, and strong forward compatibility.

2\. Engine and Runtime

2.1 Engine

The project uses Unity.

2.2 Data driven runtime

Core gameplay data is loaded from externalized tables so that balance and content can change without code changes when feasible.

3\. Authoring Source of Truth

3.1 Master spreadsheet

A single master spreadsheet is the source of truth, with one tab per content domain: constants, rooms, tiles, monsters, loot items, loot tables, research nodes, events, and localization keys.

3.2 Solo authoring

In MVP, only the primary developer edits content. The pipeline still supports future collaborator workflows through validation and id stability.

4\. Export Format

4.1 Export target

The spreadsheet is exported to JSON for runtime loading. Export can be manual or scripted as a build step.

4.2 Schema

All tables use stable string ids. Localization uses the same ids or dedicated localization keys that map to glossary terms.

4.3 Content version

Every export produces a content version string that is embedded in the build and stored in saves.

5\. Runtime Loading and Caching

5.1 Load order

Load constants first, then foundational types (tiles, room archetypes), then monsters and loot, then research and events.

5.2 Save resolution

Saves store ids and player progress, not numeric balance snapshots. On load, ids are resolved against the current content tables so balance updates apply to existing saves.

5.3 Missing id handling

If an id referenced by a save is missing in the current content build, the game must either map it through an explicit migration table, or replace it with a safe fallback that does not break the economy.

6\. Room and Tile Granularity

6.1 Tile grid model

Dungeon construction is a modular tile grid. Tiles can host traps, monsters, and room modifiers, within placement rules.

6.2 Room instances

A room is an instance defined by a set of tiles. Room instance data includes a per instance loot table, trap lethality score, and monster threat score.

6.3 Difficulty estimate

Room difficulty is computed from trap lethality plus monster threat. This estimate is used to seed expected adventurer level band and to constrain player loot table choices.

7\. Loot Table Authoring Rules

7.1 Per room instance loot table

Each room instance owns its own loot table configuration. Two rooms with similar monsters may still differ due to levels, modifiers, and player tuning.

7.2 Player editability

The content pipeline defines the available loot entries and constraints. The player edits per room loot tables in game within those constraints.

8\. Build Validation and Linting

8.1 Linter requirement

A data linter runs before every build and can fail the build if validation rules are violated.

8.2 Must never ship broken

The linter must detect and block: circular research prerequisites, negative upkeep values, and loot value ranges that break the economy.

8.3 Additional recommended checks

Recommended checks include missing ids, duplicate ids, unreachable research nodes, and invalid probability sums in loot tables.

9\. Live Updates and Offline Rules

9.1 Offline allowed

The game is playable offline.

9.2 Offline restrictions

While offline: events, leaderboards, and purchases are unavailable; research cannot be started or completed.

9.3 Content freshness

When online, the client checks for content updates and applies them. The player should not be able to run seasonal or leaderboard features on outdated content.

10\. Tuning Workflow

10.1 Iteration speed target

A few minutes per balance change is acceptable. The workflow prioritizes correctness and safety over instant hot reload.

10.2 Constants tab

All key coefficients are centralized in a constants tab so balance tuning does not require editing multiple tables.

11\. Forward Compatibility

11.1 Migration tables

Support explicit migration tables that map old ids to new ids when content is renamed or split.

11.2 Deprecation policy

Content removal should be rare. Prefer deprecating entries and mapping them to safe alternatives rather than deleting outright.

# System Spec 20: Tutorial, Onboarding, and Unlock Sequencing

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines how the player is introduced to core systems, how systems are revealed over time, and how the first 10 minutes are paced to support retention and trust.

2\. Onboarding Philosophy

2.1 Soft guided

Onboarding uses optional hints and callouts. The player retains agency and is not forced into premium currency actions.

2.2 Skippable tutorial

The tutorial is skippable. If skipped, a recommended next steps panel persists until the player completes core actions once.

2.3 Trust first

Heat and reserve mana are visible immediately, to avoid surprise mechanics. Default tooltips remain plain language.

3\. MVP First Session Goals

3.1 Session length

MVP first sessions target 5 to 10 minutes.

3.2 First clever moment

The first intended clever moment is achieved through a layout change that meaningfully improves outcomes. In later releases, a monster swap can become the first clever moment when multiple families exist.

4\. System Visibility Rules

4.1 Always visible in MVP

Total mana, reserved mana, and heat are visible immediately.

4.2 Research visibility

Research is visible immediately but locked until the player meets unlock conditions. The UI signals that research is future depth and a reason to return.

4.3 Loot table visibility

Loot tables are visible, but editing is locked until the first successful adventurer run, so the player has context for what loot means.

5\. Unlock Sequencing

5.1 Strategic order

The first strategic choice is room layout, followed by monster roster selection for the room, followed by loot strategy, followed by research path selection.

5.2 Research unlock conditions

Research unlocks when the player has completed at least one successful run and has at least one eligible research entry, such as a starter node or an absorbed loot token threshold.

6\. Recommended Next Steps Panel

6.1 Persistence

A recommended next steps panel is shown until the player completes core actions: place or modify layout, complete a successful run, start first research, and view the first offline summary.

6.2 Content

The panel presents a short ordered list with one tap navigation to the relevant UI panels.

7\. Heat and Reserve Messaging

7.1 Heat explanation affordance

The heat meter includes an action that shows the last three causes of heat change in plain language.

7.2 Reserve display

In default view, reserve mana is displayed as a single number. Reserve breakdown by source appears only in advanced views.

8\. Failure Recovery Messaging

8.1 Trigger timing

Failure recovery messaging is shown only when the player is about to trigger a failure state.

8.2 Tone

Messaging emphasizes that failures are recoverable and clarifies what will pause or shut down first, without blaming the player.

9\. Monetization Guardrails in Onboarding

9.1 No forced premium spends

The tutorial never forces a premium spend.

9.2 Optional prompts timing

Optional premium prompts must not appear until research is unlocked and the player has started at least one research task.

10\. Advanced View Integration

10.1 Default versus advanced

Default tooltips are plain language. Advanced view is per panel and can show substituted formulas, keeping onboarding uncluttered.

# System Spec 21: Economy Sinks and Late Game Deflation Control

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines mandatory economic sinks and the deflation control levers that keep the game strategically interesting as output scales.

2\. Design Intent

2.1 Late game feel

Late game should feel powerful but still pressured. Players should always be managing tradeoffs, not idling into an unbounded optimal state.

2.2 Heat as risk driver

Heat is primarily a risk driver and limiter. It is not a required progression ladder.

3\. Growth Speed Control

3.1 Fill time guardrail

The economy must avoid reaching maximum mana capacity too quickly. A primary guardrail is a minimum fill time target, initially about 2 minutes, meaning the player should not fill from empty to full in less than this target during the stages where it applies.

3.2 Future scaling of the guardrail

At later points, the minimum fill time target can increase or shift to a different controlling variable to slow pacing as complexity rises.

4\. Mandatory Sinks

4.1 Monster upkeep

Monster upkeep is a reserved mana sink that scales with both monster level and monster count.

4.2 Expansion upkeep

Expansion upkeep is a persistent sink tied to dungeon size and depth.

4.3 Renovations

Renovations are an active sink tied to structural changes to layout and room typing.

5\. Expansion Upkeep Scaling

5.1 Components

Expansion upkeep scales using three components: tiles influence cost per room, rooms influence cost per floor, and floors increase costs using an escalating coefficient per floor index.

5.2 Tuning knobs

Tuning knobs include per tile base upkeep, per room multiplier, and per floor escalation coefficient.

6\. Renovation Rules

6.1 Cost triggers

Renovation costs trigger when moving tiles or swapping room types.

6.2 Loot table edits

Loot table edits do not trigger renovation costs. Loot table edits change reserve cost through the loot system's reserve computation.

6.3 Experimentation grace window

Renovations are reversible without cost if the player undoes the change within 30 seconds. This supports experimentation without punishing curiosity.

7\. Deflation Control Levers

7.1 Primary lever

The primary stabilizer is increasing upkeep curves, especially monster upkeep scaling.

7.2 Soft caps

Soft caps are preferred. Output can continue to grow but should do so with diminishing efficiency and increasing management cost.

7.3 Layout stacking control

If needed, introduce diminishing returns for repeated identical tile patterns or repeated identical monster stacks within a room, but only as a secondary lever.

8\. Risk Reward at Higher Heat

8.1 Positive pressure

Higher heat implies higher tier adventurers, which can yield better loot odds and higher value extraction when successful.

8.2 Counterbalancing risk

Higher heat also increases raid risk and the probability of outcomes that set back research progress.

9\. Player Fairness and Escape Hatches

9.1 Intended escape hatches

When players feel stuck, they should be able to rebuild layout, change monster roster, and refocus research. Progress should not require pushing into high heat as the only solution.

9.2 Clarity

The UI should make clear which sink is limiting progress, for example expansion upkeep, monster upkeep, or lack of usable mana due to reserves.

# System Spec 22: Meta Progression, Prestige, and Seasonal Resets

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines long term progression that exists outside the primary dungeon, including seasons, optional prestige style loops for secondary content, and permanent collection goals.

2\. Core Stance

2.1 No primary dungeon resets

The primary dungeon never prestiges and never resets as a core progression method.

2.2 Permanent collection goals

Long term goals include dungeon identities, codex completion, and permanent unlocks that persist across all modes.

3\. Seasons Overview

3.1 Separate season dungeon

Seasons run in a separate season dungeon that resets and starts from the same baseline for all players, either true zero or a predefined starting state.

3.2 Variable season length

Season length is variable and is defined per season by live operations planning.

3.3 Unlock requirement

The season dungeon unlocks only after the player reaches a milestone in the primary dungeon, such as reaching floor 5.

3.4 Participation constraint

Players can participate only in the current season dungeon. If no season is running, the season dungeon is unavailable.

4\. Seasonal Content Strategy

4.1 Live beta for new content

Seasons can introduce new monster families and unique loot as a live beta test before they enter the main game.

4.2 Rule modifiers

Seasons can apply rule modifiers such as altered research times, altered mana caps, or targeted buffs and nerfs for specific monster families.

4.3 Data driven design

Season rules and content are designed to be data driven so the same base code can support many seasons through table edits.

5\. Seasonal Rewards

5.1 Season pass structure

Season rewards are delivered through a free season pass and a premium season pass.

5.2 Reward philosophy

Rewards should emphasize cosmetics, convenience within the season, and limited cross mode impact. Any cross mode rewards must be carefully capped to avoid invalidating primary dungeon balance.

6\. Seasonal Leaderboards

Season leaderboards reset each season and are distinct from primary dungeon leaderboards.

7\. Sub Dungeon Prestige Direction

7.1 Optional prestige for sub dungeons

Sub dungeons may support prestige style loops in the future.

7.2 Prestige outcomes

Prestige in sub dungeons primarily unlocks new sub dungeon types or new modifiers, rather than raw permanent power multipliers.

8\. Anti Fragmentation Guidance

Season participation should be optional and should not be required to enjoy the primary dungeon. The game should avoid forcing players into season play to keep up with core progression.

# System Spec 23: Player Inventory, Storage, and Item Lifecycle

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines what exists as an item, how loot is represented in MVP, how absorption and research gates work, and how items eventually transition into the external world economy.

2\. MVP Representation Model

2.1 Loot as unlocks and parameters

In MVP, loot drops are not stored as individual item instances in player inventory. Loot functions primarily as unlocks and parameters that shape loot tables and crafting options.

2.2 Materials as counters

Materials are represented as numeric counters, categorized by tier and type.

2.3 No storage pressure for loot drops

Loot drops do not consume storage capacity because the dungeon conjures items on demand from its loot tables.

3\. Absorption and Sample Tokens

3.1 Extracted sample tokens

When a loot item is successfully extracted by surviving adventurers, the player can absorb it into research as an extracted sample token. The research UI shows progress as 1 of X tokens collected.

3.2 Survival requirement

Absorption requires successful extraction by surviving adventurers. Loot that is generated but not extracted cannot be absorbed.

3.3 Death requirement and tradeoff

Absorption also requires at least one adventurer death during the relevant run. This creates an explicit tradeoff: aggressive dungeons increase absorption rate, while safer approaches can gain tokens through lower heat contracts that reward research progress.

4\. Research Gating

4.1 Research eligibility

A loot item becomes researchable only when the player has collected enough extracted sample tokens for that item.

4.2 Research costs

Research consumes time and reserve mana. Research start and completion require online connectivity, with offline completion pending until the next online check.

5\. Loot Pool Unlocking

5.1 Unlock versus usage cost

Researching a loot item does not increase reserve cost by itself.

5.2 Reserve cost occurs on assignment

Reserve cost changes only when the player adds that researched loot item to a specific room's loot table.

6\. Item Improvement Research

After an item is researched and unlocked, additional research paths can improve it, such as quality variants, enchant hooks, or efficiency modifiers. These improvements should remain data driven.

7\. Prohibited Actions in MVP

Items cannot be destroyed, sold, or sacrificed for mana in MVP.

8\. Long Term Item Instances and the External Economy

8.1 External economy entry point

True item instances enter the external world economy only when adventurers successfully leave the dungeon with their loot.

8.2 Export crafting

Players can craft specific items for export in later releases. Crafted exports interact with the kingdom and global market simulation, influencing prices, scarcity, and demand.

9\. Save Data Requirements

Saves store: material counters, researched loot ids, sample token counts per loot id, loot table configurations per room instance, and any item improvement research states.

# System Spec 24: Social and Competitive Systems

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines leaderboards and future competitive play features, including data normalization, reset policies, visibility rules, and anti cheat considerations.

2\. MVP and Near Term Stance

Leaderboards are the primary social feature. Other social systems are deferred, but the architecture should not block future expansion.

Competition surfaces are always visible, even if participation is optional or gated.

3\. Leaderboard Types and Scopes

3.1 Scopes

Leaderboards support global, regional, and friends scopes.

3.2 Primary versus season

Primary dungeon leaderboards are persistent and do not reset. Season dungeon leaderboards reset each season.

3.3 Separate sets

Primary dungeon leaderboards and season dungeon leaderboards are separate.

4\. Leaderboard Metrics

Supported metrics include: mana per hour, raid survival rate, most floors, highest danger rating, highest safety rating, dungeon rating, and most survivors.

Mana per hour is measured as a rolling 24 hour average to reduce cheating and reduce spike based exploits.

5\. Filters and Bracketing

Leaderboards support filtering by dungeon core level, by account age, and by overall ranking.

Bracketing is available to improve fairness across progression tiers.

6\. Submission and Validation

6.1 Online requirement

Leaderboard submission requires online connectivity.

6.2 Data submitted

Submitted data includes: player id, content version, build version, relevant metric value, and a compact summary of supporting state such as dungeon core level and floor count.

6.3 Server side validation

The server validates submissions using plausibility checks, time windows, and consistency with recent telemetry, especially for rolling 24 hour computations.

7\. Seasonal Competitive Loop

Season dungeons create an equal baseline competition where players start from the same initial state. Seasons can also serve as a live beta test platform for new content.

8\. Future Feature: Dungeon Versus Dungeon Challenge

8.1 High level concept

Two players challenge each other in synchronous play. Each player chooses how many monsters to send as attackers and how many to keep as defenders.

8.2 Win condition

The winner is determined by who damages or destroys the opposing dungeon core more effectively within the match rules.

8.3 Anti meta lever

Sending too many attackers weakens defenses. A smaller elite attacking force can defeat a weakened defense, discouraging all in attack metas.

8.4 Rewards

Rewards can include leaderboard rank, dungeon rating, and limited loot or research opportunities, such as unlocking access to items or monsters the player does not yet have.

9\. Gating and Progression

Competitive features should be gated behind progression milestones and should not destabilize the primary dungeon economy.

# System Spec 25: Security, Anti cheat, and Economy Integrity

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines the threat model, authority boundaries, online requirements, integrity checks, and responses for cheating and exploitation.

2\. Threat Model

2.1 Primary threats

The highest priority threats are clock manipulation and memory editing.

2.2 Protected assets

Premium currency and time based systems are protected at the highest priority. Mana is important but is treated as a soft currency with secondary protection.

3\. Authority Split

3.1 Online required actions

Online connectivity is required for purchases, leaderboards, events, research start, and research completion.

3.2 Offline allowed actions

The game is playable offline, but while offline: events, leaderboards, purchases, research start, and research completion are unavailable.

4\. Research Integrity Rules

4.1 Offline progress with pending completion

If research is started online, progress continues while offline. If the timer would complete while offline, completion is marked pending and is finalized only when the client next goes online.

4.2 Completion validation

On completion, validate that prerequisites are still met, that reserve conditions were satisfied at start, and that time progression is plausible.

5\. Heat and Offline Rules

5.1 Heat frozen offline

Heat does not change while offline.

5.2 Tier bounded rebound

On login, apply a pressure rebound that can move heat up or down within the current tier bounds only. Offline alone cannot move the player across tiers.

5.3 Cross tier changes require play

After login, active play can move heat across tiers through normal heat change rules.

6\. Detection Signals

6.1 Clock anomalies

Detect time discontinuities, unusually large offline durations, repeated backwards time moves, and unrealistic completion times for timed systems.

6.2 Memory editing indicators

Detect impossible currency deltas, impossible reserve states, and impossible run outcomes relative to content constraints.

6.3 Save integrity

Use save versioning and checksums to detect tampering. Maintain a monotonic save sequence to reduce rollback abuse.

7\. Response Policy

7.1 MVP response

When cheating is detected, flag the account for review in a dashboard, notify the player, and disable offline grants.

7.2 False flag handling

Provide an admin override path for testers to restore offline grants if a false flag occurs.

7.3 Escalation

Repeat offenders can receive stricter restrictions, such as disabling leaderboard participation or requiring online play for all progression.

8\. Cloud Saves and Conflicts

8.1 Early cloud preference

Cloud saves are preferred early to support cross device play and reduce local tampering.

8.2 Conflict resolution

If two devices produce conflicting saves, the most recent timestamp wins, subject to plausibility checks.

9\. Server Requirements Summary

The server must support: purchase validation, premium currency authority, research start and completion endpoints, leaderboard submission validation, cheat flag storage, and basic account management.

# System Spec 26: Accessibility and Cognitive Load Targets

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines accessibility targets, information density rules, and the advanced view approach that balances depth with clarity.

2\. Target Audience

The default audience includes idle veterans, strategy players, city and base builders, and LitRPG readers. The UI must support quick checks and deep dives.

3\. Default UI Information Budget

3.1 Key metrics

The main HUD shows 3 to 5 key metrics. MVP candidate set includes: total mana, usable mana, reserve mana, mana per hour, and heat state.

3.2 Panel details

Additional metrics, such as heat delta per hour and adventurer throughput, live in per system panels and advanced views.

4\. Advanced View Model

4.1 Per panel toggle

Advanced view is enabled per panel, not as a single global toggle.

4.2 Ordering

In advanced view, show the final formula with substituted values first, then show a plain explanation of why the value changed.

4.3 Copy support

Advanced view includes a copy formula action for debugging and balance work.

5\. Accessibility Requirements

5.1 Text scaling

MVP supports text size scaling across major UI surfaces.

5.2 Colorblind safe heat

Heat states must be colorblind safe. MVP uses text plus color. Icons and shapes can be introduced later as polish.

5.3 Interaction targets

Touch targets and scrolling behavior must support comfortable one handed portrait play.

6\. Cognitive Load Guardrails

6.1 Plain language defaults

Default tooltips use plain language and avoid dense formulas.

6.2 Progressive disclosure

Numbers and breakdowns should appear only when the player opts into a panel or advanced view.

6.3 Change explanations

For key metrics, provide short change explanations, such as last three causes for heat, without forcing the player to open deep panels.

# System Spec 27: Localization and Text System

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines the localization architecture, glossary governance, text key conventions, number formatting rules, and UI constraints needed to ship English and Japanese and expand to additional languages safely.

2\. MVP Language Strategy

2.1 English and Japanese readiness

MVP is architected and ready to ship with English and Japanese. A language toggle is available in English speaking regions. Japan release can be concurrent or shortly after the English release.

2.2 Expansion ready

The text system must support adding additional languages without refactoring core UI or content ids.

3\. Text Key Architecture

3.1 No hard coded player facing strings

All player facing strings use stable localization keys. Keys are not derived from display text.

3.2 Key conventions

Keys follow a stable namespace style, such as ui, system, tooltip, item, monster, and lore.

3.3 Content id separation

Content ids, such as monster ids and loot ids, are separate from localization keys, but can map to them in tables.

4\. Glossary Driven Terminology

4.1 Glossary authority

A glossary defines core terms that must remain consistent across UI, tooltips, and tutorials.

4.2 Ownership and workflow

The primary developer owns final glossary decisions. Helpers or paid translators can propose translations marked needs review.

4.3 Term translation policy

Terms should be fully localized when that matches industry standards in the target language. Mana should be localized if that is the standard practice in Japanese game localization.

5\. Tone and Content Rules

5.1 Translation friendly default

Default UI and system text uses translation friendly tone.

5.2 Humor placement

Humor and slang are reserved for collectible lore and optional flavor content.

6\. Number Formatting

6.1 Default compact notation

Default views use compact notation, such as 1.2K, with locale aware separators and abbreviations.

6.2 Detailed explicit numbers

Detailed views show explicit numbers.

6.3 Japanese formatting

Japanese uses Japanese friendly units and formatting rules, not forced English abbreviations.

7\. UI Layout Constraints

7.1 Text expansion support

UI must accommodate text expansion, including longer languages, to reduce rework later.

7.2 Line break and truncation rules

Critical gameplay values must not be hidden by truncation. Use wrapping and responsive layout where possible.

8\. Implementation Requirements

8.1 Placeholder and fallback behavior

If a localization key is missing in a language pack, fall back to English and log a missing key event for internal diagnostics.

8.2 Content pipeline integration

Localization keys are stored alongside content tables and exported with the content pipeline so content and text remain aligned.

# System Spec 28: Save Data Model, Versioning, and Migration

Layout Editing Save Rule

Tile placement or movement while in edit mode always triggers an immediate save.

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Primary dungeon, season dungeon, sub dungeons, account layer |
| Primary goal | Prevent data loss and integrity issues while supporting offline play |
| Non goals | Implement full backend architecture details beyond interfaces |

## 1. Purpose

Define the save contract, authoritative sources of truth, versioning rules, migration behavior, and anti abuse safeguards. This spec ensures that balance updates apply to existing saves, offline play remains viable, and competitive features remain trustworthy.

## 2. Design goals

- Players can play as a guest, but progress can be lost until an account is linked.

- Separate saves exist for primary dungeon, season dungeon, and each sub dungeon.

- Saves store stable IDs and player progress, not tuned numeric balance values.

- Server authority is used for time sensitive and competitive systems.

- Rollback and clock manipulation attempts degrade privileges rather than corrupt data.

- Migration is safe, deterministic, and does not require manual player repair for common cases.

## 3. Save partitions

Each account has separate save partitions:

- Primary dungeon save

- Season dungeon save (only one active season dungeon at a time)

- Sub dungeon saves (one per sub dungeon instance)

- Account layer (entitlements, premium currency, settings)

## 4. Sources of truth

Authority model by category:

- Primary dungeon state: server authoritative whenever online, client cached for offline play. Reconnect uses server as the merge authority.

- Season dungeon state: server authoritative always.

- Premium currency: server authoritative only.

- Research timers: server stores start time, duration, and completion status. Client may display progress offline, completion finalizes only after server confirmation.

- Leaderboards placement: server authoritative only.

## 5. Save cadence

Save writes occur at fixed intervals plus key actions.

### 5.1 Interval saves

- Interval: every 30 seconds during active play.

- Also save on app backgrounding and app termination callbacks when available.

### 5.2 Key actions that always save

- Monster placement or monster level up

- Loot table change

- Starting research

- Claiming research completion

- Floor unlock

- Any premium currency spend

- Tile placement or move while in edit mode

  **Layout Editing Save Behavior**\
  While the player is in dungeon edit mode, any tile placement or movement is considered a key action and triggers an immediate save.\
  This ensures layout experimentation cannot be silently lost due to crashes or interruptions.\
  Outside of edit mode, interval saves still apply for performance reasons.

### 5.3 Player feedback

Autosave status is invisible in MVP. Errors use clear messaging only when required.

## 6. Cloud saves and conflicts

- One save per account in early releases.

- Most recent timestamp wins when selecting between competing versions.

- If two saves are within 5 minutes, show a conflict prompt that explains the resolution logic.

- Multi device protection: if a device reconnects after another device has played online, the reconnecting device must download the server version. There is no user choice.

## 7. Guest to account linking

- Guest saves remain local only until the player links an account.

- Linking flow warns about the risk of progress loss if the device is lost or reinstalled.

- Once linked, cloud saves and telemetry identity become active.

## 8. Versioning

- Each save partition stores: save_version, content_version, and last_write_timestamp.

- Content IDs are never renamed. Deprecated IDs remain resolvable via a mapping table.

- If an ID is removed, the system either replaces with a fallback equivalent or marks the entity as disabled requiring player action, depending on the content class.

## 9. Migration and missing content behavior

### 9.1 Deprecated ID mapping

- Deprecated IDs map to a replacement ID of the same class.

- Mapping is data driven and validated in the build linter.

- Mapping applies on load, before simulation begins.

### 9.2 Removed content handling

- Rooms or tiles: replace with a fallback tile or empty tile, then mark for player review if the replacement changes function.

- Monsters: replace with fallback within the same family tier when possible, otherwise disable the slot.

- Loot items: replace with nearest tier fallback, or mark the loot entry disabled and require player removal from the loot table.

- Research nodes: preserve progress if the node still exists, otherwise mark the branch as deprecated and map to a replacement node when available.

## 10. Integrity safeguards

### 10.1 Rollback detection

- Detect save rollback if save_version decreases, timestamps move backward, or the server reports a newer authoritative state.

- On detection: force cloud pull, disable offline grants, and lock research until online verification completes.

### 10.2 Admin and testing slots

- Multiple save slots are available for testing accounts.

- Admin role can create and switch slots via backend tooling.

- Production players use a single slot.

## 11. Telemetry hooks

- save_write (partition, reason, duration_ms, success)

- save_load (partition, save_version, content_version, success)

- save_conflict_detected (delta_seconds, resolution)

- rollback_detected (reason, action_taken)

- migration_applied (mapping_count, disabled_count)

## 12. MVP constraints

- No visible autosave indicator.

- Minimal conflict prompts, only when within the 5 minute window.

- Guest mode supported, but not guaranteed across reinstall.

- Admin save slot support limited to internal testers.

## 13. Open questions

None.

# System Spec 29: Time Model and Tick Resolution

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Simulation time, ticks, offline behavior, anomaly detection |
| Primary goal | One consistent time contract across mana, upkeep, heat, and research |

## 1. Purpose

Define a unified time model for simulation and progression. This spec sets tick rates, offline handling, and anomaly detection. It ensures that the game is playable offline while protecting time based systems from manipulation.

## 2. Time sources

- When online: server time is the authoritative wall clock.

- When offline: local time is used for presentation and offline grants.

- App in background is treated as offline for simulation purposes.

## 3. Tick resolution

### 3.1 Mana generation and upkeep

- Tick rate: every 10 seconds during active play.

- Each tick applies: mana generation, upkeep reservations, and any interval based housekeeping.

### 3.2 Heat updates

- Heat changes during active play are event driven only.

- Heat events include: adventurer deaths, loot extraction outcomes, raid outcomes, and other explicitly defined events.

### 3.3 Research progress

- Research uses a continuous timer.

- Start and completion require online verification.

- Offline progress may exceed the duration, but completion remains pending until online confirmation.

## 4. Offline behavior

### 4.1 Mana offline accumulation

- Offline mana uses a single formula grant based on last known mana per hour.

- Grant is clamped to mana capacity.

- There is no offline time cap for mana credit, since the clamp limits output.

### 4.2 Heat offline behavior

- Heat is frozen while offline.

- On login, heat may rebound within the current tier based on offline pressure.

- Offline rebound cannot cross heat tiers.

- Crossing tiers requires active play after login.

### 4.3 Research offline behavior

- Research progress continues offline.

- Completion is pending until the next online check.

- Offline summary must show when research is pending completion.

## 5. Offline summary UX

- Show mana storage full when clamped to capacity.

- Show research completion pending when progress exceeded duration but completion is not finalized.

- Show any heat rebound applied within tier bounds.

## 6. Time acceleration rules

- No player driven time acceleration mechanics are allowed, except premium time skips.

- Premium time skips require online verification per the security spec.

## 7. Time anomaly detection

- Any observed clock jump above 10 minutes is suspicious.

- Anomalies are handled consistently across mana, research pending completion, and season timers.

- When an anomaly is detected, trigger integrity actions defined in the security spec.

## 8. Telemetry hooks

- offline_grant_calculated (offline_seconds, mana_granted, clamped)

- offline_summary_viewed (mana_full, research_pending, heat_rebound_applied)

- time_anomaly_detected (delta_seconds, action_taken)

- tick_update (ticks_processed, duration_ms)

## 9. MVP constraints

- Offline mana is approximation only, not full tick simulation.

- Heat remains frozen offline with tier bounded rebound at login.

- Research completion always requires an online confirmation.

## 10. Open questions

None.

# System Spec 30: Formula Framework and Modifier Stacking Rules

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Economy formulas, modifiers, stacking order, caps, advanced view |
| Primary goal | Consistency and predictability across all systems |

## 1. Purpose

Define the standard format for modifiers and the stacking order used by all formulas. This spec ensures that balance changes are predictable, advanced view explanations remain accurate, and event rules can be applied safely.

## 2. Modifier sources

- Research

- Heat state

- Dungeon identity

- Room tags

- Monster family traits

- Seasonal rules

- Contracts

- Sub dungeon modifiers

## 3. Standard modifier types

- Additive flat: adds or subtracts a fixed value

- Additive percent: adds percent in a bucket that is summed, for example plus 10 percent plus 20 percent equals plus 30 percent

- Multiplicative percent: multiplies by a factor, for example times 1.10

- Clamp: min and max bounds applied after stacking

- Soft cap: diminishing returns curve applied after stacking but before rounding

## 4. Stacking philosophy

All systems use a layered stacking model with the same bucket order.

## 5. Bucket order

1.  Base value computation (including per entity base stats and level scaling inputs)

2.  Heat layer (state effects and heat linked multipliers)

3.  Research layer (unlocks and numeric modifiers)

4.  Event or season layer (world event modifiers, season dungeon overrides where applicable)

5.  Clamps and soft caps

6.  Final rounding and display formatting

## 6. Consistency rule

- No per system exceptions to stacking order in MVP.

- If a future exception is required, it must be declared explicitly in that system spec and reflected in advanced view.

## 7. Soft caps and diminishing returns

### 7.1 Where soft caps apply

- Mana per hour

- Loot odds

- Heat reduction effects

### 7.2 Layout stacking diminishing returns

- Diminishing returns are based on threat concentration within a room.

- Threat concentration is computed from monster threat plus trap lethality density.

- The curve is smooth, meaning output still increases but at a slower rate.

Upkeep curves should scale reasonably with progression and are not intended to hard cap play.

## 8. Advanced view requirements

- Show the final formula with substituted values.

- Show modifier breakdown by source and by layer.

- Do not show a delta explanation since last run in MVP.

## 9. Telemetry hooks

- formula_evaluated (formula_id, result_value, layer_summary_hash)

- advanced_view_opened (panel_id, formula_count)

## 10. MVP constraints

- One global stacking order across all systems.

- Layer reporting must match formula evaluation.

- Soft caps are used instead of hard caps where possible.

## 11. Open questions

None.

# System Spec 31: Dungeon Identity and Theming Resolution

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Identity detection, effects, interactions with seasons and heat |
| Primary goal | Make the world respond to dungeon style without direct player toggles |

## 1. Purpose

Define how dungeon identity is derived from the dungeon state and how identity influences the world. Identity is an emergent classification that shapes attraction and loot distribution without directly altering numeric multipliers to mana or loot value.

## 2. Scope

- Primary dungeon has a single identity state.

- Season dungeons have their own identity state.

- Sub dungeons have their own identity state.

## 3. Identity generation model

Identity is derived from the observed dungeon composition and behavior, including:

- Layout patterns and theme tags

- Monster families and roster composition

- Trap density and lethality profile

- Historical outcomes such as deaths, retreats, and extraction rates

- Heat behavior trends over time

## 4. Identity change behavior

- Identity has no direct switch, token, or respec action.

- Identity changes naturally as the underlying dungeon state changes.

- Identity has no cooldown, but changing identity requires meaningful renovations and time for perception to update.

- Perception update is modeled as a smoothing window so identity does not flip instantly.

## 5. Identity effects

### 5.1 What identity can change

- Loot distribution bias toward certain tags and themes, without changing numeric loot multipliers directly

- Heat behavior shifts, such as how quickly pressure builds within tier bounds

- Adventurer attraction shifts, such as party types, motivations, and traffic composition

### 5.2 What identity cannot change

- Identity does not modify room difficulty estimates.

- Identity does not apply numeric multipliers to mana generation.

- Identity does not apply numeric multipliers to loot value.

## 6. Tradeoffs

Identities are tradeoffs. For example, an aggressive identity may increase the rate of loot absorption opportunities and improve quality distributions, while also attracting stronger parties and raising risk to the core.

## 7. Interactions

### 7.1 Seasons

- Seasons override identity while active for the season dungeon.

- Identity effects are not applied inside season dungeon rule sets unless an event explicitly opts in.

### 7.2 Events and contracts

- World events may reference identity to target effects, for example increasing traffic for certain identities.

- Contracts may reward building toward an identity profile without granting direct identity buffs.

## 8. UI requirements

- Show current identity label and a short plain language description.

- Show contributing factors in advanced view, for example top tags and roster composition influence.

- Communicate that identity is descriptive, not a direct player toggle.

## 9. Telemetry hooks

- identity_changed (old_id, new_id, confidence, smoothing_window)

- identity_influence_summary (top_tags, top_rosters, traffic_profile)

## 10. MVP constraints

- Identity is informational and world reactive, not a player selected build choice.

- No direct numeric multipliers from identity in MVP.

## 11. Open questions

None.

# System Spec 32: Event Framework and Rule Overrides

MVP Event Clarification

Event delivery exists in MVP but may return no active events.

*Dungeon Builder, locked design specification*

| Status       | Locked                                               |
|--------------|------------------------------------------------------|
| Scope        | World events, seasons, overrides, lifecycle, rewards |
| Primary goal | Reusable data driven events that safely modify rules |

## 1. Purpose

Define the framework for limited time world events and seasons. This spec defines rule override behavior, priority order, offline handling, reward timing, and the season pass purchase rules.

**MVP Scope Clarification**\
For MVP, the event framework and configuration delivery mechanism exists, but no live world events are required to be active.\
The system may return “no active events” by default.\
Regular world events and seasonal content are considered post MVP live operations features.

## 2. Event types

- World events: limited time modifiers that can affect the primary dungeon economy.

- Seasons: dedicated season dungeon experiences with unique rules, leaderboards, and season pass progression.

## 3. Rule application priority

When multiple rule sources apply, evaluate them in this order:

7.  Base game rules

8.  Heat rules

9.  Research rules

10. Event or season rules

11. Dungeon identity rules

## 4. Override mechanics

### 4.1 World events

- World events are modifiers that stack on top of existing systems.

- World events may affect primary dungeon traffic, loot demand, or contract availability.

- Example: war increases demand for health potions, increasing traffic to dungeons that produce them.

### 4.2 Seasons

- Seasons use a separate season dungeon save.

- Season rules often override, replacing parts of base rules for the season dungeon.

- Season rules are configured to be reusable via data changes, not code changes, for most seasons.

## 5. Lifecycle and verification

- If the player is offline during an event start or end, event state changes apply only after online verification.

- World events and seasons rely on server time for activation windows.

- Event content is primarily data driven.

## 6. Run boundary rule changes

### 6.1 Season dungeons

- A run in progress completes under the rules active when the run started.

- The next run uses the updated rules.

### 6.2 Primary dungeon world events

- Apply changes on the next online verification.

- Do not retroactively change outcomes of runs already resolved.

## 7. Leaderboards and rewards

- Primary dungeon leaderboards do not reset.

- Season dungeon leaderboards reset per season.

- Season leaderboard rewards grant at season end only.

- Season pass includes milestone rewards during the season.

## 8. Season pass purchase rules

- Season pass is purchasable only while the season is active.

- Each season has its own season pass. Premium track requires purchase each season.

- Purchases require online verification.

## 9. Data driven configuration

- Events define: start and end times, rule modifiers or overrides, targeted systems, and reward definitions.

- Events can reference loot tags, identities, and traffic modifiers.

- Configuration is validated by the data linter before release.

## 10. Telemetry hooks

- event_state_changed (event_id, state, verified)

- season_started (season_id)

- season_ended (season_id)

- season_pass_progress (tier, claimed)

- leaderboard_snapshot_submitted (board_id, value, bracket)

## 11. MVP constraints

- Event system should support a small number of concurrent events.

- Most events are configured by data, with minimal new code per event.

- Offline event changes are applied only after online verification.

## 12. Open questions

None.

# System Spec 33: Build, Release, and Environment Strategy

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Build environments, release cadence, feature flags, version support policy, emergency response |
| Primary goal | Enable fast iteration and safe live control without contradicting offline and security rules |
| Invariants referenced | INV-01, INV-04, INV-05, INV-08, INV-09 |

## 1. Purpose

Define the environments and release practices used to ship safely. This includes feature flags, data hotfixes, version support rules, and emergency response patterns.

## 2. Environments

- Dev environment: for daily development and local testing.

- Prod environment: for public releases.

- Test environment is strongly preferred and should be fully separate from Prod when available.

### 2.1 Data isolation rules

- Dev and Test data must never mix with Prod data.

- Admin actions and cheat reviews in Test must not affect Prod accounts.

- Prod uses strict access controls for admin tooling.

## 3. Feature flags

Feature flags allow enabling or disabling features without a new client build.

### 3.1 Flag coverage

- Seasons

- World events

- Monetization

- Experimental balance tables

### 3.2 Flag authority

- Server driven flags are required for anything that impacts economy, competition, or purchases.

- Local flags are allowed only in Dev builds for debugging and iteration.

## 4. Release cadence

- MVP updates target a weekly cadence.

- Balance and configuration hotfixes should be possible via data tables without requiring a client update.

## 5. Version support policy

- A client build may remain active for 7 days after a newer build is released.

- After the support window, the client becomes view only.

- View only mode allows inspection of the dungeon state but prevents actions that would change state.

### 5.1 View only scope

- Allow viewing layouts, inventories, research status, and logs.

- Block simulation altering actions, including layout edits and any economy actions.

- Block all online required actions by definition.

## 6. Emergency response

- If an exploit is discovered, the preferred response is to stop future gains.

- Do not roll back prior gains in MVP unless required for integrity.

- Exploit response should be implemented as clamps or validation on the server side where possible.

## 7. Telemetry hooks

- build_version_seen (version, environment)

- feature_flags_applied (flag_hash, environment)

- client_blocked (version, reason, mode)

- hotfix_applied (table_id, version)

## 8. MVP constraints

- Weekly release cadence is a target, not a promise.

- Feature flags and table hotfixes must not break offline play rules.

- View only mode must clearly explain why actions are blocked.

## 9. Open questions

None.

# System Spec 34: Backend Services and API Contract

Telemetry Handling

Telemetry uploads are buffered and retried idempotently.

Strict acknowledgment applies only to economy critical mutations.

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Managed services, required endpoints, retry rules, offline rules, admin tooling |
| Primary goal | Support save integrity, security, and data driven updates with minimal custom backend code |
| Invariants referenced | INV-01, INV-04, INV-05, INV-08, INV-10 |

## 1. Purpose

Define the backend services and API behaviors required by the game. The system should prefer managed services and only build custom endpoints where needed for security and game specific rules.

## 2. MVP required services

- Authentication

- Telemetry ingestion

- Admin dashboard

- Event configuration delivery (minimal, can return no active events)

- Cloud saves when feasible, otherwise local saves with account upgrade path

### 2.1 Not required at MVP launch

- Leaderboards are not required at MVP launch, but interfaces should be stubbed for future use.

## 3. Managed first approach

- Prefer managed services for authentication, storage, and telemetry.

- Custom services should be limited to: save validation rules, research verification, event state timing, and admin controls.

## 4. API behavior

### 4.1 Reliability

- All server calls must be strictly acknowledged with retries.

- Use idempotency keys for write operations, including purchases and save uploads.

- Retries must not duplicate purchases or premium spends.

### 4.2 Timeouts

- Client should fail gracefully after 3 seconds for non critical calls.

- Critical calls may show a blocking spinner with a clear cancel option.

### 4.3 Telemetry Exceptions

- Telemetry uploads are not economy critical mutations and do not require strict synchronous acknowledgment.

- Telemetry events may be buffered locally, retried idempotently, and uploaded opportunistically when online.

- Strict acknowledgment and retry guarantees apply only to economy critical actions such as purchases, research start or completion, save commits, and leaderboard submissions.

## 5. Offline tolerance

The following endpoints must hard fail while offline:

- Purchases

- Research start

- Research completion

- Event verification

Offline actions that require these endpoints must show explicit educational messaging.

## 6. Event configuration delivery

- The client fetches active event and season configuration after authentication when online.

- If offline, the client uses the last verified configuration and marks it as unverified.

- Event state changes apply only after online verification.

## 7. Leaderboard submissions (future behavior)

- If leaderboard submission occurs while offline, queue locally.

- Submit on next verified online session.

- Discard queued submissions that are stale past a configured window.

## 8. Admin tooling

- Admin UI supports reviewing cheat flags, granting restore overrides, managing test save slots, and editing live event data.

- All admin actions must be logged for audit purposes.

## 9. Telemetry hooks

- api_call (endpoint, duration_ms, success, retry_count)

- api_fail_graceful (endpoint, reason)

- event_config_fetched (verified, config_version)

- admin_action_logged (action_type, actor_id)

## 10. MVP constraints

- Keep custom backend code minimal.

- Security first for premium currency and time based verification.

- No leaderboards required at MVP launch.

## 11. Open questions

None.

# System Spec 35: Error Handling, Player Messaging, and Trust

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Error categories, messaging patterns, localization ready phrasing, trust building |
| Primary goal | Prevent confusion during offline, verification, and integrity edge cases |
| Invariants referenced | INV-01, INV-02, INV-03, INV-08, INV-10 |

## 1. Purpose

Define error handling and player messaging standards. Messaging must be explicit and educational, localized where applicable, and should avoid blocking unrelated gameplay.

## 2. Messaging philosophy

- Messaging is explicit and educational.

- Errors should be localized and should not block the rest of the game when possible.

- Use consistent terms and glossary keys for all messages.

## 3. Standard error categories

- Offline restricted action

- Online verification pending

- Integrity action applied

- Service unavailable

- Version unsupported view only mode

## 4. States that require clear messaging

- Research completion pending

- Offline restricted action

- Cheating flag applied

### 4.1 States that are still messaged but not emphasized

- Forced cloud pull

- Event verification pending

## 5. Localization and tone

- Tone is neutral and system oriented.

- Cheat messaging is a neutral notice, not a warning language escalation.

- Repeated issues do not escalate messaging severity, but do increase backend flag counts.

## 6. UI patterns

- Inline banners for offline restrictions and pending verification.

- Toasts for short acknowledgements.

- Modals only for actions that cannot proceed and need a decision, for example linking an account or updating a blocked client.

## 7. Required message content

### 7.1 Research completion pending

- Explain that research progress continues, but completion needs an online confirmation.

- Provide a call to action to go online.

- Show remaining steps, for example connect, verify, claim.

### 7.2 Offline restricted action

- Explain which action requires online, such as purchases or research start.

- Explain what can be done offline instead.

### 7.3 Integrity action applied

- Explain that offline grants are disabled due to an integrity check.

- Explain how to restore if this is a false positive, for example contact support or use tester override.

## 8. Telemetry hooks

- error_shown (error_type, context, blocking)

- offline_restriction_hit (action_id)

- research_pending_shown (research_id)

- integrity_notice_shown (reason)

## 9. MVP constraints

- Keep messages short in default view and provide details behind a learn more affordance.

- Do not introduce complex escalation logic in MVP.

## 10. Open questions

None.

# System Spec 36: Performance, Memory, and Device Support Targets

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Device targets, frame rate, simulation budgets, scalability limits, degradation rules |
| Primary goal | Ensure stable performance on low end devices and future scalability |
| Invariants referenced | INV-01, INV-02, INV-03 |

## 1. Purpose

Define performance targets and scalability limits for Unity mobile builds. The game must run on low end Android and older iPhones with stable frame rate and acceptable battery and heat behavior.

## 2. Platform and orientation

- Minimum device class includes low end Android and older iPhones.

- Primary screens use portrait orientation.

- Dungeon design and layout editing may use landscape orientation.

## 3. Frame rate targets

- Target frame rate: 30 FPS.

- Avoid stutters during tick updates by batching and decoupling UI refresh from simulation.

## 4. Simulation and UI budgeting

- Simulation updates should be batched and processed in small chunks when needed.

- UI should update key metrics at a stable cadence, not on every simulation event.

- Avoid rerendering the full dungeon view on every tick.

## 5. Scalability limits

### 5.1 MVP limits

- Max floors in MVP: 5.

- Max active monsters in MVP should be below 200.

- Floor size increases over time, with later floors physically larger than earlier floors.

### 5.2 Long term direction

- Long term max floors are effectively unbounded, limited by performance and content gating.

- Limits should be defined by configuration tables and can change over time.

## 6. Degradation rules

- If limits are exceeded, degrade simulation fidelity instead of hard blocking by default.

- Warn the player when degradation is active.

- Allow continued play when possible, but prioritize correctness for time based systems.

## 7. Telemetry hooks

- perf_sample (fps, frame_time_ms, device_class)

- tick_duration (duration_ms, workload_units)

- degradation_enabled (reason, level)

## 8. MVP constraints

- Focus on stable tick processing and minimal UI work.

- Avoid heavy pathfinding or per tile updates in the main loop.

- Keep dungeon view rendering efficient.

## 9. Open questions

None.

# System Spec 37: QA Strategy and Test Harness

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Manual and later automated testing, debug tools, telemetry validation gates |
| Primary goal | Make auditing and verification repeatable across offline, saves, and events |
| Invariants referenced | INV-01, INV-02, INV-03, INV-08, INV-10 |

## 1. Purpose

Define the testing strategy and tooling needed to validate core invariants and prevent regressions. MVP relies on manual testing with a clear scenario checklist. Automated tests may be added later.

## 2. Required test scenarios

- Offline progression tests

- Multi device conflict tests

- Save rollback detection tests

- Season boundary rule tests

## 3. Manual first approach

- MVP uses manual testing as the primary approach.

- Partial automation is optional, but not required for MVP.

- Automated tests are expected later for integrity and time logic.

## 4. Debug tools

- In game debug menu supports: time jumps, heat forcing, mana injection, and event toggling.

- Debug tools are compiled out of production builds.

- A minimal diagnostics panel may exist in production for admin accounts after online verification.

## 5. Telemetry validation

- Maintain a checklist of required telemetry events per feature before release.

- For MVP, missing telemetry is a warning, not a release blocker.

- After MVP, missing telemetry blocks releases for balance critical and monetization critical features.

## 6. Telemetry hooks

- qa_checklist_completed (release_id, pass_rate)

- debug_menu_used (action_id)

## 7. MVP constraints

- Do not ship debug menus to production users.

- Keep the manual checklist short and focused on invariants.

## 8. Open questions

None.
