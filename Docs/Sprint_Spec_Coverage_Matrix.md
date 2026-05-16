# Sprint Spec Coverage Matrix (MVP Planning Traceability)

Date: 2026-05-15  
Purpose: Trace each design/system spec to sprint implementation, artifact output, and validation gate.

Status legend:
- `Planned` = scheduled in current forward plan
- `Deferred` = intentionally out of MVP implementation for now
- `Reference` = informs work but no direct implementation in current sprint window

## A) Master and system spec coverage (`00`–`37`)

| Spec ID | Requirement focus (short) | Sprint target | Owner role(s) | Primary artifact(s) | Validation gate | MVP scope | Status |
|---|---|---|---|---|---|---|---|
| 00 | Lock summary + consolidated constraints | S2/S3 | Engineering, QA | planning constraints + acceptance rules | checklist verification | In-MVP | Planned |
| 01 | Mana system | S2 | Engineering, QA | offline progression + mana updates | deterministic unit tests | In-MVP | Planned |
| 02 | Heat system | S2 | Engineering, QA | heat decay/increase integration | boundary tests | In-MVP | Planned |
| 03 | Monster stats/upkeep | S2 | Engineering, Design | encounter stat inputs | spawn/outcome tests | In-MVP | Planned |
| 04 | Adventurer evaluation v1 | S2 | Engineering, Design | encounter evaluator baseline | deterministic outcome tests | In-MVP | Planned |
| 05 | Loot tables/value/attraction | S2 | Engineering, Data | loot resolver + extraction mapping | data integrity + edge tests | In-MVP | Planned |
| 06 | Party AI | S2 | Engineering, Design | party behavior baseline hooks | encounter consistency tests | In-MVP | Planned |
| 07 | Political/economic/guild reputation | Deferred | Design | notes only | n/a | Deferred | Deferred |
| 08 | Raids/escalation | S2 | Engineering, Design | baseline raid encounter hooks | progression loop tests | In-MVP | Planned |
| 09 | Progression/research | S2 | Engineering | research queue domain | state-machine tests | In-MVP | Planned |
| 10 | Identity/specialization | Reference | Design | taxonomy references | design review | In-MVP | Reference |
| 11 | Failure recovery/soft loss | Reference | Design, QA | failure-state heuristics | UAT checks | In-MVP | Reference |
| 12 | Monetization philosophy | Deferred | Design | language guidance only | n/a | Deferred | Deferred |
| 13 | Live ops/world events | Deferred | Release, Design | planning notes only | n/a | Deferred | Deferred |
| 14 | Progression loops/retention | S4 | Design, Engineering | tuning + retention pass | KPI review | In-MVP | Planned |
| 15 | UI information/trust | S2 | Engineering, Design | pending/restricted status UI | UX/UAT checklist | In-MVP | Planned |
| 16 | External world simulation | S4 | Design, Engineering | economy tuning extensions | balance review | In-MVP | Planned |
| 17 | Save state/offline sim | S2/S3 | Engineering, QA | offline orchestrator + save contracts | cap/fixture tests | In-MVP | Planned |
| 18 | Analytics/telemetry | S2/S3 | Engineering, QA | baseline counters + dashboards | telemetry event checks | In-MVP | Planned |
| 19 | Content pipeline/data authoring | S2/S3 | Data, Engineering | integrity validation rules | CI content checks | In-MVP | Planned |
| 20 | Tutorial/onboarding | S2/S4 | Design, Engineering | onboarding hooks + polish | first-session UAT | In-MVP | Planned |
| 21 | Economy sinks/deflation | S4 | Design, Engineering | tuning rules | economy balancing review | In-MVP | Planned |
| 22 | Prestige/seasonal resets | Deferred | Design | backlog notes only | n/a | Deferred | Deferred |
| 23 | Inventory/storage lifecycle | S2 | Engineering, Data | loot-to-inventory path | lifecycle tests | In-MVP | Planned |
| 24 | Social/competitive | Deferred | Design | backlog notes only | n/a | Deferred | Deferred |
| 25 | Security/anti-cheat | S3 | Engineering, QA | integrity checks + conflict handling | replay/reconcile tests | In-MVP | Planned |
| 26 | Accessibility/cognitive load | S4 | Design, QA | readability/accessibility pass | accessibility checklist | In-MVP | Planned |
| 27 | Localization/text system | S2 | Engineering, Design | loc key routing for new UI | key coverage check | In-MVP | Planned |
| 28 | Save model/version/migration | S3 | Engineering, QA | migration fixtures + partition model | migration matrix tests | In-MVP | Planned |
| 29 | Time model/tick resolution | S2/S3 | Engineering, QA | canonical elapsed-time rules | determinism + skew tests | In-MVP | Planned |
| 30 | Formula framework/modifier stacking | S2/S3 | Engineering, Data | formula order enforcement | formula consistency tests | In-MVP | Planned |
| 31 | Identity/theming resolution | S4 | Design | thematic consistency pass | design signoff | In-MVP | Planned |
| 32 | Event framework/rule overrides | S4 | Engineering, Design | limited override plumbing | event override tests | In-MVP | Planned |
| 33 | Build/release strategy | S3 | Release, Engineering | environment strategy + CI flow | release checklist | In-MVP | Planned |
| 34 | Backend/API contract | S3 | Engineering, Release | restricted-action contract stabilization | contract/replay tests | In-MVP | Planned |
| 35 | Error handling/player trust | S2 | Engineering, Design | explicit pending/failure messaging | UX/UAT trust checks | In-MVP | Planned |
| 36 | Performance/memory/device targets | S3 | Engineering, QA | budget profiles + benchmarks | perf regression gate | In-MVP | Planned |
| 37 | QA strategy/test harness | S2/S3 | QA, Engineering | deterministic replay + CI gates | full QA matrix | In-MVP | Planned |

## B) Design doc alignment coverage

| Design doc | Sprint target | Incorporation approach | Status |
|---|---|---|---|
| Dungeon Builder Game Design Doc v2 | S2/S4 | informs core-loop UX and retention tuning sequence | Planned |
| What is the smallest version of Dungeon Builder that proves the fantasy is fun | S2 | used to constrain vertical-slice success criteria | Planned |
| Dungeon_Builder_Architecture_Spec_v1 | S2/S3 | informs service boundaries and partitioning strategy | Planned |
| Dungeon_Builder_CoreTech_Spec_v1 | S3 | informs reliability, tooling, and release hardening | Planned |
| Dungeon_Builder_Monsterology_Spec_v1 | S2 | encounter and content baseline references | Planned |
| Dungeon_Builder_Trapcraft_Spec_v1 | S2/S4 | encounter hazard baseline + polish follow-ups | Planned |
| Dungeon_Builder_Arcanology_Spec_v1 | S2/S4 | research/magic loop references + balance | Planned |
| Dungeon_Builder_Diplomacy_Spec_v1 | Deferred | tracked for future expansion beyond MVP slice | Deferred |

## C) Immediate execution next actions (ticketization)
1. Convert S2-01..S2-07 into ticket IDs with assignees and estimate ranges.
2. Add acceptance checklists directly to each ticket from this matrix `Validation gate` column.
3. Add sprint board labels: `spec:<id>`, `mvp:in`, `mvp:deferred`, and `risk:high/med/low`.
4. Publish weekly coverage rollup: `% specs with implementation artifacts started` and `% gates passed`.
