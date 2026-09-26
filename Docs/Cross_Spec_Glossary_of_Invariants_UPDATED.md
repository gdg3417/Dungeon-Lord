**Cross Spec Glossary of Invariants**


**Current repository status (2026-09-26):** Phase 3 is closed through merged PR #200, Phase 4 is complete through PR #207, the Phase 5 design lock is merged in PR #208, and Phase 5A is complete in PR #209. Current main baseline is PR #210 at `75781a4cfb7a7e837c855608f9a0753139a1bf77`; PR #210 makes the AI model-selection policy canonical. Current writable schema 10 owns optional-branch topology, `corridorContent`, and `sharedBranchKnowledge`; `DeadEnd = 6` is implemented. Phase 5B route choice, optional traversal, branch encounter resolution, branch-specific outcomes, run-driven knowledge learning, and production tuning remain unimplemented. Run-event mana, durable Core Level progression, research mana effects, active soft-cap tuning, and authoritative clock-cheat enforcement remain deferred.

**Historical Phase 3B2B status (2026-09-01), superseded by PRs #200–#202:** Schema-8 leaf deletion and returned-custody integration are present and under static review and required Unity validation. Phase 3 is not closed; Phase 4 remains unimplemented.
**Historical Phase 2B6B closeout status (2026-08-21), superseded by subsequent merged packets:** PR #195 activates schema 7 with `SaveMigration.LatestSchemaVersion = 7` and `LegacyCompatibilitySchemaVersion = 6`. Legacy schemas 1–6 use the production compatibility profile and the raw-before-legacy coordinator to migrate to canonical schema 7; native creation, canonical loading, transaction/recovery, and exact-byte canonical writes are live. The validated canonical graph/content state is the sole writable spatial authority, while `mvpDungeonPlacements`, `mvpDungeonFloorLayout`, and `mvpRoomSlotAssignments` are frozen read-only migration evidence; `dungeonLayout` remains independent writable nonspatial/economic state. The approved production-owned save workload profile is loaded from `Assets/_Project/Data/Production/Save/save_spatial_migration_limits.json`. Activation remains fail-closed outside qualified Windows Editor/Standalone local, nonredirected NTFS paths. Narrow Hall remains a repair-only legacy migration blocker, canonical placement omits it, and native R1→R2 structural construction remains deferred to Phase 3. Required automated, Editor, Windows Player qualification, and Windows x86_64 Development-build validation passed at `c4ba1f68985c18c2a6a62bcfd84c217e0cf07b06`; GD66 is complete pending merge of PR #195.**

**Historical Phase 2B6A status:** PR #194 is merged at `2bcc336f5fbbb9797f6f319f738e7b9f7d0613bd`; detached candidate construction, transaction/recovery, activation preflight, and Windows durability qualification are complete. Phase 2B6A adds a fail-closed Windows durability selector and activation preflight for Windows Editor/Standalone on local, nonredirected NTFS only. Live save schema remains **6** and schema 7 remains inactive. `SaveService`, `GameRoot`, native creation, canonical readers/writers, legacy writable authority, and ordinary gameplay remain unchanged; Phase 2B6B owns the final live activation.

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

NEW paid acquisition evaluates the existing RestrictedActionType.Purchase gate using current GameRoot.IsOnline and VerificationPending before canonical mutation; offline/pending attempts are blocked without state changes. Returned owned-content redeployment is not a new purchase and remains outside that gate. Repeated MONSTER acquisition is allowed while MonsterCapacity remains: every successful acquisition charges again and creates distinct AssignmentId/sequence; capacity rejection charges zero. Stale-session protection is separate from intentional current-session repeated acquisition. Trap/loot same-option live placement remains unchanged pending explicit approval. StartingMana 40 and the eight approved prices remain config-owned. PR #203 changed no schema; current schema 10 was introduced later by PR #209. See [final qualification evidence](testing/evidence/phase4-content-acquisition/static-review-evidence.md).

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

Canonical passive online mana resolves and rounds an authoritative mana/hour rate in that order, then converts it to a fractional configured-tick award. The current Research and Event/Season stages are neutral because no canonical effect authorities exist. Mana soft-cap support is configuration-owned and explicitly disabled without sentinel values until start/slope tuning is approved. The temporary configured `MvpBaselineCoreLevel` of 1 is not persisted progression. Active floors come from validated canonical spatial state, heat from `StructureRuntimeState.Heat`, the wallet from `StructureRuntimeState.ManaReserve`, and capacity from structural economy configuration. Future offline mana must consume this canonical applicable mana/hour result rather than reimplement its formula.

Canonical offline passive mana consumes that same applicable online mana/hour result at the validated configured base efficiency of 15%, preserves fractional wallet precision, and applies one capacity-clamped grant for exact valid forward elapsed time. Offline mana has no duration cap; the existing 86,400-second scaffold is diagnostic-only. Invalid/backward timestamps fail closed. Heat and active simulation remain frozen. The wallet and consumed save timestamp publish only after one detached current-schema candidate—schema 10 at the PR #210 baseline—is atomically persisted and durably read back. Structured result evidence is available for future security monitoring without becoming gameplay authority or a claim of reliable clock-cheat detection.

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

INV-15 Dungeon Floor Spatial Validity

Every active dungeon floor must remain within its configured spatial capacity, and every active room must be reachable from that floor's entrance through a valid saved same-floor route graph.

Physical tile footprints are the geometry, fit, occupancy, overlap, and used-floor-space authority. One occupied active structure tile equals one used floor-space unit. Rectangular bounds define legal coordinates, and final available capacity cannot authorize placement outside them. The exact buildable-tile unlock/expansion and modifier model remains an authored gate; it must not introduce weighted space. Mana price and content-specific capacities remain separate, content-owned concepts. Graph ordering is deterministic; saves use stable IDs.

Numbering convention: post-audit invariants continue the glossary sequence; therefore the spatial invariant following INV-14 is INV-15, independent of its owning specification number.

Referenced by: Spec 38, 19, 28, 30, 36, 37
