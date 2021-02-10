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

// currently unused
[<LiteralAttribute>]
let UnitAccessionPattern = "#u.+?:\d+"

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

// currently unused
let parseUnitAccession (tag:string) =
    match tag with
    | Shared.HelperFunctions.Regex UnitAccessionPattern value ->
        value.Trim()
        |> Some
    | _ ->
        None

// currently unused
let parseGroup (tag:string) =
    match tag with
    | Shared.HelperFunctions.Regex GroupPattern value ->
        value.Trim().Replace("#g","") |> Some
    | _ -> None

let parseColHeader (headerStr:string) =
    let coreName = parseCoreName headerStr
    let ontology = parseSquaredBrackets headerStr
    let tagArr = parseBrackets headerStr
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