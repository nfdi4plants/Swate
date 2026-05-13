module Renderer.Components.FileExplorerDeleteHelper

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

[<RequireQualifiedAccess>]
module FileExplorerDeleteHelper =

    let private normalizeRelativePath (path: string) =
        path
        |> PathHelpers.normalizeRelativePath
        |> PathHelpers.normalizePath


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

    let private shouldReloadPageStateAfterSelectedFileUpdate (pageState: Renderer.Types.PageState option) =
        match pageState with
        | Some(Renderer.Types.PageState.TextPage _)
        | Some Renderer.Types.PageState.UnknownPage
        | Some(Renderer.Types.PageState.ErrorPage _) -> true
        | _ -> false

    let tryGetReloadableSelectedFilePath
        (fileTree: FileEntry[])
        (selectionPath: string option)
        (pageState: Renderer.Types.PageState option)
        =
        if shouldReloadPageStateAfterSelectedFileUpdate pageState |> not then
            None
        else
            selectionPath
            |> Option.map PathHelpers.normalizePath
            |> Option.bind (fun selectedPath ->
                fileTree
                |> Array.tryFind (fun entry ->
                    not entry.isDirectory
                    && PathHelpers.pathsEqual (PathHelpers.normalizePath entry.path) selectedPath
                )
                |> Option.map (fun entry -> PathHelpers.normalizePath entry.path)
            )

    let isPendingPathAffectedByDelete (deletedPath: string) (pendingPath: string option) =
        let normalizedDeletedPath = normalizeRelativePath deletedPath

        pendingPath
        |> Option.map normalizeRelativePath
        |> Option.exists (fun normalizedPendingPath ->
            isSameOrDescendantPath normalizedPendingPath normalizedDeletedPath
        )
