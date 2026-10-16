# Combat C2 reserve affordability presentation correction

Reviewed source: `d3f81ab866ff9c35b72ceb9fd86ef8015609b4f0`, existing branch `combat-c2-monster-continuity`, draft PR #240. This packet supplements, and does not replace, the original C2 qualification packet in the parent directory. Recommended configuration: GPT-6.1 Sol Medium; focused integration correction (policy Standard); GPT-6 Sol Medium fallback.

## Behavior and authority

Previously the acquisition label tested only `currentMana >= price`. A Goblin at wallet 24 therefore appeared affordable although its 20-mana purchase left only 4 against a required reserve of 6. The canonical transaction already rejected it. Required reserve and shortage appeared only in a tooltip on usable mana.

The production placement UI now obtains `SaveService.PreviewDungeonDraft(current, draft, contentPreview.Candidate)` at valid geometry boundaries. `ContentAcquisitionEconomyPresenter` displays that preview's affordability/reason and resulting global required reserve. It contains no reserve formula. Quotes include all current draft changes, existing obligations, prices and applicable refunds. A position has to be valid before the UI claims affordability; before then, the browser/legacy acquisition feedback displays price and current mana only. Confirm Placement still stages a draft; Save remains the authoritative payment/online-verification boundary. Online purchase eligibility is shown separately from the economic quote.

| Operation | Wallet | Price | Resulting required reserve | Result |
| --- | ---: | ---: | ---: | --- |
| New Goblin, no deployed defender | 24 | 20 | 6 | Reject |
| Same Goblin | 26 | 20 | 6 | Accept |
| New Skeleton, no deployed defender | 31 / 32 | 25 | 7 | Reject / accept |
| New Goblin, existing Skeleton | 32 / 33 | 20 | 13 | Reject / accept |
| Spike trap, existing Skeleton | 20 / 27 | 20 | 7 | Reject / accept |
| Hidden cache, existing Skeleton | 10 / 17 | 10 | 7 | Reject / accept |
| Acquisition while deficient | 5 | 10 | 7 | Reject |
| Free reposition / unassign while deficient | 0 | 0 | 6 / 0 | Accept |
| New Goblin on inactive floor, other defender inactive or in custody | 20 | 20 | 0 | Accept |

Focused tests compare each quote with an actual canonical draft commit. The original C2 fixtures also cover reserve-funded redeployment, authorized recovery deficits, fractional funding and identity continuity.

## Visible UI and refresh cost

The existing top HUD now has a wrapping, localized `manaReserve` label showing required recovery reserve and, when deficient, the existing funding-shortage explanation. No hover is required. Placement information also shows the prospective required reserve and rejection reason. Existing localization keys are reused; no English strings or gameplay tuning were added to runtime code.

The existing scrolling top chrome is bounded during content placement, and the map keeps at least one quarter of the safe height. This preserves the established compact layout constraints and leaves the existing details area scrollable. Small-screen/large-text header content can be reached through its existing vertical scroll surface. Normal desktop/portrait deficit captures show the reserve explanation directly.

The controller captures immutable recovery presentation rules once on initialization. Geometry changes recalculate the acquisition quote; wallet ticks call the existing `RefreshDraftPreviewBalance` and update just the acquisition information, without re-parsing canonical/configuration bytes or rebuilding the content browser.

## Qualification

| Final focused stage | Passed | Failed | Ignored | Other skipped | CLI exit |
| --- | ---: | ---: | ---: | ---: | ---: |
| EditMode attempt 02 | 259 | 0 | 0 | 0 | 0 |
| PlayMode attempt 03 | 5 | 0 | 0 | 0 | 0 |

All 65 production content gates passed within EditMode. All 16 new quote/commit cases passed. PlayMode exercised the new visible-affordability scenario, existing production content parity, measured placement layout, compact placement/localization overflow, and C2 consecutive runs/read-only replay. There are no unresolved test failures or skipped tests in this focused selection.

The final Windows x64 Development Build succeeded (Unity 6000.3.2f1, StandaloneWindows64, CLI 0, 0 errors, 1 warning). The warning is the established optional native-symbol upload failure caused by an empty service URI. The CLI reports the established nonfatal LicensingClient validation diagnostic; shutdown also logs debugger-port/Mono thread messages, while the actual BuildReport is Succeeded and process exits 0. The packed player contains recovery configuration, frozen migration limit profiles, localization and isolated save namespace `combat-c2-reserve-ui-20261016.json`.

All 1,324 qualification inputs matched after the final build. Unity-generated input differences were archived and refreshed by the established checkpoint helper (one after PlayMode; four after build). No gameplay/source change was made after successful final PlayMode/build.

All **15,957** preexisting save/recovery/player files, including the original C2 player, match their baseline hashes. All **1,312** warm-owner input/log/settings files match. No fixture restoration was necessary. The protected owner save remains SHA-256 `54c7744e7ee92b1597daa88df995ef07c120e34667398377be78591f7fb7aa13`; it was never reset, overwritten, migrated or restored. The original four C2 evidence files/archives are unchanged. The manifest records **371** unchanged gameplay/configuration inputs.

Exact commands are in [commands.md](commands.md). Start/end times, exit codes, source hashes and all intermediate failures are in [qualification-manifest.json](qualification-manifest.json) and [qualification-reports.zip](qualification-reports.zip). Local outputs:

- XML: `C:/Dev/Dungeon-Lord/TestResults/combat-c2-reserve-ui-02-EditMode.xml` and `combat-c2-reserve-ui-03-PlayMode.xml` in the same directory.
- Logs: `C:/Dev/Dungeon-Lord/Temp/combat-c2-reserve-ui-02-EditMode.log`, `combat-c2-reserve-ui-03-PlayMode.log`, and `combat-c2-reserve-ui-03-windows-build.log`.
- Player/report: `C:/Dev/Dungeon-Lord/Temp/ui-composition-combat-c0-diagnostics-validation-r2/Builds/Development/Windows-combat-c2-reserve-ui-20261016/Dungeon Lord.exe` and adjacent `build-report.json`.
- Local input/preservation/runner audits: `C:/Dev/Dungeon-Lord/TestResults/combat-c2-reserve-ui/`.

Raw logs remain local because Unity licensing startup logs may contain credential arguments.

Attempt 01 passed 259 EditMode tests and four of five PlayMode scenarios. Its compact long-localization portrait case failed the preexisting quarter-height map constraint (282 versus 320). Attempt 02 passed the same 259 EditMode tests and four of five PlayMode scenarios; preserving the map exposed insufficient details height at 1024×600 large text (43 versus 70). Neither assertion was weakened. The final layout uses the existing header ScrollView to bound top chrome as well as preserving map space. Attempt 03 passed all five affected PlayMode scenarios; EditMode source is unchanged from successful attempt 02 except stylesheet layout, exercised by these scene scenarios.

No unrelated full suite is rerun. Original C2 evidence remains 2,091 passed / 0 failed / 1 ignored EditMode and 3,518 passed / 0 failed / 10 ignored PlayMode, qualified before this presentation correction.

## Changed files and purpose

- `Scripts/Services/ContentAcquisitionEconomyPresenter.cs`: display the supplied canonical quote instead of a wallet-only affordability assertion.
- `Scripts/Services/SaveService.cs`: pass an optional canonical preview through the presentation method; transaction logic is unchanged.
- `Scripts/Services/ProductionDungeonPresenter.cs`: accept cached recovery presentation rules.
- `Scripts/UI/ProductionDungeonController.cs`: visible reserve label, cached rules and lightweight acquisition wallet refresh.
- `Scripts/UI/ProductionDungeonRoomContent.cs`: obtain/cache canonical candidate economy quotes and render their reasons separately from purchase verification.
- `UI/ProductionDungeon/Dungeon.uxml` / `Dungeon.uss`: reuse the HUD for visible wrapping reserve information and preserve compact scrolling/map layout.
- `Tests/EditMode/ContentAcquisitionEconomyTests.cs`: 16 quote/commit agreement cases and conservative unpositioned feedback assertions.
- `Editor/DungeonSpatial/Tests/CombatC2ReserveUiSceneTests.cs` / `.meta`: actual UIDocument, funding-refresh, separate gate and desktop/portrait default/large-text captures.
- `Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs`: discover the new scene scenario in the established PlayMode harness.
- `Editor/DungeonVisualBuildQualification.cs`: unique Windows build entry point that preserves the original C2 player.
- `Tools/Presentation/qualify_combat_c2_reserve_ui.py`: isolated namespace, exact-input refresh/checkpoints and separate preservation audits using established helpers.
- `Tools/Presentation/run_combat_c2_reserve_ui_qualification.ps1`: focused stages with recorded commands, strict XML verdicts and build checks.
- `Tools/Presentation/collect_combat_c2_reserve_ui_evidence.py`: separate report/archive, screenshot, unchanged-core and preservation evidence.
- This document, `commands.md`, `qualification-manifest.json`, `qualification-reports.zip`, and six screenshots: new correction evidence; original packet retained.

Asset paths above are relative to `Assets/_Project/`. Tool and document paths are repository-relative.

## Compatibility and owner acceptance still required

Schema 15, migration chains, monster continuity, recovery priority/readiness/costs, reserve obligations, canonical wallet/atomic-save behavior, B2 scheduling, C0 combat and C1 replay are unchanged. The manifest verifies unchanged tracked gameplay/configuration inputs; the SaveService diff changes only its presentation method signature/pass-through. No new writable authority, economic coefficient or localization framework exists.

Keep PR #240 draft. Independent ChatGPT review and blocking-finding resolution remain required. Owner Editor and this corrected Windows player must verify:

1. Goblin 24 rejects with visible reserve 6; 26 accepts and leaves 6. Skeleton 31 rejects / 32 accepts.
2. Existing-defender obligations restrict traps/loot; funding updates the open quote, and offline/pending verification remains a separate gate.
3. After recovery causes a deficit, total/usable/required mana and shortage are readable without hover. Free unassignment remains possible; custody/inactive floors do not add deployed obligations.
4. Desktop and portrait, including large text, keep readable reserve information, scrollable overflow, usable map and reachable placement/Save/Discard controls.
5. Perform the original parent document's C2 consecutive-run, recovery, save/reopen, identity/custody and replay UAT. Build/package success is not owner standalone acceptance or merge approval.
