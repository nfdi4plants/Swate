module ISADotNet

open Shared
open OfficeInteropTypes
open TermTypes
open ISADotNet
open ISADotNet.Json

module Assay =

    open FSharpSpreadsheetML

    /// Reads an assay from an xlsx spreadsheetdocument
    ///
    /// As factors and protocols are used for the investigation file, they are returned individually
    ///
    /// The persons from the metadata sheet are returned independently as they are not a part of the assay object
    let fromTemplateSpreadsheet (doc:DocumentFormat.OpenXml.Packaging.SpreadsheetDocument,tableName:string) = 
    
        let sst = Spreadsheet.tryGetSharedStringTable doc  
    
        // All sheetnames in the spreadsheetDocument
        let sheetNames = 
            Spreadsheet.getWorkbookPart doc
            |> Workbook.get
            |> Sheet.Sheets.get
            |> Sheet.Sheets.getSheets
            |> Seq.map Sheet.getName
    
        let assay =
            sheetNames
            |> Seq.tryPick (fun sheetName ->                    
                Spreadsheet.tryGetWorksheetPartBySheetName sheetName doc
                |> Option.bind (fun wsp ->
                    Table.tryGetByNameBy (fun s -> s = tableName) wsp
                    |> Option.map (fun table ->
                        let sheet = Worksheet.getSheetData wsp.Worksheet
                        let headers = Table.getColumnHeaders table
                        let m = Table.toSparseValueMatrix sst sheet table
                        XLSX.AssayFile.Process.fromSparseMatrix sheetName headers m
                        |> fun (_,_,_,ps) -> Assay.create(ProcessSequence = List.ofSeq ps)
                    )
                )
        )
        assay


/// Only use this function for protocol templates from db
let rowMajorOfTemplateJson jsonString =
    let assay = Assay.fromString jsonString
    let rowMajorFormat = AssayCommonAPI.RowWiseAssay.fromAssay assay
    if rowMajorFormat.Sheets.Length <> 1 then
        failwith "Swate was unable to identify the information from the requested template (<Found more than one process in template>). Please open an issue for the developers."
    let template = rowMajorFormat.Sheets.Head
    template

let private ColumnPositionCommentName = "ValueIndex"

type OntologyAnnotation with
    member this.toTermMinimal =
        match this.Name, Option.bind Regex.parseTermAccession this.TermAccessionNumber with
        | Some name, Some tan   -> TermMinimal.create (AnnotationValue.toString name) tan |> Some
        | Some name, None       -> TermMinimal.create (AnnotationValue.toString name) "" |> Some
        | _,_                   -> None

type ISADotNet.Value with
    member this.toTermMinimal =
        let nameOpt,tanUrlOpt,tsrOpt = ISADotNet.Value.toOptions this
        let name = nameOpt |> Option.defaultValue ""
        let tan =
            if tanUrlOpt.IsSome then
                Regex.parseTermAccession tanUrlOpt.Value |> Option.map (fun tan -> tan.Replace("_",":")) |> Option.defaultValue ""
            else
                ""
        TermMinimal.create name tan

let getColumnPosition (oa:OntologyAnnotation) =
    let c = oa.Comments |> Option.map (List.find (fun c -> c.Name = Some ColumnPositionCommentName))
    c.Value.Value.Value |> int

type ISADotNet.MaterialAttributeValue with

    member this.toInsertBuildingBlock : (int * InsertBuildingBlock) =
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.CharacteristicType.IsSome then
                this.Category.Value.CharacteristicType.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let columnPosition = getColumnPosition this.Category.Value.CharacteristicType.Value
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics colHeaderTerm.Value.Name
        let value =
            match this.Value with
            | Some v    -> Array.singleton v.toTermMinimal
            | None      -> Array.empty
        columnPosition, InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm value

type ISADotNet.FactorValue with

    member this.toInsertBuildingBlock : (int * InsertBuildingBlock)=
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.FactorType.IsSome then
                this.Category.Value.FactorType.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let columnPosition = getColumnPosition this.Category.Value.FactorType.Value
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Factor colHeaderTerm.Value.Name
        let value =
            match this.Value with
            | Some v    -> Array.singleton v.toTermMinimal
            | None      -> Array.empty
        columnPosition, InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm value

type ISADotNet.ProcessParameterValue with

    member this.toInsertBuildingBlock : (int * InsertBuildingBlock) =
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.ParameterName.IsSome then
                this.Category.Value.ParameterName.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let columnPosition = getColumnPosition this.Category.Value.ParameterName.Value
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Parameter colHeaderTerm.Value.Name
        let value =
            match this.Value with
            | Some v    -> Array.singleton v.toTermMinimal
            | None      -> Array.empty
        columnPosition, InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm value

/// extend existing ISADotNet.Json.AssayCommonAPI.RowWiseSheet from ISADotNet library with
/// static member to map it to the Swate InsertBuildingBlock type used as input for addBuildingBlock functions
type AssayCommonAPI.RowWiseSheet with

    /// Map ISADotNet type to Swate OfficerInterop type. Only done for first row.
    member this.headerToInsertBuildingBlockList : InsertBuildingBlock list =
        let headerRow = this.Rows.Head
        let factors = headerRow.FactorValues |> List.map (fun fv -> fv.toInsertBuildingBlock)
        let parameters = headerRow.ParameterValues |> List.map (fun ppv -> ppv.toInsertBuildingBlock)
        let characteristics = headerRow.CharacteristicValues |> List.map (fun mav -> mav.toInsertBuildingBlock)
        let newList = factors@parameters@characteristics
        newList
        |> List.sortBy fst
        |> List.map snd

    member this.toInsertBuildingBlockList : InsertBuildingBlock list =
        let insertBuildingBlockRowList =
            this.Rows |> List.collect (fun r -> 
                let factors = r.FactorValues |> List.map (fun fv -> fv.toInsertBuildingBlock)
                let parameters = r.ParameterValues |> List.map (fun ppv -> ppv.toInsertBuildingBlock)
                let characteristics = r.CharacteristicValues |> List.map (fun mav -> mav.toInsertBuildingBlock)
                let newList = factors@parameters@characteristics
                newList |> List.sortBy fst |> List.map snd
            )
        // group building block values by "InsertBuildingBlock" information (column information without values)
        insertBuildingBlockRowList
        |> List.groupBy (fun buildingBlock ->
            buildingBlock.ColumnHeader,buildingBlock.ColumnTerm,buildingBlock.UnitTerm
        )
        |> List.map (fun ((header,term,unit),buildingBlocks) ->
            let rows = buildingBlocks |> Array.ofList |> Array.collect (fun bb -> bb.Rows)
            InsertBuildingBlock.create header term unit rows
        )
