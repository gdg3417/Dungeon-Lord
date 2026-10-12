# M6-FU0: Active-run cooling composition

## Status

Accepted.

## Date

2026-05-30

## Context

M6-I0 closed M6 as GO WITH FOLLOW-UP pending an explicit decision about the retained two-stage active-run cooling composition:

1. `RunHeatDeltaSummary` includes loot cooling through `RunHeatLootCoolingPerExtractedValue` as part of run heat delta composition and persisted run diagnostics.
2. `GameRoot.SimulateRunOnce` also executes the pre-existing `LootHeatCoolingResolver` step using separately config-owned cooling values as part of the active-run loot heat cooling diagnostic and behavior path.

## Decision

Retain the current two-stage active-run cooling behavior intentionally for now. Do not change behavior until a separately scoped balance or heat-model refactor explicitly replaces it.

## Rationale

The first stage belongs to run heat delta composition and persisted run diagnostics. The second stage belongs to the pre-existing active-run loot heat cooling diagnostic and behavior path. Recording that separation resolves the M6-I0 follow-up without changing behavior.

## Consequences

- This is a documentation-only decision, not a tuning change.
- This does not change heat math, loot logic, diagnostics behavior, run outcomes, config, or save behavior.
- Active-run cooling continues to use both stages until a separately scoped behavior PR replaces the composition.

## What this does not authorize

- This does not authorize offline heat processing.
- This does not authorize M7 to alter M6 heat behavior.
- This does not start M7 or authorize unrelated gameplay, UI, or content work.

## Follow-up trigger conditions

Open a separately scoped behavior PR before changing the active-run cooling composition, cooling ownership, heat math, or diagnostic behavior. A future balance pass or heat-model refactor may trigger that work.

## Owner-approved living-dungeon addendum (2026-10-12)

See the [Living Dungeon Owner Design Lock](living-dungeon-owner-design-lock.md) and [Cross-Spec Glossary amendment](../../../Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md#living-dungeon-owner-approved-amendments-2026-10-12). The original M6-FU0 decision remains authoritative for fully foreground outcomes: both stages are retained, and this addendum does not alter their tuning or behavior.

The historical “does not authorize offline heat processing” limitation is superseded for future living-dungeon implementation only. Offline outcomes may affect numeric Heat within the tier established at the accepted offline interval boundary; prevented excess is discarded and not banked, and no second login rebound applies to that run-derived Heat. For mixed foreground/offline runs, effects are attributed to their causal activity periods. The exact allocation of aggregate bonuses and both cooling stages is an open implementation contract requiring validation against existing authorities. This addendum authorizes no new coefficient, formula, tuning, or change to fully foreground outcomes.
