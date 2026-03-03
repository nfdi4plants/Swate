module Main.Git.GitTokenProvider

open System
open Fable.Core

type GitTokenProvider = { TryGetAccessToken: string -> JS.Promise<string option> }

let defaultTokenProvider: GitTokenProvider = {
    TryGetAccessToken = (fun _ -> promise { return None })
}

let mutable private activeTokenProvider: GitTokenProvider = defaultTokenProvider

let setTokenProvider (provider: GitTokenProvider) =
    activeTokenProvider <- provider

let tryGetAccessToken (host: string) : JS.Promise<string option> =
    activeTokenProvider.TryGetAccessToken host

let private tryExtractHostFromAbsoluteUri (remoteUrl: string) =
    let mutable uri = Unchecked.defaultof<Uri>

    if Uri.TryCreate(remoteUrl, UriKind.Absolute, &uri) && not (String.IsNullOrWhiteSpace uri.Host) then
        Ok(uri.Host.Trim().ToLowerInvariant())
    else
        Error(exn $"Remote URL '{remoteUrl}' is not a valid absolute URI.")

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
