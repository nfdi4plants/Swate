module App


open Elmish
open Elmish.Navigation
open Elmish.UrlParser
open Elmish.React // do not delete this line, it is required #if !DEBUG
open Fable.Core.JsInterop


importSideEffects "./style.scss"
importSideEffects "./tailwindstyle.scss"

#if DEBUG
open Elmish.HMR
#endif

Program.mkProgram Init.init Update.Update.update Index.View
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withSubscription ARCitect.ARCitect.subscription
|> Program.toNavigable (parsePath Routing.Routing.route) Update.Update.urlUpdate
|> Program.withReactBatched "elmish-app"
|> Program.run
