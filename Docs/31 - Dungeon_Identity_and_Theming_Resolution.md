**System Spec 31: Dungeon Identity and Theming Resolution**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Identity detection, effects, interactions with seasons and heat |
| Primary goal | Make the world respond to dungeon style without direct player toggles |

# 1. Purpose

Define how dungeon identity is derived from the dungeon state and how identity influences the world. Identity is an emergent classification that shapes attraction and loot distribution without directly altering numeric multipliers to mana or loot value.

# 2. Scope

- Primary dungeon has a single identity state.

- Season dungeons have their own identity state.

- Sub dungeons have their own identity state.

# 3. Identity generation model

Identity is derived from the observed dungeon composition and behavior, including:

- Layout patterns and theme tags

- Monster families and roster composition

- Trap density and lethality profile

- Historical outcomes such as deaths, retreats, and extraction rates

- Heat behavior trends over time

# 4. Identity change behavior

- Identity has no direct switch, token, or respec action.

- Identity changes naturally as the underlying dungeon state changes.

- Identity has no cooldown, but changing identity requires meaningful renovations and time for perception to update.

- Perception update is modeled as a smoothing window so identity does not flip instantly.

# 5. Identity effects

## 5.1 What identity can change

- Loot distribution bias toward certain tags and themes, without changing numeric loot multipliers directly

- Heat behavior shifts, such as how quickly pressure builds within tier bounds

- Adventurer attraction shifts, such as party types, motivations, and traffic composition

## 5.2 What identity cannot change

- Identity does not modify room difficulty estimates.

- Identity does not apply numeric multipliers to mana generation.

- Identity does not apply numeric multipliers to loot value.

# 6. Tradeoffs

Identities are tradeoffs. For example, an aggressive identity may increase the rate of loot absorption opportunities and improve quality distributions, while also attracting stronger parties and raising risk to the core.

# 7. Interactions

## 7.1 Seasons

- Seasons override identity while active for the season dungeon.

- Identity effects are not applied inside season dungeon rule sets unless an event explicitly opts in.

## 7.2 Events and contracts

- World events may reference identity to target effects, for example increasing traffic for certain identities.

- Contracts may reward building toward an identity profile without granting direct identity buffs.

# 8. UI requirements

- Show current identity label and a short plain language description.

- Show contributing factors in advanced view, for example top tags and roster composition influence.

- Communicate that identity is descriptive, not a direct player toggle.

# 9. Telemetry hooks

- identity_changed (old_id, new_id, confidence, smoothing_window)

- identity_influence_summary (top_tags, top_rosters, traffic_profile)

# 10. MVP constraints

- Identity is informational and world reactive, not a player selected build choice.

- No direct numeric multipliers from identity in MVP.

# 11. Open questions

None.
