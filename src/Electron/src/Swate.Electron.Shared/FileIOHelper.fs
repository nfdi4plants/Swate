module Swate.Electron.Shared.FileIOHelper

/// normalizes the path by replacing backslashes with forward slashes, trimming whitespace, and removing trailing slashes
let normalizePath (path: string) =
    path.Replace("\\", "/").Trim().TrimEnd('/')

/// normalizes the path and splits it into parts
let getPathParts (path: string) =
    normalizePath path |> (fun p -> p.Split("/"))

let getFileName (path: string) = path |> getPathParts |> Array.last

let pathsEqual (left: string) (right: string) =
    normalizePath left = normalizePath right

let combineMany = ARCtrl.ArcPathHelper.combineMany

open ARCtrl

let tryGetArcFilePath (arcRootPath: ArcRootPath) (arcFile: ArcFiles) =
    let arcRootPath = defaultArg arcRootPath ""
    let root = normalizePath arcRootPath

    match arcFile with
    | ArcFiles.Investigation _ -> Some ARCtrl.ArcPathHelper.InvestigationFileName
    | ArcFiles.Study(study, _) -> ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier study.Identifier |> Some
    | ArcFiles.Assay assay -> ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier assay.Identifier |> Some
    | ArcFiles.Run run -> ARCtrl.Helper.Identifier.Run.fileNameFromIdentifier run.Identifier |> Some
    | ArcFiles.Workflow workflow ->
        ARCtrl.Helper.Identifier.Workflow.fileNameFromIdentifier workflow.Identifier
        |> Some
    | ArcFiles.DataMap(Some dmpi, _) -> DatamapParentInfo.toPath dmpi |> Some
    | ArcFiles.DataMap(None, _)
    | ArcFiles.Template _ -> None
    |> Option.map (fun p -> combineMany [| root; p |])


[<RequireQualifiedAccess>]
module DTOType =

    open ARCtrl.Contract

    /// This function checks if the given DTOType is one of the plain text variants (JSON, YAML, CWL, PlainText).
    let isPlainTextVariant (dtoType: DTOType) =
        match dtoType with
        | DTOType.JSON
        | DTOType.YAML
        | DTOType.CWL
        | DTOType.PlainText -> true
        | _ -> false

    /// This function checks if the given DTOType is one of the ISA file variants (Investigation, Study, Assay, Run, Workflow, Datamap).
    let isISAFileVariant (dtoType: DTOType) =
        match dtoType with
        | DTOType.ISA_Investigation
        | DTOType.ISA_Study
        | DTOType.ISA_Assay
        | DTOType.ISA_Run
        | DTOType.ISA_Workflow
        | DTOType.ISA_Datamap -> true
        | _ -> false

    /// Active pattern for matching all plain text variants of DTOType: (JSON, YAML, CWL, PlainText)
    let (|DTOTypeIsPlainTextVariant|_|) (dtoType: DTOType) =
        if isPlainTextVariant dtoType then Some() else None

    /// Active pattern for matching all ISA file variants of DTOType: (Investigation, Study, Assay, Run, Workflow, Datamap)
    let (|DTOTypeIsISAFileVariant|_|) (dtoType: DTOType) =
        if isISAFileVariant dtoType then Some() else None

[<RequireQualifiedAccess>]
module FileContentDTO =

    open FileIOTypes
    open ARCtrl.Helper
    open ARCtrl.Contract
    open ARCtrl.ArcPathHelper

    let DEFAULT_JSON_EXPORT_FORMAT = JsonExportFormat.ARCtrl

    let create fileType content path : FileContentDTO = {|
        fileType = fileType
        content = content
        path = path
    |}

    let toArcFile (dto: FileContentDTO) : ArcFiles option =

        let exportFormat = JsonExportFormat.ARCtrl

        let afd =
            match dto.fileType with
            | DTOType.ISA_Investigation -> Some ArcFilesDiscriminate.Investigation
            | DTOType.ISA_Study -> Some ArcFilesDiscriminate.Study
            | DTOType.ISA_Assay -> Some ArcFilesDiscriminate.Assay
            | DTOType.ISA_Run -> Some ArcFilesDiscriminate.Run
            | DTOType.ISA_Workflow -> Some ArcFilesDiscriminate.Workflow
            | DTOType.ISA_Datamap -> Some ArcFilesDiscriminate.DataMap
            | _ -> None

        match afd with
        | Some afd ->
            match Json.Generic.readFromJsonMap.TryGetValue((afd, exportFormat)) with
            | true, fn ->
                match fn dto.content with
                | ArcFiles.DataMap(None, dm) ->
                    let dmpi = DatamapParentInfo.tryFromPath dto.path
                    ArcFiles.DataMap(dmpi, dm)
                | anyElse -> anyElse
                |> Some
            | _ -> None
        | None -> None

    let fromArcFile (arcFile: ArcFiles) : FileContentDTO option =
        let exportFormat = JsonExportFormat.ARCtrl

        let path = tryGetArcFilePath (None) arcFile

        let dtoTypeOpt =
            match arcFile with
            | ArcFiles.Investigation _ ->
                Some {|
                    fileType = DTOType.ISA_Investigation
                    path = ARCtrl.ArcPathHelper.InvestigationFileName
                |}
            | ArcFiles.Study(s, _) ->
                Some {|
                    fileType = DTOType.ISA_Study
                    path = ARCtrl.Helper.Identifier.Study.fileNameFromIdentifier s.Identifier
                |}
            | ArcFiles.Assay(a) ->
                Some {|
                    fileType = DTOType.ISA_Assay
                    path = ARCtrl.Helper.Identifier.Assay.fileNameFromIdentifier a.Identifier
                |}
            | ArcFiles.Run r ->
                Some {|
                    fileType = DTOType.ISA_Run
                    path = ARCtrl.Helper.Identifier.Run.fileNameFromIdentifier r.Identifier
                |}
            | ArcFiles.Workflow w ->
                Some {|
                    fileType = DTOType.ISA_Workflow
                    path = ARCtrl.Helper.Identifier.Workflow.fileNameFromIdentifier w.Identifier
                |}
            | ArcFiles.DataMap(Some dmpi, dm) ->
                Some {|
                    fileType = DTOType.ISA_Datamap
                    path = DatamapParentInfo.toPath dmpi
                |}
            | _ -> None

        match dtoTypeOpt with
        | Some dtoType ->
            let _, json = Json.Export.parseToJsonString (arcFile, exportFormat)
            create dtoType.fileType json dtoType.path |> Some
        | None -> None



    let fromArcByPath (path: string) (arc: ARC) =
        let split = ARCtrl.ArcPathHelper.split path
        let exportFormat = DEFAULT_JSON_EXPORT_FORMAT

        /// This must be set if it returns Some
        let mutable discFileType: DTOType option = None

        let arcFileOpt =
            match split with
            | InvestigationPath _ ->
                discFileType <- Some DTOType.ISA_Investigation
                ArcFiles.Investigation arc |> Some
            | AssayPath p ->
                discFileType <- Some DTOType.ISA_Assay
                let identifier = (Identifier.Assay.identifierFromFileName p)
                let assay = arc.TryGetAssay identifier
                assay |> Option.map ArcFiles.Assay
            | StudyPath p ->
                discFileType <- Some DTOType.ISA_Study
                let identifier = (Identifier.Study.identifierFromFileName p)
                let study = arc.TryGetStudy identifier
                study |> Option.map (fun s -> ArcFiles.Study(s, []))
            | WorkflowPath p ->
                discFileType <- Some DTOType.ISA_Workflow

                let identifier = (Identifier.Workflow.identifierFromFileName p)
                let workflow = arc.TryGetWorkflow identifier
                workflow |> Option.map ArcFiles.Workflow
            | RunPath p ->
                discFileType <- Some DTOType.ISA_Run
                let identifier = (Identifier.Run.identifierFromFileName p)
                let run = arc.TryGetRun identifier
                run |> Option.map ArcFiles.Run
            | DatamapPath _ ->
                discFileType <- Some DTOType.ISA_Datamap

                match split with
                | [| AssaysFolderName; anyAssayName; DataMapFileName |] ->
                    let assay = arc.TryGetAssay(Identifier.Assay.identifierFromFileName anyAssayName)

                    let datamap =
                        assay
                        |> Option.bind (fun a -> a.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyAssayName DataMapParent.Assay |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| StudiesFolderName; anyStudyName; DataMapFileName |] ->
                    let study = arc.TryGetStudy(Identifier.Study.identifierFromFileName anyStudyName)

                    let datamap =
                        study
                        |> Option.bind (fun s -> s.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyStudyName DataMapParent.Study |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| WorkflowsFolderName; anyWorkflowName; DataMapFileName |] ->
                    let workflow =
                        arc.TryGetWorkflow(Identifier.Workflow.identifierFromFileName anyWorkflowName)

                    let datamap =
                        workflow
                        |> Option.bind (fun w -> w.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyWorkflowName DataMapParent.Workflow |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | [| RunsFolderName; anyRunName; DataMapFileName |] ->
                    let run = arc.TryGetRun(Identifier.Run.identifierFromFileName anyRunName)

                    let datamap =
                        run
                        |> Option.bind (fun r -> r.DataMap)
                        |> Option.map (fun dm ->
                            let dmpi = DatamapParentInfo.create anyRunName DataMapParent.Run |> Some
                            dmpi, dm
                        )

                    datamap |> Option.map ArcFiles.DataMap
                | _ -> None
            | _ -> None

        match arcFileOpt, discFileType with
        | Some arcFile, Some discFileType ->
            let _, json = Json.Export.parseToJsonString (arcFile, exportFormat)
            create discFileType json path |> Some
        | _ -> None