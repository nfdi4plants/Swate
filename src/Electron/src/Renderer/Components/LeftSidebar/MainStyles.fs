module Renderer.Components.LeftSidebar.MainStyles

open Renderer.Types

let wrapperClassName (leftSidebarTarget: LeftSidebarPage) =
    match leftSidebarTarget with
    | LeftSidebarPage.Git ->
        [
            "swt:box-border"
            "swt:flex"
            "swt:h-full"
            "swt:min-h-0"
            "swt:min-w-0"
            "swt:max-w-full"
            "swt:flex-col"
            "swt:overflow-hidden"
            "swt:p-4"
        ]
    | LeftSidebarPage.FileExplorer
    | LeftSidebarPage.ArcObjectExplorer ->
        [ "swt:p-4" ]
