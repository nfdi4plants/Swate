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
        abstract URI: string option
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

    type OntologyConfig =
        abstract id: string option
        abstract title: string option
        abstract description: string[] option
        abstract version: string option

    type Ontology =
        abstract ontologyId: string option
        abstract ``type``: string option
        abstract URI: string option
        abstract config: OntologyConfig option

    type OntologyArray =
        abstract ontologies: Ontology[] option
        abstract page: Page option

    type OntologiesApi =
        abstract _embedded: OntologyArray option


module private OLSApiHelper =

    let tryParseOboId (oboId: string) =
        let parts = oboId.Split([| ':' |], 2)

        if parts.Length = 2 && not (System.String.IsNullOrWhiteSpace parts.[0]) then
            let ontology = parts.[0].ToLowerInvariant()
            let iri = $"http://purl.obolibrary.org/obo/{parts.[0]}_{parts.[1]}"
            Some(ontology, iri)
        else
            None


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

    static member tryGetIRIFromOboId(oboId: string, ?database: string) = promise {
        match OLSApiHelper.tryParseOboId oboId with
        | None -> return None
        | Some(ontology, iri) ->
            let! termApi = OLSApi.getTermByIRI (ontology, iri, ?database = database)

            return
                termApi._embedded
                |> Option.bind _.terms
                |> Option.bind (
                    Array.tryFind (fun term ->
                        term.iri = Some iri
                        || term.short_form = Some(oboId.Replace(":", "_"))
                        || term.obo_id = Some oboId
                    )
                )
                |> Option.bind _.iri
    }

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

        getJson<OLSTypes.SearchApi> url |> Promise.map Some

    static member defaultSearch(q: string, ?rows: int, ?ontology: string, ?database: string, ?collectionId: string) =
        let rows = defaultArg rows 10
        OLSApi.search (q, rows = rows, ?ontology = ontology, ?database = database, ?collectionId = collectionId)

    static member getOntologies(?database: string, ?collectionId: string) =
        let queryParams: (string * obj) list = [
            if database.IsSome then
                "database", database.Value
            if collectionId.IsSome then
                "collectionId", collectionId.Value
        ]

        getJson<OLSTypes.OntologiesApi> (appendQueryParams $"{OLSTypes.BaseAPIUrl}/ols/api/ontologies" queryParams)
