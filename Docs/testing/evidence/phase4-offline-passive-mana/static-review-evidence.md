# Phase 4 canonical offline passive mana — qualification evidence

Status: initial, external-review correction, PR #206 integration, false-capacity correction automated qualification, and the focused external manual UAT rerun are complete.

Latest repository-main baseline: merged PR #206 at `f32b3ee94cf72cade159c2b5bfeba0a7ed4ee3a6`.

Latest gameplay-capability baseline: merged PR #205 at `f15504729716cc4ac22b0eb7070d91a92d3cd20d`.

Reviewed implementation commit: `061b556b0c5f544000bc6bd46d4ee2da7e504971`.

External-review correction implementation commit: `69395bf8b8c318d3f5e2bdf03bf4e2bcd67785e1`.

PR #206 integration qualification commit: `e8f6dd3ade4f6cb1820706b5a87ae9e507492927`.

False-capacity correction implementation commit: `d8226e91b611e5e273c898fae2ae4b71da9366b7`.

## External UAT false-capacity correction

External Editor UAT observed a correct 63-second fractional award at an effective 18 mana/hour (`0.315` mana, moving the wallet from approximately `0.423` to `0.738` of `1000`) accompanied by the false localized capacity-limited message. The cause was `CapacityLimited` comparing the calculated award with an award reconstructed through floating-point subtraction (`WalletAfter - WalletBefore`). Representation differences could make the reconstructed value slightly smaller even when structural capacity did not clamp the wallet.

The correction computes the unclamped candidate wallet once, sets `CapacityLimited` only when that candidate is greater than the authoritative structural `ManaCapacity`, and then clamps the wallet with `Math.Min`. It introduces no epsilon or tuning value and does not change elapsed-time calculation, fractional precision, configuration-owned 15% efficiency, canonical online-rate reuse, Heat, duration eligibility, persistence, reason codes, localization, or schema 9.

- Focused offline-passive-mana EditMode fixture: 33/33 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 1.2406238 seconds. The added UAT-shaped regression proves zero active floors, 18 mana/hour effective offline rate, 63 elapsed seconds, `0.315` award, approximately `0.423 -> 0.738`, `CapacityLimited == false`, and no capacity-limited localized text. The existing fractional test now also asserts no clamp; near-capacity, exceeding-capacity and already-full cases remain true-clamp coverage. Results: `%TEMP%/phase4_offline_capacity_correction_focused.xml`.
- Affected player-facing Bootstrap composer fixture: 7/7 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 0.0875062 seconds. Results: `%TEMP%/phase4_offline_capacity_correction_presenter.xml`.
- Full EditMode: 946/946 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 118.0266686 seconds. Results: `%TEMP%/phase4_offline_capacity_correction_editmode.xml`.
- Full PlayMode: 2,436 passed, 0 failed, 10 expected platform/mode skips, 0 inconclusive from 2,446 discovered tests; wrapper exit 0; 111.7505353 seconds. Results: `%TEMP%/phase4_offline_capacity_correction_playmode.xml`.
- Fresh Windows Development Build: succeeded for `StandaloneWindows64`; wrapper exit 0; Unity 6000.3.2f1; Development Build true; Bootstrap-only scene; 0 build errors, 1 warning; 170,453,625 bytes. Output: `Builds/Development/Windows/Dungeon Lord.exe`; report: `Builds/Development/Windows/build-report.json`; provenance: `Builds/Development/Windows/build-provenance.json`; log: `%TEMP%/phase4_offline_capacity_correction_windows_development_build.log`.
- The counted warning remains the established unavailable Unity Cloud credentials/native-symbol upload warning. The external uploader also logs its credential failure at error level, but the authoritative build report records 0 errors and a succeeded build. Shutdown-only `abort_threads` messages followed exit 0; no new `ComputeBuffer` or MemoryLeaks regression was observed.
- Qualification ran against the exact runtime/test content committed as `d8226e9`. Because qualification preceded the commit, provenance records parent `c94c575` with `dirty: true`. The worktree also retained the owner's unstaged UnityConnect setting and an unrelated TextMesh Pro fallback-asset serialization change; neither is part of the correction commit.

Temporary Bootstrap remains usable for validating the offline result, but UAT found its offline-result discoverability and at-once readability weak. Production UI must eventually present return/offline rewards more clearly. This is accepted non-blocking Bootstrap paging/scrolling/layout debt outside this packet; the false capacity message itself was a blocker and is corrected, not accepted debt.

## Focused external manual UAT rerun

The owner completed the focused external rerun after the false-capacity correction:

- Editor below-capacity offline return showed no false capacity-limited message.
- Editor genuine full/capacity-limited return showed the appropriate capacity message.
- 1920×1080 and 1280×720 remained usable for validation, with the accepted temporary Bootstrap discoverability/readability debt.
- Fresh Windows Development Build below-capacity and genuine-capacity paths passed.
- Normal active mana resumed afterward.
- No new blocking Console or Player errors were reported.

Bootstrap offline-result discoverability/readability remains weak temporary validation-UI debt; production return/offline rewards still need clearer presentation in future work.

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

The focused external manual UAT rerun is complete and passed. The committed [manual UAT plan](manual-uat.md) remains the record of the exercised validation paths and accepted temporary Bootstrap UI debt.
