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
let View (model: Model) (dispatch: Msg -> unit) =

    //Set the initial theme
    let (theme, handleSetTheme) = React.useLocalStorage ("theme", "light")
    let newTheme = if String.IsNullOrEmpty theme then "light" else theme
    handleSetTheme newTheme
    document.documentElement.setAttribute ("data-theme", newTheme)

    React.strictMode [
        Html.div [
            prop.id "ClientView"
            prop.className "swt:flex swt:w-full swt:overflow-auto swt:h-screen"
            prop.children [
                Modals.ModalProvider.Main(model, dispatch)
                match model.PageState.IsHome, model.PersistentStorageState.Host with
                | false, _ -> View.MainPageView.Main(model, dispatch)
                | _, Some Swatehost.Excel ->
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:w-full swt:h-full"
                        prop.children [ SidebarView.SidebarView.Main(model, dispatch) ]
                    ]
                | _, _ ->
                    let isActive =
                        model.SpreadsheetModel.TableViewIsActive() && model.PageState.ShowSideBar

                    //Daisy.drawer [
                    Html.div [
                        prop.className [
                            "swt:drawer swt:drawer-end"
                            if isActive then
                                "swt:drawer-open"
                        ]
                        prop.children [
                            Html.input [
                                prop.id "split-window-drawer"
                                prop.type'.checkbox
                                prop.className "swt:drawer-toggle"
                            ]
                            //Daisy.drawerContent [ SpreadsheetView.Main(model, dispatch) ]
                            Html.div [
                                prop.className "swt:drawer-content"
                                prop.children [ SpreadsheetView.Main(model, dispatch) ]
                            ]
                            //Daisy.drawerSide [ SidebarView.SidebarView.Main(model, dispatch) ]
                            Html.div [
                                prop.className "swt:drawer-side"
                                prop.children [ SidebarView.SidebarView.Main(model, dispatch) ]
                            ]
                        ]
                    ]
            ]
        ]
    ]