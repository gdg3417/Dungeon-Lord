# Final compact placement production captures

All six images are actual production-scene captures from final full PlayMode qualification of frozen implementation `834bf3b7031caa8f17bd99d17516ffde3692cc45`. They were inspected before full qualification in the focused run, then freshly captured and inspected again after the final suite. [Capture hashes](screenshot-index.json) and [measured geometry](measured-layout.json) accompany the images.

1. [720×1280 portrait Move, Default text](screenshots/placement-compact-portrait-move.png): short instructions follow content height; no unused stretched information area. Category, Cancel/Confirm and Save/Discard remain visible.
2. [1920×1080 landscape Move, Default text](screenshots/placement-compact-landscape-move.png): compact bottom sheet, full-width readable header rows, no overlap, substantial map area.
3. [1024×600 Large-text valid Move](screenshots/placement-compact-narrow-large.png): purchase-bearing draft, capacity/validity text, reachable placement and session controls, focused room and distinct valid mint cell. Map viewport is 153 pixels tall; longer information can scroll.
4. [1024×600 Large-text long information at scroll bottom](screenshots/placement-compact-long-scroll.png): test-local 20-line language expansion scrolls above fixed actions. Text clipping at the scroll viewport edge is intentional overflow containment; controls remain accessible.
5. [Muted-gold grid at ordinary Fit Floor zoom](screenshots/placement-compact-grid-floor.png): target-room grid contrasts with actual blue-gray masonry, leaves interiors transparent, and keeps the sprite, walls and doorway thresholds legible.
6. [Muted-gold grid at Focus Room zoom](screenshots/placement-compact-grid-focus.png): exact cell boundaries stay aligned; sprites/thresholds render above the grid, and mint validity remains distinct from warm grid/selection outlines.

These captures qualify Editor production rendering. The built Windows player contains the same qualified source/art and still requires the limited owner runtime UAT described in [qualification](qualification.md). No new art mappings are introduced.
