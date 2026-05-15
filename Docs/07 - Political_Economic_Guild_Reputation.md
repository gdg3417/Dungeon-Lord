# System Spec 7: Political, Economic, and Guild Reputation

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the System

This system governs how the outside world perceives, reacts to, and eventually intervenes in the dungeon.

It replaces a single overloaded heat value with three distinct reputation axes that together determine political pressure, adventurer behavior, and escalation risk.

2\. Reputation Axes Overview

The dungeon tracks three independent reputations. All three are good when high and bad when low.

Political Reputation

Represents how much the kingdom trusts the dungeon not to harm its people.

High = trust and stability.

Low = suspicion and fear.

Deaths reduce political reputation.

Economic Reputation

Represents the dungeon’s contribution to the surrounding economy.

Offsets political pressure but never fully negates it.

Guild Reputation

Represents how adventurer guilds perceive fairness, safety, and professionalism.

Strongly influences party quality and repeat traffic.

3\. Political Reputation Rules

Decreases from:

\- Adventurer deaths weighted by value

\- Elite or named deaths

\- Monster overflows

\- Unfair danger vs reward

Increases from:

\- Civic focused merchant contracts

\- High survival training events

\- Diplomacy actions

\- Successful raid defense

4\. Economic Reputation Rules

Increases from:

\- Extracted tradeable loot

\- Merchant contracts

\- Currency circulation

Decreases from:

\- Contract failures

\- Supply instability

5\. Guild Reputation Rules

Increases from:

\- High survival ratios

\- Predictable difficulty

\- Fair loot tables

Decreases from:

\- Unexpected wipes

\- Trap overuse

\- Sudden difficulty spikes

6\. Reputation States

Peace

Notice

Concern

Hostile

Raid

For MVP, Peace, Notice, and Concern are active.

7\. Merchant Contracts

Direct contracts with delivery plus safety requirements.

Player delivers specific loot quantities while maintaining survival ratios.

Rewards include reputation gains, reserve relief, and research boosts.

8\. Raids

Raids partially reduce political reputation.

They do not fully reset reputations.

Failure causes structural and progression losses.

9\. System Interfaces

Mana, Loot, and Adventurer systems all interact with reputation.

10\. MVP Constraints

Three numeric reputation values.

Diplomacy abstracted.

Raids disabled but tracked.

System Spec 7 is locked for MVP.
