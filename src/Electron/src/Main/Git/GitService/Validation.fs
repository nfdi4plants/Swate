module Main.Git.GitServiceValidation

open System
open System.Text.RegularExpressions

let private disallowedRemotePrefixes = [| "file://"; "ext::"; "fd::" |]

let private protocolOverridePattern =
    Regex("protocol\\.[^\\s=]+\\.(allow|deny)|(^|\\s)-c\\s+protocol\\.", RegexOptions.IgnoreCase)

let private remoteNamePattern = Regex("^[A-Za-z0-9._/-]+$")
let private invalidBranchCharactersPattern = Regex(@"[~^:?*\[\\\s]")

let normalizeOptionalGitRef (value: string option) =
    value
    |> Option.bind Option.ofObj
    |> Option.map _.Trim()
    |> Option.filter (fun item -> not (String.IsNullOrWhiteSpace item))

/// Validates local branch names and branch-like start points before passing them to git.
let ensureValidBranchLikeName (label: string) (value: string) =
    let trimmed = value.Trim()
    let containsControlCharacter = trimmed |> Seq.exists Char.IsControl

    if String.IsNullOrWhiteSpace trimmed then
        Error(exn $"{label} must not be empty.")
    elif containsControlCharacter then
        Error(exn $"{label} contains control characters.")
    elif trimmed.StartsWith("-") then
        Error(exn $"{label} must not start with '-'.")
    elif trimmed.StartsWith("/") then
        Error(exn $"{label} must not start with '/'.")
    elif
        trimmed.Split('/')
        |> Array.exists (fun segment -> segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
    then
        Error(exn $"{label} must not contain '.lock' path segments.")
    elif
        trimmed.Contains("..")
        || trimmed.EndsWith(".")
        || trimmed.Contains("@{")
        || trimmed.EndsWith("/")
    then
        Error(exn $"{label} contains invalid git ref segments.")
    elif invalidBranchCharactersPattern.IsMatch(trimmed) then
        Error(exn $"{label} contains invalid characters.")
    else
        Ok trimmed

/// Validates renderer-provided pathspecs as ARC-relative paths before file or git access.
let ensureValidPathspec (pathSpec: string) =
    let normalized = pathSpec.Replace("\\", "/").Trim()

    if String.IsNullOrWhiteSpace normalized then
        Error(exn "Pathspec must not be empty.")
    elif normalized.StartsWith("/") then
        Error(exn "Absolute pathspecs are not allowed.")
    elif Regex.IsMatch(normalized, "^[A-Za-z]:/") then
        Error(exn "Absolute pathspecs are not allowed.")
    elif
        normalized.Split('/')
        |> Array.exists (fun segment -> segment = "." || segment = "..")
    then
        Error(exn "Pathspec must not contain traversal segments.")
    elif normalized.Contains("\000") then
        Error(exn "Pathspec contains invalid null characters.")
    else
        Ok normalized

/// Validates a non-empty pathspec array and returns normalized pathspecs for git commands.
let validatePathspecs (pathSpecs: string[]) =
    if isNull pathSpecs || pathSpecs.Length = 0 then
        Error(exn "At least one pathspec is required.")
    else
        pathSpecs
        |> Array.map ensureValidPathspec
        |> Array.fold
            (fun state next ->
                match state, next with
                | Error e, _ -> Error e
                | _, Error e -> Error e
                | Ok acc, Ok value -> Ok(Array.append acc [| value |])
            )
            (Ok [||])

/// Validates a remote name and defaults blank input to `origin`.
let validateRemoteName (remoteName: string) =
    let normalized =
        remoteName
        |> Option.ofObj
        |> Option.map _.Trim()
        |> Option.filter (fun x -> not (String.IsNullOrWhiteSpace x))
        |> Option.defaultValue "origin"

    if remoteNamePattern.IsMatch normalized then
        Ok normalized
    else
        Error(exn "Remote name contains unsupported characters.")

/// Enforces Swate's remote URL policy before clone/add-remote/auth lookup.
let ensureAllowedRemoteUrl (remoteUrl: string) =
    let normalized = remoteUrl.Trim()

    if String.IsNullOrWhiteSpace normalized then
        Error(exn "Remote URL is empty.")
    elif protocolOverridePattern.IsMatch normalized then
        Error(exn "Remote URL contains a protocol override attempt.")
    elif
        disallowedRemotePrefixes
        |> Array.exists (fun prefix -> normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    then
        Error(exn "Remote URL uses a blocked protocol.")
    elif normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
        Ok normalized
    elif normalized.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase) then
        Ok normalized
    else
        Error(exn "Only https:// and ssh:// remotes are allowed.")

let private trimTrailingGitSuffix (path: string) =
    if path.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && path.Length > 4 then
        path.[.. path.Length - 5]
    else
        path

/// Converts a validated git remote URL (https/ssh) into a browser-friendly repository URL.
let tryGetRepositoryWebUrlFromRemoteUrl (remoteUrl: string) : Result<string, exn> =
    match ensureAllowedRemoteUrl remoteUrl with
    | Error remoteUrlError -> Error remoteUrlError
    | Ok safeRemoteUrl ->
        let mutable uri = Unchecked.defaultof<Uri>

        if not (Uri.TryCreate(safeRemoteUrl, UriKind.Absolute, &uri)) then
            Error(exn $"Remote URL '{safeRemoteUrl}' is not a valid absolute URI.")
        elif String.IsNullOrWhiteSpace uri.Host then
            Error(exn "Remote URL is missing a host.")
        else
            let normalizedPath = uri.AbsolutePath.TrimEnd('/') |> trimTrailingGitSuffix

            if String.IsNullOrWhiteSpace normalizedPath || normalizedPath = "/" then
                Error(exn "Remote URL does not contain a repository path.")
            else
                Ok($"https://{uri.Host}{normalizedPath}".TrimEnd('/'))

let validateOptionalBranchName (branchName: string option) =
    match branchName with
    | None -> Ok None
    | Some branchName -> ensureValidBranchLikeName "Branch name" branchName |> Result.map Some
