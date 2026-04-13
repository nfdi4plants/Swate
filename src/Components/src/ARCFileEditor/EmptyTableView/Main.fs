namespace Swate.Components.ArcFileEditor.EmptyTableView

open Feliz
open ARCtrl
open Fable.Core

open Swate.Components
open Swate.Components.Shared
open Swate.Components.ArcFileEditor.EmptyTableView.Helper

[<RequireQualifiedAccess>]
type private ModalState =
    | BuildingBlock
    | Templates
    | PreviousTableSelect

[<Erase; Mangle(false)>]
type Main =

    [<ReactComponent>]
    static member private SelectionShell(title: string, onBack: unit -> unit, children: ReactElement) =
        Html.div [
            prop.className "swt:flex swt:h-full swt:min-h-0 swt:w-full swt:flex-col swt:gap-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                            prop.text "Back"
                            prop.onClick (fun _ -> onBack ())
                        ]
                        Html.h3 [
                            prop.className "swt:text-xl swt:font-semibold"
                            prop.text title
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:flex-1 swt:min-h-0 swt:overflow-auto"
                    prop.children [ children ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private HomeView
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, setView: ModalState -> unit)
        =
        let hasActiveTable =
            Helper.tryGetActiveTable arcFile activeTableIndex |> Option.isSome

        let hasOutputTables = Helper.getOutputTables arcFile |> Array.isEmpty |> not

        Html.div [
            prop.className "swt:flex swt:h-full swt:items-center swt:justify-center swt:overflow-auto"
            prop.children [
                CardGrid.CardGrid(
                    React.Fragment [
                        CardGrid.CardGridButton(
                            Icons.Templates(),
                            "Start with template!",
                            "Select a full template as a starting point.",
                            fun _ -> setView ModalState.Templates
                        )
                        CardGrid.CardGridButton(
                            Icons.BuildingBlock(),
                            "Start from scratch!",
                            "Select a building block as a starting point.",
                            fun _ -> setView ModalState.BuildingBlock
                        )
                        CardGrid.CardGridButton(
                            Icons.BasicTable(),
                            "Create basic table!",
                            "Create a table with columns: Input, Protocol, Output.",
                            (fun _ -> Helper.createMinimalTable arcFile activeTableIndex setArcFile),
                            not hasActiveTable
                        )
                        CardGrid.CardGridButton(
                            Icons.OutputColumn(),
                            "Utilize prior output!",
                            "Select an output column of one table as new input column.",
                            (fun _ -> setView ModalState.PreviousTableSelect),
                            (not hasActiveTable) || (not hasOutputTables)
                        )
                    ]
                )
            ]
        ]

    [<ReactComponent>]
    static member private PreviousTableSelectView
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit)
        =
        let relevantTables = Helper.getOutputTables arcFile

        let initialSelectedTableName =
            relevantTables |> Array.tryHead |> Option.map (fun table -> table.Name)

        let selectedTableName, setSelectedTableName =
            React.useState (initialSelectedTableName: string option)

        let selectedTable =
            selectedTableName
            |> Option.bind (fun tableName -> relevantTables |> Array.tryFind (fun table -> table.Name = tableName))

        let previewColumn = selectedTable |> Option.bind Helper.tryCreatePreviewColumn

        let canImport =
            activeTableIndex.IsSome && selectedTable.IsSome && previewColumn.IsSome

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        Html.p [
                            prop.className "swt:text-sm swt:opacity-70"
                            prop.text "Choose a table with an output column to reuse as the new input column."
                        ]

                        if relevantTables.Length > 0 then
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
                                        prop.valueOrDefault (selectedTableName |> Option.defaultValue "")
                                        prop.disabled false
                                        prop.onChange (fun (tableName: string) ->
                                            if System.String.IsNullOrWhiteSpace tableName then
                                                setSelectedTableName None
                                            else
                                                setSelectedTableName (Some tableName)
                                        )
                                        prop.children [
                                            for table in relevantTables do
                                                Html.option [ prop.value table.Name; prop.text table.Name ]
                                        ]
                                    ]
                                ]
                            ]
                        else
                            Html.div [
                                prop.className "swt:text-sm swt:text-base-content/70"
                                prop.text "No tables with output columns are available."
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
                                        let previewCells = Helper.previewCells previewColumn.Cells

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
                        Helper.importSelectedPreviousOutput arcFile activeTableIndex selectedTable setArcFile
                        |> ignore
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member EmptyTableView
        (
            arcFile: ArcFiles,
            setArcFile: ArcFiles -> unit,
            activeTableIndex: int option,
            templateServices: TemplateWidgetServices
        ) =
        let modal, setModal = React.useState (None: ModalState option)

        let setIsOpen (modal: ModalState) =
            function
            | true -> setModal (Some modal)
            | false -> setModal None

        let isBuildingBlockOpen = modal = Some ModalState.BuildingBlock
        let isTemplatesOpen = modal = Some ModalState.Templates
        let isPreviousTableSelectOpen = modal = Some ModalState.PreviousTableSelect

        let isValidTableIndexForArcFile =
            Helper.tryGetActiveTable arcFile activeTableIndex |> Option.isSome

        let isDisabled = not isValidTableIndexForArcFile

        let canUsePreviousOutput =
            isValidTableIndexForArcFile
            && (Helper.getOutputTables arcFile |> Array.isEmpty |> not)

        Html.div [
            prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col swt:gap-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:overflow-auto"
                    prop.children [
                        CardGrid.CardGrid(
                            React.Fragment [
                                CardGrid.CardGridButton(
                                    Icons.Templates(),
                                    "Start with template!",
                                    "Select a full template as a starting point.",
                                    fun _ -> setModal (Some ModalState.Templates)
                                )
                                CardGrid.CardGridButton(
                                    Icons.BuildingBlock(),
                                    "Start from scratch!",
                                    "Select a building block as a starting point.",
                                    fun _ -> setModal (Some ModalState.BuildingBlock)
                                )
                                CardGrid.CardGridButton(
                                    Icons.BasicTable(),
                                    "Create basic table!",
                                    "Create a table with columns: Input, Protocol, Output.",
                                    (fun _ -> Helper.createMinimalTable arcFile activeTableIndex setArcFile),
                                    (isDisabled)
                                )
                                CardGrid.CardGridButton(
                                    Icons.OutputColumn(),
                                    "Utilize prior output!",
                                    "Select an output column of one table as new input column.",
                                    (fun _ -> setModal (Some ModalState.PreviousTableSelect)),
                                    disabled = (not canUsePreviousOutput)
                                )
                            ]
                        )
                    ]
                ]
                Modals.BuildingBlock(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    isBuildingBlockOpen,
                    setIsOpen ModalState.BuildingBlock
                )
                Modals.Templates(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    templateServices,
                    isTemplatesOpen,
                    setIsOpen ModalState.Templates
                )
                Modals.PreviousTableSelect(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    isPreviousTableSelectOpen,
                    setIsOpen ModalState.PreviousTableSelect
                )
            ]
        ]