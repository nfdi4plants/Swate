module Client

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Model
open Messages
open Update
open Shared
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

    match model.PageState.CurrentPage with
    | Routing.Route.BuildingBlock ->
        BaseView.baseViewMainElement model dispatch [
            BuildingBlock.addBuildingBlockComponent model dispatch
        ] [
            BuildingBlock.addBuildingBlockFooterComponent model dispatch
        ]

    | Routing.Route.TermSearch ->
        BaseView.baseViewMainElement model dispatch [
            TermSearch.termSearchComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.Validation ->
        BaseView.baseViewMainElement model dispatch [
            Validation.validationComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.FilePicker ->
        BaseView.baseViewMainElement model dispatch [
            FilePicker.filePickerComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.Protocol ->
        BaseView.baseViewMainElement model dispatch [
            Protocol.fileUploadViewComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.JsonExport ->
        BaseView.baseViewMainElement model dispatch [
            JsonExporter.jsonExporterMainElement model dispatch
        ] [ (*Footer*) ]

    | Routing.Route.TemplateMetadata ->
        BaseView.baseViewMainElement model dispatch [
            TemplateMetadata.newNameMainElement model dispatch
        ] [ (*Footer*) ]

    | Routing.Route.ProtocolSearch ->
        BaseView.baseViewMainElement model dispatch [
            Protocol.Search.protocolSearchViewComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.ActivityLog ->
        BaseView.baseViewMainElement model dispatch [
            ActivityLog.activityLogComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.Settings ->
        BaseView.baseViewMainElement model dispatch [
            SettingsView.settingsViewComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.SettingsXml ->
        BaseView.baseViewMainElement model dispatch [
            SettingsXml.settingsXmlViewComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.Dag ->
        BaseView.baseViewMainElement model dispatch [
            Dag.mainElement model dispatch
        ] [ (*Footer*) ]

    | Routing.Route.Info ->
        BaseView.baseViewMainElement model dispatch [
            InfoView.infoComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.NotFound ->
        BaseView.baseViewMainElement model dispatch [
            NotFoundView.notFoundComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.Home ->
        Container.container [][
            div [][ str "This is the Swate web host. For a preview click on the following link." ]
            a [ Href (Routing.Route.toRouteUrl Routing.Route.TermSearch) ] [ str "Termsearch" ]
        ]

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
