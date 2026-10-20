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

- The initial base offline efficiency is validated tuning at 15 percent. Future approved Research effects may modify the effective efficiency without changing eligible duration.

- Fractional mana is retained. A long valid forward interval remains eligible even when it exceeds a diagnostic clock-skew threshold; invalid or backward timestamps award zero.

- Grant evidence and the consumed timestamp are persisted atomically before runtime publication. Offline elapsed time never replays as active ticks.

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

## Owner-approved living-dungeon time amendment (2026-10-12)

See the [Living Dungeon Owner Design Lock](../docs/planning/decisions/living-dungeon-owner-design-lock.md) and [Cross-Spec Glossary amendment](Cross_Spec_Glossary_of_Invariants_UPDATED.md#living-dungeon-owner-approved-amendments-2026-10-12). The classification of app background as offline is preserved: foreground gameplay remains active without internet, while background and closed-app intervals use the approved offline simulation policy. During valid offline intervals, deterministic party events and configured arrivals continue to advance. The old freeze on party simulation and offline-attributed numeric Heat is superseded; offline Heat remains constrained to the interval's captured Heat Tier, with no extra run-derived login rebound.

This does not alter the existing 10-second online mana/upkeep tick, canonical offline mana formula, online research start/completion verification, or other existing online behavior. Party movement duration comes from traversable geometry; interaction durations are config-owned. Paths may be planned, but consequences occur incrementally at logical event boundaries. Simultaneous events have an explicit deterministic order independent of frame rate, device and selected floor. Exact event processing with bounded resumable batches is the MVP catch-up baseline. Arbitrary hourly samples/extrapolation are not authority; mathematically equivalent acceleration requires proof against exact processing. Any coarse historical model needs measured benefit, owner approval and exact-reference comparison.

<!-- P8-00-2026-10-20 -->

### Phase 8 owner-approved design precedence (2026-10-20; after merged PR #245)

The currently approved Living Dungeon time lock continues to govern online logical time, exact resumable event order, offline Heat tier restrictions, immutable starts and canonical passive offline mana; Phase 8 **adds** future locally validated **offline-progressing paid research timers** for the offline-first MVP (D196) and **full valid absence credit for individually eligible authentic event-based objectives** (D150), not estimated kills or extrapolated achievement grants. A research project's **claim** remains explicit and durable. Future public production research starts/claims need real online verification, not a reinterpretation of local clock validity. Collected-but-unextracted paid item refund, paid trap reset/recharge/replacement and refill on actual floor vacancy must occur on a strictly ordered, duplicate-safe canonical event timeline (D189–D201). Do not replay an online passive tick as an extra offline grant, cross offline Heat tiers through forbidden effects, infer elapsed event time from animation, or discard eligible unprocessed absence after interruption. Exact eight-hour catch-up differential and save/reopen scenarios are required **later acceptance tests**, not presently implemented features. See [Phase 8 owner lock](../docs/planning/decisions/phase-8-owner-design-lock.md).
