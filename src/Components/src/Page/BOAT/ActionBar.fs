namespace Components

open Fable.Core
open Feliz
open Feliz.DaisyUI

[<AutoOpen>]
module ActionBarExtensions =

    type prop with
        static member dataTip (value: string) =
            prop.custom ("data-tip", value)

[<Erase>]
type ActionBar =

    [<ReactComponent>]
    static member AnnotationModal (isActive: bool, toggleActive: bool -> unit, annoState, setAnnoState,  highlight, setHighlight) = 
        // Modal for displaying annotations

      Html.dialog [
          prop.className [
            "modal z-50"
            if isActive then "modal-open"
          ]          
          prop.children [
            Html.div [
                prop.className "modal-box w-fit max-w-4xl max-h-[80vh] overflow-y-auto"
                prop.children [
                Html.form [
                    prop.method "dialog"
                    prop.children [
                    Daisy.button.button [
                        prop.className "btn btn-sm btn-circle absolute right-2 top-2 z-50 h-5 w-5"
                        prop.text "✕"
                        prop.onClick (fun _ -> toggleActive(false))
                        
                    ]
                    ]
                ]
                Html.div [
                    prop.className "p-inherit "
                    prop.children [
                    match annoState = [] && isActive = true with
                    | true -> Html.h1 [prop.text "No annotations to display."]
                    | false ->
                        PreviewTable.table(annoState, setAnnoState, highlight, setHighlight)
                    ]
                ]
                ]           ]
          ]
        ]

    [<ReactComponent>]
    static member private Btn(icon: ReactElement, onClick: Browser.Types.MouseEvent -> unit, tooltip: string, ?disabled: bool, ?classNames: string, ?tooltipClassNames: string) =
        let disabled = defaultArg disabled false
        Html.div [
            prop.className [
                "tooltip"
                if tooltipClassNames.IsSome then tooltipClassNames.Value
            ]
            prop.dataTip tooltip
            prop.children [
                Html.button [
                    prop.className [
                        "btn btn-sm btn-square"
                        if classNames.IsSome then classNames.Value else "btn-primary"
                    ]
                    prop.onClick onClick
                    prop.disabled disabled
                    prop.children [
                        icon
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(annoState, setAnnoState, del: unit -> unit, fileName,  highlight, setHighlight) =
        let showAnnotationModal, setShowAnnotationModal = React.useState(false)
        let resultsIsEmpty = List.isEmpty annoState

        React.fragment [
            ReactDOM.createPortal(
            ActionBar.AnnotationModal(showAnnotationModal, setShowAnnotationModal, annoState, setAnnoState,  highlight, setHighlight),
            Browser.Dom.document.body
            )
            Html.div [
                prop.className "w-full bg-base-200/80 flex flex-row items-center gap-2 p-2 shadow-md sticky top-16 z-40"
                prop.id "action-bar"
                prop.children [
                    ActionBar.Btn(Html.i [prop.className "fa-solid fa-table-list"], (fun _ -> setShowAnnotationModal true), "Show Details", tooltipClassNames="tooltip-right" )
                    Html.div [
                        prop.className [
                            if resultsIsEmpty then "cursor-not-allowed"
                            else "tooltip tooltip-right"
                        ]
                        prop.dataTip "Download"
                        prop.children [
                            Daisy.dropdown [
                                Html.button [
                                    prop.className [
                                        "btn btn-sm btn-square"
                                        "btn-primary"
                                    ]
                                    prop.disabled resultsIsEmpty
                                    prop.children [
                                        Html.i [prop.className "fa-solid fa-download"]
                                    ]
                                ]
                                if not resultsIsEmpty then
                                    Daisy.dropdownContent [
                                        prop.className "p-2 shadow menu bg-base-100 rounded-box w-52"
                                        prop.tabIndex 0
                                        prop.children [
                                            Html.li [Html.a [
                                                prop.text "as .xlsx"
                                                prop.onClick (fun _ -> DownloadParser.downloadXlsxProm(fileName,annoState) |> Promise.start)
                                            ]]
                                            Html.li [Html.a [
                                                prop.text "as .json"
                                                prop.onClick (fun _ -> DownloadParser.downloadJsonProm(fileName,annoState) |> Promise.start)
                                            ]]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className [
                           "tooltip tooltip-left ml-auto"
                        ]
                        prop.dataTip "Delete"
                        prop.children [
                            Daisy.dropdown [
                                dropdown.end'
                                prop.children [

                                    Html.button [
                                        prop.className [
                                            "btn btn-sm btn-square"
                                            "btn-error"
                                        ]
                                        prop.children [
                                            Html.i [prop.className "fa-solid fa-trash"]
                                        ]
                                    ]

                                    Daisy.dropdownContent [
                                        prop.className "p-2 shadow menu bg-base-100 rounded-box w-52"
                                        prop.tabIndex 0
                                        prop.children [
                                            Html.li [Html.a [
                                                prop.text "delete all annotations"
                                                prop.onClick (fun _ -> 
                                                    setAnnoState []
                                                    setHighlight 
                                                        {
                                                            Keys = Map.empty
                                                            Terms = Map.empty
                                                            Values = Map.empty
                                                        }
                                                )
                                                if annoState = [] then prop.className"cursor-not-allowed"
                                            ]]
                                            Html.li [Html.a [
                                                prop.text "delete document"
                                                prop.onClick (fun _ -> del())
                                            ]]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    // ActionBar.Btn(Html.i [prop.className "fa-solid fa-trash"], (fun _ -> del()), "Delete", classNames="btn-error", tooltipClassNames="tooltip-left ml-auto" )
                ]
            ]
        ]