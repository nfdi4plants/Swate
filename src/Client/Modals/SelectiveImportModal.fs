namespace Modals

open Feliz
open Feliz.Bulma
open Model
open Messages
open Shared

open ARCtrl
open JsonImport

type SelectiveImportModal =

    static member private ImportTypeRadio(importType: TableJoinOptions, setImportType: TableJoinOptions -> unit) =
        let myradio(target: TableJoinOptions, txt: string) =
            let isChecked = importType = target
            Html.label [
                prop.className "radio is-unselectable"
                prop.children [
                    Html.input [
                        prop.type'.radio
                        prop.name "importType"
                        prop.isChecked isChecked
                        prop.onChange (fun (b:bool) -> if b then setImportType target)
                    ]
                    Html.text txt
                ]
            ]

        Bulma.box [
            Bulma.field.div [
                Bulma.label [
                    Html.i [prop.className "fa-solid fa-cog"]
                    Html.text (" Import Type")
                ]
                Bulma.control.div [
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
        Bulma.box [
            if isActive then color.hasBackgroundInfo
            prop.children [
                Bulma.field.div [
                    Bulma.label [
                        Html.i [prop.className "fa-solid fa-lightbulb"]
                        Html.textf " %s Metadata" name
                    ]
                    Bulma.control.div [
                        Html.label [
                            prop.className "checkbox is-unselectable"
                            prop.children [
                                Html.input [
                                    prop.type'.checkbox
                                    prop.onChange (fun (b:bool) -> setActive b)
                                ]
                                Html.text " Import"
                            ]
                        ]
                    ]
                    Html.span [
                        color.hasTextWarning
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
        Bulma.box [
            if isActive then color.hasBackgroundSuccess
            prop.children [
                Bulma.field.div [
                    Bulma.label [
                        Html.i [prop.className "fa-solid fa-table"]
                        Html.span (" " + name)
                        Bulma.button.span [
                            if showData then button.isActive
                            button.isSmall
                            prop.onClick (fun _ -> setShowData (not showData))
                            prop.style [style.float'.right; style.cursor.pointer]
                            prop.children [
                                Bulma.icon [
                                    icon.isSmall
                                    prop.children [
                                        Html.i [
                                            prop.style [style.transitionProperty "transform"; style.transitionDuration (System.TimeSpan.FromSeconds 0.35)]
                                            prop.className ["fa-solid"; "fa-angle-down"; if showData then "fa-rotate-180"]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Bulma.control.div [
                        Html.label [
                            let isInnerActive = isActive && import.Value.FullImport
                            prop.className "radio is-unselectable"
                            prop.children [
                                Html.input [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> addTableImport index true)
                                ]
                                Html.text " Import"
                            ]
                        ]
                        Html.label [
                            let isInnerActive = isActive && not import.Value.FullImport
                            prop.className "radio is-unselectable"
                            prop.children [
                                Html.input [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    if disableAppend then prop.disabled true
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> addTableImport index false)
                                ]
                                Html.text " Append to active table"
                            ]
                        ]
                        Html.label [
                            let isInnerActive = not isActive
                            prop.className "radio is-unselectable"
                            prop.children [
                                Html.input [
                                    prop.type'.radio
                                    prop.name radioGroup
                                    prop.isChecked isInnerActive
                                    prop.onChange (fun (b:bool) -> rmvTableImport index)
                                ]
                                Html.text " No Import"
                            ]
                        ]
                    ]
                ]
                if showData then
                    Bulma.field.div [
                        Bulma.tableContainer [
                            Bulma.table [
                                Bulma.table.isBordered
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
        Bulma.modal [
            Bulma.modal.isActive
            prop.children [
                Bulma.modalBackground [ prop.onClick rmv ]
                Bulma.modalCard [
                    prop.style [style.maxHeight(length.percent 70); style.overflowY.hidden]
                    prop.children [
                        Bulma.modalCardHead [
                            Bulma.modalCardTitle "Import"
                            Bulma.delete [ prop.onClick rmv ]
                        ]
                        Bulma.modalCardBody [
                            prop.className "p-5"
                            prop.children [
                                SelectiveImportModal.ImportTypeRadio(state.ImportType, fun it -> {state with ImportType = it} |> setState)
                                SelectiveImportModal.MetadataImport(state.ImportMetadata, setMetadataImport, disArcfile)
                                for ti in 0 .. (tables.Count-1) do
                                    let t = tables.[ti]
                                    SelectiveImportModal.TableImport(ti, t, state, addTableImport, rmvTableImport)
                            ]
                        ]
                        Bulma.modalCardFoot [
                            Bulma.button.button [
                                color.isInfo
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
