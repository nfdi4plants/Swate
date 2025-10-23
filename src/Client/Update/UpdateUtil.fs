module Update.UpdateUtil

open ARCtrl
open Swate.Components.Shared
open Fable.Remoting.Client
open OfficeInterop.Core

let download (filename, bytes: byte[]) = bytes.SaveFileAs(filename)

let downloadFromString (filename, content: string) =
    let bytes = System.Text.Encoding.UTF8.GetBytes(content)
    bytes.SaveFileAs(filename)

module JsonHelper =

    open ARCtrl.Json

    open Thoth.Json
    open Thoth.Json.Core

    //Have to add encoder and decoder for run and several subtypes
    let rec runEncoder (run: ArcRun) =
        [
            "Identifier", Encode.string run.Identifier |> Some
            Encode.tryInclude "Title" Encode.string run.Title
            Encode.tryInclude "Description" Encode.string run.Description
            Encode.tryInclude "MeasurementType" OntologyAnnotation.encoder run.MeasurementType
            Encode.tryInclude "TechnologyType" OntologyAnnotation.encoder run.TechnologyType
            Encode.tryInclude "TechnologyPlatform" OntologyAnnotation.encoder run.TechnologyPlatform
            Encode.tryInclude "DataMap" DataMap.encoder run.DataMap
            Encode.tryIncludeSeq "WorkflowIdentifiers" Encode.string run.WorkflowIdentifiers
            Encode.tryIncludeSeq "Tables" ArcTable.encoder run.Tables
            Encode.tryIncludeSeq "Performers" Person.encoder run.Performers
            Encode.tryIncludeSeq "Comments" Comment.encoder run.Comments
        ]
        |> Encode.choose
        |> Encode.object

    let runDecoder: Decoder<ArcRun> =
        Decode.object (fun get ->
            ArcRun.create(
                get.Required.Field("Identifier") Decode.string,
                ?title = get.Optional.Field "Title" Decode.string,
                ?description = get.Optional.Field "Description" Decode.string,
                ?measurementType = get.Optional.Field "MeasurementType" OntologyAnnotation.decoder,
                ?technologyType = get.Optional.Field "TechnologyType" OntologyAnnotation.decoder,
                ?technologyPlatform = get.Optional.Field "TechnologyPlatform" OntologyAnnotation.decoder,
                ?workflowIdentifiers = get.Optional.Field "WorkflowIdentifiers" (Decode.resizeArray Decode.string),
                ?tables = get.Optional.Field "Tables" (Decode.resizeArray ArcTable.decoder),
                ?datamap = get.Optional.Field "DataMap" DataMap.decoder,
                ?performers = get.Optional.Field "Performers" (Decode.resizeArray Person.decoder),
                ?comments = get.Optional.Field "Comments" (Decode.resizeArray Comment.decoder)
            ) 
        )

    //Have to add encoder and decoder for workflow and several subtypes
    let rec workflowEncoder (workflow: ArcWorkflow) =
        [
            "Identifier", Encode.string workflow.Identifier |> Some
            Encode.tryInclude "WorkflowType" OntologyAnnotation.encoder workflow.WorkflowType
            Encode.tryInclude "Title" Encode.string workflow.Title
            Encode.tryInclude "URI" Encode.string workflow.URI
            Encode.tryInclude "Description" Encode.string workflow.Description
            Encode.tryInclude "Version" Encode.string workflow.Version
            Encode.tryInclude "DataMap" DataMap.encoder workflow.DataMap
            Encode.tryIncludeSeq "SubWorkflowIdentifiers" Encode.string workflow.SubWorkflowIdentifiers
            Encode.tryIncludeSeq "Parameters" (ProtocolParameter.ISAJson.encoder None) workflow.Parameters
            Encode.tryIncludeSeq "Components" (Component.ISAJson.encoder None) workflow.Components
            Encode.tryIncludeSeq "Contacts" Person.encoder workflow.Contacts
            Encode.tryIncludeSeq "Comments" Comment.encoder workflow.Comments 
        ]
        |> Encode.choose
        |> Encode.object

    let workflowDecoder: Decoder<ArcWorkflow> =
        Decode.object (fun get ->
            ArcWorkflow.create(
                get.Required.Field("Identifier") Decode.string,
                ?title = get.Optional.Field "Title" Decode.string,
                ?description = get.Optional.Field "Description" Decode.string,
                ?workflowType = get.Optional.Field "WorkflowType" OntologyAnnotation.decoder,
                ?uri = get.Optional.Field "URI" Decode.string,
                ?version = get.Optional.Field "Version" Decode.string,
                ?subWorkflowIdentifiers = get.Optional.Field "SubWorkflowIdentifiers" (Decode.resizeArray Decode.string),
                ?parameters = get.Optional.Field "Parameters" (Decode.resizeArray ProtocolParameter.ISAJson.decoder),
                ?components = get.Optional.Field "Components" (Decode.resizeArray Component.ISAJson.decoder),
                ?datamap = get.Optional.Field "DataMap" DataMap.decoder,
                ?contacts = get.Optional.Field "Contacts" (Decode.resizeArray Person.decoder),
                ?comments = get.Optional.Field "Comments" (Decode.resizeArray Comment.decoder)
            )
        )

    let wholeDatamapEncoder(parentId: string) (parent: ARCtrl.ARCtrlHelper.DataMapParent) (datamap: ARCtrl.DataMap) =
        Encode.object [
            "ParentId", Encode.string parentId
            "Parent", Encode.string (parent.ToString())
            "Datamap", DataMap.encoder datamap
        ]

    let wholeDatamapDecoder =
        Decode.object (fun get ->               
            let parentId = 
                get.Required.Field "ParentId" Decode.string
            let parent = 
                let temp = get.Required.Field "Parent" Decode.string
                DataMapParent.tryFromString(temp)
            let datamapParent = createDataMapParentInfo parentId parent
            let datamap = get.Required.Field "Datamap" DataMap.decoder
            datamapParent, datamap
        )

module JsonImportHelper =

    open ARCtrl
    open FileImport

    /// <summary>
    ///
    /// </summary>
    /// <param name="arcTables"></param>
    /// <param name="state"></param>
    /// <param name="deselectedColumns"></param>
    /// <param name="fullImport"></param>
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
                    let appliedTable = ArcTable.init (sourceTable.Name)

                    let finalTable =
                        Table.selectiveTablePrepare appliedTable sourceTable deselectedColumnIndices

                    appliedTable.Join(finalTable, joinOptions = state.ImportType)
                    appliedTable
        ]
        |> ResizeArray

    /// <summary>
    ///
    /// </summary>
    /// <param name="uploadedFile"></param>
    /// <param name="state"></param>
    /// <param name="deselectedColumns"></param>
    let updateWithMetadata (uploadedFile: ArcFiles) (state: SelectiveImportConfig) (deselectedColumns: Set<int * int>) =
        if not state.ImportMetadata then
            failwith "Metadata must be imported"

        /// This updates the existing tables based on import config (joinOptions)
        let arcFile =
            match uploadedFile with
            | Assay a as arcFile ->
                let tables = createUpdatedTables a.Tables state deselectedColumns None
                a.Tables <- tables
                arcFile
            | Run r as arcFile ->
                let tables = createUpdatedTables r.Tables state deselectedColumns None
                r.Tables <- tables
                arcFile
            | Study(s, _) as arcFile ->
                let tables = createUpdatedTables s.Tables state deselectedColumns None
                s.Tables <- tables
                arcFile
            | Template t as arcFile ->
                let table = createUpdatedTables (ResizeArray[t.Table]) state deselectedColumns None
                t.Table <- table.[0]
                arcFile
            | Investigation _
            | Workflow _
            | DataMap _ as arcFile -> arcFile

        arcFile

    /// <summary>
    ///
    /// </summary>
    /// <param name="importTables"></param>
    /// <param name="importState"></param>
    /// <param name="activeTableIndex"></param>
    /// <param name="existingOpt"></param>
    /// <param name="deselectedColumns"></param>
    let updateTables
        (importTables: ResizeArray<ArcTable>)
        (importConfig: SelectiveImportConfig)
        (activeTableIndex: int option)
        (existingOpt: ArcFiles option)
        =
        let deselectedColumns = importConfig.DeselectedColumns

        match existingOpt with
        | Some existing ->
            let existingTables =
                match existing with
                | Assay a -> a.Tables
                | Study(s, _) -> s.Tables
                | Template t -> ResizeArray([ t.Table ])
                | Investigation _ -> ResizeArray()

            let appendTables =
                // only append if the active table exists (this is to handle join call on investigations)
                match activeTableIndex with
                | Some i when i >= 0 && i < existingTables.Count ->
                    let activeTable = existingTables.[i]

                    let selectedColumnTables =
                        createUpdatedTables importTables importConfig deselectedColumns (Some false)
                        |> Array.ofSeq
                        |> Array.rev

                    /// Everything will be appended against this table, which in the end will be appended to the main table
                    // Remove duplicate unique columns Input & Output
                    // Keep input & output columns of active table or first table that had them when appending
                    let tempTable = activeTable.Copy()

                    for table in selectedColumnTables do
                        let preparedTemplate = Table.distinctByHeader tempTable table
                        tempTable.Join(preparedTemplate, joinOptions = importConfig.ImportType)

                    existingTables.[i] <- tempTable
                | _ -> ()

            let addTables =
                let selectedColumnTables =
                    createUpdatedTables importTables importConfig deselectedColumns (Some true)
                    |> Array.ofSeq
                    |> Array.rev

                selectedColumnTables
                |> Seq.map (fun table -> // update tables based on joinOptions
                    let nTable = ArcTable.init (table.Name)
                    nTable.Join(table, joinOptions = importConfig.ImportType)
                    nTable)
                |> Seq.rev // https://github.com/nfdi4plants/Swate/issues/577
                |> Seq.iter (fun table -> existingTables.Add table)

            existing
        | None -> //
            failwith "Error! Can only append information if metadata sheet exists!"

    /// <summary>
    ///
    /// </summary>
    /// <param name="import"></param>
    /// <param name="importState"></param>
    /// <param name="activeTableIndex"></param>
    /// <param name="existingOpt"></param>
    /// <param name="deselectedColumns"></param>
    let updateArcFileTables
        (import: ArcFiles)
        (importState: SelectiveImportConfig)
        (activeTableIndex: int option)
        (existingOpt: ArcFiles option)
        =
        let importTables =
            match import with
            | Assay a -> a.Tables
            | Run r -> r.Tables
            | Study(s, _) -> s.Tables
            | Template t -> ResizeArray([ t.Table ])
            | DataMap _
            | Workflow _
            | Investigation _ -> ResizeArray()

        updateTables importTables importState activeTableIndex existingOpt

module JsonExportHelper =
    open ARCtrl
    open ARCtrl.Json

    /// <summary>
    ///
    /// </summary>
    /// <param name="arcfile"></param>
    /// <param name="jef"></param>
    let parseToJsonString (arcfile: ArcFiles, jef: JsonExportFormat) =
        let name, jsonString =
            let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")
            let nameFromId (id: string) = (n + "_" + id + ".json")

            match arcfile, jef with
            | Investigation ai, JsonExportFormat.ARCtrl -> nameFromId ai.Identifier, ArcInvestigation.toJsonString 0 ai
            | Investigation ai, JsonExportFormat.ARCtrlCompressed ->
                nameFromId ai.Identifier, ArcInvestigation.toCompressedJsonString 0 ai
            | Investigation ai, JsonExportFormat.ISA -> nameFromId ai.Identifier, ArcInvestigation.toISAJsonString 0 ai
            | Investigation ai, JsonExportFormat.ROCrate ->
                nameFromId ai.Identifier, ArcInvestigation.toROCrateJsonString 0 ai

            | Study(as', _), JsonExportFormat.ARCtrl -> nameFromId as'.Identifier, ArcStudy.toJsonString 0 (as')
            | Study(as', _), JsonExportFormat.ARCtrlCompressed ->
                nameFromId as'.Identifier, ArcStudy.toCompressedJsonString 0 (as')
            | Study(as', aaList), JsonExportFormat.ISA ->
                nameFromId as'.Identifier, ArcStudy.toISAJsonString (aaList, 0) (as')
            | Study(as', aaList), JsonExportFormat.ROCrate ->
                nameFromId as'.Identifier, ArcStudy.toROCrateJsonString (aaList, 0) (as')

            | Assay aa, JsonExportFormat.ARCtrl -> nameFromId aa.Identifier, ArcAssay.toJsonString 0 aa
            | Assay aa, JsonExportFormat.ARCtrlCompressed ->
                nameFromId aa.Identifier, ArcAssay.toCompressedJsonString 0 aa
            | Assay aa, JsonExportFormat.ISA -> nameFromId aa.Identifier, ArcAssay.toISAJsonString 0 aa
            | Assay aa, JsonExportFormat.ROCrate -> nameFromId aa.Identifier, ArcAssay.toROCrateJsonString () aa

            | Template t, JsonExportFormat.ARCtrl -> nameFromId t.FileName, Template.toJsonString 0 t
            | Template t, JsonExportFormat.ARCtrlCompressed ->
                nameFromId t.FileName, Template.toCompressedJsonString 0 t
            | Template _, anyElse ->
                failwithf "Error. It is not intended to parse Template to %s format." (string anyElse)
            | _ -> failwith $"Error, the selected type {arcfile} is not supported to be exported." //Have to implement the logic for run when toJsonString has been implemented for it

        name, jsonString