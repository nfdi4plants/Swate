module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer

open Swate.Components
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Swate.Components.FileExplorerTypes

open Browser.Dom

open ARCtrl
open ARCtrl.Json

open components.MainElement


[<ReactComponent>]
let CreateARCPreview (arcFile: ArcFiles) (setArcFileState: ArcFiles option -> unit) (activeView: PreviewActiveView) (setActiveView: PreviewActiveView -> unit) didSelectFile setDidSelectFile =

    let setArcFile arcFile =
        console.log("setArcFile")
        setArcFileState (Some arcFile)

    React.useEffect (
        (fun () ->
            if didSelectFile then
                setArcFile arcFile
                let tables = arcFile.Tables()
                setDidSelectFile false
                if tables.Count > 0 then
                    setActiveView (PreviewActiveView.Table 0)
                else
                    setActiveView PreviewActiveView.Metadata
        ),
        [| box arcFile |]
    )

    Html.div [
        prop.className "swt:flex swt:flex-col swt:h-full"
        prop.children [|
            Html.div [
                prop.className "swt:flex-1 swt:overflow-auto"
                prop.children [
                    CreateTableView activeView arcFile setArcFile
                ]
            ]
            CreateARCitectFooter arcFile activeView setActiveView
        |]
    ]

let ParseArcFileFromJson (fileType: ArcFileType) (json: string) : ArcFiles option =
    try
        match fileType with
        | ArcFileType.Investigation ->
            let inv = ArcInvestigation.fromJsonString json
            Some(ArcFiles.Investigation inv)
        | ArcFileType.Study ->
            let study = ArcStudy.fromJsonString json
            Some(ArcFiles.Study(study, []))
        | ArcFileType.Assay ->
            let assay = ArcAssay.fromJsonString json
            Some(ArcFiles.Assay assay)
        | ArcFileType.Run ->
            let run = ArcRun.fromJsonString json
            Some(ArcFiles.Run run)
        | ArcFileType.Workflow ->
            let workflow = ArcWorkflow.fromJsonString json
            Some(ArcFiles.Workflow workflow)
        | ArcFileType.DataMap ->
            let datamap = DataMap.fromJsonString json
            Some(ArcFiles.DataMap(None, datamap))
    with e ->
        console.error ("Failed to parse ArcFile JSON: " + e.Message)
        None

[<ReactComponent>]
let Main () =

    let widgets, setWidgets = React.useState ([])
    let recentARCs, setRecentARCs = React.useState ([||])
    let fileExplorer, setFileExplorer = React.useState (None)
    let didSelectFile, setDidSelectFile = React.useState false
    let appState, setAppState = React.useState (AppState.Init)
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let (previewError: string option), setPreviewError = React.useState (None)
    let (previewData: PreviewData option), setPreviewData = React.useState (None)
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())

    React.useLayoutEffectOnce (fun _ ->
        Api.arcVaultApi.getOpenPath JS.undefined
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p -> AppState.ARC p |> setAppState
            | None -> setAppState AppState.Init
        )
        |> Promise.start
    )

    let addWidget (widget: MainComponents.Widget) =
        let add (widget) widgets =
            widget :: widgets |> List.rev |> setWidgets

        if widgets |> List.contains widget then
            List.filter (fun w -> w <> widget) widgets
            |> fun filteredWidgets -> add widget filteredWidgets
        else
            add widget widgets

    let rmvWidget (widget: MainComponents.Widget) =
        widgets |> List.except [ widget ] |> setWidgets

    let bringWidgetToFront (widget: MainComponents.Widget) =
        let newList =
            widgets |> List.except [ widget ] |> (fun x -> widget :: x |> List.rev)

        setWidgets newList

    let WidgetOrderContainer bringWidgetToFront (widget) =
        Html.div [ prop.onClick bringWidgetToFront; prop.children [ widget ] ]

    //let displayWidget (widget: MainComponents.Widget) =
    //    let rmv (widget: MainComponents.Widget) = fun _ -> rmvWidget widget
    //    let bringWidgetToFront = fun _ -> bringWidgetToFront widget

    //    match widget with
    //    | MainComponents.Widget._BuildingBlock -> MainComponents.Widget.BuildingBlock(model, dispatch, rmv widget)
    //    | MainComponents.Widget._Template -> MainComponents.Widget.Templates(model, dispatch, rmv widget)
    //    | MainComponents.Widget._FilePicker -> MainComponents.Widget.FilePicker(model, dispatch, rmv widget)
    //    | MainComponents.Widget._DataAnnotator -> MainComponents.Widget.DataAnnotator(model, dispatch, rmv widget)
    //    |> WidgetOrderContainer bringWidgetToFront

    let createFileTree (parent: FileItemDTO option) =
        let rec loop (parent: FileItemDTO option) =
            if parent.IsSome then
                match parent.Value.isDirectory with
                | true ->
                    let tmp =
                        let ra = ResizeArray(parent.Value.children.Values)

                        ra.ToArray()
                        |> Array.map (fun entry -> loop (Some entry))
                        |> Array.choose id
                        |> List.ofArray

                    let result = {
                        FileTree.createFolder parent.Value.name (Some parent.Value.path) "swt:fluent--folder-24-regular" with
                            Children = Some tmp
                    }
                    Some result
                | false ->
                    Some(FileTree.createFile parent.Value.name (Some parent.Value.path) "swt:fluent--document-24-regular")
            else
                None

        let fileItem = loop parent

        let openPreview (item: FileItem) =
            promise {
               if item.Path.IsSome && not item.IsDirectory then
                    console.log ($"[Renderer] Opening file: {item.Path.Value}")
                    let! result = Api.arcVaultApi.openFile item.Path.Value

                    match result with
                    | Ok data ->
                        console.log ("[Renderer] Received data, processing...")
                        setPreviewData (Some data)
                        setPreviewError None
                        setDidSelectFile true

                    | Error exn ->
                        console.log ($"[Renderer] Error: {exn.Message}")
                        setPreviewData (None)
                        setPreviewError (Some $"Could not open preview for '{item.Name}': {exn.Message}")
                        setDidSelectFile true
                else
                    setPreviewError (Some $"File '{item.Name}' has no path.")
            }
            |> Promise.start

        if fileItem.IsSome then
            Some(FileExplorer.FileExplorer([fileItem.Value], openPreview))
        else
            None

    let insertEntry (root: FileItemDTO) (rootPath: string) (entry: FileEntry) =
        let parts = entry.path.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

        let splittedRootPath = rootPath.Split('/', System.StringSplitOptions.RemoveEmptyEntries)

        let rec loop (node: FileItemDTO) index =
            let part = parts[index]
            let isLast = index = parts.Length - 1

            let child =
                match node.children.TryGetValue(part) with
                | true, existing -> existing
                | false, _ ->
                    let newPath = parts.[0..index] |> String.concat "/"
                    let newNode =
                        FileItemDTO.create(
                            part,
                            entry.isDirectory,
                            newPath,
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
            FileItemDTO.create(tmp.name, tmp.isDirectory, tmp.path, System.Collections.Generic.Dictionary())

        adaptedFileEntires
        |> Array.iter (fun fileEntry -> insertEntry rootElement rootPath fileEntry)

        rootElement

    React.useEffect (
        (fun _ ->
            let ra = ResizeArray(fileTree.Values)
            let fileEntries = ra.ToArray()

            let fileTree =
                if fileEntries.Length > 0 then
                    Some(getFileTree fileEntries)
                else
                    None

            createFileTree fileTree |> setFileExplorer |> ignore
        ), 
        [| box fileTree |]
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

    let computeARCContent (path: string) =
        match previewData with
        | Some data ->
            match data with
            | ArcFileData(fileType, json) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile -> CreateARCPreview arcFile setArcFileState activeView setActiveView didSelectFile setDidSelectFile
                | None ->
                    Html.div [
                        prop.className "swt:p-4 swt:text-error"
                        prop.text "Failed to parse ArcFile data"
                    ]
            | Text content ->
                Html.div [
                    prop.className "swt:size-full swt:p-4 swt:overflow-auto swt:bg-base-100"
                    prop.children [|
                        Html.pre [
                            prop.className "swt:text-sm swt:font-mono"
                            prop.text content
                        ]
                    |]
                ]
            | Unknown ->
                Html.div [
                    prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                    prop.children [| Html.h1 "Unknown file type" |]
                ]
        | None ->
            match previewError with
            | Some errMsg ->
                Html.div [
                    prop.className "swt:size-full swt:flex swt:justify-center swt:items-center swt:flex-col swt:gap-2"
                    prop.children [|
                        Html.h2 [
                            prop.className "swt:text-error swt:font-bold"
                            prop.text "Preview Error"
                        ]
                        Html.span [
                            prop.className "swt:text-base-content swt:opacity-70"
                            prop.text errMsg
                        ]
                    |]
                ]
            | None ->
                Html.h1 [
                    prop.text path
                    prop.className
                        "swt:text-xl swt:uppercase swt:inline-block swt:text-transparent swt:bg-clip-text swt:bg-linear-to-r swt:from-primary swt:to-secondary"
                ]

    let children =
        React.useMemo (
            (fun _ ->
                match appState with
                | AppState.Init ->
                    Html.div [
                        prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex swt:justify-center swt:items-center"
                        prop.children [
                            Html.div [
                                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex-none" 
                                        prop.children [ CreateARCitectNavbar activeView addWidget ]
                                    ]
                                    Html.div [
                                        prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                                        prop.children [ components.InitState.InitState() ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                | AppState.ARC path ->
                    Html.div [
                        prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex"
                        prop.children [
                            Html.div [
                                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                                prop.children [
                                    // Navbar
                                    Html.div [
                                        prop.className "swt:flex-none"
                                        prop.children [ CreateARCitectNavbar activeView addWidget ]
                                    ]
                                    // Main content
                                    Html.div [
                                        prop.className "swt:flex-1 swt:overflow-y-auto swt:flex swt:flex-col swt:min-w-0"
                                        prop.children [
                                            computeARCContent path
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
            ),
            [| appState; box previewData; box activeView; box arcFileState |]
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
                (let sidebarContent =
                    match fileExplorer with
                    | Some fe -> fe
                    | None -> Html.span [ prop.className "swt:opacity-50"; prop.text "No files" ]

                 Some(
                     Html.div [
                         prop.className "swt:p-4"
                         prop.children [|
                            Html.h2 [
                                prop.text "ARC-Tree"
                            ]
                            sidebarContent
                        |]
                     ]
                 )),
            leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
        )
    )