# Targeted owner retest and outstanding Windows UAT

Owner confirmed the parent commit's Editor route playback, Balanced/Cautious runs, pause/resume, editing and historical-layout rejection. The agent has not performed owner gameplay testing. These corrected visuals require owner retesting; Windows standalone manual qualification remains outstanding.

Use Unity **6000.3.2f1** and the existing isolated project:

`C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat`

Updated player (new directory, earlier player preserved):

`C:/Dev/Dungeon-Lord/Temp/ui-composition-visual-replay-owner-uat/Builds/Development/Windows-visual-run-feedback/Dungeon Lord.exe`

Both use the existing disposable filename `dungeon-lord-visual-run-manual-uat-20261014.json` under `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord/`. Existing UAT progress is preserved; the save is not recopied/reset. Keep all primary/permanent/B2 saves and backups untouched. Run Editor and player sequentially because they share this disposable namespace.

1. Open `Assets/_Project/Scenes/Bootstrap.unity` in the isolated project. Verify existing UAT layout, progression and history. Previous-session history has no detailed replay; complete a new manual Balanced run using existing Bootstrap controls.
2. Replay and pause partway through a monster approach. Its art should move from the configured tile along the recorded path and freeze while paused. Resume; only one encounter should engage at a time. Resolved monsters remain blue at their recorded endpoint during replay, with no kill/corpse claim or unexpected return to the starting tile.
3. Read the party-adjacent damage label: member, exact damage, remaining HP and fallen state when proven. A zero-damage mitigated trigger must be distinguishable when naturally supported by the run. Untriggered/unreachable traps receive no damage number. Do not assume every run contains zero damage or casualties.
4. Compare untouched and triggered traps, unreached/unreachable and reached loot, current gold highlights and previous historical states. A proven casualty leaves a small red diamond at the aggregate encounter tile until completion/Close. No individual path is implied. Expand/scroll Details for the localized marker legend.
5. Pause during damage feedback, pan/zoom, select other floors when supported, and return. Only the selected floor renders; floor/camera selection cannot alter the run. The recorded global cursor can advance while viewing another floor if unpaused.
6. Close and replay again: normal monster/trap/loot visuals must restore on Close, and the new replay starts untouched. Enter Edit Mode during approach/feedback: presentation hides and remains paused after leaving. Discard editing, then commit a disposable spatial change and verify strict historical rejection.
7. If corridor content is available, complete a run that traverses an optional branch, then modify/unassign relevant corridor content through supported controls. That incompatible historical replay must become unavailable. A normal mana/Heat change does not by itself invalidate a compatible replay.
8. Complete a new Cautious run (or Greedy), inspect actual recorded route/branch reasons and compare interactions. Equal routes are valid. Replay must not create another run, history entry, reward or Heat event. Ordinary foreground/passive mana ticks remain independent of replay.
9. Save, close and reopen: progression/history persist; detailed session-only replay is unavailable until another new manual run. Repeat the core run/replay/pause/Close/edit/save/reopen cycle in the updated Windows player.
10. Check **1920×1080, 1280×720 and 720×1280 portrait**, including Large text. Damage labels, pause/Close and editing must remain readable/reachable. Desktop portrait is not phone performance qualification.

Required owner screenshots/notes: corrected commit and resolution; movement/route context; paused monster approach; monster damage; successive resolved/current monsters; triggered versus untouched trap; reached/unreached loot; zero-damage mitigation and casualty if exercised (otherwise explicitly unexercised); Close restoration; Edit Mode suspension; corridor rejection when exercised; restart unavailability with persistent history; Windows standalone core loop at requested resolutions. Report unexpected positions, duplicate sprites, clipped labels or gameplay-state changes. Do not merge as part of this UAT.
