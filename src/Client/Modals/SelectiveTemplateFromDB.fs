namespace Modals.Template

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared
open Shared.DTOs.SelectedColumnsModalDto

open ARCtrl
open JsonImport
open Components

open Modals
open Modals.ModalElements

type SelectiveTemplateFromDBModal =

    static member private LogicContainer (children: ReactElement list) =
        Html.div [
            prop.className "relative flex p-4 animated-border shadow-md gap-4 flex-col" //experimental
            prop.children children
        ]

    [<ReactComponent>]
    static member displaySelectedProtocolElements (model: Model, selectionInformation: SelectedColumns, setSelectedColumns: SelectedColumns -> unit, dispatch, ?hasIcon: bool) =
        let hasIcon = defaultArg hasIcon true
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                if model.ProtocolState.TemplateSelected.IsSome then
                    if hasIcon then
                        Html.i [prop.className "fa-solid fa-cog"]
                    Html.span $"Template: {model.ProtocolState.TemplateSelected.Value.Name}"
                if model.ProtocolState.TemplateSelected.IsSome then
                    ModalElements.TableWithImportColumnCheckboxes(model.ProtocolState.TemplateSelected.Value.Table, selectionInformation, setSelectedColumns)
            ]
        ]

    static member addFromDBToTableButton (model: Model) selectionInformation importType dispatch =
        let addTemplate (templatePot: Template option, selectedColumns) =
            if model.ProtocolState.TemplateSelected.IsNone then
                failwith "No template selected!"
            if templatePot.IsSome then
                let table = templatePot.Value.Table
                SpreadsheetInterface.AddTemplate(table, selectedColumns, importType) |> InterfaceMsg |> dispatch
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
    static member Main(model: Model, dispatch) =
        let length =
            if model.ProtocolState.TemplateSelected.IsSome then
                model.ProtocolState.TemplateSelected.Value.Table.Columns.Length
            else 0
        let selectedColumns, setSelectedColumns = React.useState(SelectedColumns.init length)
        let importTypeState, setImportTypeState = React.useState(SelectiveImportModalState.init)
        SelectiveTemplateFromDBModal.LogicContainer [
            Html.div [
                Daisy.button.button [
                    prop.onClick(fun _ -> UpdateModel { model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch } |> dispatch)
                    button.primary
                    button.block
                    prop.text "Browse database"
                ]
                if model.ProtocolState.TemplateSelected.IsSome then
                    Html.div [
                        Html.div [
                            ModalElements.RadioPluginsBox(
                                "Import Type",
                                "fa-solid fa-cog",
                                importTypeState.ImportType,
                                [|
                                    ARCtrl.TableJoinOptions.Headers, " Column Headers";
                                    ARCtrl.TableJoinOptions.WithUnit, " ..With Units";
                                    ARCtrl.TableJoinOptions.WithValues, " ..With Values";
                                |],
                                fun importType -> {importTypeState with ImportType = importType} |> setImportTypeState
                            )
                            ModalElements.Box(
                                model.ProtocolState.TemplateSelected.Value.Name,
                                "",
                                SelectiveTemplateFromDBModal.displaySelectedProtocolElements(model, selectedColumns, setSelectedColumns, dispatch, false))
                        ]
                    ]
                Html.div [
                    SelectiveTemplateFromDBModal.addFromDBToTableButton model selectedColumns importTypeState dispatch
                ]
            ]
        ]
