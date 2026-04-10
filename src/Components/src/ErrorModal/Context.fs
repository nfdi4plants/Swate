module Swate.Components.Context

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
module ErrorModal =

    let ErrorModalCtx = React.createContext<ErrorModalContext> (ErrorModalContext.Empty)

    [<Hook>]
    let useErrorModal () = React.useContext ErrorModalCtx