# Phase 4A structural economy contract

Implementation baseline: PR #200, `8093f2f886b8f5c40de67867c85623b875f7a550`. This packet is subject to external review and later PlayMode/manual qualification.

## Authoring and formulas

The sole production authority is `Assets/_Project/Resources/structural_economy.json`, loaded as a TextAsset through Unity Resources by GameRoot. A dedicated economy catalog avoids adding prices to the geometry authoring/export pipeline or frozen migration inputs. Immutable snapshots validate an exact match to production room/corridor IDs, reject duplicates, missing/extra IDs, nonfinite/negative values, invalid percentage factors, nonpositive capacity and nonpositive undo duration. Tests may inject snapshots. Entries are indexed in ordinal order; authoring changes require neither code nor save migration.

| Seed | Value |
|---|---:|
| Basic Room | 100 mana |
| Rectangle Room | 100 mana |
| Large Chamber | 200 mana |
| Straight Stone Corridor | 5 mana per occupied tile |
| Direct Doorway | No independent physical corridor charge |
| Movement factor | 0.10 of current room base price |
| Replacement factor | 0.10 of old room base price |
| Deletion refund | 0.75, rounded down |
| Mana storage capacity | 1000 |
| Session renovation undo | 30 seconds |

These are provisional tuning seeds. Basic Room's design target is 5–10 minutes of baseline early progression after a productive starter dungeon, initially 7.5 minutes. Runtime prices consume authored mana values, never elapsed real-world minutes.

Construction sums room base plus newly created physical corridor tiles. Replacement adds its fee to `max(0, newBase - oldBase)`; downgrades never generate mana. Movement charges the selected room only and never compounds historical investment or adds a corridor movement surcharge. Cost evaluation delegates to FormulaEngine, including its final midpoint-away-from-zero rounding. The modifier input uses the existing ordered bucket contract; production supplies neutral modifiers. No active Architecture, floor, theme or research values are invented.

## Save and investment authority

`StructureRuntimeState.ManaReserve` remains the only writable balance. Schema 9 appends `structuralInvestment` after the existing canonical authority, floors and lifecycle/ownership members in the complete primary save. The spatial schema and its fingerprint remain unchanged. The complete-save session owns the ledger; it is not a second wallet or a runtime projection field.

The ledger has exactly one record for each live room and route edge, including doorways with zero initial investment. Fields have strict order: `StructureId`, `ConstructionMana`, `RenovationMana`. Records are ordinal by stable identity. Missing, duplicate, negative, nonfinite, unknown, out-of-order and noncanonical numeric records fail closed. Geometry validation remains independent of investment and wallet state. Ledger records are bounded by the canonical record limit, and the complete payload including the ledger must pass existing serialized node/collection/byte and raw scan/array/byte budgets before persistence.

Construction records the final amount charged. Corridor portions receive their proportional share of the final total rounded down; the room receives the remainder. Thus allocations always sum to the actual paid total even when future flat/percent modifiers and rounding change the total. Movement/replacement adds actual paid renovation mana to the surviving room identity. Existing edge IDs retain investment even when renovation changes between physical corridor and doorway representations.

Retirement follows the existing inverse-tail lifecycle: the old terminal edge's investment transfers to the new incoming relationship on construction (same surviving predecessor). On deletion the removed incoming edge's investment transfers to the new predecessor-to-terminal relationship, because that relationship/geometry survives. Investment of the removed room and outgoing edge is retired and forms the refund basis. A transferred record is excluded from refund, so there is no duplicate ownership or refund. When reconstruction creates a new physical corridor, its new actual paid investment is added to the transferred historical basis. No catalog repricing changes historical records.

Schemas 1–6 retain their frozen migration into schema 7, then explicitly transition 7 → 8 → 9. Schema 8 upgrades directly to 9 without passing through legacy projection. All old structures receive zero paid investment; unrelated recognized and extension state, balance, identities, counters, assignments, custody and run history are preserved. The schema 8 boundary parser remains explicit. Current schema 9 load is a no-op; migration refuses already-current input. Schema 7/8 starter/contract records remain intact and schema 9 adds its own selection with the same approved geometry.

## Transactions and undo

The write authority refreshes spatial validity and then computes current price/affordability using current live ManaReserve. Displayed economy results are never commit authority. Structural staleness still uses the unchanged spatial fingerprint. Exact balance succeeds. Insufficient funds return `structural.economy.insufficient_mana` before persistence, without publishing geometry, mana, ledger or allocator changes.

Spending/refunding changes only the detached recognized-state snapshot and detached investment ledger. One complete candidate goes through validation, session reopen, exact atomic persistence and durable readback before runtime publication. Failed operations leave runtime unchanged, and existing expected-byte/stale-preview rules prevent duplicate spend/refund on retry.

Refunds floor the configured fraction of removed historical investment. The single economy snapshot owns storage capacity. Newly credited mana is limited to remaining capacity; an existing over-cap balance is preserved and gets no added refund. Spending naturally reduces an over-cap balance. This packet does not change existing generation ticks or implement offline accumulation; the same capacity authority is available to those later consumers.

SaveService privately owns one pending renovation inverse in memory. Stopwatch's monotonic timestamp/frequency is the production elapsed-time source, injectable in tests. A successful move/replacement captures the previous spatial state, ledger and exact charged delta. Undo restores those with current unrelated recognized state and adds back exactly the renovation charge, without the deletion refund percentage or capacity clamp. A failed undo persistence retains the pending inverse until expiry. At the configured deadline undo is unavailable. Successful canonical edits (including content assignment, which could otherwise be lost by restoring a layout snapshot), starting a run, service replacement/reopen, or successful undo invalidate the previous inverse. Another renovation can establish a new inverse. There is no serialized undo field and no multi-step stack.

The existing initial implicit/explicit starter container and content placement flows remain free, with zero structural investment: this packet supplies no starting grant and no acquisition transaction. Canonical room-definition changes continue through renovation; duplicate basic-room placement remains a no-op.

## Presentation and deferred work

Bootstrap composes the economy preview around the existing pure spatial preview. Localized text shows base/final cost or removed historical basis/nominal and credited refund, current/resulting mana, affordability, insufficient mana and undo availability. Spatial capacity and consequence descriptions remain alongside this information. Raw IDs and internal reasons are not displayed as fallback copy.

Approved follow-up: monster/trap/loot acquisition should be substantially cheaper than a Basic Room; a basic monster + basic loot or basic trap + basic loot starter package targets 25%–40% of Basic Room price. A future starting grant must cover either package without also funding an immediate second Basic Room. Implement only after distinguishing new acquisitions from redeployment of already-owned returned content; placement must not charge ownership twice. No content prices or starting grant are implemented here.

Offline mana remains the next independent Phase 4 packet. Active Architecture/floor/theme/research modifiers, floor expansion pricing, upkeep, Floor 2, branches, additional content and a production editor are outside this packet. PlayMode, manual gameplay and standalone build qualification are deliberately deferred until external architecture review.
