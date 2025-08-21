module Swate.Components.Contexts

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
module TermSearch =

    let TermSearchConfigCtx =
        React.createContext<TermSearchConfigCtx> ("TermSearchConfigCtx", TermSearchConfigCtx.init ())

    let TermSearchActiveKeysCtx =
        React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysCtx>> (
            "TermSearchActiveKeysCtx",
            {
                data = TermSearchConfigLocalStorageActiveKeysCtx.init ()
                setData = fun keys -> printfn "Setting active keys not given: %A" keys
            }
        )

    let TermSearchAllKeysCtx =
        React.createContext<Set<string>> ("TermSearchAllKeysCtx", Set.empty)