# Amendment A implementation contract

PR 2 implements independent room placement and supported route insertion through the transactional Dungeon editor. The original linked tail action remains available for the initial two-room setup. Its production path built the free first Basic Room at `(0,2)`, used normally generated mana to append at `(0,6)` with the north terminal opening, and produced a verified zero-investment direct A → B edge. The retained canonical evidence includes the Completion Terminal at `(1,10)`. The existing starter profile supplies the same geometry.

## Draft and economy

Format 6 placements bind the floor, room definition, anchor, orientation, and expected room identity. An accepted disconnected placement can be repositioned without changing its room or node ID. A complete insertion binds the retired baseline edge, new room, exact selected openings and tile sequences, and two expected newly allocated edge IDs. Revisions replace the draft insertion without changing unrelated layout. Recovery replays the acknowledged prefix and verifies the allocations. Historic formats keep frozen field-order readers. A disconnected room blocks Save Changes, and Discard has no canonical effects.

The production editor offers an optional bounded suggestion and separate selection, passage editing, and acceptance. Suggestions compare gross quotes when all shortlisted valid candidates can be priced by the existing authority. Otherwise they prefer shortest valid geometry with stable ties. The current edit has no refund or reuse claim. Eligibility checks the exact canonical ledger and corridor ownership at preview and commit. The existing whole-dungeon writer prices the final delta once, uses existing formula order and reserve checks, and publishes the save atomically. Legacy tail construction, move, replace, and delete block structural edits on a floor whose authored links they cannot preserve.

## Geometry and save authority

Native root schema 19 uses spatial registration 19. Root 18 retains its frozen registration 17 and has no migration into 19. Prior development roots are refused explicitly and kept intact. Existing compatibility registrations and their geometry records retain their exact content. The new registration selects the original starter geometry.

An authored edge stores source and destination point IDs and a source-to-destination tile array. The destination ID is empty only at an optional cap. Direct edges have an empty passage. Inferred legacy producers retain their straight occupancy records. An edge takes one representation and mixed data fails validation. Record order remains canonical, while the authored passage order is preserved exactly. Occupancy and wall crossings are derived. Semantic validation checks the catalog, endpoint joins, used openings, floor bounds, occupancy, configured capacities and route order. The approved production catalog adds Basic Room points `east.2` and `north.2` while preserving the original points. The Straight Stone definition keeps its ID, sockets, one-tile width and content limits, with an explicit orthogonal category and six occupied tiles at most.

H1 precise timing and recovery and paid Research timing retain their existing runtime semantics. The complete-save validation budget projection checks the new geometry without changing older save bytes. Admitted parties retain immutable checkpoints and exact movement order. Newly admitted parties capture the current geometry and graph order. Branch assignment ordering and terminal tiles use the captured passage and historical straight edges keep their original order. Rendering opens only the selected doorway joins and the consecutive corridor steps. Room-local combat keeps its existing authority.

## Evidence and review

The pre-integration Windows Editor PlayMode run at `a8047255` proved the native construction path. It recorded a 110-mana insertion at 60/60 floor space, exact reopen, an unchanged old-party checkpoint, and the next scheduler run through A → N → B with movement `(5,7) → (4,7) → (3,7)`. The Floor 2 movement fixtures passed at the same source for the 80- and 76-tile arrangements across closed side contacts. The approved exporter previously reproduced the three generated content files byte for byte.

The integration merges main `ecf52331` and retains the historical migration, refusal and native-boot coverage qualified in PR #276. Schema 19 remains the only current writer. The Foreground golden comparison projects the outer root and inferred-edge spatial member through the frozen serializer. It verifies the exact catalog-bound migration and H1 input identities before projecting the corresponding historical values. It also verifies the untouched current-schema round trip and readback. New checkpoints for inferred routes retain the historical embedded floor byte shape. Authored floors capture the full geometry, and reopening preserves the originally captured checkpoint.

Automated results for the integrated source belong to their source-pinned campaigns linked from PR #275. Previous verdicts and receipts remain historical evidence. Independent review determines UAT readiness. The temporary development UI supports the accepted Small text option at 1024 × 600 landscape. Large text at that resolution remains a release-candidate issue.

Scope follows [Spec 38](../38%20-%20Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md) and the [Amendment A handoff](../../docs/planning/decisions/amendment-a-construction-contract-2026-11-21.md).

## Owner UAT interaction clarification (2026-11-26)

Owner UAT at PR #275 head a6961215 confirmed independent room placement and rejected the bottom-menu route workflow. In supported insertion contexts, the room card starts independent placement. Linked append remains available for initial progression and as an explicit end-of-route action where supported.

After confirming placement, tap the preceding room or eligible fixed structure, the new room, and the following room or eligible fixed structure directly on the map. Use the existing retirement authority to resolve that sequence. A complete supported sequence automatically previews a bounded suggestion. Map markers show visit order, the disconnected room, the proposed links, doorway choices and segment handles. Tap openings on the map, or tap and move a segment or drag its handle. Supporting controls show the price, space and blockers and allow revert, acceptance and final Save Changes.

This interaction uses the existing restricted insertion, ordered geometry, draft and atomic publication contracts. Accept Route records an explicit draft edit. Save Changes publishes the valid draft. The original automated qualification remains evidence for its recorded head. Replacement owner UAT requires independent approval of the corrected build.
