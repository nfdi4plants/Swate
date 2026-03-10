namespace Renderer.Components

open Fable.Core
open Feliz

open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper
open Swate.Components
open Fable.Electron.Remoting.Renderer

module NavbarHelper =

    module Selector =

        /// Unified open: main process decides current window / new window / focus existing.
        let openARC =
            fun _ ->
                promise {
                    let! r = Api.ipcArcVaultApi.openARC (unbox null)

                    match r with
                    | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
                    | Ok _ -> ()
                }
                |> Promise.start

        /// Click on a recent ARC: main process decides open-or-focus.
        let openArcByPath (clickedARC: SelectorTypes.ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.openARCByPath (unbox null) clickedARC.path with
                | Ok _ -> ()
                | Error exn -> console.error (Fable.Core.JS.JSON.stringify exn.Message)
            }
            |> Promise.start

        let rmvRecentArc (pointer: SelectorTypes.ARCPointer) =
            promise {
                match! Api.ipcArcVaultApi.removeRecentARC pointer with
                | Ok _ -> ()
                | Error exn -> console.error (Fable.Core.JS.JSON.stringify exn.Message)
            }
            |> Promise.start


type private Selector =

    [<ReactComponent>]
    static member private Actionbar(setNewArcModalIsOpen, toggleSelector: unit -> unit) =

        let createARC =
            Actionbar.ButtonInfo.create (
                "swt:fluent--document-add-24-regular swt:size-5",
                "Create a new ARC",
                fun _ ->
                    setNewArcModalIsOpen true
                    toggleSelector ()
            )

        let openARCButtonInfo =
            Actionbar.ButtonInfo.create (
                "swt:fluent--folder-open-24-regular swt:size-5",
                "Open an existing ARC",
                fun _ ->
                    NavbarHelper.Selector.openARC ()
                    toggleSelector ()
            )

        // let downloadARC =
        //     Actionbar.ButtonInfo.create (
        //         "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
        //         "Download an existing ARC",
        //         onClick
        //     )
        Actionbar.Main([| createARC; openARCButtonInfo |], 4)

    [<ReactComponent>]
    static member Main() =
        let recentArc, setRecentArc = React.useState ([||]: SelectorTypes.ARCPointer[])
        let isLoading, setIsLoading = React.useState true

        let currentlyOpenArcPath, setCurrentlyOpenArcPath =
            React.useState (None: string option)

        let newArcModalIsOpen, setNewArcModalIsOpen = React.useState false

        let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
            pathChange = setCurrentlyOpenArcPath
            recentARCsUpdate = fun arcs -> setRecentArc arcs
            fileTreeUpdate = ignore
            gitProgressUpdate = ignore
        }

        // Get remote recent ARCs on first load before rendering the selector
        React.useLayoutEffectOnce (fun _ ->
            promise {
                let! arcs = Api.ipcArcVaultApi.getRecentARCs ()
                let! currentlyOpenArcPath = Api.ipcArcVaultApi.getOpenPath (unbox null)
                setCurrentlyOpenArcPath currentlyOpenArcPath
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
                    Api.ipcArcVaultApi.getRecentARCs () |> Promise.map setRecentArc |> Promise.start

        React.Fragment [
            BaseModal.BaseModal(
                newArcModalIsOpen,
                setNewArcModalIsOpen,
                Renderer.Components.InitState.CreateNewArcModalContent(fun () -> setNewArcModalIsOpen false)
            )
            Swate.Components.Selector.Main(
                recentArc,
                NavbarHelper.Selector.openArcByPath,
                rmvRecentArc = NavbarHelper.Selector.rmvRecentArc,
                onOpenChange = onOpen,
                actionbar = Selector.Actionbar(setNewArcModalIsOpen, selectorControlRef.current.toggle),
                isLoading = isLoading,
                controlRef = selectorControlRef,
                ?currentlyOpenArcPath = currentlyOpenArcPath
            )
        ]

type Navbar =

    [<ReactComponent>]
    static member Main() =

        let left = Selector.Main()

        Swate.Components.Navbar.Main(left = left)