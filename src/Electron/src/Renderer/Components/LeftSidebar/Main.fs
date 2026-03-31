module Renderer.Components.LeftSidebar.Main

open Feliz

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (explorerMode: Renderer.Types.LeftSidebarPage, setExplorerMode: Renderer.Types.LeftSidebarPage -> unit) =
    Html.div [
        prop.className "swt:p-4"
        prop.children [| FileExplorerSidebar.Main(explorerMode, setExplorerMode) |]
    ]
