module Renderer.components.WidgetRegistry

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
    (activeView: Renderer.components.AddDataAnnotatorWidget.HostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.DataAnnotator,
    {|
        prefix = "DATAANNOTATOR"
        content =
            Renderer.components.AddDataAnnotatorWidget.Main(
                arcFileState,
                activeView,
                activeTableIndex,
                setArcFileState
            )
    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (activeView: Renderer.components.AddDataAnnotatorWidget.HostView)
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

    let controlButton (widgetType: WidgetType) =
        let isActive = context.isActive widgetType

        Html.button [
            prop.className [
                "swt:btn swt:btn-sm"
                if isActive then "swt:btn-primary" else "swt:btn-outline"
            ]
            prop.textf "%s %s" (if isActive then "Close" else "Open") (widgetType.ToString())
            prop.onClick (fun _ -> context.toggleWidget widgetType)
        ]

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
