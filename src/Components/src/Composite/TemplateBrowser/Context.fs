module Swate.Components.Template.Context

open ARCtrl
open Feliz
open Swate.Components.Types
open Swate.Components.Primitives

let FilteredTemplateCtx =
    React.createContext<StateContext<Template[]>> ({ state = [||]; setState = fun _ -> () })

[<Hook>]
let useFilteredTemplateCtx () = React.useContext FilteredTemplateCtx
