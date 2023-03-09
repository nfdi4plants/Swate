module Spreadsheet.KeyboardShortcuts

let private onKeydownEvent (dispatch: Messages.Msg -> unit) =
    fun (e: Browser.Types.Event) ->
        //e.preventDefault()
        //e.stopPropagation()
        let e = e :?> Browser.Types.KeyboardEvent
        match e.ctrlKey, e.which with
        | false, _ -> ()
        // Ctrl + c
        | _, _ ->
            match e.ctrlKey, e.which with
            | true, 67. ->
                Spreadsheet.CopySelectedCell |> Messages.SpreadsheetMsg |> dispatch
            // Ctrl + c
            | true, 88. ->
                Spreadsheet.CutSelectedCell |> Messages.SpreadsheetMsg |> dispatch
            // Ctrl + v
            | true, 86. ->
                Spreadsheet.PasteSelectedCell |> Messages.SpreadsheetMsg |> dispatch
            | _, _ -> ()

///<summary>These events only get reapplied on reload, not during hot reload</summary>
let addOnKeydownEvent dispatch =
    Browser.Dom.document.body.removeEventListener("keydown", onKeydownEvent dispatch)
    Browser.Dom.document.body.addEventListener("keydown", onKeydownEvent dispatch)
