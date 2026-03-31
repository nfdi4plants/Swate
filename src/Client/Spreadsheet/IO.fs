module Spreadsheet.IO

open ARCtrl
open ARCtrl.ARCtrlHelper
open ARCtrl.Spreadsheet
open Swate.Components.Shared

module Xlsx =

    let readFromBytes (bytes: byte[]) =
        // Try each conversion function and return the first successful result
        promise {
            let! fswb = FsSpreadsheet.Js.Xlsx.fromXlsxBytes bytes
            let ws = fswb.GetWorksheets()

            let arcfile =
                match ws with
                | _ when ws.Exists(fun ws -> ARCtrl.Spreadsheet.ArcAssay.isMetadataSheetName ws.Name) ->
                    ArcAssay.fromFsWorkbook fswb |> Assay
                | _ when ws.Exists(fun ws -> ARCtrl.Spreadsheet.ArcStudy.isMetadataSheetName ws.Name) ->
                    ArcStudy.fromFsWorkbook fswb |> Study
                | _ when ws.Exists(fun ws -> ARCtrl.Spreadsheet.ArcInvestigation.isMetadataSheetName ws.Name) ->
                    ArcInvestigation.fromFsWorkbook fswb |> Investigation
                | _ when
                    ws.Exists(fun ws ->
                        ARCtrl.Spreadsheet.Template.metadataSheetName = ws.Name
                        || ARCtrl.Spreadsheet.Template.obsoleteMetadataSheetName = ws.Name
                    )
                    ->
                    ARCtrl.Spreadsheet.Template.fromFsWorkbook fswb |> Template
                | _ when ws.Exists(fun ws -> ws.Name.ToLower().Contains("datamap")) ->
                    let datamap = DataMap.fromFsWorkbook fswb
                    DataMap(Some(DatamapParentInfo.create "default" DataMapParent.Assay), datamap)

                //Adapt to FSWorkBook and FromFSWorkbook of ARCtrl to include DatamapParentInfo
                //match ws with
                //| _ when ws.Exists(fun ws -> ARCtrl.Spreadsheet.ArcAssay.isMetadataSheetName ws.Name) ->
                //    let assay = ArcAssay.fromFsWorkbook fswb
                //    DataMap(Some(createDataMapParentInfo assay.Identifier DataMapParent.Assay), datamap)
                //| _ when ws.Exists(fun ws -> ARCtrl.Spreadsheet.ArcStudy.isMetadataSheetName ws.Name) ->
                //    let (study, _) = ArcStudy.fromFsWorkbook fswb
                //    DataMap(Some(createDataMapParentInfo study.Identifier DataMapParent.Study), datamap)
                //| _ -> failwith "ws must be from assay or study to contain a datamap!"
                | _ -> failwith "Unable to identify given file. Missing metadata sheet with correct name."

            return arcfile
        }

module Json =

    open ARCtrl.Json

    let readFromJsonMap =
        Map [
            (ArcFilesDiscriminate.Investigation, JsonExportFormat.ARCtrl),
            fun json -> ArcInvestigation.fromJsonString json |> ArcFiles.Investigation
            (ArcFilesDiscriminate.Investigation, JsonExportFormat.ARCtrlCompressed),
            fun json -> ArcInvestigation.fromCompressedJsonString json |> ArcFiles.Investigation
            (ArcFilesDiscriminate.Investigation, JsonExportFormat.ISA),
            fun json -> ArcInvestigation.fromISAJsonString json |> ArcFiles.Investigation
            (ArcFilesDiscriminate.Investigation, JsonExportFormat.ROCrate),
            fun json -> ArcInvestigation.fromROCrateJsonString json |> ArcFiles.Investigation

            (ArcFilesDiscriminate.Study, JsonExportFormat.ARCtrl),
            fun json -> ArcStudy.fromJsonString json |> fun x -> ArcFiles.Study(x, [])
            (ArcFilesDiscriminate.Study, JsonExportFormat.ARCtrlCompressed),
            fun json -> ArcStudy.fromCompressedJsonString json |> fun x -> ArcFiles.Study(x, [])
            (ArcFilesDiscriminate.Study, JsonExportFormat.ISA),
            fun json -> ArcStudy.fromISAJsonString json |> ArcFiles.Study
            (ArcFilesDiscriminate.Study, JsonExportFormat.ROCrate),
            fun json -> ArcStudy.fromROCrateJsonString json |> ArcFiles.Study

            (ArcFilesDiscriminate.Assay, JsonExportFormat.ARCtrl),
            fun json -> ArcAssay.fromJsonString json |> ArcFiles.Assay
            (ArcFilesDiscriminate.Assay, JsonExportFormat.ARCtrlCompressed),
            fun json -> ArcAssay.fromCompressedJsonString json |> ArcFiles.Assay
            (ArcFilesDiscriminate.Assay, JsonExportFormat.ISA),
            fun json -> ArcAssay.fromISAJsonString json |> ArcFiles.Assay
            (ArcFilesDiscriminate.Assay, JsonExportFormat.ROCrate),
            fun json -> ArcAssay.fromROCrateJsonString json |> ArcFiles.Assay

            (ArcFilesDiscriminate.Template, JsonExportFormat.ARCtrl),
            fun json -> Template.fromJsonString json |> ArcFiles.Template
            (ArcFilesDiscriminate.Template, JsonExportFormat.ARCtrlCompressed),
            fun json -> Template.fromCompressedJsonString json |> ArcFiles.Template

            (ArcFilesDiscriminate.Run, JsonExportFormat.ARCtrl), fun json -> ArcRun.fromJsonString json |> ArcFiles.Run
            (ArcFilesDiscriminate.Run, JsonExportFormat.ARCtrlCompressed),
            fun json -> ArcRun.fromCompressedJsonString json |> ArcFiles.Run

            (ArcFilesDiscriminate.Workflow, JsonExportFormat.ARCtrl),
            fun json -> ArcWorkflow.fromJsonString json |> ArcFiles.Workflow
            (ArcFilesDiscriminate.Workflow, JsonExportFormat.ARCtrlCompressed),
            fun json -> ArcWorkflow.fromCompressedJsonString json |> ArcFiles.Workflow

            (ArcFilesDiscriminate.DataMap, JsonExportFormat.ARCtrl),
            fun json -> ArcFiles.DataMap(None, DataMap.fromJsonString json)
        ]
