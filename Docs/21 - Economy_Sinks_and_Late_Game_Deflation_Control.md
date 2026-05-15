# System Spec 21: Economy Sinks and Late Game Deflation Control

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines mandatory economic sinks and the deflation control levers that keep the game strategically interesting as output scales.

2\. Design Intent

2.1 Late game feel

Late game should feel powerful but still pressured. Players should always be managing tradeoffs, not idling into an unbounded optimal state.

2.2 Heat as risk driver

Heat is primarily a risk driver and limiter. It is not a required progression ladder.

3\. Growth Speed Control

3.1 Fill time guardrail

The economy must avoid reaching maximum mana capacity too quickly. A primary guardrail is a minimum fill time target, initially about 2 minutes, meaning the player should not fill from empty to full in less than this target during the stages where it applies.

3.2 Future scaling of the guardrail

At later points, the minimum fill time target can increase or shift to a different controlling variable to slow pacing as complexity rises.

4\. Mandatory Sinks

4.1 Monster upkeep

Monster upkeep is a reserved mana sink that scales with both monster level and monster count.

4.2 Expansion upkeep

Expansion upkeep is a persistent sink tied to dungeon size and depth.

4.3 Renovations

Renovations are an active sink tied to structural changes to layout and room typing.

5\. Expansion Upkeep Scaling

5.1 Components

Expansion upkeep scales using three components: tiles influence cost per room, rooms influence cost per floor, and floors increase costs using an escalating coefficient per floor index.

5.2 Tuning knobs

Tuning knobs include per tile base upkeep, per room multiplier, and per floor escalation coefficient.

6\. Renovation Rules

6.1 Cost triggers

Renovation costs trigger when moving tiles or swapping room types.

6.2 Loot table edits

Loot table edits do not trigger renovation costs. Loot table edits change reserve cost through the loot system's reserve computation.

6.3 Experimentation grace window

Renovations are reversible without cost if the player undoes the change within 30 seconds. This supports experimentation without punishing curiosity.

7\. Deflation Control Levers

7.1 Primary lever

The primary stabilizer is increasing upkeep curves, especially monster upkeep scaling.

7.2 Soft caps

Soft caps are preferred. Output can continue to grow but should do so with diminishing efficiency and increasing management cost.

7.3 Layout stacking control

If needed, introduce diminishing returns for repeated identical tile patterns or repeated identical monster stacks within a room, but only as a secondary lever.

8\. Risk Reward at Higher Heat

8.1 Positive pressure

Higher heat implies higher tier adventurers, which can yield better loot odds and higher value extraction when successful.

8.2 Counterbalancing risk

Higher heat also increases raid risk and the probability of outcomes that set back research progress.

9\. Player Fairness and Escape Hatches

9.1 Intended escape hatches

When players feel stuck, they should be able to rebuild layout, change monster roster, and refocus research. Progress should not require pushing into high heat as the only solution.

9.2 Clarity

The UI should make clear which sink is limiting progress, for example expansion upkeep, monster upkeep, or lack of usable mana due to reserves.
