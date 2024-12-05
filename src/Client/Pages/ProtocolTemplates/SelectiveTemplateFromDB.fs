namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared
open Types.TableImport

open ARCtrl
open JsonImport

type AdaptTableName = {
        UseTemplateName: bool
    }
    with
        static member init() =
            {
                UseTemplateName = false
            }

type SelectiveTemplateFromDBModal =

    static member CheckBoxForTakeOverTemplateName(adaptTableName: AdaptTableName, setAdaptTableName: AdaptTableName -> unit, templateName) =
        Html.label [
            prop.className "join flex flex-row centered gap-2"
            prop.children [
                Daisy.checkbox [
                    prop.type'.checkbox
                    prop.isChecked adaptTableName.UseTemplateName
                    prop.onChange (fun (b: bool) ->
                        { adaptTableName with UseTemplateName = b } |> setAdaptTableName)
                ]
                Html.text $"Use Template name: {templateName}"
            ]
        ]

    static member ToProtocolSearchElement (model: Model) dispatch =
        Daisy.button.button [
            prop.onClick(fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch} |> dispatch)
            button.primary
            button.block
            prop.text "Browse database"
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
                    SelectiveImportModal.TableWithImportColumnCheckboxes(model.ProtocolState.TemplateSelected.Value.Table, selectionInformation, setSelectedColumns)
            ]
        ]

    static member AddFromDBToTableButton (model: Model) selectionInformation importType useTemplateName dispatch =
        let addTemplate (templatePot: Template option, selectedColumns) =
            if model.ProtocolState.TemplateSelected.IsNone then
                failwith "No template selected!"
            if templatePot.IsSome then
                let table = templatePot.Value.Table
                SpreadsheetInterface.AddTemplate(table, selectedColumns, importType, useTemplateName) |> InterfaceMsg |> dispatch
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
    static member Main (model: Model, dispatch) =
        let length =
            if model.ProtocolState.TemplateSelected.IsSome then
                model.ProtocolState.TemplateSelected.Value.Table.Columns.Length
            else 0
        let selectedColumns, setSelectedColumns = React.useState(SelectedColumns.init length)
        let importTypeState, setImportTypeState = React.useState(SelectiveImportModalState.init)
        let useTemplateName, setUseTemplateName = React.useState(AdaptTableName.init)
        SidebarComponents.SidebarLayout.LogicContainer [
            Html.div [
                SelectiveTemplateFromDBModal.ToProtocolSearchElement model dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then                    
                Html.div [
                    SelectiveImportModal.RadioPluginsBox(
                        "Import Type",
                        "fa-solid fa-cog",
                        importTypeState.ImportType,
                        "importType",
                        [|
                            ARCtrl.TableJoinOptions.Headers, " Column Headers";
                            ARCtrl.TableJoinOptions.WithUnit, " ..With Units";
                            ARCtrl.TableJoinOptions.WithValues, " ..With Values";
                        |],
                        fun importType -> {importTypeState with ImportType = importType} |> setImportTypeState
                    )
                ]
                Html.div [
                    ModalElements.Box(
                        "Rename Table",
                        "fa-solid fa-cog",
                        SelectiveTemplateFromDBModal.CheckBoxForTakeOverTemplateName(useTemplateName, setUseTemplateName, model.ProtocolState.TemplateSelected.Value.Name))
                ]
                Html.div [
                    ModalElements.Box(
                        model.ProtocolState.TemplateSelected.Value.Name,
                        "fa-solid fa-cog",
                        SelectiveTemplateFromDBModal.displaySelectedProtocolElements(model, selectedColumns, setSelectedColumns, dispatch, false))
                ]
            Html.div [
                SelectiveTemplateFromDBModal.AddFromDBToTableButton model selectedColumns importTypeState useTemplateName.UseTemplateName dispatch
            ]
        ]
