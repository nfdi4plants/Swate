module Renderer.Components.LeftSidebar.Main

open Feliz
open Renderer.Types

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (leftSidebarTarget: LeftSidebarPage) =
    Html.div [
        prop.className [
            "swt:p-4"
            // The Git sidebar uses @tanstack/react-virtual for the changed-file list.
            // The virtualizer needs its scroll container to have a bounded height, which
            // requires CSS `height: 100%` on every ancestor between the fixed-height
            // Layout.SidebarPanel and the GitSidebar's flex column. Without this, the
            // scroll container never overflows and the virtualizer renders every item.
            if leftSidebarTarget = LeftSidebarPage.Git then "swt:h-full"
        ]
        prop.children [|
            match leftSidebarTarget with
            | LeftSidebarPage.FileExplorer -> FileExplorerSidebar.Main()
            | LeftSidebarPage.ArcObjectExplorer -> ArcObjectTreeSidebar.Main()
            | LeftSidebarPage.Git -> GitSidebarPanel.Main()
        |]
    ]
