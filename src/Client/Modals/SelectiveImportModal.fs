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

    static member private ImportTypeRadio(importType: TableJoinOptions, setImportType: TableJoinOptions -> unit) =
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            Daisy.formControl [
                Daisy.label [
                    prop.children [
                        Daisy.radio [
                            prop.name "importType"
                            prop.isChecked isChecked
                            prop.onChange (fun (b:bool) -> if b then setImportType target)
                        ]
                        Daisy.labelText txt
                    ]
                ]
            ]
        Daisy.card [
            Daisy.cardBody [
                Daisy.label [
                    Daisy.labelText [
                        Html.i [prop.className "fa-solid fa-cog"]
                        Html.text (" Import Type")
                    ]
                ]
                Html.div [
                    prop.className "is-flex is-justify-content-space-between"
                    prop.children [
                        myradio(ARCtrl.TableJoinOptions.Headers, " Column Headers")
                        myradio(ARCtrl.TableJoinOptions.WithUnit, " ..With Units")
                        myradio(ARCtrl.TableJoinOptions.WithValues, " ..With Values")
                    ]
                ]
            ]
        ]

    static member private MetadataImport(isActive: bool, setActive: bool -> unit, disArcFile: ArcFilesDiscriminate) =
        let name = string disArcFile
        Daisy.card [
            if isActive then prop.className "bg-info"
            prop.children [
                Html.div [
                    Daisy.label [
                        Daisy.labelText [
                            Html.i [prop.className "fa-solid fa-lightbulb"]
                            Html.textf " %s Metadata" name
                        ]
                    ]
                    Daisy.formControl [
                        Daisy.label [
                            Daisy.checkbox [
                                prop.type'.checkbox
                                prop.onChange (fun (b:bool) -> setActive b)
                            ]
                            Daisy.labelText " Import"
                        ]
                    ]
                    Html.span [
                        prop.className "text-warning"
                        prop.text "Importing metadata will overwrite the current file."
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private TableImport(index: int, table: ArcTable, state: SelectiveImportModalState, addTableImport: int -> bool -> unit, rmvTableImport: int -> unit) =
        let showData, setShowData = React.useState(false)
        let name = table.Name
        let radioGroup = "radioGroup_" + name
        let import = state.ImportTables |> List.tryFind (fun it -> it.Index = index)
        let isActive = import.IsSome
        let disableAppend = state.ImportMetadata
        Daisy.card [
            if isActive then prop.className "bg-success"
            prop.children [
                Html.div [
                    Daisy.label [
                        Daisy.labelText [
                            Html.i [prop.className "fa-solid fa-table"]
                            Html.span (" " + name)
                            Daisy.button.label [
                                if showData then button.active
                                button.sm
                                prop.onClick (fun _ -> setShowData (not showData))
                                prop.style [style.float'.right; style.cursor.pointer]
                                prop.children [
                                    Html.i [
                                        prop.style [style.transitionProperty "transform"; style.transitionDuration (System.TimeSpan.FromSeconds 0.35)]
                                        prop.className ["fa-solid"; "fa-angle-down"; if showData then "fa-rotate-180"]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        Daisy.formControl [
                            let isInnerActive = isActive && import.Value.FullImport
                            Daisy.label [
                                Daisy.radio [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> addTableImport index true)
                                ]
                                Daisy.labelText " Import"
                            ]
                        ]
                        Daisy.formControl [
                            let isInnerActive = isActive && not import.Value.FullImport
                            Daisy.label [
                                Daisy.radio [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    if disableAppend then prop.disabled true
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> addTableImport index true)
                                ]
                                Daisy.labelText " Append to active table"
                            ]
                        ]
                        Daisy.formControl [
                            let isInnerActive = not isActive
                            Daisy.label [
                                Daisy.radio [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    if disableAppend then prop.disabled true
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> addTableImport index true)
                                ]
                                Daisy.labelText " No Import"
                            ]
                        ]
                    ]
                ]
                if showData then
                    Html.div [
                        prop.className "overflow-x-auto"
                        prop.children [
                            Daisy.table [
                                prop.children [
                                    Html.thead [
                                        Html.tr [
                                            for c in table.Headers do
                                                Html.th (c.ToString())
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
                        ]
                    ]
            ]
        ]

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
            {state with ImportMetadata = b; ImportTables = state.ImportTables |> List.map (fun t -> {t with FullImport = true})} |> setState
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
                Daisy.card [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Daisy.cardBody [
                            Daisy.cardActions [
                                prop.className "justify-end"
                                prop.children [
                                    Components.DeleteButton(props=[prop.onClick rmv])
                                ]
                            ]
                            Daisy.cardTitle "Import"
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
        ]
