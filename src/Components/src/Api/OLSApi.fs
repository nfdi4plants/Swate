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
        abstract id: string option
        abstract label: string
        abstract isPublic: bool
        abstract terminologies: Terminology[] option

module private OLSApiHelper =

    type HierarchyApi =
        abstract elements: OLSTypes.Term[] option

    type HierarchyResource = {
        Iri: string
        Ontology: string
        Database: string
    }

    let tryCreateHierarchyResource (parentOboId: string) (collection: OLSTypes.Collection) =
        match parentOboId.Split ':' with
        | [| prefix; accession |] when prefix.Length > 0 && accession.Length > 0 ->
            collection.terminologies
            |> Option.defaultValue [||]
            |> Array.tryFind (fun terminology ->
                System.String.Equals(terminology.uri, prefix, System.StringComparison.OrdinalIgnoreCase)
                || System.String.Equals(terminology.label, prefix, System.StringComparison.OrdinalIgnoreCase)
            )
            |> Option.map (fun terminology ->
                let shortForm = $"{prefix}_{accession}"

                let iri =
                    if prefix.Equals("DPBO", System.StringComparison.OrdinalIgnoreCase) then
                        $"https://purl.org/nfdi4plants/ontology/dpbo/{shortForm}"
                    else
                        $"http://purl.obolibrary.org/obo/{shortForm}"

                {
                    Iri = iri
                    Ontology = terminology.uri
                    Database = terminology.source
                }
            )
        | _ -> None

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

    static member private searchHierarchy(q: string, parentOboId: string, collection: OLSTypes.Collection, ?rows: int) =
        match OLSApiHelper.tryCreateHierarchyResource parentOboId collection with
        | None -> Promise.lift None
        | Some resource ->
            let queryParams: (string * obj) list = [
                "database", resource.Database
                "page", 0
                "size", (defaultArg rows 10 |> box)
                if q <> "*" then
                    "search", q
                if collection.id.IsSome then
                    "collectionId", collection.id.Value
            ]

            let encodedParent = OLSApi.encodeClassPath (resource.Iri, resource.Database)

            appendQueryParams
                $"{OLSTypes.BaseAPIUrl}/ols/api/v2/ontologies/{JS.encodeURIComponent resource.Ontology}/classes/{encodedParent}/children"
                queryParams
            |> getJson<OLSApiHelper.HierarchyApi>
            |> Promise.map (fun response -> response.elements |> Option.defaultValue [||] |> Some)

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
