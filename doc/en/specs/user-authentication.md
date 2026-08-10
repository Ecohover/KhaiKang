# User and Authentication Specification

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [使用者與登入規格](../../zh-TW/02-specs/01-user-authentication.md).

## Purpose and Scope

The MVP uses local web accounts, cookie authentication, server-side login sessions, CSRF protection, global roles, permissions, account administration, and first-run administrator setup.

LDAP, OIDC/SSO, social login, email password reset, MFA, mobile authentication, and delegated provisioning are deferred.

## First-Run Setup

- `GET /api/v1/setup/status` reports whether initialization is required.
- When no administrator exists, `POST /api/v1/setup/initialize` creates the one-time `admin` account and returns a generated initial password once.
- The password is hashed before persistence and is not stored as plaintext.
- The administrator must change the initial password after signing in.
- Repeating initialization returns a conflict.

## Authentication and Sessions

- Login accepts username and password and rejects invalid, suspended, or disabled accounts.
- The server issues an HttpOnly `KhaiKang.Session` authentication cookie and a protected HttpOnly `KhaiKang.Refresh` cookie.
- Cookies use `SameSite=Lax`; secure-cookie enforcement is configurable and defaults to secure outside Development.
- State-changing requests require the paired CSRF cookie/header token.
- Refresh validates the server-side session. Logout revokes it and clears cookies.
- Authentication ticket, session, and remember-me durations are configurable.

## Accounts and Authorization

- Global roles are Admin and User. Admin is system-level; User alone grants no Project or Workspace access.
- Account status values are `active`, `suspended`, and `disabled`.
- Administrators with the relevant permission may list, create, update, suspend, disable, or reactivate local accounts.
- Newly created accounts receive a one-time generated password and must change it after login.
- Project and Test Workspace scope is supplied by active membership and fixed resource roles, not by encoding resource IDs in permission strings.

## Password Rules

- Passwords use one-way hashing and are never persisted or logged in plaintext.
- Minimum length is configurable and defaults to 12.
- Changing a password requires the current password and clears `mustChangePassword` after success.

## Security and Audit

Login success/failure, logout, setup, account lifecycle, and password changes are auditable. Suspended or disabled accounts cannot establish or continue a valid authenticated session. Public failures use the canonical Problem Details contract.

## Acceptance Criteria

- A clean database enters setup mode and can create the first administrator once.
- The initial administrator and newly created users must replace one-time passwords.
- Active users can log in, refresh, query the current user, change password, and log out.
- Suspended or disabled accounts cannot authenticate.
- Cookie and CSRF settings protect browser-based state-changing requests.
