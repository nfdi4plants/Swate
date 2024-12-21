namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open ARCtrl
open JsonImport

type SelectiveTemplateFromDB =

    /// <summary>
    /// 
    /// </summary>
    /// <param name="adaptTableName"></param>
    /// <param name="setAdaptTableName"></param>
    /// <param name="templateName"></param>
    static member CheckBoxForTakeOverTemplateName(adaptTableName: SelectiveImportModalState, setAdaptTableName: SelectiveImportModalState -> unit, templateName) =
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    static member ToProtocolSearchElement(model: Model) setProtocolSearch dispatch =
        Daisy.button.button [
            prop.onClick(fun _ ->
                setProtocolSearch true
                UpdateModel model |> dispatch)
            button.primary
            button.block
            prop.text "Browse database"
        ]

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selectedTemplate"></param>
    /// <param name="templateIndex"></param>
    /// <param name="selectionInformation"></param>
    /// <param name="setSelectedColumns"></param>
    /// <param name="dispatch"></param>
    /// <param name="hasIcon"></param>
    static member DisplaySelectedProtocolElements(selectedTemplate: Template option, templateIndex, selectionInformation: SelectiveImportModalState, setSelectedColumns: SelectiveImportModalState -> unit, dispatch, ?hasIcon: bool) =
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="model"></param>
    /// <param name="selectionInformation"></param>
    /// <param name="importType"></param>
    /// <param name="useTemplateName"></param>
    /// <param name="dispatch"></param>
    static member AddFromDBToTableButton name (model: Model) selectionInformation importType setImportType useTemplateName protocolSearchState setProtocolSearch dispatch =
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
                        prop.onClick (fun _ ->
                            Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch
                            {importType with SelectedColumns = Array.empty} |> setImportType
                            setProtocolSearch protocolSearchState)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="model"></param>
    /// <param name="importType"></param>
    /// <param name="dispatch"></param>
    static member AddTemplatesFromDBToTableButton name (model: Model) importType setImportType protocolSearchState setProtocolSearch dispatch =
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
                let selectedColumnValues = importType.SelectedColumns
                ModalElements.Button(name, addTemplates, (model, selectedColumnValues), isDisabled)
                if model.ProtocolState.TemplatesSelected.Length > 0 then
                    Daisy.button.a [
                        button.outline
                        prop.onClick (fun _ ->
                            Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch
                            {importType with SelectedColumns = Array.empty} |> setImportType
                            setProtocolSearch protocolSearchState)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    /// <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    [<ReactComponent>]
    static member Main (model: Model, protocolSearchState, setProtocolSearch, importTypeState, setImportTypeState, dispatch) =
        let addTableImport = fun (i: int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::importTypeState.ImportTables |> List.distinctBy (fun x -> x.Index)
            {importTypeState with ImportTables = newImportTables} |> setImportTypeState
        let rmvTableImport = fun i ->
            {importTypeState with ImportTables = importTypeState.ImportTables |> List.filter (fun it -> it.Index <> i)} |> setImportTypeState
        React.fragment [
            Html.div [
                SelectiveTemplateFromDB.ToProtocolSearchElement model setProtocolSearch dispatch
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
                        SelectiveTemplateFromDB.CheckBoxForTakeOverTemplateName(importTypeState, setImportTypeState, template.Name))
                ]
                Html.div [
                    ModalElements.Box(
                        template.Name,
                        "fa-solid fa-cog",
                        SelectiveTemplateFromDB.DisplaySelectedProtocolElements(Some template, 0, importTypeState, setImportTypeState, dispatch, false))
                ]
                Html.div [
                    SelectiveTemplateFromDB.AddFromDBToTableButton "Add template" model importTypeState importTypeState setImportTypeState importTypeState.TemplateName protocolSearchState setProtocolSearch dispatch
                ]
            else if model.ProtocolState.TemplatesSelected.Length > 1 then
                let templates = model.ProtocolState.TemplatesSelected
                for templateIndex in 0..templates.Length-1 do
                    let template = templates.[templateIndex]
                    SelectiveImportModal.TableImport(templateIndex, template.Table, importTypeState, addTableImport, rmvTableImport, importTypeState, setImportTypeState, template.Name)
                Html.div [
                    SelectiveTemplateFromDB.AddTemplatesFromDBToTableButton "Add templates" model importTypeState setImportTypeState protocolSearchState setProtocolSearch dispatch
                ]
        ]
