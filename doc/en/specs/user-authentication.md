# User and Authentication Spec

## Purpose

This document defines the first-phase user and authentication model for KhaiKang.
The initial release is web-only and uses local user accounts. LDAP, OIDC, SSO, and mobile app authentication are out of scope for v0.1.

## Core Model

The first phase must support:

- local user accounts
- web-only authentication
- global roles
- project roles
- permission strings
- authorization scope through membership or assignment

## Related Docs

- [Documentation Guidelines](../documentation-guidelines.md)
- [Architecture Overview](../architecture/overview.md)
- [Roadmap](../roadmap/)
- [README](../../../README.md)
- [Traditional Chinese README](../../../README.zh-TW.md)

## Scope

This spec covers:

- first-run administrator bootstrap
- local user login
- logout
- authentication ticket expiration
- refresh state expiration
- password policy
- minimal authorization boundaries
- account status handling

## Non-Goals

This phase does not include:

- LDAP integration
- OIDC / SSO integration
- mobile app authentication
- social login providers
- password reset by email
- multi-factor authentication
- delegated admin provisioning workflows

## Flow

### First-Run Administrator Bootstrap

When the system starts for the first time and no administrator exists, it must enter bootstrap mode.

In bootstrap mode:

- the system must create a default administrator account with username `admin`
- the system must generate a strong random initial password
- the password must be displayed to the operator in the first-run setup screen
- the password may also be written to startup logs or an initial setup output location
- the password must not be stored in plaintext after bootstrap completes
- the administrator must be forced to change the initial password after the first successful login

The bootstrap state ends after the first administrator account is created.

### Login Flow

The user logs in with:

- username or account name
- password

The system must:

- verify the local user credentials
- reject suspended or disabled accounts
- issue an authenticated login state only after successful validation

## State and Rules

The system must support at least the following user states:

- `active`
- `suspended`
- `disabled`

The system must support at least the following roles:

- `admin`
- `user`

The initial version may keep authorization simple, but role-based checks must be designed so they can expand later.

### Authorization Model

KhaiKang authorization must distinguish three concerns:

1. Roles
2. Permission strings
3. Authorization scope

#### Global Roles

The first phase must support the following global roles:

- `admin`
- `user`

Where:

- `admin` represents a system administrator
- `user` represents a normal signed-in user

#### Project Roles

Project-level roles may be defined separately, for example:

- `project_admin`
- `project_member`
- `project_viewer`

Project roles are only valid within a specific project and do not grant global administrative access.

#### Permission String Format

Permission strings must use `.` as the separator, for example:

- `user.manage`
- `project.read`
- `project.write`
- `project.manage`
- `testcase.read`
- `testcase.write`

Permission strings describe what a user can do, not where they can do it.

#### Authorization Scope

Authorization scope must be expressed through membership or assignment relationships, not encoded into the permission string.

Examples:

- User A is `project_admin` in Project X
- User A is `project_member` in Project Y
- User B only has access to selected projects

This means the same user can have different roles and different access scopes in different projects.

#### Authorization Decision Order

The system should evaluate authorization in the following order:

1. Whether the user is a global `admin`
2. Whether the user belongs to the target resource
3. The user's role in that resource
4. The permission strings granted by that role

#### Design Rules

- Permission strings represent capabilities, not scope
- Project scope must be represented through membership or assignment
- `admin` and project roles must remain separate concerns
- If workspace scope is added later, it should follow the same pattern

## Security and Audit

### Login Session

The web client uses cookie-based authentication.

Requirements:

- authentication cookies must be `HttpOnly`
- authentication cookies must be `Secure`
- authentication cookies must define an explicit `SameSite=Lax` policy
- the application must not expose the refresh credential to browser JavaScript

The implementation should support a short-lived authentication ticket and a longer-lived renewal mechanism.

### Session and Token Policy

The system must support:

- a short-lived authentication ticket for normal API usage
- server-side revocation of login state
- logout that invalidates the current login state

Recommended policy for v0.1:

- authentication ticket state should expire automatically
- persistent login should only be enabled when the user explicitly chooses a remember-me style option
- server-side refresh state must be revocable

The exact expiration durations are implementation details, but they must be configurable.

### Logout

Logout must:

- clear the browser authentication cookie
- invalidate the server-side refresh state
- prevent the current login state from being reused after logout

### Password Policy

The initial password policy must include:

- minimum password length
- password hashing before storage
- no plaintext password persistence

Optional in v0.1:

- password complexity checks
- forced password change after bootstrap login

### Authorization Boundary

The first phase must support a minimal authorization model:

- `admin` can create and manage users
- `admin` can access system-level settings
- `user` can use normal project features once those modules exist

Authorization for project-level and workspace-level permissions may be expanded later, but the model must not block future granular permission checks.

### Account Lifecycle

The system must support these account actions:

- create user
- update user profile
- suspend user
- disable user
- reactivate user

When a user becomes suspended or disabled:

- existing sessions must be invalidated or denied on the next auth check

### Security Requirements

- Passwords must be hashed using a modern one-way hashing algorithm.
- Authentication cookies must use `HttpOnly`, `Secure`, and explicit `SameSite` settings.
- Sensitive credentials must never be logged in plaintext.
- The login flow must be compatible with browser-based usage.
- CSRF protection must be considered for cookie-authenticated state-changing requests.

### Audit Requirements

The system must write audit entries for:

- user created
- user updated
- password changed
- user suspended
- user disabled
- user reactivated
- login success
- login failure
- logout
- bootstrap administrator creation

## Acceptance Criteria

- A clean installation without any existing administrator enters bootstrap mode.
- The bootstrap page shows an `admin` account and a generated initial password.
- The first administrator can log in successfully.
- The first administrator is forced to change the initial password after login.
- Normal users can log in using local credentials.
- Suspended or disabled users cannot log in.
- Logout invalidates the current authentication state.
- Browser authentication uses `HttpOnly` cookies.

## Open Questions

- Should the initial password be shown only in the web bootstrap page, or also emitted to container logs?
- Should password change on first login be mandatory or configurable?
