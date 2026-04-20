module Swate.Components.TermSearch.TermSearchConfigContext

open Feliz
open Swate.Components.Types

type TermSearchConfigContext = {
    hasProvider: bool
    disableDefault: bool
    termSearchQueries: ResizeArray<string * SearchCall>
    parentSearchQueries: ResizeArray<string * ParentSearchCall>
    allChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>
} with

    static member init() = {
        hasProvider = false
        disableDefault = false
        termSearchQueries = ResizeArray()
        parentSearchQueries = ResizeArray()
        allChildrenSearchQueries = ResizeArray()
    }

let TermSearchConfigCtx =
    React.createContext<TermSearchConfigContext>(TermSearchConfigContext.init ())

[<Hook>]
let useTermSearchConfigCtx () = React.useContext TermSearchConfigCtx
