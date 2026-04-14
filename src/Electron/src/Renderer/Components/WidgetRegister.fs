module Renderer.Components.WidgetRegistry

open Feliz
open Swate.Components
open Swate.Components.Shared
open Swate.Components.Widgets.Contexts
open ARCtrl
open Swate.Electron.Shared.IPCTypes

let private filePickerServices: FilePickerWidgetServices = {
    pickPaths =
        fun () -> promise {
            let! result = Api.ipcArcVaultApi.pickAbsolutePaths (unbox null)
            return result |> Result.mapError (fun error -> error.Message)
        }
}

let private dataAnnotatorServices: DataAnnotatorWidgetServices = {
    pickTextFiles =
        fun () -> promise {
            let! result = Api.ipcArcVaultApi.pickExternalTextFiles (unbox null)
            return result |> Result.mapError (fun error -> error.Message)
        }
}

let createTemplateServices () = {
    loadTemplates =
        fun () -> async {
            try
                let! templatesJson = Api.templateApi.getTemplates ()

                let templates = templatesJson |> ARCtrl.Json.Templates.fromJsonString |> Array.ofSeq

                return Ok templates
            with error ->
                return Error error.Message
        }
}

let templateServices = createTemplateServices ()

let BuildingBlockWidget
    (arcFileState: ArcFiles)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.BuildingBlock,
    {|
        prefix = "ADD_BUILDINGBLOCK"
        content = Swate.Components.BuildingBlockWidget.Main(arcFileState, activeTableIndex, setArcFileState)
    |}

let TemplateWidget
    (arcFileState: ArcFiles)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles -> unit)
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
    (arcFileState: ArcFiles)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.FilePicker,
    {|
        prefix = "FILEPICKER"
        content =
            Swate.Components.FilePickerWidget.Main(arcFileState, activeTableIndex, setArcFileState, filePickerServices)
    |}

let DataAnnotatorWidget
    (arcFileState: ArcFiles)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles -> unit)
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

let createWidgets
    (arcFileState: ArcFiles)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        BuildingBlockWidget arcFileState activeTableIndex setArcFileState
        TemplateWidget arcFileState activeTableIndex setArcFileState importType setImportType
        FilePickerWidget arcFileState activeTableIndex setArcFileState
        DataAnnotatorWidget arcFileState activeView activeTableIndex setArcFileState
    ]
    |> Map.ofList


[<ReactComponent>]
let NavbarButtons (widgetTypes: WidgetType list, isEnabled: bool) =
    let context = useWidgetController ()

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.Playground -> "Playground", Icons.Templates()

    let controlButton (widgetType: WidgetType) =
        let isActive = context.isActive widgetType
        let label, icon = widgetInfo widgetType

        let tooltip =
            if not isEnabled then "Select a table to open widgets"
            elif isActive then $"Close {label}"
            else $"Open {label}"

        QuickAccessButton.QuickAccessButton(
            tooltip,
            icon,
            (fun _ -> context.toggleWidget widgetType),
            isDisabled = (not isEnabled),
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
    WidgetType.BuildingBlock
    WidgetType.Template
    WidgetType.FilePicker
    WidgetType.DataAnnotator
]

[<ReactComponent>]
let NavbarButtonsForAllWidgets widgets children =

    Widget.WidgetController(widgets, children = children)
