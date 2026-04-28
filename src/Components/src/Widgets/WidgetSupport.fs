namespace Swate.Components.Widgets

open ARCtrl
open Fable.Core
open Swate.Components.Shared

[<RequireQualifiedAccess>]
type WidgetHostView =
    | TableView
    | DataMapView
    | MetadataView
    | PreviewErrorView

type FilePickerWidgetServices = {
    pickPaths: unit -> JS.Promise<Result<string[], string>>
}

type DataAnnotatorWidgetServices = {
    pickTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], string>>
}

type TemplateWidgetServices = {
    loadTemplates: unit -> Async<Result<Template[], string>>
}