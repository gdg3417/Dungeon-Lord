# Backlog, Sprint and GD Traceability Matrix

> **GD61 authority note:** Sprint 2-4 are historical planning buckets, not the current execution order. The [post-GD60 MVP execution plan](post-gd60-mvp-execution-plan.md) is authoritative. “Completed” below is used only where merged GD history provides practical evidence; detailed validation remains in committed tests/evidence. Anything not evidenced is **requires confirmation**.

## Reconciled workstreams

| Historical requirement | Original ticket(s) | Implementation status | Relevant GD PR/workstream | Validation evidence | Remaining gap / disposition |
|---|---|---|---|---|---|
| Mana, heat, deterministic loop | S2-T00, S2-T03 | Partially completed | Pre-GD9 foundations; expanded GD10-GD60 | Runtime/EditMode suites; exact sprint closure requires confirmation | Construction spending/offline production gaps: Phases 4/9 |
| Dungeon placement categories/effects | S2-T00A / post-GD9 GD10 | Completed at prototype scope | PR #99, #101 | Placement/effects tests | Spatial contracts/geometry: Phases 1-3 |
| Composition-driven runs | S2-T05 / GD11 | Completed at prototype scope | PR #103 | Deterministic outcome tests | Graph routes/multi-floor: Phases 2, 5, 6 |
| Basic floor/node representation and editor | S2-T00A / GD12-GD13 | Partially completed | PRs #105-#106 | Layout/editor presentation tests | Ordered slots are not geometry/graph; Phases 1-3 and 7 |
| Loot and inventory handoff | S2-T06 / GD14 | Partially completed | PR #107; later loot/spoils PRs #146, #149-#150 | Loot/ledger tests | Content breadth and lifecycle confirmation: Phase 8/9 |
| Research lifecycle/unlock bridge | S2-T01 / GD15 | Partially completed | PR #108, GD59 PR #157 | Research/save/presentation tests | Architecture tree, branching, editor: Phases 5/8 |
| Trust UI/localization | S2-T07 | Partially completed | GD16 onward; MVP screen PR #113; GD53/GD55 | Presenter/localization/smoke suites | Production graphical editor/onboarding: Phases 7/9 |
| Room slots and ordered two-room route | Extension after historical sprint plan | Completed only for current representation | PRs #130-#142; GD60 PR #158 | Room-slot/route regression suites and GD60 runbook | Backward-compatible graph migration: Phases 1-2 |
| Save/migration reliability | S3-T01, S3-T03 | Partially completed | GD56 PR #154 | Save/lifecycle tests | Spatial version and migration fixtures: Phases 1-2/9 |
| Performance/build readiness | S3-T04, S3-T05 | Partially completed | GD58 PR #156 | Development-build diagnostics/tests | Spatial profiling, Android/external build: Phase 9 |
| Balance/onboarding/accessibility | S4-T01..T03 | Remaining; temporary usability work completed | GD55 PR #153; GD57 PR #155 | Prototype UI/pacing tests | Execute after editor/content stability: Phase 9 |
| Test override hooks | S4-T04 | Deferred / requires confirmation | No completion evidence asserted | None asserted | Only implement for concrete MVP validation need |
| Backend/live ops/prestige/social/monetization | Historical hardening/deferred specs | Deferred | No completion claimed | N/A | Post-MVP unless a testing blocker is proven |
| Locked main dungeon / up-to-five-floor maximum | Locked MVP source | Partial; one-floor prototype only | GD12-GD60 layout/route work | Layout/route tests; no multi-floor evidence claimed | Floor 2 in Phase 6; Floors 3-5 in Phase 8; validate Phase 9 |
| Idle offline mana | Locked MVP source / S2-T04 | Partial or requires confirmation | Earlier mana/offline foundations; no GD61 completion claim | Exact closure requires confirmation | Required Phase 4 packet and Phase 9 validation |
| Undead and Goblinoid launch families | Locked MVP source | Partial or requires confirmation | Starter content is not full-family evidence | Exact family breadth requires confirmation | Both families are required Phase 8 content; validate Phase 9 |
| Boss-set constraint | Locked MVP exclusion | Optional; no completion evidence claimed | No boss-set requirement inferred from GD history | Any requirement must come from another authoritative content spec | Capped at no more than one set per family; not independently required |
| Loot progression through Steel | Locked MVP source / S2-T06 | Partial; loot systems exist | GD14 and later loot/spoils work | Tier breadth requires confirmation | Required Phase 8; validate Phase 9 |
| One research slot | Locked MVP source / S2-T01 | Implemented at prototype scope; final integration remaining | GD15 and GD59 | Research/save/presentation tests | Required; Architecture/UI work Phases 5/8/9 |
| One Mana Farm sub-dungeon | Locked MVP source / MVP-MF01 | Remaining | No completion claimed | None asserted | Required Phase 8 after main spatial/economy contracts; validate Phase 9 |
| Peace, Notice and Concern heat states | Locked MVP source / S2-T03 | Partial; heat foundation exists | Earlier core-loop work | Exact three-state journey evidence requires confirmation | Required Phase 8/9; more than three excluded |
| Five adventurer classes | Locked MVP source / S2-T05 | Partial or requires confirmation | Encounter/adventurer work through GD60 | Exact class breadth requires confirmation | Required Phase 8; validate Phase 9 |

## Current phase mapping

| Active phase | Carries forward historical intent | Primary specification/gate |
|---|---|---|
| 0 GD61 reset | Planning reconciliation | This matrix, active plan, Spec 38 |
| 1 Spatial foundation | S2 layout + S3 schema/save rigor | Specs 19, 28, 30, 37, 38 |
| 2 Migration | S3 save/migration | Specs 17, 28, 38 |
| 3 Structural editing | S2 layout/editor | Specs 15, 19, 35, 38 |
| 4 Mana construction | S2 mana + research | Specs 01, 09, 30, 38 |
| 5 Branching | Architecture Basic Branching + encounter behavior | Specs 04, 06, 09, 38 |
| 6 Additional floor | Historical up-to-five-floor MVP intent | Specs 17, 28, 36, 38 |
| 7 Graphical editor | Historical editor/trust/accessibility intent | Specs 15, 26, 27, 35, 36, 38 |
| 8 Content/research | Loot, monsters, traps, identity, progression | Specs 03, 05, 09, 10, 19, 23, 31, 38 |
| 9 Validation/hardening | S3/S4 evidence goals | Specs 20, 26, 28, 33, 36, 37 |

## Preserved historical disposition

- Sprint 2 is **historical and partially completed**; acceptance criteria remain useful, but gaps are re-sequenced.
- Sprint 3 is **historical and partially completed**; dependency-critical save/build work exists, while broad production hardening is later.
- Sprint 4 is **historical and remaining**; temporary UI/pacing improvements are not production polish completion.
- Closeout checklists and evidence records remain unchanged historical governance artifacts. Their existence does not establish a pass.
- Earlier post-GD9 and vertical-slice plans are superseded as forward roadmaps; they remain evidence of why prototype capabilities were built.

## Unconfirmed items

Exact closure of old offline progression, verification reconciliation, content-CI, device budgets, onboarding/accessibility, and release-readiness tickets requires repository evidence confirmation. GD61 does not invent it. Current work should use phase exit criteria rather than reopening old sprint order.


# Historical Appendix — Original Sprint 2 through Sprint 4 Traceability

> The following material preserves the pre-GD61 source-document, ticket, sprint, status, notes, reconciliation, clarification flags, deferrals and test-governance references. These statuses describe the historical planning record and are not current implementation claims.

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

## Test Governance Artifacts
- Sprint 2 to Sprint 4 matrix: `docs/planning/test-stage-matrix.md`
- Sprint closeout checklists: `docs/planning/sprint-2-closeout-checklist.md`, `docs/planning/sprint-3-closeout-checklist.md`, `docs/planning/sprint-4-closeout-checklist.md`
- Build promotion policy: `docs/planning/build-promotion-policy.md`
