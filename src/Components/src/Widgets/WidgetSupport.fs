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
module WidgetArcFile =

    let refreshRef (arcFile: ArcFiles) =
        match arcFile with
        | ArcFiles.Investigation investigation -> ArcFiles.Investigation <| investigation.Copy()
        | ArcFiles.Study(study, _) -> ArcFiles.Study(study.Copy(), [])
        | ArcFiles.Assay assay -> ArcFiles.Assay <| assay.Copy()
        | ArcFiles.Run run -> ArcFiles.Run <| run.Copy()
        | ArcFiles.Workflow workflow -> ArcFiles.Workflow <| workflow.Copy()
        | ArcFiles.DataMap(parent, dataMap) -> ArcFiles.DataMap(parent, dataMap.Copy())
        | ArcFiles.Template template -> ArcFiles.Template <| template.Copy()

type FilePickerWidgetServices = {
    pickPaths: unit -> JS.Promise<Result<string[], string>>
}

type DataAnnotatorWidgetServices = {
    pickTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], string>>
}

type TemplateWidgetServices = {
    loadTemplates: unit -> Async<Result<Template[], string>>
}