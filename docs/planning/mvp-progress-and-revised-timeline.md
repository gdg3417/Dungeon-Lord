# MVP Progress and Revised Timeline After PR 52

_Date: 2026-05-30 (UTC)_

_Status basis: merged repository state through PR 52_

## Purpose

This planning snapshot updates the MVP implementation timeline to reflect the deterministic backend progress already merged into the repository. It does not amend locked design specs, expand MVP scope, or claim that the playable MVP vertical slice is complete.

## Original calendar assumptions

The original MVP implementation plan assumed approximately **5 hours of development per week** and scheduled the following milestone windows:

| Milestone | Original calendar window | Original planning label |
| --- | --- | --- |
| M4 | 2026-05-25 to 2026-07-19 | Run simulation and outcome logging |
| M5 | 2026-07-20 to 2026-08-23 | Loot tables, extraction, and economy hooks |
| M6 | 2026-08-24 to 2026-09-27 | Heat and reputations MVP tiers |
| M7 | 2026-09-28 to 2026-11-08 | Offline rules and research slot |
| M8 | 2026-11-09 to 2026-12-06 | Tutorial, telemetry, and performance guardrails |

These dates remain useful as the original planning baseline, but they no longer describe the actual completion state of the deterministic backend foundation.

## Actual merged milestone status through PR 52

| Milestone | Current status | Evidence-backed interpretation |
| --- | --- | --- |
| M4 | Complete for its documented development-overlay slice | Run simulation and outcome logging closed through PR 31. This was explicitly a development-overlay implementation slice, not production UI or a complete playable loop. |
| M5 | **Complete** | PR 47 closed M5 with the M5-I0 deterministic run-outcome foundation audit. The audit recommendation is GO for M5 closeout within the documented scope. |
| M6 | **In progress** | PRs 48 through 52 established demand-budget resolution and diagnostics, run-heat-delta resolution and diagnostics, and active-run application of resolved heat delta to the current MVP heat state. M6 is not closed until its remaining diagnostics, regression review, and closeout audit are complete. |
| M7 | Not started | Offline summary and research-pending work must wait until M6 closeout. |
| M8 | Not started | Tutorial, telemetry, and performance-guardrail work remains future work. |

### M6 completed increments

The following M6 increments are complete in the merged repository state:

| Increment | Status | Merged result |
| --- | --- | --- |
| M6-A0 | Complete | Adventurer Demand Budget resolver scaffold. |
| M6-A1 | Complete | Adventurer Demand Budget diagnostics. |
| M6-B0 | Complete | Run Heat Delta summary resolver scaffold. |
| M6-B1 | Complete | Run Heat Delta diagnostics. |
| M6-C0 | Complete | Resolved run heat delta applied to active MVP heat state. |

M6 progress is intentionally described as **architecture and diagnostics progress**. It must not be interpreted as completion of a production UI, offline heat processing, or a fully validated player-facing loop.

## Revised timeline interpretation

The repository is **ahead of the original calendar plan on deterministic backend systems**: M5 is closed and M6 implementation is already in progress even though the original calendar baseline placed M5 between 2026-07-20 and 2026-08-23 and M6 between 2026-08-24 and 2026-09-27.

The **playable MVP validation timeline remains dependent on vertical-slice UI and player-facing loop integration**. Backend resolvers, persistence-safe summaries, and development diagnostics reduce implementation risk, but they do not prove that the core experience is understandable, responsive, or fun in a 10-minute playable session.

## Revised near-term roadmap

Work should proceed in this order:

| Increment | Near-term objective | Boundary conditions |
| --- | --- | --- |
| M6-C1 | Finish heat diagnostics and closeout audit preparation. | Keep diagnostics localized and preserve read-only display behavior. Do not introduce offline heat processing or post-MVP heat states. |
| M6-D0 | Add any missing active-play-only heat regression coverage if needed. | Add coverage only where review identifies a gap. Preserve config ownership, determinism, save compatibility, and legacy-safe behavior. |
| M6-I0 | Perform the M6 closeout audit. | Confirm delivered M6 scope and explicit exclusions before starting M7. |
| M7-A0 | Add an offline summary and research-pending scaffold. | Begin only after M6 closeout. Keep this to the scaffold boundary; do not implement expanded offline systems or research completion. |

## Playable vertical-slice readiness risk

Current backend progress must **not** be interpreted as full MVP completion. The core 10-minute playable loop still needs a coherent player-facing path for:

- placing rooms;
- placing monsters;
- seeing simulated runs;
- spending mana;
- receiving offline progress;
- seeing heat reactions clearly; and
- receiving clear UI feedback throughout the loop.

Until those pieces are integrated and validated together, milestone progress records architecture readiness and diagnostic coverage rather than playable vertical-slice readiness.

## Guardrails and locked boundaries

The following repository guardrails remain required for every milestone:

- All gameplay tuning remains config-owned. Runtime and simulation code must consume injected config, loaded config, or test-scoped fake config rather than embedding tuning values.
- No player-facing English strings may be hardcoded. UI, debug, and player messages must use localization keys or table references so additional languages can be added without code changes.
- Locked specs remain locked unless an explicit amendment note is added. This planning snapshot is a status update, not a spec amendment.
- Save compatibility and legacy-safe behavior remain part of milestone acceptance, including additive persistence changes and safe handling of older records.
- MVP exclusions remain in force. This timeline does not add Hostile, Raid, raid warnings, diplomacy, reputation axes beyond existing MVP boundaries, offline heat processing, research completion, events, leaderboards, monetization, seasons, or production UI.

## Evidence index

### M5 closeout

- [M5-I0 deterministic run-outcome foundation closeout audit](../testing/evidence/m5/M5-I0-deterministic-run-outcome-foundation-closeout.md)
- [M5-A/M5-B loot foundation audit](../testing/evidence/m5/m5-a-m5-b-loot-foundation-audit-2026-05-25.md)
- [M5-E0 loot survival extraction foundation audit](../testing/evidence/m5/m5-e0-loot-survival-extraction-foundation-audit-2026-05-25.md)

### M6 progress through PR 52

- [M6-A0 Adventurer Demand Budget resolver scaffold](../testing/evidence/m6/M6-A0-adventurer-demand-budget-scaffold.md)
- [M6-A1 Adventurer Demand Budget diagnostics](../testing/evidence/m6/M6-A1-adventurer-demand-budget-diagnostics.md)
- [M6-B0 Run Heat Delta summary resolver scaffold](../testing/evidence/m6/M6-B0-run-heat-delta-summary.md)
- [M6-B1 Run Heat Delta diagnostics](../testing/evidence/m6/M6-B1-run-heat-delta-diagnostics.md)
- [M6-C0 apply resolved run heat delta](../testing/evidence/m6/M6-C0-apply-run-heat-delta.md)

## Acceptance criteria for this documentation update

- [x] The original approximately 5-hours-per-week assumption and M4 through M8 calendar windows are recorded.
- [x] Actual merged progress through PR 52 is recorded.
- [x] M5 is marked complete based on the M5-I0 GO closeout audit.
- [x] M6 is marked in progress with M6-A0, M6-A1, M6-B0, M6-B1, and M6-C0 marked complete.
- [x] Deterministic backend architecture and diagnostics progress is separated from playable vertical-slice readiness.
- [x] The revised near-term roadmap lists M6-C1, M6-D0, M6-I0, and M7-A0 in order.
- [x] The playable-loop risk note identifies the remaining 10-minute-loop integration needs.
- [x] Config ownership, localization, locked-spec, MVP-exclusion, save-compatibility, and legacy-safe guardrails are preserved.
- [x] Existing M5 and M6 evidence files are linked.
- [x] This update changes documentation only: no C# files, JSON tuning files, Unity scenes, gameplay tuning, or runtime systems are modified.
