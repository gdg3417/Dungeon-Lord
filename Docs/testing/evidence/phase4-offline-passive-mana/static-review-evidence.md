# Phase 4 canonical offline passive mana — qualification evidence

Status: initial, external-review correction, and PR #206 integration automated qualification are complete; external manual UAT remains outstanding.

Latest repository-main baseline: merged PR #206 at `f32b3ee94cf72cade159c2b5bfeba0a7ed4ee3a6`.

Latest gameplay-capability baseline: merged PR #205 at `f15504729716cc4ac22b0eb7070d91a92d3cd20d`.

Reviewed implementation commit: `061b556b0c5f544000bc6bd46d4ee2da7e504971`.

External-review correction implementation commit: `69395bf8b8c318d3f5e2bdf03bf4e2bcd67785e1`.

PR #206 integration qualification commit: `e8f6dd3ade4f6cb1820706b5a87ae9e507492927`.

## PR #206 integration qualification

The current main branch was integrated with a no-fast-forward merge. The merge preserves the complete reviewed offline implementation and incorporates PR #206's line-ending-independent passive-mana fixture mutation. `PassiveOnlineManaTests` retains schema-version-2 and `BaseOfflineEfficiency` validation, normalizes CRLF/CR input to LF, uses guarded fixture-fragment replacement for offline and Heat mutations, and fails explicitly when an expected mutation source is absent.

- Focused passive-online/config EditMode fixture: 54/54 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 1.1436466 seconds. Results: `%TEMP%/phase4_offline_pr206_passive_online.xml`.
- Focused offline-passive-mana EditMode fixture: 32/32 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 1.2482175 seconds. Results: `%TEMP%/phase4_offline_pr206_focused.xml`.
- Full EditMode: 945/945 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 120.2920423 seconds. Results: `%TEMP%/phase4_offline_pr206_editmode.xml`.
- Full PlayMode: 2,435 passed, 0 failed, 10 expected platform/mode skips, 0 inconclusive from 2,445 discovered tests; wrapper exit 0; 111.5849675 seconds. Results: `%TEMP%/phase4_offline_pr206_playmode.xml`.
- The first sandboxed focused invocation could not access the user-profile Unity Licensing configuration and produced no test verdict or result file. It was stopped and rerun through the approved wrapper with normal user permissions; all reported qualification results above are from completed runs.
- A new Windows Development Build was not run. Relative to the already build-qualified external-review correction, this integration changes only an EditMode test and active Markdown planning/evidence; no runtime source, player asset, content, schema, localization data, or build configuration changed.
- Unity rewrote only the ordering of two `applicationIdentifier` entries in `ProjectSettings.asset` during qualification; that generated no-content change was discarded. The owner's local `UnityConnectSettings.asset` `m_Enabled: 1` change remained unstaged and untouched.

## External-review correction

The focused follow-up addresses five review findings without changing schema or gameplay tuning:

- rejected backward/invalid wall-clock observations cannot move the durable `lastSavedUtcUnix` boundary backward during later Boot, periodic, manual, pause or quit saves;
- the localized offline result is composed into the normal player-facing Bootstrap smoke surface, while remaining available in diagnostics;
- the long-pause clock-anomaly signal is localization-backed and no longer claims that a valid forward offline award was duration-limited;
- cold-start coverage now discards the first runtime/service, reopens durable bytes through a new canonical session/service and proves the interval cannot replay;
- active Phase 4 planning text is reconciled to merged PR #205 and the current unmerged offline packet.

The original results below remain the evidence for reviewed commit `061b556`; correction results are recorded separately rather than replacing them.

## Correction automated evidence for `69395bf`

- Focused offline EditMode fixture: 32/32 passed, 0 failed, 0 skipped; wrapper exit 0; 1.2585096 seconds. This includes durable backward-clock rejection, all five canonical save reasons, a fresh durable reopen, fractional persistence and long-forward resume behavior. Results: `%TEMP%/phase4_offline_external_review_focused.xml`.
- Affected passive-online and canonical-session fixtures: 62/62 passed, 0 failed, 0 skipped; wrapper exit 0; 1.3164736 seconds (54 passive-online/config and 8 save-session cases). Results: `%TEMP%/phase4_offline_external_review_affected.xml`.
- Affected clock, player-facing smoke composer and save-lifecycle fixtures: 15/15 passed, 0 failed, 0 skipped; wrapper exit 0; 0.2859369 seconds (4 clock, 7 smoke-composer and 4 lifecycle cases). Results: `%TEMP%/phase4_offline_external_review_ui_clock_lifecycle.xml`.
- Full EditMode: 945/945 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 118.3657275 seconds. Results: `%TEMP%/phase4_offline_external_review_editmode.xml`.
- Full PlayMode: 2,435 passed, 0 failed, 10 expected platform/mode skips, 0 inconclusive from 2,445 discovered tests; wrapper exit 0; 111.9588627 seconds. Results: `%TEMP%/phase4_offline_external_review_playmode.xml`.
- Windows Development Build: succeeded for `StandaloneWindows64`; wrapper exit 0; 0 errors, 1 warning; 170,453,594 bytes. Output: `Builds/Development/Windows/Dungeon Lord.exe`. Reports: `Builds/Development/Windows/build-report.json` and `Builds/Development/Windows/build-provenance.json`.
- The single counted build warning remains the established missing Unity Cloud credentials/native-symbol upload warning. Shutdown-only `abort_threads` messages followed the successful build and return code 0; no new `ComputeBuffer`/MemoryLeaks regression was observed.
- Qualification ran against the exact correction content subsequently committed as `69395bf`. Because the commit was created after qualification, build provenance records parent revision `061b556` with `dirty: true`; the staged source diff used by the run is the content of `69395bf`.

## Contract

- Schema remains 9; no derived rate, efficiency, or evidence fields are persisted.
- `Assets/_Project/Resources/passive_online_mana.json` schema version 2 is the single validated base-efficiency authority and owns `BaseOfflineEfficiency = 0.15`.
- The offline resolver consumes `CanonicalPassiveManaService.ResolveRate`, exact elapsed seconds, and the structural-economy capacity authority. It does not consume `maxOfflineSeconds`.
- The complete-save writer atomically persists only the resulting wallet and consumed `lastSavedUtcUnix` before runtime publication.
- Structured result evidence is available for pre-MVP security monitoring but is not authoritative clock-cheat proof.

## Initial automated evidence for `061b556`

- Focused offline EditMode fixture: 26/26 passed, 0 failed, 0 skipped; wrapper exit 0; 1.1281893 seconds. Results: `%TEMP%/phase4_offline_passive_mana_focused.xml`.
- Affected passive-online/config fixture: 54/54 passed, 0 failed, 0 skipped; wrapper exit 0. Results: `%TEMP%/phase4_passive_online_affected.xml`.
- Full EditMode: 924/924 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 120.4255693 seconds. Results: `%TEMP%/phase4_offline_passive_mana_editmode.xml`.
- Full PlayMode: 2,427 passed, 0 failed, 10 skipped, 0 inconclusive from 2,437 discovered tests; wrapper exit 0; 114.4969168 seconds. The skips are the established platform/mode exclusions (eight synchronous EditMode-only cases, one non-Windows inverse, and one Windows Player-only case). Results: `%TEMP%/phase4_offline_passive_mana_playmode.xml`.
- Windows Development Build: succeeded for `StandaloneWindows64`; wrapper exit 0; 0 errors, 1 warning; 170,452,610 bytes. Output: `Builds/Development/Windows/Dungeon Lord.exe`. Reports: `Builds/Development/Windows/build-report.json` and `Builds/Development/Windows/build-provenance.json`.
- The single build warning is the established missing Unity Cloud credentials warning: native symbols were not uploaded. Shutdown-only `abort_threads` and debugger-listener messages appeared after the successful build and return code 0; no new `ComputeBuffer`/MemoryLeaks regression was observed.

## Manual evidence

External manual UAT is not claimed. Automated integration qualification is complete and the branch is ready for external manual UAT using [the committed manual UAT plan](manual-uat.md).
