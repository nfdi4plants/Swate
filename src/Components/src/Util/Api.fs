module Swate.Components.Api

open Shared
open Fable.Core
open Fable.Remoting.Client

let SwateApi : IOntologyAPIv3 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IOntologyAPIv3>



// https://api.terminology.tib.eu/swagger-ui/index.html
open Fetch

let private makeQueryParam (name: string, value: obj) =
    Fable.Core.JS.encodeURIComponent name + "=" + (string >> Fable.Core.JS.encodeURIComponent) value

let private makeQueryParamStr (queryParams: (string * obj) list) : string =
    queryParams
    |> List.map (fun (name, value) -> makeQueryParam(name, value))
    |> String.concat "&"
    |> (+) "?"

let private appendQueryParams (url: string) (queryParams: (string * obj) list) : string =
    url + makeQueryParamStr queryParams


[<RequireQualifiedAccess>]
module TIBTypes =
    type Term =
        abstract iri: string
        abstract lang: string
        abstract description: string []
        abstract synonyms: string []

        abstract label: string
        abstract ontology_name: string
        abstract ontology_prefix: string option
        abstract ontology_iri: string

        abstract is_obsolete: bool option
        abstract term_replaced_by: string option

        abstract short_form: string
        abstract obo_id: string

        abstract hasChildren: bool option

    type TermArray =
        abstract terms: Term []

    type TermApi =
        abstract _embedded: TermArray

    type SearchResults =
        abstract numFound: int
        abstract start: int
        abstract docs: Term []

    type SearchApi =
        abstract response: SearchResults

[<AutoOpen>]
module TIBTypesExtensions =
    type TIBTypes.SearchApi with
        /// This function is used to transform TIB term type into the Swate compatible Term type.
        member this.ToMyTerm() =
            this.response.docs
            |> Array.map (fun t ->
                Swate.Components.Term(
                    t.label,
                    t.obo_id,
                    t.description |> String.concat ";",
                    t.ontology_name,
                    t.iri,
                    t.is_obsolete |> Option.defaultValue false
                )
            )

[<AttachMembers>]
type TIBApi =
    static member tryGetIRIFromOboId (oboId: string) =
        fetch
            (appendQueryParams "https://api.terminology.tib.eu/api/terms" ["obo_id", oboId])
            [
                RequestProperties.Method HttpMethod.GET
                requestHeaders  [ HttpRequestHeaders.Accept "application/json" ]
            ]
        |> Promise.bind(fun response ->
            response.json<TIBTypes.TermApi>()
            |> Promise.map(fun termApi ->
                termApi._embedded.terms
                |> Array.tryFind (fun term -> term.obo_id = oboId)
                |> Option.map (fun term -> term.iri)
            )
        )

    // TODO: Maybe we should use allChildrenOf instead of childrenOf?
    // The latter only uses subclassOf/is_a, whereas the other one uses all relationships
    static member search(q: string, ?rows: int, ?obsoletes: bool, ?queryFields: ResizeArray<string>, ?childrenOf: string) =
        promise {
            let baseUrl = @"https://api.terminology.tib.eu/api/search"
            let mutable childrenOf_ = None
            if childrenOf.IsSome then
                let! parentIri = TIBApi.tryGetIRIFromOboId childrenOf.Value
                childrenOf_ <- parentIri
                if childrenOf.IsSome && childrenOf_.IsNone then // exit condition should we not find the parent IRI
                    failwith ("Could not find parent IRI for childrenOf: " + childrenOf.Value)
            let queryParams: (string * obj) list = [
                "q", q
                if rows.IsSome then
                    "rows", rows.Value
                if obsoletes.IsSome then
                    "obsoletes", obsoletes.Value
                if queryFields.IsSome then
                    "queryFields", (queryFields.Value |> String.concat "," |> box)
                if childrenOf_.IsSome then
                    "childrenOf", childrenOf_.Value
            ]
            let url = appendQueryParams baseUrl queryParams
            return!
                fetch url [
                    RequestProperties.Method HttpMethod.GET
                    requestHeaders [ HttpRequestHeaders.Accept "application/json" ]
                ]
                |> Promise.bind (fun response ->
                    response.json<TIBTypes.SearchApi>()
                )
                |> Promise.map (fun searchApi ->
                    searchApi.ToMyTerm()
                )
        }

    static member defaultSearch(q: string, ?rows: int) =
        let rows = defaultArg rows 10
        TIBApi.search(q, rows = rows, obsoletes = false, queryFields = ResizeArray ["label"])

    static member searchChildrenOf(q: string, parentOboId: string, ?rows: int) =
        let rows = defaultArg rows 10
        TIBApi.search(q, rows = rows, childrenOf = parentOboId)

    static member searchAllChildrenOf(parentOboId: string, ?rows: int) =
        let rows = defaultArg rows 500
        TIBApi.search("*", rows = rows, childrenOf = parentOboId)