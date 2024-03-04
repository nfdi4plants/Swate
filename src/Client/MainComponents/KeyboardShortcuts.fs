module Spreadsheet.KeyboardShortcuts

let onKeydownEvent (dispatch: Messages.Msg -> unit) =
    fun (e: Browser.Types.Event) ->
        //e.preventDefault()
        //e.stopPropagation()
        let e = e :?> Browser.Types.KeyboardEvent
        match e.ctrlKey, e.which with
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
        | false, key when key <= 90 && key >= 65 ->
            SetActiveCellFromSelected |> Messages.SpreadsheetMsg |> dispatch
        | false, _ -> 
            ()
        // Ctrl + c
        | _, _ ->
            match (e.ctrlKey || e.metaKey), e.which with
            // Ctrl + c
            | true, 67. ->
                Spreadsheet.CopySelectedCell |> Messages.SpreadsheetMsg |> dispatch
            // Ctrl + x
            | true, 88. ->
                Spreadsheet.CutSelectedCell |> Messages.SpreadsheetMsg |> dispatch
            // Ctrl + v
            | true, 86. ->
                Spreadsheet.PasteSelectedCell |> Messages.SpreadsheetMsg |> dispatch
            | _, _ -> ()

/// <summary>
/// Returns a function to remove the event listener
/// </summary>
/// <param name="eventHandler"></param>
let initEventListener (dispatch) : unit -> unit =
    log "INIT"
    let handle = fun (e: Browser.Types.Event) -> onKeydownEvent dispatch e
    Browser.Dom.window.addEventListener("message", handle)
    fun () -> Browser.Dom.window.removeEventListener("keydown", handle)
