# Combat C0 prerequisite and owner review packet

Status: **Proposed, unapproved; production integration blocked. Not merge-ready.**

Baseline `eb80df1e3fd09c18aedb062afc108eb941deb8a3`, merged #237. Branch `codex/combat-c0-deterministic-combat`. Remote main was verified by fetch; initial clean local branch was `codex/lock-combat-recovery-design`, local main still at #236. No intervening remote changes. Recommendation: GPT-6.1 Sol, High; Complex, well-specified gameplay implementation. Reason: deterministic execution/geometry/HP/settlement/checkpoint integration. User-directed fallback: GPT-6 Sol High, then GPT-5.6 Sol High; no automatic Astra escalation. The task runtime does not expose a trustworthy actual model/reasoning identifier; no substitution was requested by this agent and none can be independently verified here.

This packet implements only the explicitly authorized safe bounded prerequisites. The October 14 [owner lock](../../docs/planning/decisions/combat-and-recovery-owner-design-lock.md), section 8, leaves formulas, tuning, LOS, positions, encounter conditions, ties, encoding and work limits unapproved. It does not authorize their production activation. No owner Unity testing is requested at this stage.

## Existing ownership and evidence

| Authority | Owning implementation/source | C0 boundary |
| --- | --- | --- |
| Required-route execution and settlement calculations | `RunSimulationService.cs`, `.Execution.cs`, `.Branches.cs`, `.Floors.cs` | Keep one roster and existing loot/Heat/branch/floor/knowledge/outcome calculations. No new per-death loot or mana. |
| Room traversability, path, canonical transforms | `IntraroomSnapshot`, `DeterministicRoomPaths`, `IntraroomTraversal`, `RoomLocalCoordinateTransform`, `ActiveFloorRunSnapshot` | Derive from captured canonical room definition and assignments. Reachability does not establish LOS. Reserved tiles currently describe nontraversability, not an approved opacity map. |
| Adventurer health/casualties | `RunPartyMember.ApplyDamage`, `RunParty.DeriveSurvival` | Existing integer HP remains sole authority. No aggregate survivor formula. |
| B1 executable and checkpoint | `RunStartCapture`, `RunCheckpointCodec`, `RunExecution`, existing resolvers | Actual code, enum values, start encoding and evidence folding remain unchanged. |
| Manual publication | `CalculatePhaseFiveBRun`, `DetachedCanonicalWriteAuthority`, `SaveService`, `GameRoot` | Complete detached transaction, durable readback, shared allocator and history unchanged. |
| Transient evidence and replay | `Models.RunOutcomeRecord`, `RunTransientEvidence`, PR #236 `DungeonRunReplay` | Existing evidence/replay unchanged. C0 kernel evidence is isolated; not yet attached to outcomes. |
| Production config and localization | `run_simulation_config.json`, `BootstrapConfigValidationService`, existing string tables | No C0 field/default/flag added before approval. No new player-facing text. |
| Content authoring | GD65B source contract and normalized `ContentAuthoring/DungeonSpatial` | Owns geometry/catalog/localization; does not approve reciprocal combat values. |
| B2 | Profile A, `LivingDungeonScheduling`, schema-14 contracts | Remains dormant in production and explicitly B1 for injected scheduling. |

Reviewed sources include Specs 01/03/06/11/21/28/38, Monsterology, glossary, roadmap, Phase 5B contract and A/B1/B2 contracts. Live PR metadata for #233–#237 was inspected. #236 final acceptance is at `92cb8859`; its replay is sequential one-sided damage and records no monster HP/death. That limitation remains correct. Frozen Phase A fixtures and B1 restoration tests remain regression authority, not fixtures to regenerate.

## Inventory: approved active values versus authoring drafts

The [Phase 5B owner-approved contract](../../docs/planning/phase-5b-production-tuning-and-run-condition-contract.md), sections 8 and 13, and active bootstrap config approve adventurer level-one HP: Warrior 10, Cleric 8, Rogue 7, Ranger 7, Mage 6; initial party size 3–5 and level 1. Initial formation priority is Warrior 1, Rogue 2, Ranger 3, Cleric 4, Mage 5.

Existing one-shot, severity-interpolated damage is Goblin 1–2 and Skeleton 1–3, both `run.targeting.lead_active`. These are approved **B1 assignment interaction** values, not an approval to repeat them each C0 round. Traps remain Snare 1–2, Spike 2–4, Chilling Sigil 0–0 with existing expertise mitigation and targeting.

`Assets/_Project/Data/Production/Research/Dungeon_Builder_Research_Export_Bundle/monsterology/tables.json` contains authoring rows `stat_goblin_base` HP 50/damage 8 and `stat_skeleton_base` HP 55/damage 7, as well as header-like mixed-type rows. Neither that exported workbook nor draft Spec 03 establishes active C0 balance. No values are imported from it. No level scaling, upkeep, evolution, injury or recovery formula is activated.

## Exact proposed production decisions (all unapproved)

Proposal P1 is a small basic-attack profile. It uses integer fixed damage with no RNG, severity, armor or level multiplier. These are **unapproved proposals, not defaults, test expectations or existing authority**:

| Participant profile | HP owner / proposed monster HP | Proposed damage per basic action | Proposed initiative | Proposed type / maximum range |
| --- | --- | --- | --- | --- |
| Warrior | existing HP 10 | 2 | 30 | melee / 1 |
| Rogue | existing HP 7 | 3 | 50 | melee / 1 |
| Mage | existing HP 6 | 3 | 20 | ranged / 4 |
| Cleric | existing HP 8 | 1 | 10 | melee / 1 (no healing) |
| Ranger | existing HP 7 | 2 | 40 | ranged / 4 |
| Goblin | **8** | **1** | **35** | melee / 1 |
| Skeleton | **10** | **2** | **25** | melee / 1 |

The numbers are a conservative first balance hypothesis for reciprocal rounds at current party HP, not a qualified balance result. Both baseline monsters stay melee; ranged legality is tested with explicitly injected profiles, without introducing Archer species/evolutions.

Proposed ties: initiative descending, adventurer before monster on equal initiative, adventurer numeric MemberOrdinal, monster numeric canonical Sequence then ordinal AssignmentId. Proposed monster targeting: legal active Warrior, Rogue, Ranger, Cleric, Mage class order, then numeric MemberOrdinal. Adventurers choose legal living monsters by Sequence then AssignmentId. No HP preference, aggro, spells or healing. Policy IDs and exact semantics require approval.

Proposed encounter boundaries: freeze party movement at the earliest path step at which any defender can legally attack or be attacked; compute all eligible room defenders at that step into one encounter. A room may require more than one encounter as the party subsequently reaches formerly ineligible defenders. Track resolved defender identities so intersections never regenerate HP/actions. No pursuing beyond the current room. Traps/loot at that step retain existing monster-before-trap-before-loot ordering; wipe stops unreached interactions. Reassess only at path/encounter boundaries, never each frame. Retain existing room/branch/floor retreat and room-clear/reward qualification. Round-level retreat is deferred.

The current kernel implements **one supplied candidate encounter per room**, with idempotent receipts. It does not implement the proposed multi-step room lifecycle; that adapter must be reviewed before integration. It accepts a complete directed attack-legality matrix for tests only and computes no movement, range or LOS. This is not geometry qualification.

## Position and LOS choices needing explicit review

P2 proposes a party-anchor abstraction at the existing path tile for C0; member identities share that anchor, rather than invented individual paths. Melee defenders need distinct reachable four-neighbour engagement tiles adjacent to it; other defender positions reserve their chosen tile. Assign by Sequence/AssignmentId and deterministic BFS distance/coordinate ties, with no capacity beyond real geometry/config. Clarify whether sharing the party anchor is acceptable, whether defenders may move through each other, and whether party standing on a configured monster start permits an engagement position change. A missing tile means bypass, not a forced hazard.

P3 proposes Manhattan range and integer tile-centre supercover LOS; any touched solid tile blocks, including both tiles at a corner crossing. Friendly participants do not add opacity. Ranged defenders use configured starts and require no free adjacent tile. **Do not treat all nontraversable/reserved tiles as opaque without approval.** Choose either an approved derivation from existing room geometry (identifying exactly which cells are solid barriers) or a reviewed content-only opacity field sourced through the existing authoring package. No second layout is proposed. These choices are absent from the lock and remain gates.

## Config, evidence and workload contract proposal

Proposed production owner: one optional, explicitly enabled versioned `CombatC0` section of existing `RunSimulationConfig`, in existing bootstrap JSON; absent/disabled keeps B1. Enabled-but-incomplete fails before mutation, with no fallback. The typed shape needs executable/rule/version, damage policy, initiative/tie policy, targeting, all five class and both monster profiles, attack type/range, position/grouping/LOS/opacity policies, zero-progress and round-limit behavior, and technical bounds. No C0 config is serialized into an existing B1 start.

Proposed identities (all unapproved): configuration version `1`; production executable `run.executable.combat-c0.v1`; rule `run.combat.c0.basic.v1`; damage `combat.c0.fixed_integer.v1`; initiative `combat.c0.initiative_descending_party_first.v1`; targeting `combat.c0.targeting.class_priority.v1`; position `combat.c0.party_anchor.distinct_melee.v1`; grouping `combat.c0.earliest_path_step.room_group.v1`; range `combat.c0.manhattan.v1`; LOS `combat.c0.supercover.solid_corner.v1`; zero-progress `combat.c0.zero_damage_round.v1`; round-limit `combat.c0.technical_failure.v1`. These are review proposals only; the existing injected kernel deliberately uses qualification identities. New manual C0 remains untimed, with rounds/work as duration bounds; production logical attack durations and timed scheduling remain unapproved and B2 remains B1.

Proposed technical bounds for owner review: 32 rounds/encounter, 64 total run participants including party, 30 encounters/run, 32,768 combat events/run, 1,000,000 charged resolver work units/run, and existing spatial tile/parser limits without relaxation. These are proposed qualification limits, not gameplay density caps. For the kernel, work units count staging participants + directed links + action slots + target checks; they are not CPU instructions. Sorting/input validation also costs bounded work. A geometry adapter needs its own budget accounting before production.

Proposed zero-progress completion: stop after a full round changes no HP. Proposed round-limit completion: stop combat and fail the detached run technically, **not** manufacture retreat, success or rewards. The kernel exposes explicit completion reasons; it has no settlement callback. Whether production should reject the run at that boundary or use another approved stop condition needs approval.

Kernel events are immutable, chronologically ordered values: rules/run/encounter identity; global zero-based event ordinal; encounter action slot ordinal/round; stable actor/target; floor/room; basic attack/target policy; room-local source/target; actual before/after/damage/defeat; start, directed eligibility/denial, skipped defeated/no-target slots, completion. Eligibility reasons are injected evidence, not calculated claims. Encounter work is staged and committed to the existing adventurer HP authority only after all validation/budget checks. Monster HP is run-local and never heals. Failed work publishes no HP/events/receipt. No evidence journal/save field is added. C1 must observe the future integrated chronology; current kernel events alone do not supply a complete route replay.

Complexity: with P participants and L directed attack pairs, preparation is bounded by O(P×L + P² log P + L log L), rounds O(R×P²), storage O(P+L+E) including retained receipts. There is no per-frame work. No approved worst-case production envelope exists yet; synthetic measured tests qualify only injected bounds.

## Executable isolation and subsequent integration

The kernel has the distinct internal qualification identity `run.executable.combat-c0.qualification.v1`, with no production selector. Its checkpoint export always rejects `combat.c0.checkpoint_contract_unapproved`. It cannot be passed as a `RunExecution` or encoded by B1. Actual B1 implementation, start capture, enum, fold, restore and B2 scheduling are untouched.

After approval, smallest integration boundary: add a separately selected C0 room resolver at new manual start, preserve the existing B1 resolver and route control flow, keep immutable C0 profiles in a transient execution-owned object, and block checkpoint export before any B1 capture/encoding. C0 start capture must not claim B1; no C0 durable format is authorized. Route/evidence bounding and immutable capture need C0-specific qualification. Production selection, chronological route attachment, trap/loot/outcome integration, transaction/reopen evidence and owner manual plan remain blocked, not completed by kernel tests.

C1 graphical animation and C2 persistent injuries/reserve/recovery remain separate. Writable schema stays 14. No migration, mana grant/spend, auto arrival, ongoing-party activation, evolution, boss or recovery is added.

## Approval request and completion gate

Owner review must explicitly accept or amend P1 balance, initiative/target policy, P2 positions/grouping, P3 LOS/opacity/range, encounter/trap ordering, terminal behavior, config shape/IDs/event encoding and work limits. Approval of the high-level October 14 lock alone does not close these decisions. Keep the same PR branch for integration; do not merge this prerequisite state or request manual gameplay qualification. Automated reports and exact limitations are recorded separately under `Docs/testing/evidence/combat-c0/`.
