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
                match model.PageState with
                | {
                      SidebarPage = Routing.SidebarPage.BuildingBlock
                  } -> BuildingBlock.Core.addBuildingBlockComponent model dispatch

                | {
                      SidebarPage = Routing.SidebarPage.TermSearch
                  } -> TermSearch.Main(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.FilePicker
                  } -> Pages.FilePicker.Sidebar(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.Protocol
                  } -> Protocol.Templates.Main(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.DataAnnotator
                  } -> Pages.DataAnnotator.Sidebar(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.JsonExport
                  } -> JsonExporter.Core.FileExporter.Main(model, dispatch)
            ]
        ]

    /// The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        Html.div [
            prop.className
                "min-h-full flex flex-col bg-base-300 min-w-[500px] xl:min-w-[600px] overflow-y-auto h-40 [scrollbar-gutter:stable] @container/sidebar"
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