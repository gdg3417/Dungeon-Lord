# Sprint 2 Risks and Blockers

## Critical Gate

### R-01: Sprint 1 closeout evidence gate (Blocker)
- Description: Sprint 2 cannot start until `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` has complete [PASS] evidence for UAT-01..UAT-05.
- Impact: All Sprint 2 issues blocked.
- Affected issues: All S2 issues.
- Mitigation: Assign immediate QA/Release validation task to confirm evidence links and signoff.
- Start status: Blocked until validated.

## Planning and Sequencing Risks

### R-02: Mana foundation coverage missing or unverified
- Description: Mana tick, reserve pressure, and formula order requirements from Spec 01 and Spec 30 are underrepresented unless S2-T00 is executed.
- Impact: Core loop outputs and offline reconciliation can drift from locked specs.
- Affected issues: S2-T00-I01, S2-T04-I01, S2-T05-I02.
- Mitigation: Execute S2-T00-I01 before encounter downstream integration and add deterministic/replay persistence tests.
- Start status: Ready after Sprint 1 gate.

### R-03: Dungeon layout state missing or unverified
- Description: Encounter resolver inputs are not reliable without an explicit MVP dungeon layout and placement baseline.
- Impact: Encounter contract instability, rework risk, and save/load ambiguity.
- Affected issues: S2-T00A-I01, S2-T05-I01, S2-T05-I02.
- Mitigation: Freeze minimal layout contract early with immediate-save behavior and validation guards.
- Start status: Ready after Sprint 1 gate.

### R-04: Encounter-to-loot payload schema dependency
- Description: Loot resolution and inventory handoff depend on stable encounter payload schema.
- Impact: Integration delays and duplicate contract churn.
- Affected issues: S2-T05-I02, S2-T06-I01, S2-T06-I02.
- Mitigation: Freeze encounter event payload contract before full loot/inventory integration.
- Start status: Ready with sequencing control.

### R-05: Verification retry/backoff and terminal failure policy uncertainty
- Description: Retry cadence and ownership of failure UX policy are not finalized.
- Impact: S2-T02-I02 acceptance signoff risk; mismatch between system behavior and UX messaging.
- Affected issues: S2-T02-I02, S2-T07-I01, S2-T07-I02.
- Mitigation: Define temporary policy contract for Sprint 2, with explicit backlog carryover to Sprint 3 if needed.
- Start status: Ready (non-blocking clarification).

### R-06: Mana Farm is MVP scope but intentionally delayed
- Description: Mana Farm sub dungeon is in MVP scope, but starting it before core loop stabilization risks scope churn.
- Impact: Capacity fragmentation and reduced chance of Sprint 2 deterministic loop closure.
- Affected issues: Deferred MVP-MF01 placeholder and post-core-loop planning.
- Mitigation: Keep Mana Farm as deferred MVP item gated by core loop stability evidence.
- Start status: Deferred by sequencing policy.

## Clarification Risks

### R-07: Heat post-implementation tuning ownership clarity
- Description: Heat behavior and locked MVP constants are sufficient for implementation; only post-implementation tuning ownership needs confirmation.
- Impact: Low implementation risk; may affect later balance iteration workflow ownership.
- Affected issues: S2-T03-I01, S2-T04-I01.
- Mitigation: Assign balance owner for post-implementation tuning cadence and approvals.
- Start status: Ready (non-blocking clarification).

## Scope Statement
- The risks above are planning and sequencing risks for locked MVP scope.
- They are not requests to add non-MVP systems or expand feature scope.

## Test Governance Risks

### R-08: Unit, SIT, and UAT evidence not yet generated
- Description: Sprint 2 planning defines test obligations, but implementation evidence artifacts are not yet produced.
- Impact: Closeout cannot complete and promotion claims become unverifiable.
- Affected issues: All Sprint 2 issues and Sprint 2 closeout packet.
- Mitigation: Generate and link evidence per `docs/planning/sprint-2-closeout-checklist.md`.
- Start status: Open.

### R-09: SIT likely required for cross-system flows
- Description: Core loop flows cross layout, encounter, loot, heat, mana, research, and persistence boundaries.
- Impact: Unit-only validation can miss integration defects and sequencing faults.
- Affected issues: S2-T00A, S2-T00, S2-T01, S2-T04, S2-T05, S2-T06.
- Mitigation: Execute SIT checks and keep integration traces in closeout packet.
- Start status: Open.

### R-10: UAT likely required for player-facing loop clarity
- Description: Player trust depends on understandable layout, encounter outcomes, reserve pressure, pending verification messaging, and offline summaries.
- Impact: Technical pass without usability evidence can still fail MVP acceptance.
- Affected issues: S2-T00A, S2-T00, S2-T05, S2-T06, S2-T07, and Sprint 2B conditional checks.
- Mitigation: Execute scripted UAT checks and capture artifacts for each required step.
- Start status: Open.

### R-11: Build promotion should be blocked by missing evidence artifacts
- Description: Promotions without Unit, SIT, UAT, and build gate evidence introduce governance and release integrity risk.
- Impact: Invalid branch-to-PR, merge, or RC decisions.
- Affected issues: All sprint transitions starting with Sprint 2.
- Mitigation: Enforce `docs/planning/build-promotion-policy.md` blocker rules.
- Start status: Open.
