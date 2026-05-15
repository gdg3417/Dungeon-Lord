# Backlog to Sprint Traceability Matrix

## Ticket Mapping Matrix

| Source Document | Source Requirement or Section | Epic | Ticket ID | Sprint | Status | Notes |
|---|---|---|---|---|---|---|
| docs/planning/actionable-backlog.md | Recommended Next Sprint (stories 1-7) | Playable Core Loop Vertical Slice | S2-T01..S2-T07 | Sprint 2 | Modified | Converted to ticket-ready slices with testable AC. |
| Docs/01 - Mana System v3.md | Mana tick, reserve pressure, and pool invariants | Playable Core Loop Vertical Slice | S2-T00 | Sprint 2 | Added | Explicit mana foundation coverage with deterministic and persistence tests. |
| Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md; Docs/What is the smallest version of Dungeon Builder that proves the fantasy is fun.md | Minimal dungeon layout, placement, and edit-mode immediate save for encounter inputs | Playable Core Loop Vertical Slice | S2-T00A | Sprint 2 | Added | MVP layout baseline limited to one dungeon and up to five floors. |
| Docs/09 - Dungeon_Progression_and_Research.md | Single-slot research progression | Research and Verification-Safe Progression | S2-T01 | Sprint 2 | Modified | State machine + persistence + pending verify path. |
| Docs/25 - Security_Anti_cheat_and_Economy_Integrity.md | Economy-critical verification integrity | Research and Verification-Safe Progression | S2-T02, S3-T02 | Sprint 2/3 | Split | Queue in S2; reconciliation hardening in S3. |
| Docs/34 - Backend_Services_and_API_Contract.md | Idempotent contract/replay behavior | Research and Verification-Safe Progression | S2-T02, S3-T02, S3-T05 | Sprint 2/3 | Modified | Contract handling split across implementation and release readiness. |
| Docs/02 - Heat System v3.md | Heat gain/reduction/tier bounds | Heat and Risk-Reward Regulation | S2-T03 | Sprint 2 | Existing | Locked MVP constants are sufficient for implementation; later tuning authority is a non-blocking balance clarification. |
| Docs/17 - Save_State_Offline_Simulation_and_Time_Handling.md | Offline elapsed + save integrity | Playable Core Loop / Reliability Hardening | S2-T04, S3-T01 | Sprint 2/3 | Modified | Execution split by capability phase. |
| Docs/03,04,06,08 | Encounter and party baseline behavior | Playable Core Loop Vertical Slice | S2-T05 | Sprint 2 | Split | Kept baseline only; advanced AI deferred. |
| Docs/05,19,23 | Loot + content integrity + inventory lifecycle | Playable Core Loop Vertical Slice | S2-T06 | Sprint 2 | Modified | Added explicit invalid-reference fail behavior. |
| Docs/15,27,35 | Trust UI + localization + error messaging | Playable Core Loop Vertical Slice | S2-T07, S4-T02 | Sprint 2/4 | Modified | Sprint 2 minimum transparency, Sprint 4 polish. |
| Docs/28 - Save_Data_Model_Versioning_and_Migration.md | Save versioning and migration matrix | Reliability and Data Integrity Hardening | S3-T01, S3-T03 | Sprint 3 | Existing | Adds fixture-based compatibility gating. |
| Docs/19 - Content_Pipeline_and_Data_Authoring.md | Validator and authoring integrity | Reliability and Data Integrity Hardening | S3-T03 | Sprint 3 | Split From Existing | Promoted to standalone CI gate ticket. |
| Docs/36 - Performance_Memory_and_Device_Support_Targets.md | Performance budget targets | Reliability and Data Integrity Hardening | S3-T04 | Sprint 3 | Existing | Device profile list Needs clarification. |
| Docs/33 - Build_Release_and_Environment_Strategy.md | Release process + environment gating | Release Readiness | S3-T05 | Sprint 3 | Modified | Adds go/no-go dashboard + mandatory evidence blocking. |
| Docs/14,16,21,18 | Retention and economy tuning via telemetry | MVP Vertical Slice Polish | S4-T01 | Sprint 4 | Existing | Tuning-only scope; no new feature systems. |
| Docs/20 - Tutorial_Onboarding_and_Unlock_Sequencing.md | First-session guidance sequence | MVP Vertical Slice Polish | S4-T02 | Sprint 4 | Modified | Converted to testable onboarding ticket. |
| Docs/26 - Accessibility_and_Cognitive_Load_Targets.md | Accessibility pass targets | MVP Vertical Slice Polish | S4-T03 | Sprint 4 | Existing | Checklist evidence required. |
| Docs/32 - Event_Framework_and_Rule_Overrides.md | Internal test override support only | MVP Vertical Slice Polish | S4-T04 | Sprint 4 | Modified | Internal-only test override hooks; not player-facing live-ops events. |
| Docs/00 - All Design Specs_AUDITED_AND_LOCKED.md; Docs/What is the smallest version of Dungeon Builder that proves the fantasy is fun.md | One sub dungeon type in MVP: Mana Farm, gated until core loop stability | Deferred MVP Scope Management | MVP-MF01 (Deferred MVP) | Post-Sprint 2A | Added | MVP scope retained but deferred until deterministic core loop validation is stable. |
| Docs/07,12,13,22,24 | Non-MVP features | Deferred Scope Management | N/A (Deferred) | Deferred | Deferred | Diplomacy depth, monetization impl, live-ops, prestige, social excluded. |

## Existing Sprint Reconciliation

### File: Docs/Sprint1_Completion_Plan_2026-05-13.md
- Items preserved as-is:
  - Sprint sequencing intent: S2 core loop, S3 hardening, S4 polish.
  - Deferred non-MVP scope exclusions.
- Items modified:
  - Converted S2/S3/S4 build steps into discrete ticket units with explicit invalid-state AC.
  - Added explicit "Needs clarification" flags where execution blockers exist.
- Items split:
  - S2-02 verification pipeline split from S3 reconciliation hardening.
  - Encounter and loot items split into separate sprint-sized tickets.
- Items moved:
  - Onboarding deep polish moved to Sprint 4, while minimum trust state UI remains Sprint 2.
- Items deferred:
  - No change to deferred non-MVP systems; preserved with explicit deferral notes.
- Items newly added:
  - Sprint 3 CI content integrity gate as standalone execution ticket (S3-T03).
  - Sprint 3 release go/no-go dashboard workflow ticket (S3-T05).
- Rationale for each change:
  - Reduce oversized sprint items into testable work packets and align with gap assessment dependency order.

### File: Docs/Sprint_Spec_Coverage_Matrix.md
- Items preserved as-is:
  - Spec-to-sprint high-level placement and deferred statuses.
- Items modified:
  - Added ticket-level granularity (S2-Txx / S3-Txx / S4-Txx mapping) in planning layer.
- Items split:
  - Spec 25/34 work split into S2 queue implementation and S3 conflict reconciliation.
- Items moved:
  - No spec moved across sprint families beyond granularity refinements.
- Items deferred:
  - Spec 07/12/13/22/24 remain deferred.
- Items newly added:
  - Explicit matrix link between source sections and ticket IDs.
- Rationale for each change:
  - Keep original coverage strategy while making execution board-ready.

### File: Docs/Sprint1_Closeout_Checklist_2026-05-13.md
- Items preserved as-is:
  - UAT-01..UAT-05 closure gates and evidence requirements.
- Items modified:
  - None in source file; consumed as Sprint 2 prerequisite in planning.
- Items split:
  - N/A.
- Items moved:
  - N/A.
- Items deferred:
  - N/A.
- Items newly added:
  - Planning-level dependency: Sprint 2 tickets blocked until closeout evidence confirmed.
- Rationale for each change:
  - Maintain governance continuity between sprint closeout and sprint kickoff.

## Not Sprint-Ready / Needs Clarification Flags
1. Heat tuning authority for post-implementation balance iteration (affects S2-T03) - non-blocking for MVP implementation.
2. Retry/backoff + terminal-failure UX ownership for verification queue (affects S2-T02).
3. MVP device profile list for perf gates (affects S3-T04).
4. Approved MVP-safe override scenarios for Spec 32 subset (affects S4-T04).

## Deferred Items (Explicit)
- Non-MVP diplomacy depth (Spec 07).
- Monetization implementation (Spec 12).
- Live-ops/world events full system (Spec 13).
- Prestige/seasonal reset systems (Spec 22).
- Social/competitive systems (Spec 24).
