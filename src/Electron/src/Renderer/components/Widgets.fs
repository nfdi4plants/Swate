module Renderer.components.Widgets

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
        content = AddBuildingBlockWidget.Main(arcFileState, activeTableIndex, setArcFileState)
    |}

let templateWidget: WidgetType * WidgetDefinition =
    WidgetType.Template,
    {|
        prefix = "ADD_TEMPLATE"
        content =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-80"
                prop.children [
                    Html.h3 [
                        prop.className "swt:font-bold"
                        prop.text "Add Template"
                    ]
                    //TermSearch.TermSearch(term, setTerm)
                    Html.span [
                        prop.className "swt:text-xs swt:opacity-70"
                        prop.textf
                            "Selected term: %s"
                            //(term |> Option.map (fun t -> t.ToString()) |> Option.defaultValue "None")
                            "Test"
                    ]
                ]
            ]
    |}

let filePickerWidget: WidgetType * WidgetDefinition =
    WidgetType.FilePicker,
        {|
            prefix = "FILEPICKER"
            content =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-72"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-bold"
                            prop.text "File Picker"
                        ]
                        Html.input [
                            prop.className "swt:input swt:input-sm swt:input-bordered"
                            //prop.value fileName
                            //prop.onChange setFileName
                        ]
                        Html.span [
                            prop.className "swt:text-xs swt:opacity-70"
                            //prop.textf "Current: %s" fileName
                        ]
                    ]
                ]
        |}

let dataAnnotatorWidget: WidgetType * WidgetDefinition =
    WidgetType.DataAnnotator,
    {|
        prefix = "DATAANNOTATOR"
        content =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:min-w-64"
                prop.children [
                    Html.h3 [
                        prop.className "swt:font-bold"
                        prop.text "Data Annotator"
                    ]
                    Html.label [
                        prop.className "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
                        prop.children [
                            Html.input [
                                prop.type'.checkbox
                                prop.className "swt:checkbox swt:checkbox-sm"
                                //prop.isChecked annotateEnabled
                                //prop.onChange (fun (isChecked: bool) -> setAnnotateEnabled isChecked)
                            ]
                            Html.span [ prop.text "Enable annotation" ]
                        ]
                    ]
                    Html.span [
                        prop.className "swt:text-xs swt:opacity-70"
                        //prop.text (
                        //    if annotateEnabled then
                        //        "Status: enabled"
                        //    else
                        //        "Status: disabled"
                        //)
                    ]
                ]
            ]
    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        buildingBlockWidget arcFileState activeTableIndex setArcFileState
        templateWidget
        filePickerWidget
        dataAnnotatorWidget
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
