module Renderer.Components.LeftSidebar.FileExplorerSidebar

open Feliz
open Swate.Electron.Shared
open Swate.Components
open Renderer.Types

[<ReactComponent>]
let Main (explorerMode: ExplorerMode, setExplorerMode: ExplorerMode -> unit) =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()

    let toggleExplorerMode () =
        match explorerMode with
        | ExplorerMode.NormalFileTree -> setExplorerMode ExplorerMode.ArcObjectTree
        | ExplorerMode.ArcObjectTree -> setExplorerMode ExplorerMode.NormalFileTree

    React.Fragment [

        Html.div [
            prop.className "swt:mb-2 swt:flex swt:justify-center"
            prop.children [
                Swate.Components.Actionbar.Main(
                    [|
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-bullet-list-24-regular swt:size-5",
                            "Lab book view",
                            fun _ -> pageStateCtx.setState (Some PageState.LandingDraftPage)
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-24-regular swt:size-5",
                            "Create Note",
                            fun _ -> pageStateCtx.setState (Some PageState.NotesDraftPage)
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--search-24-regular swt:size-5",
                            "Note Search",
                            fun _ -> pageStateCtx.setState (Some PageState.NotesSearchPage)
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--database-24-regular swt:size-5",
                            (match explorerMode with
                             | ExplorerMode.NormalFileTree -> "Show ARC object explorer"
                             | ExplorerMode.ArcObjectTree -> "Show normal file tree"),
                            fun _ -> toggleExplorerMode ()
                        )
                    |],
                    4
                )
            ]
        ]
        match explorerMode with
        | ExplorerMode.NormalFileTree -> Renderer.Components.FileExplorer.FileTree()
        | ExplorerMode.ArcObjectTree -> Renderer.Components.LeftSidebar.ArcObjectTreeSidebar.Main()
    ]
