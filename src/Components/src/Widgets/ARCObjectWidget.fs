namespace Swate.Components

open Fable.Core
open Feliz
open WidgetsLocalStorage
open Swate.Components.FileExplorerTypes

[<Erase; Mangle(false)>]
type ARCObjectWidget =

    static member private StoryPrefix = "ARC_OBJECT_WIDGET"
    static member private StorySize = { X = 1180; Y = 760 }
    static member private StoryPosition = { X = 24; Y = 24 }

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-[72rem] swt:max-w-[95vw] swt:h-[70vh] swt:max-h-[80vh]"

    static member private StoryItemIdRoot = "arc"
    static member private StoryItemIdStudy = "study:plant-stress"
    static member private StoryItemIdStudyAssayRef = "study:plant-stress:assay-ref:metabolomics"
    static member private StoryItemIdAssay = "assay:metabolomics"
    static member private StoryItemIdWorkflow = "workflow:extraction"
    static member private StoryItemIdRun = "run:2026-04-01"
    static member private StoryItemIdNote = "note:plant-stress:sampling-protocol"
    static member private StoryItemIdSample = "sample:leaf-01"

    static member private StoryItems() : FileItem list =
        let folder id name children =
            {
                FileTree.createFolder name None "swt:fluent--folder-24-regular" with
                    Id = id
                    IsExpanded = true
                    Children = Some children
            }

        let document id name =
            {
                FileTree.createFile name None "swt:fluent--document-24-regular" with
                    Id = id
            }

        let tag id name =
            {
                FileTree.createFile name None "swt:fluent--tag-24-regular" with
                    Id = id
            }

        let note id name =
            {
                FileTree.createFile name None "swt:fluent--document-24-regular" with
                    Id = id
            }

        [
            folder
                ARCObjectWidget.StoryItemIdRoot
                "MyArc"
                [
                    folder
                        "group:studies"
                        "Studies"
                        [
                            folder
                                ARCObjectWidget.StoryItemIdStudy
                                "PlantStressStudy"
                                [
                                    folder
                                        "study:plant-stress:assays"
                                        "Assays"
                                        [ document ARCObjectWidget.StoryItemIdStudyAssayRef "MetabolomicsAssay" ]
                                    folder
                                        "study:plant-stress:notes"
                                        "Notes"
                                        [ note ARCObjectWidget.StoryItemIdNote "Sampling protocol" ]
                                    folder
                                        "study:plant-stress:samples"
                                        "Samples"
                                        [ tag ARCObjectWidget.StoryItemIdSample "Leaf-01" ]
                                ]
                        ]
                    folder
                        "group:assays"
                        "Assays"
                        [ document ARCObjectWidget.StoryItemIdAssay "MetabolomicsAssay" ]
                    folder
                        "group:workflows"
                        "Workflows"
                        [ document ARCObjectWidget.StoryItemIdWorkflow "ExtractionWorkflow" ]
                    folder
                        "group:runs"
                        "Runs"
                        [ document ARCObjectWidget.StoryItemIdRun "Run-2026-04-01" ]
                ]
        ]

    static member private StoryMeta =
        [
            ARCObjectWidget.StoryItemIdRoot,
            ("MyArc", "ARC", "Canonical root", "isa.investigation.xlsx", "The ARC root groups all registered objects.")
            ARCObjectWidget.StoryItemIdStudy,
            ("PlantStressStudy", "Study", "Canonical object", "studies/PlantStressStudy/isa.study.xlsx", "Registered study with linked assay refs, notes, and derived samples.")
            ARCObjectWidget.StoryItemIdStudyAssayRef,
            ("MetabolomicsAssay", "Assay Reference", "Relationship node", "assays/MetabolomicsAssay/isa.assay.xlsx", "Reference node from the study to the canonical assay entry.")
            ARCObjectWidget.StoryItemIdAssay,
            ("MetabolomicsAssay", "Assay", "Canonical object", "assays/MetabolomicsAssay/isa.assay.xlsx", "Assay metadata and tables live on the canonical assay node.")
            ARCObjectWidget.StoryItemIdWorkflow,
            ("ExtractionWorkflow", "Workflow", "Canonical object", "workflows/ExtractionWorkflow/isa.workflow.xlsx", "Workflow nodes can expose subworkflow references and workflow datamaps.")
            ARCObjectWidget.StoryItemIdRun,
            ("Run-2026-04-01", "Run", "Canonical object", "runs/Run-2026-04-01/isa.run.xlsx", "Run nodes can reference workflows and derived sample collections.")
            ARCObjectWidget.StoryItemIdNote,
            ("Sampling protocol", "Note", "Filesystem-backed", "notes/studies/PlantStressStudy/17_03_2026/sampling_protocol.md", "Notes remain backed by markdown files even in the object tree.")
            ARCObjectWidget.StoryItemIdSample,
            ("Leaf-01", "Sample", "Virtual node", "-", "Sample nodes are derived from table content, not standalone files.")
        ]
        |> Map.ofList

    [<ReactComponent>]
    static member private StoryExample() =
        let selectedId, setSelectedId = React.useState ARCObjectWidget.StoryItemIdStudy
        let selectedMeta =
            ARCObjectWidget.StoryMeta
            |> Map.tryFind selectedId
            |> Option.defaultValue ("Unknown", "Unknown", "Unknown", "-", "No details available.")

        let selectedTitle, kind, role, previewTarget, description = selectedMeta

        let treePane =
            let items = ARCObjectWidget.StoryItems()

            Swate.Components.FileExplorer.FileExplorer(
                initialItems = items,
                selectedItemId = selectedId,
                onItemClick = (fun item -> setSelectedId item.Id)
            )

        let explorerPane =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-4 swt:h-full swt:overflow-auto"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            Html.span [ prop.className "swt:text-xs swt:uppercase swt:tracking-wide swt:opacity-60"; prop.text role ]
                            Html.h4 [ prop.className "swt:text-lg swt:font-semibold"; prop.text selectedTitle ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-200/40 swt:p-4"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "How This Example Behaves" ]
                            Html.ul [
                                prop.className "swt:list-disc swt:pl-5 swt:text-sm swt:space-y-1"
                                prop.children [
                                    Html.li "The left pane reuses the generic tree widget."
                                    Html.li "Clicking a node updates the middle and right panes."
                                    Html.li "Canonical nodes and reference nodes are both visible."
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex-1 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-4"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Selected Object" ]
                            Html.p [ prop.className "swt:text-sm swt:opacity-80"; prop.text description ]
                        ]
                    ]
                ]
            ]

        let detailsPane =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-3 swt:h-full"
                prop.children [
                    Html.div [
                        prop.className "swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-200/40 swt:p-3"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Properties" ]
                            Html.dl [
                                prop.className "swt:grid swt:grid-cols-[auto_1fr] swt:gap-x-3 swt:gap-y-2 swt:text-sm"
                                prop.children [
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Kind" ]
                                    Html.dd kind
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Role" ]
                                    Html.dd role
                                    Html.dt [ prop.className "swt:font-medium"; prop.text "Preview" ]
                                    Html.dd previewTarget
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:flex-1 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-3"
                        prop.children [
                            Html.h5 [ prop.className "swt:text-sm swt:font-semibold swt:mb-2"; prop.text "Notes" ]
                            Html.p [ prop.className "swt:text-sm swt:opacity-80"; prop.text description ]
                        ]
                    ]
                ]
            ]

        ARCObjectWidget.Main(treePane = treePane, explorerPane = explorerPane, detailsPane = detailsPane)

    [<ReactComponent>]
    static member Main(?treePane: ReactElement, ?explorerPane: ReactElement, ?detailsPane: ReactElement) =
        Html.div [
            prop.className ARCObjectWidget.WidgetContainerClass
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-1"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-base swt:font-bold"
                            prop.text "ARC Object Widget"
                        ]
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text "Placeholder layout for ARC object navigation and inspection."
                        ]
                    ]
                ]
                Html.div [
                    prop.className
                        "swt:grid swt:grid-cols-1 swt:lg:grid-cols-[minmax(16rem,20rem)_minmax(0,1fr)_minmax(14rem,18rem)] swt:gap-3 swt:flex-1 swt:min-h-0"
                    prop.children [
                        ARCObjectTree.Main(?content = treePane)
                        ARCObjectExplorer.Main(?content = explorerPane)
                        ARCObjectDetails.Main(?content = detailsPane)
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private EntryControls() =
        let context = WidgetContext.useWidgetController ()
        let isActive = context.isActive WidgetType.ARCObject

        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-center"
            prop.children [
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if isActive then "swt:btn-primary" else "swt:btn-outline"
                    ]
                    prop.text (if isActive then "Close ARC Object" else "Open ARC Object")
                    prop.onClick (fun _ -> context.toggleWidget WidgetType.ARCObject)
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        React.useEffectOnce (fun _ ->
            Size.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StorySize)
            Position.write (ARCObjectWidget.StoryPrefix, ARCObjectWidget.StoryPosition)
        )

        let widgets: Map<WidgetType, WidgetDefinition> =
            [
                WidgetType.ARCObject,
                {|
                    prefix = ARCObjectWidget.StoryPrefix
                    content = ARCObjectWidget.StoryExample()
                |}
            ]
            |> Map.ofList

        Widget.WidgetController(widgets, children = [ ARCObjectWidget.EntryControls() ])
