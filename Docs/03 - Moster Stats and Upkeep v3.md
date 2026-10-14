**System Spec 3. Monster Stats and Upkeep (Draft v3 Locked Pending Final Numbers)**

**Purpose**

Monsters define dungeon identity and difficulty. Their scaling and upkeep enforce tradeoffs between power, density, and sustainability.

------------------------------------------------------------------------

**Inputs**

- Monster base stats

- Monster level

- Evolution tier

- Synergy bonuses

- Active monster count

------------------------------------------------------------------------

**Outputs**

- Monster damage

- Monster health

- Summon cost

- Reserved mana upkeep

------------------------------------------------------------------------

**Core Rules**

**Stat Scaling**

Health = Base Health × (1 + Level × 0.12)

Damage = Base Damage × (1 + Level × 0.10)

------------------------------------------------------------------------

**Summon Cost**

Summon cost = Base Cost + (Level × 10 mana)

------------------------------------------------------------------------

**Evolution Efficiency**

Tier 2 evolution\
+20 percent stat efficiency

Tier 3 evolution\
+35 percent stat efficiency

------------------------------------------------------------------------

**Monster Upkeep**

Each monster reserves mana based on:

- Monster level

- Evolution tier

Upkeep is a **reserved mana cost**, not a continuous drain.

Upkeep formula conceptually scales faster than linear to prevent infinite low level stacking.

------------------------------------------------------------------------

**Replacement Logic**

When a monster dies:

- If sufficient mana exists, it is replaced automatically

- If insufficient mana exists, the slot remains empty

The dungeon continues operating with reduced effectiveness.

This can cascade into further danger if too many monsters are missing.

Monster deaths do not directly create penalties. The cost is implicit through upkeep pressure and replacement risk.

------------------------------------------------------------------------

**Player Decisions Enabled**

- Fewer elite monsters versus many weak ones

- Managing reserved mana headroom

- Intentional monster loss as a recovery tool

- Timing expansions around upkeep capacity

**Failure Conditions**

- Excessive upkeep starves usable mana

- Poor monster composition weakens dungeon defense

- Over commitment increases systemic risk

### Owner-approved recovery clarification (2026-10-14)

The [Combat and Reserve-Backed Floor Recovery Owner Design Lock](../docs/planning/decisions/combat-and-recovery-owner-design-lock.md) supersedes the future-facing automatic replacement and empty-slot rules above. On vacant floors, approved recovery first restores eligible injured or defeated existing monsters toward configured functional readiness, preserving monster identity and committed progression. Insufficient funding leaves an inactive pending defense and pending recovery; automatic creation of a replacement identity is not the default. New-identity replacement remains a separate future option. Stable monster HP/level persistence, slot representation, and migration require save-design review. The wording above remains historical design evidence and describes no shipped recovery implementation.
