# System Spec 8: Raids and Escalation Encounters

Status: Locked v1

Scope: Post-MVP systems defined, MVP simulation only

1\. Purpose of the Raid System

Raids represent the ultimate consequence of sustained political failure.

They enforce long-term balance and create high-stakes pressure without hard game over.

Raids are dangerous multi-wave pressure events focused on protecting the dungeon core.

For MVP, raids are simulated only.

2\. Raid Triggering and Warning Phase

2.1 Trigger Conditions

A raid becomes eligible when Political Reputation drops below the raid threshold.

Raids never trigger offline and always include a warning window.

2.2 Warning Window

The player is notified and given time to prepare.

If Political Reputation is restored during this window, the raid is canceled.

Once begun, raids cannot be abandoned.

2.3 Raid Frequency Limits

Multiple raids may occur if defended successfully.

A maximum of two raids may occur consecutively.

3\. Raid Structure

3.1 Wave-Based Design

Raids are composed of multiple waves such as scouts, elites, heroes, and a final assault.

For MVP, wave resolution is simulated.

3.2 Dynamic Raid Composition

Raid forces use predefined templates modified dynamically by dungeon depth, layout, monsters, loot profile, and past outcomes.

Raids react dynamically to resistance.

4\. Dungeon Core Rules

4.1 Core Location

The dungeon core is located in the final room of the final floor and hidden until revealed.

4.2 Failure Condition

A raid fails when dungeon core health reaches zero.

4.3 Victory Condition

A raid is successfully defended only if all planned waves are completed.

5\. Dungeon Core Damage and Consequences

5.1 Effects of Damage

Core damage reduces mana generation, slows research, and applies reserve penalties.

5.2 Recovery Rules

Damage is temporary and fully recoverable over time.

5.3 Game Over Rules

Core destruction does not end the game.

6\. Player Agency During Raids

For MVP, player agency exists only during preparation.

Future versions may allow mana sacrifice for emergency boosts.

7\. Raid Research

Raid-specific research does not exist in MVP.

8\. Rewards and Penalties

8.1 Successful Defense

Successful defense grants partial political reputation recovery and a permanent prestige marker.

8.2 Failure Penalties

Failure applies temporary mana penalties and reduces active research progress.

9\. System Interfaces

Raids interact with Political, Economic, and Guild Reputation systems.

10\. MVP Constraints

Raids are simulated only.

No real-time raid gameplay.

No raid-specific UI beyond warnings and results.

11\. Failure Conditions

The system fails if raids can be farmed, fully reset reputation, or cause irreversible loss.

System Spec 8 is locked.
