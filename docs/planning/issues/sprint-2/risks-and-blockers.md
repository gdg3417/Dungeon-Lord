# Sprint 2 Risks and Blockers

## Critical Gate

### R-01: Sprint 1 closeout evidence gate (Blocker)
- Description: Sprint 2 cannot start until `Docs/Sprint1_Closeout_Checklist_2026-05-13.md` has complete ✅ evidence for UAT-01..UAT-05.
- Impact: All Sprint 2 issues blocked.
- Affected issues: All S2 issues.
- Mitigation: Assign immediate QA/Release validation task to confirm evidence links and signoff.
- Start status: Blocked until validated.

## Clarification Risks

### R-02: Heat post-implementation tuning ownership clarity
- Description: Heat behavior and locked MVP constants are sufficient for implementation; only post-implementation tuning ownership needs confirmation.
- Impact: Low implementation risk; may affect later balance iteration workflow ownership.
- Affected issues: S2-T03-I01, S2-T04-I01.
- Mitigation: Assign balance owner for post-implementation tuning cadence and approvals.
- Start status: Ready (non-blocking clarification).

### R-03: Verification retry/backoff and terminal failure policy uncertainty
- Description: Retry cadence and ownership of failure UX policy are not finalized.
- Impact: S2-T02-I02 acceptance signoff risk; mismatch between system behavior and UX messaging.
- Affected issues: S2-T02-I02, S2-T07-I01, S2-T07-I02.
- Mitigation: Define temporary policy contract for Sprint 2, with explicit backlog carryover to Sprint 3 if needed.
- Start status: Ready (non-blocking clarification).

## Split-Dependency Risks

### R-04: Encounter/Loot split introduces integration ordering risk
- Description: Splitting encounter contracts/events and loot/inventory handoff improves sizing but adds handoff dependency points.
- Impact: Delay risk if payload schemas are not frozen early.
- Affected issues: S2-T05-I01, S2-T05-I02, S2-T06-I01, S2-T06-I02.
- Mitigation: Freeze event payload contract before full parallel work; add schema compatibility tests.
- Start status: Ready with sequencing control.

### R-05: UI bindings may start before stable state contracts
- Description: UI issue depends on outputs from verification, encounter, and loot flows.
- Impact: Rework in S2-T07-I01 if contracts change late.
- Affected issues: S2-T07-I01.
- Mitigation: Delay final UI wiring until event/state contract signoff; begin with placeholder adapters only.
- Start status: Conditionally Ready.

## Tickets That Should Not Start Until Clarification Resolves
- S2-T03-I01 can proceed and complete using locked MVP constants; tuning ownership clarification is follow-up.
- S2-T02-I02 should not be marked complete until retry/backoff policy and terminal-failure handling ownership are resolved.

## Recommended Blocker Triage Order
1. Validate Sprint 1 UAT evidence gate.
2. Confirm post-implementation heat tuning ownership and review cadence.
3. Resolve verification retry/backoff policy and ownership.
4. Lock encounter-to-loot event payload schema.
5. Start UI final wiring and localization completion.
