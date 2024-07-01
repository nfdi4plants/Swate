module Spreadsheet.KeyboardShortcuts

let onKeydownEvent (dispatch: Messages.Msg -> unit) =
    fun (e: Browser.Types.Event) ->
        let e = e :?> Browser.Types.KeyboardEvent
        match (e.ctrlKey || e.metaKey), e.which with
        | false, 27. | false, 13. | false, 9. | false, 16.  -> // escape, enter, tab, shift
            ()
        | false, 46. -> // del
            Spreadsheet.ClearSelected |> Messages.SpreadsheetMsg |> dispatch
        | false, 37. -> // arrow left
            MoveSelectedCell Key.Left |> Messages.SpreadsheetMsg |> dispatch
        | false, 38. -> // arrow up
            MoveSelectedCell Key.Up |> Messages.SpreadsheetMsg |> dispatch
        | false, 39. -> // arrow right
            MoveSelectedCell Key.Right |> Messages.SpreadsheetMsg |> dispatch
        | false, 40. -> // arrow down
            MoveSelectedCell Key.Down |> Messages.SpreadsheetMsg |> dispatch
        | false, key -> 
            SetActiveCellFromSelected |> Messages.SpreadsheetMsg |> dispatch
        // Ctrl + c
        | true, _ ->
            match e.which with
            | 67. -> // Ctrl + c
                Spreadsheet.CopySelectedCells |> Messages.SpreadsheetMsg |> dispatch
            | 88. -> // Ctrl + x
                Spreadsheet.CutSelectedCells |> Messages.SpreadsheetMsg |> dispatch
            |  86. -> // Ctrl + v
                Spreadsheet.PasteSelectedCells |> Messages.SpreadsheetMsg |> dispatch
            | _ -> ()

