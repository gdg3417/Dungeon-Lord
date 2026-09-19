# Phase 5A automated qualification

## Provenance rule

A result is authoritative only when no relevant production, test, or configuration source changed after it. Results from the interrupted run that were followed by source edits are recorded as superseded or failed, not reused as final qualification.

## Interrupted-run reconstruction

- Initial continuation HEAD: `ad026a29b1f8020a7ab8c682ac3c98da1ebf341c`.
- Phase 5A work was entirely uncommitted on `codex/phase5a-durable-optional-branch-state`.
- Earlier full EditMode attempts progressed from 414/946 to 479/946 to 902/946 to 940/946. Those invocations were superseded by later corrections; the last had six genuine failures that were corrected before continuation.
- A subsequent invocation was interrupted at compile time by a sealed-fixture inheritance error; the fixture was corrected before continuation.
- Known unrelated modified files were preserved: `ProjectSettings/UnityConnectSettings.asset` and `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`.

## Current focused results

| Suite | Result | Evidence |
|---|---:|---|
| Phase 5A durable branch fixture | 6/6 passed | `TestResults/phase5a-focused-current.xml` |
| Atomic canonical writes | 59/59 passed | `TestResults/phase5a-writes-current.xml` |
| Strict complete-save contract | 10/10 passed | `TestResults/phase5a-complete-save-current.xml` |
| GameRoot/bootstrap structural integration | 28/28 passed | `TestResults/phase5a-bootstrap-current.xml` |
| Structural economy | 70/70 passed | `TestResults/phase5a-economy-current.xml` |
| Content acquisition economy | 58/58 passed | `TestResults/phase5a-acquisition-current.xml` |
| Returned-content redeployment | 23/23 passed | `TestResults/phase5a-redeployment-current.xml` |
| Direct content unassignment | 31/31 passed | `TestResults/phase5a-unassignment-current.xml` |
| Canonical load/migration coordinator | 52/52 passed | `TestResults/phase5a-load-current.xml` |
| Save workload accounting | 14/14 passed | `TestResults/phase5a-focused-workload.xml` |
| Structural/spatial validation | 76/76 passed | `TestResults/phase5a-focused-spatial-rerun.xml` |

The Phase 5A fixture covers schema 9 → 10, research allowance resolution, construction cost and identity, corridor acquisition/custody/redeployment/unassignment, deletion blocking, reopen, one-tile occupancy conflict, topology fingerprint applicability, and unchanged required-route outcomes with branch content present.

## Final qualification

Final full EditMode, full PlayMode, and Windows x86_64 Development Build results will be appended after the implementation source is frozen. Manual UAT is tracked separately and remains outstanding.
