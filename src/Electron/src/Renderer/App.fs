module Renderer.App

open Feliz
open Swate.Components

[<ReactComponent>]
let Main () = Components.Layout.Layout.Entry()