module MainComponents.SpreadsheetView.Generic

open Feliz
open Feliz.DaisyUI

open Spreadsheet
open Messages
open Spreadsheet.Cells
open ARCtrl
open Swate.Components.Shared
open Model


[<ReactComponent>]
let Main (data, createCell, headers, createHeaderCell, model:Model, dispatch: Msg -> unit) =
    //React.useListener.on("keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    let ref = React.useElementRef()
    //React.useElementListener.on(ref, "keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    /// This state is used to track which columns are expanded
    let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
    React.useEffect((fun _ -> setState Set.empty), [|box model.SpreadsheetModel.ActiveView|])
    let createRowLabel (rowIndex: int) = MainComponents.CellStyles.RowLabel rowIndex
    Html.div [
        prop.id "SPREADSHEET_MAIN_VIEW"
        prop.key $"SPREADSHEET_MAIN_VIEW_{model.SpreadsheetModel.ActiveView.ViewIndex}"
        prop.tabIndex 0
        prop.className "flex grow overflow-y-hidden"
        prop.style [style.border(1, borderStyle.solid, "grey")]
        prop.ref ref
        prop.onKeyDown(fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
        prop.children [
            Components.LazyLoadTable.Main(
                "SpreadsheetViewTable",
                data state,
                createCell,
                {|data=headers state setState; createCell=createHeaderCell|},
                35,
                tableClasses=[|
                    //sticky header
                    "[&_thead_>_tr]:sticky [&_thead_>_tr]:top-0 [&_thead_>_tr]:bg-base-100"
                    // sticky row
                    "[&_tbody_>_tr_>_th]:sticky [&_tbody_>_tr_>_th]:left-0 [&_tbody_>_tr_>_th]:bg-base-100"
                    |],
                containerClasses=[|"pr-[10vw]"|],
                rowLabel={|styling=Some createRowLabel|}
            )
        ]
    ]