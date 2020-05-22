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
let init (pageOpt: Routing.Page option) : Model * Cmd<Msg> =
    let loadCountCmd =
        Cmd.batch [
            Cmd.OfPromise.either
                initializeAddIn
                ()
                (fun x -> (x.host.ToString(),x.platform.ToString()) |> Initialized |> ExcelInterop )
                (fun x -> x |> GenericError |> Dev)
            Cmd.ofMsg (FetchAllOntologies |> Request |> Api)
        ]
    initializeModel pageOpt , loadCountCmd

let button (colorMode: ExcelColors.ColorMode) (isActive:bool) txt onClick =

    Button.button [
        Button.IsFullWidth
        Button.IsActive isActive
        Button.Props [Style [BackgroundColor colorMode.BodyForeground; Color colorMode.Text]]
        Button.OnClick onClick
        ][
        str txt
    ]



let renderActivityLog (model:Model) =
    Table.table [
        Table.IsFullWidth
        Table.Props [ExcelColors.colorBackground model.SiteStyleState.ColorMode]
    ] (
        model.DevState.Log
        |> List.map LogItem.toTableRow
    )
    

let view (model : Model) (dispatch : Msg -> unit) =
    match model.PageState.CurrentPage with
    | Routing.Page.FilePicker ->
        BaseView.baseViewComponent model dispatch [
            FilePickerView.filePickerComponent model dispatch
        ] [
            str "Footer content"
        ]

    | Routing.Page.TermSearch ->
        BaseView.baseViewComponent model dispatch [
            TermSearchView.termSearchComponent model dispatch
        ] [
            str "Footer content"
        ]
        
    | Routing.Page.NotFound ->
        BaseView.baseViewComponent model dispatch [
            NotFoundView.notFoundComponent model dispatch
        ] [
            str "Footer content"
        ]



    | _ ->
        div [   Style [MinHeight "100vh"; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text;]
            ] [
            Container.container [Container.IsFluid] [
                br []
                br []
                button model.SiteStyleState.ColorMode true "make a test db insert xd" (fun _ -> ((sprintf "Me am test %A" (System.Guid.NewGuid())),"1","Me is testerino",System.DateTime.UtcNow,"MEEEMuser") |> TestOntologyInsert |> Request |> Api|> dispatch)
                button model.SiteStyleState.ColorMode true "idk man=(" (fun _ -> TryExcel |> ExcelInterop |> dispatch)
                button model.SiteStyleState.ColorMode true "create annoation table" (fun _ -> model.SiteStyleState.IsDarkMode |> CreateAnnotationTable |> ExcelInterop |> dispatch)
                button model.SiteStyleState.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> Dev |> dispatch)
                button model.SiteStyleState.ColorMode true "Log table metadata" (fun _ -> LogTableMetadata |> Dev |> dispatch)

                Footer.footer [ Props [ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
                    Content.content [
                        Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Left)]
                        Content.Props [ExcelColors.colorControl model.SiteStyleState.ColorMode] 
                    ][
                        renderActivityLog model
                    ]
                ] 
            ]
        ]
#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init Update.update view
|> Program.toNavigable (parseHash Routing.pageParser) Update.urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
