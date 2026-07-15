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
        abstract hasDirectChildren: bool option

    type SearchResults =
        abstract docs: Term[] option

    type SearchApi =
        abstract response: SearchResults option

    let searchTerms (response: SearchApi) =
        response.response |> Option.bind _.docs |> Option.defaultValue [||]

    type TermArray =
        abstract terms: Term[]

    type TermApi =
        abstract _embedded: TermArray option

    type Terminology =
        abstract uri: string
        abstract label: string
        abstract source: string

    type Collection =
        abstract id: string
        abstract label: string
        abstract isPublic: bool
        abstract terminologies: Terminology[]

module private OLSApiHelper =

    type HierarchyApi =
        abstract elements: OLSTypes.Term[] option

    type HierarchyResource = {
        Iri: string
        Ontology: string
        Database: string
    }

    let equalsIgnoreCase (left: string) (right: string) =
        System.String.Equals(left, right, System.StringComparison.OrdinalIgnoreCase)

    let termIri (term: OLSTypes.Term) = term.iri |> Option.orElse term.URI

    let termOntology (term: OLSTypes.Term) =
        term.ontology_name |> Option.orElse term.ontologyId

    let containsQuery query (term: OLSTypes.Term) =
        if System.String.IsNullOrWhiteSpace query || query = "*" then
            true
        else
            [|
                term.label
                term.short_form
                term.shortForm
                yield! term.description |> Option.defaultValue [||] |> Array.map Some
                yield!
                    term.definition
                    |> Option.bind _.value
                    |> Option.defaultValue [||]
                    |> Array.map Some
            |]
            |> Array.choose id
            |> Array.exists (fun value -> value.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)

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

    static member private resolveParent(parentId: string, collection: OLSTypes.Collection) =
        let parentPrefix =
            match parentId.Split ':' with
            | [| prefix; _ |] -> Some prefix
            | _ -> None

        appendQueryParams $"{OLSTypes.BaseAPIUrl}/ols/api/terms" [ "iri", parentId; "collectionId", collection.id ]
        |> getJson<OLSTypes.TermApi>
        |> Promise.map (fun response ->
            response._embedded
            |> Option.map _.terms
            |> Option.defaultValue [||]
            |> Array.tryPick (fun term ->
                match OLSApiHelper.termIri term, OLSApiHelper.termOntology term with
                | Some iri, Some ontology when parentPrefix |> Option.forall (OLSApiHelper.equalsIgnoreCase ontology) ->
                    collection.terminologies
                    |> Array.tryFind (fun terminology ->
                        OLSApiHelper.equalsIgnoreCase terminology.label ontology
                        || OLSApiHelper.equalsIgnoreCase terminology.uri ontology
                    )
                    |> Option.map (fun terminology ->
                        {
                            Iri = iri
                            Ontology = ontology
                            Database = terminology.source
                        }
                        : OLSApiHelper.HierarchyResource
                    )
                | _ -> None
            )
        )

    static member private getChildren
        (resource: OLSApiHelper.HierarchyResource, collectionId: string, ?size: int, ?search: string)
        =
        let encodedParent = OLSApi.encodeClassPath (resource.Iri, resource.Database)

        let queryParams: (string * obj) list = [
            "database", resource.Database
            "page", 0
            "size", (defaultArg size 500 |> box)
            "collectionId", collectionId
            if search.IsSome then
                "search", search.Value
        ]

        appendQueryParams
            $"{OLSTypes.BaseAPIUrl}/ols/api/v2/ontologies/{JS.encodeURIComponent resource.Ontology}/classes/{encodedParent}/children"
            queryParams
        |> getJson<OLSApiHelper.HierarchyApi>
        |> Promise.map (fun response -> response.elements |> Option.defaultValue [||])

    static member search(q: string, ?collection: string) =
        let queryParams: (string * obj) list = [
            "query", q
            "targetDbSchema", "ols"
            if collection.IsSome then
                "collectionId", collection.Value
        ]

        appendQueryParams $"{OLSTypes.BaseAPIUrl}/search" queryParams
        |> getJson<OLSTypes.SearchApi>

    static member searchChildrenOf(q: string, parentId: string, collection: OLSTypes.Collection, ?rows: int) = promise {
        let rows = defaultArg rows 10

        match! OLSApi.resolveParent (parentId, collection) with
        | None -> return None
        | Some resource ->
            let! terms = OLSApi.getChildren (resource, collection.id, rows, q)

            return
                terms
                |> Array.filter (OLSApiHelper.containsQuery q)
                |> Array.truncate rows
                |> Some
    }

    static member searchAllChildrenOf(parentId: string, collection: OLSTypes.Collection, ?rows: int) = promise {
        match! OLSApi.resolveParent (parentId, collection) with
        | None -> return None
        | Some resource ->
            let rows = defaultArg rows 500

            let rec collectDescendants (frontier: string[]) (visited: Set<string>) (results: OLSTypes.Term[]) = promise {
                if Array.isEmpty frontier || results.Length >= rows then
                    return results
                else
                    let remainingRows = rows - results.Length

                    let! childGroups =
                        frontier
                        |> Array.map (fun iri -> OLSApi.getChildren ({ resource with Iri = iri }, collection.id))
                        |> Promise.all

                    let newTerms =
                        childGroups
                        |> Array.concat
                        |> Array.choose (fun term -> OLSApiHelper.termIri term |> Option.map (fun iri -> iri, term))
                        |> Array.distinctBy fst
                        |> Array.filter (fun (iri, _) -> not (Set.contains iri visited))
                        |> Array.truncate remainingRows

                    let nextVisited =
                        newTerms |> Array.fold (fun state (iri, _) -> Set.add iri state) visited

                    let nextFrontier =
                        newTerms
                        |> Array.choose (fun (iri, term) ->
                            if term.hasDirectChildren |> Option.defaultValue true then
                                Some iri
                            else
                                None
                        )

                    let nextResults = Array.append results (newTerms |> Array.map snd)

                    return! collectDescendants nextFrontier nextVisited nextResults
            }

            let! results = collectDescendants [| resource.Iri |] (Set.singleton resource.Iri) [||]

            return Some results
    }

    static member getCollections() =
        getJson<OLSTypes.Collection[]> $"{OLSTypes.BaseAPIUrl}/collections/"
