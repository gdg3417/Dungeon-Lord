# Phase 7A5 owner UAT

Automated qualification does not prove usability, fun, mobile performance or native mobile persistence. Perform this after external review using the final branch/commit in the qualification report. Use a backed-up prepared save or disposable test profile. A reset is not required; a fresh save needs existing Bootstrap construction/acquisition controls to prepare an eligible native movable room. Do not delete the owner save for this test. Normal Bootstrap movement has been retired; construction and acquisition remain available.

1. Check out `codex/phase-7a5-transactional-room-movement-economics` at the exact reviewed commit, preserving unrelated local changes.
2. Open `Assets/_Project/Scenes/Bootstrap.unity` in Unity 6000.3.2f1 and enter Play, or launch the complete qualified Windows player listed in qualification.md with its adjacent files.
3. Use a prepared disposable save with an existing native required-route room; if starting fresh, construct one using the retained Bootstrap controls. Do not select an implicit legacy compatibility container for movement.
4. Prepare representative monster, trap and loot content in that room through retained acquisition/placement controls. Record their identities where development diagnostics expose them and note relative tile arrangement. Leave an empty room tile for room selection.
5. Record current Total/Usable Mana, structural investment where available, room anchor, required-route corridor and Completion Terminal positions.
6. Start a run where practical. Record its run-start layout in existing diagnostics to compare the immutable snapshot later.
7. Return to Dungeon and enter production Edit Mode.
8. Tap an empty tile inside the room. Confirm the entire room footprint is selected. A content tile should still select that content.
9. Confirm the contextual sheet identifies the intended room through its localized definition/name and ordinal, then choose Move.
10. Tap an anchor that overlaps the entrance, another room or a fixed structure.
11. Confirm the attempted footprint stays visibly invalid while canonical geometry/content remain unchanged.
12. Confirm the localized reason explains the failure and the sheet permits correction.
13. Confirm Save Changes is unavailable after local protection completes.
14. Choose Move again and tap an out-of-bounds anchor. Confirm the attempt and localized boundary reason remain; Focus floor can include the attempted footprint.
15. Choose Move and tap a valid anchor admitted by existing deterministic renovation routing (no nearest-position correction).
16. Confirm the room, translated downstream rooms, corridor reconnection and Completion Terminal consequences match the displayed final geometry.
17. Confirm contained content stays arranged at the same room-local tiles. Check each representative category.
18. Confirm drafting has not reduced or reserved mana; allow for normal passive accrual when comparing the HUD.
19. Confirm cost, current mana, resulting mana, affordability and final consequences are understandable. Record the configured cost shown.
20. Move the same room to another valid anchor. Confirm the fee reflects one final movement, not accumulated experimentation.
21. Move it back to its canonical starting anchor. Confirm structural movement cost and new renovation investment project to zero; if no other changes remain, Save stays unavailable as a no-op.
22. Move it to a final valid anchor and wait for the protected-local status.
23. Close and reopen before Save Changes using normal application lifecycle.
24. Choose Resume Editing in recovery.
25. Confirm exact acknowledged anchor, content arrangement, valid/invalid state and economic preview are restored. Also repeat close/reopen with one invalid attempt and verify its stable reason survives before correcting it.
26. Choose Save Changes, inspect the review, then confirm.
27. Confirm the authoritative current configured charge is applied exactly once. Compare passive accrual separately from the discrete charge.
28. Inspect the committed room/corridor/terminal consequences and canonical presentation.
29. Close/reopen and verify committed geometry, wallet and investment persist together.
30. Verify contained content identities/category/option/sequence/ownership/relative positions remain unchanged; use existing diagnostics only if identity fields are not player-visible.
31. In a disposable low-mana prepared save, create a valid unaffordable draft. Save Changes may open review, but commit must fail with localized insufficient-mana feedback. Confirm geometry/wallet/investment remain canonical and the protected draft survives for later correction/retry. Do not corrupt owner storage to simulate faults.
32. Confirm the already-started run's snapshot retained its prior geometry through drafting, discard and commit.
33. Start the next run and confirm it uses the newly committed geometry. Existing applicable floor knowledge should become inapplicable through its material fingerprint after commit; draft/discard alone must not do so.
34. Repeat room selection → invalid attempt → correction → economic review → close/reopen recovery → Save → reopen in the qualified Windows Development Build. Also repeat Discard and verify untouched canonical state. No full repeat of A4 pan/pinch/Focus is required; check that the new selection/chrome surfaces do not intercept world interaction incorrectly.
35. Record whether room selection, invalid feedback, correction and economics feel understandable. Include resolution/profile/commit and focused screenshots for unreadable states or defects; include logs only for failures. Report owner UAT as passed only after completing and reviewing these observations.

Storage faults, context drift and outcome-unknown boundaries are automated. Do not manually damage saves to reproduce them. Android/iOS native qualification and device feel remain outside the Windows evidence.
