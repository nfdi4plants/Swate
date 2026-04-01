module Main.Auth.AuthService

open System
open Fable.Core
open Swate.Electron.Shared.AuthTypes
open Swate.Components.Authentication.Types

// ── Types ────────────────────────────────────────────────────────────

type internal AuthFailure = GitLabApi.GitLabAuthFailure

type private AccountState = {
    Summary: AccountSummary
    Token: string
}

/// Normalize and validate a GitLab base URL. HTTPS only.
let private normalizeBaseUrl (baseUrl: string) : Result<string, AuthFailure> =
    let trimmed = baseUrl.Trim().TrimEnd('/')

    if String.IsNullOrWhiteSpace trimmed then
        Error {
            Kind = AuthFailureKind.EndpointInvalid
            Message = "DataHub URL is empty."
        }
    else
        let mutable uri = Unchecked.defaultof<Uri>

        if Uri.TryCreate(trimmed, UriKind.Absolute, &uri) && uri.Scheme = "https" then
            Ok trimmed
        else
            Error {
                Kind = AuthFailureKind.EndpointInvalid
                Message = "DataHub URL must be a valid HTTPS URL."
            }


// ── Mutable in-memory state (multi-account) ──────────────────────────
//
// Token provider policy:
//   1. Check active account first — if its host matches, use its token.
//   2. If no match, search all accounts for a host match.
//   3. If still no match, return None.

/// Map of accountId → account state (summary + PAT).
let mutable private accounts: Map<string, AccountState> = Map.empty

/// Currently active account ID.
let mutable private activeAccountId: string option = None

let private getActiveAccountState () =
    activeAccountId |> Option.bind (fun id -> accounts |> Map.tryFind id)

let private persistActiveSelection () =
    SecureAuthStore.setActiveAccountId activeAccountId

let private reconcileActiveAccountInvariant () =
    let nextActive =
        if accounts.IsEmpty then
            None
        else
            match activeAccountId with
            | Some id when accounts |> Map.containsKey id -> Some id
            | _ -> accounts |> Map.tryPick (fun id _ -> Some id)

    if activeAccountId <> nextActive then
        activeAccountId <- nextActive
        persistActiveSelection ()

let private toMetadata (accountId: string) (summary: AccountSummary) : SecureAuthStore.AuthMetadata = {
    AccountId = accountId
    Name = summary.User.Name
    Email = summary.User.Email
    AvatarUrl = summary.User.AvatarUrl
    TargetDataHub = summary.User.TargetDataHub
    DateAdded = summary.DateAdded
    TokenInvalid = summary.TokenInvalid
}

let private persistAccountState (accountId: string) (accountState: AccountState) : unit =
    let storeResult =
        SecureAuthStore.store {
            Metadata = toMetadata accountId accountState.Summary
            Token = accountState.Token
        }

    match storeResult with
    | Error msg -> Browser.Dom.console.warn $"Auth storage warning: {msg}"
    | Ok() -> ()

let private getAuthStateDto () : AuthStateDto = {
    ActiveAccount = getActiveAccountState () |> Option.map _.Summary
    StoredAccounts = accounts |> Map.toArray |> Array.map (fun (_, v) -> v.Summary)
}

/// Try to get token for a given host (used by GitTokenProvider).
/// Policy: active account first, then any account matching the host.
let tryGetTokenForHost (host: string) : string option =
    // 1. Check active account
    match getActiveAccountState () with
    | Some accountState when
        String.Equals(
            SecureAuthStore.extractHost accountState.Summary.User.TargetDataHub,
            host,
            StringComparison.OrdinalIgnoreCase
        )
        ->
        Some accountState.Token
    | _ ->
        // 2. Search all accounts
        accounts
        |> Map.tryPick (fun _ accountState ->
            if
                String.Equals(
                    SecureAuthStore.extractHost accountState.Summary.User.TargetDataHub,
                    host,
                    StringComparison.OrdinalIgnoreCase
                )
            then
                Some accountState.Token
            else
                None
        )

let private refreshTokenProvider () =
    Main.Git.GitTokenProvider.setTokenProvider {
        TryGetAccessToken = fun host -> promise { return tryGetTokenForHost host }
    }

/// Get the current in-memory auth state for the active account.
let getState () : AuthStateDto =
    reconcileActiveAccountInvariant ()
    getAuthStateDto ()

/// List all stored account summaries.
let listAccounts () : AccountSummary array = (getState ()).StoredAccounts

/// Main-process only helper to read the active account and token.
let tryGetActiveAccountWithToken () : (AuthUserDto * string) option =
    getActiveAccountState ()
    |> Option.map (fun accountState -> accountState.Summary.User, accountState.Token)

/// Main-process helper to resolve a DataHub endpoint for unauthenticated/public browsing.
let tryGetPreferredDataHub () : string option =
    match getActiveAccountState () with
    | Some accountState -> Some accountState.Summary.User.TargetDataHub
    | None ->
        accounts
        |> Map.tryPick (fun _ accountState ->
            if String.IsNullOrWhiteSpace accountState.Summary.User.TargetDataHub then
                None
            else
                Some accountState.Summary.User.TargetDataHub
        )

// ── Public operations ────────────────────────────────────────────────

let private toAuthResult (result: Result<AuthStateDto, AuthFailure>) : AuthResult =
    match result with
    | Ok user -> {
        Success = true
        User = Some user
        FailureKind = None
        Message = None
      }
    | Error failure -> {
        Success = false
        User = None
        FailureKind = Some failure.Kind
        Message = Some failure.Message
      }

/// Sign in: validate, verify with GitLab, persist, set as active, update token provider.
let signIn (request: AuthSignInRequest) : JS.Promise<AuthResult> = promise {
    if not (SecureAuthStore.isAvailable ()) then
        return
            toAuthResult (
                Error {
                    Kind = AuthFailureKind.StorageUnavailable
                    Message = "Electron safe storage is not available on this system."
                }
            )
    else
        match normalizeBaseUrl request.GitLabBaseUrl with
        | Error failure -> return toAuthResult (Error failure)
        | Ok baseUrl ->
            let! verifyResult = GitLabApi.verifyToken baseUrl request.PersonalAccessToken

            match verifyResult with
            | Error failure -> return toAuthResult (Error failure)
            | Ok user ->
                let dateAdded =
                    accounts
                    |> Map.tryFind user.AccountId
                    |> Option.map (fun accountState -> accountState.Summary.DateAdded)
                    |> Option.defaultValue (DateTime.UtcNow.ToString())

                let accountState = {
                    Summary = {
                        User = user
                        DateAdded = dateAdded
                        TokenInvalid = false
                    }
                    Token = request.PersonalAccessToken
                }

                persistAccountState user.AccountId accountState

                accounts <- accounts |> Map.add user.AccountId accountState
                activeAccountId <- Some user.AccountId
                persistActiveSelection ()
                refreshTokenProvider ()
                let authStateDto = getState ()
                return toAuthResult (Ok authStateDto)
}

/// Sign out: remove active account from memory and disk, pick next or clear.
let signOut () : unit =
    match activeAccountId with
    | Some id ->
        SecureAuthStore.remove id
        accounts <- accounts |> Map.remove id
        reconcileActiveAccountInvariant ()
        refreshTokenProvider ()
    | None -> ()

/// Set a different account as active.
let setActiveAccount (accountId: string) : AuthStateDto =
    match accounts |> Map.tryFind accountId with
    | Some _ ->
        activeAccountId <- Some accountId
        persistActiveSelection ()
        refreshTokenProvider ()
    | None -> ()

    getState ()

/// Remove a specific account (by ID). If it was active, switch to next or clear.
let removeAccount (accountId: string) : unit =
    SecureAuthStore.remove accountId
    accounts <- accounts |> Map.remove accountId

    reconcileActiveAccountInvariant ()
    refreshTokenProvider ()

/// Revalidate all stored accounts and mark invalid tokens without removing accounts.
let revalidate () : JS.Promise<AuthResult> = promise {
    if accounts.IsEmpty then
        let authStateDto = getState ()

        return {
            Success = false
            User = Some authStateDto
            FailureKind = Some AuthFailureKind.Unauthorized
            Message = Some "No stored accounts."
        }
    else
        let mutable nextAccounts = accounts
        let mutable firstFailure: AuthFailure option = None

        for accountId, accountState in accounts |> Map.toArray do
            let currentUser = accountState.Summary.User
            let! verifyResult = GitLabApi.verifyToken currentUser.TargetDataHub accountState.Token

            match verifyResult with
            | Ok verifiedUser ->
                // Keep the persisted account id stable to avoid file renames on profile changes.
                let stableUser = {
                    verifiedUser with
                        AccountId = accountId
                }

                let updatedAccountState = {
                    accountState with
                        Summary = {
                            accountState.Summary with
                                User = stableUser
                                TokenInvalid = false
                        }
                }

                nextAccounts <- nextAccounts |> Map.add accountId updatedAccountState
                persistAccountState accountId updatedAccountState

            | Error failure ->
                if firstFailure.IsNone then
                    firstFailure <- Some failure

                let updatedAccountState = {
                    accountState with
                        Summary = {
                            accountState.Summary with
                                TokenInvalid = true
                        }
                }

                nextAccounts <- nextAccounts |> Map.add accountId updatedAccountState
                persistAccountState accountId updatedAccountState

        accounts <- nextAccounts
        reconcileActiveAccountInvariant ()
        refreshTokenProvider ()

        let authStateDto = getState ()

        match firstFailure with
        | Some failure ->
            return {
                Success = false
                User = Some authStateDto
                FailureKind = Some failure.Kind
                Message = Some failure.Message
            }
        | None ->
            return {
                Success = true
                User = Some authStateDto
                FailureKind = None
                Message = None
            }
}

/// Restore all accounts from persisted secure storage on app startup.
let tryRestoreFromStorage () : unit =
    let stored = SecureAuthStore.loadAll ()

    for credential in stored do
        let user: AuthUserDto = {
            AccountId = credential.Metadata.AccountId
            Name = credential.Metadata.Name
            Email = credential.Metadata.Email
            AvatarUrl = credential.Metadata.AvatarUrl
            TargetDataHub = credential.Metadata.TargetDataHub
        }

        let accountState = {
            Summary = {
                User = user
                DateAdded = credential.Metadata.DateAdded
                TokenInvalid = credential.Metadata.TokenInvalid
            }
            Token = credential.Token
        }

        accounts <- accounts |> Map.add user.AccountId accountState

    // Restore persisted active account selection
    activeAccountId <- SecureAuthStore.getActiveAccountId ()

    reconcileActiveAccountInvariant ()

    if not accounts.IsEmpty then
        refreshTokenProvider ()