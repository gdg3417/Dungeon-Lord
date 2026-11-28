# Dungeon Lord Gameplay Decision Log

**Version** 0.6 · **Reconciled** 2026-11-21 · **Verified main** `9a0629c51e1ebb5eb0a39e5326c694e92c9afcff`

The source for the 27 owner decisions is the supplied *Dungeon Lord Gameplay Decision Log v0.5*, dated November 19–20, 2026. The register summarizes the approved decisions. The full detailed record follows the current reconciliation. Decisions 001–021 were recorded November 19 and 022–027 November 20. Later entries refine earlier ones, including the capacity formula, monster retirement and the floor boundary on investment reuse. Implementation observations describe the verified merge baseline. Merged specifications and applicable dated amendments govern changes to runtime contracts. The amendment proposals and PR sequence below require separate review and do not authorize gameplay implementation.

## Active milestone dispositions (2026-11-27)

The [active execution plan](../post-gd60-mvp-execution-plan.md#active-product-milestones-2026-11-27) governs delivery at the #277 baseline. The 27 approved decision texts and their earlier reconciliation remain below. Deliver the remaining decisions through their owning gameplay systems. Amendment labels describe requirements and technical dependencies. The current construction PR includes the settlement and immediate safety needed for a complete legal renovation.

| Decision IDs | Owning milestone and current disposition |
| --- | --- |
| 001, 015–016, 021–022 | **1. Construction.** [#275](https://github.com/gdg3417/Dungeon-Lord/pull/275) accepted independent placement, map-first restricted insertion, authored doorway editing and automatic reconnect with one pinned endpoint. Complete full perimeter selection, combined endpoints, committed revisions, legal reorder and insertion, and approved fixed placement. Fixed access and positioning details require reconciliation against Spec 38 and the owner's relocation direction. |
| 002, 014, 017–020 | **1. Construction; reached-tile content in 2.** #275 delivered ordered one-through-six orthogonal corridors, exact actual travel and closed side contacts within supported layouts. Preserve cost, floor space, ordinary eligibility, room-local combat and unchanged run snapshots when expanding edits. Required-route content interactions remain in Research and Trapcraft. |
| 004–005, 026–027 | **1. Construction.** Existing structural economy and ledger protect current price, reserve and provenance. Complete canonical-to-final same-floor reuse and capped, verified net settlement. The one global wallet receives genuine authorized refunds. Any unresolved cap, rounding or eligibility interpretation requires an explicit decision. |
| 006 | **1 / 2 / 3 / 5.** Existing placement/custody is partial. Construction owns explicit displaced-content consequences and legal identity-preserving reassignment supported by current authorities. Trapcraft, Monsterology and Blueprint/loot own their respective complete contextual content lifecycles. |
| 007–009 | **3. Monsterology; applicable base retirement also in 2 and 5.** Pending verified acquisition and paid progression provenance and retirement receipts. Preserve operating expense, recovery reserve and global permission distinctions. |
| 003, 010, 012–013 | **2. Research and Trapcraft.** Existing optional-corridor contents and starter Research are foundations. Pending required-route eligibility, ordered encounters, Research-controlled tiers, legal distinct placements and verified apparatus/restoration/retirement. |
| 011 | **5. Blueprint and loot.** Pending per-node paid stock, authoritative extraction/refund provenance and Ward relationship lifecycle. Preserve current supported loot behavior. |
| 023–025 | **1. Construction.** Reset with explicit confirmation and linked content/branch consequences, player-directed graph reconstruction and net-ranked nonbinding suggestions remain open. Depends on complete draft validation, structural settlement and an explicit supported content disposition. |

## Historical merged Research baseline

- **Timing and saves.** [PR #272](https://github.com/gdg3417/Dungeon-Lord/pull/272) merged at `7b492ecfcb8e3e5ea8f20905b9afaef82f57e934`. Native schema 18 retains spatial registration 17. The [timing contract](../phase-8-research-timing-compatibility-contract.md) pins accepted work to the immutable rule selected at Start, including H1 recovery and off-grid partial-first-tick accounting. The [pre-release development save policy](pre-release-development-save-policy-2026-11-19.md) permits explicit refusal of obsolete development saves with originals and evidence preserved. Schema 18 uses native creation and supplies no 17→18 migration. Current-schema exact reopen, fail-closed recovery and accepted-work preservation still apply. Establish supported public-save versions and explicit migrations before first public release.
- **Playable starter slice.** [PR #271](https://github.com/gdg3417/Dungeon-Lord/pull/271) merged at the verified head above. Production Research starts and claims Adventurer Activity Analysis through [the player action handler][research-action] and canonical save authority. One genuine completed adventurer run qualifies, including defeat. Start charges 10 usable mana once under the protected reserve, using the [starter selection][research-select] and [archived rule][research-rule]. The project requires 120 eligible foreground seconds on the existing 10-second authoritative cadence. A foreground presentation countdown updates each second and may show finalizing before durable readiness. Claim uses canonical ready state and completes once. Local Start and Claim work disconnected under the same barriers. Unfocused/closed time grants no starter progress. The merged PR records qualification and owner UAT, including the accepted focus-loss display fallback to committed progress.
- **Remaining roadmap.** [Spec 09][s09], [Spec 20][s20], and the [Phase 8 roadmap](../phase-8-dependency-roadmap.md) distinguish the single paid starter General project from independent General/Blueprint slots, additional tier catalogs and rewards, exact eligible offline Research, and genuine remote Start and Claim verification before public production release. No Trapcraft capacity permission ships with R1.

## Decision register and reconciliation matrix

**Partial** means an existing capability covers part of the approved direction. **Pending** means the named behavior is absent. **Aligned** means the current scope already honors the decision. An amendment letter refers to a proposed dated amendment below. Spec references name the governing clauses and linked authorities identify the current seam.

| Decision | Owner-approved behavior | Status, governing clauses and current authority | Remaining work or conflict |
| --- | --- | --- | --- |
| DL-GAME-001 | Tap-first or drag a room onto any legal sufficient grid footprint. Choose connections independently, insert or reorder without moving unaffected rooms, and inspect actual visit order. Incomplete drafts are editable. Save requires a valid active route and atomic consequences. | **Partial.** [Spec 38][s38] §§4–8 and [A0][a0] D2, D11–21, D27–37, D57/63, D82–84. [Construction][build] inserts at the terminal boundary, [renovation][move] handles constrained movement, [layout validation][validator] checks geometry and connectivity, and the [graph][graph] owns order. | A. Add independent placement and connection commands, localized blockers and route preview. Preserve unrelated edges, room/content identities and run snapshots. |
| DL-GAME-002 | Occupied floor space, configured structural price, actual logical travel time and eligible placed content make length matter. Distance alone adds no damage, fatigue, loot, Heat or danger roll. | **Partial.** [Spec 38][s38] §§3/5, [Phase 5][p5] D20 and [Living Dungeon][living] §2. [Economy][economy], [required path capture][paths] and [branch projection][branch] cover existing geometry. | A/B. Ordered bent paths must drive elapsed travel and actual reached encounters, including reopen/replay. Compare otherwise equal short and long layouts. |
| DL-GAME-003 | Baseline remains available. Research permits additional slots on sufficiently long legal required and optional corridors. Traps are separately acquired and operated, occupy distinct legal tiles, and keep identity on repair or receive explicit disposition. | **Pending.** [Spec 38][s38] §5.1 fixes one trap. [Phase 5B][p5b] and [corridor mutation][corr-content] use optional-branch content and [its projection][branch]. | B. Add required-edge eligibility, actual traversal encounters, repair/block rules and workload validation before ranked capacity. |
| DL-GAME-004 | Reuse verified paid structural investment on eligible legal redesigns, including disjoint tiles and changed edge IDs. Charge uncovered cost, preserve attached content for explicit disposition, and prevent reuse plus refund of the same amount. | **Partial.** [Spec 38][s38] §9 and [A0][a0] D19–23. [Economy][economy] and [investment][investment] retain existing investment, with movement carry constrained by matched edges and shared tiles. | C. Qualify same-floor transfers and allocate against canonical-to-final change. Preview fresh cost, reuse and genuine refunds separately. |
| DL-GAME-005 | Eligible refunds in the same atomic whole-dungeon renovation can fund purchases if the actual final wallet respects cap, reserves and verification. Preview capped credits and final balance. Failed or stale commits preserve the draft and canonical state. | **Pending.** [A0][a0] D19–23, [Spec 01][s01], [Spec 21][s21] §6 and [recovery lock][recovery]. [Economy][economy] checks spending before applying refund and [draft commit][commit] uses the existing canonical writer. | C. Specify order-independent credited result and cap/reserve treatment, then test rollback, stale inputs and exact-once retries. |
| DL-GAME-006 | Contextual Add, Move, Replace and permanent Remove for monsters, traps and reusable loot nodes. Replace settles retirement and acquisition together. Move preserves identity and applicable state. Displaced assets require explicit disposition. Retire the permanent New/Owned workflow. | **Partial.** [Spec 38][s38] §§4/8 and [A0][a0] D14/39. [Room-content UI][room-ui] and [removal policy][removal] support New/Owned and reusable custody. | D. Replace the custody contract explicitly. Account for live or returned legacy assets on supported inputs without inventing payment provenance. |
| DL-GAME-007 | Eligible permanent retirement returns 100% of verified historical base mana once. Free acquisitions return zero. Move neither pays nor refunds. Upkeep, recovery, recharge and stock have separate rules. Ward links must resolve atomically. | **Pending.** [Spec 38][s38] §§4/8–9, [Spec 21][s21] and [Phase 8 lock][p8] §§5/7–9. [Acquisition config][acquisition] holds current prices. [Economy][economy] charges additions but does not refund historical content base. | C/D. Prove original instance payments including discounts/grants, and refuse unproven credits. Preserve separate stock/extraction and Ward receipts. |
| DL-GAME-008 | Permanent monster retirement also returns an initial, tunable 80% of eligible verified paid individual progression. Move preserves it. Global permissions and operating/recovery costs are excluded. A purchased replacement has its own identity and progression. | **Pending.** [Phase 8 lock][p8] §5 and [Spec 03][s03]. [Monster continuity][monster] preserves individual condition/level state and supplies no paid advancement ledger. | D. Author the approved fraction, define provenance, dormant investments and rounding, and test duplicate prevention. |
| DL-GAME-009 | Retirement returns the same eligible amount when healthy, injured, defeated or awaiting revival. Move preserves condition. Retirement ends the old identity's recovery obligation without requiring revival. | **Pending.** [Recovery lock][recovery] and [Spec 03][s03]. [Monster continuity][monster] owns condition and reserve obligations. | D. Recompute reserve from the final state. Test equivalent refunds and measure retire-and-rebuy balance before proposing any change. |
| DL-GAME-010 | Surviving apparatus returns eligible original base regardless of readiness. Fully consumed single-use devices return zero apparatus mana. Move preserves state. Paid reset/recharge/replacement remain separate. | **Pending.** [Phase 8 lock][p8] §§8–9 and [Spec 21][s21]. [Removal policy][removal] describes custody, with no general proved apparatus-retirement accounting. | D. Author per-definition lifecycle and prove apparatus existence, original payment and independence from restoration. |
| DL-GAME-011 | Move the same loot node with focus, stock identities and paid provenance. Retirement returns eligible node base and explicitly forfeits uncollected stock once. Handle Ward separately. Collected/unextracted refunds keep their existing separate cause. | **Pending.** [Phase 8 lock][p8] §§7–8 and [Spec 23][s23]. [Room-content draft][content-draft] handles current instances, while paid per-node stock remains future. | D. Wait for canonical paid stock, extraction and Ward lifecycle. Preview forfeiture and preserve independent identities and payments. |
| DL-GAME-012 | Potential capacity is `ceil(L / 2)` for legal connected corridor length `L`. A six-tile corridor can qualify for three positions, bounded by Research, authored restrictions and legal distinct tiles. | **Pending.** [Spec 38][s38] §5.1 and [corridor catalog][corridors] use a fixed one-trap maximum. | B. Derive from the logical edge, with boundary and split/bend exploit tests. Leave longer catalog entries to separate approval. |
| DL-GAME-013 | Sequential global General/Trapcraft tiers add one usable slot each on eligible geometry. With highest completed and claimed tier `R`, the ceiling is `min(ceil(L / 2), 1 + R)`. No per-corridor unlock charge. | **Pending.** [Spec 09][s09] and [Phase 8 lock][p8] §§4/8 govern Research, and [the merged starter][research-rule] grants no capacity tier. | B. Author project IDs, prerequisites, price, time and initial rank count. Derive permission from claimed state and keep one capacity authority. |
| DL-GAME-014 | Longer geometry unlocks through ordinary construction catalog eligibility with configured build cost. Dedicated Architecture length Research is excluded. Trapcraft affects capacity independently. | **Aligned in boundary, pending longer catalog.** [Spec 38][s38] §§5/9, [Architecture spec][arch] and [catalog][corridors]. | A. Keep normal room/floor permissions and author reviewed geometry. Clarify the distinction in the amendment. |
| DL-GAME-015 | Suggest compatible openings and a path. Either room doorway can be dragged to another authored-legal position, including a different wall, without moving room or contents. Fixed structure access is preserved. | **Pending.** [Spec 38][s38] §4.1 fixes authored offsets and [A0][a0] D32–35 governs proposals. [Socket table][sockets] and [construction][build] resolve fixed room sockets. | A. Author legal boundary choices and persist chosen openings with stable identities, occupancy and validation. |
| DL-GAME-016 | Deterministically suggest a legal path for selected endpoints. Allow manual segment/bend edits, cost/capacity inspection and explicit draft acceptance. Explain unreachable cases without disturbing unrelated edges. | **Pending.** [A0][a0] D31–35 already provides the interaction direction. [Construction][build] supplies aligned straight pairs. | A. Qualify preview and commit using the same geometry rules. Cost-aware ranking follows authoritative settlement. |
| DL-GAME-017 | Any number of legal orthogonal bends within total length. One tile wide, corners counted once, and one continuous logical edge for traversal, cost and capacity. Overlap, unintended junctions and loops remain invalid. | **Pending.** [Spec 38][s38] §§5/8 and [tile geometry][tiles] use a straight line. [Branch projection][branch] orders by distance from source. | A/B. Choose ordered passage representation and validate traversal without shortcuts, repeated effects or multiplied space/credits. |
| DL-GAME-018 | Initial `spatial.corridor.straight_stone` reaches six occupied tiles with ordinary cost and space. Longer catalog lengths need separate review. | **Pending.** [Spec 38][s38] §5 and [GD65B rows 33–35][gd65b] authorize 1–4 tiles in the [catalog][corridors]. | A. Update approved content authority and validators together, including six/seven-tile boundary tests. |
| DL-GAME-019 | Separate hallways, nonconsecutive stretches of the same hallway, and room/hallway footprints may touch sides. Walls block movement and sight except at authorized passages/openings. Same-tile overlap stays invalid. | **Pending.** [Spec 38][s38] §§4–8 and [A0][a0] D36. [Required path capture][paths] uses shortest paths across adjacent captured cells. | A. Explicit or derived passage/wall boundaries must govern traversal and sight. Under side contact, adjacency alone gives the wrong path. |
| DL-GAME-020 | Preserve MVP room-local combat including range, targeting and line of sight. Even a legal open door does not grant cross-room or room/corridor ranged combat. | **Aligned.** [Spec 38][s38] §19, [A0][a0] D45–55 and [recovery lock][recovery]. [Combat geometry][combat] is room-local. | Regress combat and ingress/egress when openings change. Cross-room ranged combat remains deferred. |
| DL-GAME-021 | Suggest a route insertion point after legal placement. Player may choose another predecessor/successor, edit openings/path and review route order. Existing connections change only through explicit edit. | **Pending.** [A0][a0] D31–37 and [Spec 38][s38] §§6–8. [Construction][build] inserts at the completion boundary, and [graph][graph] determines order. | A. Use a nonbinding preview and graph-derived ordering. Avoid forced movement, content disposition and resets. |
| DL-GAME-022 | Insertion proposes replacing the selected A–B edge with editable A–new and new–B connections. Show affected edge, contents and economics. Explicitly accept into the reversible draft before final Save Changes. | **Pending.** [A0][a0] D11–25/31–37 and [Spec 38][s38] §§8–9. [Draft commands][draft] have no general edge replacement. | A/C. Handle direct and physical edges, stable ID retirement, legitimate investment and trapped edges. A bounded first slice can avoid physical retirement. |
| DL-GAME-023 | Confirm Reset Hallways for the selected floor's draft. Clear all editable direct, physical and optional connections including editable entrance/terminal links. Keep fixed structures, rooms, room contents and other floors. Flag corridor contents and branch stock/knowledge for explicit disposition. | **Pending.** [A0][a0] D14–21/31–37 and [Spec 38][s38] §§6–8. [Branch edits][branch-edit] and [draft][draft] lack this operation. | A/C/D. Preserve atomic repair/discard, investment and ownership. Block invalid active-floor commit after partial reconstruction. |
| DL-GAME-024 | Among legal suggestions that preserve unrelated structures and content, prefer lowest projected final net mana, then shorter travel, then stable canonical ties. Manual override remains available. Suggestion spends nothing. | **Pending.** [A0][a0] D19–23/33. [Economy][economy] has final-draft preview but lacks the approved settlement and path ranking. | C. Reuse final authoritative preview and bound candidate search. Search method and workload are technical choices. |
| DL-GAME-025 | After reset, player selects each next room pair, edits the suggested connection and accepts it into the draft. Revisit earlier edits and add eligible optional branches separately. Route labels derive from the graph. No automatic room order or full-floor rebuild. | **Pending.** [A0][a0] D32–37 and [Spec 38][s38] §§6–8. [Graph][graph] and [draft][draft] have no guided reconnect sequence. | A/C. Require a full legal entrance-to-completion route at commit. Test chosen order in a subsequent run and exact draft reopen. |
| DL-GAME-026 | Allocate proven eligible investment automatically once to maximize legitimate same-floor reuse against final geometry. Use canonical ties and show gross cost, reused amount, true refund and net. Gesture order cannot change the result. | **Pending.** [A0][a0] D19–23 and [Spec 38][s38] §9. [Investment][investment] and [economy][economy] lack a floor-wide allocator. | C. Qualify split/merge, partial reuse, retries and strict exclusion from refund for the reused portion. |
| DL-GAME-027 | Keep structural credit on the source floor, including disjoint replacement footprints. Separately authorized monetary refunds can fund another floor through the one global wallet, with reserves and no double credit. | **Pending.** [Spec 38][s38] §9, [Spec 01][s01] and [A0][a0] D21. [Economy][economy] has no portable same-floor allocation pool. | C. Prove floor provenance and separate credit classes. Test cross-floor refusal, valid monetary settlement and rollback. No new general refund rate. |

## Dated amendments required before the affected changes

- **A · Geometry, openings and draft reconstruction.** Amend Spec 38 §4.1's eligible room openings and §5's straight, 1–4-tile profile, with the corresponding GD65B records, catalog and orientation contract. Preserve fixed access and amend §§6–8 where needed to distinguish side contact from passage, legal edge replacement/reordering from existing subtree movement and leaf deletion, and a valid committed route from an incomplete draft. Add a dated A0 D31–37 clarification for explicit insertion, reset including direct links, and one-connection-at-a-time rebuilding. Preserve one required route per floor, at most one unlocked optional corridor-only dead end per floor, fixed structures and room-only combat. Keep the approved five-floor scope and sub-dungeon exclusion. Spec 38 §9 should distinguish catalog length eligibility from Architecture permissions.
- **B · Corridor encounters and capacity.** Amend Spec 38 §5.1's fixed one-trap cap and clarify required-route trap eligibility while retaining optional-dead-end-only loot, distinct tiles and no corridor monsters. Amend the Phase 5/5B optional-corridor placement/encounter and workload contracts where they assume one trap and linear distance ordering. Establish ordered, reached-tile effects in the runtime and snapshot contracts. Supplement Spec 09 and Phase 8 §§4/8 for claimed global tiers without changing R1 terms.
- **C · Structural investment and affordability.** Amend Spec 38 §9 and Spec 21 §6 where existing renovation/refund treatment needs extension. Clarify A0 D19–23's affordability and disclosure against the shipped spend-first rule. Set final settlement order, eligible capped credits and final reserve checks. Define automatic floor-local reuse disjoint from monetary refunds, without a second wallet or changed purchase authorization.
- **D · Content retirement.** Amend Spec 38 §§4/8's free unassignment and reusable return-to-custody clauses and A0 D14/39's custody consequences. Reconcile Phase 8 §§5/7–9, especially Ward D79–D86 and D186/D187, with explicit retirement, stock forfeiture and legal link handling. Update Spec 21's eligible recovery basis and Spec 23's stock lifecycle where affected. Reconcile the November 19 sequencing decision §2.7 tutorial custody wording when the replacement workflow is adopted. Keep paid operational recovery and collected-but-unextracted item refunds independent.

## Implementation gates and recommended PR order

Retain the [glossary][invariants], [development rules][devrules], [Spec 17][s17], [Spec 28][s28] and [Spec 29][s29] guarantees. The spatial graph, corridor content, Research, wallet and investment each retain one canonical writer. [Draft format 5][draft] is durable and non-authoritative. The [draft commit][commit] and [SaveService][save] publish atomically after validation and exact readback. Keep stable IDs and reason codes, canonical ordering, side-effect-free validation, localized UI, authored tuning, protected mana and immutable run-start snapshots. Version saved representations or accepted rules only when later implementation demonstrates the need. Apply the pre-release save policy to obsolete development inputs and preserve original files and historical evidence.

1. **Smallest proposed gameplay PR.** After the scoped geometry/draft amendment and technical review, let a player place a room at an offset legal footprint on an existing active floor, explicitly insert it at one eligible content-free direct edge, preview/edit its two legal connections, commit and see a subsequent adventurer traverse it. Include authored doorway selection and ordered six-tile bent paths for that supported case, with side-contact wall validity. Leave paid physical-edge replacement and content displacement blocked until their requirements are available. If the reviewed persistence or geometry seam requires a split, pair the domain prerequisite with a named playable follow-up. More anchors alone leave the player without independent route control.
2. **Physical redesign and rebuilding.** Qualify final-draft settlement and same-floor investment transfer before replacing paid physical links, ranking by net cost, or enabling selected-floor reset and manual reconnect. Block unresolved contents while building the safe disposition path. Test direct/physical replacement, exact reopen, unaffected floors and in-flight versus subsequent runs.
3. **Corridor encounters, then capacity.** Prove baseline required-edge trap placement, reached-tile encounters and bounded traversal before layering claimed Trapcraft capacity. Review offline scope separately. Keep paid restoration, stock, Ward behavior and individual monster advancement on their own dependency paths.
4. **Direct content lifecycle.** Introduce verified acquisition provenance and direct Move/Replace/Remove with each content domain's confirmed dependencies. Apply progression, condition, apparatus and stock rules when their authorities are available. Broad floor/content expansion follows the [dated gameplay-first sequence](mvp-execution-sequencing-2026-11-19.md) and Phase 8 roadmap.

Open implementation choices include ordered path/door serialization, edge identity on replacement, deterministic search bounds, exact settlement rounding, credit cap interactions, legacy custody handling and corridor contents disposition on reset. Obtain owner approval before choosing any new player-facing disposition, unsupported payment treatment, trap classification or project tuning. Review gameplay usability with real build/run/inspect/revise runs and later qualify Unit, SIT, owner UAT, save/reopen and applicable player builds by slice.

## Detailed decision history from v0.5

The record below preserves the November 19–20 source wording and section numbers. The v0.6 matrix above replaces the original summary register. The historical implementation observations and sequencing reflect the date each note was written. Use the v0.6 baseline above for current implementation status.

### Dated reconciliation notes

- **November 19, 2026.** Decisions 008 and 009 resolve the refund questions left open in §9.4. Eligible verified paid individual monster progression initially returns 80%, and health, injury, defeat or pending revival do not change the eligible refund amount. Decisions 012 and 013 set the approved physical capacity and claimed Trapcraft tier formula. Their formula governs the examples in §5.3. Decisions 017 and 018 settle the player-facing geometry questions in §4.3. One continuous bent corridor is one logical edge and the initial Straight Stone maximum is six occupied tiles. Serialization remains a technical choice.
- **November 20, 2026.** Decision 022 resolves the explicit insertion acceptance question in Decision 021. Decisions 023 and 025 define reset scope and player-directed rebuilding. Decisions 026 and 027 resolve automatic allocation and the floor-local reuse boundary that §6.4 treated as provisional. Separately authorized refunds still settle through the global wallet.
- **November 21, 2026.** The earlier PR statuses in the source notes, §31, update history and historical references reflect their original checkpoints. Both Research PRs have now merged. Apply the dated pre-release save policy to all historical migration and legacy-custody references. Obsolete development saves may be explicitly refused with originals preserved. Supported assets still require safe ownership handling, and current-schema exact recovery remains mandatory.

**Document type:** Rolling, owner-approved gameplay decision record and future implementation handoff\
**Log version:** 0.5\
**Initial record date:** November 19, 2026\
**Repository:** [gdg3417/Dungeon-Lord](https://github.com/gdg3417/Dungeon-Lord)\
**Repository baseline last rechecked for this revision:** `main` at `7b492ecfcb8e3e5ea8f20905b9afaef82f57e934` (November 20, 2026, after merged PR #272)\
**Authority:** Owner decisions recorded here express intended *future gameplay behavior*. They are **not** evidence of shipped functionality, merged specification amendments, approved numeric tuning, or successful testing. Before implementing, independently recheck current `main`, `AGENTS.md`, active dated specification amendments, recent PRs, code, configuration, save format, and tests.

## 1. Purpose and maintenance rules

This file preserves decisions that would otherwise be lost across development chats. Add a new dated entry every few decisions or whenever the owner asks for an update. Its goals are to prevent repeated design debates, expose unresolved implementation gates, avoid conflicts with existing specifications, and keep the player-facing build, run, inspect, revise loop central.

**Status vocabulary**

- **Owner-approved direction:** The intended player behavior is decided in conversation. Technical design, configuration/tuning, documentation amendment, implementation, and qualification may still be outstanding.
- **Existing repository design lock:** A decision already approved in the source repository. Do not relock or supersede without a specific new owner decision and dated amendment.
- **Provisional:** A recommendation or illustrative example, not an approved requirement.
- **Open implementation gate:** A required technical/content/test decision that must be resolved before relevant code is committed.
- **Deferred:** Explicitly out of the current MVP or deliberately not decided.

**Change policy:** Preserve prior entry IDs and the history of decisions. If a decision changes, add a dated amendment stating what it replaces, why, and which specs/code/tests are affected. Do not silently rewrite a formerly approved rule. Never treat this standalone log as higher authority than current merged source code and formally approved dated specifications; reconcile and promote applicable entries to repository-owned decision records before implementation.

### November 20 implementation checkpoint

**Current development status (November 20, 2026):** Research prerequisite PR #272 has merged into `main` at `7b492ecfcb8e3e5ea8f20905b9afaef82f57e934`. It introduces native schema 18 and immutable versioned Research timing. Obsolete development saves are rejected without silent conversion under the approved pre-release policy, retaining original evidence; this must not be misstated as a supported 17-to-18 migration. PR #271 remains draft and must integrate the merged #272 source before production activation. The 22–27 construction directions do not enlarge PR #271 scope.

**Already decided elsewhere:** Starter Research pacing and first Research unlock conditions are owned by the Research specifications and PRs #271/#272. The owner approved 10 usable mana, 120 eligible seconds and the existing 10-second tick with exact partial-interval accounting. PR #272 merged the timing foundation; the player-facing paid Research vertical slice in #271 is not yet qualified or merged. These are **not additional DL-GAME decisions**. Import the authoritative final Research contract, current PR review and any updated approval before coding.

---

## 3. DL-GAME-001: Player-positioned rooms and player-controlled hallway rerouting

**Status:** Owner-approved gameplay direction, November 19, 2026.\
**Owner choices:** Selected Option B, allowing insertion of new rooms and reordering existing rooms through legal route edits; clarified that rooms are dragged/placed on the map and hallways determine adventurer traversal order.

### 3.1 Player fantasy and objective

The player designs a *physical dungeon*, not an abstract ordered list of encounters. Room placement and corridor connections are distinct strategic decisions. A player should be able to place a room at any legally available, sufficiently large, grid-aligned footprint, then create or rearrange hallways so adventurers visit rooms in the chosen physical order. The selected room order must arise from the actual valid route graph, not a second editable sequence authority.

### 3.2 Approved player-facing behavior

1. **Place room:** In Edit Mode, select a room type and tap or drag it onto a legally available floor footprint. Tap-first remains the canonical interaction; dragging is a convenience supported by the same domain operation. Placement can be previewed and confirmed.
2. **Keep layout control separate from room placement:** Adding a room does not automatically delete or reset existing hallways. The player decides whether and how to connect it.
3. **Edit Hallways:** Select existing connections and remove, connect, reroute, or reshape them in the draft, subject to the authored geometry and topology rules.
4. **Reset Hallways:** Provide an explicit player-triggered way to clear/rebuild the applicable floor's editable hallway connections **in the draft**, retaining the room structures. This is a voluntary editing operation, not an automatic effect of placing a room. Exact scope/selection and conflict-handling UI are implementation gates.
5. **Insert room into middle:** A new room may become an intermediate room on the required route, not only the last room before the Completion Terminal.
6. **Reorder existing rooms:** The player may change encounter order by changing legal connections among already existing rooms **without** constructing or physically moving a room. A room only moves if the player explicitly chooses to move it or a separately approved movement operation requires it.
7. **Route visibility:** The editor displays the proposed required route and the room traversal order. Invalid, overlapping, disconnected, unaffordable, or otherwise blocked edits explain the reason and can be corrected.
8. **Draft semantics:** Temporary disconnected or incomplete arrangements may be shown as draft edits. A canonical **active** floor cannot be committed in an invalid required-route state. On Discard, the committed dungeon remains unchanged. On Save Changes, the final valid draft and all economic/ownership consequences are checked and committed atomically.
9. **Existing runs:** Adventurers already in an active run continue on their immutable run-start layout. Committed edits affect subsequent runs only.
10. **Content safety:** Rebuilding hallways must not silently delete, duplicate, orphan, refund, or relocate traps/loot. Affected contents require a deliberate, reviewed preservation, return, repair, or other authorized disposition with preview of consequences.

### 3.3 MVP topology boundary

- One required entrance-to-completion route per floor.
- At most one optional corridor-only dead-end branch per floor after the existing branching unlock; no optional-branch monster-room chain in the MVP.
- No unrestricted loops, multiple entrances, multiple alternate required routes, multi-room side branches, or maze/backtracking AI.
- Corridor connections must satisfy same-floor geometry, compatible authored endpoints/sockets, occupied-tile rules, floor-space limits, deterministic graph ordering, and applicable lifecycle validity.
- MVP remains five main dungeon floors with all sub-dungeons, including Mana Farm, excluded.

### 3.4 Rationale and intended strategy

- Room and corridor arrangement should create tradeoffs between the cost of construction, the order of encounters, the arrival condition of parties at later rooms, the ability to reach/collect loot, and retreat/survival outcomes.
- Allowing insertion and reconnection prevents a layout from becoming permanently obsolete just because a room was initially built at the end of a route.
- Not requiring full maze topology avoids bringing advanced route-choice AI, multiple competing shortest paths, and complex knowledge models into the MVP before the simpler layout decisions prove enjoyable.
- The user should learn through the **build, run, inspect, revise, rerun** loop whether a rearrangement helped; complex corridor controls alone do not prove strategic depth.

### 3.5 Existing design locks preserved

- `docs/planning/decisions/mvp-execution-sequencing-2026-11-19.md`, §2: any legally available sufficient grid-aligned room footprint; player-controlled corridor creation/path suggestion/bend editing; construct after initial Phase 8 Research slice.
- `docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md`: Decisions 2 (tap-first), 11–21 (preview/durable draft/validity/atomicity), 27–36 (movement, corridor authoring, no silent reroute, segment/bend control), 37 (route overlay), 57/63 (run-start isolation), 82–84 (durable draft semantics).
- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`, §§5–8: physical same-floor graph, one required route, optional branch limitations, capacity and validity.
- `docs/planning/phase-5-branching-and-route-choice-design.md`, §2.1: one required route with one optional corridor-only dead-end branch when unlocked.
- `docs/planning/decisions/living-dungeon-owner-design-lock.md`, §2: authentic geometry-dependent movement and immutable run-start snapshots.

### 3.6 Known implementation gaps at inspected baseline

- `StructuralEditService.cs` builds a new room by modifying the existing predecessor-to-Completion edge; this is not arbitrary route insertion.
- `StructuralRenovationService.cs` handles existing constrained movement and reconnection; do not assume it already supports user-selected arbitrary route edits.
- `TransactionalDungeonDraft.cs` at this baseline has command kinds for construction, room movement, content reposition and room content, but no separate general corridor reroute command.
- `FloorRouteGraph.cs` owns saved nodes and edges; extension should use it, not introduce a second ordered-room writable authority.
- `TransactionalDungeonDraftCommit.cs` is part of the detached canonical atomic commit path and should remain the sole commit mechanism.
- `StructuralEconomyService.cs` owns cost/refund logic and original investment preservation; do not duplicate pricing in the UI.

### 3.7 Open implementation gates and tests

**To decide/review before implementation:** exact corridor path/segment representation for the now-approved legal bends and six-tile initial maximum; draggable legal doorway/socket geometry; how Reset Hallways handles corridor content; stable edge IDs when reusing versus replacing a corridor; deterministic reallocation/retirement policy; content disposition; graph change fingerprints and shared knowledge invalidation; save/draft format and migrations; final net-cost/refund semantics; relevant performance limits.

**Required acceptance scenarios:**

- Insert a new room between two existing encounters on a legal footprint, then commit; subsequent runs traverse it in the chosen order.
- Reorder existing rooms without constructing or moving them, using only legal hallway changes.
- Reset Hallways changes the draft only; Discard restores the original route, contents, mana and identities.
- A temporarily disconnected draft remains editable but cannot commit as an invalid active floor; localized blocker points to the issue.
- Detect crossing/overlap, incompatible socket, insufficient space or funds, illegal optional branch/loop, and multiple connection ambiguity; never auto-fix silently.
- Check corridor traps/loot and investment are not duplicated or lost after reconnection, deletion, discard, recovery and reopen.
- Check original running parties stay on their prior run-start graph while new parties use the newly committed graph.
- Verify deterministic route ordering, preview/commit equivalence, lifecycle compatibility, versioned saves/migrations, mobile touch interactions, accessible labels and low-end profiling.

---

## 4. DL-GAME-002: Hallway length has physical and tactical consequences, not generic damage

**Status:** Owner-approved gameplay direction, November 19, 2026.\
**Owner choice:** Selected Option A.

### 4.1 Approved rule

Hallway length matters because it changes **occupied floor space**, **configured structural cost**, **actual geometry-dependent traversal time**, and **eligible physical opportunities for placed corridor content**. Length by itself does **not** apply hidden HP loss, fatigue, generic damage, extra combat rolls, automatic Heat, added loot, or a danger multiplier.

- Corridor occupied tiles each count as one floor-space unit, with no second weighted tile-capacity system.
- Construction and subsequent renovation use configuration-owned costs, actual historical investment and the existing final-draft economic rules. No free speculative construction charges before final commit.
- Adventurers travel the actual legal path in deterministic logical time, with interaction timing and traversal independent of animation/frame rate. Specific movement duration coefficients require configured authority.
- Traps and other effects trigger only at eligible reached locations and by the rules of each actual placed object, not by abstract corridor-length scoring.
- Route length may affect *when* encounters happen. Room order and actual defenses determine *what* happens. Neither distance nor extra turns automatically supply combat damage or rewards.
- Extra length may have indirect costs such as slower flow; do not automatically assume longer is better.
- Any later timed hazard, cooldown or other traversal-linked effect needs its own approved specification and authoring. It is **not** automatically granted by this rule.

### 4.2 Rationale and gameplay consequence

A long hallway is a spatial and economic commitment. It may create tactical value when it connects rooms in a different order or provides room for additional authorized traps. An empty hallway is still expensive and slower to cross. This keeps meaningful player agency, avoids opaque punishment, and limits simulation complexity. It is compatible with the genre lesson that paths become interesting through the defenses and rooms players place along them, not just length-based damage multipliers.

### 4.3 Existing authority and required reconciliation

- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`, §5: one space unit per occupied corridor tile; configured mana pricing; saved physical edges. Current `spatial.corridor.straight_stone` is **one tile wide and 1–4 tiles long** and permits compatible direct doorways between adjacent rooms.
- `docs/planning/phase-5-branching-and-route-choice-design.md`, Decision 20: expressly no arbitrary per-corridor-tile penalty.
- `docs/planning/decisions/living-dungeon-owner-design-lock.md`, §2: movement duration from actual traversable geometry and event-time determinism; exact ongoing Living Dungeon processing is a future implementation workstream, not proven complete here.
- `docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md`, Decisions 32–37: proposed player-controlled segments/bends; geometry and route classification visible in Edit Mode.
- `Assets/_Project/Scripts/Economy/StructuralEconomyService.cs`: existing construction pricing considers physical corridor tiles; inspect latest cost logic before adding any new edge representation.

**Important unresolved geometry conflict:** The initial authored corridor is a straight 1–4-tile definition, while the future editor calls for player-managed bends/segments. A later authoring and validation review must decide whether a bent route is a single logical corridor edge, several segments, or another approved geometry, without creating accidental extra trap capacity, costs, or triggers by segment splitting.

### 4.4 Qualification targets

Compare two otherwise equivalent valid layouts with different traversed distances. Verify correct occupied-tile count and configured cost, exact reproducible logical time difference, no damage/loot/Heat produced by distance alone, no repeated encounter triggers due to route segments, and identical outcomes across frame rates and compatible save/reopen paths when event ordering is held constant. Measure performance and UI comprehensibility on target mobile hardware; geometry validation and route planning must be bounded, not run expensively on every rendered frame.

---

## 5. DL-GAME-003: Length-qualified additional corridor trap capacity through Trapcraft research

**Status:** Owner-approved gameplay direction, November 19, 2026.\
**Owner choice:** Approved a **hybrid** rule: greater hallway length can qualify for more trap slots, and the additional capacity must be unlocked through Trapcraft research.

### 5.1 Approved rule

1. Baseline existing trap capacity remains available under current authored rules before research.
2. Completing an eligible **General Research / Trapcraft** project unlocks permission for greater trap capacity on corridors long enough to qualify.
3. Longer corridors do **not** automatically gain more trap slots without the relevant research.
4. Research unlocks **permission**, not free trap instances; every installed trap still has its own acquisition and applicable restore/reset/recharge/replacement costs.
5. Every corridor trap occupies a legal, distinct physical tile (or future explicitly authored footprint), subject to effective trap capacity, occupancy and topology rules. A legal-looking empty tile alone is not guaranteed to grant a slot.
6. Capacity is determined for the **real connected corridor geometry**, not an arbitrary count of editor-created line segments or bends. Splitting one hallway visually must not manufacture extra slots.
7. The player should ultimately be able to use the feature on **required-route corridors and eligible optional branches**, with category-specific rules respected. This requires explicit implementation and specification amendment because current corridor placement and encounters principally cover optional branches.
8. No stacking or free repeated triggering: every trap is a separately identified content assignment, resolved in actual traversal order with its own eligibility and lifecycle rules.
9. If a corridor shortens, is replaced or loses eligibility, preserve identities/investment where possible and require authorized repair/disposition. Never silently delete an over-capacity trap or canonically commit an invalid layout.

### 5.2 Why this decision belongs in MVP direction

- Makes longer corridors potentially useful without imposing artificial per-tile attrition.
- Connects Architecture/construction decisions with a real **Trapcraft** progression benefit.
- Creates a meaningful choice between floor space for rooms and floor space for defensive passageways.
- Unlocks new combinations of distinct physical defenses rather than merely increasing a global damage coefficient.
- Retains simple base gameplay while adding a comprehensible research milestone after the player knows how hallways and traps work.

### 5.3 Subsequent numerical approvals and remaining open tuning

**Superseded by DL-GAME-012 and DL-GAME-013 on November 19, 2026:** The earlier three- or four-tile, two-trap example is now an approved *initial capacity threshold*, not merely an illustration. Physical capacity scales in two-tile bands and is gated by sequential, global Trapcraft tiers. Tier 1 permits two traps on eligible 3-4 tile corridors; Tier 2 permits three on eligible 5-6 tile corridors; subsequent tiers continue by one trap slot and two tiles per rank. See the dedicated entries below for the full rule and implications.

Still open: exact set of tiers shipping in the MVP, individual project IDs, names, prerequisites, prices and durations; authored legal maximum corridor lengths, curved/bent geometry representation, actual trap slots for future definitions, per-floor balance and workload qualification. The threshold progression is approved; **unbounded construction length and unlimited research tiers are not automatically implemented or approved for the initial content package**.

### 5.4 Explicit specification and code conflicts requiring amendment

- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`, §5.1: current `spatial.corridor.straight_stone` has **TrapCapacity = 1** and length 1–4 tiles. Increasing effective capacity based on research requires an explicit approved rule and appropriate authored/config owner rather than silently editing the static definition.
- The same spec gives effective **LootCapacity = 1 only for an eligible optional capped dead-end corridor**, not a required-route corridor. This decision **does not** grant new corridor loot capacity.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CorridorContentMutationService.cs` validates assignments against the static corridor definition and currently resolves corridor target edges as optional physical corridors. Required-route trap placement needs new reviewed behavior and appropriate semantics.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/OptionalBranchStructuralEditService.cs` reports static corridor trap capacity. Effective research-gated capacity cannot be duplicated in a presentation-side counter.
- `docs/planning/phase-5b-production-tuning-and-run-condition-contract.md` has a **configured workload maximum of two corridor-content assignments per branch**. A future two-trap-plus-one-loot branch would exceed that allowance; revise and qualify the canonical bounds if the approved tuning requires it. Do **not** silently relax existing workload limits before design approval.
- `Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseFiveBRouteProjection.cs` orders the existing optional-branch content by physical distance from the source, followed by canonical identifiers. A multi-trap path must maintain actual reached-tile ordering, deterministic event identities, HP/formation consequences, and no duplicate triggers.
- `docs/planning/decisions/phase-8-owner-design-lock.md`, §4 and §8: Trapcraft belongs to General Research; separate general trap restoration modes have their own authored costs and exact-once authority. Extra slots must not accidentally grant free resets, magical recharges, Ward co-location, or new trap types.
- Do not invent a second wallet, writable research permission, corridor-content authority, save schema, or static derived-capacity copy. If persisted fields change, review exact schema and backward-compatible migrations.

### 5.5 Research and simulation implementation guidance

- Determine whether an existing canonical research entitlement can be consumed at edit validation, preview and commit, or whether a separately approved authored modifier is needed. A *derived* effective trap capacity should not become a second persisted writable truth.
- Use configured, versioned length/geometry eligibility, caps, and modifier ordering. Exact unlock timing should fit a meaningful early-to-mid MVP player decision, subject to balancing.
- Validate both category capacity and actual eligible tile occupancy. Trap positions are authoritative and distinct.
- Use the original run-start snapshot of trap assignment, research-derived capacity and rules where applicable; a later research completion or corridor edit must not rewrite a running party's corridor.
- Preserve deterministic placement ordering and on-traversal trigger ordering with stable assignment IDs. Consider multiple traps of the same type, one trap on a required-route corridor, optional-branch trap plus eligible loot, interrupted traversal, retreat/wipe mid-corridor, reset/recharge and full-floor vacancy ordering.
- Add localized upgrade explanation and previews that show base slots, earned additional slots, used slots and exact blocking reason when applicable.

### 5.6 Qualification targets

1. Before unlock, existing corridor capacity remains exactly as approved; no additional slot becomes available from length alone.
2. After real eligible Trapcraft completion, only qualifying corridors gain effective additional capacity, with no free instantiated trap or charge bypass.
3. Two distinct traps can occupy different legal tiles only under a formally authored capacity profile; same-tile and over-capacity placement must fail safely.
4. Test eligible required-route versus optional corridor encounters independently, including branch loot physical occupancy and existing loot eligibility restrictions.
5. Test physical event ordering, party HP, survival, retreat, Heat, trap spent/unready state, funded restore/reset/recharge/replacement and full-floor vacancy; no repeated triggers or duplicate wallet entries.
6. Test invalid shortened corridors, moving/bending/replacing a corridor with assigned traps, draft discard, canonical commit, save/reopen, migration and recovery of old one-trap layouts.
7. Check limits under maximum authored capacity and total applicable branch/floor/run workloads; qualify low-end mobile cost and exact offline event processing when implemented.
8. Run player-experience tests: do longer, researched corridors enable interesting defense arrangements, or do players simply maximize length/traps as a dominant strategy?

---

## 6. DL-GAME-004: Reuse previously paid corridor investment when redesigning hallways

**Status:** Owner-approved gameplay direction, November 19, 2026. Not implemented.\
**Owner choice:** Selected full transfer of existing paid corridor investment into a legally redesigned hallway, rather than limiting investment retention to shared physical tiles or requiring buyback at partial refund.

### 6.1 Approved player behavior

1. The player may reset or redraw hallway connections in a non-authoritative draft without immediately spending mana or creating refunds.
2. When a valid final draft replaces corridor infrastructure, its **verified previously paid corridor investment may transfer into eligible replacement corridor construction**, even if the replacement uses entirely different coordinates.
3. Matching tiles and unchanged edge IDs are not required conditions for such transfer. Actual legal geometry, final ownership, compatibility, and investment eligibility remain required.
4. A player pays any remaining eligible new construction charges after applying valid investment reuse. Permanent growth or new structures cannot be funded by duplicated historical investment.
5. The same investment amount cannot both transfer to a successor and generate a demolition refund. All allocation and settlement come from canonical starting state versus final draft, never the intermediate edit sequence.
6. The player sees reused investment, fresh construction expenditure, any true retirement refund, and final net wallet effect separately before atomic Save Changes.
7. Replacing hallways never implicitly moves, deletes, converts, or fully refunds attached traps or corridor loot. Their placement and retirement decisions follow DL-GAME-006 and 007, and currently approved content restrictions.

### 6.2 Rationale and gameplay tradeoff

Corridor routes are a core strategic control. Requiring players to repurchase the same infrastructure each time a route changes would discourage the intended run, inspect, revise loop. Paid investment should feel reusable, while adding new hallway capacity, rooms, or higher-cost structures still creates meaningful resource tradeoffs.

Full reusable investment is **not** a general ability to convert placed corridor construction into liquid mana. A redesign should not become a method for creating excess cash or a duplicate investment record.

### 6.3 Repository conflicts and integration constraints

- `Assets/_Project/Scripts/Economy/StructuralEconomyService.cs` currently carries investment on the same surviving edge ID and on certain replaced physical relationships by **overlap of occupied tiles** and eligible matching definition. It cannot implement the selected full transfer to wholly disjoint replacement geometry without a focused policy change.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralInvestment.cs` records `ConstructionMana` and `RenovationMana` by structural `StructureId`. Any new allocation must preserve ledger totals and valid per-structure identities, without a competing writable material bank or wallet.
- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`, §9 and `docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md`, Decisions 19 and 23, retain historical-investment pricing, canonical-to-final draft evaluation, and separate gross/refund/net preview.
- Do not assume the current legacy constrained room-movement and corridor reconnection APIs support the forthcoming user-directed hallway edits in DL-GAME-001.
- Preserve side-effect-free validation, deterministic matching/allocation, qualified authored construction geometry and costs, exact save recovery, and immutable run-start captures.

### 6.4 Technical design gates, not additional owner approvals

- Define eligible transfer scope (initial **same-floor** transfer is a technical recommendation, **not yet a separately owner-approved cross-floor restriction**), compatibility of corridor types, and whether renovated/modified corridor investment is also transferable.
- Specify deterministic allocation when one hallway becomes several or several become one, and the treatment of any verified paid investment that exceeds a cheaper replacement's capacity/value. The owner approved **full reuse**, not a specific accounting algorithm or free cash-out of unused credit.
- Define how investment from retired corridor connections splits between transferred amounts and eligible historical refunds, without counting the same paid unit twice.
- Determine exact versioned representation/migration only after verifying existing structural ledger and final draft contract; no schema number is approved in this log.

### 6.5 Acceptance and player testing

- Whole-hallway reposition with zero overlapping original tiles preserves eligible paid value.
- Equal-value replacements do not repeatedly charge the original purchase, and additional legal infrastructure has correct incremental cost.
- Split, merge, reverse, bend, reset and reconnect sequences yield one canonical final amount, independent of edit gesture/order, and no duplicate wallet credit.
- Repeated A to B to A experiments cost nothing if the committed result is identical to A; crash recovery/discard preserve canonical state.
- Traps/loot displaced by geometry changes remain detectable, with deterministic repair and explicit retirement consequences.
- Save, quit, reopen and legacy migration preserve investment integrity; failed commit never partially changes the wallet or ledger.

---

## 7. DL-GAME-005: Net-affordable atomic renovations using eligible refunds

**Status:** Owner-approved gameplay direction, November 19, 2026. Not implemented.\
**Owner choice:** Allow eligible genuine refunds generated in the **same atomic renovation** to offset its new spending, subject to wallet capacity, protected reserve, and exact-once transaction invariants.

### 7.1 Approved player behavior

1. At Save Changes, compute the complete final canonical-to-draft structural and content transaction under all applicable configured prices, legitimate investment transfers, genuine refunds, and reserve obligations.
2. Permit the transaction if the **actual final credited result** is legal and affordable, even when the player could not cover gross expenditure independently before applying eligible same-transaction refunds.
3. Preview gross new spending, reused investment, genuine refunds, any capped or uncredited refund amount, final net cost, resulting wallet, and protected obligations. Numeric UI detail can be expandable, consistent with existing Decision 22.
4. Do not temporarily publish a negative wallet, grant an early refund, spend protected reserves, or let expected but not yet authorized income fund the transaction.
5. Invalid, underfunded, stale, unverified, or storage-failed commits publish **nothing**. Retain the recoverable draft and diagnostic reason.
6. This applies to combined room, hallway, and content swaps when they enter the same legal whole-dungeon atomic draft. Applicable purchase-verification restrictions remain in force.

### 7.2 Why it was chosen

A player should be able to replace an existing defensive layout with a slightly more expensive one without separately accumulating the entire gross acquisition price. The economic decision is the **final** resource commitment, not an artificial timing requirement inside a transaction the player perceives as one change. This is central to avoiding avoidable friction while preserving meaningful expansion and restoration costs.

**Illustrative arithmetic only:** 100 usable mana, 150 of new cost, 80 genuinely eligible credited refund, 70 net spend, 30 final usable mana if every reserve and capacity requirement is satisfied. None of these example amounts is approved tuning.

### 7.3 Existing implementation conflict

- `StructuralEconomyService.RefreshBalance` currently checks `balance < preview.Cost` before refund availability can satisfy gross affordability. The movement-only pricing path also subtracts `result.Cost` independently. These paths require a coherently specified final settlement, not a UI-only net display.
- `TransactionalDungeonDraftCommit.cs` calls final structural/content pricing, enforces purchase gates, and publishes via existing atomic write/recovery authority. Extend that pipeline; never establish a second pricing or wallet owner.
- Existing `phase-7a0-graphical-dungeon-editor-design-lock.md`, Decisions 19–24 already establish uncharged draft experimentation, one atomic whole-dungeon commit, and required gross/refund/net confirmation. This decision changes **affordability settlement**, not draft authority.
- `docs/planning/decisions/combat-and-recovery-owner-design-lock.md` governs global protected mana and eligible reserve changes; the new rule cannot count protected mana as spendable.

### 7.4 Settlement implementation gates and tests

- Specify the authoritative order of actual spend, transfer, refund, wallet-cap application and reserve recalculation so the result is independent of incidental code iteration and cannot overcredit a capped wallet.
- For acquisitions with verification requirements, do not bypass the gate simply because the net spend is zero or negative.
- Qualify net-affordable but gross-unaffordable commits, genuinely underfunded final balances, reserve increases/decreases, wallet caps, stale-session replays, rounding, multi-floor atomicity, crash/reopen and exactly-once retries.
- No new mana grant, arbitrary formula, source of income or refund percentage is approved.

---

## 8. DL-GAME-006: Direct content management, without a permanent New versus Owned inventory

**Status:** Owner-approved gameplay direction, November 19, 2026. Not implemented.\
**Owner choice:** Remove the permanent player-facing `New` versus `Owned` distinction for monsters, traps and reusable loot-node structures. Players place newly acquired content, **Move** an existing item, **Replace** it, or permanently **Remove** it, instead of depositing retired assets in a persistent owned-content collection.

### 8.1 Approved player experience

1. **Add:** Choose an eligible, unlocked monster, trap or loot-node structure and purchase/place a fresh instance; authored capacities, geometry, verification and charges apply.
2. **Move / Reassign:** Select an existing placed instance and give it a new legal location. This is not a repurchase; preserve its identity and all valid individual properties, including applicable monster level and injury, trap state, loot-node focus and stock, and legal Ward links.
3. **Replace:** Retire the selected instance and acquire a different one in the same final draft. Refunds and purchases settle together under DL-GAME-005, not through a separate Owned storage step.
4. **Remove:** Explicitly permanently retire the instance, subject to category-specific validity and the base acquisition refund rule in DL-GAME-007. Retirement is an irreversible canonical disposition on successful commit, not an unassignment to a player inventory.
5. **Reset Hallways / Delete Room:** Do not silently move, sell or delete displaced contents. Show affected assets as unresolved **draft** issues; player repositions them or explicitly chooses their permitted permanent removal before committing a valid layout.
6. **Discard Draft:** Restores the complete original canonical state, content identities, inventory/stock, wallet and historical ledgers. Existing ongoing runs continue using their own immutable run-start captured state.
7. Keep content management contextual to the selected room/corridor and understandable on mobile. The previously approved direct drag gesture is optional convenience, not a prerequisite for domain correctness.

### 8.2 Rationale

A permanent collection of unused purchased Goblins, Skeletons, traps and loot nodes introduces a third place players must search when simply rebuilding a dungeon. It also prevents the player from recovering liquidity after changing monster family or defense strategy. Direct relocation plus explicit refunding retirement better expresses the build, run, inspect, revise loop and reduces UI complexity.

Removing `Owned` from the **player workflow** does not justify simply deleting all internal custody records or historical save fields. Player-facing simplicity must coexist with correct migrations and ownership accounting.

### 8.3 Existing design and code reconciliation

- `Assets/_Project/Scripts/UI/ProductionDungeonRoomContent.cs` currently has `contentBrowserOwned`, `newContent`, `ownedContent`, `SelectNewContent` and `SelectReturnedContent`. These controls become superseded player-facing behavior once a safe replacement provides parity.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSpatialMutation.cs`, `CorridorContentMutationService.cs` and `StructuralDeletionService.cs` currently unassign reusable assets to `LifecycleAndOwnership.ReturnedContents` rather than retiring them with refunds.
- `Assets/_Project/Data/Production/Save/structural_content_removal_policy.json` currently maps all eight initial monster/trap/loot-node definitions to `ReturnToPlayerCustody`. New policies, source authority and tests need formal dated specification amendments.
- `docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md`, Decisions 39, 41 and 43, preserve room-first management, explicit Move/Reassign, and one whole-dungeon atomic draft. Those concepts remain valid; the New/Owned presentation and persistent custody outcome are the superseded parts.
- `docs/planning/decisions/phase-8-owner-design-lock.md` §8 defines narrow Treasure Ward and loot-node association/returned-custody semantics. These must be reconciled, not silently discarded; the Ward remains its own stable asset and links only to a legal node.
- The currently shipped complete-save model includes returned content and `MonsterContinuity` state for both assigned and returned monsters. Preserve old paid assets while converting, offering safe resolution, or otherwise migrating old returned entities. Do not silently liquidate them at unverified prices or erase their progress.

### 8.4 Implementation and validation gates

- Provide deterministic identity-preserving cross-room/content reassignment with legal capacities and category rules; do not create duplicate instances during drag or draft replay.
- Define explicit retirement and historical-cost ownership in the canonical save architecture; no parallel inventory/wallet authority.
- Existing returned-content saves need a versioned, owner-reviewed migration or accessible recovery path that never strands assets after the UI removes the Owned selector.
- Preserve trap readiness, Ward links and loot-stock state on moves; fail closed if a target cannot accept the asset. Resolve associations explicitly when a protected loot node or its Ward is retired.
- Provide category-aware localized consequences; require confirmation for destructive retirement, especially for a progressed monster or stocked loot node.
- Test old-save reopening, failed exact-byte persistence, stale drafts, max category capacity, cross-floor attempts, duplicate IDs, mixed multi-floor edits, current active-run isolation and subsequent run behavior.

---

## 9. DL-GAME-007: Full refund of eligible original base content acquisition costs

**Status:** Owner-approved gameplay direction, November 19, 2026. Not implemented.\
**Owner choice:** On permitted **permanent retirement**, return **100% of the verified actual original eligible base acquisition mana paid** for a monster, recoverable trap apparatus, or reusable loot-node structure. This is not a promise to refund upgrades, combat recovery, trap restoration or stocked reward items.

### 9.1 Approved refund boundary

1. Refund the historical eligible base acquisition payment for an actually owned, eligible asset **once**, not a guessed current catalog price or the displayed market value. If the original acquisition was legitimately free, its refundable paid base is zero.
2. A direct legal Move/Reassign carries the asset and its identity forward; it does not trigger a refund or repurchase. An explicit Replace performs retirement plus a new acquisition in the same atomic draft.
3. Eligibility is about the existence and recoverability of the **asset**, not all past operating expenses. Actual paid monster levels, respecialization, form or class investment, combat recovery, trap recharge/reset, and restocked/generated items are separate systems with separate policies.
4. A consumed single-use trap has no recoverable apparatus to retire merely because it once had a purchase price. A deployed but unready reusable trap may retain an apparatus; classify per authored trap lifecycle, not by name or one generic `triggered` flag.
5. Refund of a loot **node** is not automatically a refund of its contents. Existing paid stock, ignored items, collected items and future failed-extraction partial refunds retain distinct authoritative receipts and eligibility.
6. A Treasure Ward is a separate instance with its own eligibility and upgrade history. Retirement of a linked Ward or loot node must atomically repair/resolve the association; no double sale or orphan.
7. Verified refunds offset eligible same-transaction new acquisitions under DL-GAME-005 after all protected reserve, capacity and purchase verification constraints.
8. No refund can exceed historical eligible original paid base; no duplicate receipt, replay credit, catalog reprice or cash creation from migrated/unknown data.

### 9.2 Design reasoning and worked example

Players should be able to abandon a low-level monster-family strategy and apply the recovered base resources toward a newly unlocked alternative. The economic commitments remain in the difference between content costs, extra construction, upgrade and restoration costs, and future reserve requirements, rather than in an accumulation of unwanted permanent owned assets.

**Illustrative example using the presently authored starter prices, not a newly approved balance table:** Five Goblins historically acquired at 20 mana each yield 100 eligible base mana if their actual paid costs and retirement eligibility are proven; five new Skeletons currently priced at 25 each cost 125; 25 additional mana is due, subject to unlocks, wallet cap, reserves and the purchase gate. Paid Goblin upgrades are not automatically returned. This is a simple baseline case, not an endorsement of any unverified legacy purchase record.

### 9.3 Exact conflicts with current implementation and approved future specs

- `Assets/_Project/Scripts/Economy/ContentAcquisitionEconomyConfiguration.cs` contains *current acquisition price* by `OptionId`, not a per-instance **historical actual paid purchase record**. `StructuralEconomyService.PreviewFinalContentDraft` prices newly added identities, but has no corresponding exact paid-base refund for removed ones.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs` owns monster-level, HP and revival state against stable `AssignmentId`; source identity must remain on legal moves, and individual level/progression must be intentionally retired, not transferred to a newly purchased replacement.
- `docs/planning/decisions/combat-and-recovery-owner-design-lock.md` protects permanent monster identity and injury across moves, plus floor-vacancy recovery and global required reserve. Free original acquisition refunds do not make combat recovery automatically refundable or imply instant full-health preservation on replacement.
- `docs/planning/decisions/phase-8-owner-design-lock.md`, §§5, 7, 8 and 9, protects researched form/class distinctions, individual paid advancement, trap-specific paid restoration, separately paid stock, Ward links, exact-once receipts, original-cost refund provenance and reserve protection. Its explicit return-to-custody behavior conflicts with the new permanent-retirement rule and needs a narrow dated amendment; do not rewrite unrelated research, loot-extraction or Ward behavior.
- `Assets/_Project/Data/Production/Save/structural_content_removal_policy.json` and current unassignment/redeployment tests explicitly assume **no mana refund** and permanent custody. Those assertions remain historical shipping evidence until changed through a qualified PR.
- This is an **exceptional recovery of previously paid mana**, not a new general mana-generation source; the economic sink/recovery and current mana capacity rules remain in force.

### 9.4 Open technical and balance gates

- Decide how to store, recover or reliably derive per-instance historical paid base, including promotions/discounts, starter grants, old saves and legitimately zero-cost instances; preserve exactly once and fail closed on contradictory evidence.
- Decide how legacy returned-content records are converted or made accessible without guessing missing purchase receipts or destroying paid assets.
- Confirm trap definition lifecycle classifications and collectible apparatus existence before permitting retirement, especially paid replacement versus reset/recharge types.
- Specify precise stocked item disposition and Ward association consequences on node retirement. Do not double-credit prior paid item stock or alter the independently approved collected-but-unextracted partial refund.
- The **percentage and treatment of monster progression/level/class/form investments** is deliberately left open and is the proposed next owner decision. Nothing in DL-GAME-007 automatically refunds it.
- The relationship between refund eligibility and monster injured/defeated state has **not yet been owner-approved**. Its interaction with funded recovery and newly acquired combat-ready instances needs a balance decision before code.
- Avoid hardcoding new amounts or assigning speculative save schema; qualify compatibility, stale/retry failure, exact-byte atomic publication and mobile-safe compute.

### 9.5 Tests and manual UAT

- Acquire at known price, move, retire, replace, reopen, retry and replay. Confirm one actual paid-base refund, no refund for move, no duplicate payment on legal reassignment and no post-retirement resurrection of the same identity.
- A zero-cost legitimate acquisition refunds zero. A previously paid discounted instance refunds actual paid amount despite later changes to catalog price.
- Demonstrate a net-affordable five-Goblin-to-five-Skeleton bulk swap, plus rejected insufficient-final-wallet/reserve and rejected purchase-gate cases. All failures preserve exact wallet, spatial state, paid ledger and draft.
- Verify levelled or injured monsters retain progression/HP on legal moves, and intentionally lose the retired individual's identity on permanent retirement. Future progression reimbursement is not assumed.
- Verify consumed vs recoverable traps and paid restoration histories, stocked/empty loot nodes, linked Wards, saved recovery state and historical returned assets.
- Verify active-run snapshots, offline event order, canonical ordering, localized loss/refund preview and no duplicate credits under crash/reopen or retry.
- User test: replacing a floor of Goblins with Skeletons should require selecting existing monsters and replacing/removing them, not navigating a separate permanent Owned stockpile.

---

## 10. DL-GAME-008: 80% refund of verified paid individual monster progression

**Decision date:** November 19, 2026.\
**Status:** Owner-approved future economy design; individual advanced Monsterology progression and paid-cost provenance are not fully implemented.\
**Owner choice:** Permanent monster retirement returns the already approved 100% eligible historical base acquisition payment **plus an initial, tunable 80% of eligible, verified paid individual progression investment**.

### 10.1 Approved player behavior

1. Retirement is a deliberate permanent action. It retires the monster's stable individual identity and individual level/form/class state. A replacement monster is a separate purchased entity and does not inherit the prior monster's HP, individual progression or identity.
2. A valid move between legal places retains that same monster and **all** individual progression; no resale or new acquisition occurs.
3. Retirement refunds the eligible actual base paid amount at 100% (DL-GAME-007) and eligible actual paid individual progression at **80%** initially. The fraction belongs to versioned economy/configuration authoring and may be retuned with deliberate save-rule compatibility.
4. The 80% share concerns instance-specific investments, such as eligible paid level advancement, form advancement or instance-specific class development. **Globally purchased research permissions do not transfer into the refund basis** and remain unlocked.
5. Previously spent health recovery, revival, upkeep and other operational mana are not included in base or progression refund. A single historical expenditure cannot be counted in more than one refund bucket.
6. Previously paid progression that becomes dormant on a valid form/class change must follow a single documented provenance and refund classification. It must not be double-counted simply because multiple prior forms/classes appear in the monster history.
7. No premium-currency conversion, new source of mana or speculative respecialization discount is created by this rule.

### 10.2 Rationale and strategic tradeoff

The player should be free to improve and change a monster roster without losing every level, but retaining a developed monster should still offer an economic advantage. An 80% progression return leaves 20% of eligible paid individual progression unrecovered; keeping and moving the same monster retains that value and its developed identity. A strong monster can be reassigned to a lower-difficulty region rather than retired. Recruiting a higher initial level/form may be attractive if its authored cost justifies it, but **no specific recruitment discount is approved here**.

**Illustrative arithmetic, not tuning:** a base purchase paid at 20 mana and eligible progression paid at 80 mana produces a 20 + (0.80 x 80) = **84 mana** refund before any approved rounding, credit cap and reserve calculation. The 20 and 80 are example payments; the 80% rate is approved.

### 10.3 Repo and implementation reconciliation

- `docs/planning/decisions/phase-8-owner-design-lock.md`, Monsterology: family, form, individually researched class, paid level, and per-instance advancement are distinct. Preserve legal evolutions, stable IDs, HP proportionality and dormant investments where specified.
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs`: currently stores individual ID, initial level, HP and revival flag, but is not yet a full arbitrary-level investment ledger. Do not claim otherwise.
- Extend the existing single complete-save/economy authority with evidence of original instance-specific paid costs. Reject double credit; migrate older live/returned content without silently wiping assets or inventing unsupported payment provenance.
- Do not use today's catalog prices as a substitute for actual old paid progression. Determine precise rounding, integer/fraction representation, purchase modifiers, direct higher-level recruitment, discounted/complimentary investments and historical compatibility during the implementation contract review.

### 10.4 Validation

Test moved leveled monster unchanged; paid progression at zero, one, and multiple upgrades; discounted acquisitions; dormant class investment; retirement/recruitment in one multi-floor draft; repeated previews, commit/retry and reopen; old saves; invalid provenance; cap/reserve saturation; exact-once refund; and distinct IDs for purchased replacements.

---

## 11. DL-GAME-009: Condition-independent monster retirement refund

**Decision date:** November 19, 2026.\
**Status:** Owner-approved future gameplay economy.\
**Owner choice:** Eligible monster retirement refunds are **the same whether the monster is fully healthy, injured, defeated or awaiting revival**. No additional injury deduction or recovery prerequisite is required.

### 11.1 Approved behavior and rationale

- Compute the base plus eligible individual progression refund solely from verified paid investments and applicable authored refund fractions, never the current HP, revival flag or condition.
- The player may retire a defeated monster without first buying its revival. Retiring it clears its identity and associated future recovery obligations from the final canonical dungeon under the one authoritative recovery/reserve policy.
- Moving an injured or defeated monster preserves that condition. It does **not** cure, revive or reset it; paid automatic recovery remains separately governed by the vacant-floor policy.
- Do not add a punitive condition adjustment merely to prevent the theoretical strategy of retiring an injured starter and buying a fresh one. The owner explicitly accepts that possible optimization unless testing finds a material balance issue.

### 11.2 Important qualification

The owner noted that at long-term scale, manually optimizing hundreds of monsters across many occupied floors is unlikely to dominate play. The approved **MVP is five floors**, however, and its editor supports an **atomic whole-dungeon draft**, so multiple-floor replacement may be feasible. This is a **balance risk to measure, not a basis to override the approved simple rule**. Potential future premium-currency monetization is not evidence of sound balance and should not be used to add or justify a cash-flow mechanic.

### 11.3 Integration and tests

Preserve `docs/planning/decisions/combat-and-recovery-owner-design-lock.md`'s automatic vacant-floor recovery, floor occupancy, protected reserve and no offline-created permanent losses. Compute retirement against the final draft only. Verify identical refunds for otherwise identical healthy/injured/defeated monsters; updated required reserves; no forced recovery; unchanged active-run snapshots; duplicate/retry protection; save/reopen; and failure atomicity. Explicitly test the potential no-cost replacement optimization in player balance studies.

---

## 12. DL-GAME-010: Refund surviving trap apparatus, not consumed single-use devices

**Decision date:** November 19, 2026.\
**Status:** Owner-approved retirement policy; authored per-trap lifecycle and historic payments remain implementation gates.\
**Owner choice:** Full eligible original base acquisition refund for **surviving, recoverable** trap apparatus, regardless of readiness. **No apparatus refund when a genuinely single-use device has been fully consumed and no recoverable apparatus remains**.

### 12.1 Final player-facing rule

| Instance condition | Eligible base refund upon permanent retirement |
| --- | --- |
| Intact and ready | 100% of verified historical eligible base payment |
| Still physically present, awaiting mechanical reset | 100% of verified historical eligible base payment |
| Still physically present, awaiting magical recharge | 100% of verified historical eligible base payment |
| Fully consumed single-use device without recoverable apparatus | 0% apparatus refund |

Moving a surviving trap keeps its identity, current ready/unready state and legitimate persistent configuration. A move is not a free reset or recharge. Previously paid resets, recharges, replacements and other operational charges do not automatically increase refundable base investment. The player-facing edit preview identifies whether this is **Move** or **permanent Remove** and displays the resulting financial consequence.

### 12.2 Why it was chosen

The owner favors simple, predictable base refunds over condition-by-condition resale penalties. Retiring a mechanically unready but recoverable trap is not a smaller asset refund merely because it has activated. The player accepts that retiring, receiving the full base refund, and reacquiring a new trap might sometimes compete economically with restoration. This is a balance observation for playtesting rather than permission to introduce a special condition charge.

### 12.3 Specification and implementation contract

- `docs/planning/decisions/phase-8-owner-design-lock.md`, Trapcraft D199/D200: each trap definition must author its lifecycle as paid replacement, mechanical reset or magical recharge. **Do not infer that Spike Trap is single-use from its name**.
- The definition and saved instance state must distinguish *unready but surviving* from *actually consumed*. Do not issue a refund based only on a static trap name or a no-longer-visible icon.
- An installed Treasure Ward is also a separately identified trap with authored attachment and paid upgrades. Preserve its association/retirement constraints; do not invent refundable upgrade costs.
- Content acquisition provenance and retirement credits share the existing canonical economy and complete-save atomic transaction; no second refund path, free manual repair or double refund. Preserve older returned-content assets with reviewed migration.

### 12.4 Tests

Same-price surviving traps in all readiness conditions must receive the same base refund. Fully consumed single-use devices have no apparatus refund. Test multiple identical traps, paid restorations, move during/after being triggered, wrong-content classification, retire/rebuy, retry after partial durability failure, reserved mana, active-run isolation and migration/reopen. Test all newly authored lifecycle definitions individually, not only the initial Snare/Chilling examples.

---

## 13. DL-GAME-011: Move loot nodes with stock; retirement forfeits uncollected stock

**Decision date:** November 19, 2026.\
**Status:** Owner-approved loot-node retirement and move design; per-node paid stock authority remains future work.\
**Owner choice:** A legal move preserves the **node, its stocked rewards and their paid provenance**. Permanent retirement returns the node's eligible original base acquisition payment and **forfeits any uncollected stocked items without separate stocked-item refund**, with clear disclosure before commit.

### 13.1 Approved player experience

1. Move a loot node: preserve the same node ID, focus, authored configuration, legitimate paid stock, each stock item's identity/provenance and its already charged cost. No new item is minted or rerolled; no additional stocking debit is created.
2. Retire a loot node: refund the node apparatus/base acquisition payment under DL-GAME-007, but **do not** refund separately paid uncollected/unextracted stocked items merely because the container is retired. Those stock entries are permanently disposed exactly once at final commit.
3. Discard an editing draft: neither the canonical node nor its stock changes. A pending draft can show a consequence preview but cannot award a refund or destroy stock.
4. Show explicit loss in final review, including the node, number/type or other sufficiently informative description of forfeited paid stock, and its mana consequence. High-impact retirement may require explicit acknowledgement under existing editor rules.
5. If a loot node is associated with a Treasure Ward, resolve the Ward's legally independent move/retirement within the same draft. Do not silently orphan, refund, destroy or detach an otherwise recoverable Ward.
6. The existing narrow Phase 8 **partial refund for collected but not successfully extracted items** remains a separate settlement cause; uncollected forfeited stock is not treated as collected, lost-in-escape or extracted.

### 13.2 Rationale and alternatives rejected

The player can preserve the reward investment by moving its container but may choose permanent removal knowing the stocked items are lost. No new item-transfer inventory, per-item swapping UI or general unstokcing resale loop is introduced. This avoids arbitrage through repeated removal/rerolling of randomly selected stock and preserves the meaningful distinction between the node purchase and separately purchased rewards.

### 13.3 Implementation dependencies and validation

Read `docs/planning/decisions/phase-8-owner-design-lock.md` §§7–8 and the existing authored optional-corridor dead-end loot rule in Spec 38. When a move changes tile, route class, room or floor, validate target category capacity, eligible topology, floor/room constraints, stock compatibility and Ward link in the same canonical candidate. Do not quietly adapt illegal stock or convert it into a paid item elsewhere.

Resolve receipt ownership and exact-once stock disposition with future node-level persisted stock. Preserve running-party immutable snapshots and the original item refund attribution for actual collection. Apply versioned migrations where necessary, keeping legacy assets intact. Test fully stocked, empty, depleted, waiting-for-mana, linked Ward, floor-switch, same-node identity, refill after vacancy, active party, noncollecting party, retirement/discard/reopen, fail-closed stock mismatch and explicit localized forfeiture review.

---

## 14. DL-GAME-012: Potential trap capacity scales by two-tile corridor length bands

**Decision date:** November 19, 2026.\
**Status:** Owner-approved capacity geometry rule, not yet implemented or authored as production capacity.\
**Owner choice:** A physically longer eligible corridor supports **one potential trap slot per two-tile length band, rounded up**, with the research level gating usable additional slots. A six-tile corridor can qualify for **three** trap positions.

### 14.1 Approved progression of potential physical capacity

| Physical corridor length | Potential trap slots, when research permits |
| --- | ---: |
| 1–2 tiles | 1 |
| 3–4 tiles | 2 |
| 5–6 tiles | 3 |
| 7–8 tiles | 4 |
| 9–10 tiles | 5 |

For a legal corridor of length `L`, length-qualified potential capacity is `ceil(L / 2)`. This numerical relationship is an approved design requirement. It is **not** automatic authorization to build a corridor of any length: the catalog and current floor geometry rules still apply.

### 14.2 Gameplay and physical constraints

- A purchased trap must be placed at its own legal eligible physical tile and count against both tile occupancy and effective category capacity. No two ordinary trap assignments occupy the same tile; eligible dead-end loot occupies a **different tile**.
- Under the current dead-end loot model, a 3-tile branch could fit two traps plus one eligible loot item, provided all tiles and topology validate. A two-tile dead end cannot fit two traps and an additional loot item simultaneously.
- Required-route traps and optional-branch traps should use the same researched length-to-capacity principle, subject to category-specific eligibility and the required-route encounter system being implemented. **This does not authorize ordinary loot on required corridors**.
- Splitting one logical continuous corridor into multiple technical edges, editor segments or bends cannot manufacture extra initial slots or reset the researched cap. Determine what constitutes one connected corridor using a canonical, validated geometric authority.
- The player makes a real spatial tradeoff: longer routes cost floor tiles and configured construction mana, use physical travel time and may host more separately paid defenses. No automatic generic hallway-length damage is added.

### 14.3 Required specification and test changes

Amend Spec 38's initial `TrapCapacity = 1` and any static-only capacity consumers with a single derived research-aware effective-capacity rule. Validate geometry and assignments in preview, candidate validation, commit, reload, and run-start snapshots. Test all band boundaries (1/2/3/4/5/6), blocked extra slot without research, legitimate multiple traps, shared-loot tile exclusion, split/merge exploits, invalid shortening, retired trapped corridors, event order, deterministic saves and existing migrated one-slot corridors.

---

## 15. DL-GAME-013: Global ranked Trapcraft permissions unlock increasing corridor capacities

**Decision date:** November 19, 2026.\
**Status:** Owner-approved research progression model; IDs, prices, durations and initial shippable rank count remain content/implementation gates.\
**Owner choice:** A **global sequential General Research / Trapcraft tier series** increases the maximum usable trap slots on all sufficiently long legal corridors. No per-corridor upgrade or slot-activation payment is required.

### 15.1 Approved research bands

| Completed capacity tier (`R`) | Global researched maximum | Minimum length to use newest slot |
| --- | ---: | ---: |
| None, `R = 0` | 1 trap | 1 tile |
| Tier 1, `R = 1` | 2 traps | 3 tiles |
| Tier 2, `R = 2` | 3 traps | 5 tiles |
| Tier 3, `R = 3` | 4 traps | 7 tiles |
| Each next tier | +1 trap maximum | Next two-tile band |

**Derived effective category-capacity ceiling for compatible legal corridors:**

`min(ceil(L / 2), 1 + R)`

`L` is validated connected physical corridor length. `R` is the highest *canonically completed and claimed* applicable Trapcraft capacity tier. Additional authored corridor restrictions and valid distinct tile availability further constrain actual placements. This is a **derived** result, not a new writable save field. Do not infer that Tier 3 or unlimited future ranks ship in the five-floor MVP.

### 15.2 Research and balance rationale

This resembles the approved **ranked Architecture floor-permission** model, but is a distinct General Research category/branch. Tier projects are separately authored, ordered, manually started and individually paid at their approved research costs, progress under the existing research authority, and unlock globally only on legitimate completion/claim. The player then pays ordinary acquisition and restoration for traps used in those slots. Physical corridor construction costs and space pressure remain, so no second per-corridor slot fee is needed.

### 15.3 Repo conflicts and implementation gates

- `docs/planning/decisions/phase-8-owner-design-lock.md` §4 owns two-track Research, manual configured costs/time, eligible verification and exact-once claim; this decision supplies **capacity permissions**, not a new Research timer, independent wallet or invented node ID.
- The currently authored `spatial.corridor.straight_stone` is length 1–4 with static TrapCapacity 1. The first rank could affect that authored length, but Tier 2 requires a legal 5–6-tile corridor authored later. No tier may silently extend corridor geometry.
- Existing `CorridorContentMutationService` accepts static capacity and optional-branch targets. Required-route placements, route projection, occupancy, research permission validation and run snapshots require coordinated review.
- `docs/planning/phase-5b-production-tuning-and-run-condition-contract.md` allows only **two processed optional-branch content assignments**, currently one trap plus one loot. Tier 1 plus optional loot may require three; Tier 2 plus optional loot may require four. Revise configuration/workload acceptance tests deliberately, not as an unchecked limit removal.
- Legacy complete saves with one-slot corridors must remain valid; newer paid research cannot retroactively change an already started adventurer run.

### 15.4 Validation

Validate rank 0 through available tiers with length boundary cases; completed-but-unclaimed and pending/invalid prerequisites grant no permission; globally apply to new and old eligible corridors; no free trap creation; no double capacity from splitting; affordability and reserve for separately purchased traps; over-capacity invalid candidates; deterministic traversal and save/reopen; exact offline event processing once available; and no player-visible stale-capacity preview after a research claim.

---

## 16. DL-GAME-014: Longer corridor geometry progresses through construction content, not Architecture research

**Decision date:** November 19, 2026.\
**Status:** Owner-approved design choice, subject to authored length and validation gates.\
**Owner choice:** **Option A.** New legal corridor lengths become available through ordinary approved construction content progression, **not** through an additional dedicated Architecture research level required solely to extend length.

### 16.1 Player-facing rule and rationale

When an eligible longer corridor is supported by the canonical construction catalog and the player has the normal construction eligibility, the player may build it at its configured physical footprint and mana cost. A player does not first have to purchase a *separate corridor-length Architecture research tier* just to create the longer geometry.

Trapcraft independently governs how many traps that geometry can host. Example: a physically legal 6-tile corridor can be built under its authored construction rules, yet without Trapcraft Tier 2 it **cannot** use three trap slots. The player still pays for corridor tiles and separately acquired traps. This keeps the construction decision legible and avoids a double research gate for the same defensive opportunity.

**Scope distinction:** The choice does not repeal other ordinary room, floor, construction-catalog or lifecycle eligibility. It does **not** declare all lengths or curved/maze paths immediately buildable in MVP. The initial Straight Stone definition remains 1–4 tiles until an approved content/validator change supports more geometry.

### 16.2 Dependencies and tests

- Update approved spatial content/geometry in Spec 38 and production authoring, including max length and route connection validation; decide whether a longer definition or a versioned expansion of an existing corridor type minimizes old-save risk. Do not silently reclassify saved edges or retrofit invalid footprints.
- Ensure authored longer geometry fits floor bounds, capacity, sockets, adjacent endpoints, optional-branch dead ends, no-overlap rules, stable edge IDs and deterministic path/travel time.
- Confirm direct room-to-room corridor authoring, segment/bend representation and edit reset work with the same validated physical length used by DL-GAME-012 and DL-GAME-013, without arbitrary segment-count capacity resets.
- Qualification: length 4 versus 5, max legal length versus first illegal length, turns/segments where supported, no ability to buy 3 slots before Tier 2, old saves/reopen, invalid candidate unchanged, run-start isolation and final economy preview.

---

## 17. DL-GAME-015: Suggested connections with player-draggable doorway positions

**Decision date:** November 19, 2026.\
**Status:** Owner-approved design direction; domain/socket authoring and migration pending.\
**Owner choice:** Generate an initial legal, compatible pair of doorways and a suggested hallway. Permit the player to move either doorway to **any other authored-legal position along its room boundary**, potentially on a different wall, when a valid passage and hallway can be constructed. Preserve further manual hallway editing.

### 24.1 Player behavior

The player places a room on a legal footprint, receives an initial suggested connection, and may reposition the doorway from one point on a wall to another or onto a different legal wall. A moved doorway changes its connection preview only; it does not move the room or reposition its contents. The editor updates the affected proposed hallway when an eligible route exists, while allowing the player to reshape the result. Invalid endpoints, insufficient corridor reach, blocked geometry, occupied doorway boundaries, incompatible connections, or floor-capacity violations receive explainable feedback. Invalid drafts may remain editable, but cannot be canonically committed.

Existing fixed-structure access points, including Entrance Hall and Completion Terminal, are **not** automatically made movable by this approval. The required source and destination opening semantics and exact persisted positions are technical gates, not license to infer that every boundary tile is legal.

### 24.2 Reconciliation

`Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md` currently defines one approved authored doorway per wall for each initial room, backed by `ContentAuthoring/DungeonSpatial/tables/room_connection_points.csv`. `StructuralEditService` and `StructuralRenovationService` resolve the fixed socket pairs. The future implementation must formally amend this authoring restriction, retain legal compatible socket types and authored wall constraints, and preserve existing saved edges. Do not introduce a separate visual-only doorway authority that disagrees with canonical run geometry.

### 24.3 Tests and open gates

Test shifts along the same wall and across legal walls; different orientations; doorway conflicts; neighboring occupied content; shared borders; direct doorway versus physical corridor; too-short/too-long routes; exact opening persistence through save/reopen and movement/rotation; active run snapshots; invalid draft and Discard; accessibility/touch targeting. Authored legal boundaries, socket compatibility, save representation and old-save migration need engineering review.

---

## 18. DL-GAME-016: Suggested legal corridor with player-controlled reshape

**Decision date:** November 19, 2026.\
**Status:** Owner-approved direction; authoring implementation pending.\
**Owner choice:** **Option A.** Generate a deterministic suggested legal hallway between selected compatible endpoints when possible, then allow manual segment/bend reshaping and deliberate draft confirmation.

### 18.1 Player behavior

The proposal is a convenience preview, not an authoritative automatically imposed route. The player can manipulate bends, endpoints and segment placement within legal bounds, inspect cost/capacity changes, and accept or discard the draft result. If no path exists, indicate why and permit manual edits or a different legal endpoint. Changing one proposed connection must not silently reroute unrelated corridors or move existing rooms. The completed proposed path cannot become canonical without the existing whole-dungeon Save Changes validation.

### 18.2 Reconciliation and validation

This extends the approved Phase 7A0 Decisions 32 through 35 (tap-to-connect, deterministic suggestions, segment/bend manipulation and incomplete-preview semantics). Existing route construction is straight-only and detects ambiguous authored pairs. Implementation must use the same canonical route validator for suggested and manually edited paths, not assume every visually attractive polyline is legal. Test deterministic choice when multiple alternatives exist, invalid or blocked paths, saved path equality, cross-device repeatability, Discard and concurrent-party snapshot isolation. Route-ranking rules are a technical/configuration design gate, not a new randomized routing mechanic.

---

## 19. DL-GAME-017: Any number of legal corridor bends within total-length limit

**Decision date:** November 19, 2026.\
**Status:** Owner-approved direction; bent-geometry validation and persistence pending.\
**Owner choice:** A physical corridor may contain any number of bends allowed by its configured **total** occupied length and valid geometry; no arbitrary two-bend limit.

### 19.1 Player-facing and simulation rules

For the initial grid-aligned MVP geometry, use connected, one-tile-wide orthogonal segments and 90-degree turns. A bend's physical tile counts once. The continuous corridor remains **one logical edge and one trap-capacity basis**, not independent purchasable sub-corridors. Do not grant extra traps, reset historical investment, fabricate doorways, or duplicate travel by segmenting the representation. A route must be a well-defined contiguous traversal, not overlapping itself or creating an unapproved branch, junction or loop. The player may bend around rooms and other legal geometry and may later place traps in legal tiles. Segment appearance and geometry cannot substitute for actual ordered movement and interaction authority.

### 19.2 Reconciliation and tests

Current `TileGeometry.TryResolveStraightCorridor`, straight pair resolution in `StructuralEditService` and the straight-validation assumptions of Spec 38 do not support arbitrary bends. `FloorRouteEdge.Footprint.OccupiedTiles` is canonicalized as a tile set, whereas a bent corridor requires deterministic traversal-order derivation or approved persisted order from its exact endpoints and permitted openings. For side-contacting corridors, Manhattan distance from the origin is **not** sufficient to reconstruct traversal order. Review route projection and trap ordering, especially `PhaseFiveBRouteProjection`, before implementation. Validate all corner patterns, no self-overlap, legal side-contact, reversal, stable IDs, six-tile maximum, changed doorway positions, repeated reroutes, exact run travel time, trap order and save/reopen under legacy schema constraints.

---

## 20. DL-GAME-018: Six-tile initial Straight Stone Corridor maximum

**Decision date:** November 19, 2026.\
**Status:** Owner-approved initial content tuning; production authoring/validator changes pending.\
**Owner choice:** Increase the initial configured maximum physical length of `spatial.corridor.straight_stone` from four to **six occupied corridor tiles**, counting every corner tile once.

### 20.1 Gameplay interaction with Trapcraft

The six-tile corridor requires normal construction mana and real floor space. Its **potential** trap capacity is three under DL-GAME-012 but usable capacity is one without Trapcraft capacity research, two after Tier 1, and three after Tier 2. Each trap still needs a legal unoccupied tile, and an optional dead-end loot node remains a separately placed item requiring its own eligible tile. No Architecture length research is required purely to unlock the six-tile geometry, per DL-GAME-014. Longer future corridors remain a separate content review and are not automatically in MVP.

### 20.2 Specification and validation changes

Spec 38 and the production spatial content package still authorize a maximum of four. The new maximum needs explicit authoring, compatibility validation for old 1–4-tile corridors, and bounds, overflow, wallet, canonical time and deterministic interaction tests at lengths 4, 5, 6 and illegal 7. Do not automatically grow existing corridors or rescale their historical mana records. The six-tile initial number is approved; no new per-tile acquisition price or extra research cost has been decided.

---

## 21. DL-GAME-019: Touching corridors have authoritative separating walls

**Decision date:** November 19, 2026.\
**Status:** Owner-approved gameplay and physical-geometry rule; edge-wall mechanics not yet fully implemented.\
**Owner choice:** Adjacent corridor sections may physically touch without automatically connecting, because **rooms and corridors have walls**. Unapproved openings are not inferred from tile adjacency.

### 21.1 Player-facing rules

Two stretches of the same bent corridor may share a side, and separate corridors or a corridor and room may share a physical boundary, provided footprints do not illegitimately occupy the same tile. Wall boundaries prevent crossing or seeing through these contacts; only canonical intended consecutive corridor passages and legally opened doorways are passable. Side contact is not a junction, secret connection, additional route, or free shortcut. Self-overlap on the same occupied tile remains invalid under the present MVP. Rooms use solid perimeter walls outside openings. Ordinary wall segments can be derived from authorized authored room/corridor geometry and opening choices; no individual player-painted wall objects are approved.

### 21.2 Existing code and risks

Existing Combat C0 (`CombatC0Geometry.cs`) uses room-local traversable tiles and confirmed room boundaries, with no universal production shared-edge wall/opacity authority for arbitrary adjacent corridors. Existing general adjacency handling must not be copied into new bent-corridor movement without legal edge-passability checks. Specify one canonical representation or derivation for physically adjacent versus legally connected tile sides that both movement and sight consume. Visibility and movement tests must include a U-shaped corridor with touching sections, separated parallel hallways, corridor next to room, direct-doorway opening, blocked solid corner, stable deterministic travel and legacy save migration.

---

## 22. DL-GAME-020: MVP combat remains room-based; cross-room ranged attacks deferred

**Decision date:** November 19, 2026.\
**Status:** Owner-approved scope restriction, consistent with current Combat C0 implementation.\
**Owner choice:** Preserve combat within each room for MVP. Do **not** allow adventurers or monsters to initiate ranged engagements across a room/corridor boundary, even when an open doorway could eventually support visibility. Keep physical walls and legitimate doorways authoritative for traversability.

### 22.1 Gameplay and scope

Adventurers follow real corridors and enter rooms through legal doors. Once a room encounter is active, use the existing deterministic engagement, range, line-of-sight, HP, target selection and bypass rules. An open doorway allows legal movement but does not by itself authorize shooting into or out of a different room. Walls block illegal physical traversal; no floor-wide combat resolver, multi-room pursuit, new ranged classes or scouting system is approved. A doorway's eventual influence on route ingress/egress and room-local traversal remains legitimate gameplay, but it does not require cross-room targeting.

### 22.2 Reconciliation, tests and deferred work

`RunSimulationService.CombatC0.cs` enters and resolves rooms individually. `CombatC0Geometry.cs` evaluates local line of sight and legal targets; `IntraroomTraversal.cs` uses room-local ingress and egress. Spec 38's door/socket geometry must be extended for moved doors and bent paths while preserving the current combat boundary. Validate room entry/exit, wall blocking, range/LOS within room, no shooting from neighboring corridors or rooms, untouched historical execution/checkpoints, active-run geometry isolation and correct replay evidence. Cross-room ranged combat is **deferred**, not promised for a specific milestone.

---

## 23. DL-GAME-021: Suggest route insertion point when placing a room, with player override

**Decision date:** November 19, 2026.\
**Status:** Owner-approved room-construction interaction; selection and reroute implementation pending.\
**Owner choice:** **Option A.** After placing a room in a legal draft position, suggest where it belongs in the existing adventurer route, then let the player explicitly choose a different legal predecessor and successor before connection and commit.

### 23.1 Player-facing behavior

The suggested position may include inserting between two existing encounters. The route insertion suggestion is not an automatically committed decision and must not silently reorder encounters, move rooms, reset hallways or retire content. The player can review affected room order and corridor changes, select different legal connection endpoints, drag doorway positions, reshape suggested hallways and accept a valid draft. A disconnected/partially connected draft may remain in the editor, but cannot be committed on an active floor unless all route rules are met. The final encounter order derives from the **canonical spatial route graph**, not an independently stored sequence.

### 23.2 Specification reconciliation

The current `StructuralEditService` primarily appends a constructed room through a constrained predecessor-to-Completion rewrite. This does not implement general route insertion or deliberate selection between existing rooms. Phase 7A0 permits temporary invalid drafts and requires whole-dungeon atomic commits; the gameplay-first plan schedules free placement and player-controlled connections after the first Research slice. Review which old corridor is affected when an insertion is explicitly accepted, how the old edge's historical investment carries, what happens to its existing traps/loot, how branch knowledge fingerprints are invalidated, and how unchanged corridors and run-start snapshots are preserved. These are domain and technical design questions, **not** consent to automatically replace every connection.

### 23.3 Tests and remaining choices

Test suggested end insertion and middle insertion; override predecessor/successor to a different legal position; no suggestion within six-tile reach; multiple equally legal candidate positions and stable deterministic ordering; preview of any affected edge and content; valid commit and run encounter order; cannot commit disconnected active floor; no duplicate corridor refunds or content removal; Discard and save/reopen; mobile input clarity. The exact approved interaction for accepting suggested insertion and intentionally replacing a preexisting corridor remains a suitable next owner decision. **Resolved November 20, 2026 by DL-GAME-022:** the player explicitly accepts replacing the selected old connection with two editable draft connections; no unauthorized replacement.

---

## 24. DL-GAME-022: Explicit assisted insertion between existing rooms

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future gameplay direction; not implemented or incorporated into repository design locks.\
**Owner choice:** Option A, with explicit player acceptance of the connection replacement.

### 24.1 Player-facing rule

When the player chooses to insert a new room between existing connected Rooms A and B, the editor proposes retiring the existing A-to-B connection from the **draft** and adding editable A-to-New-Room and New-Room-to-B connections. It shows suggested doorway locations and legal hallway paths. The player can reposition the room, select other eligible doorway wall positions, reshape segments and bends, and explicitly accept the proposed replacement into the draft. The player may later change or undo the draft. This is not an automatic canonical edit, a forced room relocation or permission to reorder unrelated rooms.

### 24.2 Economics, content custody and validity

Preview the specific affected edge, construction changes, displaced traps/loot, eligible same-floor corridor investment reuse, original investment/refund consequences and net commit impact. The existing content is not silently deleted, moved, double-refunded or converted to inventory. The intermediate draft may be disconnected or invalid, but an active-floor Save Changes must validate the entire required route and funds and then commit all affected state atomically. Existing runs retain their immutable snapshot. Final economics evaluate canonical beginning against final draft, not each provisional gesture.

### 24.3 Acceptance evidence and open technical design

Cover direct-edge and physical-corridor replacements; previously trapped corridors; multiple legal doorway pairs; legal and unreachable placement; cancellation/discard and crash/reopen draft; identity preservation; unchanged unrelated edges; original investment reused at most once; deterministic final room visit order; one durable whole-dungeon commit or no mutation. Technical design still owns ordered-route storage, save representation, migrations and how stable old edge IDs are treated when physically replaced.

---

## 25. DL-GAME-023: Reset Hallways removes all player-editable floor connections

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future gameplay direction; not shipped.\
**Owner choice:** Option A, including direct room-to-room connections.

### 25.1 Player-facing rule

The explicit **Reset Hallways** command affects the *selected floor* within the non-authoritative whole-dungeon edit draft. After an informed confirmation and consequences preview, it removes **all player-editable route connections** on that floor: physical corridors, legal direct room-to-room connections and editable optional branch connections. Player-owned editable connections touching Entrance Hall or Completion Terminal are also in scope, but the protected fixed structures themselves are never removed. All rooms, room-local content, room positions and fixed structures remain. Other floors are unaffected.

Associated corridor traps, loot and their identities must be flagged for explicit legal disposition before commit; an existing optional branch's knowledge and stock must not silently disappear. No automatic permanent retirement, hidden refund, arbitrary content relocation or cross-floor asset transfer is authorized. The resulting active-floor draft can temporarily have no valid required route. Save Changes is blocked until it has been repaired; Discard restores the canonical design unchanged. Existing active runs are isolated from the draft.

### 25.2 Validation

Test a mix of physical/direct/fixed-structure editable edges, side-contacting hallways, optional dead ends, traps and stocked loot; selected versus other floors; reset followed by partial rebuilding and blocked commit; reset followed by fully legal rebuild; stale/failed persistence; Discard and interrupted/resumed draft; no duplicate spend, credit or ownership changes.

---

## 26. DL-GAME-024: Cost-aware suggested corridor routes

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future gameplay direction; suggestion policy not implemented.\
**Owner choice:** Option A, nonbinding cost-aware route suggestion.

### 26.1 Suggestion priorities

When proposing candidate legal routes between player-selected endpoints, first **preserve unrelated rooms, connections and contents**. Among permitted candidate routes, prefer the **lowest projected net mana cost** under the authoritative *final-draft* settlement and eligible historical corridor investment rules. When economic results are equivalent, prefer shorter physical traversal. Use stable, documented canonical ordering for exact ties, independently of dictionary iteration, UI gesture timing or platform.

A player may freely override a suggested legal path, even when the override is longer or costs more. The editor must show implications without spending mana until Save Changes. A configuration-owned bounded deterministic candidate search is acceptable; this approval does not demand exhaustive exponential global optimization, a particular pathfinder, or a new pricing authority. A preview is not proof of final commit affordability if canonical state/resources later change.

### 26.2 Validation

Cover cost ties, expensive-short versus cheaper-long routes, reusing nearby paid investment, no disturbance of unrelated content, insufficient six-tile reach, stable suggestion after reopen, workload bounds and manual override, including the case that no legal candidate exists.

---

## 27. DL-GAME-025: Player-selected sequential route rebuilding after Reset Hallways

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future gameplay direction; not implemented.\
**Owner choice:** Guided manual rebuilding, with explicit room order from the player.

### 27.1 Player flow

After reset, the editor identifies disconnected rooms and offers available next connection choices. The **player selects the next relationship**, such as Room 1 to Room 2, and the editor previews a deterministic suggested legal pair of doorways and hallway. The player may drag doorways to other legal wall positions and change bends/segments before accepting *that connection into the draft*. Next the player chooses, for example, Room 2 to Room 3, and repeats. The player may revisit previous draft connections and can later add separately eligible optional dead-end branches.

The editor must **not select the actual encounter order, install multiple route connections, or rebuild an entire floor automatically**. UI room numbers/order labels are derived from the actual draft graph, not independent permanent identity. The fixed entrance and completion remain canonical endpoints of a legal active-floor required route; no generalized MVP loops, mazes or alternate completion routes are authorized. Invalid intermediate drafts remain editable but uncommittable.

### 27.2 Validation

Test a three-room reorder and complete entrance-to-completion rebuild, an attempted disconnected or overlength connection, multiple candidates with manual override, exact desired encounter order in subsequent runs, room identity preservation, draft interruption/reopen, atomic Save Changes and discard, and touch-friendly selecting/editing on mobile screen sizes.

---

## 28. DL-GAME-026: Automatic exact-once corridor investment allocation

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future gameplay and economy direction; historical investment ledger not yet qualified.\
**Owner choice:** Option A, automatic allocation rather than manual credit assignment.

### 28.1 Rule

The player edits dungeon geometry, not accounting entries. When a whole-dungeon draft is committed, the economic authority reconciles verified *original paid, eligible* corridor structural investments to the final approved corridor structure and automatically allocates legitimate reusable investment. The allocation maximizes legitimate reuse within each allowed source floor, subject to approved eligibility and the final net economic result. Structural investment amounts are neither unlimited coupons nor a second writable mana wallet. They are bounded by individually proven original payments and may be consumed/assigned as legal credit **once** per final transaction. Creating extra bend segments or splitting one corridor into multiple technical route records cannot mint extra credit.

Recycling, refunds and reuse must be mutually exclusive for the same paid investment portion. The preview separately explains gross new spending, reused amounts, separately authorized monetary refunds and final net mana impact. Ties use canonical ordering; a valid final draft cannot gain or lose mana based on the order of interim edit gestures.

### 28.2 Technical gates and evidence

Before coding, reconcile existing corridor investment records, supported pre-release save policy, split/merge cases, mixed eligible/ineligible historical spending, partial investment reuse, deleted/restored edges, multi-floor transactions, stale drafts, wallet/reserve floor, duplicate transaction retries and replay. Do not invent a new asset inventory or silently assign provenance that old saves cannot prove. Test fully reused, partially reused and newly paid paths without double credit, plus a repeated same-final-state edit that costs zero.

---

## 29. DL-GAME-027: Corridor investment reuse is confined to the originating floor

**Decision date:** November 20, 2026.\
**Status:** Owner-approved future economic boundary; not implemented.\
**Owner choice:** Option A, floor-local reuse of historical structural investment.

### 29.1 Scope

A corridor's eligible, paid historical **structural investment** can support legal replacement corridor construction anywhere on the **same original floor**, including disjoint footprints. It cannot be transferred directly to structural work on a different floor or represented as a transferable global inventory. This narrows DL-GAME-004's otherwise unspecified reuse scope without rescinding its same-floor portability guarantee.

The single canonical mana wallet remains dungeon-wide. A **separately authorized monetary refund** from one floor can affect legal net spending on another floor within the same atomic whole-dungeon edit, subject to global protected reserve and other rules. Such a refund cannot also count as structurally reused investment. The exact refund eligibility must be proven; no general corridor-refund rate is invented here.

### 29.2 Validation

Test Floor 1 investment fully reused on Floor 1; a proposed Floor 1 to Floor 2 structural reuse rejected; a separately authorized monetary refund from Floor 1 used toward legitimate Floor 2 construction without double counting; missing or unprovable provenance; floor activation differences; partial funding, save/reopen and failed atomic commit rollback.

---

## 30. Cross-decision rules and design tensions

### 30.1 The central strategic interaction

**DL-GAME-001 through 003 and 012 through 025** define physical construction, corridor defense, wall semantics, and room-only combat, while **004 through 011** govern economically safe redesign and content retirement:

- The player chooses room positions and hallway connections, using nonbinding suggested doorway locations, paths, and route-insertion points with deliberate manual overrides.
- Room perimeters and corridor sides are bounded by authoritative walls; touching side-by-side tiles are not automatically connected.
- Corridor paths may have any number of legal bends within an initial six-tile length maximum; each bent path remains one logical corridor.
- The resulting graph establishes the actual room encounter order. For reset reconstruction, the player explicitly chooses each next pair of rooms; route suggestions never choose encounter order.
- Hallway geometry consumes real floor space and resources and affects travel time.
- Trapcraft can make sufficiently long, legally placed corridors more defensible, with the traps themselves separately purchased and operated.
- Adventurers experience the actual resulting geometry and defenses; the player inspects outcomes and revises the plan.
- Corridor investment can fund legal redesigned routes rather than becoming a repeated sunk cost, while legitimate new structures still cost mana. The reusable paid structural portion is allocated automatically, exact-once, and only within its originating floor.
- A combined atomic redesign can use eligible same-transaction credits to pay for new construction and acquisitions.
- Monsters, traps and loot nodes are directly moved or retired for verified eligible original-base refunds, not placed in a growing permanent Owned inventory.
- A permanently retired monster refunds the full eligible original base payment and initially 80% of verified eligible paid individual progression, regardless of current condition. Moves preserve the monster's identity, HP and progress.
- A surviving recoverable trap apparatus receives its full eligible base refund even if unready; a fully consumed single-use device does not. Paid reset/recharge remains a separate operational expense.
- A loot node moves with its stocked rewards and provenance, but retiring it forfeits still-uncollected stock without a stock-cost refund, with clear disclosure.
- Corridor trap capacity scales by two-tile length bands but requires global sequential Trapcraft tiers for additional slots. New corridor lengths are available through ordinary approved construction catalog progression, not a second Architecture research gate.

This should create tradeoffs, not a universal formula that the longest possible hallway is always optimal.

### 30.2 Existing protected invariants

Do not relax these in implementation without explicit owner approval and specification reconciliation:

- Deterministic simulation and event ordering; stable room, edge, trap, content and research IDs.
- One canonical writable authority for spatial layout, corridor contents, research state, wallet, and investment, with derived views rather than duplicated persisted counters.
- Atomic validated canonical changes; edit drafts are durable but non-authoritative and may be temporarily invalid.
- Explicit compatibility/migrations for saved layouts and draft format revisions; fail-closed recovery that preserves original evidence.
- Configuration-owned costs, capacity thresholds, duration factors, damage, unlocks, and workload limits.
- Stable localized player-facing reasons, mobile-safe interaction and performance.
- One required route and current MVP optional-branch scope; valid same-floor connectivity and completion terminals. Side contact is not a new graph connection, and an explicit legal doorway is the only passage through a room perimeter.
- Cross-room attacks are outside MVP combat. Preserve existing room-local engagement authority, valid line-of-sight blocking, and run-start snapshots.
- Current run inputs are immutable after start; later edits or research affect only eligible future runs.
- No silent loss of paid traps/loot or orphaning of content when resetting, moving, or reconnecting corridors.
- One exact original-paid-base acquisition refund per eligible retired content instance, with a safe migration/handling path for legacy returned-content assets.
- Actual wallet capacity and globally protected reserves govern final atomic economic settlement; do not credit hypothetical refunds twice or bypass purchase restrictions.
- Actual observed player understanding and fun must be assessed separately from green automated tests.

### 30.3 Items for a later focused technical design review

These **are not preapproved engineering solutions**:

1. Define the distinction among a continuous hallway, physical corridor edge, individual bend/segment, authored corridor definition and trap-capacity calculation, including canonical ordered traversal and side-contacting wall boundary representation.
2. Update the currently straight 1–4-tile corridor content to the approved six-tile, bend-capable initial design, accounting for authored movable doorway eligibility, route endpoints, side walls and available floor bounds/capacity. Avoid route-splitting exploits.
3. Define explicit hallway reset/reconnection behavior and disposition of existing corridor traps/loot, including ownership, stable identity and refund/investment accounting.
4. Extend required-route corridor content/encounter authority only after confirming the exact impact on current path, branch, run and offline systems.
5. Author the **approved** sequential global Trapcraft tiers and two-tile capacity bands through a single Research/corridor capacity authority; decide only still-open project IDs, prerequisites, costs, durations and the maximum initially shipped rank. Review two Research slots and active rule-version compatibility.
6. Establish deterministic workload caps, canonical traversal order and event/time semantics for many corridor traps on several floors; profile the actual supported mobile scale.
7. Decide whether existing schema and draft journal can express changes safely; add versioned schema/migration only if concrete evidence requires it.
8. Reconcile paid corridor investment transfer and refund-backed settlement with legacy historical ledgers and existing gross-affordability behavior; test split/merge, capped refunds and no repeated resale.
9. Design per-instance content acquisition-payment provenance and future custody migration, with approved classification for expended traps and retained loot stock. Do not silently convert unresolved legacy items to zero-value or free mana.
10. Implement the **approved** 80% eligible individual monster progression retirement refund and condition-independent treatment with historical paid provenance, no refund for upkeep/recovery, and exact-once cross-floor settlement.
11. Integrate recoverable-apparatus versus consumed-trap retirement and loot-node stock forfeiture with separately paid restoration/stock systems; require explicit Ward-link handling and high-impact review.
12. Expand approved corridor geometry via construction content authoring rather than an extra Architecture research gate, but do not treat every future length or tier as automatically MVP-enabled.
13. Define safe nonbinding initial connections and room route-insertion suggestions, with manual override and clear preview of the exact replaced edge, stable identity, displacement and cost implications.
14. Prove room/corridor boundary-wall semantics, legal door openings and nonconsecutive side-contact block movement and line of sight while **preserving room-based MVP combat**.
15. For DL-GAME-022 through 027, distinguish one-connection-at-a-time draft authoring, deterministic bounded cost-aware suggestions, same-floor automatic investment allocation and global separately authorized monetary refunds. Test side effects of Reset Hallways and both direct/physical connection replacements.
16. Run playable comparisons against simpler, shorter dungeons. If new layout flexibility rarely changes player decisions or outcomes, correct gameplay clarity/balance rather than adding more infrastructure.

## 31. Implementation timing and PR planning guidance

**Sequencing verified against November 19, 2026 main and merged PR #270:** First complete the initial playable Phase 8 Research vertical slice and its necessary timing/save compatibility prerequisite. Then implement player-selectable legal room placement and player-controlled corridor/route construction, including the approved selectable wall openings, initial suggested paths, safe middle-route insertion, bent six-tile corridors and wall-separated side contact. Preserve the existing room-only combat scope. Only after that foundation should broader Phase 8 floor, Monsterology, Blueprint/loot and Trapcraft content be sequenced according to actual dependencies. The new Trapcraft capacity permission comes after baseline corridor authoring and actual trap encounter behavior are reliable, unless a smaller dependency-correct PR is justified. Detailed order is governed by `docs/planning/decisions/mvp-execution-sequencing-2026-11-19.md` and the current `docs/planning/post-gd60-mvp-execution-plan.md`, **not** this design log.

**Live dependency note at this revision:** PR #271 (`Phase 8 R1: Wire mana-backed starter Research`) and PR #272 (`Phase 8: Define versioned Research timing and H1 compatibility contract`) are both drafts. PR #272 documents owner-approved immutable per-project terms, 10 usable-mana initial price, 120 eligible-second initial duration, and the existing 10-second global tick with partial-first-tick accounting. Those already approved Research matters should not be reopened as gameplay choices. Independent PR review/implementation gates remain. Their open status is not implementation proof. Do not change H1 time-provenance or saved research rules in order to implement corridor capacity.

Potential bounded work, **not locked PR count or mandate**:

- Reconcile authoring geometry, graph and draft command design against latest `main` and the new player-facing requirements; update owned specs and tests only as required.
- Deliver a testable functional room-positioning and hallway route-authoring slice, with suggested insertion/connection, movable legal doorways, bent six-tile paths, authoritative walls, safe draft reset and valid commit, before broad floor/content expansion. Do not bundle cross-room combat.
- After functional room positioning and player-authored routes, implement eligible required-route corridor trap handling and qualified longer-corridor geometry, then the global tier-derived capacity modifier and associated gameplay/UAT coverage.
- In later, separately scoped PRs, implement the approved corridor investment carry and net-affordability settlement with exact ledger/recovery tests; these should precede a wide production hallway-editing UAT that depends on their economics.
- Transition existing content retirement/custody to direct Move/Replace/Remove and original-paid-base refunding only with a reviewed old-save strategy, paid-cost provenance and complete-save atomicity. Apply the 80% individual monster progression fraction, condition independence, recoverable trap apparatus rule and forfeited node stock as their respective domain systems become available. Do not bundle an entire Phase 8 roster or loot economy rollout simply to remove the Owned tab.

Codex implements only after a separate scoped implementation prompt and independent technical review; this log is not authorization for repository edits. Do not bundle the entire design into one large PR merely because the decisions are collected here. Do not restart paused H2 optimization or pull deferred maze topology into the MVP.

## 32. Decision update template

Copy this section for future owner decisions and append a new row to the decision register.

```markdown
## DL-GAME-00X: [Short decision title]

**Decision date:** [Month day, year]
**Status:** [Owner-approved direction / Provisional / Deferred]
**Owner choice:** [What was explicitly decided]

### Player-facing behavior
[What the player can do or observe; examples and edge cases]

### Why this choice
[Player fantasy, meaningful tradeoffs, comparable-game lessons, alternatives rejected]

### Interaction with existing decisions
[Related DL-GAME IDs and authoritative repository specs, with specific clauses]

### Implementation constraints and likely affected systems
[Current implementation gap; owners; deterministic behavior; configs; content IDs; saves/migrations; localization; performance]

### Tests and manual UAT
[Concrete acceptance, negative/boundary, save/reopen, mobile and player-comprehension scenarios]

### Deferred values / remaining approval gates
[Numbers, asset IDs, formulas and other details that were NOT approved]
```

## 33. Update history

| Version | Date | Changes |
| --- | --- | --- |
| 0.1 | November 19, 2026 | Initial log: DL-GAME-001 room placement and hallway rerouting, DL-GAME-002 real physical hallway length, DL-GAME-003 length-qualified research-unlocked corridor trap capacity. Includes rationale, repository reconciliation, explicit non-decisions, implementation risks and QA gates. |
| 0.2 | November 19, 2026 | Added DL-GAME-004 full paid hallway investment transfer, DL-GAME-005 same-transaction refund-backed final affordability, DL-GAME-006 direct content Move/Replace/Remove without permanent New/Owned inventory, and DL-GAME-007 100% eligible historical base acquisition refunds. Reconciled Phase 7, Phase 8, monster continuity, trap/loot lifecycle, old saves, transaction economics and QA. Identified next unresolved owner decisions on monster progression investment and injured/defeated retirement. |
| 0.3 | November 19, 2026 | Added DL-GAME-008 through 014: 80% paid individual monster progression return; no injury or defeat adjustment; intact/unready apparatus refund versus consumed single-use exception; preserve stock on node moves, forfeit uncollected stock on retirement; one potential trap per two-tile corridor band; sequential globally unlocked Trapcraft tiers; and no separate Architecture research requirement for longer corridor construction. Reconciled the earlier illustrative thresholds in DL-GAME-003, existing 1-4 tile geometry, optional-branch workload and approved gameplay-first PR sequence. |
| 0.4 | November 19, 2026 | Added DL-GAME-015 through 021: suggested and player-draggable legal doorway openings; deterministic suggested connecting paths with manual reshaping; unrestricted legal bend count within a single corridor; initial six-tile corridor length; side contact protected by authoritative walls; room-only MVP combat with cross-room ranged combat deferred; and suggested route insertion with player override. Reconciled existing fixed sockets, straight-only validation, tile-set versus traversal-order authority, wall and line-of-sight semantics, Phase 7A0 draft rules, Spec 38, and the Research-first construction sequence. |
| 0.5 | November 20, 2026 | Added DL-GAME-022 through 027: explicitly accepted two-connection insertion; reset of all editable selected-floor connections; cost-aware nonbinding path suggestions; player-selected sequential route rebuilding; automatic exact-once corridor structural investment allocation; and investment reuse limited to its originating floor, while authorized monetary refunds remain globally spendable. Verified PR #272 merged as native schema 18 Research timing prerequisite and PR #271 remains draft pending implementation/qualification. Documented that current repository specs and runtime remain the implementation baseline until reviewed amendments. |
| 0.6 | November 21, 2026 | Reconciled all 27 decisions against merged PRs #271 and #272. Retained the full v0.5 decision history alongside the current matrix, proposed amendments and construction sequence. |

## 34. Primary repository references

- [Gameplay-first dated execution sequence](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/decisions/mvp-execution-sequencing-2026-11-19.md)
- [Active post-GD60 execution plan](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/post-gd60-mvp-execution-plan.md)
- [Research R1 draft PR #271](https://github.com/gdg3417/Dungeon-Lord/pull/271)
- [Merged native schema 18 and versioned Research timing PR #272](https://github.com/gdg3417/Dungeon-Lord/pull/272)
- [AGENTS.md](https://github.com/gdg3417/Dungeon-Lord/blob/main/AGENTS.md)
- [MVP gameplay-first sequencing decision, November 19, 2026](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/decisions/mvp-execution-sequencing-2026-11-19.md)
- [Phase 7A0 graphical editor design lock](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md)
- [System Spec 38: Dungeon Floor Spatial Capacity and Route Graph](https://github.com/gdg3417/Dungeon-Lord/blob/main/Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md)
- [Phase 5 branching and route-choice design lock](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/phase-5-branching-and-route-choice-design.md)
- [Phase 5B production tuning and run-condition contract](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/phase-5b-production-tuning-and-run-condition-contract.md)
- [Living Dungeon owner design lock](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/decisions/living-dungeon-owner-design-lock.md)
- [Phase 8 owner design lock](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/decisions/phase-8-owner-design-lock.md)
- [Phase 8 dependency roadmap](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/phase-8-dependency-roadmap.md)
- [Combat and reserve-backed recovery owner design lock](https://github.com/gdg3417/Dungeon-Lord/blob/main/docs/planning/decisions/combat-and-recovery-owner-design-lock.md)
- [System Spec 21: Economy sinks and late-game deflation](https://github.com/gdg3417/Dungeon-Lord/blob/main/Docs/21%20-%20Economy_Sinks_and_Late_Game_Deflation_Control.md)
- [Structural economy and paid corridor transfer source](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Economy/StructuralEconomyService.cs)
- [Per-instance monster continuity source](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs)
- [Production room content UI with legacy New/Owned](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/UI/ProductionDungeonRoomContent.cs)
- [Authored structural content removal policy](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Data/Production/Save/structural_content_removal_policy.json)
- [Existing room door connection points](https://github.com/gdg3417/Dungeon-Lord/blob/main/ContentAuthoring/DungeonSpatial/tables/room_connection_points.csv)
- [Current straight-only room construction and corridor pair resolution](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralEditService.cs)
- [Current corridor footprint and tile canonicalization](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/DungeonSpatial/TileGeometry.cs)
- [Corridor run projection and existing Manhattan ordering](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseFiveBRouteProjection.cs)
- [Existing room-local combat geometry and line of sight](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Geometry.cs)
- [Existing room-only combat resolver](https://github.com/gdg3417/Dungeon-Lord/blob/main/Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.CombatC0.cs)

**Comparable-game heuristics (non-authoritative):** Dungeon Warfare / Dungeon Warfare 2 for the importance of defenses along real enemy travel routes; Dungeon Maker: Dark Lord for room/trap synergies; Kingdom Rush for route-position-driven defensive opportunities. These games are inspiration, not Dungeon Lord gameplay specifications or evidence that a mechanic has been validated with players.

[devrules]: ../../development/codex-development-rules.md
[a0]: ../phase-7a0-graphical-dungeon-editor-design-lock.md
[p5]: ../phase-5-branching-and-route-choice-design.md
[p5b]: ../phase-5b-production-tuning-and-run-condition-contract.md
[p8]: phase-8-owner-design-lock.md
[living]: living-dungeon-owner-design-lock.md
[recovery]: combat-and-recovery-owner-design-lock.md
[invariants]: ../../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md
[s01]: ../../../Docs/01%20-%20Mana%20System%20v3.md
[s03]: ../../../Docs/03%20-%20Moster%20Stats%20and%20Upkeep%20v3.md
[s09]: ../../../Docs/09%20-%20Dungeon_Progression_and_Research.md
[s17]: ../../../Docs/17%20-%20Save_State_Offline_Simulation_and_Time_Handling.md
[s20]: ../../../Docs/20%20-%20Tutorial_Onboarding_and_Unlock_Sequencing.md
[s21]: ../../../Docs/21%20-%20Economy_Sinks_and_Late_Game_Deflation_Control.md
[s23]: ../../../Docs/23%20-%20Player_Inventory_Storage_and_Item_Lifecycle.md
[s28]: ../../../Docs/28%20-%20Save_Data_Model_Versioning_and_Migration.md
[s29]: ../../../Docs/29%20-%20Time_Model_and_Tick_Resolution.md
[s38]: ../../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md
[arch]: ../../../Docs/Dungeon_Builder_Architecture_Spec_v1.md
[build]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralEditService.cs
[move]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralRenovationService.cs
[graph]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorRouteGraph.cs
[validator]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLayoutValidation.cs
[gd65b]: ../gd65b-production-spatial-content-approval.md
[tiles]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/TileGeometry.cs
[paths]: ../../../Assets/_Project/Scripts/Gameplay/RunSimulation/RunConnectionSnapshot.cs
[branch]: ../../../Assets/_Project/Scripts/Gameplay/RunSimulation/PhaseFiveBRouteProjection.cs
[branch-edit]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/OptionalBranchStructuralEditService.cs
[corr-content]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/CorridorContentMutationService.cs
[combat]: ../../../Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Geometry.cs
[economy]: ../../../Assets/_Project/Scripts/Economy/StructuralEconomyService.cs
[investment]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/StructuralInvestment.cs
[monster]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs
[acquisition]: ../../../Assets/_Project/Scripts/Economy/ContentAcquisitionEconomyConfiguration.cs
[removal]: ../../../Assets/_Project/Data/Production/Save/structural_content_removal_policy.json
[room-ui]: ../../../Assets/_Project/Scripts/UI/ProductionDungeonRoomContent.cs
[draft]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/TransactionalDungeonDraft.cs
[commit]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/TransactionalDungeonDraftCommit.cs
[content-draft]: ../../../Assets/_Project/Scripts/Gameplay/DungeonSpatial/TransactionalDungeonDraftContent.cs
[save]: ../../../Assets/_Project/Scripts/Services/SaveService.cs
[corridors]: ../../../ContentAuthoring/DungeonSpatial/tables/corridors.csv
[sockets]: ../../../ContentAuthoring/DungeonSpatial/tables/room_connection_points.csv
[research-action]: ../../../Assets/_Project/Scripts/Services/PlayerResearchActionHandler.cs
[research-select]: ../../../Assets/_Project/Resources/research_starter_selection.json
[research-rule]: ../../../Assets/_Project/Resources/ResearchTiming/adventurer_analysis_v1.json
