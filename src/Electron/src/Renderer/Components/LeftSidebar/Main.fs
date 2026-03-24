module Renderer.Components.LeftSidebar.Main

open Feliz
open Swate.Electron.Shared

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactComponent>]
let Main () = Html.none