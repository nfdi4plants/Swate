namespace SidebarComponents


open Messages
open Feliz
open Feliz.DaisyUI

type Footer =
    [<ReactComponent>]
    static member Main (model: Model.Model, dispatch) =
        React.useEffectOnce(fun () ->
            async {
                let! versionResponse = Api.serviceApi.getAppVersion()
                PersistentStorage.UpdateAppVersion versionResponse |> PersistentStorageMsg |> dispatch
            }
            |> Async.StartImmediate
        )
        Html.div [
            prop.className "flex items-center justify-center p-2"
            prop.children [
                Html.div [
                    Html.text "Swate Release Version "
                    Html.a [prop.href "https://github.com/nfdi4plants/Swate/releases"; prop.target.blank; prop.text model.PersistentStorageState.AppVersion]
                    Html.text " Host "
                    Html.a [prop.style [style.cursor.defaultCursor]; prop.text (sprintf "%O" model.PersistentStorageState.Host)]
                ]
            ]
        ]