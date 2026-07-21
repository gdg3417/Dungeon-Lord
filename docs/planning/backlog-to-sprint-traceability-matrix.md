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
