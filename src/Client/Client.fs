module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Messages
open Update

///<summary> This is a basic test case used in Client unit tests </summary>
let sayHello name = $"Hello {name}"

// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let route = (parseHash Routing.Routing.route) Browser.Dom.document.location
    let pageEntry = if route.IsSome then route.Value.toSwateEntry else Routing.SwateEntry.Core
    let initialModel = initializeModel (pageOpt,pageEntry)
    // The initial command from urlUpdate is not needed yet. As we use a reduced variant of subModels with no own Msg system.
    let model, _ = urlUpdate route initialModel
    let cmd = Cmd.ofMsg <| InterfaceMsg SpreadsheetInterface.Initialize 
    model, cmd

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

Program.mkProgram init Update.update view
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
