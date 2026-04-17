module Renderer.Components.LeftSidebar.FileExplorerSidebar

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

    React.Fragment [
        Html.div [
            prop.className "swt:mb-2 swt:flex swt:justify-center"
            prop.children [
                Swate.Components.Actionbar.Main(
                    [|
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-bullet-list-24-regular swt:size-5",
                            "Lab book view",
                            fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.LandingDraftPage)
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--document-24-regular swt:size-5",
                            "Create Note",
                            fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.NotesDraftPage)
                        )
                        Actionbar.ButtonInfo.create (
                            "swt:fluent--search-24-regular swt:size-5",
                            "Note Search",
                            fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.NotesSearchPage)
                        )
                    |],
                    3
                )
            ]
        ]
        Renderer.Components.FileExplorer.FileTree()
    ]
