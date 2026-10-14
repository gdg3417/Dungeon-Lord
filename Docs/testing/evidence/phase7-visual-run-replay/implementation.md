# Phase 7 completed manual-run replay

Baseline: `fc281885f68f0ff15be4f3ee9bca9205b3cc6c59`, merged PR #235. Branch: `codex/phase7-visual-run-replay`.

Recommended configuration: GPT-6.1 Sol, High. Classification: Complex, locked integration across deterministic evidence, canonical publication, spatial presentation and production UI. Policy fallbacks: GPT-6 Sol High, then GPT-6 Astra Low. No fallback was requested for this task.

## Authority and evidence

`GameRoot.SimulateRunOnce` still invokes the existing atomic `CommitPhaseFiveBRun`. `CalculatePhaseFiveBRun` starts the same `RunExecution` and drains it once. The manual drain now optionally retains its already emitted stamped `RunStep` values. Its geometry-derived execution ceiling bounds collection; the presentation policy additionally rejects sequences above its display ceiling. Ordinary checkpoint drains retain their original behavior.

The result has two new **assembly-internal transient properties**, `PresentationSteps` and `PresentationLayoutFingerprint`, with explicitly nonserialized backing fields. The latter uses `StructuralEditService.TryFingerprint` on the transaction's validated spatial state. Both pass through the established `RunTransientEvidence.Retain` helper only after successful durable publication. They are omitted from canonical save serialization, alongside existing `SpatialEvents`, party and encounter detail. Schema remains **14**; no migration, executable/checkpoint identity, settlement formula, save owner or durable event journal changes.

The graphical sequence consumes committed `RunStep.Ordinal`, `PositionSpace`, position, floor index, room/assignment identity and exact encounter HP evidence. `RunSpatialEvent` supplies category/option and configured positions for unreachable interactions. Matching by floor/room/assignment is indexed once. It does not sort aggregate `SpatialEvents` as if their storage order were playback time: aggregate traversal is appended after interactions. The actual chronological step stream already contains room entry/movement, corridor and branch approach/return movement, interactions, egress, floor transfer and terminal boundaries.

Room-local cells use `RoomLocalCoordinateTransform.TryToFloor`, including anchor and orientation. Floor-global recorded cells remain floor-global. Tile centers add the established half-cell display offset. A matching complete canonical spatial fingerprint binds floor-index resolution to the current stable floor identities and geometry. Any mismatch fails closed with a localized historical-layout explanation; there is no remapping or historical resimulation.

`DungeonRunReplay` owns only a detached bounded display sequence and a presentation cursor. It has no save/service/simulator reference and cannot resolve encounters, settle rewards, advance gameplay, change mana/Heat or append history. Playback duration comes from `DungeonPresentationPolicy`; it is unrelated to simulation time. At most the bounded frame sequence is advanced after a long render stall. Ordinary movement interpolates only between adjacent recorded cells on the same floor. Transfers and missing positions never create connecting motion.

## Visible behavior

The production Dungeon interface exposes localized Replay Run, Pause/Resume and Close Replay actions. Only the latest available completed manual outcome is offered. Existing Bootstrap controls initiate runs. The interface explicitly labels this a recorded completed run and explains the party's diamond marker.

One aggregate party diamond follows the recorded route. Existing content artwork marks monster encounters, trap triggers and reached loot. An engaged monster's configured-start icon is temporarily replaced by its recorded engagement icon, avoiding a second apparent monster. Unreachable content uses the existing invalid marker at its recorded configured position while the party stays on its recorded path. Descriptions distinguish movement, entry, exit and each interaction. Encounter text reports the recorded damaged member, damage, remaining HP and survivor count. Completion uses the authoritative reason, survivors, casualties and score; existing outcome/history inspection remains available.

The world retains two replay sprite views and reuses them. No search, collection construction or unbounded allocation runs in the animation update. Projection and localized label updates happen at preparation/event boundaries, respectively. Only the selected floor renders the party/interaction; offscreen floors have no hidden sprite animation. The cursor may advance while the player views another floor, and a localized floor notice identifies where to look.

Edit Mode pauses and hides replay. Returning from an unchanged/discarded draft leaves it paused until Resume. A committed spatial change invalidates incompatible replay instead of changing its historical evidence. Closing stops presentation immediately. Camera pan/zoom/Fit and floor selection remain presentation operations.

## Limits

- Detailed replay is session-only. Save/restart preserves coarse run history but provides no detailed replay; the interface explains its unavailability. Complete a new manual run to enable it again.
- Fingerprint compatibility is deliberately conservative across the whole dungeon, including other floors and spatial content. No historical geometry overlay is introduced.
- The party is one aggregate symbol, not independently positioned members. Monster paths, formations, attack animation and tactical combat are not animated. Casualties come from exact encounter evidence and authoritative final summaries.
- The existing manual camera controls remain. This packet does not implement future live-party smart-follow, concurrent party selection, admissions, B3A settlement or offline progression.
- The display ceiling, movement/interaction durations, sizes and color live in the typed presentation policy. Current Windows evidence does not establish physical-device/mobile performance or translated-font qualification.

## Fixture corrections and interrupted work

The initial focused report (11 passed / 2 failed) remains archived. The wipe fixture requested an unsupported one-member party; it now follows the existing three-member wipe fixture and supplies three authoritative fatal encounters. Gameplay configuration validation was not weakened.

The remaining two-floor failure was initially described as activation failure. Diagnostic evidence proved activation eligibility and candidate preparation succeeded. The **subsequent manual commit** used the single-floor branch fixture's writer, which lacks injected `runLoot`. The existing persistence preflight repeats `ActiveFloorRunSnapshot.Create` for multiple active floors and correctly rejects that missing dependency. The fixture now uses `PhaseSixA4Tests.Writer`, as the established multi-floor commit regressions do. Production activation/persistence validation is unchanged. Temporary diagnostic logging existed only in the disposable project and was restored before qualified runs.

The expanded screenshot fixture additionally needed explicit funding before buying trap/loot instances. Its first failed report and the screenshot-test compile correction log are retained. No production price or capacity changed.

The first full EditMode report exposed two integration-contract failures. The new read-only controller partial is now explicitly included in the existing exact consumer allowlist, with all prior composition guards and an additional prohibition on replay invoking `SaveService`. The presentation properties were narrowed to assembly-internal/nonserialized visibility so they do not extend the public gameplay evidence contract. The frozen Phase A projector and 38-scenario fixture remain byte-for-byte unchanged; they were not regenerated or taught to ignore new public fields.

After the overnight power interruption, the branch, all source changes, complete XML/JSON evidence, and all 1,266 isolated inputs were verified intact. No Unity process or stale project lock remained. The confirming run had not started, so no interrupted run was counted as a pass. No recovery, reset, cleanup or branch replacement was performed.
