# System Spec 26: Accessibility and Cognitive Load Targets

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines accessibility targets, information density rules, and the advanced view approach that balances depth with clarity.

2\. Target Audience

The default audience includes idle veterans, strategy players, city and base builders, and LitRPG readers. The UI must support quick checks and deep dives.

3\. Default UI Information Budget

3.1 Key metrics

The main HUD shows 3 to 5 key metrics. MVP candidate set includes: total mana, usable mana, reserve mana, mana per hour, and heat state.

3.2 Panel details

Additional metrics, such as heat delta per hour and adventurer throughput, live in per system panels and advanced views.

4\. Advanced View Model

4.1 Per panel toggle

Advanced view is enabled per panel, not as a single global toggle.

4.2 Ordering

In advanced view, show the final formula with substituted values first, then show a plain explanation of why the value changed.

4.3 Copy support

Advanced view includes a copy formula action for debugging and balance work.

5\. Accessibility Requirements

5.1 Text scaling

MVP supports text size scaling across major UI surfaces.

5.2 Colorblind safe heat

Heat states must be colorblind safe. MVP uses text plus color. Icons and shapes can be introduced later as polish.

5.3 Interaction targets

Touch targets and scrolling behavior must support comfortable one handed portrait play.

6\. Cognitive Load Guardrails

6.1 Plain language defaults

Default tooltips use plain language and avoid dense formulas.

6.2 Progressive disclosure

Numbers and breakdowns should appear only when the player opts into a panel or advanced view.

6.3 Change explanations

For key metrics, provide short change explanations, such as last three causes for heat, without forcing the player to open deep panels.
