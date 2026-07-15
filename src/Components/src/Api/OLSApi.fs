/// https://terminology.services.base4nfdi.de/api-gateway/v3/api-docs
module Swate.Components.Api.OLSApi

open Swate.Components
open Swate.Components.Shared
open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client
open Fetch
open Swate.Components.Api.Helper


[<RequireQualifiedAccess>]
module OLSTypes =

    [<Literal>]
    let BaseAPIUrl = "https://terminology.services.base4nfdi.de/api-gateway"

    type Page =
        abstract number: int option
        abstract size: int option
        abstract totalElements: int option

    type Term =
        abstract iri: string option
        abstract lang: string option
        abstract description: string[] option
        abstract synonym: string[] option
        abstract synonyms: string[] option

        abstract label: string option
        abstract ontology_name: string option
        abstract ontology_prefix: string option
        abstract ontology_iri: string option

        abstract is_obsolete: bool option
        abstract obsolete: bool option
        abstract term_replaced_by: string option

        abstract short_form: string option
        abstract obo_id: string option

        abstract hasChildren: bool option

    type TermArray =
        abstract terms: Term[] option
        abstract page: Page option

    type TermApi =
        abstract _embedded: TermArray option

    type SearchResults =
        abstract numFound: int option
        abstract start: int option
        abstract docs: Term[] option

    type SearchApi =
        abstract response: SearchResults option

    type Collection =
        abstract id: string option
        abstract label: string
        abstract isPublic: bool

module private OLSApiHelper =

    [<Emit("(($0.iri = $0.iri ?? $0.URI), delete $0.URI)")>]
    let private canonicalizeTermIdentifier (term: OLSTypes.Term) : unit = jsNative

    let canonicalizeTerms getTerms response =
        response |> getTerms |> Option.iter (Array.iter canonicalizeTermIdentifier)

        response

[<AttachMembers>]
type OLSApi =

    static member getTermByIRI(ontology: string, iri: string, ?database: string) =
        let queryParams: (string * obj) list = [
            "iri", iri
            if database.IsSome then
                "database", database.Value
        ]

        getJson<OLSTypes.TermApi> (
            appendQueryParams $"{OLSTypes.BaseAPIUrl}/ols/api/ontologies/{ontology}/terms" queryParams
        )
        |> Promise.map (OLSApiHelper.canonicalizeTerms (fun response -> response._embedded |> Option.bind _.terms))

    static member search(q: string, ?rows: int, ?ontology: string, ?database: string, ?collectionId: string) =
        let baseUrl = $"{OLSTypes.BaseAPIUrl}/ols/api/select"

        let queryParams: (string * obj) list = [
            "q", q
            if rows.IsSome then
                "rows", rows.Value
            if ontology.IsSome then
                "ontology", ontology.Value
            if database.IsSome then
                "database", database.Value
            if collectionId.IsSome then
                "collectionId", collectionId.Value
        ]

        let url = appendQueryParams baseUrl queryParams

        getJson<OLSTypes.SearchApi> url
        |> Promise.map (
            OLSApiHelper.canonicalizeTerms (fun response -> response.response |> Option.bind _.docs)
            >> Some
        )

    static member defaultSearch(q: string, ?rows: int, ?ontology: string, ?database: string, ?collectionId: string) =
        let rows = defaultArg rows 10
        OLSApi.search (q, rows = rows, ?ontology = ontology, ?database = database, ?collectionId = collectionId)

    static member getCollections() =
        getJson<OLSTypes.Collection[]> $"{OLSTypes.BaseAPIUrl}/collections/"
