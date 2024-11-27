module Shared.DTOs.ParentTermQuery

open Shared

type ParentTermQueryDto = {
    parentTermId: string
    limit: int option
} with
    static member create(parentTermId, ?limit) = {
        parentTermId = parentTermId
        limit = limit
    }

type ParentTermQueryDtoResults = {
    query: ParentTermQueryDto
    results: Database.Term []
} with
    static member create(query, results) = {
        query = query
        results = results
    }