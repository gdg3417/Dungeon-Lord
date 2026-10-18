# Living Dungeon B3B1 durable timed combat compatibility

Authorized 2026-10-18. Verified local/remote main: `ce39ca44759f1f7d2338d9ec73aab9ea95b95534` (#242/#243 merged); clean start, no open PRs. Branch `codex/living-dungeon-b3b1-durable-combat`. Recommendation: GPT-6.1 Sol High, Complex: locked deterministic combat, immutable inputs, historical executable and save integration. Fallback: GPT-6 Sol High, then GPT-5.6 Sol High; no Astra escalation or delegation.

## Execution and inputs

The explicit `StartTimedCombatSnapshot` uses the existing production snapshot, party generator, iterator and C0/C2 kernel. Timing is mandatory/injected. No production duration, clock, scheduler activation, round subdivision or second simulator is added. `StartSnapshot`/`StartRoute` remain B1. Existing untimed manual C0/C2 starts retain their behavior and refusal to export a B1 checkpoint.

Format **2** stores actual `run.executable.combat-c0.v1` or `run.executable.combat-c2.v1`, separately versioned `run.combat.prefix.v1` evidence, immutable start bytes, ordinal/time/deadline, terminal/consumed state, prefix digest and committed inspection. Independently retained checkpoint/start identities remain mandatory; checksums do not establish provenance.

Combat start version 1 includes sequence, optional event ceiling, timing, the existing complete snapshot representation (configuration, loot, runtime, knowledge, plans, geometry/connections and fingerprints), effective C0 profiles/policies/limits, room bounds, and C2 identity/level/HP/revival and readiness-validation policy. Unity-nonserialized combat inputs are explicit. Reconstruction consults no current dungeon, resource, recovery service, clock or save. Retained continuity is immutable input, not a second writable owner.

Each existing engagement stays atomic. Damage, defeats, receipts and reached consequences commit only after the existing pre-effect deadline gate. Equal times keep ordinal order. Combat evidence binds ordered action/attack/target/position/reason/HP/damage/defeat data, cumulative monster/member condition, counts and committed work, alongside existing route/trap/reward/branch/floor evidence. Outcomes are reconstructed without a second journal.

## Lookahead and limits

`Prepare` may perform bounded geometry collection and charge kernel work before announcing a boundary. Pending announcements are cached; repeated due-time reads do not repeat that work. Export never prepares. Only a successful boundary updates `CommittedCombatWork` and committed evidence. Pending work is derived, not serialized. After replay, the next probe recomputes it once; eventual work and workload-limit failure behavior must equal uninterrupted execution. Failed handles cannot export or publish. Manual work accounting and kernel limits stay unchanged.

New decoding requires an independently injected nonzero combat-work envelope, plus existing byte/replay limits. Existing Profile A retains its exact serialized B1 definition, timing/start factory and numeric budgets. An explicit compatible injected profile can validate mixed records. Aggregate replay is still checked before reconstruction; total combat work is bounded by retained records times the injected per-run work envelope. No save/parser/replay budget is enlarged automatically.

## Canonical and activation boundaries

Writable schema remains **15**. No new owner, allocator, migration or historical reader. Frozen B1 format-1 encoding/fold remains intact; supported-format dispatch selects its reader or the combat reader. B3A Editor-only settlement rejects both C0 and C2. No production combat admission adapter or settlement is added. Replay cannot mutate canonical continuity, grant mana, apply Heat, recover monsters, append history, publish knowledge or consume rewards. Existing predecessor/atomic persistence/readback/rollback/recovery-required behavior remains authoritative.

Use existing test fixtures/bridges and the root Unity project/installation. Preserve saves/builds/historical evidence, verify substitutions and committed source before every launch, and use unused campaign/output paths. Actual results, failed attempts, sizes/work and preservation belong to `Docs/testing/evidence/living-dungeon-b3b1/qualification.md`; this contract is not a passing qualification claim.

Before one automatic foreground C2 run can execute and settle: qualify trusted foreground provenance/restart and configured logical/world timing; admission demand and causal event/edit ordering; one-party-per-floor admission and durable descent waiting; atomic production C2 outcome settlement/retirement with exact-once injury, reserve/recovery/history/knowledge/economy effects; then the bounded end-to-end save/reopen/failure path. Mixed/offline periods separately require causal attribution and loss protection. No optional planning phase, sub-dungeon, Mana Farm, tuning, text or graphical redesign is introduced.

Unresolved settlement reconciliation: later edits can move/unassign/retire a captured monster or alter its condition. This packet retains the exact start/outcome but grants no overwrite/rebase policy. The smallest future decision is an explicit identity/condition conflict contract at production C2 settlement, qualified with occupancy and ordered edits.
