module MainComponents.FooterTabs

open Feliz
open Feliz.Bulma
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
        Delete  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        Rename  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let rmv_element = Html.div [
            prop.onClick rmv
            prop.onContextMenu(fun e -> e.preventDefault(); rmv e)
            prop.style [
                style.position.fixedRelativeToWindow
                style.backgroundColor.transparent
                style.left 0
                style.top 0
                style.right 0
                style.bottom 0
                style.display.block
            ]
        ]
        let button (name:string, icon: string, msg, props) = Html.li [
            Bulma.button.button [
                prop.style [style.borderRadius 0; style.justifyContent.spaceBetween; style.fontSize (length.rem 0.9)]
                prop.onClick msg
                prop.className "py-1"
                Bulma.button.isFullWidth
                //Bulma.button.isSmall
                Bulma.color.isBlack
                Bulma.button.isInverted
                yield! props
                prop.children [
                    Bulma.icon [Html.i [prop.className icon]]
                    Html.span name
                ]
            ]
        ]
        let divider = Html.li [
            Html.div [ prop.style [style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0); style.width (length.perc 75); style.marginLeft length.auto] ]
        ]
        let buttonList = [
            button ("Delete", "fa-solid fa-trash", funcs.Delete rmv, [])
            button ("Rename", "fa-solid fa-pen-to-square", funcs.Rename rmv, [])
        ]
        Html.div [
            prop.style [
                style.backgroundColor "white"
                style.position.absolute
                style.left mousex
                style.top (mousey-40)
                style.width 150
                style.zIndex 40 // to overlap navbar
                style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
            ]
            prop.children [
                rmv_element
                Html.ul buttonList
            ]
        ]


[<RequireQualifiedAccess>]
module private PlusContextMenu =
    type ContextFunctions = {
        AddTable  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        AddDatamap  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let rmv_element = Html.div [
            prop.onClick rmv
            prop.onContextMenu(fun e -> e.preventDefault(); rmv e)
            prop.style [
                style.position.fixedRelativeToWindow
                style.backgroundColor.transparent
                style.left 0
                style.top 0
                style.right 0
                style.bottom 0
                style.display.block
            ]
        ]
        let button (name:string, icon: string, msg, props) = Html.li [
            Bulma.button.button [
                prop.style [style.borderRadius 0; style.justifyContent.spaceBetween; style.fontSize (length.rem 0.9)]
                prop.onClick msg
                prop.className "py-1"
                Bulma.button.isFullWidth
                //Bulma.button.isSmall
                Bulma.color.isBlack
                Bulma.button.isInverted
                yield! props
                prop.children [
                    Bulma.icon [Html.i [prop.className icon]]
                    Html.span name
                ]
            ]
        ]
        let divider = Html.li [
            Html.div [ prop.style [style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0); style.width (length.perc 75); style.marginLeft length.auto] ]
        ]
        let buttonList = [
            button ("Add Table", "fa-solid fa-table", funcs.AddTable rmv, [])
            button ("Add Datamap", "fa-solid fa-map", funcs.AddDatamap rmv, [])
        ]
        Html.div [
            prop.style [
                style.backgroundColor "white"
                style.position.absolute
                style.left mousex
                style.top (mousey-40)
                style.width 150
                style.zIndex 40 // to overlap navbar
                style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
            ]
            prop.children [
                rmv_element
                Html.ul buttonList
            ]
        ]

module DataMapContextMenu =
    type ContextFunctions = {
        Delete  : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    }

    let contextmenu (mousex: int, mousey: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
        /// This element will remove the contextmenu when clicking anywhere else
        let rmv_element = Html.div [
            prop.onClick rmv
            prop.onContextMenu(fun e -> e.preventDefault(); rmv e)
            prop.style [
                style.position.fixedRelativeToWindow
                style.backgroundColor.transparent
                style.left 0
                style.top 0
                style.right 0
                style.bottom 0
                style.display.block
            ]
        ]
        let button (name:string, icon: string, msg, props) = Html.li [
            Bulma.button.button [
                prop.style [style.borderRadius 0; style.justifyContent.spaceBetween; style.fontSize (length.rem 0.9)]
                prop.onClick msg
                prop.className "py-1"
                Bulma.button.isFullWidth
                //Bulma.button.isSmall
                Bulma.color.isBlack
                Bulma.button.isInverted
                yield! props
                prop.children [
                    Bulma.icon [Html.i [prop.className icon]]
                    Html.span name
                ]
            ]
        ]
        let divider = Html.li [
            Html.div [ prop.style [style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0); style.width (length.perc 75); style.marginLeft length.auto] ]
        ]
        let buttonList = [
            button ("Delete", "fa-solid fa-trash", funcs.Delete rmv, [])
        ]
        Html.div [
            prop.style [
                style.backgroundColor "white"
                style.position.absolute
                style.left mousex
                style.top (mousey-40)
                style.width 150
                style.zIndex 40 // to overlap navbar
                style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
            ]
            prop.children [
                rmv_element
                Html.ul buttonList
            ]
        ]

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
    Bulma.tab [
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
        if model.SpreadsheetModel.ActiveView = Spreadsheet.ActiveView.Table index then Bulma.tab.isActive
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView (Spreadsheet.ActiveView.Table index) |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY
            let deleteMsg rmv = fun e -> rmv e; Spreadsheet.RemoveTable index |> Messages.SpreadsheetMsg |> dispatch
            let renameMsg rmv = fun e -> rmv e; {state with IsEditable = true} |> setState
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
                Bulma.input.text [
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
                Html.a [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-table"]]
                    Html.text table.Name
                ]
        ]
    ]

[<ReactComponent>]
let MainMetadata(model: Model, dispatch: Messages.Msg -> unit) =
    let id = "Metadata-Tab"
    let nav = Spreadsheet.ActiveView.Metadata
    let order = nav.TableIndex
    Bulma.tab [
        if model.SpreadsheetModel.ActiveView = nav then Bulma.tab.isActive
        prop.key id
        prop.id id
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView nav |> Messages.SpreadsheetMsg |> dispatch)
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.a  [
                Bulma.icon [Html.i [prop.className "fa-solid fa-circle-info"]]
                Html.text "Metadata"
            ]
        ]
    ]

[<ReactComponent>]
let MainDataMap(model: Model, dispatch: Messages.Msg -> unit) =
    let id = "Metadata-Tab"
    let nav = Spreadsheet.ActiveView.DataMap
    let order = nav.TableIndex
    Bulma.tab [
        if model.SpreadsheetModel.ActiveView = nav then Bulma.tab.isActive
        prop.key id
        prop.id id
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveView nav |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY
            let deleteDatamapMsg rmv = fun e -> rmv e; SpreadsheetInterface.UpdateDatamap None |> Messages.InterfaceMsg |> dispatch
            let funcs : DataMapContextMenu.ContextFunctions = {
                Delete = deleteDatamapMsg
            }
            let child = DataMapContextMenu.contextmenu mousePosition funcs
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.a [
                Bulma.icon [Html.i [prop.className "fa-solid fa-map"]]
                Html.text "Data Map"
            ]
        ]
    ]

[<ReactComponent>]
let MainPlus(model: Model, dispatch: Messages.Msg -> unit) =
    let state, setState = React.useState(FooterTab.init())
    let order = System.Int32.MaxValue-1 // MaxValue will be sidebar toggle
    let id = "Add-Spreadsheet-Button"
    Bulma.tab [
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
            let mousePosition = int e.pageX, int e.pageY
            let addTableMsg rmv = fun e -> rmv e; SpreadsheetInterface.CreateAnnotationTable false |> Messages.InterfaceMsg |> dispatch
            let addDatamapMsg rmv = fun e -> rmv e; SpreadsheetInterface.UpdateDatamap (DataMap.init() |> Some) |> Messages.InterfaceMsg |> dispatch
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
            Html.a [
                prop.style [style.height.inheritFromParent; style.pointerEvents.none]
                prop.children [
                    Bulma.icon [
                        Bulma.icon.isSmall
                        prop.children [
                            Html.i [prop.className "fa-solid fa-plus"]
                        ]
                    ]
                ]
            ]
        ]
    ]

let ToggleSidebar(model: Model, dispatch: Messages.Msg -> unit) =
    let show = model.PersistentStorageState.ShowSideBar
    let order = System.Int32.MaxValue
    let id = "Toggle-Sidebar-Button"
    Bulma.tab [
        prop.key id
        prop.id id
        prop.onClick (fun e -> Messages.PersistentStorage.UpdateShowSidebar (not show) |> Messages.PersistentStorageMsg |> dispatch)
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer; style.marginLeft length.auto]
        prop.children [
            Html.a [
                prop.style [style.height.inheritFromParent; style.pointerEvents.none]
                prop.children [
                    Bulma.icon [
                        Bulma.icon.isSmall
                        prop.children [
                            Html.i [prop.className ["fa-solid"; if show then "fa-chevron-right" else "fa-chevron-left"]]
                        ]
                    ]
                ]
            ]
        ]
    ]