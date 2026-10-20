**System Spec 1. Mana System**

**Purpose**

Mana is the primary economic resource that fuels progression, optimization, and long term strategic identity. Mana rewards high adventurer engagement, strong dungeon throughput, and intelligent risk management rather than passive hoarding.

------------------------------------------------------------------------

**Inputs**

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

------------------------------------------------------------------------

**Outputs**

- Gross mana per minute

- Total Mana

- Reserved mana

- Usable mana

- Offline mana gained

------------------------------------------------------------------------

**Core Rules**

**Base Mana Generation**

Base mana per minute = Core Level × 2

**Floor Contribution**

Additional mana per minute = Active Floors × 1

**Adventurer Death Mana**

Base death mana = Adventurer Level × 5

Elite adventurer death mana = Base death mana × 2.5

Elite adventurers apply their multiplier to **both death mana and skill spill mana**.

------------------------------------------------------------------------

**Skill Spill Mana Online**

Mana from skill spill = Total damage dealt × 0.15

There is no per adventurer cap on skill spill. Higher adventurer count and stronger adventurers always increase total skill spill.

Elite adventurers multiply their contribution to total damage, and therefore indirectly multiply skill spill mana. In practice this is implemented by applying the 2.5 multiplier to elite tagged runs when converting their damage into spill.

------------------------------------------------------------------------

**Offline Skill Spill**

During offline progression, skill spill is calculated using **expected damage**, not simulated combat.

Offline skill spill mana = Expected damage × 0.15 × 0.30 (offline multiplier)

- The base coefficient is lower than online play

- Research can increase the retained percentage for offline (base 30%)

- Offline skill spill can never exceed online potential

This prevents incentivizing leaving the game open continuously.

------------------------------------------------------------------------

**Heat Efficiency Modifier**

Peace\
Mana efficiency = 100 percent

Notice\
Mana efficiency = 95 percent

Concern\
Mana efficiency = 85 percent

All mana sources are multiplied by the current heat efficiency.

------------------------------------------------------------------------

**Mana Reservation and Availability**

Mana is divided into three visible pools:

- Total Mana

- Reserved Mana

- Usable Mana

Reserved mana is allocated to monster upkeep and other persistent systems.

Usable Mana = Total Mana − Reserved Mana

If Reserved Mana exceeds incoming mana generation, the dungeon enters a fragile state where recovery depends on player intervention.

------------------------------------------------------------------------

**Player Decisions Enabled**

- Attract many weaker adventurers versus fewer elite ones

- Optimize for skill spill versus death mana

- Balance monster density against reserved mana pressure

- Invest in research to improve offline efficiency

------------------------------------------------------------------------

**Failure Conditions**

- Over investment in elites increases danger and risk

- Excessive reserved mana limits growth

- Poor layouts underperform regardless of monster strength

### Owner-approved combat and recovery clarification (2026-10-14)

For future implementation, see the [Combat and Reserve-Backed Floor Recovery Owner Design Lock](../docs/planning/decisions/combat-and-recovery-owner-design-lock.md). The existing `StructureRuntimeState.ManaReserve` and current single wallet are spendable balance authorities; this historical “reserved mana” pool wording does not mean the runtime has an implemented protected allocation. The approved future model derives a required reserve from configured obligations, exposes usable mana as `max(0, TotalMana - RequiredReservedMana)`, protects it from ordinary spending, and permits authorized recovery to spend it exactly once without lowering the requirement. Existing passive, skill-spill, and adventurer-death mana sources remain unchanged. No field rename, second wallet, or schema change is authorized here.

<!-- P8-00-2026-10-20 -->

### Phase 8 owner-approved design precedence (2026-10-20; after merged PR #245)

The approved future Phase 8 economy **retains one canonical total-mana wallet**, with a derived protected obligation reserve governed by the [Combat and Reserve owner lock](../docs/planning/decisions/combat-and-recovery-owner-design-lock.md). Phase 8 research charges configured mana **once at project start** from eligible usable funds rather than holding a project-duration reservation (D138). Future loot-node item creation, paid trap replacement/reset/recharge and eligible restoration each use exact-once atomic debits under a separately specified protected-reserve classification (D130, D189–D200). D195/D197 permit a **partial recovery credit** solely for an originally paid item unit actually collected but not extracted: calculate from the **captured historical creation payment**, not a current price or world value, and never more than that original cost. Exact refund rate, rounding, same-timestamp event order and capacity clamp are **unselected config/technical gates**. No research wallet, loot income wallet, copied reserve owner, mana-cheat exception or speculative online/offline rate is introduced. See [Phase 8 owner lock](../docs/planning/decisions/phase-8-owner-design-lock.md).
