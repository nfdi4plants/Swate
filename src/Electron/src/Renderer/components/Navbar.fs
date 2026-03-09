namespace Renderer.Components

open Fable.Core
open Feliz

open ARCtrl
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Components
open Fable.Electron.Remoting.Renderer

module NavbarHelper =

    module Selector =

        ///Selector module
        let openNewWindow =
            fun _ ->
                promise {
                    match! Api.arcVaultApi.openARCInNewWindow () with
                    | Ok _ -> ()
                    | Error exn -> failwith $"{exn.Message}"

                    return ()
                }
                |> Promise.start

        ///Selector module
        let openCurrentWindow =
            fun _ ->
                promise {
                    let! r = Api.arcVaultApi.openARC (unbox null)

                    match r with
                    | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
                    | Ok _ -> ()
                }
                |> Promise.start

        ///Selector module
        let openARC (appState: AppState) =
            if appState.IsInit then openCurrentWindow else openNewWindow

        ///Selector module
        let onARCClick (clickedARC: SelectorTypes.ARCPointer) =
            promise {
                match! Api.arcVaultApi.focusExistingARCWindow clickedARC.path with
                | Ok _ -> ()
                | Error exn -> failwith $"{exn.Message}"

                return ()
            }
            |> Promise.start


type private Selector =

    [<ReactComponent>]
    static member private Actionbar(toggleSelector: unit -> unit) =

        let appStateCtx = React.useContext Renderer.Context.AppStateCtx.AppStateCtx

        // let createARC =
        //     Actionbar.ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", onClick)

        let openARCButtonInfo =
            Actionbar.ButtonInfo.create (
                "swt:fluent--folder-open-24-regular swt:size-5",
                "Open an existing ARC",
                fun _ ->
                    NavbarHelper.Selector.openARC appStateCtx.state ()
                    toggleSelector ()
            )

        // let downloadARC =
        //     Actionbar.ButtonInfo.create (
        //         "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
        //         "Download an existing ARC",
        //         onClick
        //     )

        Actionbar.Main([| openARCButtonInfo |], 4)

    [<ReactComponent>]
    static member Main() =
        let recentArc, setRecentArc = React.useState ([||]: SelectorTypes.ARCPointer[])
        let isLoading, setIsLoading = React.useState true

        let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
            pathChange = ignore
            recentARCsUpdate =
                fun arcs ->
                    console.log ("[Swate] CHANGE RECENTARCS!")
                    setRecentArc arcs
            fileTreeUpdate = ignore
            gitProgressUpdate = ignore
        }

        // Get remote recent ARCs on first load before rendering the selector
        React.useLayoutEffectOnce (fun _ ->
            promise {
                let! arcs = Api.arcVaultApi.getRecentARCs ()

                setRecentArc arcs
                setIsLoading false
            }
            |> Promise.start
        )

        React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

        let selectorControlRef =
            React.useRef ({ toggle = ignore }: SelectorTypes.SelectorRef)

        let onOpen =
            fun (b: bool) ->
                if b then
                    Api.arcVaultApi.getRecentARCs () |> Promise.map setRecentArc |> Promise.start

        Swate.Components.Selector.Main(
            recentArc,
            NavbarHelper.Selector.onARCClick,
            onOpenChange = onOpen,
            actionbar = Selector.Actionbar(selectorControlRef.current.toggle),
            isLoading = isLoading,
            controlRef = selectorControlRef
        )


type Navbar =


    [<ReactComponent>]
    static member Main() =

        let left = Selector.Main()

        Swate.Components.Navbar.Main(left = left)