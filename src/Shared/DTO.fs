module Shared.DTO

open ARCtrl

open Database

type TermQuery = {
    query: string
    limit: int option
    parentTermId: string option
    ontologies: string list option
    searchMode: Database.FullTextSearch option
} with
    static member create(query, ?limit, ?parentTermId, ?ontologies, ?searchMode) = {
        query = query
        limit = limit
        parentTermId = parentTermId
        ontologies = ontologies
        searchMode = searchMode
    }

type TermQueryResults = {
    query: TermQuery
    results: Term []
} with
    static member create(query, results) = {
        query = query
        results = results
    }

