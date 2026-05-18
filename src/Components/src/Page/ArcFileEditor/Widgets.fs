namespace Swate.Components.Page.ArcFileEditor.Widgets

open Feliz
open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.Buttons
open Swate.Components.Page.ArcFileEditor
open Swate.Components.Composite.Widgets
open Swate.Components.Composite.Widgets.Context

module private WidgetsHelper =

    let widgetTypes = [
        WidgetType.BuildingBlock
        WidgetType.Template
        WidgetType.FilePicker
        WidgetType.DataAnnotator
    ]

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.Playground -> "Playground", Icons.Templates()

open WidgetsHelper

[<Erase; Mangle(false)>]
type Main =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member private WidgetToggleBtn(widgetType: WidgetType, isOpen: bool, toggle: unit -> unit) =

        let label, icon = widgetInfo widgetType

        let tooltip = if isOpen then $"Close {label}" else $"Open {label}"

        Buttons.QuickAccessButton(
            icon,
            tooltip,
            (fun _ -> toggle ()),
            classes = (if isOpen then "swt:!text-primary" else "")
        )

    [<ReactComponent>]
    static member WidgetToggleBtns() =

        let context = useWidgetControllerCtx ()

        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:justify-center"
            prop.children [
                for widgetType in widgetTypes do
                    let isOpen = context.isActive widgetType
                    let toggle = fun () -> context.toggleWidget widgetType
                    Main.WidgetToggleBtn(widgetType, isOpen, toggle)
            ]
        ]

    [<ReactComponent>]
    static member Widgets
        (
            children: ReactElement,
            buildingBlockWidget: ReactElement,
            templateWidget: ReactElement,
            filePickerWidget: ReactElement,
            dataAnnotatorWidget: ReactElement
        ) =
        let widgets: Map<WidgetType, WidgetDefinition> =
            React.useMemo (
                (fun () ->
                    [
                        WidgetType.BuildingBlock,
                        {|
                            prefix = "ADD_BUILDINGBLOCK"
                            content = buildingBlockWidget
                        |}
                        WidgetType.Template,
                        {|
                            prefix = "ADD_TEMPLATE"
                            content = templateWidget
                        |}
                        WidgetType.FilePicker,
                        {|
                            prefix = "FILEPICKER"
                            content = filePickerWidget
                        |}
                        WidgetType.DataAnnotator,
                        {|
                            prefix = "DATAANNOTATOR"
                            content = dataAnnotatorWidget
                        |}
                    ]
                    |> Map.ofList
                ),
                [|
                    box buildingBlockWidget
                    box templateWidget
                    box filePickerWidget
                |]
            )

        Widget.WidgetController(widgets, children = children)