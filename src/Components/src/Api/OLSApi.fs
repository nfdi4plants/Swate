/// https://terminology.services.base4nfdi.de/api-gateway/v3/api-docs
module Swate.Components.Api.OLSApi

open Fable.Core
open Swate.Components.Api.Helper

[<RequireQualifiedAccess>]
module OLSTypes =

    [<Literal>]
    let BaseAPIUrl = "https://terminology.services.base4nfdi.de/api-gateway"

    type StringValues =
        abstract value: string[] option

    type Term =
        abstract iri: string option
        abstract URI: string option
        abstract description: string[] option
        abstract definition: StringValues option
        abstract label: string option
        abstract ontology_name: string option
        abstract ontologyId: string option
        abstract is_obsolete: bool option
        abstract isObsolete: bool option
        abstract obsolete: bool option
        abstract short_form: string option
        abstract shortForm: string option
        abstract obo_id: string option

    type SearchResults =
        abstract docs: Term[] option

    type SearchApi =
        abstract response: SearchResults option

    let searchTerms (response: SearchApi) =
        response.response |> Option.bind _.docs |> Option.defaultValue [||]

    type Terminology =
        abstract uri: string
        abstract label: string
        abstract source: string

    type Collection =
        abstract id: string
        abstract label: string
        abstract isPublic: bool
        abstract terminologies: Terminology[]

module private OLSApiTypes =

    type HierarchyApi =
        abstract elements: OLSTypes.Term[] option

[<AttachMembers>]
type OLSApi =

    static member private encodeClassPath(iri: string, database: string) =
        // The gateway double-encodes OLS2 IRIs itself. OLS1 backends expect the
        // already double-encoded value, after Spring has decoded the path once.
        let encodingCount =
            if database.Equals("ebi", System.StringComparison.OrdinalIgnoreCase) then
                1
            else
                3

        [ 1..encodingCount ]
        |> List.fold (fun encoded _ -> JS.encodeURIComponent encoded) iri

    static member private resolveParent(parentOboId: string, collection: OLSTypes.Collection) =
        OLSApi.search (parentOboId, 1, collection = collection.id)
        |> Promise.map (fun response ->
            response
            |> Option.bind (fun response ->
                response
                |> OLSTypes.searchTerms
                |> Array.tryFind (fun term -> term.obo_id = Some parentOboId)
                |> Option.bind (fun term ->
                    match
                        term.iri |> Option.orElse term.URI, term.ontology_name |> Option.orElse term.ontologyId
                    with
                    | Some iri, Some ontology ->
                        let sameOntology value =
                            System.String.Equals(value, ontology, System.StringComparison.OrdinalIgnoreCase)

                        collection.terminologies
                        |> Array.tryFind (fun terminology ->
                            sameOntology terminology.uri || sameOntology terminology.label
                        )
                        |> Option.map (fun terminology -> iri, terminology)
                    | _ -> None
                )
            )
        )

    static member private searchHierarchy(q: string, parentOboId: string, collection: OLSTypes.Collection, ?rows: int) = promise {
        match! OLSApi.resolveParent (parentOboId, collection) with
        | Some(parentIri, terminology) ->
            let queryParams: (string * obj) list = [
                "database", terminology.source
                "page", 0
                "size", (defaultArg rows 10 |> box)
                if q <> "*" then
                    "search", q
                    "collectionId", collection.id
            ]

            let encodedParent = OLSApi.encodeClassPath (parentIri, terminology.source)

            return!
                appendQueryParams
                    $"{OLSTypes.BaseAPIUrl}/ols/api/v2/ontologies/{JS.encodeURIComponent terminology.uri}/classes/{encodedParent}/children"
                    queryParams
                |> getJson<OLSApiTypes.HierarchyApi>
                |> Promise.map (fun response -> response.elements |> Option.defaultValue [||] |> Some)
        | None -> return None
    }

    static member search(q: string, ?rows: int, ?collection: string) =
        let queryParams: (string * obj) list = [
            "q", q
            if rows.IsSome then
                "rows", rows.Value
            if collection.IsSome then
                "collectionId", collection.Value
        ]

        appendQueryParams $"{OLSTypes.BaseAPIUrl}/ols/api/select" queryParams
        |> getJson<OLSTypes.SearchApi>
        |> Promise.map Some

    static member defaultSearch(q: string, ?rows: int, ?collection: string) =
        OLSApi.search (q, rows = defaultArg rows 10, ?collection = collection)

    static member searchChildrenOf(q: string, parentOboId: string, collection: OLSTypes.Collection, ?rows: int) =
        OLSApi.searchHierarchy (q, parentOboId, collection, rows = defaultArg rows 10)

    static member searchAllChildrenOf(parentOboId: string, collection: OLSTypes.Collection, ?rows: int) =
        OLSApi.searchHierarchy ("*", parentOboId, collection, rows = defaultArg rows 500)

    static member getCollections() =
        getJson<OLSTypes.Collection[]> $"{OLSTypes.BaseAPIUrl}/collections/"
