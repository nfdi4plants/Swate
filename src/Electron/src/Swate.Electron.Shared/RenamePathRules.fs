module Swate.Electron.Shared.RenamePathRules

open System
open Swate.Components.Shared

let private normalizeRelativePath (path: string) =
    path
    |> PathHelpers.normalizeCanonicalRelativePath

let private tryGetParentPath (path: string) =
    let normalizedPath = PathHelpers.normalizePath path
    let separatorIndex = normalizedPath.LastIndexOf('/')

    if separatorIndex < 0 then
        None
    else
        Some(normalizedPath.Substring(0, separatorIndex))

let validateRenameName (newName: string) =
    let normalizedNewName = newName.Trim()

    if String.IsNullOrWhiteSpace normalizedNewName then
        Error "A new name is required."
    elif normalizedNewName = "." || normalizedNewName = ".." then
        Error "The new name must not be '.' or '..'."
    elif
        normalizedNewName.Contains("/")
        || normalizedNewName.Contains("\\")
        || normalizedNewName.Contains("\u0000")
    then
        Error "The new name must not contain path separators or null characters."
    else
        Ok normalizedNewName

let buildRenamedSiblingPath (sourcePath: string) (newName: string) =
    let normalizedSourcePath = normalizeRelativePath sourcePath
    let normalizedNewName = newName.Trim()

    match tryGetParentPath normalizedSourcePath with
    | Some parentPath when String.IsNullOrWhiteSpace parentPath |> not -> $"{parentPath}/{normalizedNewName}"
    | _ -> normalizedNewName

let tryBuildRenameTargetPath (sourcePath: string) (newName: string) =
    let normalizedSourcePath = normalizeRelativePath sourcePath

    if String.IsNullOrWhiteSpace normalizedSourcePath then
        Error "Rename source path is required."
    else
        match validateRenameName newName with
        | Error validationError -> Error validationError
        | Ok normalizedNewName ->
            let targetPath = buildRenamedSiblingPath normalizedSourcePath normalizedNewName

            if PathHelpers.pathsEqual normalizedSourcePath targetPath then
                Error "Rename target is identical to the current path."
            else
                Ok targetPath
