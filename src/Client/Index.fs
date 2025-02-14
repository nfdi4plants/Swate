module Index

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

[<ReactComponent>]
let View (model : Model) (dispatch : Msg -> unit) =
    let colorstate, setColorstate = React.useState(LocalStorage.Darkmode.State.init)
    // Make ARCitect always use lighttheme
    let makeColorSchemeLight = fun _ ->
        if model.PersistentStorageState.Host.IsSome && model.PersistentStorageState.Host.Value = Swatehost.ARCitect then
            setColorstate {colorstate with Theme = LocalStorage.Darkmode.DataTheme.Light}
            LocalStorage.Darkmode.DataTheme.SET LocalStorage.Darkmode.DataTheme.Light
        else
            setColorstate (colorstate.Update())
    React.useEffect(makeColorSchemeLight, [|box model.PersistentStorageState.Host|])
    let v = {colorstate with SetTheme = setColorstate}
    React.strictMode [
        React.contextProvider(LocalStorage.Darkmode.themeContext, v,
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
        )
    ]