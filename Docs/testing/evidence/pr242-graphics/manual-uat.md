# PR #242 — short owner usability check

The previous owner UAT failed usability. The new correction passed Codex's focused, four-resolution/text-size, affected EditMode/PlayMode and Windows build checks. **Your usability approval and independent review are still pending; PR remains Draft.**

Codex prepares and opens the isolated Editor with a valid starter room and sufficient resources, using a complete save produced by the existing starter/QA authorities. Your normal save and the prior UAT draft remain protected. You do not need PowerShell, fixture setup, Bootstrap room creation, automated tests, log inspection, cost calculations or a rebuild. If the starter room/resources are missing, stop and tell Codex.

## Unity Editor

1. Use the **Game** tab at **1280×720**. If the scene is Untitled, double-click `Assets/_Project/Scenes/Bootstrap.unity`, then press **Play**. Open **Display → Large**, then close Display. Check that the existing room and controls are comfortably readable. Opening/closing Display should not obstruct or trap an interaction.
2. Enter **Edit → Rooms → Rectangle Room**. Try **0° / 90°** and a different **Terminal side**; use the visible horizontal scrollbar/arrows to reach the remaining options. Click a highlighted legal location. Inspect **Expand Details / Collapse Details**, scroll the information, and pan/zoom the map if helpful. Judge whether tiles are comfortable to inspect/target and **Confirm Placement / Cancel Placement / Save Changes / Discard draft** are understandable and reachable. Confirm Placement records a draft; Save Changes publishes it. Cancel this room preview for the trap check.
3. Click the middle of the existing Basic Room, then **Traps → Spike Trap**. Check **New / Owned** and the cards/prices. Click two different empty tiles: the preview should follow the intended tile each time. Try **Cancel Placement**, then select Spike Trap again and **Confirm Placement → Save Changes**, confirming the review when shown.
4. Stop and restart **Play**; check that the saved trap remains. Make and cancel one further preview. If you leave a confirmed draft across a restart, **Resume** must restore it and **Discard draft** must preserve the earlier saved change. Report confusing controls, clipping, overlap, difficult targeting, or unexpected persistence.

When finished, stop Play, **close Unity normally**, and tell Codex. Codex restores the temporary inputs, checks preservation and starts the qualified Windows player. Do not launch another player while the Editor attempt is active. Screenshots are helpful; Codex will preserve and organize them. Physical touch is optional—unavailable hardware is not a failure.

## Windows standalone

Codex launches the qualified isolated Development player at 1280×720 with its own funded starter save. Repeat only steps 1–4's essential checks (Display/Large, room options/details, two trap targets, confirm/cancel and save/reopen). Tell Codex when you close the player and when a reopen is needed; no command-line work is required.

Your report can be: **Construction usable/unusable; trap placement usable/unusable; save/reopen correct/incorrect**, with a short description or screenshot for each problem. This is a usability check, not financial arithmetic or the broader technical matrix.

The graphical first-room Bootstrap dependency remains deferred; the supplied fixture avoids it for this check. The five previously lost historical screenshots remain an accepted documented exception, not restored originals. Prior procedure bytes are archived in the [new reports](../pr242-usability/reports.zip); those old commands are historical and must not be reused.

[Correction evidence and exact technical results](../pr242-usability/qualification.md) · [Before/after captures](../pr242-usability/visual-review.md)
