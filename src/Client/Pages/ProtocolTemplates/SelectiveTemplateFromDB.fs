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
        TemplateName: string option
    }
    with
        static member init() =
            {
                TemplateName = None
            }

type SelectiveTemplateFromDB =

    static member CheckBoxForTakeOverTemplateName(adaptTableName: AdaptTableName, setAdaptTableName: AdaptTableName -> unit, templateName) =
        Html.label [
            prop.className "join flex flex-row centered gap-2"
            prop.children [
                Daisy.checkbox [
                    prop.type'.checkbox
                    prop.isChecked adaptTableName.TemplateName.IsSome
                    prop.onChange (fun (b: bool) ->
                        { adaptTableName with TemplateName = if b then Some templateName else None} |> setAdaptTableName)
                ]
                Html.text $"Use Template name: {templateName}"
            ]
        ]

    static member ToProtocolSearchElement(model: Model) dispatch =
        Daisy.button.button [
            prop.onClick(fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch} |> dispatch)
            button.primary
            button.block
            prop.text "Browse database"
        ]

    [<ReactComponent>]
    static member displaySelectedProtocolElements(selectedTemplate: Template option, templateIndex, selectionInformation: SelectedColumns, setSelectedColumns: SelectedColumns -> unit, dispatch, ?hasIcon: bool) =
        let hasIcon = defaultArg hasIcon true
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                if selectedTemplate.IsSome then
                    if hasIcon then
                        Html.i [prop.className "fa-solid fa-cog"]
                    Html.span $"Template: {selectedTemplate.Value.Name}"
                if selectedTemplate.IsSome then
                    SelectiveImportModal.TableWithImportColumnCheckboxes(selectedTemplate.Value.Table, templateIndex, selectionInformation, setSelectedColumns)
            ]
        ]

    static member AddFromDBToTableButton name (model: Model) selectionInformation importType useTemplateName dispatch =
        let addTemplate (model: Model, selectedColumns) =
            let template =
                if model.ProtocolState.TemplatesSelected.Length = 0 then
                    failwith "No template selected!"
                else
                    model.ProtocolState.TemplatesSelected.Head
            SpreadsheetInterface.AddTemplate(template.Table, selectedColumns, importType, useTemplateName) |> InterfaceMsg |> dispatch
        Html.div [
            prop.className "flex flex-row justify-center gap-2"
            prop.children [
                let isDisabled = model.ProtocolState.TemplatesSelected.Length = 0
                ModalElements.Button(name, addTemplate, (model, selectionInformation.SelectedColumns.[0]), isDisabled)
                if model.ProtocolState.TemplatesSelected.Length > 0 then
                    Daisy.button.a [
                        button.outline
                        prop.onClick (fun _ -> Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    static member AddTemplatesFromDBToTableButton name (model: Model) (selectionInformation: SelectedColumns) importType dispatch =
        let addTemplates (model: Model, selectedColumns) =
            let templates = model.ProtocolState.TemplatesSelected
            if templates.Length = 0 then
                failwith "No template selected!"
            if model.ProtocolState.TemplatesSelected.Length > 1 then
                let importTables = templates |> List.map (fun item -> item.Table) |> Array.ofList
                SpreadsheetInterface.AddTemplates(importTables, selectedColumns, importType) |> InterfaceMsg |> dispatch
        Html.div [
            prop.className "join flex flex-row justify-center gap-2"
            prop.children [
                let isDisabled = model.ProtocolState.TemplatesSelected.Length = 0
                let selectedColumnValues = selectionInformation.SelectedColumns
                ModalElements.Button(name, addTemplates, (model, selectedColumnValues), isDisabled)
                if model.ProtocolState.TemplatesSelected.Length > 0 then
                    Daisy.button.a [
                        button.outline
                        prop.onClick (fun _ -> Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        let selectedColumns, setSelectedColumns =
            let columns =
                model.ProtocolState.TemplatesSelected
                |> Array.ofSeq
                |> Array.map (fun t -> Array.init t.Table.Columns.Length (fun _ -> true))
            React.useState(SelectedColumns.init columns)
        let useTemplateNameState, setUseTemplateNameState = React.useState(AdaptTableName.init)
        let importTypeState, setImportTypeState = React.useState(SelectiveImportModalState.init)
        let addTableImport = fun (i: int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::importTypeState.ImportTables |> List.distinctBy (fun x -> x.Index)
            {importTypeState with ImportTables = newImportTables} |> setImportTypeState
        let rmvTableImport = fun i ->
            {importTypeState with ImportTables = importTypeState.ImportTables |> List.filter (fun it -> it.Index <> i)} |> setImportTypeState
        SidebarComponents.SidebarLayout.LogicContainer [
            Html.div [
                SelectiveTemplateFromDB.ToProtocolSearchElement model dispatch
            ]
            if model.ProtocolState.TemplatesSelected.Length > 0 then                
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
            if model.ProtocolState.TemplatesSelected.Length = 1 then
                let template = model.ProtocolState.TemplatesSelected.Head
                Html.div [
                    ModalElements.Box(
                        "Rename Table",
                        "fa-solid fa-cog",
                        SelectiveTemplateFromDB.CheckBoxForTakeOverTemplateName(useTemplateNameState, setUseTemplateNameState, template.Name))
                ]
                Html.div [
                    ModalElements.Box(
                        template.Name,
                        "fa-solid fa-cog",
                        SelectiveTemplateFromDB.displaySelectedProtocolElements(Some template, 0, selectedColumns, setSelectedColumns, dispatch, false))
                ]
                Html.div [
                    SelectiveTemplateFromDB.AddFromDBToTableButton "Add template" model selectedColumns importTypeState useTemplateNameState.TemplateName dispatch
                ]
            else if model.ProtocolState.TemplatesSelected.Length > 1 then
                let templates = model.ProtocolState.TemplatesSelected
                for templateIndex in 0..templates.Length-1 do
                    let template = templates.[templateIndex]
                    SelectiveImportModal.TableImport(templateIndex, template.Table, importTypeState, addTableImport, rmvTableImport, selectedColumns, setSelectedColumns, template.Name)
                Html.div [
                    SelectiveTemplateFromDB.AddTemplatesFromDBToTableButton "Add templates" model selectedColumns importTypeState dispatch
                ]
        ]
