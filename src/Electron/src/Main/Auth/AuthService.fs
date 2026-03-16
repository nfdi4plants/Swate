module Main.Auth.AuthService

open System
open Fable.Core
open Fetch
open Swate.Electron.Shared.AuthTypes

// ── Types ────────────────────────────────────────────────────────────

type internal AuthFailure = {
    Kind: AuthFailureKind
    Message: string
}

// ── GitLab API verification (Fable.Fetch) ────────────────────────────

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

/// Call GitLab /api/v4/user with the provided PAT to verify the token.
/// Uses Fable.Fetch following the pattern in Authentication.fs.
let private verifyToken (baseUrl: string) (pat: string) : JS.Promise<Result<AuthUserDto, AuthFailure>> = promise {
    let url = $"{baseUrl}/api/v4/user"

    let requestOptions = [
        RequestProperties.Method HttpMethod.GET
        requestHeaders [ HttpRequestHeaders.Custom("PRIVATE-TOKEN", pat) ]
    ]

    try
        let! response = fetchUnsafe url requestOptions

        if not response.Ok then
            return
                match response.Status with
                | 401 ->
                    Error {
                        Kind = AuthFailureKind.Unauthorized
                        Message = "Personal Access Token is invalid or expired."
                    }
                | 403 ->
                    Error {
                        Kind = AuthFailureKind.Forbidden
                        Message = "Access forbidden. The token may lack required scopes."
                    }
                | 404 ->
                    Error {
                        Kind = AuthFailureKind.EndpointInvalid
                        Message = "GitLab API endpoint not found. Check the DataHub URL."
                    }
                | code ->
                    Error {
                        Kind = AuthFailureKind.Unknown
                        Message = $"Unexpected HTTP status {code}."
                    }
        else
            let! json =
                response.json<
                    {|
                        name: string
                        email: string
                        avatar_url: string
                    |}
                 > ()

            let accountId = SecureAuthStore.generateAccountId baseUrl json.email

            let user: AuthUserDto = {
                AccountId = accountId
                Name = json.name
                Email = json.email
                AvatarUrl = json.avatar_url
                TargetDataHub = baseUrl
            }

            return Ok user
    with _ ->
        return
            Error {
                Kind = AuthFailureKind.Network
                Message = "Network error contacting the DataHub. Check your connection."
            }
}

// ── Mutable in-memory state (multi-account) ──────────────────────────
//
// Token provider policy:
//   1. Check active account first — if its host matches, use its token.
//   2. If no match, search all accounts for a host match.
//   3. If still no match, return None.

/// Map of accountId → (AuthUserDto, token)
let mutable private accounts: Map<string, AuthUserDto * string> = Map.empty

/// Currently active account ID.
let mutable private activeAccountId: string option = None

let private getActiveAccount () =
    activeAccountId |> Option.bind (fun id -> accounts |> Map.tryFind id)

let private toSummary (user: AuthUserDto, _token: string) : AuthAccountSummary = {
    AccountId = user.AccountId
    Name = user.Name
    Email = user.Email
    AvatarUrl = user.AvatarUrl
    TargetDataHub = user.TargetDataHub
    IsActive = activeAccountId = Some user.AccountId
}

/// Try to get token for a given host (used by GitTokenProvider).
/// Policy: active account first, then any account matching the host.
let tryGetTokenForHost (host: string) : string option =
    // 1. Check active account
    match getActiveAccount () with
    | Some(user, token) when
        String.Equals(SecureAuthStore.extractHost user.TargetDataHub, host, StringComparison.OrdinalIgnoreCase)
        ->
        Some token
    | _ ->
        // 2. Search all accounts
        accounts
        |> Map.tryPick (fun _ (user, token) ->
            if
                String.Equals(SecureAuthStore.extractHost user.TargetDataHub, host, StringComparison.OrdinalIgnoreCase)
            then
                Some token
            else
                None
        )

let private refreshTokenProvider () =
    Main.Git.GitTokenProvider.setTokenProvider {
        TryGetAccessToken = fun host -> promise { return tryGetTokenForHost host }
    }

/// Get the current in-memory auth state for the active account.
let getState () : AuthStateDto =
    let allSummaries = accounts |> Map.toArray |> Array.map (fun (_, v) -> toSummary v)

    match getActiveAccount () with
    | Some(user, _) -> {
        IsAuthenticated = true
        User = Some user
        Accounts = allSummaries
      }
    | None -> {
        IsAuthenticated = false
        User = None
        Accounts = allSummaries
      }

/// List all stored account summaries.
let listAccounts () : AuthAccountSummary array =
    accounts |> Map.toArray |> Array.map (fun (_, v) -> toSummary v)

// ── Public operations ────────────────────────────────────────────────

let private toAuthResult (result: Result<AuthUserDto, AuthFailure>) : AuthResult =
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
            let! verifyResult = verifyToken baseUrl request.PersonalAccessToken

            match verifyResult with
            | Error failure -> return toAuthResult (Error failure)
            | Ok user ->
                let storeResult =
                    SecureAuthStore.store {
                        Metadata = {
                            AccountId = user.AccountId
                            Name = user.Name
                            Email = user.Email
                            AvatarUrl = user.AvatarUrl
                            TargetDataHub = user.TargetDataHub
                        }
                        Token = request.PersonalAccessToken
                    }

                match storeResult with
                | Error msg -> Browser.Dom.console.warn $"Auth storage warning: {msg}"
                | Ok() -> ()

                accounts <- accounts |> Map.add user.AccountId (user, request.PersonalAccessToken)
                activeAccountId <- Some user.AccountId
                SecureAuthStore.setActiveAccountId activeAccountId
                refreshTokenProvider ()
                return toAuthResult (Ok user)
}

/// Sign out: remove active account from memory and disk, pick next or clear.
let signOut () : unit =
    match activeAccountId with
    | Some id ->
        SecureAuthStore.remove id
        accounts <- accounts |> Map.remove id
        // Pick the next available account as active, or None
        let nextActive = accounts |> Map.tryPick (fun k _ -> Some k)
        activeAccountId <- nextActive
        SecureAuthStore.setActiveAccountId activeAccountId
        refreshTokenProvider ()
    | None -> ()

/// Set a different account as active.
let setActiveAccount (accountId: string) : AuthStateDto =
    match accounts |> Map.tryFind accountId with
    | Some _ ->
        activeAccountId <- Some accountId
        SecureAuthStore.setActiveAccountId activeAccountId
        refreshTokenProvider ()
    | None -> ()

    getState ()

/// Remove a specific account (by ID). If it was active, switch to next or clear.
let removeAccount (accountId: string) : unit =
    SecureAuthStore.remove accountId
    accounts <- accounts |> Map.remove accountId

    if activeAccountId = Some accountId then
        let nextActive = accounts |> Map.tryPick (fun k _ -> Some k)
        activeAccountId <- nextActive
        SecureAuthStore.setActiveAccountId activeAccountId

    refreshTokenProvider ()

/// Revalidate: re-verify the active account's token with GitLab.
let revalidate () : JS.Promise<AuthResult> = promise {
    match getActiveAccount () with
    | None ->
        return {
            Success = false
            User = None
            FailureKind = Some AuthFailureKind.Unauthorized
            Message = Some "No active account."
        }
    | Some(user, token) ->
        let! verifyResult = verifyToken user.TargetDataHub token

        match verifyResult with
        | Error failure ->
            // Token is no longer valid — remove only this account
            removeAccount user.AccountId
            return toAuthResult (Error failure)
        | Ok updatedUser ->
            accounts <- accounts |> Map.add updatedUser.AccountId (updatedUser, token)
            return toAuthResult (Ok updatedUser)
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

        accounts <- accounts |> Map.add user.AccountId (user, credential.Token)

    // Restore persisted active account selection
    activeAccountId <- SecureAuthStore.getActiveAccountId ()

    // If persisted active account no longer exists, pick the first available
    match activeAccountId with
    | Some id when accounts |> Map.containsKey id -> ()
    | _ ->
        activeAccountId <- accounts |> Map.tryPick (fun k _ -> Some k)
        SecureAuthStore.setActiveAccountId activeAccountId

    if not accounts.IsEmpty then
        refreshTokenProvider ()