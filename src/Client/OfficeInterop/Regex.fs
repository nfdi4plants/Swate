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
let UnitAccessionPattern = "#u.+?:\d+"

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

let parseUnitAccession (tag:string) =
    match tag with
    | Shared.HelperFunctions.Regex UnitAccessionPattern value ->
        value.Trim()
        |> Some
    | _ ->
        None

let parseColHeader (headerStr:string) =
    let coreName = parseCoreName headerStr
    let ontology = parseSquaredBrackets headerStr
    let tagArr = parseBrackets headerStr
    let isUnit, accessionOpt =
        match tagArr with
        | None ->
            false, None
        | Some ta ->
            let checkForAccession = ta |> Array.choose parseUnitAccession
            let isUnit = ta |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTagStart)
            match checkForAccession.Length with
            | 1 -> isUnit, checkForAccession.[0].Replace(ColumnTags.UnitTagStart,"") |> Some
            | _ -> isUnit, None
    {
        Header              = headerStr
        CoreName            = coreName
        Ontology            = ontology
        TagArr              = tagArr
        HasUnitAccession    = accessionOpt
        IsUnitCol           = isUnit
    }