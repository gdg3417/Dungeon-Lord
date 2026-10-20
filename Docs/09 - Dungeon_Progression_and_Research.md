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

<!-- P8-00-2026-10-20 -->

### Phase 8 owner-approved design precedence (2026-10-20; after merged PR #245)

This is an **approved future Phase 8 design**, not a claim about current production research. Sections 3.1–3.3 and the historical one-global-slot requirement are superseded **for Phase 8 MVP** to the extent they reserve research mana throughout a timer, pause paid projects for lower later mana, or allow only one shared General/Blueprint project. The owner approved **two independent single-project tracks** (General and Blueprint); manual start; **one-time atomic upfront spend of usable mana**; offline timer progression; explicit once-only locally validated MVP completion/claim; no cancel/automatic queue; and no unlock/slot release before valid claim (D106–D109, D137–D140, D196). A public production release **after the offline-first MVP** requires genuine backend/online start and claim verification; locally checked state is not online verification. Actual timed/state/save design, gate bypass of legacy online-only restrictions and future server integration require reviewed contracts. Monsterology prerequisite gates may count *distinct researched predecessor-form classes* and authentic achievements (D157–D173, D182–D185, D201), rather than assume universal direct-parent class dependencies. No costs/durations/thresholds are set by this addendum.

Current `PlayerResearchActionHandler` is a **single-slot, no-charge, no-offline-progress local-MVP scaffold**, not proof of Phase 8 paid dual tracks. Follow [the Phase 8 owner lock](../docs/planning/decisions/phase-8-owner-design-lock.md), research section; preserve current behavior until a separately tested PR activates it.
