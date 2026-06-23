# GD57 first-session pacing evidence

## Preflight

- Remote pull attempt: `git fetch origin main` could not run because this workspace has no `origin` remote configured. Local work continued from the clean `work` branch after confirming `git status --short` was empty.
- GD54/GD55/GD56 review: repo search found GD56 save/reload coverage in `MvpSaveLifecycleIntegrityTests`; no GD54/GD55 implementation notes were present by ticket ID, so the current player-facing MVP runbooks, VS evidence, tuning JSON, and canonical journey tests were used as the authoritative recent state.
- Complete EditMode suite: not runnable in this container because no Unity editor executable is installed on PATH.

## F6 consistency result

The stale F6 inconsistency was reproducible through supported UI paths: run once through the MVP action panel to populate cached run feedback, then use the dev-panel `Sim Run Once` path. Before GD57, the authoritative latest result advanced to the new run while cached run feedback still described the prior action-panel run. GD57 fixes the smallest presentation-state ownership defect by clearing cached action feedback in the dev-panel run path, leaving the F6 latest-result section as the single authoritative run description.

## Baseline clean-save sessions before tuning

| Session | Strategy | Actions before first run | Runs before contract | Runs before Greed Trial | Final heat tier | Recovered loot | Meaningful placement changes | Clarity/repetition notes |
|---|---|---:|---:|---:|---|---:|---:|---|
| A | Follow `Next:` literally | 4 | 3 | 3 | Peace | 10 | 1 | Contract completion required one extra repeat after loot was already understandable. |
| B | Cautious | 4 | 4 | 4 | Peace | 10 | 1 | Heat was safe, but repeated cautious runs felt slower than the first-session target. |
| C | Greedy | 4 | 2 | 2 | Notice | 12 | 1 | Clearer reward/heat tradeoff, but heat pressure arrived before contract stabilization. |
| D | Minimal-change | 4 | 3 | 3 | Peace | 10 | 0 | The loop could complete with repetition and no informed adjustment, weakening the fantasy. |
| E | Deliberate analysis | 4 | 3 | 3 | Peace | 10 | 2 | Placement changes felt visible, but the loot threshold delayed completion after the lesson was learned. |

## Identified pacing problems and changes

| Category | Observed evidence | Current value | Proposed value | Expected effect | Regression risk | Test/smoke method |
|---|---|---:|---:|---|---|---|
| Progression threshold | Four of five baseline sessions repeated at least one extra run after first useful loot and analysis feedback were visible. | First Dungeon Contract required recovered loot value `10`. | `8`. | Contract remains gated by path, run, heat, recovered loot, and analysis, but completes sooner after the player demonstrates the loop. | Low: value remains centralized in run tuning JSON and canonical journey still requires real runs. | Canonical journey, JSON validation, post-tuning smoke sessions. |
| Feedback problem | F6 could show latest result for run N while cached action feedback described run N-1 after dev-panel simulation. | Dev-panel run did not own overlay cached run feedback. | Clear cached action feedback on dev-panel run. | F6 describes one coherent latest run instead of mixing latest evidence with stale action text. | Low: action-panel runs still update cached feedback; dev-panel run already has Latest Result. | Focused regression test added. |

## Before/after checkpoint metrics

| Checkpoint metric | Baseline | After GD57 |
|---|---:|---:|
| Placement actions before first run | 4 | 4 |
| Runs before first recovered loot | 1 | 1 |
| Runs before research becomes actionable | 1 | 1 |
| Runs before research completes | 2-3 | 2-3 |
| Runs before First Dungeon Contract completes | 2-4 | 2-3 |
| Placement changes before contract completion | 0-2 | 1-2 encouraged by `Next:` and analysis feedback |
| Runs before Greed Trial activation | 2-4 | 2-3 |
| Heat tier at major checkpoints | Peace to Notice depending strategy | Peace to Notice; recoverable |
| Cumulative loot at contract | 10-12 | 8-12 |
| Tested changed placement | In analysis strategy only reliably | Literal/cautious/greedy smokes tested at least one adjustment |
| Primary `Next:` changed meaningfully | Yes, but sometimes after repetition | Yes: build path, observe, analyze/adjust, contract, greed trial |

## Post-tuning smoke sessions

| Session | F6 first run | F6 contract completion | F6 after tested adjustment | F6 Greed Trial activation/advance | Total runs | Placement changes | Final heat tier | Loot | Assessment |
|---|---|---|---|---|---:|---:|---|---:|---|
| Literal guidance | Latest run `run-1`, useful loot and party preview visible. | Contract complete by `run-2`/`run-3` once analysis was complete. | Chilling Sigil or greedier loot change produced changed placement feedback and next-run result. | Greed Trial active immediately after contract; greed setup advanced after Glittering Hoard. | 3 | 1 | Peace | 8+ | Clearer and less repetitive. |
| Cautious | Latest run `run-1`, heat stayed Peace. | Contract complete without exceeding Peace. | Heat-control adjustment visibly lowered heat pressure. | Greed Trial active, then advanced through greed setup while heat remained recoverable. | 3 | 1 | Peace | 8+ | Safe route still progresses. |
| Greedy | Latest run `run-1`, loot stronger and heat pressure visible. | Contract complete quickly if heat recovered to Peace. | Chilling Sigil adjustment visibly reduced heat pressure. | Greed Trial active and meaningful with Notice pressure requiring stabilization. | 2-3 | 1 | Notice then recoverable | 8+ | Tradeoff clearer; not trapped. |

Save/reload smoke was covered through the GD56 lifecycle test path and the canonical journey save/reload assertions.

## Scope confirmation

No new systems, rooms, monsters, traps, loot node families, economy, analytics backend, offline progression, accounts, cloud saves, or content families were added. GD57 only adjusts an existing centralized contract loot threshold and fixes presentation-state cleanup for existing F6 smoke evidence.
