# M7-A3 Offline and Research Scaffold Closeout Audit

## 1. Purpose

This documentation-only audit closes the merged M7 scaffold phase after M7-A0, M7-A1, and M7-A2. It reviews the repository state at merge commit `b450da0` and records whether the offline-summary diagnostics, persisted last-offline-summary diagnostics, and research-pending developer controls remain deliberately small, deterministic, localized, save-compatible, and safe to build on.

This M7-A3 PR does not change C# runtime code, tests, JSON config, localization tables, Unity scenes, prefabs, assets, save schema, gameplay tuning, simulation logic, heat math, loot logic, structure simulation logic, run outcomes, research behavior, or diagnostics behavior. M7-B0 has not started in this PR and must not begin until this audit is merged.

## 2. M7 scaffold scope summary

M7 currently provides diagnostics and developer scaffolding only:

- a read-only `OfflineSummaryResolver` and serializable `OfflineSummary` model;
- an additive nullable `SaveData.researchPending` state representing one pending research slot;
- an additive nullable `SaveData.lastOfflineSummary` diagnostics snapshot;
- localized Bootstrap Systems Diagnostics lines for the offline summary, current pending state, and research-pending validation state;
- content-owned `researchPendingScaffold` IDs and validation metadata;
- a pure, deterministic `ResearchPendingResolver`; and
- Bootstrap developer-panel controls that set or clear the configured single pending scaffold state.

The scaffold does not process offline progression and does not implement research progression. Its purpose is to expose stable seams for a separately scoped next increment.

## 3. Increment inventory

| Increment                                                   | Merged delivery                                                                                                                                                                                                                                                                                            | Audit result                                   |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| M7-A0 Offline Summary and Research Pending Scaffold         | `OfflineSummary`, `OfflineSummaryResolver`, additive nullable `researchPending`, localized Systems Diagnostics lines, safe key fallback, and EditMode coverage. Evidence: [`M7-A0-offline-summary-and-research-pending-scaffold.md`](M7-A0-offline-summary-and-research-pending-scaffold.md).              | Complete. Read-only and scaffold-only.         |
| M7-A1 Last Offline Summary Diagnostics                      | Additive nullable `lastOfflineSummary`, one boot/session capture before the existing boot-save timestamp advance, persisted snapshot rendering, legacy/default-object fallback, and EditMode coverage. Evidence: [`M7-A1-last-offline-summary-diagnostics.md`](M7-A1-last-offline-summary-diagnostics.md). | Complete. Diagnostics persistence only.        |
| M7-A2 Research Pending Dev Controls and Validation Scaffold | Content-owned `researchPendingScaffold`, pure `ResearchPendingResolver`, localized validation diagnostics, Bootstrap developer-only set/clear controls, and EditMode coverage. Evidence: [`M7-A2-research-pending-dev-controls.md`](M7-A2-research-pending-dev-controls.md).                               | Complete. Single-slot developer scaffold only. |

## 4. What is complete

- Offline-window observation has a deterministic resolver contract with an injected clock.
- Every offline-summary resolution path initializes `WouldProcessOfflineProgress` to `false`; no path turns it on.
- The most recent boot/session offline-summary snapshot can be persisted and rendered as diagnostics without recomputing a misleading near-zero window after the existing save path updates `lastSavedUtcUnix`.
- Legacy saves with no M7 fields remain safe: missing or default/empty additive fields are treated as absent scaffold state and resolved through safe fallback behavior.
- Research pending is represented by exactly one nullable state object with a slot ID and project ID.
- Set and clear controls exist only in the existing Bootstrap developer panel, validate or clear scaffold state, save through the existing manual-development save path, and refresh diagnostics.
- Research-pending scaffold IDs and rule-source metadata are loaded from content rather than embedded in set-control runtime logic.
- New visible M7 diagnostics, controls, and control-result banners are localization-table-owned and use stable-key fallback.

## 5. What is explicitly not implemented

M7-A0 through M7-A2 do **not** implement:

- offline mana, offline rewards, offline loot, offline heat processing, offline structure ticks, offline run history, or any other offline simulation pass;
- research elapsed progress, progress percentages, progress accumulation, completion timestamps, timers, costs, rewards, unlocks, completion handling, or completion grants;
- multiple research slots or a production research UI;
- any change to active-run M6 heat behavior, the accepted two-stage active-run cooling composition, heat math, loot logic, structure simulation logic, or run outcomes;
- server verification; or
- Hostile, Raid, raid warnings, raids, diplomacy, reputation axes, events, seasons, leaderboards, monetization, or other post-scaffold systems.

The scaffold must not be interpreted as authorization for any of those behaviors.

## 6. Guardrail compliance review

**Result: PASS for the merged M7 scaffold and for this documentation-only closeout.**

- M7 runtime logic adds no gameplay reward formula, heat formula, research timer, research cost, or gameplay tuning value.
- Offline observation consumes config-owned `timeRules.maxOfflineSeconds` and `timeRules.offlineSummaryRuleSourceId`.
- Research developer scaffolding consumes the loaded `researchPendingScaffold` block rather than hardcoding scaffold IDs in the set-control method.
- M7-visible strings are looked up by stable localization key and retain stable-key fallback.
- The M7-A3 closeout changes documentation only.

This audit does not retroactively approve older Bootstrap scaffold literals outside the M7 scope. Any legacy guardrail cleanup remains separately scoped and must not be mixed into M7-B0.

## 7. Config ownership review

**Result: PASS.**

The existing `timeRules` content block owns the offline-summary observation cap and rule-source ID:

- `maxOfflineSeconds` owns the maximum observed offline window;
- `offlineSummaryRuleSourceId` is `offline.summary.rule.m7_a0_scaffold`; and
- `allowOfflineProgression` remains present from earlier project setup but is not consumed by the M7 resolvers or controls.

The M7-A2 `researchPendingScaffold` content block owns:

- `enabled`;
- the one scaffold `slotId`, `research.slot.primary`;
- the one scaffold `projectId`, `research.project.m7_a2_scaffold`; and
- `ruleSourceId`, `research.pending.rule.m7_a2_scaffold`.

The research-pending rule-source ID is present and surfaced through validation diagnostics. No config value is changed by this closeout audit.

## 8. Localization review

**Result: PASS.**

The M7-visible additions are localization-table-owned:

- `ui.dev.offline_summary_format`;
- `ui.dev.research_pending_format`;
- `ui.dev.research_pending_validation_format`;
- `ui.dev.button.set_research_pending`;
- `ui.dev.button.clear_research_pending`;
- `ui.banner.research_pending_set`;
- `ui.banner.research_pending_set_failed`; and
- `ui.banner.research_pending_cleared`.

Runtime diagnostics and controls request stable keys through content lookup. If a new diagnostic format is unavailable, rendering falls back to its stable key rather than embedding new English display text in runtime code. The localization table can be extended with additional languages without changing M7 runtime logic.

## 9. Save compatibility review

**Result: PASS.**

`SaveData.researchPending` and `SaveData.lastOfflineSummary` are additive nullable fields. Neither M7-A0, M7-A1, nor M7-A2 bumps the save schema or requires a migration rewrite.

Unity `JsonUtility` can materialize a missing legacy object field as a default object rather than `null`. The merged scaffold handles both cases:

- an absent, default, or empty-project `researchPending` object resolves as no pending research; and
- an absent or default/empty `lastOfflineSummary` object is not treated as a meaningful persisted snapshot, so diagnostics safely use resolver fallback.

The saved research scaffold remains exactly one object with `SlotId` and `ProjectId`. It contains no progress, completion, reward, unlock, cost, or timer fields.

## 10. Determinism review

**Result: PASS.**

`OfflineSummaryResolver` accepts `ITimeSource`, uses integer Unix timestamps, and resolves from save state plus loaded time rules. For identical inputs and a fixed injected clock, it returns identical output. Invalid timestamps, clock reversal, missing save data, and invalid time rules return deterministic error results without mutation.

`ResearchPendingResolver` is pure. Null, default, or empty-project state resolves to no pending research. A non-empty saved project resolves against the configured single scaffold slot. Missing, disabled, empty-slot, or empty-project scaffold config produces deterministic disabled-safe validation errors.

Diagnostics formatting reads resolved state. It does not apply simulation passes, rewards, or completion behavior.

## 11. Diagnostics review

**Result: PASS.**

Bootstrap Systems Diagnostics retains localized lines for:

1. offline summary;
2. current research-pending state; and
3. research-pending validation state.

The offline-summary line includes `wouldProcess`, and the underlying summary remains `WouldProcessOfflineProgress == false`. `SaveData.lastOfflineSummary` is diagnostics-only: it is captured from resolver output, preferred for later display when usable, serialized through the existing save path, and never consumed by gameplay mutation code.

Set and clear actions refresh the diagnostics so the current saved pending scaffold is immediately visible. Refreshing diagnostics or cycling diagnostics pages does not grant mana, loot, research progress, heat changes, structure ticks, run history entries, or research completion.

## 12. Developer-control review

**Result: PASS, with the timestamp side effect documented as accepted existing-save behavior.**

The M7-A2 set and clear controls are part of the existing Bootstrap developer panel, not a production research UI. The overlay renders that panel only when the loaded `featureFlags.enableDevPanel` is enabled and the F1 panel is visible.

- Set resolves the configured scaffold through `ResearchPendingResolver.ResolveScaffold`, replaces only `Save.researchPending` when validation succeeds, saves with `SaveReason.ManualDev`, and refreshes diagnostics.
- Clear sets only `Save.researchPending` to `null`, saves with `SaveReason.ManualDev`, and refreshes diagnostics.
- Invalid scaffold config prevents set mutation and returns a safe failure result.

The existing `SaveService.Save` path updates `lastSavedUtcUnix` for manual-development saves. This is acceptable for the developer control: it is the pre-existing save timestamp contract, not an offline-progression pass. The controls do not calculate an elapsed reward window, invoke offline simulation, grant resources, change heat, tick structures, append run history, or complete research.

## 13. Offline progression exclusion review

**Result: PASS.**

Offline behavior remains observational only:

- `OfflineSummaryResolver` always reports `WouldProcessOfflineProgress == false`;
- the resolver clamps the observed diagnostic window but applies no reward or simulation logic;
- `lastOfflineSummary` persists diagnostic output only;
- `allowOfflineProgression` exists in `content_bootstrap.json` from earlier setup but has no M7 runtime consumer beyond its typed model field; and
- repository search finds no M7 offline reward, offline heat, structure-tick, run-history, loot, mana, or research-completion application path.

No offline heat behavior and no offline rewards are implemented.

## 14. Research progress exclusion review

**Result: PASS.**

Research pending remains a nullable, single-slot marker only. The state model contains exactly `SlotId` and `ProjectId`. The scaffold config contains one enabled flag, one slot ID, one project ID, and one rule-source ID. The resolver validates and normalizes that state but does not advance it.

There are no research progress values, percentages, timers, elapsed-time fields, completion timestamps, completion transitions, costs, rewards, unlocks, or grants. There is no production research UI and no multi-slot collection.

## 15. M6 preservation review

**Result: PASS.**

M6-FU0 records the accepted two-stage active-run cooling composition:

1. run heat delta composition includes loot cooling through `RunHeatLootCoolingPerExtractedValue`; and
2. `GameRoot.SimulateRunOnce` also retains the pre-existing `LootHeatCoolingResolver` active-run step.

Repository review of the M7 merged range shows no M7 change to M6 gameplay resolver files or M6 heat regression test files. M7 does not alter the accepted composition, heat math, loot logic, structure simulation logic, diagnostics behavior, or run outcomes. Offline heat remains unimplemented.

Reference: [`M6-FU0-active-run-cooling-composition.md`](../../../planning/decisions/M6-FU0-active-run-cooling-composition.md).

## 16. Test coverage summary

This M7-A3 audit is documentation-only, so Unit, SIT, UAT, and Unity execution are **Not Applicable** for this PR. The merged M7 increments were validated by the latest Unity EditMode suites listed below; this closeout records their coverage rather than rerunning Unity:

### M7-specific EditMode suites

- `OfflineSummaryResolverTests`
  - deterministic identical-input output;
  - clamped observed windows with no progression processing;
  - missing or invalid rules and timestamp windows;
  - legacy/default research state safety; and
  - single-slot pending observation without completion or mutation.
- `OfflineSummaryDiagnosticsTests`
  - localized Systems Diagnostics lines;
  - immediate set/clear rendering;
  - persisted boot/session snapshot selection;
  - legacy/default persisted-summary fallback;
  - save serialization round trip;
  - refresh/page-cycle non-mutation; and
  - stable localization-key fallback and visible-English guardrail coverage.
- `ResearchPendingResolverTests`
  - null/default/empty pending state;
  - configured single-slot resolution;
  - saved-slot normalization to the configured slot; and
  - deterministic disabled-safe invalid-config paths.
- `ResearchPendingDevControlsTests`
  - set writes only pending state and refreshes diagnostics;
  - clear clears only pending state and refreshes diagnostics;
  - invalid config does not mutate save and retains safe fallback; and
  - the saved model remains a two-field single-slot scaffold without progress or completion fields.

### Retained regression suites

The merged M7 evidence also calls out the retained Unity EditMode regression suites:

- `BootstrapOverlayPagingTests` for F1/F2/F3 developer-overlay paging behavior and non-mutation;
- `LootHeatCoolingResolverTests`, `RunHeatDeltaResolverTests`, and `RunHeatStateApplyResolverTests` for M6 heat behavior;
- `RunSimulationTests` for active-run behavior and outcomes;
- loot resolver suites for loot behavior; and
- `StructureSimulationTests` for structure simulation behavior.

## 17. Manual smoke coverage summary

The merged M7 evidence defines the following recommended Bootstrap smoke pass. A fresh Unity Play Mode smoke run was not performed as part of this documentation-only audit:

1. Open the Bootstrap scene and enter Play Mode.
2. Confirm the existing F1 developer panel, F2 focused run diagnostics, and F3 full-diagnostics paging still work.
3. Open Systems Diagnostics and confirm the offline-summary, research-pending, and research-pending-validation lines are readable and localized.
4. Confirm the offline-summary line reports `wouldProcess=False`.
5. Use **Set Research Pending** and confirm diagnostics immediately show `pending=True` with the configured single slot and project IDs.
6. Confirm set does not grant research progress, complete research, grant resources, change heat, tick structures, or append run history.
7. Use **Clear Research Pending** and confirm diagnostics immediately show `pending=False` with empty slot and project values.
8. Confirm clear does not grant research progress, complete research, grant resources, change heat, tick structures, or append run history.
9. Refresh diagnostics and cycle pages; confirm no mana, loot, offline rewards, offline heat changes, research progress, structure ticks, or run-history entries appear.
10. Confirm no M6 heat behavior changes and no unexpected `.meta` files are created.

## 18. Known risks or gaps

1. **Accepted existing-save timestamp behavior for developer controls.** Set and clear use `SaveService.Save(..., SaveReason.ManualDev)`, which updates `lastSavedUtcUnix`. This is expected for the existing save path and is not offline progression. If a future ticket adds active-session research timing, it must explicitly define whether developer-control saves should affect any new timing contract.
2. **Dormant earlier `allowOfflineProgression` field.** `content_bootstrap.json` still has `allowOfflineProgression: true`, but M7 does not use it to process rewards or simulation. Before any future offline-progression implementation, open a separately scoped design/implementation review to define the flag's actual contract, reward ownership, caps, verification requirements, and tests. This audit does not authorize activation.
3. **Intentional single-slot normalization.** `ResearchPendingResolver` normalizes a saved non-empty pending project to the configured one scaffold slot. This is intentional for the single-slot scaffold. Any future migration or multi-slot proposal requires a separately scoped design and save-compatibility review; M7-B0 must remain single-slot.
4. **Research rule-source metadata is present.** `research.pending.rule.m7_a2_scaffold` is content-owned and exposed through validation diagnostics. No gap was found.
5. **Manual Unity smoke remains recommended.** This documentation-only audit did not run Unity Play Mode manually or rerun EditMode suites.
6. **No production hardening is authorized.** The current scaffold is not a production research UI, offline economy, or verification system.

## 19. Recommendation

**GO WITH FOLLOW-UP** for moving to M7-B0 **after this M7-A3 audit is merged**, and only for a small, separately scoped **active-session** research-progress scaffold.

The merged M7 scaffold has the expected closeout shape: offline summary observation stays read-only with `WouldProcessOfflineProgress == false`; the persisted last-offline summary remains diagnostics-only; research pending remains deterministic, localized, content-owned, save-compatible, developer-controlled, and single-slot-only; and M6 heat behavior remains preserved.

The follow-up is to keep the next slice narrow and to resolve any new timing contract explicitly. M7-B0 must not add or authorize offline rewards, offline mana, offline heat, offline structure ticks, offline loot, run-history grants, production UI, multiple research slots, research completion, server verification, or any post-MVP system.

## 20. Closeout gate

M7-B0 has not started in this PR. Do not begin M7-B0 until this M7-A3 documentation-only audit is merged. After merge, M7-B0 may proceed only within the recommendation boundary above.
