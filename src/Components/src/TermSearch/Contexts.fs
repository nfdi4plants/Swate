namespace Swate.Components.TermSearch

open Feliz
open Swate.Components.Types

type TermSearchConfigLocalStorageActiveKeysCtx = {
    disableDefault: bool
    aktiveKeys: string[]
} with

    static member init(?defaultActive: Set<string>) = {
        disableDefault = false
        aktiveKeys = defaultActive |> Option.map Set.toArray |> Option.defaultValue [||]
    }

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

module private StableContexts =

    let TermSearchConfigCtx =
        React.createContext<TermSearchConfigCtx> (TermSearchConfigCtx.init ())

    let TermSearchActiveKeysCtx =
        React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysCtx>> (
            {
                state = TermSearchConfigLocalStorageActiveKeysCtx.init ()
                setState = fun keys -> printfn "Setting active keys not given: %A" keys
            }
        )

    let TermSearchAllKeysCtx = React.createContext<Set<string>> (Set.empty)

type TermSearchConfigCtx with

    static member TermSearchConfigCtx =
        StableContexts.TermSearchConfigCtx

    [<Hook>]
    static member useTermSearchConfigCtx () = React.useContext TermSearchConfigCtx.TermSearchConfigCtx

    static member TermSearchActiveKeysCtx =
        StableContexts.TermSearchActiveKeysCtx

    [<Hook>]
    static member useTermSearchActiveKeysCtx () = React.useContext TermSearchConfigCtx.TermSearchActiveKeysCtx

    static member TermSearchAllKeysCtx = StableContexts.TermSearchAllKeysCtx

    [<Hook>]
    static member useTermSearchAllKeysCtx () = React.useContext TermSearchConfigCtx.TermSearchAllKeysCtx
