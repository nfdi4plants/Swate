module ISADotNet

open Shared
open OfficeInteropTypes
open TermTypes
open ISADotNet
open ISADotNet.Json

let assayJsonToRowMajorDM jsonString = 
    let assay = Assay.fromString jsonString
    let rowMajorFormat = AssayCommonAPI.RowWiseAssay.fromAssay assay
    rowMajorFormat

let rowMajorOfTemplateJson jsonString =
    let rowMajorAssay = assayJsonToRowMajorDM jsonString
    if rowMajorAssay.Sheets.Length <> 1 then
        failwith "Swate was unable to identify the information from the requested template (<Found more than one process in template>). Please open an issue for the developers."
    let template = rowMajorAssay.Sheets.Head
    template

type OntologyAnnotation with
    member this.toTermMinimal : TermMinimal option =
        match this.Name, Option.bind Regex.parseTermAccessionSimplified this.TermAccessionNumber with
        | Some name, Some tan   -> TermMinimal.create (AnnotationValue.toString name) tan |> Some
        | Some name, None       -> TermMinimal.create (AnnotationValue.toString name) "" |> Some
        | _,_                   -> None

type ISADotNet.MaterialAttributeValue with
    member this.toInsertBuildingBlock : InsertBuildingBlock =
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.CharacteristicType.IsSome then
                this.Category.Value.CharacteristicType.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics colHeaderTerm.Value.Name
        InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

type ISADotNet.FactorValue with
    member this.toInsertBuildingBlock =
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.FactorType.IsSome then
                this.Category.Value.FactorType.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics colHeaderTerm.Value.Name
        InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

type ISADotNet.ProcessParameterValue with
    member this.toInsertBuildingBlock =
        let colHeaderTerm =
            if this.Category.IsSome && this.Category.Value.ParameterName.IsSome then
                this.Category.Value.ParameterName.Value.toTermMinimal
            else
                None
        if colHeaderTerm.IsNone then failwith $"Could not find name for column header."
        if colHeaderTerm.Value.Name = "" then failwith $"Found empty name for column header."
        let unitTerm =
            if this.Unit.IsSome then this.Unit.Value.toTermMinimal else None
        let headerPrePrint = OfficeInteropTypes.BuildingBlockNamePrePrint.create BuildingBlockType.Characteristics colHeaderTerm.Value.Name
        InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

/// extend existing ISADotNet.Json.AssayCommonAPI.RowWiseSheet from ISADotNet library with
/// static member to map it to the Swate InsertBuildingBlock type used as input for addBuildingBlock functions
type AssayCommonAPI.RowWiseSheet with
    /// Map ISADotNet type to Swate OfficerInterop type.
    member this.toInsertBuildingBlockList : InsertBuildingBlock list =
        if this.Rows.Length <> 1 then failwith $"Swate encountered unknown template schema with {this.Rows.Length} rows. Excepted \"1\"."
        let headerRow = this.Rows.Head
        let factors = headerRow.FactorValues |> List.map (fun fv -> fv.toInsertBuildingBlock)
        let parameters = headerRow.ParameterValues |> List.map (fun ppv -> ppv.toInsertBuildingBlock)
        let characteristics = headerRow.CharacteristicValues |> List.map (fun mav -> mav.toInsertBuildingBlock)
        let newList = factors@parameters@characteristics
        newList

module TemplateMetadata =

    open System
    open System.IO
    open FSharpSpreadsheetML
    open ProtocolTemplateTypes

    type TemplateMetadata = {
        TemplateID      : Guid
        TemplateName    : string
        Version         : string
        Description     : string
        DocsLink        : string
        Organisation    : string
        ER              : string list
        Author          : string list
        Tags            : string list
        TableJson       : string
    } 

    //let parseMetadataFromByteArr (byteArray:byte []) =
    //    printfn "START"
    //    let ms = new MemoryStream(byteArray)
    //    let spreadsheet = Spreadsheet.fromStream ms false
    //    let sst = Spreadsheet.tryGetSharedStringTable spreadsheet
    //    let sheetOpt = Spreadsheet.tryGetSheetBySheetName ProtocolTemplateTypes.TemplateMetadataWorksheetName spreadsheet
    //    if sheetOpt.IsNone then failwith $"Could not find template metadata worksheet: {ProtocolTemplateTypes.TemplateMetadataWorksheetName}"
    //    printfn "0.5"
    //    let nRows = SheetData.getRows sheetOpt.Value
    //    printfn $"nRows = {Seq.length nRows}"
    //    /// Get all rows, but choose only those with a valid field key
    //    let sheetData =
    //        SheetData.getRows sheetOpt.Value
    //        |> Array.ofSeq
    //        |> Array.mapi (fun i row ->
    //            printfn $"row {i+1}"
    //            Row.getRowValues sst row |> Array.ofSeq
    //        )
    //        |> Array.choose (fun row ->
    //            printfn $"START c {row.ToString()}"
    //            if ProtocolTemplateTypes.MetadataFieldKeys.MetadataFieldKeysArray |> Array.contains row.[0] then
    //                if row.Length = 1 then
    //                    Some (row.[0],"")
    //                else
    //                    Some (row.[0],row.[1])
    //            else
    //                None
    //        )
    //    printfn "1"
    //    /// Check if all fields exist, else fail
    //    MetadataFieldKeys.MetadataFieldKeysArray
    //    |> Array.iter (fun key ->
    //        let isExisting = sheetData |> Array.exists (fun (k,v) -> k = key)
    //        if not isExisting then failwith $"Could not find template metadata key: {key}"
    //    )
    //    printfn "2"
    //    let dataMap = sheetData |> Map.ofArray
    //    let protocolTemplate = {
    //        TemplateID      =
    //            let v = dataMap.[MetadataFieldKeys.TemplateID]
    //            let isParsable,vGuid = Guid.TryParse(v)
    //            match isParsable with
    //            | true  -> vGuid
    //            | false -> failwith "Could not find parsable guid for template id"
    //        TemplateName    = dataMap.[MetadataFieldKeys.TemplateName]
    //        Version         = dataMap.[MetadataFieldKeys.Version].Trim()
    //        Author          = dataMap.[MetadataFieldKeys.TemplateName].Split(",", options = StringSplitOptions.TrimEntries) |> List.ofArray
    //        Description     = dataMap.[MetadataFieldKeys.Description].Trim()
    //        DocsLink        = dataMap.[MetadataFieldKeys.DocsLink].Trim()
    //        ER              = dataMap.[MetadataFieldKeys.ER].Split(",", options = StringSplitOptions.TrimEntries) |> List.ofArray
    //        Organisation    = dataMap.[MetadataFieldKeys.Organisation].Trim()
    //        Tags            = dataMap.[MetadataFieldKeys.Tags].Split(",", options = StringSplitOptions.TrimEntries) |> List.ofArray
    //        TableJson       = ""
    //    }
    //    protocolTemplate
            

