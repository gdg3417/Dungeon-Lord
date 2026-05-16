# System Spec 24: Social and Competitive Systems

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose

This specification defines leaderboards and future competitive play features, including data normalization, reset policies, visibility rules, and anti cheat considerations.

2\. MVP and Near Term Stance

Leaderboards are the primary social feature. Other social systems are deferred, but the architecture should not block future expansion.

Competition surfaces are always visible, even if participation is optional or gated.

3\. Leaderboard Types and Scopes

3.1 Scopes

Leaderboards support global, regional, and friends scopes.

3.2 Primary versus season

Primary dungeon leaderboards are persistent and do not reset. Season dungeon leaderboards reset each season.

3.3 Separate sets

Primary dungeon leaderboards and season dungeon leaderboards are separate.

4\. Leaderboard Metrics

Supported metrics include: mana per hour, raid survival rate, most floors, highest danger rating, highest safety rating, dungeon rating, and most survivors.

Mana per hour is measured as a rolling 24 hour average to reduce cheating and reduce spike based exploits.

5\. Filters and Bracketing

Leaderboards support filtering by dungeon core level, by account age, and by overall ranking.

Bracketing is available to improve fairness across progression tiers.

6\. Submission and Validation

6.1 Online requirement

Leaderboard submission requires online connectivity.

6.2 Data submitted

Submitted data includes: player id, content version, build version, relevant metric value, and a compact summary of supporting state such as dungeon core level and floor count.

6.3 Server side validation

The server validates submissions using plausibility checks, time windows, and consistency with recent telemetry, especially for rolling 24 hour computations.

7\. Seasonal Competitive Loop

Season dungeons create an equal baseline competition where players start from the same initial state. Seasons can also serve as a live beta test platform for new content.

8\. Future Feature: Dungeon Versus Dungeon Challenge

8.1 High level concept

Two players challenge each other in synchronous play. Each player chooses how many monsters to send as attackers and how many to keep as defenders.

8.2 Win condition

The winner is determined by who damages or destroys the opposing dungeon core more effectively within the match rules.

8.3 Anti meta lever

Sending too many attackers weakens defenses. A smaller elite attacking force can defeat a weakened defense, discouraging all in attack metas.

8.4 Rewards

Rewards can include leaderboard rank, dungeon rating, and limited loot or research opportunities, such as unlocking access to items or monsters the player does not yet have.

9\. Gating and Progression

Competitive features should be gated behind progression milestones and should not destabilize the primary dungeon economy.
