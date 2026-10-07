# Phase 7A4 owner-UAT runtime theme correction

Reviewed head: `4352bf5a725e2151f879149cb7c4b950ca8ce0d7`, PR #226. Owner recommendation: GPT-6.1 Sol / Medium, Standard; GPT-6 Sol / Medium fallback. Earlier persistence and review evidence is preserved.

## Reproduction and root cause

Owner UAT stopped in the real Bootstrap runtime: the world rendered, but production HUD/actions appeared largely as unlabeled rectangles. Unity reported: `No Theme Style Sheet set to PanelSettings Panel, UI will not render properly`. The retained Bootstrap Development tools overlay was usable and is not this defect.

Panel.asset referred to GUID `2a4008d95f764354a8cf2a417d584140`. No committed asset resolved it. The validation checkout contained an uncommitted `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` with exactly that GUID, masking the portability defect. The owner checkout had no TSS. The authoring entry point checked PanelSettings persistence, but neither assigned nor validated its theme. Previous tests checked localized strings, layout and interactions without checking the theme dependency or resolved font source; warnings alone did not fail those tests.

The new regression against the old panel failed: one total, zero passed, one failed (`production.dungeon.theme_not_runtime_asset`). XML: ignored `TestResults/phase7a4-theme-reproduction.xml`. This rejects the masking generated theme rather than accepting a nonzero GUID as evidence.

## Correction and ownership

Project-owned `Assets/_Project/UI/ProductionDungeon/DungeonTheme.tss` imports `@import url("unity-theme://default");`, the normal Unity 6000.3 default-runtime-theme import documented in [Unity's TSS manual](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-tss.html). Its Unity-generated meta GUID is `ba4f8dad78c8a0942a67571183125d86`. Panel.asset now references that imported ThemeStyleSheet. No warning suppression, Library reference, package or machine-specific path is used. Dungeon.uxml/uss and Bootstrap scene composition are unchanged.

ProductionDungeonAssetAuthoring loads/assigns this expected theme after opening Bootstrap, validates non-null/persistent/imported identity, project TSS/meta and the expected runtime import before saving, then reopens the scene and validates its retained theme. The first authoring attempt correctly failed its new guard when opening the scene unloaded an earlier unreferenced theme object; loading it after OpenScene fixes that authoring lifetime issue. The final authoring/save/reopen run succeeds (`Temp/phase7a4-theme-authoring-final.log`). Only the panel reference and generated TSS/meta are copied back; broad scene serialization is excluded.

The old generated theme and meta are retained outside the isolated checkout's Assets, under Temp. The focused test passes 1/1 without that masking asset (`TestResults/phase7a4-theme-focused.xml`). Recursive Bootstrap dependencies must contain the project-owned TSS and must not include the former generated-theme directory.

## Regression and visual evidence

One new actual-scene coroutine also runs through a genuine PlayMode adapter. It observes missing-theme warnings from before scene loading, validates the real UIDocument/PanelSettings/theme after runtime load, rejects null/transient authoring themes, checks warning suppression remains false, and checks runtime dependency ownership. After layout it verifies exact localized mode/HUD/action/text-size strings, attached panel, nonzero bounds/font size, a resolved font source, visible ancestors and nontransparent text. It explicitly fails if the warning occurred.

Fresh `phase7a4-theme-normal-1080x1920.png`, captured from the actual Bootstrap UIDocument in its stable disposable test state, was inspected: Dungeon mode, Total Mana, Usable Mana, Mana/hour, Heat, Focus floor, Edit dungeon and Small/Default/Large labels/buttons visibly render. This is targeted rendering confirmation, not complete owner gameplay/usability UAT. Structural/render-state assertions do not individually prove every glyph in every language; broader owner visual checks remain necessary.

## Qualification

Focused theme: 1/1; genuine shell PlayMode: 11/11; complete A4 EditMode: 76/76. Full EditMode: 1,485 total, 1,484 passed, 0 failed, 1 skipped. Full PlayMode: 2,928 total, 2,918 passed, 0 failed, 10 skipped. Both exact skipped-test sets match retained A3 (difference count zero). All 160 canonical integration cases pass in the full EditMode rerun. The accepted GameRoot/SaveService lifecycle cases are reverified in full PlayMode (56 coordinator passed, 14 GameRoot passed/8 established skips), and all accepted A4 cases remain green. No test was ignored or reclassified. Final build/production results and ignored paths follow below. Schema remains 13; no migration, package, gameplay/draft protocol, localization or graphical-scope change. The two unrelated owner changes (TMP fallback-font asset and test-folder meta) are preserved and excluded from this correction.

The correction diff is whitespace-clean. Whole-owner-tree git diff --check reports six pre-existing trailing-whitespace lines in the unrelated modified TMP fallback font; it is preserved, not normalized or committed. New generated theme-meta whitespace is normalized narrowly. Scene authoring produced only two isolated trailing-whitespace differences; the unchanged committed Bootstrap scene is used for final production/build gates.

## Final production/build qualification

| Gate | Result | Ignored evidence |
| --- | --- | --- |
| Theme reproduction against old reference | 1 total, 0 passed, 1 failed | TestResults/phase7a4-theme-reproduction.xml |
| Corrected theme focus | 1 passed, 0 failed, 0 skipped | TestResults/phase7a4-theme-focused.xml |
| Genuine production shell | 11 passed, 0 failed, 0 skipped | TestResults/phase7a4-theme-shell-playmode.xml |
| Complete A4 | 76 passed, 0 failed, 0 skipped | TestResults/phase7a4-theme-complete-focused-editmode.xml |
| Full EditMode | 1,485 total, 1,484 passed, 0 failed, 1 skipped | TestResults/phase7a4-theme-full-editmode.xml |
| Full PlayMode | 2,928 total, 2,918 passed, 0 failed, 10 skipped | TestResults/phase7a4-theme-full-playmode.xml |
| Production gates | 281 passed, 0 failed, 0 skipped (65 build / 112 export / 57 recovery / 37 loading / 10 scene) | TestResults/phase7a4-theme-production-gates.xml |
| Windows Development Build | Succeeded, 0 errors, 1 cloud-symbol credential warning, 171,640,420 bytes | TestResults/phase7a4-theme-windows-build-report.json |

The required DevelopmentBuildUtility.BuildWindowsDevelopment runs through the established Unity CLI: Unity 6000.3.2f1, StandaloneWindows64 Development, Bootstrap-only. The real pre-build gate passes. The packed-asset log includes the project-owned TSS (659.9 KB); there is no dependency on the former generated theme. Log: Temp/phase7a4-theme-windows-development-build.log. Complete player: Builds/Phase7A4-ThemeUAT-2026-10-06/Windows/Dungeon Lord.exe. Existing debugger/Mono shutdown and empty test-assembly diagnostics were not suppressed or refactored.

The missing-theme warning is absent from the explicitly monitored runtime load and focused/full XML/build-log searches. Actual theme/font/text render assertions pass in both scene runners. Five fresh/retained PNGs are in TestResults/phase7a4-theme-screenshots, including the inspected phase7a4-theme-normal-1080x1920.png. Screenshots, reports, logs and builds are ignored and excluded from the commit.

Changed implementation/assets: ProductionDungeonAssetAuthoring.cs, Panel.asset's one theme GUID, DungeonTheme.tss/meta, the actual-scene regression and genuine PlayMode adapter. Existing asset metadata, Bootstrap scene, UXML/USS, all runtime controllers/save/draft services and 41 localization entries remain unchanged. Documentation updates add this evidence and latest totals/UAT instructions without rewriting earlier stops or review records.

No manual player gameplay, complete owner usability, native mobile or low-end performance qualification is claimed. Owner UAT must resume after review. PR #226 is updated; no new PR or merge.
