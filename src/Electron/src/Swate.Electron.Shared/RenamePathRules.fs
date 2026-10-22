module Swate.Electron.Shared.RenamePathRules

open System
open Swate.Components.Shared

let private normalizeRelativePath (path: string) =
    path |> PathHelpers.normalizeCanonicalRelativePath

let private windowsForbiddenChars = [| '<'; '>'; ':'; '"'; '|'; '?'; '*' |]

let private windowsReservedDeviceNames =
    set [
        "CON"
        "PRN"
        "AUX"
        "NUL"
        "CONIN$"
        "CONOUT$"
        for i in 1..9 do
            $"COM{i}"
            $"LPT{i}"
        for c in [ '\u00B9'; '\u00B2'; '\u00B3' ] do
            $"COM{c}"
            $"LPT{c}"
    ]

/// Rejects names Windows or git for Windows cannot check out. A device name such as AUX is reserved with any extension, so the check uses the part before the first dot.
let validateRenameName (newName: string) =
    let normalizedNewName = newName.Trim()

    if String.IsNullOrWhiteSpace normalizedNewName then
        Error "A name is required."
    elif normalizedNewName = "." || normalizedNewName = ".." then
        Error "The name must not be '.' or '..'."
    elif
        normalizedNewName.Contains("/")
        || normalizedNewName.Contains("\\")
        || normalizedNewName.Contains("\u0000")
    then
        Error "The name must not contain path separators or null characters."
    elif
        normalizedNewName.IndexOfAny windowsForbiddenChars >= 0
        || normalizedNewName |> Seq.exists (fun c -> int c >= 1 && int c <= 31)
    then
        Error "The name must not contain < > : \" | ? * or control characters, because Windows does not allow them."
    elif normalizedNewName.EndsWith "." then
        Error "The name must not end with a dot, because Windows does not allow it."
    else
        let baseName =
            match normalizedNewName.IndexOf '.' with
            | -1 -> normalizedNewName
            | index -> normalizedNewName.Substring(0, index)
            |> fun name -> name.TrimEnd(' ')

        if windowsReservedDeviceNames.Contains(baseName.ToUpperInvariant()) then
            Error
                $"The name must not be '{baseName}' or start with '{baseName}.', because Windows reserves it for a device."
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

let private addZoneFileNameError =
    "Files directly inside an ARC collection folder need a file extension (for example 'notes.md'), so they cannot be mistaken for an ARC entity."

let legacyDataMapNameError =
    $"'{ArcEntityPathRules.legacyDataMapFileName}' is the outdated DataMap name. Loading the ARC would rename it to '{ARCtrl.ArcPathHelper.DataMapFileName}'."

// These checks live here because FileIOHelper is compiled after this module. FileIOHelper re-exports them.
let isOfficeLockFilePath (path: string) =
    Text.RegularExpressions.Regex.IsMatch(
        PathHelpers.normalizeSeparators path,
        """(^|/)(\.?~\$[^/]*\.[^/]+|\.~lock\.[^/]+#)$"""
    )

let isTemporaryImportPath (path: string) =
    Text.RegularExpressions.Regex.IsMatch(
        PathHelpers.normalizeSeparators path,
        """(^|/)\.swate-import-[0-9a-fA-F]{32}(/|$)"""
    )

let isLfsBackupPath (path: string) =
    Text.RegularExpressions.Regex.IsMatch(PathHelpers.normalizeSeparators path, """\.vcs-lfs-backup-[0-9a-fA-F]{32}$""")

let private lockFileNameError =
    "ARCitISA hides files named like the lock file of a document that is open in Office or LibreOffice, so the file would disappear from the file tree."

let lfsBackupNameError =
    "ARCitISA uses names like this for Git LFS backups and hides them, so the file would disappear from the file tree."

let private stagingFolderNameError =
    "ARCitISA uses names like this for import staging folders and does not watch them, so changes inside would not show up."

let stagingFileNameError =
    "ARCitISA uses names like this for import staging folders and does not watch them, so the file would not show changes."

let private stagingParentError =
    "ARCitISA does not watch import staging folders, so new items inside would not show up. Delete or rename the folder first."

let private stagingRenameParentError =
    "ARCitISA does not watch import staging folders, so the renamed item would not show up. Delete or rename the staging folder first."

let private validateNewItemName (isFile: bool) (targetPath: string) =
    let name = PathHelpers.getFileName targetPath

    if ArcEntityPathRules.isLegacyDataMapFileName name then
        Error legacyDataMapNameError
    elif isFile && isOfficeLockFilePath name then
        Error lockFileNameError
    elif isFile && isLfsBackupPath name then
        Error lfsBackupNameError
    elif isTemporaryImportPath name then
        Error(
            if isFile then
                stagingFileNameError
            else
                stagingFolderNameError
        )
    elif ArcEntityPathRules.isNewGenericFileSystemNameAllowed targetPath |> not then
        Error addZoneFileNameError
    else
        Ok targetPath

let tryBuildGenericFileSystemChildPath (isFile: bool) (parentPath: string) (name: string) =
    let normalizedParentPath = normalizeRelativePath parentPath

    let parentIsAllowed =
        String.IsNullOrWhiteSpace normalizedParentPath
        || ArcEntityPathRules.isGenericFileSystemCreateParentAllowed isFile normalizedParentPath

    if isTemporaryImportPath normalizedParentPath then
        Error stagingParentError
    elif parentIsAllowed |> not then
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

            match ArcEntityPathRules.describeBlockedGenericCreateTarget isFile targetPath with
            | Some reason -> Error reason
            | None -> validateNewItemName isFile targetPath

/// `isFile` tells whether the source is a file or a folder. `isAddZoneRootFile` marks a file directly inside an add zone (for example `studies/notes.md` or
/// `studies/.DS_Store`). Pass `false` for every other source.
let tryBuildGenericFileSystemRenameTargetPath
    (isFile: bool)
    (isAddZoneRootFile: bool)
    (sourcePath: string)
    (newName: string)
    =
    let normalizedSourcePath = normalizeRelativePath sourcePath

    let isTargetAllowed =
        if isAddZoneRootFile then
            ArcEntityPathRules.isGenericFileSystemFileTargetAllowed
        else
            ArcEntityPathRules.isGenericFileSystemTargetAllowed

    let isInsideStagingFolder =
        PathHelpers.tryGetParentPath normalizedSourcePath
        |> Option.exists isTemporaryImportPath

    if isInsideStagingFolder then
        Error stagingRenameParentError
    elif isTargetAllowed normalizedSourcePath |> not then
        Error "Generic file and folder rename is only allowed for safe non-canonical paths inside the ARC."
    else
        match tryBuildRenameTargetPath normalizedSourcePath newName with
        | Error validationError -> Error validationError
        | Ok targetPath ->
            if isTargetAllowed targetPath |> not then
                match ArcEntityPathRules.describeBlockedGenericCreateTarget isFile targetPath with
                | Some reason -> Error reason
                | None ->
                    // Creating a protected name such as README.md is allowed, renaming to it is not.
                    let name = PathHelpers.getFileName targetPath

                    Error $"ARCitISA protects the name '{name}', so it could not be renamed or deleted afterwards."
            else
                validateNewItemName isFile targetPath
