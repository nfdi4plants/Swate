module SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages

type private CellState = {
    Selected: bool
    Active: bool
    /// This value is used to show during input cell editing. After confirming edit it will be used to push update
    Value: string
} with
    static member init() =
        {
            Selected    = false
            Active      = false
            Value       = ""
        }
    static member init(v: string) =
        {
            Selected    = false
            Active      = false
            Value       = v
        }


// 3. Change value to term

[<ReactComponent>]
let Cell(index: (int*int), isHeader:bool, model: Model, dispatch) =
    let state = model.SpreadsheetModel
    let cell = state.ActiveTable.[index]
    let cell_value = if isHeader then cell.Header.SwateColumnHeader else cell.Body.Term.Name
    let state_cell, setState_cell = React.useState(CellState.init(cell_value))
    let innerPadding = style.padding(0, 3)
    let cell_element : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    /// TODO! Try get from db?
    let updateMainStateTable dispatch =
        // Only update if changed
        if state_cell.Value <> cell_value then
            let nextTerm = cell.updateDisplayValue state_cell.Value
            Msg.UpdateTable (index, nextTerm) |> SpreadsheetMsg |> dispatch
    cell_element [
        prop.key $"{state.Tables.[state.ActiveTableIndex].Id}_Cell_{fst index}-{snd index}"
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
                            setState_cell {state_cell with Active = false}
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
                Html.p [
                    prop.style [
                        innerPadding
                        style.width(length.percent 100)
                    ]
                    //prop.onClick(fun e ->
                    //    e.preventDefault()
                    //    e.stopPropagation()
                    //    setModel {model with Selected = not model.Selected}
                    //)
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
                        printfn "[ROWS] %A" rows
                        for rowInd in 1 .. rows do
                            yield bodyRow rowInd model dispatch 
                    ]
                ]
            ]
        ]
    ]