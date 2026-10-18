# PR #242 owner UI qualification — pending

Automation is not owner usability approval. Keep PR #242 Draft until independent ChatGPT code review and these owner results are reviewed. Do not use an existing owner save or an earlier player build. The five lost screenshot originals remain an accepted documented exception, not restored evidence.

## Unity Editor — do this first

1. Close all Unity Editors for this project. In PowerShell at `C:\Dev\Dungeon-Lord`, explicitly prepare a new owner-only attempt (the `focused` argument selects its isolation plan; **do not invoke the automated `run` command**):

   ```powershell
   $python = 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
   & $python -B Tools/Presentation/qualify_pr242_graphics.py prepare owner-editor-01 focused
   if ($LASTEXITCODE -ne 0) { throw 'Stop: owner isolation preparation failed' }
   & $python -B Tools/Presentation/qualify_pr242_graphics.py preflight owner-editor-01
   if ($LASTEXITCODE -ne 0) { throw 'Stop: owner isolation is not verified' }
   & 'C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Unity.exe' -projectPath 'C:/Dev/Dungeon-Lord' -logFile 'C:/Dev/Dungeon-Lord/TestResults/pr242-graphics-20261018/owner-editor-01/editor.log'
   ```

   A used attempt must not be deleted/reused. Choose a new suffix if necessary and use it consistently in every command/path. Any preservation mismatch is a stop condition. The temporary product is `Dungeon Lord PR242 Graphics owner-editor-01`; all its saves and Unity persistent-data reports are separate from the original `Dungeon Lord` directory.

2. Open `Assets/_Project/Scenes/Bootstrap.unity`, enter Play, and confirm the graphical Dungeon appears. If necessary, use **Development Tools → QA Mana: Fill to Capacity**, then return to Dungeon. This only funds the isolated UAT save. If the fresh floor has no room, first use Edit → Rooms → Basic Room, choose a legal suggested starter anchor, Confirm Placement, then Save Changes. Record this as the one-room starting baseline. Do not edit/save scene or project assets.
3. In Game View use fixed resolutions, in this order: **720×1280, 1280×720, 1080×1920, 1920×1080**. At each resolution select **Small, Default, Large** in Display. Record the actual resolution/text setting with each screenshot. The automated matrix additionally covers explicit inset safe areas; include an existing notched-device simulator if available, without installing anything.
4. Enter Edit, select an existing room, choose **Traps → Spike Trap**, and focus the room. Click an empty room cell with the mouse. Expect a valid preview and no confirmed draft placement. After the panel/map settles, select a different visibly empty cell with touch input if available. Expect the preview at that current cell, not an adjacent or off-room cell. Confirm Placement must create one draft placement. Repeat with Cancel Placement and verify it does not create another placement. Report unavailable touch hardware explicitly; mouse-only testing is not manual touch approval.
5. Save the confirmed draft, verify one placement and one appropriate charge, then reopen the app/Play session and verify persistence. Create another unconfirmed preview and cancel it. Create a confirmed draft, stop and restart Play, choose Resume, verify the same draft, and discard it; the prior saved placement must remain.
6. For construction, choose **Rooms → Rectangle Room**, rotate **90°**, choose a legal suggested anchor, and preview it. At each resolution/text size, expand and collapse details. Expect readable full-size text, a usable map, scrollable information, and reachable **Confirm Placement / Cancel Placement / Save Changes / Discard draft** controls without overlaps or clipped labels. Scroll details to both ends. In landscape construction, Development Tools is available inside the existing **Display** popover; it returns to the action row outside construction.
7. At **1280×720, Large**, inspect most carefully with details both collapsed and expanded: map panning/zoom must remain usable, and at least one complete information line must be visible when expanded. Confirm and save one funded room; preview/cancel another; confirm a draft, restart, resume and discard it. Verify no unexpected extra room or duplicate charge. Also inspect ordinary gameplay, floor controls and replay for visual regressions.
8. Capture screenshots in a **new owner subdirectory**, e.g. `TestResults/pr242-graphics-20261018/owner-editor-01/owner-captures`, using unique names. Record resolution, text size, details state, actual mouse/touch device, result and any defect. Do not replace automated or historical captures.
9. Stop Play and close the Editor. Then restore the temporary inputs and verify preservation:

   ```powershell
   & 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B Tools/Presentation/qualify_pr242_graphics.py finish owner-editor-01
   ```

   Stop and report any mismatch; do not reset or delete files to force this to pass. This restores the exact original local Unity asset overrides as well as authored source.

## Windows standalone — after Editor results

1. Use only `Builds/Development/pr242-graphics-20261018-build-03/Dungeon Lord.exe`. Its packaged product/save identity is `Dungeon Lord PR242 Graphics build-03`; it does not use the original owner save. Close each player instance before changing launch settings.
2. Launch each resolution with a new, unused log name, for example:

   ```powershell
   $player = 'C:/Dev/Dungeon-Lord/Builds/Development/pr242-graphics-20261018-build-03/Dungeon Lord.exe'
   $log = 'C:/Dev/Dungeon-Lord/TestResults/pr242-graphics-20261018/owner-windows-1280x720-01.log'
   if (Test-Path -LiteralPath $log) { throw 'Choose an unused log filename' }
   & $player -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile $log
   ```

   Repeat for **720×1280, 1080×1920 and 1920×1080**, always with unused log names. Record actual client-area dimensions; a window constrained by the desktop may not attain its requested size.
3. Repeat Editor steps 4–7 using the player's current map: all three text sizes, mouse and available real touch, both details states, scrolling, confirm/cancel, save/reopen, draft resume/discard, normal floor controls and replay. Use the isolated Development Tools QA mana control only if needed. Verify the construction UI remains usable rather than judging only a screenshot.
4. Keep screenshots/logs, report any unavailable hardware/scenario, and provide explicit pass/fail observations. No player usability or merge approval is claimed until this evidence is reviewed. Do not mark the PR Ready or merge it yourself as part of these checks.
