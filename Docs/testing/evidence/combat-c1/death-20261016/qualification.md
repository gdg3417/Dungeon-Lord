# Combat C1 death visibility correction

Status: runtime, affected automated qualification, replacement Windows build and preservation audit complete. Continue the single draft PR #239. Independent ChatGPT review precedes owner retest; do not merge or claim acceptance of this death correction. No remaining implementation or required automated-qualification blocker is known.

Verified starting branch: `codex/combat-c1-graphical-replay`, clean at `ecc025d72ee8597ceb340850c5a1ad39b7d9ab9e`, matching the last reviewed head. Qualified runtime/UI/test source: `8e9f63195af9df26dde180e318c21e2ea7c47b84`. Later documentation/evidence commits leave all four Unity input trees identical. The main/C0 baseline remains `22e7ce1b08ae17391bfd3542a45821a46f02bbe1` (#238).

Model recommendation: GPT-6.1 Sol High; focused graphical integration correction. High reasoning is requested for recorded death identity/position, floor lifecycle, pooled presentation and B1 compatibility integration under established authority boundaries. Fallback: GPT-6 Sol High. No model switch, escalation or delegation was performed. Existing UI Toolkit and Unity CLI skills were used; no live Editor was detected outside the sandbox before direct asset editing.

The owner confirmed the previous localized-name, independent-HP, contextual-party and floating-number improvements. The new finding was that zero HP/tint/bottom text did not make death sufficiently apparent. The [prior RPG correction](../rpg-20261016/qualification.md) and all historical qualification artifacts retain their dated source applicability.

## Implementation and authority

`DungeonFloorWorldReplay` prepares selected-floor casualty sites once when binding a valid C0 replay. It walks the already validated chronological display frames, accepts recorded positive-to-zero combat/trap results, identifies the captured member by stable replay actor index/member ordinal, and de-duplicates each member's death. Each site retains the exact immutable frame position and death frame indices. Grouping is by that floor's exact recorded shared-anchor position, never an invented individual adventurer position. No gameplay resolver, damage calculation, current balance lookup or canonical mutation is involved.

One pooled `RecordedAdventurerCasualty` SpriteRenderer is used per unique site, with a compact localized `×N` when multiple deaths have occurred there. The count includes only death frames at or before the current cursor; future sites stay hidden. Markers remain after movement, on the original floor, and through C0 replay completion. Viewing a prior floor after the party has descended still shows its recorded casualties. The approved production `Invalid` artwork supplies a transparent outlined cross, distinguished from the ordinary interaction diamond. No new artwork or generalized HUD/animation framework is added.

Each selected-floor monster binding owns one reused `RecordedMonsterDefeat` indicator. At the exact recorded zero-HP frame, the original sprite stays at its recorded position with the policy's clearly subdued defeated color/opacity, and a small cross appears beside it. Both persist on later frames, including completion and floor revisits. Names and zero/max HP remain. Living monsters retain original colors and have no defeat indicator. Close/Edit restore original canonical positions/colors/orders and hide all replay-only markers. The source evidence and actual monster outcomes are unchanged.

The existing red killing damage number and localized two-line Defeated cue remain the moment-of-death feedback. They are derived from the current event and replay elapsed time; refresh does not trigger effects or add objects. The contextual party roster keeps every member with the same class-only/future supplied-name formatting and exact zero/max HP; defeated rows become red and bold. Marker/count bounds participate in the existing bounded overlay placement, keeping labels/numbers clear of combat artwork and controls.

Preparation is bounded by the existing supported frame/actor limits. Unique casualty sites cannot exceed captured members; monster indicators cannot exceed captured monsters. Each pool is bounded by the configured actor ceiling (64 in production), and is reused across floors/runs; retained high-water marks from different runs do not imply new objects per refresh. Updates visit only bounded prepared sites/death ordinals, actor states and current-floor markers; they do not rescan the full event stream. Counts use pooled UI Toolkit labels and existing locale-aware formatting. Dimensions, offsets, colors and text sizing remain in `DungeonPresentationPolicy` and `Presentation.asset`; existing replay timing owns the unchanged momentary damage/Defeated cue.

The original B1 `ReplayBinding.Casualty` path remains separate, including its anchor artwork, size, damage boundary and completion hiding behavior. C0 traps use the new C0 site path, preventing duplicate B1-style markers. Historical B1 executable/checkpoints and frozen fixtures are unchanged. C0 resolution/targeting/initiative/HP, transient capture/availability, source fingerprints, schema 14, migrations, canonical geometry, rewards/settlement, mana/Heat/loot and Living Dungeon authorities are unchanged. No persistent death/HP data, journal or second state owner is introduced.

## Exact correction files

- `Assets/_Project/Scripts/UI/DungeonFloorWorldReplay.cs`: C0 recorded casualty grouping, persistent selected-floor marker pools, monster defeat state and bounds; B1 separation and cleanup.
- `Assets/_Project/Scripts/UI/ProductionDungeonReplay.cs`: localized pooled counts, marker-aware overlay placement, red/bold zero-HP roster rows and UI cleanup.
- `Assets/_Project/Scripts/UI/DungeonPresentationPolicy.cs` and `Assets/_Project/UI/ProductionDungeon/Presentation.asset`: presentation-only cross size, monster offset/tint/opacity and count font size.
- `Assets/_Project/UI/ProductionDungeon/Dungeon.uss`: transparent count styling and defeated row emphasis.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`: localized count/detail keys; existing damage/Defeated keys retained.
- `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC1ReplaySceneTests.cs`: stronger existing C1 scenes for exact death positions/frames, grouped combat casualties, fatal corridor trap, Floor 2 progression/prior-floor revisit, camera controls, completion/Edit/Close/replay/new-run cleanup, persistent monster state and save/runtime preservation. Test-only HP/damage inputs create the required recorded outcomes before replay; production tuning is unchanged.
- `Assets/_Project/Editor/DungeonVisualBuildQualification.cs`: distinct replacement Windows path using the existing qualified production build mechanism.

This report, its source/evidence inventories and current status pointers are the documentation changes. No UXML change, new Unity file/meta, production allowlist relaxation or build-gate weakening was needed.

## Focused qualification

Unity **6000.3.2f1** (`a9779f353c9b`), existing CLI **1.0.0-beta.12**. Only r2 automated and warm owner projects were reused. The fresh preservation baseline includes **1,572** save/recovery and five previous C0/C1/readability/RPG player files. It is captured after legitimate owner UAT; no older owner save is restored.

| Check | Source | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| Initial focused C1 target/traversal/responsive and B1 casualty scenes | identical final runtime, before extra camera/cleanup assertions | 4 | 0 | 0 |
| Final affected EditMode, including 65 content build gates, approved publication, responsive UI, C1 and historical B1 replay | `8e9f631` | 138 | 0 | 0 |
| Final affected PlayMode, including all nine C1/C0/B1 production scenes | `8e9f631` | 80 | 0 | 0 |

The strengthened scenes verify no marker before death; distinct monster defeated appearance/indicator; combat and trap casualties; exact frozen positions and floor; one marker with counts 2–5 at a shared anchor; persistent zero-HP rows; pause/Next/refresh; normal desktop/portrait visibility; pan/zoom/Fit without moving death positions; progression from fatal Floor 1 trap to Floor 2; prior-floor revisit; completion; Edit/Close restoration; same-run reopen without pool growth; later-run absence of inherited casualties; and unchanged playback save/runtime bytes. The historical B1 casualty/mitigation scene passes with its original expectations. All three text sizes at 1920×1080, 1280×720 and 720×1280 are retained in the responsive scene. No failed test attempt or fixture relaxation occurred for this correction.

Original RPG final-source results remain **233 EditMode / 171 PlayMode** at `9edf3cf`. Original full suites remain **2,034 passed / 0 failed / 1 existing skip EditMode** and **3,460 / 0 / 10 existing skips PlayMode** at `f501344`. These are historical unchanged-area evidence, not new full-source reruns. No unrelated full suite or unchanged C0 resolver suite was repeated; no frozen B1 fixture was regenerated. Final report case lists and source hashes are adjacent.

All final automated and owner-build checkpoints match **1,306/1,306** inputs, with no unexpected or unresolved differences. The exact namespace substitution and established text/JSON normalization are unchanged. The build's four known generated settings/assets differences were archived before restoration. Frozen B1 fixture ZIP SHA-256 remains `197793b3b6f4dd127b0e96ce3003f3bee58138e58a81ee1d7dec13b2374dc8fb`. No gameplay, core/save, Packages, ProjectSettings or ContentAuthoring source changed.

Exactly **one** replacement Windows x64 Development Build succeeded on `8e9f631`: **0 errors, 1 existing warning** (native symbols were not uploaded because of an empty Unity services URI). Bootstrap is the sole scene; all **52** required artwork assets are packed, including the existing cross. Reported build size is **181,696,136 bytes**. The original reports and complete artifact hashes are in `build-reports-original.zip` and `windows-artifact-manifest.json`.

Executable: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c1-death-20261016/Dungeon Lord.exe`. It was not launched. All previous C0/C1/readability/RPG players remain at their original paths. The final **1,572-file** preservation audit has **zero changes**, including the primary/owner save, recovery files and five prior players. No save was restored, reset, replaced, transferred or migrated. No owner gameplay was automated; no project/cache was created, recreated or deleted. Only the retained r2 and warm environments were refreshed. Process checks found no owner Editor/player before refresh/build.

The support archive retains exact input manifests, generated-input archives, preserving/collection helpers, redacted logs and complete protected-file before/after hashes. Original NUnit reports are in `qualification-reports.zip`. The final screenshot archive and SHA-256 inventory include desktop/portrait grouped casualties, monster/member death, fatal trap, prior-floor persistence, completion and the existing nine responsive attack combinations. Representative views were visually inspected before building: [monster defeat](composition-combat-c1-death-monster-defeat-720x1280.png), [five co-located casualties / portrait](composition-combat-c1-death-group-5-720x1280.png), [desktop](composition-combat-c1-death-group-5-1920x1080.png), [fatal corridor trap](composition-combat-c1-death-corridor-trap-1920x1080.png), [prior-floor persistence](composition-combat-c1-death-prior-floor-1920x1080.png), [completion](composition-combat-c1-death-completed-720x1280.png). These are automated fixture captures, not owner acceptance.

R=`C:/Dev/Dungeon-Lord`, P=`R/Temp/ui-composition-combat-c0-diagnostics-validation-r2`, W=`R/Temp/ui-composition-combat-c0-diagnostics-validation-warm`.

```text
unity test P --mode PlayMode --filter "CombatC1TargetFeedback|CombatC1PartyFollows|CombatC1MultiDefender|VisualRunFeedbackCasualty" --output R/TestResults/combat-c1-death-focused01.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-death-focused01.log
unity test P --mode EditMode --filter "CombatC1ReplayEditModeFixtures|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests|UatContentHeightCardsDisplayAndSafeActions" --output R/TestResults/combat-c1-death-editmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-death-editmode.log
unity test P --mode PlayMode --filter "CombatC1|VisualRunReplay|VisualRunFeedbackCasualty|CombatC0ProductionEvidenceExport|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-death-playmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-death-playmode.log
unity run W --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC1DeathVisibilityWindows -logFile R/Temp/combat-c1-death-build.log
```

## Owner retest after independent review

1. Open the refreshed warm project in Unity 6000.3.2f1 with the retained two-floor `combat-c0-diagnostics-validation-20261015.json` save. Complete a new manual run, then replay. Detailed prior-session replay remains intentionally unavailable; do not reset or replace saves.
2. Pause/Next over a monster killing hit. Check the red number/Defeated cue, original but subdued art, small cross and zero/max HP. Continue into later frames and revisit its floor; the defeated state must persist.
3. Inspect an adventurer killing hit and a fatal trap when available. Check the cross at the recorded shared party anchor, persistent zero/max row with red/bold emphasis, and a compact count for co-located deaths. Continue to another room/floor and return to the death floor; the marker must stay at its original location.
4. Pan/zoom/Fit; switch floors during a paused death; Resume/complete; enter/discard Edit; Close and replay the same retained run. Verify correct hiding/restoration and no accumulation or inherited markers on another run. Repeat portrait/desktop/text sizes.
5. Close Editor before launching the new death-correction Windows player under the same namespace. Repeat the short death/lifecycle checks and save/reopen behavior. Never run Editor and standalone concurrently against that save. No owner save is launched automatically by qualification.

Accepted limits: C0 has one shared party anchor, so a grouped marker records a location/count rather than invented individual bodies. The existing outlined cross is reused rather than new skull/corpse artwork. Very constrained/offscreen count labels may be suppressed by collision-aware placement; the world marker and detailed roster remain. Visibility at arbitrary zoom/density, physical-device performance and owner visual acceptance are not claimed. No C2/live combat work is included.
