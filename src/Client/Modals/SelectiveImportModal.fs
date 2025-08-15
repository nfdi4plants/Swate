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
    static member RadioPluginsBox
        (
            boxName,
            icon,
            importType: TableJoinOptions,
            radioGroupName,
            radioData: (TableJoinOptions * string)[],
            setImportType: TableJoinOptions -> unit
        ) =

        let guid = React.useMemo (fun () -> System.Guid.NewGuid().ToString())
        let radioGroupName = radioGroupName + guid

        let myradio (target: TableJoinOptions, txt: string) =
            let isChecked = importType = target

            ModalElements.RadioPlugin(
                radioGroupName,
                txt,
                isChecked,
                fun (b: bool) ->
                    if b then
                        setImportType target
            )

        ModalElements.Box(
            boxName,
            icon,
            React.fragment [
                Html.div [
                    for i in 0 .. radioData.Length - 1 do
                        myradio (radioData.[i])
                ]
            ]
        )

    static member CheckBoxForTableColumnSelection
        (columns: CompositeColumn[], tableIndex, columnIndex, isActive: bool, model: Model.Model, dispatch)
        =
        let importConfig = model.ProtocolState.ImportConfig

        let isChecked =
            importConfig.DeselectedColumns |> Set.contains (tableIndex, columnIndex) |> not

        Html.div [
            prop.className "swt:flex swt:justify-center"
            prop.children [
                //Daisy.checkbox [
                Html.input [
                    prop.type'.checkbox
                    prop.className "swt:checkbox swt:checkbox-neutral"
                    prop.disabled (not isActive)
                    prop.style [ style.height (length.perc 100) ]
                    prop.isChecked isChecked
                    prop.onChange (fun (_: bool) ->
                        if columns.Length > 0 then
                            let nextImportConfig = importConfig.toggleDeselectColumn (tableIndex, columnIndex)
                            nextImportConfig |> Protocol.UpdateImportConfig |> ProtocolMsg |> dispatch
                    )
                ]
            ]
        ]

    static member TableWithImportColumnCheckboxes
        (table: ArcTable, tableIndex, isActive: bool, model: Model.Model, dispatch: Messages.Msg -> unit)
        =
        let columns = table.Columns

        //Daisy.table [
        Html.table [
            prop.className "swt:table"
            prop.children [
                Html.thead [
                    Html.tr [
                        for columnIndex in 0 .. columns.Length - 1 do
                            Html.th [
                                Html.label [
                                    prop.className "swt:flex swt:flex-row swt:gap-2"
                                    prop.children [
                                        SelectiveImportModal.CheckBoxForTableColumnSelection(
                                            columns,
                                            tableIndex,
                                            columnIndex,
                                            isActive,
                                            model,
                                            dispatch
                                        )
                                        Html.text (columns.[columnIndex].Header.ToString())
                                    ]
                                ]
                            ]
                    ]
                ]
                Html.tbody [
                    for ri in 0 .. (table.RowCount - 1) do
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

        ModalElements.Box(
            sprintf "%s Metadata" name,
            Icons.LightBulb(),
            React.fragment [
                //Daisy.fieldset [
                Html.fieldSet [
                    prop.className "swt:fieldset"
                    prop.children [
                        //Daisy.label [
                        Html.label [
                            prop.className "swt:label swt:cursor-pointer"
                            prop.children [
                                //Daisy.checkbox [
                                Html.input [
                                    prop.type'.checkbox
                                    prop.className "swt:checkbox"
                                    prop.onChange (fun (b: bool) -> setActive b)
                                ]
                                Html.span [ prop.className "swt:text-sm"; prop.text "Import" ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:alert swt:alert-warning"
                    prop.children [
                        Icons.ExclamationTriangle()
                        Html.text " Importing metadata will overwrite the current file."
                    ]
                ]
            ],
            className = [
                if isActive then
                    "swt:border swt:border-info"
            ]
        )

    [<ReactComponent>]
    static member TableImport(tableIndex: int, table0: ArcTable, model: Model.Model, dispatch, ?templateName) =
        let importConfig = model.ProtocolState.ImportConfig
        let name = defaultArg templateName table0.Name
        let radiogroupId = React.useMemo (System.Guid.NewGuid)

        let radioGroup =
            "RADIO_GROUP" + table0.Name + string tableIndex + radiogroupId.ToString()

        let import =
            importConfig.ImportTables |> List.tryFind (fun it -> it.Index = tableIndex)

        let isActive = import.IsSome
        let isDisabled = importConfig.ImportMetadata

        let addTableImport =
            fun (i: int) (fullImport: bool) ->
                let newImportTable: ImportTable = { Index = i; FullImport = fullImport }

                let newImportTables =
                    newImportTable :: importConfig.ImportTables
                    |> List.distinctBy (fun table -> table.Index)

                {
                    importConfig with
                        ImportTables = newImportTables
                }
                |> Protocol.UpdateImportConfig
                |> ProtocolMsg
                |> dispatch

        let rmvTableImport =
            fun i ->
                let tableRemoved =
                    importConfig.ImportTables |> List.filter (fun it -> it.Index <> i)

                {
                    importConfig with
                        ImportTables = tableRemoved
                }
                |> Protocol.UpdateImportConfig
                |> ProtocolMsg
                |> dispatch

        ModalElements.Box(
            name,
            Icons.Table(),
            React.fragment [
                Html.div [
                    ModalElements.RadioPlugin(
                        radioGroup,
                        "Import",
                        isActive && import.Value.FullImport,
                        (fun _ -> addTableImport tableIndex true),
                        isDisabled
                    )
                    ModalElements.RadioPlugin(
                        radioGroup,
                        "Append to active table",
                        isActive && not import.Value.FullImport,
                        (fun _ -> addTableImport tableIndex false),
                        isDisabled
                    )
                    ModalElements.RadioPlugin(
                        radioGroup,
                        "No Import",
                        not isActive,
                        (fun _ -> rmvTableImport tableIndex),
                        isDisabled
                    )
                ]
                //Daisy.collapse [
                Html.div [
                    prop.className "swt:collapse"
                    prop.children [
                        Html.input [ prop.type'.checkbox; prop.className "swt:min-h-0 swt:h-5" ]
                        //Daisy.collapseTitle [
                        Html.div [
                            prop.className [
                                "swt:flex swt:items-center swt:collapse-title swt:p-1 swt:min-h-0 swt:h-5 swt:text-sm swt:font-bold swt:space-x-2"
                            ]
                            prop.children [
                                Html.span [
                                    prop.text (
                                        if isActive then
                                            "Preview Selected Columns"
                                        else
                                            "Preview Table"
                                    )
                                ]
                                Icons.MagnifyingClass()
                            ]
                        ]
                        //Daisy.collapseContent [
                        Html.div [
                            prop.className "swt:collapse-content swt:overflow-x-auto"
                            prop.children [
                                SelectiveImportModal.TableWithImportColumnCheckboxes(
                                    table0,
                                    tableIndex,
                                    isActive,
                                    model,
                                    dispatch
                                )
                            ]
                        ]
                    ]
                ]
            ],
            className = [
                if isActive then
                    "swt:border-success"
            ]
        )

    [<ReactComponent>]
    static member Main(import: ArcFiles, model, dispatch, rmv) =

        let isOpen, setIsOpen = React.useState (true)

        let tables, disArcfile =
            match import with
            | Assay a -> a.Tables, ArcFilesDiscriminate.Assay
            | Study(s, _) -> s.Tables, ArcFilesDiscriminate.Study
            | Template t -> ResizeArray([ t.Table ]), ArcFilesDiscriminate.Template
            | Investigation _ -> ResizeArray(), ArcFilesDiscriminate.Investigation

        let setMetadataImport =
            fun b ->
                {
                    model.ProtocolState.ImportConfig with
                        ImportMetadata = b
                }
                |> Protocol.UpdateImportConfig
                |> ProtocolMsg
                |> dispatch

        let content =
            React.fragment [
                SelectiveImportModal.RadioPluginsBox(
                    "Import Type",
                    Icons.Cog(),
                    model.ProtocolState.ImportConfig.ImportType,
                    "importType",
                    [|
                        ARCtrl.TableJoinOptions.Headers, " Column Headers"
                        ARCtrl.TableJoinOptions.WithUnit, " ..With Units"
                        ARCtrl.TableJoinOptions.WithValues, " ..With Values"
                    |],
                    fun importType ->
                        {
                            model.ProtocolState.ImportConfig with
                                ImportType = importType
                        }
                        |> Protocol.UpdateImportConfig
                        |> ProtocolMsg
                        |> dispatch
                )
                SelectiveImportModal.MetadataImport(
                    model.ProtocolState.ImportConfig.ImportMetadata,
                    setMetadataImport,
                    disArcfile
                )
                for ti in 0 .. (tables.Count - 1) do
                    let t = tables.[ti]
                    SelectiveImportModal.TableImport(ti, t, model, dispatch)
            ]

        let footer =
            Html.div [
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    //Daisy.button.button [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick rmv
                    ]
                    //Daisy.button.button [
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.style [ style.marginLeft length.auto ]
                        prop.text "Submit"
                        prop.onClick (fun e ->
                            {|
                                importState = model.ProtocolState.ImportConfig
                                importedFile = import
                                deselectedColumns = model.ProtocolState.ImportConfig.DeselectedColumns
                            |}
                            |> SpreadsheetInterface.ImportJson
                            |> InterfaceMsg
                            |> dispatch

                            rmv e
                        )
                    ]
                ]
            ]

        Swate.Components.BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p "Import",
            content,
            className = "@container/importModal",
            footer = footer
        )

    static member Main(import: ArcFiles, model, dispatch: Messages.Msg -> unit) =
        let rmv = Util.RMV_MODAL dispatch
        SelectiveImportModal.Main(import, model, dispatch, rmv = rmv)

    static member Templates(model, dispatch: Messages.Msg -> unit) =

        let isOpen, setIsOpen = React.useState (true)

        let rmv = Util.RMV_MODAL dispatch

        let tables = model.ProtocolState.TemplatesSelected |> List.map (fun t -> t.Table)

        let content =
            React.fragment [
                SelectiveImportModal.RadioPluginsBox(
                    "Import Type",
                    Icons.Cog(),
                    model.ProtocolState.ImportConfig.ImportType,
                    "importType",
                    [|
                        ARCtrl.TableJoinOptions.Headers, " Column Headers"
                        ARCtrl.TableJoinOptions.WithUnit, " ..With Units"
                        ARCtrl.TableJoinOptions.WithValues, " ..With Values"
                    |],
                    fun importType ->
                        {
                            model.ProtocolState.ImportConfig with
                                ImportType = importType
                        }
                        |> Protocol.UpdateImportConfig
                        |> ProtocolMsg
                        |> dispatch
                )
                for ti in 0 .. (tables.Length - 1) do
                    let t = tables.[ti]
                    SelectiveImportModal.TableImport(ti, t, model, dispatch)
            ]

        let footer =
            Html.div [
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick rmv
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.style [ style.marginLeft length.auto ]
                        prop.text "Submit"
                        prop.onClick (fun e ->
                            SpreadsheetInterface.AddTemplates(tables, model.ProtocolState.ImportConfig)
                            |> InterfaceMsg
                            |> dispatch

                            rmv e
                        )
                    ]
                ]
            ]

        Swate.Components.BaseModal.Modal(
            isOpen,
            setIsOpen,
            Html.p "Import",
            content,
            className = "@container/importModal",
            footer = footer
        )
