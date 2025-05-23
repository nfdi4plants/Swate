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
let Main (data, createCell, headers, createHeaderCell, model: Model, dispatch: Msg -> unit) =
    //React.useListener.on("keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    let ref = React.useElementRef ()
    //React.useElementListener.on(ref, "keydown", (Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch))
    /// This state is used to track which columns are expanded
    let state, setState: Set<int> * (Set<int> -> unit) = React.useState (Set.empty)
    React.useEffect ((fun _ -> setState Set.empty), [| box model.SpreadsheetModel.ActiveView |])

    let createRowLabel (rowIndex: int) =
        MainComponents.CellStyles.RowLabel rowIndex

    Html.div [
        prop.id "SPREADSHEET_MAIN_VIEW"
        prop.key $"SPREADSHEET_MAIN_VIEW_{model.SpreadsheetModel.ActiveView.ViewIndex}"
        prop.tabIndex 0
        prop.className "swt:flex swt:grow swt:overflow-y-hidden"
        prop.style [ style.border (1, borderStyle.solid, "grey") ]
        prop.ref ref
        prop.onKeyDown (fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
        prop.children [
            Components.LazyLoadTable.Main(
                "SpreadsheetViewTable",
                data state,
                createCell,
                {|
                    data = headers state setState
                    createCell = createHeaderCell
                |},
                35,
                tableClasses = [|
                    //sticky header
                    "swt:[&_thead_>_tr]:sticky swt:[&_thead_>_tr]:top-0 [swt:&_thead_>_tr]:bg-base-100"
                    // sticky row
                    "swt:[&_tbody_>_tr_>_th]:sticky swt:[&_tbody_>_tr_>_th]:left-0 swt:[&_tbody_>_tr_>_th]:bg-base-100"
                |],
                containerClasses = [| "swt:pr-[10vw] swt:pb-[20vw]" |],
                rowLabel = {| styling = Some createRowLabel |}
            )
        ]
    ]