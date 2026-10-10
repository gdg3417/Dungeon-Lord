# Final owner-UAT correction captures

Actual production PlayMode captures from frozen implementation `344a5edcf540ee6c0ed81c63be723a5f6a0a784e`, visually inspected after final full qualification. Identity, geometry, non-overlap and preview mutation assertions are retained in the XML archive.

- [New definitions only](screenshots/room-content-uat-new.png): configured Goblin/Skeleton purchases, separate Owned count.
- [Owned returned custody](screenshots/room-content-uat-owned.png): explicit Skeleton selection, free redeployment.
- [Target switched to Room 2](screenshots/room-content-uat-target-room.png): same returned identity, current target capacity and valid preview.
- [Portrait grid and multi-cell placement](screenshots/room-content-uat-grid-portrait.png): hollow cells above masonry, below objects and boundaries.
- [Outside invalid tap after valid multi-cell preview](screenshots/room-content-uat-invalid-multicell.png): old green footprint cleared, invalid anchor, Confirm disabled.
- [1920×1080 landscape, Large text](screenshots/room-content-uat-landscape-placement.png): information viewport above Cancel/Confirm, then Save/Discard.
- [Selected-content inspector](screenshots/room-content-uat-inspector.png): localized identity/room, actual Monsters category, Move/Reassign/Unassign, no raw coordinates.

Tests also measure 720×1280 portrait and 1024×600 fallback landscape at Small/Default/Large text. Information scrolls; critical action rows remain reachable. Editor and Windows manual owner rerun remains pending.
