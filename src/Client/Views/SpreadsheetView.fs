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

[<ReactComponent>]
let Cell(v: string, isHeader:bool) =
    let model, setModel = React.useState(CellState.init(v))
    let innerPadding = style.padding(0, 3)
    let cell : IReactProperty list -> ReactElement = if isHeader then Html.th else Html.td
    cell [
        prop.style [
            style.minWidth 100
            style.width(length.percent 100)
            style.border(length.px 1, borderStyle.solid, if model.Selected then "green" else "darkgrey")
        ]
        prop.onDoubleClick(fun e ->
            e.preventDefault()
            e.stopPropagation()
            if not model.Active then setModel {model with Active = true}
        )
        prop.children [
            if model.Active then
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
                    prop.onBlur(fun _ ->
                        setModel {model with Active = false}
                    )
                    prop.onKeyDown(fun e ->
                        match e.which with
                        | 13. -> setModel {model with Active = false} //enter
                        | _ -> ()
                    )
                    prop.onChange(fun e ->
                        setModel {model with Value = e}
                    )
                    prop.defaultValue model.Value
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
                    prop.text model.Value
                ]
        ]
    ]

let private bodyRow (state: Context.SpreadsheetData) (i:int) (model:Model) (dispatch: Msg -> unit) =
    let r = state.State |> Map.filter (fun (r,_) _ -> r = i)
    //Html.div [
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Value, false)
    ]

let private headerRow (state: Context.SpreadsheetData) (model:Model) (dispatch: Msg -> unit) =
    let r = state.State |> Map.filter (fun (r,_) _ -> r = 0)
    //Html.div [
    Html.tr [
        for cell in r do
            yield
                Cell(cell.Value, true)
    ]

let main (model:Model) (dispatch: Msg -> unit) =
    // builds main container filling all possible space
    let state = React.useContext(Context.SpreadsheetDataCtx)
    let rows = state.State.Keys |> Seq.maxBy fst |> fst
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
                    for i in 1 .. rows do
                        yield bodyRow state i model dispatch 
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