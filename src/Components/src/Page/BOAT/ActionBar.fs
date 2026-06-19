namespace Components

open Fable.Core
open Feliz
open ARCtrl.Json
open Thoth.Json.Core
open Types
open ARCtrl

[<AutoOpen>]
module ActionBarExtensions =

    type prop with
        static member dataTip(value: string) = prop.custom ("data-tip", value)

module private ActionBarHelper =

    let icon (iconClassName: string) =
        Html.i [ prop.className $"swt:iconify {iconClassName} swt:size-4" ]

    let annotationBodyText (cell: CompositeCell) =
        match cell with
        | CompositeCell.Term oa -> oa.NameText
        | CompositeCell.Unitized(value, oa) ->
            if System.String.IsNullOrWhiteSpace oa.NameText then
                value
            elif System.String.IsNullOrWhiteSpace value then
                oa.NameText
            else
                $"{value} ({oa.NameText})"
        | CompositeCell.FreeText text -> text
        | CompositeCell.Data data -> data.NameText

    let removeAt index annotations =
        annotations
        |> List.indexed
        |> List.choose (fun (currentIndex, annotation) ->
            if currentIndex = index then
                None
            else
                Some annotation
        )

    let downloadFileName (fileName: string) extension =
        let fallbackName = "boat-annotations"

        let baseName =
            if System.String.IsNullOrWhiteSpace fileName then
                fallbackName
            else
                let parts = fileName.Split('.')

                if parts.Length > 1 then
                    parts.[0]
                else
                    fileName

        $"{baseName}-annotations{extension}"

module private DownloadParser =

    open ActionBarHelper

    let downloadJsonProm (fileName: string, annotations: Annotation list) = promise {
        let json =
            annotations
            |> List.map encoderAnno
            |> Encode.list
            |> Encode.toJsonString 2

        Swate.Components.Util.Download.downloadFromString (downloadFileName fileName ".json", json)
    }

    let downloadTsvProm (fileName: string, annotations: Annotation list) = promise {
        let header = "Key\tType\tValue"

        let rows =
            annotations
            |> List.map (fun annotation ->
                [
                    annotation.Search.Key.NameText
                    string annotation.Search.KeyType
                    annotationBodyText annotation.Search.Body
                ]
                |> List.map (fun value -> value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " "))
                |> String.concat "\t"
            )

        let content = (header :: rows) |> String.concat "\n"
        Swate.Components.Util.Download.downloadFromString (downloadFileName fileName ".tsv", content)
    }

module private PreviewTable =

    open ActionBarHelper

    let table
        (
            annoState: Annotation list,
            setAnnoState: Annotation list -> unit,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        ) =
        Html.div [
            prop.className "swt:overflow-x-auto"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-sm"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th [ prop.text "Key" ]
                                Html.th [ prop.text "Type" ]
                                Html.th [ prop.text "Value" ]
                                Html.th [ prop.className "swt:w-12"; prop.text "" ]
                            ]
                        ]
                        Html.tbody [
                            for index, annotation in annoState |> List.indexed do
                                let bodyText = annotationBodyText annotation.Search.Body

                                Html.tr [
                                    Html.td [
                                        prop.className "swt:max-w-56 swt:truncate"
                                        prop.title annotation.Search.Key.NameText
                                        prop.text annotation.Search.Key.NameText
                                    ]
                                    Html.td [ prop.text (string annotation.Search.KeyType) ]
                                    Html.td [
                                        prop.className "swt:max-w-72 swt:truncate"
                                        prop.title bodyText
                                        prop.text bodyText
                                    ]
                                    Html.td [
                                        Html.button [
                                            prop.className "swt:btn swt:btn-ghost swt:btn-xs swt:btn-square"
                                            prop.title "Remove"
                                            prop.onClick (fun _ ->
                                                setAnnoState (removeAt index annoState)

                                                setHighlight {
                                                    Keys = highlight.Keys |> Map.remove annotation.Height
                                                    Terms = highlight.Terms |> Map.remove annotation.Height
                                                    Values = highlight.Values |> Map.remove annotation.Height
                                                }
                                            )
                                            prop.children [ icon "swt:fluent--delete-20-regular" ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]
        ]

[<Erase>]
type ActionBar =

    [<ReactComponent>]
    static member AnnotationModal
        (
            isActive: bool,
            toggleActive: bool -> unit,
            annoState: Annotation list,
            setAnnoState: Annotation list -> unit,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        ) =
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
                                    prop.children [ ActionBarHelper.icon "swt:fluent--dismiss-20-regular" ]
                                    prop.onClick (fun _ -> toggleActive false)
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "swt:p-inherit"
                            prop.children [
                                if annoState = [] && isActive then
                                    Html.h1 [ prop.text "No annotations to display." ]
                                else
                                    PreviewTable.table (annoState, setAnnoState, highlight, setHighlight)
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
    static member Main
        (
            annoState: Annotation list,
            setAnnoState: Annotation list -> unit,
            del: unit -> unit,
            fileName: string,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        ) =
        let showAnnotationModal, setShowAnnotationModal = React.useState false
        let resultsIsEmpty = List.isEmpty annoState

        React.Fragment [
            ReactDOM.createPortal(
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
                    "swt:w-full swt:bg-base-200/80 swt:flex swt:flex-row swt:items-center swt:gap-2 swt:p-2 swt:shadow-md swt:sticky swt:top-16 swt:z-40"
                prop.id "action-bar"
                prop.children [
                    ActionBar.Btn(
                        ActionBarHelper.icon "swt:fluent--table-24-regular",
                        (fun _ -> setShowAnnotationModal true),
                        "Show Details",
                        tooltipClassNames = "swt:tooltip-right"
                    )
                    Html.div [
                        prop.className [
                            if resultsIsEmpty then
                                "swt:cursor-not-allowed"
                            else
                                "swt:tooltip swt:tooltip-right"
                        ]
                        prop.dataTip "Download"
                        prop.children [
                            Html.div [
                                prop.className "swt:dropdown"
                                prop.children [
                                    Html.button [
                                        prop.tabIndex 0
                                        prop.className "swt:btn swt:btn-sm swt:btn-square swt:btn-primary"
                                        prop.disabled resultsIsEmpty
                                        prop.children [
                                            ActionBarHelper.icon "swt:fluent--arrow-download-24-regular"
                                        ]
                                    ]
                                    if not resultsIsEmpty then
                                        Html.ul [
                                            prop.className
                                                "swt:dropdown-content swt:z-50 swt:p-2 swt:shadow swt:menu swt:bg-base-100 swt:rounded-box swt:w-52"
                                            prop.tabIndex 0
                                            prop.children [
                                                Html.li [
                                                    Html.button [
                                                        prop.text "as .tsv"
                                                        prop.onClick (fun _ ->
                                                            DownloadParser.downloadTsvProm (fileName, annoState)
                                                            |> Promise.start
                                                        )
                                                    ]
                                                ]
                                                Html.li [
                                                    Html.button [
                                                        prop.text "as .json"
                                                        prop.onClick (fun _ ->
                                                            DownloadParser.downloadJsonProm (fileName, annoState)
                                                            |> Promise.start
                                                        )
                                                    ]
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "swt:tooltip swt:tooltip-left swt:ml-auto"
                        prop.dataTip "Delete"
                        prop.children [
                            Html.div [
                                prop.className "swt:dropdown swt:dropdown-end"
                                prop.children [
                                    Html.button [
                                        prop.tabIndex 0
                                        prop.className "swt:btn swt:btn-sm swt:btn-square swt:btn-error"
                                        prop.children [ ActionBarHelper.icon "swt:fluent--delete-24-regular" ]
                                    ]
                                    Html.ul [
                                        prop.className
                                            "swt:dropdown-content swt:z-50 swt:p-2 swt:shadow swt:menu swt:bg-base-100 swt:rounded-box swt:w-52"
                                        prop.tabIndex 0
                                        prop.children [
                                            Html.li [
                                                Html.button [
                                                    prop.text "delete all annotations"
                                                    if annoState = [] then
                                                        prop.className "swt:cursor-not-allowed"
                                                    prop.onClick (fun _ ->
                                                        setAnnoState []

                                                        setHighlight {
                                                            Keys = Map.empty
                                                            Terms = Map.empty
                                                            Values = Map.empty
                                                        }
                                                    )
                                                ]
                                            ]
                                            Html.li [
                                                Html.button [
                                                    prop.text "delete document"
                                                    prop.onClick (fun _ -> del ())
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
