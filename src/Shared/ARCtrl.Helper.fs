namespace Swate.Components.Shared

open System
open Fable.Core
open ARCtrl

/// This module contains helper functions which might be useful for ARCtrl
[<AutoOpen>]
module ARCtrlHelper =

    /// WORKAROUND: ARCtrl's DataContext.Copy() omits DataContext.Label.
    /// Remove this function after upgrading to an ARCtrl version that preserves labels.
    let preserveDataMapLabelsWorkaround (source: DataMap) (target: DataMap) =
        Seq.iter2
            (fun (source: DataContext) (target: DataContext) -> target.Label <- source.Label)
            source.DataContexts
            target.DataContexts

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

        let tryFromFolderPath (path: string) =
            let segments = split path

            match segments with
            | [| AssaysFolderName; anyAssayName |] -> create anyAssayName DataMapParent.Assay |> Some
            | [| StudiesFolderName; anyStudyName |] -> create anyStudyName DataMapParent.Study |> Some
            | [| WorkflowsFolderName; anyWorkflowName |] -> create anyWorkflowName DataMapParent.Workflow |> Some
            | [| RunsFolderName; anyRunName |] -> create anyRunName DataMapParent.Run |> Some
            | _ -> None

        let toFolderPath (dmpi: DatamapParentInfo) =
            let folderName =
                match dmpi.Parent with
                | DataMapParent.Assay -> AssaysFolderName
                | DataMapParent.Study -> StudiesFolderName
                | DataMapParent.Run -> RunsFolderName
                | DataMapParent.Workflow -> WorkflowsFolderName

            combineMany [| folderName; dmpi.ParentId |]

        let toPath (dmpi: DatamapParentInfo) =
            combineMany [| toFolderPath dmpi; DatamapFileName |]

    let createNewTableName (tables: seq<ArcTable>) =
        let existingNames = tables |> Seq.map _.Name |> Set.ofSeq

        let rec loop index =
            let name = $"New Table {index}"

            if existingNames.Contains name then
                loop (index + 1)
            else
                name

        loop 0

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

        member this.TryGetDataMapParentInfo() =
            match this with
            | ArcFiles.Assay assay -> Some(DatamapParentInfo.create assay.Identifier DataMapParent.Assay)
            | ArcFiles.Study(study, _) -> Some(DatamapParentInfo.create study.Identifier DataMapParent.Study)
            | ArcFiles.Run run -> Some(DatamapParentInfo.create run.Identifier DataMapParent.Run)
            | ArcFiles.Workflow workflow -> Some(DatamapParentInfo.create workflow.Identifier DataMapParent.Workflow)
            | ArcFiles.DataMap(parentInfo, _) -> parentInfo
            | _ -> None

        member this.CanRenderDataMapView() = this.TryGetDataMap() |> Option.isSome

        /// React only refreshes if the reference changes, but when we update the ArcFile, we usually mutate the existing object. This function creates a new reference with the same content, which can be used to force React to re-render.
        static member refreshRef(arcFile: ArcFiles) : ArcFiles =
            let copy =
                match arcFile with
                | ArcFiles.Investigation investigation -> ArcFiles.Investigation <| investigation.Copy()
                | ArcFiles.Study(study, _) -> ArcFiles.Study(study.Copy(), [])
                | ArcFiles.Assay assay -> ArcFiles.Assay <| assay.Copy()
                | ArcFiles.Run run -> ArcFiles.Run <| run.Copy()
                | ArcFiles.Workflow workflow -> ArcFiles.Workflow <| workflow.Copy()
                | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap.Copy())
                | ArcFiles.Template template -> ArcFiles.Template <| template.Copy()

            match arcFile.TryGetDataMap(), copy.TryGetDataMap() with
            | Some source, Some target -> preserveDataMapLabelsWorkaround source target
            | _ -> ()

            copy

    type ARC with

        member this.TryGetDataMap(parentInfo: DatamapParentInfo) =
            match parentInfo.Parent with
            | DataMapParent.Assay -> this.TryGetAssay parentInfo.ParentId |> Option.bind _.DataMap
            | DataMapParent.Study -> this.TryGetStudy parentInfo.ParentId |> Option.bind _.DataMap
            | DataMapParent.Run -> this.TryGetRun parentInfo.ParentId |> Option.bind _.DataMap
            | DataMapParent.Workflow -> this.TryGetWorkflow parentInfo.ParentId |> Option.bind _.DataMap

        member this.TrySetDataMap(parentInfo: DatamapParentInfo, dataMap: DataMap option) =
            match parentInfo.Parent with
            | DataMapParent.Assay ->
                this.TryGetAssay parentInfo.ParentId
                |> Option.map (fun assay -> assay.DataMap <- dataMap)
            | DataMapParent.Study ->
                this.TryGetStudy parentInfo.ParentId
                |> Option.map (fun study -> study.DataMap <- dataMap)
            | DataMapParent.Run ->
                this.TryGetRun parentInfo.ParentId
                |> Option.map (fun run -> run.DataMap <- dataMap)
            | DataMapParent.Workflow ->
                this.TryGetWorkflow parentInfo.ParentId
                |> Option.map (fun workflow -> workflow.DataMap <- dataMap)
            |> Option.isSome

    /// Single source of truth for file paths stored relative to the ARC root.
    let toArcRootRelativeFilePath (arcFile: ArcFiles) (filePath: string) =
        let parentInfo = arcFile.TryGetDataMapParentInfo()

        let withExplicitRelativePrefix (path: string) =
            let normalizedPath = PathHelpers.normalizeSeparators path

            if normalizedPath.StartsWith("./") then
                normalizedPath
            else
                $"./{normalizedPath}"

        match parentInfo, ARCtrl.ArcPathHelper.split filePath with
        // Browser uploads expose only the bare file name, while stored references are ARC-root-relative.
        | Some parentInfo, [| _ |] ->
            let childFolder =
                match parentInfo.Parent with
                | DataMapParent.Assay -> Some ARCtrl.ArcPathHelper.AssayDatasetFolderName
                | DataMapParent.Study -> Some ARCtrl.ArcPathHelper.StudiesResourcesFolderName
                | DataMapParent.Run
                | DataMapParent.Workflow -> None

            [|
                Some(DatamapParentInfo.toFolderPath parentInfo)
                childFolder
                Some filePath
            |]
            |> Array.choose id
            |> ARCtrl.ArcPathHelper.combineMany
            |> withExplicitRelativePrefix
        | _, segments when segments.Length > 1 -> withExplicitRelativePrefix filePath
        | _ -> filePath

    [<RequireQualifiedAccess>]
    module ArcFileDefaults =

        [<Literal>]
        let BasicAnnotationTableRowCount = 3

        let createBasicAnnotationTable (tableName: string) =
            let table = ArcTable.init tableName

            table.AddColumns [|
                CompositeColumn.create (CompositeHeader.Input IOType.Source)
                CompositeColumn.create CompositeHeader.ProtocolUri
                CompositeColumn.create (CompositeHeader.Output IOType.Sample)
            |]

            table.AddRowsEmpty BasicAnnotationTableRowCount
            table

        let private withInitialTable (identifier: string) (arcFile: ArcFiles) =
            arcFile.ArcTables().AddTable(createBasicAnnotationTable $"{identifier} Table")
            arcFile

        let createDefaultArcFile (fileType: ArcFilesDiscriminate) (identifier: string) =
            match fileType with
            | ArcFilesDiscriminate.Study -> ArcFiles.Study(ArcStudy.init identifier, []) |> withInitialTable identifier
            | ArcFilesDiscriminate.Assay -> ArcFiles.Assay(ArcAssay.init identifier) |> withInitialTable identifier
            | ArcFilesDiscriminate.Workflow -> ArcFiles.Workflow(ArcWorkflow.init identifier)
            | ArcFilesDiscriminate.Run -> ArcFiles.Run(ArcRun.init identifier) |> withInitialTable identifier
            | unsupportedFileType -> failwithf "Cannot create default ARC file for %A." unsupportedFileType

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
