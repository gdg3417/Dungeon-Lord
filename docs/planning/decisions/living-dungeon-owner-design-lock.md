# Living Dungeon Owner Design Lock

| Field | Decision |
| --- | --- |
| Status | **Owner-approved design lock; implementation deferred** |
| Approval date | 2026-10-12 |
| Verified repository baseline | `39ed8aa160aef65f874cc8cc5609002c8fb5e9a4` (`main`, clean at inspection) |
| Scope | Living-dungeon simulation, offline time, Heat, mana, persistence, Phase 7 simulation/presentation, and roadmap order |
| Supersedes | Conflicting legacy offline-freeze, simulation-light, playback-only, and automatic cohort lifecycle requirements for future implementation |
| Companion authority | [Cross-Spec Glossary amendments](../../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md); [active execution roadmap](../post-gd60-mvp-execution-plan.md) |

## 1. Purpose and rationale

Dungeon Lord is a living dungeon. Parties arrive because of dungeon conditions, make decisions, traverse actual geometry, encounter consequences, and progress on authoritative game time. The player builds, observes and revises while that simulation continues. Completed-run animation playback is not a substitute for ongoing simulation.

This lock records approved gameplay direction and reconciles older specifications. It authorizes documentation only. At the verified baseline, the repository has deterministic one-shot run foundations, Heat and mana authorities, durable knowledge and save infrastructure, and Phase 7 graphical/editor work. It does **not** have persistent active parties, automatic arrival scheduling, background run advancement, resumable offline party catch-up, a waiting queue, or production ongoing-run presentation.

## 2. Owner-approved decisions

### World, admission, and time

1. Arrival scheduling is deterministic and responds to existing dungeon attraction, Heat and eligible traffic-pressure inputs. Every interval, cap and workload limit is configuration-owned; this lock chooses no numeric value.
2. A bounded waiting queue is the long-term design. MVP may throttle admissions at configured concurrent capacity without saving a waiting queue. The admission contract must permit a queue later without replacing run identity or scheduler authority.
3. Movement duration derives from actual traversable geometry. Interaction durations are configuration-owned. Animation and frame rate never set gameplay time.
4. Paths may be planned deterministically, but consequences advance incrementally at authoritative logical event boundaries. Simultaneous events have a documented deterministic total order independent of frame rate, device and selected floor.
5. Multiple independent parties may eventually occupy the dungeon concurrently. Spec 16 regional population/cohort lifecycle is expressly deferred for MVP; stable run IDs or an admission queue do not implement regional cohorts.
6. The player can edit during incomplete offline catch-up. Draft edits remain noncanonical. Canonical commits are ordered against completed earlier intervals so no edit retroactively changes a run that already started.
7. Normal foreground play remains available without internet. Existing verification requirements continue to govern restricted actions.

Spec 29's time classification remains: foreground activity remains foreground when internet is unavailable; background and closed-app intervals are offline and follow the approved offline simulation policy.

### Presentation and snapshots

8. A run receives immutable start inputs: dungeon/layout, applicable rules and compatible executable behavior. Later edits affect later runs only. Historical runs retain sufficient inputs and executable compatibility; a version string or hash alone is insufficient.
9. Only the selected floor needs graphical presentation. All other parties continue independent authoritative simulation. Selected parties may show historical route or geometry overlays when the run-start layout differs from the current dungeon.
10. The intended loop is build → run → inspect → revise, with real ongoing simulation and aggregate inspection. Playback of already completed outcomes cannot stand in for that loop.

### Heat and offline outcomes

11. Offline adventurer outcomes may increase or decrease numeric Heat. They cannot change the Heat Tier established at the accepted offline interval boundary. Foreground-eligible effects may cross tiers.
12. Preserve fractional Heat. For every finite Heat value valid under the applicable configured Heat range, assign a containing tier using its configured lower thresholds: a tier's lower threshold is inclusive and the next tier's lower threshold is exclusive; the final tier ends at the configured valid maximum. Preserve all existing valid fractional states without arbitrary epsilon or rounding. The active configuration currently starts at `HeatPeaceMinimum = 0`; the historical -15 minimum is not active. Negative-Heat support and its tier mapping require a separate future design/configuration decision and are neither activated nor clamped/discarded by this lock.
13. Retain the existing two-stage run Heat/cooling composition. This decision does not change fully foreground outcomes. Offline restrictions apply to offline-attributed contributions. For a run spanning foreground and offline periods, effects are attributed to the periods that caused them; foreground-eligible consequences may cross tiers, offline contributions may not. The causal mapping for aggregate bonuses and both cooling stages remains an implementation contract requiring validation against existing authorities; no new coefficients or formulas are approved here.
14. Excess offline Heat blocked by the tier boundary is discarded, not banked. No run-derived login rebound is added on top of Heat already caused by offline runs. A separate future rebound requires an independently approved cause.

### Mana, outcomes, and restricted actions

15. An offline-created party captures the mana reserve logically available at its admission timestamp, evaluated using the established canonical offline mana formula. Historical evaluation must not issue extra grants, replay normal online ticks, add a wallet, or change the configured 15% base offline efficiency and capacity-clamped one-time grant.
16. Valid offline intervals may advance ordinary adventurer parties and individually eligible ordinary run outcomes, including adventurer combat casualties, applicable survivor knowledge and eligible abstract loot extraction. The prohibition on offline raids and permanent losses to the player's dungeon remains in force; no new destructive player-dungeon consequence or raid mechanic is approved. Restricted rewards, research completion and gated actions retain their verification requirements. Each objective is individually classified for offline eligibility; an unrestricted settlement helper cannot automatically complete every objective.

### Persistence and catch-up

17. Eventually, unfinished parties, scheduler state and catch-up progress belong to one versioned canonical save authority. Schema 14 is provisional only; no fields, migration mapping, or next schema are approved until a concrete schema and migration design is reviewed.
18. MVP catch-up uses exact deterministic event processing, with mathematically equivalent acceleration where proven and bounded resumable batches. Arbitrary 1-, 6- or 12-hour sampling and outcome extrapolation are not approved gameplay authority.
19. A coarse historical model may be considered only as a measured performance optimization. Before activation it needs an explicit owner-approved model and comparison against exact reference outcomes.
20. Persistence failures recover the last proven committed prefix. They cannot duplicate settlement, fabricate events, or create a competing writable run journal.
21. Stable identities, determinism, config-owned tuning and bounded workloads, localization, atomic canonical changes, and backward-compatible save migration remain mandatory.

## 3. Current behavior and authority boundaries

The baseline implementation evidence establishes:

- The current run entry point is `GameRoot.SimulateRunOnce`; it resolves/applies a completed deterministic run, applies the persisted run Heat delta and then runs the separate `LootHeatCoolingResolver` step. The M6-FU0 decision intentionally retained both foreground stages.
- Canonical offline passive mana uses the canonical applicable online mana/hour rate, configured 15% base efficiency, fractional precision, one capacity-clamped grant for valid forward elapsed time, atomic wallet/timestamp persistence and structured evidence. It does not replay online mana ticks.
- Schema 13 is the writable save baseline. Schema 12 introduced durable `sharedFloorKnowledge`; knowledge is survivor-gated, captured in the run snapshot, and material content changes affect applicability while activation alone does not. Durable run history is coarse; detailed transition causes remain transient.
- Phase 7 has canonical geometry, transactional draft/editor foundations and production Dungeon composition work. Current graphical movement visualizes authority; ongoing parties and offline party simulation have not shipped.

These facts are compatibility inputs, not permission to create duplicate authority. Existing valid foreground behavior and the M6 two-stage Heat composition remain unchanged until a separately reviewed implementation proves and validates causal attribution.

## 4. Open implementation contracts

The following must be resolved with repository evidence and reviewed implementation designs; this lock deliberately supplies no invented tuning or persistence detail:

- Deterministic scheduler inputs, attraction/Heat/traffic composition, event identity, tie-break order, capacity behavior, and config ownership.
- Geometry-to-duration mapping and bounded event/workload representation; interaction timing config and exact-equivalence acceleration proof.
- Mixed foreground/offline causal attribution for aggregate Heat terms and both cooling stages, continuous fractional tier boundary representation, and the exact discarded-overflow calculation.
- Canonical offline mana-at-admission historical evaluation without side effects, including ordering when grants, admissions and save commits share a timestamp.
- Per-objective offline eligibility, survivor knowledge publication, loot/reward restrictions, research verification, and duplicate-safe settlement.
- Immutable executable-rule compatibility sufficient to resolve old runs, not only a saved version/hash.
- One canonical durable party/scheduler/catch-up owner, provisional schema review, migration plan, recovery prefix and resumable-batch atomicity.
- Exact-reference equivalence, mobile workload measurements and criteria for any future coarse historical optimization.
- Presentation reconstruction for a selected floor and historical geometry overlays without coupling camera selection to simulation.

Until those contracts are approved and implemented, the vision remains planned, not active gameplay.

## 5. Explicit deferrals

- Regional population or adventurer cohorts, circulation, retirement, gear distribution and region-level lifecycle from Spec 16: **deferred for MVP**.
- Persisted waiting queue: long-term only; admission throttling is acceptable for MVP.
- Sophisticated tactical AI, maze psychology, procedural generation, unrestricted paths, and high-concurrency offscreen rendering remain within their existing deferrals unless separately approved.
- Backend/server time, new verification capabilities, additional rewards, new research or objective behavior, and altered Heat tuning are not authorized by this lock.

## 6. Provisional next implementation sequence

The active roadmap records this relative order. It is a recommended dependency sequence, not a permanent three-PR partition; split a packet when implementation or migration evidence proves a dependency or qualification need.

A. **Resumable deterministic simulation engine and pure boundary calculations.** Specify event ordering, snapshots, geometry timing, Heat tier boundaries and causal attribution; establish pure exact reference/boundary calculations and resumable deterministic processing.
B. **Canonical durable active parties, automatic arrivals and bounded offline catch-up.** After the simulation contracts exist, add reviewed persistence/migration ownership, admission and exact resumable offline progress with eligibility and recovery rules.
C. **Production living-dungeon presentation and aggregate inspection.** Present selected-floor live runs and historical overlays, summarize other parties without rendering them, and complete the build-run-inspect-revise loop.

Do not start simulation implementation from this documentation change. The next intended implementation objective for a subsequent task is sequence A, beginning with its pure deterministic event and Heat boundary contracts against this design lock.

## 7. Reconciliation map

The [Cross-Spec Glossary addendum](../../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md#living-dungeon-owner-approved-amendments-2026-10-12) is the concise precedence authority. Narrow addenda are appended to Specs 2, 16, 17, 25, 28, 29 and 36; the [Phase 7A0 design lock](../phase-7a0-graphical-dungeon-editor-design-lock.md#living-dungeon-owner-approved-scope-addendum-2026-10-12) and [M6-FU0 decision](M6-FU0-active-run-cooling-composition.md#owner-approved-living-dungeon-addendum-2026-10-12) retain their historical records and state their supersedence. Older text remains evidence of prior decisions; only the identified future living-dungeon clauses are superseded. Unrelated online behavior and current shipped behavior remain in force.

The [Combat and Reserve-Backed Floor Recovery Owner Design Lock](combat-and-recovery-owner-design-lock.md), approved 2026-10-14, further governs future monster combat, per-floor party occupancy, monster recovery, and reserve-backed affordability. It supplements this Living Dungeon lock and the cross-spec glossary without changing its offline Heat, exact-event progression, canonical ordering, or no-offline-permanent-loss decisions.
