module Renderer.Components.FileExplorerDeleteHelper

open System
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

[<RequireQualifiedAccess>]
module FileExplorerDeleteHelper =

    let private normalizeRelativePath (path: string) =
        path
        |> PathHelpers.normalizeRelativePath
        |> PathHelpers.normalizePath

    let private addZoneRoots = [ "studies"; "assays"; "workflows"; "runs" ]

    let private protectedDeleteTargetNames = [ ".gitkeep"; "readme.md" ]

    let private isProtectedDeleteTarget (normalizedPath: string) =
        normalizedPath
        |> PathHelpers.getFileName
        |> PathHelpers.pathMatchesAny protectedDeleteTargetNames

    let isDeletePathAllowed (relativePath: string) =
        let normalizedPath = normalizeRelativePath relativePath

        if String.IsNullOrWhiteSpace normalizedPath then
            false
        else
            let segments =
                normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries)

            segments.Length >= 2
            && PathHelpers.pathMatchesAny addZoneRoots segments.[0]
            && not (isProtectedDeleteTarget normalizedPath)

    let containsPath (paths: string seq) (relativePath: string) =
        let normalizedTargetPath = PathHelpers.normalizePath relativePath

        paths
        |> Seq.exists (fun path -> PathHelpers.pathsEqual (PathHelpers.normalizePath path) normalizedTargetPath)

    let isSelectionMissing (paths: string seq) (selectionPath: string option) =
        selectionPath
        |> Option.map PathHelpers.normalizePath
        |> Option.exists (fun selectedPath -> containsPath paths selectedPath |> not)

    let shouldResetPageStateAfterSelectionRemoval (pageState: Renderer.Types.PageState option) =
        match pageState with
        | Some(Renderer.Types.PageState.ArcFilePage _)
        | Some(Renderer.Types.PageState.TextPage _)
        | Some Renderer.Types.PageState.UnknownPage
        | Some(Renderer.Types.PageState.ErrorPage _) -> true
        | _ -> false

    let isPendingPathAffectedByDelete (deletedPath: string) (pendingPath: string option) =
        let normalizedDeletedPath = normalizeRelativePath deletedPath

        pendingPath
        |> Option.map normalizeRelativePath
        |> Option.exists (fun normalizedPendingPath ->
            isSameOrDescendantPath normalizedPendingPath normalizedDeletedPath
        )
