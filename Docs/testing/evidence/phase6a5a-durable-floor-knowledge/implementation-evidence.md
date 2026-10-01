# Phase 6A5A durable floor knowledge: implementation evidence

Baseline: merged PR #218, `2505c679afd5172dc82dd08b44aedf3add96001c` on `main`. Branch: `codex/phase-6a5a-floor-knowledge`.

The writable save target is schema 12. The frozen schema 11 reader remains available. The single explicit 11-to-12 step accepts valid frozen 11 bytes, preserves non-colliding existing root and primary members in canonical order, changes the version token, and appends one empty `sharedFloorKnowledge` member after `sharedBranchKnowledge`. A pre-existing primary extension named `sharedFloorKnowledge` collides with the newly reserved owner and is rejected fail-closed rather than overwritten. It does not infer historical knowledge. Earlier schemas retain their sequential path through 11. New native saves initialize the same empty owner. The current complete-save reader requires the seven-member canonical tail and rejects missing, malformed, duplicate, or out-of-order floor records.

Each floor record owns stable `FloorInstanceId`, canonical applicability SHA-256, explicit reward and danger known flags and perceived scores, explicit shared confidence state/value, and last-confirmed RunId evidence. Records sort by ordinal floor ID. Validation rejects malformed IDs and fingerprints, duplicates, unknown/nonzero score mismatches, nonfinite or negative scores, confidence outside (0,1] when known, and incoherent last-run state. Record reading and writing are bounded by the approved five Active floors. Branch knowledge remains a separate owner.

Applicability hashes the canonical floor identity, geometry, fixed structures, room content, and that floor's optional corridor content in stable order. Activation state and current balance coefficients are excluded. A material edit makes old knowledge inapplicable without erasing it. Pre-run snapshots defensively capture knowledge and each Active floor's fingerprint alongside the existing floor-local plans, before party formation. Later live mutations cannot change that run.

Only a completed floor with final survivors contributes a report. The report sums reached floor-local `LootBonus` and `Danger` effects from existing room resolution and returned optional-branch evidence. No new weighting formula is introduced. A final wipe makes no precise update; an unseen Floor 2 has no completion evidence and cannot be learned from its hidden snapshot. Compatible evidence reconfirms by the configured `0.125` up to configured `1.0`; first, changed, or newly applicable evidence starts at configured `0.75`. A4 unknown transition perception remains unchanged. Knowledge learned in one run is available to later runs only.

Run settlement proposes knowledge on the detached candidate and writes it with branch knowledge, Heat, history, run sequence, extraction, and other settlement in the existing exact complete-save atomic transaction. Live state publishes only after exact durable readback and reopen validation. Stale sessions, concurrent-byte mismatches, malformed candidates, write/readback/reopen failures, and workload rejection retain the old durable and live state.

## Automated qualification

| Gate | Command or filter | Total | Passed | Failed | Skipped |
|---|---|---:|---:|---:|---:|
| Focused A5A review correction | `unity.exe test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseSixA5A --output TestResults/phase6a5a-review-focused.xml --timeout 600 --no-color` | 22 | 22 | 0 | 0 |
| Prior focused Phase 5B (production unchanged in review correction) | `unity.exe test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseFiveB --output TestResults/phase6a5a-phase5b-editmode.xml --timeout 600 --no-color` | 107 | 107 | 0 | 0 |
| Focused Phase 6 review correction | `unity.exe test C:/Dev/Dungeon-Lord --mode EditMode --filter PhaseSixA --output TestResults/phase6a5a-review-phasesix.xml --timeout 600 --no-color` | 214 | 214 | 0 | 0 |
| Complete EditMode review correction | `unity.exe test C:/Dev/Dungeon-Lord --mode EditMode --output TestResults/phase6a5a-review-full-editmode.xml --timeout 1200 --no-color` | 1293 | 1293 | 0 | 0 |
| Complete PlayMode review correction | `unity.exe test C:/Dev/Dungeon-Lord --mode PlayMode --output TestResults/phase6a5a-review-full-playmode.xml --timeout 1200 --no-color` | 2792 | 2782 | 0 | 10 |

The complete suites include production Phase 6 configuration, English localization, save and sequential migration, spatial and production content, and the existing atomic save fault matrix. The ten PlayMode skips are the same platform or mode-specific fixtures recorded in the A4 qualification evidence. The focused A5A suite proves schema source nonmutation, deterministic 11-to-12 output, non-colliding unknown root/primary preservation and reserved-owner collision rejection, exact tail ordering, record canonicalization/rejection, applicability, positive descended two-floor learning, survivor reconfirmation and clamp, no precise update to existing knowledge on a wipe, unseen-floor privacy, snapshot isolation, durable reopen, stale and concurrent rejection, and failed persistence/readback atomicity.

Windows Development Build command:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' build 'C:/Dev/Dungeon-Lord' --target StandaloneWindows64 --execute-method DungeonBuilder.M0.EditorTools.DevelopmentBuildUtility.BuildWindowsDevelopment --log-file 'C:/Dev/Dungeon-Lord/TestResults/phase6a5a-windows-development-build.log' --provenance-path 'C:/Dev/Dungeon-Lord/Builds/Development/Windows/build-provenance.json' --allow-dirty-build --no-tail
```

The build succeeded under Unity 6000.3.2f1 with only `Assets/_Project/Scenes/Bootstrap.unity`, Development Build enabled, 170,690,100 total reported bytes, zero errors, and one unavailable Unity Cloud native-symbol credential warning. The production spatial pre-build gate passed. Automated build success does not substitute for standalone owner UAT.

The external-review correction changes only EditMode tests and this evidence text. No runtime, save, migration, configuration, content, localization, or build code changed, so the Windows Development Build was not rerun for the correction.

## Final review and owner qualification

External review passed. Owner Editor/manual UAT passed at the exact qualified implementation HEAD `7d5655c7c7411295b68e30d5b4340dce0f77ba0f`. Normal and 1280x720 resolution smoke checks passed, as did save-close-reopen and Windows standalone UAT with close/reopen persistence. No corruption, recovery warning, or player-facing regression was reported.

Owner UAT covered schema-11 to schema-12 migration and existing-state preservation; one-floor survivor learning, compatible reconfirmation, material-change invalidation, and wipe privacy; Floor 2 EXIT privacy and true DESCEND; party identity and exact HP continuity with one final settlement; lifecycle applicability; and a later material Floor 2 change followed by a survivor run. Observed durable knowledge had two distinct floor records: Floor 1 reconfirmed confidence `0.875` and Floor 2 changed/applicable knowledge at initial confidence `0.75`, both associated with the qualifying run. The final floor completed successfully.

This qualification-status update changes documentation only. No new runtime test or Windows build was required. Phase 6 remains open. A5B retains knowledge-backed transition perception/explanation, richer aggregate multi-floor reporting, and Phase 6 closeout.

`ProjectSettings/UnityConnectSettings.asset` is intentionally excluded from all staged and committed changes. Unity build execution may leave a local-only settings diff; this packet does not restore or stage that file.
