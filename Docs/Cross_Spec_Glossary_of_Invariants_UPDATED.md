**Cross Spec Glossary of Invariants**

*Dungeon Builder, locked global invariants*

| Status | Locked |
|----|----|
| Purpose | Prevent contradictions across system specs and ensure consistent implementation |
| Format | Human readable list, paired with spreadsheet checklist |

# 1. How to use this glossary

- Every new spec must list which invariants it depends on.

- Spec review must confirm that no invariant is contradicted.

- When a change is proposed, update this glossary first, then update dependent specs.

# 2. Invariants

## INV-01

Offline play is allowed, but purchases, events, leaderboards, research start, and research completion require online verification.

Referenced by: Spec 19, 25, 29, 33, 34, 35, 37

## INV-02

Heat is frozen while offline. On login, heat may rebound within the current tier but cannot cross tiers due to offline alone.

Referenced by: Spec 7, 18, 25, 29, 35

## INV-03

Research progress continues offline, but completion is pending until online confirmation.

Referenced by: Spec 9, 18, 25, 29, 35

## INV-04

Saves store stable IDs and player progress. Numeric tuning resolves from current content tables.

Referenced by: Spec 19, 28, 33, 34

## INV-05

Authority model: server authoritative for premium currency, leaderboards, season dungeon, research timing, and primary dungeon when online.

Referenced by: Spec 25, 28, 34

## INV-06

Modifier stacking uses layered buckets with a single global order: base, heat, research, event or season, clamps and soft caps, rounding.

Referenced by: Spec 30, 18, 21, 32

## INV-07

Seasons run in a separate season dungeon and override identity effects inside season rules.

Referenced by: Spec 22, 31, 32

## INV-08

Event state changes apply only after online verification. Runs in progress complete under rules active at run start.

Referenced by: Spec 29, 32, 35, 37

## INV-09

Telemetry is tied to account sign in when linked. Retention uses D1 and D7 with at least 60 seconds active time.

Referenced by: Spec 18, 28, 33, 37

## INV-10

If rollback is detected: force cloud pull, disable offline grants, and lock research until online verification.

Referenced by: Spec 25, 28, 35

# 3. Enforcement

- Primary enforcement is during spec review.

- QA scenarios in Spec 37 must cover invariants that involve offline, saves, time, and event boundaries.

- Telemetry in Spec 18 and Spec 37 provides observability for invariant compliance.

## ADDENDUM – POST AUDIT LOCKS

INV-11 Heat Model Definitions

Heat Tier is the coarse band (Peace, Notice, Concern, etc.).

Heat State is a numeric value within a tier.

Offline rebound may move at most one Heat State step and may not cross Heat Tiers.

INV-12 Dungeon Editing Save Safety

Tile placement and movement during edit mode must trigger immediate saves.

INV-13 Offline Crafting Constraints

Offline crafting must be deterministic, non-premium, and non-gating.

Economy-impacting crafting must be marked pending verification.

INV-14 Telemetry Reliability Rule

Telemetry may buffer offline and upload idempotently.

Strict acknowledgment applies only to economy-critical mutations.
