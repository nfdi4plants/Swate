module Swate.Components.TermSearch.TermSearchConfigCtx

open Feliz
open Swate.Components.Types
open Swate.Components.TermSearch.TermSearchConfigLocalStorageActiveKeysCtx

type TermSearchConfigCtx = {
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

let termSearchConfigCtx =
    React.createContext<TermSearchConfigCtx> (TermSearchConfigCtx.init ())

let termSearchActiveKeysCtx =
    React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysCtx>> (
        {
            state = TermSearchConfigLocalStorageActiveKeysCtx.init ()
            setState = fun keys -> printfn "Setting active keys not given: %A" keys
        }
    )

let TermSearchAllKeysCtx = React.createContext<Set<string>>(Set.empty)

[<Hook>]
let useTermSearchConfigCtx () = React.useContext termSearchConfigCtx

[<Hook>]
let useTermSearchActiveKeysCtx () = React.useContext termSearchActiveKeysCtx

let termSearchAllKeysCtx = TermSearchAllKeysCtx

[<Hook>]
let useTermSearchAllKeysCtx () = React.useContext TermSearchAllKeysCtx
