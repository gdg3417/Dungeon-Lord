# Owner comprehension and UAT

Automated checks do not establish owner comprehension, usability or visual approval. Do not treat this branch as merge-ready until external review and this gate pass.

Use the production Dungeon screen on a disposable Editor validation save with an existing required-route room tail. The existing authority cannot seed an empty active Floor 1; its legacy starter setup remains available and is not retired by this packet. Do not use the owner's real save for automated tests or cleanup.

## Comprehension gate

1. Enter the production Dungeon screen, then Edit Mode.
2. Find Rooms without opening Bootstrap. Select an authored room card.
3. Identify a diamond anchor on the complete visible floor grid.
4. Tap it. Explain which room is previewed, how it extends the required route, where the highlighted Completion Terminal moves, and the complete draft cost.

If any step is unclear, stop further UAT and correct the interaction. A screenshot of that specific state and the expected interaction would help the correction.

## After comprehension passes

1. Try an invalid anchor, then a valid one. Try the available orientations and terminal-side choices. Confirm Placement; verify the draft changes while committed mana does not.
2. Review Save Changes, confirm, and verify exactly one quoted deduction. Close/reopen and inspect the committed room, route and room-local contents.
3. Make another draft and Discard it. Verify geometry and mana remain unchanged.
4. Confirm a placement, close/reopen, and Resume Draft. Also keep an invalid attempt, reopen, then correct or explicitly cancel it; Save must remain blocked until correction.
5. Move a newly constructed room before Save and compare the final charge with direct placement at the same final geometry. Move an existing room and verify its contents retain their local arrangement.
6. With insufficient mana, attempt Save; confirm the draft remains available. Correct the placement or wait for sufficient mana and retry.
7. Check portrait/landscape, Small/Default/Large text, collapse/expand, floor navigation and viewport input. Repeat the same comprehension and core Save/Discard/reopen steps in the Windows Development player.

Focus Room was evaluated and deferred: inspect-only Normal room selection and camera focus require a coordinated selection/input lifetime change. This packet keeps the established A5 Edit selection and Focus Floor behavior. No successor packet or floor-lifecycle sequence is committed.
