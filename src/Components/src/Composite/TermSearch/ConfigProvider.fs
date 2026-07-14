namespace Swate.Components.Composite.TermSearch

open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Primitive
open Swate.Components.Composite.TermSearch.Context
open Swate.Components.Composite.TermSearch.Types

module private TermSearchConfigProviderHelper =

    type SearchCollection = {
        Key: string
        TermSearch: SearchCall
        ParentSearch: ParentSearchCall option
        AllChildrenSearch: AllChildrenSearchCall option
    }

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

        let create (collections: seq<SearchCollection>) =
            let queries = empty ()

            for collection in collections do
                queries.TermSearch.Add(collection.Key, collection.TermSearch)

                collection.ParentSearch
                |> Option.iter (fun search -> queries.ParentSearch.Add(collection.Key, search))

                collection.AllChildrenSearch
                |> Option.iter (fun search -> queries.AllChildrenSearch.Add(collection.Key, search))

            queries

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
    let TIB_DATAPLANT_COLLECTION_KEY = "DataPLANT"

    [<Literal>]
    let OLS_DATAPLANT_COLLECTION_KEY = "DataPLANT Project"

    [<Literal>]
    let LOCAL_STORAGE_KEY = "swate-termsearchconfig-ctx-v3"

    [<Literal>]
    let DEFAULT_SEARCH_ROWS = 10

    [<Literal>]
    let ALL_CHILDREN_SEARCH_ROWS = 300

    let private normalizeTerms rows (terms: Term[]) =
        terms
        |> Array.distinctBy (fun term -> term.id, term.href, term.name)
        |> Array.truncate rows

    let private mapSearchResults rows (convert: 'Api -> Term[]) (request: JS.Promise<'Api option>) =
        request
        |> Promise.map (fun response ->
            response
            |> Option.map convert
            |> Option.defaultValue [||]
            |> normalizeTerms rows
            |> ResizeArray
        )

    let private createTIBCollection collection = {
        Key = TermSearchSourceKey.create TermSearchSource.TIB collection
        TermSearch =
            fun query ->
                Api.TIBApi.TIBApi.defaultSearch (query, DEFAULT_SEARCH_ROWS, collection)
                |> mapSearchResults DEFAULT_SEARCH_ROWS _.ToMyTerm()
        ParentSearch =
            Some(fun (parent, query) ->
                Api.TIBApi.TIBApi.searchChildrenOf (query, parent, DEFAULT_SEARCH_ROWS, collection)
                |> mapSearchResults DEFAULT_SEARCH_ROWS _.ToMyTerm()
            )
        AllChildrenSearch =
            Some(fun parent ->
                Api.TIBApi.TIBApi.searchAllChildrenOf (parent, ALL_CHILDREN_SEARCH_ROWS, collection = collection)
                |> mapSearchResults ALL_CHILDREN_SEARCH_ROWS _.ToMyTerm()
            )
    }

    let mkTIBQueries collections =
        collections |> Seq.map createTIBCollection |> QueryCollection.create

    let private tryCreateOLSCollection (collection: Api.OLSApi.OLSTypes.Collection) =
        match collection.id with
        | Some collectionId when not (System.String.IsNullOrWhiteSpace collection.label) ->
            Some {
                Key = TermSearchSourceKey.create TermSearchSource.OLS collection.label
                TermSearch =
                    fun query ->
                        Api.OLSApi.OLSApi.defaultSearch (query, DEFAULT_SEARCH_ROWS, collectionId = collectionId)
                        |> mapSearchResults DEFAULT_SEARCH_ROWS _.ToMyTerm()
                ParentSearch = None
                AllChildrenSearch = None
            }
        | _ -> None

    let mkOLSQueries collections =
        collections |> Seq.choose tryCreateOLSCollection |> QueryCollection.create

    let loadCollections source request toQueries setQueries = promise {
        try
            let! response = request ()
            response |> toQueries |> setQueries
        with ex ->
            console.error ($"Error fetching {source} collections:", ex)
    }

open TermSearchConfigProviderHelper

[<Erase; Mangle(false)>]
type TermSearchConfigProvider =

    [<ReactComponent>]
    static member private QueryProvider(children: ReactElement, includeOLS: bool) =

        let tibQueries, setTIBQueries =
            React.useState (fun () ->
                Set.singleton TIB_DATAPLANT_COLLECTION_KEY
                |> TermSearchConfigProviderHelper.mkTIBQueries
            )

        let olsQueries, setOLSQueries = React.useState (fun () -> QueryCollection.empty ())

        React.useEffect (
            (fun () ->
                TermSearchConfigProviderHelper.loadCollections
                    "TIB"
                    Api.TIBApi.TIBApi.getCollections
                    (fun response ->
                        response.content
                        |> Set.ofArray
                        |> Set.add TIB_DATAPLANT_COLLECTION_KEY
                        |> TermSearchConfigProviderHelper.mkTIBQueries
                    )
                    setTIBQueries
                |> Promise.start
            ),
            [||]
        )

        React.useEffect (
            (fun () ->
                if includeOLS then
                    TermSearchConfigProviderHelper.loadCollections
                        "OLS"
                        Api.OLSApi.OLSApi.getCollections
                        TermSearchConfigProviderHelper.mkOLSQueries
                        setOLSQueries
                    |> Promise.start
                else
                    setOLSQueries (QueryCollection.empty ())
            ),
            [| box includeOLS |]
        )

        let allQueries =
            React.useMemo (
                (fun () -> QueryCollection.append tibQueries olsQueries),
                [| box tibQueries; box olsQueries |]
            )

        TermSearchConfigProvider.TermSearchConfigProvider(
            children,
            allQueries.TermSearch,
            allQueries.ParentSearch,
            allQueries.AllChildrenSearch,
            defaultActive =
                Set [
                    TermSearchSourceKey.create TermSearchSource.TIB TIB_DATAPLANT_COLLECTION_KEY
                    TermSearchSourceKey.create TermSearchSource.OLS OLS_DATAPLANT_COLLECTION_KEY
                ]
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
        let localStorageKey = defaultArg localStorageKey LOCAL_STORAGE_KEY
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
                    let activeKeySet = Set.ofArray activeKeys.activeKeys

                    let activeQueries queries =
                        queries
                        |> Seq.filter (fun (key, _) -> Set.contains key activeKeySet)
                        |> ResizeArray

                    {
                        hasProvider = true
                        disableDefault = activeKeys.disableDefault
                        termSearchQueries = activeQueries allTermSearchQueries
                        parentSearchQueries = activeQueries allParentSearchQueries
                        allChildrenSearchQueries = activeQueries allAllChildrenSearchQueries
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
