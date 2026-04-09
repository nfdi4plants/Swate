module Renderer.Components.LeftSidebar.Main

open Feliz
open Swate.Electron.Shared
open Swate.Components
open Renderer.Types



/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main () =

    let leftSidebarCtx =
        React.useContext (Swate.Components.LayoutContext.LeftSidebarContext<LeftSidebarPage>)

    Html.div [
        prop.className "swt:p-4"
        prop.children [|
            match leftSidebarCtx.sidebarType with
            | LeftSidebarPage.FileExplorer -> FileExplorerSidebar.Main()
            | LeftSidebarPage.Git -> GitSidebarPanel.Main()
        |]
    ]
