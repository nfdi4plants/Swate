module Swate.Components.Page.Metadata.FormComponents.PublicationsInput.Lookup

open System
open System.Text.RegularExpressions
open ARCtrl
open Fable.Core
open Fetch
open Swate.Components.Page.Metadata.FormComponents.PublicationsInput.Types

let private nonblank (value: string option) =
    value
    |> Option.bind (fun text ->
        if String.IsNullOrWhiteSpace text then
            None
        else
            Some(text.Trim())
    )

let private optionalArray (values: 'T[] option) = values |> Option.defaultValue [||]

let normalizeDoi (query: string) : Result<string, string> =
    let value = if isNull query then "" else query.Trim()

    let urlPrefix =
        [
            "https://doi.org/"
            "http://doi.org/"
            "https://dx.doi.org/"
            "http://dx.doi.org/"
        ]
        |> List.tryFind (fun prefix -> value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))

    try
        let doi =
            match urlPrefix with
            | Some prefix ->
                value.Substring(prefix.Length).Split([| '?'; '#' |]).[0]
                |> JS.decodeURIComponent
                |> fun text -> text.Trim()
            | None when value.StartsWith("doi:", StringComparison.OrdinalIgnoreCase) -> value.Substring(4).Trim()
            | None -> value

        if Regex.IsMatch(doi, @"^10\.\d{4,9}/\S+$") then
            Ok doi
        else
            Error "Enter a DOI such as 10.1000/example, or a doi.org link."
    with _ ->
        Error "This DOI link contains an invalid encoded character."

let crossrefUrl query =
    normalizeDoi query
    |> Result.map (fun doi -> "https://api.crossref.org/works/" + JS.encodeURIComponent doi)

/// Preserve free-text search syntax, but make recognizable identifiers exact searches.
let europePmcQuery (query: string) =
    let trimmed = query.Trim()
    let pmid = Regex.Match(trimmed, @"^(?:PMID\s*:\s*)?(\d+)$", RegexOptions.IgnoreCase)

    let pmcid =
        Regex.Match(trimmed, @"^(?:PMCID\s*:\s*)?(PMC\d+)$", RegexOptions.IgnoreCase)

    if pmid.Success then
        $"EXT_ID:{pmid.Groups.[1].Value} AND SRC:MED"
    elif pmcid.Success then
        "PMCID:" + pmcid.Groups.[1].Value.ToUpperInvariant()
    else
        trimmed

let europePmcUrl query cursor =
    let query = europePmcQuery query
    let cursor = cursor |> nonblank |> Option.defaultValue "*"

    "https://www.ebi.ac.uk/europepmc/webservices/rest/search?format=json&resultType=lite&pageSize=10&query="
    + JS.encodeURIComponent query
    + "&cursorMark="
    + JS.encodeURIComponent cursor

let formatCrossrefAuthor (author: CrossrefAuthor) =
    let personalName =
        [ author.given; author.family ]
        |> List.choose nonblank
        |> String.concat " "
        |> Some
        |> nonblank

    personalName |> Option.orElse (nonblank author.name)

let private firstText values =
    values |> optionalArray |> Array.tryPick (Some >> nonblank)

let private crossrefYear (date: CrossrefDate option) =
    date
    |> Option.bind (fun value -> value.``date-parts``)
    |> optionalArray
    |> Array.tryPick (fun parts ->
        if isNull parts || parts.Length = 0 then
            None
        else
            Some(string parts.[0])
    )

let mapCrossrefWork (work: CrossrefWork) : SearchResult =
    let doi = nonblank work.DOI

    {
        key = "crossref:" + (doi |> Option.defaultValue "publication")
        title = firstText work.title
        authors =
            work.author
            |> optionalArray
            |> Array.choose formatCrossrefAuthor
            |> String.concat "; "
            |> Some
            |> nonblank
        doi = doi
        pubMedId = None
        pmcId = None
        journal = firstText work.``container-title``
        year =
            [
                work.published
                work.``published-print``
                work.``published-online``
                work.issued
            ]
            |> List.tryPick crossrefYear
    }

let mapEuropePmcResult (result: EuropePmcResult) : SearchResult =
    let source = nonblank result.source
    let id = nonblank result.id

    let pmid =
        nonblank result.pmid
        |> Option.orElseWith (fun () ->
            if source = Some "MED" then
                id |> Option.filter (fun value -> Regex.IsMatch(value, @"^\d+$"))
            else
                None
        )

    {
        key =
            (source |> Option.defaultValue "europepmc")
            + ":"
            + (id |> Option.defaultValue "publication")
        title = nonblank result.title
        authors = nonblank result.authorString
        doi = nonblank result.doi
        pubMedId = pmid
        pmcId = nonblank result.pmcid
        journal = nonblank result.journalTitle
        year = nonblank result.pubYear
    }

let mapEuropePmcPage cursor (response: EuropePmcResponse) : SearchPage =
    let requestedCursor = cursor |> nonblank |> Option.defaultValue "*"

    let results =
        response.resultList
        |> Option.bind (fun value -> value.result)
        |> optionalArray
        |> Array.map mapEuropePmcResult

    let nextCursor =
        response.nextCursorMark
        |> nonblank
        |> Option.filter (fun next -> results.Length = 10 && next <> requestedCursor)

    {
        results = results
        nextCursor = nextCursor
    }

let searchDoi query : JS.Promise<SearchPage> = promise {
    match crossrefUrl query with
    | Error error -> return failwith error
    | Ok url ->
        let! response =
            fetch url [
                requestHeaders [ HttpRequestHeaders.Accept "application/json" ]
            ]

        if response.Status = 404 then
            return { results = [||]; nextCursor = None }
        elif not response.Ok then
            return failwith $"Crossref could not load this publication (HTTP {response.Status}). Please try again."
        else
            let! payload = response.json<CrossrefResponse> ()

            return {
                results =
                    payload.message
                    |> Option.map (mapCrossrefWork >> Array.singleton)
                    |> Option.defaultValue [||]
                nextCursor = None
            }
}

let searchEuropePmc query cursor : JS.Promise<SearchPage> = promise {
    if String.IsNullOrWhiteSpace query then
        return failwith "Enter a title, author, PubMed ID, or PMC ID."
    else
        let! response =
            fetch (europePmcUrl query cursor) [
                requestHeaders [ HttpRequestHeaders.Accept "application/json" ]
            ]

        if not response.Ok then
            return failwith $"Europe PMC could not search publications (HTTP {response.Status}). Please try again."
        else
            let! payload = response.json<EuropePmcResponse> ()
            return mapEuropePmcPage cursor payload
}

/// Apply only returned metadata to a copy, retaining manual metadata and annotations.
let mergePublication (publication: Publication) (result: SearchResult) =
    let merged = publication.Copy()
    merged.Title <- nonblank result.title |> Option.orElse publication.Title
    merged.Authors <- nonblank result.authors |> Option.orElse publication.Authors
    merged.DOI <- nonblank result.doi |> Option.orElse publication.DOI
    merged.PubMedID <- nonblank result.pubMedId |> Option.orElse publication.PubMedID
    merged
