namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types

open DataHubSidebarTypes

[<Erase; Mangle(false)>]
type DataHubSidebar =

    // tiny helpers

    [<ReactComponent>]
    static member private StatusBadge(text: string, variant: string) =
        Html.span [
            prop.testId "DataHubStatusBadge"
            prop.className [
                "swt:badge swt:badge-sm"
                match variant with
                | "success" -> "swt:badge-success"
                | "error" -> "swt:badge-error"
                | "warning" -> "swt:badge-warning"
                | _ -> "swt:badge-ghost"
            ]
            prop.text text
        ]

    [<ReactComponent>]
    static member private SectionHeading(text: string) =
        Html.h3 [
            prop.className "swt:text-sm swt:font-semibold swt:text-base-content/70 swt:uppercase swt:tracking-wide"
            prop.text text
        ]

    [<ReactComponent>]
    static member private ChangedFileStatusDot(status: ChangedFileStatus) =
        let color, label =
            match status with
            | ChangedFileStatus.New     -> "swt:bg-success", "new"
            | ChangedFileStatus.Changed -> "swt:bg-warning", "changed"
            | ChangedFileStatus.Deleted -> "swt:bg-error",   "deleted"
            | ChangedFileStatus.Moved   -> "swt:bg-info",    "moved"

        Html.span [
            prop.title label
            prop.className [
                "swt:inline-block swt:size-2 swt:rounded-full swt:shrink-0"
                color
            ]
        ]

    // disconnected panel

    [<ReactComponent>]
    static member private DisconnectedView(onConnect: unit -> unit, isConnecting: bool) =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children [
                        Html.span [
                            prop.className "swt:iconify swt:fluent--cloud-off-24-regular swt:size-5"
                        ]
                        Html.span [
                            prop.className "swt:text-lg swt:font-bold"
                            prop.text "DataHub"
                        ]
                        DataHubSidebar.StatusBadge("not connected", "warning")
                    ]
                ]
                Html.p [
                    prop.className "swt:text-sm swt:text-base-content/70"
                    prop.text "Connect to a DataHub to save, retrieve and share your ARCs online."
                ]
                Html.button [
                    prop.testId "ConnectDataHubButton"
                    prop.className "swt:btn swt:btn-primary swt:w-full"
                    prop.disabled isConnecting
                    prop.children [
                        if isConnecting then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                            ]
                        Html.span [
                            prop.text (if isConnecting then "Connecting..." else "Connect to DataHub")
                        ]
                    ]
                    prop.onClick (fun _ -> onConnect ())
                ]
            ]
        ]

    // changed files section

    [<ReactComponent>]
    static member private ChangedFilesList(changedFiles: ChangedFile[], onDiscardFile: ChangedFile -> unit) =
        let containerRef = React.useElementRef ()

        Html.div [
            prop.testId "ChangedFilesList"
            prop.className "swt:flex swt:flex-col swt:gap-1"
            prop.children [
                DataHubSidebar.SectionHeading("Local Changes")
                if changedFiles.Length = 0 then
                    Html.p [
                        prop.testId "ChangedFilesEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60 swt:py-2"
                        prop.text "No local changes. Your ARC is up to date."
                    ]
                else
                    Html.div [
                        prop.ref containerRef
                        prop.className
                            "swt:flex swt:flex-col swt:max-h-48 swt:overflow-y-auto swt:bg-base-200 swt:rounded swt:p-1 swt:gap-0.5"
                        prop.children [
                            for file in changedFiles do
                                Html.div [
                                    prop.testId ("ChangedFileItem-" + file.Path)
                                    prop.key file.Path
                                    prop.custom ("data-filepath", file.Path)
                                    prop.className
                                        "swt:flex swt:items-center swt:gap-2 swt:px-2 swt:py-1 swt:rounded swt:hover:bg-base-300 swt:cursor-default swt:text-sm swt:group"
                                    prop.children [
                                        DataHubSidebar.ChangedFileStatusDot(file.Status)
                                        Html.span [
                                            prop.className "swt:truncate swt:flex-1"
                                            prop.title (
                                                match file.OldPath with
                                                | Some old -> "Moved from " + old
                                                | None -> file.Path
                                            )
                                            prop.text file.Path
                                        ]
                                        Html.span [
                                            prop.className "swt:text-xs swt:text-base-content/50 swt:shrink-0"
                                            prop.text (
                                                match file.Status with
                                                | ChangedFileStatus.New     -> "new"
                                                | ChangedFileStatus.Changed -> "changed"
                                                | ChangedFileStatus.Deleted -> "deleted"
                                                | ChangedFileStatus.Moved   -> "moved"
                                            )
                                        ]
                                        Html.button [
                                            prop.testId ("DiscardFileButton-" + file.Path)
                                            prop.className
                                                "swt:btn swt:btn-ghost swt:btn-xs swt:opacity-0 swt:group-hover:opacity-100 swt:transition-opacity"
                                            prop.title "Discard this change"
                                            prop.onClick (fun e ->
                                                e.stopPropagation ()
                                                onDiscardFile file
                                            )
                                            prop.children [
                                                Html.span [
                                                    prop.className
                                                        "swt:iconify swt:fluent--dismiss-16-regular swt:size-3"
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
                    Html.span [
                        prop.className "swt:text-xs swt:text-base-content/50 swt:mt-1"
                        prop.text (string changedFiles.Length + " file(s) with local changes. Right-click a file to discard.")
                    ]
                    ContextMenu.ContextMenu(
                        (fun (data: obj) ->
                            let filePath = data |> unbox<string>
                            [
                                ContextMenuItem(
                                    text = Html.span [ prop.text "Discard this change" ],
                                    icon = Icons.ExclamationTriangle(),
                                    onClick =
                                        (fun e ->
                                            e.buttonEvent.stopPropagation ()
                                            let file =
                                                changedFiles |> Array.tryFind (fun f -> f.Path = filePath)
                                            file |> Option.iter onDiscardFile
                                        )
                                )
                            ]
                        ),
                        ref = containerRef,
                        onSpawn =
                            (fun e ->
                                let target = e.target :?> HTMLElement
                                let row = target.closest ("[data-filepath]")

                                match row with
                                | Some el ->
                                    let el = el :?> HTMLElement
                                    Some(box el?dataset?filepath)
                                | None -> None
                            )
                    )
            ]
        ]

    // ARC browser panel

    [<ReactComponent>]
    static member private ARCBrowser
        (
            browserMode: ARCBrowserMode,
            onBrowserModeChange: ARCBrowserMode -> unit,
            browserProjects: ARCProject[],
            isLoadingBrowser: bool,
            onSelectProject: ARCProject -> unit,
            selectedProject: ARCProject option
        ) =
        Html.div [
            prop.testId "ARCBrowserPanel"
            prop.className "swt:flex swt:flex-col swt:gap-1"
            prop.children [
                DataHubSidebar.SectionHeading("ARC Browser")
                Html.div [
                    prop.testId "ARCBrowserTabs"
                    prop.role.tabList
                    prop.className "swt:tabs swt:tabs-box swt:tabs-xs swt:w-full"
                    prop.children [
                        let modes =
                            [
                                ARCBrowserMode.YourARCs, "Your ARCs"
                                ARCBrowserMode.Latest, "Latest"
                                ARCBrowserMode.Featured, "Featured"
                            ]

                        for mode, label in modes do
                            Html.div [
                                prop.role.tab
                                prop.testId ("ARCBrowserTab-" + label.Replace(" ", ""))
                                prop.key label
                                prop.className [
                                    "swt:tab"
                                    if browserMode = mode then
                                        "swt:tab-active"
                                ]
                                prop.onClick (fun _ -> onBrowserModeChange mode)
                                prop.text label
                            ]
                    ]
                ]
                if isLoadingBrowser then
                    Html.div [
                        prop.testId "ARCBrowserLoading"
                        prop.className "swt:flex swt:items-center swt:gap-2 swt:py-2"
                        prop.children [
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                            ]
                            Html.span [
                                prop.className "swt:text-sm swt:text-base-content/70"
                                prop.text "Loading ARCs..."
                            ]
                        ]
                    ]
                elif browserProjects.Length = 0 then
                    Html.p [
                        prop.testId "ARCBrowserEmpty"
                        prop.className "swt:text-sm swt:text-base-content/60 swt:py-2"
                        prop.text (
                            match browserMode with
                            | ARCBrowserMode.YourARCs -> "No ARCs found. Create one to get started."
                            | ARCBrowserMode.Latest -> "No recent ARCs available."
                            | ARCBrowserMode.Featured -> "No featured ARCs at the moment."
                        )
                    ]
                else
                    Html.ul [
                        prop.className
                            "swt:menu swt:menu-sm swt:flex-nowrap swt:max-h-48 swt:overflow-y-auto swt:bg-base-200 swt:rounded"
                        prop.children [
                            for project in browserProjects do
                                let isSelected =
                                    selectedProject
                                    |> Option.map (fun p -> p.Id = project.Id)
                                    |> Option.defaultValue false

                                Html.li [
                                    prop.key (string project.Id)
                                    prop.children [
                                        Html.a [
                                            prop.testId ("ARCBrowserItem-" + string project.Id)
                                            prop.className [
                                                if isSelected then
                                                    "swt:active"
                                            ]
                                            prop.onClick (fun _ -> onSelectProject project)
                                            prop.children [
                                                Html.span [
                                                    prop.className "swt:truncate"
                                                    prop.text project.Name
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
            ]
        ]

    // selected project actions

    [<ReactComponent>]
    static member private ProjectActions
        (
            project: ARCProject,
            onSave: ARCProject -> unit,
            onFetch: ARCProject -> unit,
            onShare: ARCProject -> unit,
            operationState: OperationState,
            operationMessage: string option,
            hasChanges: bool
        ) =
        Html.div [
            prop.testId "ProjectActionsPanel"
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2 swt:p-2 swt:rounded swt:bg-base-200"
                    prop.children [
                        Html.span [
                            prop.className "swt:iconify swt:fluent--folder-24-regular swt:size-5"
                        ]
                        Html.span [
                            prop.testId "SelectedProjectName"
                            prop.className "swt:font-semibold swt:truncate"
                            prop.text project.Name
                        ]
                    ]
                ]

                Html.p [
                    prop.className "swt:text-xs swt:text-base-content/60"
                    prop.text "Save uploads all local changes. Fetch downloads the latest version."
                ]

                let isWorking = operationState = OperationState.Loading

                Html.button [
                    prop.testId "SaveToDataHubButton"
                    prop.className "swt:btn swt:btn-primary swt:btn-sm swt:w-full"
                    prop.disabled (isWorking || not hasChanges)
                    prop.title "Save all local changes to the DataHub"
                    prop.children [
                        if isWorking then
                            Html.span [
                                prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                            ]
                        Icons.CloudUpload("swt:size-4")
                        Html.span [ prop.text "Save to DataHub" ]
                    ]
                    prop.onClick (fun _ -> onSave project)
                ]

                Html.button [
                    prop.testId "FetchFromDataHubButton"
                    prop.className "swt:btn swt:btn-outline swt:btn-sm swt:w-full"
                    prop.disabled isWorking
                    prop.children [
                        Icons.ArrowsRotate()
                        Html.span [ prop.text "Get Latest from DataHub" ]
                    ]
                    prop.onClick (fun _ -> onFetch project)
                ]

                Html.button [
                    prop.testId "ShareARCButton"
                    prop.className "swt:btn swt:btn-ghost swt:btn-sm swt:w-full"
                    prop.disabled isWorking
                    prop.children [ Icons.Copy(); Html.span [ prop.text "Copy Share Link" ] ]
                    prop.onClick (fun _ -> onShare project)
                ]

                match operationState, operationMessage with
                | OperationState.Success, Some msg ->
                    Html.div [
                        prop.testId "OperationSuccess"
                        prop.className "swt:alert swt:alert-success swt:text-xs swt:py-1"
                        prop.children [ Icons.Check("swt:size-4"); Html.span [ prop.text msg ] ]
                    ]
                | OperationState.Error, Some msg ->
                    Html.div [
                        prop.testId "OperationError"
                        prop.className "swt:alert swt:alert-error swt:text-xs swt:py-1"
                        prop.children [ Icons.ExclamationTriangle(); Html.span [ prop.text msg ] ]
                    ]
                | _ -> Html.none
            ]
        ]

    // connected header

    [<ReactComponent>]
    static member private ConnectedHeader(dataHubUrl: string, onDisconnect: unit -> unit) =
        Html.div [
            prop.className "swt:flex swt:items-center swt:gap-2"
            prop.children [
                Html.span [
                    prop.className "swt:iconify swt:fluent--cloud-checkmark-24-regular swt:size-5 swt:text-success"
                ]
                Html.span [
                    prop.className "swt:text-lg swt:font-bold"
                    prop.text "DataHub"
                ]
                DataHubSidebar.StatusBadge("connected", "success")
                Html.button [
                    prop.testId "DisconnectButton"
                    prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:ml-auto"
                    prop.title "Disconnect"
                    prop.onClick (fun _ -> onDisconnect ())
                    prop.children [ Icons.LinkSlash() ]
                ]
            ]
        ]

    // PUBLIC ENTRY POINT

    [<ReactComponent>]
    static member Main
        (
            connectionState: ConnectionState,
            ?dataHubUrl: string,
            ?selectedProject: ARCProject option,
            ?onConnect: unit -> unit,
            ?onDisconnect: unit -> unit,
            ?onSelectProject: ARCProject -> unit,
            ?onSave: ARCProject -> unit,
            ?onFetch: ARCProject -> unit,
            ?onShare: ARCProject -> unit,
            ?operationState: OperationState,
            ?operationMessage: string option,
            ?errorMessage: string option,
            ?changedFiles: ChangedFile[],
            ?onDiscardFile: ChangedFile -> unit,
            ?browserMode: ARCBrowserMode,
            ?onBrowserModeChange: ARCBrowserMode -> unit,
            ?browserProjects: ARCProject[],
            ?isLoadingBrowser: bool
        ) =

        let selectedProject = defaultArg selectedProject None
        let onConnect = defaultArg onConnect ignore
        let onDisconnect = defaultArg onDisconnect ignore
        let onSelectProject = defaultArg onSelectProject ignore
        let onSave = defaultArg onSave ignore
        let onFetch = defaultArg onFetch ignore
        let onShare = defaultArg onShare ignore
        let operationState = defaultArg operationState OperationState.Idle
        let operationMessage = defaultArg operationMessage None
        let errorMessage = defaultArg errorMessage None
        let changedFiles = defaultArg changedFiles [||]
        let onDiscardFile = defaultArg onDiscardFile ignore
        let browserMode = defaultArg browserMode ARCBrowserMode.YourARCs
        let onBrowserModeChange = defaultArg onBrowserModeChange ignore
        let browserProjects = defaultArg browserProjects [||]
        let isLoadingBrowser = defaultArg isLoadingBrowser false

        Html.div [
            prop.testId "DataHubSidebar"
            prop.className "swt:flex swt:flex-col swt:gap-4 swt:p-3 swt:w-full swt:overflow-y-auto"
            prop.children [
                match connectionState with
                | ConnectionState.Disconnected
                | ConnectionState.Connecting ->
                    DataHubSidebar.DisconnectedView(
                        onConnect,
                        isConnecting = (connectionState = ConnectionState.Connecting)
                    )
                | ConnectionState.Connected ->
                    DataHubSidebar.ConnectedHeader(defaultArg dataHubUrl "DataHub", onDisconnect)

                    match selectedProject with
                    | Some proj ->
                        Html.div [ prop.className "swt:divider swt:my-0" ]
                        DataHubSidebar.ChangedFilesList(changedFiles, onDiscardFile)
                        Html.div [ prop.className "swt:divider swt:my-0" ]
                        DataHubSidebar.ProjectActions(
                            proj,
                            onSave,
                            onFetch,
                            onShare,
                            operationState,
                            operationMessage,
                            hasChanges = (changedFiles.Length > 0)
                        )
                    | None -> ()

                    Html.div [ prop.className "swt:divider swt:my-0" ]
                    DataHubSidebar.ARCBrowser(
                        browserMode,
                        onBrowserModeChange,
                        browserProjects,
                        isLoadingBrowser,
                        onSelectProject,
                        selectedProject
                    )

                match errorMessage with
                | Some err ->
                    Html.div [
                        prop.testId "GlobalError"
                        prop.className "swt:alert swt:alert-error swt:text-xs swt:py-1"
                        prop.children [ Icons.ExclamationTriangle(); Html.span [ prop.text err ] ]
                    ]
                | None -> ()
            ]
        ]

    // STORYBOOK ENTRY

    [<ReactComponent>]
    static member Entry() =

        let connectionState, setConnectionState =
            React.useState ConnectionState.Disconnected

        let selectedProject, setSelectedProject =
            React.useState (None: ARCProject option)

        let operationState, setOperationState =
            React.useState OperationState.Idle

        let operationMessage, setOperationMessage =
            React.useState (None: string option)

        let errorMessage, setErrorMessage =
            React.useState (None: string option)

        let changedFiles, setChangedFiles =
            React.useState ([||]: ChangedFile[])

        let browserMode, setBrowserMode =
            React.useState ARCBrowserMode.YourARCs

        let browserProjects, setBrowserProjects =
            React.useState ([||]: ARCProject[])

        let isLoadingBrowser, setIsLoadingBrowser =
            React.useState false

        let sampleYourARCs: ARCProject[] = [|
            {
                Id = 1
                Name = "Metabolomics Study 2026"
                Description = Some "LC-MS of leaf tissue"
                WebUrl = "https://git.nfdi4plants.org/user/metabolomics-2026"
                LastActivity = Some "2026-03-01"
            }
            {
                Id = 2
                Name = "RNAseq Drought Stress"
                Description = Some "Paired-end sequencing"
                WebUrl = "https://git.nfdi4plants.org/user/rnaseq-drought"
                LastActivity = Some "2026-02-28"
            }
            {
                Id = 3
                Name = "Proteomics Roots"
                Description = None
                WebUrl = "https://git.nfdi4plants.org/user/proteomics-roots"
                LastActivity = Some "2026-01-15"
            }
        |]

        let sampleLatestARCs: ARCProject[] = [|
            {
                Id = 10
                Name = "Lipidomics Arabidopsis"
                Description = Some "Lipid profiling"
                WebUrl = "https://git.nfdi4plants.org/community/lipidomics-arab"
                LastActivity = Some "2026-03-04"
            }
            {
                Id = 11
                Name = "Single-Cell Transcriptomics"
                Description = Some "10x Genomics workflow"
                WebUrl = "https://git.nfdi4plants.org/community/sc-transcriptomics"
                LastActivity = Some "2026-03-03"
            }
        |]

        let sampleFeaturedARCs: ARCProject[] = [|
            {
                Id = 20
                Name = "CEPLAS Reference ARC"
                Description = Some "Best-practice template ARC"
                WebUrl = "https://git.nfdi4plants.org/featured/ceplas-reference"
                LastActivity = Some "2026-02-20"
            }
        |]

        let sampleChangedFiles: ChangedFile[] = [|
            { Path = "assays/metabolomics/isa.assay.xlsx"; Status = ChangedFileStatus.Changed; OldPath = None }
            { Path = "studies/drought/protocols/extraction.md"; Status = ChangedFileStatus.New; OldPath = None }
            { Path = "runs/old-run/result.csv"; Status = ChangedFileStatus.Deleted; OldPath = None }
            {
                Path = "workflows/analysis.cwl"
                Status = ChangedFileStatus.Moved
                OldPath = Some "workflows/old-analysis.cwl"
            }
        |]

        let loadBrowserProjects (mode: ARCBrowserMode) =
            setIsLoadingBrowser true

            promise {
                do! Promise.sleep 500

                let projects =
                    match mode with
                    | ARCBrowserMode.YourARCs -> sampleYourARCs
                    | ARCBrowserMode.Latest -> sampleLatestARCs
                    | ARCBrowserMode.Featured -> sampleFeaturedARCs

                setBrowserProjects projects
                setIsLoadingBrowser false
            }
            |> Promise.start

        let onConnect () =
            setErrorMessage None
            setConnectionState ConnectionState.Connecting

            promise {
                do! Promise.sleep 1000
                setConnectionState ConnectionState.Connected
                setBrowserMode ARCBrowserMode.YourARCs
                loadBrowserProjects ARCBrowserMode.YourARCs
            }
            |> Promise.start

        let onDisconnect () =
            setConnectionState ConnectionState.Disconnected
            setSelectedProject None
            setOperationState OperationState.Idle
            setOperationMessage None
            setErrorMessage None
            setChangedFiles [||]
            setBrowserProjects [||]
            setBrowserMode ARCBrowserMode.YourARCs
            setIsLoadingBrowser false

        let onSelectProject (p: ARCProject) =
            setSelectedProject (Some p)
            setOperationState OperationState.Idle
            setOperationMessage None
            setChangedFiles sampleChangedFiles

        let onDiscardFile (f: ChangedFile) =
            setChangedFiles (changedFiles |> Array.filter (fun cf -> cf.Path <> f.Path))

        let onBrowserModeChange (mode: ARCBrowserMode) =
            setBrowserMode mode
            loadBrowserProjects mode

        let runOperation msg =
            fun (_: ARCProject) ->
                setOperationState OperationState.Loading
                setOperationMessage None

                promise {
                    do! Promise.sleep 1200
                    setOperationState OperationState.Success
                    setOperationMessage (Some msg)
                }
                |> Promise.start

        let onSave = runOperation "All changes saved to the DataHub."
        let onFetch = runOperation "Your ARC was updated with the latest version."

        let onShare (p: ARCProject) =
            setOperationState OperationState.Success
            setOperationMessage (Some ("Link copied: " + p.WebUrl))

        DataHubSidebar.Main(
            connectionState,
            dataHubUrl = "https://git.nfdi4plants.org/",
            selectedProject = selectedProject,
            onConnect = onConnect,
            onDisconnect = onDisconnect,
            onSelectProject = onSelectProject,
            onSave = onSave,
            onFetch = onFetch,
            onShare = onShare,
            operationState = operationState,
            operationMessage = operationMessage,
            errorMessage = errorMessage,
            changedFiles = changedFiles,
            onDiscardFile = onDiscardFile,
            browserMode = browserMode,
            onBrowserModeChange = onBrowserModeChange,
            browserProjects = browserProjects,
            isLoadingBrowser = isLoadingBrowser
        )
