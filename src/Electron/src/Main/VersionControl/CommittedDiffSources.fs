/// Adapts pinned history sources to the workspace-only contract of VersionControlService 0.2.1.
/// This envelope stays inside the main/worker boundary; the renderer uses ordinary paths.
module Main.VersionControl.CommittedDiffSources

open System

type Selection = {
    Revision: string
    ParentRevision: string option
    Path: string
}

[<Literal>]
let private prefix = "__swate_committed_diff__/"

[<Literal>]
let private workspacePrefix = "__swate_workspace_diff__/"

/// Escape literal workspace paths which could otherwise resemble an internal source envelope.
let encodeWorkspacePath (path: string) =
    if
        path.StartsWith(prefix, StringComparison.Ordinal)
        || path.StartsWith(workspacePrefix, StringComparison.Ordinal)
    then
        workspacePrefix + Uri.EscapeDataString path
    else
        path

let decodeWorkspacePath (path: string) =
    if path.StartsWith(workspacePrefix, StringComparison.Ordinal) then
        Uri.UnescapeDataString(path.Substring(workspacePrefix.Length))
    else
        path

let validRevision (value: string) =
    not (isNull value)
    && (value.Length = 40 || value.Length = 64)
    && value |> Seq.forall (fun c -> (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))

let encode (selection: Selection) =
    prefix
    + selection.Revision
    + "/"
    + (selection.ParentRevision |> Option.defaultValue "root")
    + "/"
    + Uri.EscapeDataString selection.Path

let tryDecode (value: string) =
    if isNull value || not (value.StartsWith(prefix, StringComparison.Ordinal)) then
        None
    else
        let fields = value.Substring(prefix.Length).Split('/')

        if fields.Length <> 3 || not (validRevision fields[0]) then
            None
        elif fields[1] <> "root" && not (validRevision fields[1]) then
            None
        else
            try
                let path = Uri.UnescapeDataString fields[2]

                match VersionControlService.Abstractions.RepositoryPath.tryCreate path with
                | Error _ -> None
                | Ok _ ->
                    Some {
                        Revision = fields[0]
                        ParentRevision = if fields[1] = "root" then None else Some fields[1]
                        Path = path
                    }
            with _ ->
                None
