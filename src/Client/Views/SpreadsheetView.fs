module SpreadsheetView

open Feliz
open Feliz.Bulma

open Messages

type private CellState = {
    Selected: bool
    Active: bool
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
let Cell(index: (int*int), dataState: Context.SpreadsheetData, isHeader:bool) =
    let state_cell, setState_cell = React.useState(CellState.init(dataState.State.[index]))
    let innerPadding = style.padding(0, 3)
    let cell : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    let state, setState = dataState.State, dataState.SetState
    cell [
        prop.key $"Cell_{fst index}-{snd index}"
        prop.style [
            style.minWidth 100
            style.width(length.percent 100)
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
                        state.Change(index, fun _ -> Some state_cell.Value) |> setState
                        setState_cell {state_cell with Active = false}
                    )
                    // .. when pressing "ENTER". "ESCAPE" will negate changes.
                    prop.onKeyDown(fun e ->
                        match e.which with
                        | 13. -> //enter
                            state.Change(index, fun _ -> Some state_cell.Value) |> setState
                            setState_cell {state_cell with Active = false}
                        | 27. -> //escape
                            setState_cell {state_cell with Active = false; Value = state.[index]}
                        | _ -> ()
                    )
                    // Only change cell value while typing to increase performance. 
                    prop.onChange(fun e ->
                        setState_cell {state_cell with Value = e}
                    )
                    prop.defaultValue state_cell.Value
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
                    prop.text state_cell.Value
                ]
        ]
    ]

let private bodyRow (state: Context.SpreadsheetData) (i:int) (model:Model) (dispatch: Msg -> unit) =
    let r = state.State |> Map.filter (fun (r,_) _ -> r = i)
    //Html.div [
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Key, state, false)
    ]

let private headerRow (state: Context.SpreadsheetData) (model:Model) (dispatch: Msg -> unit) =
    let rowInd = 0
    let r = state.State |> Map.filter (fun (r,_) _ -> r = rowInd)
    //Html.div [
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Key, state, true)
    ]

[<ReactComponent>]
let Main (model:Model) (dispatch: Msg -> unit) =
    // builds main container filling all possible space
    let state = React.useContext(Context.SpreadsheetDataCtx)
    let rows = state.State.Keys |> Seq.maxBy fst |> fst
    Html.div [
        Bulma.button.button [
            prop.onClick(fun _ -> printfn "%A" state.State )
            prop.text "print map"
        ]
        Bulma.button.button [
            prop.onClick(fun _ ->
                let set = state.SetState
                state.State.Change((1,1),fun _ -> Some "This is new!") |> set
            )
            prop.text "set 1-1"
        ]
        Html.div [
            prop.style [
                style.width (length.percent 100)
                style.height (length.percent 100)
                style.overflowX.scroll
            ]
            prop.children [
                //
                Html.table [
                    Html.thead [
                        headerRow state model dispatch
                    ]
                    Html.tbody [
                        for rowInd in 1 .. rows do
                            yield bodyRow state rowInd model dispatch 
                    ]
                ]
                //Html.div [
                //    prop.style [
                //        style.display.flex
                //        style.flexDirection.row
                //        style.maxWidth.maxContent
                //    ]
                //    prop.children [
                //        for i in 0 .. model.InputMap_Rows do
                //            yield row i model dispatch 
                //    ]
                //]
            ]
        ]
    ]