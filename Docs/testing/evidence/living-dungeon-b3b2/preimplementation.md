# B3B2 preimplementation inspection

Historical status at the preimplementation gate: decisions A/B were pending; this record did not claim production qualification or runtime changes. **Superseded by the owner's subsequent approval** of configuration-owned 30–90-second pacing and the additional movement/interaction/unit values, and explicit rejection of an occupied-floor edit restriction. The current [contract](../../../planning/living-dungeon-phase-b3b2-implementation-contract.md#active-run-editing-and-c2-current-state-reconciliation) records the accepted stable-identity net-damage policy. Preserve the inspection and unrun qualification table below as historical evidence; later implementation results are recorded separately.

## Repository evidence

- Inspection date: 2026-10-18 America/Chicago session (UTC date may be 2026-10-19).
- `git status --short --branch`: clean initial B3B1 branch, HEAD `f44d10f85a1812a045b3fcb28f018b5ace8fc263`.
- `git fetch origin`: initial sandbox attempt could not write `.git/FETCH_HEAD`; approved elevated retry succeeded. No reset/restore/clean performed.
- `git rev-parse main` and `git rev-parse origin/main`: both `5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d` after refresh.
- `git diff --stat origin/main HEAD`: empty. B3B1 commits are squash-merged; ancestry differs but content does not.
- `git log -8 --oneline origin/main`: #244, #243, #242, #241, prior two-floor UAT records, Combat C2 and #239.
- Connected GitHub merged-PR search confirms #244/#243/#242/#241/#240/#239. Open-PR search returned zero. Older PR bodies/documentation saying unmerged are historical.
- `git switch -c codex/living-dungeon-b3b2-autonomous-foreground-c2 origin/main`: succeeded. Branch baseline and current code HEAD are `5fbaa5f5d845c1341ee4cc4f52b706a8ba586d0d`.
- Additional instructions: root `AGENTS.md`; the only other found AGENTS file is under `dungeon-builder/`, outside the Unity implementation scope. No sub-agents used.

The shell has no `gh` executable on PATH; connected GitHub reads were used. Read-only search corrections (PowerShell brace expansion and wildcard paths) produced no source changes or test results.

## Source findings

| Authority | Inspected evidence and implication |
| --- | --- |
| `TimeService`, `SimulationClock`, `GameRoot`, `ActivePlaySaveScheduler` | Current frame delta is scaled, float remainder is transient, only one tick can be consumed per call, and GameRoot has no focus callback. Current pause/quit saves and 10-second ticks/30-second periodic saving are reusable integration points, not a sufficient new foreground proof. `SimulationClockTests` cover basic tick/pause/skew but do not prove partition/restart/focus behavior. |
| `SaveService`, `DetachedCanonicalWriteAuthority`, `DetachedCanonicalSaveSession` | Detached validation, complete predecessor equality, exact persistence/readback and publication already exist. Default production context registers no active Living Dungeon profile. Ordinary mutations wait for horizon closure; lifecycle saves preserve an unfinished owner. New production adapters must preserve those guarantees. |
| `LivingDungeonContracts`, `LivingDungeonScheduling` | Owner has accepted/closed horizons, pending/last opportunity time and ordinal, immutable profile and pinned checkpoint records. Existing scheduler orders existing events before opportunities, allocates from history once, and consumes throttled opportunities. Its capture adapter currently calls B1 `StartSnapshot`; production needs explicit C2 capture. |
| `LivingDungeonTerminalSettlement` | Entire file is Editor-only. It rejects C0/C2, uses synthetic provenance, and composes current-state Heat/cooling/compatible survivor learning/history/individual first-session objective. It provides consequence/history groundwork, not a production settlement caller. |
| `RunSimulationService.Execution`, `RunExecution`, `CombatStartCapture`, B3B1 tests | `StartTimedCombatSnapshot` captures actual C2 immutable inputs; `AdvanceThrough` gates whole boundaries and has canonical ordinals. Repeated reconstruction/due-time/work tests retain exact receipts and historical B1 behavior. Runtime publication must advance this prefix, not display a pre-completed manual result. |
| `MonsterContinuity`, manual C2 writer, `OccupiedFloors`, `PrepareAndPersist` | Manual C2 checks initial HP/revival before applying final condition. Recovery spends the one wallet in stable order on vacant active floors. Existing terminal records are excluded from occupancy; automatic production terminal settlement must protect them until injury publication and order recovery before later admission. |
| `RunHeatStateApplyResolver`, `LootHeatCoolingResolver`, `RunSimulationService.Branches` | Run Heat applies to current state; loot cooling is separate. Manual extraction/knowledge/history/objective authorities already exist. No new reward or mana grant is justified by scheduling alone. |
| Arrival/traffic resolvers and presenters | Existing configured score/band formulas are read-only forecasts (`WouldMutateState=false`), with traffic's estimated concurrency explicitly a forecast. No authoritative production cadence or demand-to-opportunity policy exists. Bootstrap run config owns existing coefficients, not new arrival intervals. |
| Production Dungeon controller/replay, `DungeonRunReplay`, world view and presenter | UI Toolkit and pooled world primitives already consume committed/transient evidence. Replay validates layout/corridor fingerprints and fails closed after incompatible edits or missing detail. Replay durations are presentation policy and cannot supply gameplay timing. |
| Localization/data | Existing English table owns Dungeon/replay/recovery/forecast text. B3B2 needs additional localized receiving/active/result/blocked-edit states. Profile A is explicitly qualification-only. Bootstrap `tickSeconds=10`, `activeSaveIntervalSeconds=30`; no production logical movement/interaction/arrival timing was found. |
| Save/migration tests | C2 tests cover frozen fourteen preservation, healthy continuity migration, malformed owner rejection, explicit schema budgets and isolated Windows reopen. B2/B3A/B3B1 suites cover allocator/pins, incomplete horizons, synthetic provenance rejection, rollback, exact-once history/settlement and format dispatch. They must be extended/regressed, not relabeled as proof of the new loop. |

## Retained evidence reviewed

- B2/B3A/B3B1 implementation contracts and prior qualification documents.
- `combat-c2/implementation-and-qualification.md`: approved schema/recovery semantics and preservation requirements; Combat C2 is now merged as #240.
- `living-dungeon-b3b1/workload-review.md`: reported 50 focused and 340 regression EditMode passes; representative one-floor C2 2,051 work; no production activation.
- #244 qualification/PR record: representative one-floor C2 checkpoint 23,625 bytes/13 events; full EditMode baseline starter-copy collision, corrected scoped qualification, relevant PlayMode and Windows technical smoke. These are historical results, not B3B2 passes.
- Phase 7 production composition and visual replay implementation records, and #242 responsive-editing closeout reflected by merged main. Decisions 57/63 permit active-run commits; #243 preserves Decisions 56–58.

Historical preservation exceptions (five #242 screenshot originals and one owner-accepted #244 auxiliary shader-cache loss) remain historical; no restoration or new waiver is claimed. No Unity/player process was launched for this gate, no save/build namespace was used, and no test/build artifacts were generated. Changes are limited to this record, the proposed B3B2 contract and a dated roadmap status entry until the gate is answered.

## Qualification status

| Stage | B3B2 result |
| --- | --- |
| Repository/main/merge/instruction inspection | Completed as recorded above |
| Runtime/config/localization implementation | Pending owner decisions A/B |
| Focused and regression EditMode | Not run |
| PlayMode | Not run |
| Windows Development build | Not run |
| Autonomous standalone smoke/reopen/second run | Not run |
| PR | Not created; no separate planning PR |

Do not count this document, earlier synthetic tests or prior builds as acceptance of the autonomous milestone.
