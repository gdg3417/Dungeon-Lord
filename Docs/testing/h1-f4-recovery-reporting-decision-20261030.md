# H1-F4 recovery summary: accepted R1 presentation contract

Status: **independently accepted and explicitly authorized by the owner on 2026-10-30**. Implementation/qualification is in progress from `cfa609cfebb0ce86cc6e817a1dd7c2b62115f50f`. Core recovery/time/settlement policies remain unchanged. This decision concerns truthful presentation of committed effects, not eligibility, amounts, save authority or durable encoding.

## Demonstrated gap

At `cbac2ea3d1cf3f2db82634c4944e434355f0935a`, campaign `544e9c1243b64539b1445e3193ebef47` selected the existing cold-start case and existing root coordinator control. Raw result: 1 passed / 1 failed / 0 skipped, normal FAILED with guarded restoration verified.

The cold case authors canonical W=1000 and wallet=12.5, loads through the real prebound SaveService, captures recovery at 1001 and pumps the actual GameRoot coordinator. It successfully asserts closure, wallet=12.5075 and W=1001. It then fails because `LatestOfflinePassiveManaResultForTests` is null. The unchanged assertion is at `OfflinePassiveManaTests.cs:326` in that pinned source. This is not the earlier legacy direct-grant rejection.

Source path:

- `GameRoot.cs:597–612` selects atomic recovery instead of legacy `TryApplyOfflinePassiveMana`.
- `GameRoot.Recovery.cs:119` pumps the real recovery service and handles durable success without setting the old presentation result.
- `DetachedCanonicalWriteAuthority.Recovery.cs:30–51` calculates the original/residual grant locally and persists wallet/healing, but does not return its grant evidence for presentation.
- `GameRoot.cs:794` assigns `_latestOfflinePassiveManaResult` only in the legacy path. `RefreshOfflinePassiveManaLine` reads that field; the current recovery path leaves it empty.
- `OfflinePassiveManaPresenter.cs:24` renders null as an empty line.

The existing approved Phase-4 UAT contract, `Docs/testing/evidence/phase4-offline-passive-mana/manual-uat.md`, requires the summary to show elapsed time, effective rate, award, current mana and capacity limitation. Removing that assertion would conceal an existing feature regression.

## Why the display contract needs an explicit choice

H1-F4 introduces two settlement publications and eligible durations that can contain separately classified segments. A wall subtraction is not the grant duration. Known foreground stalls and successful S→Q foreground eligibility cannot be added to offline reporting as if they were eligible absence.

After a crash, the save proves settlement but does not necessarily retain a detailed grant receipt. The approved format-2 owner stores W, context and pending duration obligations; it does not store the original pre-grant wallet or an award history, and closure clears Episode. Neither schema 17 nor its reserve should be expanded just to recreate a display.

Illustrative information-loss counterexample, derived from the existing capacity rule, not an additional qualified scenario: with capacity 1000 and a calculated original award of 27, starting wallets 980 and 990 produce actual awards 20 and 10 but the same final wallet 1000. With otherwise identical state and zero residual duration, their closed saves can be identical. A current balance, duration and configuration cannot prove which historical award occurred. A retained predecessor hash is not the missing predecessor bytes.

The existing unavailable fallback is unsafe for this situation: `ui.offline_mana.no_award_format` says **"No offline mana was awarded: {0}."** That claim may be false when effects committed but reporting evidence was lost. The existing applied text also says "Away for ... seconds"; eligible recovery processing is not proof of exact physical background duration under L1.

## Accepted R1: complete-proof-only, transient summary

1. Preserve the existing summary for an episode only when independently proven original and residual publication evidence is available. Use the existing exact transition durations, computed/clamped awards, healing costs and canonical final wallet; do not infer these from wallet differences or UTC spans.
2. Keep at most one fixed-size process-local aggregate and the already bounded pending-publication evidence. It is derived presentation data with no grant/spend/time capability, no file, no new save field, no timestamp cursor, no journal and no new durable identity.
3. Original plus residual evidence is combined only for the same existing episode identity and exact proven candidate(s). Duplicate callbacks/retries do not accumulate again. Proven rollback contributes nothing; uncertain publication contributes nothing until exact canonical proof succeeds.
4. When an earlier receipt cannot be proven after reopening or process loss, suppress the historical breakdown. If this process proves episode closure, show a neutral localized completion status and the current authoritative wallet. If it merely loads a closed save with no current-process history, retain only the normal wallet display. Do not display zero/no-award, infer a pre-grant wallet, claim a complete episode total or block otherwise completed recovery for missing display evidence.
5. Distinguish an actual fully observed zero-duration episode from missing evidence. Preserve the existing zero-elapsed/no-generation/capacity distinctions when their facts are known.
6. In the applicable localized summary, describe **eligible recovery time**, rather than claiming exact physical time away. Retain stable localization ownership and existing quantity formatting; no broader UI redesign or tuning.

The independent acceptance includes these proof boundaries:

- A reporting receipt is bound to exact predecessor/candidate bytes, validated settlement phase, existing episode identity, context and exact classified eligible duration. Calculated and actual awards, capacity limitation and actual healing spending are copied at preparation; provisional receipts are not committed.
- The one SaveService-owned process-local aggregate accepts original/residual evidence only after successful durable publication or independent proof of the exact retained candidate. A proven predecessor rollback discards attempted evidence. Duplicate acknowledgements do not add awards twice.
- Uncertain-publication reconstruction does not reconstruct spending. Its default spending value is not a receipt. Missing original or pending spending evidence prevents a full breakdown even if durable settlement succeeds.
- The fixed aggregate contains at most two receipts and a predecessor hash for continuity, with no persistence, time, retry, wallet or scheduler capability. New episodes, incompatible identity or lost continuity clear stale evidence. Ordinary refreshes preserve completed evidence and render the current canonical wallet.
- Original/residual rates remain separately available. Equal rates may share a label; unequal rates use distinct localized labels. Duration is the exact sum of classified 100 ns counters, never a UTC span or the sum of truncated whole seconds. Proven zero remains distinct from unavailable evidence.
- The legacy result/presenter remains unchanged. Current recovery uses a separate presentation path with stable localization keys, not a fabricated legacy result. No save fields, migrations, schema or atomic persistence behavior are added.

The time, wallet, monster and save policies themselves do not change. No storage or hidden observations may be reconstructed to satisfy the old summary assertion.

## Qualification after acceptance

The three deferred lifecycle/reporting cases now pass through the actual single-owner coordinator. The complete directly affected cohort passed 306/0/0 at `2c90eca04cc8510672086dd9fe05130423bfc3d8`, campaign `7f3d8b5c387b4b65aee0bd0e8a6394a9`, with all five supervisor stages and independent preservation verified. [Source-pinned report and accepted limitations](h1-f4-r1-reporting-qualification-20261030.md). This covers same-process original/residual summary, exact fractions and excluded stall segments, capacity/healing/current wallet, rollback/retry, uncertain acknowledgement and proven resolution, repeated callbacks, and reopened residualPending with unavailable original receipt. Pure legacy presenter/calculation tests and historical contracts remain intact. Remaining regressions and mandatory full-suite/Player/performance gates are not completed by this cohort.

Other remaining fixture investigations can continue independently. No new production clock seam is proposed: the approved Editor-only prebinding seam already exists in current source. The historical draft-loss cause remains unproven.
