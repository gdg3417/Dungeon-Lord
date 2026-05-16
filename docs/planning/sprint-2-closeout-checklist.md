# Sprint 2 Closeout Checklist

Date: 2026-05-15
Scope: Sprint 2A and Sprint 2B closeout evidence for Unit, SIT, UAT, and build promotion gates.

## Completion rule
Sprint 2 is not closed until every applicable check is marked PASS with linked evidence.

## Pre-flight setup
1. Confirm Sprint 1 closeout evidence is complete in `Docs/Sprint1_Closeout_Checklist_2026-05-13.md`.
2. Sync branch and confirm clean working tree for test execution.
3. Confirm test harness and CI jobs for Unit and SIT are available.
4. Create timestamped evidence folder for Sprint 2 artifacts.
5. Confirm sprint scope split in execution report: Sprint 2A versus Sprint 2B.

## Unit test evidence section
- S2-UNIT-01 (Sprint 2A and Sprint 2B): Validate deterministic unit tests for all implemented Sprint 2 ticket contracts.
  - Pass criteria: all implemented Sprint 2 ticket unit suites pass with deterministic replay safety.
  - Evidence: test logs, failing-test resolution notes, and commit hash.

## SIT evidence section
- S2-SIT-01 (Sprint 2A): Validate layout -> encounter -> loot -> heat integration.
  - Pass criteria: integrated flow completes with deterministic outputs and no invalid state transition.
  - Evidence: SIT run log and flow artifact.
- S2-SIT-02 (Sprint 2B): Validate mana -> research -> verification queue state integration.
  - Pass criteria: queue transitions reflect expected lifecycle and blocking behavior.
  - Evidence: SIT transition logs and state snapshots.
- S2-SIT-03A (Sprint 2A): Validate save/load persistence for layout, mana, encounter event state, loot output, and heat state.
  - Pass criteria: roundtrip preserves required Sprint 2A fields and deterministic resume behavior.
  - Evidence: persistence logs, fixture outputs, schema comparison summary.
- S2-SIT-03B (Sprint 2B): Validate save/load persistence for research lifecycle, verification queue, inventory handoff, UI trust state, and offline orchestration.
  - Pass criteria: roundtrip preserves required Sprint 2B fields and deterministic resume behavior.
  - Evidence: persistence logs, fixture outputs, schema comparison summary.

## UAT evidence section
- S2-UAT-01 (Sprint 2A): Validate minimal dungeon layout and placement flow.
- S2-UAT-02 (Sprint 2A): Validate mana tick and reserve pressure visibility.
- S2-UAT-03 (Sprint 2A): Validate deterministic encounter run and event output.
- S2-UAT-04 (Sprint 2A): Validate loot and heat response from encounter output.
- S2-UAT-05 (Sprint 2B conditional): Validate research pending/verification messaging if Sprint 2B is included.
- S2-UAT-06 (Sprint 2B conditional): Validate offline return summary if Sprint 2B includes offline orchestration.

For each UAT check above:
- Pass criteria: expected player-facing behavior is observable and understandable, with no blocking defects.
- Evidence: screenshots or recordings, tester notes, and issue links for any non-blocking defects.

## Build promotion rule
- Build promotion to internal test is blocked unless S2-UNIT-01 and all applicable S2-SIT checks pass.
- Release candidate promotion is blocked unless all applicable Sprint 2 UAT checks pass.
- Conditional checks S2-UAT-05 and S2-UAT-06 are mandatory only when Sprint 2B scope is included in the build.

## Closeout packet template
- Branch and commit tested:
- Sprint scope included: Sprint 2A only or Sprint 2A plus Sprint 2B
- Execution date and time UTC:
- S2-UNIT-01 result:
- S2-SIT-01 result:
- S2-SIT-02 result:
- S2-SIT-03A result:
- S2-SIT-03B result:
- S2-UAT-01 result:
- S2-UAT-02 result:
- S2-UAT-03 result:
- S2-UAT-04 result:
- S2-UAT-05 result (if applicable):
- S2-UAT-06 result (if applicable):
- Artifact locations:
- Open issues and dispositions:
- Final signoff name and date:
