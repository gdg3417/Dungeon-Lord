# Combat C1 RPG presentation correction

Status: correction, final affected automated qualification, Windows build and preservation audit complete. PR #239 remains the single C1 implementation PR. Independent ChatGPT review precedes the owner retest; no owner acceptance or merge claim. No remaining implementation or required automated-qualification blocker is known.

Baseline: clean existing `codex/combat-c1-graphical-replay` at `cce2e365ff47ece4f0db80330c2475174774d0b0`, matching the independently reviewed PR. Main remains `22e7ce1b08ae17391bfd3542a45821a46f02bbe1` (#238). Initial correction: `cb7d1c2d16fd9cbfc77c61448f945ecd1c4debf8`; test-only follow-up: `5a5c7c30ca6b525b3fbad5db956c82bed47f497c`. Final runtime/UI/test source is `9edf3cf7d0db96e94c53e3e470809cad83d38b3a`, including the long-name HP correction. Later documentation does not change Unity inputs. Exact qualified source and four input-tree hashes are recorded in `source-identity.json`.

Recommended configuration: GPT-6.1 Sol, High. Complex graphical/UI integration across recorded evidence, spatial rendering, localization, responsive UI and Unity lifecycle. Fallback: GPT-6 Sol High; Astra Low only for a documented capability limitation under the canonical model policy. No model switch or delegation was performed. UI Toolkit and Unity CLI skill instructions were followed.

## Display and authority

Monster sprites remain the canonical artwork. Transparent, outlined labels adjacent to sprite bounds now show the localized content name and exact current/max recorded HP. Name and HP are separately rendered in one compact group: the name can ellipsize without hiding the HP line. Both Skeletons display **Skeleton**, with no numerical suffix or M alias. Full immutable actor IDs still bind separate sprites, labels, targets and independent health transitions. Labels remain during pause, stepping and defeat (zero HP); close/edit restores canonical art and hides replay-only UI. The bottom roster/action text also uses localized names/classes instead of A/M aliases. C0 corridor-trap details use the recorded target's class and HP transition; historical B1 wording is unchanged.

The existing target feedback label shows a compact red negative number, a localized zero, or a negative number plus small defeat cue. It has no opaque panel or padding. The recorded attack line is unchanged. Numbers use the current immutable attack/trap result; HP labels use the existing validated replay ledger at that same frame. No damage, initiative, targeting or movement is recalculated. A restrained rise/fade uses replay presentation elapsed time and policy values; pause/Next hold the selected result without retriggering on refresh. Non-hit boundaries and completion clear the number; zero HP persists.

One pooled contextual party group contains every captured member, each with exact `Class - current/max` HP. There are no individual member world positions. The shared recorded anchor, existing floor transforms and camera projection place the group beside the current room, fixed structure or corridor. The preferred side is right, then left, above and below; three offsets per side and at most four policy-owned obstacle-push passes handle nearby room/corridor spans. The maximum displacement is policy bounded. Candidates must stay inside the map viewport and avoid visible dungeon tiles, combatant bodies and floor controls. Projection of the prepared occupied regions is cached until floor rendering/camera/panel scale changes. There is no new pathfinding or full-event scan in display updates.

Party rows use separate shrinking identity and nonshrinking HP text, so longer localization cannot push HP beyond the group. Rich text is disabled. Full row text remains in the tooltip; an optional supplied captured name/title formatter produces `Name/Title - Class - current/max`. Current `RunPartyMember` evidence contains no such name/title, so production always uses class-only fallback. This adds no names, metadata, capture authority or persistent identity system. Formatter tests cover absent, whitespace, supplied and long names.

Only the recorded party floor shows the contextual group. Switching floors does not move its anchor or advance replay. Floor transfer boundaries without a recorded position hide it rather than inventing movement. Pan/zoom/Fit reproject the same evidence. Existing detailed roster and exact chronological bottom text remain available.

C0/B1 projection, executable/checkpoint semantics, source-state capture, frozen fixtures, chronological ordering, HP ledger, full-layout/corridor fingerprints, transient availability, canonical geometry, schema 14, migrations and settlement are unchanged. The only service-file addition is pure localized party formatting in `ProductionDungeonPresenter`; no gameplay/save authority changes. Replay performs no save/reward/history mutation. Passive foreground activity is separated from playback side-effect assertions by the existing fixtures.

## Exact correction files

- `Assets/_Project/Scripts/UI/ProductionDungeonReplay.cs`: natural names, persistent monster HP, pooled contextual roster, bounded placement, lightweight recorded feedback.
- `Assets/_Project/Scripts/UI/DungeonFloorWorldReplay.cs`: prepared rendered-region bounds and selected-floor combatant bounds.
- `Assets/_Project/Scripts/UI/DungeonFloorWorldView.cs`: clear/rebuild the region cache with existing floor rendering.
- `Assets/_Project/Scripts/UI/DungeonPresentationPolicy.cs`, `Assets/_Project/UI/ProductionDungeon/Presentation.asset`: presentation-only widths, font scales, placement and rise/fade/color values.
- `Assets/_Project/UI/ProductionDungeon/Dungeon.uxml`, `Dungeon.uss`: one contextual pooled group, transparent compact labels/feedback and bounded row styling.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`: stable localized name/HP, party formatting, number/zero/defeat and legend keys.
- `Assets/_Project/Scripts/Services/ProductionDungeonPresenter.cs`: optional supplied name/title formatting with class-only fallback.
- `Assets/_Project/Tests/EditMode/CombatC1ReplayTests.cs`: five localized formatter variants; prior projection/integrity cases retained.
- `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC1ReplaySceneTests.cs`: duplicate names/separate HP, exact target numbers, transparency/no body overlap, continuous party HP, recorded branch/trap/two-floor traversal, long localization, nine viewport/text-size combinations, pause stability and unchanged save/runtime bytes.
- `Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs`: two new scene adapters in the established production set.
- `Assets/_Project/Editor/DungeonVisualBuildQualification.cs`: distinct RPG Windows output using the existing qualified build mechanism.

No allowlist or production gate is weakened. No new framework, gameplay tuning, disposable project or cleanup.

## Qualification and original attempts

Unity 6000.3.2f1 (`a9779f353c9b`), existing CLI and r2 automated project only. The warm owner project retains the same namespace and existing two-floor owner save. A fresh preservation baseline after legitimate owner UAT covers 966 save/recovery and previous C0/C1/readability player files. No prior owner save is restored.

Original focused results are retained: focused01 **2/0/0**; focused02 **0/3/0**; focused03 **1/2/0**; focused04 **2/0/0** (passed/failed/skipped). The failures found UITK automatic tooltip replacement on ellipsis, a fixture using unsupported legacy Floor 2 placement, and a real party-placement gap beside terminal structures/wider neighboring rooms. The fixes disable automatic tooltip replacement, use the public structural construction preview/commit, locate the actual recorded anchor's containing structure, and use bounded neighboring-obstacle placement. Visibility/no-overlap requirements were preserved. Focused04 passes desktop/portrait/text-size, movement, completion and two-floor traversal.

The focused04 preservation checkpoint overlapped the last two source edits (a nonallocating `Mathf.Max` form and pause-stability test assertions). Its final comparison correctly refused those two mismatches. No test result was attributed to that later source. The committed source was then freshly prepared with all 1,306 inputs exact before final affected qualification. The strict comparer and permitted transforms were not weakened; original failed checkpoint output/manifests and generated settings archive are retained.

The first bounded EditMode passed **233/0/0**. The first bounded PlayMode passed **170/1/0**: its only failure counted a pooled child whose parent row was correctly hidden after a later run captured a smaller party. A test-only correction counts visible parent rows and verifies the actual assigned corridor trap rather than an initial Floor-space movement inside a room. All nine production scenes then passed at `5a5c7c3`. All 162 core cases passed in the original bounded attempt. These original reports are preserved.

Visual inspection subsequently found that an unusually long monster name could ellipsize the combined text and hide the HP line, despite its text value still containing HP. The production group now renders name and HP separately, and tests verify actual nonempty HP bounds below the name and inside the group. The corridor screenshot also exposed C0's inherited Member-number trap description, now replaced by the captured class and exact recorded HP transition. These runtime/localization changes require fresh affected sets and make the already completed first RPG build obsolete; that player and its original report are retained, with no UAT launch. Final post-correction results are recorded below.

| Applicable check | Source | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| Focused long-name HP, duplicate/target feedback and corridor/two-floor scenes | final runtime, before commit | 4 | 0 | 0 |
| Final bounded EditMode, including 65 production build gates, approved publication, C0/B1/C1 replay and responsive sheet | `9edf3cf` | 233 | 0 | 0 |
| Final bounded PlayMode, including all nine production scenes | `9edf3cf` | 171 | 0 | 0 |

The final reports supersede earlier attempts for changed inputs. They verify separate duplicate identities/HP, visible exact monster HP even with long localized names, red borderless numbers at recorded targets, zero/defeat, all party members and trap HP continuity, recorded corridor and two-floor traversal, floor selection, Edit/layout invalidation, pinned selected run, pause/Next/Resume/Close, pan/zoom/Fit, completion, stable pools and unchanged playback state/save bytes. All three text sizes at 1920×1080, 1280×720 and 720×1280 are exercised. Fourteen final screenshots include nine attack combinations, completion, member/monster defeat, an actual corridor-trap action and long localization. Representative desktop, portrait, defeat, corridor and long-name views were inspected visually. No B1 or save-lifecycle regression was observed. Automated tests do not claim owner visual acceptance or physical-device performance.

The historical full suites remain qualified to their original source: `f501344` EditMode **2,034/0/1 existing skip**, PlayMode **3,460/0/10 existing skips**. They support unchanged-area history and are not represented as full runs on this correction. No broad suite or historical expected B1 output was regenerated.

All final automated and owner-build input checkpoints match **1,306/1,306** files, with zero unexpected inputs or unresolved differences. Permitted transformations remain the exact disposable namespace substitution and established text/JSON normalization. The build generated four known settings/assets differences, archived before exact restoration. The frozen B1 ZIP remains SHA-256 `197793b3b6f4dd127b0e96ce3003f3bee58138e58a81ee1d7dec13b2374dc8fb`. Source diffs against the correction baseline contain no gameplay, core/save, Packages, ProjectSettings or ContentAuthoring change. No allowlist was loosened.

The final Windows x64 Development Build succeeded on **`9edf3cf`**, Unity 6000.3.2f1: **0 errors, 1 existing warning** (native symbols were not uploaded because the Unity services URI is empty). Bootstrap is the sole scene, all **52** required artwork assets are packed, and the unchanged disposable owner namespace is verified in packed assets. Reported build size: **181,688,587 bytes**. The complete **303-file** artifact and original reports are hashed in `windows-artifact-manifest.json` and `build-reports-original.zip`.

Final executable: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c1-rpg-final-20261016/Dungeon Lord.exe`. It was not launched. There were **two** RPG builds: the first succeeded at `5a5c7c3`, then became obsolete when visual inspection required the HP correction. Its original player, reports and 303-file hash inventory are retained separately. Exactly one replacement build was made after final runtime stabilization; it is the one for review/retest.

The fresh **966-file** audit has **zero changes**, including primary/owner saves, recovery files and the three previous C0/C1/readability comparison builds. The newly superseded RPG build's additional **303 files** also remain identical. No save was restored, reset, replaced, transferred or migrated; no owner gameplay was automated. Only the retained r2 automated project and warm owner project were reused. No project/cache recreation, deletion or other cleanup occurred. No Editor or player was active against the owner namespace during refresh/build. All original failed reports, redacted logs, input inventories, generated-input archives, preserving helpers and complete before/after hashes are retained in the adjacent archives.

Representative final captures: [desktop / two Skeletons](composition-combat-c1-rpg-attack-Default-1920x1080.png), [portrait / Large text](composition-combat-c1-rpg-attack-Large-720x1280.png), [corridor trap and party HP](composition-combat-c1-rpg-corridor-1920x1080.png), [defeat](composition-combat-c1-rpg-monster-defeat-720x1280.png), [long localized names with separate HP](composition-combat-c1-rpg-localized-long-720x1280.png). All fourteen original PNGs are in [screenshots.zip](screenshots.zip), with SHA-256 inventory in `screenshots.json`.

Exact invocation conventions: R=`C:/Dev/Dungeon-Lord`, P=`R/Temp/ui-composition-combat-c0-diagnostics-validation-r2`, W=`R/Temp/ui-composition-combat-c0-diagnostics-validation-warm`.

```text
unity test P --mode EditMode --filter "CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests|UatContentHeightCardsDisplayAndSafeActions" --output R/TestResults/combat-c1-rpg-editmode-final.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-rpg-editmode-final.log
unity test P --mode PlayMode --filter "CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-rpg-playmode-final.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-rpg-playmode-final.log
unity run W --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC1RpgFinalWindows -logFile R/Temp/combat-c1-rpg-build-final.log
```

## Accepted visual limits and owner retest

At extreme zoom/crowding or a viewport too short for the full party list, a candidate may not fit; the bounded fallback suppresses it and the detailed roster remains accessible. This is not the normal desktop/portrait default: the established nine combinations require visible names/HP, party group and target damage. Long identity text may ellipsize; exact HP remains visible and full text is in the tooltip. Outline/contrast and existing production artwork are reused. Arbitrary-density packing, device performance, translated-font coverage and owner visual acceptance are not claimed. No independent party paths or live combat streaming.

After independent review confirms no blockers:

1. Open the refreshed warm project in Unity 6000.3.2f1, retaining `combat-c0-diagnostics-validation-20261015.json`. Complete a new manual run and start replay; transient detail from a prior app session is intentionally unavailable.
2. Find two Skeletons. Check identical localized names, separate HP, visible artwork, small red target-local numbers, exact HP changes and a compact defeat cue/zero HP. Compare Pause/Next with the supplementary chronological bottom details.
3. Check all party rows, class/current/max HP and muted defeated members. Follow the group beside rooms/corridors and onto the next recorded floor. Pan/zoom/Fit and switch away/return during a paused attack. Check Resume, final completion and Close restore/hide behavior.
4. Repeat desktop and portrait with Small/Default/Large text; inspect long-name elision without lost HP. Close Editor before launching the new RPG Windows player with the same protected namespace; repeat the short graphical checks and save/reopen behavior. Do not run Editor/player concurrently or reset/replace saves.

Windows parity and owner UAT remain manual outcomes. Stop after updating PR #239 for independent review; do not merge or start C2.
