# PR 231 owner-UAT correction qualification

Reviewed branch HEAD: `6afca9caa672f02eee03a2c0ec5fc09f43af1928`. Base/latest fetched main: `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch: `codex/phase7-graphical-room-content-management`. The initial working tree was clean, with no uncommitted owner work. This updates the existing PR; it does not create another PR or merge it.

Selected configuration: **GPT-6.1 Sol, High**. Classification: Standard correction packet with focused integration risk. The owner explicitly selected High; rendering, selection transitions and measured responsive layout span known presentation components and justify retaining it. Repository policy fallback is GPT-6 Sol, then GPT-5.6 Sol at the appropriate equivalent reasoning level if unavailable. No substitution was needed. AGENTS.md, the canonical model policy, Phase 7A0 Decisions 38–47/82–84, mobile UI vision, graphical tests and relevant transactional invariants were reviewed before editing.

Frozen implementation: `344a5edcf540ee6c0ed81c63be723a5f6a0a784e`. Later documentation/evidence commits leave the four Unity input trees identical. Full qualification results below apply to this implementation, including the rectangular-room orientation regression.

## Owner findings and corrections

The owner confirmed initial placement/confirmation, monster move/unassign/reassign, Save/reopen, Discard and durable draft recovery on the reviewed HEAD.

| Owner finding | Correction |
|---|---|
| Room interior had no legible placement grid | The existing `EditorGrid` is behind room textures. A bounded target-room overlay now renders above textures and below content, boundaries and doorway thresholds. Hollow lines use existing presentation-policy width/color, preserve transparent interiors and exact transformed cells, and clear at cancellation/floor/target changes. The overview grid remains unchanged. |
| Raw room-local coordinates distract from play | Selected-content inspection and preview text show localized identity/room context. Coordinates remain in canonical state, validation, replay and diagnostics; no coordinate contract changed. |
| Assigned identities clutter acquisition choices | Compact New/Owned controls filter definitions and returned custody separately. Assigned identities are selected on the map. Repeated monster purchases retain the original rules. |
| Returned owned content is hard to locate | Owned shows its returned count and identity-specific free-redeployment cards. Unassign opens Owned without choosing an identity. New never silently chooses an owned item. |
| Switching target rooms takes too many steps | New and returned choices retarget on a constructed-room tap, preserving option/category or AssignmentId and updating capacity/validity. Preview performs no journal, custody or wallet write. Existing Move stays within its room; Reassign remains explicit. |
| Landscape controls overlap information | Placement gives the existing bottom sheet a bounded height. Information scrolls in `contextDetails`; action rows do not shrink. Tests measure viewport/action/session separation at portrait, wide/narrow landscape and all three text sizes. |
| Some options share placeholder art | Existing Skeleton, Spike Trap and Glittering Hoard mappings match authored assets. Goblin, Chilling Sigil, Snare Trap, Hidden Cache and Basic Loot Node retain their permitted category fallbacks. No distinct authored asset for these five was found in the production catalog/art inventory. Tests bind the accepted fallbacks without changing IDs/names or creating an art pass. |

Placement displays one valid/invalid status with relevant cost/eligibility details and localized Add Monster/Add Trap/Add Loot headings. Browser Move controls are hidden. Selected objects highlight their actual content category. Resolution changes retain the contextual heading; selecting the map clears obsolete browser instructions.

## Review thread

[Stale occupied-cell preview comment](https://github.com/gdg3417/Dungeon-Lord/pull/231#discussion_r4239002779), thread `PRRT_kwDOQxUKE86rHSUp`, was unresolved at inspection. The invalid-room branch now clears the prior preview before showing the invalid anchor. Tests exercise two-cell valid→invalid→valid transitions, target-room changes, cancellation and real floor changes, and assert old grid/footprint cells disappear. Fix and qualification evidence are complete; the existing GitHub thread records its resolution after publication.

## Contracts and changed files

No gameplay, purchase, identity, custody, economy or save/draft authority is changed. Save schema remains **13**, draft journal format **5**, immutable record version **4**. Packages, ProjectSettings, ContentAuthoring and production gameplay/tuning tables are unchanged. Existing commit, exact-byte persistence/readback, recovery, current-run snapshots, restricted purchase gates and floor-knowledge behavior retain their original tests and authorities.

- `DungeonFloorWorldView`: target overlay, render ordering, bounded constructed-room hit projection and preview cleanup.
- `ProductionDungeonRoomContent`, `ProductionDungeonComposition`, `ProductionDungeonController`: separate browser state, preview retargeting, contextual labels, coordinate-free inspection, feedback lifetime and control visibility.
- `Dungeon.uxml` / `Dungeon.uss`: compact selector and bounded scrolling/nonshrinking placement actions in the approved bottom sheet.
- English string table: localized headings, selector labels, empty/hint text and identity/room presentation.
- Production scene tests, PlayMode adapters and visual-catalog tests: UAT regressions plus unchanged original assertions.
- Editor-only `DungeonVisualBuildQualification`: reuse the established build/packed-art checks with a separate correction output path, preserving the earlier owner build.
- Evidence collector: archives this correction in a new subfolder without rewriting the original qualification packet.

## Automated qualification

| Run | Passed | Failed | Skipped | Disposition |
|---|---:|---:|---:|---|
| Focused EditMode 01 | 48 | 2 | 0 | Corrected a test fixture's post-Discard grid budget and a real resolution-refresh heading reset; exact failed names/messages retained |
| Focused EditMode 02 | 50 | 0 | 0 | Content domain, new UAT scenes and authored/fallback mappings passed; screenshot review then strengthened the faint grid |
| Production PlayMode 03 | 40 | 1 | 0 | Removed a content status override that exposed construction's intentionally hidden status; original viewport-height assertion retained |
| Production PlayMode 04 | 41 | 0 | 0 | All production journeys passed, including original construction layouts and new UAT flows |
| Full final EditMode | 1,666 | 0 | 1 | Total 1,667; immediate post-test equality across all 1,208 inputs |
| Full final PlayMode | 3,088 | 0 | 10 | Total 3,098; generated TMP/settings changes rejected and archived, with no target-only files |

Both complete suites passed. Raw failed runs are retained; none is called a passing qualification. [Case/outcome comparison](test-outcome-comparison.json) confirms no existing case was removed or changed, including all skips. Final logs contain no C# compiler errors or warnings. Final screenshot/geometry evidence is captured by the production PlayMode journeys. The orientation fixture was strengthened from a square to a rectangular room after focused 04 and passed complete qualification; its deliberate source difference is archived alongside generated-output diffs.

The [Windows Standalone64 Development report](build-report.json) records **Succeeded, zero errors, one warning**, Unity 6000.3.2f1. The warning is an empty Unity Cloud symbol-upload URI; native symbols were not uploaded. [Exact messages](build-messages.txt) are retained. All 52 referenced dungeon art assets are packed. The [303-file artifact manifest](windows-artifact-manifest.json) hashes the corrected player, **181,826,080 bytes** including reports. Build completion follows both final suites. Raw build reports remain in `build-reports-original.zip`; human-readable copies trim Unity-generated trailing whitespace only.

[Seven final production-scene captures](screenshots.md) were visually inspected: legible hollow grid above masonry, separated New/Owned choices, retained owned identity in Room 2, no stale two-cell footprint after an invalid outside tap, coordinate-free selected inspector, portrait placement, and 1920×1080 Large-text placement with separated information/actions. These are actual final PlayMode captures, not a claim of completed manual Windows UAT.

## Isolation, strict inventory and disk strategy

The existing project `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation` was closed: no Unity.exe process, no UnityLockfile, imported Library present. Strict preflight matched all 1,208 inputs with zero target-only files. Its initial measured size was **2.506 GiB**. [Pre-reuse project inventory](project-inventory-before.json) records all eight existing validation project paths, approximate sizes and locks; no new imported copy was needed.

The four input roots remain Assets, Packages, ProjectSettings and ContentAuthoring, including generated metadata. Existing strict helper/namespace behavior is unchanged. [Fifteen source checkpoints](source-checkpoint-index.json) retain immediate pre/post complete-suite and build inventories. Post-EditMode matched exactly. Post-PlayMode rejected two expected TMP/settings serializations. Post-build rejected four expected URP/settings serializations with zero target-only files; strict preparation restored exact equality. Final verification is **1,208 inputs, zero mismatches, zero target-only files**. All per-file expected/actual hashes are archived in `source-inventories-and-diffs.zip`.

Evidence limitation: the post-build mismatch strings include `: mismatch`. A diff-export command mistakenly treated that suffix as part of each path, so those four textual diffs were not captured before restoration. Their exact expected/actual hashes and paths remain in the immediate post-build inventory. Focused/test-generated textual diffs and the deliberate rectangular-fixture change were captured. No unexpected input is ignored, deleted or quarantined.

The isolated filename remains `phase7-ui-composition-validation-01a11d08.json`; scene fixtures override it with unique test filenames before root initialization. [Fresh owner audit](owner-preservation.json) confirms all **eight** primary/validation save/sidecar and owner source-setting hashes and save-prefix filename inventories are identical. [Earlier-build audit](earlier-build-preservation.json) confirms all **303** files are unchanged. Owner project execution, primary save access for mutation and owner PlayerPrefs APIs are not part of qualification. This is a file audit, not a claim to hash the entire registry/editor preferences. Existing tests retain scoped fixture cleanup.

One new corrected Windows player is retained at `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation/Builds/Development/Windows-room-content-uat/Dungeon Lord.exe`. The earlier `Builds/Development/Windows` player is retained unchanged; it is historical owner UAT evidence, not a duplicate correction build. No project, cache, old build, evidence or owner file was deleted/quarantined. The seven other project copies and existing historical root builds remain cleanup candidates only, as inventoried in the original packet.

## Owner rerun and retained limitations

Rerun these presentation/interaction cases in the reused isolated Editor project and corrected Windows player, sequentially against the isolated save:

1. Select a room, enter Monsters/Traps/Loot and confirm New contains definitions only; Owned contains returned identities with free-redeployment labels.
2. Unassign an existing object, locate it under Owned, switch to New and confirm it is not silently selected; redeploy the chosen owned identity.
3. While placing new/owned content, tap another room, verify capacity/validity update and confirm the chosen identity/option remains. Tap outside rooms and verify invalid feedback with no old footprint. Recover with a valid tap.
4. Focus the target room and check the grid over masonry while zooming/panning. Cancel, change room/category/floor, and verify the placement overlay clears appropriately.
5. Select an assigned monster/trap/loot on the map. Confirm its actual category highlight and coordinate-free inspector; exercise Move, explicit Reassign and Unassign.
6. Check placement information and Cancel/Confirm/Save/Discard at 720×1280, 1920×1080 and narrower landscape with Small/Default/Large text. Information may scroll; action controls must remain reachable without overlap.
7. Confirm the draft remains separate from Save Changes; save/reopen one corrected journey and discard another. Owner already confirmed basic persistence/recovery; automated suites recheck their edge cases.

The five accepted fallback options still share category artwork and require a later bounded art-authoring task for distinct identities. No right-side inspector, visual theme framework, new content/lifecycle/balancing, schema/draft revision or broad UI refactor is included. Automated qualification is complete and the correction is ready for external review. Targeted owner Editor/Windows rerun remains required; the PR is not merged. No new specification approval is required.

[Final disk inventory](disk-inventory-after.json): reused project **2.682 GiB**, imported Library **2.220 GiB**, corrected player **0.169 GiB**, preserved earlier player **0.169 GiB**. No Unity.exe process or UnityLockfile remained after qualification. The additional player explains most of the increase from the pre-reuse size; no new imported project was created.
