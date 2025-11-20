namespace SidebarComponents

open Model
open Messages
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


type Tabs =

    static member private NavigationTab (pageLink: Routing.SidebarPage) (model: Model) (dispatch: Msg -> unit) =
        let isActive = pageLink = model.PageState.SidebarPage

        //Daisy.tab [
        Html.div [
            prop.className [
                "swt:tab"
                if isActive then
                    "swt:tab-active"
            ]
            prop.onClick (fun e ->
                e.preventDefault ()
                PageState.UpdateSidebarPage pageLink |> PageStateMsg |> dispatch
            )
            prop.children (pageLink.AsIcon())
        ]


    static member Main (model: Model) dispatch =
        let isIEBrowser: bool = Browser.Dom.window.document?documentMode

        //Daisy.tabs [
        Html.div [
            prop.className "swt:flex swt:w-full"
            prop.children [
                Html.div [
                    prop.className
                        "swt:tabs swt:tabs-box swt:my-1 swt:w-fit swt:mx-auto swt:*:[--tab-bg:var(--color-secondary)] swt:*:[&.swt\:tab-active]:text-secondary-content"
                    prop.children [
                        Tabs.NavigationTab Routing.SidebarPage.BuildingBlock model dispatch
                        Tabs.NavigationTab Routing.SidebarPage.TermSearch model dispatch
                        Tabs.NavigationTab Routing.SidebarPage.Protocol model dispatch
                        Tabs.NavigationTab Routing.SidebarPage.FilePicker model dispatch
                        // Tabs.NavigationTab Routing.SidebarPage.DataAnnotator model dispatch
                        Tabs.NavigationTab Routing.SidebarPage.JsonImport model dispatch
                        Tabs.NavigationTab Routing.SidebarPage.JsonExport model dispatch
                    ]
                ]
            ]
        ]