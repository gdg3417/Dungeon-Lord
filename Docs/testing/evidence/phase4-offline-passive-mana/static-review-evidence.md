# Phase 4 canonical offline passive mana — qualification evidence

Status: automated qualification complete; external manual UAT remains outstanding.

Baseline: merged PR #205 at `f15504729716cc4ac22b0eb7070d91a92d3cd20d`.

## Contract

- Schema remains 9; no derived rate, efficiency, or evidence fields are persisted.
- `Assets/_Project/Resources/passive_online_mana.json` schema version 2 is the single validated base-efficiency authority and owns `BaseOfflineEfficiency = 0.15`.
- The offline resolver consumes `CanonicalPassiveManaService.ResolveRate`, exact elapsed seconds, and the structural-economy capacity authority. It does not consume `maxOfflineSeconds`.
- The complete-save writer atomically persists only the resulting wallet and consumed `lastSavedUtcUnix` before runtime publication.
- Structured result evidence is available for pre-MVP security monitoring but is not authoritative clock-cheat proof.

## Automated evidence

- Focused offline EditMode fixture: 26/26 passed, 0 failed, 0 skipped; wrapper exit 0; 1.1281893 seconds. Results: `%TEMP%/phase4_offline_passive_mana_focused.xml`.
- Affected passive-online/config fixture: 54/54 passed, 0 failed, 0 skipped; wrapper exit 0. Results: `%TEMP%/phase4_passive_online_affected.xml`.
- Full EditMode: 924/924 passed, 0 failed, 0 skipped, 0 inconclusive; wrapper exit 0; 120.4255693 seconds. Results: `%TEMP%/phase4_offline_passive_mana_editmode.xml`.
- Full PlayMode: 2,427 passed, 0 failed, 10 skipped, 0 inconclusive from 2,437 discovered tests; wrapper exit 0; 114.4969168 seconds. The skips are the established platform/mode exclusions (eight synchronous EditMode-only cases, one non-Windows inverse, and one Windows Player-only case). Results: `%TEMP%/phase4_offline_passive_mana_playmode.xml`.
- Windows Development Build: succeeded for `StandaloneWindows64`; wrapper exit 0; 0 errors, 1 warning; 170,452,610 bytes. Output: `Builds/Development/Windows/Dungeon Lord.exe`. Reports: `Builds/Development/Windows/build-report.json` and `Builds/Development/Windows/build-provenance.json`.
- The single build warning is the established missing Unity Cloud credentials warning: native symbols were not uploaded. Shutdown-only `abort_threads` and debugger-listener messages appeared after the successful build and return code 0; no new `ComputeBuffer`/MemoryLeaks regression was observed.

## Manual evidence

External manual UAT is not claimed. Use [the committed manual UAT plan](manual-uat.md).
