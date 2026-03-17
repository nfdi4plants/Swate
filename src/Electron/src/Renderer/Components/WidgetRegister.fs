module Renderer.Components.WidgetRegistry

open Feliz
open Swate.Components
open ARCtrl
open Swate.Electron.Shared
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

[<ReactComponent>]
let private ARCObjectWidgetContent
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    =
    let appCtx = React.useContext Renderer.Context.AppStateCtx.AppStateCtx

    let workspaceCtx =
        React.useContext Renderer.Context.WorkspaceStateCtx.WorkspaceStateCtx

    let rootRepoPath =
        match appCtx.state with
        | AppState.ARC arcPath -> Some arcPath
        | AppState.Init -> None

    let treePane =
        Renderer.Components.ArcExplorer.createArcExplorer
            rootRepoPath
            workspaceCtx.state.ArcExplorerTree
            workspaceCtx.state.SelectedExplorerItemId
            workspaceCtx.state.SelectedTreeItemPath
            setSelectedExplorerItemId
            setSelectedTreeItemPath
            setPageState

    match treePane with
    | Some treePane -> Swate.Components.ARCObjectWidget.Main(treePane = treePane)
    | None -> Swate.Components.ARCObjectWidget.Main()

let ARCObjectWidget
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    : WidgetType * WidgetDefinition =
    WidgetType.ARCObject,
    {|
        prefix = "ARC_OBJECT"
        content = ARCObjectWidgetContent setSelectedExplorerItemId setSelectedTreeItemPath setPageState
    |}

let createWidgets
    (arcFileState: ArcFiles option)
    (activeView: WidgetHostView)
    (activeTableIndex: int option)
    (setArcFileState: ArcFiles option -> unit)
    (importType: TableJoinOptions)
    (setImportType: TableJoinOptions -> unit)
    (setSelectedExplorerItemId: string option -> unit)
    (setSelectedTreeItemPath: string option -> unit)
    (setPageState: PageState option -> unit)
    : Map<WidgetType, WidgetDefinition> =
    [
        BuildingBlockWidget arcFileState activeTableIndex setArcFileState
        TemplateWidget arcFileState activeTableIndex setArcFileState importType setImportType
        FilePickerWidget arcFileState activeTableIndex setArcFileState
        DataAnnotatorWidget arcFileState activeView activeTableIndex setArcFileState
        ARCObjectWidget setSelectedExplorerItemId setSelectedTreeItemPath setPageState
    ]
    |> Map.ofList

let private widgetRequiresTable =
    function
    | WidgetType.ARCObject -> false
    | _ -> true

[<ReactComponent>]
let NavbarButtons(widgetTypes: WidgetType list, hasSelectedTable: bool) =
    let context = WidgetContext.useWidgetController ()

    let widgetInfo (widgetType: WidgetType) =
        match widgetType with
        | WidgetType.BuildingBlock -> "Add Building Block", Icons.BuildingBlock()
        | WidgetType.Template -> "Add Template", Icons.Templates()
        | WidgetType.FilePicker -> "File Picker", Icons.FilePicker()
        | WidgetType.DataAnnotator -> "Data Annotator", Icons.DataAnnotator()
        | WidgetType.ARCObject -> "ARC Object", Icons.Docs()
        | WidgetType.Playground -> "Playground", Icons.Templates()

    let controlButton (widgetType: WidgetType) =
        let isActive = context.isActive widgetType
        let label, icon = widgetInfo widgetType
        let isDisabled = widgetRequiresTable widgetType && not hasSelectedTable
        let tooltip =
            if isDisabled then
                "Select a table to open widgets"
            elif isActive then
                $"Close {label}"
            else
                $"Open {label}"

        QuickAccessButton.QuickAccessButton(
            tooltip,
            icon,
            (fun _ -> context.toggleWidget widgetType),
            isDisabled = isDisabled,
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
    WidgetType.ARCObject
]

[<ReactComponent>]
let NavbarButtonsForAllWidgets widgets children =

    Widget.WidgetController(widgets, children = children)
