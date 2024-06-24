namespace Modals

open Feliz
open Feliz.Bulma
open Model
open Messages
open Shared

open ARCtrl

[<RequireQualifiedAccess>]
type private ImportTable = {
    Index: int
    /// If FullImport is true, the table will be imported in full, otherwise it will be appended to active table.
    FullImport: bool
}

type private SelectiveImportModalState = {
    ImportType: ARCtrl.TableJoinOptions
    ImportMetadata: bool
    ImportTables: ImportTable list
} with
    static member init() =
        {
            ImportType = ARCtrl.TableJoinOptions.Headers
            ImportMetadata = false
            ImportTables = []
        }

module private Helper =

    let submitWithMetadata (uploadedFile: ArcFiles) (state: SelectiveImportModalState) (dispatch: Messages.Msg -> unit) =
        if not state.ImportMetadata then failwith "Metadata must be imported"
        let createUpdatedTables (arcTables: ResizeArray<ArcTable>) =
            [
                for it in state.ImportTables do
                    let sourceTable = arcTables.[it.Index]
                    let appliedTable = ArcTable.init(sourceTable.Name)
                    appliedTable.Join(sourceTable, joinOptions=state.ImportType)
                    appliedTable
            ]
            |> ResizeArray
        let arcFile =
            match uploadedFile with
            | Assay a as arcFile->
                let tables = createUpdatedTables a.Tables
                a.Tables <- tables
                arcFile
            | Study (s,_) as arcFile ->
                let tables = createUpdatedTables s.Tables
                s.Tables <- tables
                arcFile
            | Template t as arcFile ->
                let table = createUpdatedTables (ResizeArray[t.Table])
                t.Table <- table.[0]
                arcFile
            | Investigation _ as arcFile ->
                arcFile
        SpreadsheetInterface.UpdateArcFile arcFile |> InterfaceMsg |> dispatch

open Helper

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
                        myradio(ARCtrl.TableJoinOptions.Headers, " Only Column Headers")
                        myradio(ARCtrl.TableJoinOptions.WithValues, " With Values")
                        myradio(ARCtrl.TableJoinOptions.WithUnit, " With Units")
                    ]
                ]
            ]
        ]

    static member private MetadataImport(isActive: bool, setActive: bool -> unit) =
        Bulma.box [
            if isActive then color.hasBackgroundInfo
            prop.children [
                Bulma.field.div [
                    Bulma.label [
                        Html.i [prop.className "fa-solid fa-lightbulb"]
                        Html.text (" Metadata")
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
                                            Html.th "Column"
                                            for ri in 1 .. table.RowCount do
                                                Html.th ri
                                        ]
                                    ]
                                    Html.tbody [
                                        for c in table.Columns do
                                            Html.tr [
                                                Html.th (c.Header.ToString())
                                                for cv in c.Cells do
                                                    Html.td (cv.ToString())
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member Main (import: ArcFiles) (rmv: _ -> unit) =
        let state, setState = React.useState(SelectiveImportModalState.init)
        let tables =
            match import with
            | Assay a -> a.Tables
            | Study (s,_) -> s.Tables
            | Template t -> ResizeArray([t.Table])
            | _ -> ResizeArray()
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
                                Bulma.button.a [
                                    prop.onClick (fun _ -> log state)
                                    prop.text "test"
                                ]
                                SelectiveImportModal.ImportTypeRadio(state.ImportType, fun it -> {state with ImportType = it} |> setState)
                                SelectiveImportModal.MetadataImport(state.ImportMetadata, setMetadataImport)
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
                                    rmv e
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]
