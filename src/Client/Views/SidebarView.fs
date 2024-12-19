module SidebarView

open Fable.React
open Fable.React.Props
open Model
open Messages
open Browser
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

type SidebarView =
    static member private content (model: Model) (dispatch: Msg -> unit) =
        Html.div [
            prop.className "grow overflow-y-auto"
            prop.children [
                match model.ProtocolState with
                | {WidgetTypes = Routing.WidgetTypes.BuildingBlock } ->
                    BuildingBlock.Core.addBuildingBlockComponent model dispatch

                | {WidgetTypes = Routing.WidgetTypes.TermSearch } ->
                    TermSearch.Main (model, dispatch)

                | {WidgetTypes = Routing.WidgetTypes.FilePicker } ->
                    FilePicker.filePickerComponent model dispatch

                | {WidgetTypes = Routing.WidgetTypes.Protocol } ->
                    Protocol.Templates.Main (model, dispatch)

                | {WidgetTypes = Routing.WidgetTypes.DataAnnotator } ->
                    Pages.DataAnnotator.Main(model, dispatch)

                | {WidgetTypes = Routing.WidgetTypes.JsonExport } ->
                    JsonExporter.Core.FileExporter.Main(model, dispatch)

                | {WidgetTypes = Routing.WidgetTypes.ProtocolSearch } ->
                    Protocol.SearchContainer.Main model dispatch
            ]
        ]

    /// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
    [<ReactComponent>]
    static member Main (model: Model, dispatch: Msg -> unit) =
        Html.div [
            prop.className "min-h-full flex flex-col bg-base-300 min-w-[500px] xl:min-w-[600px] @container/sidebar"
            prop.children [

                SidebarComponents.Navbar.NavbarComponent model dispatch
                Html.div [
                    prop.className "pl-4 pr-4 flex flex-col grow"
                    prop.children [
                        SidebarComponents.Tabs.Main model dispatch

                        SidebarView.content model dispatch

                        SidebarComponents.Footer.Main(model, dispatch)
                    ]
                ]
            ]
        ]