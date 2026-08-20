module Renderer.Components.FileExplorerDeleteHelper

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Types

[<RequireQualifiedAccess>]
module FileExplorerDeleteHelper =

    let isSelectionMissing (paths: string seq) (selectionPath: string option) =
        selectionPath |> Option.exists (PathHelpers.pathMatchesAny paths >> not)

    type private PageRefreshBehavior =
        | Keep
        | Reload
        | Reset

    let private pageRefreshBehavior =
        function
        | PageState.ArcFilePage _ -> Reset
        | PageState.MarkdownPage _
        | PageState.TextPage _
        | PageState.UnknownPage
        | PageState.ErrorPage _ -> Reload
        | _ -> Keep

    let shouldResetPageStateAfterSelectionRemoval (pageState: PageState option) =
        pageState
        |> Option.exists (
            pageRefreshBehavior
            >> function
                | Keep -> false
                | _ -> true
        )

    let private shouldReloadSelectedFile pageState (entry: FileEntry) =
        match (entry.lfs |> Option.map _.checkout), (pageState |> Option.map pageRefreshBehavior) with
        | Some false, _ -> false
        | _, Some Reload -> true
        | Some true, None -> true
        | _ -> false

    let private tryFindSelectedFileEntry (fileTree: FileEntry[]) (selectionPath: string option) =
        selectionPath
        |> Option.bind (fun selectedPath ->
            fileTree
            |> Array.tryFind (fun entry -> not entry.isDirectory && PathHelpers.pathsEqual entry.path selectedPath)
        )

    let shouldClearPageStateForLfsPointerSelection
        (fileTree: FileEntry[])
        (selectionPath: string option)
        (pageState: PageState option)
        =
        shouldResetPageStateAfterSelectionRemoval pageState
        && (tryFindSelectedFileEntry fileTree selectionPath
            |> Option.bind (fun entry -> entry.lfs |> Option.map _.checkout)
            |> Option.exists not)

    let tryGetReloadableSelectedFilePath
        (fileTree: FileEntry[])
        (selectionPath: string option)
        (pageState: PageState option)
        =
        tryFindSelectedFileEntry fileTree selectionPath
        |> Option.bind (fun entry ->
            if shouldReloadSelectedFile pageState entry then
                Some(PathHelpers.normalizePath entry.path)
            else
                None
        )
