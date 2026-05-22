# Sprint 2A PR-C UAT Instructions (Manual Validation)

1. Open `Assets/_Project/Scenes/Bootstrap.unity`.
2. Enter Play Mode.
3. Press `F1` to open the Dev Panel.
4. Click `Select Next Slot` until desired deterministic slot is shown in overlay status line.
5. Click one of:
   - `Place Mana Generator`
   - `Place Heat Scrubber`
   - `Place Risk Lab`
6. Verify overlay status updates selected slot + placed structure.
7. Click `Run Structure Tick`.
8. Verify overlay updates:
   - Mana value
   - Heat value
   - Heat crisis status
   - Tick
9. Click `Save Now`.
10. Exit Play Mode, re-enter Play Mode, and verify placed structure and runtime values persisted from save.

Expected pass criteria:
- Placement works for the 3 allowed PR-C structures.
- Tick updates mana/heat/crisis in visible overlay output.
- Save/load preserves `SaveData.dungeonLayout` and `SaveData.structureRuntime`.
