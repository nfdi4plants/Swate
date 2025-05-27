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
            prop.className "swt:grow swt:overflow-y-auto"
            prop.children [
                match model.PageState with
                | {
                      SidebarPage = Routing.SidebarPage.BuildingBlock
                  } -> BuildingBlock.Core.addBuildingBlockComponent model dispatch

                | {
                      SidebarPage = Routing.SidebarPage.TermSearch
                  } -> TermSearch.Main(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.Protocol
                  } -> Protocol.Templates.Main(model, dispatch)

                | {
                      SidebarPage = Routing.SidebarPage.FilePicker
                  } -> Pages.FilePicker.Sidebar(model, dispatch)

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
                "swt:min-h-full swt:flex swt:flex-col swt:bg-base-300 swt:min-w-[500px] swt:xl:min-w-[600px] swt:h-40 swt:overflow-y-auto swt:[scrollbar-gutter:stable] swt:@container/sidebar"
            prop.children [

                SidebarComponents.Navbar.NavbarComponent model dispatch
                Html.div [
                    prop.className "swt:pl-4 swt:pr-4 swt:flex swt:flex-col swt:grow"
                    prop.children [
                        SidebarComponents.Tabs.Main model dispatch

                        SidebarView.content model dispatch

                        SidebarComponents.Footer.Main(model, dispatch)
                    ]
                ]
            ]
        ]