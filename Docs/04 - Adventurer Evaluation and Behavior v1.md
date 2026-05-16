**4D. Adventurer Evaluation and Behavior (Spec v1)**

**Purpose**

The Adventurer Evaluation and Behavior system determines who enters the dungeon, in what composition, how they move through it, when they retreat, and how they impact mana and heat. This system is the primary driver of emergent gameplay and dungeon identity.

------------------------------------------------------------------------

**Inputs**

- Dungeon rating

- Heat value and heat state

- Active floor count

- Highest monster level present

- Monster family composition

- Loot table composition

- Mana efficiency

- Negative or positive heat modifiers

------------------------------------------------------------------------

**Outputs**

- Adventurer party spawn rate

- Party size

- Adventurer class mix

- Elite adventurer probability

- Run outcome (clear, partial clear, retreat, death)

- Mana generation events

- Heat change events

------------------------------------------------------------------------

**Core Rules**

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

**Party Spawn Rate**

Base adventurer party spawn rate is influenced by:

- Dungeon rating (higher rating attracts stronger but fewer parties)

- Heat state (higher heat reduces traffic)

- Negative heat bonus (increases low level traffic)

When heat is negative:

- Low level adventurer traffic increases

- Mana efficiency increases to 110 percent

- Adventurer death mana generation is reduced to compensate

------------------------------------------------------------------------

**Party Size**

Early game party size range:

- 1 to 3 adventurers

Mid game party size range:

- 3 to 6 adventurers

Party size selection weights increase with dungeon rating and floor count.

------------------------------------------------------------------------

**Elite Adventurer Spawning**

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

------------------------------------------------------------------------

**Adventurer Class Selection**

Adventurer classes are selected based on:

- Loot table composition

- Monster families present

- Floor themes (if applicable)

Balanced parties are preferred at higher dungeon ratings.

------------------------------------------------------------------------

**Pathing and Depth**

Adventurers attempt to progress until:

- Risk exceeds tolerance

- Objectives are met

- Party members die or retreat logic triggers

Depth reached is defined as:

- Highest floor entered during the run

------------------------------------------------------------------------

**Retreat Logic**

Retreat chance increases when:

- Party health is low

- Multiple adventurers fall

- Difficulty spikes sharply

Surviving full party runs reduce heat based on:

- Party size

- Highest floor entered

------------------------------------------------------------------------

**Player Decisions Enabled**

- Shape dungeon to attract elites or farmers

- Smooth difficulty curves to reduce heat

- Manipulate party survival for reputation

- Control traffic type via monster and loot choices

------------------------------------------------------------------------

**Failure Conditions**

- Over tuned dungeons reduce traffic

- Excessive lethality spikes heat

- Poor dungeon signaling leads to mismatched parties

------------------------------------------------------------------------

**Design Notes**

This system is intentionally probabilistic to encourage experimentation and emergent outcomes rather than deterministic farming.
