module Renderer.Components.FileExplorerDeleteHelper

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Renderer.Types

[<RequireQualifiedAccess>]
module FileExplorerDeleteHelper =

    let containsPath (paths: string seq) (relativePath: string) =
        let normalizedTargetPath = PathHelpers.normalizePath relativePath

        paths
        |> Seq.exists (fun path -> PathHelpers.pathsEqual (PathHelpers.normalizePath path) normalizedTargetPath)

    let isSelectionMissing (paths: string seq) (selectionPath: string option) =
        selectionPath
        |> Option.map PathHelpers.normalizePath
        |> Option.exists (fun selectedPath -> containsPath paths selectedPath |> not)

    let private resetsWhenSelectionIsRemoved =
        function
        | PageState.ArcFilePage _
        | PageState.MarkdownPage _
        | PageState.TextPage _
        | PageState.UnknownPage
        | PageState.ErrorPage _
        | PageState.GitLfsPointerPage _ -> true
        | _ -> false

    let shouldResetPageStateAfterSelectionRemoval (pageState: PageState option) =
        pageState |> Option.exists resetsWhenSelectionIsRemoved

    let private reloadsWhenSelectedFileChanges =
        function
        | PageState.MarkdownPage _
        | PageState.TextPage _
        | PageState.UnknownPage
        | PageState.ErrorPage _ -> true
        | _ -> false

    let tryGetReloadableSelectedFilePath
        (fileTree: FileEntry[])
        (selectionPath: string option)
        (pageState: PageState option)
        =
        pageState
        |> Option.filter reloadsWhenSelectedFileChanges
        |> Option.bind (fun _ ->
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
        )
