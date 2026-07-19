# GD59 Player Research Smoke and Validation Evidence

## Scope

GD59 makes the configured Adventurer Activity Analysis project completable from the normal MVP action panel. It reuses the single-slot pending, active-progress, completion-pending, claim, completed-research, restricted-action, and Basic Run Analysis unlock boundaries. It adds no offline research progression or remote verification claim.

## Editor smoke procedure

1. Start from **Clean MVP validation reset**.
2. Keep development diagnostics hidden.
3. Build the complete starter dungeon path.
4. Run or observe the dungeon until a valid adventurer run is recorded.
5. Confirm the authoritative `Next:` instruction says to start Adventurer Activity Analysis.
6. Use **Start Activity Analysis** in the normal action panel.
7. Confirm the panel shows `Research in progress: X / Y` and that active simulation ticks advance it.
8. Wait until the panel exposes only **Claim Activity Analysis** for research.
9. Claim it and confirm the completed state, Basic Run Analysis advice, and First Dungeon Contract completion once its other configured requirements are met.
10. Pause and resume, then close and reopen the Editor player. Confirm the dungeon, run history, mana, loot, heat, active/completed research, unlock, and contract remain coherent.
11. Run the clean reset again and confirm pending, progress, completed research, and contract completion are cleared.
12. Confirm the Console has no errors or missing references.

## Windows non-development release probe

1. Create a Windows non-development build using the established GD58 workflow.
2. Confirm F1-F6 and development diagnostics are inaccessible.
3. Repeat the full editor journey using only the normal action panel.
4. Confirm no project ID, slot ID, localization key, rule source ID, or diagnostic value is visible.
5. Close and reopen the build and confirm persistence remains coherent.

## Evidence status

- Automated EditMode coverage exercises prerequisite rejection, configured start, occupied-slot safety, repeated start, deterministic active ticks, idempotent completion-pending, offline and verification-pending claim blocks, successful exactly-once claim, persistence, reset, legacy state, localization output, unlock, contract, and the diagnostics-disabled canonical journey.
- Manual Editor smoke is **pending developer execution** because this environment does not provide interactive Unity Editor operation.
- Windows non-development build execution is **pending developer execution**; do not promote GD59 based on automated evidence alone.

## Non-goals and known limitations

- No research tree, additional project or slot, acceleration, research cost, offline progress, backend, or real verification server is introduced.
- The local MVP claim is permitted only by the existing restricted-action gate while online and with no verification already pending. It does not claim or imply remote server authority.
- Ordered two-room run resolution remains the next gameplay priority after GD59.
