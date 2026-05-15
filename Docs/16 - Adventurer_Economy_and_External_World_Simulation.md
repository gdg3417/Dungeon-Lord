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
