namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open ARCtrl
open JsonImport
open OfficeInterop.Core

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
    static member ToProtocolSearchElement (model: Model, setProtocolSearch, dispatch) =
        Daisy.button.button [
            prop.onClick(fun _ ->
                setProtocolSearch true
                if model.ProtocolState.TemplatesSelected.Length > 0 then
                    Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch
                    //{importTypeState with DeSelectedColumns = Array.empty} |> setImportTypeState
                else
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
    static member DisplaySelectedProtocolElements(selectedTemplate: Template option, templateIndex, selectedInformation, setSelectedInformation, dispatch, ?hasIcon: bool) =
        let hasIcon = defaultArg hasIcon true
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                if selectedTemplate.IsSome then
                    if hasIcon then
                        Html.i [prop.className "fa-solid fa-cog"]
                    Html.span $"Template: {selectedTemplate.Value.Name}"
                if selectedTemplate.IsSome then
                    SelectiveImportModal.TableWithImportColumnCheckboxes(selectedTemplate.Value.Table, templateIndex, selectedInformation, setSelectedInformation)
            ]
        ]

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="model"></param>
    /// <param name="importType"></param>
    /// <param name="dispatch"></param>
    static member AddTemplatesFromDBToTableButton(name, model: Model, importType, setImportType, protocolSearchState, setProtocolSearch, dispatch) =
        let addTemplates (model: Model, deselectedColumns) =
            let templates = model.ProtocolState.TemplatesSelected
            if templates.Length = 0 then
                failwith "No template selected!"
            if model.ProtocolState.TemplatesSelected.Length > 0 then
                let importTables = templates |> List.map (fun item -> item.Table) |> Array.ofList
                SpreadsheetInterface.AddTemplates(importTables, deselectedColumns, importType) |> InterfaceMsg |> dispatch
                Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch
                setProtocolSearch protocolSearchState
                { importType with DeselectedColumns = Set.empty } |> setImportType

        Html.div [
            prop.className "join flex flex-row justify-center gap-2"
            prop.children [
                let isDisabled = model.ProtocolState.TemplatesSelected.Length = 0
                ModalElements.Button(name, addTemplates, (model, importType.DeselectedColumns), isDisabled)
            ]
        ]

    /// <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    [<ReactComponent>]
    static member Main (model: Model, protocolSearchState, setProtocolSearch, importTypeStateData, dispatch) =
        let importTypeState, setImportTypeState = importTypeStateData
        let addTableImport = fun (i: int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::importTypeState.ImportTables |> List.distinctBy (fun table -> table.Index)
            {importTypeState with ImportTables = newImportTables} |> setImportTypeState
        let rmvTableImport = fun index ->
            {importTypeState with ImportTables = importTypeState.ImportTables |> List.filter (fun it -> it.Index <> index)} |> setImportTypeState
        React.fragment [
            Html.div [
                SelectiveTemplateFromDB.ToProtocolSearchElement(model, setProtocolSearch, dispatch)
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

            if model.ProtocolState.TemplatesSelected.Length > 0 then
                let templates = model.ProtocolState.TemplatesSelected
                for templateIndex in 0..templates.Length-1 do
                    let template = templates.[templateIndex]
                    SelectiveImportModal.TableImport(
                        templateIndex, template.Table, importTypeState, addTableImport, rmvTableImport, importTypeState, setImportTypeState, template.Name)
                Html.div [
                    SelectiveTemplateFromDB.AddTemplatesFromDBToTableButton(
                        "Import", model, importTypeState, setImportTypeState, protocolSearchState, setProtocolSearch, dispatch)
                ]
        ]
