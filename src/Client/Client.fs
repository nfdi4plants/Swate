module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Thoth.Json
open Thoth.Elmish
open ExcelColors
open Api
open Model
open Messages
open Update
open Shared

module Server =

    open Shared
    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : IAnnotatorAPI =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<IAnnotatorAPI>

let initializeAddIn () =
    OfficeInterop.Office.onReady()
   

// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let initialModel = initializeModel pageOpt
    let route = (parseHash Routing.Routing.route) Browser.Dom.document.location
    // The initial command from urlUpdate is not needed yet. As we use a reduced variant of subModels with no own Msg system.
    let model, _ = urlUpdate route initialModel
    let initialCmd =
        Cmd.batch [
            Cmd.OfPromise.either
                initializeAddIn
                ()
                (fun x -> (x.host.ToString(),x.platform.ToString()) |> Initialized |> ExcelInterop )
                (fun x -> x |> GenericError |> Dev)
        ]
    model, initialCmd


let view (model : Model) (dispatch : Msg -> unit) =

    match model.PageState.CurrentPage with
    | Routing.Route.AddBuildingBlock ->
        BaseView.baseViewComponent model dispatch [
            AddBuildingBlockView.addBuildingBlockComponent model dispatch
        ] [
            AddBuildingBlockView.addBuildingBlockFooterComponent model dispatch
        ]


    | Routing.Route.TermSearch ->
        BaseView.baseViewComponent model dispatch [
            TermSearchView.termSearchComponent model dispatch
        ] [
            str "Footer content"
        ]


    | Routing.Route.FilePicker ->
        BaseView.baseViewComponent model dispatch [
            FilePickerView.filePickerComponent model dispatch
        ] [
            str "Footer content"
        ]

    | Routing.Route.ActivityLog ->
        BaseView.baseViewComponent model dispatch [
            ActivityLogView.activityLogComponent model
        ] [
            str "Footer content"
        ]

    | Routing.Route.NotFound ->
        BaseView.baseViewComponent model dispatch [
            NotFoundView.notFoundComponent model dispatch
        ] [
            str "Footer content"
        ]

    | Routing.Route.Home ->
        Container.container [][
            div [][ str "This is the Swate web host. For a preview click on the following link." ]
            a [ Href (Routing.Route.toRouteUrl Routing.Route.TermSearch) ] [ str "Termsearch" ]
        ]

    //| _ ->
    //    div [   Style [MinHeight "100vh"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text;]
    //        ] [
    //        Container.container [Container.IsFluid] [
    //            br []
    //            br []
    //            Button.buttonComponent model.SiteStyleState.ColorMode true "make a test db insert xd" (fun _ -> ((sprintf "Me am test %A" (System.Guid.NewGuid())),"1","Me is testerino",System.DateTime.UtcNow,"MEEEMuser") |> TestOntologyInsert |> Request |> Api|> dispatch)
    //            Button.buttonComponent model.SiteStyleState.ColorMode true "idk man=(" (fun _ -> TryExcel |> ExcelInterop |> dispatch)
    //            Button.buttonComponent model.SiteStyleState.ColorMode true "create annoation table" (fun _ -> model.SiteStyleState.IsDarkMode |> CreateAnnotationTable |> ExcelInterop |> dispatch)
    //            Button.buttonComponent model.SiteStyleState.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> Dev |> dispatch)
    //            Button.buttonComponent model.SiteStyleState.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> Dev |> dispatch)

    //            Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
    //                Content.content [
    //                    Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
    //                    Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
    //                ][

    //                ]
    //            ] 
        //    ]
        //]


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
|> Program.withDebugger
#endif
|> Program.run
