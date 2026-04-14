namespace Swate.Components

open System
open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.Widgets.Contexts

module private DataAnnotatorWidgetModel =

    [<RequireQualifiedAccess>]
    type TargetColumn =
        | Input
        | Output
        | Autodetect

        static member fromString(value: string) =
            match value.ToLowerInvariant() with
            | "input" -> TargetColumn.Input
            | "output" -> TargetColumn.Output
            | _ -> TargetColumn.Autodetect

    [<RequireQualifiedAccess>]
    type DataTarget =
        | Cell of columnIndex: int * rowIndex: int
        | Row of int
        | Column of int

        member this.ToFragmentSelectorString(hasHeader: bool) =
            let rowOffset = if hasHeader then 2 else 1

            match this with
            | DataTarget.Row rowIndex -> sprintf "row=%i" (rowIndex + rowOffset)
            | DataTarget.Column columnIndex -> sprintf "col=%i" (columnIndex + 1)
            | DataTarget.Cell(columnIndex, rowIndex) -> sprintf "cell=%i,%i" (rowIndex + rowOffset) (columnIndex + 1)

        member this.ToReactKey() =
            match this with
            | DataTarget.Row rowIndex -> sprintf "row-%i" rowIndex
            | DataTarget.Column columnIndex -> sprintf "col-%i" columnIndex
            | DataTarget.Cell(columnIndex, rowIndex) -> sprintf "cell-%i-%i" rowIndex columnIndex

    type DataFile = {
        DataFileName: string
        DataFileType: string
        DataContent: string
        DataSize: float
    } with

        static member create(dataFileName, dataFileType, dataContent, dataSize) = {
            DataFileName = dataFileName
            DataFileType = dataFileType
            DataContent = dataContent
            DataSize = dataSize
        }

        member this.ExpectedSeparator =
            if this.DataFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) then
                ","
            elif this.DataFileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) then
                "\t"
            elif this.DataFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) then
                "\t"
            else
                ","

    type ParsedDataFile = {
        HeaderRow: string[] option
        BodyRows: string[][]
    } with

        static member fromFileBySeparator (separator: string) (file: DataFile) =
            let splitRow (value: string) (separator: string) =
                if separator.Length = 1 then
                    value.Split separator.[0]
                else
                    value.Split([| separator |], StringSplitOptions.None)

            let sanitizedSeparator =
                match separator with
                | "\\t" -> "\t"
                | "\\n" -> "\n"
                | "\\f" -> "\f"
                | "\\r" -> "\r"
                | "\\r\\n" -> "\r\n"
                | "\\v" -> "\v"
                | _ -> separator

            let rows = file.DataContent.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)

            let splitRows =
                rows
                |> Array.map (fun row -> row.TrimEnd '\r' |> fun value -> splitRow value sanitizedSeparator)

            if splitRows.Length > 1 then
                {
                    HeaderRow = Some splitRows.[0]
                    BodyRows = splitRows.[1..]
                }
            else
                {
                    HeaderRow = None
                    BodyRows = splitRows
                }

        member this.ToggleHeader() =
            match this.HeaderRow with
            | Some header -> {
                this with
                    HeaderRow = None
                    BodyRows = Array.insertAt 0 header this.BodyRows
              }
            | None when this.BodyRows.Length > 1 -> {
                this with
                    HeaderRow = Some this.BodyRows.[0]
                    BodyRows = this.BodyRows.[1..]
              }
            | _ -> this

    type AnnotationInput = {
        Selectors: string[]
        FileName: string
        FileType: string
        TargetColumn: TargetColumn
    }

    let private compareTargets (left: DataTarget) (right: DataTarget) =
        let key =
            function
            | DataTarget.Cell(columnIndex, rowIndex) -> 0, rowIndex, columnIndex
            | DataTarget.Column columnIndex -> 1, 0, columnIndex
            | DataTarget.Row rowIndex -> 2, rowIndex, 0

        compare (key left) (key right)

    let selectorsFromTargets (hasHeader: bool) (targets: Set<DataTarget>) =
        targets
        |> Seq.sortWith compareTargets
        |> Seq.map (fun target -> target.ToFragmentSelectorString(hasHeader))
        |> Array.ofSeq

    let tryParseDataFile (separator: string) (file: DataFile) =
        try
            let parsed = ParsedDataFile.fromFileBySeparator separator file

            if parsed.BodyRows.Length = 0 then
                Error "Parsed file does not contain any data rows."
            else
                Ok parsed
        with exceptionValue ->
            Error exceptionValue.Message

    let tryGetTargetHeader (table: ArcTable) (targetColumn: TargetColumn) =
        match targetColumn with
        | TargetColumn.Input -> Ok(CompositeHeader.Input IOType.Data)
        | TargetColumn.Output -> Ok(CompositeHeader.Output IOType.Data)
        | TargetColumn.Autodetect ->
            match table.TryGetInputColumn(), table.TryGetOutputColumn() with
            | Some _, None
            | None, None -> Ok(CompositeHeader.Output IOType.Data)
            | None, Some _ -> Ok(CompositeHeader.Input IOType.Data)
            | Some _, Some _ -> Error "Both Input and Output columns already exist. Select Input or Output explicitly."

    let private mkDataCell (fileName: string) (fileType: string) (selector: string) =
        let data = Data()
        data.FilePath <- Some fileName
        data.Selector <- Some selector
        data.Format <- Some fileType
        data.SelectorFormat <- Some URLs.Data.SelectorFormat.csv
        CompositeCell.createData data

    let private mkEmptyDataCell () =
        let data = Data()
        CompositeCell.createData data

    let applyToTable (table: ArcTable) (input: AnnotationInput) =
        match tryGetTargetHeader table input.TargetColumn with
        | Error errorMessage -> Error errorMessage
        | Ok header ->
            try
                if table.ColumnCount > 0 && input.Selectors.Length > table.RowCount then
                    table.AddRowsEmpty(input.Selectors.Length - table.RowCount)

                let targetRowCount = System.Math.Max(table.RowCount, input.Selectors.Length)

                let values =
                    [|
                        for rowIndex in 0 .. targetRowCount - 1 do
                            if rowIndex < input.Selectors.Length then
                                mkDataCell input.FileName input.FileType input.Selectors.[rowIndex]
                            else
                                mkEmptyDataCell ()
                    |]
                    |> ResizeArray

                table.AddColumn(header, values, forceReplace = true)
                Ok input.Selectors.Length
            with exceptionValue ->
                Error exceptionValue.Message

    let applyToDataMap (dataMap: DataMap) (input: AnnotationInput) =
        try
            if input.Selectors.Length > dataMap.DataContexts.Count then
                let toAdd =
                    Array.init (input.Selectors.Length - dataMap.DataContexts.Count) (fun _ -> DataContext())

                dataMap.DataContexts.AddRange toAdd

            for index in 0 .. input.Selectors.Length - 1 do
                let selector = input.Selectors.[index]
                let dataContext = dataMap.DataContexts.[index]
                dataContext.FilePath <- Some input.FileName
                dataContext.Selector <- Some selector
                dataContext.Format <- Some input.FileType
                dataContext.SelectorFormat <- Some URLs.Data.SelectorFormat.csv

            Ok input.Selectors.Length
        with exceptionValue ->
            Error exceptionValue.Message

[<Erase; Mangle(false)>]
type DataAnnotatorWidget =

    static member private DefaultSeparatorOptions: (string * string)[] = [|
        "\\t", "Tab (\\t)"
        ",", "Comma (,)"
        ";", "Semicolon (;)"
        "|", "Pipe (|)"
    |]

    static member private WidgetContainerClass =
        "swt:flex swt:flex-col swt:gap-3 swt:p-2 swt:w-fit swt:max-w-[95vw]"

    static member private fileTypeFromName(fileName: string) =
        if fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) then
            "text/csv"
        elif fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) then
            "text/tab-separated-values"
        elif fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) then
            "text/plain"
        else
            "text/plain"

    static member private separatorToInput(separator: string) =
        match separator with
        | "\t" -> "\\t"
        | "\n" -> "\\n"
        | "\r" -> "\\r"
        | "\r\n" -> "\\r\\n"
        | "\f" -> "\\f"
        | "\v" -> "\\v"
        | _ -> separator

    static member private parseDataFileBySeparator (separator: string) (dataFile: DataAnnotatorWidgetModel.DataFile) =
        match DataAnnotatorWidgetModel.tryParseDataFile separator dataFile with
        | Ok parsed -> Ok parsed
        | Error errorMessage ->
            let fallbackSeparator = dataFile.ExpectedSeparator

            if separator <> fallbackSeparator then
                DataAnnotatorWidgetModel.tryParseDataFile fallbackSeparator dataFile
            else
                Error errorMessage

    static member private inferDisabledMessage
        (arcFile: ArcFiles)
        (activeView: WidgetHostView)
        (activeTableIndex: int option)
        =
        match activeView with
        | WidgetHostView.MetadataView -> Some "Switch to a table or datamap tab to use Data Annotator."
        | WidgetHostView.PreviewErrorView ->
            Some "Data Annotator is unavailable while the preview is in an error state."
        | WidgetHostView.TableView ->
            match arcFile.TryGetActiveTable(activeTableIndex) with
            | Some _ -> None
            | None -> Some "Select a table tab to use Data Annotator."
        | WidgetHostView.DataMapView ->
            if arcFile.TryGetDataMap() |> Option.isSome then
                None
            else
                Some "No DataMap available for this ARC file."

    [<ReactComponent>]
    static member private Table
        (
            parsedFile: DataAnnotatorWidgetModel.ParsedDataFile,
            selectedTargets: Set<DataAnnotatorWidgetModel.DataTarget>,
            columnCount: int,
            toggleTarget: DataAnnotatorWidgetModel.DataTarget -> unit
        ) =
        Html.table [
            prop.className "swt:table swt:table-xs swt:table-pin-rows"
            prop.children [
                Html.thead [
                    Html.tr [
                        Html.th [ prop.className "swt:w-24"; prop.text "#" ]
                        for columnIndex in 0 .. columnCount - 1 do
                            let target = DataAnnotatorWidgetModel.DataTarget.Column columnIndex
                            let isSelected = selectedTargets.Contains target

                            let label =
                                parsedFile.HeaderRow
                                |> Option.bind (fun row -> row |> Array.tryItem columnIndex)
                                |> Option.defaultValue $"C{columnIndex + 1}"

                            Html.th [
                                prop.key (target.ToReactKey())
                                prop.className [
                                    "swt:cursor-pointer swt:max-w-48 swt:truncate"
                                    if isSelected then
                                        "swt:bg-primary swt:text-primary-content"
                                ]
                                prop.title "Toggle entire column selector"
                                prop.onClick (fun _ -> toggleTarget target)
                                prop.text label
                            ]
                    ]
                ]
                Html.tbody [
                    for rowIndex in 0 .. parsedFile.BodyRows.Length - 1 do
                        let row = parsedFile.BodyRows.[rowIndex]
                        let rowTarget = DataAnnotatorWidgetModel.DataTarget.Row rowIndex
                        let isRowSelected = selectedTargets.Contains rowTarget

                        Html.tr [
                            prop.key (rowTarget.ToReactKey())
                            prop.children [
                                Html.th [
                                    prop.className [
                                        "swt:cursor-pointer swt:font-mono"
                                        if isRowSelected then
                                            "swt:bg-primary swt:text-primary-content"
                                    ]
                                    prop.title "Toggle entire row selector"
                                    prop.onClick (fun _ -> toggleTarget rowTarget)
                                    prop.text (string (rowIndex + 1))
                                ]
                                for columnIndex in 0 .. columnCount - 1 do
                                    let cellTarget = DataAnnotatorWidgetModel.DataTarget.Cell(columnIndex, rowIndex)

                                    let isDirectSelection = selectedTargets.Contains cellTarget

                                    let isInheritedSelection =
                                        selectedTargets.Contains(DataAnnotatorWidgetModel.DataTarget.Column columnIndex)
                                        || selectedTargets.Contains(DataAnnotatorWidgetModel.DataTarget.Row rowIndex)

                                    let isSelected = isDirectSelection || isInheritedSelection

                                    let value = if columnIndex < row.Length then row.[columnIndex] else ""

                                    Html.td [
                                        prop.key (cellTarget.ToReactKey())
                                        prop.className [
                                            "swt:cursor-pointer swt:max-w-64 swt:truncate"
                                            if isSelected then
                                                "swt:bg-primary/70 swt:text-primary-content"
                                        ]
                                        prop.title "Toggle cell selector"
                                        prop.onClick (fun _ -> toggleTarget cellTarget)
                                        prop.text (if String.IsNullOrWhiteSpace value then " " else value)
                                    ]
                            ]
                        ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private FileControllerElements
        (
            pickFile: unit -> JS.Promise<unit>,
            loading: bool,
            selectedFileName: string option,
            dataFile: DataAnnotatorWidgetModel.DataFile option,
            reset: unit -> unit
        ) =
        Html.div [
            prop.className "swt:flex swt:flex-wrap swt:gap-2"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-primary swt:btn-sm"
                    prop.disabled loading
                    prop.text "Choose File"
                    prop.onClick (fun _ -> pickFile () |> Promise.start)
                ]
                Html.input [
                    prop.className "swt:input swt:input-sm swt:grow"
                    prop.readOnly true
                    prop.placeholder "No file selected"
                    prop.value (selectedFileName |> Option.defaultValue "")
                ]
                Html.button [
                    prop.className "swt:btn swt:btn-outline swt:btn-sm"
                    prop.disabled dataFile.IsNone
                    prop.text "Reset"
                    prop.onClick (fun _ -> reset ())
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            arcFile: ArcFiles,
            activeView: WidgetHostView,
            activeTableIndex: int option,
            setArcFile: ArcFiles -> unit,
            services: DataAnnotatorWidgetServices
        ) =

        let dataFile, setDataFile =
            React.useState (None: DataAnnotatorWidgetModel.DataFile option)

        let parsedFile, setParsedFile =
            React.useState (None: DataAnnotatorWidgetModel.ParsedDataFile option)

        let selectedTargets, setSelectedTargets =
            React.useStateWithUpdater (Set.empty<DataAnnotatorWidgetModel.DataTarget>)

        let separatorInput, setSeparatorInput = React.useState ""
        let selectedFileName, setSelectedFileName = React.useState (None: string option)

        let targetColumn, setTargetColumn =
            React.useState DataAnnotatorWidgetModel.TargetColumn.Autodetect

        let loading, setLoading = React.useState false
        let statusMessage, setStatusMessage = React.useState (None: string option)
        let errorMessage, setErrorMessage = React.useState (None: string option)
        let widgetCtx = useWidgetController ()

        let disabledMessage =
            DataAnnotatorWidget.inferDisabledMessage arcFile activeView activeTableIndex

        let reset () =
            setDataFile None
            setParsedFile None
            setSelectedTargets (fun _ -> Set.empty)
            setSeparatorInput ""
            setSelectedFileName None
            setTargetColumn DataAnnotatorWidgetModel.TargetColumn.Autodetect
            setStatusMessage None
            setErrorMessage None

        let setLoadedFile
            (fileName: string)
            (nextDataFile: DataAnnotatorWidgetModel.DataFile)
            (nextParsedFile: DataAnnotatorWidgetModel.ParsedDataFile option)
            =
            setSelectedFileName (Some fileName)
            setDataFile (Some nextDataFile)
            setParsedFile nextParsedFile
            setSeparatorInput (DataAnnotatorWidget.separatorToInput nextDataFile.ExpectedSeparator)
            setSelectedTargets (fun _ -> Set.empty)

        let loadImportedFile (importedFile: ImportedTextFile) = promise {
            setLoading true
            setStatusMessage None
            setErrorMessage None

            try
                let loadedDataFile =
                    DataAnnotatorWidgetModel.DataFile.create (
                        importedFile.Name,
                        DataAnnotatorWidget.fileTypeFromName importedFile.Name,
                        importedFile.Content,
                        float importedFile.Content.Length
                    )

                match
                    DataAnnotatorWidget.parseDataFileBySeparator loadedDataFile.ExpectedSeparator loadedDataFile
                with
                | Ok parsed ->
                    setLoadedFile importedFile.Name loadedDataFile (Some parsed)
                    setStatusMessage (Some $"Loaded {importedFile.Name} ({parsed.BodyRows.Length} rows).")
                | Error message ->
                    setLoadedFile importedFile.Name loadedDataFile None
                    setErrorMessage (Some message)
            finally
                setLoading false
        }

        let pickFile () = promise {
            setStatusMessage None
            setErrorMessage None

            let! importResult = services.pickTextFiles ()

            match importResult with
            | Error message when message <> "Cancelled" -> setErrorMessage (Some $"Failed to pick file: {message}")
            | Error _ -> ()
            | Ok importedFiles when importedFiles.Length = 0 -> setStatusMessage (Some "No file selected.")
            | Ok importedFiles ->
                if importedFiles.Length > 1 then
                    setStatusMessage (Some "Multiple files selected. Using the first one.")

                do! loadImportedFile importedFiles.[0]
        }

        let applySeparator () =
            match dataFile with
            | None -> ()
            | Some loadedDataFile ->
                if String.IsNullOrWhiteSpace separatorInput then
                    setErrorMessage (Some "Separator must not be empty.")
                else
                    match DataAnnotatorWidget.parseDataFileBySeparator separatorInput loadedDataFile with
                    | Ok parsed ->
                        setParsedFile (Some parsed)
                        setSelectedTargets (fun _ -> Set.empty)
                        setErrorMessage None
                        setStatusMessage (Some "Separator updated.")
                    | Error message ->
                        setParsedFile None
                        setErrorMessage (Some message)

        let toggleHeader () =
            match parsedFile with
            | None -> ()
            | Some currentParsedFile ->
                setParsedFile (Some(currentParsedFile.ToggleHeader()))
                setSelectedTargets (fun _ -> Set.empty)
                setStatusMessage None
                setErrorMessage None

        let toggleTarget (target: DataAnnotatorWidgetModel.DataTarget) =
            setSelectedTargets (fun currentTargets ->
                if currentTargets.Contains target then
                    currentTargets.Remove target
                else
                    currentTargets.Add target
            )

            setErrorMessage None

        let submit () =
            if selectedTargets.IsEmpty then
                setErrorMessage (Some "Select at least one target in the preview table.")
            else
                match dataFile, parsedFile with
                | Some loadedDataFile, Some currentParsedFile ->
                    let selectors =
                        DataAnnotatorWidgetModel.selectorsFromTargets currentParsedFile.HeaderRow.IsSome selectedTargets

                    let input: DataAnnotatorWidgetModel.AnnotationInput = {
                        Selectors = selectors
                        FileName = loadedDataFile.DataFileName
                        FileType = loadedDataFile.DataFileType
                        TargetColumn = targetColumn
                    }

                    let applySuccess count =
                        setArcFile (WidgetArcFile.refreshRef arcFile)
                        setErrorMessage None
                        setStatusMessage (Some $"Applied {count} data annotation(s).")
                        widgetCtx.closeWidget WidgetType.DataAnnotator

                    match activeView with
                    | WidgetHostView.TableView ->
                        match arcFile.TryGetActiveTable(activeTableIndex) with
                        | Some(_, table) ->
                            match DataAnnotatorWidgetModel.applyToTable table input with
                            | Ok count -> applySuccess count
                            | Error message -> setErrorMessage (Some message)
                        | None -> setErrorMessage (Some "No active table selected.")
                    | WidgetHostView.DataMapView ->
                        match arcFile.TryGetDataMap() with
                        | Some dataMap ->
                            match DataAnnotatorWidgetModel.applyToDataMap dataMap input with
                            | Ok count -> applySuccess count
                            | Error message -> setErrorMessage (Some message)
                        | None -> setErrorMessage (Some "No DataMap available.")
                    | WidgetHostView.MetadataView ->
                        setErrorMessage (Some "Data annotation cannot be applied in metadata view.")
                    | WidgetHostView.PreviewErrorView ->
                        setErrorMessage (Some "Data annotation cannot be applied while preview is in error state.")
                | _ -> setErrorMessage (Some "Load a file first.")

        let previewSection =
            match parsedFile with
            | None ->
                Html.div [
                    prop.className
                        "swt:rounded-box swt:border swt:border-base-300 swt:flex swt:items-center swt:justify-center swt:text-sm swt:opacity-70 swt:min-h-24 swt:px-3 swt:py-4"
                    prop.text "Load a data file to preview selectable targets."
                ]
            | Some currentParsedFile ->
                let headerCount =
                    currentParsedFile.HeaderRow |> Option.map _.Length |> Option.defaultValue 0

                let bodyCount =
                    currentParsedFile.BodyRows
                    |> Array.fold (fun count row -> max count row.Length) 0

                let columnCount = max headerCount bodyCount

                Html.div [
                    prop.className "swt:overflow-auto swt:rounded-box swt:border swt:border-base-300 swt:max-h-[55vh]"
                    prop.children [
                        if currentParsedFile.BodyRows.Length = 0 || columnCount = 0 then
                            Html.div [
                                prop.className
                                    "swt:flex swt:items-center swt:justify-center swt:h-full swt:text-sm swt:opacity-70"
                                prop.text "Parsed file contains no data rows."
                            ]
                        else
                            DataAnnotatorWidget.Table(currentParsedFile, selectedTargets, columnCount, toggleTarget)
                    ]
                ]

        let selectedTargetCount = selectedTargets.Count

        let canSubmit =
            disabledMessage.IsNone
            && dataFile.IsSome
            && parsedFile.IsSome
            && (not loading)
            && selectedTargetCount > 0

        match disabledMessage with
        | Some message ->
            Html.div [
                prop.className DataAnnotatorWidget.WidgetContainerClass
                prop.children [
                    Html.h3 [
                        prop.className "swt:text-xl swt:font-bold"
                        prop.text "Data Annotator"
                    ]
                    Html.div [
                        prop.className "swt:alert swt:alert-warning swt:text-sm"
                        prop.children [ Html.text message ]
                    ]
                ]
            ]
        | None ->
            Html.div [
                prop.className DataAnnotatorWidget.WidgetContainerClass
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-wrap swt:items-end swt:gap-2"
                        prop.children [
                            Html.h3 [
                                prop.className "swt:text-xl swt:font-bold"
                                prop.text "Data Annotator"
                            ]
                            Html.span [
                                prop.className "swt:text-xs swt:opacity-70 swt:ml-auto"
                                prop.textf "%d target(s) selected" selectedTargetCount
                            ]
                        ]
                    ]
                    DataAnnotatorWidget.FileControllerElements(
                        pickFile,
                        loading,
                        selectedFileName,
                        dataFile,
                        reset
                    )
                    if dataFile.IsSome then
                        Html.div [
                            prop.className "swt:flex swt:flex-wrap swt:gap-2 swt:items-center"
                            prop.children [
                                let hasCustomSeparator =
                                    (DataAnnotatorWidget.DefaultSeparatorOptions
                                     |> Array.exists (fun (separator, _) -> separator = separatorInput)
                                     |> not)
                                    && (String.IsNullOrWhiteSpace separatorInput |> not)

                                Html.select [
                                    prop.className "swt:select swt:select-sm swt:w-44"
                                    prop.valueOrDefault separatorInput
                                    prop.onChange setSeparatorInput
                                    prop.children [
                                        if hasCustomSeparator then
                                            Html.option [
                                                prop.value separatorInput
                                                prop.text $"Custom ({separatorInput})"
                                            ]

                                        for separator, label in DataAnnotatorWidget.DefaultSeparatorOptions do
                                            Html.option [ prop.value separator; prop.text label ]
                                    ]
                                ]

                                Html.input [
                                    prop.className "swt:input swt:input-sm swt:w-32"
                                    prop.placeholder "separator"
                                    prop.value separatorInput
                                    prop.onChange setSeparatorInput
                                    prop.onKeyDown (key.enter, fun _ -> applySeparator ())
                                ]

                                Html.button [
                                    prop.className "swt:btn swt:btn-sm"
                                    prop.text "Apply Separator"
                                    prop.onClick (fun _ -> applySeparator ())
                                    prop.disabled (String.IsNullOrWhiteSpace separatorInput)
                                ]

                                Html.button [
                                    prop.className "swt:btn swt:btn-sm"
                                    prop.text (
                                        match parsedFile with
                                        | Some currentParsedFile when currentParsedFile.HeaderRow.IsSome -> "Header: On"
                                        | _ -> "Header: Off"
                                    )
                                    prop.onClick (fun _ -> toggleHeader ())
                                    prop.disabled parsedFile.IsNone
                                ]

                                if activeView = WidgetHostView.TableView then
                                    Html.select [
                                        prop.className "swt:select swt:select-sm"
                                        prop.valueOrDefault (string targetColumn)
                                        prop.onChange (fun (value: string) ->
                                            setTargetColumn (DataAnnotatorWidgetModel.TargetColumn.fromString value)
                                        )
                                        prop.children [
                                            Html.option [
                                                prop.value (string DataAnnotatorWidgetModel.TargetColumn.Autodetect)
                                                prop.text (string DataAnnotatorWidgetModel.TargetColumn.Autodetect)
                                            ]
                                            Html.option [
                                                prop.value (string DataAnnotatorWidgetModel.TargetColumn.Input)
                                                prop.text (string DataAnnotatorWidgetModel.TargetColumn.Input)
                                            ]
                                            Html.option [
                                                prop.value (string DataAnnotatorWidgetModel.TargetColumn.Output)
                                                prop.text (string DataAnnotatorWidgetModel.TargetColumn.Output)
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    previewSection
                    if loading then
                        Html.div [
                            prop.className "swt:alert swt:alert-info swt:text-xs"
                            prop.children [ Html.text "Loading file..." ]
                        ]
                    if statusMessage.IsSome then
                        Html.div [
                            prop.className "swt:alert swt:alert-success swt:text-xs"
                            prop.children [ Html.text statusMessage.Value ]
                        ]
                    if errorMessage.IsSome then
                        Html.div [
                            prop.className "swt:alert swt:alert-error swt:text-xs"
                            prop.children [ Html.text errorMessage.Value ]
                        ]
                    Html.div [
                        prop.className "swt:flex swt:gap-2"
                        prop.children [
                            Html.button [
                                prop.className "swt:btn swt:btn-outline"
                                prop.text "Cancel"
                                prop.onClick (fun _ -> widgetCtx.closeWidget WidgetType.DataAnnotator)
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                prop.disabled (not canSubmit)
                                prop.text "Submit"
                                prop.onClick (fun _ -> submit ())
                            ]
                        ]
                    ]
                ]
            ]
