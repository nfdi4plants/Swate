module ISADotNet

open Shared
open OfficeInteropTypes
open TermTypes
open ISADotNet
open ISADotNet.Json

module Assay =

    open FsSpreadsheet.ExcelIO

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
                        |> fun ps -> Assay.create(ProcessSequence = ps)
                    )
                )
        )
        assay


/// Only use this function for protocol templates from db
let rowMajorOfTemplateJson jsonString =
    let assay = Assay.fromString jsonString
    let rowMajorFormat =
        //AssayCommonAPI.RowWiseAssay.fromAssay assay
        QueryModel.QAssay.fromAssay assay
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

open ISADotNet.QueryModel

type ISAValue with

    member this.toInsertBuildingBlock : (int * InsertBuildingBlock) =
        let buildingBlockType =
            if this.IsFactorValue then
                BuildingBlockType.Factor
            elif this.IsParameterValue then
                BuildingBlockType.Parameter
            elif this.IsCharacteristicValue then
                BuildingBlockType.Characteristic
            elif this.IsComponent then
                BuildingBlockType.Component
            else
                failwithf "This function should only ever be used to parse Factor/Parameter/Characteristic/Component, instead parsed: %A" this
        let colHeaderTermName =
            if this.HasCategory |> not then
                None
            else
                this.Category.toTermMinimal
        let columnPosition = getColumnPosition this.Category
        let unitTerm = if this.HasUnit then this.Unit.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create buildingBlockType colHeaderTermName.Value.Name
        let value = if this.HasValue then Array.singleton this.Value.toTermMinimal else [||]
        //printfn "%A" (if this.HasValue then box this.Value else box "")
        columnPosition, InsertBuildingBlock.create headerPrePrint colHeaderTermName unitTerm value


type IOType with
    member this.toBuildingBlockType =
        match this with
        | Source -> BuildingBlockType.Source
        | Sample -> BuildingBlockType.Sample
        | Data -> BuildingBlockType.Data
        | RawData -> BuildingBlockType.RawDataFile
        | ProcessedData -> BuildingBlockType.DerivedDataFile
        | anyElse -> failwith $"Cannot parse {anyElse} IsaDotNet IOType to BuildingBlockType."

/// extend existing ISADotNet.Json.AssayCommonAPI.RowWiseSheet from ISADotNet library with
/// static member to map it to the Swate InsertBuildingBlock type used as input for addBuildingBlock functions
//type AssayCommonAPI.RowWiseSheet with
type QueryModel.QSheet with

    /// Map ISADotNet type to Swate OfficerInterop type. Only done for first row.
    member this.headerToInsertBuildingBlockList : InsertBuildingBlock list =
        let headerRow = this.Rows.Head
        let rawCols = headerRow.Values().Values
        let cols = rawCols |> List.map (fun fv -> fv.toInsertBuildingBlock)
        cols
        |> List.sortBy fst
        |> List.map snd

    /// This function looses input and output names + Component [instrument model]
    member this.toInsertBuildingBlockList : InsertBuildingBlock list =
        let insertBuildingBlockRowList =
            this.Rows |> List.collect (fun r ->
                let cols =
                    let cols = r.Values().Values
                    cols |> List.map (fun fv -> fv.toInsertBuildingBlock)
                cols |> List.sortBy fst |> List.map snd
            )
        // Check if protocolREF column exists in assay. because this column is not represented as other columns in isa we need to infer this.
        let protocolRef =
            let sheetName, _ = Process.decomposeName this.SheetName
            let protocolNames = this.Protocols |> List.map (fun x -> x.Name, x)
            // if sheetname and any protocol name differ then we need to create the protocol ref column
            let hasProtocolRef = protocolNames |> List.exists (fun (name, _) -> name.IsSome && name.Value <> sheetName)
            if hasProtocolRef then
                // header must be protocol ref
                let header = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.ProtocolREF ""
                let rows =
                    this.Protocols
                    |> Array.ofList
                    |> Array.collect (fun protRef ->
                        // row range information is saved in comments and can be accessed + parsed by isadotnet function
                        let rowStart, rowEnd = protRef.GetRowRange()
                        [| for _ in rowStart .. rowEnd do
                            yield TermMinimal.create protRef.Name.Value "" |]
                    )
                InsertBuildingBlock.create header None None rows |> Some
            else
                None
                
        let protocolType =
            let hasProtocolType =
                let prots = this.Protocols |> List.choose (fun x -> x.ProtocolType)
                prots.Length > 0
            if hasProtocolType then
                // header must be protocol type
                let header = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.ProtocolType ""
                let columnTerm = Some BuildingBlockType.ProtocolType.getFeaturedColumnTermMinimal 
                let rows =
                    this.Protocols
                    |> Array.ofList
                    |> Array.collect (fun protType ->
                        // row range information is saved in comments and can be accessed + parsed by isadotnet function
                        let rowStart, rowEnd = protType.GetRowRange()
                        let tmEmpty = TermMinimal.create "" ""
                        [| for _ in rowStart .. rowEnd do
                            let hasValue = protType.ProtocolType.IsSome
                            yield
                                if hasValue then protType.ProtocolType.Value.toTermMinimal |> Option.defaultValue tmEmpty else tmEmpty
                        |]
                    )
                InsertBuildingBlock.create header columnTerm None rows |> Some
            else
                None

        /// https://github.com/nfdi4plants/ISADotNet/issues/80
        /// This needs to be fixed! For now we only have one input so we can assume "Source" but should this change we need to adapt.
        /// As Soon as this is fixed, create one function for both input and output with (string*IOType option) list as input.
        let input =
            if List.isEmpty this.Inputs then
                None
            else
                let names, types = this.Inputs |> List.unzip
                let inputType =
                    //let distinct = (List.choose id >> List.distinct) types
                    //try
                    //    distinct |> List.exactlyOne
                    //with
                    //    | _ -> failwith $"Cannot have input of multiple types: {distinct}"
                    IOType.Source.toBuildingBlockType
                let header = OfficeInteropTypes.BuildingBlockNamePrePrint.create inputType ""
                let rows = names |> List.map (fun x -> TermMinimal.create x "") |> Array.ofList
                InsertBuildingBlock.create header None None rows |> Some

        /// https://github.com/nfdi4plants/ISADotNet/issues/80
        let output =
            if List.isEmpty this.Outputs then
                None
            else
                let names, types = this.Outputs |> List.unzip
                //printfn "[OUTPUTS]: %A" this.Outputs
                //printfn "[OUTUT_TYPES]: %A" types
                let inputType =
                //    let distinct = (List.choose id >> List.distinct) types
                //    try
                //        distinct |> List.exactlyOne
                //    with
                //        | _ -> failwith $"Cannot have input of multiple types: {distinct}"
                //    |> fun d -> d.toBuildingBlockType
                    IOType.Sample.toBuildingBlockType
                let header = OfficeInteropTypes.BuildingBlockNamePrePrint.create inputType ""
                let rows = names |> List.map (fun x -> TermMinimal.create x "") |> Array.ofList
                InsertBuildingBlock.create header None None rows |> Some

        // group building block values by "InsertBuildingBlock" information (column information without values)
        insertBuildingBlockRowList
        |> List.groupBy (fun buildingBlock ->
            buildingBlock.ColumnHeader,buildingBlock.ColumnTerm,buildingBlock.UnitTerm
        )
        |> List.map (fun ((header,term,unit),buildingBlocks) ->
            let rows = buildingBlocks |> Array.ofList |> Array.collect (fun bb -> bb.Rows)
            InsertBuildingBlock.create header term unit rows
        )
        |> fun l -> // add special columns
            match protocolRef, protocolType with
            | None, None -> l
            | Some ref, Some t -> ref::t::l
            | Some ref, None -> ref::l
            | None, Some t -> t::l
        |> fun l -> // add input
            match input with
            | Some i -> i::l
            | None -> l
        |> fun l -> // add output
            match output with
            | Some o -> l@[o]
            | None -> l