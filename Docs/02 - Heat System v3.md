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

**Historical v3 design target (not current active configuration):** Heat has a minimum of -15 in MVP. This was a configurable target for future expansion.

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

## Owner-approved living-dungeon amendment (2026-10-12)

See the [Living Dungeon Owner Design Lock](../docs/planning/decisions/living-dungeon-owner-design-lock.md) and [Cross-Spec Glossary amendment](Cross_Spec_Glossary_of_Invariants_UPDATED.md#living-dungeon-owner-approved-amendments-2026-10-12). This amendment governs future living-dungeon implementation and preserves this specification's historical Heat inputs, outcome composition and fully foreground behavior.

Offline adventurer outcomes may change numeric Heat but must remain in the Heat Tier held at the accepted offline interval boundary. Excess is discarded, not banked; login does not add a rebound for Heat already caused by offline runs. For mixed foreground/offline runs, causal attribution for aggregate bonuses and both existing cooling stages is unresolved implementation work. Retain the M6-FU0 two-stage foreground composition; do not infer new coefficients, cooling rules or tier crossing behavior.

The -15 minimum above is historical design text, not the current active Heat domain. The active run-simulation configuration sets `HeatPeaceMinimum` to 0, and the current tier resolver rejects values below that configured minimum. Continuous tier membership applies to finite Heat values valid under the applicable configured Heat range and tier definitions: each configured tier lower threshold is inclusive, and the next tier's lower threshold is exclusive. The final tier remains bounded by the configured valid maximum. The existing fractional Heat values are preserved without rounding or arbitrary epsilon, and fully foreground behavior is unchanged.

Negative-Heat support and its tier mapping are a separate future design and configuration decision. They are not currently activated. This document does not extend Peace below its configured threshold, clamp or discard negative values, or authorize any migration or runtime configuration change. All tuning and workload limits remain config-owned.
