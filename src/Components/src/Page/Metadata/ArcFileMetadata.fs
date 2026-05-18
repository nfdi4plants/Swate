namespace Swate.Components.Page.Metadata

open System
open Feliz
open ARCtrl
open Swate.Components
open Swate.Components.Shared

type private LazyComponents =

    [<ReactLazyComponent>]
    static member LazyInvestigationMetadata
        (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit)
        =
        InvestigationMetadata.InvestigationMetadata(investigation = investigation, setInvestigation = setInvestigation)

    [<ReactLazyComponent>]
    static member LazyStudyMetadata(study: ArcStudy, setStudy: ArcStudy -> unit) =
        StudyMetadata.StudyMetadata(study = study, setStudy = setStudy)

    [<ReactLazyComponent>]
    static member LazyAssayMetadata(assay: ArcAssay, setAssay: ArcAssay -> unit) =
        AssayMetadata.AssayMetadata(assay = assay, setAssay = setAssay)

    [<ReactLazyComponent>]
    static member LazyRunMetadata(run: ArcRun, setRun: ArcRun -> unit) =
        RunMetadata.RunMetadata(run = run, setRun = setRun)

    [<ReactLazyComponent>]
    static member LazyWorkflowMetadata(workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
        WorkflowMetadata.WorkflowMetadata(workflow = workflow, setWorkflow = setWorkflow)

    [<ReactLazyComponent>]
    static member LazyDataMapMetadata(datamap: DataMap) =
        DataMapMetadata.DataMapMetadata(datamap = datamap)

    [<ReactLazyComponent>]
    static member LazyTemplateMetadata(template: ARCtrl.Template, setTemplate: ARCtrl.Template -> unit) =
        TemplateMetadata.TemplateMetadata(template = template, setTemplate = setTemplate)

open Fable.Core

[<Erase; Mangle(false)>]
type ArcFileMetadata =

    [<ReactComponent>]
    static member private LazyFallback(text: string) =
        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-center swt:p-3 swt:text-sm swt:opacity-70"
            prop.text text
        ]

    [<ReactComponent>]
    static member private LazyInvestigationMetadata
        (investigation: ArcInvestigation, setInvestigation: ArcInvestigation -> unit)
        =
        React.Suspense(
            [
                LazyComponents.LazyInvestigationMetadata(investigation, setInvestigation)
            ],
            fallback = ArcFileMetadata.LazyFallback("Loading investigation metadata...")
        )

    [<ReactComponent>]
    static member private LazyStudyMetadata(study: ArcStudy, setStudy: ArcStudy -> unit) =
        React.Suspense(
            [ LazyComponents.LazyStudyMetadata(study, setStudy) ],
            fallback = ArcFileMetadata.LazyFallback("Loading study metadata...")
        )

    [<ReactComponent>]
    static member private LazyAssayMetadata(assay: ArcAssay, setAssay: ArcAssay -> unit) =
        React.Suspense(
            [ LazyComponents.LazyAssayMetadata(assay, setAssay) ],
            fallback = ArcFileMetadata.LazyFallback("Loading assay metadata...")
        )

    [<ReactComponent>]
    static member private LazyRunMetadata(run: ArcRun, setRun: ArcRun -> unit) =
        React.Suspense(
            [ LazyComponents.LazyRunMetadata(run, setRun) ],
            fallback = ArcFileMetadata.LazyFallback("Loading run metadata...")
        )

    [<ReactComponent>]
    static member private LazyWorkflowMetadata(workflow: ArcWorkflow, setWorkflow: ArcWorkflow -> unit) =
        React.Suspense(
            [
                LazyComponents.LazyWorkflowMetadata(workflow, setWorkflow)
            ],
            fallback = ArcFileMetadata.LazyFallback("Loading workflow metadata...")
        )

    [<ReactComponent>]
    static member private LazyDataMapMetadata(datamap: DataMap) =
        React.Suspense(
            [ LazyComponents.LazyDataMapMetadata(datamap) ],
            fallback = ArcFileMetadata.LazyFallback("Loading datamap metadata...")
        )

    [<ReactComponent>]
    static member private LazyTemplateMetadata(template: ARCtrl.Template, setTemplate: ARCtrl.Template -> unit) =
        React.Suspense(
            [
                LazyComponents.LazyTemplateMetadata(template, setTemplate)
            ],
            fallback = ArcFileMetadata.LazyFallback("Loading template metadata...")
        )

    [<ReactComponent(true)>]
    static member ArcFileMetadata
        // 👀 If you rename these variables, ensure that the names are forwarded for lazy loading in `src\Components\src\ARCFileEditor\ArcFileEditor.fs` as well!
        (arcFile: ArcFiles, setArcFile: ArcFiles -> unit) =

        match arcFile with
        | ArcFiles.Investigation investigation ->
            ArcFileMetadata.LazyInvestigationMetadata(
                investigation,
                fun updated -> setArcFile (ArcFiles.Investigation updated)
            )
        | ArcFiles.Study(study, assays) ->
            ArcFileMetadata.LazyStudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
        | ArcFiles.Assay assay ->
            ArcFileMetadata.LazyAssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
        | ArcFiles.Run run -> ArcFileMetadata.LazyRunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
        | ArcFiles.Workflow workflow ->
            ArcFileMetadata.LazyWorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
        | ArcFiles.DataMap(_, datamap) -> ArcFileMetadata.LazyDataMapMetadata(datamap)
        | ArcFiles.Template template ->
            ArcFileMetadata.LazyTemplateMetadata(template, fun updated -> setArcFile (ArcFiles.Template updated))
