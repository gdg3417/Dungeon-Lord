# Phase 8 Audit Disposition — F01–F16 at Merged PR #245

**Status:** Owner-design review complete, **technical specification implementation/QA incomplete**. Prepared 2026-10-20.  
**Repository:** `gdg3417/Dungeon-Lord` `main` = `9b63400662c7d3534fe8443a43e7c1999384a84d`, merged [PR #245](https://github.com/gdg3417/Dungeon-Lord/pull/245).  
**Inputs:** v41 cumulative owner register (D01–D201 where present), audit ledger v7, detailed F06–F08 / F10–F14 / F15–F16 review packets; repository owner locks and source code.  
**Governing target:** [Owner design lock](decisions/phase-8-owner-design-lock.md). The full historical audit source remains an external transfer reference, not a second runtime authority.

## Meaning of statuses

- **Owner policy CLOSED**: the choice needed to define behavior is approved, **not** necessarily implemented.
- **Engineering/content OPEN**: precise source ownership, stable IDs, save contracts, runtime integration, authored values or migration require later reviewed implementation.
- **QA OPEN**: no passing comprehensive final evidence exists for that feature; earlier B3B2-specific passing checks do not qualify new Phase 8 paths.
- **No new owner vote needed now**: does not preclude a narrow question if a real incompatible implementation alternative arises during technical design.

## Full finding matrix

| Finding / severity | Owner-policy disposition | Current #245 boundary / concrete concern | Remaining must-pass closure gate |
|---|---|---|---|
| **F01 P0 Research slots/payment/verification** | **CLOSED** D106–D109, D138, D139, D196 | Local-MVP claim exists but research bridge still one slot; flags do **not** charge costs or process offline research; production claim false | Two independently payable usable-mana tracks; migration/reopen; deterministic local offline progress; real online verification **before public production release** |
| **F02 P0 Living Dungeon** | **CLOSED direction** in living/combat owner locks | **Improved baseline:** #245 shipped single Active Floor 1 / party auto foreground C2 + atomic terminal settlement and accepted edits; *not* general multi-floor/offline | Authenticated effective clock and causal boundaries where required; multiple distinct-floor parties/descend waiting; complete exact offline event catch-up, save/recovery and device tests |
| **F03 P0 Paid persistent loot stock** | **CLOSED** D130/D189–D194 | Current abstract one-roll-per-cleared-room system is not persistent per-node paid item ownership | Stable generated-item cost receipts; safe vacancy refill in canonical order; one wallet; snapshot/occupancy and atomic exact-once tests |
| **F04 P0 Lost extraction/refund** | **CLOSED** D195/D197 | Aggregate survivor extraction alone cannot identify each paid item's extracted portion | Lossless item/portion attribution, original *paid* mana, tuned partial credit, no world sale/no reinsertion/double credit, retry/offline/partial-survivor save tests |
| **F05 P0 Ranked floor permits** | **CLOSED** D174 | Production `ac_100` still permits Floor 2 only; Floors 3–5 not authored | Explicit old permit/project entitlement mapping, separate ranks and costs, no double charge, authored fit and versioned migration/reopen |
| **F06 P1 Monsterology/legacy roster** | **CLOSED** D144–D173 | Old `mon_300` Warrior and obsolete Shaman class prerequisites in historical data | Each named class individually researched; count-based immediate-form gates, stable IDs, nonexclusive branches, equality/proportional injury, content validation |
| **F07 P1 Combat achievements/offline progress** | **CLOSED** D182–D185, D201 | C0/C2 provides combat receipts but Phase 8 authentic objectives not implemented; offline party advancement still pending | Four exact metrics, D201 1/qualifying encounter, durable dedupe and complete eligible offline progress |
| **F08 P1 Treasure Ward** | **CLOSED** D59–D88/D186–D188 | Ordinary standard placements prohibit overlap; no Ward implementation | One narrow Ward–room-node same-tile exception, separate capacities, custody/relink, valid 1 attempt per party/node visit, damage/outcome and snapshots |
| **F09 P1 Signatures/stacking** | **CLOSED** D151–D156, D198 | Basic C0/C2 doesn't itself provide approved advanced signature abilities | Versioned action/cooldown/eligibility/replay and strongest same-name effect, nonstacking at multiple boss placements |
| **F10 P1 Environment** | **CLOSED** D177–D181 | Combat currently has no authored production interior opacity; generic reserved cells don't prove LOS wall | Pillar explicit opaque+blocked cells, Flooded water logical movement delay, replay/offline consistency and UI |
| **F11 P1 Catalog/Blueprint discovery** | **CLOSED** D90–D143 | Current bootstrap item pools are limited/abstract; no validated Phase 8 complete item-tier+rareness catalog | Single approved normalized authoring owner, discoverable ordinary sources, individual tier blueprints, independently paid track and eligible reward selection |
| **F12 P1 Trap restoration** | **CLOSED** D199/D200 | Current traps are not established as having saved lifecycle/readiness state | Distinct single-use/reusable reset/recharge with **mana fee for each**, per-definition category including no assumed Spike mode, vacancy, reserve, ownership and no duplicate costs |
| **F13 P1 Floors 3–5** | **CLOSED** D175/D176 and earlier geometry targets | Authored production floors currently 1–2 only | New content-defined bounds/capacity/fit/meaningful Floor 5 milestone, rank/activation and build/Windows/reopen qualification |
| **F14 P1 Superseded register wording** | **CLOSED interpretation** with explicit precedence | v41 historical body still includes original provisional/open states | Govern via concise new lock, effective index, dated addenda; preserve history without activating D46/D47/D160 etc. |
| **F15 P2 UX/accessibility** | **CLOSED scope** D141/D194 + Specs 26/27 + approved UI | #245 Windows visual checks not universal mobile/touch/localization proof | Noncolor-readable node halo, trap/monster/research explanatory states, large text, safe-area portrait/landscape, player-fun UAT |
| **F16 P1 Save/performance/offline** | **CLOSED exactness policy** D150 + Living lock | #245 bounded synthetic/editor/Windows paths and optimized event stalls; no full five-floor native-mobile throughput | Exact eight-hour interrupted offline differential, preserved formats/saves, bounded high-density work, real supported devices and measured 30 FPS target |

## Immediate documentation-action outcome

The **P8-00 documentation-only PR** may proceed after this owner design reconciliation. It should not declare any technical finding “fixed” merely because new text states the approved behavior. The current no-open-PR lookup at handoff was empty, and `main` compared identical with #245; recheck before beginning Codex work.

**No additional owner-gameplay vote identified by F01–F16 at this stage.** Do not begin backend/security, economics, class content, authoring or schema implementation in P8-00. Once it merges, reassess whether a separate Living Dungeon multi-floor/offline slice or a dependency-independent research/domain slice is the smallest user-visible next implementation task.

## Source pointers for reviewers

- [Current master roadmap](post-gd60-mvp-execution-plan.md) and [updated Phase 8 sequence](phase-8-dependency-roadmap.md).
- [Living Dungeon owner lock](decisions/living-dungeon-owner-design-lock.md) and [Combat/Recovery owner lock](decisions/combat-and-recovery-owner-design-lock.md).
- [Merged B3B2 implementation contract](../../Docs/planning/living-dungeon-phase-b3b2-implementation-contract.md) and [actual #245 qualification](../../Docs/testing/evidence/living-dungeon-b3b2/qualification.md).
- [Research bridge](../../Assets/_Project/Scripts/Services/PlayerResearchActionHandler.cs) / [research local authority](../../Assets/_Project/Scripts/Services/PlayerResearchClaimAuthorityResolver.cs); [Floor construction](../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorConstructionAuthority.cs); [C0 geometry](../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Geometry.cs); [legacy loot roll](../../Assets/_Project/Scripts/Services/LootRollResolver.cs).
- [Spec 26](../../Docs/26%20-%20Accessibility_and_Cognitive_Load_Targets.md), [Spec 27](../../Docs/27%20-%20Localization_and_Text_System.md), [Spec 36](../../Docs/36%20-%20Performance_Memory_and_Device_Support_Targets.md).

**Validation not performed by this document:** Unity tests, standalone build, real mobile device profiling, PR merge/approval, new saved-state migration, balance/fun UAT.
