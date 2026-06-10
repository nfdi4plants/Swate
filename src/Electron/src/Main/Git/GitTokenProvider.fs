module Main.Git.GitTokenProvider

open System
open Fable.Core

/// Main-process hook used by Git services to resolve an access token for a remote host.
/// AuthService installs the active provider after sign-in; tests and startup code can reset it to the default provider.
type GitTokenProvider = {
    TryGetAccessToken: string -> JS.Promise<string option>
}

/// Provider used when no account is active. Returning None keeps clone unauthenticated and makes authenticated sync fail clearly.
let defaultTokenProvider: GitTokenProvider = {
    TryGetAccessToken = (fun _ -> promise { return None })
}

let mutable private activeTokenProvider: GitTokenProvider = defaultTokenProvider

/// Replaces the process-wide token source used by subsequent Git operations.
let setTokenProvider (provider: GitTokenProvider) = activeTokenProvider <- provider

/// Resolves a token for a normalized host name. Callers decide whether None is allowed for their operation.
let tryGetAccessToken (host: string) : JS.Promise<string option> =
    activeTokenProvider.TryGetAccessToken host

let private tryExtractHostFromAbsoluteUri (remoteUrl: string) =
    let mutable uri = Unchecked.defaultof<Uri>

    if
        Uri.TryCreate(remoteUrl, UriKind.Absolute, &uri)
        && not (String.IsNullOrWhiteSpace uri.Host)
    then
        Ok(uri.Host.Trim().ToLowerInvariant())
    else
        Error(exn $"Remote URL '{remoteUrl}' is not a valid absolute URI.")

/// Extracts the lowercase host from supported HTTPS/SSH remote URLs before token lookup.
/// SCP-style SSH URLs are intentionally rejected by the shared remote URL policy.
let tryExtractHostFromRemoteUrl (remoteUrl: string) : Result<string, exn> =
    let normalized = remoteUrl.Trim()

    if String.IsNullOrWhiteSpace normalized then
        Error(exn "Remote URL is empty.")
    elif normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
        tryExtractHostFromAbsoluteUri normalized
    elif normalized.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase) then
        tryExtractHostFromAbsoluteUri normalized
    else
        Error(exn "Remote URL must use https:// or ssh://.")
