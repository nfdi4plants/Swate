namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open ARCtrl
open FileImport
open OfficeInterop.Core

type SelectiveTemplateFromDB =

    // /// <summary>
    // ///
    // /// </summary>
    // /// <param name="adaptTableName"></param>
    // /// <param name="setAdaptTableName"></param>
    // /// <param name="templateName"></param>
    // static member CheckBoxForTakeOverTemplateName(adaptTableName: SelectiveImportConfig, setAdaptTableName: SelectiveImportConfig -> unit, templateName) =
    //     Html.label [
    //         prop.className "join flex flex-row centered gap-2"
    //         prop.children [
    //             Daisy.checkbox [
    //                 prop.type'.checkbox
    //                 prop.isChecked adaptTableName.TemplateName.IsSome
    //                 prop.onChange (fun (b: bool) ->
    //                     { adaptTableName with TemplateName = if b then Some templateName else None} |> setAdaptTableName)
    //             ]
    //             Html.text $"Use Template name: {templateName}"
    //         ]
    //     ]

    /// <summary>
    ///
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    static member ToProtocolSearchElement (model: Model, dispatch, ?className: Fable.Core.U2<string, string list>) =
        Daisy.button.button [
            prop.onClick(fun _ ->
                    Protocol.UpdateShowSearch true |> ProtocolMsg |> dispatch
                    if model.ProtocolState.TemplatesSelected.Length > 0 then
                        Protocol.RemoveSelectedProtocols |> ProtocolMsg |> dispatch
                )
            button.primary
            if className.IsSome then
                match className.Value with // this can also be done with !^ , but i was too lazy to open Fable.Core
                | Fable.Core.Case1 className ->
                    prop.className className
                | Fable.Core.U2.Case2 className ->
                    prop.className className
            prop.text "Browse database"
        ]

    // /// <summary>
    // ///
    // /// </summary>
    // /// <param name="selectedTemplate"></param>
    // /// <param name="templateIndex"></param>
    // /// <param name="selectionInformation"></param>
    // /// <param name="setSelectedColumns"></param>
    // /// <param name="dispatch"></param>
    // /// <param name="hasIcon"></param>
    // static member DisplaySelectedProtocolElements(selectedTemplate: Template option, templateIndex, selectedInformation, setSelectedInformation, dispatch, ?hasIcon: bool) =
    //     let hasIcon = defaultArg hasIcon true
    //     Html.div [
    //         prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
    //         prop.children [
    //             if selectedTemplate.IsSome then
    //                 if hasIcon then
    //                     Html.i [prop.className "fa-solid fa-cog"]
    //                 Html.span $"Template: {selectedTemplate.Value.Name}"
    //             if selectedTemplate.IsSome then
    //                 SelectiveImportModal.TableWithImportColumnCheckboxes(selectedTemplate.Value.Table, templateIndex, true, selectedInformation, setSelectedInformation)
    //         ]
    //     ]

    // /// <summary>
    // ///
    // /// </summary>
    // /// <param name="importState"></param>
    // /// <param name="activeTableIndex"></param>
    // /// <param name="existingOpt"></param>
    // /// <param name="appendTables"></param>
    // /// <param name="joinTables"></param>
    // static member CreateUpdatedTables (arcTables: ResizeArray<ArcTable>) (state: SelectiveImportConfig) (deselectedColumns: Set<int*int>) fullImport =
    //     [
    //         for importTable in state.ImportTables do
    //             let fullImport = defaultArg fullImport importTable.FullImport
    //             if importTable.FullImport = fullImport then
    //                 let deselectedColumnIndices = getDeselectedTableColumnIndices deselectedColumns importTable.Index
    //                 let sourceTable = arcTables.[importTable.Index]
    //                 let appliedTable = ArcTable.init(sourceTable.Name)

    //                 let finalTable = Table.selectiveTablePrepare appliedTable sourceTable deselectedColumnIndices
    //                 appliedTable.Join(finalTable, joinOptions=state.ImportType)
    //                 appliedTable
    //     ]
    //     |> ResizeArray

    /// <summary>
    ///
    /// </summary>
    /// <param name="name"></param>
    /// <param name="model"></param>
    /// <param name="importType"></param>
    /// <param name="dispatch"></param>
    static member AddTemplatesFromDBToTableButton(label: string, model: Model, dispatch) =
        let addTemplates () =
            let templates = model.ProtocolState.TemplatesSelected
            if templates.Length = 0 then
                failwith "No template selected!"
            else
                let importTables = templates |> List.map (fun item -> item.Table) |> Array.ofList
                SpreadsheetInterface.AddTemplates(importTables, model.ProtocolState.ImportConfig) |> InterfaceMsg |> dispatch

        let isDisabled = model.ProtocolState.TemplatesSelected.Length = 0
        ModalElements.Button(label, addTemplates, (), isDisabled, "grow")

    /// <summary>
    ///
    /// </summary>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    [<ReactComponent>]
    static member Main (model: Model, dispatch, isWidget) =
        let radioGroup = if isWidget then "Widget" else ""
        Html.div [
            prop.className "flex flex-col gap-2 lg:gap-4 overflow-hidden"
            prop.children [
                Html.div [
                    prop.className "grid grid-cols-2 gap-2"
                    prop.children [
                        SelectiveTemplateFromDB.ToProtocolSearchElement(
                            model,
                            dispatch,
                            Fable.Core.U2.Case2 ["grow"; if model.ProtocolState.TemplatesSelected.Length > 0 then "btn-outline"]
                        )
                        SelectiveTemplateFromDB.AddTemplatesFromDBToTableButton(
                            "Import",
                            model,
                            dispatch
                        )
                    ]
                ]
                if model.ProtocolState.TemplatesSelected.Length > 0 then
                    Html.div [
                        prop.className "flex gap-2 flex-col shrink overflow-y-auto"
                        prop.children [
                            SelectiveImportModal.RadioPluginsBox(
                                "Import Type",
                                "fa-solid fa-cog",
                                model.ProtocolState.ImportConfig.ImportType,
                                "importType" + radioGroup,
                                [|
                                    ARCtrl.TableJoinOptions.Headers, " Column Headers";
                                    ARCtrl.TableJoinOptions.WithUnit, " ..With Units";
                                    ARCtrl.TableJoinOptions.WithValues, " ..With Values";
                                |],
                                fun importType ->
                                    {model.ProtocolState.ImportConfig with ImportType = importType}
                                    |> Protocol.UpdateImportConfig |> ProtocolMsg |> dispatch
                            )

                            for templateIndex in 0..model.ProtocolState.TemplatesSelected.Length-1 do
                                let template = model.ProtocolState.TemplatesSelected.[templateIndex]
                                SelectiveImportModal.TableImport(
                                    templateIndex,
                                    template.Table,
                                    model,
                                    dispatch,
                                    template.Name
                                )
                        ]
                    ]
            ]
        ]
