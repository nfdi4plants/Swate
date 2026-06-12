module Main.Git.GitServiceFailureClassification

open System
open Swate.Electron.Shared.GitTypes

let private lfsInstallRequiredTokens = [|
    "git lfs is required for files larger than"
    "git lfs is required for this operation"
    "git: 'lfs' is not a git command"
    "git-lfs filter-process"
    "this repository is configured for git lfs but 'git-lfs' was not found"
    "external filter 'git-lfs filter-process' failed"
    "smudge filter lfs failed"
    "clean filter 'lfs' failed"
|]

/// Classifies git/simple-git/LFS error text into the shared failure taxonomy used over IPC.
let classifyFailureKind (message: string) =
    let normalizedMessage =
        message
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> fun text -> text.ToLowerInvariant()

    let containsAny (terms: string[]) =
        terms |> Array.exists normalizedMessage.Contains

    if containsAny lfsInstallRequiredTokens then
        GitFailureKind.LfsInstallRequired
    elif containsAny [| "abort"; "cancelled"; "canceled"; "aborterror" |] then
        GitFailureKind.Canceled
    elif containsAny [| "timed out"; "timeout"; "time out" |] then
        GitFailureKind.Timeout
    elif containsAny [| "forbidden"; "403" |] then
        GitFailureKind.Forbidden
    elif
        containsAny [|
            "unauthorized"
            "authentication failed"
            "401"
            "could not read username"
            "no access token available"
            "permission denied"
        |]
    then
        GitFailureKind.Unauthorized
    elif
        containsAny [|
            "network"
            "could not resolve host"
            "failed to connect"
            "connection reset"
            "connection refused"
            "unable to access"
        |]
    then
        GitFailureKind.Network
    else
        GitFailureKind.Unknown
