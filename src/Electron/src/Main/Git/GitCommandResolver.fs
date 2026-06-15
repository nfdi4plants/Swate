module Main.Git.GitCommandResolver

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Node

let private macGitToolDirectories = [|
    "/opt/homebrew/bin"
    "/usr/local/bin"
    "/opt/local/bin"
    "/usr/bin"
    "/bin"
|]

let private linuxGitToolDirectories = [|
    "/home/linuxbrew/.linuxbrew/bin"
    "/usr/local/bin"
    "/usr/bin"
    "/bin"
    "/snap/bin"
|]

let private supportsFallback (platform: string) =
    platform = "darwin" || platform = "linux"

let private fallbackDirectories (platform: string) =
    match platform with
    | "darwin" -> macGitToolDirectories
    | "linux" -> linuxGitToolDirectories
    | _ -> [||]

let private pathSeparator (platform: string) =
    if platform.StartsWith("win", StringComparison.OrdinalIgnoreCase) then
        ';'
    else
        ':'

let private distinctPathEntries (separator: char) (fallbackEntries: string[]) (currentPath: string) =
    let entries = ResizeArray<string>()
    let seen = HashSet<string>()

    let addEntry (entry: string) =
        let normalizedEntry =
            entry
            |> Option.ofObj
            |> Option.defaultValue String.Empty
            |> _.Trim()

        if not (String.IsNullOrWhiteSpace normalizedEntry) && seen.Add normalizedEntry then
            entries.Add normalizedEntry

    fallbackEntries |> Array.iter addEntry

    currentPath.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.iter addEntry

    entries.ToArray() |> String.concat (string separator)

/// Returns the inherited PATH when the current environment can run `git lfs --version`.
/// On macOS/Linux desktop launches, Electron may miss shell startup PATH entries; in that case
/// prepend common package-manager Git locations while preserving the original PATH tail.
let resolveGitToolPath (canResolveGitLfs: string -> bool) (platform: string) (currentPath: string) =
    let normalizedPlatform =
        platform
        |> Option.ofObj
        |> Option.defaultValue String.Empty
        |> _.Trim()
        |> _.ToLowerInvariant()

    let existingPath =
        currentPath |> Option.ofObj |> Option.defaultValue String.Empty

    if supportsFallback normalizedPlatform && not (canResolveGitLfs existingPath) then
        distinctPathEntries (pathSeparator normalizedPlatform) (fallbackDirectories normalizedPlatform) existingPath
    else
        existingPath

[<Emit("process.platform")>]
let private getProcessPlatform () : string = jsNative

[<Emit("$0?.PATH || $0?.Path || $0?.path || ''")>]
let private getEnvironmentPath (_environment: obj) : string = jsNative

[<Emit("{ ...$0, PATH: $1 }")>]
let private withEnvironmentPath (_environment: obj) (_pathValue: string) : obj = jsNative

let private canRunGitLfsVersionWithPath (baseEnvironment: obj) (pathValue: string) =
    try
        let environment = withEnvironmentPath baseEnvironment pathValue

        childProcessDynamic?execFileSync (
            "git",
            [| "lfs"; "--version" |],
            createObj [
                "encoding" ==> "utf8"
                "stdio" ==> "pipe"
                "shell" ==> false
                "env" ==> environment
            ]
        )
        |> ignore

        true
    with _ ->
        false

let mutable private cachedGitToolPath: string option = None

/// Applies a resolved PATH to an environment object only when the inherited environment cannot
/// run `git lfs --version` on macOS/Linux.
let ensureGitToolPath (environment: obj) =
    let currentPath = getEnvironmentPath environment

    let resolvedPath =
        match cachedGitToolPath with
        | Some path -> path
        | None ->
            let path =
                resolveGitToolPath (canRunGitLfsVersionWithPath environment) (getProcessPlatform ()) currentPath

            cachedGitToolPath <- Some path
            path

    if String.Equals(currentPath, resolvedPath, StringComparison.Ordinal) then
        environment
    else
        withEnvironmentPath environment resolvedPath
