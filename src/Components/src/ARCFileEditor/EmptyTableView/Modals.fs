namespace Swate.Components.ArcFileEditor.EmptyTableView

open Feliz
open Fable.Core
open ARCtrl
open Swate.Components
open Swate.Components.Shared

module EmptyTableViewHelpers =

    let tryGetActiveTable (arcFile: ArcFiles) (activeTableIndex: int option) =
        arcFile.TryGetActiveTable(activeTableIndex)

    let createMinimalTable (arcFile: ArcFiles) (activeTableIndex: int option) (setArcFile: ArcFiles -> unit) =
        match tryGetActiveTable arcFile activeTableIndex with
        | Some(_, activeTable) ->
            let newColumns = [|
                CompositeColumn.create (CompositeHeader.Input IOType.Sample)
                CompositeColumn.create CompositeHeader.ProtocolUri
                CompositeColumn.create (CompositeHeader.Output IOType.Sample)
            |]

            activeTable.AddColumns(newColumns)
            activeTable.AddRowsEmpty(3)
            setArcFile (WidgetArcFile.refreshRef arcFile)
        | None -> ()

    let getOutputTables (arcFile: ArcFiles) =
        arcFile.Tables()
        |> Seq.filter (fun table -> table.TryGetOutputColumn().IsSome)
        |> Seq.toArray

    let tryCreatePreviewColumn (table: ArcTable) =
        match table.TryGetOutputColumn() with
        | Some outputColumn ->
            match outputColumn.Header.TryIOType() with
            | Some ioType -> Some(CompositeColumn.create (CompositeHeader.Input ioType, outputColumn.Cells))
            | None -> None
        | None -> None

    let previewCells (cells: seq<CompositeCell>) =
        cells |> Seq.truncate 10 |> Seq.map string |> Seq.toArray

    let importSelectedPreviousOutput
        (arcFile: ArcFiles)
        (activeTableIndex: int option)
        (selectedTable: ArcTable option)
        (setArcFile: ArcFiles -> unit)
        =
        match tryGetActiveTable arcFile activeTableIndex, selectedTable with
        | Some(activeIndex, activeTable), Some sourceTable ->
            match sourceTable.TryGetOutputColumn() with
            | Some outputColumn ->
                match outputColumn.Header.TryIOType() with
                | Some ioType ->

                    activeTable.AddColumn(CompositeHeader.Input ioType, outputColumn.Cells)

                    setArcFile (WidgetArcFile.refreshRef arcFile)
                    true
                | None -> false
            | None -> false
        | _ -> false

[<Erase; Mangle(false)>]
type Modals =

    [<ReactComponent>]
    static member BuildingBlock
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            isOpen: bool,
            setIsOpen: bool -> unit
        ) =

        let setArcFileAndClose nextArcFile =
            setArcFile nextArcFile
            setIsOpen false

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Select a building block",
            // TODO: This does not correctly mirror look from Client/
            children = BuildingBlockWidget.Main(arcFile, activeTableIndex, setArcFileAndClose),
            className = "swt:max-w-3xl"
        )

    [<ReactComponent>]
    static member Templates
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            templateServices: TemplateWidgetServices,
            isOpen: bool,
            setIsOpen: bool -> unit
        ) =

        let importType, setImportType = React.useState TableJoinOptions.Headers

        let setArcFileAndClose nextArcFile =
            setArcFile nextArcFile
            setIsOpen false

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Select template(s)",
            children =
                TemplateWidget.Main(
                    arcFile,
                    activeTableIndex,
                    setArcFileAndClose,
                    importType,
                    setImportType,
                    templateServices
                ),
            className = "swt:flex swt:min-w-fit"
        )

    [<ReactComponent>]
    static member PreviousTableSelect
        (
            arcFile: ArcFiles,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            isOpen: bool,
            setIsOpen: bool -> unit
        ) =

        let relevantTables = EmptyTableViewHelpers.getOutputTables arcFile

        let (selectedTable: ArcTable option), setSelectedTable =
            React.useState (
                if relevantTables.Length > 0 then
                    Some relevantTables.[0]
                else
                    None
            )

        let previewColumn =
            selectedTable |> Option.bind EmptyTableViewHelpers.tryCreatePreviewColumn

        let canImport =
            activeTableIndex.IsSome && selectedTable.IsSome && previewColumn.IsSome

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Select table for source",
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.div [
                            prop.className "swt:join swt:mt-2"
                            prop.children [
                                Html.span [
                                    prop.className
                                        "swt:join-item swt:btn swt:btn-ghost swt:shadow-none swt:pointer-events-none"
                                    prop.text "Tables:"
                                ]
                                Html.select [
                                    prop.className "swt:select swt:join-item swt:min-w-fit"
                                    prop.valueOrDefault (
                                        selectedTable |> Option.map (fun table -> table.Name) |> Option.defaultValue ""
                                    )
                                    prop.onChange (fun (tableName: string) ->
                                        let table =
                                            relevantTables |> Array.find (fun candidate -> candidate.Name = tableName)

                                        Some table |> setSelectedTable
                                    )
                                    prop.children (
                                        relevantTables
                                        |> Array.map (fun table ->
                                            Html.option [ prop.value table.Name; prop.text table.Name ]
                                        )
                                    )
                                ]
                            ]
                        ]

                        match previewColumn with
                        | Some previewColumn ->
                            Html.div [
                                prop.className "swt:overflow-auto swt:max-h-64"
                                prop.children [
                                    Html.table [
                                        prop.className "swt:table swt:table-sm"
                                        prop.children [
                                            Html.caption [
                                                prop.className
                                                    "swt:text-lg swt:font-semibold swt:pb-2 swt:text-left swt:align-left"
                                                prop.text "Preview:"
                                            ]
                                            Html.thead [
                                                Html.tr [
                                                    Html.th [
                                                        prop.className "swt:truncate swt:align-left"
                                                        prop.text (previewColumn.Header.ToString())
                                                        prop.title (previewColumn.Header.ToString())
                                                    ]
                                                ]
                                            ]
                                            Html.tbody [
                                                let previewCells =
                                                    EmptyTableViewHelpers.previewCells previewColumn.Cells

                                                if previewCells.Length = 0 then
                                                    Html.tr [
                                                        Html.td [
                                                            prop.className "swt:opacity-70"
                                                            prop.text "No cells to preview."
                                                        ]
                                                    ]
                                                else
                                                    for cellText in previewCells do
                                                        Html.tr [
                                                            Html.td [
                                                                prop.className "swt:truncate swt:align-left"
                                                                prop.text cellText
                                                                prop.title cellText
                                                            ]
                                                        ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        | None ->
                            Html.div [
                                prop.className "swt:text-sm swt:text-error"
                                prop.text "The selected table does not expose a usable output column."
                            ]

                        Html.button [
                            prop.className "swt:btn swt:w-full swt:btn-primary"
                            prop.text "Import selected output column"
                            prop.disabled (not canImport)
                            prop.onClick (fun _ ->
                                if
                                    EmptyTableViewHelpers.importSelectedPreviousOutput
                                        arcFile
                                        activeTableIndex
                                        selectedTable
                                        setArcFile
                                then
                                    setIsOpen false
                            )
                        ]
                    ]
                ],
            className = "swt:max-w-3xl"
        )