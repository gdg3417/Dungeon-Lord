# Issue: Loot Table Resolution and Validation

Parent Ticket: S2-T06
Issue ID: S2-T06-I01
Sprint: Sprint 2
Priority: Must Have
Type: Content
Status: Ready

## Goal
Implement data-driven loot resolution with integrity validation and deterministic edge-case handling.

## Source References
- docs/planning/sprint-2-ticket-backlog.md (S2-T06)
- Docs/05 - Loot Tables  Value  Attraction.md
- Docs/19 - Content_Pipeline_and_Data_Authoring.md

## Requirements
- Resolve loot using configured tables and caps.
- Validate references used by loot definitions.
- Handle empty/invalid/capped tables deterministically.

## Acceptance Criteria
- Given valid table references, when loot resolves, then output follows configured rules.
- Given empty/capped table edge case, when resolved, then deterministic fallback behavior is applied.
- Given invalid table reference, when resolve runs, then safe failure occurs and no invalid reward is emitted.

## Implementation Notes
- Keep resolver data-driven and schema-aligned.
- Ensure deterministic randomization under seed.
- Sequence before inventory handoff integration.

## Tests Required
- Valid table resolution test.
- Empty/capped table deterministic fallback test.
- Invalid reference safe-failure test.

## Definition of Done
- Implementation complete.
- Tests pass.
- Acceptance criteria verified.
- No unsupported feature expansion.
- Source traceability preserved.

## Blockers
- None.

## Non-Goals
- New loot tiers beyond MVP limit.
- Long-term economy balancing pass.
