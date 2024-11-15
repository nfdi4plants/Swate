namespace SidebarComponents

open Model
open Messages
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


type Tabs =

    static member private NavigationTab (pageLink: Routing.Route) (model:Model) (dispatch:Msg-> unit) =
        let isActive = pageLink.isActive(model.PageState.CurrentPage)
        Daisy.tab [
            if isActive then tab.active
            prop.className "navigation" // this class does not do anything, but disables <a> styling.
            prop.onClick (fun e -> e.preventDefault(); UpdatePageState (Some pageLink) |> dispatch)
            prop.children (pageLink |> Routing.Route.toIcon)
        ]


    static member Main (model:Model) dispatch =
        let isIEBrowser : bool = Browser.Dom.window.document?documentMode
        Daisy.tabs [
            tabs.boxed
            prop.className "w-full"
            prop.children [
                Tabs.NavigationTab Routing.Route.BuildingBlock     model dispatch
                Tabs.NavigationTab Routing.Route.TermSearch        model dispatch
                Tabs.NavigationTab Routing.Route.Protocol          model dispatch
                Tabs.NavigationTab Routing.Route.FilePicker        model dispatch
                Tabs.NavigationTab Routing.Route.DataAnnotator     model dispatch
                Tabs.NavigationTab Routing.Route.JsonExport        model dispatch
            ]
        ]