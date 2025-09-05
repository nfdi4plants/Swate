module App


open Elmish
open Elmish.Navigation
open Elmish.UrlParser
open Elmish.React // do not delete this line, it is required #if !DEBUG
open Fable.Core.JsInterop


importSideEffects "./tailwind.css"
// importSideEffects "../Components/tailwind.css"
importSideEffects "./App.css"

module Subscriptions =

    let private ARCitectInAPI (dispatch: Messages.Msg -> unit) : Model.ARCitect.Interop.IARCitectInAPI = {
        TestHello = fun name -> promise { return sprintf "Hello %s" name }
        ResponsePaths =
            fun paths -> promise {
                Model.ARCitect.ResponsePaths paths |> Messages.ARCitectMsg |> dispatch
                return true
            }
        ResponseFile =
            fun file -> promise {
                Model.ARCitect.ResponseFile file |> Messages.ARCitectMsg |> dispatch
                return true
            }
        Refresh =
            fun () -> promise {
                ApiCall.Start() |> Model.ARCitect.Init |> Messages.ARCitectMsg |> dispatch
                return true
            }
        SetARCFile =
            fun (file, name) -> promise {
                ApiCall.Finished(file, name)
                |> Model.ARCitect.Init
                |> Messages.ARCitectMsg
                |> dispatch

                return true
            }
    }

    let subscription (initial: Model.Model) : (SubId * Subscribe<Messages.Msg>) list =
        let arcitect (dispatch: Messages.Msg -> unit) : System.IDisposable =
            let initEventHandler =
                MessageInterop.MessageInterop.createApi ()
                |> MessageInterop.MessageInterop.buildInProxy<Model.ARCitect.Interop.IARCitectInAPI> (
                    ARCitectInAPI dispatch
                )

            { new System.IDisposable with
                member _.Dispose() = initEventHandler ()
            }

        [ [ "ARCitect" ], arcitect ]

#if DEBUG
open Elmish.HMR
#endif

Program.mkProgram Init.init Update.Update.update Index.View
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withSubscription Subscriptions.subscription
|> Program.toNavigable (parsePath Routing.Routing.route) Update.Update.urlUpdate
|> Program.withReactBatched "elmish-app"
|> Program.run