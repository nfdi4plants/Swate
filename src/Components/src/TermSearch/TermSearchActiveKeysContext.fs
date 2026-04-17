module Swate.Components.TermSearch.TermSearchActiveKeysContext

open Feliz
open Swate.Components.Types

type TermSearchConfigLocalStorageActiveKeysContext = {
    disableDefault: bool
    activeKeys: string[]
} with

    static member init(?defaultActive: Set<string>) = {
        disableDefault = false
        activeKeys = defaultActive |> Option.map Set.toArray |> Option.defaultValue [||]
    }

let TermSearchActiveKeysCtx =
    React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysContext>> (
        {
            state = TermSearchConfigLocalStorageActiveKeysContext.init ()
            setState = fun keys -> printfn "Setting active keys not given: %A" keys
        }
    )

[<Hook>]
let useTermSearchActiveKeysCtx () = React.useContext TermSearchActiveKeysCtx