# H1-F3 completion correction and approved recovery

Continuation dated 2026-10-26. This report supplements the [original stopped-attempt record](h1-f3-stable-recovery-qualification.md); it does not amend its verdict or rewrite historical evidence. The [H1-F3 contract](../planning/h1-f3-stable-recovery-precision.md) remains the implementation scope authority.

Base: `443319983a746b4a1670bc8a87d7434a14f7a492`. Branch: `codex/h1-f3-stable-recovery-precision`. Correction and replacement-campaign source: `6a2ac27e9daf7a943be2c7ea03643c539b9ce82f`. Actual model: GPT-6.1 Sol High; Complex, bounded process completion/evidence-integrity investigation. Availability fallback: GPT-6 Sol High. No substitution, automatic escalation or broader native forensic investigation occurred.

## Cause and historical evidence

Original campaign `b343351f516b4a19b8992ad00910ae40`, source `a68451bcebaab04a25dafeac223a075e4c190081`, stopped at `focused`. Its complete original plan remains in `TestResults/h1-f3-preparation/plan.json` and its immutable campaign JSON. The selected command was:

```text
C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe test <managed-worktree> --mode EditMode --filter H1F3;H1F2;FrozenSchemaFifteen --output <original-campaign>/focused/results.xml --timeout 900 --no-color -- -logFile <original-campaign>/focused/unity.log
```

Supervisor host PID 7992 has recorded creation FILETIME `134375128247144883`; the CLI child PID 45544 has `134375128248283334`. The supervisor's launch receipt identifies PID 47804 with creation FILETIME `134375128144893408`. These records include executable paths and commands; ownership is not inferred from PID alone.

The CLI's OS-wait receipt records **exit 0 at 18:27:34.783646 UTC**; the host also exited zero. Complete NUnit XML independently validates **188 passed / 0 failed / 0 skipped**, including 34 new H1-F3 cases. The supervisor nevertheless recorded `INCOMPLETE / UNCERTAIN` at **18:27:39.794464 UTC**, after its five-second Job Object completion drain. That refusal was correct and remains unchanged.

The previously retained OS inventory observed CLI descendant PID **3568**, parent **45544**, executable `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`, creation **18:27:05.261223 UTC**, and its console host PID **44988**, parent **3568**, executable `C:/Windows/system32/conhost.exe`, creation **18:27:05.266351 UTC**. Their exact command lines and OS exit receipts were not retained and are not reconstructed. Editor PID **49796**, separately identified in the Unity log, is not the remaining CLI descendant.

Read-only inspection of the local CLI log established the missing role/timing evidence, correlated with the recorded creation time and parent chain:

| UTC | PID | Recorded event |
| --- | ---: | --- |
| 18:27:05.287 | 3568 | CLI beta.14 startup |
| 18:27:05.418 | 3568 | AuthBroker listening, `idleMs=120000` |
| 18:27:34.778 | 45544 | CLI invocation finished |
| 18:29:05.381 | 3568 | AuthBroker idle; exiting |
| 18:29:05.382 | 3568 | AuthBroker stopped |

The broker stop log is approximately **90.6 seconds after CLI exit** and **85.6 seconds after the failed completion drain**. These are log timestamps, not retroactive OS exit certificates. Current absence likewise does not establish successful historical descendant termination.

Best-supported classification: **A — invocation/helper lifetime mismatch**. `unity test` waits for Editor testing, but its authentication access can create a resident CLI helper inside the inherited job. The installed CLI's own embedded `references/auth-license-cloud.md`, retrieved with `unity skill show --path ...`, documents the broker's two-minute idle lifetime and supported `UNITY_NO_AUTH_BROKER=1` direct OS-keyring mode. This is not evidence for extending the drain as normal Editor shutdown (B), a persistent orphan (C), or faulty completion accounting (D).

Before correction, all eight pinned tool entries and all 1,462 inputs matched the original attempt; archived campaign files matched the publication archive; owner/save preservation hashes matched. Current exact-identity checks found neither recorded supervisor/host/CLI alive, and a read-only OS query found neither recorded descendant nor an Editor/player. No original process was terminated. The original campaign remains historical **INCOMPLETE**.

## Smallest correction and authority

`qualify_h1_f3.py` uses a synchronous campaign-specific runner and sets `UNITY_NO_AUTH_BROKER=1` only in its CLI child's copied environment. It preserves the existing filters, output reports, 900-second CLI limit and 960-second stage limit, records the command/override/return code, and propagates failures. It does not alter global authentication settings, detach work or manage/terminate processes.

The supervisor's reservation was stored under Unity-owned `Temp`; after the original attempt, its `active.json` was absent. The exact removal event was not retained. A nine-line scoped supervisor correction moves reservation/lock storage to `TestResults/qualification-supervisor-reservation` and refuses existing unresolved legacy markers without automatic migration. The Job Object primitives, handle/creation identity checks, five-second drain, cancellation rules, source/tool/input validation, and fail-closed completion are unchanged. The new reservation has been observed surviving real Editor execution. This bounded, owner-authorized ancillary correction does not require a separate prerequisite tooling PR.

The owner explicitly approved **“Approve scoped recovery”** after the runbook requirement was explained. Approval covered retaining the original unchanged, restoring only three recorded temporary settings after exact verification, the persistent-reservation correction, and one fresh supervised campaign with child-local direct-keyring mode. The private approval/identity/hash record is `TestResults/h1-f3-completion-investigation/approved-recovery.json`.

Ordinary resume was unavailable because the original campaign was terminal INCOMPLETE and lacked a verified passing focused stage. The approved recovery did not use resume, force-unlock, alter the original JSON, delete evidence, or treat a missing reservation marker as automatic permission. Exact old settings bytes were recovered only when they matched the original input hashes; originals and all 1,462 inputs then matched preparation. The subsequent campaign uses unused outputs and a fresh product/save identity.

## Correction validation

| Gate | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Existing supervisor regressions plus reservation survival/legacy refusal | 30 | 0 | 0 |
| Child-local runner and exact restoration regressions, including measured font rewrite | 10 | 0 | 0 |

Tests use disposable fake repositories/processes and do not launch Unity or access owner saves. Existing cancellation/crash tests retain exact fixture-process ownership. The initial runner attempt was prevented by sandbox temporary-directory access (four methods, eight setup/cleanup errors); its elevated rerun passed. The initial 30-case supervisor run had 29 passes and one fixture-start race error. That test previously injected a crash after Popen identity publication, before the fixture necessarily wrote its startup marker. It now uses the existing bounded startup-marker wait before injecting the crash; the final 30 cases pass. No production wait or timeout was extended.

## Replacement qualification

Replacement campaign: `b227e498d4ef4f2a91cd7801fb915881`, under this managed worktree's `TestResults/qualification-supervisor`. Product: `Dungeon Lord B3B2 Smoke H1F3 Recovery 20261026`; save namespace: `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F3 Recovery 20261026`. Preparation and original settings archive: `TestResults/h1-f3-recovery-20261026`.

The plan pins exact source/tools and all 1,462 inputs. Qualified input identity: `0a3554245f80cb59ff4eeff60d0e8cdef52a5e999d882c45a8b313efe9f0f84a`. Predeclared restored identity: `b8070243f931cf7b860cb846d4c705843df64a989850e6524b78cd67ed884b7a`. It selects focused, integration, migration, a direct restoration preflight, exact settings restoration, and a final preservation gate dependent on every earlier selected stage. Original campaign/preparation hashes are protected throughout. The old 188-pass XML is not a verdict for this campaign.

| Stage | Actual status |
| --- | --- |
| Focused Unit/SIT | Supervisor PASS, 188 passed / 0 failed / 0 skipped |
| Integration/SIT | XML 436 passed / 0 failed / 0 skipped; supervisor INCOMPLETE on changed input |
| Migration/regression | NOT RUN after stop |
| Exact original settings restoration | NOT RUN after stop |
| Final preservation | NOT RUN after stop |

The first replacement proved CLI/job completion, then stopped on a distinct input-integrity condition: Unity's TextMeshPro use rewrote `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` from SHA-256 `6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d` to `f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52`. It was the only changed input. The retained 9,748-byte generated asset shows Unity material/font/texture serialization updates, not an H1 gameplay change. Its exact bytes were copied to private diagnosis evidence before recovery. The integration CLI exited zero at 19:12:09.294719 UTC and its owned job completed; the supervisor correctly refused input applicability at 19:12:10.640159 UTC. That campaign remains INCOMPLETE and will not be resumed or relabeled.

The narrow runner correction retains the measured generated font bytes/hash and restores the exact captured pre-stage font bytes before a successful integration stage returns. Unknown input changes or failed CLI runs are preserved without automatic restoration. The ten runner regressions cover this allowlist, retained evidence, failed-run preservation, exact final restoration and failure propagation. The supervisor's identity and input checks remain unchanged. A further owner approval is required before releasing the first replacement's verified idle reservation, restoring its font/settings or launching the next isolated campaign.

No Unity/runtime/content/schema/save-authority changes were made during correction. The inactive R and precise calculation seams, legacy zero fraction, existing wallet/publication, and deferred original/residual recovery remain unchanged. New PlayMode, Windows Development build and owner UAT remain N/A for this inactive technical slice; no omitted gate is counted as passed. No full-project/sustained profiling suite, new build, owner UAT session, H1-F4/H2/O1/Phase 8 work or merge is authorized here.

## Authorized R2 qualification — current result: PASS

The owner's subsequent **H1-F3 — Authorization for Scoped Qualification Recovery** explicitly approved verification of both stopped campaigns/processes, release only of b227's verified idle reservation through the reviewed procedure, exact restoration of the four archived inputs, retained generated-font evidence, and one fresh campaign. That authorization and the exact restoration results are retained privately in `TestResults/h1-f3-completion-investigation/approved-font-recovery.json` and `font-recovery-complete.json`. All identity/hash checks passed. Both old campaigns remain byte-identical and INCOMPLETE; neither was resumed or relabeled.

Fresh campaign **`dcc63c966bb444c98c4d59ae4ef12457`** ran against the exact requested head **`9e5f7aa7ddc08f192f70675b52c81b6c7a86e2e5`**, using product `Dungeon Lord B3B2 Smoke H1F3 Recovery R2 20261026` and unused outputs. It returned authoritative supervisor **PASSED**, including every selected preflight, test, restoration and final preservation stage. Qualification completed at **19:39:33.108436 UTC** on 2026-10-26. Unity 6000.3.2f1, CLI 1.0.0-beta.14 and Python 3.12.14 remain pinned by executable/tool hashes in the reviewed plan and retained records.

| R2 stage | Passed | Failed | Skipped | Supervisor |
| --- | ---: | ---: | ---: | --- |
| Focused Unit/SIT | 188 | 0 | 0 | PASS |
| Integration/SIT | 436 | 0 | 0 | PASS |
| Migration/regression and native Windows EditMode filesystem | 223 | 0 | 1 | PASS |
| Exact settings restoration | N/A | N/A | N/A | PASS |
| Final preservation | N/A | N/A | N/A | PASS |

Counts describe invocations, with overlapping fixtures, not unique project tests. The 34 new H1-F3 cases are inside the 188; the 98 Research durability cases are inside the 436. The single expected Windows inverse-platform skip is `DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`. No unexpected skip or inconclusive result occurred. The 30 supervisor and 10 runner regressions above remain applicable through exact unchanged tested tool-file hashes; their compact report distinguishes their actual execution source from the later evidence commit.

The integration runner retained the generated font rewrite (`f53586850a70c4f308e7e71b99b63881a49f38de4bd40f4edb7fe7ae6ecf8a52`) as a sealed stage artifact and restored the original (`6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d`) before returning. The supervisor then independently verified exact inputs and owned-job completion. Migration changed no unapproved input. No timeout, completion-drain extension, process exemption, forced termination or additional recovery was used.

All 1,462 inputs matched both predeclared states: qualified identity `983bf2fcba7639f02ef87da62c37b4ea63639987630849a246c16ab9d87530f0`, then restored identity `b8070243f931cf7b860cb846d4c705843df64a989850e6524b78cd67ed884b7a`. The four original input hashes after completion are:

| Input | SHA-256 |
| --- | --- |
| `ProjectSettings/ProjectSettings.asset` | `34da6d701e4c4629ca7b1cecb638f801d9c5ea4f40d33b09feca46777073b993` |
| `ProjectSettings/EditorBuildSettings.asset` | `53b28254e635e69536453567ccf17ced2aa18b42f73c896ed7251fbdbfd84fd9` |
| `ProjectSettings/ShaderGraphSettings.asset` | `6d98840f808336cd3b599d4ae82ef1582cc4bcb74b1f27bb4ad64c6c1d8a2f72` |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` | `6cd2faa131f346f601036104691f0223744c5806a51ceec454e6d4ff3bb6831d` |

After completion, no recorded supervisor, stage host or child remained live, and the read-only Unity/player process guard was clear. The durable reservation was released by the normal successful supervisor path and names R2 as `previous`; no unrelated reservation was changed. The intended worktree was clean before adding this documentation/evidence closeout.

### Evidence and preservation inventory

Published [summary, exact process identities, hashes and inventory](evidence/h1-f3-completion/summary.json), [sealed XML/plan/receipt/generated-font archive](evidence/h1-f3-completion/qualification-records.zip), and [tool regression applicability](evidence/h1-f3-completion/tool-regressions.json). Archive SHA-256: **`c341ad3146f3ba54cd0c0cf6029487a5b96c7025cacbf231db38c860da2c3238`**. Raw logs, private preservation manifests and authorization records stay local. The old source-specific reports and archives remain unchanged.

| Exact identity | Owner / purpose / source | Retention |
| --- | --- | --- |
| `C:/Users/gdg34/.codex/worktrees/h1-f3-stable-recovery/Dungeon-Lord` | Codex; managed implementation/review worktree; qualified source 9e5f7aa7 | Retain for independent review |
| Worktree `TestResults/qualification-supervisor/dcc63c966bb444c98c4d59ae4ef12457` | Supervisor; authoritative passing campaign, native identities and raw evidence | Retained evidence |
| Worktree `TestResults/h1-f3-recovery-20261026-r2` | Codex; exact input archive, reviewed plan, original settings | Retained evidence |
| Worktree `TestResults/h1-f3-completion-investigation` | Codex; diagnosis, approvals, restoration proof, original generated font | Retain privately |
| Worktree `TestResults/qualification-supervisor/b343351f516b4a19b8992ad00910ae40` and `b227e498d4ef4f2a91cd7801fb915881` | Prior supervisor; original stopped attempts at a68451bc and 6a2ac27e | Both INCOMPLETE, preserved byte-identically |
| Worktree `TestResults/h1-f3-preparation` and `TestResults/h1-f3-recovery-20261026` | Codex; prior source/input/preparation archives | Preserved byte-identically |
| `C:/Users/gdg34/AppData/LocalLow/gdg3417/Dungeon Lord B3B2 Smoke H1F3 Recovery R2 20261026` | Codex qualification; fresh synthetic test namespace | Retain for reproduction |
| Other 53 save namespaces, 506 files, under `C:/Users/gdg34/AppData/LocalLow/gdg3417` | Owner/prior qualification; exact private baseline includes both earlier H1-F3 namespaces | Hash-identical |
| `C:/Dev/Dungeon-Lord` | Owner checkout; still HEAD 4c13b9db with only original planning modification | Protected; untouched |
| `C:/Users/gdg34/.codex/worktrees/h1-f2-time-provenance/Dungeon-Lord/Builds/Development/Windows-h1-f2-headroom/Dungeon Lord.exe` | Prior qualified player at runtime source 591b2c3c | Published executable SHA-256 still `abc8179e345b70c9d7ca423c54e02739ed9b9adca668c2c745ffb81aadc05cc6` |
| `Docs/testing/evidence/h1-f3-completion` | Codex; sanitized review evidence from 9e5f7aa7 | Retained |

The original owner planning-edit hash remains `b04136e1b8375bef6dc2105bacd3e19062b4365be10e043b91c2d87670bd267d`. No owner save, build or prior evidence was deleted or overwritten. Only existing test-owned disposable fixture cleanup occurred; retained project/build/save/evidence artifacts were not broadly cleaned. No owner-UAT setup or new Windows build was created, because those remain N/A for this inactive slice.

This closeout adds documentation/evidence only after the qualified source. It does not relabel R2 as a run of a later SHA: the supervisor's exact-HEAD applicability check will reject a later commit, while its immutable recorded PASS remains attached to 9e5f7aa7. The PR head's executable/tool/input equality is checked separately, and no new Unity execution is claimed for the documentation commit. No blocking defect remains for included scope. The draft PR is eligible for independent ChatGPT review; owner testing/merge and all later H1/H2/O1 work remain separate.
