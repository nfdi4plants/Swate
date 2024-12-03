module Update.UpdateUtil

open ARCtrl
open Shared
open SelectedColumns
open Fable.Remoting.Client

let download(filename, bytes:byte []) = bytes.SaveFileAs(filename)

let downloadFromString(filename, content:string) =
    let bytes = System.Text.Encoding.UTF8.GetBytes(content)
    bytes.SaveFileAs(filename)

module JsonImportHelper =

    open ARCtrl
    open JsonImport

    let updateWithMetadata (uploadedFile: ArcFiles) (state: SelectiveImportModalState) (selectedColumns: SelectedColumns []) =
        if not state.ImportMetadata then failwith "Metadata must be imported"
        /// This updates the existing tables based on import config (joinOptions)
        let createUpdatedTables (arcTables: ResizeArray<ArcTable>) =
            [
                for importTable in state.ImportTables do

                    let selectedColumn = selectedColumns.[importTable.Index]
                    let selectedColumnIndices =
                        selectedColumn.Columns
                        |> Array.mapi (fun i item -> if item = false then Some i else None)
                        |> Array.choose (fun x -> x)
                        |> List.ofArray

                    let sourceTable = arcTables.[importTable.Index]
                    let appliedTable = ArcTable.init(sourceTable.Name)

                    let finalTable = Table.selectiveTablePrepare appliedTable sourceTable selectedColumnIndices
                    let values =
                        sourceTable.Columns
                        |> Array.map (fun item -> item.Cells |> Array.map (fun itemi -> itemi.ToString()))
                    appliedTable.Join(finalTable, joinOptions=state.ImportType)
                    appliedTable
            ]
            |> ResizeArray
        let arcFile =
            match uploadedFile with
            | Assay a as arcFile ->
                let tables = createUpdatedTables a.Tables
                a.Tables <- tables
                arcFile
            | Study (s,_) as arcFile ->
                let tables = createUpdatedTables s.Tables
                s.Tables <- tables
                arcFile
            | Template t as arcFile ->
                let table = createUpdatedTables (ResizeArray[t.Table])
                t.Table <- table.[0]
                arcFile
            | Investigation _ as arcFile ->
                arcFile
        arcFile

    /// <summary>
    ///
    /// </summary>
    /// <param name="import"></param>
    /// <param name="importState"></param>
    /// <param name="activeTableIndex">Required to append imported tables to the active table.</param>
    /// <param name="existing"></param>
    let updateTables (import: ArcFiles) (importState: SelectiveImportModalState) (activeTableIndex: int option) (existingOpt: ArcFiles option) =
        match existingOpt with
        | Some existing ->
            let importTables =
                match import with
                | Assay a -> a.Tables
                | Study (s,_) -> s.Tables
                | Template t -> ResizeArray([t.Table])
                | Investigation _ -> ResizeArray()
            let existingTables =
                match existing with
                | Assay a -> a.Tables
                | Study (s,_) -> s.Tables
                | Template t -> ResizeArray([t.Table])
                | Investigation _ ->  ResizeArray()
            let appendTables =
                // only append if the active table exists (this is to handle join call on investigations)
                match activeTableIndex with
                | Some i when i >= 0 && i < existingTables.Count ->
                    let activeTable = existingTables.[i]
                    let tables = importState.ImportTables |> Seq.filter (fun x -> not x.FullImport) |> Seq.map (fun x -> importTables.[x.Index])
                    /// Everything will be appended against this table, which in the end will be appended to the main table
                    let tempTable = activeTable.Copy()
                    for table in tables do
                        let preparedTemplate = Table.distinctByHeader tempTable table
                        tempTable.Join(preparedTemplate, joinOptions=importState.ImportType)
                    existingTables.[i] <- tempTable
                | _ -> ()
            let addTables =
                importState.ImportTables
                |> Seq.filter (fun x -> x.FullImport)
                |> Seq.map (fun x -> importTables.[x.Index])
                |> Seq.map (fun table -> // update tables based on joinOptions
                    let nTable = ArcTable.init(table.Name)
                    nTable.Join(table, joinOptions=importState.ImportType)
                    nTable
                )
                |> Seq.rev // https://github.com/nfdi4plants/Swate/issues/577
                |> Seq.iter (fun table -> existingTables.Add table)
            existing
        | None -> //
            failwith "Error! Can only append information if metadata sheet exists!"

module JsonExportHelper =
    open ARCtrl
    open ARCtrl.Json

    let parseToJsonString (arcfile: ArcFiles, jef: JsonExportFormat) =
        let name, jsonString =
            let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_hhmmss")
            let nameFromId (id: string) = (n + "_" + id + ".json")
            match arcfile, jef with
            | Investigation ai, JsonExportFormat.ARCtrl -> nameFromId ai.Identifier, ArcInvestigation.toJsonString 0 ai
            | Investigation ai, JsonExportFormat.ARCtrlCompressed -> nameFromId ai.Identifier, ArcInvestigation.toCompressedJsonString 0 ai
            | Investigation ai, JsonExportFormat.ISA -> nameFromId ai.Identifier, ArcInvestigation.toISAJsonString 0 ai
            | Investigation ai, JsonExportFormat.ROCrate -> nameFromId ai.Identifier, ArcInvestigation.toROCrateJsonString 0 ai

            | Study (as',_), JsonExportFormat.ARCtrl -> nameFromId as'.Identifier, ArcStudy.toJsonString 0 (as')
            | Study (as',_), JsonExportFormat.ARCtrlCompressed -> nameFromId as'.Identifier, ArcStudy.toCompressedJsonString 0 (as')
            | Study (as',aaList), JsonExportFormat.ISA -> nameFromId as'.Identifier, ArcStudy.toISAJsonString (aaList,0) (as')
            | Study (as',aaList), JsonExportFormat.ROCrate -> nameFromId as'.Identifier, ArcStudy.toROCrateJsonString (aaList,0) (as')

            | Assay aa, JsonExportFormat.ARCtrl -> nameFromId aa.Identifier, ArcAssay.toJsonString 0 aa
            | Assay aa, JsonExportFormat.ARCtrlCompressed -> nameFromId aa.Identifier, ArcAssay.toCompressedJsonString 0 aa
            | Assay aa, JsonExportFormat.ISA -> nameFromId aa.Identifier, ArcAssay.toISAJsonString 0 aa
            | Assay aa, JsonExportFormat.ROCrate -> nameFromId aa.Identifier, ArcAssay.toROCrateJsonString () aa

            | Template t, JsonExportFormat.ARCtrl -> nameFromId t.FileName, Template.toJsonString 0 t
            | Template t, JsonExportFormat.ARCtrlCompressed -> nameFromId t.FileName, Template.toCompressedJsonString 0 t
            | Template _, anyElse -> failwithf "Error. It is not intended to parse Template to %s format." (string anyElse)
        name, jsonString