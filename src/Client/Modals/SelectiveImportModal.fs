namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared
open Types.TableImport

open ARCtrl
open JsonImport
open Components

type SelectiveImportModal =

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
                                            SelectiveImportModal.CheckBoxForTableColumnSelection(columns, i, selectionInformation.Value, setSelectedColumns.Value)
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
    static member private TableImport(index: int, table0: ArcTable, state: SelectiveImportModalState, addTableImport: int -> bool -> unit, rmvTableImport: int -> unit, selectedColumns, setSelectedColumns) =
        let name = table0.Name
        let radioGroup = "radioGroup_" + name
        let import = state.ImportTables |> List.tryFind (fun it -> it.Index = index)
        let isActive = import.IsSome
        let isDisabled = state.ImportMetadata
        ModalElements.Box (name, "fa-solid fa-table", React.fragment [
            Html.div [
                ModalElements.RadioPlugin (radioGroup, "Import",
                    isActive && import.Value.FullImport,
                    (fun (b:bool) -> addTableImport index true),
                    isDisabled
                )
                ModalElements.RadioPlugin (radioGroup, "Append to active table",
                    isActive && not import.Value.FullImport,
                    (fun (b:bool) -> addTableImport index false),
                    isDisabled
                )
                ModalElements.RadioPlugin (radioGroup, "No Import",
                    not isActive,
                    (fun (b:bool) -> rmvTableImport index),
                    isDisabled
                )
            ]
            Daisy.collapse [
                Html.input [prop.type'.checkbox; prop.className "min-h-0 h-5"]
                Daisy.collapseTitle [
                    prop.className "p-1 min-h-0 h-5 text-sm"                    
                    prop.text (if isActive then "Select Columns" else "Preview Table")
                ]
                Daisy.collapseContent [
                    prop.className "overflow-x-auto"
                    prop.children [
                        if isActive then
                            SelectiveImportModal.TableWithImportColumnCheckboxes(table0, selectedColumns, setSelectedColumns)
                        else
                            SelectiveImportModal.TableWithImportColumnCheckboxes(table0)
                    ]
                ]
            ]
        ],
        className = [if isActive then "!bg-primary !text-primary-content"])

    [<ReactComponent>]
    static member Main (import: ArcFiles, dispatch, rmv) =
        let state, setState = React.useState(SelectiveImportModalState.init)
        let tables, disArcfile =
            match import with
            | Assay a -> a.Tables, ArcFilesDiscriminate.Assay
            | Study (s,_) -> s.Tables, ArcFilesDiscriminate.Study
            | Template t -> ResizeArray([t.Table]), ArcFilesDiscriminate.Template
            | Investigation _ -> ResizeArray(), ArcFilesDiscriminate.Investigation
        let setMetadataImport = fun b ->
            if b then
                {
                    state with
                        ImportMetadata  = true;
                        ImportTables    = [for ti in 0 .. tables.Count-1 do {ImportTable.Index = ti; ImportTable.FullImport = true}]
                } |> setState
            else
                SelectiveImportModalState.init() |> setState
        let addTableImport = fun (i: int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::state.ImportTables |> List.distinct
            {state with ImportTables = newImportTables} |> setState
        let rmvTableImport = fun i ->
            {state with ImportTables = state.ImportTables |> List.filter (fun it -> it.Index <> i)} |> setState
        let selectedColumns =
            tables
            |> Array.ofSeq
            |> Array.map (fun t -> React.useState(SelectedColumns.init t.Columns.Length))
        Daisy.modal.div [
            modal.active
            prop.children [
                Daisy.modalBackdrop [ prop.onClick rmv ]
                Daisy.modalBox.div [
                    prop.className "w-4/5 overflow-y-auto flex flex-col @container/importModal gap-2"
                    prop.children [
                        Daisy.cardTitle [
                            prop.className "justify-between"
                            prop.children [
                                Html.p "Import"
                                Components.DeleteButton(props=[prop.onClick rmv])
                            ]
                        ]
                        SelectiveImportModal.RadioPluginsBox(
                            "Import Type",
                            "fa-solid fa-cog",
                            state.ImportType,
                            [|
                                ARCtrl.TableJoinOptions.Headers,    " Column Headers";
                                ARCtrl.TableJoinOptions.WithUnit,   " ..With Units";
                                ARCtrl.TableJoinOptions.WithValues, " ..With Values";
                            |],
                            fun importType -> {state with ImportType = importType} |> setState)
                        SelectiveImportModal.MetadataImport(state.ImportMetadata, setMetadataImport, disArcfile)
                        for ti in 0 .. (tables.Count-1) do
                            let t = tables.[ti]
                            let selectedColumns, setSelectedColumns = fst selectedColumns.[ti], snd selectedColumns.[ti]
                            SelectiveImportModal.TableImport(ti, t, state, addTableImport, rmvTableImport, selectedColumns, setSelectedColumns)
                        Daisy.cardActions [
                            Daisy.button.button [
                                button.info
                                prop.style [style.marginLeft length.auto]
                                prop.text "Submit"
                                prop.onClick(fun e ->
                                    {| importState = state; importedFile = import; selectedColumns = selectedColumns |} |> SpreadsheetInterface.ImportJson |> InterfaceMsg |> dispatch
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]

    static member Main(import: ArcFiles, dispatch: Messages.Msg -> unit) =
        let rmv = Util.RMV_MODAL dispatch
        SelectiveImportModal.Main (import, dispatch, rmv = rmv)