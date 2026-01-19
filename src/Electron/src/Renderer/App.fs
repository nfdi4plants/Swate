module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Swate.Components
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Browser.Dom

[<ReactComponent>]
let Main () =

    let btnActive, setBtnActive = React.useState false
    let recentARCs, setRecentARCs = React.useState ([||])
    let appState, setAppState = React.useState (AppState.Init)
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())
    let fileExplorer, setFileExplorer = React.useState (None)
    let (assay: ARCtrl.ArcAssay option), setAssay = React.useState (None)

    React.useLayoutEffectOnce (fun _ ->
        Api.arcVaultApi.getOpenPath JS.undefined
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p -> AppState.ARC p |> setAppState
            | None -> setAppState AppState.Init
        )
        |> Promise.start
    )

    React.useEffect ((fun _ ->
        if assay.IsSome then
            Swate.Components.console.log($"getFileName assay: {assay.Value.Identifier}")
    ), [| box assay |])

    let createFileTree (parent: FileItemDTO option) =
        let rec loop (parent: FileItemDTO option) =
            if parent.IsSome then
                match parent.Value.isDirectory with
                | true ->
                    let tmp =
                        parent.Value.children.Values
                        |> Array.ofSeq
                        |> Array.map (fun entry -> loop (Some entry))
                        |> Seq.choose (fun item -> item)
                        |> List.ofSeq
                    let result =
                        {
                            FileTree.createFolder parent.Value.name "swt:fluent--folder-24-regular" with
                                Children = Some tmp
                        }
                    Some result
                | false -> Some (FileTree.createFile parent.Value.name "swt:fluent--document-24-regular")
            else
                None

        let fileItem = loop parent

        let getFileName (item: FileItem) =
            promise {
                let! result = Api.arcVaultApi.openAssay item.Name

                match result with
                | Ok assay ->
                    
                    setAssay (Some assay)
                    return ()
                | Error exn ->
                    setAssay (None)
                    failwith $"{exn.Message}"
            }
            |> Promise.start

        if fileItem.IsSome then
            Some (FileExplorer.FileExplorer([fileItem.Value], getFileName))
        else
            None

    let insertEntry (root: FileItemDTO) (rootPath: string) (entry: FileEntry) =
        let parts =
            entry.path.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

        let splittedRootPath =
            rootPath.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

        let rec loop (node: FileItemDTO) index =
            let part = parts[index]
            let isLast = index = parts.Length - 1

            let child =
                match node.children.TryGetValue(part) with
                | true, existing -> existing
                | false, _ ->
                    let newNode =
                        FileItemDTO.create(
                            part,
                            entry.isDirectory,
                            System.Collections.Generic.Dictionary()
                        )
                    node.children.Add(part, newNode)
                    newNode

            if not isLast then
                loop child (index + 1)

        loop root splittedRootPath.Length

    let getFileTree (fileEntries: FileEntry []) =

        let rootPath =
            fileEntries
            |> Array.map (fun fileEntry -> fileEntry.path)
            |> Array.map (fun path -> path.Split("/"))
            |> Array.sortByDescending (fun path -> path.Length)
            |> Array.last
            |> String.concat "/"

        let adaptedFileEntires = fileEntries |> Array.filter (fun fileEntry -> fileEntry.path <> rootPath)

        let rootElement =
            let tmp =
                fileEntries
                |> Array.find(fun fileEntry -> fileEntry.path = rootPath)
            FileItemDTO.create(tmp.name, tmp.isDirectory, System.Collections.Generic.Dictionary())

        adaptedFileEntires
        |> Array.iter (fun fileEntry -> insertEntry rootElement rootPath fileEntry)

        rootElement

    React.useEffect (
        (fun _ ->
            let fileEntries =
                fileTree.Values
                |> Array.ofSeq
            let fileTree =
                if fileEntries.Length > 0 then
                    Some (getFileTree fileEntries)
                else
                    None
            createFileTree fileTree
            |> setFileExplorer
            |> ignore
        ), [| box fileTree |]
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
                console.log ("[Swate] FILETREE Create!")
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
                if fileExplorer.IsSome then
                    Some (
                        Html.div [
                            prop.className "swt:p-4"
                            prop.children [
                                Html.h2 [
                                    prop.text "ARC-Tree"
                                ]
                                fileExplorer.Value
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