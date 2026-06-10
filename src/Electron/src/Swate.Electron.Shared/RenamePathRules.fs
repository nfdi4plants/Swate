module Swate.Electron.Shared.RenamePathRules

open System
open Swate.Components.Shared

let private normalizeRelativePath (path: string) =
    path |> PathHelpers.normalizeCanonicalRelativePath

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

    match PathHelpers.tryGetParentPath normalizedSourcePath with
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

let tryBuildGenericFileSystemChildPath (parentPath: string) (name: string) =
    let normalizedParentPath = normalizeRelativePath parentPath

    let parentIsAllowed =
        String.IsNullOrWhiteSpace normalizedParentPath
        || ArcEntityPathRules.isGenericFileSystemParentAllowed normalizedParentPath

    if parentIsAllowed |> not then
        Error "Generic file and folder creation is only allowed inside safe ARC directories."
    else
        match validateRenameName name with
        | Error validationError -> Error validationError
        | Ok normalizedName ->
            let targetPath =
                if String.IsNullOrWhiteSpace normalizedParentPath then
                    normalizedName
                else
                    $"{normalizedParentPath}/{normalizedName}"

            if ArcEntityPathRules.isGenericFileSystemTargetAllowed targetPath then
                Ok targetPath
            else
                Error
                    "Generic file and folder targets must stay inside the ARC and must not target canonical ARC files."

let tryBuildGenericFileSystemRenameTargetPath (sourcePath: string) (newName: string) =
    let normalizedSourcePath = normalizeRelativePath sourcePath

    if ArcEntityPathRules.isGenericFileSystemTargetAllowed normalizedSourcePath |> not then
        Error "Generic file and folder rename is only allowed for safe non-canonical paths inside the ARC."
    else
        match tryBuildRenameTargetPath normalizedSourcePath newName with
        | Error validationError -> Error validationError
        | Ok targetPath ->
            if ArcEntityPathRules.isGenericFileSystemTargetAllowed targetPath then
                Ok targetPath
            else
                Error
                    "Generic file and folder rename targets must stay inside the ARC and must not target canonical ARC files."
