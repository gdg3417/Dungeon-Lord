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
