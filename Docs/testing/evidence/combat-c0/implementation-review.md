# Combat C0 review status

The owner-approved PR #238 continuation supersedes the pre-approval gaps in the historical record below. Production manual C0, real geometry, encounter-level identities, immutable optional configuration and atomic failure integration are now implemented. Current review and qualification are under `integration/`; the earlier reports remain prerequisite evidence only. No C1/C2 work is included.

## Historical prerequisite review at c0dba584

# C0 prerequisite review and acceptance gaps

This branch is a draft prerequisite, **not completed Combat C0 and not merge-ready**. Production/manual callers remain B1. The [owner review packet](../../../planning/combat-c0-implementation-and-tuning-proposal.md) records every proposed value and technical decision; no production approval is inferred.

## Implemented boundary

Three new internal runtime files provide immutable injected configuration, run-local monster HP, reciprocal deterministic round resolution, and chronological evidence. They do not select a production run executable, implement geometry, change outcomes, or publish transactions. One new test fixture and its established EditMode inheritance wrapper qualify the kernel. A preserving build entry point and a small wrapper over the existing exact-input/audit helper qualify the same source in an isolated project.

All existing runtime files are unchanged, including B1 execution/resolvers, `RunExecution`, event enumeration, start capture, checkpoint codec/folding/restoration, snapshot, models, transient retention, SaveService, canonical writer, GameRoot, B2 scheduling and production config. Therefore the new kernel cannot run under B1 or be encoded by B1 accidentally. Its internal checkpoint method rejects every call. This is isolation of prerequisites, not completion of production C0 execution selection.

## Safety and determinism review

- Five required class profiles and the two supported monster profiles have exact unique IDs. Required policy identities, nonempty attack IDs, HP/damage/initiative domains, complete class priorities and positive bounds are checked. There are no production defaults, fallback values, authoring imports or configuration fields added to the frozen B1 capture.
- Profile/priority/placement values are immutable and input arrays are defensively owned. Monster identity uses canonical AssignmentId, retaining Int64 Sequence ordering. Party health uses the existing roster and damage method. No heal or parallel casualty calculation is introduced.
- Initiative descends, then proposed party-first ties, numeric member/assignment sequence and ordinal stable IDs. Canonical target order is precomputed. Collections and directed eligibility evidence are sorted; dictionary enumeration never selects gameplay order. All damage is integer and HP-clamped. No RNG, frame time, presentation speed or selected floor is read.
- The complete directed legality matrix must have exactly one opposing pair, consistent participant positions and explicit known eligibility/denial reasons. Missing, duplicate, same-side, unknown identity/room/floor/reason or contradictory position fails before effects. This input validation is not proof of actual geometry.
- One action slot per participant per round; dead slots cannot attack. Targets are reselected from living legal participants. Stop immediately on supported injected terminal conditions; a completed receipt cannot attack again. Repeated identical room inputs return the same immutable receipt; contradictory repeats reject.
- Work/event/encounter boundaries are positive injected values. An exhausted stage publishes no monster/member HP, receipt, evidence or consumed counters. All successful damage publishes through the existing party HP method after the encounter's entire stage validates. This synchronous encounter staging is a kernel prerequisite, not an incremental run-event integration contract.
- No loot, Heat, mana, save, research, clock, file or canonical transaction API is available to the kernel. C0 events have no serialization contract, player-facing text or renderer. Existing #236 evidence and replay remain untouched.

## Required acceptance coverage still blocked

| Requested coverage | Current evidence / remaining gate |
| --- | --- |
| Reciprocal 1v1/group rounds, targets, initiative/ties, dead turns, monster HP, party wipe, retargeting | Injected kernel tests; production route integration pending. |
| Real melee positions, ranged range/LOS, solid walls, friendly transparency, geometry bypass | **Not implemented or qualified.** Tests only honor supplied denial/permission values. Requires approved positioning, LOS and opacity source. |
| Multiple encounters in one room / retained surviving monster HP | Current kernel uses one encounter per room; production lifecycle adapter remains pending. Run-local HP is never recovered. |
| Repeated intersections, empty/zero-damage/terminal, exact/over budgets, collection reordering, invalid inputs | Injected kernel coverage; actual path intersection integration pending. |
| Two-floor HP carryover, traps and loot | Injected party reuse with existing trap resolver is tested. Actual C0 floor transfer and loot/effect integration remains blocked; unchanged historical tests run separately. |
| Config cloning / mutation after start | Injected immutable config and nested collection ownership tested. Production config capture/selection blocked. |
| C0 event chronology and C1 replay support | Encounter-only chronology tested. Full route chronology, public inspector/retention and renderer adaptation pending C0 integration/C1. |
| Historical B1 outcomes / byte/evidence checkpoint identity | Frozen A and B1 suites re-executed; no historical code or fixture regeneration. Extra C0 isolation/checkpoint rejection case. |
| B2 dormant/injected compatibility; manual history/allocator/transaction rollback; retention/reopen/replay | Existing regression/full suites re-executed; no C0 transaction path exists yet. |
| No new serialized runtime state | No existing models/save/config/codec changed; isolated kernel is internal/transient. Production C0 reopen qualification still blocked. |
| Max workload and complexity | Synthetic injected 64-participant/32-round case measured. No approved production envelope or mobile qualification. |
| Owner manual plan and standalone gameplay parity | **Not requested/prepared as ready.** Requires approved production integration, review and automated validation first. |

## Subsequent same-branch work

Resolve the packet's numerical/technical decisions. Integrate a distinct immutable manual C0 executable while preserving actual B1 and its encoding unchanged. Add the captured-geometry adapter, multi-step encounter lifecycle, staged/incremental route event integration, full chronological transient evidence retention, singular outcome/effect/settlement ownership and selection/export guards. Qualify real geometry, two-floor behavior, save failure/reopen, replay compatibility, and the approved workload envelope. Then prepare owner instructions with exact reviewed commit, scene/player/save namespace and placement/HP/LOS/event expectations. Do not merge or request gameplay UAT before those gates close.

C1 graphics and C2 persistent monster state/recovery/protected mana are explicitly deferred; this branch adds none of them.
