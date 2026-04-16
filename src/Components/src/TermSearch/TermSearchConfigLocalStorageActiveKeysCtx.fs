module Swate.Components.TermSearch.TermSearchConfigLocalStorageActiveKeysCtx


type TermSearchConfigLocalStorageActiveKeysCtx = {
    disableDefault: bool
    aktiveKeys: string[]
} with

    static member init(?defaultActive: Set<string>) = {
        disableDefault = false
        aktiveKeys = defaultActive |> Option.map Set.toArray |> Option.defaultValue [||]
    }
