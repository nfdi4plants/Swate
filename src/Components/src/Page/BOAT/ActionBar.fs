namespace Components

open Fable.Core
open Feliz
open Types


[<AutoOpen>]
module ActionBarExtensions =

    type prop with
        static member dataTip(value: string) = prop.custom ("data-tip", value)

[<Erase>]
type ActionBar =

    [<ReactComponent>]
    static member AnnotationModal
        (isActive: bool, toggleActive: bool -> unit, annoState, setAnnoState, highlight, setHighlight)
        =
        // Modal for displaying annotations

        Html.dialog [
            prop.className [
                "swt:modal swt:z-50"
                if isActive then
                    "swt:modal-open"
            ]
            prop.children [
                Html.div [
                    prop.className "swt:modal-box swt:w-fit swt:max-w-4xl swt:max-h-[80vh] swt:overflow-y-auto"
                    prop.children [
                        Html.form [
                            prop.method "dialog"
                            prop.children [
                                Html.button [
                                    prop.className
                                        "swt:btn swt:btn-sm swt:btn-circle swt:absolute swt:right-2 swt:top-2 swt:z-50 swt:h-5 swt:w-5"
                                    prop.text "✕"
                                    prop.onClick (fun _ -> toggleActive (false))
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:p-inherit "
                            prop.children [
                                match annoState = [] && isActive = true with
                                | true -> Html.h1 [ prop.text "No annotations to display." ]
                                | false -> PreviewTable.table(annoState, setAnnoState, highlight, setHighlight)
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private Btn
        (
            icon: ReactElement,
            onClick: Browser.Types.MouseEvent -> unit,
            tooltip: string,
            ?disabled: bool,
            ?classNames: string,
            ?tooltipClassNames: string
        ) =
        let disabled = defaultArg disabled false

        Html.div [
            prop.className [
                "swt:tooltip"
                if tooltipClassNames.IsSome then
                    tooltipClassNames.Value
            ]
            prop.dataTip tooltip
            prop.children [
                Html.button [
                    prop.className [
                        "swt:btn swt:btn-sm swt:btn-square"
                        if classNames.IsSome then
                            classNames.Value
                        else
                            "swt:btn-primary"
                    ]
                    prop.onClick onClick
                    prop.disabled disabled
                    prop.children [ icon ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(annoState, setAnnoState, del: unit -> unit, fileName, highlight, setHighlight) =
        let showAnnotationModal, setShowAnnotationModal = React.useState (false)
        let resultsIsEmpty = List.isEmpty annoState

        let exportDropdownOpen, setExportDropdownOpen = React.useState false
        let deleteDropdownOpen, setDeleteDropdownOpen = React.useState false

        React.Fragment [
            ReactDOM.createPortal (
                ActionBar.AnnotationModal(
                    showAnnotationModal,
                    setShowAnnotationModal,
                    annoState,
                    setAnnoState,
                    highlight,
                    setHighlight
                ),
                Browser.Dom.document.body
            )
            Html.div [
                prop.className
                    "swt:w-full swt:flex swt:flex-row swt:items-center swt:gap-2 swt:p-2 swt:shadow-md swt:sticky swt:top-0 swt:z-40"
                prop.id "action-bar"
                prop.children [
                    ActionBar.Btn(
                        Html.i [ prop.className "swt:iconify swt:fluent--table-24-regular swt:size-4" ],
                        (fun _ -> setShowAnnotationModal true),
                        "Show Summary",
                        tooltipClassNames = "swt:tooltip-right"
                    )
                    Html.div [
                        Swate.Components.Primitive.Dropdown.Dropdown.Main(
                            isOpen = exportDropdownOpen,
                            setIsOpen = setExportDropdownOpen,
                            toggle =
                                Html.div [
                                    prop.className "swt:tooltip swt:tooltip-right"
                                    prop.dataTip "Export"
                                    prop.children [
                                        Html.button [
                                            prop.className [ "swt:btn swt:btn-sm swt:btn-square"; "swt:btn-primary" ]
                                            prop.onClick (fun _ -> setExportDropdownOpen (not exportDropdownOpen))
                                            prop.children [
                                                Html.i [
                                                    prop.className
                                                        "swt:iconify swt:fluent--arrow-export-24-regular swt:size-4"
                                                ]
                                            ]
                                        ]
                                    ]
                                ],
                            children =
                                React.Fragment [
                                    Html.li [
                                        Html.a [
                                            prop.text "add to existing table in this ARC"
                                            prop.onClick (fun _ -> ())
                                        ]
                                    ]
                                    Html.li [
                                        Html.a [
                                            prop.text "as .xlsx"
                                            prop.onClick (fun _ -> DownloadParser.downloadXlsxProm(fileName,annoState) |> Promise.start)
                                        ]
                                    ]
                                    Html.li [
                                        Html.a [
                                            prop.text "as .json"
                                            prop.onClick (fun _ -> DownloadParser.downloadJsonProm(fileName,annoState) |> Promise.start)
                                        ]
                                    ]
                                ]
                        )
                    ]
                    Html.div [
                        prop.className "swt:ml-auto"
                        prop.children [
                            Swate.Components.Primitive.Dropdown.Dropdown.Main(
                                isOpen = deleteDropdownOpen,
                            setIsOpen = setDeleteDropdownOpen,
                            toggle =
                                Html.div [
                                    prop.className "swt:tooltip swt:tooltip-left"
                                    prop.dataTip "Delete"
                                    prop.children [
                                        Html.button [
                                            prop.className [ "swt:btn swt:btn-sm swt:btn-square"; "swt:btn-error" ]
                                            prop.onClick (fun _ -> setDeleteDropdownOpen (not deleteDropdownOpen))
                                            prop.children [
                                                Html.i [
                                                    prop.className
                                                        "swt:iconify swt:fluent--delete-24-regular swt:size-4"
                                                ]
                                            ]
                                        ]
                                    ]
                                ],
                            children =
                                React.Fragment [
                                    Html.li [
                                        Html.a [
                                            prop.text "delete all annotations"
                                            prop.onClick (fun _ ->
                                                setAnnoState []
                                                setHighlight {
                                                    Keys = Map.empty
                                                    Terms = Map.empty
                                                    Values = Map.empty
                                                }
                                            )
                                            if annoState = [] then
                                                prop.className "swt:cursor-not-allowed"
                                        ]
                                    ]
                                    Html.li [
                                        Html.a [
                                            prop.text "delete document"
                                            prop.onClick (fun _ -> del ())
                                        ]
                                    ]
                                ],
                            dropdownClassName = "swt:dropdown-end"
                        )
                    ]
                ]
            ]
        ]
    ]
