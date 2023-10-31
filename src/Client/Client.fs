module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Messages
open Update
open Fable.Core.JsInterop
let _ = importSideEffects "./style.scss"

///<summary> This is a basic test case used in Client unit tests </summary>
let sayHello name = $"Hello {name}"

open Feliz

let split_container model dispatch = 
    let mainWindow = Seq.singleton <| MainWindowView.Main model dispatch
    let sideWindow = Seq.singleton <| SidebarView.SidebarView model dispatch
    SplitWindowView.Main
        mainWindow
        sideWindow
        dispatch

let view (model : Model) (dispatch : Msg -> unit) =
    match model.PersistentStorageState.Host with
    | Swatehost.Excel (h,p) ->
        SidebarView.SidebarView model dispatch
    | _ ->
        split_container model dispatch
            
    
#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram Init.init Update.update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.toNavigable (parseHash Routing.Routing.route) Update.urlUpdate
|> Program.withReactBatched "elmish-app"
#if DEBUG
//|> Program.withDebuggerCoders CustomDebugger.modelEncoder CustomDebugger.modelDecoder
|> Program.withDebugger
#endif
|> Program.run
