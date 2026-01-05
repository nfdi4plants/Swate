module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared

open Renderer.components.InitState

[<ReactComponent>]
let Main () =

    let recentARCs, setRecentARCs = React.useState ([||])
    let appState, setAppState = React.useState (AppState.Init)

    let maxNumberRecentARCs = 5

    React.useLayoutEffectOnce (fun _ ->
        Api.arcVaultApi.getOpenPath JS.undefined
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p ->
                console.log $"[Swate] Found open path: {p}"
                AppState.ARC p |> setAppState
            | None -> setAppState AppState.Init
        )
        |> Promise.start
    )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")
                match pathOption with
                | Some p -> AppState.ARC p |> setAppState
                | None -> setAppState AppState.Init
        recentARCsUpdate =
            fun arcs ->
                console.log ("[Swate] CHANGE RECENTARCS !")
                setRecentARCs arcs
    }

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    let children =
        React.useMemo (
            (fun _ ->
                match appState with
                | AppState.Init ->
                    Html.div [
                        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                        prop.children [ components.InitState.InitState() ]
                    ]
                | AppState.ARC path ->
                    Html.div [
                        prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                        prop.children (
                            Html.h1 [
                                prop.text path
                                prop.className
                                    "
                                    swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text
                                    swt:bg-linear-to-r swt:from-primary swt:to-secondary
                                "
                            ]
                        )
                    ]
            ),
            [| appState |]
        )

    let openFunction =
        fun _ ->
            promise {
                match! Api.arcVaultApi.openARCInNewWindow () with
                | Ok _ -> ()
                | Error exn -> failwith $"{exn.Message}"

                return ()
            }
            |> Promise.start

    let openARC =
        if appState.IsARC then
            openFunction
        else
            InitStateHelper.openARC >> Promise.start

    let onARCClick (clickedARC: ARCPointer) =
        promise {
            match! Api.arcVaultApi.focusExistingARCWindow clickedARC.path with
            | Ok _ -> ()
            | Error exn -> failwith $"{exn.Message}"

            return ()
        }
        |> Promise.start

    let actionbar =
        let createARC =
            ButtonInfo.create ("swt:fluent--document-add-24-regular swt:size-5", "Create a new ARC", fun _ -> ())

        let openARCButtonInfo =
            ButtonInfo.create (
                "swt:fluent--folder-open-24-regular swt:size-5",
                "Open an existing ARC",
                openARC
            )

        let downloadARC =
            ButtonInfo.create (
                "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
                "Download an existing ARC",
                fun _ -> ()
            )

        Actionbar.Main([| createARC; openARCButtonInfo; downloadARC |], 3)

    let onOpenSelector () =
        promise {
            let! newARCs = Api.arcVaultApi.getRecentARCs()

            match appState with
            | AppState.Init -> ()
            | AppState.ARC path ->
                newARCs
                |> Array.map (fun arc ->
                    ARCPointer.create(arc.name, arc.path, arc.path = path))
                |> setRecentARCs
        }
        |> Promise.start

    let selector = Selector.Main(recentARCs, onARCClick, maxNumberRecentARCs, actionbar, onOpenSelector = onOpenSelector)

    let navbar = Navbar.Main(selector)

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Layout.Main(children = children, navbar = navbar)
    )