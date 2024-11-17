namespace SidebarComponents

open Model
open Messages
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


type Tabs =

    static member private NavigationTab (pageLink: Routing.SidebarPage) (model:Model) (dispatch:Msg-> unit) =
        let isActive = pageLink = model.PageState.SidebarPage
        Daisy.tab [
            if isActive then tab.active
            prop.className "navigation" // this class does not do anything, but disables <a> styling.
            prop.onClick (fun e -> e.preventDefault(); UpdateModel { model with Model.PageState.SidebarPage = pageLink } |> dispatch)
            prop.children (pageLink.AsIcon())
        ]


    static member Main (model:Model) dispatch =
        let isIEBrowser : bool = Browser.Dom.window.document?documentMode
        Daisy.tabs [
            tabs.boxed
            prop.className "w-full"
            prop.children [
                Tabs.NavigationTab Routing.SidebarPage.BuildingBlock     model dispatch
                Tabs.NavigationTab Routing.SidebarPage.TermSearch        model dispatch
                Tabs.NavigationTab Routing.SidebarPage.Protocol          model dispatch
                Tabs.NavigationTab Routing.SidebarPage.FilePicker        model dispatch
                Tabs.NavigationTab Routing.SidebarPage.DataAnnotator     model dispatch
                Tabs.NavigationTab Routing.SidebarPage.JsonExport        model dispatch
            ]
        ]