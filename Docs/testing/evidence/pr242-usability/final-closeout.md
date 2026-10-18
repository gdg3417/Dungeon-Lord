# PR #242 — final qualification closeout

Recorded 2026-10-18 after the owner's explicit Windows pass. **Both Editor and Windows owner UAT are complete. PR #242 is ready for independent final merge review and remains Draft.** No merge, Ready transition or independent review approval is claimed.

## Owner observations and scope

The owner confirms **all requested Windows standalone usability checks passed at 1280×720 with Large text**, including construction controls, room placement options, trap targeting, saving, reopening and persistence. These are owner-reported observations, not automated test assertions or Codex-performed gameplay. They do not establish physical-touch hardware, additional resolutions/devices, native mobile, performance or release-build qualification.

Editor UAT already passed and is recorded in commit `d33d0c04d85e8e008c307a4a7e349d31bbcc4146` and [the Editor record](owner-editor-uat-passed.md): landscape room construction is usable, right-side landscape controls approved, portrait bottom controls preferred, Spike Trap placement passed and save/restart-Play persistence passed. The Windows result completes the remaining requested owner usability check.

The original Phase 7A0 Decision 5 required a contextual bottom panel/sheet. The owner explicitly approved the narrow landscape construction/content refinement, retaining preferred portrait bottom controls. The [dated design-lock addendum](../../../../docs/planning/phase-7a0-graphical-dungeon-editor-design-lock.md#owner-approved-responsive-editing-clarification--pr-242-2026-10-18) and [UI-vision clarification](../../../../docs/planning/production-dungeon-mobile-ui-vision.md#owner-approved-responsive-editing-clarification--pr-242-2026-10-18) reconcile that decision with portrait-first, safe-area/text-size and single-information-architecture requirements. Earlier decisions, concept references and PR #231 exclusions remain historical; no broad side-inspector redesign is approved.

## Closed Windows attempt and preservation

Codex verified the Windows player and Unity Editor were absent before closeout; only the unrelated Unity CLI server remained. The retained log records canonical `AppQuit` save at **2026-10-18 20:16:34 UTC** followed by input-system shutdown. No process was force-terminated.

Attempt: `owner-usability-windows-01`. Executable: `Builds/Development/pr242-usability-20261018-usability-build-01/Dungeon Lord.exe`. Product: `Dungeon Lord PR242 Graphics usability-build-01`. Separate isolated save: `pr242-usability-20261018-usability-build-01.json`. Private launch/closeout folder: `TestResults/pr242-usability-owner-20261018/owner-usability-windows-01/`.

This player required no temporary source substitutions: the Editor inputs had already been restored. Closeout reverified the exact input inventory, the 1,340 restored source/local-override hashes, the closed Editor session, complete qualified build artifact index, original save/player baseline and historical evidence manifest. It preserved **14 Windows session files** (save, launch records/log and generated product data) in a new `preserved-session.zip`, then created immutable `closed.json` and a 16-file artifact index. No existing archive/log/report/save was overwritten by the closeout tool; UAT's intended game writes remain in the isolated save.

| Preservation check | Final result |
| --- | --- |
| Restored Unity inputs, including five pre-existing local asset modifications | 1,340 / 1,340 exact |
| Original save/player baseline | 6,839 / 6,839 unchanged |
| Owner baseline | 14,400 / 14,401 unchanged; the only change is the explicitly writable isolated Windows UAT save |
| Historical evidence original hashes | 1,986 / 1,991 match; exactly five owner-accepted lost originals |
| Existing qualified build and earlier Editor/session evidence | Hash verification passed |

The single writable-save exception is bound to the preflight's exact path and starter hash; no other baseline file was excluded. Its final SHA-256 is `ef4fb9e121867168cb2aaad2e89ede47e3358a1735d9044480327988848f498c`, retained separately from the unchanged authority-generated starter (`f9004a81971e5c7e60009048eb2dd1c868c1cb639d8bf4a0d505f312a992b743`). The new session archive SHA-256 is `6897da7e026938290c7dfcfe926ac634896b7482b8fa93cbaa7877cce5f48ac1`. Save contents are not published or imported into normal play. Original hashes/incident/replacement captures for the five lost screenshots are unchanged; those originals were **not recovered**. See [machine-readable closeout](final-closeout.json).

## Applicable automated evidence and source identity

Qualified Unity source remains `0997d553ada69d54c4f53891932edd8df9b4cee7`. Complete committed Unity-input diff (`Assets`, `Packages`, `ProjectSettings`, `ContentAuthoring`) to the closeout head is empty. The 1,340 working input hashes also match the restored manifest, including the owner's five preserved local asset overrides. B3A protected gameplay/service/core and B3A fixture diffs remain empty against `b5502291e9da3d229bbcc3d152f8bd56954d84eb`. Later commits contain documentation/evidence only.

| Existing qualified check | Result |
| --- | --- |
| Focused construction/content/input and matrix | 3 passed, 0 failed/skipped |
| Affected UI EditMode | 224 passed, 0 failed/skipped |
| UI/C2 PlayMode | 72 passed, 0 failed/skipped |
| Windows Development, Unity 6000.3.2f1 | Succeeded; CLI 0; 0 errors, 1 existing symbol-upload warning |

No automated Unity suite or build was rerun for this closeout. Exact earlier commands/results/failures remain in [qualification.json](qualification.json) and [reports.zip](reports.zip). Protected prior B3A 41/0 and Phase A/B1/B2/B3A/C2 PlayMode 7/0 evidence remains applicable. The historical full EditMode report remains **2,175 passed / 2 failed / 1 existing skip**; both old graphical failures and baseline-source reproduction remain archived. The corrected tests pass in the final affected checks, but this is **not a fresh full-suite pass**.

## Review state, known diagnostics and remaining limits

GitHub returned **zero review threads, zero review submissions and zero discussion comments** at closeout. No unresolved blocking thread or changes-requested review was identified. Commit status/workflow queries returned no entries; this is not a claim of GitHub CI success. Independent final review has not occurred and remains the owner's next step.

The player log contains the shutdown diagnostic `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.` The same diagnostic exists in the preserved earlier C2 player logs (`TestResults/combat-c2-uat-save-preparation/startup-first.log`, `startup-reopen.log`, `before-Player.log`, `before-Player-prev.log`). It is retained as an existing cleanup diagnostic, not represented as fixed or as a new UI qualification failure. No fatal exception/error was found in this UAT log. The existing build symbol-upload warning also remains documented.

Long localized details still require scrolling; the floor overlay may be collapsed to uncover geometry; Fit Floor intentionally retains the legal Edit overview; graphical starter-room onboarding remains deferred and uses existing Bootstrap authority. Physical touch, native mobile/performance, further owner hardware/resolution checks and release-identity packaging are outside the reported result. The five original screenshot losses remain an accepted exception. B3B still requires production provenance/restart and time mapping, admission/occupancy/waiting and causal ordering, production durable C0/C2 execution, eligible economics/offline attribution and bounded catch-up/device qualification. B3A does not activate automatic production scheduling or settlement.

No unresolved **qualification** blocker or current design/status contradiction was identified after this reconciliation. Historical pending-UAT fields in older reports/procedures describe earlier stages and are explicitly superseded by this closeout, not silently edited. Accepted limitations and independent final review remain visible; no owner merge decision is inferred.
