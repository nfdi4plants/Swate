module Renderer.Components.WidgetRegistry

open Feliz
open Swate.Components
open ARCtrl


type WidgetBlock =
    {
        prefix: string
        content: ReactElement
    }

let buildingBlockWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.BuildingBlock,
    {|
        prefix = "ADD_BUILDINGBLOCK"
        content =
            Renderer.components.Widgets.AddBuildingBlockWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState
            )
    |}

let templateWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.Template,
    {|
        prefix = "ADD_TEMPLATE"
        content =
            Renderer.components.Widgets.AddTemplateWidget.Main(
                arcFileState,
                activeTableIndex,
                setArcFileState
            )
    |}

let filePickerWidget
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.FilePicker,
        {|
            prefix = "FILEPICKER"
            content =
                Renderer.components.Widgets.AddFilePickerWidget.Main(
                    arcFileState,
                    activeTableIndex,
                    setArcFileState
                )
        |}

let dataAnnotatorWidget
    (arcFileState: ArcFiles option)
    (activeView: Renderer.components.Widgets.AddDataAnnotatorWidget.HostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.DataAnnotator,
    {|
        prefix = "DATAANNOTATOR"
        content =
            Renderer.components.Widgets.AddDataAnnotatorWidget.Main(
                arcFileState,
                activeView,
                activeTableIndex,
                setArcFileState
            )
    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (activeView: Renderer.components.Widgets.AddDataAnnotatorWidget.HostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        buildingBlockWidget arcFileState activeTableIndex setArcFileState
        templateWidget arcFileState activeTableIndex setArcFileState
        filePickerWidget arcFileState activeTableIndex setArcFileState
        dataAnnotatorWidget arcFileState activeView activeTableIndex setArcFileState
    ]
    |> Map.ofList


[<ReactComponent>]
let NavbarButtons(widgetTypes: WidgetType list) =
    let context = WidgetContext.useWidgetController ()

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
        let tooltip = if isActive then $"Close {label}" else $"Open {label}"

        QuickAccessButton.QuickAccessButton(
            tooltip,
            icon,
            (fun _ -> context.toggleWidget widgetType),
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

let CreateNavbarButtonsForAllWidgets widgets children =

    Widget.WidgetController(widgets, children = children)
