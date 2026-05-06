namespace Swate.Components.TermSearch

open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.TermSearch.TermSearchConfigContext
open Swate.Components.TermSearch.TermSearchAllKeysContext
open Swate.Components.TermSearch.TermSearchActiveKeysContext


module private TermSearchConfigProviderHelper =

    [<Emit("Array.isArray($0)")>]
    let isJsArray (value: obj) : bool = jsNative

    [<Literal>]
    let TIB_PREFIX = "TIB_"

    [<Literal>]
    let TIB_DATAPLANT_COLLECTION_KEY = "DataPLANT"

    let sanitizeStringArray (arr: string[]) =
        if isNullOrUndefined (box arr) || not (isJsArray (box arr)) then
            [||]
        else
            arr
            |> Array.choose (fun item ->
                if isNullOrUndefined (box item) then
                    None
                else
                    let value = string item

                    if System.String.IsNullOrWhiteSpace value then
                        None
                    else
                        Some value
            )

    let sanitizeQueryList<'T> (queries: ResizeArray<string * 'T>) =
        if isNullOrUndefined (box queries) || not (isJsArray (box queries)) then
            ResizeArray()
        else
            queries
            |> Seq.choose (fun entry ->
                if isNullOrUndefined (box entry) || not (isJsArray (box entry)) then
                    None
                else
                    let key, value = entry

                    if isNullOrUndefined (box key) || isNullOrUndefined (box value) then
                        None
                    else
                        let key = string key

                        if System.String.IsNullOrWhiteSpace key then
                            None
                        else
                            Some(key, value)
            )
            |> ResizeArray

    let sanitizeActiveKeysState
        (fallbackState: TermSearchConfigLocalStorageActiveKeysContext)
        (state: TermSearchConfigLocalStorageActiveKeysContext)
        : TermSearchConfigLocalStorageActiveKeysContext * bool =
        if isNullOrUndefined (box state) then
            fallbackState, true
        else
            let hasDisableDefault = not (isNullOrUndefined (box state.disableDefault))

            let disableDefault =
                if hasDisableDefault then
                    state.disableDefault
                else
                    fallbackState.disableDefault

            let activeKeysRaw = state.activeKeys

            let hasActiveKeys =
                not (isNullOrUndefined (box activeKeysRaw)) && isJsArray (box activeKeysRaw)

            let activeKeys =
                if hasActiveKeys then
                    sanitizeStringArray activeKeysRaw
                else
                    fallbackState.activeKeys

            let wasFiltered = hasActiveKeys && activeKeys.Length <> activeKeysRaw.Length

            {
                disableDefault = disableDefault
                activeKeys = activeKeys
            },
            (not hasDisableDefault || not hasActiveKeys || wasFiltered)

    let mkTIBQueries (collections: Set<string>) = {|
        TermSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.SearchCall =
                        fun (q: string) -> Swate.Components.Api.TIBApi.TIBApi.defaultSearch (q, 10, c)

                    yield (n, query)
            ]
        ParentSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.ParentSearchCall =
                        fun (parent: string, query: string) ->
                            Swate.Components.Api.TIBApi.TIBApi.searchChildrenOf (query, parent, 10, c)

                    yield (n, query)
            ]
        AllChildrenSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.AllChildrenSearchCall =
                        fun (p: string) ->
                            Swate.Components.Api.TIBApi.TIBApi.searchAllChildrenOf (p, 300, collection = c)

                    yield (n, query)
            ]
    |}

open TermSearchConfigProviderHelper

[<Erase; Mangle(false)>]
type TermSearchConfigProvider =

    [<ReactComponent>]
    static member TIBQueryProvider(children: ReactElement) =

        let allTermSearchQueries, setAllTermSearchQueries =
            React.useState<ResizeArray<string * SearchCall>> (fun () -> ResizeArray())

        let allParentSearchQueries, setAllParentSearchQueries =
            React.useState<ResizeArray<string * ParentSearchCall>> (fun () -> ResizeArray())

        let allAllChildrenSearchQueries, setAllAllChildrenSearchQueries =
            React.useState<ResizeArray<string * AllChildrenSearchCall>> (fun () -> ResizeArray())

        React.useEffect (
            (fun _ -> // get all currently supported catalogues
                promise {
                    try
                        let! collections = Api.TIBApi.TIBApi.getCollections ()

                        let collectionSet =
                            collections.content |> Option.ofObj |> Option.defaultValue [||] |> Set.ofArray

                        let tibQueries = TermSearchConfigProviderHelper.mkTIBQueries collectionSet
                        setAllTermSearchQueries (ResizeArray tibQueries.TermSearch)
                        setAllParentSearchQueries (ResizeArray tibQueries.ParentSearch)
                        setAllAllChildrenSearchQueries (ResizeArray tibQueries.AllChildrenSearch)
                    with ex ->
                        console.error ("Error fetching TIB collections:", ex)
                }
                |> Promise.start
            ),
            [||]
        )

        TermSearchConfigProvider.TermSearchConfigProvider(
            children,
            allTermSearchQueries,
            allParentSearchQueries,
            allAllChildrenSearchQueries,
            defaultActive = Set [ TIB_PREFIX + TIB_DATAPLANT_COLLECTION_KEY ]
        )


    [<ReactComponent(true)>]
    static member TermSearchConfigProvider
        (
            children: ReactElement,
            allTermSearchQueries: ResizeArray<string * SearchCall>,
            allParentSearchQueries: ResizeArray<string * ParentSearchCall>,
            allAllChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>,
            ?defaultActive: Set<string>,
            ?localStorageKey: string
        ) =
        let localStorageKey = defaultArg localStorageKey "swate-termsearchconfig-ctx"

        let (activeKeys: TermSearchConfigLocalStorageActiveKeysContext), setActiveKeys =
            React.useLocalStorage (
                localStorageKey,
                TermSearchConfigLocalStorageActiveKeysContext.init (?defaultActive = defaultActive)
            )

        let defaultActiveKeysState =
            TermSearchConfigLocalStorageActiveKeysContext.init (?defaultActive = defaultActive)

        let sanitizedActiveKeys, shouldRepairActiveKeysState =
            TermSearchConfigProviderHelper.sanitizeActiveKeysState defaultActiveKeysState activeKeys

        React.useEffect (
            (fun () ->
                if shouldRepairActiveKeysState then
                    setActiveKeys sanitizedActiveKeys
            ),
            [|
                box shouldRepairActiveKeysState
                box sanitizedActiveKeys.disableDefault
                box (sanitizedActiveKeys.activeKeys |> String.concat ";")
            |]
        )

        let allTermSearchQueries =
            TermSearchConfigProviderHelper.sanitizeQueryList allTermSearchQueries

        let allParentSearchQueries =
            TermSearchConfigProviderHelper.sanitizeQueryList allParentSearchQueries

        let allAllChildrenSearchQueries =
            TermSearchConfigProviderHelper.sanitizeQueryList allAllChildrenSearchQueries

        let allKeys =
            React.useMemo (
                (fun () ->

                    let allKeys =
                        [
                            yield! allTermSearchQueries |> Seq.map fst
                            yield! allParentSearchQueries |> Seq.map fst
                            yield! allAllChildrenSearchQueries |> Seq.map fst
                        ]
                        |> Set.ofList

                    allKeys
                ),
                [|
                    box allTermSearchQueries
                    box allParentSearchQueries
                    box allAllChildrenSearchQueries
                |]
            )

        /// This is used for memoization
        let activeKeysString =
            match sanitizedActiveKeys.activeKeys with
            | [||] -> ""
            | keys -> keys |> Array.sort |> String.concat "; "

        let queries =
            React.useMemo (
                (fun () ->
                    let activeKeysSet = sanitizedActiveKeys.activeKeys |> Set.ofSeq

                    let termSearchQueries =
                        allTermSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeysSet |> Set.contains key)
                        |> ResizeArray

                    let parentSearchQueries =
                        allParentSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeysSet |> Set.contains key)
                        |> ResizeArray

                    let allChildrenSearchQueries =
                        allAllChildrenSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeysSet |> Set.contains key)
                        |> ResizeArray

                    {
                        hasProvider = true
                        disableDefault = sanitizedActiveKeys.disableDefault
                        termSearchQueries = termSearchQueries
                        parentSearchQueries = parentSearchQueries
                        allChildrenSearchQueries = allChildrenSearchQueries
                    }
                ),
                [|
                    activeKeysString
                    sanitizedActiveKeys.disableDefault
                    allTermSearchQueries
                    allParentSearchQueries
                    allAllChildrenSearchQueries
                |]
            )

        TermSearchActiveKeysContext.TermSearchActiveKeysCtx.Provider(
            {
                state = sanitizedActiveKeys
                setState = setActiveKeys
            },
            TermSearchConfigCtx.Provider(queries, TermSearchAllKeysCtx.Provider(allKeys, children))
        )