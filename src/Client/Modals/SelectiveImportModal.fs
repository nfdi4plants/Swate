namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Swate.Components.Shared

open ARCtrl
open FileImport
open Swate.Components

type SelectiveImportModal =

    [<ReactComponent>]
    static member RadioPluginsBox(boxName, icon, importType: TableJoinOptions, radioGroupName, radioData: (TableJoinOptions * string)[], setImportType: TableJoinOptions -> unit) =

        let guid = React.useMemo(fun () -> System.Guid.NewGuid().ToString())
        let radioGroupName = radioGroupName + guid
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            ModalElements.RadioPlugin(radioGroupName, txt, isChecked, fun (b: bool) -> if b then setImportType target)
        ModalElements.Box (boxName, icon, React.fragment [
            Html.div [
                for i in 0..radioData.Length-1 do
                    myradio(radioData.[i])
            ]
        ])

    static member CheckBoxForTableColumnSelection(columns: CompositeColumn [], tableIndex, columnIndex, isActive: bool, model: Model.Model, dispatch) =
        let importConfig = model.ProtocolState.ImportConfig
        let isChecked = importConfig.DeselectedColumns |> Set.contains (tableIndex, columnIndex) |> not
        Html.div [
            prop.className "flex justify-center"
            prop.children [
                Daisy.checkbox [
                    prop.type'.checkbox
                    prop.disabled (not isActive)
                    prop.style [
                        style.height(length.perc 100)
                    ]
                    prop.isChecked isChecked
                    prop.onChange (fun (b: bool) ->
                        if columns.Length > 0 then
                            let nextImportConfig = importConfig.toggleDeselectColumn(tableIndex, columnIndex)
                            nextImportConfig |> Protocol.UpdateImportConfig |> ProtocolMsg |> dispatch
                    )
                ]
            ]
        ]

    static member TableWithImportColumnCheckboxes(table: ArcTable, tableIndex, isActive: bool, model: Model.Model, dispatch: Messages.Msg -> unit) =
        let columns = table.Columns
        Daisy.table [
            prop.children [
                Html.thead [
                    Html.tr [
                        for columnIndex in 0..columns.Length-1 do
                            Html.th [
                                Html.label [
                                    prop.className "join flex flex-row centered gap-2"
                                    prop.children [
                                        SelectiveImportModal.CheckBoxForTableColumnSelection(columns, tableIndex, columnIndex, isActive, model, dispatch)
                                        Html.text (columns.[columnIndex].Header.ToString())
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

    static member private MetadataImport(isActive: bool, setActive: bool -> unit, disArcFile: ArcFilesDiscriminate) =
        let name = string disArcFile
        ModalElements.Box (sprintf "%s Metadata" name, "fa-solid fa-lightbulb", React.fragment [
            Daisy.formControl [
                Daisy.label [
                    prop.className "cursor-pointer"
                    prop.children [
                        Daisy.checkbox [
                            prop.type'.checkbox
                            prop.onChange (fun (b:bool) -> setActive b)
                        ]
                        Html.span [
                            prop.className "text-sm"
                            prop.text "Import"
                        ]
                    ]
                ]
            ]
            Html.span [
                prop.className "text-warning bg-warning-content flex flex-row gap-2 justify-center items-center"
                prop.children [
                    Html.i [prop.className "fa-solid fa-exclamation-triangle"]
                    Html.text " Importing metadata will overwrite the current file."
                ]
            ]
        ],
        className = [if isActive then "!bg-info !text-info-content"]
    )

    [<ReactComponent>]
    static member TableImport(tableIndex: int, table0: ArcTable, model: Model.Model, importDataState, setImportDataState, dispatch, ?templateName) =
        let name = defaultArg templateName table0.Name
        let radiogroupId = React.useMemo(System.Guid.NewGuid)
        let radioGroup = "RADIO_GROUP" + table0.Name + string tableIndex + radiogroupId.ToString()
        let import = importDataState.ImportTables |> List.tryFind (fun it -> it.Index = tableIndex)
        let isActive = import.IsSome
        let isDisabled = importDataState.ImportMetadata
        let addTableImport = fun (i: int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::importDataState.ImportTables |> List.distinct
            {importDataState with ImportTables = newImportTables} |> setImportDataState
        let rmvTableImport = fun i ->
            let tableRemoved = importDataState.ImportTables |> List.filter (fun it -> it.Index <> i)
            {importDataState with ImportTables = tableRemoved} |> setImportDataState
        ModalElements.Box (name, "fa-solid fa-table", React.fragment [
            Html.div [
                ModalElements.RadioPlugin (radioGroup, "Import",
                    isActive && import.Value.FullImport,
                    (fun _ -> addTableImport tableIndex true),
                    isDisabled
                )
                ModalElements.RadioPlugin (radioGroup, "Append to active table",
                    isActive && not import.Value.FullImport,
                    (fun _ -> addTableImport tableIndex false),
                    isDisabled
                )
                ModalElements.RadioPlugin (radioGroup, "No Import",
                    not isActive,
                    (fun _ -> rmvTableImport tableIndex),
                    isDisabled
                )
            ]
            Daisy.collapse [
                Html.input [prop.type'.checkbox; prop.className "min-h-0 h-5"]

                Daisy.collapseTitle [
                    prop.className [
                        "p-1 min-h-0 h-5 text-sm font-bold space-x-2"
                        if isActive then "text-primary-content" else "text-success"
                    ]
                    prop.children [
                        Html.span (if isActive then "Select Columns" else "Preview Table")
                        Html.i [prop.className "fa-solid fa-magnifying-glass"]
                    ]
                ]
                Daisy.collapseContent [
                    prop.className "overflow-x-auto"
                    prop.children [
                        SelectiveImportModal.TableWithImportColumnCheckboxes(table0, tableIndex, isActive, model, dispatch)
                    ]
                ]
            ]
        ],
        className = [if isActive then "!bg-primary !text-primary-content"])

    [<ReactComponent>]
    static member Main (import: ArcFiles, model, dispatch, rmv) =
        let tables, disArcfile =
            match import with
            | Assay a -> a.Tables, ArcFilesDiscriminate.Assay
            | Study (s,_) -> s.Tables, ArcFilesDiscriminate.Study
            | Template t -> ResizeArray([t.Table]), ArcFilesDiscriminate.Template
            | Investigation _ -> ResizeArray(), ArcFilesDiscriminate.Investigation
        let importDataState, setImportDataState = React.useState(SelectiveImportConfig.init())
        let setMetadataImport = fun b ->
            {
                importDataState with ImportMetadata  = b;
            } |> setImportDataState
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    prop.className "w-4/5 flex flex-col @container/importModal"
                    prop.children [
                        Daisy.cardTitle [
                            prop.className "justify-between"
                            prop.children [
                                Html.p "Import"
                                Components.DeleteButton(props=[prop.onClick rmv])
                            ]
                        ]
                        Html.div [
                            prop.className "overflow-y-auto space-y-2"
                            prop.children [
                                SelectiveImportModal.RadioPluginsBox(
                                    "Import Type",
                                    "fa-solid fa-cog",
                                    importDataState.ImportType,
                                    "importType",
                                    [|
                                        ARCtrl.TableJoinOptions.Headers,    " Column Headers";
                                        ARCtrl.TableJoinOptions.WithUnit,   " ..With Units";
                                        ARCtrl.TableJoinOptions.WithValues, " ..With Values";
                                    |],
                                    fun importType -> {importDataState with ImportType = importType} |> setImportDataState)
                                SelectiveImportModal.MetadataImport(importDataState.ImportMetadata, setMetadataImport, disArcfile)
                                for ti in 0 .. (tables.Count-1) do
                                    let t = tables.[ti]
                                    SelectiveImportModal.TableImport(ti, t, model, importDataState, setImportDataState, dispatch)
                            ]
                        ]
                        Daisy.cardActions [
                            Daisy.button.button [
                                button.info
                                prop.style [style.marginLeft length.auto]
                                prop.text "Submit"
                                prop.onClick(fun e ->
                                    {| importState = importDataState; importedFile = import; deselectedColumns = importDataState.DeselectedColumns |} |> SpreadsheetInterface.ImportJson |> InterfaceMsg |> dispatch
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Main(import: ArcFiles, model, dispatch: Messages.Msg -> unit) =
        let rmv = Util.RMV_MODAL dispatch
        SelectiveImportModal.Main (import, model, dispatch, rmv = rmv)