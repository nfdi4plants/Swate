module Renderer.Components.LeftSidebar.Main

open Feliz
open Renderer.Types

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (leftSidebarTarget: LeftSidebarPage) =
    Html.div [
        prop.className [
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
        prop.children [|
            match leftSidebarTarget with
            | LeftSidebarPage.FileExplorer -> Renderer.Components.LeftSidebar.FileExplorer.Main.Main()
            | LeftSidebarPage.Git -> GitSidebarPanel.Main()
        |]
    ]
