# Phase 7A4 persistence safety stop

Status: incomplete, unqualified implementation. No PR, commit, or merge was created.

Historical status above describes this first stop. The subsequent owner-approved unknown-outcome clarification and corrected protocol are recorded in [transactional resolution and validation](transactional-resolution-and-validation.md); this original reproduction and interpretation are retained unchanged below.

## Baseline

- Exact starting commit and current HEAD: `64d31257f06d1dafbf49593ade88f32b808bd556` (merged PR #225).
- Branch: `codex/phase-7a4-transactional-editor-production-dungeon`.
- Initial working tree: clean. Local main was at PR #224; the new branch was created from the exact locally available PR #225 commit.
- Writable canonical schema: 13. No migration or schema change.
- Validation checkout: `C:/Users/gdg34/.codex/worktrees/phase7a4-validation/Dungeon-Lord`.
- Owner save files were not used for automated qualification.

## Confirmed blocker

The proposed `FileDungeonDraftStore` reuses `ExactCompleteSaveAtomicPersistence` for draft replacements. The primitive can return a recovery-required failure when its post-replacement durability flush fails and restoration of the previous bytes also fails. The proposed independent draft reader does not reconcile that transaction evidence: it reads the surviving active file directly.

The targeted regression `FailedDraftDurabilityAndFailedRollbackNeverRecoverAnUnacknowledgedCommand` proves:

1. Create the separate empty draft, with acknowledged sequence 0.
2. Accept one reposition command into Pending presentation state.
3. Install its candidate bytes through atomic replacement.
4. Inject failure into the durability flush.
5. Inject failure into rollback replacement.
6. The proposed draft returns Failed and keeps its live acknowledged sequence at 0.
7. Reopen independently through the proposed store and replay.
8. Recovery reconstructs acknowledged sequence 1 from the surviving candidate file.

This violates the binding requirement that only the acknowledged predecessor-complete prefix is recoverable. Canonical save bytes remain unchanged in this reproduction. The problem is the proposed draft storage/recovery integration, not evidence that the existing canonical save recovery workflow is defective.

The implementation must not be treated as qualified or used for owner UAT. Implementation stopped instead of accepting the failed case or weakening INV-12. A dedicated acknowledgement/recovery protocol needs review before further implementation. No claim is made that such a protocol is impossible; the current small reuse does not provide it.

## Model policy reassessment

Recommended configuration for investigating and resolving this blocker: GPT-6 Astra, Medium.

Task classification: Ambiguous or High-Risk.

Reason: concrete fault evidence now exposes unresolved persistent-state acknowledgement and recovery semantics, changing the original well-defined integration task into persistence design work under sections 7, 11, and 16 of the merged model policy. This is a recommendation, not a claim that the executing session changed models.

## Validation actually completed

- Runtime/UI source compilation and production asset authoring through the established Unity CLI succeeded in the isolated checkout.
- Focused draft/presenter EditMode run before adding the blocker regression: 31 total, 31 passed, 0 failed, 0 skipped. XML: `TestResults/phase7a4-focused-editmode.xml`.
- Accurately injected blocker regression: 1 total, 0 passed, 1 failed, 0 skipped. XML: `TestResults/phase7a4-draft-rollback-boundary.xml`.
- Earlier attempts exposed a short long-text sample and test-discovery/API setup errors; these were corrected. They were not accepted as qualification.
- Actual-scene test code and screenshot helper were added but not executed or qualified.
- Full EditMode, full PlayMode, A2/A3 and Phase 4/6 regression selections, production content/build gates, Windows Development Build, and screenshot capture were not run after this stop.
- No screenshots or owner manual UAT evidence was produced.
- `git diff --check` reports two trailing-space lines introduced by Unity scene authoring (`m_Name:` fields). This remains an unfinished check, not a passing result.

## Partial work retained

Uncommitted work includes a proposed sparse version-1 whole-dungeon reposition journal, pending/acknowledged/failed states, detached canonical commit seam, UI Toolkit assets and scene composition, selected-floor Tilemap/SpriteRenderer presentation, viewport calculations and Input System device adapter, centralized presentation/physical-target policy, 39 English localization entries, focused tests, scene test source, and a screenshot helper.

The iOS physical-unit bridge and Android density adapter have not been device-built or qualified. Production visual layout and usability have not been inspected. Total/Usable Mana currently share the existing spendable wallet because no live upkeep-reservation authority was found; that presentation choice also requires review.

Planning status correction, complete required tests, evidence closeout, final PR description, source-control commit/push, and PR creation remain unfinished. Bootstrap capability implementation and the existing EventSystem were retained. No package dependency was added. All partial changes are subject to review and correction; they are not completion evidence.

## Subsequent acknowledgement-boundary reassessment

The original failure remains reproduced. The [companion investigation](acknowledgement-boundary-reassessment.md) records the trust-transition analysis, five passing diagnostic probes demonstrating indistinguishable successful/failed storage evidence, and the unresolved final acknowledgement boundary. Those probes are not a protocol fix. The original failing regression was strengthened and rerun: 0 passed, 1 failed, 0 skipped. Canonical bytes and mana remain unchanged, and live Save Changes remains blocked during the failure. The two scene trailing spaces have now been removed without reserialization; `git diff --check` passes. Broader validation and PR creation remain stopped. The historical results above are preserved as originally recorded.
