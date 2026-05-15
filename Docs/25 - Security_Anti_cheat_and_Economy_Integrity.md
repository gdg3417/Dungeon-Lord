# System Spec 25: Security, Anti cheat, and Economy Integrity

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines the threat model, authority boundaries, online requirements, integrity checks, and responses for cheating and exploitation.

2\. Threat Model

2.1 Primary threats

The highest priority threats are clock manipulation and memory editing.

2.2 Protected assets

Premium currency and time based systems are protected at the highest priority. Mana is important but is treated as a soft currency with secondary protection.

3\. Authority Split

3.1 Online required actions

Online connectivity is required for purchases, leaderboards, events, research start, and research completion.

3.2 Offline allowed actions

The game is playable offline, but while offline: events, leaderboards, purchases, research start, and research completion are unavailable.

4\. Research Integrity Rules

4.1 Offline progress with pending completion

If research is started online, progress continues while offline. If the timer would complete while offline, completion is marked pending and is finalized only when the client next goes online.

4.2 Completion validation

On completion, validate that prerequisites are still met, that reserve conditions were satisfied at start, and that time progression is plausible.

5\. Heat and Offline Rules

5.1 Heat frozen offline

Heat does not change while offline.

5.2 Tier bounded rebound

On login, apply a pressure rebound that can move heat up or down within the current tier bounds only. Offline alone cannot move the player across tiers.

5.3 Cross tier changes require play

After login, active play can move heat across tiers through normal heat change rules.

6\. Detection Signals

6.1 Clock anomalies

Detect time discontinuities, unusually large offline durations, repeated backwards time moves, and unrealistic completion times for timed systems.

6.2 Memory editing indicators

Detect impossible currency deltas, impossible reserve states, and impossible run outcomes relative to content constraints.

6.3 Save integrity

Use save versioning and checksums to detect tampering. Maintain a monotonic save sequence to reduce rollback abuse.

7\. Response Policy

7.1 MVP response

When cheating is detected, flag the account for review in a dashboard, notify the player, and disable offline grants.

7.2 False flag handling

Provide an admin override path for testers to restore offline grants if a false flag occurs.

7.3 Escalation

Repeat offenders can receive stricter restrictions, such as disabling leaderboard participation or requiring online play for all progression.

8\. Cloud Saves and Conflicts

8.1 Early cloud preference

Cloud saves are preferred early to support cross device play and reduce local tampering.

8.2 Conflict resolution

If two devices produce conflicting saves, the most recent timestamp wins, subject to plausibility checks.

9\. Server Requirements Summary

The server must support: purchase validation, premium currency authority, research start and completion endpoints, leaderboard submission validation, cheat flag storage, and basic account management.
