# Amendment A implementation contract

This checkpoint implements independent room placement in the transactional draft. The native progression checks use the production Dungeon screen and a fresh save. The existing linked tail action creates the initial two-room geometry using normal mana and a free first room. It remains a separate action from independent placement.

## Draft authority

A format 6 placement command records the chosen floor, room definition, anchor, orientation, and expected native room identity. It adds the room and its route node, keeping all prior edges and fixed structures. Recovery replays the acknowledged commands against the bound baseline and rules and checks the allocated identity. Format 5 and earlier command and record bytes keep their frozen readers. A disconnected draft blocks Save Changes. Discard has no canonical effects.

The insertion command will record the retired edge, new room and the exact two proposed links. A suggestion has no persistent effect. The canonical investment and corridor ownership records must prove retirement eligibility at preview and final commit. Allocation uses the existing floor-scoped authorities. Save Changes will price and publish the complete validated result once.

## Geometry and canonical save extension

The proposed current root is native schema 19 and uses spatial registration 19. Registration 17 remains the spatial contract for roots 17 and 18. A current authored edge will store a source point ID, a destination point ID (empty for an optional cap) and a source-to-destination array of corridor tiles. A direct link has an empty passage. Existing straight producers may retain their inferred geometry representation in the current registration while their frozen persisted profiles and authority remain distinct from authored edges. Each authored edge has one passage authority. Its sorted occupancy and open boundaries are derived. Exact field shape and byte round-trip reject mixed geometry representations, and semantic validation rejects unresolved openings, conflicting usage, disallowed sockets, invalid steps and extra routes. The corridor category will distinguish the new orthogonal contract from the frozen straight-axis meaning.

The complete-save validator, native creation and load dispatcher must agree on the new registration and reject obsolete development roots without modifying originals. No schema-18 migration is planned. H1 precise clock and atomic recovery semantics and Research timing fields continue under the new root. Current-schema active parties keep their own captured paths and resume from their exact checkpoints. The authored path is captured for subsequent admissions without introducing a new checkpoint field or interpreting an earlier run against the current floor. Historic executable and fixture paths remain pinned to their wire contracts.

## Validation scope

Unit tests exercise legal and invalid placement, path geometry, record shape and exact rehydration. SIT uses production content and normal UI, tests affordability, save authority, Windows reopen and traversal, and qualifies content export and regressions. Owner UAT uses the normal fresh-save path and an isolated Floor 2 side-contact fixture after independent approval. Gameplay scope follows [Spec 38](../../Docs/38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) and the [Amendment A handoff](decisions/amendment-a-construction-contract-2026-11-21.md).
