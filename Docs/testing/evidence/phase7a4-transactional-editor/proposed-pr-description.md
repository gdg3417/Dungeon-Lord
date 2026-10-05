# Phase 7A4: Add transactional editor authority and production Dungeon foundation

Starting baseline: **64d31257f06d1dafbf49593ade88f32b808bd556**, including PR #225 governance. Depends on the approved A0 design and merged/qualified A1–A3, including PR #224. This adds the first production Dungeon/Edit Mode foundation and one graphical mutation: select an existing monster, trap or loot assignment, choose Move, then tap an exact valid tile in the same room.

## Behavior and authority

New runtime UIDocument/UXML/USS/PanelSettings chrome provides four Normal HUD metrics, floor navigation, Edit Mode capacity, contextual Move, Save/Discard, recovery/failure surfaces and Small/Default/Large text. A selected-floor-only Grid/Tilemap plus pooled SpriteRenderer world remains presentation-only. Input System taps, pan, pinch, mouse equivalents and Focus use a testable viewport boundary. Bootstrap uGUI/TMP, its EventSystem/InputSystemUIInputModule and legacy development capabilities remain; the new shell does not retire them.

Canonical schema **13 remains the only gameplay authority**. No migration, canonical field, gameplay tuning or third-party package was added. Existing identities, assignment/category/option/sequence, room, custody and unrelated fields are preserved. Reposition costs/refunds zero mana. Simulation, passive mana, lifecycle and already-active immutable run snapshots never consume the draft; later runs use committed positions.

The independent version-2 whole-dungeon draft is a sparse ordered delta journal. Immutable version-1 candidate generations have separate SHA-256-bound commit records binding session, relevant canonical baseline, sequence and predecessor. Candidate bytes alone cannot recover as commands. Previously committed generations remain available; correctness requires no destructive rollback or multi-file atomicity claim. The live sequence advances only after successful write/boundary/readback. Unknown outcomes block Save Changes without gameplay/economic mutation. Restart may resolve an unknown result from a complete predecessor chain, otherwise recover its proven predecessor or fail closed. Stale drafts never merge/rebase. Failed discard blocks live saving and never claims durable deletion. Original stop evidence and storage probes are preserved alongside the approved resolution.

Save Changes rechecks durability, current baseline/session and durable replay, validates current spatial/production semantics, reconciles the supported zero-cost mutation, captures current recognized runtime/economy state into a detached complete-save candidate, then uses the existing complete-save validation, atomic persistence and durable readback before runtime publication. Canonical failure applies nothing and retains the draft. Existing material floor-knowledge applicability is preserved; successful commit with failed draft cleanup leaves stale evidence unable to reapply.

Stable IDs, ordinal ordering, bounded production workload limits, exact coordinates and configuration-owned geometry/occupancy remain authoritative. All new player text uses **41 stable Bootstrap localization entries** and existing localization/label/mana/heat presentation seams. Longer test-localized labels exercise wrapping. No Unity Localization or Japanese translation was added. Safe-area chrome, centralized physical interaction-target conversion (48 Android dp / 44 iOS points) and three text modes are tested; device qualification and owner usability remain outstanding.

## Automated validation

- Final Phase 7A4 EditMode: **63 passed, 0 failed, 0 skipped** (37 domain/presenter/probe cases, 23 durability cases, 3 actual-scene cases). Retained probes: 5/5; corrected original reproduction: 1/1. Genuine shell PlayMode: 4/4, including real Input System two-touch injection and chrome-origin blocking.
- Canonical save/load/session/complete-save focused integration: **160/160**. Full-suite relevant coverage: A2 **44 passed, 1 established skip**; A3 **36/36**; Phase 4 **736/736**; Phase 6 **223/223**.
- Full EditMode: **1,472 total — 1,471 passed, 0 failed, 1 skipped**.
- Full PlayMode: **2,915 total — 2,905 passed, 0 failed, 10 skipped**.
- Both exact skipped-test sets match the retained Phase 7A3 baseline. No skip, ignore or failure reclassification was added.
- Explicit production gates: **274/274** (build gate 65, export 112, recovery 57, loading 37, actual scene 3). Real production pre-build gate also passed in the player build.
- Required `DevelopmentBuildUtility.BuildWindowsDevelopment`: **Succeeded**, StandaloneWindows64 Development, Bootstrap-only, Unity 6000.3.2f1; **0 build errors, 1 warning** for unavailable native-symbol cloud-upload credentials. Complete build size: 171,638,812 bytes.
- `git diff --check`: passed. Owner Packages and ProjectSettings remain unchanged.

Validation used the established isolated checkout with the same baseline and intentional changes; owner saves were not used. XML, build logs/report and four inspected representative portrait/landscape/tablet/long-localization PNGs are retained in established ignored evidence locations. Repository-owned capture uses Unity APIs with bounded stable-state warm-up; no visual-regression dependency or strict pixel-golden gate was introduced. See the repository Phase 7A4 evidence for exact test/localization/skip inventories, earlier failures, final protocol and owner UAT instructions.

## Scope and remaining qualification

External review and owner visual/gameplay UAT are still required after blocking review findings are resolved. Automated success is not a ready-to-merge claim. Windows standalone presentation, mobile feel, native Android/iOS bridge behavior and low-end performance remain manually unqualified; Device Simulator alone is not multi-touch proof. Existing filesystem platform qualification was not expanded. Total and Usable Mana currently read the same spendable-wallet authority because no separate live reservation owner exists. Primitive artwork, a bounded journal command envelope and Bootstrap coexistence are deliberate limits.

Non-goals: graphical room/corridor/branch construction or structural edits; cross-room reassignment; acquisition/shop/custody placement; drag reposition; adventurer/run animation or speed controls; final art/effects/audio; complete Research/Analysis/More; Japanese translation or full screen-reader hierarchy; Bootstrap retirement; new tuning/save fields/schema/migration; Figma or third-party runtime packages. No new floors beyond current production content are invented.

The planning correction is limited to current status/baseline text: A3/#224 merged and qualified, schema 13, #225 governance included, transactional editor/production Dungeon now active. Historical A1/A2/A3 evidence and unrelated A0 decisions remain unchanged.
