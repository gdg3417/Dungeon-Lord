**System Spec 37: QA Strategy and Test Harness**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Manual and later automated testing, debug tools, telemetry validation gates |
| Primary goal | Make auditing and verification repeatable across offline, saves, and events |
| Invariants referenced | INV-01, INV-02, INV-03, INV-08, INV-10 |

# 1. Purpose

Define the testing strategy and tooling needed to validate core invariants and prevent regressions. MVP relies on manual testing with a clear scenario checklist. Automated tests may be added later.

# 2. Required test scenarios

- Offline progression tests

- Multi device conflict tests

- Save rollback detection tests

- Season boundary rule tests

# 3. Manual first approach

- MVP uses manual testing as the primary approach.

- Partial automation is optional, but not required for MVP.

- Automated tests are expected later for integrity and time logic.

# 4. Debug tools

- In game debug menu supports: time jumps, heat forcing, mana injection, and event toggling.

- Debug tools are compiled out of production builds.

- A minimal diagnostics panel may exist in production for admin accounts after online verification.

# 5. Telemetry validation

- Maintain a checklist of required telemetry events per feature before release.

- For MVP, missing telemetry is a warning, not a release blocker.

- After MVP, missing telemetry blocks releases for balance critical and monetization critical features.

# 6. Telemetry hooks

- qa_checklist_completed (release_id, pass_rate)

- debug_menu_used (action_id)

# 7. MVP constraints

- Do not ship debug menus to production users.

- Keep the manual checklist short and focused on invariants.

# 8. Open questions

None.
