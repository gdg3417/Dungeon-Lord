# Pre-release development save support

Owner-approved on 2026-11-19. Applies before the first public release.

| Save or artifact | Support policy |
| --- | --- |
| Obsolete development save | A development build may refuse it explicitly. Gameplay continuity and migration are optional. Preserve the original on refusal. Initialize a fresh canonical save using an isolated clean path or the existing explicit reset workflow. |
| Current-schema runtime save | Validate and reopen through the canonical authority. Preserve deterministic progression, atomic publication, exact readback, H1 recovery and accepted work. |
| Historical qualification, performance or failure evidence | Preserve sources, saves, logs, reservations and results under the existing evidence policy. A gameplay version refusal grants no deletion, rewrite or campaign recovery authority. |
| Publicly supported player save | Establish supported versions and explicit migration requirements before the first public release and enforce them for player saves. The development exception expires at release. |

For Research schema 18, use native creation. Development saves from schema 17 and earlier may require reset. The legacy debug control could save pending records for an already completed project. Those saves have no gameplay compatibility exception. Current validation rejects simultaneous completed and pending status for the same project. Preserve historical code and fixtures for reproducing earlier behavior. [Research timing contract](../phase-8-research-timing-compatibility-contract.md).
