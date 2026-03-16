# Authentication Implementation Notes

## Overview

Swate Electron authenticates users to GitLab-compatible DataHub instances via Personal Access Token (PAT).

- Authentication is verified in the Main process (`GET /api/v4/user`).
- Credentials are encrypted at rest using Electron `safeStorage`.
- Multiple accounts are supported concurrently.
- Git access tokens are supplied through `GitTokenProvider`.
- Account-list changes are broadcast to all open windows.

## Architecture

```
Renderer                    Main Process
────────                    ────────────
Navbar.fs UserAvatar        IPC/IAuthApi.fs
  │ signIn(baseUrl, PAT)      │
  │ setActiveAccount(id)      │
  │ removeAccount(id)         │
  │ getAuthState()            │
  │─────────────────────────► │ Auth/AuthService.fs
  │                           │  verifyToken → GitLab /api/v4/user
  │                           │  persist/load → Auth/SecureAuthStore.fs
  │                           │  update token provider
  │ ◄───────────────────────  │ return AuthResult/AuthStateDto
  │                           │
  │ ◄───────────────────────  │ IMainUpdateRendererApi.authAccountsUpdate(accounts)
  │                           │ (broadcast on account-list changes)
```

## Storage model

All auth storage lives under:

- `{userData}/Settings/Auth`

Where `{userData}` is `app.getPath("userData")`.

### Files

| Data | Location | Format |
|------|----------|--------|
| Encrypted PAT per account | `{userData}/Settings/Auth/{accountId}-credentials.enc` | Base64 of `safeStorage.encryptString(token)` |
| Metadata per account | `{userData}/Settings/Auth/{accountId}-meta.json` | Plain JSON (`accountId`, `name`, `email`, `avatarUrl`, `targetDataHub`) |
| Active account marker | `{userData}/Settings/Auth/active-account.json` | JSON containing `accountId` |

- PATs are never written in plaintext.
- `safeStorage` uses the OS credential backend (DPAPI/Keychain/libsecret or similar).
- If `safeStorage.isEncryptionAvailable()` is `false`, sign-in fails with `AuthFailureKind.StorageUnavailable`.

## Token-provider integration

`AuthService` sets `GitTokenProvider` to call `tryGetTokenForHost`.

Selection policy:

1. If active account host matches requested host, use active account token.
2. Otherwise search all accounts for matching host.
3. If no match, return `None`.

Provider is refreshed on:

- restore from storage
- sign-in
- sign-out
- set active account
- remove account

If no accounts remain, provider behavior effectively becomes no-token (`None` for all hosts).

## Active account and account list behavior

- `AuthService` keeps in-memory state:
  - `accounts: Map<accountId, (user, token)>`
  - `activeAccountId: string option`
- Active account is persisted in `active-account.json` and restored on startup.
- If the persisted active account is missing, first available account is selected.

### Cross-window synchronization

`Main.IPC.AuthApi` broadcasts `authAccountsUpdate` (one-way Main -> Renderer) with the latest `AuthAccountSummary[]` on account-list mutations:

- successful `signIn`
- `signOut`
- `revalidate`
- `removeAccount`

`setActiveAccount` does not broadcast `authAccountsUpdate`.

Renderer (`Navbar.fs`) listens for `authAccountsUpdate` and updates only the local account-list UI state from that push.

## Expired token recovery / logout behavior

1. **At sign-in**
  Main verifies `GET /api/v4/user` using PAT.
  - 401 -> `Unauthorized`
  - 403 -> `Forbidden`
  - 404 -> `EndpointInvalid`
  - other non-OK -> `Unknown`
  - network error -> `Network`

2. **During session (`revalidate`)**
  Revalidates the active account token.
  If invalid, only that account is removed.

3. **During Git operations**
  If Git fails with auth errors, the renderer should guide the user to re-authenticate.

4. **Sign-out**
  `signOut()` removes the currently active account (memory + disk), then selects another account if available.

## IPC contract

Defined in `Swate.Electron.Shared/IPCTypes.fs` as `IAuthApi`:

- `signIn: AuthSignInRequest -> Promise<Result<AuthResult, exn>>`
- `getAuthState: unit -> Promise<Result<AuthStateDto, exn>>`
- `signOut: unit -> Promise<Result<unit, exn>>`
- `revalidate: unit -> Promise<Result<AuthResult, exn>>`
- `listAccounts: unit -> Promise<Result<AuthAccountSummary array, exn>>`
- `setActiveAccount: string -> Promise<Result<AuthStateDto, exn>>`
- `removeAccount: string -> Promise<Result<unit, exn>>`

And in `IMainUpdateRendererApi`:

- `authAccountsUpdate: AuthAccountSummary[] -> unit`

Note: `IAuthApi` endpoints do not take `IpcMainEvent` because auth state is managed centrally in Main.

## Error categories

`AuthFailureKind` in `Swate.Electron.Shared/AuthTypes.fs`:

| Kind | Meaning |
|------|---------|
| `Unauthorized` | 401 — token invalid or expired |
| `Forbidden` | 403 — token lacks required scopes |
| `Network` | DNS/connection failure reaching the DataHub |
| `EndpointInvalid` | DataHub URL is malformed or returns 404 |
| `StorageUnavailable` | Electron safe storage is not available |
| `Unknown` | Unexpected error |
