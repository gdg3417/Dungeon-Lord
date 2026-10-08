# Phase 7 production Dungeon composition and first visual treatment

Starting local and fetched GitHub `main`: `7b5911e5482aa20851e8329c0bb311f9aa6e7206`. PRs #226–#229 were verified merged; #229 is the starting baseline. One new branch: `codex/phase7-dungeon-ui-visual-composition`. External review and owner UAT remain pending. This is the first presentation pass, not final art or release qualification.

Recommended configuration: GPT-6.1 Sol, High; Complex. The established authorities and design locks make this a well-specified UI/world integration. No model substitution or delegation was used. Canonical policy: `Docs/process/AI_Model_Selection_Policy.md`.

## Composition and interaction

The existing UI Toolkit document, controller, transactional construction partial, presenter, world view and viewport remain the integration points. A narrowly scoped controller partial owns read-only composition and camera actions. Four persistent metrics consume the existing presenter; Total and Usable Mana still share the spendable wallet. The vertical floor rail descends by canonical floor index and includes actual lifecycle, changed-draft and invalid-intent indicators. It collapses without inventing floors or lifecycle actions.

Normal Mode opens to a floor summary: actual lifecycle, room count, required-route validation and floor-space usage. Room and content taps retain canonical selection precedence and open read-only details. Physical corridor cells also support read-only inspection. Selection preserves camera composition. Focus Room explicitly fits selected resolved room geometry; Fit Floor restores overview using the existing DungeonViewport. Neither operation issues draft commands. The grid remains Edit-only.

When Bootstrap publishes the starter geometry after an empty initialization, the Normal overview now refits the current floor. Explicit pan/zoom/focus still retains its camera intent. The genuine-input fixture exposed this presentation issue: the old empty-floor zoom could leave the newly authored room outside the tappable viewport. The correction changes camera composition only, with no first-room domain changes.

One shared bottom sheet serves summary, inspection, category browsing, movement, previews, correction and recovery. Landscape keeps that sheet below the same viewport. Internal header and placement columns adapt horizontally; the old right-side construction panel rule is removed. Collapse hides optional scrollable details and recovers map area, while local placement and whole-dungeon actions stay outside that scroll view. Normal and Edit states have localized labels and amber/cyan treatments. Only Rooms offers current construction behavior. Other room-context categories are disabled with a localized deferred explanation.

Local Confirm Placement/Keep Invalid Attempt retains the durable, non-authoritative draft protocol. Final Save Changes still requires the existing review/confirmation and canonical publication. Bootstrap diagnostics remain available, including a return control. Empty canonical state explains the retained starter dependency.

## World and ownership

Imported stone surfaces, corridor paving, low exposed-edge trim, surrounding rock, entrance stairs and cyan completion crystal replace prototype fills. Edge trim follows the union of actual constructed cells; adjacent structures and actual corridors retain open joins. It communicates constructed extent without inventing doors, impassable walls or routes. Selected-room outline follows resolved footprint cells, including future nonrectangular silhouettes, rather than supplying gameplay geometry.

`DungeonVisualCatalog` owns only sprites and category/option identity mappings. It never supplies prices, occupancy, dimensions, behavior or save fields. Actual skeleton, spike and glittering-hoard identities resolve dedicated art; unmapped options use distinct monster, trap and loot fallbacks. Arrays do not determine authored identity. Fixed structures use existing kind/geometry. Content centers and occupied-tile hit testing remain canonical room-local transforms.

Reusable Tile objects are cached, content and fixed-structure renderers are pooled, and only the selected floor is reconstructed. Resource ticks refresh informative values without replaying floor geometry. Decorative objects and textures are not generated every frame.

## Compatibility and deferred work

No save schema, migration, simulation, catalog tuning, domain pricing, occupancy, draft journal, durability protocol, transaction or active-run authority changed. Writable schema remains 13. Existing invalid-intent presentation bounds and camera reachability remain intact. No Bootstrap retirement, first-room domain construction, floor lifecycle controls, graphical content assignment, research/analysis/more screens or future art catalog is implemented.

New UI strings are in the English string table with the existing language fallback and number formatting. Small, Default and Large text, native minimum hit targets and safe-area transforms use the existing typed presentation policy. Physical mobile device, low-end GPU and native Japanese font qualification remain future work; desktop results do not establish mobile performance.

See [asset inventory](assets.md), [screenshots](screenshots.md), [qualification](qualification.md) and [owner UAT](owner-uat.md) for concrete evidence and limits.
