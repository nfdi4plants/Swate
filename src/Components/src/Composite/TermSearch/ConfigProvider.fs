namespace Swate.Components.Composite.TermSearch

open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.Composite.TermSearch.Context
open Swate.Components.Composite.TermSearch.Types
open System.Collections.Generic

module private TermSearchConfigProviderHelper =

    type SearchCollection = {
        Key: string
        TermSearch: SearchCall
        ParentSearch: ParentSearchCall
        AllChildrenSearch: AllChildrenSearchCall
    }

    type OLSHierarchyResource = {
        Iri: string
        Ontology: string
        Database: string
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
                queries.ParentSearch.Add(collection.Key, collection.ParentSearch)
                queries.AllChildrenSearch.Add(collection.Key, collection.AllChildrenSearch)

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
    let LOCAL_STORAGE_KEY = "swate-termsearchconfig-ctx-v4"

    [<Literal>]
    let DEFAULT_SEARCH_ROWS = 10

    [<Literal>]
    let ALL_CHILDREN_SEARCH_ROWS = 300

    let private normalizeTerms rows (terms: Term[]) =
        terms
        |> Array.distinctBy (fun term -> term.id, term.href, term.name)
        |> Array.truncate rows

    let private mapResults rows convert request =
        request |> Promise.map (convert >> normalizeTerms rows >> ResizeArray)

    let private mapSearchResults rows (convert: 'Api -> Term[]) (request: JS.Promise<'Api option>) =
        request |> mapResults rows (Option.map convert >> Option.defaultValue [||])

    let private mapTIBResults rows (request: JS.Promise<Api.TIBApi.TIBTypes.SearchApi option>) =
        request |> mapSearchResults rows _.ToSwateTerms()

    let private createTIBCollection collection = {
        Key = create TermSearchSource.TIB collection
        TermSearch =
            fun query ->
                Api.TIBApi.TIBApi.defaultSearch (query, DEFAULT_SEARCH_ROWS, collection)
                |> mapTIBResults DEFAULT_SEARCH_ROWS
        ParentSearch =
            fun (parent, query) ->
                Api.TIBApi.TIBApi.searchChildrenOf (query, parent, DEFAULT_SEARCH_ROWS, collection)
                |> mapTIBResults DEFAULT_SEARCH_ROWS
        AllChildrenSearch =
            fun parent ->
                Api.TIBApi.TIBApi.searchAllChildrenOf (parent, ALL_CHILDREN_SEARCH_ROWS, collection = collection)
                |> mapTIBResults ALL_CHILDREN_SEARCH_ROWS
    }

    let mkTIBQueries collections =
        collections
        |> Set.add TIB_DATAPLANT_COLLECTION_KEY
        |> Seq.map createTIBCollection
        |> QueryCollection.create

    let private tryCreateOLSCollection (collection: Api.OLSApi.OLSTypes.Collection) =
        match collection.id with
        | Some collectionId when not (System.String.IsNullOrWhiteSpace collection.label) ->
            let resources = Dictionary<string, OLSHierarchyResource>()
            let terminologies = collection.terminologies |> Option.defaultValue [||]

            let tryFindDatabase ontology =
                terminologies
                |> Array.tryFind (fun terminology ->
                    System.String.Equals(terminology.uri, ontology, System.StringComparison.OrdinalIgnoreCase)
                    || System.String.Equals(terminology.label, ontology, System.StringComparison.OrdinalIgnoreCase)
                )
                |> Option.map _.source

            let cacheTerms (database: string option) (terms: Api.OLSApi.OLSTypes.Term[]) =
                for term in terms do
                    let iri = Api.OLSApi.OLSTypes.TermHelpers.iri term
                    let ontology = Api.OLSApi.OLSTypes.TermHelpers.ontology term
                    let termDatabase = ontology |> Option.bind tryFindDatabase |> Option.orElse database

                    match Api.OLSApi.OLSTypes.TermHelpers.id term, iri, ontology, termDatabase with
                    | Some id, Some iri, Some ontology, Some database when not (resources.ContainsKey id) ->
                        resources.Add(
                            id,
                            {
                                Iri = iri
                                Ontology = ontology
                                Database = database
                            }
                        )
                    | _ -> ()

                OLSTypesExtensions.toSwateTerms terms

            let tryFallbackResource (parent: string) =
                match parent.Split ':' with
                | [| prefix; accession |] when prefix.Length > 0 && accession.Length > 0 ->
                    let shortForm = $"{prefix}_{accession}"

                    tryFindDatabase prefix
                    |> Option.map (fun database ->
                        let iri =
                            if prefix.Equals("DPBO", System.StringComparison.OrdinalIgnoreCase) then
                                $"https://purl.org/nfdi4plants/ontology/dpbo/{shortForm}"
                            else
                                $"http://purl.obolibrary.org/obo/{shortForm}"

                        {
                            Iri = iri
                            Ontology = prefix.ToLowerInvariant()
                            Database = database
                        }
                    )
                | _ -> None

            let tryFindResource parent =
                match resources.TryGetValue parent with
                | true, resource -> Some resource
                | _ -> tryFallbackResource parent

            let emptyTerms () = Promise.lift (ResizeArray<Term>())

            let mapHierarchyResults rows database request =
                request |> mapResults rows (cacheTerms (Some database))

            let searchHierarchy rows parent search =
                match tryFindResource parent with
                | Some resource -> search resource |> mapHierarchyResults rows resource.Database
                | None -> emptyTerms ()

            Some {
                Key = create TermSearchSource.OLS collection.label
                TermSearch =
                    fun query ->
                        Api.OLSApi.OLSApi.defaultSearch (query, DEFAULT_SEARCH_ROWS, collectionId = collectionId)
                        |> mapSearchResults DEFAULT_SEARCH_ROWS (Api.OLSApi.OLSTypes.searchTerms >> cacheTerms None)
                ParentSearch =
                    fun (parent, query) ->
                        searchHierarchy
                            DEFAULT_SEARCH_ROWS
                            parent
                            (fun resource ->
                                Api.OLSApi.OLSApi.searchChildrenOf (
                                    query,
                                    resource.Iri,
                                    resource.Ontology,
                                    resource.Database,
                                    DEFAULT_SEARCH_ROWS,
                                    collectionId
                                )
                            )
                AllChildrenSearch =
                    fun parent ->
                        searchHierarchy
                            ALL_CHILDREN_SEARCH_ROWS
                            parent
                            (fun resource ->
                                Api.OLSApi.OLSApi.searchAllChildrenOf (
                                    resource.Iri,
                                    resource.Ontology,
                                    resource.Database,
                                    ALL_CHILDREN_SEARCH_ROWS,
                                    collectionId
                                )
                            )
            }
        | _ -> None

    let mkOLSQueries collections =
        collections |> Seq.choose tryCreateOLSCollection |> QueryCollection.create

    [<Hook>]
    let useCollectionQueries enabled source initialQueries request toQueries =
        let queries, setQueries =
            React.useState (fun () ->
                if enabled then
                    initialQueries ()
                else
                    QueryCollection.empty ()
            )

        React.useEffect (
            (fun () ->
                if enabled then
                    promise {
                        try
                            let! response = request ()
                            response |> toQueries |> setQueries
                        with ex ->
                            console.error ($"Error fetching {source} collections:", ex)
                    }
                    |> Promise.start
                else
                    setQueries (QueryCollection.empty ())
            ),
            [| box enabled |]
        )

        queries

open TermSearchConfigProviderHelper

[<Erase; Mangle(false)>]
type TermSearchConfigProvider =

    [<ReactComponent>]
    static member private QueryProvider(children: ReactElement, includeTIB: bool, includeOLS: bool) =
        let tibQueries =
            TermSearchConfigProviderHelper.useCollectionQueries
                includeTIB
                "TIB"
                (fun () -> TermSearchConfigProviderHelper.mkTIBQueries Set.empty)
                Api.TIBApi.TIBApi.getCollections
                (fun response -> response.content |> Set.ofArray |> TermSearchConfigProviderHelper.mkTIBQueries)

        let olsQueries =
            TermSearchConfigProviderHelper.useCollectionQueries
                includeOLS
                "OLS"
                QueryCollection.empty
                Api.OLSApi.OLSApi.getCollections
                TermSearchConfigProviderHelper.mkOLSQueries

        let allQueries =
            React.useMemo (
                (fun () -> QueryCollection.append tibQueries olsQueries),
                [| box tibQueries; box olsQueries |]
            )

        let defaultActive =
            Set [
                if includeTIB then
                    TermSearchSource.TIB.DefaultKey
                if includeOLS then
                    TermSearchSource.OLS.DefaultKey
            ]

        TermSearchConfigProvider.TermSearchConfigProvider(
            children,
            allQueries.TermSearch,
            allQueries.ParentSearch,
            allQueries.AllChildrenSearch,
            defaultActive = defaultActive
        )

    [<ReactComponent>]
    static member TIBQueryProvider(children: ReactElement) =
        TermSearchConfigProvider.QueryProvider(children, true, false)

    [<ReactComponent>]
    static member OLSQueryProvider(children: ReactElement) =
        TermSearchConfigProvider.QueryProvider(children, false, true)

    [<ReactComponent>]
    static member DefaultQueryProvider(children: ReactElement) =
        TermSearchConfigProvider.QueryProvider(children, true, true)

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

        // From v1.0.7 to v2.0.0 the field was renamed from `aktiveKeys` to `activeKeys`.
        match activeKeys.activeKeys with
        | null -> Browser.Dom.window.localStorage.removeItem (localStorageKey)
        | _ -> ()

        let allKeys =
            React.useMemo (
                (fun () ->
                    [
                        yield! allTermSearchQueries |> Seq.map fst
                        yield! allParentSearchQueries |> Seq.map fst
                        yield! allAllChildrenSearchQueries |> Seq.map fst
                    ]
                    |> Set.ofList
                ),
                [|
                    box allTermSearchQueries
                    box allParentSearchQueries
                    box allAllChildrenSearchQueries
                |]
            )

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
