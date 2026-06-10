namespace Swate.Components.Shared

open System


[<RequireQualifiedAccess>]
module PathHelpers =

    let formatContractErrors (errors: string[]) =
        errors |> Array.map string |> String.concat "\n"

    /// normalizes the path by replacing backslashes with forward slashes, trimming whitespace, and removing trailing slashes
    let normalizeSeparators (path: string) = path.Replace("\\", "/")

    /// normalizes the path by replacing backslashes with forward slashes, trimming whitespace, and removing trailing slashes
    let normalizePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.Trim().TrimEnd('/')

    let normalizeRelativePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.Trim('/').Trim()

    let normalizeCanonicalRelativePath (path: string) =
        path |> normalizeRelativePath |> normalizePath

    let normalizeForComparison (path: string) =
        normalizeSeparators path
        |> fun normalized -> normalized.Trim().TrimEnd('/').ToLowerInvariant()

    let normalizePathForFsComparison (path: string) =
        path |> normalizePath |> normalizeForComparison

    let isSameOrDescendantPath (path: string) (ancestorPath: string) =
        let normalizedPath = normalizePath path
        let normalizedAncestorPath = normalizePath ancestorPath

        String.IsNullOrWhiteSpace normalizedAncestorPath
        || normalizedPath = normalizedAncestorPath
        || normalizedPath.StartsWith(normalizedAncestorPath + "/", StringComparison.OrdinalIgnoreCase)

    let isSameOrDescendantPathForFsComparison (path: string) (ancestorPath: string) =
        let normalizedPath = normalizePathForFsComparison path
        let normalizedAncestorPath = normalizePathForFsComparison ancestorPath

        not (String.IsNullOrWhiteSpace normalizedPath)
        && not (String.IsNullOrWhiteSpace normalizedAncestorPath)
        && (normalizedPath = normalizedAncestorPath
            || normalizedPath.StartsWith(normalizedAncestorPath + "/"))

    let containsPathTraversalSegments (path: string) =
        normalizeSeparators path
        |> fun normalized -> normalized.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.exists (fun segment -> segment = "." || segment = "..")

    let isSafePathSegment (value: string) =
        not (String.IsNullOrWhiteSpace value)
        && not (value.Contains "/")
        && not (value.Contains "\\")
        && not (value.Contains "..")

    let pathsEqual (left: string) (right: string) =
        normalizeForComparison left = normalizeForComparison right

    let pathMatchesAny (candidates: string seq) (path: string) =
        candidates |> Seq.exists (fun candidate -> pathsEqual candidate path)

    let getNameFromPath (path: string) =
        normalizePath path |> (fun normalized -> normalized.Split('/')) |> Array.last

    let tryGetParentPath (path: string) =
        let normalizedPath = normalizePath path
        let separatorIndex = normalizedPath.LastIndexOf('/')

        if separatorIndex < 0 then
            None
        else
            Some(normalizedPath.Substring(0, separatorIndex))

    let private tryResolveDatamapPreviewPath (normalizedPath: string) (folderName: string) (targetFileName: string) =
        let pathSegments =
            normalizedPath.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)

        match pathSegments with
        | [| firstSegment; _; lastSegment |] when
            String.Equals(firstSegment, folderName, StringComparison.OrdinalIgnoreCase)
            && String.Equals(lastSegment, "isa.datamap.xlsx", StringComparison.OrdinalIgnoreCase)
            ->
            tryGetParentPath normalizedPath
            |> Option.map (fun folderPath -> $"{folderPath}/{targetFileName}")
        | _ -> None

    let resolveArcViewPath (path: string) =
        let normalizedPath = normalizePath path

        [
            tryResolveDatamapPreviewPath normalizedPath "assays" "isa.assay.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "studies" "isa.study.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "workflows" "isa.workflow.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "runs" "isa.run.xlsx"
        ]
        |> List.tryPick id
        |> Option.defaultValue normalizedPath

    /// normalizes the path and splits it into parts
    let getPathParts (path: string) =
        normalizePath path |> fun p -> p.Split('/')

    let getFileName (path: string) = path |> getPathParts |> Array.last

    let isProtectedDeleteTarget protectedDeleteTargetNames (normalizedRelativePath: string) =
        normalizedRelativePath
        |> getFileName
        |> pathMatchesAny protectedDeleteTargetNames

[<RequireQualifiedAccess>]
module ArcEntityPathRules =

    type AddZone =
        | Studies
        | Assays
        | Workflows
        | Runs

    type CanonicalArcFileTarget =
        | InvestigationFile
        | EntityFile of zone: AddZone * identifier: string
        | DataMapFile of zone: AddZone * identifier: string

    type DeletePathClassification =
        | ProtectedTarget of normalizedRelativePath: string
        | CanonicalFileTarget of target: CanonicalArcFileTarget * normalizedRelativePath: string
        | EntityFolderTarget of zone: AddZone * identifier: string * normalizedRelativePath: string
        | AddZoneDescendantTarget of zone: AddZone * normalizedRelativePath: string
        | GenericTarget of normalizedRelativePath: string
        | DisallowedTarget of normalizedRelativePath: string

    type RenamePathClassification =
        | RootTarget
        | DisallowedTarget of normalizedRelativePath: string
        | ProtectedTarget of normalizedRelativePath: string
        | InvestigationFileTarget of normalizedRelativePath: string
        | AddZoneRootTarget of zone: AddZone * normalizedRelativePath: string
        | EntityFolderTarget of zone: AddZone * identifier: string * normalizedRelativePath: string
        | CanonicalEntityFileTarget of zone: AddZone * identifier: string * normalizedRelativePath: string
        | CanonicalDataMapFileTarget of zone: AddZone * identifier: string * normalizedRelativePath: string
        | GenericTarget of normalizedRelativePath: string

    let private protectedDeleteTargetNames = [ ".gitkeep"; "readme.md" ]
    let private disallowedGenericPathSegments = [ ".git" ]

    let private normalizeRelativePath (path: string) =
        path |> PathHelpers.normalizeCanonicalRelativePath

    let private splitPathSegments (path: string) =
        path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)

    let private zoneFolderName =
        function
        | AddZone.Studies -> ARCtrl.ArcPathHelper.StudiesFolderName
        | AddZone.Assays -> ARCtrl.ArcPathHelper.AssaysFolderName
        | AddZone.Workflows -> ARCtrl.ArcPathHelper.WorkflowsFolderName
        | AddZone.Runs -> ARCtrl.ArcPathHelper.RunsFolderName

    let private zoneEntityFileName =
        function
        | AddZone.Studies -> ARCtrl.ArcPathHelper.StudyFileName
        | AddZone.Assays -> ARCtrl.ArcPathHelper.AssayFileName
        | AddZone.Workflows -> ARCtrl.ArcPathHelper.WorkflowFileName
        | AddZone.Runs -> ARCtrl.ArcPathHelper.RunFileName

    let private tryParseZone (segment: string) =
        if PathHelpers.pathsEqual segment ARCtrl.ArcPathHelper.StudiesFolderName then
            Some AddZone.Studies
        elif PathHelpers.pathsEqual segment ARCtrl.ArcPathHelper.AssaysFolderName then
            Some AddZone.Assays
        elif PathHelpers.pathsEqual segment ARCtrl.ArcPathHelper.WorkflowsFolderName then
            Some AddZone.Workflows
        elif PathHelpers.pathsEqual segment ARCtrl.ArcPathHelper.RunsFolderName then
            Some AddZone.Runs
        else
            None

    let private tryParseCanonicalArcFileTargetFromSegments (segments: string[]) =
        if segments.Length = 0 then
            None
        elif PathHelpers.pathsEqual segments.[segments.Length - 1] ARCtrl.ArcPathHelper.InvestigationFileName then
            Some CanonicalArcFileTarget.InvestigationFile
        elif segments.Length >= 3 then
            let fileName = segments.[segments.Length - 1]
            let identifier = segments.[segments.Length - 2]
            let folder = segments.[segments.Length - 3]

            match tryParseZone folder with
            | None -> None
            | Some zone when PathHelpers.pathsEqual fileName (zoneEntityFileName zone) ->
                Some(CanonicalArcFileTarget.EntityFile(zone, identifier))
            | Some zone when PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.DataMapFileName ->
                Some(CanonicalArcFileTarget.DataMapFile(zone, identifier))
            | Some _ -> None
        else
            None

    let private isCanonicalArcFileName (fileName: string) =
        PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.InvestigationFileName
        || PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.StudyFileName
        || PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.AssayFileName
        || PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.WorkflowFileName
        || PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.RunFileName
        || PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.DataMapFileName

    let private containsDisallowedGenericPathSegment (segments: string[]) =
        segments
        |> Array.exists (fun segment ->
            disallowedGenericPathSegments
            |> List.exists (fun blocked -> PathHelpers.pathsEqual segment blocked)
        )

    /// Parses canonical ARC file targets from the tail of a path and supports absolute paths.
    let tryParseCanonicalArcFileTarget (path: string) =
        path
        |> PathHelpers.normalizePath
        |> splitPathSegments
        |> tryParseCanonicalArcFileTargetFromSegments

    let classifyDeleteTarget (relativePath: string) =
        let normalizedRelativePath = normalizeRelativePath relativePath

        if String.IsNullOrWhiteSpace normalizedRelativePath then
            DeletePathClassification.DisallowedTarget normalizedRelativePath
        elif PathHelpers.isProtectedDeleteTarget protectedDeleteTargetNames normalizedRelativePath then
            DeletePathClassification.ProtectedTarget normalizedRelativePath
        else
            let segments = normalizedRelativePath |> splitPathSegments

            match segments with
            | [| singleSegment |] ->
                match tryParseZone singleSegment with
                | Some _ -> DeletePathClassification.DisallowedTarget normalizedRelativePath
                | None when PathHelpers.pathsEqual singleSegment ARCtrl.ArcPathHelper.InvestigationFileName ->
                    DeletePathClassification.CanonicalFileTarget(
                        CanonicalArcFileTarget.InvestigationFile,
                        normalizedRelativePath
                    )
                | None -> DeletePathClassification.GenericTarget normalizedRelativePath
            | [| zoneSegment; identifier |] ->
                match tryParseZone zoneSegment with
                | Some zone -> DeletePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath)
                | None -> DeletePathClassification.GenericTarget normalizedRelativePath
            | [| zoneSegment; identifier; fileName |] ->
                match tryParseZone zoneSegment with
                | Some zone when PathHelpers.pathsEqual fileName (zoneEntityFileName zone) ->
                    DeletePathClassification.CanonicalFileTarget(
                        CanonicalArcFileTarget.EntityFile(zone, identifier),
                        normalizedRelativePath
                    )
                | Some zone when PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.DataMapFileName ->
                    DeletePathClassification.CanonicalFileTarget(
                        CanonicalArcFileTarget.DataMapFile(zone, identifier),
                        normalizedRelativePath
                    )
                | Some zone -> DeletePathClassification.AddZoneDescendantTarget(zone, normalizedRelativePath)
                | None ->
                    if PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.InvestigationFileName then
                        DeletePathClassification.CanonicalFileTarget(
                            CanonicalArcFileTarget.InvestigationFile,
                            normalizedRelativePath
                        )
                    else
                        DeletePathClassification.GenericTarget normalizedRelativePath
            | _ ->
                if segments.Length >= 2 then
                    match tryParseZone segments.[0] with
                    | Some zone -> DeletePathClassification.AddZoneDescendantTarget(zone, normalizedRelativePath)
                    | None -> DeletePathClassification.GenericTarget normalizedRelativePath
                else
                    match tryParseCanonicalArcFileTargetFromSegments segments with
                    | Some target -> DeletePathClassification.CanonicalFileTarget(target, normalizedRelativePath)
                    | None -> DeletePathClassification.DisallowedTarget normalizedRelativePath

    let isGenericFileSystemTargetAllowed (relativePath: string) =
        let normalizedRelativePath = normalizeRelativePath relativePath

        if
            String.IsNullOrWhiteSpace normalizedRelativePath
            || PathHelpers.containsPathTraversalSegments normalizedRelativePath
            || PathHelpers.isProtectedDeleteTarget protectedDeleteTargetNames normalizedRelativePath
        then
            false
        else
            let segments = normalizedRelativePath |> splitPathSegments

            segments.Length >= 1
            && (segments |> containsDisallowedGenericPathSegment |> not)
            && (PathHelpers.getFileName normalizedRelativePath |> isCanonicalArcFileName |> not)
            && (segments.Length <> 1 || (tryParseZone segments.[0]).IsNone)
            && (segments.Length <> 2 || (tryParseZone segments.[0]).IsNone)

    let isDeletePathAllowed (relativePath: string) =
        match classifyDeleteTarget relativePath with
        | DeletePathClassification.CanonicalFileTarget(CanonicalArcFileTarget.EntityFile _, _)
        | DeletePathClassification.CanonicalFileTarget(CanonicalArcFileTarget.DataMapFile _, _)
        | DeletePathClassification.EntityFolderTarget _ -> true
        | DeletePathClassification.GenericTarget normalizedRelativePath
        | DeletePathClassification.AddZoneDescendantTarget(_, normalizedRelativePath) ->
            isGenericFileSystemTargetAllowed normalizedRelativePath
        | _ -> false

    let private canonicalEntityFilePath zone identifier =
        let zoneFolder = zoneFolderName zone
        let entityFileName = zoneEntityFileName zone
        $"{zoneFolder}/{identifier}/{entityFileName}"

    let private canonicalDataMapFilePath zone identifier =
        let zoneFolder = zoneFolderName zone
        $"{zoneFolder}/{identifier}/{ARCtrl.ArcPathHelper.DataMapFileName}"

    let private canonicalEntityFolderPath zone identifier =
        let zoneFolder = zoneFolderName zone
        $"{zoneFolder}/{identifier}"

    let buildFallbackUnlinkPaths (relativePath: string) =
        let fallbackPaths =
            match classifyDeleteTarget relativePath with
            | DeletePathClassification.CanonicalFileTarget(CanonicalArcFileTarget.EntityFile _, normalizedRelativePath)
            | DeletePathClassification.CanonicalFileTarget(CanonicalArcFileTarget.DataMapFile _, normalizedRelativePath) -> [
                normalizedRelativePath
              ]
            | DeletePathClassification.EntityFolderTarget(zone, identifier, _) -> [
                canonicalEntityFilePath zone identifier
                canonicalDataMapFilePath zone identifier
              ]
            | _ -> []

        fallbackPaths |> Seq.distinctBy PathHelpers.normalizeForComparison |> Seq.toList

    let classifyRenameTarget (relativePath: string) =
        let normalizedRelativePath = normalizeRelativePath relativePath

        if String.IsNullOrWhiteSpace normalizedRelativePath then
            RenamePathClassification.RootTarget
        else
            let segments = normalizedRelativePath |> splitPathSegments

            if PathHelpers.containsPathTraversalSegments normalizedRelativePath then
                RenamePathClassification.DisallowedTarget normalizedRelativePath
            elif PathHelpers.isProtectedDeleteTarget protectedDeleteTargetNames normalizedRelativePath then
                RenamePathClassification.ProtectedTarget normalizedRelativePath
            else
                match segments with
                | [| singleSegment |] ->
                    match tryParseZone singleSegment with
                    | Some zone -> RenamePathClassification.AddZoneRootTarget(zone, normalizedRelativePath)
                    | None when PathHelpers.pathsEqual singleSegment ARCtrl.ArcPathHelper.InvestigationFileName ->
                        RenamePathClassification.InvestigationFileTarget normalizedRelativePath
                    | None -> RenamePathClassification.GenericTarget normalizedRelativePath
                | [| zoneSegment; identifier |] ->
                    match tryParseZone zoneSegment with
                    | Some zone -> RenamePathClassification.EntityFolderTarget(zone, identifier, normalizedRelativePath)
                    | None -> RenamePathClassification.GenericTarget normalizedRelativePath
                | [| zoneSegment; identifier; fileName |] ->
                    match tryParseZone zoneSegment with
                    | Some zone when PathHelpers.pathsEqual fileName (zoneEntityFileName zone) ->
                        RenamePathClassification.CanonicalEntityFileTarget(zone, identifier, normalizedRelativePath)
                    | Some zone when PathHelpers.pathsEqual fileName ARCtrl.ArcPathHelper.DataMapFileName ->
                        RenamePathClassification.CanonicalDataMapFileTarget(zone, identifier, normalizedRelativePath)
                    | _ -> RenamePathClassification.GenericTarget normalizedRelativePath
                | _ ->
                    if PathHelpers.pathsEqual normalizedRelativePath ARCtrl.ArcPathHelper.InvestigationFileName then
                        RenamePathClassification.InvestigationFileTarget normalizedRelativePath
                    else
                        RenamePathClassification.GenericTarget normalizedRelativePath

    let isRenamePathAllowed (relativePath: string) =
        match classifyRenameTarget relativePath with
        | RenamePathClassification.EntityFolderTarget _ -> true
        | RenamePathClassification.GenericTarget normalizedRelativePath ->
            isGenericFileSystemTargetAllowed normalizedRelativePath
        | _ -> false

    let isGenericFileSystemParentAllowed (relativePath: string) =
        let normalizedRelativePath = normalizeRelativePath relativePath

        if
            String.IsNullOrWhiteSpace normalizedRelativePath
            || PathHelpers.containsPathTraversalSegments normalizedRelativePath
            || PathHelpers.isProtectedDeleteTarget protectedDeleteTargetNames normalizedRelativePath
        then
            false
        else
            let segments = normalizedRelativePath |> splitPathSegments

            let isArcEntityFolder = segments.Length = 2 && (tryParseZone segments.[0]).IsSome

            let isSafeGenericDirectoryCandidate =
                isGenericFileSystemTargetAllowed normalizedRelativePath

            (isArcEntityFolder || isSafeGenericDirectoryCandidate)
            && (segments |> containsDisallowedGenericPathSegment |> not)
            && (PathHelpers.getFileName normalizedRelativePath |> isCanonicalArcFileName |> not)

    let resolveRenameSourcePath (relativePath: string) =
        match classifyRenameTarget relativePath with
        | RenamePathClassification.CanonicalEntityFileTarget(zone, identifier, _)
        | RenamePathClassification.CanonicalDataMapFileTarget(zone, identifier, _) ->
            canonicalEntityFolderPath zone identifier
        | RenamePathClassification.EntityFolderTarget(_, _, normalizedRelativePath)
        | RenamePathClassification.GenericTarget normalizedRelativePath
        | RenamePathClassification.AddZoneRootTarget(_, normalizedRelativePath)
        | RenamePathClassification.InvestigationFileTarget normalizedRelativePath
        | RenamePathClassification.DisallowedTarget normalizedRelativePath
        | RenamePathClassification.ProtectedTarget normalizedRelativePath -> normalizedRelativePath
        | RenamePathClassification.RootTarget -> ""

    let tryGetRenameEntityFolderTarget (relativePath: string) =
        match classifyRenameTarget relativePath with
        | RenamePathClassification.EntityFolderTarget(zone, identifier, _)
        | RenamePathClassification.CanonicalEntityFileTarget(zone, identifier, _)
        | RenamePathClassification.CanonicalDataMapFileTarget(zone, identifier, _) -> Some(zone, identifier)
        | _ -> None

    let buildCanonicalEntityPaths zone identifier = [
        canonicalEntityFilePath zone identifier
        canonicalDataMapFilePath zone identifier
    ]
