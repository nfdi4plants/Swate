module Renderer.Components.LeftSidebar.Main

open Feliz

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main (workspaceMode: Renderer.Types.WorkspaceMode) =
    Html.div [
        prop.className "swt:p-4"
        prop.children [| ExplorerSidebar.Main(workspaceMode) |]
    ]
