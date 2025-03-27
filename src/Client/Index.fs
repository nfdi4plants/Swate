module Index

open System
open Elmish.Navigation
open Elmish.UrlParser
open Elmish
open Messages
open Model
open Update

///<summary> This is a basic test case used in Client unit tests </summary>
let sayHello name = $"Hello {name}"

open Feliz
open Feliz.DaisyUI

open Fable.Core.JsInterop

[<ReactComponent>]
let View (model : Model) (dispatch : Msg -> unit) =

    let themeChangeModule: obj = importDefault "theme-change"

    let themeChange =
        let themeChange: obj = themeChangeModule?themeChange
        themeChange :?> (bool -> unit)

    React.useEffectOnce (fun () ->
        themeChange false // Reattach event listeners manually
        let storedTheme = Browser.WebStorage.localStorage.getItem("theme")
        if (String.IsNullOrEmpty storedTheme) then
            Browser.Dom.document.documentElement.setAttribute("data-theme", "light")
        None
    )

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