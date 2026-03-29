namespace Swate.Components.Shared

open System

[<RequireQualifiedAccess>]
module PathHelpers =

    let normalizeSeparators (path: string) = path.Replace("\\", "/")

    let normalizePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.Trim().TrimEnd('/')

    let normalizeRelativePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.Trim('/').Trim()

    let normalizeForComparison (path: string) =
        normalizeSeparators path
        |> fun normalized -> normalized.Trim().TrimEnd('/').ToLowerInvariant()

    let pathsEqual (left: string) (right: string) =
        normalizeForComparison left = normalizeForComparison right

    let getNameFromPath (path: string) =
        normalizePath path
        |> fun normalized -> normalized.Split('/')
        |> Array.last

    let private tryGetParentPath (path: string) =
        let normalizedPath = normalizePath path
        let separatorIndex = normalizedPath.LastIndexOf('/')

        if separatorIndex < 0 then
            None
        else
            Some(normalizedPath.Substring(0, separatorIndex))

    let private tryResolveDatamapPreviewPath
        (normalizedPath: string)
        (folderSegment: string)
        (targetFileName: string)
        =
        let lowered = normalizedPath.ToLowerInvariant()

        if lowered.Contains(folderSegment) && lowered.EndsWith("/isa.datamap.xlsx") then
            tryGetParentPath normalizedPath
            |> Option.map (fun folderPath -> $"{folderPath}/{targetFileName}")
        else
            None

    let resolveArcPreviewPath (path: string) =
        let normalizedPath = normalizePath path

        [
            tryResolveDatamapPreviewPath normalizedPath "/assays/" "isa.assay.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "/studies/" "isa.study.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "/workflows/" "isa.workflow.xlsx"
            tryResolveDatamapPreviewPath normalizedPath "/runs/" "isa.run.xlsx"
        ]
        |> List.tryPick id
        |> Option.defaultValue normalizedPath
