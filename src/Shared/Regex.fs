namespace Shared

module Regex =

    module Pattern =

        [<LiteralAttribute>]
        let HashNumberPattern = "#\d+"

        [<LiteralAttribute>]
        /// This pattern captures all characters between squared brackets (with squared brackets).
        let SquaredBracketsPattern = "\[.*\]"

        [<LiteralAttribute>]
        /// This pattern captures all characters between brackets (with brackets).
        let BracketsPattern = "\([^\]]*\)"

        [<LiteralAttribute>]
        let DoubleQuotesPattern = "\"(.*?)\""

        [<LiteralAttribute>]
        /// This pattern captures all input coming before an opening square bracket or normal bracket (with whitespace).
        let CoreNamePattern = "^[^[(]*"

        [<LiteralAttribute>]
        let TermAccessionPattern = "[a-zA-Z0-9]+?:[a-zA-Z0-9]+"

    module Aux =
    
        open System.Text.RegularExpressions
    
        /// (|Regex|_|) pattern input
        let (|Regex|_|) pattern input =
            let m = Regex.Match(input, pattern)
            if m.Success then Some(m.Value)
            else None

    open Pattern
    open Aux
    open System
    open System.Text.RegularExpressions

    let parseSquaredBrackets (headerStr:string) =
        match headerStr with
        | Regex SquaredBracketsPattern value ->
            // remove brackets
            value.[1..value.Length-2]
            |> Some
        | _ ->
            None

    let parseBrackets (headerStr:string) =
        match headerStr with
        | Regex BracketsPattern value   -> Some value.[1..value.Length-2] // remove brackets
        | _                             -> None

    let parseCoreName (headerStr:string) =
        match headerStr with
        | Regex CoreNamePattern value ->
            value.Trim()
            |> Some
        | _ ->
            None

    let parseTermAccession (headerStr:string) =
        match headerStr with
        | Regex TermAccessionPattern value ->
            value.Trim()
            |> Some
        | _ ->
            None

    let parseDoubleQuotes (headerStr:string) =
        match headerStr with
        | Regex DoubleQuotesPattern value ->
            // remove quotes at beginning and end of matched string
            value.[1..value.Length-2].Trim()
            |> Some
        | _ ->
            None

    let removeId (squareBracket:string) =
        Regex.Replace(squareBracket, HashNumberPattern, "") 
       

//module MinimalBuildingBlock =

//    open Fable.Core

//    open Types
//    open Types.BuildingBlockTypes

//    open System
//    open Fable.SimpleXml
//    open Fable.SimpleXml.Generator

//    let ofExcelTableXml (xml:string) =
//        let tableTag = "table"
//        let xml = xml |> SimpleXml.parseDocument |> fun x -> x.Root
//        let table = xml |> SimpleXml.tryFindElementByName tableTag
//        if table.IsNone then failwith (sprintf "Could not find existing <%s> tag." tableTag)
//        let tableName = table.Value.Attributes.["name"]
//        //let tableLocation = table.Value.Attributes.["ref"]
//        let cols = xml |> SimpleXml.findElementsByName "tableColumn"
//        let colHeaders = cols |> List.map (fun x -> parseColHeader x.Attributes.["name"]) |> Array.ofList
//        let rec parseToMinBB iterator (currentMinBBCol:MinimalBuildingBlock option) (minBBColList:MinimalBuildingBlock list) =
//            if iterator >= colHeaders.Length then
//                if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
//            else
//                let currentCol = colHeaders.[iterator]
//                /// skip source name and sample name. These are always auto generated
//                if
//                    currentCol.CoreName.Value = ColumnCoreNames.Shown.Source
//                    || currentCol.CoreName.Value = ColumnCoreNames.Shown.Sample
//                then
//                    let newMinBBColList =
//                        if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
//                    parseToMinBB (iterator+1) None newMinBBColList
//                /// This case checks for non hidden main columns
//                elif
//                    currentCol.TagArr.IsNone
//                    || currentCol.TagArr.Value |> Array.contains Types.ColumnTags.HiddenTag |> not
//                then
//                    let newCurrentMinBBCol =
//                        let ont = if currentCol.Ontology.IsSome then sprintf " [%s]" currentCol.Ontology.Value.Name else ""
//                        let name = sprintf "%s%s" currentCol.CoreName.Value ont
//                        MinimalBuildingBlock.create name None None None false |> Some
//                    let newMinBBColList =
//                        if currentMinBBCol.IsSome then currentMinBBCol.Value::minBBColList else minBBColList
//                    parseToMinBB (iterator+1) newCurrentMinBBCol newMinBBColList
//                /// This checks main col TSR and TAN
//                elif
//                    ( currentCol.CoreName.Value = Types.ColumnCoreNames.Hidden.TermAccessionNumber
//                        || currentCol.CoreName.Value = Types.ColumnCoreNames.Hidden.TermSourceREF )
//                    && currentCol.TagArr.IsSome && not currentCol.IsUnitCol
//                then
//                    let newCurrentMinBBCol =
//                        let hasAccession = if currentCol.Ontology.IsSome then Some currentCol.Ontology.Value.TermAccession else None
//                        if currentMinBBCol.IsNone then
//                            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
//                        { currentMinBBCol.Value with ColumnTermAccession = hasAccession } |> Some
//                    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
//                /// Check unit main col
//                elif
//                    currentCol.IsUnitCol && currentCol.CoreName.Value = ColumnCoreNames.Hidden.Unit
//                then
//                    let newCurrentMinBBCol =
//                        let name = if currentCol.Ontology.IsSome then currentCol.Ontology.Value.Name else ""
//                        if currentMinBBCol.IsNone then
//                            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
//                        { currentMinBBCol.Value with UnitName = Some name } |> Some
//                    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
//                ///// check unit TSR and TAN
//                //elif
//                //    currentCol.IsUnitCol && (currentCol.CoreName.Value = ColumnCoreNames.Hidden.TermAccessionNumber || currentCol.CoreName.Value = ColumnCoreNames.Hidden.TermSourceREF)
//                //then
//                //    let newCurrentMinBBCol =
//                //        if currentMinBBCol.IsNone then
//                //            failwith (sprintf "TableXml parser found unknown column pattern and tried to add column (%s) without previous main column." currentCol.Header)
//                //        { currentMinBBCol.Value with UnitTermAccession = hasAccession } |> Some
//                //    parseToMinBB (iterator+1) newCurrentMinBBCol minBBColList
//                else
//                    failwith (sprintf "TableXml parser could not recognize column pattern (%s)" currentCol.Header)

//        let reorderedMinBBList = parseToMinBB 0 None [] |> List.rev

//        tableName,reorderedMinBBList
                                
                    
                
