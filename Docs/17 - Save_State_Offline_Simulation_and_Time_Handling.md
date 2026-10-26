# System Spec 17: Save State, Offline Simulation, and Time Handling

Status: Locked (v1)\
Scope: MVP with forward compatibility

**H1-F2 candidate amendment (2026-10-25; independent review required):** The [contract/status table](planning/h1-f2-time-provenance-paired-cuts.md) records all eight approved H1 directions D1–D6, H1-C01-A and H1-C02-A separately from implementation. Proposed schema 16 has one compound accounted/passive boundary in whole-second legacy mode. Precise paired observations do not activate fractional grants. Existing passive mana, capacity, healing and action barriers remain. Original/residual grants, recovery episodes and overload remain deferred. Contrary older proposed precision/stall wording is superseded only as identified there. H1/H2/O1 and Phase 7 are not complete.

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

## Owner-approved living-dungeon amendment (2026-10-12)

See the [Living Dungeon Owner Design Lock](../docs/planning/decisions/living-dungeon-owner-design-lock.md) and [Cross-Spec Glossary amendment](Cross_Spec_Glossary_of_Invariants_UPDATED.md#living-dungeon-owner-approved-amendments-2026-10-12). For future ordinary adventurer-party simulation, “offline uses summarized approximations,” “Heat is frozen offline,” and “generous but capped progression” are superseded: valid offline periods advance parties and individually eligible ordinary run outcomes using exact event processing and bounded resumable batches. The prohibition on offline raids and permanent losses to the player's dungeon remains in force. No destructive player-dungeon consequence or raid mechanic is authorized offline. Adventurer combat casualties, applicable survivor knowledge and eligible abstract loot outcomes may occur under the approved living-dungeon rules; restricted rewards, research completion and gated actions retain their existing verification requirements. The old model remains historical for existing implemented offline systems; passive mana remains the canonical one-time formula grant and research completion still requires online confirmation.

Automatic arrivals are allowed during valid offline periods. Party outcomes and objectives must be individually classified for offline eligibility; restricted rewards, research completion and gated actions retain verification. Applicable survivor knowledge and abstract loot extraction can result from eligible offline outcomes. No unrestricted settlement helper is authorized.

Draft edits remain possible during incomplete catch-up, with ordered canonical commits that cannot affect an earlier run. Active parties, scheduler and catch-up progress eventually use one versioned canonical save authority; schema 14 is provisional, not a selected schema or field plan. Catch-up resumes from its proven committed prefix. Exact deterministic processing is the reference; sampling/extrapolation is not gameplay authority.
