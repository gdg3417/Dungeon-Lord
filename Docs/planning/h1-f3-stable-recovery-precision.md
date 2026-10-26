# H1-F3 stable recovery entry and precise passive calculation

Implementation contract, 2026-10-26. Base: `443319983a746b4a1670bc8a87d7434a14f7a492` (#257).

## Scope and dependencies

This is the H1 foundation after F1's frozen historical reader and F2's schema-16 provenance/paired cuts. The [F2 policy/status table](h1-f2-time-provenance-paired-cuts.md) records owner-approved D1–D6, H1-C01-A and H1-C02-A; it supersedes conflicting older proposal wording. Specs 17/28/29, the invariant glossary, Living Dungeon and combat/recovery owner locks remain governing authorities. Current repository rules and qualification policies apply directly; historical Sprint gates are not new H1 dependencies.

The smallest dependency-correct slice is **inactive recovery capture plus read-only precise arithmetic**. It requires neither another schema nor a recovery episode. Production activation is deferred: boot currently grants passive mana before foreground initialization/draining; resume drains through the existing foreground barrier; ordinary/action saves independently sample whole seconds. Switching their passive endpoint to R would need the complete accepted-work/original/residual sequence, interval classification, and durable episode closure. Capturing a value alone cannot supply those guarantees.

## Actual foundation

`RecoveryEntryObservation.Capture` is internal and has **no production caller**. Its intended future coordinator boundary is after independently proven canonical load/recovery and before accepted-work draining. It requires validated `TimeProvenance` and an injected `IPreciseTimeSource`, calls `UtcNow()` exactly once, and retains the immutable complete `UnixTimePoint`. It neither reconstructs fractions from a seconds source nor offers an interpolation/subdivision API. Null, unsupported/nonpositive and backward samples are rejected. The caller must establish known-background versus unknown-post-crash classification; the type does not infer it. Known foreground, persistence stalls and unrecognized classifications are refused before clock capture.

The future coordinator must retain that same observation through processing/retry. R is process-local evidence, distinct from durable accounted time; it does **not survive another crash**. This slice does not enforce once-per-durable-episode capture, establish episode identity, or connect R to a grant. No UTC difference advances adventurers. Production boot/resume and their legacy sampling remain unchanged.

`CanonicalOfflinePassiveManaService.CalculatePrecise` accepts explicit validated point representations and boundary-state inputs. It rejects null, unsupported positive-time-domain violations and reversed intervals; subtraction is checked integer 100 ns arithmetic. All supported positive endpoints fit the signed 64-bit difference; overflowing/unrepresentable points cannot be constructed through `UnixTimePoint`'s validated API. Zero duration produces zero award. No duration cap is introduced.

Only after integer subtraction does it convert quotient seconds plus remainder to the existing `double` domain. This preserves exact whole-second duration even beyond double's exact integer tick range. The existing online rate resolver, configured base/effective efficiency, modifier/rounding order and shared offline formula own the result. Wallet fractions, full-capacity behavior and capacity clamping remain unchanged. Representation precision does not claim clock accuracy or exact arbitrary-precision wallet arithmetic.

The internal immutable `PreciseOfflinePassiveManaCalculation` is a separate type, with no conversion to the public grant result accepted by `SaveService.CommitOfflinePassiveMana`. Its proposed award cannot publish or become spendable. Calculation changes no save, session, wallet, provenance, geometry or disk state. Explicit source/endpoint inputs establish arithmetic only; they do not prove interval eligibility, current authority or boundary-state applicability.

## Compatibility and activation fence

The live `Resolve(long)` and historical projections use the same formula in their original operation order. No transaction signature, lifecycle coordinator or publication path changes. `lastSavedUtcUnix` remains the only seconds component of the schema-16 compound boundary; `legacyWholeSeconds` requires persisted fraction exactly zero. No schema bump, migration, new writable cursor/wallet, temporary field or staging owner is added. Frozen 7–15 contracts, exact 15→16 mapping, supported v1/v2 A>C obligations, monster recovery, Research acknowledgement and atomic rollback/readback/recovery authorities remain unchanged.

No gameplay tuning, content, localization text/keys, feature flag, renderer or graphics configuration changes are authorized or included. All sub-dungeons remain excluded from MVP and initial release.

## Acceptance and qualification

Required Unit: same-second/100 ns intervals, carry/borrow, zero/reversed/invalid/unrepresentable/overflow inputs, uncapped long intervals, exact legacy parity, rate/efficiency, fractional wallets/capacity, nonmutation, stable complete R and classification refusals. Required SIT: actual canonical boot/passive grant/reopen, lifecycle boundaries, action/stale saves, accepted-unclosed work, failed/uncertain publication/retry, Research durability and frozen/current-target compatibility. Existing affected suites supply real paths; helper-only tests do not substitute for them.

New PlayMode and Windows standalone qualification are N/A for this inactive slice: boot, grant publication, lifecycle coordination, writable bytes and filesystem primitives do not change. Existing deterministic integration paths still require execution. Separate owner UAT is N/A because there is no new player-facing flow or changed playable behavior. These applicability decisions require independent review; no omitted gate is called a pass. The [original stopped-attempt record](../testing/h1-f3-stable-recovery-qualification.md) remains historical. See the [current completion and R2 qualification report](../testing/h1-f3-completion-recovery.md#authorized-r2-qualification--current-result-pass) for authoritative results, exact source applicability and preservation inventory.

## Deferred work and risk

Complete A>C → original grant/recovery → grouped residual grant/recovery → ordinary actions ordering, durable episode identity/closure, nonzero fractional activation and interrupted-write crash-cut qualification remain H1 dependencies. Measured configuration-owned workload/overload policy, H2 retention/batching, O1 adventurers, remaining Phase 7 and Phase 8 remain separate. The historical intermittent native shutdown failure is preserved as an undiagnosed existing risk, not attributed to this foundation. Sequence remains H1 → H2 → O1 → project reassessment.
