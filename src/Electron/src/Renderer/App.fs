module Renderer.App

open Feliz
open Fable.Electron.Remoting.Renderer
open Fable.Core
open Swate.Components
open Swate.Electron.Shared
open Browser.Dom

[<ReactComponent>]
let Main () =
    let appState, setAppState = React.useState (AppState.Init)

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

    let navbar =
        Html.div [
            prop.className "swt:size-full swt:flex swt:items-center swt:p-2 swt:gap-2"
            prop.children [
                Html.button [
                    prop.onClick (fun _ ->
                        promise {
                            match! Api.arcVaultApi.openARCInNewWindow () with
                            | Ok _ -> ()
                            | Error exn -> failwith $"{exn.Message}"

                            return ()
                        }
                        |> Promise.start
                    )
                    prop.title "Open ARC"
                    prop.className "swt:btn swt:btn-square swt:btn-xs swt:btn-ghost"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--folder-open-24-filled swt:size-4"
                        ]
                    ]
                ]
            ]
        ]

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Layout.Main(children = children, navbar = navbar)
    )