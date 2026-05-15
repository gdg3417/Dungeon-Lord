**System Spec 33: Build, Release, and Environment Strategy**

*Dungeon Builder, locked design specification*

| Status | Locked |
|----|----|
| Scope | Build environments, release cadence, feature flags, version support policy, emergency response |
| Primary goal | Enable fast iteration and safe live control without contradicting offline and security rules |
| Invariants referenced | INV-01, INV-04, INV-05, INV-08, INV-09 |

# 1. Purpose

Define the environments and release practices used to ship safely. This includes feature flags, data hotfixes, version support rules, and emergency response patterns.

# 2. Environments

- Dev environment: for daily development and local testing.

- Prod environment: for public releases.

- Test environment is strongly preferred and should be fully separate from Prod when available.

## 2.1 Data isolation rules

- Dev and Test data must never mix with Prod data.

- Admin actions and cheat reviews in Test must not affect Prod accounts.

- Prod uses strict access controls for admin tooling.

# 3. Feature flags

Feature flags allow enabling or disabling features without a new client build.

## 3.1 Flag coverage

- Seasons

- World events

- Monetization

- Experimental balance tables

## 3.2 Flag authority

- Server driven flags are required for anything that impacts economy, competition, or purchases.

- Local flags are allowed only in Dev builds for debugging and iteration.

# 4. Release cadence

- MVP updates target a weekly cadence.

- Balance and configuration hotfixes should be possible via data tables without requiring a client update.

# 5. Version support policy

- A client build may remain active for 7 days after a newer build is released.

- After the support window, the client becomes view only.

- View only mode allows inspection of the dungeon state but prevents actions that would change state.

## 5.1 View only scope

- Allow viewing layouts, inventories, research status, and logs.

- Block simulation altering actions, including layout edits and any economy actions.

- Block all online required actions by definition.

# 6. Emergency response

- If an exploit is discovered, the preferred response is to stop future gains.

- Do not roll back prior gains in MVP unless required for integrity.

- Exploit response should be implemented as clamps or validation on the server side where possible.

# 7. Telemetry hooks

- build_version_seen (version, environment)

- feature_flags_applied (flag_hash, environment)

- client_blocked (version, reason, mode)

- hotfix_applied (table_id, version)

# 8. MVP constraints

- Weekly release cadence is a target, not a promise.

- Feature flags and table hotfixes must not break offline play rules.

- View only mode must clearly explain why actions are blocked.

# 9. Open questions

None.
