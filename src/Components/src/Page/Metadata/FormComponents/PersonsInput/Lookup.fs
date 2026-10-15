module Swate.Components.Page.Metadata.FormComponents.PersonsInput.Lookup

open System
open System.Text.RegularExpressions
open ARCtrl
open Fable.Core
open Fetch
open Swate.Components.Page.Metadata.FormComponents.PersonsInput.Types

let private nonblank (value: string option) =
    value
    |> Option.bind (fun text ->
        if String.IsNullOrWhiteSpace text then
            None
        else
            Some(text.Trim())
    )

let private optionalArray (value: 'T[] option) = value |> Option.defaultValue [||]

let private joined (values: string option[]) =
    values
    |> Array.choose nonblank
    |> Array.distinct
    |> String.concat "; "
    |> Some
    |> nonblank

let normalizeOrcid (query: string) : Result<string, string> =
    let value = if isNull query then "" else query.Trim()

    let url =
        Regex.Match(value, @"^https?://orcid\.org/([^/?#]+)/?(?:[?#].*)?$", RegexOptions.IgnoreCase)

    let identifier =
        (if url.Success then url.Groups.[1].Value else value).ToUpperInvariant()

    if not (Regex.IsMatch(identifier, @"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$")) then
        Error "Enter an ORCID iD such as 0000-0002-1825-0097, or an orcid.org link."
    else
        let compact = identifier.Replace("-", "")

        let total =
            compact.Substring(0, 15)
            |> Seq.fold (fun total digit -> (total + int digit - int '0') * 2) 0

        let checksum = (12 - total % 11) % 11
        let expected = if checksum = 10 then "X" else string checksum

        if compact.Substring(15) = expected then
            Ok identifier
        else
            Error "This ORCID iD has an invalid checksum. Check the identifier and try again."

let recordUrl query =
    normalizeOrcid query
    |> Result.map (fun orcid -> "https://pub.orcid.org/v3.0/" + JS.encodeURIComponent orcid + "/record")

/// Quote every token so name input cannot introduce Solr fields or boolean operators.
let nameQuery (query: string) =
    let tokens =
        Regex.Split(query.Trim(), @"\s+")
        |> Array.filter (String.IsNullOrWhiteSpace >> not)

    let escaped =
        tokens
        |> Array.map (fun token -> "\"" + token.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"")
        |> String.concat " AND "

    "given-and-family-names:(" + escaped + ")"

let nameSearchUrl query start =
    "https://pub.orcid.org/v3.0/expanded-search/?q="
    + JS.encodeURIComponent (nameQuery query)
    + "&rows=10&start="
    + string (max 0 start)

let fullName (result: PersonResult) =
    let names =
        [ result.firstName; result.lastName ]
        |> List.choose nonblank
        |> String.concat " "
        |> Some
        |> nonblank

    nonblank result.displayName
    |> Option.orElse names
    |> Option.defaultValue result.orcid

let mapRecord orcid (record: OrcidRecord) : PersonResult =
    let name = record.person |> Option.bind (fun person -> person.name)

    let nameValue (selector: OrcidName -> OrcidValue option) =
        name
        |> Option.bind selector
        |> Option.bind (fun field -> field.value)
        |> nonblank

    let emails =
        record.person
        |> Option.bind (fun person -> person.emails)
        |> Option.bind (fun emails -> emails.email)
        |> optionalArray

    let affiliation =
        record.``activities-summary``
        |> Option.bind (fun activities -> activities.employments)
        |> Option.bind (fun employments -> employments.``affiliation-group``)
        |> optionalArray
        |> Array.collect (fun group -> group.summaries |> optionalArray)
        |> Array.choose (fun summary -> summary.``employment-summary``)
        |> Array.map (fun summary ->
            [|
                summary.organization |> Option.bind (fun organization -> organization.name)
                summary.``department-name``
            |]
            |> joined
        )
        |> joined

    {
        orcid = orcid
        firstName = nameValue (fun name -> name.``given-names``)
        lastName = nameValue (fun name -> name.``family-name``)
        displayName = nameValue (fun name -> name.``credit-name``)
        email = emails |> Array.tryPick (fun email -> nonblank email.email)
        affiliation = affiliation
    }

let mapExpandedResult (result: OrcidExpandedResult) : PersonResult option =
    result.``orcid-id``
    |> nonblank
    |> Option.bind (fun identifier ->
        match normalizeOrcid identifier with
        | Error _ -> None
        | Ok orcid ->
            Some {
                orcid = orcid
                firstName = nonblank result.``given-names``
                lastName = nonblank result.``family-names``
                displayName = nonblank result.``credit-name``
                email = result.email |> optionalArray |> Array.tryPick (Some >> nonblank)
                affiliation = result.``institution-name`` |> optionalArray |> Array.map Some |> joined
            }
    )

let mapExpandedPage start (response: OrcidExpandedResponse) : SearchPage =
    let rawResults = response.``expanded-result`` |> optionalArray
    let next = max 0 start + rawResults.Length

    let nextStart =
        response.``num-found``
        |> Option.bind (fun count ->
            if rawResults.Length = 10 && next < min count 10000 then
                Some next
            else
                None
        )

    {
        results = rawResults |> Array.choose mapExpandedResult
        nextStart = nextStart
    }

let httpError status =
    match status with
    | 401
    | 403 ->
        "Blocked: ORCID requires an access token or login for this request. This lookup is unavailable without credentials."
    | 429 -> "ORCID is receiving too many requests. Please try again later."
    | _ -> $"ORCID could not load people (HTTP {status}). Please try again."

let private requestOptions = [
    requestHeaders [ HttpRequestHeaders.Accept "application/vnd.orcid+json" ]
    Credentials RequestCredentials.Omit
]

let getRecord query : JS.Promise<SearchPage> = promise {
    match normalizeOrcid query with
    | Error error -> return failwith error
    | Ok orcid ->
        let url = "https://pub.orcid.org/v3.0/" + JS.encodeURIComponent orcid + "/record"
        let! response = fetchUnsafe url requestOptions

        if response.Status = 404 then
            return { results = [||]; nextStart = None }
        elif response.Status = 401 || response.Status = 403 then
            return raise (LookupBlocked(httpError response.Status))
        elif not response.Ok then
            return failwith (httpError response.Status)
        else
            let! payload = response.json<OrcidRecord> ()

            return {
                results = [| mapRecord orcid payload |]
                nextStart = None
            }
}

let searchByName query start : JS.Promise<SearchPage> = promise {
    if String.IsNullOrWhiteSpace query then
        return failwith "Enter a person's name."
    else
        let! response = fetchUnsafe (nameSearchUrl query start) requestOptions

        if response.Status = 404 then
            return { results = [||]; nextStart = None }
        elif response.Status = 401 || response.Status = 403 then
            return raise (LookupBlocked(httpError response.Status))
        elif not response.Ok then
            return failwith (httpError response.Status)
        else
            let! payload = response.json<OrcidExpandedResponse> ()
            return mapExpandedPage start payload
}

let mergePerson (person: Person) (result: PersonResult) =
    let merged = person.Copy()
    merged.ORCID <- nonblank (Some result.orcid) |> Option.orElse person.ORCID
    merged.FirstName <- nonblank result.firstName |> Option.orElse person.FirstName
    merged.LastName <- nonblank result.lastName |> Option.orElse person.LastName
    merged.EMail <- nonblank result.email |> Option.orElse person.EMail
    merged.Affiliation <- nonblank result.affiliation |> Option.orElse person.Affiliation
    merged
