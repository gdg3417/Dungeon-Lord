**System Spec 29: Time Model and Tick Resolution**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Simulation time, ticks, offline behavior, anomaly detection |
| Primary goal | One consistent time contract across mana, upkeep, heat, and research |

# 1. Purpose

Define a unified time model for simulation and progression. This spec sets tick rates, offline handling, and anomaly detection. It ensures that the game is playable offline while protecting time based systems from manipulation.

# 2. Time sources

- When online: server time is the authoritative wall clock.

- When offline: local time is used for presentation and offline grants.

- App in background is treated as offline for simulation purposes.

# 3. Tick resolution

## 3.1 Mana generation and upkeep

- Tick rate: every 10 seconds during active play.

- Each tick applies: mana generation, upkeep reservations, and any interval based housekeeping.

## 3.2 Heat updates

- Heat changes during active play are event driven only.

- Heat events include: adventurer deaths, loot extraction outcomes, raid outcomes, and other explicitly defined events.

## 3.3 Research progress

- Research uses a continuous timer.

- Start and completion require online verification.

- Offline progress may exceed the duration, but completion remains pending until online confirmation.

# 4. Offline behavior

## 4.1 Mana offline accumulation

- Offline mana uses a single formula grant based on last known mana per hour.

- Grant is clamped to mana capacity.

- There is no offline time cap for mana credit, since the clamp limits output.

## 4.2 Heat offline behavior

- Heat is frozen while offline.

- On login, heat may rebound within the current tier based on offline pressure.

- Offline rebound cannot cross heat tiers.

- Crossing tiers requires active play after login.

## 4.3 Research offline behavior

- Research progress continues offline.

- Completion is pending until the next online check.

- Offline summary must show when research is pending completion.

# 5. Offline summary UX

- Show mana storage full when clamped to capacity.

- Show research completion pending when progress exceeded duration but completion is not finalized.

- Show any heat rebound applied within tier bounds.

# 6. Time acceleration rules

- No player driven time acceleration mechanics are allowed, except premium time skips.

- Premium time skips require online verification per the security spec.

# 7. Time anomaly detection

- Any observed clock jump above 10 minutes is suspicious.

- Anomalies are handled consistently across mana, research pending completion, and season timers.

- When an anomaly is detected, trigger integrity actions defined in the security spec.

# 8. Telemetry hooks

- offline_grant_calculated (offline_seconds, mana_granted, clamped)

- offline_summary_viewed (mana_full, research_pending, heat_rebound_applied)

- time_anomaly_detected (delta_seconds, action_taken)

- tick_update (ticks_processed, duration_ms)

# 9. MVP constraints

- Offline mana is approximation only, not full tick simulation.

- Heat remains frozen offline with tier bounded rebound at login.

- Research completion always requires an online confirmation.

# 10. Open questions

None.
