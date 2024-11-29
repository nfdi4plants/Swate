namespace Modals.Template

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open JsonImport
open Components

open Modals
open Modals.ModalElements

type SelectedColumns = {
    Columns: bool []
}
with
    static member init(length) =
        {
            Columns = Array.init length (fun _ -> true)
        }

type SelectiveTemplateFromDBModal = 

    static member private LogicContainer (children: ReactElement list) =
        Html.div [
            prop.className "relative flex p-4 animated-border shadow-md gap-4 flex-col" //experimental
            prop.children children
        ]

    [<ReactComponent>]
    static member displaySelectedProtocolEle (model: Model) (selectionInformation:SelectedColumns) (setSelectedColumns:SelectedColumns -> unit) dispatch =
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                Daisy.table [
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "Selection"
                                Html.th "Column"
                                Html.th "Column TAN"
                            ]
                        ]
                        Html.tbody [
                            let length =
                                if model.ProtocolState.TemplateSelected.IsSome then model.ProtocolState.TemplateSelected.Value.Table.Columns.Length-1
                                else 0
                            for i in 0..length do
                                let column = model.ProtocolState.TemplateSelected.Value.Table.Columns.[i]
                                yield
                                    Html.tr [
                                        Html.div [
                                            prop.style [style.display.flex; style.justifyContent.center]
                                            prop.children [
                                                Daisy.checkbox [
                                                    prop.type'.checkbox
                                                    prop.isChecked
                                                        (if selectionInformation.Columns.Length > 0 then
                                                            selectionInformation.Columns.[i]
                                                        else true)
                                                    prop.onChange (fun (b: bool) ->
                                                        if selectionInformation.Columns.Length > 0 then
                                                            let selectedData = selectionInformation.Columns
                                                            selectedData.[i] <- b
                                                            {selectionInformation with Columns = selectedData} |> setSelectedColumns)
                                                ]
                                            ]
                                        ]
                                        Html.td (column.Header.ToString())
                                        Html.td (if column.Header.IsTermColumn then column.Header.ToTerm().TermAccessionShort else "-")
                                    ]
                        ]
                    ]
                ]
            ]
        ]

    static member addFromDBToTableButton (model: Model) selectionInformation dispatch =
        let addTemplate (templatePot: Template option, selectedColumns) =
            if model.ProtocolState.TemplateSelected.IsNone then
                failwith "No template selected!"
            if templatePot.IsSome then
                let table = templatePot.Value.Table
                SpreadsheetInterface.AddTemplate(table, selectedColumns) |> InterfaceMsg |> dispatch
        Html.div [
            prop.className "join flex flex-row justify-center gap-2"
            prop.children [
                ModalElements.Button("Add template", addTemplate, (model.ProtocolState.TemplateSelected, selectionInformation.Columns), model.ProtocolState.TemplateSelected.IsNone)
                if model.ProtocolState.TemplateSelected.IsSome then
                    Daisy.button.a [
                        button.outline
                        prop.onClick (fun _ -> Protocol.RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model:Model, dispatch) =
        let length =
            if model.ProtocolState.TemplateSelected.IsSome then
                model.ProtocolState.TemplateSelected.Value.Table.Columns.Length
            else 0
        let selectedColumns, setSelectedColumns = React.useState(SelectedColumns.init length)
        SelectiveTemplateFromDBModal.LogicContainer [
            Html.div [
                Daisy.button.button [
                    prop.onClick(fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch} |> dispatch)
                    button.primary
                    button.block
                    prop.text "Browse database"
                ]
            ]
            Html.div [
                SelectiveTemplateFromDBModal.addFromDBToTableButton model selectedColumns dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then
                Html.div [
                    SelectiveTemplateFromDBModal.displaySelectedProtocolEle model selectedColumns setSelectedColumns dispatch
                ]
                Html.div [
                    SelectiveTemplateFromDBModal.addFromDBToTableButton model selectedColumns dispatch
                ]
        ]
