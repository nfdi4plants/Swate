module Index

open System
open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Messages
open Model
open Update

open Swate.Components.ReactHelper

///<summary> This is a basic test case used in Client unit tests </summary>
let sayHello name = $"Hello {name}"

open Feliz
open Feliz.DaisyUI

open Fable.Core.JsInterop

open Browser.Dom

[<ReactComponent>]
let View (model : Model) (dispatch : Msg -> unit) =

    //Set the initial theme
    let useLocalStorage = importMember "@uidotdev/usehooks"
    let (theme, handleSetTheme) = React.useLocalStorage(useLocalStorage, "theme", "light")
    let newTheme = if String.IsNullOrEmpty theme then "light" else theme
    handleSetTheme newTheme
    document.documentElement.setAttribute("data-theme", newTheme)

    React.strictMode [
        Html.div [
            prop.id "ClientView"
            prop.className "flex w-full overflow-auto h-screen"
            prop.children [
                Modals.ModalProvider.Main(model, dispatch)
                match model.PageState.IsHome, model.PersistentStorageState.Host with
                | false, _ ->
                    View.MainPageView.Main(model, dispatch)
                | _, Some Swatehost.Excel ->
                    Html.div [
                        prop.className "flex flex-col w-full h-full"
                        prop.children [
                            SidebarView.SidebarView.Main(model, dispatch)
                        ]
                    ]
                | _, _ ->
                    let isActive = model.SpreadsheetModel.TableViewIsActive() && model.PageState.ShowSideBar
                    Daisy.drawer [
                        prop.className [
                            "drawer-end"
                            if isActive then "drawer-open"
                        ]
                        prop.children [
                            Html.input [
                                prop.id "split-window-drawer"
                                prop.type'.checkbox
                                prop.className "drawer-toggle"
                            ]
                            Daisy.drawerContent [
                                SpreadsheetView.Main (model, dispatch)
                            ]
                            Daisy.drawerSide [
                                SidebarView.SidebarView.Main(model, dispatch)
                            ]
                        ]
                    ]
            ]
        ]
    ]