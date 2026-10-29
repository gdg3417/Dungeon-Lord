# H1-F4 historical retention fixture qualification

Branch `codex/h1-f4-recovery`, draft PR #259; main baseline `3eb6ccf93558b8145d64e9e63cbefcbd73484602`. Qualified source **`902b4a6b0ab461a3cc79ecc89c1351b8cca2a8a5`**, changes `4d8f3ba3` and `902b4a6b`. Recommended GPT-6.1 Sol High, Complex; GPT-6 Sol High fallback. Changes are confined to `LivingDungeonPhaseB2Tests.cs`. No production, frozen-reader, serializer, migration, resource or tuning change.

## Historical reads versus current publication

The obsolete fixture attempted to reopen schema 17 with an unbound B1 qualification-profile context. That cannot authorize the historical retained-terminal executable under the approved current writer. The corrected `HistoricalModel` is an Editor-only private unit fixture: it uses the existing pure B1 scheduler, captures actual validated spatial/configuration inputs, and authors fresh in-memory schema-14 reader fixtures. Its complete frames pass the frozen reader with **CurrentTargetValidated=false**. It has no publication method, never installs its profile into the current context, and verifies that the source current session and active bytes remain unchanged. These frames are synthetic test data, not historical producer or migration proof.

| Original behavior | Retained assertions / justified current equivalent |
| --- | --- |
| Split/batch/reopen | Four budgets retain complete frozen-frame byte equality, two retained unconsumed terminals and allocator 3. Every reopen uses the frozen reader. |
| Active versus retained capacity | Existing two-active/four-retained behavior and allocator 3/5 remain pure historical scheduler assertions, with complete frozen-reader validation. No C2 occupancy claim is made. |
| Larger profile | Original synthetic technical bounds remain; pure profile upgrade and owner read preserve exact checkpoint bytes. The unsupported profile cannot bind current recovery inputs; no current context or resource envelope is replaced. |
| Two floors | Descent, success/health inputs are authored before ownership. Actual canonical floor activation precedes the historical model. Both terminal snapshots retain two floor transitions, one descent, the exact destination and checkpoint ceiling. The post-binding configuration replacement is removed. |
| Invalid owners / allocator | Existing whitespace/order/version/profile and duplicate/blank-history rejection assertions remain, after a valid frozen-frame baseline. Source canonical bytes remain unchanged. |
| Duplicate opportunities / overflow / no-op | Pure duplicate installation preserves revision and bytes; retroactive/consumed opportunities and checked overflow still reject. A supported schema-17 duplicate explicit-room placement exercises canonical no-op: IsNoOp=true, established no-op reason, no write, no runtime/session publication and unchanged bytes/references. |

## Exact reproduction and correction

First campaign **`94ea70301ef94b4b9a071ce208c86b86`**, source `4d8f3ba3`: **7 passed / 6 failed / 0 skipped**, permanently FAILED. Five failures reached `ContractJsonWriter.String` because the new fixture incorrectly used its generic source fixture's **100,000-character** budget. The historical reader/writer must use the actual frozen schema-14 resource: **655,360 characters**. `902b4a6b` selects those existing versioned reader limits without changing the resource, source context or current writer limits.

The sixth failure asserted IsNoOp on a normal foreground-pump transaction. `PrepareAndPersist` explicitly returns successful publication with IsNoOp=false; that flag is not a gameplay-equivalence predicate. The canonical duplicate-placement entry point explicitly returns IsSuccess=false / IsNoOp=true with reason `gd66.diagnostic.canonical_write_noop` before persistence. The corrected assertion tests that established contract, retaining no-write/no-publication and byte-equality checks. The historical scheduler's successful duplicate semantics remain independently checked as unchanged revision and bytes. No production behavior was modified to satisfy either assertion.

## Qualification and preservation

Fresh campaign **`6d14b60f32c94e2fb34e1527eb0d5278`** at full source `902b4a6b0ab461a3cc79ecc89c1351b8cca2a8a5`: **13 passed / 0 failed / 0 skipped**, all five stages PASSED. Independent verification matched the exact sealed NUnit names, source and tools, recorded native/host exits and Job completion, original inputs/settings/TMP, released reservation, protected owner/save/historical evidence and prior preparation/campaign trees. Compile-only diagnostics 03/04 completed separately with exit 0 and no Unity/NUnit execution; they are not counted as test passes.

The failed campaign had complete exit/Job records, unchanged qualified inputs and an already released reservation. Existing-Lease guarded finalization restored only its three settings, preserving original FAILED evidence and reservation bytes. No exceptional recovery, force unlock, process termination or reinterpretation occurred.

Frozen schema-14 resource SHA-256 remains `f535a0f7ba406916eb088dc499e58630e95eac340e88f18ce47227914bde642f`. [Exact cases, stacks, source/tool/input identities and receipts](h1-f4-historical-retention-qualification-20261029.json). H1-F4 owns preparations `TestResults/h1-f4-20261027-hm01` and `hm02`, their exact supervisor campaign directories, and compiler diagnostic outputs/scripts 03/04; all are retained for review. Generated default plans are preserved but only the source-pinned `plan-selected01.json` 13-case selections executed.

This is not a new complete 123-case result or a full-suite failure count. Remaining B2/B3A current-writer, lifecycle, compatibility and settlement fixtures still require correction/qualification. Full EditMode, applicable PlayMode, interruption/reopen, Windows runtime/shutdown and matched performance remain pending. PR #259 stays draft; no merge or owner UAT.
