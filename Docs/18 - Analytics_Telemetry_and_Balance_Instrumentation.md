# System Spec 18: Analytics, Telemetry, and Balance Instrumentation

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines what gameplay data is captured, how it is structured, and how it is used to measure retention, balance health, and the success of the core fantasy in MVP and beyond.

The guiding goal is to learn whether players return on day 1 and day 7, and whether the short session loop creates a reliable 'one more thing' compulsion centered on layout optimization, loot table tuning, and monster placement decisions.

2\. Instrumentation Principles

2.1 Minimal but decisive

Only log events that answer questions tied to a decision, a pressure shift, a progression milestone, or a failure state transition. Favor outcome logging plus state snapshots over logging every intermediate coefficient.

2.2 Player trust and privacy

Telemetry is tied to account sign in to support cloud saves and seasons. No personally identifying information is required for gameplay analytics.

2.3 Debuggability for small scale tests

MVP includes a developer only export bundle that captures the current save state summary, plus the last 50 run summaries, plus the recent event log, enabling fast diagnosis of tester feedback.

3\. Data Retention and Storage

3.1 Retention window

Telemetry is retained for 90 days in MVP. After 90 days, events are either deleted or aggregated into coarse summaries, such as retention cohorts and balance percentiles.

3.2 Offline buffering

While offline, events are buffered locally and uploaded when online. Upload uses idempotent event ids to prevent duplicate ingestion.

3.3 Cloud preferred, local fallback

Cloud collection is preferred from day one. If cloud collection is not available for a small friends and family build, local logs plus copy to clipboard export is acceptable.

4\. Definitions

4.1 Active time

Active time is wall clock time while the app is in foreground and not paused. A return day counts only if the player accumulates at least 60 seconds of active time that day.

4.2 Session

A session begins at app foreground and ends after 30 seconds of background time or a clean quit, whichever occurs first.

4.3 Run

A run is an adventurer party entering the dungeon and resolving until exit, wipe, or retreat. MVP does not include any player triggered simulation or test run button.

5\. MVP Success Metrics

5.1 Retention

Primary success metrics are day 1 retention and day 7 retention, using the active time definition above.

5.2 Session intent

Target session length is 5 to 10 minutes. The session should end with a clear next optimization hook, such as an improved tile layout, a revised loot table, or a rebalanced monster placement.

5.3 Tutorial completion

Tutorial completion for analytics is defined as the first time the player starts research.

6\. Funnel and Milestones

6.1 MVP funnel events

The primary funnel is: install, first run, first research started, first new floor unlocked, first offline summary shown.

6.2 Milestone definitions

First run is the first resolved adventurer attempt. First new floor is the first time the player unlocks an additional floor. First offline summary is the first return session where offline summary data is displayed.

7\. Event Taxonomy

7.1 Required events

Each event payload should include: player id, session id, timestamp, content version, save version, and a small state snapshot.

Session events: session_start, session_end.

Account events: account_created, account_signed_in, cloud_save_enabled.

Tutorial events: tutorial_step_complete, tutorial_skipped, recommended_next_step_clicked.

Economy events: mana_earned, mana_spent, reserve_changed.

Pressure events: heat_changed, heat_tier_changed, pressure_rebound_applied.

Progression events: floor_unlocked, room_built, room_modified, monster_placed, monster_upgraded, loot_table_edited, research_started, research_completed, research_completion_pending.

Run events: party_spawned, run_started, run_ended, room_resolved.

Offline events: offline_entered, offline_exited, offline_summary_shown, offline_summary_viewed.

Security events: clock_anomaly_detected, save_integrity_warning, offline_grant_disabled, cheat_flagged.

7.2 Run end payload

run_ended should capture: outcome (clear, partial, retreat, wipe), depth reached, rooms cleared, deaths, survivors, loot_generated_value, loot_extracted_value, heat_delta, mana_delta, and a coarse layout signature.

7.3 Layout signature

Layout signatures are coarse, for example counts of tile types and tags per room, plus key placements such as core adjacency and choke points. Do not store full grids in telemetry.

8\. Balance Red Flags and Alerts

8.1 Early heat spike

Trigger a balance alert if a player reaches the Concern heat tier before unlocking floor 2.

8.2 Heat not moving

Trigger a balance alert if total heat delta in the first hour is below a configurable threshold across a meaningful sample.

8.3 Dominant strategy detection

Compute strategy clusters across layout signatures, monster rosters, and loot table patterns. Flag clusters that exceed a configurable advantage threshold, such as median mana per hour or win rate exceeding the next best cluster by more than a set margin.

9\. KPI Computations

9.1 Economy KPIs

Track mana per hour, usable mana versus reserved mana ratio, reserve composition in advanced diagnostics, and average renovation frequency per hour.

9.2 Pressure KPIs

Track heat tier distribution over time, heat volatility (absolute delta per hour), and time to recover to Peace from Concern among players who recover.

9.3 Run KPIs

Track run completion rate, retreat rate, wipe rate, average depth, loot extracted per run, and research setback incidence driven by raid failures.

10\. Reporting Surfaces

10.1 In game developer view

MVP includes a hidden developer view for testers that can export the debug bundle, and can display basic retention counters, last three heat change causes, and last run summary.

10.2 Dashboard requirements

The analytics backend must support: cohort retention, per build comparisons, and simple filters by dungeon core level, floor count, and heat tier.
