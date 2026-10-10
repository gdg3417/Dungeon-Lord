# Graphical room-content management qualification

Baseline: merged PR #230, `5932a9fe0b7cec87c81f09e8e7d619df62c53101`. Branch: `codex/phase7-graphical-room-content-management`. Selected configuration: GPT-6.1 Sol, High; Complex. The approved packet extends established authorities across draft identity, custody, economics, persistence and UI. GPT-6 Sol High is the availability fallback; no substitution or new ownership policy was needed.

## Reconciliation and authority

PR #229 room construction and PR #230 production composition are merged dependencies. Phase 7A1/schema 13 supplies durable exact room-local coordinates; A2/A3 supply deterministic spatial engagement; A4/A5 supply the whole-dungeon transactional editor, movement, durability and recovery. A0 Decisions 38–43 require room-first categories, ownership-preserving management and one final transaction. Specs 38 and 28, the glossary, Phase 4A economy and current acquisition/removal/occupancy contracts remain authoritative.

The reusable detached spatial mutation handles NEW acquisition, unassignment and redeployment. Reassignment composes detached unassignment and redeployment as one replayable command. Reposition reuses exact-position validation. These operations mutate only the draft read model. No live acquisition or custody write is used during Edit Mode. Stable IDs, option/category/sequence and canonical ordinal ordering remain owned by the existing contracts. Placement evaluation uses injected occupancy and configured workload limits. No new gameplay values or content definitions are introduced.

`StructuralEconomyService` compares baseline owned identities (assigned plus returned) against the complete final ownership set. Only new identities are priced using `ContentAcquisitionEconomySnapshot`; a purchased item unassigned into custody still costs its acquisition price. Returning/redeploying/reassigning an existing identity costs no acquisition mana. Content never enters structural investment/refund accounting. Final purchase eligibility uses the current restricted-action gate; affordability uses the current wallet. The existing `DetachedCanonicalWriteAuthority` validates the full resulting state, prepares one complete save, persists exact validated bytes, performs durable readback and then publishes. Run snapshots remain immutable and material floor-knowledge fingerprints retain their existing applicability behavior.

Save schema remains **13**, with no migration or save DTO changes. Draft journal vocabulary is **5**, and its separate immutable candidate/commit record version is **4**. Frozen journal formats 2–4 and record versions 1–3 retain their exact serialization. New evidence appends to the existing predecessor chain without rewriting older files. The frozen A5 material-rule fingerprint is preserved; custody commands separately bind the exact removal-policy fingerprint. Unknown durability blocks further accepted commands/commit and requires independent recovery evidence. Historical compatibility and corruption assertions remain active.

## Player behavior

Select a constructed room, enter Edit Mode, then choose Monsters, Traps or Loot. The bottom sheet shows current capacity, assigned items, returned owned items and actual configured new options/prices. Select a choice, tap an exact local cell, inspect occupied-cell validity, then Confirm Placement. Confirmation changes the local draft; Save Changes reviews the combined transaction and commits the entire dungeon. Existing assignments support Move, Unassign and explicit destination-room Reassign. Returned choices redeploy the same identity without purchasing again. Preview cancellation adds no journal command; Discard removes the draft without creating ownership or spending mana.

Room inspection, Focus Room, Fit Floor, floor navigation, camera controls and the category bar remain. Input System mouse/touch, portrait/landscape and Small/Default/Large text are qualified. Detailed economics remain in the final review while a compact quote stays beside Save/Discard. Existing artwork is reused; options without authored art retain the catalog's fallback. Player strings are localization keys with English fallback.

## Qualification record

Automated results, source checkpoints, owner preservation, build and artifact inventory will be finalized after complete qualification. Raw intermediate failures are retained with their dispositions; no Unity or Windows pass is inferred from source inspection.

The existing disposable project `C:/Dev/Dungeon-Lord/Temp/ui-composition-floorhud-validation` is reused with its Library intact. See [pre-reuse inventory](validation-inventory.md). All four input roots and generated metadata remain strictly compared; target-only files are rejected. Only the existing reviewed save-name substitutions are permitted. No owner project execution or owner save namespace is used. No output/cache deletion or quarantine is performed.

## Retained limitations and owner UAT

External review and owner UAT are pending. The owner journey covers adding one monster/trap/loot, invalid overlap/capacity recovery, moving/unassigning/redeploying, reviewing and saving, closing/reopening, discarding later edits, focused-room readability and equivalent Windows behavior. Automated tests cover transactional edge cases so owner UAT can focus on comprehension and gameplay interactions.

No Floor 2 research/construction/lifecycle UI, new content, combat expansion, loot-table authoring, corridor content editor, research tree, entrance movement, capacity rebalance, new floor bounds, landscape side inspector, art-theme framework, Bootstrap retirement, broad refactor or unrelated migration is included. Existing straight-corridor construction limits and the absence of a live mana-reservation owner remain separate dependencies. No specification decision requires new approval in this packet.
