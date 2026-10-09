# PR #230 translucent floor HUD refinement

Reviewed start: `4d3ed8b1efaf4b9613eaf68e32f6e359645c3ea2`. Local/GitHub main: `7b5911e5482aa20851e8329c0bb311f9aa6e7206`. Qualified implementation: `b85daa637cf500f66bf3cbd50b5782f979046e51`. Existing branch/PR continued, no merge. Classification: Standard, recommended GPT-6.1 Sol Medium; no model fallback. A0 Decision 6 and the current mobile UI vision govern this bounded styling correction.

## Exact source changes

- `Assets/_Project/UI/ProductionDungeon/Dungeon.uss`: floor-navigation-only overrides remove the opaque shared action image and full-box framing. Idle row background is dark RGBA alpha **0.56**, selected **0.68**, collapse **0.38**. Text/control opacity stays **1**. Rows use light left accents; selected state retains a thicker **3-unit gold edge and bold localized text**. Blocked floors retain their localized label and an additional bottom edge. Hover/focus/press raise only background prominence, using existing USS pseudo-states; focus also has a thin cyan top edge. No transitions, animations, opacity timer or state controller.
- `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonOverlaySceneTests.cs`: one targeted actual-scene fixture covers portrait/landscape, all text sizes, safe insets, expanded/collapsed views, selected active/inactive floors and actual edited/blocked draft state. It verifies full viewport width, minimum hit regions, ordering, opaque foreground/no action background, selected/blocked non-color edge cues, genuine touch and mouse floor controls, transparent padding picking, preserved camera across collapse and no navigation-generated canonical/mana/draft changes. Screenshots use this same seeded fixture before/after.
- `Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs`: adapter runs that fixture through the genuine PlayMode/Input System scene, alongside retained overlay, camera, construction, invalid/recovery and transactional tests.

Only these **three Unity source inputs** changed. Evidence collector `Tools/Presentation/collect_floor_hud_evidence.py` and this packet retain results. `Dungeon.uxml`, composition/controller, viewport/camera, visual catalog, imported artwork and gameplay sources are unchanged. No new assets/dependencies, strings, configuration authority, theme options or navigation architecture. Full-width safe-area overlay/picking/scrolling behavior is retained.

## Visual evidence and qualification

[Before/after frames](screenshots.md), `before-index.json` and `screenshot-index.json` identify actual Unity captures; no mockups. The reviewed stylesheet/runtime were captured with an added capture-only test fixture before styling changed. The focused-room view deliberately places stone beneath the rail, showing remaining texture detail through the new backgrounds. Both versions use the same authored state, camera operations, safe insets and text sizes.

See [qualification](qualification.md), actual XML/log archives, exact source inventories/output diffs, frozen Git input trees, owner-file hashes and complete Windows player manifest. Earlier overlay/readability and owner UAT packets remain historical and unchanged.

## Compatibility, limitations and focused retest

Schema **13**, migrations, save/draft formats, construction/economics, stable identities/order, floor/content definitions, simulation, run snapshots and all transaction/recovery authorities are unchanged. No owner primary save or draft evidence is used as a fixture. No root ProjectSettings, font or package changes.

Translucent controls remain interactive and therefore cover world input within their hit regions; empty overlay space passes through. Collapse and existing pan/zoom permit reaching covered geometry. Unoccupied surroundings can still appear flat because the world underneath is flat; focused captures show real stone through the HUD. Focus can briefly strengthen a surface; no hover-only action. Desktop screenshots/tests do not establish physical-device performance, font coverage or owner UAT. No FPS/allocation claim.

After external review, check floor-row legibility over both stone and surrounding rock; selected active/inactive, edited and blocked text; collapse/expand at all text sizes in portrait/landscape; floor scrolling and touch/click; empty-gap pan/zoom versus button interception; camera, placement and Save/Discard/recovery parity. Owner manual UI/Windows retest remains pending. Do not merge without external review and owner approval.
