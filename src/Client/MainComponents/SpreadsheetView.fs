module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages

type private CellState = {
    Selected: bool
    Active: bool
    /// This value is used to show during input cell editing. After confirming edit it will be used to push update
    Value: string
    Width: int
    Height: int
} with
    static member init() =
        {
            Selected    = false
            Active      = false
            Value       = ""
            Width       = 0
            Height      = 0
        }
    static member init(v: string) =
        {
            Selected    = false
            Active      = false
            Value       = v
            Width       = 0
            Height      = 0
        }


// 3. Change value to term

let private contextmenu (x: int, y: int) deleteRow deleteCol (rmv: _ -> unit) =
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
            style.width 100
            style.height height
            style.zIndex 31 // to overlap navbar
        ]
        prop.children [
            rmv_element
            Html.ul [
                button ("Delete Row", deleteRow rmv, [])
                button ("Delete Column", deleteCol rmv, [])
            ]
        ]
    ]

[<ReactComponent>]
let Cell(index: (int*int), isHeader:bool, model: Model, dispatch) =
    let index_column = fst index
    let index_row = snd index
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.[index]
    let cell_value = if isHeader then cell.Header.SwateColumnHeader else cell.Body.Term.Name
    let state_cell, setState_cell = React.useState(CellState.init(cell_value))
    let innerPadding = style.padding(0, 3)
    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    /// TODO! Try get from db?
    /// Update change to mainState and exit active input.
    let updateMainStateTable dispatch =
        // Only update if changed
        if state_cell.Value <> cell_value then
            let nextTerm = cell.updateDisplayValue state_cell.Value
            Msg.UpdateTable (index, nextTerm) |> SpreadsheetMsg |> dispatch
        setState_cell {state_cell with Active = false}
    cell_element [
        prop.key $"{state.Tables.[state.ActiveTableIndex].Id}_Cell_{index_column}-{index_row}"
        prop.style [
            style.minWidth 100
            style.height 22
            if isHeader then style.backgroundColor.coral
            style.border(length.px 1, borderStyle.solid, if state_cell.Selected then "green" else "darkgrey")
        ]
        prop.onDoubleClick(fun e ->
            e.preventDefault()
            e.stopPropagation()
            if not state_cell.Active then setState_cell {state_cell with Active = true}
        )
        if not isHeader then
            prop.onContextMenu(fun e ->
                e.stopPropagation()
                e.preventDefault()
                let mousePosition = int e.pageX, int e.pageY
                //let deleteMsg rmv = fun e -> rmv e; Spreadsheet.RemoveTable input.i |> Messages.SpreadsheetMsg |> dispatch
                //let renameMsg rmv = fun e -> rmv e; {state with IsEditable = true} |> setState
                let deleteRow rmv = fun e -> rmv e; Spreadsheet.DeleteRow index_row |> Messages.SpreadsheetMsg |> dispatch
                let deleteColumn rmv = fun e -> rmv e; Spreadsheet.DeleteColumn index_column |> Messages.SpreadsheetMsg |> dispatch
                let child = contextmenu mousePosition deleteRow deleteColumn
                let name = $"context_{mousePosition}"
                Modals.Controller.renderModal(name, child)
            )
        prop.children [
            if state_cell.Active then
                //Html.input [
                Bulma.input.text [
                    prop.style [
                        if isHeader then style.fontWeight.bold
                        innerPadding
                        style.width(length.percent 100)
                        style.height.unset
                        style.borderRadius(0)
                        style.border(0,borderStyle.none,"")
                        style.backgroundColor.transparent
                    ]
                    // Update main spreadsheet state when leaving focus or...
                    prop.onBlur(fun _ ->
                        updateMainStateTable dispatch
                    )
                    // .. when pressing "ENTER". "ESCAPE" will negate changes.
                    prop.onKeyDown(fun e ->
                        match e.which with
                        | 13. -> //enter
                            updateMainStateTable dispatch
                        | 27. -> //escape
                            setState_cell {state_cell with Active = false; Value = cell_value}
                        | _ -> ()
                    )
                    // Only change cell value while typing to increase performance. 
                    prop.onChange(fun e ->
                        setState_cell {state_cell with Value = e}
                    )
                    prop.defaultValue cell_value
                ]
            else
                let id = sprintf "span,%A" index
                Html.p [
                    prop.id id
                    prop.style [
                        innerPadding
                        style.width(length.percent 100)
                    ]
                    prop.text cell_value
                ]
        ]
    ]

let private bodyRow (i:int) (model:Model) (dispatch: Msg -> unit) =
    let state = model.SpreadsheetModel
    let r = state.ActiveTable |> Map.filter (fun (_,r) _ -> r = i)
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Key, false, model, dispatch)
    ]

let private headerRow (model:Model) (dispatch: Msg -> unit) =
    let rowInd = 0
    let state = model.SpreadsheetModel
    let r = state.ActiveTable |> Map.filter (fun (_,r) _ -> r = rowInd)
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Key, true, model, dispatch)
    ]

[<ReactComponent>]
let Main (model:Model) (dispatch: Msg -> unit) =
    let state = model.SpreadsheetModel
    Html.div [
        prop.style [style.border(1, borderStyle.solid, "grey"); style.width.minContent]
        prop.children [
            Html.table [
                prop.className "fixed_headers"
                //prop.style [style.height.minContent; style.width.minContent]
                prop.children [
                    Html.thead [
                        headerRow model dispatch
                    ]
                    Html.tbody [
                        let rows = state.ActiveTable.Keys |> Seq.maxBy snd |> snd
                        for rowInd in 1 .. rows do
                            yield bodyRow rowInd model dispatch 
                    ]
                ]
            ]
        ]
    ]