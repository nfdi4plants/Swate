module MainComponents.SpreadsheetView

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages

type private CellState = {
    Active: bool
    /// This value is used to show during input cell editing. After confirming edit it will be used to push update
    Value: string
} with
    static member init() =
        {
            Active      = false
            Value       = ""
        }
    static member init(v: string) =
        {
            Active      = false
            Value       = v
        }

type private ContextFunctions = {
    DeleteRow       : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    DeleteColumn    : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Copy            : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Cut             : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
    Paste           : (Browser.Types.MouseEvent -> unit) -> Browser.Types.MouseEvent -> unit
        
}

let private contextmenu (x: int, y: int) (funcs:ContextFunctions) (rmv: _ -> unit) =
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
            prop.style [style.borderRadius 0; style.justifyContent.spaceBetween]
            prop.onClick msg
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
        Html.div [ prop.style [style.border(2, borderStyle.solid, NFDIColors.DarkBlue.Base); style.margin(2,0)] ]
    ]
    let buttonList = [
        button ("Copy", "fa-solid fa-copy", funcs.Copy rmv, [])
        button ("Cut", "fa-solid fa-scissors", funcs.Cut rmv, [])
        button ("Paste", "fa-solid fa-paste",  funcs.Paste rmv, [prop.disabled Spreadsheet.Table.Controller.clipboardCell.IsNone])
        divider
        button ("Delete Row", "fa-solid fa-delete-left", funcs.DeleteRow rmv, [])
        button ("Delete Column", "fa-solid fa-delete-left fa-rotate-270", funcs.DeleteColumn rmv, [])
    ]
    Html.div [
        prop.style [
            style.backgroundColor "white"
            style.position.absolute
            style.left x
            style.top (y - 40)
            style.zIndex 20
            style.width 150
            style.zIndex 31 // to overlap navbar
            style.border(1, borderStyle.solid, NFDIColors.DarkBlue.Base)
        ]
        prop.children [
            rmv_element
            Html.ul buttonList
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
    let isSelected = state.SelectedCells.Contains index
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
            style.border(length.px 1, borderStyle.solid, "darkgrey")
            if isHeader then style.backgroundColor.coral
            if isSelected then style.backgroundColor(NFDIColors.Mint.Lighter80)
        ]
        prop.onDoubleClick(fun e ->
            e.preventDefault()
            e.stopPropagation()
            UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
            if not state_cell.Active then setState_cell {state_cell with Active = true}
        )
        if not isHeader then
            prop.onContextMenu(fun e ->
                e.stopPropagation()
                e.preventDefault()
                let mousePosition = int e.pageX, int e.pageY
                let funcs = {
                    DeleteRow       = fun rmv e  -> rmv e; Spreadsheet.DeleteRow index_row |> Messages.SpreadsheetMsg |> dispatch
                    DeleteColumn    = fun rmv e  -> rmv e; Spreadsheet.DeleteColumn index_column |> Messages.SpreadsheetMsg |> dispatch
                    Copy            = fun rmv e  -> rmv e; Spreadsheet.CopyCell index |> Messages.SpreadsheetMsg |> dispatch
                    Cut             = fun rmv e  -> rmv e; Spreadsheet.CutCell index |> Messages.SpreadsheetMsg |> dispatch
                    Paste           = fun rmv e  -> rmv e; Spreadsheet.InsertCell index |> Messages.SpreadsheetMsg |> dispatch
        
                }
                let child = contextmenu mousePosition funcs
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
                let id = sprintf "span_%A" index
                Html.p [
                    prop.onClick(fun _ ->
                        let next = Set([index])
                        UpdateSelectedCells next |> SpreadsheetMsg |> dispatch
                    )
                    //prop.onBlur(fun _ ->
                    //    UpdateSelectedCells Set.empty |> SpreadsheetMsg |> dispatch
                    //)
                    prop.id id
                    prop.style [
                        innerPadding
                        style.width(length.percent 100)
                        style.height(length.percent 100)
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