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

    static member TermSearchConfigCtx =
        React.createContext<TermSearchConfigCtx> (TermSearchConfigCtx.init ())

    [<Hook>]
    static member useTermSearchConfigCtx () = React.useContext TermSearchConfigCtx.TermSearchConfigCtx

    static member TermSearchActiveKeysCtx =
        React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysCtx>> (
            {
                state = TermSearchConfigLocalStorageActiveKeysCtx.init ()
                setState = fun keys -> printfn "Setting active keys not given: %A" keys
            }
        )

    [<Hook>]
    static member useTermSearchActiveKeysCtx () = React.useContext TermSearchConfigCtx.TermSearchActiveKeysCtx

    static member TermSearchAllKeysCtx = React.createContext<Set<string>> (Set.empty)

    [<Hook>]
    static member useTermSearchAllKeysCtx () = React.useContext TermSearchConfigCtx.TermSearchAllKeysCtx
