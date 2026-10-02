module Renderer.Components.Helper.FileExplorerStateReconciliation

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Types
open Swate.Components.Page.ArcFileEditor.Types

let containsPath (paths: string seq) (relativePath: string) =
    let normalizedTargetPath = PathHelpers.normalizePath relativePath

    paths
    |> Seq.exists (fun path -> PathHelpers.pathsEqual (PathHelpers.normalizePath path) normalizedTargetPath)

let isSelectionMissing (paths: string seq) (selectionPath: string option) =
    selectionPath
    |> Option.map PathHelpers.normalizePath
    |> Option.exists (fun selectedPath -> containsPath paths selectedPath |> not)

let isSelectionMissingWithLookup (tryFindEntry: string -> FileEntry option) (selectionPath: string option) =
    selectionPath
    |> Option.map PathHelpers.normalizePath
    |> Option.exists (tryFindEntry >> Option.isNone)

let private resetsWhenSelectionIsRemoved =
    function
    | PageState.ArcFilePage _
    | PageState.MarkdownPage _
    | PageState.TextPage _
    | PageState.UnknownPage
    | PageState.ErrorPage _ -> true
    | _ -> false

let shouldResetPageStateAfterSelectionRemoval (pageState: PageState option) =
    pageState |> Option.exists resetsWhenSelectionIsRemoved

let tryGetDataMapMismatchReloadWithLookup (tryFindEntry: string -> FileEntry option) (pageState: PageState option) =
    match pageState with
    | Some(PageState.ArcFilePage(ArcFiles.DataMap _, _)) -> None
    | Some(PageState.ArcFilePage(arcFile, requestedView)) ->
        match arcFile.TryGetDataMapParentInfo() with
        | Some parentInfo ->
            let treeHasDataMap =
                DatamapParentInfo.toPath parentInfo
                |> PathHelpers.normalizePath
                |> tryFindEntry
                |> Option.isSome

            let pageHasDataMap = arcFile.TryGetDataMap().IsSome

            if treeHasDataMap = pageHasDataMap then
                None
            else
                arcFile.TryGetRelativePath()
                |> Option.map (fun parentPath ->
                    let nextRequestedView =
                        match treeHasDataMap, requestedView with
                        | false, Some ActiveView.DataMap -> Some ActiveView.Metadata
                        | _ -> requestedView

                    PathHelpers.normalizePath parentPath, nextRequestedView
                )
        | None -> None
    | _ -> None

let tryGetDataMapMismatchReload (fileTree: FileEntry[]) (pageState: PageState option) =
    let tryFindEntry path =
        fileTree
        |> Array.tryFind (fun entry -> PathHelpers.pathsEqual (PathHelpers.normalizePath entry.path) path)

    tryGetDataMapMismatchReloadWithLookup tryFindEntry pageState

let private reloadsWhenSelectedFileChanges =
    function
    | PageState.MarkdownPage _
    | PageState.TextPage _
    | PageState.UnknownPage
    | PageState.ErrorPage _ -> true
    | _ -> false

let private isCheckedOutLfsFile (entry: FileEntry) =
    entry.largeObject |> Option.exists (fun state -> state.IsMaterialized)

let private isPointerLfsFile (entry: FileEntry) =
    entry.largeObject |> Option.exists (fun state -> not state.IsMaterialized)

let private shouldReloadSelectedFile pageState entry =
    if isPointerLfsFile entry then
        false
    else
        match pageState with
        | Some state -> reloadsWhenSelectedFileChanges state
        | None -> isCheckedOutLfsFile entry

let private tryFindSelectedFileEntryWithLookup
    (tryFindEntry: string -> FileEntry option)
    (selectionPath: string option)
    =
    selectionPath
    |> Option.map PathHelpers.normalizePath
    |> Option.bind tryFindEntry
    |> Option.filter (fun entry -> not entry.isDirectory)

let private fileTreeLookup (fileTree: FileEntry[]) path =
    fileTree
    |> Array.tryFind (fun entry -> PathHelpers.pathsEqual (PathHelpers.normalizePath entry.path) path)

let shouldClearPageStateForLfsPointerSelectionWithLookup
    (tryFindEntry: string -> FileEntry option)
    (selectionPath: string option)
    (pageState: PageState option)
    =
    pageState |> Option.exists resetsWhenSelectionIsRemoved
    && (tryFindSelectedFileEntryWithLookup tryFindEntry selectionPath
        |> Option.exists isPointerLfsFile)

let shouldClearPageStateForLfsPointerSelection
    (fileTree: FileEntry[])
    (selectionPath: string option)
    (pageState: PageState option)
    =
    shouldClearPageStateForLfsPointerSelectionWithLookup (fileTreeLookup fileTree) selectionPath pageState

let tryGetReloadableSelectedFilePathWithLookup
    (tryFindEntry: string -> FileEntry option)
    (selectionPath: string option)
    (pageState: PageState option)
    =
    tryFindSelectedFileEntryWithLookup tryFindEntry selectionPath
    |> Option.bind (fun entry ->
        if shouldReloadSelectedFile pageState entry then
            Some(PathHelpers.normalizePath entry.path)
        else
            None
    )
