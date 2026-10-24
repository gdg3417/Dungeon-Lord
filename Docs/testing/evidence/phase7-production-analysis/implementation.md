# Production Analysis and Dungeon navigation

Implementation baseline: `7dc6f96b5c35f24923f6e8cbfa8a5e256c16c3ac`, latest main fetched 2026-10-22. This contains #247 automatic two-floor foreground gameplay and #248 completion-driven qualification tooling. Branch: `feature/phase7-production-analysis`.

## Capability and authority

The Dungeon remains the landing surface. A compact recent-performance entry occupies the existing normal/inspection sheet header and Analysis opens a dedicated, scrollable read-only destination. Dungeon returns to the retained floor and viewport. Display settings, safe-area layout, existing platform touch-target policy and Default/Large text apply to Analysis. During Edit Mode, Analysis is available through the Display menu so essential editing controls retain their space. Research and More remain subsequent work; no placeholders or Bootstrap retirement are introduced.

`ProductionAnalysisPresenter` projects only `RunHistoryState.RecentOutcomes`, bounded by the existing history owner. Both surfaces consume the same cached detached projection. Append/reopen replaces the retained array; frame updates do not reconstruct the aggregate. Canonical publication refreshes presentation. Report categories have deterministic ordinal key ordering; source history stays in its original append order.

Non-null retained completed records form the reporting window. Valid resolved survival summaries require positive party size, nonnegative survivor/death counts, matching party totals and no deterministic error. Resolved, nonnegative extraction summaries with no deterministic error contribute extracted world value. Each total has its own coverage denominator. Nonempty durable route-outcome keys contribute recorded categories. Missing fields do not imply failure, zero loot, floor reach, trends, or causes. LatestOutcome is not counted again or used to fabricate an absent window. Existing presenters provide localization/formatting and compatible transient replay; the spoils ledger's fallback loot estimates are deliberately unsuitable for durable extraction totals.

Historical movement/combat/EXIT-versus-DESCEND causes are not reconstructed. A localized explanation points to the existing session-only compatible Replay Run capability on Dungeon. Analysis itself supplies no causal reconstruction or new storage. No Advanced View is added, so Decision 102 introduces no new preference here.

## Transactions and continuity

Clean edit drafts use the existing discard/close authority before navigation. Modified valid drafts offer Save, Discard and Continue Editing, with Save disabled while durability or baseline eligibility blocks it. Structurally invalid drafts offer Discard and Continue Editing. The existing save-review economic and movement information accompanies eligible saves. Navigation occurs only after a successful commit or discard. Failed operations retain the draft and current destination; Continue cancels the request. Recovery uses the existing recovery modal and authority.

Analysis hides the dungeon camera and blocks world/covered-control input. It does not disable GameRoot, pause time, create gameplay authorities or mutate active parties. Replay presentation closes when entering Analysis; the authoritative Living Dungeon continues. Selected floor and camera context remain in the same controller. During replay the compact header uses one ellipsized line, with the full localized summary in its tooltip, so replay/edit controls retain their safe-area allocation. Both the direct replay transition and later presentation updates refresh that compact form.

Schema 15, migrations, canonical models, checkpoints, history ownership, event order, gameplay tuning, mana, Heat and recovery rules are untouched. Specifications: Phase 7A0 Decisions 69–78, 85, 87, 95–96, 100, 102–103 and the Living Dungeon owner design lock.

## Gates and delivery

See qualification.md for actual commands/results and exact applicability. Full final EditMode/PlayMode qualification, Windows standalone build and owner Moonlight UAT are pending independent ChatGPT review and any corrections. No final qualification, owner acceptance or standalone readiness is claimed by this implementation packet.

After review, Codex must prepare fresh isolated qualification inputs and a new standalone product/save identity, complete all required final gates, then prepare the owner fixture and launch the build. The owner should only connect through Moonlight and exercise navigation, continued arrivals/outcomes, draft Save/Discard/Continue, close/reopen consistency, portrait/landscape and Large text. Owner save handling, JSON preparation, PowerShell and Unity/process setup remain Codex responsibilities. This packet does not launch that post-review campaign.

The #248 supervisor has frozen historical adapters and does not itself prepare a new Unity campaign. It must not reuse historical output identities or stand in for final gates. Focused iteration uses bounded Unity CLI invocations against a fresh disposable project; sustained 68-run profiles are excluded from iteration.

Remaining Phase 7 work includes Research/More production parity, onboarding and any remaining Bootstrap capability replacement, independent review, final qualification and owner acceptance. Phase 8, offline catch-up and additional simulation systems are outside scope.
