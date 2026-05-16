module Swate.Components.TermSearch.TermSearchAllKeysContext

open Feliz

let TermSearchAllKeysCtx = React.createContext<Set<string>>(Set.empty)

[<Hook>]
let useTermSearchAllKeysCtx () = React.useContext TermSearchAllKeysCtx