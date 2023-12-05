module MainComponents.FooterTabs

open Feliz
open Feliz.Bulma
open ARCtrl.ISA

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

let private popup (x: int, y: int) renameMsg deleteMsg (rmv: _ -> unit) =
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
    let button (name:string, msg, props) = Html.li [
        Bulma.button.button [
            prop.style [style.borderRadius 0]
            prop.onClick msg
            Bulma.button.isFullWidth
            Bulma.button.isSmall
            yield! props
            prop.text name
        ]
    ]
    Html.div [
        prop.style [
            let height = 53
            style.backgroundColor "white"
            style.position.absolute
            style.left x
            style.top (y - height)
            style.zIndex 20
            style.width 70
            style.height height
        ]
        prop.children [
            rmv_element
            Html.ul [
                button ("Delete", deleteMsg rmv, [Bulma.color.isDanger])
                button ("Rename", renameMsg rmv, [])
            ]
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
        Browser.Dom.console.log(data)
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
let Main (input: {|index: int; tables: ArcTables; model: Messages.Model; dispatch: Messages.Msg -> unit|}) =
    let index = input.index
    let table = input.tables.GetTableAt(index)
    let state, setState = React.useState(FooterTab.init(table.Name))
    let dispatch = input.dispatch
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
        if input.model.SpreadsheetModel.ActiveTableIndex = index then Bulma.tab.isActive
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveTable index |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY
            let deleteMsg rmv = fun e -> rmv e; Spreadsheet.RemoveTable index |> Messages.SpreadsheetMsg |> dispatch
            let renameMsg rmv = fun e -> rmv e; {state with IsEditable = true} |> setState
            let child = popup mousePosition renameMsg deleteMsg
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.draggable true
        prop.children [
            if state.IsEditable then
                let updateName = fun e ->
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
                Html.a [prop.text (table.Name)]
        ]
    ]

[<ReactComponent>]
let MainPlus(input:{|dispatch: Messages.Msg -> unit|}) =
    let dispatch = input.dispatch
    let state, setState = React.useState(FooterTab.init())
    let order = System.Int32.MaxValue
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
        prop.style [style.custom ("order", order); style.height (length.percent 100); style.cursor.pointer]
        prop.children [
            Html.a [
                prop.style [style.height.inheritFromParent; style.pointerEvents.none]
                prop.children[
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