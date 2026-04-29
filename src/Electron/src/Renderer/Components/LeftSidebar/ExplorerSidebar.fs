module Renderer.Components.LeftSidebar.FileExplorerSidebar

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

    Html.div [
        prop.testId "left-sidebar-file-explorer"
        prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col"
        prop.children [
            Html.div [
                prop.testId "left-sidebar-file-explorer-toolbar"
                prop.className "swt:mb-2 swt:flex swt:shrink-0 swt:justify-center swt:bg-base-100"
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
            Html.div [
                prop.testId "left-sidebar-file-explorer-tree"
                prop.className
                    "swt:flex-1 swt:min-h-0 swt:overflow-y-auto swt:overflow-x-auto swt:[scrollbar-gutter:stable]"
                prop.children [ Renderer.Components.FileExplorer.FileTree() ]
            ]
        ]
    ]
