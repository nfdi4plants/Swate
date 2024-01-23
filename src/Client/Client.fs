module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Messages
open Update
open Fable.Core.JsInterop
open Routing
let _ = importSideEffects "./style.scss"

///<summary> This is a basic test case used in Client unit tests </summary>
let sayHello name = $"Hello {name}"

open Feliz

let private split_container model dispatch = 
    let mainWindow = Seq.singleton <| MainWindowView.Main model dispatch
    let sideWindow = Seq.singleton <| SidebarView.SidebarView model dispatch
    SplitWindowView.Main
        mainWindow
        sideWindow
        model
        dispatch

[<ReactComponent>]
let View (model : Model) (dispatch : Msg -> unit) =
    let (colorstate, setColorstate) = React.useState(LocalStorage.Darkmode.State.init)
    let v = {colorstate with SetTheme = setColorstate}
    React.contextProvider(LocalStorage.Darkmode.themeContext, v,
        Html.div [
            Html.div [prop.id "modal-container"]
            match model.PersistentStorageState.Host with
            | Some Swatehost.Excel ->
                SidebarView.SidebarView model dispatch
            | _ ->
                split_container model dispatch
        ]
    )
            
let ARCitect_subscription (initial: Messages.Model) : (SubId * Subscribe<Messages.Msg>) list =
    let subscription (dispatch: Messages.Msg -> unit) : System.IDisposable =
        let rmv = ARCitect.Interop.initEventListener (ARCitect.ARCitect.EventHandler dispatch)
        { new System.IDisposable with
            member _.Dispose() = rmv()
        }
    [ 
        // Only subscribe to ARCitect messages when host is set correctly via query param.
        if initial.PersistentStorageState.Host = Some (Swatehost.ARCitect) then
            ["ARCitect"], subscription 
    ]    

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram Init.init Update.update View
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withSubscription ARCitect_subscription
|> Program.toNavigable (parsePath Routing.Routing.route) Update.urlUpdate
|> Program.withReactBatched "elmish-app"
#if DEBUG
//|> Program.withDebuggerCoders CustomDebugger.modelEncoder CustomDebugger.modelDecoder
|> Program.withDebugger
#endif
|> Program.run
