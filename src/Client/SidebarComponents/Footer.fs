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
            prop.className "flex items-center justify-center p-2 text-sm @md/sidebar:text-md"
            prop.children [
                Html.div [
                    Html.text "Swate Release Version "
                    Html.a [prop.className "link-primary"; prop.href "https://github.com/nfdi4plants/Swate/releases"; prop.target.blank; prop.text model.PersistentStorageState.AppVersion]
                    // Html.textf ". Host: %O." model.PersistentStorageState.Host
                ]
            ]
        ]