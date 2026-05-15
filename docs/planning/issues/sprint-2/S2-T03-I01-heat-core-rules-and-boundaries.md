# Issue: Heat Core Rules and Boundary Enforcement

Parent Ticket: S2-T03
Issue ID: S2-T03-I01
Sprint: Sprint 2
Priority: Must Have
Type: Feature
Status: Ready

## Goal
Implement heat gain/reduction logic with tier/state boundary enforcement and invariant-safe clamping.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T03)
- Docs/02 - Heat System v3.md
- Docs/29 - Time_Model_and_Tick_Resolution.md

## Requirements
- Apply heat gain from normal/elite deaths and multi-death bonus.
- Apply reduction from full-party survival and extracted tradeable value.
- Clamp to valid tier/state bounds and preserve invariants.

## Acceptance Criteria
- Given threshold boundaries, when heat crosses a band, then tier/state transition is correct.
- Given full-party survival and loot extraction value, when reduction applies, then heat decreases per configured rules.
- Given invalid heat input (underflow/overflow), when applied, then value is clamped safely with no invariant break.

## Implementation Notes
- Keep threshold and coefficients data-driven.
- Add guardrails for offline rebound rules.
- Implement using locked MVP constants; treat later tuning authority as balance follow-up only.

## Tests Required
- Tier boundary crossing test.
- Survival/value reduction rule test.
- Clamp safety test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None for implementation. Locked MVP constants are sufficient; later tuning authority is a non-blocking balance clarification.

## Non-Goals
- Late-game balancing pass.
- New heat tiers or states.
