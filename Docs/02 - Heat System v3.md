**System Spec 2. Heat System (Draft v3 Locked Pending Final Numbers)**

**Purpose**

Heat represents the world’s response to dungeon behavior. It ensures that danger, profit, and reputation remain in tension and prevents consequence free exploitation.

------------------------------------------------------------------------

**Inputs**

- Adventurer deaths

- Elite adventurer deaths

- Party survival outcomes

- Party size

- Dungeon depth reached

- Tradeable loot value extracted

- Passive decay rate

- Research modifiers

------------------------------------------------------------------------

**Outputs**

- Current heat value

- Heat state

- World response triggers

------------------------------------------------------------------------

**Core Rules**

**Heat Gain**

Normal adventurer death\
+1 heat

Elite adventurer death\
+3 heat

Multiple deaths in a single run\
+1 bonus heat

------------------------------------------------------------------------

**Heat Reduction From Successful Runs**

If an entire adventurer party survives a run, heat is reduced based on:

- Party size

- Depth reached

Deeper runs imply smoother difficulty progression and reduce heat more.

Larger parties reduce perceived danger and therefore **reduce the magnitude of heat reduction**.

This creates a balancing curve rather than a flat reward.

------------------------------------------------------------------------

**Heat Reduction From Tradeable Loot**

Heat is reduced based on the **total value of tradeable loot successfully extracted**.

Higher value loot that enters the world economy increases goodwill, repeat traffic, and reputation.

------------------------------------------------------------------------

**Heat Floor**

Heat is capped at a minimum of -15 in MVP. This cap is configurable for future expansion.

The minimum heat cap is intended to enable future leaderboard style tracking for lowest heat.

------------------------------------------------------------------------

**Passive Heat Decay**

Heat decays over time at a slow baseline rate.

Research can increase passive decay speed and improve recovery from high heat states.

Passive decay can never fully counteract reckless play on its own.

------------------------------------------------------------------------

**Heat States (MVP)**

Peace\
Heat 0 to 9

Notice\
Heat 10 to 24

Concern\
Heat 25 to 49

Higher states are intentionally excluded from MVP.

------------------------------------------------------------------------

**Player Decisions Enabled**

- Allow parties to escape intentionally

- Design smoother difficulty curves

- Adjust loot tables to stabilize heat

- Invest in diplomacy and decay research

------------------------------------------------------------------------

**Failure Conditions**

- Sustained Concern state reduces adventurer traffic

- Poor reputation slows progression

- Heat mismanagement forces reactive play
