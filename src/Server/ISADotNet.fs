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
        match this.Name, Option.bind Regex.parseTermAccession this.TermAccessionNumber with
        | Some name, Some tan   -> TermMinimal.create (AnnotationValue.toString name) tan |> Some
        | Some name, None       -> TermMinimal.create (AnnotationValue.toString name) "" |> Some
        | _,_                   -> None

type ISADotNet.MaterialAttributeValue with
    member this.toInsertBuildingBlock : BuildingBlockTypes.InsertBuildingBlock =
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
        BuildingBlockTypes.InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

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
        BuildingBlockTypes.InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

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
        BuildingBlockTypes.InsertBuildingBlock.create headerPrePrint colHeaderTerm unitTerm

/// extend existing ISADotNet.Json.AssayCommonAPI.RowWiseSheet from ISADotNet library with
/// static member to map it to the Swate InsertBuildingBlock type used as input for addBuildingBlock functions
type AssayCommonAPI.RowWiseSheet with
    /// Map ISADotNet type to Swate OfficerInterop type.
    member this.toInsertBuildingBlockList : BuildingBlockTypes.InsertBuildingBlock list =
        if this.Rows.Length <> 1 then failwith $"Swate encountered unknown template schema with {this.Rows.Length} rows. Excepted \"1\"."
        let headerRow = this.Rows.Head
        let factors = headerRow.FactorValues |> List.map (fun fv -> fv.toInsertBuildingBlock)
        let parameters = headerRow.ParameterValues |> List.map (fun ppv -> ppv.toInsertBuildingBlock)
        let characteristics = headerRow.CharacteristicValues |> List.map (fun mav -> mav.toInsertBuildingBlock)
        factors@parameters@characteristics