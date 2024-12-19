namespace SidebarComponents

open Model
open Messages
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


type Tabs =

    static member private NavigationTab (pageLink: Routing.WidgetTypes) (model:Model) (dispatch:Msg-> unit) =
        let isActive = pageLink = model.ProtocolState.WidgetTypes
        Daisy.tab [
            if isActive then tab.active
            prop.className "navigation" // this class does not do anything, but disables <a> styling.
            prop.onClick (fun e -> e.preventDefault(); UpdateModel { model with Model.ProtocolState.WidgetTypes = pageLink } |> dispatch)
            prop.children (pageLink.AsIcon())
        ]


    static member Main (model:Model) dispatch =
        let isIEBrowser : bool = Browser.Dom.window.document?documentMode
        Daisy.tabs [
            tabs.boxed
            prop.className "w-full"
            prop.children [
                Tabs.NavigationTab Routing.WidgetTypes.BuildingBlock     model dispatch
                Tabs.NavigationTab Routing.WidgetTypes.TermSearch        model dispatch
                Tabs.NavigationTab Routing.WidgetTypes.Protocol          model dispatch
                Tabs.NavigationTab Routing.WidgetTypes.FilePicker        model dispatch
                Tabs.NavigationTab Routing.WidgetTypes.DataAnnotator     model dispatch
                Tabs.NavigationTab Routing.WidgetTypes.JsonExport        model dispatch
            ]
        ]