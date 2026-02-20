namespace Renderer.components.Widgets

open Feliz
open Fable.Core
open ARCtrl
open ARCtrl.Json

open Swate.Components
open Swate.Components.Shared


module Helper =

    [<Literal>]
    let DefaultSeparator = ","

    let noDataRowsError = "Parsed file does not contain any data rows."

open Helper

type AnnotationInput = {
    Selectors: string[]
    FileName: string
    FileType: string
    TargetColumn: DataAnnotator.TargetColumn
} 

type DataAnnotatorDataSource =

    static member private NormalizeSeparator (separator: string) (file: DataAnnotator.DataFile) =
        if System.String.IsNullOrWhiteSpace separator then
            if System.String.IsNullOrWhiteSpace file.ExpectedSeparator then
                DefaultSeparator
            else
                file.ExpectedSeparator
        else
            separator

    static member private TargetSortKey (target: DataAnnotator.DataTarget) =
        match target with
        // Keep legacy priority (Cell -> Column -> Row) and make it numerically stable.
        | DataAnnotator.DataTarget.Cell(columnIndex, rowIndex) -> 0, rowIndex, columnIndex
        | DataAnnotator.DataTarget.Column columnIndex -> 1, -1, columnIndex
        | DataAnnotator.DataTarget.Row rowIndex -> 2, rowIndex, -1

    static member SortTargets (targets: seq<DataAnnotator.DataTarget>) =
        targets
        |> Seq.sortBy DataAnnotatorDataSource.TargetSortKey
        |> Seq.toArray

    static member SelectorsFromTargets (hasHeader: bool) (targets: seq<DataAnnotator.DataTarget>) =
        targets
        |> DataAnnotatorDataSource.SortTargets
        |> Array.map (fun target -> target.ToFragmentSelectorString(hasHeader))

    static member HasDataRows (parsedFile: DataAnnotator.ParsedDataFile) =
        parsedFile.BodyRows.Length > 0

    static member TryParseDataFile (separator: string) (file: DataAnnotator.DataFile) =
        try
            let separator = DataAnnotatorDataSource.NormalizeSeparator separator file
            let parsed = DataAnnotator.ParsedDataFile.fromFileBySeparator separator file

            if DataAnnotatorDataSource.HasDataRows parsed then
                Ok parsed
            else
                Error noDataRowsError
        with exn ->
            Error exn.Message

    static member TryGetTargetHeader (table: ArcTable) (target: DataAnnotator.TargetColumn) =
        match target with
        | DataAnnotator.TargetColumn.Input -> Ok(CompositeHeader.Input IOType.Data)
        | DataAnnotator.TargetColumn.Output -> Ok(CompositeHeader.Output IOType.Data)
        | DataAnnotator.TargetColumn.Autodetect ->
            match table.TryGetInputColumn(), table.TryGetOutputColumn() with
            | Some _, None
            | None, None -> Ok(CompositeHeader.Output IOType.Data)
            | None, Some _ -> Ok(CompositeHeader.Input IOType.Data)
            | Some _, Some _ ->
                Error "Both Input and Output columns already exist. Select Input or Output explicitly."

    static member CreateDataCells (selectors: string[]) (fileName: string) (fileType: string) =
        selectors
        |> Array.map (fun selector ->
            let d = Data()
            d.FilePath <- Some fileName
            d.Selector <- Some selector
            d.Format <- Some fileType
            d.SelectorFormat <- Some URLs.Data.SelectorFormat.csv
            CompositeCell.createData d
        )
        |> ResizeArray

    static member ApplyToTable (table: ArcTable) (input: AnnotationInput) : Result<int, string> =
        try
            match DataAnnotatorDataSource.TryGetTargetHeader table input.TargetColumn with
            | Error err -> Error err
            | Ok header ->
                let values = DataAnnotatorDataSource.CreateDataCells input.Selectors input.FileName input.FileType
                table.AddColumn(header, values, forceReplace = true)
                Ok input.Selectors.Length
        with exn ->
            Error exn.Message

    static member ApplyToDataMap (dataMap: DataMap) (input: AnnotationInput) : Result<int, string> =
        try
            let inputLength = input.Selectors.Length

            if inputLength > dataMap.DataContexts.Count then
                let rows =
                    Array.init (inputLength - dataMap.DataContexts.Count) (fun _ -> DataContext())

                dataMap.DataContexts.AddRange(rows)

            for i in 0 .. inputLength - 1 do
                let selector = input.Selectors.[i]
                let dtx = dataMap.DataContexts.[i]
                dtx.FilePath <- Some input.FileName
                dtx.Selector <- Some selector
                dtx.Format <- Some input.FileType
                dtx.SelectorFormat <- Some URLs.Data.SelectorFormat.csv

            Ok inputLength
        with exn ->
            Error exn.Message

    static member syncArcVault (arcFile: ArcFiles) : JS.Promise<Result<unit, string>> =
        Renderer.ArcFilePersistence.saveArcFile arcFile

type DataAnnotatorWidget =

    [<ReactComponent>]
    static member PreviewTable
        (
            parsedFile: DataAnnotator.ParsedDataFile,
            selectedTargets: Set<DataAnnotator.DataTarget>,
            setSelectedTargets: (Set<DataAnnotator.DataTarget> -> Set<DataAnnotator.DataTarget>) -> unit
        ) =
        let toggleTarget (target: DataAnnotator.DataTarget) =
            setSelectedTargets (fun current ->
                if Set.contains target current then
                    Set.remove target current
                else
                    Set.add target current
            )

        let bodyColumnMax =
            parsedFile.BodyRows
            |> Array.fold (fun current row -> max current row.Length) 0

        let headerColumnMax =
            parsedFile.HeaderRow
            |> Option.map _.Length
            |> Option.defaultValue 0

        let columnCount = max bodyColumnMax headerColumnMax

        let isDirectlySelected (target: DataAnnotator.DataTarget) =
            Set.contains target selectedTargets

        let isCellActive (columnIndex: int) (rowIndex: int) =
            Set.contains (DataAnnotator.DataTarget.Cell(columnIndex, rowIndex)) selectedTargets
            || Set.contains (DataAnnotator.DataTarget.Row rowIndex) selectedTargets
            || Set.contains (DataAnnotator.DataTarget.Column columnIndex) selectedTargets

        let getHeaderText (columnIndex: int) =
            match parsedFile.HeaderRow with
            | Some header when columnIndex < header.Length -> header.[columnIndex]
            | _ -> $"Column {columnIndex + 1}"

        let getCellText (rowIndex: int) (columnIndex: int) =
            if rowIndex < parsedFile.BodyRows.Length && columnIndex < parsedFile.BodyRows.[rowIndex].Length then
                parsedFile.BodyRows.[rowIndex].[columnIndex]
            else
                ""

        Html.div [
            prop.className "swt:overflow-auto swt:border swt:rounded-md swt:max-h-[55vh]"
            prop.children [
                Html.table [
                    prop.className "swt:table swt:table-xs swt:table-pin-rows swt:table-pin-cols"
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "#"
                                for columnIndex in 0 .. columnCount - 1 do
                                    let columnTarget = DataAnnotator.DataTarget.Column columnIndex
                                    let isSelected = isDirectlySelected columnTarget

                                    Html.th [
                                        prop.key $"target-col-{columnIndex}"
                                        prop.children [
                                            Html.button [
                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs swt:btn-ghost swt:w-full swt:justify-start"
                                                    if isSelected then
                                                        "swt:btn-primary"
                                                ]
                                                prop.onClick (fun _ -> toggleTarget columnTarget)
                                                prop.title "Toggle full column selector"
                                                prop.text (getHeaderText columnIndex)
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                        Html.tbody [
                            for rowIndex in 0 .. parsedFile.BodyRows.Length - 1 do
                                let rowTarget = DataAnnotator.DataTarget.Row rowIndex
                                let isRowSelected = isDirectlySelected rowTarget

                                Html.tr [
                                    prop.key $"target-row-{rowIndex}"
                                    prop.children [
                                        Html.th [
                                            Html.button [
                                                prop.type'.button
                                                prop.className [
                                                    "swt:btn swt:btn-xs swt:btn-ghost swt:w-full"
                                                    if isRowSelected then
                                                        "swt:btn-primary"
                                                ]
                                                prop.onClick (fun _ -> toggleTarget rowTarget)
                                                prop.title "Toggle full row selector"
                                                prop.text $"{rowIndex + 1}"
                                            ]
                                        ]
                                        for columnIndex in 0 .. columnCount - 1 do
                                            let cellTarget = DataAnnotator.DataTarget.Cell(columnIndex, rowIndex)
                                            let isDirectCell = isDirectlySelected cellTarget
                                            let isActiveCell = isCellActive columnIndex rowIndex

                                            Html.td [
                                                prop.key $"target-cell-{rowIndex}-{columnIndex}"
                                                prop.children [
                                                    Html.button [
                                                        prop.type'.button
                                                        prop.className [
                                                            "swt:btn swt:btn-xs swt:btn-ghost swt:w-full swt:justify-start"
                                                            if isDirectCell then
                                                                "swt:btn-primary"
                                                            elif isActiveCell then
                                                                "swt:bg-primary/20"
                                                        ]
                                                        prop.onClick (fun _ -> toggleTarget cellTarget)
                                                        prop.title "Toggle single cell selector"
                                                        prop.text (getCellText rowIndex columnIndex)
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            activeTableData: ActiveTableData option,
            activeDataMapData: ActiveDataMapData option,
            onTableMutated: unit -> unit
        ) =
        let dataFile, setDataFile = React.useState (None: DataAnnotator.DataFile option)
        let parsedFile, setParsedFile = React.useState (None: DataAnnotator.ParsedDataFile option)
        let selectedTargets, setSelectedTargets =
            React.useStateWithUpdater Set.empty<DataAnnotator.DataTarget>
        let targetColumn, setTargetColumn = React.useState DataAnnotator.TargetColumn.Autodetect
        let separatorInput, setSeparatorInput = React.useState DefaultSeparator
        let isModalOpen, setIsModalOpen = React.useState false
        let isReadingFile, setIsReadingFile = React.useState false
        let status, setStatus = React.useState (None: StatusMessage option)
        let isSeparatorDropdownOpen, setIsSeparatorDropdownOpen = React.useState false
        let fileInputRef = React.useElementRef ()
        let separatorInputRef = React.useElementRef ()

        let hasActiveTarget = activeTableData.IsSome || activeDataMapData.IsSome
        let canOpenModal = hasActiveTarget && dataFile.IsSome && parsedFile.IsSome
        let canSubmit =
            canOpenModal
            && (selectedTargets |> Set.isEmpty |> not)

        let clearSelection () =
            setSelectedTargets (fun _ -> Set.empty)

        let resetAll () =
            setDataFile None
            setParsedFile None
            setSeparatorInput DefaultSeparator
            clearSelection ()
            setIsModalOpen false
            setIsSeparatorDropdownOpen false
            setStatus None

            fileInputRef.current
            |> Option.iter (fun input -> (input :?> Browser.Types.HTMLInputElement).value <- "")

        let setErrorStatus text =
            setStatus (
                Some {
                    Kind = StatusKind.Error
                    Text = text
                }
            )

        let parseDataFile (clearOnError: bool) (separator: string) (file: DataAnnotator.DataFile) =
            match DataAnnotatorDataSource.TryParseDataFile separator file with
            | Ok nextParsedFile ->
                setParsedFile (Some nextParsedFile)
                clearSelection ()
                setStatus None
            | Error err ->
                if clearOnError then
                    setParsedFile None
                    clearSelection ()
                setErrorStatus err

        let setFileFromBrowser (file: Browser.Types.File) =
            promise {
                setIsReadingFile true

                try
                    let! content = file.text ()

                    let nextDataFile =
                        DataAnnotator.DataFile.create (file.name, file.``type``, content, file.size)

                    let nextSeparator = nextDataFile.ExpectedSeparator

                    setDataFile (Some nextDataFile)
                    setSeparatorInput nextSeparator
                    parseDataFile true nextSeparator nextDataFile
                with exn ->
                    setErrorStatus $"Failed to parse selected file: {exn.Message}"

                setIsReadingFile false
            }
            |> Promise.start

        let updateSeparator () =
            match dataFile with
            | None -> setErrorStatus "Upload a data file before updating separator."
            | Some file ->
                let nextSeparatorInput =
                    separatorInputRef.current
                    |> Option.map (fun input -> (input :?> Browser.Types.HTMLInputElement).value)
                    |> Option.defaultValue separatorInput

                let separatorToUse =
                    if System.String.IsNullOrWhiteSpace nextSeparatorInput then
                        if System.String.IsNullOrWhiteSpace file.ExpectedSeparator then
                            DefaultSeparator
                        else
                            file.ExpectedSeparator
                    else
                        nextSeparatorInput

                setSeparatorInput separatorToUse

                parseDataFile false separatorToUse file

        let toggleHeader () =
            match parsedFile with
            | None -> ()
            | Some parsed ->
                setParsedFile (Some(parsed.ToggleHeader()))
                clearSelection ()

        let applyQuickSeparator (separator: string) =
            setSeparatorInput separator

        let trySubmitAnnotation () =
            match dataFile, parsedFile with
            | None, _
            | _, None ->
                setErrorStatus "Upload and parse a data file before submitting."
            | Some file, Some parsed ->
                let selectors =
                    DataAnnotatorDataSource.SelectorsFromTargets parsed.HeaderRow.IsSome selectedTargets

                if selectors.Length = 0 then
                    setErrorStatus "Select at least one row, column, or cell from preview."
                else
                    let annotationInput: AnnotationInput = {
                        Selectors = selectors
                        FileName = file.DataFileName
                        FileType = file.DataFileType
                        TargetColumn = targetColumn
                    }

                    let arcFileOpt =
                        match activeTableData, activeDataMapData with
                        | Some tableData, _ -> Some tableData.ArcFile
                        | None, Some dataMapData -> Some dataMapData.ArcFile
                        | None, None -> None

                    let applyResult =
                        match activeTableData, activeDataMapData with
                        | Some tableData, _ ->
                            DataAnnotatorDataSource.ApplyToTable tableData.Table annotationInput
                        | None, Some dataMapData ->
                            DataAnnotatorDataSource.ApplyToDataMap dataMapData.DataMap annotationInput
                        | None, None ->
                            Error "Open a table or datamap before submitting data annotation."

                    match applyResult with
                    | Error applyError ->
                        setErrorStatus $"Data annotation failed: {applyError}"
                    | Ok addedCount ->
                        onTableMutated ()
                        setIsModalOpen false

                        promise {
                            let! syncResult =
                                match arcFileOpt with
                                | Some arcFile -> DataAnnotatorDataSource.syncArcVault arcFile
                                | None -> promise { return Ok() }

                            match syncResult with
                            | Ok() ->
                                setStatus (
                                    Some {
                                        Kind = StatusKind.Info
                                        Text = $"Added data annotation with {addedCount} selector(s)."
                                    }
                                )
                            | Error syncError ->
                                setStatus (
                                    Some {
                                        Kind = StatusKind.Warning
                                        Text = $"Data annotation added locally, but save failed: {syncError}"
                                    }
                                )
                        }
                        |> Promise.start

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-3 swt:p-2"
            prop.children [
                if not hasActiveTarget then
                    Html.div [
                        prop.className "swt:text-sm swt:opacity-70"
                        prop.text "No active table/datamap. Open a table or datamap to use Data Annotator."
                    ]
                Html.input [
                    prop.ref fileInputRef
                    prop.type'.file
                    prop.style [ style.display.none ]
                    prop.onChange setFileFromBrowser
                ]
                Html.button [
                    prop.type'.button
                    prop.className "swt:btn swt:btn-primary swt:w-full"
                    prop.onClick (fun _ ->
                        fileInputRef.current
                        |> Option.iter (fun input -> input.click ())
                    )
                    prop.children [
                        if isReadingFile then
                            Html.span [ prop.className "swt:loading swt:loading-spinner swt:loading-sm" ]
                        Html.span (if dataFile.IsSome then "Change File" else "Choose File")
                    ]
                ]
                match dataFile with
                | Some file ->
                    Html.div [
                        prop.className "swt:text-xs swt:opacity-80 swt:break-all"
                        prop.text $"{file.DataFileName} ({file.DataFileType})"
                    ]
                | None -> Html.none
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-error"
                            if dataFile.IsNone then
                                prop.disabled true
                            prop.text "Reset"
                            prop.onClick (fun _ -> resetAll ())
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className [
                                "swt:btn swt:grow"
                                if canOpenModal then "swt:btn-primary" else "swt:btn-disabled"
                            ]
                            prop.disabled (not canOpenModal)
                            prop.text "Open Annotator"
                            prop.onClick (fun _ -> setIsModalOpen true)
                        ]
                    ]
                ]
                Swate.Components.BaseModal.Modal(
                    isOpen = isModalOpen,
                    setIsOpen = setIsModalOpen,
                    header = Html.text "Data Annotator",
                    description = Html.text "Select data points and add annotation as Data selectors.",
                    children =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-3"
                            prop.children [
                                match parsedFile with
                                | None ->
                                    Html.div [
                                        prop.className "swt:text-sm swt:opacity-70"
                                        prop.text "Upload a valid file to preview data."
                                    ]
                                | Some parsed ->
                                    Html.div [
                                        prop.className "swt:flex swt:flex-wrap swt:gap-2"
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:join"
                                                prop.children [
                                                    Components.BaseDropdown.Main(
                                                        isSeparatorDropdownOpen,
                                                        setIsSeparatorDropdownOpen,
                                                        Html.button [
                                                            prop.type'.button
                                                            prop.role.button
                                                            prop.className "swt:btn swt:btn-sm swt:join-item swt:flex-nowrap"
                                                            prop.onClick (fun _ ->
                                                                setIsSeparatorDropdownOpen (not isSeparatorDropdownOpen)
                                                            )
                                                            prop.children [ Icons.AngleDown() ]
                                                        ],
                                                        [
                                                            Html.li [
                                                                Html.a [
                                                                    prop.onClick (fun _ ->
                                                                        applyQuickSeparator "\\t"
                                                                        setIsSeparatorDropdownOpen false
                                                                    )
                                                                    prop.children [ Html.span [ prop.text "Tab (\\t)" ] ]
                                                                ]
                                                            ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.onClick (fun _ ->
                                                                        applyQuickSeparator ","
                                                                        setIsSeparatorDropdownOpen false
                                                                    )
                                                                    prop.children [ Html.span [ prop.text "," ] ]
                                                                ]
                                                            ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.onClick (fun _ ->
                                                                        applyQuickSeparator ";"
                                                                        setIsSeparatorDropdownOpen false
                                                                    )
                                                                    prop.children [ Html.span [ prop.text ";" ] ]
                                                                ]
                                                            ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.onClick (fun _ ->
                                                                        applyQuickSeparator "|"
                                                                        setIsSeparatorDropdownOpen false
                                                                    )
                                                                    prop.children [ Html.span [ prop.text "|" ] ]
                                                                ]
                                                            ]
                                                        ],
                                                        style =
                                                            Style.init (
                                                                "swt:join-item swt:dropdown",
                                                                Map [ "content", Style.init ("swt:!min-w-28") ]
                                                            )
                                                    )
                                                    Html.input [
                                                        prop.ref separatorInputRef
                                                        prop.className "swt:input swt:input-sm swt:join-item swt:w-32"
                                                        prop.placeholder "Separator"
                                                        prop.value separatorInput
                                                        prop.onChange setSeparatorInput
                                                    ]
                                                    Html.button [
                                                        prop.type'.button
                                                        prop.className "swt:btn swt:btn-sm swt:join-item"
                                                        prop.text "Update"
                                                        prop.onClick (fun _ -> updateSeparator ())
                                                    ]
                                                ]
                                            ]
                                            Html.button [
                                                prop.type'.button
                                                prop.className "swt:btn swt:btn-sm"
                                                prop.onClick (fun _ -> toggleHeader ())
                                                prop.text (
                                                    if parsed.HeaderRow.IsSome then
                                                        "Header: On"
                                                    else
                                                        "Header: Off"
                                                )
                                            ]
                                            if activeTableData.IsSome then
                                                Html.select [
                                                    prop.className "swt:select swt:select-sm"
                                                    prop.valueOrDefault (string targetColumn)
                                                    prop.onChange (fun value ->
                                                        setTargetColumn (DataAnnotator.TargetColumn.fromString value)
                                                    )
                                                    prop.children [
                                                        Html.option [
                                                            prop.value "Autodetect"
                                                            prop.text "Autodetect"
                                                        ]
                                                        Html.option [
                                                            prop.value "Input"
                                                            prop.text "Input"
                                                        ]
                                                        Html.option [
                                                            prop.value "Output"
                                                            prop.text "Output"
                                                        ]
                                                    ]
                                                ]
                                            elif activeDataMapData.IsSome then
                                                Html.span [
                                                    prop.className "swt:text-sm swt:opacity-70 swt:self-center"
                                                    prop.text "DataMap mode: target column selection is not required."
                                                ]
                                        ]
                                    ]
                                    DataAnnotatorWidget.PreviewTable(parsed, selectedTargets, setSelectedTargets)
                            ]
                        ],
                    footer =
                        Html.div [
                            prop.className "swt:justify-end swt:flex swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-outline"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> setIsModalOpen false)
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                    prop.disabled (not canSubmit)
                                    prop.text "Submit"
                                    prop.onClick (fun _ -> trySubmitAnnotation ())
                                ]
                            ]
                        ]
                )
                match status with
                | Some message -> StatusElement.Create message
                | None -> Html.none
            ]
        ]
