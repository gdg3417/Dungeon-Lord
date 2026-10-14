# System Spec 11: Failure, Recovery, and Soft Loss

Status: Locked v1

Scope: MVP plus forward compatible

1\. Purpose of the Failure System

Failure exists to punish reckless or imbalanced play while avoiding hard game over states.

All failure is recoverable given time.

2\. Failure Types in MVP

Mana collapse where reserved mana exceeds generation.

Research collapse due to sustained shortages.

Monster extinction on a floor.

3\. Mana Collapse

Occurs when reserved mana exceeds total capacity or usable mana is zero for a sustained period.

This places the dungeon into a crisis state.

4\. Crisis State Behavior

Systems shut down in priority order:

Research first.

Loot crafting second.

Infrastructure effects third.

Monster spawning last.

No systems are permanently destroyed.

5\. Research Collapse

Research progress may be slowed or partially lost due to sustained mana shortages.

6\. Recovery Philosophy

Recovery is primarily driven by player action with minimal passive assistance.

Reducing dungeon complexity and sacrificing efficiency speeds recovery.

7\. Penalties and Memory

No permanent mechanical penalties exist after recovery.

Failure history is retained for narrative and analytics purposes only.

8\. Player Messaging

The system provides advance warnings and post-failure explanations.

9\. Anti-Abuse Rules

Failure is never optimal.

Intentional collapse provides no benefits.

10\. Failure Conditions

The system fails if collapse is profitable or unrecoverable.

System Spec 11 is locked.

### Owner-approved vacant-floor monster recovery clarification (2026-10-14)

The [Combat and Reserve-Backed Floor Recovery Owner Design Lock](../docs/planning/decisions/combat-and-recovery-owner-design-lock.md) supersedes “Recovery is primarily driven by player action with minimal passive assistance” only for eligible monster recovery on a vacant floor. Recovery proceeds toward a configured readiness threshold subject to affordability, prioritizes functional readiness before optional top-ups in stable order, and leaves unaffordable work pending for automatic retry only while vacant. No recovery or healing occurs while a party occupies the floor. Other failure, collapse, and recovery policies remain unchanged; these approved rules are not yet implemented.
