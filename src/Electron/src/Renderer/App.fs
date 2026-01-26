module Renderer.App

open Feliz
open Fable.Core
open Fable.Electron.Remoting.Renderer
open Swate.Components
open Swate.Components.FileExplorerTypes
open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes
open Browser.Dom
open ARCtrl
open ARCtrl.Json
open MetadataForms

module private MainPageUtil =
    [<Literal>]
    let DrawerId = "MainPageDrawerId"

open MainPageUtil

[<RequireQualifiedAccess>]
type PreviewActiveView =
    | Metadata
    | Table of int
    | DataMap

type ArcFileState = {
    ArcFile: ArcFiles
    ActiveView: PreviewActiveView
}

[<ReactComponent>]
let TablePreview (table: ARCtrl.ArcTable) =
    let tableState, setTableState = React.useState (table)

    // Update table state when prop changes
    React.useEffect (
        (fun () ->
            console.log ("TablePreview received table object")
            setTableState (table)
        ),
        [| box table |]
    )

    // Wrap in context provider as required by AnnotationTable
    AnnotationTableContextProvider.AnnotationTableContextProvider(
        Html.div [
            prop.className "swt:size-full"
            prop.children [
                AnnotationTable.AnnotationTable(tableState, setTableState)
            ]
        ]
    )

[<ReactComponent>]
let createARCitectNavbar () =
    Components.BaseNavbar.Glow [
        Html.label [
            prop.className "swt:btn swt:btn-square swt:btn-ghost swt:md:hidden"
            prop.htmlFor DrawerId
            prop.children [
                Svg.svg [
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.className "swt:size-5"
                    svg.fill "none"
                    svg.viewBox (0, 0, 24, 24)
                    svg.stroke "currentColor"
                    svg.children [
                        Svg.path [
                            svg.strokeLineCap "round"
                            svg.strokeLineJoin "round"
                            svg.strokeWidth 2
                            svg.d "M4 6h16M4 12h16M4 18h7"
                        ]
                    ]
                ]
            ]
        ]
        //Components.Logo.Main(
        //    onClick = (fun _ -> PageState.UpdateMainPage Routing.MainPage.Default |> PageStateMsg |> dispatch)
        //)
    ]

/// Footer tabs for switching between metadata and tables
[<ReactComponent>]
let createARCitectFooter (arcFile: ArcFiles) (activeView: PreviewActiveView) (setActiveView: PreviewActiveView -> unit) =
    let tables = arcFile.Tables()

    Html.div [
        prop.className "swt:flex swt:gap-1 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300 swt:overflow-x-auto"
        prop.children [|
            // Metadata tab
            Html.button [
                prop.className [
                    "swt:btn swt:btn-sm"
                    if activeView = PreviewActiveView.Metadata then
                        "swt:btn-primary"
                    else
                        "swt:btn-ghost"
                ]
                prop.onClick (fun _ -> setActiveView PreviewActiveView.Metadata)
                prop.children [|
                    Html.span [ prop.className "swt:i-fluent--info-24-regular" ]
                    Html.span [ prop.text "Metadata" ]
                |]
            ]
            // Table tabs
            for i = 0 to tables.Count - 1 do
                let table = tables.[i]

                Html.button [
                    prop.key (string i)
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.Table i then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView (PreviewActiveView.Table i))
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--table-24-regular" ]
                        Html.span [ prop.text table.Name ]
                    |]
                ]
            // DataMap tab
            match arcFile with
            | ArcFiles.Assay a when a.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.Study(s, _) when s.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.Run r when r.DataMap.IsSome ->
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | ArcFiles.DataMap _ ->
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if activeView = PreviewActiveView.DataMap then
                            "swt:btn-primary"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView PreviewActiveView.DataMap)
                    prop.children [|
                        Html.span [ prop.className "swt:i-fluent--database-24-regular" ]
                        Html.span [ prop.text "DataMap" ]
                    |]
                ]
            | _ -> ()
        |]
    ]

/// Render metadata view based on ArcFile type using editable form components
[<ReactComponent>]
let MetadataPreview (arcFile: ArcFiles, setArcFile: ArcFiles -> unit) =
    Html.div [
        prop.className "swt:p-4 swt:overflow-y-auto swt:h-full"
        prop.children [
            match arcFile with
            | ArcFiles.Investigation inv ->
                InvestigationMetadata(inv, fun updated -> setArcFile (ArcFiles.Investigation updated))
            | ArcFiles.Study(study, assays) ->
                StudyMetadata(study, fun updated -> setArcFile (ArcFiles.Study(updated, assays)))
            | ArcFiles.Assay assay -> AssayMetadata(assay, fun updated -> setArcFile (ArcFiles.Assay updated))
            | ArcFiles.Run run -> RunMetadata(run, fun updated -> setArcFile (ArcFiles.Run updated))
            | ArcFiles.Workflow workflow ->
                WorkflowMetadata(workflow, fun updated -> setArcFile (ArcFiles.Workflow updated))
            | ArcFiles.DataMap(path, datamap) -> DataMapMetadata(datamap)
            | ArcFiles.Template template ->
                TemplateMetadata(template, fun updated -> setArcFile (ArcFiles.Template updated))
        ]
    ]

let createTableView activeView arcFileState setArcFileState =
    match activeView with
    | PreviewActiveView.Metadata -> MetadataPreview(arcFileState, setArcFileState)
    | PreviewActiveView.Table index ->
        let tables = arcFileState.Tables()

        if index < tables.Count then
            TablePreview(tables.[index])
        else
            Html.div [
                prop.className "swt:p-4 swt:text-error"
                prop.text "Table not found"
            ]
    | PreviewActiveView.DataMap ->
        match arcFileState with
        | ArcFiles.Assay a when a.DataMap.IsSome ->
            let dm, setDm = React.useState a.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.Study(s, _) when s.DataMap.IsSome ->
            let dm, setDm = React.useState s.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.Run r when r.DataMap.IsSome ->
            let dm, setDm = React.useState r.DataMap.Value
            DataMapTable.DataMapTable(dm, setDm)
        | ArcFiles.DataMap(_, datamap) ->
            let dm, setDm = React.useState datamap
            DataMapTable.DataMapTable(dm, setDm)
        | _ ->
            Html.div [
                prop.className "swt:p-4 swt:text-error"
                prop.text "No DataMap available"
            ]

[<ReactComponent>]
let createARCPreview (arcFile: ArcFiles) =
    let arcFileState, setArcFileState = React.useState arcFile
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata

    React.useEffect (
        (fun () ->
            setArcFileState arcFile
            let tables = arcFile.Tables()

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
                prop.className "swt:flex-1 swt:overflow-hidden"
                prop.children [
                    createTableView activeView arcFileState setArcFileState
                ]
            ]
            createARCitectFooter arcFileState activeView setActiveView
        |]
    ]

let parseArcFileFromJson (fileType: ArcFileType) (json: string) : ArcFiles option =
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

    let recentARCs, setRecentARCs = React.useState ([||])
    let appState, setAppState = React.useState (AppState.Init)
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())
    let fileExplorer, setFileExplorer = React.useState (None)
    let (assay: ARCtrl.ArcAssay option), setAssay = React.useState (None)
    let (previewData: PreviewData option), setPreviewData = React.useState (None)
    let (previewError: string option), setPreviewError = React.useState (None)

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
                    Some(
                        FileTree.createFile parent.Value.name (Some parent.Value.path) "swt:fluent--document-24-regular"
                    )
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
                    | Error exn ->
                        console.log ($"[Renderer] Error: {exn.Message}")
                        setPreviewData (None)
                        setPreviewError (Some $"Could not open preview for '{item.Name}': {exn.Message}")
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
                // Parse JSON and render using ArcFilePreview
                match parseArcFileFromJson fileType json with
                | Some arcFile -> createARCPreview(arcFile)
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
                                        prop.children [ createARCitectNavbar () ]
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
                        prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex swt:justify-center swt:items-center"
                        prop.children [
                            Html.div [
                                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex-none" 
                                        prop.children [ createARCitectNavbar () ]
                                    ]
                                    Html.div [
                                        prop.className "swt:flex-1 swt:flex swt:justify-center swt:items-center"
                                        prop.children [ computeARCContent path ]
                                    ]
                                ]
                            ]
                        ]
                    ]
            ),
            [| appState; box previewData |]
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
                         prop.children [| Html.h2 [ prop.text "ARC-Tree" ]; sidebarContent |]
                     ]
                 )),
            leftActions = React.Fragment [| Layout.LeftSidebarToggleBtn() |]
        )
    )