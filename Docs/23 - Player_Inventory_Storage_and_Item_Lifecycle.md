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
