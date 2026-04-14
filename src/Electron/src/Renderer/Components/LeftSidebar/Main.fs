module Renderer.Components.LeftSidebar.Main

open Feliz
open Renderer.Types

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (leftSidebarTarget: LeftSidebarPage) =
    Html.div [
        // GitSidebar's virtualized changed-file list owns its own scroll viewport.
        // Give only that target a bounded, box-border host; keep other sidebars
        // content-sized so Layout.SidebarArea remains their scroll container.
        prop.className (Renderer.Components.LeftSidebar.MainStyles.wrapperClassName leftSidebarTarget)
        prop.children [|
            match leftSidebarTarget with
            | LeftSidebarPage.FileExplorer -> FileExplorerSidebar.Main()
            | LeftSidebarPage.ArcObjectExplorer -> ArcObjectTreeSidebar.Main()
            | LeftSidebarPage.Git -> GitSidebarPanel.Main()
        |]
    ]
