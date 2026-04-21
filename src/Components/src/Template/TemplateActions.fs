module Swate.Components.Template.TemplateActions

open ARCtrl
open Swate.Components.Template.Types

/// Syncs the selected template IDs with the currently loaded templates, removing any IDs that no longer exist in the loaded templates.
let syncSelectedTemplateIds
    (loadedTemplates: Template[])
    (setSelectedTemplateIds: (Set<System.Guid> -> Set<System.Guid>) -> unit)
    =
    let templateIds =
        loadedTemplates |> Seq.map (fun template -> template.Id) |> Set.ofSeq

    setSelectedTemplateIds (fun selected -> selected |> Set.intersect templateIds)
    |> ignore

let toggleTemplateSelection (templateId: System.Guid) (selectedTemplateIds: Set<System.Guid>) =
    let isCurrentlySelected = selectedTemplateIds.Contains templateId

    if isCurrentlySelected then
        selectedTemplateIds.Remove templateId
    else
        selectedTemplateIds.Add templateId

let getTemplateImportAction
    (templateId: System.Guid)
    (templateImportDecisions: Map<System.Guid, TemplateImportAction>)
    =
    templateImportDecisions
    |> Map.tryFind templateId
    |> Option.defaultValue TemplateImportAction.AppendToActiveTable

let setTemplateImportAction
    (templateId: System.Guid)
    (importAction: TemplateImportAction)
    (setTemplateImportDecisions:
        (Map<System.Guid, TemplateImportAction> -> Map<System.Guid, TemplateImportAction>) -> unit)
    =
    setTemplateImportDecisions (fun decisions -> decisions.Add(templateId, importAction))
    |> ignore

let isTemplateColumnSelected
    (templateId: System.Guid)
    (columnIndex: int)
    (deselectedTemplateColumns: Set<System.Guid * int>)
    =
    deselectedTemplateColumns.Contains(templateId, columnIndex) |> not

let toggleTemplateColumnSelection
    (templateId: System.Guid)
    (columnIndex: int)
    (setDeselectedTemplateColumns: (Set<System.Guid * int> -> Set<System.Guid * int>) -> unit)
    =
    setDeselectedTemplateColumns (fun deselected ->
        if deselected.Contains(templateId, columnIndex) then
            deselected.Remove(templateId, columnIndex)
        else
            deselected.Add(templateId, columnIndex)
    )
    |> ignore

let selectAllTemplateColumns
    (templateId: System.Guid)
    (setDeselectedTemplateColumns: (Set<System.Guid * int> -> Set<System.Guid * int>) -> unit)
    =
    setDeselectedTemplateColumns (fun deselected ->
        deselected
        |> Set.filter (fun (candidateTemplateId, _) -> candidateTemplateId <> templateId)
    )
    |> ignore

let unselectAllTemplateColumns
    (template: Template)
    (setDeselectedTemplateColumns: (Set<System.Guid * int> -> Set<System.Guid * int>) -> unit)
    =
    let allTemplateColumns =
        template.Table.Columns
        |> Seq.mapi (fun columnIndex _ -> template.Id, columnIndex)
        |> Set.ofSeq

    setDeselectedTemplateColumns (fun deselected -> Set.union deselected allTemplateColumns)
    |> ignore

let selectedTemplatesForImport
    (templates: Template[])
    (selectedTemplateIds: Set<System.Guid>)
    (templateImportDecisions: Map<System.Guid, TemplateImportAction>)
    =
    templates
    |> Array.filter (fun template -> selectedTemplateIds.Contains template.Id)
    |> Array.choose (fun template ->
        match getTemplateImportAction template.Id templateImportDecisions with
        | TemplateImportAction.NoImport -> None
        | nextAction -> Some(template, nextAction)
    )

let selectedTemplateIndexById (selectedTemplatesForImport: (Template * TemplateImportAction)[]) =
    selectedTemplatesForImport
    |> Array.mapi (fun tableIndex (template, _) -> template.Id, tableIndex)
    |> Map.ofArray

let deselectedColumnsForImport
    (deselectedTemplateColumns: Set<System.Guid * int>)
    (selectedTemplateIndexById: Map<System.Guid, int>)
    =
    deselectedTemplateColumns
    |> Seq.choose (fun (templateId, columnIndex) ->
        selectedTemplateIndexById
        |> Map.tryFind templateId
        |> Option.map (fun tableIndex -> tableIndex, columnIndex)
    )
    |> Set.ofSeq

let importTablesConfig (selectedTemplatesForImport: (Template * TemplateImportAction)[]) =
    selectedTemplatesForImport
    |> Array.mapi (fun tableIndex (_, action) ->
        ({
            Index = tableIndex
            FullImport = (action = TemplateImportAction.ImportAsNewTable)
        }
        : ImportTable)
    )
    |> Array.toList

let buildSelectiveImportConfig
    (importType: TableJoinOptions)
    (importTablesConfig: ImportTable list)
    (deselectedColumns: Set<int * int>)
    =
    {
        SelectiveImportConfig.init () with
            ImportType = importType
            ImportMetadata = false
            ImportTables = importTablesConfig
            DeselectedColumns = deselectedColumns
    }