namespace SidebarComponents


open Messages
open Feliz
open Feliz.DaisyUI

type Footer =
    [<ReactComponent>]
    static member Main(model: Model.Model, dispatch) =
        React.useEffectOnce (fun () ->
            async {
                let! versionResponse = Api.serviceApi.getAppVersion ()

                PersistentStorage.UpdateAppVersion versionResponse
                |> PersistentStorageMsg
                |> dispatch
            }
            |> Async.StartImmediate
        )

        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-center swt:p-2 swt:text-sm swt:@md/sidebar:text-md"
            prop.children [
                Html.div [
                    prop.children [
                        if
                            model.PersistentStorageState.AppVersion = System.AssemblyVersionInformation.AssemblyMetadata_Version
                        then
                            Html.textf "Version %s " System.AssemblyVersionInformation.AssemblyMetadata_Version
                        else
                            Html.textf "Client %s" System.AssemblyVersionInformation.AssemblyMetadata_Version
                            Html.textf " – Server %s " model.PersistentStorageState.AppVersion
                        Html.a [
                            prop.className "swt:link-primary"
                            prop.href "https://github.com/nfdi4plants/Swate/releases"
                            prop.target.blank
                            prop.children [ Html.small "release" ]
                        ]
                    ]
                ]
            ]
        ]