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
open components.ExperimentLanding
open Renderer.components.BuildingBlockWidget

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
                prop.className "swt:flex-1 swt:overflow-x-hidden swt:overflow-y-auto"
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

let SerializeArcFileForSave (arcFile: ArcFiles) : SaveArcFileRequest option =
    match arcFile with
    | ArcFiles.Investigation investigation ->
        Some {
            FileType = ArcFileType.Investigation
            Json = ArcInvestigation.toJsonString 0 investigation
        }
    | ArcFiles.Study(study, _) ->
        Some {
            FileType = ArcFileType.Study
            Json = ArcStudy.toJsonString 0 study
        }
    | ArcFiles.Assay assay ->
        Some {
            FileType = ArcFileType.Assay
            Json = ArcAssay.toJsonString 0 assay
        }
    | ArcFiles.Run run ->
        Some {
            FileType = ArcFileType.Run
            Json = ArcRun.toJsonString 0 run
        }
    | ArcFiles.Workflow workflow ->
        Some {
            FileType = ArcFileType.Workflow
            Json = ArcWorkflow.toJsonString 0 workflow
        }
    | ArcFiles.DataMap _ ->
        None
    | ArcFiles.Template _ ->
        None

[<ReactComponent>]
let Main () =

    let widgets, setWidgets = React.useState []
    let tableMutationTick, setTableMutationTick = React.useStateWithUpdater 0
    let recentARCs, setRecentARCs = React.useState [||]
    let fileExplorer, setFileExplorer = React.useState None
    let didSelectFile, setDidSelectFile = React.useState false
    let appState, setAppState = React.useState (AppState.Init)
    let (arcFileState: ArcFiles option), setArcFileState = React.useState None
    let activeView, setActiveView = React.useState PreviewActiveView.Metadata
    let (previewError: string option), setPreviewError = React.useState (None)
    let (previewData: PreviewData option), setPreviewData = React.useState (None)
    let (fileTree: System.Collections.Generic.Dictionary<string, FileEntry>), setFileTree = React.useState (System.Collections.Generic.Dictionary<string, FileEntry>())
    let (selectedTreeItemPath: string option), setSelectedTreeItemPath = React.useState (None)
    let landingDraft, setLandingDraft = React.useState LandingDraft.init
    let landingUiState, setLandingUiState = React.useState LandingUiState.init
    let landingDraftActive, setLandingDraftActive = React.useState false
    let showLandingDraft, setShowLandingDraft = React.useState false

    let resetLandingDraft () =
        setLandingDraft LandingDraft.init
        setLandingUiState LandingUiState.init
        setLandingDraftActive true
        setShowLandingDraft true
        setPreviewData None
        setPreviewError None
        setSelectedTreeItemPath None
        setDidSelectFile false
        setArcFileState None

    React.useEffect (
        (fun () ->
            match previewData with
            | Some (ArcFileData(fileType, json)) ->
                match ParseArcFileFromJson fileType json with
                | Some arcFile ->
                    match arcFileState with
                    | None ->
                        setArcFileState (Some arcFile)
                    | Some existing when existing.getIdentifier() <> arcFile.getIdentifier() ->
                        setArcFileState (Some arcFile)
                    | _ -> ()
                | None -> ()
            | _ -> ()
        ),
        [| box previewData |]
    )

    React.useLayoutEffectOnce (fun _ ->
        Api.getOpenPath()
        |> Promise.map (fun pathOption ->
            match pathOption with
            | Some p ->
                resetLandingDraft ()
                AppState.ARC p |> setAppState
            | None ->
                setLandingDraftActive false
                setShowLandingDraft false
                setSelectedTreeItemPath None
                setAppState AppState.Init
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
        let normalizePath (path: string) = path.Replace("\\", "/").TrimEnd('/')

        let isFocusedPathOrAncestor (nodePath: string) =
            match selectedTreeItemPath with
            | Some focusedPath ->
                let normalizedNode = normalizePath nodePath
                let normalizedFocused = normalizePath focusedPath

                normalizedFocused = normalizedNode
                || normalizedFocused.StartsWith(normalizedNode + "/", System.StringComparison.OrdinalIgnoreCase)
            | None -> false

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
                            Id = parent.Value.path
                            IsExpanded = isFocusedPathOrAncestor parent.Value.path
                            Children = Some tmp
                    }
                    Some result
                | false ->
                    Some(
                        {
                            FileTree.createFile parent.Value.name (Some parent.Value.path) "swt:fluent--document-24-regular" with
                                Id = parent.Value.path
                        }
                    )
            else
                None

        let fileItem = loop parent

        let openPreview (item: FileItem) =
            promise {
               let isDirectoryByPath =
                    match item.Path with
                    | Some p when fileTree.ContainsKey(p) -> fileTree.[p].isDirectory
                    | _ -> item.IsDirectory

               if item.Path.IsSome && not isDirectoryByPath then
                    console.log ($"[Renderer] Opening file: {item.Path.Value}")
                    setSelectedTreeItemPath item.Path
                    setShowLandingDraft false
                    let! result = Api.openFile item.Path.Value

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
               elif item.Path.IsSome && isDirectoryByPath then
                    // Folders are not preview targets.
                    setPreviewError None
                else
                    setPreviewError (Some $"File '{item.Name}' has no path.")
            }
            |> Promise.start

        if fileItem.IsSome then
            Some(
                FileExplorer.FileExplorer(
                    initialItems = [ fileItem.Value ],
                    onItemClick = openPreview,
                    ?selectedItemId = selectedTreeItemPath
                )
            )
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
                | true, existing when ((not isLast) || entry.isDirectory) && not existing.isDirectory ->
                    // A node may first appear via a file path segment; upgrade it to a directory when needed.
                    let upgraded = { existing with isDirectory = true }
                    node.children.[part] <- upgraded
                    upgraded
                | true, existing -> existing
                | false, _ ->
                    let newPath = parts.[0..index] |> String.concat "/"
                    let isDirectory =
                        if isLast then
                            entry.isDirectory
                        else
                            true

                    let newNode =
                        FileItemDTO.create(
                            part,
                            isDirectory,
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

        let adaptedFileEntires =
            fileEntries
            |> Array.filter (fun fileEntry -> fileEntry.path <> rootPath)
            // Deterministic order avoids creating parents from file entries before their directory entries.
            |> Array.sortBy (fun fileEntry ->
                let depth =
                    fileEntry.path.Split('/', System.StringSplitOptions.RemoveEmptyEntries).Length

                depth, (if fileEntry.isDirectory then 0 else 1), fileEntry.path
            )

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
        [| box fileTree; box selectedTreeItemPath |]
    )

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        pathChange =
            fun pathOption ->
                console.log ("[Swate] CHANGE PATH!")
                match pathOption with
                | Some p ->
                    resetLandingDraft ()
                    AppState.ARC p |> setAppState
                | None ->
                    setLandingDraftActive false
                    setShowLandingDraft false
                    setSelectedTreeItemPath None
                    setAppState AppState.Init
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
                let! r = Api.openARC()

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

    let tryGetCreatedFilePath (target: ExperimentTarget) (identifier: string) =
        match appState with
        | AppState.ARC arcPath ->
            let root = arcPath.Replace("\\", "/").TrimEnd('/')

            match target with
            | ExperimentTarget.Study -> Some $"{root}/studies/{identifier}/isa.study.xlsx"
            | ExperimentTarget.Assay -> Some $"{root}/assays/{identifier}/isa.assay.xlsx"
        | AppState.Init -> None

    let createFromLanding (target: ExperimentTarget) =
        promise {
            setLandingUiState {
                landingUiState with
                    IsSubmitting = true
                    Error = None
            }

            let request = toCreateRequest landingDraft target
            let! result = Api.createExperimentFromLanding request

            match result with
            | Ok response ->
                response.CreatedIdentifier
                |> tryGetCreatedFilePath target
                |> setSelectedTreeItemPath
                setPreviewData (Some response.PreviewData)
                setPreviewError None
                setDidSelectFile true
                setShowLandingDraft false
                setLandingDraftActive false
                setLandingDraft LandingDraft.init
                setLandingUiState LandingUiState.init
            | Error exn ->
                setLandingUiState {
                    landingUiState with
                        IsSubmitting = false
                        Error = Some exn.Message
                }
        }
        |> Promise.start

    React.useEffectOnce (fun _ -> Remoting.init |> Remoting.buildHandler ipcHandler)

    //let changedArcFile arcFileState arcFile =
    //    match arcFileState with
    //    | ArcFiles.Assay a when a.DataMap.IsSome ->
    //        let dm, setDm = React.useState a.DataMap.Value
    //        DataMapTable.DataMapTable(dm, setDm)
    //    | ArcFiles.Study(s, _) when s.DataMap.IsSome ->
    //        let dm, setDm = React.useState s.DataMap.Value
    //        DataMapTable.DataMapTable(dm, setDm)
    //    | ArcFiles.Run r when r.DataMap.IsSome ->
    //        let dm, setDm = React.useState r.DataMap.Value
    //        DataMapTable.DataMapTable(dm, setDm)
    //    | ArcFiles.DataMap(_, datamap) ->
    //        let dm, setDm = React.useState datamap
    //        DataMapTable.DataMapTable(dm, setDm)

    let computeARCContent (path: string) =
        if landingDraftActive && showLandingDraft then
            ExperimentLandingView(landingDraft, setLandingDraft, landingUiState, setLandingUiState, createFromLanding)
        else
            match previewData with
            | Some data ->
                match data with
                | ArcFileData _ ->
                    match arcFileState with
                    | Some arcFile ->
                        CreateARCPreview arcFile setArcFileState activeView setActiveView didSelectFile setDidSelectFile
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

    let onClick _ =
        match arcFileState with
        | None -> ()
        | Some arcFile ->
            match SerializeArcFileForSave arcFile with
            | None ->
                setPreviewError (Some "Saving this file type is not supported in Electron yet.")
            | Some request ->
                promise {
                    let! result = Api.saveArcFile request

                    match result with
                    | Ok updatedPreview ->
                        setPreviewData (Some updatedPreview)
                        setPreviewError None
                        setDidSelectFile true
                    | Error exn ->
                        setPreviewError (Some $"Save failed: {exn.Message}")
                }
                |> Promise.start

    let activeTableData : ActiveTableData option =
        match arcFileState, activeView with
        | Some arcFile, PreviewActiveView.Table tableIndex ->
            let tables = arcFile.Tables()

            if tableIndex >= 0 && tableIndex < tables.Count then
                let table = tables.[tableIndex]

                Some {
                    ArcFile = arcFile
                    Table = table
                    TableName = table.Name
                }
            else
                None
        | _ -> None

    let onTableMutated () =
        setTableMutationTick (fun latest -> latest + 1)

    let children =
        React.useMemo (
            (fun _ ->
                match appState with
                | AppState.Init ->
                    Html.div [
                        prop.className "swt:drawer swt:md:drawer-open swt:size-full swt:flex swt:justify-center swt:items-center"
                        prop.children [
                            components.Widgets.FloatingWidgetLayer widgets setWidgets activeTableData onTableMutated
                            Html.div [
                                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex-none" 
                                        prop.children [ CreateARCitectNavbar activeView addWidget arcFileState onClick ]
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
                            components.Widgets.FloatingWidgetLayer widgets setWidgets activeTableData onTableMutated
                            Html.div [
                                prop.className "swt:size-full swt:flex swt:flex-col swt:drawer-content"
                                prop.children [
                                    Html.div [
                                        prop.className "swt:flex-none"
                                        prop.children [ CreateARCitectNavbar activeView addWidget arcFileState onClick ]
                                    ]
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
            [|
                box appState
                box previewData
                box activeView
                box arcFileState
                box previewError
                box landingDraft
                box landingUiState
                box landingDraftActive
                box showLandingDraft
                box widgets
                box tableMutationTick
            |]
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
                            match appState with
                            | AppState.ARC _ ->
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:btn-outline swt:mb-2 swt:w-full"
                                    prop.text "Landing Page"
                                    prop.onClick (fun _ ->
                                        setPreviewError None

                                        if landingDraftActive then
                                            setShowLandingDraft true
                                        else
                                            resetLandingDraft ()
                                    )
                                ]
                            | _ -> Html.none
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
