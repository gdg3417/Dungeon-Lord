# M7-C0 Research Productionization and Online Verification Boundary Plan

## Purpose

This documentation-only plan defines the safe bridge from the completed developer-only M7 research lifecycle scaffolds to future production-facing research behavior. M7-B8 closed the active research lifecycle scaffold as **GO WITH FOLLOW-UP**. M7-C0 records the boundary for that follow-up without implementing production UI, online verification, claim mutations, gameplay behavior, or additional research features.

The next safe productionization step is a read-only view of the existing lifecycle state. Production mutation must remain blocked until an online verification boundary is explicitly scoped and decided.

## Current Completed Foundation

The developer-only research lifecycle foundation currently includes:

1. A research-pending scaffold for one active project.
2. An active progress preview.
3. Additive research progress state.
4. Active-session progress accumulation.
5. Completion eligibility evaluation.
6. A completion-pending marker.
7. Claim-readiness diagnostics.
8. Completed-research save state.
9. A developer-only claim transition that can record completed research and clear active pending and progress state.
10. Research Diagnostics paging and scrolling.

This foundation is an audit-ready development scaffold, not a production research experience. It does not provide production research UI, production claim behavior, online verification, rewards, unlocks, costs, multiple active research slots, offline research progression, or offline heat.

## Locked Constraints

The following constraints remain locked for M7-C0 and for the immediate productionization bridge:

1. Research remains MVP single-slot: only one active research slot is supported.
2. No production claim may be added until the online verification boundary exists and its behavior is explicitly decided.
3. No research rewards are introduced.
4. No research unlocks are introduced. Completed research must not silently unlock content until content-owned unlock tables and diagnostics exist.
5. No research costs are introduced.
6. No offline research processing is introduced. `WouldProcessOfflineProgress` must remain `false` until a separately scoped offline research PR changes it.
7. `allowOfflineProgression`, if present, remains dormant.
8. No offline heat is introduced.
9. No server-authority implementation is introduced in this PR.
10. No M6 heat behavior changes are permitted.

Research start and research completion require online verification in the intended production model. M7-C0 does not implement either verification path.

## Productionization Boundary Decision

The recommended productionization path is deliberately incremental:

1. **M7-C1** should add a production-safe, read-only research status presenter.
2. **M7-C1** must not add claim mutation.
3. **M7-C1** must not add rewards, unlocks, costs, or offline behavior.
4. **M7-C2** should add an online verification boundary stub or an explicitly scoped local-development placeholder before any production claim action is introduced.
5. A production claim button must not be added until verification behavior is decided.

Read-only production status must precede any production claim affordance. This sequencing allows the project to expose lifecycle state safely while keeping mutation behind an explicit verification decision.

## Online Verification Boundary

The intended future online verification model is:

1. Research start requires online verification.
2. Research completion requires online verification.
3. A project that reaches its local progress target while offline must remain pending rather than becoming production-claimable.
4. Server verification is future work and is not implemented by M7-C0.
5. Local development may use a clearly named placeholder only if a later PR explicitly scopes that placeholder, its limitations, and its replacement path.
6. Player-facing UI must distinguish **progress complete**, **verification pending**, and **claimable** states.

Until this boundary is implemented and decided, existing developer-only claim behavior must not be promoted into a production action.

## Future Production Research Status States

Future UI planning should use a small, explicit state vocabulary:

| State | Intended meaning |
| --- | --- |
| `noResearch` | No active research project exists. |
| `activeInProgress` | One active research project exists and has not yet reached completion eligibility. |
| `activeCompletionPending` | Local progress has reached the completion threshold and the lifecycle is awaiting its next permitted step. |
| `verificationRequired` | Progress is complete, but required online verification has not succeeded or cannot currently run. |
| `readyToClaim` | Verification behavior allows a future production claim action. |
| `completed` | The stable project ID is recorded in completed research state. |
| `blockedOrInvalid` | The state cannot safely advance because required data is missing, inconsistent, or rejected. |

These names are a planning vocabulary, not a request to add runtime enums, save fields, or localization entries in M7-C0.

## Player-Facing Information Requirements

A future production presenter should clearly expose:

1. The current research project ID, or a localized project name once content-owned naming exists.
2. The current progress amount.
3. Completion-pending status.
4. Verification-pending status.
5. Claim availability.
6. Completed state.
7. A clear no-reward and no-unlock caveat until reward and unlock systems exist.

Any future player-facing wording must come from localization references. This plan does not add player-facing English text, localization keys, content-owned research naming, or production UI.

## Save Compatibility Guidance

Later research productionization PRs must preserve save compatibility:

1. Preserve `researchPending`.
2. Preserve `researchProgress`.
3. Preserve `completedResearch`.
4. Do not rename stable research project IDs.
5. Do not add save fields unless a later PR proves that they are needed.
6. Keep legacy saves safe, including saves with no pending research, partial progress, completion-pending progress, and completed research state.

M7-C0 does not change save models or migration behavior.

## Diagnostics Guidance

Research Diagnostics remains the audit surface throughout productionization:

1. Research Diagnostics remains available as the lifecycle audit surface.
2. A future production presenter must not remove or replace diagnostics.
3. Diagnostics must continue showing no-pending, in-progress, completion-pending, ready, claimed, and completed-state transitions.
4. Claim or clear transitions must leave no stale project IDs.

M7-C0 does not change diagnostics behavior.

## Recommended Future PR Sequence

1. `M7-C1: production-safe read-only research status presenter`
2. `M7-C2: research online verification boundary stub`
3. `M7-C3: production-safe claim affordance, disabled unless verification allows`
4. `M7-C4: research lifecycle closeout audit`
5. `M7-D0: offline research progression planning` — documentation-only
6. `M7-D1+` — only if the project is ready to change `WouldProcessOfflineProgress`

Each PR must remain independently scoped. In particular, M7-C1 is read-only, M7-C2 decides the verification boundary before production claim behavior exists, and offline research remains a separately planned M7-D concern.

## Explicit Non-Goals

M7-C0 does not implement or authorize:

1. Production UI implementation.
2. A claim button.
3. A server call.
4. A backend API.
5. Rewards.
6. Unlocks.
7. Costs.
8. Multiple active research slots.
9. Offline research progression.
10. Offline heat.
11. M6 heat behavior changes.
12. Prestige.
13. Seasons.
14. PvP.
15. Leaderboards.
16. Hero adventurers.
17. Advanced diplomacy.
18. Raids, `Hostile`, `Raid`, or reputation axes.

## Acceptance Criteria

M7-C0 is complete only when:

1. The change is documentation-only.
2. Exactly one new planning file is added: `docs/planning/m7-c0-research-productionization-boundary-plan.md`.
3. No runtime code changes are made.
4. No test changes are made.
5. No content JSON changes are made.
6. No localization changes are made.
7. No Unity scenes or prefabs are changed.
8. No Unity assets or `.meta` files are changed.
9. No save models, diagnostics behavior, simulation behavior, or gameplay behavior are changed.
10. No Unity tests are required because the change is documentation-only.

## M7-C0 Closeout Review

Before promotion, confirm:

1. Only `docs/planning/m7-c0-research-productionization-boundary-plan.md` changed.
2. No C# files changed.
3. No JSON content files changed.
4. No localization files changed.
5. No Unity scene or prefab files changed.
6. No test files changed.
7. The plan recommends read-only production research status before any production claim.
8. The plan blocks production claim until the verification boundary is explicitly scoped and decided.
9. The plan does not start rewards, unlocks, offline research, or multi-slot behavior.
