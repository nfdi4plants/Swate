module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Swate.Components
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared
open Browser.Dom

[<ReactComponent>]
let Main () =

    let btnActive, setBtnActive = React.useState false
    let recentARCs, setRecentARCs = React.useState ([||])
    let appState, setAppState = React.useState (AppState.Init)
    let (fileTree: IPCTypes.FileEntry option), setFileTree = React.useState (None)

    React.useLayoutEffectOnce (fun _ ->
        Api.arcVaultApi.getOpenPath JS.undefined
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p -> AppState.ARC p |> setAppState
            | None -> setAppState AppState.Init
        )
        |> Promise.start
    )

    let createFileTree (parent: IPCTypes.FileEntry option) =
        let rec loop (parent: IPCTypes.FileEntry option) =
            if parent.IsSome then
                match parent.Value.isDirectory with
                | true ->
                    let tmp =
                        parent.Value.children
                        |> Array.map (fun entry ->
                            printfn $"entry: {entry.name}"
                            (loop (Some entry)))
                        |> Array.choose (fun item -> item)
                        |> List.ofArray
                    let result =
                        {
                            FileTree.createFolder parent.Value.name "swt:fluent--folder-24-regular" with
                                Children = Some tmp
                        }
                    (Some result)
                | false -> Some (FileTree.createFile parent.Value.name "swt:fluent--document-24-regular")
            else
                None

        let fileItem = loop parent
        if fileItem.IsSome then
            FileExplorer.FileExplorer([fileItem.Value])
        else
            Html.div []

    let fileExplorer =
        React.useMemo (
            (fun _ ->
                createFileTree(fileTree)
            ), [| fileTree |]
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
                console.log ("[Swate] CHANGE RECENTARCS!")
                setRecentARCs arcs
        fileTreeUpdate =
            fun fileExplorer ->
                console.log ("[Swate] FILETREE UPDATE!")
                setFileTree fileExplorer
    }

    let openNewWindow =
        fun _ ->
            promise {
                match! Api.arcVaultApi.openARCInNewWindow () with
                | Ok _ -> ()
                | Error exn -> failwith $"{exn.Message}"

                return ()
            }
            |> Promise.start

    let openCurrentWindow =
        fun _ ->
            promise {
                let! r = Api.arcVaultApi.openARC Fable.Core.JS.undefined

                match r with
                | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
                | Ok _ -> ()
            }
            |> Promise.start

    let openARC =
        if appState.IsInit then
            openCurrentWindow
        else
            openNewWindow

    let onARCClick (clickedARC: ARCPointer) =
        promise {
            match! Api.arcVaultApi.focusExistingARCWindow clickedARC.path with
            | Ok _ -> ()
            | Error exn -> failwith $"{exn.Message}"

            return ()
        }
        |> Promise.start

    let actionbar onClick =
        let createARC =
            ButtonInfo.create (
                "swt:fluent--document-add-24-regular swt:size-5",
                "Create a new ARC",
                onClick
            )

        let openARCButtonInfo =
            ButtonInfo.create (
                "swt:fluent--folder-open-24-regular swt:size-5",
                "Open an existing ARC",
                fun _ ->
                    onClick()
                    openARC()
            )

        let downloadARC =
            ButtonInfo.create (
                "swt:fluent--cloud-arrow-down-24-regular swt:size-5",
                "Download an existing ARC",
                onClick
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

    let recentARCElements =
        recentARCs
        |> Array.map (fun arcPointer -> Selector.SelectorItem(arcPointer, onARCClick))

    let selector = Selector.Main(recentARCElements, actionbar, onOpenSelector = onOpenSelector)

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

    let navbar = Navbar.Main(selector)

    context.AppStateCtx.AppStateCtx.Provider(
        {
            state = appState
            setState = setAppState
        },
        Layout.Main(
            children = children,
            navbar = navbar,
            ?leftSidebar =
                if fileTree.IsSome then
                    Some (
                        Html.div [
                            prop.className "swt:p-4"
                            prop.children [
                                Html.h2 [
                                    prop.text "ARC-Tree"
                                ]
                                fileExplorer
                            ]
                        ]
                    )
                else None
                ,
            leftActions =
                React.Fragment [
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--home-24-regular size-5",
                        tooltip = "Home",
                        isActive = btnActive,
                        onClick = fun () -> setBtnActive (not btnActive)
                    )
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--settings-24-regular size-5",
                        tooltip = "Settings",
                        onClick = fun () -> Browser.Dom.window.alert "Settings clicked"
                    )
                    Layout.LayoutBtn(
                        iconClassName = "swt:fluent--info-24-regular size-5",
                        tooltip = "Info",
                        onClick = fun () -> Browser.Dom.window.alert "Info clicked"
                    )
                ]
        )
    )