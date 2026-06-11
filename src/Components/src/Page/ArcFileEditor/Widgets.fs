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
        WidgetType.JsonImport
        WidgetType.JsonExport
    ]

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> 
            "Add Building Block", 
            Html.i [
                prop.className "swt:iconify swt:fluent--table-column-insert-24-filled swt:size-6"
            ]
        | WidgetType.Template -> 
            "Add Template",
            Html.i [
                prop.className "swt:iconify swt:fluent--table-add-24-filled swt:size-6"
            ]
        | WidgetType.FilePicker -> 
            "File Picker", 
            Html.i [
                prop.className "swt:iconify swt:fluent--document-text-link-20-filled swt:size-6"
            ]
        | WidgetType.DataAnnotator -> 
            "Data Annotator", 
            Html.i [
                prop.className "swt:iconify swt:fluent--document-data-link-24-filled swt:size-6"
            ]
        | WidgetType.JsonImport ->
            "Import JSON", 
            Html.i [
                prop.className "swt:iconify swt:fluent--arrow-import-20-filled swt:size-6"
            ]
        | WidgetType.JsonExport ->
            "Export JSON", 
            Html.i [
                prop.className "swt:iconify swt:fluent--arrow-export-20-filled swt:size-6"
            ]
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
            dataAnnotatorWidget: ReactElement,
            jsonImportWidget: ReactElement,
            jsonExportWidget: ReactElement
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
                        WidgetType.JsonImport,
                        {|
                            prefix = "JSONIMPORT"
                            content = jsonImportWidget
                        |}
                        WidgetType.JsonExport,
                        {|
                            prefix = "JSONEXPORT"
                            content = jsonExportWidget
                        |}
                    ]
                    |> Map.ofList
                ),
                [|
                    box buildingBlockWidget
                    box templateWidget
                    box filePickerWidget
                    box dataAnnotatorWidget
                    box jsonImportWidget
                    box jsonExportWidget
                |]
            )

        Widget.WidgetController(widgets, children = children)
