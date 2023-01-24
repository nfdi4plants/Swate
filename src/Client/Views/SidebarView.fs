module SidebarView

open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Model
open Messages

let sidebarView (model:Model) (dispatch: Msg -> unit) =
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
            Protocol.Core.fileUploadViewComponent model dispatch
        ] [
            //Text.p [] [str ""]
        ]

    | Routing.Route.JsonExport ->
        BaseView.baseViewMainElement model dispatch [
            JsonExporter.Core.jsonExporterMainElement model dispatch
        ] [ (*Footer*) ]

    | Routing.Route.TemplateMetadata ->
        BaseView.baseViewMainElement model dispatch [
            TemplateMetadata.Core.newNameMainElement model dispatch
        ] [ (*Footer*) ]

    | Routing.Route.ProtocolSearch ->
        BaseView.baseViewMainElement model dispatch [
            Protocol.Search.protocolSearchView model dispatch
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
            Dag.Core.mainElement model dispatch
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
        Container.container [] [
            div [] [ str "This is the Swate web host. For a preview click on the following link." ]
            a [ Href (Routing.Route.toRouteUrl Routing.Route.TermSearch) ] [ str "Termsearch" ]
        ]