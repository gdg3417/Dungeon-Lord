# Phase 7A0 - Graphical Dungeon Editor Design Lock

**Status:** Proposed Phase 7A0 implementation authority; owner decisions resolved and awaiting review/merge through this documentation PR.
**Purpose:** Record the approved owner decisions, their rationale where useful, implementation constraints, dependencies, deferred work, and explicit supersedences for Phase 7.
**Implementation remains deferred to later Phase 7 implementation PRs.** This document is reconciled against merged Phase 6A5B / PR #220 at `main` `e9f93b8d742ccaba38c7de32b776970006791d93`; it does not claim that graphical-editor, positional-save, simulation, or production-UI work already exists.

## Final Phase 6 baseline and authority boundary

Phase 6 is complete. The Phase 7 starting baseline is writable save schema **12**, atomic floor-lifecycle authority, contiguous Active-floor prefix semantics, immutable run-start Active-floor snapshots, and production content containing Floors 1 and 2 only. Current runs are unaffected by later lifecycle/editor changes. Durable shared floor knowledge uses applicability fingerprints: lifecycle activation alone does not invalidate it, while material gameplay-affecting floor changes make stored knowledge inapplicable until fresh qualifying evidence replaces or reconfirms it. Knowledge-backed EXIT/DESCEND perception is available; durable run history contains coarse facts, while detailed transition causes are same-session/transient.

All numeric tuning remains configuration/content-owned. Later repository evidence may require implementation sequencing adjustments, but may not silently change these approved decisions.

## Governing intent

Phase 7 must establish the graphical build-run-inspect-revise loop without creating duplicate gameplay authorities. The editor should consume canonical spatial, economy, lifecycle, validation, localization, simulation, and save authorities. Player-visible editing must be mobile-first, portrait-friendly, deterministic, understandable, and forward-compatible with later dungeon complexity.

The normal dungeon view and Edit Mode are deliberately distinct:

- **Normal / Inspect / Run view:** presents the dungeon as an actual living dungeon, without the editor tile grid. Rooms, corridors, traps, loot, monsters, and adventurers are visualized as dungeon content.
- **Edit Mode:** exposes the canonical spatial grid, placement footprints, validity, draft changes, capacities, route information, and editing controls.

A recurring forward-compatibility rule applies throughout this record: MVP may use a narrower authoring subset, but runtime/editor architecture must not hardcode that subset as a permanent universal limitation when later systems are already expected to expand it.

---

# Group 1 - Editor Interaction Foundation

## Decision 1 - Explicit Edit Mode

**Approved design lock:** Normal/Inspect Mode plus explicit Edit Mode.

Normal mode focuses on observing and understanding the dungeon. Edit Mode clearly communicates that the player is modifying a draft of the dungeon.

**Implications**
- Reduces accidental editing while inspecting runs or dungeon state.
- Gives the UI a clean place for grid, placement, validity, and editing tools.
- Supports later transactional draft behavior.

## Decision 2 - Placement interaction

**Approved design lock:** Tap-first placement is canonical. Dragging is supported as a convenience path.

Canonical flow:
1. Select content/room.
2. Tap candidate location.
3. View preview and validity.
4. Commit placement into the draft.

Dragging must ultimately invoke the same domain command/result as tap placement.

## Decision 3 - Tile-grid presentation

**Approved design lock:** The canonical tile grid is shown in Edit Mode and hidden outside Edit Mode.

In Edit Mode:
- Show canonical grid and room/corridor footprints.
- Highlight valid, invalid, blocked, occupied, or otherwise relevant cells.
- Grid remains visually subordinate to the dungeon objects.

Outside Edit Mode:
- Hide the editor grid.
- Show the actual dungeon floor plan, rooms, corridors, monsters, traps, loot, and later adventurer movement/combat presentation.

## Decision 4 - Camera

**Approved design lock:** Auto-fit initially, constrained pan and pinch zoom, plus reset/focus. No camera rotation for MVP.

## Decision 5 - Contextual action surface

**Approved design lock:** Selecting a dungeon object opens a contextual bottom panel/sheet rather than radial or floating action menus.

The bottom panel should be thumb-accessible, localization-friendly, expandable, and able to expose costs, capacities, validity, consequences, and actions without cluttering the map.

---

# Group 2 - Floors and Floor Lifecycle

## Decision 6 - Floor navigation

**Approved design lock:** Vertical floor selector, descending physically down the UI.

Order is Floor 1 at the top, then Floor 2, Floor 3, etc. below it, reinforcing descent into a dungeon rather than upward tower construction.

The selector should be collapsible. Presentation may become more opaque while actively used and more transparent/subdued while idle. A dedicated user-controlled opacity setting is not required for MVP unless testing shows it is necessary.

## Decision 7 - Locked-floor visibility

**Approved design lock:** Show existing/revealed floors plus only the next unavailable floor.

Do not expose every possible future floor count. This supports future dungeons with far more than five floors without spoiling long-term depth.

## Decision 8 - Lifecycle-state presentation

**Approved design lock:** Use explicit text plus visual treatment/iconography. Never rely on color alone.

Expected states include concepts such as:
- Locked
- Available / Not Constructed
- Inactive
- Active

## Decision 9 - Lifecycle actions

**Approved design lock:** Floor selector is navigation only. Construct / Activate / Deactivate actions live in the selected floor's contextual information surface.

The UI must consume lifecycle/eligibility authorities rather than reimplementing eligibility rules.

## Decision 10 - Unconstructed-floor presentation

**Approved design lock:** An unlocked but unconstructed floor uses a dedicated construction-preview state, not an empty editable grid.

It should communicate that the depth is available but not yet constructed, along with relevant construction cost and baseline characteristics.

---

# Group 3 - Editing Commands, Transaction Model, Validation, and Destruction

## Decision 11 - Placement confirmation behavior

**Approved design lock:** Default Preview + Confirm behavior, with an optional Quick Placement mode for experienced players.

Both modes modify only the draft while in Edit Mode. Neither directly spends resources or changes the committed dungeon.

## Decision 12 - Transactional Edit Session

**Approved design lock:** Edit Mode operates on a non-authoritative draft. Canonical dungeon state remains unchanged until the player commits the final draft.

While editing, the player may freely:
- place, move, rotate, and remove rooms,
- alter corridors,
- assign/reassign/remove monsters, traps, and loot,
- enter temporarily invalid intermediate states,
- inspect projected costs/refunds/capacities/validity.

No draft operation directly spends mana, changes structural investment, changes canonical custody, changes canonical layout, or changes active simulation.

Exit behavior:

**Valid changed draft**
- Save Changes
- Discard Changes
- Continue Editing

**Invalid changed draft**
- Continue Editing
- Discard Changes

**No changes**
- Exit directly.

## Decision 13 - Invalid placement behavior

**Approved design lock:** Invalid placements remain visible in the draft and explain exactly why they are invalid.

Examples:
- overlap,
- outside bounds,
- capacity exceeded,
- invalid connection,
- missing requirement.

Invalidity blocks canonical commit, not continued experimentation.

## Decision 14 - Destructive changes

**Approved design lock:** Consequence-sensitive confirmation/preview. Destruction modifies only the draft until final commit.

High-impact deletion should explain material consequences such as disconnected corridors or content returning to custody. Discarding the draft restores the original canonical dungeon because the canonical dungeon was never changed.

## Decision 15 - Validity required to commit

**Approved design lock:** The player may exit Edit Mode only by either committing a valid-for-purpose draft or discarding it. An invalid draft can never be committed.

Validity is lifecycle-sensitive:
- Active floors must satisfy the validity required for active/run-capable state.
- Inactive constructed floors may persist incomplete but construction-valid layouts and do not need to be activation-eligible.
- Structurally illegal state may never become canonical.

---

# Group 4 - Draft Persistence, Recovery, and Commit Economics

## Decision 16 - Draft storage

**Approved design lock:** Durable local, non-authoritative draft storage separate from canonical gameplay state.

The draft must never be consumed by simulation, passive mana, active-floor authority, run resolution, or canonical gameplay systems.

## Decision 17 - Recovery after crash/interruption

**Approved design lock:** On reopening, load the canonical dungeon normally and surface the recovered draft separately.

Player choices:
- Resume Editing
- Discard Draft

Recovery never implicitly commits or activates a draft.

## Decision 18 - Stale recovered draft

**Approved design lock:** A recovered draft can safely resume only against the same relevant canonical spatial baseline from which it was created.

Normal same-device crash/reopen with unchanged canonical dungeon should resume safely.

If a newer canonical layout exists, a migration materially changes the relevant spatial baseline, or another authority changed the committed dungeon, the old draft must not silently overlay or merge into the newer state.

MVP does not require automatic three-way merge/rebase.

**Future saved-layout note:** Post-MVP named dungeon-layout slots are a separate feature. A future saved layout containing fewer floors may reasonably replace only matching saved floors and leave later current floors unchanged, rather than deleting newer progression. Exact semantics remain deferred.

## Decision 19 - Commit economics

**Approved design lock:** Economic consequences are calculated only from canonical starting state to final draft.

Intermediate edit history has no gameplay or economic authority.

Examples:
- A -> B -> C is evaluated as A -> C.
- A -> B -> A produces no final structural change.
- Experimenting with multiple corridor layouts and returning to the original produces no committed corridor change.

## Decision 20 - Insufficient resources at commit

**Approved design lock:** Drafting reserves and spends nothing.

At Save Changes:
1. Revalidate final draft.
2. Recalculate authoritative economic consequences.
3. Check current usable resources.
4. If valid and affordable, apply all changes atomically.
5. If invalid or unaffordable, apply nothing and preserve the draft.

The player may create an aspirational draft they cannot yet afford.

## Decision 21 - Whole-dungeon draft and atomic commit

**Approved design lock:** One seamless whole-dungeon editing session, not separate per-floor edit transactions.

A player may edit multiple floors in one session and commit once.

Commit is atomic across all changed floors:
- all changed floors and associated resource/ownership consequences succeed, or
- none succeed.

Any blocking issue prevents the full commit and must identify the affected floor and location.

Draft storage should be sparse/delta-oriented so untouched floors do not need unnecessary duplication. This avoids one independent draft save per floor even if the dungeon later contains dozens or hundreds of floors.

Future researchable/named dungeon-layout save slots are explicitly deferred beyond MVP, though the draft architecture should not preclude them.

---

# Group 5 - Cost Preview and Commit Review

## Decision 22 - Economic preview detail

**Approved design lock:** Show projected net economic result prominently with expandable detail.

## Decision 23 - Gross costs and refunds

**Approved design lock:** Commit review separately exposes gross spending, refunds, and final net result. Do not hide refunds inside one unexplained net number.

## Decision 24 - Final commit review

**Approved design lock:** Every non-empty MVP draft gets one final commit review before atomic save.

Post-MVP quick-commit behavior may become configurable/progression-enabled, but ownership of that future convenience is not locked to the research tree.

## Decision 25 - Commit blockers

**Approved design lock:** Structured blocker list grouped by floor, with direct navigation to the affected location.

Tapping a blocker should:
1. switch to the floor,
2. move/zoom the camera to the issue,
3. select/highlight the relevant object or footprint,
4. show the localized reason.

## Decision 26 - Changed-floor indicators

**Approved design lock:** Floors with draft differences receive a subtle edited indicator in the floor selector. Blocking floors receive a distinct warning state.

Indicators reflect final canonical-to-draft difference, not raw gesture/action count.

---

# Group 6 - Manipulating Existing Dungeon Objects

## Decision 27 - Moving rooms

**Approved design lock:** Explicit Move action is canonical; direct dragging is a convenience path. Both must produce the same draft mutation.

## Decision 28 - Room rotation and future geometry

**Approved MVP design lock:** Explicit grid-based rotation through supported canonical orientations.

No free-angle rotation is required for MVP.

**Forward-compatibility requirement:** MVP rectangular, grid-aligned room geometry is an authoring subset, not a permanent universal contract. Architecture must not permanently assume every future room is an axis-aligned rectangle or every corridor is orthogonal. Future content may include circles, triangles, irregular shapes, angled corridors, curved corridors, and other supported transforms.

## Decision 29 - Room duplication

**Approved design lock:** Structure-only duplication is acceptable as an optional Phase 7 convenience.

Duplicate may copy room structure/type/footprint/orientation intent but must not clone persistent monster/trap/loot identities. This convenience may be deferred if it expands scope.

## Decision 30 - Multi-select/group manipulation

**Approved design lock:** No multi-select/group movement for MVP. This is a post-MVP capability candidate.

## Decision 31 - Corridors when rooms move

**Approved design lock:** Preserve existing corridor geometry only when it remains valid. Otherwise disconnect/flag the connection and require player correction. Never silently invent a new path.

---

# Group 7 - Corridor Placement and Route Editing

## Decision 32 - Creating corridors

**Approved design lock:** Tap-to-connect is canonical. Drag-to-connect is an optional convenience path.

## Decision 33 - Corridor path authority

**Approved design lock:** The editor may generate a deterministic suggested path, but the player retains final authority to reshape it before adding it to the draft.

The suggestion is convenience, not gameplay authority.

## Decision 34 - Corridor reshaping

**Approved design lock:** MVP uses segment/bend manipulation rather than requiring tile-by-tile editing.

## Decision 35 - Incomplete corridor state

**Approved design lock:** An actively drawn incomplete corridor is an interaction preview, not a persisted draft entity. It becomes part of the draft only after valid endpoints are connected.

## Decision 36 - Corridor intersections and future topology

**Approved MVP design lock:** Ambiguous corridor overlap/crossing is invalid. Intersections require explicit connection semantics.

**Forward-compatibility requirement:** This is an MVP editor/content limitation, not a permanent rule that dungeon graphs can never contain junctions, loops, crossings, bridges/tunnels, or complex maze topology.

Post-MVP dungeon design is expected to support more complex topology, potentially enabling:
- maze-like floors,
- multiple routes,
- loops,
- dead ends,
- backtracking,
- navigational mistakes,
- adventurers becoming lost or giving up,
- intersections that connect,
- crossings that geometrically overlap but do not connect.

Exact future behavior is not locked here.

## Decision 37 - Required/optional route visualization

**Approved design lock:** Normal corridors look like actual dungeon corridors. Route classification, optional branches, disconnections, and validity are shown through Edit Mode or route overlays rather than permanent debug coloration.

---

# Group 8 - Monsters, Traps, Loot, and Editor Ownership

## Decision 38 - Authoritative intraroom positioning

**Approved design lock:** Exact intraroom placement is authoritative gameplay state.

### Traps
- Exact stationary room-local position.
- Placement matters to gameplay.
- Remains stationary unless edited.

### Loot nodes
- Exact stationary room-local position.
- Placement matters to gameplay.
- Supports deliberate arrangements such as defended loot in a corner or deeper area of a room.

### Monsters
- Exact configured room-local starting position.
- Starting position is durable dungeon-layout state.
- During an encounter, monsters may move according to combat/behavior logic.
- Runtime monster movement is transient encounter state and does not rewrite the configured dungeon layout.
- Future encounters begin from configured starting position unless a future system explicitly changes that rule.

**Repository impact already identified:** Current schema-12 room content assignments persist identity, room, category, option, and sequence but not room-local coordinates. This decision requires an intentional domain/save extension and explicit versioned migration before exact room-local placement becomes authoritative. This design lock deliberately does not reserve or declare the next schema number. Stable assignment identity and canonical ordering must be preserved.

**Design integrity rule:** Player-controlled tactical positioning must have gameplay effect. Do not expose exact positioning as cosmetic-only fake choice.

## Decision 39 - Adding room content

**Approved design lock:** Room-first contextual management for MVP. Select a room, then manage Monsters, Traps, and Loot through the room's contextual interface.

## Decision 40 - Content appearance in Edit Mode

**Approved design lock:** Keep graphical content visible and layer selection/editor affordances over it. Edit Mode should not replace the dungeon with icon-only abstractions.

## Decision 41 - Moving/reassigning room content

**Approved design lock:** Explicit Move/Reassign is primary. Direct drag between rooms may be added as an optional convenience later.

## Decision 42 - Capacity presentation

**Approved design lock:** Compact capacity normally. Show category-specific capacity when the relevant content-management panel is active.

## Decision 43 - Unified draft transaction

**Approved design lock:** Structural edits and content edits participate in the same whole-dungeon atomic draft.

Discard Draft restores the complete pre-edit dungeon design, including room/corridor/content assignment changes.

---

# Group 9 - Intraroom Spatial Rules

## Decision 44 - Content coordinates

**Approved design lock:** Room-local coordinates, not floor-global coordinates.

Floor position is derived from room transform plus local content position.

This allows room movement and rotation to preserve designed encounters and better supports future non-rectangular rooms.

## Decision 45 - Moving a room with content

**Approved design lock:** Contents retain room-local positions and move with the room.

## Decision 46 - Rotating a room with content

**Approved design lock:** Contents rotate deterministically with the room, preserving the encounter arrangement as a unit.

Future irregular room definitions may support only their authored valid transforms rather than assuming arbitrary rotation.

## Decision 47 - Content overlap rules

**Approved design lock:** Data-driven, category-specific placement/occupancy rules.

Do not hardcode one-content-object-per-tile forever. Future traps or environmental objects may attach to floor, wall, ceiling, or other surfaces and may have authored overlap rules.

## Decision 48 - Placement must affect MVP gameplay

**Approved MVP end state:** Exact intraroom placement has gameplay consequences.

At minimum, materially different valid arrangements must be able to produce materially different deterministic encounter behavior.

Example designs that should not be mechanically identical by MVP completion:
- loot beside entrance, traps elsewhere, monsters distant,
- loot deeper in room, traps defending approach, monsters positioned to intercept.

**Phase 7 ownership boundary:**
Phase 7 should implement the authoritative placement/persistence/editor foundation and enough deterministic intraroom encounter behavior to prove placement matters. Phase 7 does not need to implement every future tactical-combat behavior. Phase 9 should validate, balance, optimize, and test player comprehension rather than introducing spatial consequence for the first time.

---

# Group 10 - Minimum Intraroom Gameplay Contract

## Decision 49 - Intraroom navigation model

**Approved design lock:** Adventurers and monsters navigate the actual room-local traversable spatial representation for MVP rather than a disconnected lane/waypoint abstraction.

For rectangular MVP rooms this may be a bounded traversable grid. Future irregular geometry may expose an equivalent traversable representation without changing the gameplay concept.

## Decision 50 - Trap placement/effect geometry

**Approved design lock:** Data-driven trap placement and effect geometry.

MVP traps may be simple, but runtime/editor architecture must not permanently assume every trap is one floor tile triggered only by stepping on it.

Future examples may include:
- floor traps,
- wall traps,
- ceiling traps,
- magical wards,
- larger trigger/effect footprints.

## Decision 51 - Loot-node interaction

**Approved design lock:** Adventurers must physically reach/interact with loot nodes to obtain loot. Entering a room does not automatically resolve all loot regardless of placement.

## Decision 52 - Monster starting position and movement

**Approved MVP design lock:** Monster begins at configured starting position and moves deterministically toward appropriate targets after engagement behavior triggers.

Full tactical AI with richer formation, line-of-sight, patrol, targeting, and retreat behavior is a post-MVP candidate, not a Phase 7 requirement.

## Decision 53 - Initial adventurer pathing sophistication

**Approved Phase 7 design lock:** Deterministic shortest legal path to the current objective.

Navigation API must remain extensible so later adventurer behavior can score multiple legal routes based on intelligence, knowledge, risk tolerance, trap awareness, personality, or other systems without replacing the spatial foundation.

Future maze gameplay may include getting lost, backtracking, choosing poorly, or giving up, but those behaviors are not Phase 7 requirements.

---

# Group 11 - Normal Mode and Run Visualization

## Decision 54 - Simulation versus presentation authority

**Approved design lock:** Authoritative deterministic simulation produces a run/event record or equivalent authoritative state; graphical movement/animation visualizes that authority.

Animation timing, frame rate, camera, presentation speed, or device performance must not become gameplay authority.

## Decision 55 - Visible-floor run rendering and future speed controls

**Approved design lock:** Only the currently viewed floor renders/animates its applicable adventurer run/party. Offscreen floors do not maintain invisible graphical animation.

Long-term architecture must allow multiple parties/runs to exist across different floors even though Phase 7 does not need to implement full high-concurrency dungeon traffic.

For MVP Phase 7, normal run speed is sufficient.

Future simulation/run acceleration may come from progression such as research, premium-currency time acceleration, or another approved system. Exact ownership is deferred. Any future speed modifier must not change deterministic decisions/outcomes for otherwise equivalent run state.

Presentation skipping and actual simulation acceleration are separate concepts and should not be conflated.

## Decision 56 - Run camera

**Approved design lock:** Smart follow by default, manual pan/zoom override, and explicit Follow/Recenter action.

When several floors eventually contain parties, the camera follows only the party on the currently viewed floor.

## Decision 57 - Editing while runs are active

**Approved design lock:** Editing is allowed while runs are active because current runs use their run-start authority/snapshot and committed changes affect subsequent runs only.

Player may:
- enter Edit Mode during a run,
- create a draft,
- commit a valid/affordable draft while runs are active.

Existing active runs remain unchanged. Future runs use the newly committed canonical dungeon.

This direction is consistent with the Phase 6 rule that active edits affect subsequent runs and lifecycle changes affect future runs only.

## Decision 58 - Required graphical fidelity

**Approved design lock:** Functional sprite/representation-based visualization sufficient to understand encounter behavior, not production-quality combat animation.

Phase 7 should visibly communicate at least:
- adventurer movement,
- monster starting positions and engagement,
- trap triggers,
- health/damage,
- loot reaching/acquisition,
- deaths,
- corridor traversal,
- future floor transition presentation as Phase 6 authority permits.


---

# Group 12 - Concurrent Runs and Editing During Active Runs

## Decision 59 - Active-party indicator in floor selector

**Approved design lock:** Use a compact active-party/activity indicator attached to the existing floor control.

The indicator must not increase the normal selector width or create persistent text rows. It should remain subordinate to floor navigation so the selector stays viable on a portrait phone and can scale to many floors.

When the selector is collapsed, it does not need to enumerate every active floor. A compact aggregate activity affordance may be used if later UX testing shows it is useful.

## Decision 60 - Offscreen run rendering

**Approved design lock:** Runs on floors that are not currently viewed continue through authoritative simulation without graphical rendering or invisible animation.

When the player views a floor, presentation reconstructs the appropriate current visible state from authoritative run/event state. Camera visibility must never become simulation authority.

This is a scalability and mobile-performance requirement, especially for the long-term possibility of many floors and multiple parties.

## Decision 61 - Switching floors during runs

**Approved design lock:** Floor switching is immediate.

The previously viewed floor stops rendering its active run. The newly selected floor renders its applicable current run state, if any. Underlying simulation continues independently of which floor is viewed.

## Decision 62 - Entering Edit Mode on a floor with an active run

**Approved design lock:** Hide/suspend that floor's active-run visualization while Edit Mode is open, but do not pause or alter the authoritative run.

The editor may show a compact informational state indicating that an active party is using the pre-edit/run-start dungeon state. This avoids visually combining a changing draft with a party that is actually traversing the older run snapshot.

Leaving Edit Mode restores the appropriate current run presentation if the run is still active.

## Decision 63 - Committing edits while runs are active

**Approved design lock:** A valid, affordable draft may be committed while one or more runs are active.

Existing active runs remain bound to their run-start authoritative state/snapshot. Newly starting runs use the newly committed canonical dungeon.

No active run is restarted, retroactively altered, or re-resolved because the player committed an edit.

---

# Group 13 - Normal-Mode Inspection and Information Overlays

## Decision 64 - Direct entity inspection

**Approved design lock:** Monsters, traps, loot nodes, and adventurers are directly selectable in Normal/Inspect/Run view and open a contextual inspection panel.

The inspection surface should expose player-appropriate current/configured information without turning internal diagnostic evidence into normal UI.

## Decision 65 - Normal-mode analytical overlays

**Approved design lock:** Normal Mode may expose a limited set of gameplay-analysis overlays, while construction-specific overlays remain Edit Mode-only.

Normal/Inspect/Run view may support information such as adventurer path/route visualization, trap effect areas, or encounter/interaction highlights where useful for understanding gameplay.

Edit Mode additionally owns construction-specific information such as the placement grid, footprint validity, collisions, draft differences, build capacities, and other authoring diagnostics.

## Decision 66 - Overlay activation

**Approved design lock:** Use one compact analysis/layers control for explicit floor-wide overlays, combined with contextual behavior when an entity is selected.

Selecting an entity may automatically show information directly relevant to it, such as a trap's trigger/effect footprint, without requiring the player to enable a separate global overlay first.

Avoid a permanent row of independent overlay buttons that consumes scarce portrait-screen space.

## Decision 67 - Overlay coexistence

**Approved design lock:** Compatible overlays may coexist. Conflicting or unreadable overlay combinations may be mutually exclusive.

Overlay compatibility should be explicit rather than assuming every visualization can always be stacked. MVP can use a small compatibility set while leaving the architecture extensible.

## Decision 68 - Persistent HUD information budget

**Approved design lock:** Keep only a small set of high-value information persistently visible on the floor screen. Detailed metrics and secondary systems require deliberate navigation, expansion, or selection elsewhere.

Persistent HUD space is scarce. A metric should remain permanently visible only when players routinely need it to understand or make immediate dungeon decisions.

For the current MVP, the approved set is Decision 86: Total Mana, Usable Mana, Mana/hour, and Heat. Reserve Mana remains understandable and discoverable through an appropriate resource/detail/advanced surface, but is not a required persistent default metric.

---


# Group 14 - Production Navigation and Bootstrap Retirement

## Decision 69 - Production landing surface

**Approved design lock:** Dungeon-first landing surface.

The production application opens directly into the dungeon's Normal / Inspect / Run view rather than requiring a separate Home or command-hub screen first.

**Implications**
- Keeps the dungeon itself central to the core fantasy.
- Minimizes navigation before the player can inspect the dungeon or observe runs.
- Secondary systems remain reachable through the production navigation model rather than displacing the dungeon as the primary surface.

## Decision 70 - Major production navigation

**Approved design lock:** Compact contextual navigation.

Normal Mode keeps a small persistent set of high-frequency destinations readily accessible. Lower-frequency destinations live behind an additional compact navigation affordance rather than a permanent full-width or crowded navigation bar.

Edit Mode may replace or suppress normal global navigation while editing so draft controls, floor navigation, and contextual editing surfaces retain clear interaction space.

The approved MVP destination set is Decision 95: Dungeon, Research, Analysis, and More.

## Decision 71 - Bootstrap replacement strategy

**Approved design lock:** Replace Bootstrap player controls capability by capability.

Production UI should take ownership of one player-facing capability at a time. The corresponding Bootstrap player control is retired only after the production path reaches player-capability parity and passes its required smoke/qualification evidence.

Both temporary Bootstrap controls and production UI must call the same gameplay/domain authorities while they coexist. Bootstrap must not become a second gameplay authority.

## Decision 72 - Bootstrap retirement parity requirement

**Approved design lock:** Player-capability parity, not diagnostic parity.

A Bootstrap player control may be retired when the production replacement provides equivalent player-relevant capability, including:
- the same underlying player action,
- the same gameplay consequences,
- the same blockers and reason semantics,
- localization-backed player messaging,
- equivalent save/persistence behavior,
- required qualification evidence.

Developer diagnostics, test shortcuts, and internal evidence surfaces do not require equivalent release-player UI before the old player control is retired.

## Decision 73 - Development diagnostics after Bootstrap retirement

**Approved design lock:** Development-only diagnostics remain available inside the production application shell.

Useful QA/debug inspection and control surfaces may survive Bootstrap retirement through an explicit development-only diagnostic path that consumes the same canonical state and authorities as production UI.

These diagnostics must be absent or inaccessible in normal release builds and must not create a second writable gameplay authority.

---

# Group 15 - Production Screen Hierarchy and Core Navigation

## Decision 74 - Dungeon floor presentation

**Approved design lock:** One continuous dungeon screen with in-place floor switching.

The dungeon is one major production surface. Selecting another floor changes the displayed floor in place rather than navigating into a separate floor-specific screen or scene.

This keeps all floors conceptually part of one dungeon and supports quick comparison/navigation through the existing floor selector.

## Decision 75 - Research presentation

**Approved design lock:** Research is a dedicated major production screen.

Research is reached through the production navigation model rather than being embedded as a dungeon contextual sheet. This gives the research system room to expand without crowding the graphical dungeon/editor surface.

## Decision 76 - Run-result presentation

**Approved design lock:** Do not present a player-facing result surface for every individual run. Use the Phase 6 aggregate multi-run reporting model.

Individual runs contribute to the authoritative accumulated/aggregate outcome information defined by Phase 6 planning. Phase 7 must not introduce a modal, dedicated screen, interruption, or other result presentation after every simulated run unless a later approved specification explicitly changes this direction.

Normal inspection and aggregate reporting should help the player understand patterns across multiple runs rather than requiring each run to become its own player-facing results event.

## Decision 77 - Entering Edit Mode

**Approved design lock:** Persistent explicit Edit control on the dungeon screen.

The currently selected dungeon floor exposes a clear Edit affordance from Normal / Inspect / Run view. Direct geometry interaction may supplement that affordance later, but it is not the sole discovery path into editing.

Edit Mode remains a mode of the same dungeon surface rather than a separate global Build destination.

## Decision 78 - Back/exit behavior while editing

**Approved design lock:** Exit behavior follows draft cleanliness and commit validity.

**No draft changes**
- Exit Edit Mode immediately.

**Valid changed draft**
- Save Changes.
- Discard Changes.
- Continue Editing.

**Invalid changed draft**
- Discard Changes.
- Continue Editing.
- Saving remains unavailable until the draft becomes valid-for-purpose.

Back/navigation must never silently commit a changed draft or silently discard player work.

---

# Group 16 - Mobile, Safe Area, and Text Accessibility

## Decision 79 - Primary mobile orientation

**Approved design lock:** Portrait-first production UI. Landscape remains supported and functional but does not require a separately optimized first-class MVP layout.

The normal production experience is designed around comfortable one-handed portrait use. Landscape must remain usable and non-broken, including where dungeon design/layout editing benefits from additional horizontal space, but MVP does not require maintaining two independently optimized production UI systems.

## Decision 80 - Device safe-area behavior

**Approved design lock:** All interactive controls and critical information respect the device safe area.

Dungeon artwork, background treatment, or noncritical map rendering may extend behind notches, rounded corners, or similar cutout regions where visually appropriate.

Required controls, critical values, warnings, confirmation actions, and other necessary interaction targets must remain inside the usable safe area.

## Decision 81 - Text-size accessibility control

**Approved design lock:** Three explicit text-size settings: Small, Default, and Large.

Major production UI surfaces must respond consistently to the selected size. Critical gameplay values and required actions must remain readable without being lost through truncation; wrapping/responsive layout remains preferred where needed.

This resolves the MVP control shape for the existing locked requirement that major UI surfaces support text scaling.

**Clarification - no new Decisions 82/83 from the earlier accessibility discussion:**
The previously proposed separate decisions for color-independent editor validity and destructive-action confirmation were duplicates of already-settled Decisions 8, 13, 14, and 24. They were therefore not added as independent locks. Decision numbering continues below with newly resolved specification-conflict decisions.

---

# Group 17 - Draft Persistence and Legacy Renovation Reconciliation

## Decision 82 - Immediate edit-save rule

**Approved design lock:** Immediate draft persistence replaces immediate canonical saving during Edit Mode.

Each completed draft mutation is promptly persisted to the separate durable non-authoritative draft store. The canonical dungeon does not change merely because a placement, movement, removal, corridor edit, or content edit was completed in the editor.

Canonical gameplay state changes only when the player explicitly chooses Save Changes and the complete whole-dungeon transaction passes current validation, affordability, persistence, and publication requirements.

**Specification reconciliation:** This direction intentionally supersedes the older rule that tile placement/movement in Edit Mode immediately saves canonical gameplay state. The preservation intent of that rule is retained by immediately protecting the draft from ordinary interruption/crash loss instead.

## Decision 83 - Legacy 30-second renovation undo

**Approved design lock:** Retire the 30-second renovation undo as a player-facing mechanism in the production graphical editor.

The transactional draft already provides experimentation without canonical cost: the player can alter the draft, inspect consequences, return to the original design, discard the entire draft, or review the final canonical-to-draft transaction before committing.

The existing renovation-undo mechanism may remain temporarily available through legacy Bootstrap controls while those controls are still being replaced, but it is not part of the final production graphical-editor interaction contract.

Phase 7A0 promotion must explicitly supersede the old production-editor undo requirement rather than leaving both systems as competing player-facing authorities.

## Decision 84 - Draft persistence granularity

**Approved design lock:** Persist after each discrete completed draft command, not every continuous pointer/drag frame.

Examples of persistence boundaries include:
- confirmed room placement,
- completed room move or supported rotation,
- completed corridor creation/reshape command,
- confirmed content assignment/reassignment/removal,
- completed object deletion or other accepted draft mutation.

Pointer movement, drag interpolation, camera movement, hover/preview state, and other transient interaction frames are not separate durable writes.

Draft persistence may be ordered/asynchronous relative to presentation so the UI is not forced to block rendering for every write, but ordering must preserve the authoritative sequence of accepted draft commands and recovery must never reconstruct a later command without its required predecessors.


---

# Group 18 - Touch Targets, HUD Information, Aggregate Analysis, and Edit Capacity

## Decision 85 - Minimum mobile touch target

**Approved design lock:** Use platform-standard minimum interactive hit regions.

- Android interactive hit regions must be at least 48 dp by 48 dp.
- iOS interactive hit regions must be at least 44 pt by 44 pt.
- Visual icons or glyphs may be smaller than the hit region when appropriate.
- High-frequency or high-consequence actions may use larger targets.

The production UI must not assume that the visible artwork bounds are the same as the effective touch target. Touch-target implementation must remain compatible with safe-area handling, one-handed portrait play, text scaling, and responsive layout.

## Decision 86 - Persistent Normal Mode HUD metrics

**Approved design lock:** Persistent Normal / Inspect / Run HUD shows four primary metrics:

- Total Mana,
- Usable Mana,
- Mana per hour,
- Heat.

Reserve Mana and other secondary details are available through deliberate expansion, contextual information, or system-detail surfaces rather than consuming another permanently visible HUD slot.

This uses four of the locked 3-to-5 persistent metric budget and leaves detailed/advanced information to progressive disclosure.

## Decision 87 - Aggregate run-analysis location

**Approved design lock:** Use a hybrid aggregate-analysis model.

The dungeon view exposes a compact aggregate run-performance summary and a direct entry point into deeper analysis. Selecting that entry point opens a dedicated Analysis / Reports surface with room for causal explanations, trends, comparisons, and the approved multi-run aggregate reporting model.

Do not introduce a mandatory result screen after each individual run. Individual run outcomes contribute to the aggregate reporting authority instead of interrupting the build-run-inspect-revise loop one run at a time.

The compact dungeon summary and deeper Analysis / Reports surface must consume the same reporting authority rather than calculate independent results.

## Decision 88 - Persistent Edit Mode floor-space information

**Approved design lock:** Used and remaining floor-space capacity remain persistently visible while Edit Mode is active.

Detailed capacity breakdowns, configured maximum capacity, prospective placement consumption, and other relevant consequences may expand contextually when the player selects or previews a placement.

The persistent view should communicate the continuous build-space constraint without turning the editor HUD into a full diagnostic dashboard.

# Group 19 - Localization Delivery, Orientation Continuity, Run Reconstruction, Performance Degradation, and Mobile Resume

## Decision 89 - Japanese production UI delivery timing

**Approved design lock:** Phase 7 uses an English-first implementation while preserving Japanese-ready architecture and layout qualification.

All production UI introduced in Phase 7 must:
- use stable localization keys rather than hardcoded player-facing strings,
- consume the production localization system and English fallback authority,
- support text expansion and alternate-language test packs without code or layout redesign,
- preserve glossary ownership and locale-aware number/text presentation,
- remain capable of accepting Japanese production content later without architectural rework.

Finished Japanese translation content is not required in every Phase 7 implementation PR. Japanese remains required for release readiness under the locked localization specification and must be qualified before the applicable release gate.

## Decision 90 - Orientation changes during Edit Mode

**Approved design lock:** Orientation changes are presentation-only and preserve the complete Edit Mode session.

Changing between supported portrait and landscape presentation must preserve:
- the non-authoritative draft,
- selected floor,
- selected object or active editing tool when still meaningful,
- changed-floor indicators,
- draft validity/blockers,
- pending canonical-to-draft economic consequences.

The camera may deterministically reframe or clamp to the new viewport as needed, but orientation changes must never commit, discard, restart, or otherwise mutate the authoritative meaning of the draft.

## Decision 91 - Returning to a run after its visualization was hidden

**Approved design lock:** Reconstruct the graphical presentation directly from the authoritative current run/event state. Do not replay a backlog of missed animation.

When a run becomes visible again after floor switching, Edit Mode, backgrounding, or another presentation interruption:
- authoritative simulation remains the source of truth,
- presentation reconstructs the current applicable state,
- missed graphical animation is not replayed merely to catch up,
- historical understanding comes from approved aggregate analysis and other inspection surfaces rather than a mandatory animation backlog.

Presentation must never create a second timeline that delays or changes simulation authority.

## Decision 92 - Performance degradation priority

**Approved design lock:** Degrade non-authoritative presentation fidelity before gameplay authority or outcomes.

If a supported device cannot maintain the intended graphical presentation budget, the game may reduce nonessential visual work such as cosmetic effects, interpolation frequency, animation update frequency, or other presentation detail.

The following must not change merely because a device is slower:
- deterministic simulation decisions,
- dungeon/canonical state,
- economic results or rewards,
- run outcomes for otherwise equivalent authoritative state,
- save semantics,
- lifecycle authority.

Any degradation behavior must remain compatible with the locked performance specification, including the 30 FPS target, minimal UI work, batching, and avoidance of full-dungeon rerendering on every tick.

## Decision 93 - Background/resume while editing

**Approved design lock:** Resume directly into the same Edit Mode session when the durable draft is still safe against its canonical baseline.

On application resume:
1. Load/verify the current canonical baseline.
2. Load/revalidate the durable non-authoritative draft.
3. If the relevant baseline still matches, restore the Edit Mode session and its meaningful presentation state.
4. If the baseline no longer matches, do not silently overlay or rebase the draft. Route through the existing stale-draft recovery behavior instead.

Backgrounding alone never commits or discards the draft.


# Group 20 - HUD Reconciliation, Production Navigation, Responsive Layout, Floor Rendering, and Dense Presentation

## Decision 94 - Persistent HUD reconciliation with onboarding Spec 20

**Approved design lock:** Keep Decision 86 as written and explicitly supersede the older Spec 20 requirement that Reserve Mana remain persistently visible in the default HUD.

Persistent Normal / Inspect / Run HUD remains:
- Total Mana,
- Usable Mana,
- Mana per hour,
- Heat.

Reserve Mana remains available through deliberate expansion, contextual resource information, or a system-detail surface rather than consuming a persistent HUD slot.

This is an intentional design change, not an accidental contradiction. This PR amends the affected canonical onboarding/UI wording. Other Spec 20 requirements remain unchanged unless separately superseded.

## Decision 95 - Permanent Normal Mode navigation destinations

**Approved design lock:** Persistent Normal Mode navigation uses Dungeon / Research / Analysis / More.

- Dungeon is the landing surface and primary build-run-inspect-revise destination.
- Research is a dedicated major production screen.
- Analysis opens the deeper aggregate multi-run reporting surface established by Decision 87.
- More contains lower-frequency destinations such as settings and future secondary systems rather than expanding the persistent navigation bar indefinitely.

Edit Mode suppresses or replaces this normal global navigation as needed so editing controls retain clear interaction space.

## Decision 96 - Tablet and wide-screen layout strategy

**Approved MVP design lock:** Use one responsive information architecture across supported phone and tablet layouts.

Phones and tablets share the same navigation hierarchy, gameplay authorities, and core interaction model. Wider screens may use available space more effectively by expanding panels, exposing additional context simultaneously, or adjusting composition, but MVP does not require a separate tablet-only UX architecture.

This is an MVP scope decision, not a permanent prohibition on a materially different tablet presentation in a later release.

## Decision 97 - Floor rendering during Edit Mode

**Approved design lock:** Graphically render only the currently selected floor while editing.

The whole-dungeon draft remains one atomic editing transaction and may contain changes across multiple floors, but offscreen floors do not remain actively instantiated/rendered merely because they participate in the draft.

Floor-selector state remains responsible for exposing offscreen changed-floor indicators, blockers, lifecycle state, and navigation. Selecting another floor reconstructs/renders that floor from the current draft state.

This presentation rule must not create separate per-floor draft authorities.

## Decision 98 - Dense room-content presentation

**Approved design lock:** Use adaptive visual detail while preserving every authoritative entity and exact placement.

Normal / Inspect / Run presentation may simplify, cluster, or reduce noncritical graphical detail when the camera is zoomed out or a room is visually dense. This is presentation-only. It must not aggregate away gameplay authority, identity, occupancy, targeting, simulation, or save state.

Zooming in, selecting the room/entity, or opening the relevant Edit Mode content-management surface must provide access to individual entities and their exact authoritative placement where that information matters.

The adaptive-detail system must preserve the living-dungeon presentation rather than collapsing Normal Mode into permanent icon-only abstraction.

# Group 21 - Onboarding Surface, Edit-Mode Navigation, More Hub, and Advanced-View Persistence

## Decision 99 - Recommended Next Steps presentation

**Approved design lock:** Use a compact, collapsible Recommended Next Steps card on the Dungeon screen.

The card normally shows the current recommended action. Expanding it shows the remaining ordered onboarding actions with one-tap navigation to the relevant production surface.

The card may be collapsed, but it is not permanently dismissible until the required core onboarding actions are completed. Completion removes the onboarding requirement rather than merely hiding it.

This preserves the locked soft-guided onboarding model without turning onboarding into a separate mandatory screen.

## Decision 100 - Navigating away while Edit Mode has a draft

**Approved design lock:** Any attempt to navigate from Edit Mode to another production destination uses the same transactional exit rules as leaving Edit Mode normally.

- Clean draft: leave Edit Mode and complete the requested navigation immediately.
- Valid changed draft: offer Save Changes / Discard Changes / Continue Editing.
- Invalid changed draft: offer Discard Changes / Continue Editing.
- Successful Save Changes or Discard Changes completes the requested navigation.
- Continue Editing cancels the navigation request and leaves the player in Edit Mode.

No alternate hidden draft-suspension authority is created merely because the destination is Research, Analysis, More, or another production surface.

## Decision 101 - More as a scalable secondary hub

**Approved design lock:** More is a low-frequency secondary hub rather than a dumping ground for core gameplay systems.

It may itself contain several organized tabs or categories as the game grows. Plausible future examples include:
- game mechanics/reference information,
- premium currency purchase management,
- premium store,
- settings,
- accessibility/language controls,
- help/support,
- other genuinely secondary destinations.

The exact tab set is not locked by this decision and should expand only as approved systems require it. A system that becomes a frequent core gameplay loop should receive an appropriate first-class surface rather than remaining permanently buried under More merely because it was introduced there first.

This preserves the MVP Dungeon / Research / Analysis / More navigation model while allowing More to scale without expanding the persistent navigation bar indefinitely.

## Decision 102 - Advanced-view preference persistence

**Approved design lock:** Remember Advanced View preference independently per panel on the local device.

Each panel initially uses its simple/default view until the player explicitly enables Advanced View for that panel. Once changed, that panel remembers the preference across sessions on the same device until the player changes it again.

Advanced-view preferences are presentation/user-preference state only. They are not canonical gameplay state, do not affect deterministic outcomes, and do not create save/migration gameplay authority.

The per-panel model remains required; there is no single global Advanced Mode that automatically changes every panel.


# Group 22 - Durable Analysis Boundary

## Decision 103 - Multi-run Analysis durability

**Approved design lock:** Use durable coarse analysis with transient detailed causes for MVP.

The production Analysis surface may aggregate durable run-history facts across multiple runs, including floor reach, continuation depth, exit/retreat/wipe outcomes, survivor/death patterns, loot/extraction, and route outcomes.

Detailed causal evidence that is only authoritative in the current session, such as exact EXIT-versus-DESCEND transition reasoning retained through transient Phase 6 run evidence, is shown only while that evidence exists. Phase 7 must not reconstruct historical detailed causes from incomplete durable data.

MVP does not add a new save owner, schema extension, or durable per-run causal-history authority solely to support analytics. If later player testing proves durable causal analytics are necessary, that becomes a separately reviewed persistence/design change.

The compact Dungeon summary and deeper Analysis / Reports surface must derive from the existing durable run-history/reporting authorities plus currently available transient evidence rather than creating a duplicate gameplay or reporting authority.


# Group 23 - Floor Lifecycle Production Presentation

## Decision 104 - Blocked activation presentation

**Approved design lock:** Keep the Activate action visible but disabled when the selected constructed Inactive floor is not currently eligible, and present the authoritative localized blocker reason immediately nearby.

The production UI must consume the existing floor-activation eligibility/lifecycle authorities rather than duplicate their rules. A blocked action remains discoverable so the player can understand that activation is a goal and what condition must be corrected.

Do not hide the lifecycle action merely because it is unavailable, and do not require the player to press an enabled action just to discover a known blocker.

## Decision 105 - Deactivation confirmation threshold

**Approved design lock:** Require confirmation only when deactivation produces meaningful cascading consequences.

Deactivating only the deepest Active floor may complete directly with concise localized success feedback.

Deactivating a shallower Active floor that will also deactivate deeper floors requires a consequence-focused confirmation that identifies the affected floors and explains that:
- future runs will use the shorter Active prefix,
- Active-floor passive-mana contribution changes accordingly,
- construction, room content, historical investment, and custody are not destroyed or refunded,
- already-started runs remain bound to their existing run-start snapshot.

The UI must not imply data loss or current-run mutation where the underlying lifecycle authority performs neither.

## Decision 106 - Activate All Eligible production exposure

**Approved design lock:** `Activate All Eligible` is contextual rather than a permanently visible production control.

For the current two-floor MVP, normal production UI exposes activation of the selected eligible floor. The bulk action should appear only when it would meaningfully differ from activating the selected floor, such as after future production content includes multiple deeper constructed floors that can be activated as a contiguous eligible prefix.

The underlying authority remains available for future production use and development diagnostics. This decision does not remove or fork the existing lifecycle command.

## Decision 107 - Post-lifecycle navigation

**Approved design lock:** Successful activation or deactivation leaves the player on the currently selected floor.

After the lifecycle transaction succeeds, the production UI updates in place:
- lifecycle state,
- applicable floor-selector states,
- passive-mana HUD values,
- activation/deactivation actions and blocker state,
- concise localized success feedback.

A lifecycle change does not automatically switch floors, enter Edit Mode, start a run, or open Analysis.


# Known specification/repository conflicts requiring deliberate Phase 7A0 reconciliation

## A. Immediate edit save rule versus transactional draft

Historically, Spec 28 and the global lock stated that edit-mode tile placement/movement triggered immediate saves. The current Phase 4 structural economy implementation also uses immediate canonical edit transactions plus a single short-lived renovation undo.

Approved Decisions 12, 16, 20, 21, 82, and 84 resolve the Phase 7 direction:
- separate non-authoritative durable draft,
- persistence after each discrete completed draft command,
- no canonical/economic effects during experimentation,
- final canonical-to-draft reconciliation,
- atomic Save Changes / Discard Draft.

This PR explicitly supersedes the old immediate-canonical-save wording rather than silently contradicting it.

## B. 30-second renovation undo versus draft experimentation

Historically, Spec 21 used a 30-second cost-free renovation undo as an experimentation grace window.

Approved Decision 83 retires the 30-second undo from the production graphical editor because draft experimentation and final commit review replace its player-facing purpose. The legacy mechanism may remain temporarily available through Bootstrap during capability-by-capability replacement, but it is not part of the final production editor contract.

This PR explicitly supersedes the old requirement in the canonical specifications rather than leaving both interaction models active.

## C. Current room content assignments have no room-local coordinates

Current canonical schema-12 room content assignment state lacks room-local position. Approved Decisions 38 and 44 therefore require an explicit save/domain extension and migration strategy after the Phase 6 baseline.

Do not implement private UI-only coordinates that are disconnected from canonical authority.

## D. Current run system versus intraroom graphical movement

Current deterministic run/traversal systems are not yet a full graphical intraroom movement simulation. Phase 7 must add a deterministic spatial/event layer or equivalent authoritative representation sufficient for exact placement to matter without making animation timing authoritative.

## E. Final Phase 6 reconciliation baseline

**Resolved implementation baseline for Phase 7A0:** Phase 6 closed with merged PR #220 at `main` `e9f93b8d742ccaba38c7de32b776970006791d93`. Writable schema remains 12.

Reconciliation confirms:
- atomic floor lifecycle and contiguous Active-floor authority are implemented and qualified,
- active runs consume immutable run-start floor snapshots and later lifecycle/edit changes affect later runs only,
- durable `sharedFloorKnowledge` is schema-12 authority and material floor/content changes can make stored knowledge inapplicable while activation alone does not,
- knowledge-backed EXIT/DESCEND perception and localized same-session causal explanation are implemented,
- durable run history retains coarse reach, attrition, loot, and final-route information, while detailed transition-cause evidence remains transient,
- production content remains Floors 1-2 and Phase 7 must not invent Floors 3-5 behavior merely because the architecture supports a larger bound.

Phase 7A0 must update canonical references to this baseline and preserve these authorities rather than introducing duplicate lifecycle, run-snapshot, knowledge, or reporting ownership.

## F. Decision 86 HUD metric set versus locked onboarding visibility

Historically, Spec 20 required Total Mana, Reserved Mana, and Heat to be visible immediately and stated that Reserve Mana was displayed as a single number in the default view. Spec 26 permits a 3-to-5-metric main HUD and had listed total mana, usable mana, reserve mana, mana per hour, and heat as the MVP candidate set.

Approved Decision 94 resolves this conflict in favor of Decision 86: the persistent Normal / Inspect / Run HUD remains Total Mana, Usable Mana, Mana per hour, and Heat, while Reserve Mana moves behind deliberate expansion or a detail surface.

This PR explicitly amends/supersedes the affected Spec 20 wording. The conflict is resolved.

---

# Explicitly deferred beyond MVP / Phase 7 unless later evidence changes scope

- Full tactical monster AI with sophisticated formations, line-of-sight, patrol, advanced target switching, retreat, etc.
- Complex maze psychology such as getting lost, uncertain backtracking, or giving up.
- Multi-select/group room manipulation.
- Full configured-room cloning including persistent content identities.
- Production-quality combat art/animation/effects.
- Curved/angled corridor authoring and irregular room geometry, while preserving forward compatibility.
- Explicit complex junction/loop/maze authoring, while preserving forward compatibility.
- Researchable/named dungeon-layout save slots/loadouts.
- Cloud synchronization of uncommitted local drafts.
- Automatic three-way merge/rebase of stale drafts.
- High-concurrency rendering of offscreen runs.
- Final ownership/design of run speed acceleration or quick-commit progression benefits.

---


# Resolution, supersedence, and implementation sequence

Owner Decisions 1–107 are resolved for the current Phase 7A0 scope. The only deferred items are the explicitly identified non-goals and later implementation details that do not alter those decisions. This document is promoted by its reviewed documentation PR; no Phase 7 implementation is authorized by documentation alone.

## Explicit Phase 7A0 supersedences

- **INV-12 / edit saving:** immediate save safety now means durable persistence of each completed discrete **draft** command to a separate non-authoritative draft store. It does not mean immediate canonical mutation. Continuous pointer/drag frames are transient. Save Changes performs final validation, current-economics/resource calculation, and one atomic canonical commit; failure applies nothing and preserves the draft.
- **Legacy renovation undo:** the Phase 4 30-second undo remains historical implementation evidence and may remain temporarily behind Bootstrap while capabilities migrate, but is not required in the final production graphical editor. Draft experimentation, discard, and final commit review replace its player-facing purpose.
- **Persistent HUD:** the default Normal/Inspect/Run HUD is Total Mana, Usable Mana, Mana/hour, and Heat. Reserve Mana remains discoverable in an appropriate detail/advanced surface; it is not removed as a resource.
- **Reporting:** no mandatory per-run results modal is introduced. The Dungeon view offers a compact aggregate entry point and Analysis is a major destination. It consumes existing durable coarse run history and currently available transient detail; Phase 7A0 adds no analytics save owner or schema extension and must not reconstruct historical detailed causes from incomplete evidence.

## Dependency-correct Phase 7 implementation packets

The packet count may be adjusted by review evidence, but these boundaries must remain intact.

1. **Positional canonical foundation.** Depends on schema 12 and Phase 6 lifecycle/snapshot authorities. Add the intentional room-local positional domain/save extension and explicit migration; preserve stable assignment IDs and canonical order; define deterministic transforms, data-driven multi-tile/category occupancy, and invalid-state failure. Do not deliver production graphical UI. Acceptance includes migration/reopen/ordering/invalid-state coverage and verification that material positional changes participate in the floor-knowledge applicability input while activation alone still does not.
2. **Deterministic intraroom mechanics.** Depends on packet 1. Establish bounded deterministic room-local traversal, interaction and configured monster-start semantics sufficient to prove valid placement changes gameplay, with presentation non-authoritative. Do not broaden into full tactical AI or production-editor polish. Acceptance includes reproducibility, snapshot isolation, mobile-safe workload, and placement-sensitive behavior tests.
3. **Transactional editor authority and production Dungeon presentation.** Depends on packets 1–2. Deliver the whole-dungeon durable draft, recovery/stale-baseline rules, side-effect-free previews, final atomic commit economics, and Normal/Edit production presentation consuming existing authorities. Do not retire Bootstrap controls yet. Acceptance includes draft crash/reopen, no canonical effects before commit, atomic failure preservation, localized reasons, responsive/safe-area/accessibility checks, and editor/production smoke evidence.
4. **Production capability migration and closeout.** Depends on packet 3 and each applicable player capability reaching parity. Retire normal-player Bootstrap controls only after parity and smoke evidence; retain diagnostics only behind development-only inaccessible-in-release paths. Qualify the full build → run → inspect → revise → rerun loop, including lifecycle presentation and aggregate Analysis boundaries. Do not claim fun is proven; later Phase 9 owns balancing/comprehension validation.

## Ongoing implementation constraints

Room movement carries room-local content; supported room rotation transforms it deterministically. Stationary traps/loot use room-local positions, and monsters use a configured room-local start while runtime movement remains transient. Invalid placement must fail explicitly, never silently relocate content. Rectangle/square MVP rooms remain supported without making irregular rooms impossible. Production UI remains portrait-first, safe-area-aware, responsive across phone/tablet, supports landscape without separate MVP information architecture, three text sizes, platform-standard targets, and non-color-only critical states.

Lifecycle presentation consumes the final Phase 6 authority: blocked Activate remains visible but disabled with a localized authoritative reason; deepest-only deactivation may complete directly; shallower deactivation confirms its future-run/Active-floor passive-mana consequences without claiming loss or current-run mutation; Activate All Eligible is contextual and unnecessary in the current two-floor UI when redundant; successful lifecycle changes update in place on the selected floor and do not start a run, switch floors, or enter Edit Mode.

No later implementation may introduce duplicate writable state, embedded tuning, player-facing hardcoded text, or a parallel Bootstrap gameplay authority.
