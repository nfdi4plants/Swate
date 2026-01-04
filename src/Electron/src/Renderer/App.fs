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
    let refreshTrigger, setRefresh = React.useState (0)

    let maxNumberRecentARCs = 5

    React.useEffect ((fun _ ->
        fun _ ->
            promise {
                let! arcs = Api.arcVaultApi.getRecentARCs()
                arcs
                |> Array.iter (fun arc -> console.log($"arc: {arc.name}"))

                if recentARCs <> arcs then
                    setRecentARCs arcs
            }
            |> Promise.start
        ),
        [| box refreshTrigger |]
    )

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

    let selector = Selector.Main(recentARCs, setRecentARCs, maxNumberRecentARCs, actionbar, onClick = setRefresh, refreshCounter = refreshTrigger)

    let navbar = Navbar.Main(selector)

    let ARCPointerExists (path: string) =
        recentARCs
        |> Array.exists (fun arcPointer -> arcPointer.path = path)

    let createNewARCPointers (currentARC: ARCPointer) (recentARCs: ARCPointer []) =
        if recentARCs.Length = maxNumberRecentARCs then
            let tmp = Array.take (maxNumberRecentARCs - 1) recentARCs
            Array.append [| currentARC |] tmp
        else
            Array.append [| currentARC |] recentARCs

    match appState with
    | AppState.Init -> ()
    | AppState.ARC path ->
        if ARCPointerExists path then
            ()
        else
            let newARCPointers =
                let newARCPointer = ARCPointer.create(path, path, true)
                createNewARCPointers newARCPointer recentARCs
            setRecentARCs newARCPointers

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Layout.Main(children = children, navbar = navbar)
    )