# Actual production capture review

The focused captures were inspected before full qualification. These two final captures were regenerated during the full PlayMode run and visually reviewed before the Windows build; the collector checks timestamps against that run and retains SHA-256 hashes.

- [Duplicate Hoard, 1920x1080](screenshots/loot-reassignment-duplicate.png): source and destination each contain one Hoard. Destination Room 2 reads **1 / 2**, so the invalid red cell is not a full-room rejection. The sheet explicitly says **This room already contains this loot type. Choose another room.** Confirm is disabled; Cancel and Discard remain reachable. No confirmed changes or purchase are shown.
- [Completed sequential exchange, 720x1280](screenshots/loot-reassignment-sequential.png): Hidden Cache is in Room 2, Basic Loot and Hoard are in Room 1. Room 2 reads **1 / 2**, Owned is 0, and the draft quote is **0 mana cost** with the existing wallet preserved. Save and Discard remain available. Test output records both rooms' counts at each intermediate/final stage; the screenshot is a final visual sample, not the sole proof of the sequence.

Existing placeholder mappings, accepted bottom-sheet composition and artwork are unchanged. The clipped lower browsing hint can scroll in its established context area; this task adds no layout or art pass. No standalone runtime UAT is claimed from Editor captures.
