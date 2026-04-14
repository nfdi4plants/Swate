module Swate.Components.TemplateContext

open ARCtrl
open Feliz
open Swate.Components.Types

let FilteredTemplateCtx =
    React.createContext<StateContext<Template[]>> ({ state = [||]; setState = fun _ -> () })

[<Hook>]
let useFilteredTemplateCtx () = React.useContext FilteredTemplateCtx
