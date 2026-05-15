# Issue: Pending/Restricted State UI Bindings

Parent Ticket: S2-T07
Issue ID: S2-T07-I01
Sprint: Sprint 2
Priority: Must Have
Type: UI
Status: Ready

## Goal
Expose pending verification, restricted action, and loop outcome states in Sprint 2 UI surfaces for player trust and debuggability.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T07)
- Docs/15 - UI Information Exposure and Player Trust.md
- Docs/35 - Error_Handling_Player_Messaging_and_Trust.md

## Requirements
- Display pending verification states for relevant actions.
- Display restricted-action reason states.
- Display encounter + loot outcome summary hooks.

## Acceptance Criteria
- Given pending verification state, when UI refreshes, then pending status is visible in expected surface.
- Given restricted action attempt, when blocked, then reason state is displayed consistently.
- Given missing status payload, when UI attempts render, then safe fallback state appears (no crash/no broken UI).

## Implementation Notes
- Bind to system state outputs from S2-T01/S2-T02/S2-T05/S2-T06.
- Keep implementation minimal (no broad polish).
- Do not add new gameplay behavior.

## Tests Required
- Pending state rendering test.
- Restricted reason rendering test.
- Missing payload fallback rendering test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- Requires upstream state outputs from S2-T02 and encounter/loot integration.

## Non-Goals
- Full onboarding sequence polish.
- Visual redesign.
