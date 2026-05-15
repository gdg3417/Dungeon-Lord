**System Spec 35: Error Handling, Player Messaging, and Trust**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Error categories, messaging patterns, localization ready phrasing, trust building |
| Primary goal | Prevent confusion during offline, verification, and integrity edge cases |
| Invariants referenced | INV-01, INV-02, INV-03, INV-08, INV-10 |

# 1. Purpose

Define error handling and player messaging standards. Messaging must be explicit and educational, localized where applicable, and should avoid blocking unrelated gameplay.

# 2. Messaging philosophy

- Messaging is explicit and educational.

- Errors should be localized and should not block the rest of the game when possible.

- Use consistent terms and glossary keys for all messages.

# 3. Standard error categories

- Offline restricted action

- Online verification pending

- Integrity action applied

- Service unavailable

- Version unsupported view only mode

# 4. States that require clear messaging

- Research completion pending

- Offline restricted action

- Cheating flag applied

## 4.1 States that are still messaged but not emphasized

- Forced cloud pull

- Event verification pending

# 5. Localization and tone

- Tone is neutral and system oriented.

- Cheat messaging is a neutral notice, not a warning language escalation.

- Repeated issues do not escalate messaging severity, but do increase backend flag counts.

# 6. UI patterns

- Inline banners for offline restrictions and pending verification.

- Toasts for short acknowledgements.

- Modals only for actions that cannot proceed and need a decision, for example linking an account or updating a blocked client.

# 7. Required message content

## 7.1 Research completion pending

- Explain that research progress continues, but completion needs an online confirmation.

- Provide a call to action to go online.

- Show remaining steps, for example connect, verify, claim.

## 7.2 Offline restricted action

- Explain which action requires online, such as purchases or research start.

- Explain what can be done offline instead.

## 7.3 Integrity action applied

- Explain that offline grants are disabled due to an integrity check.

- Explain how to restore if this is a false positive, for example contact support or use tester override.

# 8. Telemetry hooks

- error_shown (error_type, context, blocking)

- offline_restriction_hit (action_id)

- research_pending_shown (research_id)

- integrity_notice_shown (reason)

# 9. MVP constraints

- Keep messages short in default view and provide details behind a learn more affordance.

- Do not introduce complex escalation logic in MVP.

# 10. Open questions

None.
