# Sprint 4 Ticket Backlog (Onboarding, Tuning, Accessibility, MVP-safe Hooks)

## Ticket: Telemetry-Driven Core Loop Balance Pass

Ticket ID: S4-T01
Epic: MVP Vertical Slice Polish
Feature Area: Core Gameplay Loop / Resource Economy
Priority: Must Have
Sprint: Sprint 4
Status: Existing
Source References:
- docs/planning/actionable-backlog.md (Sprint 4 recommended purpose)
- Docs/14 - Player Progression Loops and Retention.md
- Docs/16 - Adventurer_Economy_and_External_World_Simulation.md
- Docs/21 - Economy_Sinks_and_Late_Game_Deflation_Control.md
- Docs/18 - Analytics_Telemetry_and_Balance_Instrumentation.md

User Story:
As a designer,
I want telemetry-backed tuning of mana/heat/encounter/loot pacing,
so that early-session progression is understandable and engaging.

Functional Requirements:
- Review telemetry trends for first-session loop completion.
- Adjust tuning values within locked-system constraints.
- Document changes and expected KPI impact.

Technical Requirements:
- Use existing telemetry schema and KPI reports.
- Preserve deterministic formula framework order.
- Keep changes data-driven where possible.

Acceptance Criteria:
- Given baseline KPI report, when tuning pass is executed, then updated values and rationale are documented.
- Given unchanged simulation inputs except tuned parameters, when deterministic tests run, then behavior remains deterministic.
- Given proposed tuning outside MVP constraints, when reviewed, then change is rejected or marked deferred.

Implementation Tasks:
- Produce baseline KPI snapshot.
- Apply bounded parameter tuning in approved data tables/config.
- Re-run deterministic + KPI comparison checks.

Test Cases:
- Pre/post KPI comparison test.
- Determinism regression test after tuning.
- Constraint compliance review test.

Dependencies:
- Sprint 2 and 3 telemetry/reliability gates.
- Approved KPI targets. **Needs clarification**.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- No new systems; tuning only.

## Ticket: First-Session Onboarding Clarity Pass

Ticket ID: S4-T02
Epic: MVP Vertical Slice Polish
Feature Area: UI/UX Transparency, Error Handling, and Onboarding
Priority: Must Have
Sprint: Sprint 4
Status: Modified
Source References:
- docs/planning/actionable-backlog.md (Sprint 4 recommended purpose)
- Docs/20 - Tutorial_Onboarding_and_Unlock_Sequencing.md
- Docs/15 - UI Information Exposure and Player Trust.md
- Docs/35 - Error_Handling_Player_Messaging_and_Trust.md

User Story:
As a player,
I want first-session guidance to explain loop actions and consequences,
so that I can complete the MVP loop without confusion.

Functional Requirements:
- Sequence onboarding hints for core loop actions.
- Clarify pending/restricted/error states in context.
- Validate comprehension checkpoints for first loop completion.

Technical Requirements:
- Localized text key usage for onboarding content.
- Reusable onboarding trigger/state tracking.
- UAT script updates for first-session flow.

Acceptance Criteria:
- Given a new profile, when first session runs, then onboarding steps for core loop appear in intended sequence.
- Given pending/restricted state during onboarding, when displayed, then messaging explains next expected action.
- Given missing onboarding trigger state, when flow continues, then safe fallback path prevents blocking progression.

Implementation Tasks:
- Map onboarding steps to loop milestones.
- Update localized content keys/messages.
- Add first-session UAT/checklist coverage.

Test Cases:
- New-profile onboarding sequence test.
- Pending/restricted onboarding message test.
- Missing trigger fallback test.

Dependencies:
- Sprint 2 trust UI outputs.
- Localization content review.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Keep scope to MVP first-session only.

## Ticket: Accessibility and Cognitive Load Compliance Pass

Ticket ID: S4-T03
Epic: MVP Vertical Slice Polish
Feature Area: Accessibility and QA
Priority: Should Have
Sprint: Sprint 4
Status: Existing
Source References:
- docs/planning/actionable-backlog.md (Sprint 4 recommended purpose)
- Docs/26 - Accessibility_and_Cognitive_Load_Targets.md
- Docs/15 - UI Information Exposure and Player Trust.md

User Story:
As a player,
I want readable, low-friction interfaces,
so that I can understand the game state without cognitive overload.

Functional Requirements:
- Validate readability/contrast and information hierarchy for MVP surfaces.
- Reduce ambiguous state presentations.
- Document accessibility exceptions that cannot be resolved in MVP.

Technical Requirements:
- Accessibility checklist execution artifact.
- UI key/label consistency checks.
- QA evidence capture with reproducible steps.

Acceptance Criteria:
- Given MVP UI screens, when accessibility checklist runs, then required items are marked pass/fail with evidence.
- Given identified high-cognitive-load surface, when revised, then clarity metric/checklist item improves.
- Given unresolved accessibility blocker, when sprint closes, then blocker is recorded with owner and defer rationale.

Implementation Tasks:
- Execute accessibility checklist against MVP UI.
- Apply targeted clarity improvements within scope.
- Publish pass/fail evidence and unresolved items.

Test Cases:
- Contrast/readability checklist test.
- State-label clarity consistency test.
- Exception logging completeness test.

Dependencies:
- S4-T02 onboarding pass.
- QA checklist framework.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Any broad redesign beyond MVP should be deferred.

## Ticket: MVP-safe Event Override Hooks (Limited)

Ticket ID: S4-T04
Epic: MVP Vertical Slice Polish
Feature Area: Event Framework and Rule Overrides
Priority: Could Have
Sprint: Sprint 4
Status: Needs Clarification
Source References:
- docs/planning/actionable-backlog.md (Open question + Sprint 4 event subset)
- Docs/32 - Event_Framework_and_Rule_Overrides.md
- Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md

User Story:
As a system,
I want limited event override hooks for approved MVP scenarios,
so that controlled tuning/testing can occur without introducing full live-ops complexity.

Functional Requirements:
- Implement only explicitly approved override cases tied to MVP testing/tuning.
- Prevent unsupported broad event-system behaviors.
- Ensure overrides are auditable and reversible.

Technical Requirements:
- Scope-locked override registry.
- Deterministic application order with formula framework compliance.
- Override activation logging.

Acceptance Criteria:
- Given an approved override case, when activated, then only scoped rule changes apply.
- Given no active overrides, when simulation runs, then baseline behavior is unchanged.
- Given unsupported override request, when attempted, then system rejects request with explicit reason.

Implementation Tasks:
- Define approved MVP override list and constraints.
- Implement scoped override application path.
- Add baseline-vs-override regression tests.

Test Cases:
- Approved override application test.
- No-override baseline equivalence test.
- Unsupported override rejection test.

Dependencies:
- Clarified approved override scenarios. **Needs clarification**.
- Formula order and determinism checks.

Definition of Done:
- Code or planning artifact change completed.
- Acceptance criteria verified.
- Tests added or updated.
- Source references preserved.
- No unsupported feature expansion.

Notes:
- Defer broad live-ops event platform behavior.
