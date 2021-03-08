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
let TermAccessionPattern = "#t[a-zA-Z0-9]+?:[a-zA-Z0-9]+"

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

module MinimalBuildingBlock =

    open Fable.Core

    open Types
    open Types.BuildingBlockTypes

    open System
    open Fable.SimpleXml
    open Fable.SimpleXml.Generator

    let ofExcelTableXml (xml:string) =
        let tableTag = "table"
        let xml = xml |> SimpleXml.parseDocument |> fun x -> x.Root
        let table = xml |> SimpleXml.tryFindElementByName tableTag
        if table.IsNone then failwith (sprintf "Could not find existing <%s> tag." tableTag)
        let tableName = table.Value.Attributes.["name"]
        //let tableLocation = table.Value.Attributes.["ref"]
        let cols = xml |> SimpleXml.findElementsByName "tableColumn"
        let colHeaders = cols |> List.map (fun x -> parseColHeader x.Attributes.["name"]) |> Array.ofList
        let rec parseToMinBB iterator (currentMinBBCol:MinimalBuildingBlock option) (minBBColList:MinimalBuildingBlock list) =
            if iterator >= colHeaders.Length then
                if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
            else
                let currentCol = colHeaders.[iterator]
                /// skip source name and sample name. These are always auto generated
                if
                    currentCol.CoreName.Value = ColumnCoreNames.Shown.Source
                    || currentCol.CoreName.Value = ColumnCoreNames.Shown.Sample
                then
                    let newMinBBColList =
                        if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
                    parseToMinBB (iterator+1) None newMinBBColList
                /// This case checks for non hidden main columns
                elif
                    currentCol.TagArr.IsNone
                    || currentCol.TagArr.Value |> Array.contains Types.ColumnTags.HiddenTag |> not
                then
                    let newCurrentMinBBCol =
                        let ont = if currentCol.Ontology.IsSome then sprintf " [%s]" currentCol.Ontology.Value.Name else ""
                        let name = sprintf "%s%s" currentCol.CoreName.Value ont
                        MinimalBuildingBlock.create name None None None None false |> Some
                    let newMinBBColList =
                        if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
                    parseToMinBB (iterator+1) newCurrentMinBBCol newMinBBColList
                /// This checks main col TSR and TAN
                elif
                    ( currentCol.CoreName.Value = Types.ColumnCoreNames.Hidden.TermAccessionNumber
                        || currentCol.CoreName.Value = Types.ColumnCoreNames.Hidden.TermSourceREF )
                    && currentCol.TagArr.IsSome && not currentCol.IsUnitCol
                then
                    let newCurrentMinBBCol =
                        let hasAccession = if currentCol.Ontology.IsSome then Some currentCol.Ontology.Value.TermAccession else None
                        if currentMinBBCol.IsNone then
                            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
                        { currentMinBBCol.Value with MainColumnTermAccession = hasAccession } |> Some
                    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
                /// Check unit main col
                elif
                    currentCol.IsUnitCol && currentCol.CoreName.Value = ColumnCoreNames.Hidden.Unit
                then
                    let newCurrentMinBBCol =
                        let name = if currentCol.Ontology.IsSome then currentCol.Ontology.Value.Name else ""
                        if currentMinBBCol.IsNone then
                            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
                        { currentMinBBCol.Value with UnitName = Some name } |> Some
                    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
                /// check unit TSR and TAN
                elif
                    currentCol.IsUnitCol && (currentCol.CoreName.Value = ColumnCoreNames.Hidden.TermAccessionNumber || currentCol.CoreName.Value = ColumnCoreNames.Hidden.TermSourceREF)
                then
                    let newCurrentMinBBCol =
                        let hasAccession = if currentCol.Ontology.IsSome then Some currentCol.Ontology.Value.TermAccession else None
                        if currentMinBBCol.IsNone then
                            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
                        { currentMinBBCol.Value with UnitTermAccession = hasAccession } |> Some
                    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
                else
                    failwith (sprintf "TableXml parser could not recognize column pattern (%s)" currentCol.Header)

        let reorderedMinBBList = parseToMinBB 0 None [] |> List.rev

        tableName,reorderedMinBBList
                                
                    
                
