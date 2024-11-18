module MainComponents.FooterTabs

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Model

type private FooterTab = {
    IsEditable: bool
    IsDraggedOver: bool
    Name: string
} with
    static member init(?name: string) = {
        IsEditable = false
        IsDraggedOver = false
        Name = Option.defaultValue "" name
    }

[<RequireQualifiedAccess>]
module private TableContextMenu =

    type ContextFunctions = {
        Delete  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        Rename  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        let buttonList = [
            Components.BaseContextMenu.Item (Html.span "Delete", funcs.Delete rmv, "fa-solid fa-trash")
            Components.BaseContextMenu.Item (Html.span "Rename", funcs.Rename rmv, "fa-solid fa-pen-to-square")
        ]
        Components.BaseContextMenu.Main(mousex, mousey, rmv, buttonList)


[<RequireQualifiedAccess>]
module private PlusContextMenu =
    type ContextFunctions = {
        AddTable  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
        AddDatamap  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        let buttonList = [
            Components.BaseContextMenu.Item (Html.span "Add Table", funcs.AddTable rmv, "fa-solid fa-table")
            Components.BaseContextMenu.Item (Html.span "Add Datamap", funcs.AddDatamap rmv, "fa-solid fa-map")
        ]
        Components.BaseContextMenu.Main(mousex, mousey, rmv, buttonList)

module DataMapContextMenu =
    type ContextFunctions = {
        Delete  : (unit -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        let buttonList = [
            Components.BaseContextMenu.Item (Html.span "Delete", funcs.Delete rmv, "fa-solid fa-trash")
        ]
        Components.BaseContextMenu.Main(mousex, mousey, rmv, buttonList)

open Spreadsheet.Types

///<summary>This must be used on dragover events to enable dropping elements on them.</summary>
let private drag_preventdefault = fun (e: Browser.Types.DragEvent) ->
    e.preventDefault()
    e.stopPropagation()

///<summary>This is fired from the element on which something is dropped. Gets the data set during dragstart and uses it to update order.</summary>
let private drop_handler (eleOrder, state, setState, dispatch) = fun (e: Browser.Types.DragEvent) ->
    // This event fire on the element on which something is dropped! Not on the element which is dropped!
    let data = e.dataTransfer.getData("text")
    let getData = FooterReorderData.ofJson data
    setState {state with IsDraggedOver = false}
    match getData with
    | Ok data ->
        let prev_index = data.OriginOrder
        let next_index = eleOrder
        Spreadsheet.UpdateTableOrder(prev_index, next_index) |> Messages.SpreadsheetMsg |> dispatch
    | _ ->
        ()

///<summary>Sets styling on event, styling must then be removed ondragleave and ondrop.</summary>
let private dragenter_handler(state, setState) = fun (e: Browser.Types.DragEvent) ->
    e.preventDefault()
    e.stopPropagation()
    setState {state with IsDraggedOver = true}

///<summary>Removes dragenter styling.</summary>
let private dragleave_handler(state, setState) = fun (e: Browser.Types.DragEvent) ->
    e.preventDefault()
    e.stopPropagation()
    setState {state with IsDraggedOver = false}

[<ReactComponent>]
let Main (index: int, tables: ArcTables, model: Model, dispatch: Messages.Msg -> unit) =
    let table = tables.GetTableAt(index)
    let state, setState = React.useState(FooterTab.init(table.Name))
    let id = $"ReorderMe_{index}_{table.Name}"
    Daisy.tab [
        if state.IsDraggedOver then prop.className "dragover-footertab"
        prop.draggable true
        prop.onDrop <| drop_handler (index, state, setState, dispatch)
        prop.onDragLeave <| dragleave_handler (state, setState)
        prop.onDragStart(fun e ->
            e.dataTransfer.clearData() |> ignore
            let data = FooterReorderData.create index id
            let dataJson = data.toJson()
            e.dataTransfer.setData("text", dataJson) |> ignore
            ()
        )
        prop.onDragEnter <| dragenter_handler(state, setState)
        prop.onDragOver drag_preventdefault
        // This will determine the position of the tab
        prop.style [style.custom ("order", index)]
        // Use this to ensure updating reactelement correctly
        prop.key id
        prop.id id
        if model.SpreadsheetModel.ActiveView = Spreadsheet.ActiveView.Table index then tab.active
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView (Spreadsheet.ActiveView.Table index) |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY - 30
            let deleteMsg rmv = fun _ -> rmv(); Spreadsheet.RemoveTable index |> Messages.SpreadsheetMsg |> dispatch
            let renameMsg rmv = fun _ -> rmv(); {state with IsEditable = true} |> setState
            let funcs : TableContextMenu.ContextFunctions = {
                Rename = renameMsg
                Delete = deleteMsg
            }
            let child = TableContextMenu.contextmenu mousePosition funcs
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.draggable true
        prop.children [
            if state.IsEditable then
                let updateName = fun e ->
                    if state.Name <> table.Name then
                        Spreadsheet.RenameTable (index, state.Name) |> Messages.SpreadsheetMsg |> dispatch
                    setState {state with IsEditable = false}
                Html.input [
                    prop.className "bg-transparent px-2 border-0 focus:ring-0"
                    prop.autoFocus(true)
                    prop.id (id + "input")
                    prop.onChange (fun e ->
                        setState {state with Name = e}
                    )
                    prop.onBlur updateName
                    // .. when pressing "ENTER". "ESCAPE" will negate changes.
                    prop.onKeyDown(fun e ->
                        match e.which with
                        | 13. -> //enter
                            updateName e
                        | 27. -> //escape
                            setState {state with IsEditable = false; Name = table.Name}
                        | _ -> ()
                    )
                    prop.defaultValue table.Name
                ]
            else
                Html.i [prop.className "fa-solid fa-table"]
                Html.span [
                    prop.className "truncate"
                    prop.text table.Name
                ]
        ]
    ]

[<ReactComponent>]
let MainMetadata(model: Model, dispatch: Messages.Msg -> unit) =
    let id = "Metadata-Tab"
    let nav = Spreadsheet.ActiveView.Metadata
    let order = nav.ViewIndex
    Daisy.tab [
        if model.SpreadsheetModel.ActiveView = nav then tab.active
        prop.key id
        prop.id id
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView nav |> Messages.SpreadsheetMsg |> dispatch)
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.i [prop.className "fa-solid fa-circle-info"]
            Html.text model.SpreadsheetModel.FileType
        ]
    ]

[<ReactComponent>]
let MainDataMap(model: Model, dispatch: Messages.Msg -> unit) =
    let id = "Metadata-Tab"
    let nav = Spreadsheet.ActiveView.DataMap
    let order = nav.ViewIndex
    Daisy.tab [
        if model.SpreadsheetModel.ActiveView = nav then tab.active
        prop.key id
        prop.id id
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView nav |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY
            let deleteDatamapMsg rmv = fun _ -> rmv(); SpreadsheetInterface.UpdateDatamap None |> Messages.InterfaceMsg |> dispatch
            let funcs : DataMapContextMenu.ContextFunctions = {
                Delete = deleteDatamapMsg
            }
            let child = DataMapContextMenu.contextmenu mousePosition funcs
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.i [prop.className "fa-solid fa-map"]
            Html.text "Data Map"
        ]
    ]

[<ReactComponent>]
let MainPlus(model: Model, dispatch: Messages.Msg -> unit) =
    let state, setState = React.useState(FooterTab.init())
    let order = System.Int32.MaxValue-1 // MaxValue will be sidebar toggle
    let id = "Add-Spreadsheet-Button"
    Daisy.tab [
        prop.key id
        prop.id id
        if state.IsDraggedOver then prop.className "dragover-footertab"
        prop.onDragEnter <| dragenter_handler(state, setState)
        prop.onDragLeave <| dragleave_handler (state, setState)
        prop.onDragOver drag_preventdefault
        prop.onDrop <| drop_handler (order, state, setState, dispatch)
        prop.onClick (fun e -> SpreadsheetInterface.CreateAnnotationTable e.ctrlKey |> Messages.InterfaceMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, (int e.pageY - 20)
            let addTableMsg rmv = fun _ -> rmv(); SpreadsheetInterface.CreateAnnotationTable false |> Messages.InterfaceMsg |> dispatch
            let addDatamapMsg rmv = fun _ -> rmv(); SpreadsheetInterface.UpdateDatamap (DataMap.init() |> Some) |> Messages.InterfaceMsg |> dispatch
            let funcs : PlusContextMenu.ContextFunctions = {
                AddTable = addTableMsg
                AddDatamap = addDatamapMsg
            }
            let child = PlusContextMenu.contextmenu mousePosition funcs
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.i [prop.className "fa-solid fa-plus"]
        ]
    ]

let ToggleSidebar(model: Model, dispatch: Messages.Msg -> unit) =
    let show = model.PersistentStorageState.ShowSideBar
    let id = "toggle-sidebar-button"
    Html.div [
        prop.id id
        prop.onClick (fun _ -> Messages.PersistentStorage.UpdateShowSidebar (not show) |> Messages.PersistentStorageMsg |> dispatch)
        prop.className "h-full cursor-pointer ml-auto"
        prop.children [
            Html.label [
                // prop.for' "split-window-drawer"
                prop.className "drawer-button btn btn-sm px-2 py-2 swap swap-rotate rounded-none h-full"
                prop.children [
                    Html.input [prop.type'.checkbox]
                    Html.i [prop.className ["fa-solid"; "fa-chevron-left"; "swap-off"]]
                    Html.i [prop.className ["fa-solid"; "fa-chevron-right"; "swap-on" ]]
                ]
            ]
        ]
    ]

let SpreadsheetSelectionFooter (model: Model) dispatch =
    Html.div [
        prop.className "sticky bottom-0 flex flex-row border-t-2"
        prop.children [
            Html.div [
                prop.className "tabs tabs-lifted w-full overflow-x-auto overflow-y-hidden
                flex flex-row items-center pt-1
                *:!border-b-0 *:gap-1 *:flex-nowrap"
                prop.children [
                    Daisy.tab  [
                        prop.style [style.width (length.px 20); style.custom ("order", -2)]
                    ]
                    MainMetadata (model, dispatch)
                    if model.SpreadsheetModel.HasDataMap() then
                        MainDataMap (model, dispatch)
                    for index in 0 .. (model.SpreadsheetModel.Tables.TableCount-1) do
                        Main (index, model.SpreadsheetModel.Tables, model, dispatch)
                    if model.SpreadsheetModel.CanHaveTables() then
                        MainPlus (model, dispatch)
                ]
            ]
            if model.SpreadsheetModel.TableViewIsActive() then
                ToggleSidebar(model, dispatch)
        ]
    ]