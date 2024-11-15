module SidebarView

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Browser
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

type SidebarView =
    static member private content (model:Model) (dispatch: Msg -> unit) =
        Html.div [
            prop.className "grow overflow-y-auto"
            prop.children [
                match model.PageState.CurrentPage with
                | Routing.Route.BuildingBlock | Routing.Route.Home _ ->
                    BuildingBlock.Core.addBuildingBlockComponent model dispatch

                | Routing.Route.TermSearch ->
                    TermSearch.Main (model, dispatch)

                | Routing.Route.FilePicker ->
                    FilePicker.filePickerComponent model dispatch

                | Routing.Route.Protocol ->
                    Protocol.Templates.Main (model, dispatch)

                | Routing.Route.DataAnnotator ->
                    Pages.DataAnnotator.Main(model, dispatch)

                | Routing.Route.JsonExport ->
                    JsonExporter.Core.FileExporter.Main(model, dispatch)

                | Routing.Route.ProtocolSearch ->
                    Protocol.SearchContainer.Main model dispatch

                | Routing.Route.ActivityLog ->
                    ActivityLog.activityLogComponent model dispatch

                | Routing.Route.Settings ->
                    SettingsView.settingsViewComponent model dispatch

                | Routing.Route.Info ->
                    Pages.Info.Main

                | Routing.Route.PrivacyPolicy ->
                    Pages.PrivacyPolicy.Main()

                | Routing.Route.NotFound ->
                    NotFoundView.notFoundComponent model dispatch
            ]
        ]

    /// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
    [<ReactComponent>]
    static member Main (model: Model, dispatch: Msg -> unit) =
        Html.div [
            prop.className "min-h-full flex flex-col bg-base-300 min-w-[400px]"
            prop.children [

                SidebarComponents.Navbar.NavbarComponent model dispatch
                Html.div [
                    prop.className "pl-4 pr-4 flex flex-col grow w-[500px] xl:w-[600px]"
                    prop.children [
                        SidebarComponents.Tabs.Main model dispatch

                        match model.PersistentStorageState.Host, model.ExcelState.HasAnnotationTable with
                        | Some Swatehost.Excel, false ->
                            SidebarComponents.AnnotationTableMissingWarning.annotationTableMissingWarningComponent model dispatch
                            Html.none
                        | _ ->
                            Html.none

                        SidebarView.content model dispatch
                        SidebarComponents.Footer.Main(model, dispatch)
                    ]
                ]
            ]
        ]