# Owner interaction qualification after independent review

Do not use the protected C0 warm save, an existing C2 owner-progress namespace, or a previous binary to qualify these changes. No new Windows player was built for this PR. No new Unity project is needed.

## Isolated setup

The archive [owner-uat-samples.zip](owner-uat-samples.zip) contains four synthetic schema-15 saves exported through the actual complete-save authority by passing PlayMode tests. They contain authored Floors 1–2 only, configured content and completed test research; they contain no owner progress. Local originals are also in `C:/Dev/Dungeon-Lord/TestResults/phase7-floor-lifecycle-uat/`.

With Editor and players closed, extract the four files. Copy each to a **new, unused** save filename in `%USERPROFILE%/AppData/LocalLow/gdg3417/Dungeon Lord/`; stop if the destination already exists rather than reseeding progress. Set only `save.fileName` in the existing Bootstrap build configuration to the chosen filename before pressing Play. Retain the original value and restore it after qualification. Never run an Editor and player against the same namespace. The approved standalone workflow must pack its own isolated namespace; inspect the packed filename before launch.

| Sample | Starting presentation |
| --- | --- |
| `phase7-floor-ready-uat.json` | One Active floor, completed required permit, 1,000 total mana; shell construction is eligible. |
| `phase7-floor-insufficient-uat.json` | Same one-floor scenario with zero mana; construction disabled with the actual mana reason. Passive funding may subsequently change affordability. |
| `phase7-floor-shell-uat.json` | Two floors; Floor 2 is an empty Inactive shell with blocked activation. |
| `phase7-floor-lifecycle-uat.json` | Two floors; Floor 2 has a valid route and is Inactive/eligible to activate. |

These are starting conditions, not new tuning. All prices, reserves and thresholds continue to come from production configuration. Keep any subsequent manual progress; use another unused filename when a pristine sample is required.

## Sequential checks

1. Open `C:/Dev/Dungeon-Lord` in the existing Unity Editor. Play the production Bootstrap/Dungeon scene using the ready sample. Find **Manage floors** in the Floor Summary. Confirm the selected floor/status, next authored opportunity, shell price and permit status are understandable without opening development tools.
2. Stop and select the insufficient sample's unused namespace. Open Manage floors. Check the nearby localized blocker is readable, the construction control looks disabled, and clicking/tapping it has no visible effect. State/debit atomicity is already automated; this check concerns presentation and interaction.
3. Return to the ready sample namespace. Click **Construct Floor 2 Shell** once. Confirm Floor 2 appears immediately in the selector, the wallet/HUD updates, the selected Floor 1 stays selected and no invented next floor appears. Select Floor 2; inspect its Inactive status and route blocker. Its visible Activate control must remain disabled.
4. For focused lifecycle checks, stop and select the lifecycle sample namespace. Select Floor 2 in production UI, leaving Bootstrap's diagnostic selection alone. Activate it. The screen should retain Floor 2, show Active, replace the contextual action and update mana/hour in place. No reopen is needed to see these changes. Exact target isolation is already regression-tested.
5. Deactivate Floor 2. It is the deepest Active floor, so it should complete directly without a confirmation, retain selection and update status/mana/hour. Floor 1 remains Active. Select Floor 1 and read its disabled-deactivation explanation.
6. Cascade confirmation/cancellation: current authored content cannot produce a committable cascade (Floor 1 must remain Active; Floor 2 is deepest). Do **not** add Floor 3 or change authority rules to force this test. Review the synthetic suffix/confirmation/cancel tests and localized consequence text instead. Once future authored content supports a cascade, the owner interaction check is: open shallower deactivation, read the listed actual affected floors/consequences, cancel and verify no change, then reopen and confirm. That future-content gameplay qualification is not claimed here.
7. Activate Floor 2 again, stop Play, then reopen the same namespace. Confirm both floor states and the appropriate actions are present. UI selection is transient and may initially return to Floor 1 after a fresh launch; within-session lifecycle actions retain selection. Save compatibility and canonical identity/order are already automated.
8. At desktop 1920×1080 and portrait 720×1280 (plus tablet if available), check Small/Default/Large text. Open/collapse management/details, scroll to requirements/actions, switch floors, and interact with controls using real mouse/touch. Confirm disabled text and blockers remain readable, buttons remain reachable, and the map still has usable space. Other locales require translating the nine new keys through the existing string table; there is no code-only English fallback.
9. When an updated Windows Development player is produced through the approved existing build workflow/output location, repeat only steps 1, 3–5, 7 and a brief desktop interaction pass using separate unused player namespaces. Existing C0/C1/C2 binaries do not include this PR. No standalone parity or owner acceptance is claimed until this step is complete.
10. Close the Editor/player and restore only the original build-config filename. Retain the new UAT saves and notes; no workspace/save cleanup is part of this task. Report interaction/readability problems with the actual build/commit and save namespace.

Automation covers rejection atomicity, duplicates, stale/missing selections/previews, exact Floor 2 targeting while Bootstrap selects Floor 1, ordered consequences, cancel-without-mutation, save reopen, schema 15, localization/UXML references and supported-size action bounds. The owner should focus on player-facing integration rather than repeating fault injection or inspecting canonical JSON.
