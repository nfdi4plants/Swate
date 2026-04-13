module Swate.Components.ErrorModal.Context


open Feliz
open Swate.Components


let ErrorModalCtx = React.createContext<ErrorModalContext> (ErrorModalContext.Empty)

[<Hook>]
let useErrorModal () = React.useContext ErrorModalCtx