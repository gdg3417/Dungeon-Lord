**System Spec 34: Backend Services and API Contract**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Managed services, required endpoints, retry rules, offline rules, admin tooling |
| Primary goal | Support save integrity, security, and data driven updates with minimal custom backend code |
| Invariants referenced | INV-01, INV-04, INV-05, INV-08, INV-10 |

# 1. Purpose

Define the backend services and API behaviors required by the game. The system should prefer managed services and only build custom endpoints where needed for security and game specific rules.

# 2. MVP required services

- Authentication

- Telemetry ingestion

- Admin dashboard

- Event configuration delivery (minimal, can return no active events)

- Cloud saves when feasible, otherwise local saves with account upgrade path

## 2.1 Not required at MVP launch

- Leaderboards are not required at MVP launch, but interfaces should be stubbed for future use.

# 3. Managed first approach

- Prefer managed services for authentication, storage, and telemetry.

- Custom services should be limited to: save validation rules, research verification, event state timing, and admin controls.

# 4. API behavior

## 4.1 Reliability

- All server calls must be strictly acknowledged with retries.

- Use idempotency keys for write operations, including purchases and save uploads.

- Retries must not duplicate purchases or premium spends.

## 4.2 Timeouts

- Client should fail gracefully after 3 seconds for non critical calls.

- Critical calls may show a blocking spinner with a clear cancel option.

# 5. Offline tolerance

The following endpoints must hard fail while offline:

- Purchases

- Research start

- Research completion

- Event verification

Offline actions that require these endpoints must show explicit educational messaging.

# 6. Event configuration delivery

- The client fetches active event and season configuration after authentication when online.

- If offline, the client uses the last verified configuration and marks it as unverified.

- Event state changes apply only after online verification.

# 7. Leaderboard submissions (future behavior)

- If leaderboard submission occurs while offline, queue locally.

- Submit on next verified online session.

- Discard queued submissions that are stale past a configured window.

# 8. Admin tooling

- Admin UI supports reviewing cheat flags, granting restore overrides, managing test save slots, and editing live event data.

- All admin actions must be logged for audit purposes.

# 9. Telemetry hooks

- api_call (endpoint, duration_ms, success, retry_count)

- api_fail_graceful (endpoint, reason)

- event_config_fetched (verified, config_version)

- admin_action_logged (action_type, actor_id)

# 10. MVP constraints

- Keep custom backend code minimal.

- Security first for premium currency and time based verification.

- No leaderboards required at MVP launch.

# 11. Open questions

None.
