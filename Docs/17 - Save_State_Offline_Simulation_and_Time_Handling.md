# System Spec 17: Save State, Offline Simulation, and Time Handling

Status: Locked (v1)\
Scope: MVP with forward compatibility

## 1. Time Model

Hybrid time model with event-driven steps. Offline uses summarized approximations.

## 2. Offline Simulation

Offline systems include mana, research, adventurer traffic, and loot crafting. Raids and permanent losses never occur offline.

## 3. Heat Handling

Heat is frozen offline but pressure is applied on login, capped to one heat state.

## 4. Save Rules

Saves occur on fixed intervals and key actions. Save scumming is prevented.

## 5. Anti-Abuse

Protections exist against clock manipulation and offline farming abuse.

## 6. MVP Boundary

Time handling is simulation-light with generous but capped progression.
