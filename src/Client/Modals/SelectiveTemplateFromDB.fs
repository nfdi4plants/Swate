namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared
open Shared.DTOs.SelectedColumnsModalDto

open ARCtrl
open JsonImport
open Components

type SelectiveTemplateFromDBModal =

    static member RadioPluginsBox(boxName, icon, importType: TableJoinOptions, radioData: (TableJoinOptions * string)[], setImportType: TableJoinOptions -> unit) =
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            ModalElements.RadioPlugin("importType", txt, isChecked, fun (b: bool) -> if b then setImportType target)
        ModalElements.Box (boxName, icon, React.fragment [
            Html.div [
                for i in 0..radioData.Length-1 do
                    myradio(radioData.[i])
            ]
        ])

    static member CheckBoxForTableColumnSelection(columns: CompositeColumn [], index, selectionInformation: SelectedColumns, setSelectedColumns: SelectedColumns -> unit) =
        Html.div [
            prop.style [style.display.flex; style.justifyContent.center]
            prop.children [
                Daisy.checkbox [
                    prop.type'.checkbox
                    prop.style [
                        style.height(length.perc 100)
                    ]
                    prop.isChecked
                        (if selectionInformation.Columns.Length > 0 then
                            selectionInformation.Columns.[index]
                        else true)
                    prop.onChange (fun (b: bool) ->
                        if columns.Length > 0 then
                            let selectedData = selectionInformation.Columns
                            selectedData.[index] <- b
                            {selectionInformation with Columns = selectedData} |> setSelectedColumns)
                ]
            ]
        ]

    static member TableWithImportColumnCheckboxes(table: ArcTable, ?selectionInformation: SelectedColumns, ?setSelectedColumns: SelectedColumns -> unit) =
        let columns = table.Columns
        let displayCheckBox =
            //Determine whether to display checkboxes or not
            selectionInformation.IsSome && setSelectedColumns.IsSome                    
        Daisy.table [
            prop.children [
                Html.thead [
                    Html.tr [
                        for i in 0..columns.Length-1 do                            
                            Html.th [
                                Html.label [
                                    prop.className "join flex flex-row centered gap-2"
                                    prop.children [
                                        if displayCheckBox then
                                            SelectiveTemplateFromDBModal.CheckBoxForTableColumnSelection(columns, i, selectionInformation.Value, setSelectedColumns.Value)
                                        Html.text (columns.[i].Header.ToString())
                                        Html.div [
                                            prop.onClick (fun e ->
                                                if columns.Length > 0 && selectionInformation.IsSome then
                                                    let selectedData = selectionInformation.Value.Columns
                                                    selectedData.[i] <- not selectedData.[i]
                                                    {selectionInformation.Value with Columns = selectedData} |> setSelectedColumns.Value)
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]

                Html.tbody [
                    for ri in 0 .. (table.RowCount-1) do
                        let row = table.GetRow(ri, true)
                        Html.tr [
                            for c in row do
                                Html.td (c.ToString())
                        ]
                ]
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
                    SelectiveTemplateFromDBModal.TableWithImportColumnCheckboxes(model.ProtocolState.TemplateSelected.Value.Table, selectionInformation, setSelectedColumns)
            ]
        ]

    static member AddFromDBToTableButton (model: Model) selectionInformation importType dispatch =
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
    static member Main (model: Model, dispatch) =
        let length =
            if model.ProtocolState.TemplateSelected.IsSome then
                model.ProtocolState.TemplateSelected.Value.Table.Columns.Length
            else 0
        let selectedColumns, setSelectedColumns = React.useState(SelectedColumns.init length)
        let importTypeState, setImportTypeState = React.useState(SelectiveImportModalState.init)
        ModalElements.LogicContainer [
            Html.div [
                SelectiveTemplateFromDBModal.ToProtocolSearchElement model dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then                    
                Html.div [
                    SelectiveTemplateFromDBModal.RadioPluginsBox(
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
                ]
                Html.div [
                    ModalElements.Box(
                        model.ProtocolState.TemplateSelected.Value.Name,
                        "",
                        SelectiveTemplateFromDBModal.displaySelectedProtocolElements(model, selectedColumns, setSelectedColumns, dispatch, false))
                ]
            Html.div [
                SelectiveTemplateFromDBModal.AddFromDBToTableButton model selectedColumns importTypeState dispatch
            ]
        ]
