namespace Modals

open Feliz
open Feliz.DaisyUI
open Model
open Messages
open Shared

open ARCtrl
open JsonImport
open Components

type SelectiveImportModal =

    static member private Radio(radioGroup: string, txt:string, isChecked, onChange: bool -> unit, ?isDisabled: bool) =
        let isDisabled = defaultArg isDisabled false
        Daisy.formControl [
            Daisy.label [
                prop.className [
                    "cursor-pointer transition-colors"
                    if isDisabled then
                        "!cursor-not-allowed"
                    else
                        "hover:bg-base-300"
                ]
                prop.children [
                    Daisy.radio [
                        prop.disabled isDisabled
                        radio.xs
                        prop.name radioGroup
                        prop.isChecked isChecked
                        prop.onChange onChange
                    ]
                    Html.span [
                        prop.className "text-sm"
                        prop.text txt
                    ]
                ]
            ]
        ]
    static member private Box (title: string, icon: string, content: ReactElement, ?className: string list) =
        Html.div [
            prop.className [
                "rounded shadow p-2 flex flex-col gap-2 border"
                if className.IsSome then
                    className.Value |> String.concat " "
            ]
            prop.children [
                Html.h3 [
                    prop.className "font-semibold gap-2 flex flex-row items-center"
                    prop.children [
                        Html.i [prop.className icon]
                        Html.span title
                    ]
                ]
                content
            ]
        ]

    static member private ImportTypeRadio(importType: TableJoinOptions, setImportType: TableJoinOptions -> unit) =
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            SelectiveImportModal.Radio("importType", txt, isChecked, fun (b:bool) -> if b then setImportType target)
        SelectiveImportModal.Box ("Import Type", "fa-solid fa-cog", React.fragment [
            Html.div [
                myradio(ARCtrl.TableJoinOptions.Headers, " Column Headers")
                myradio(ARCtrl.TableJoinOptions.WithUnit, " ..With Units")
                myradio(ARCtrl.TableJoinOptions.WithValues, " ..With Values")
            ]
        ])

    static member private MetadataImport(isActive: bool, setActive: bool -> unit, disArcFile: ArcFilesDiscriminate) =
        let name = string disArcFile
        SelectiveImportModal.Box (sprintf "%s Metadata" name, "fa-solid fa-lightbulb", React.fragment [
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
    static member private TableImport(index: int, table0: ArcTable, state: SelectiveImportModalState, addTableImport: int -> bool -> unit, rmvTableImport: int -> unit) =
        let name = table0.Name
        let radioGroup = "radioGroup_" + name
        let import = state.ImportTables |> List.tryFind (fun it -> it.Index = index)
        let isActive = import.IsSome
        let isDisabled = state.ImportMetadata
        SelectiveImportModal.Box (name, "fa-solid fa-table", React.fragment [
            Html.div [
                SelectiveImportModal.Radio (radioGroup, "Import",
                    isActive && import.Value.FullImport,
                    (fun (b:bool) -> addTableImport index true),
                    isDisabled
                )
                SelectiveImportModal.Radio (radioGroup, "Append to active table",
                    isActive && not import.Value.FullImport,
                    (fun (b:bool) -> addTableImport index false),
                    isDisabled
                )
                SelectiveImportModal.Radio (radioGroup, "No Import",
                    not isActive,
                    (fun (b:bool) -> rmvTableImport index),
                    isDisabled
                )
            ]
            Daisy.collapse [
                Html.input [prop.type'.checkbox; prop.className "min-h-0 h-5"]
                Daisy.collapseTitle [
                    prop.className "p-1 min-h-0 h-5 text-sm"
                    prop.text "Preview Table"
                ]
                Daisy.collapseContent [
                    prop.className "overflow-x-auto"
                    prop.children [
                        Daisy.table [
                            table.xs
                            prop.children [
                                Html.thead [
                                    Html.tr [
                                        for c in table0.Headers do
                                            Html.th (c.ToString())
                                    ]
                                ]
                                Html.tbody [
                                    for ri in 0 .. (table0.RowCount-1) do
                                        let row = table0.GetRow(ri, true)
                                        Html.tr [
                                            for c in row do
                                                Html.td (c.ToString())
                                        ]
                                ]
                            ]
                        ]
                    ]
                ]
        ]],
            className = [if isActive then "!bg-primary !text-primary-content"]
        )

    [<ReactComponent>]
    static member Main (import: ArcFiles) (model: Spreadsheet.Model) dispatch (rmv: _ -> unit) =
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
                        ImportMetadata = true;
                        ImportTables = [ for ti in 0 .. tables.Count-1 do {ImportTable.Index = ti; ImportTable.FullImport = true}]
                } |> setState
            else
                SelectiveImportModalState.init() |> setState
        let addTableImport = fun (i:int) (fullImport: bool) ->
            let newImportTable: ImportTable = {Index = i; FullImport = fullImport}
            let newImportTables = newImportTable::state.ImportTables |> List.distinct
            {state with ImportTables = newImportTables} |> setState
        let rmvTableImport = fun i ->
            {state with ImportTables = state.ImportTables |> List.filter (fun it -> it.Index <> i)} |> setState
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
                        SelectiveImportModal.ImportTypeRadio(state.ImportType, fun it -> {state with ImportType = it} |> setState)
                        SelectiveImportModal.MetadataImport(state.ImportMetadata, setMetadataImport, disArcfile)
                        for ti in 0 .. (tables.Count-1) do
                            let t = tables.[ti]
                            SelectiveImportModal.TableImport(ti, t, state, addTableImport, rmvTableImport)
                        Daisy.cardActions [
                            Daisy.button.button [
                                button.info
                                prop.style [style.marginLeft length.auto]
                                prop.text "Submit"
                                prop.onClick(fun e ->
                                    {| importState = state; importedFile = import|} |> SpreadsheetInterface.ImportJson |> InterfaceMsg |> dispatch
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]
