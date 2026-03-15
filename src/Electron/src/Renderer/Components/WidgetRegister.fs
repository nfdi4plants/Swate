module Renderer.Components.WidgetRegistry

open Feliz
open Swate.Components
open ARCtrl
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

let private filePickerServices: FilePickerWidgetServices = {
    pickPaths =
        fun () ->
            promise {
                let! result = Api.ipcArcVaultApi.pickPaths (unbox null)
                return result |> Result.mapError (fun error -> error.Message)
            }
}

let private dataAnnotatorServices: DataAnnotatorWidgetServices = {
    pickPaths =
        fun () ->
            promise {
                let! result = Api.ipcArcVaultApi.pickPaths (unbox null)
                return result |> Result.mapError (fun error -> error.Message)
            }
    loadTextFile =
        fun path ->
            promise {
                let! result = Api.ipcArcVaultApi.openFile (unbox null) path

                return
                    match result with
                    | Error error -> Error error.Message
                    | Ok(PageState.Text content) -> Ok content
                    | Ok _ -> Error "Selected file could not be loaded as plain text. Only csv/tsv/txt are supported."
            }
}

let private templateServices: TemplateWidgetServices = {
    loadTemplates =
        fun () ->
            async {
                try
                    let! templatesJson = Api.templateApi.getTemplates()

                    let templates =
                        templatesJson
                        |> ARCtrl.Json.Templates.fromJsonString
                        |> Array.ofSeq

                    return Ok templates
                with error ->
                    return Error error.Message
            }
}

let BuildingBlockWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.BuildingBlock,
    {|
        prefix = "ADD_BUILDINGBLOCK"
        content =
            Swate.Components.BuildingBlockWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState
            )
    |}

let TemplateWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.Template,
    {|
        prefix = "ADD_TEMPLATE"
        content =
            Swate.Components.TemplateWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState,
                importType,
                setImportType,
                templateServices
            )
    |}

let FilePickerWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.FilePicker,
        {|
            prefix = "FILEPICKER"
            content =
                Swate.Components.FilePickerWidget.Main(
                    arcFileState,
                    activeTableIndex,
                    setArcFileState,
                    filePickerServices
                )
        |}

let DataAnnotatorWidget
    (arcFileState: ArcFiles option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.DataAnnotator,
    {|
        prefix = "DATAANNOTATOR"
        content =
            Swate.Components.DataAnnotatorWidget.Main(
                arcFileState,
                activeView,
                activeTableIndex,
                setArcFileState,
                dataAnnotatorServices
            )
    |}

let ARCObjectSelectorWidget
    (arcFileState: ArcFiles option)
    (selectedTarget: ARCObjectTarget option)
    (onSelectTarget: ARCObjectTarget -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.ARCObjectSelector,
    {|
        prefix = "ARC_OBJECT_SELECTOR"
        content =
            Swate.Components.ARCObjectSelectorWidget.Main(
                arcFileState,
                selectedTarget,
                onSelectTarget
            )
    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    (selectedTarget: ARCObjectTarget option)
    (onSelectTarget: ARCObjectTarget -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        ARCObjectSelectorWidget arcFileState selectedTarget onSelectTarget
        BuildingBlockWidget arcFileState activeTableIndex setArcFileState
        TemplateWidget arcFileState activeTableIndex setArcFileState importType setImportType
        FilePickerWidget arcFileState activeTableIndex setArcFileState
        DataAnnotatorWidget arcFileState activeView activeTableIndex setArcFileState
    ]
    |> Map.ofList

let widgetDisabledReason
    (arcFileState: ArcFiles option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (widgetType: WidgetType)
    =
    match widgetType with
    | WidgetType.ARCObjectSelector ->
        if arcFileState.IsSome then
            None
        else
            Some "Open an ARC file first."
    | WidgetType.BuildingBlock
    | WidgetType.Template
    | WidgetType.FilePicker ->
        match arcFileState with
        | None -> Some "Open an ARC file first."
        | Some _ when activeTableIndex.IsNone -> Some "Select a table to open widgets."
        | Some _ -> None
    | WidgetType.DataAnnotator ->
        match arcFileState with
        | None ->
            Some "Open an ARC file first."
        | Some arcFile ->
            match activeView with
            | WidgetHostView.MetadataView ->
                Some "Switch to a table or datamap tab to use Data Annotator."
            | WidgetHostView.PreviewErrorView ->
                Some "Data Annotator is unavailable while the preview is in an error state."
            | WidgetHostView.TableView ->
                if activeTableIndex.IsSome then
                    None
                else
                    Some "Select a table tab to use Data Annotator."
            | WidgetHostView.DataMapView ->
                if WidgetArcFile.tryGetDataMap arcFile |> Option.isSome then
                    None
                else
                    Some "No DataMap available for this ARC file."
    | WidgetType.Playground ->
        None

[<ReactComponent>]
let NavbarButtons(widgetTypes: WidgetType list, getDisabledReason: WidgetType -> string option) =
    let context = WidgetContext.useWidgetController ()

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.ARCObjectSelector -> "ARC Object Selector", Icons.MagnifyingGlassPlus()
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.Playground -> "Playground", Icons.Templates()

    let controlButton (widgetType: WidgetType) =
        let isActive = context.isActive widgetType
        let disabledReason = getDisabledReason widgetType
        let label, icon = widgetInfo widgetType
        let tooltip =
            match disabledReason with
            | Some reason ->
                reason
            | None ->
                if isActive then
                    $"Close {label}"
                else
                    $"Open {label}"

        QuickAccessButton.QuickAccessButton(
            tooltip,
            icon,
            (fun _ -> context.toggleWidget widgetType),
            isDisabled = disabledReason.IsSome,
            classes = (if isActive then "swt:!text-primary" else "")
        )

    Html.div [
        prop.className "swt:flex swt:flex-col swt:gap-3 swt:items-center swt:justify-center"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
                prop.children [
                    for widgetType in widgetTypes do
                        controlButton widgetType
                ]
            ]
        ]
    ]

let widgetTypes = [
    WidgetType.ARCObjectSelector
    WidgetType.BuildingBlock
    WidgetType.Template
    WidgetType.FilePicker
    WidgetType.DataAnnotator
]

[<ReactComponent>]
let NavbarButtonsForAllWidgets widgets children =

    Widget.WidgetController(widgets, children = children)
