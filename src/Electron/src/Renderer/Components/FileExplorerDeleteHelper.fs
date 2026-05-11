module Renderer.Components.FileExplorerDeleteHelper

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

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

    let shouldResetPageStateAfterSelectionRemoval (pageState: Renderer.Types.PageState option) =
        match pageState with
        | Some(Renderer.Types.PageState.ArcFilePage _)
        | Some(Renderer.Types.PageState.TextPage _)
        | Some Renderer.Types.PageState.UnknownPage
        | Some(Renderer.Types.PageState.ErrorPage _) -> true
        | _ -> false
