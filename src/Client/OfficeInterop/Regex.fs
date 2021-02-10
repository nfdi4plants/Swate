module OfficeInterop.Regex

open System

open Types

[<LiteralAttribute>]
let HashNumberPattern = "#\d+"

[<LiteralAttribute>]
let SquaredBracketsPattern = "\[[^\]]*\]"

[<LiteralAttribute>]
let BracketsPattern = "\([^\]]*\)"

[<LiteralAttribute>]
let CoreNamePattern = "^[^[(]*"

[<LiteralAttribute>]
let TermAccessionPattern = "#t.+?:\d+"

// currently unused
[<LiteralAttribute>]
let GroupPattern = "#g[a-zA-Z0-9]+"

let parseSquaredBrackets (headerStr:string) =
    match headerStr with
    | Shared.HelperFunctions.Regex SquaredBracketsPattern value ->
        // remove brackets
        value.[1..value.Length-2]
        |> Some
    | _ ->
        None

let parseBrackets (headerStr:string) =
    match headerStr with
    | Shared.HelperFunctions.Regex BracketsPattern value ->
        value
            // remove brackets
            .[1..value.Length-2]
            // split by separator to get information array
            // can consist of e.g. #h, #id, parentOntology
            .Split([|"; "|], StringSplitOptions.None)
        |> Some
    | _ ->
        None

let parseCoreName (headerStr:string) =
    match headerStr with
    | Shared.HelperFunctions.Regex CoreNamePattern value ->
        value.Trim()
        |> Some
    | _ ->
        None

let parseTermAccession (tag:string) =
    match tag with
    | Shared.HelperFunctions.Regex TermAccessionPattern value ->
        value.Trim()
        |> Some
    | _ ->
        None

open Shared

let parseColHeader (headerStr:string) =
    let coreName = parseCoreName headerStr
    let tagArr = parseBrackets headerStr
    let ontology =
        let hasOnt = parseSquaredBrackets headerStr
        let termAccession =
            match tagArr with
            | None -> None
            | Some ta ->
                let hasAccession = ta |> Array.tryFind (fun x -> x.StartsWith ColumnTags.TermAccessionTag)
                if hasAccession.IsSome && (parseTermAccession hasAccession.Value).IsSome
                then hasAccession.Value.Replace(Types.ColumnTags.TermAccessionTag,"") |> Some
                else None
        match hasOnt,termAccession with
        | Some ontName, None    -> OntologyInfo.create ontName "" |> Some
        | Some ontName, Some ta -> OntologyInfo.create ontName ta |> Some
        | _,_                   -> None

    let isUnit =
        match tagArr with
        | None -> false
        | Some ta ->
            let isUnit = ta |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTag)
            isUnit
    {
        Header              = headerStr
        CoreName            = coreName
        Ontology            = ontology
        TagArr              = tagArr
        IsUnitCol           = isUnit
    }