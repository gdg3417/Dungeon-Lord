# M7-A2 Research Pending Dev Controls and Validation Scaffold

## Scope delivered

M7-A2 adds developer-only Bootstrap controls and deterministic validation for the existing nullable, single-slot `ResearchPendingState`. The Bootstrap dev panel can set the configured scaffold project or clear the saved scaffold state, and Systems Diagnostics re-renders the current pending state after either action.

## Explicit non-goals

This slice does not add a research system. It does not add research timers, elapsed progress, percentages, completion timestamps, completion handling, costs, rewards, unlocks, multiple slots, or production research UI. It also does not add offline rewards, offline heat processing, server verification, raids, diplomacy, events, seasons, leaderboards, or monetization.

## Config ownership notes

The content-owned `researchPendingScaffold` block in `Assets/_Project/Data/Bootstrap/content_bootstrap.json` owns the enabled flag, the one scaffold slot ID, the one scaffold project ID, and the diagnostic rule-source ID. Runtime set behavior consumes that loaded config rather than embedding scaffold IDs in code.

## Save compatibility notes

M7-A2 reuses the additive nullable `SaveData.researchPending` field introduced by M7-A0. No save-schema bump or new save field is added. Legacy saves with no `researchPending` field remain a safe no-pending state.

## Dev-control behavior notes

The existing Bootstrap developer panel path now includes localized `Set Research Pending` and `Clear Research Pending` controls. Set validates loaded scaffold config before replacing only `Save.researchPending`; clear sets only `Save.researchPending` to null. Both actions use the existing `SaveService.Save(..., SaveReason.ManualDev)` path when a save service is attached and then refresh diagnostic lines. They do not invoke simulation passes or apply rewards.

## Diagnostics notes

Systems Diagnostics retains the offline summary and pending-state lines and adds a localized research-pending validation line. Pending diagnostics are rendered from the current saved scaffold state so set immediately shows `pending=True` with the configured slot/project IDs and clear immediately shows `pending=False` with empty slot/project values. Missing localization safely renders localization keys.

## Determinism and validation notes

`ResearchPendingResolver` is pure and single-slot-only. Null, default, or empty-project states resolve as no pending research. A pending project always normalizes to the one configured slot. Missing, disabled, empty-slot, or empty-project scaffold config returns deterministic disabled-safe errors. The scaffold resolver never advances or completes research.

## Test list

Unity EditMode coverage added or updated:

- `ResearchPendingResolverTests`
  - null/default/empty state reports pending false;
  - configured scaffold reports pending true;
  - any saved slot normalizes to the configured single slot;
  - missing, disabled, empty-slot, and empty-project config return deterministic disabled-safe results.
- `ResearchPendingDevControlsTests`
  - set writes only `Save.researchPending` and refreshes diagnostics;
  - clear clears only `Save.researchPending` and refreshes diagnostics;
  - invalid config does not mutate the save and uses safe localization fallback;
  - `ResearchPendingState` remains exactly a two-field single-slot scaffold with no progress or completion fields.
- `OfflineSummaryDiagnosticsTests`
  - Systems Diagnostics includes localized offline summary, pending state, and validation lines;
  - legacy, persistence, safe-key fallback, and no-offline-effects diagnostics coverage remains intact.
- Existing M6 tests remain the regression suite for heat behavior, including `LootHeatCoolingResolverTests`, `RunHeatDeltaResolverTests`, and `RunHeatStateApplyResolverTests`.
- Existing `BootstrapOverlayPagingTests` remain the regression suite for F1/F2/F3 overlay paging behavior where applicable.

## Manual Bootstrap smoke checklist

1. Open the Bootstrap scene.
2. Enter Play Mode.
3. Confirm F1 dev panel still works.
4. Confirm F2 run diagnostics focus still works.
5. Confirm F3 diagnostics page cycling still works.
6. Go to Systems Diagnostics.
7. Confirm Research Pending initially displays the current saved scaffold state.
8. Use the dev panel Set Research Pending control.
9. Confirm Systems Diagnostics shows `pending=True` with the configured slot/project ID.
10. Confirm no research progress or completion occurs.
11. Use the dev panel Clear Research Pending control.
12. Confirm Systems Diagnostics shows `pending=False` with empty slot/project.
13. Confirm no mana, loot, research progress, heat changes, structure ticks, or run history entries are granted from offline time.
14. Confirm no offline heat behavior appears.
15. Confirm no M6 heat behavior changes.
16. Confirm no unexpected `.meta` files are created.

## M6 preservation notes

M7-A2 does not modify active-run heat delta composition, the accepted M6-FU0 two-stage active-run cooling composition, `LootHeatCoolingResolver`, run simulation math, loot logic, structure simulation logic, or existing tuning values.

## Confirmation

M7-A2 is developer-only scaffold control and validation work. It does **not** implement offline rewards, research progress, research completion, offline heat, production UI, or server verification.
