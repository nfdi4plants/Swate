module ARCitect.ARCitect

open ARCitect.Interop

let send (msg:Msg) =
    match msg with
    | Init ->
        postMessageToARCitect (msg, "Hello from Swate!")
    | Error exn ->
        postMessageToARCitect (msg, exn)

let EventHandler (dispatch: Messages.Msg -> unit) : IEventHandler =
    {
        InitResponse = fun msg ->
            Browser.Dom.console.log(msg)
        Error = fun exn ->
            Browser.Dom.window.alert(exn)
    }