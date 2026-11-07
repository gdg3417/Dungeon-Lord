# H1-F4 Combat C1 ownership qualification — 2026-10-31

PR #259 remains draft. Tested source: `654c02119acc43c4846f4474d231d5a36c03004a`; main baseline `3eb6ccf93558b8145d64e9e63cbefcbd73484602`.

Both original identities passed: `CombatC1PartyFollowsRoomsCorridorsAndFloors` and `CombatC1TargetFeedbackTracksRecordedHitsAndDefeats`. The complete six-method C1 cohort and two direct clock controls passed **8/0/0 EditMode**, campaign `e77e46ba480d4ff6add230f5c1e52f87`, and **8/0/0 actual PlayMode wrappers**, campaign `ccc0e4dfe3664ea687a45e0ebc92eb8e`. Counts overlap between modes and are not summed as unique tests. Each campaign completed all five supervisor stages and independent verification.

## Cause and correction

The old fixtures altered combat damage, health, branch and floor rules after the canonical owner consumed them. Current schema-17 validation correctly refused subsequent writes. The original test-only coefficients are now authored as complete validated bootstrap bytes before the real root starts. Party traversal uses that original scenario throughout. Feedback scenarios 1 and 2 each receive a fresh disposable save, scene root, immutable inputs and single prebound TimeService via the existing fixture lifecycle. No context rebinding or writable authority was added. The full bootstrap includes CombatC0; frozen serializers were not changed.

The first corrected focus at `96a5da1234a9e64feffeab5795ee84370619c9b1` completed **3/1/0**, campaign `04aba41c4c5c482d9cc3e49592982b3a`, permanently **FAILED**. Feedback reached scenario 1 but setup accepted the preceding loaded save before the new scene's native boot. Its exact first failure was the disposable-save filename assertion, not a production validation relaxation opportunity. Setup now waits at most its existing 120-frame bound for this boot's exact isolated filename. It does not disable the root, alter clocks, bypass recovery or manufacture provenance. That same source passes the ordered C1 class and PlayMode counterparts.

Original recorded-hit, defeat, death, zero-damage, casualty, layout, pooling, room/corridor/trap/loot/two-floor traversal, historical positioning and raw canonical-byte assertions remain. A pending legitimate foreground publication settles before the party replay baseline using the already qualified strict baseline helper.

The historical intermittent missing-label NullReferenceException did not reproduce in the complete ordered C1 cohort. Its cause is not claimed established or fixed. The historical missing-Draft cause likewise remains unproven. No production source, gameplay tuning, recovery semantics, schema/migration, localization or validator changed.

## Preservation and evidence

All 1,494 original input/archive members, exact selected NUnit leaves, pinned tools, complete host/child exits and Job accounting, original settings/TMP, protected owner saves and historical evidence were verified. Independent checks covered 337 prior evidence directories for EditMode and 339 for PlayMode. Reservation is released with previous campaign `ccc0e4dfe3664ea687a45e0ebc92eb8e`. Temporary PlayMode InitTestScene files disappeared through normal runner completion; no manual deletion or normalization was used.

The failed focus had full process completion and unchanged qualified inputs. Existing guarded normal-FAILED finalization restored only the three settings, retained its FAILED verdict and raw evidence, and received a separate independent check. No exceptional recovery, force unlock, termination or owner-save operation occurred.

[Compact exact-name, source, bootstrap, byte and process evidence](h1-f4-combat-c1-qualification-20261031.json). Private raw evidence remains under `TestResults/h1-f4-20261027-ca01`, `ca02`, `ca03` and their exact supervisor campaign directories. Each preparation owns its input archive, plan, runner, isolated product/save and independent receipt; all are retained for review. Root preparation/compile/report adapters are closeout-owned disposable tools retained with this evidence. Compile diagnostics 03/04 launched no Unity and count as zero NUnit tests.

Original-84 coverage is **80 qualified /4 outstanding**, not current full-suite totals. Remaining: both floor-review cases, mouse/touch recovery, and historical visual replay. Full ordered scene/full suites, sustained workload, final interruption/reopen, native Windows runtime/exit and matched performance/memory remain required. Unit clock controls and selected production-scene SIT passed; owner UAT has not begun and is not authorized by this report.
