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

    let enforceLightTheme =
        match model.PersistentStorageState.Host with
        | Some Swatehost.ARCitect -> Some Swate.Components.Types.Theme.Sunrise
        | _ -> None

    React.strictMode [
        Swate.Components.ThemeProvider.ThemeProvider(
            ReactContext.ThemeCtx,
            Swate.Components.TermSearchConfigProvider.TIBQueryProvider(
                Swate.Components.AnnotationTableContextProvider.AnnotationTableContextProvider(
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
                                let isActive = model.PageState.ShowSideBar

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
                                            prop.className "swt:drawer-toggle"
                                            prop.type'.checkbox
                                            prop.isChecked isActive
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
                )
            ),
            ?enforceTheme = enforceLightTheme
        )
    ]