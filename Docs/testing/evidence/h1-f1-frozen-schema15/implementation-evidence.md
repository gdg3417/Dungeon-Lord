# H1-F1 qualification evidence

Date: 2026-10-25 (America/Chicago). Model recommendation: GPT-6.1 Sol High, Complex; locked source-contract compatibility work. No fallback or unresolved contract contradiction.

## Source and isolation

GitHub/main was fetched and verified as `09a46810aeab7441a2ac09ac6220b0f4cc13d6dd` (PR #254), both before implementation and during qualification. Branch: `codex/h1-f1-frozen-schema15`. Isolated worktree: `C:/Dev/Dungeon-Lord/Temp/h1-f1-frozen-schema15`.

The owner checkout remains on `codex/r0-research-durable-acknowledgement`; its existing `Docs/planning/living-dungeon-hybrid-persistence-proposal.md` modification was not edited, staged or included. Its observed SHA-256 during final verification is `b04136e1b8375bef6dc2105bacd3e19062b4365be10e043b91c2d87670bd267d`. The initial app-managed worktree was inaccessible to sandboxed Git; the approved fallback created the isolated worktree under the writable repository Temp directory. No owner changes were copied or discarded.

Unity Editor: 6000.3.2f1, matching ProjectVersion.txt. Unity CLI: 1.0.0-beta.14, confirmed current by `unity self-update --check --format json`. Initial sandbox DNS/licensing failures required an approved retry outside the sandbox. No alternative test runner or dependency installation was used.

## Historical evidence

The capture ran against unchanged baseline runtime before implementation. `baseline-runtime-hashes.json` records pre-edit runtime hashes; `baseline-04.xml` records the successful baseline capture. The exact capture harness is retained as text beside the fixtures, outside Unity inputs. Regression tests read committed bytes and verdicts and never regenerate them.

- 46 original JSON files verified by `Tools/Presentation/Fixtures/schema15-09a46810/SHA256SUMS.tsv`.
- 35 independently captured structural/contextual verdict pairs in `verdicts.tsv`.
- Empty dormant candidate SHA-256: `a40e619ff2491c0380734acedaef5594ab8fcab81f59efa233a717f7a349e661`.
- Populated dormant candidate SHA-256: `9293dfe42d20d6eff08b1c8128290b7c651e1768fcb99a7ea116450ae97d19b5`.
- Frozen schema-15 limits SHA-256: `aa162762f7c437ca58f8a4f8f4cccb22e6868801269dc6f04dd3d241482a9782`.

Both dormant 14→15 outputs match the original candidate bytes exactly, and removing only the new monster owner/version change reconstructs the exact source bytes. Active schema-14 v1/v2 sources remain rejected because the baseline transformation's structural output validation receives no profiles. Historical schema-15 active v1/v2 saves themselves remain valid with registered profiles. Preserving this distinction is intentional; no older transformation was changed.

## Executed commands and outcomes

All project test commands ran from the isolated worktree. Full executable paths, arguments, exit codes and totals are preserved in the command JSON records. Reports and logs retain initial failures as well as successful retries.

Baseline command form (attempt 04 is the successful capture):

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test C:/Dev/Dungeon-Lord/Temp/h1-f1-frozen-schema15 --mode EditMode --filter H1F1BaselineCapture --output C:/Dev/Dungeon-Lord/Temp/h1-f1-frozen-schema15/TestResults/h1-f1/baseline-04.xml --timeout 1200 --no-color -- -logFile C:/Dev/Dungeon-Lord/Temp/h1-f1-frozen-schema15/TestResults/h1-f1/baseline-04.log
```

| Run | Actual outcome |
| --- | --- |
| Baseline initial sandbox run | Licensing system-information access denied; interrupted, no verdict |
| Baseline 02, before Editor discovery wrapper | Exit 0, **zero tests executed**; not counted as a pass |
| Baseline 03 | 0/1; invalid synthetic JSON in capture harness (extra comma), corrected before runtime edits |
| Baseline 04 | 1/1 passed; original bytes, hashes and verdicts captured |
| Focused 01 | 24/27; harness expected ordinal 1 instead of captured 0, and inherited NUnit case source was private |
| Focused 02 | **63/63 passed**, including the full 35-case verdict corpus |
| Broader regression 01 | **1220 passed, 3 failed**, 0 skipped; the three scene-profile cases rejected the ordinary product identity with `b3b2.profile.isolation_required` / `two_floor.smoke.isolation_required` |
| Migration 01 | **117 passed, 0 failed, 1 skipped**; includes frozen-15 tests, existing sealed migration failure matrix, limit-loader assertions and native Windows migration fixtures |
| Scene-profile 01 | **CLI timeout, incomplete**: 1800-second limit, CLI exit 14, no NUnit XML. Single-floor capture passed 68 runs; two-floor profile incomplete and layout case unverified. Not restarted. |

The single migration skip is `PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`, with reason `gd66.test.windows_only_inverse`, expected on this Windows host.

```powershell
& Tools/Presentation/run_h1_f1_qualification.ps1 -Stage focused -Attempt 01
& Tools/Presentation/run_h1_f1_qualification.ps1 -Stage focused -Attempt 02
& Tools/Presentation/run_h1_f1_qualification.ps1 -Stage regression -Attempt 01
& Tools/Presentation/run_h1_f1_qualification.ps1 -Stage migration -Attempt 01
& Tools/Presentation/run_h1_f1_qualification.ps1 -Stage scene-profile -Attempt 01
```

The broad filter also selected existing scene-profile methods. Their guards require the repository's disposable B3B2 Smoke Two Floor product treatment (`qualify_b3b1.py` / `qualify_two_floor.py`). For their targeted rerun only, the isolated `ProjectSettings.asset` was backed up and its product name changed from `Dungeon Lord` to `Dungeon Lord B3B2 Smoke Two Floor H1F1`. The existing fixtures generate unique save names/manifests and assert the isolated paths. The temporary product/settings changes are excluded from the PR and restored after qualification. No production save namespace is used by the rerun. The three initial failures are retained; they are not reported as a passing broad run.

Static verification:

```powershell
& 'C:/Users/gdg34/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B Docs/testing/evidence/h1-f1-frozen-schema15/static-checks.py
git diff --check
```

Static checks verify every fixture hash, exact frozen limit bytes, the unchanged earlier transformations, writable target 15, and whitespace. See `static-checks.json`.

## Coverage and limits

The focused suite covers dormant and active v1/v2 owners; A>C; exact bytes/identities/allocator/ordinals/parties; configuration-dependent monster validity; missing configuration/occupancy/compatibility/profile; missing/unsupported executable headers with recomputed hashes; malformed/reordered owners; supported unknown extensions; case aliases; frozen limits with both larger and smaller target selection; exact 14→15 candidates and rejection parity; unsupported future refusal; contextual recovery; predecessor/candidate proof, rollback, readback rejection, idempotence and uncertain authority. The supplemental Editor adapter executes the existing sealed failure-matrix assertions without rewriting their scenarios.

No runtime changes followed successful focused qualification. Later changes only added the established-fixture Editor adapter and qualification/evidence tooling. No Windows filesystem implementation or persistence protocol changed. Additional isolated Windows standalone durability qualification is not required solely for this packet; native Windows EditMode coverage is included. No manual gameplay UAT is required solely for this historical validation change.

This is not a complete unfiltered EditMode/PlayMode/player-build qualification. A later schema still needs a reviewed migration, retained executable/content/config compatibility, and separate end-to-end H1 qualification. No later writable schema, time provenance, ordinary-save clock/passive-mana change, H2 retention/batching, O1 simulation or UI change is implemented.


## Continuation closeout

The original execution session (8899) returned `Unity process timed out after 1800 seconds. Terminating.` The saved command records CLI exit **14**, start `2026-10-25T19:40:41.7292426Z` and finish `2026-10-25T20:10:47.1202528Z` (1805.3910102 seconds including shutdown). There is no `scene-profile-01.xml`. This is a CLI execution-limit outcome, not a Unity compilation failure or a completed failing NUnit run. The recurring licensing warning also appears in successful runs and is not the timeout verdict. See `scene-profile-01-cli-output.txt`, `scene-profile-01-command.json` and the preserved log archive.

The completed single-floor capture reports `Passed=true`, `CompletedRuns=68`, empty failure, and the disposable product identity. Its original report and SHA-256 are preserved in `scene-profile-captures.zip` / `scene-profile-captures.json`. No completed two-floor capture was produced. The interrupted campaign does not establish a pass for the two-floor profile or final layout case. No restart or unrelated performance investigation was performed. Process inspection found no remaining qualification Editor, only an unrelated Unity Hub `serve` process, which was left untouched.

These profiling/layout cases are supplementary to H1-F1's historical validation scope: no gameplay, UI, clock accrual or filesystem primitive changed. The mandatory frozen-source, historical-limit, 14-to-15 byte-equivalence, recovery/failure-matrix and Windows migration checks have completed passing results. The broader run remains accurately recorded as **1220 passed / 3 isolation-guard failures**; its failed suite status has not been relabeled as green. Incomplete supplementary profiling remains a non-blocking limitation for independent review, subject to the reviewer's judgment. This is not merge approval or full gameplay/performance qualification.

Before cleanup, all qualified source/configuration inputs matched `d42be511fc17b3ac41c81ed9e2ba4f8a22d6fe7b`; no later implementation commits existed. The only differences were the disposable product name in `ProjectSettings/ProjectSettings.asset` and line-ending-only Unity rewrites in `EditorBuildSettings.asset` / `ShaderGraphSettings.asset`. All three were individually compared and restored from the isolated branch's committed versions, using normal Git checkout line endings. No source fixture, approved resource or owner file was removed. `qualified-input-hashes.json` pins the unchanged qualified Assets inputs, and `closeout-verification.json` records the report verification and restored settings. Static validation was rerun; no Unity suite was rerun during closeout.

The original owner checkout is still `codex/r0-research-durable-acknowledgement` at `4c13b9dbfe3e1ee167cedb9c3328a69c21c08a0a`, with only its pre-existing planning-document modification; its SHA-256 still matches the value above. The separately attached app-managed worktree is a clean detached baseline checkout, not the implementation worktree. All implementation and qualification cleanup remains in the original isolated Temp worktree.

**READY FOR INDEPENDENT PR REVIEW.** Runtime implementation remains at the qualified source commit; this continuation adds only evidence/documentation. PR #255 remains a draft, is not merged, and no next H1 component is started. No manual gameplay UAT or additional Windows standalone durability qualification is required solely for H1-F1; optional performance/layout completion remains unverified as stated above. The existing active-schema-14 upgrade rejection and future H1 migration/provenance dependencies remain unchanged.

The log archive also preserves the original NUnit XML, command records and recovered timeout output as raw bytes; their hashes remain verifiable across Git line-ending settings. Archive integrity and XML per-case counts were checked during closeout.

The original command records retain sourceCommit 09a46810 because qualification ran with the implementation uncommitted on that parent. The same implementation was subsequently committed as d42be511; the closeout input hashes verify the current files against that commit, rather than claiming a new hash snapshot was captured at test launch. Original records and expected fixtures were not rewritten.
