# Authentication Implementation Notes

## Overview

Swate Electron authenticates users to GitLab-compatible DataHub instances via Personal Access Token (PAT). Authentication is verified server-side (Main process), credentials are encrypted at rest via Electron safe storage, and the token is injected into Git operations through the existing `GitTokenProvider` interface.

## Architecture

```
Renderer                    Main Process
────────                    ────────────
Navbar.fs UserAvatar        IPC/IAuthApi.fs
  │  signIn(baseUrl, PAT)    │
  │ ─────────────────────► Auth/AuthService.fs
  │                           │  verifyToken → GitLab /api/v4/user
  │                           │  persist → Auth/SecureAuthStore.fs
  │                           │  wire → Git/GitTokenProvider.fs
  │ ◄─────────────────────   │  return AuthResult
  │  getAuthState()           │
  │ ─────────────────────►    │  read in-memory state
  │  signOut()                │
  │ ─────────────────────►    │  clear memory + storage + reset provider
```

## Where auth is stored

| Data | Location | Format |
|------|----------|--------|
| Encrypted PAT | `{userData}/Settings/Auth/auth-credentials.enc` | Base64-encoded `safeStorage.encryptString` output |
| User metadata | `{userData}/Settings/Auth/auth-meta.json` | Plaintext JSON (name, email, avatar URL, DataHub URL) |

- `{userData}` = `app.getPath("userData")` (e.g. `C:\Users\<user>\AppData\Roaming\Swate`)
- The PAT never appears in plaintext on disk. It is encrypted by Electron's `safeStorage` API which uses OS-level credential stores (DPAPI on Windows, Keychain on macOS, libsecret/kwallet on Linux).
- If `safeStorage.isEncryptionAvailable()` is false at runtime, sign-in still works for the session but credentials are not persisted to disk.

## Token-provider integration

On successful sign-in (or when restoring from storage on startup), `AuthService` calls:

```fsharp
Git.GitTokenProvider.setTokenProvider {
    TryGetAccessToken = fun host -> promise {
        return AuthService.tryGetTokenForHost host
    }
}
```

`tryGetTokenForHost` compares the requested `host` against the authenticated DataHub's host. If they match, it returns the PAT. Otherwise it returns `None`, and Git operations for that host will fail with `Unauthorized` as documented in the Git Functionality Guide.

On sign-out, the provider is reset to the default (always returns `None`).

## Expired token recovery / logout behavior

1. **At sign-in time**: Main calls `GET /api/v4/user` with the PAT. If GitLab returns 401/403, sign-in fails immediately with a typed `AuthFailureKind` and no data is persisted.

2. **During session (revalidate)**: Renderer can call `revalidate()` at any time. If the token is no longer valid, `AuthService` automatically signs the user out (clears memory + storage + provider) and returns the failure.

3. **During Git operations**: If a fetch/pull/push fails with `Unauthorized` at the git-transport level, the renderer should prompt the user to sign in again. The auth state does not auto-clear on git-level auth failures — the user must explicitly sign out or sign in again with a new token.

4. **Logout**: Calling `signOut()` removes:
   - In-memory auth state (immediate)
   - Encrypted PAT file from disk
   - Metadata JSON file from disk
   - Resets `GitTokenProvider` to default (no token)

## IPC contract

Defined in `Swate.Electron.Shared/IPCTypes.fs` as `IAuthApi`:

- `signIn: AuthSignInRequest -> Promise<Result<AuthResult, exn>>`
- `getAuthState: unit -> Promise<Result<AuthStateDto, exn>>`
- `signOut: unit -> Promise<Result<unit, exn>>`
- `revalidate: unit -> Promise<Result<AuthResult, exn>>`

Note: Unlike `IGitApi` and `IArcVaultsApi`, auth endpoints do not take `IpcMainEvent` as the first parameter because they are not window-scoped operations.

## Error categories

`AuthFailureKind` in `Swate.Electron.Shared/AuthTypes.fs`:

| Kind | Meaning |
|------|---------|
| `Unauthorized` | 401 — token invalid or expired |
| `Forbidden` | 403 — token lacks required scopes |
| `Network` | DNS/connection failure reaching the DataHub |
| `EndpointInvalid` | DataHub URL is malformed or returns 404 |
| `StorageUnavailable` | Electron safe storage not available (non-fatal for session) |
| `Unknown` | Unexpected error |
