module Swate.Components.Page.CwlEditor.Context

open Feliz
open Swate.Components.Page.CwlEditor.Types

let CwlEditorHostCtx = React.createContext<CwlEditorHost option> (None)

[<Hook>]
let useCwlEditorHostCtx () = React.useContext CwlEditorHostCtx
