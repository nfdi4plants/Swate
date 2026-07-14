namespace Swate.Components.Composite.TermSearch

open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Primitive
open Swate.Components.Composite.TermSearch.Context
open Swate.Components.Composite.TermSearch.Types

module private TermSearchConfigProviderHelper =

    type QueryCollection = {
        TermSearch: ResizeArray<string * SearchCall>
        ParentSearch: ResizeArray<string * ParentSearchCall>
        AllChildrenSearch: ResizeArray<string * AllChildrenSearchCall>
    }

    module QueryCollection =

        let empty () = {
            TermSearch = ResizeArray()
            ParentSearch = ResizeArray()
            AllChildrenSearch = ResizeArray()
        }

        let private appendItems (first: ResizeArray<'Item>) (second: ResizeArray<'Item>) =
            let result = ResizeArray first
            result.AddRange second
            result

        let append (first: QueryCollection) (second: QueryCollection) = {
            TermSearch = appendItems first.TermSearch second.TermSearch
            ParentSearch = appendItems first.ParentSearch second.ParentSearch
            AllChildrenSearch = appendItems first.AllChildrenSearch second.AllChildrenSearch
        }

    [<Literal>]
    let TIB_PREFIX = "TIB_"

    [<Literal>]
    let TIB_DATAPLANT_COLLECTION_KEY = "DataPLANT"

    [<Literal>]
    let OLS_PREFIX = "OLS_"

    [<Literal>]
    let OLS_TS4NFDI_KEY = "TS4NFDI"

    let OLS_DEFAULT_KEY = OLS_PREFIX + OLS_TS4NFDI_KEY

    let private normalizeTerms rows (terms: Term[]) =
        terms
        |> Array.distinctBy (fun term -> term.id, term.href, term.name)
        |> Array.truncate rows

    let private mapSearchResults (convert: 'Api -> Term[]) (request: JS.Promise<'Api option>) =
        request
        |> Promise.map (fun response -> response |> Option.map convert |> Option.defaultValue [||] |> ResizeArray)

    let mkTIBQueries (collections: Set<string>) = {
        TermSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: SearchCall =
                        fun (q: string) ->
                            Swate.Components.Api.TIBApi.TIBApi.defaultSearch (q, 10, c)
                            |> mapSearchResults _.ToMyTerm()

                    yield (n, query)
            ]
        ParentSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: ParentSearchCall =
                        fun (parent: string, query: string) ->
                            Swate.Components.Api.TIBApi.TIBApi.searchChildrenOf (query, parent, 10, c)
                            |> mapSearchResults _.ToMyTerm()

                    yield (n, query)
            ]
        AllChildrenSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: AllChildrenSearchCall =
                        fun (p: string) ->
                            Swate.Components.Api.TIBApi.TIBApi.searchAllChildrenOf (p, 300, collection = c)
                            |> mapSearchResults _.ToMyTerm()

                    yield (n, query)
            ]
    }

    let mkOLSQueries () = {
        TermSearch =
            ResizeArray [
                let n = OLS_DEFAULT_KEY
                let rows = 10

                let query: SearchCall =
                    fun (q: string) ->
                        Swate.Components.Api.OLSApi.OLSApi.defaultSearch (q, rows)
                        |> mapSearchResults (fun api -> api.ToMyTerm() |> normalizeTerms rows)

                yield (n, query)
            ]
        ParentSearch = ResizeArray<string * ParentSearchCall>()
        AllChildrenSearch = ResizeArray<string * AllChildrenSearchCall>()
    }

open TermSearchConfigProviderHelper

[<Erase; Mangle(false)>]
type TermSearchConfigProvider =

    [<ReactComponent>]
    static member private QueryProvider(children: ReactElement, includeOLS: bool) =

        let configuredQueries =
            React.useMemo (
                (fun () ->
                    if includeOLS then
                        TermSearchConfigProviderHelper.mkOLSQueries ()
                    else
                        QueryCollection.empty ()
                ),
                [| box includeOLS |]
            )

        let allTermSearchQueries, setAllTermSearchQueries =
            React.useState<ResizeArray<string * SearchCall>> (fun () -> ResizeArray configuredQueries.TermSearch)

        let allParentSearchQueries, setAllParentSearchQueries =
            React.useState<ResizeArray<string * ParentSearchCall>> (fun () ->
                ResizeArray configuredQueries.ParentSearch
            )

        let allAllChildrenSearchQueries, setAllAllChildrenSearchQueries =
            React.useState<ResizeArray<string * AllChildrenSearchCall>> (fun () ->
                ResizeArray configuredQueries.AllChildrenSearch
            )

        let applyQueries (queries: QueryCollection) =
            setAllTermSearchQueries (ResizeArray queries.TermSearch)
            setAllParentSearchQueries (ResizeArray queries.ParentSearch)
            setAllAllChildrenSearchQueries (ResizeArray queries.AllChildrenSearch)

        React.useEffect (
            (fun _ -> // get all currently supported catalogues
                promise {
                    try
                        let! collections =
                            Api.TIBApi.TIBApi.getCollections ()
                            |> Promise.catch (fun ex -> failwithf "Error fetching TIB collections: %s" ex.Message)

                        let collectionSet = Set.ofArray collections.content

                        let tibQueries = TermSearchConfigProviderHelper.mkTIBQueries collectionSet

                        QueryCollection.append tibQueries configuredQueries |> applyQueries
                    with ex ->
                        console.error ("Error fetching TIB collections:", ex)
                }
                |> Promise.start
            ),
            [| box includeOLS |]
        )

        TermSearchConfigProvider.TermSearchConfigProvider(
            children,
            allTermSearchQueries,
            allParentSearchQueries,
            allAllChildrenSearchQueries,
            defaultActive = Set [ TIB_PREFIX + TIB_DATAPLANT_COLLECTION_KEY ]
        )

    [<ReactComponent>]
    static member TIBQueryProvider(children: ReactElement) =
        TermSearchConfigProvider.QueryProvider(children, false)

    [<ReactComponent>]
    static member DefaultQueryProvider(children: ReactElement) =
        TermSearchConfigProvider.QueryProvider(children, true)


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
        let defaultActive = defaultArg defaultActive Set.empty

        let (activeKeys: TermSearchActiveKeysContext), setActiveKeys =
            React.useLocalStorage (localStorageKey, TermSearchActiveKeysContext.init (defaultActive))

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
        let activeKeysString = activeKeys.activeKeys |> Array.sort |> String.concat "; "

        let queries =
            React.useMemo (
                (fun () ->

                    let termSearchQueries =
                        allTermSearchQueries
                        |> Seq.filter (fun (key, _) ->
                            let isActive = activeKeys.activeKeys |> Set.ofSeq |> Set.contains key

                            isActive
                        )
                        |> ResizeArray

                    let parentSearchQueries =
                        allParentSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeys.activeKeys |> Set.ofSeq |> Set.contains key)
                        |> ResizeArray

                    let allChildrenSearchQueries =
                        allAllChildrenSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeys.activeKeys |> Set.ofSeq |> Set.contains key)
                        |> ResizeArray

                    {
                        hasProvider = true
                        disableDefault = activeKeys.disableDefault
                        termSearchQueries = termSearchQueries
                        parentSearchQueries = parentSearchQueries
                        allChildrenSearchQueries = allChildrenSearchQueries
                    }
                ),
                [|
                    activeKeysString
                    activeKeys.disableDefault
                    allTermSearchQueries
                    allParentSearchQueries
                    allAllChildrenSearchQueries
                |]
            )

        ActiveKeysContext.TermSearchActiveKeysCtx.Provider(
            {
                state = activeKeys
                setState = setActiveKeys
            },
            TermSearchConfigCtx.Provider(queries, TermSearchAllKeysCtx.Provider(allKeys, children))
        )
