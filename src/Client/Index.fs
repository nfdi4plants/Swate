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

let private split_container model dispatch =
    let mainWindow = Seq.singleton <| MainWindowView.Main (model, dispatch)
    let sideWindow = Seq.singleton <| SidebarView.SidebarView.Main(model, dispatch)
    SplitWindowView.Main
        mainWindow
        sideWindow
        model
        dispatch

[<ReactComponent>]
let View (model : Model) (dispatch : Msg -> unit) =
    let (colorstate, setColorstate) = React.useState(LocalStorage.Darkmode.State.init)
    // Make ARCitect always use lighttheme
    let makeColorSchemeLight = fun _ ->
        if model.PersistentStorageState.Host.IsSome && model.PersistentStorageState.Host.Value = Swatehost.ARCitect then
            setColorstate {colorstate with Theme = LocalStorage.Darkmode.DataTheme.Light}
            LocalStorage.Darkmode.DataTheme.SET LocalStorage.Darkmode.DataTheme.Light
        else
            setColorstate (colorstate.Update())
    React.useEffect(makeColorSchemeLight, [|box model.PersistentStorageState.Host|])
    let v = {colorstate with SetTheme = setColorstate}
    React.contextProvider(LocalStorage.Darkmode.themeContext, v,
        Html.div [
            prop.id "ClientView"
            prop.className "flex w-full h-full overflow-auto"
            prop.children [
                match model.PersistentStorageState.Host with
                | Some Swatehost.Excel ->
                    SidebarView.SidebarView.Main(model, dispatch)
                | _ ->
                    split_container model dispatch
            ]
        ]
    )