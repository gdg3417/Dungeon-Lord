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

Heat and reserve mana are visible immediately, to avoid surprise mechanics. Default tooltips remain plain language.

3\. MVP First Session Goals

3.1 Session length

MVP first sessions target 5 to 10 minutes.

3.2 First clever moment

The first intended clever moment is achieved through a layout change that meaningfully improves outcomes. In later releases, a monster swap can become the first clever moment when multiple families exist.

4\. System Visibility Rules

4.1 Always visible in MVP

Total mana, reserved mana, and heat are visible immediately.

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

In default view, reserve mana is displayed as a single number. Reserve breakdown by source appears only in advanced views.

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
