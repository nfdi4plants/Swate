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
        abstract hasDirectChildren: bool option

    [<RequireQualifiedAccess>]
    module TermHelpers =

        let private normalizeShortForm (value: string) =
            let separatorIndex = value.IndexOf "_"

            if separatorIndex > 0 && separatorIndex < value.Length - 1 then
                value.Substring(0, separatorIndex) + ":" + value.Substring(separatorIndex + 1)
            else
                value

        let iri (term: Term) = term.iri

        let ontology (term: Term) =
            term.ontology_name |> Option.orElse term.ontologyId

        let description (term: Term) =
            term.description |> Option.orElse (term.definition |> Option.bind _.value)

        let shortForm (term: Term) =
            term.short_form |> Option.orElse term.shortForm

        let id (term: Term) =
            term.obo_id
            |> Option.orElse (shortForm term |> Option.map normalizeShortForm)
            |> Option.orElse (iri term)

        let isObsolete (term: Term) =
            term.is_obsolete |> Option.orElse term.isObsolete |> Option.orElse term.obsolete

        let searchableValues (term: Term) =
            [|
                term.label
                term.obo_id
                shortForm term
                iri term
                yield! description term |> Option.defaultValue [||] |> Array.map Some
            |]
            |> Array.choose (fun value -> value)

    type SearchResults =
        abstract numFound: int option
        abstract start: int option
        abstract docs: Term[] option

    type SearchApi =
        abstract response: SearchResults option

    let searchTerms (response: SearchApi) =
        response.response |> Option.bind _.docs |> Option.defaultValue [||]

    type HierarchyApi =
        abstract elements: Term[] option

    type Terminology =
        abstract uri: string
        abstract label: string
        abstract source: string

    type Collection =
        abstract id: string option
        abstract label: string
        abstract isPublic: bool
        abstract terminologies: Terminology[] option

module private OLSResponse =

    // The TS4NFDI gateway currently returns the same identifier as both `iri` and `URI`.
    // Keep the canonical `iri` field locally until the duplicate is removed upstream.
    [<Emit("($0.iri = $0.iri ?? $0.URI, delete $0.URI)")>]
    let private canonicalizeIri (_term: OLSTypes.Term) : unit = jsNative

    let canonicalizeTerms terms =
        terms |> Array.iter canonicalizeIri
        terms

    let canonicalizeSearch (response: OLSTypes.SearchApi) =
        response |> OLSTypes.searchTerms |> canonicalizeTerms |> ignore
        response

[<AttachMembers>]
type OLSApi =

    static member private encodeClassPath(iri: string, database: string) =
        // The gateway double-encodes OLS2 IRIs itself. OLS1 backends expect the
        // already double-encoded value, after Spring has decoded the path once.
        let encodingCount = if database = "ebi" then 1 else 3

        [ 1..encodingCount ]
        |> List.fold (fun encoded _ -> JS.encodeURIComponent encoded) iri

    static member private getChildren(parentIri: string, ontology: string, database: string, ?collectionId: string) =
        let encodedParent = OLSApi.encodeClassPath (parentIri, database)

        let queryParams: (string * obj) list = [
            "database", database
            "page", 0
            "size", 500
            if collectionId.IsSome then
                "collectionId", collectionId.Value
        ]

        appendQueryParams
            $"{OLSTypes.BaseAPIUrl}/ols/api/v2/ontologies/{JS.encodeURIComponent ontology}/classes/{encodedParent}/children"
            queryParams
        |> getJson<OLSTypes.HierarchyApi>
        |> Promise.map (fun response -> response.elements |> Option.defaultValue [||] |> OLSResponse.canonicalizeTerms)

    static member private containsQuery(query: string, term: OLSTypes.Term) =
        if System.String.IsNullOrWhiteSpace query || query = "*" then
            true
        else
            let contains (value: string) =
                value.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0

            term |> OLSTypes.TermHelpers.searchableValues |> Array.exists contains

    static member search(q: string, ?rows: int, ?ontology: string, ?database: string, ?collectionId: string) =
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

        appendQueryParams $"{OLSTypes.BaseAPIUrl}/ols/api/select" queryParams
        |> getJson<OLSTypes.SearchApi>
        |> Promise.map (OLSResponse.canonicalizeSearch >> Some)

    static member defaultSearch(q: string, ?rows: int, ?ontology: string, ?database: string, ?collectionId: string) =
        OLSApi.search (
            q,
            rows = defaultArg rows 10,
            ?ontology = ontology,
            ?database = database,
            ?collectionId = collectionId
        )

    static member searchChildrenOf
        (q: string, parentIri: string, ontology: string, database: string, ?rows: int, ?collectionId: string)
        =
        OLSApi.getChildren (parentIri, ontology, database, ?collectionId = collectionId)
        |> Promise.map (
            Array.filter (fun term -> OLSApi.containsQuery (q, term))
            >> Array.truncate (defaultArg rows 10)
        )

    static member searchAllChildrenOf
        (parentIri: string, ontology: string, database: string, ?rows: int, ?collectionId: string)
        =
        promise {
            let rows = defaultArg rows 500
            let visited = System.Collections.Generic.HashSet<string>()
            let results = ResizeArray<OLSTypes.Term>()
            let mutable frontier = [| parentIri |]
            visited.Add parentIri |> ignore

            while frontier.Length > 0 && results.Count < rows do
                let! childGroups =
                    frontier
                    |> Array.map (fun iri -> OLSApi.getChildren (iri, ontology, database, ?collectionId = collectionId))
                    |> Promise.all

                let nextFrontier = ResizeArray<string>()

                for term in Array.concat childGroups do
                    if results.Count < rows then
                        let iri = OLSTypes.TermHelpers.iri term

                        match iri with
                        | Some value when visited.Add value ->
                            results.Add term

                            if term.hasDirectChildren |> Option.defaultValue true then
                                nextFrontier.Add value
                        | _ -> ()

                frontier <- nextFrontier.ToArray()

            return results.ToArray()
        }

    static member getCollections() =
        getJson<OLSTypes.Collection[]> $"{OLSTypes.BaseAPIUrl}/collections/"
