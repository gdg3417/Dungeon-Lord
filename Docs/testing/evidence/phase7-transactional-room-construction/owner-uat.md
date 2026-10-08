# Owner comprehension and UAT

Automated checks do not establish owner comprehension, usability or visual approval. Do not treat this branch as merge-ready until external review and this gate pass.

Use the production Dungeon screen on a disposable Editor validation save with an existing required-route room tail. The existing authority cannot seed an empty active Floor 1; its legacy starter setup remains available and is not retired by this packet. Do not use the owner's real save for automated tests or cleanup.

## Exact isolated setup

1. In Unity Hub open **`C:/Dev/Dungeon-Lord/Temp/room-construction-validation`**, using Unity 6000.3.2f1. Open `Assets/_Project/Scenes/Bootstrap.unity` and enter Play. Do not use the main owner project for this UAT.
2. This copy and the preserved Windows player use `phase7-room-construction-uat-b681ebc8e4664312a1feb2a471ebd4f2.json` under the existing `LocalLow/gdg3417/Dungeon Lord` directory. Its drafts share this disposable prefix. Do not delete or change `save_primary.json` or its drafts.
3. On a fresh disposable save, use retained Bootstrap starter controls: select **Basic Room** in the retained Rooms group → **Place or modify selected placement**. This one-time prerequisite is outside the production-construction comprehension gate. If it does not create the established route tail, stop and report the observed state.
4. If a recovery prompt appears for the existing UAT draft, choose Resume Draft to retain the owner's prior work. For affordable Save testing, press F1 to open the existing development panel and use **QA Mana: Fill to Capacity**, then close the panel with F1. Use **QA Mana: Clear** later for insufficient-mana testing. These actions affect only the disposable save; do not reset/delete the save or use these controls on the owner's primary save.
5. For standalone testing, exit Editor Play first, then launch **`C:/Dev/Dungeon-Lord/Builds/Phase7RoomConstruction-UI-482632b-20261008/Windows/Dungeon Lord.exe`** in place with all adjacent files. The player shares this disposable UAT namespace; do not run Editor and standalone concurrently. Repeat the comprehension gate and core tests below. Close/relaunch this exact executable to verify persistence. The older player remains preserved but is superseded for visual testing.

Owner testing confirmed starter setup, Rooms selection, legal guidance, orientation changes, footprint preview and cost/consequences before this correction. It did not approve the revised interaction. The new build has not been manually tested. Its warning, actual screenshots, exact affected results and validation-only namespace are documented in [ui-correction.md](ui-correction.md).

## Comprehension gate

1. Enter the production Dungeon screen, then Edit Mode.
2. Find Rooms without opening Bootstrap. Select an authored room card.
3. Identify a diamond anchor on the complete visible floor grid.
4. Tap it. Explain which room is previewed, how it connects, where the highlighted terminal moves, the proposed complete draft cost and resulting mana. Distinguish that hypothetical quote from **Current draft**, which excludes the unconfirmed placement.
5. Without scrolling, find **Confirm Placement** and **Cancel Placement** above the separate Save Changes / Discard Draft section. Collapse/expand details and verify both local actions stay reachable. Check 720×1280 portrait, landscape and Large text; orientation and terminal-side choices remain in the horizontal options strip.
6. Try an invalid footprint: its reason and **Keep Invalid Attempt** / Cancel must stay visible with details collapsed. If unaffordable, read the warning and verify confirmation is allowed into the draft while final Save is still blocked.

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
