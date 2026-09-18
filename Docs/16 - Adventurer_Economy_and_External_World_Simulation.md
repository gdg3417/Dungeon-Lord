# System Spec 16: Adventurer Economy and External World Simulation

Status: Locked (v1)\
Scope: MVP with forward compatibility

## 1. Purpose

This system defines how adventurers exist in the external world, how they circulate between regions, how loot and death affect the economy, and how the dungeon interfaces with a living but bounded world.

## 2. World Model

Adventurers are finite and simulated at a high level. In MVP, ordinary adventurers are represented as pooled counts or cohorts per region and per level band.

## 3. Adventurer Lifecycle

Ordinary-adventurer lifecycle effects persist across dungeon runs at the pooled or cohort level: progression, gear distribution, retirement, death, circulation, and regional counts may remain durable without giving every ordinary adventurer an individual save identity. Death removes ordinary adventurers from the region with a short cooldown before reappearance elsewhere. Named characters or heroes are the future path for durable individual identity, history, relationships, rivalries, grudges, and disproportionate party-profile influence; that system is not implemented by Phase 5.

**Phase 5 owner clarification:** The pooled/cohort versus durable-individual identity distinction above clarifies the original locked v1 lifecycle language. See the [Phase 5 branching and route-choice design lock](../docs/planning/phase-5-branching-and-route-choice-design.md).

## 4. Loot and Economy

Extracted loot circulates abstractly. Merchant availability is fixed in MVP. No global supply saturation occurs.

## 5. Dungeon Competition

Dungeons do not compete for a single global pool. Competition is indirect through reputation and archetypes.

## 6. MVP Boundary

The world economy is a background approximation with visible effects only.
