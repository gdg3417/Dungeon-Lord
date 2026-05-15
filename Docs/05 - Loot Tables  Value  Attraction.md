**System Spec 5: Loot Tables, Value, and Attraction**

**Status: Locked (v1)\
Scope: MVP + forward compatible**

**1. Purpose of the Loot System**

**The loot system exists to drive four things simultaneously:**

1.  **Adventurer attraction and traffic**

2.  **Political pressure and heat mitigation**

3.  **Mana reserve pressure through crafting**

4.  **Long term dungeon identity and progression**

**Loot is not a direct mana source. Instead, it is a traffic amplifier and a heat stabilizer that introduces risk, reserve tension, and strategic tradeoffs. Loot represents value returned to the world and defines how the dungeon is perceived politically and economically.**

**2. Loot Generation Model**

**2.1 Loot Rolls Basis**

**Loot rolls are generated per room cleared, not per run.**

- **Each room has its own loot table.**

- **The dungeon lord configures loot tables at the room level.**

- **Deeper rooms may have richer tables, higher tier weights, or rarity guarantees.**

**This allows:**

- **Early rooms to provide low risk, low value loot**

- **Later rooms to function as high risk, high reward targets**

- **Fine grained dungeon pacing and specialization**

**3. Loot Tables**

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

**4. Loot Extraction Rules**

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

**5. Boss Loot Rules**

**5.1 Boss Guarantees**

**Bosses guarantee:**

- **Minimum rarity: Rare**

**Bosses do not guarantee:**

- **A tier minimum**

- **A specific item category**

**This preserves excitement without forcing deterministic progression.**

**6. Loot Categories (Expanded and Locked)**

**Loot belongs to a family. Each family has tiers, rarity weights, economic impact, and attraction behavior.**

**6.1 Materials**

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

**6.2 Consumables**

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

**6.3 Currency**

**Includes**

- **Local kingdom coinage**

- **Dungeon minted tokens**

- **Rare collector coins from boss events**

**Design Notes**

- **Simple mana replacement cost**

- **Medium adventurer attraction impact**

- **Low prestige, high liquidity**

- **Useful for stabilizing heat without increasing lethality**

**6.4 Utility Items**

**Includes**

- **Rope, torches, climbing gear**

- **Backpacks, tents**

- **Magical utilities such as everbright lanterns or warm tents**

- **Multi function magical tools**

**Design Notes**

- **Attracts explorers, scouts, merchants, and caravan guards**

- **Low combat impact, high world flavor**

- **Enables non combat and logistics focused dungeon identities**

**6.5 Gear and Equipment**

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

**6.6 Spell Scrolls**

**Includes**

- **Elemental spells**

- **Protective spells**

- **Support spells**

- **Unique scrolls derived from adventurer drops**

**Design Notes**

- **Strongly attracts mages and scholars**

- **Synergizes with magic themed floors**

- **Enables spell focused dungeon specializations**

**6.7 Boss Sets**

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

**7. Trade Goods (Hybrid Model)**

**Trade goods exist in two forms:**

1.  **Dedicated trade items**

    - **Coffee beans, monster silk, magical plants**

2.  **Flagged items**

    - **Consumables or materials that are both usable and sellable**

**Tradeable status determines whether loot contributes to heat reduction and merchant traffic.**

**8. Loot Value System**

**8.1 World Value**

**World value represents economic benefit to the surrounding world.**

**WorldValue =**

**TierBaseValue × (TierGrowthFactor^(TierIndex − 1)) × RarityMultiplier**

**World value is used for:**

- **Heat reduction from extracted loot**

- **Attraction strength calculations**

**World value is not used for mana generation.**

**9. Loot Crafting Reserve Cost**

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

**10. Loot Progression and Research Lock**

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

**11. Attraction System Interface**

**Loot contributes to attraction through expected extracted world value, not generated value.**

**Higher expected value:**

- **Increases traffic**

- **Increases average adventurer level**

- **Increases elite chance indirectly**

**Heat applies a penalty to attraction to ensure balance.**

**12. Heat System Interface**

- **Heat reduction from loot only applies to extracted tradeable loot**

- **No survivors means no cooling**

- **High value adventurer deaths increase heat more strongly**

**This enforces:**

- **Political danger over raw lethality**

- **Viable non lethal and hybrid playstyles**

**13. Failure Conditions**

**The system is considered broken if:**

- **Loot reduces heat without survival**

- **Loot crafting reserve is negligible**

- **High tier loot becomes free through traffic**

- **Loot attraction overwhelms heat penalties**

- **Dungeon identity collapses into a single optimal table**

**14. MVP Constraints**

**For MVP:**

- **Tier range limited**

- **Regional scarcity ignored**

- **Item pool sizes small**

- **Loot table UI simple but explicit**

**All systems are forward compatible.**
