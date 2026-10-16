# Combat C1 map readability correction

Status: correction and affected automated qualification complete. PR #239 awaits independent review, followed by the short owner retest. No merge or owner acceptance claim. This dated correction supersedes the initial C1 presentation choices while retaining their original reports.

Baseline: clean `codex/combat-c1-graphical-replay` at `a3dfea81d0dcde750a21168ac8454a3825c1ccf4`, matching PR #239. Fetched main remains `22e7ce1b08ae17391bfd3542a45821a46f02bbe1`. Runtime/UI correction: `c75ddf43e45d77de28ac8c0099c3b6a9f39d7fa4`. Final Unity source including scene-fixture setup corrections: `742c45de6cd153aebeeb642183475b3129744075`. Later documentation/evidence commits do not alter Unity inputs.

Recommended configuration: GPT-6.1 Sol, High; Complex continuation across production presentation, spatial/UI transforms, localization and Unity lifecycle. Fallback: GPT-6 Sol High, then Astra Low only under the documented availability/capability conditions. No model switch or delegation was performed.

## Correction

Owner Editor UAT found the earlier opaque aliases obscured monster art and C0 damage was only practical to read in the bottom panel. Existing canonical monster SpriteRenderers now retain their original living colors. Stable M/A aliases use small, translucent, single-line chips. The active adventurer is identified at the existing shared party anchor; no independent adventurer position is introduced. Full identities remain in tooltips and the roster.

The existing `replayDamage` UI Toolkit label now also presents C0 attack results. Its alias, damage, remaining HP and defeat come directly from the current immutable combat event. Zero damage has an explicit localized form; defeat is red and says zero HP/defeated. The recorded attack line and complete chronological bottom description remain. Pause/Next keep the selected recorded result visible; nonattack boundaries, completion, close, Edit and other selected floors suppress the callout.

The selected floor exposes the actual recorded combatant SpriteRenderer bounds. Reused bounded rectangle lists project those bounds into the existing map panel. Four adjacent candidate positions avoid combatant bodies, floor controls and (for chips) other chips/the higher-priority damage callout. No new scene/HUD framework or visual object stream is created. Chip text scales modestly with text size and never wraps. All presentation dimensions/font scales remain in `DungeonPresentationPolicy` and the production asset. Offscreen or crowded overlays with no clear candidate are suppressed rather than covering sprites; pan/zoom and full roster details remain available. This is not a guarantee of arbitrary-density label packing.

No C0 evidence validation/resolution, B1 executable/replay semantics, route ordering, HP ledger, fingerprints, transient retention, availability, schema 14, migration or settlement changes. No new project or cleanup. The production UXML already supplies the feedback label and needs no change; `DungeonFloorWorldView.cs` also needs no unrelated change because its partial replay file owns the bounds access.

## Files changed for this correction

- `Assets/_Project/Scripts/UI/ProductionDungeonReplay.cs`: chips, target result callout and bounded placement.
- `Assets/_Project/Scripts/UI/DungeonFloorWorldReplay.cs`: actual combatant bounds; preserve living artwork colors.
- `Assets/_Project/Scripts/UI/DungeonPresentationPolicy.cs` and `Assets/_Project/UI/ProductionDungeon/Presentation.asset`: presentation tuning.
- `Assets/_Project/UI/ProductionDungeon/Dungeon.uss`: single-line translucent chips and compact C0 feedback style.
- `Assets/_Project/Data/Bootstrap/string_table_en.json`: localized hit/zero/defeat and updated legend.
- `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC1ReplaySceneTests.cs`: visible original art, no body occlusion, recorded member/monster hit/zero/defeat, nine resolution/text-size captures, completion, active-attack Edit/floor switching and stable label pool assertions.
- `Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs`: adapter for the new recorded-result scene.
- `Assets/_Project/Editor/DungeonVisualBuildQualification.cs`: existing build mechanism with a distinct readability output path, preserving previous C1/C0 players.
- This report, its original evidence archives and the current C1 status pointers. No historical expected B1 output is regenerated.

## Qualification and preservation

Reuse only `Temp/ui-composition-combat-c0-diagnostics-validation-r2` for automated tests, with namespace `combat-c0-access-validation-20261015.json`. The retained warm owner project keeps `combat-c0-diagnostics-validation-20261015.json`. Scene fixtures use their established unique names before Boot and remove only their own disposable saves. Capture a fresh preservation baseline after the owner's legitimate UAT; do not restore the older pre-UAT owner save. The new snapshot contains 663 save/recovery and prior comparison-build files.

Initial attempt did not produce a test verdict: a new test omitted the required `ContentService.GetString` fallback argument. That test API call was corrected. The subsequent two focused scenes passed, including exact event callouts and non-overlap at all nine resolution/text-size combinations.

Two bounded PlayMode attempts each passed 163/164. The sole failure was the strengthened floor readability fixture. Test-local diagnostics established that the recorded and selected floors agreed, but the inherited CLI view was 640x360 with only a 77-pixel-high map: none of the four two-line callout candidates was unobstructed. Suppression preserved sprite/control visibility as designed. The fixture now explicitly selects the recorded floor and the established 720x1280 portrait view before asserting visible feedback. Temporary diagnostic code was removed; the diagnostic source/log and failed attempts remain in the support/report archives. Only the scene-test file changed after the EditMode result; runtime/configuration stayed frozen. The corrected final production follow-up supersedes that failed scene verdict.

| Applicable check | Source | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| Initial focused map/result scenes | pre-commit correction | 2 | 0 | 0 |
| Bounded EditMode, including 65 build gates, approved publication, responsive sheet and C0/B1/C1 projection regressions | `c75ddf4` | 228 | 0 | 0 |
| Bounded PlayMode core cases (both attempts) | unchanged core/runtime inputs at `c75ddf4` / `e61dece` | 157 | 0 | 0 |
| Final seven production scenes | `742c45d` | 7 | 0 | 0 |

The last two rows are explicit coverage across reports, not a claim that either earlier 164-case report was wholly green. `qualification-applicability.json` lists every core case and source applicability; `report-summary.json` preserves every original verdict. The seven final scenes cover all four C1 production cases plus historical B1 lifecycle/responsive controls, historical casualty/mitigation and C0 production evidence. They verify exact recorded member/monster/zero/defeat callouts, original art, no combat-body occlusion, no wrapping, controls, active-attack Edit/floor changes, completion, stable pooled visual/label counts, and unchanged runtime/save bytes during playback. Twelve final screenshots include nine resolution/text-size attack views, member/monster defeat and completion. Portrait Large, desktop Default, member defeat and completion were also inspected visually. No B1 or save-lifecycle regression was observed. Earlier full suites and skips remain preserved with their original applicability; no full suite was rerun for this correction.

All final checkpoints match 1,306/1,306 inputs, with zero unexpected files or unresolved differences after known generated settings/assets are preserved and restored. The source-consumer allowlist and build gates are unchanged. One intermediate checkpoint correctly refused an already-edited test input; its generated settings content was already preserved byte-for-byte in the focused archive, as recorded by `generated-settings-provenance.json` in the support ZIP. Neither the strict comparer nor its permitted transformations was weakened.

Exactly one new Windows x64 Development Build succeeded under Unity 6000.3.2f1: zero errors, one existing native-symbol upload warning (`UriFormatException`, empty Unity services URI). Bootstrap is the sole scene; all 52 required artwork dependencies are packed. Reported build size is 181,674,872 bytes; the complete 303-file artifact is 182,148,527 bytes. The existing disposable namespace is verified in packed assets. Executable: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-warm/Builds/Development/Windows-combat-c1-readability-20261016/Dungeon Lord.exe`. It was not launched. Prior C1 and final C0 comparison players remain at their original paths with identical hashes. The build generated four known input differences, archived before exact restoration.

The fresh 663-file preservation audit has zero changes, including primary/owner saves, recovery files and both comparison builds. No save was restored, replaced, transferred, reset or migrated. No owner gameplay was automated, and no project was created or deleted. `preservation-summary.json`, `source-identity.json`, input manifests, original build reports and full artifact hashes are adjacent or in the support archive. No remaining implementation or required automated-qualification blocker is known; owner visual acceptance and standalone gameplay parity remain unclaimed.

The established bounded EditMode filter is `CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests|UatContentHeightCardsDisplayAndSafeActions`. The established bounded PlayMode filter is `CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved`. No unrelated full suite rerun is needed for presentation-only inputs; earlier full results retain their original source applicability and are not relabeled.

Exact invocation conventions: R=`C:/Dev/Dungeon-Lord`, P=`R/Temp/ui-composition-combat-c0-diagnostics-validation-r2`, W=`R/Temp/ui-composition-combat-c0-diagnostics-validation-warm`. Installed Unity CLI: `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`. Unity 6000.3.2f1. Bundled Python: `C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

```text
unity test P --mode EditMode --filter "CombatC1ReplayEditModeFixtures|CombatC0|VisualRunReplay|ProductionPublicationHasOnlyApproved|ProductionSpatialContentBuildGateTests|UatContentHeightCardsDisplayAndSafeActions" --output R/TestResults/combat-c1-readable-editmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-readable-editmode.log
unity test P --mode PlayMode --filter "CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-readable-playmode.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-readable-playmode.log
unity test P --mode PlayMode --filter "CombatC1|CombatC0|VisualRunReplay|VisualRunFeedbackCasualty|ProductionPublicationHasOnlyApproved" --output R/TestResults/combat-c1-readable-playmode-final.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-readable-playmode-final.log
unity test P --mode PlayMode --filter "CombatC1FloorSelection|CombatC1EditSuspension|CombatC1MultiDefender|CombatC1TargetFeedback|VisualRunReplayProductionLifecycle|VisualRunFeedbackCasualty|CombatC0ProductionEvidenceExport" --output R/TestResults/combat-c1-readable-production-final.xml --timeout 1200 --no-color -- -c0DiagnosticsSerialImports -logFile R/Temp/combat-c1-readable-production-final.log
unity run W --timeout 1800 --no-color -- -c0DiagnosticsSerialImports -executeMethod DungeonBuilder.M0.EditorTools.DungeonVisualBuildQualification.BuildCombatC1ReadabilityWindows -logFile R/Temp/combat-c1-readable-build.log
```

## Owner retest, after independent review

1. Open the refreshed warm project and its existing two-floor save. Do not reset, replace or transfer the save. Complete a new manual run and start replay.
2. In a multi-defender encounter, inspect visible monster artwork, distinct small M chips, the current A chip at the shared party anchor, attack lines, target-local damage/HP and red defeat. Compare the supplementary bottom chronology with each selected action.
3. Pause/Next/Resume, switch floors during a hit and return, pan/zoom/Fit, and try Small/Default/Large text in portrait and desktop. Finish and close replay; check stale callouts disappear and the completed outcome/roster remains readable.
4. Close Editor before launching the new readability Windows player under the same namespace. Repeat the short checks and reopen/save lifecycle. No concurrent Editor/standalone access. Owner visual acceptance and Windows gameplay parity remain manual outcomes, not automated claims.

Stop after updating PR #239 for independent review. Do not merge or begin C2.
