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

type DataAnnotatorWidgetServices = {
    pickTextFiles: unit -> JS.Promise<Result<ImportedTextFile[], string>>
}

type TemplateWidgetServices = {
    loadTemplates: unit -> Async<Result<Template[], string>>
}

/// <summary>
/// Is not only used to store position but also size.
/// </summary>
type Rect = {
    X: int
    Y: int
} with

    static member init() = { X = 0; Y = 0 }