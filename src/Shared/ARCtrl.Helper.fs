namespace Swate.Components.Shared

open System
open Fable.Core
open ARCtrl

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    [<RequireQualifiedAccess; StringEnum>]
    type ArcFilesDiscriminate =
        | [<CompiledName("investigation")>] Investigation
        | [<CompiledName("study")>] Study
        | [<CompiledName("assay")>] Assay
        | [<CompiledName("run")>] Run
        | [<CompiledName("workflow")>] Workflow
        | [<CompiledName("datamap")>] DataMap
        | [<CompiledName("template")>] Template

        static member tryFromString(str: string) =
            match str.ToLower() with
            | "assays" -> Some Assay
            | "studies" -> Some Study
            | "investigations" -> Some Investigation
            | "runs" -> Some Run
            | "workflows" -> Some Workflow
            | "datamaps" -> Some DataMap
            | "templates" -> Some Template
            | "assay" -> Some Assay
            | "study" -> Some Study
            | "investigation" -> Some Investigation
            | "run" -> Some Run
            | "workflow" -> Some Workflow
            | "datamap" -> Some DataMap
            | "template" -> Some Template
            | _ -> None

        static member fromString(str: string) =
            match ArcFilesDiscriminate.tryFromString str with
            | Some r -> r
            | None -> failwithf "Unknown ArcFilesDiscriminate: %s" str

    [<StringEnum>]
    type DataMapParent =
        | Assay
        | Study
        | Run
        | Workflow

    type DatamapParentInfo = {|
        ParentId: string
        Parent: DataMapParent
    |}

    module DatamapParentInfo =

        open ARCtrl.ArcPathHelper

        [<Literal>]
        let DatamapFileName = "isa.datamap.xlsx"

        let create (parentId: string) (parent: DataMapParent) : DatamapParentInfo = {|
            ParentId = parentId
            Parent = parent
        |}

        let tryFromPath (path: string) =
            let segments = split path

            match segments with
            | [| AssaysFolderName; anyAssayName; DatamapFileName |] -> create anyAssayName DataMapParent.Assay |> Some
            | [| StudiesFolderName; anyStudyName; DatamapFileName |] -> create anyStudyName DataMapParent.Study |> Some
            | [| WorkflowsFolderName; anyWorkflowName; DatamapFileName |] ->
                create anyWorkflowName DataMapParent.Workflow |> Some
            | [| RunsFolderName; anyRunName; DatamapFileName |] -> create anyRunName DataMapParent.Run |> Some
            | _ -> None

        let toPath (dmpi: DatamapParentInfo) =
            let folderName =
                match dmpi.Parent with
                | DataMapParent.Assay -> AssaysFolderName
                | DataMapParent.Study -> StudiesFolderName
                | DataMapParent.Run -> RunsFolderName
                | DataMapParent.Workflow -> WorkflowsFolderName

            combineMany [| folderName; dmpi.ParentId; DatamapFileName |]

    type ArcFiles =
        | Template of Template
        | Investigation of ArcInvestigation
        | Study of ArcStudy * ArcAssay list
        | Assay of ArcAssay
        | Run of ArcRun
        | Workflow of ArcWorkflow
        | DataMap of (DatamapParentInfo option * DataMap)

        member this.HasMetadata() =
            match this with
            | Assay _
            | Template _
            | Run _
            | Workflow _
            | Investigation _ -> true
            | Study(_, _) -> true
            | DataMap _ -> false

        member this.ArcTables() : ArcTables =
            match this with
            | Template t -> ResizeArray([ t.Table ]) |> ArcTables
            | Study(s, _) -> s
            | Assay a -> a
            | Run r -> r
            | Investigation _
            | Workflow _
            | DataMap _ -> ArcTables(ResizeArray [])

        member this.Tables() : ResizeArray<ArcTable> =
            match this with
            | Template t -> ResizeArray([ t.Table ])
            | Study(s, _) -> s.Tables
            | Assay a -> a.Tables
            | Run r -> r.Tables
            | Investigation _
            | Workflow _
            | DataMap _ -> ResizeArray()

        member this.RelatedArcFilesDiscriminate: ArcFilesDiscriminate =
            match this with
            | Template _ -> ArcFilesDiscriminate.Template
            | Investigation _ -> ArcFilesDiscriminate.Investigation
            | Study _ -> ArcFilesDiscriminate.Study
            | Assay _ -> ArcFilesDiscriminate.Assay
            | Run _ -> ArcFilesDiscriminate.Run
            | Workflow _ -> ArcFilesDiscriminate.Workflow
            | DataMap _ -> ArcFilesDiscriminate.DataMap

        member this.TryGetRelativePath() : string option =
            match this with
            | ArcFiles.Investigation _ -> Some ARCtrl.ArcPathHelper.InvestigationFileName
            | ArcFiles.Study(study, _) -> ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier study.Identifier |> Some
            | ArcFiles.Assay assay -> ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier assay.Identifier |> Some
            | ArcFiles.Run run -> ARCtrl.Helper.Identifier.Run.fileNameFromIdentifier run.Identifier |> Some
            | ArcFiles.Workflow workflow ->
                ARCtrl.Helper.Identifier.Workflow.fileNameFromIdentifier workflow.Identifier
                |> Some
            | ArcFiles.DataMap(Some parentInfo, _) -> DatamapParentInfo.toPath parentInfo |> Some
            | ArcFiles.DataMap(None, _)
            | ArcFiles.Template _ -> None

        member this.CanCreateTables() =
            match this with
            | ArcFiles.Assay _
            | ArcFiles.Study _
            | ArcFiles.Run _ -> true
            | _ -> false

        member this.TryGetActiveTable(activeTableIndex: int option) =
            match activeTableIndex with
            | Some tableIndex when tableIndex >= 0 && tableIndex < this.Tables().Count ->
                Some(tableIndex, this.Tables().[tableIndex])
            | _ -> None

        member this.TryGetDataMap() =
            match this with
            | ArcFiles.Assay assay when assay.DataMap.IsSome -> Some assay.DataMap.Value
            | ArcFiles.Study(study, _) when study.DataMap.IsSome -> Some study.DataMap.Value
            | ArcFiles.Workflow workflow when workflow.DataMap.IsSome -> Some workflow.DataMap.Value
            | ArcFiles.Run run when run.DataMap.IsSome -> Some run.DataMap.Value
            | ArcFiles.DataMap(_, dataMap) -> Some dataMap
            | _ -> None

        member this.CanRenderDataMapView() = this.TryGetDataMap() |> Option.isSome

        /// React only refreshes if the reference changes, but when we update the ArcFile, we usually mutate the existing object. This function creates a new reference with the same content, which can be used to force React to re-render.
        static member refreshRef(arcFile: ArcFiles) : ArcFiles =
            match arcFile with
            | ArcFiles.Investigation investigation -> ArcFiles.Investigation <| investigation.Copy()
            | ArcFiles.Study(study, _) -> ArcFiles.Study(study.Copy(), [])
            | ArcFiles.Assay assay -> ArcFiles.Assay <| assay.Copy()
            | ArcFiles.Run run -> ArcFiles.Run <| run.Copy()
            | ArcFiles.Workflow workflow -> ArcFiles.Workflow <| workflow.Copy()
            | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap.Copy())
            | ArcFiles.Template template -> ArcFiles.Template <| template.Copy()

    [<RequireQualifiedAccess>]
    type JsonExportFormat =
        | ARCtrl
        | ARCtrlCompressed
        | ISA
        | ROCrate

        static member tryFromString(str: string) =
            match str.ToLower() with
            | "arctrl" -> Some ARCtrl
            | "arctrl compressed"
            | "arctrlcompressed" -> Some ARCtrlCompressed
            | "isa" -> Some ISA
            | "ro-crate metadata"
            | "rocrate" -> Some ROCrate
            | _ -> None

        static member fromString(str: string) =
            JsonExportFormat.tryFromString str
            |> Option.defaultWith (fun () -> failwithf "Unknown JsonExportFormat: %s" str)

        member this.AsStringRdbl =
            match this with
            | ARCtrl -> "ARCtrl"
            | ARCtrlCompressed -> "ARCtrl Compressed"
            | ISA -> "ISA"
            | ROCrate -> "RO-Crate Metadata"

module Json =

    open ARCtrl
    open System
    open ARCtrl.Json

    module Generic =

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

                (ArcFilesDiscriminate.Run, JsonExportFormat.ARCtrl),
                fun json -> ArcRun.fromJsonString json |> ArcFiles.Run
                (ArcFilesDiscriminate.Run, JsonExportFormat.ARCtrlCompressed),
                fun json -> ArcRun.fromCompressedJsonString json |> ArcFiles.Run

                (ArcFilesDiscriminate.Workflow, JsonExportFormat.ARCtrl),
                fun json -> ArcWorkflow.fromJsonString json |> ArcFiles.Workflow
                (ArcFilesDiscriminate.Workflow, JsonExportFormat.ARCtrlCompressed),
                fun json -> ArcWorkflow.fromCompressedJsonString json |> ArcFiles.Workflow

                (ArcFilesDiscriminate.DataMap, JsonExportFormat.ARCtrl),
                fun json -> ArcFiles.DataMap(None, DataMap.fromJsonString json)
            ]

        let toFileName (id: string) (fileType: ArcFilesDiscriminate) (jsonType: JsonExportFormat) =
            let n = System.DateTime.Now.ToUniversalTime().ToString("yyyyMMdd_HHmmss")
            let formatString = jsonType.ToString()
            let fileTypeString = fileType.ToString()
            n + "_" + fileTypeString + "_" + id + "_" + formatString + ".json"

        let private formatOrder = [
            JsonExportFormat.ARCtrl
            JsonExportFormat.ARCtrlCompressed
            JsonExportFormat.ISA
            JsonExportFormat.ROCrate
        ]

        let private supportedFormatsFromReadMap (fileType: ArcFilesDiscriminate) =
            readFromJsonMap.Keys
            |> Seq.choose (fun (arcFileType, jsonFormat) -> if arcFileType = fileType then Some jsonFormat else None)
            |> Seq.distinct
            |> Seq.sortBy (fun jsonFormat ->
                formatOrder
                |> List.tryFindIndex ((=) jsonFormat)
                |> Option.defaultValue Int32.MaxValue
            )
            |> Seq.toList

        let private preferredFormat (formats: JsonExportFormat list) =
            if formats |> List.contains JsonExportFormat.ROCrate then
                Some JsonExportFormat.ROCrate
            elif formats |> List.contains JsonExportFormat.ARCtrl then
                Some JsonExportFormat.ARCtrl
            else
                formats |> List.tryHead

        let supportedExportFormats (fileType: ArcFilesDiscriminate) = supportedFormatsFromReadMap fileType

        let supportedImportFormats (fileType: ArcFilesDiscriminate) = supportedFormatsFromReadMap fileType

        let isExportFormatSupported (fileType: ArcFilesDiscriminate) (jsonFormat: JsonExportFormat) =
            supportedExportFormats fileType |> List.contains jsonFormat

        let isImportFormatSupported (fileType: ArcFilesDiscriminate) (jsonFormat: JsonExportFormat) =
            supportedImportFormats fileType |> List.contains jsonFormat

        let tryGetDefaultExportFormat (fileType: ArcFilesDiscriminate) =
            supportedExportFormats fileType |> preferredFormat

        let tryGetDefaultImportFormat (fileType: ArcFilesDiscriminate) =
            supportedImportFormats fileType |> preferredFormat

        let tryParseJsonFileName (fileName: string) =
            if fileName.EndsWith(".json") then
                let parts =
                    fileName.Substring(0, fileName.Length - 5).Split([| "_" |], StringSplitOptions.RemoveEmptyEntries)

                let jsonFormat =
                    parts |> Array.tryPick (fun part -> JsonExportFormat.tryFromString part)

                let arcfile =
                    parts |> Array.tryPick (fun part -> ArcFilesDiscriminate.tryFromString part)

                match jsonFormat, arcfile with
                | Some jf, Some af -> Some(jf, af)
                | _ -> None
            else
                None

    module Import =

        let private arcFileTypeLabel (fileType: ArcFilesDiscriminate) = fileType |> unbox<string>

        let private uniqueTableName (usedNames: Set<string>) (preferredName: string) =
            let baseName =
                if String.IsNullOrWhiteSpace preferredName then
                    "New Table"
                else
                    preferredName.Trim()

            let rec loop index =
                let candidate = if index = 0 then baseName else $"{baseName} {index}"

                if usedNames.Contains candidate then
                    loop (index + 1)
                else
                    candidate

            loop 0

        let private copyTableWithName (name: string) (table: ArcTable) =
            let copiedTable = table.Copy()

            if copiedTable.Name <> name then
                ArcTables(ResizeArray [ copiedTable ]).RenameTableAt(0, name)

            copiedTable

        let applyToCurrentArcFile (currentArcFile: ArcFiles, importedArcFile: ArcFiles) =
            let currentFileType = currentArcFile.RelatedArcFilesDiscriminate
            let importedFileType = importedArcFile.RelatedArcFilesDiscriminate

            if currentFileType <> importedFileType then
                Error(
                    exn
                        $"Cannot import {arcFileTypeLabel importedFileType} JSON into the current {arcFileTypeLabel currentFileType} editor."
                )
            else
                match currentArcFile, importedArcFile with
                | ArcFiles.DataMap(currentParentInfo, _), ArcFiles.DataMap(_, importedDataMap) ->
                    Ok(ArcFiles.DataMap(currentParentInfo, importedDataMap))
                | _ when currentArcFile.CanCreateTables() ->
                    let importedTables = importedArcFile.Tables()

                    if importedTables.Count = 0 then
                        Error(
                            exn $"Imported {arcFileTypeLabel importedFileType} JSON does not contain tables to append."
                        )
                    else
                        let targetTables = currentArcFile.ArcTables()
                        let mutable usedNames = targetTables.TableNames |> Set.ofSeq

                        for sourceTable in importedTables do
                            let nextName = uniqueTableName usedNames sourceTable.Name
                            targetTables.AddTable(copyTableWithName nextName sourceTable)
                            usedNames <- usedNames.Add nextName

                        Ok(ArcFiles.refreshRef currentArcFile)
                | _ -> Ok importedArcFile

        let tryParseFromJsonString
            (
                jsonString: string,
                jsonType: JsonExportFormat option,
                filetype: ArcFilesDiscriminate option,
                fileName: string option
            ) : ArcFiles option =
            let assumedJsonType =
                match jsonType, filetype with
                | Some jt, Some ft -> (jt, ft) |> Some
                | _, _ ->
                    match fileName with
                    | Some name ->
                        let resOpt = Generic.tryParseJsonFileName name

                        match resOpt with
                        | Some(jf, af) -> Some(jf, af)
                        | None -> None
                    | None -> None

            match assumedJsonType with
            | Some(jsonFormat, arcfileType) ->
                match Generic.readFromJsonMap.TryFind(arcfileType, jsonFormat) with
                | Some arcFileFn ->
                    try
                        arcFileFn jsonString |> Some
                    with _ ->
                        None
                | None -> None
            | None -> None

        let parseFromJsonString
            (
                jsonString: string,
                jsonType: JsonExportFormat option,
                filetype: ArcFilesDiscriminate option,
                fileName: string option
            ) : ArcFiles =
            match tryParseFromJsonString (jsonString, jsonType, filetype, fileName) with
            | Some arcfile -> arcfile
            | None ->
                failwith
                    "Error. Unable to find correct JSON format. This function relies on correct naming conventions for the file. We will improve this in the future. The file name must contain the json format, as well as the file type, separated by \"_\". Example: 'ARCtrlCompressed_Assay.json"

    module Export =

        let parseToJsonString (arcfile: ArcFiles, jef: JsonExportFormat) =
            let name, jsonString =
                let nameFromId (id: string) =
                    Generic.toFileName id arcfile.RelatedArcFilesDiscriminate jef

                match arcfile, jef with
                | Investigation ai, JsonExportFormat.ARCtrl ->
                    nameFromId ai.Identifier, ArcInvestigation.toJsonString 0 ai
                | Investigation ai, JsonExportFormat.ARCtrlCompressed ->
                    nameFromId ai.Identifier, ArcInvestigation.toCompressedJsonString 0 ai
                | Investigation ai, JsonExportFormat.ISA ->
                    nameFromId ai.Identifier, ArcInvestigation.toISAJsonString 0 ai
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

                | Template t, JsonExportFormat.ARCtrl ->
                    nameFromId (t.Name.Replace(" ", "_") + ".xlsx"), Template.toJsonString 0 t
                | Template t, JsonExportFormat.ARCtrlCompressed ->
                    nameFromId (t.Name.Replace(" ", "_") + ".xlsx"), Template.toCompressedJsonString 0 t
                | Template _, anyElse ->
                    failwithf "Error. It is not intended to parse Template to %s format." (string anyElse)

                | Run r, JsonExportFormat.ARCtrl -> nameFromId r.Identifier, ArcRun.toJsonString 0 r
                | Run r, JsonExportFormat.ARCtrlCompressed -> nameFromId r.Identifier, ArcRun.toCompressedJsonString 0 r
                | Run _, anyElse -> failwithf "Error. It is not intended to parse Run to %s format." (string anyElse)

                | Workflow w, JsonExportFormat.ARCtrl -> nameFromId w.Identifier, ArcWorkflow.toJsonString 0 w
                | Workflow w, JsonExportFormat.ARCtrlCompressed ->
                    nameFromId w.Identifier, ArcWorkflow.toCompressedJsonString 0 w
                | Workflow _, anyElse ->
                    failwithf "Error. It is not intended to parse Workflow to %s format." (string anyElse)

                | DataMap(_, d), JsonExportFormat.ARCtrl -> nameFromId "", DataMap.toJsonString 0 d
                | DataMap(_), anyElse ->
                    failwithf "Error. It is not intended to parse Datamap to %s format." (string anyElse)

            name, jsonString

        let tryParseToJsonString (arcfile: ArcFiles, jef: JsonExportFormat) =
            if Generic.isExportFormatSupported arcfile.RelatedArcFilesDiscriminate jef then
                try
                    parseToJsonString (arcfile, jef) |> Ok
                with exn ->
                    Error exn
            else
                Error(
                    exn
                        $"JSON export format {jef.AsStringRdbl} is not supported for {arcfile.RelatedArcFilesDiscriminate}."
                )
