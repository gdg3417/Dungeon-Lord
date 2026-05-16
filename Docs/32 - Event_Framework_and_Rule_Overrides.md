**System Spec 32: Event Framework and Rule Overrides**

*Dungeon Builder, locked design specification*

| Status       | Locked                                               |
|--------------|------------------------------------------------------|
| Scope        | World events, seasons, overrides, lifecycle, rewards |
| Primary goal | Reusable data driven events that safely modify rules |

# 1. Purpose

Define the framework for limited time world events and seasons. This spec defines rule override behavior, priority order, offline handling, reward timing, and the season pass purchase rules.

# 2. Event types

- World events: limited time modifiers that can affect the primary dungeon economy.

- Seasons: dedicated season dungeon experiences with unique rules, leaderboards, and season pass progression.

# 3. Rule application priority

When multiple rule sources apply, evaluate them in this order:

1.  Base game rules

2.  Heat rules

3.  Research rules

4.  Event or season rules

5.  Dungeon identity rules

# 4. Override mechanics

## 4.1 World events

- World events are modifiers that stack on top of existing systems.

- World events may affect primary dungeon traffic, loot demand, or contract availability.

- Example: war increases demand for health potions, increasing traffic to dungeons that produce them.

## 4.2 Seasons

- Seasons use a separate season dungeon save.

- Season rules often override, replacing parts of base rules for the season dungeon.

- Season rules are configured to be reusable via data changes, not code changes, for most seasons.

# 5. Lifecycle and verification

- If the player is offline during an event start or end, event state changes apply only after online verification.

- World events and seasons rely on server time for activation windows.

- Event content is primarily data driven.

# 6. Run boundary rule changes

## 6.1 Season dungeons

- A run in progress completes under the rules active when the run started.

- The next run uses the updated rules.

## 6.2 Primary dungeon world events

- Apply changes on the next online verification.

- Do not retroactively change outcomes of runs already resolved.

# 7. Leaderboards and rewards

- Primary dungeon leaderboards do not reset.

- Season dungeon leaderboards reset per season.

- Season leaderboard rewards grant at season end only.

- Season pass includes milestone rewards during the season.

# 8. Season pass purchase rules

- Season pass is purchasable only while the season is active.

- Each season has its own season pass. Premium track requires purchase each season.

- Purchases require online verification.

# 9. Data driven configuration

- Events define: start and end times, rule modifiers or overrides, targeted systems, and reward definitions.

- Events can reference loot tags, identities, and traffic modifiers.

- Configuration is validated by the data linter before release.

# 10. Telemetry hooks

- event_state_changed (event_id, state, verified)

- season_started (season_id)

- season_ended (season_id)

- season_pass_progress (tier, claimed)

- leaderboard_snapshot_submitted (board_id, value, bracket)

# 11. MVP constraints

- Event system should support a small number of concurrent events.

- Most events are configured by data, with minimal new code per event.

- Offline event changes are applied only after online verification.

# 12. Open questions

None.
