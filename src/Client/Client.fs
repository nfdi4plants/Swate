module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Messages
open Update
open ExcelJS.Fable.GlobalBindings

let sayHello name = $"Hello {name}"

let initializeAddIn () = Office.onReady()

// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let route = (parseHash Routing.Routing.route) Browser.Dom.document.location
    let pageEntry = if route.IsSome then route.Value.toSwateEntry else Routing.SwateEntry.Core
    let initialModel = initializeModel (pageOpt,pageEntry)
    // The initial command from urlUpdate is not needed yet. As we use a reduced variant of subModels with no own Msg system.
    let model, _ = urlUpdate route initialModel
    let initialCmd =
        Cmd.batch [
            Cmd.OfPromise.either
                initializeAddIn
                ()
                (fun x -> (x.host.ToString(),x.platform.ToString()) |> OfficeInterop.Initialized |> OfficeInteropMsg )
                (curry GenericError Cmd.none >> DevMsg)
        ]
    model, initialCmd

let view (model : Model) (dispatch : Msg -> unit) =
    if model.ExcelState.Host <> "null" && model.ExcelState.Platform <> "null" then
        SidebarView.sidebarView model dispatch
    else
        let mainWindow = Seq.singleton <| div [] [str "TEasinmdklasjdmlkasjdlknjaslkj"] 
        let sideWindow = Seq.singleton <| SidebarView.sidebarView model dispatch
        SplitWindowView.Main
            mainWindow
            sideWindow
            
    
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
