namespace Swate.Components

open System
open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Fable.Core

type EditConfig =        

    static member CreateColumnTab () =
        Html.div [
        ]

    static member UpdateColumnTab () =
        Html.div [
        ]

    static member EditTabs (columnIndex, table, selectedTab, setSelectedTab, setColumn, rmv) =
        Html.div [
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:tabs swt:tabs-box swt:my-1 swt:w-fit swt:mx-auto swt:*:[--tab-bg:var(--color-secondary)] swt:*:[&.swt\:tab-active]:text-secondary-content"
                        prop.children [
                            Html.div [
                                prop.className [
                                    "swt:tab"
                                    if selectedTab = 0 then
                                        "swt:tab-active"
                                ]
                                prop.text "Edit Column"
                                prop.onClick (fun _ -> setSelectedTab 0)
                            ]
                            Html.div [
                                prop.className [
                                    "swt:tab"
                                    if selectedTab = 1 then
                                        "swt:tab-active"
                                ]
                                prop.text "Create Column"
                                prop.onClick (fun _ -> setSelectedTab 1)
                            ]
                            Html.div [
                                prop.className [
                                    "swt:tab"
                                    if selectedTab = 2 then
                                        "swt:tab-active"
                                ]
                                prop.text "Update Column"
                                prop.onClick (fun _ -> setSelectedTab 2)
                            ]
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.children [
                    match selectedTab with
                    | 0 -> EditColumnModal.EditColumnModal(columnIndex, table, setColumn, rmv)
                    | 1 -> CreateColumnModal.CreateColumnModal(columnIndex, table, setColumn, rmv)
                    | 2 -> UpdateColumnModal.UpdateColumnModal(columnIndex, table, setColumn, rmv)
                    | _ -> Html.none
                ]
            ]
        ]

    [<ReactComponent>]
    static member CompositeCellEditModal
        (
            columnIndex,
            table: ArcTable,
            setArcTable,
            rmv: unit -> unit,
            ?debug: bool
        ) =

        let selectedTab, setSelectedTab = React.useState(0)

        let setColumn =
            fun (column: CompositeColumn) ->
                table.UpdateColumn(columnIndex, column.Header, column.Cells)
                setArcTable table

        let debubString =
            if debug.IsSome && debug.Value then
                Some "Edit"
            else
                None
        BaseModal.BaseModal(
            (fun _ -> rmv ()),
            header = Html.div "Edit Column",
            content =
                React.fragment [
                    EditConfig.EditTabs(columnIndex, table, selectedTab, setSelectedTab, setColumn, rmv)
                ],
            contentClassInfo = "swt:space-y-2 swt:py-2",
            ?debug = debubString
        )