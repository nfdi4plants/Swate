namespace Swate.Components

open ARCtrl
open Fable.Core
open Swate.Components.Shared

[<RequireQualifiedAccess>]
type WidgetHostView =
    | TableView
    | DataMapView
    | MetadataView
    | PreviewErrorView

[<RequireQualifiedAccess>]
type ARCObjectTarget =
    | Metadata
    | TableView of int
    | DataMap

[<RequireQualifiedAccess>]
module WidgetArcFile =

    let refreshRef (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation -> ArcFiles.Investigation investigation
        | ArcFiles.Study(study, assays) -> ArcFiles.Study(study, assays)
        | ArcFiles.Assay assay -> ArcFiles.Assay assay
        | ArcFiles.Run run -> ArcFiles.Run run
        | ArcFiles.Workflow workflow -> ArcFiles.Workflow workflow
        | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap)
        | ArcFiles.Template template -> ArcFiles.Template template

    let tryGetActiveTable (activeTableIndex: int option) (arcFile: ArcFiles) =
        match activeTableIndex with
        | Some tableIndex when tableIndex >= 0 && tableIndex < arcFile.Tables().Count ->
            Some(tableIndex, arcFile.Tables().[tableIndex])
        | _ -> None

    let tryGetDataMap (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Assay assay when assay.DataMap.IsSome -> Some assay.DataMap.Value
        | ArcFiles.Study(study, _) when study.DataMap.IsSome -> Some study.DataMap.Value
        | ArcFiles.Run run when run.DataMap.IsSome -> Some run.DataMap.Value
        | ArcFiles.DataMap(_, dataMap) -> Some dataMap
        | _ -> None

type FilePickerWidgetServices = {
    pickPaths: unit -> JS.Promise<Result<string[], string>>
}

type DataAnnotatorWidgetServices = {
    pickPaths: unit -> JS.Promise<Result<string[], string>>
    loadTextFile: string -> JS.Promise<Result<string, string>>
}

type TemplateWidgetServices = {
    loadTemplates: unit -> Async<Result<Template[], string>>
}

[<RequireQualifiedAccess>]
module ARCObjectTarget =

    let availableTargets (arcFile: ArcFiles) =
        [
            ARCObjectTarget.Metadata

            yield!
                arcFile.Tables()
                |> Seq.mapi (fun tableIndex _ -> ARCObjectTarget.TableView tableIndex)

            if WidgetArcFile.tryGetDataMap arcFile |> Option.isSome then
                ARCObjectTarget.DataMap
        ]

    let label (arcFile: ArcFiles) (target: ARCObjectTarget) =
        match target with
        | ARCObjectTarget.Metadata ->
            "Metadata"
        | ARCObjectTarget.TableView tableIndex ->
            let tables = arcFile.Tables()

            if tableIndex >= 0 && tableIndex < tables.Count then
                tables.[tableIndex].Name
            else
                $"Table {tableIndex + 1}"
        | ARCObjectTarget.DataMap ->
            "DataMap"

[<RequireQualifiedAccess>]
module WidgetTemplateImport =

    type ImportTable = {
        Index: int
        FullImport: bool
    }

    type SelectiveImportConfig = {
        ImportType: ARCtrl.TableJoinOptions
        ImportMetadata: bool
        ImportTables: ImportTable list
        DeselectedColumns: Set<int * int>
        TemplateName: string option
    } with

        static member init() = {
            ImportType = ARCtrl.TableJoinOptions.Headers
            ImportMetadata = false
            ImportTables = []
            DeselectedColumns = Set.empty
            TemplateName = None
        }

    [<RequireQualifiedAccess>]
    module TemplateImportMode =

        let options: (ARCtrl.TableJoinOptions * string * string)[] = [|
            ARCtrl.TableJoinOptions.Headers, "Headers", "Column Headers"
            ARCtrl.TableJoinOptions.WithUnit, "WithUnit", "With Units"
            ARCtrl.TableJoinOptions.WithValues, "WithValues", "With Values"
        |]

    let private getDeselectedTableColumnIndices (deselectedColumns: Set<int * int>) (tableIndex: int) =
        deselectedColumns
        |> Seq.choose (fun (candidateTableIndex, columnIndex) ->
            if candidateTableIndex = tableIndex then
                Some columnIndex
            else
                None
        )
        |> List.ofSeq

    let createUpdatedTables
        (arcTables: ResizeArray<ArcTable>)
        (state: SelectiveImportConfig)
        (deselectedColumns: Set<int * int>)
        fullImport
        =
        [
            for importTable in state.ImportTables do
                let fullImport = defaultArg fullImport importTable.FullImport

                if importTable.FullImport = fullImport then
                    let deselectedColumnIndices =
                        getDeselectedTableColumnIndices deselectedColumns importTable.Index

                    let sourceTable = arcTables.[importTable.Index]
                    let appliedTable = ArcTable.init sourceTable.Name

                    let finalTable =
                        Table.selectiveTablePrepare appliedTable sourceTable deselectedColumnIndices

                    appliedTable.Join(finalTable, joinOptions = state.ImportType)
                    appliedTable
        ]
        |> ResizeArray

    let updateTables
        (importTables: ResizeArray<ArcTable>)
        (importConfig: SelectiveImportConfig)
        (activeTableIndex: int option)
        (existingOpt: ArcFiles option)
        =
        let deselectedColumns = importConfig.DeselectedColumns

        match existingOpt with
        | Some existing ->
            let existingTables = existing.Tables()

            match activeTableIndex with
            | Some tableIndex when tableIndex >= 0 && tableIndex < existingTables.Count ->
                let activeTable = existingTables.[tableIndex]

                let selectedColumnTables =
                    createUpdatedTables importTables importConfig deselectedColumns (Some false)
                    |> Array.ofSeq
                    |> Array.rev

                let tempTable = activeTable.Copy()

                for table in selectedColumnTables do
                    if table.RowCount = 0 then
                        let cells =
                            table.Columns
                            |> Array.ofSeq
                            |> Array.map (fun column ->
                                match column.Header with
                                | CompositeHeader.Factor _
                                | CompositeHeader.Component _
                                | CompositeHeader.Parameter _
                                | CompositeHeader.Characteristic _ ->
                                    match importConfig.ImportType with
                                    | TableJoinOptions.WithUnit ->
                                        CompositeCell.Unitized("", OntologyAnnotation.empty ())
                                    | _ ->
                                        CompositeCell.Term(OntologyAnnotation.empty ())
                                | _ ->
                                    CompositeCell.FreeText ""
                            )
                            |> ResizeArray

                        let rows = Array.create tempTable.RowCount cells
                        table.AddRows(ResizeArray rows)
                    elif table.RowCount < tempTable.RowCount then
                        table.AddRowsEmpty(tempTable.RowCount - table.RowCount)

                    let preparedTemplate = Table.distinctByHeader tempTable table
                    tempTable.Join(preparedTemplate, joinOptions = importConfig.ImportType)

                existingTables.[tableIndex] <- tempTable
            | _ -> ()

            let selectedColumnTables =
                createUpdatedTables importTables importConfig deselectedColumns (Some true)
                |> Array.ofSeq
                |> Array.rev

            selectedColumnTables
            |> Seq.map (fun table ->
                let nextTable = ArcTable.init table.Name
                nextTable.Join(table, joinOptions = importConfig.ImportType)
                nextTable
            )
            |> Seq.rev
            |> Seq.iter existingTables.Add

            existing
        | None ->
            failwith "Error! Can only append information if metadata sheet exists!"
