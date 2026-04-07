module Renderer.Components.LeftSidebar.FileExplorerSidebar

open Feliz
open Swate.Components
open Swate.Components.Shared
open Renderer.Types

[<ReactComponent>]
let Main (explorerMode: LeftSidebarPage) =

    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()

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
                    |],
                    3
                )
            ]
        ]
        match explorerMode with
        | LeftSidebarPage.FileExplorer -> Renderer.Components.FileExplorer.FileTree()
        | LeftSidebarPage.ArcObjectTree -> Renderer.Components.LeftSidebar.ArcObjectTreeSidebar.Main()
    ]
