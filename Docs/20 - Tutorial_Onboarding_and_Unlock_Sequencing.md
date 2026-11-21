# System Spec 20: Tutorial, Onboarding, and Unlock Sequencing

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines how the player is introduced to core systems, how systems are revealed over time, and how the first 10 minutes are paced to support retention and trust.

2\. Onboarding Philosophy

2.1 Soft guided

Onboarding uses optional hints and callouts. The player retains agency and is not forced into premium currency actions.

2.2 Skippable tutorial

The tutorial is skippable. If skipped, a recommended next steps panel persists until the player completes core actions once.

2.3 Trust first

Heat is persistently visible immediately. Reserve mana remains understandable and discoverable through an appropriate resource/detail/advanced surface, so it cannot become a surprise mechanic. Default tooltips remain plain language.

3\. MVP First Session Goals

3.1 Session length

MVP first sessions target 5 to 10 minutes.

3.2 First clever moment

The first intended clever moment is achieved through a layout change that meaningfully improves outcomes. In later releases, a monster swap can become the first clever moment when multiple families exist.

4\. System Visibility Rules

4.1 Always visible in MVP

The persistent Normal/Inspect/Run HUD shows Total Mana, Usable Mana, Mana/hour, and Heat. Reserve mana is available through deliberate expansion, contextual resource information, or an advanced/detail surface.

4.2 Research visibility

Research is visible immediately but locked until the player meets unlock conditions. The UI signals that research is future depth and a reason to return.

4.3 Loot table visibility

Loot tables are visible, but editing is locked until the first successful adventurer run, so the player has context for what loot means.

5\. Unlock Sequencing

5.1 Strategic order

The first strategic choice is room layout, followed by monster roster selection for the room, followed by loot strategy, followed by research path selection.

5.2 Research unlock conditions

Research unlocks when the player has completed at least one successful run and has at least one eligible research entry, such as a starter node or an absorbed loot token threshold.

6\. Recommended Next Steps Panel

6.1 Persistence

A recommended next steps panel is shown until the player completes core actions: place or modify layout, complete a successful run, start first research, and view the first offline summary.

6.2 Content

The panel presents a short ordered list with one tap navigation to the relevant UI panels.

7\. Heat and Reserve Messaging

7.1 Heat explanation affordance

The heat meter includes an action that shows the last three causes of heat change in plain language.

7.2 Reserve display

Reserve mana is available in a deliberate resource/detail/advanced surface; its breakdown by source appears only in advanced views. It is not a required persistent default-HUD metric.

8\. Failure Recovery Messaging

8.1 Trigger timing

Failure recovery messaging is shown only when the player is about to trigger a failure state.

8.2 Tone

Messaging emphasizes that failures are recoverable and clarifies what will pause or shut down first, without blaming the player.

9\. Monetization Guardrails in Onboarding

9.1 No forced premium spends

The tutorial never forces a premium spend.

9.2 Optional prompts timing

Optional premium prompts must not appear until research is unlocked and the player has started at least one research task.

10\. Advanced View Integration

10.1 Default versus advanced

Default tooltips are plain language. Advanced view is per panel and can show substituted formulas, keeping onboarding uncluttered.

## Owner-approved onboarding sequencing clarification (2026-11-19)

PR #269, **Guide the first Dungeon session**, was closed without merging after owner UAT. The owner deferred the complete interactive tutorial until the supporting MVP mechanics and production UI are sufficiently mature. This is a scheduling decision, not abandonment of onboarding, a finding that its qualification tests failed, or approval to merge its implementation. Preserve its GitHub history and associated branch, worktrees, screenshots, tests, reports, protected saves, qualification receipts, and design findings.

The existing soft-guided, skippable model and recommended-next-steps panel remain. Preserve the four core milestones: place or modify a layout; complete a successful adventurer run; start first research; and view the first offline summary. This amendment does not change the existing Research unlock condition, redefine successful-run completion, authorize fabricated run events, or add tutorial rewards.

The owner-approved future interaction direction, still requiring implementation and detailed design where appropriate, is: introduce the player as a living dungeon core with a short skippable introduction and a small guide sprite available for contextual help; teach first-room construction; allow a nonexclusive MVP starter choice between Goblinoid/Goblin and Undead/Skeleton; guide actual monster and loot placement; demonstrate moving, unassigning, and redeploying owned content; retain Cancel, Skip, and draft safeguards; and keep the normal completed tutorial path eligible for scheduler-controlled adventurer visits without fabricating one. Keep tutorial control lessons distinct from a compact HUD-style dungeon quest tracker whose progress comes from authoritative gameplay evidence, not tutorial button presses. Quest rewards remain deferred. Add Research onboarding only when its Phase 8 mechanics and production UI exist. Retain dungeon-first navigation and emergent dungeon identity, reconciling any opening scene with approved direction.

These are future interaction requirements, not implemented or finally designed mechanics. Existing research eligibility, progression, save, localization, domain-invariant, and no-forced-spend decisions remain governed by their specifications and approved authorities. See the [dated MVP sequencing decision](../docs/planning/decisions/mvp-execution-sequencing-2026-11-19.md) and the [owner's PR #269 closure decision](https://github.com/gdg3417/Dungeon-Lord/pull/269#issuecomment-7015127781).

## Owner-approved starter Research prerequisite (2026-11-19)

For Adventurer Activity Analysis, the owner clarified section 5.2 in [PR #271 Review 2](https://github.com/gdg3417/Dungeon-Lord/pull/271). The starter project's run prerequisite is met after one genuine, canonically completed adventurer run, regardless of whether the adventurers successfully cleared the dungeon. The authoritative run record must have a valid allocated identity and a confirmed outcome. An admission alone, an unfinished run, or fabricated or uncommitted evidence cannot meet the prerequisite. Sections 4.3 and 6.1 retain their separate successful-run requirements.
