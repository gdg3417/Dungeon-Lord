# Dungeon Lord: Production Dungeon Mobile UI Vision

**Status:** Owner-reviewed directional design reference, not an implementation specification or replacement for locked design decisions.  
**Captured:** 2026-10-07  
**Reference image:** [Five-screen mobile UI concept](assets/production-dungeon-mobile-ui-concept-2026-10-07.png)  
**Context at capture:** Phase 7A5 room movement merged as PR #227; production Dungeon UI functional but visually provisional. Later development should verify current repository state rather than assume this snapshot remains current.

## Purpose and authority

Keep a durable, discoverable reference for the intended **portrait-first production Dungeon interface** across future chats, Codex implementation prompts, PR reviews, and owner UAT. The goal is to make incremental capability work converge on an understandable, attractive dungeon-building game instead of accumulating unrelated prototype controls.

This document captures **owner-reviewed direction**, not pixel-perfect approval. The concept image illustrates composition and player interactions. It is **not** a screenshot of the game, a source of gameplay rules, a production-ready sprite sheet, or approval of the specific room list, floor list, names, prices, combat statistics, resource totals, capacities, color-coded outcomes, mechanics, or fantasy art details shown. Only repository-authored content, current domain authorities, approved specs, and localized tables define real game behavior and text. Do not hardcode mockup values or invent mechanics to reproduce the image.

In a conflict, the existing locked specifications, especially `docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md`, canonical gameplay invariants, and latest approved repository decisions take precedence. Escalate contradictions rather than silently redefining a lock. This reference may evolve through explicit owner review.

## Five reference states

1. **Regular Dungeon: floor overview.** Dungeon dominates the viewport; resource HUD at top; vertical floor navigation on left; no edit grid. A collapsible lower sheet displays selected-floor information when no specific object is selected.
2. **Regular Dungeon: selected object.** Tapping a room, monster, trap, or loot node selects it. The same lower sheet changes from the floor summary to that object's details. Selection does not automatically zoom the camera. Room selection reveals a **Focus Room** action.
3. **Edit Mode: room category.** The canonical floor grid becomes visible. A horizontally scrollable category bar remains visible above the changing lower-panel content. Rooms is the first fully realized building category for the next capability packet; the content area shows currently available, authored room options and their relevant information.
4. **Edit Mode: placement preview.** Category bar remains visible while a selected room is positioned. Clearly distinguish valid anchors, invalid attempts, proposed footprint, and current committed geometry. Tapping a location previews it, then **Confirm Placement** adds the proposed construction to the non-authoritative draft. **Save Changes** is a distinct, final, atomic commit, not the same action as placement confirmation.
5. **Focused room: content inspection and future editing.** An existing room can be selected and deliberately focused, zooming/centering the camera for understandable placement of individual room contents. A visible **Fit Floor / Exit Room View** affordance returns to the overview. The eventual room-focused surface represents actual monsters, traps, and loot as distinguishable game objects rather than identical geometric placeholders.

## Information architecture

- **Top:** persistent, legible, localized key-resource HUD, including Total Mana, Usable Mana, Mana per hour, and Heat when these are the correct canonical values for the view.
- **Center:** the dungeon is visually primary. Keep the viewport large enough for floor-level layout choices and support close focus for content-level choices. Respect phone safe areas and portrait size constraints; check landscape and long localized labels.
- **Left:** vertical descending floor selector, whose role is **navigation**, not lifecycle actions. Floor lifecycle actions belong in the floor context, per the A0 lock. Show floor states explicitly, without color as the only signal.
- **Bottom in Normal Mode:** one **collapsible contextual inspection sheet**. Default content is a floor summary; selecting a room, monster, trap, or loot item switches to its detail view. The player can collapse it to reclaim dungeon space and expand it again. Returning to floor information should be discoverable.
- **Bottom in Edit Mode:** one **collapsible shared editing sheet** with a **persistent category bar** and a content area that switches between option selection, selection details, placement preview, and errors. The category bar does **not** disappear during room placement. Horizontal scrolling should indicate that more categories exist and keep selection stable.
- **Global navigation:** long-term Dungeon / Research / Analysis / More-style information architecture is illustrative, not a requirement to implement incomplete destinations or permit unsafe departure from a pending edit draft. An edit session must respect existing Save/Discard/recovery rules.
- **Editing action hierarchy:** distinguish an immediate local action (Cancel or Confirm Placement) from session-level actions (Discard Draft or Save Changes). Display affordability and consequences at the appropriate decision boundary without flooding the screen with implementation diagnostics.

## Selection and camera behavior

- Tap an existing room to **select** it first. It reveals **Focus Room**, rather than instantly zooming.
- Focus Room centers/zooms to the selected room within usable viewport bounds without creating a second spatial or gameplay authority. Existing pan, pinch zoom, and floor focus remain available; the player can clearly return to the floor overview.
- Room-level camera focus is a presentation/navigation tool. It must not make draft changes, mutate canonical state, spend mana, change active runs, or implicitly alter assignments.
- Detailed intra-room monster/trap/loot placement is a **future capability**; the focused-room screen is a target interaction state, not proof the capability already exists.

## Building categories and inspector details

Intended primary category paradigm: **Rooms, Monsters, Traps, Loot**. The category strip may later accommodate **Floors**, environmental hazards, decorations, or other authored categories without a new layout, but none of these are automatically approved new game systems. If a Floors category is eventually used, its functions should be distinct from the side floor navigation selector. Do not prematurely build a generic category plugin framework.

The selected-object inspector can eventually expose suitable canonical configurations, such as monster attack/route preferences, trap settings, or the loot table belonging to a loot node. These are **potential UI homes for future mechanics**, not approval to implement new behavior, alter rules, or expose nonexistent settings.

## Visual object identities and art direction

The long-term production dungeon should present a **visually identifiable sprite/icon for each authored monster, trap, and loot type that appears on the floor**, not uniform anonymous boxes. Examples expressly requested by the owner:

- Skeleton: recognizable skeleton creature sprite.
- Glittering Hoard: recognizable treasure-hoard / chest sprite.
- Spike Trap: recognizable spike-trap sprite.

These should be legible at mobile viewing sizes, distinct under selection and zoom, and support localized labels/accessibility cues where relevant. Actual assets must follow the project's art, import, attribution/licensing, performance, content-ID, and build conventions. The composite illustration is **not** an importable game sprite atlas or a license to invent content not present in production data. Placeholder upgrades may be phased; do not make full artwork production a hidden dependency of room construction.

The mockup suggests a dark-fantasy stone-and-metal treatment, restrained luminous accents, recognizable room/corridor silhouettes, and stronger selection/validity hierarchy. Fidelity to that exact palette or decoration is not locked. Ensure readability and interaction clarity before ornamentation; avoid reducing dungeon visibility just to match a decorative mockup.

## Next implementation boundary and sequencing

The **approved next implementation direction** is **transactional graphical room construction in production Dungeon Edit Mode**, using existing native construction, validation, economics, localization, draft persistence, and save authorities. Scope only enough UI to make the capability discoverable: an understandable Rooms category, authored room selection, legal anchor discovery, visible draft placement, invalid reasons, local placement confirmation, cost/consequence review, and atomic Save/Discard. Preserve the working A4/A5 features and current schema/migration discipline.

A minimal selection-to-Focus Room interaction and shared-panel/collapse behavior should be assessed for fit against this PR's regression risk. Do not quietly expand it into full content placement, all-category completion, loot-table editing, monster behavior authoring, or full visual redesign.

**After room construction, the next lifecycle/floor PR boundary is provisional.** Reinspect the merged baseline, dependency graph, owner UI findings, and remaining Bootstrap parity before choosing the next capability. A dedicated Phase 7 UI composition pass remains expected before broad Phase 7 closeout, but its placement must be reassessed rather than precommitted now.

## UAT and review prompts for future chats

Evaluate progress against the vision incrementally, not by demanding pixel-perfect imitation of a generated picture. Ask:

1. Can an unfamiliar player distinguish Normal view from Edit Mode, select a room, and deliberately focus it without accidental camera movement?
2. Does collapsing the lower sheet recover meaningful dungeon space, and can the player restore floor/object details?
3. Is the category bar still present during room-placement preview, and is its current category unmistakable?
4. Can the player locate a valid placement visually, understand why an invalid placement fails, preview costs, and distinguish Confirm Placement from Save Changes?
5. In focused-room presentation, can the player identify a skeleton, spike trap, and Glittering Hoard by their own visual identities rather than identical boxes, when these authored objects are available?
6. Are buttons reachable, text legible, floor geometry dominant, and critical states understandable in phone portrait and landscape, with long localized labels?
7. Are all displayed costs, behaviors, IDs, content inventories, and save outcomes sourced from canonical authorities rather than copied from the concept art?

Implementation PRs should only be blocked by the **capability-relevant** acceptance criteria assigned to them. Broader visual convergence can be tracked separately, rather than inflating each feature PR.

## Maintenance

Treat this guide and the linked image as a **versioned repository reference**. When the owner approves a substantive change, update this document, date the change, and retain or replace visual references deliberately. Future Codex prompts and chat handoffs working on production Dungeon UI should explicitly read this guide **alongside** the current A0 design lock, relevant current specs, AGENTS.md, and latest merged PR evidence. Do not assume the UI image or document alone determines the next PR.
